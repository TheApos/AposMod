// Weapon Mastery: every weapon (item class, any quality: a machete of quality 2 and 6 share it) gains mastery from
// kills made while it is in your hands, up to level 10. Each level gives 1 point for that weapon's skill tree:
// 20 nodes, so about half can be taken. The tree shape is shared by a weapon class (blades, clubs, ... 13 classes);
// the points are per weapon. Alt+K opens the window for the weapon in hand (XUiC_AposMastery). Resetting a tree costs
// Eridium and works only within 5 m of an Eridium Forge.
// - Stat nodes go to cvars on the player (aposM*), read by buffAposMastery (buffs.xml) while that weapon is held.
// - Hit nodes (crit, combo, elements, ...) are applied from AposCombatDamagePatch (AposCombat.cs).
// Progress is saved in <save game folder>/AposMastery.dat (text: item name, kills, learned nodes as a bit mask).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class AposMastery : MonoBehaviour
{
    public const int MaxLevel = 10;
    public const int NodeCount = 20;
    public const int ResetCostPerPoint = 5;
    public const float ForgeRange = 5f;
    const string FileName = "AposMastery.dat";
    const int EliteKills = 5;
    const float ComboWindow = 3f;
    const int ComboMax = 5;
    const int NthHit = 5;
    const float ExecuteBelow = 0.35f;
    const float SplashRadius = 3f;
    const float SaveDelay = 10f;

    // kills needed for levels 1..10
    public static readonly int[] KillsFor = { 10, 30, 60, 100, 150, 220, 300, 400, 520, 660 };

    // ===== tree layout: 5 columns x 6 rows (row 0 at the bottom). Branches A (column 0), B (2), C (4) go up from the
    // root; links (columns 1 and 3) join them on rows 1 and 3. Row 3 = notable, row 5 = keystone. =====
    public static readonly int[] Col = { 2, 0, 1, 2, 3, 4, 0, 2, 4, 0, 1, 2, 3, 4, 0, 2, 4, 0, 2, 4 };
    public static readonly int[] Row = { 0, 1, 1, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 5, 5, 5 };
    static readonly int[][] Edges =
    {
        new[] { 0, 1 }, new[] { 0, 3 }, new[] { 0, 5 },
        new[] { 1, 2 }, new[] { 2, 3 }, new[] { 3, 4 }, new[] { 4, 5 },
        new[] { 1, 6 }, new[] { 3, 7 }, new[] { 5, 8 },
        new[] { 6, 9 }, new[] { 7, 11 }, new[] { 8, 13 },
        new[] { 9, 10 }, new[] { 10, 11 }, new[] { 11, 12 }, new[] { 12, 13 },
        new[] { 9, 14 }, new[] { 11, 15 }, new[] { 13, 16 },
        new[] { 14, 17 }, new[] { 15, 18 }, new[] { 16, 19 },
    };

    public static bool IsBig(int node) { return Row[node] == 3 || Row[node] == 5; }

    // ===== node kinds =====
    public enum Kind
    {
        Dmg, Speed, Stamina, Reload, Mag, Handling, Dismember,                       // stats (buffAposMastery)
        Crit, CritDmg, Execute, Opener, Head, Combo, Nth, Leech, Splash,              // hit damage / effects
        Bleed, DeepBleed, Burn, Shock, Acid, Stun, Knock,                             // chance to apply a buff
        Haste, Ammo,                                                                  // on kill
    }

    public struct Node
    {
        public Kind Kind;
        public int Value;
    }

    // ===== weapon classes and their trees (20 nodes, index order as Col/Row above) =====
    public static readonly string[] Classes =
        { "Blades", "Clubs", "Sledges", "Spears", "Knuckles", "Batons", "Pistols", "Shotguns", "Rifles", "MG", "Bows", "Crossbows", "Explosives" };

    static readonly string[] TreeText =
    {
        // Blades: A speed, B bleeding, C crits
        "Dmg 5|Speed 5|Stamina -10|Dmg 6|Dismember 10|Crit 5|Speed 6|Bleed 20|Leech 3|Haste 20|Dmg 6|Execute 30|Crit 5|Opener 60|Stamina -15|Bleed 20|Leech 4|Combo 8|DeepBleed 35|CritDmg 100",
        // Clubs: A stun, B knock / splash, C tempo
        "Dmg 5|Stun 8|Dmg 5|Dmg 6|Stamina -10|Speed 5|Stun 8|Dismember 15|Stamina -10|Execute 30|Crit 5|Knock 10|Speed 5|Haste 20|Dmg 8|Dmg 8|Speed 6|Nth 150|Splash 30|Combo 8",
        // Sledges: A knockdown, B raw damage, C tempo
        "Dmg 5|Knock 8|Stamina -10|Dmg 6|Dismember 15|Speed 5|Knock 8|Dmg 6|Stamina -12|Splash 25|Dmg 6|Execute 35|Stun 8|Haste 25|Splash 15|Dmg 10|Speed 6|Nth 200|Opener 100|Combo 10",
        // Spears: A tempo, B reach / splash, C crits
        "Dmg 5|Speed 5|Dismember 10|Dmg 6|Stamina -10|Crit 5|Speed 5|Bleed 15|Crit 5|Haste 20|Dmg 6|Splash 20|Knock 8|Head 40|Stamina -15|Dmg 8|Crit 6|Combo 8|Splash 35|CritDmg 100",
        // Knuckles: A combo, B damage, C control / leech
        "Speed 5|Speed 5|Stamina -10|Dmg 6|Leech 3|Stun 6|Combo 4|Dmg 6|Stun 6|Haste 25|Crit 5|Execute 30|Leech 3|Knock 10|Speed 6|Dmg 8|Stamina -15|Combo 10|Nth 150|Leech 8",
        // Batons: A shock, B damage, C control
        "Dmg 5|Shock 10|Speed 5|Dmg 6|Stamina -10|Stun 6|Shock 10|Dmg 6|Stun 6|Splash 20|Crit 5|Execute 30|Speed 5|Knock 10|Shock 15|Dmg 8|Stamina -15|Splash 40|Nth 150|Combo 8",
        // Pistols: A headshots, B reload / ammo, C tempo
        "Dmg 5|Head 15|Handling 10|Reload 10|Mag 15|Speed 5|Head 15|Reload 10|Speed 6|Crit 8|Dmg 6|Ammo 2|Handling 10|Haste 20|Head 20|Mag 20|Dmg 8|CritDmg 100|Combo 6|Nth 150",
        // Shotguns: A damage / fire, B reload, C control
        "Dmg 5|Dismember 15|Handling 10|Reload 10|Mag 20|Knock 6|Dmg 6|Reload 10|Knock 6|Execute 30|Dmg 6|Ammo 1|Stun 6|Opener 50|Dmg 8|Mag 25|Splash 15|Burn 25|Reload 25|Splash 30",
        // Rifles: A headshots, B reload / ammo, C first shot
        "Dmg 5|Head 15|Handling 15|Reload 10|Dmg 5|Opener 30|Head 15|Reload 10|Opener 30|Crit 8|Handling 10|Ammo 1|Mag 20|Execute 30|Head 25|Speed 10|Dmg 8|CritDmg 150|Splash 40|Opener 100",
        // Machine guns: A control, B magazine, C tempo
        "Dmg 5|Handling 10|Speed 5|Mag 15|Reload 10|Dmg 5|Handling 10|Mag 15|Speed 5|Combo 3|Dmg 5|Ammo 3|Crit 5|Shock 8|Handling 15|Reload 15|Speed 6|Combo 5|Mag 40|Burn 15",
        // Bows: A headshots, B splinters, C bleeding / fire
        "Dmg 5|Head 15|Speed 5|Dmg 6|Handling 10|Bleed 15|Head 15|Dmg 6|Bleed 15|Crit 8|Opener 50|Splash 20|Speed 5|Acid 15|Head 20|Dmg 8|Burn 15|CritDmg 150|Splash 40|DeepBleed 30",
        // Crossbows: A headshots, B damage / knock, C reload / shock
        "Dmg 5|Head 15|Reload 10|Dmg 6|Handling 10|Reload 10|Head 15|Dmg 6|Reload 15|Crit 8|Opener 60|Knock 10|Stun 8|Haste 20|Head 25|Dmg 10|Shock 15|CritDmg 150|Splash 40|Combo 8",
        // Explosives: A damage, B elements, C reload
        "Dmg 5|Dmg 6|Reload 10|Burn 10|Knock 8|Reload 10|Dmg 6|Burn 10|Stun 8|Execute 30|Dmg 6|Shock 15|Knock 8|Ammo 1|Dmg 10|Acid 15|Reload 20|Opener 60|Splash 20|Reload 40",
    };

    public static readonly Node[][] Trees = ParseTrees();

    static Node[][] ParseTrees()
    {
        Node[][] trees = new Node[TreeText.Length][];
        for (int t = 0; t < TreeText.Length; t++)
        {
            string[] parts = TreeText[t].Split('|');
            if (parts.Length != NodeCount) throw new Exception("AposMastery: tree " + Classes[t] + " has " + parts.Length + " nodes");
            trees[t] = new Node[NodeCount];
            for (int i = 0; i < NodeCount; i++)
            {
                string[] kv = parts[i].Split(' ');
                trees[t][i] = new Node { Kind = (Kind)Enum.Parse(typeof(Kind), kv[0]), Value = int.Parse(kv[1], CultureInfo.InvariantCulture) };
            }
        }
        return trees;
    }

    // ===== weapon -> class =====
    static readonly string[] ClassTags =
        { "perkDeepCuts", "perkPummelPete", "perkSkullCrusher", "perkJavelinMaster", "perkBrawler", "perkFlurryOfIntellect",
          "perkGunslinger", "perkBoomstick", "perkDeadEye", "perkMachineGunner", "perkArchery", "perkArchery", "perkDemolitionsExpert" };
    static FastTags<TagGroup.Global>[] classTags;

    // -1 = not a mastery weapon (tools, fists, turrets)
    public static int ClassOf(ItemClass ic)
    {
        if (ic == null || ic.ItemTags.IsEmpty) return -1;
        if (classTags == null)
        {
            classTags = new FastTags<TagGroup.Global>[ClassTags.Length];
            for (int i = 0; i < ClassTags.Length; i++) classTags[i] = FastTags<TagGroup.Global>.Parse(ClassTags[i]);
        }
        for (int i = 0; i < ClassTags.Length; i++)
        {
            if (!ic.ItemTags.Test_AnySet(classTags[i])) continue;
            if (ClassTags[i] == "perkArchery")
            {
                // bows are catapults, crossbows are launchers
                bool bow = ic.Actions != null && ic.Actions.Length > 0 && ic.Actions[0] is ItemActionCatapult;
                return bow ? Array.IndexOf(Classes, "Bows") : Array.IndexOf(Classes, "Crossbows");
            }
            return i;
        }
        return -1;
    }

    // ===== progress per weapon =====
    public class Progress
    {
        public int Kills;
        public int Mask;
    }

    static readonly Dictionary<string, Progress> weapons = new Dictionary<string, Progress>();
    static string file;
    static bool dirty;
    static float saveAt;

    public static Progress Get(string itemName)
    {
        Progress p;
        if (!weapons.TryGetValue(itemName, out p))
        {
            p = new Progress();
            weapons[itemName] = p;
        }
        return p;
    }

    public static int Level(Progress p)
    {
        int level = 0;
        while (level < MaxLevel && p.Kills >= KillsFor[level]) level++;
        return level;
    }

    public static int Spent(Progress p)
    {
        int n = 0;
        for (int i = 0; i < NodeCount; i++) if ((p.Mask & (1 << i)) != 0) n++;
        return n;
    }

    public static bool Has(Progress p, int node) { return (p.Mask & (1 << node)) != 0; }

    // the root is always open; any other node needs a learned neighbour
    public static bool CanReach(Progress p, int node)
    {
        if (node == 0) return true;
        for (int i = 0; i < Edges.Length; i++)
        {
            int a = Edges[i][0], b = Edges[i][1];
            if ((a == node && Has(p, b)) || (b == node && Has(p, a))) return true;
        }
        return false;
    }

    public static bool CanLearn(Progress p, int node)
    {
        return !Has(p, node) && CanReach(p, node) && Spent(p) < Level(p);
    }

    public static void Learn(string itemName, int node)
    {
        Progress p = Get(itemName);
        if (!CanLearn(p, node)) return;
        p.Mask |= 1 << node;
        appliedFor = null; // refresh the stat cvars
        Save();
    }

    public static void Reset(string itemName)
    {
        Get(itemName).Mask = 0;
        appliedFor = null;
        Save();
    }

    // ===== the held weapon's learned nodes =====
    static string appliedFor;
    static int appliedMask = -1;
    static int heldClass = -1;
    static readonly int[] held = new int[Enum.GetValues(typeof(Kind)).Length];

    static float Sum(Kind k) { return held[(int)k]; }

    static void Recalc(ItemClass ic)
    {
        Array.Clear(held, 0, held.Length);
        heldClass = ClassOf(ic);
        if (heldClass < 0) return;
        Progress p = Get(ic.Name);
        Node[] tree = Trees[heldClass];
        for (int i = 0; i < NodeCount; i++)
        {
            if (Has(p, i)) held[(int)tree[i].Kind] += tree[i].Value;
        }
    }

    static void ApplyStats(EntityPlayerLocal player)
    {
        ItemClass ic = player.inventory.holdingItem;
        string name = ic != null ? ic.Name : "";
        int mask = ClassOf(ic) >= 0 ? Get(name).Mask : 0;
        if (name == appliedFor && mask == appliedMask) return;
        appliedFor = name;
        appliedMask = mask;
        Recalc(ic);
        player.Buffs.SetCustomVar("aposMDmg", Sum(Kind.Dmg) / 100f);
        player.Buffs.SetCustomVar("aposMSpeed", Sum(Kind.Speed) / 100f);
        player.Buffs.SetCustomVar("aposMStam", Sum(Kind.Stamina) / 100f);
        player.Buffs.SetCustomVar("aposMReload", Sum(Kind.Reload) / 100f);
        player.Buffs.SetCustomVar("aposMMag", Sum(Kind.Mag) / 100f);
        player.Buffs.SetCustomVar("aposMSpread", -Sum(Kind.Handling) / 100f);
        player.Buffs.SetCustomVar("aposMDism", Sum(Kind.Dismember) / 100f);
    }

    // ===== hits by the local player with the held weapon (AposCombatDamagePatch) =====
    static int comboStacks;
    static float comboTime;
    static int hitCount;

    static bool Applies(EntityPlayer attacker, EntityAlive target, DamageSource source)
    {
        GameManager gm = GameManager.Instance;
        return attacker != null && target != null && !(target is EntityPlayer) && gm.World != null
            && attacker == gm.World.GetPrimaryPlayer() && heldClass >= 0
            && attacker.inventory.holdingItem != null && attacker.inventory.holdingItem.Name == appliedFor
            && (AposShadow.IsWeaponHit(source) || source.GetDamageType() == EnumDamageTypes.Heat && heldClass == Array.IndexOf(Classes, "Explosives"));
    }

    // Prefix: damage before it is applied
    public static int ModifyHit(EntityPlayer attacker, EntityAlive target, DamageSource source, int strength)
    {
        if (!Applies(attacker, target, source)) return strength;
        float mult = 1f;
        float max = target.GetMaxHealth();
        if (Sum(Kind.Execute) > 0 && target.Health < max * ExecuteBelow) mult += Sum(Kind.Execute) / 100f;
        if (Sum(Kind.Opener) > 0 && target.Health >= max) mult += Sum(Kind.Opener) / 100f;
        if (Sum(Kind.Head) > 0 && (source.GetEntityDamageBodyPart(target) & EnumBodyPartHit.Head) != 0) mult += Sum(Kind.Head) / 100f;

        if (Sum(Kind.Combo) > 0)
        {
            if (Time.time - comboTime > ComboWindow) comboStacks = 0;
            mult += comboStacks * Sum(Kind.Combo) / 100f;
            comboStacks = Math.Min(ComboMax, comboStacks + 1);
            comboTime = Time.time;
        }
        if (Sum(Kind.Nth) > 0)
        {
            hitCount++;
            if (hitCount >= NthHit)
            {
                hitCount = 0;
                mult += Sum(Kind.Nth) / 100f;
            }
        }
        if (Sum(Kind.Crit) > 0 && UnityEngine.Random.value * 100f < Sum(Kind.Crit))
        {
            mult *= 2f + Sum(Kind.CritDmg) / 100f;
        }
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    // Postfix: after the damage (dealt = health actually lost); returns the extra leech share
    public static float AfterHit(EntityPlayer attacker, EntityAlive target, DamageSource source, int dealt, bool fatal)
    {
        if (!Applies(attacker, target, source)) return 0f;
        if (!fatal)
        {
            Chance(target, Kind.Bleed, "buffInjuryBleeding", -1f);
            Chance(target, Kind.DeepBleed, "buffAposDeepBleed", -1f);
            Chance(target, Kind.Burn, "buffBurningElement", -1f);
            Chance(target, Kind.Shock, "buffShocked", 3f);
            Chance(target, Kind.Acid, "buffAposCorroded", 6f);
            Chance(target, Kind.Stun, "buffInjuryStunned01", 1.5f);
            Chance(target, Kind.Knock, "buffInjuryKnockdown01", -1f);
        }
        if (Sum(Kind.Splash) > 0 && dealt > 0)
        {
            int share = Mathf.RoundToInt(dealt * Sum(Kind.Splash) / 100f);
            if (share > 0) AposCombat.SplashAround(target, SplashRadius, share);
        }
        if (fatal) OnKill(attacker as EntityPlayerLocal, target);
        return Sum(Kind.Leech) / 100f;
    }

    static void Chance(EntityAlive target, Kind kind, string buff, float duration)
    {
        if (Sum(kind) <= 0 || UnityEngine.Random.value * 100f >= Sum(kind)) return;
        target.Buffs.AddBuff(buff, -1, true, false, duration);
    }

    static void OnKill(EntityPlayerLocal player, EntityAlive target)
    {
        if (player == null) return;
        if (Sum(Kind.Haste) > 0)
        {
            player.Buffs.SetCustomVar("aposMHaste", Sum(Kind.Haste) / 100f);
            player.Buffs.AddBuff("buffAposMasteryHaste");
        }
        if (Sum(Kind.Ammo) > 0) AposAmmo.LoadRounds(player, (int)Sum(Kind.Ammo));
    }

    // A kill with the held weapon (any damage type: burning from it counts too) gives mastery
    public static void CountKill(EntityPlayer attacker, EntityAlive target)
    {
        GameManager gm = GameManager.Instance;
        if (attacker == null || target == null || target is EntityPlayer || gm.World == null || attacker != gm.World.GetPrimaryPlayer()) return;
        ItemClass ic = attacker.inventory.holdingItem;
        if (ClassOf(ic) < 0) return;
        Progress p = Get(ic.Name);
        int before = Level(p);
        p.Kills += AposElites.IsElite(target) ? EliteKills : 1;
        int after = Level(p);
        if (after > before)
        {
            GameManager.ShowTooltip(attacker as EntityPlayerLocal, string.Format(Localization.Get("aposMasteryLevelUp"), ic.GetLocalizedItemName(), after));
            Save();
        }
        else MarkDirty();
    }

    // ===== Eridium Forge nearby (reset) =====
    public static bool NearForge(EntityPlayerLocal player)
    {
        World world = GameManager.Instance.World;
        Vector3i c = World.worldToBlockPos(player.position);
        int r = Mathf.CeilToInt(ForgeRange);
        for (int x = -r; x <= r; x++)
            for (int y = -2; y <= 3; y++)
                for (int z = -r; z <= r; z++)
                {
                    if (world.GetBlock(c + new Vector3i(x, y, z)).Block is BlockAposEridiumForge) return true;
                }
        return false;
    }

    // ===== MonoBehaviour: load/save, hotkey, stat cvars =====
    void Update()
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        if (player == null)
        {
            if (dirty) Save();
            file = null;
            appliedFor = null;
            return;
        }
        string path = Path.Combine(GameIO.GetSaveGameDir(), FileName);
        if (path != file) Load(path);
        if (dirty && Time.time >= saveAt) Save();

        if (!player.Buffs.HasBuff("buffAposMastery")) player.Buffs.AddBuff("buffAposMastery");
        ApplyStats(player);

        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(player);
        if (ui == null || ui.windowManager.IsInputActive()) return;
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (alt && Input.GetKeyDown(KeyCode.K)) ui.windowManager.Open("aposMastery", true);
    }

    static void MarkDirty()
    {
        if (!dirty) saveAt = Time.time + SaveDelay;
        dirty = true;
    }

    static void Load(string path)
    {
        file = path;
        weapons.Clear();
        appliedFor = null;
        dirty = false;
        if (!File.Exists(path)) return;
        try
        {
            foreach (string line in File.ReadAllLines(path))
            {
                string[] f = line.Split('\t');
                if (f.Length != 3) continue;
                weapons[f[0]] = new Progress
                {
                    Kills = int.Parse(f[1], CultureInfo.InvariantCulture),
                    Mask = int.Parse(f[2], CultureInfo.InvariantCulture),
                };
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[AposMod] Mastery file could not be read, it was kept as " + path + ".bad: " + e.Message);
            try { File.Copy(path, path + ".bad", true); } catch (Exception) { }
            weapons.Clear();
        }
    }

    static void Save()
    {
        dirty = false;
        if (file == null) return;
        try
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, Progress> kv in weapons)
            {
                if (kv.Value.Kills == 0 && kv.Value.Mask == 0) continue;
                sb.Append(kv.Key).Append('\t').Append(kv.Value.Kills.ToString(CultureInfo.InvariantCulture))
                  .Append('\t').Append(kv.Value.Mask.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
            string tmp = file + ".tmp";
            File.WriteAllText(tmp, sb.ToString());
            // replace in one step so a crash mid-write never leaves a half file
            if (File.Exists(file)) File.Delete(file);
            File.Move(tmp, file);
        }
        catch (Exception e)
        {
            Debug.LogError("[AposMod] Mastery could not be saved: " + e);
        }
    }

    // ===== texts =====
    public static string NodeShort(Node n)
    {
        return Localization.Get("aposMK" + n.Kind + "Short") + "\n" + ValueText(n);
    }

    public static string NodeLong(Node n)
    {
        return string.Format(Localization.Get("aposMK" + n.Kind), Math.Abs(n.Value));
    }

    static string ValueText(Node n)
    {
        if (n.Kind == Kind.Ammo) return "+" + n.Value;
        return (n.Value > 0 ? "+" : "") + n.Value + "%";
    }
}

// Mastery window (Alt+K): the tree of the weapon in hand, its level, a node's description, Learn and Reset
public class XUiC_AposMastery : XUiController
{
    readonly XUiC_SimpleButton[] nodes = new XUiC_SimpleButton[AposMastery.NodeCount];
    XUiV_Label weaponLabel, levelLabel, pointsLabel, nodeTitle, nodeDesc, resetHint;
    XUiC_SimpleButton learn, reset;
    int selected = -1;
    float nextRefresh, nextForgeCheck;
    bool nearForge;

    public override void Init()
    {
        base.Init();
        weaponLabel = Label(this, "lblWeapon");
        levelLabel = Label(this, "lblLevel");
        pointsLabel = Label(this, "lblPoints");
        nodeTitle = Label(this, "lblNodeTitle");
        nodeDesc = Label(this, "lblNodeDesc");
        resetHint = Label(this, "lblResetHint");
        for (int i = 0; i < nodes.Length; i++)
        {
            int n = i;
            nodes[i] = Button("btnNode" + i, () => selected = n);
        }
        learn = Button("btnLearn", () =>
        {
            ItemClass ic = Held();
            if (ic != null && selected >= 0) AposMastery.Learn(ic.Name, selected);
        });
        reset = Button("btnReset", () =>
        {
            EntityPlayerLocal p = xui.playerUI.entityPlayer;
            ItemClass ic = Held();
            if (p == null || ic == null || !nearForge) return;
            int cost = ResetCost(ic);
            if (cost <= 0 || XUiC_AposForge.Count(p, AposElites.Eridium) < cost) return;
            XUiC_AposForge.Take(p, AposElites.Eridium, cost);
            AposMastery.Reset(ic.Name);
        });
        SetText(learn, Localization.Get("aposMasteryLearn"));
    }

    XUiC_SimpleButton Button(string id, Action onPress)
    {
        XUiC_SimpleButton b = GetChildById(id) as XUiC_SimpleButton;
        if (b == null)
        {
            Debug.LogError("[AposMod] Mastery window: no button " + id);
            return null;
        }
        b.OnPressed += (_sender, _mouseButton) => { onPress(); Refresh(); };
        return b;
    }

    static XUiV_Label Label(XUiController parent, string id)
    {
        XUiController c = parent.GetChildById(id);
        return c != null ? c.ViewComponent as XUiV_Label : null;
    }

    static void SetText(XUiC_SimpleButton b, string text)
    {
        if (b == null) return;
        XUiV_Label l = Label(b, "btnLabel");
        if (l != null) l.Text = text;
    }

    ItemClass Held()
    {
        EntityPlayerLocal p = xui != null && xui.playerUI != null ? xui.playerUI.entityPlayer : null;
        ItemClass ic = p != null ? p.inventory.holdingItem : null;
        return AposMastery.ClassOf(ic) >= 0 ? ic : null;
    }

    static int ResetCost(ItemClass ic)
    {
        return AposMastery.Spent(AposMastery.Get(ic.Name)) * AposMastery.ResetCostPerPoint;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        selected = -1;
        nextForgeCheck = 0f;
        Refresh();
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + 0.3f;
        Refresh();
    }

    void Refresh()
    {
        EntityPlayerLocal p = xui != null && xui.playerUI != null ? xui.playerUI.entityPlayer : null;
        if (p == null) return;
        if (Time.time >= nextForgeCheck)
        {
            nextForgeCheck = Time.time + 2f;
            nearForge = AposMastery.NearForge(p);
        }
        ItemClass ic = Held();
        if (ic == null)
        {
            if (weaponLabel != null) weaponLabel.Text = Localization.Get("aposMasteryNoWeapon");
            if (levelLabel != null) levelLabel.Text = "";
            if (pointsLabel != null) pointsLabel.Text = "";
            if (nodeTitle != null) nodeTitle.Text = "";
            if (nodeDesc != null) nodeDesc.Text = "";
            if (resetHint != null) resetHint.Text = "";
            for (int i = 0; i < nodes.Length; i++) SetText(nodes[i], "");
            if (learn != null) learn.Enabled = false;
            if (reset != null) reset.Enabled = false;
            return;
        }

        int cls = AposMastery.ClassOf(ic);
        AposMastery.Node[] tree = AposMastery.Trees[cls];
        AposMastery.Progress pr = AposMastery.Get(ic.Name);
        int level = AposMastery.Level(pr), spent = AposMastery.Spent(pr);

        if (weaponLabel != null) weaponLabel.Text = ic.GetLocalizedItemName() + "\n[A0A0A0]" + Localization.Get("aposMasteryClass" + AposMastery.Classes[cls]) + "[-]";
        if (levelLabel != null)
        {
            string next = level < AposMastery.MaxLevel
                ? string.Format(Localization.Get("aposMasteryKills"), pr.Kills, AposMastery.KillsFor[level])
                : string.Format(Localization.Get("aposMasteryKillsMax"), pr.Kills);
            levelLabel.Text = string.Format(Localization.Get("aposMasteryLevel"), level, AposMastery.MaxLevel) + "\n" + next;
        }
        if (pointsLabel != null) pointsLabel.Text = string.Format(Localization.Get("aposMasteryPoints"), level - spent, spent, AposMastery.NodeCount);

        for (int i = 0; i < nodes.Length; i++)
        {
            string color = AposMastery.Has(pr, i) ? "[FFD24D]" : AposMastery.CanReach(pr, i) ? "[7CFF7C]" : "[C8C8C8]";
            string text = AposMastery.NodeShort(tree[i]);
            if (AposMastery.IsBig(i)) text = "[b]" + text + "[/b]";
            if (i == selected) text = "> " + text.Replace("\n", " <\n");
            SetText(nodes[i], color + text + "[-]");
        }

        if (selected >= 0)
        {
            AposMastery.Node n = tree[selected];
            string state = AposMastery.Has(pr, selected) ? Localization.Get("aposMasteryLearned")
                : !AposMastery.CanReach(pr, selected) ? Localization.Get("aposMasteryLocked")
                : level - spent <= 0 ? Localization.Get("aposMasteryNoPoints") : "";
            string kindTitle = Localization.Get("aposMK" + n.Kind + "Short");
            if (AposMastery.IsBig(selected)) kindTitle = "[FFD24D]" + kindTitle + "[-]";
            if (nodeTitle != null) nodeTitle.Text = kindTitle;
            if (nodeDesc != null) nodeDesc.Text = AposMastery.NodeLong(n) + (state != "" ? "\n\n[A0A0A0]" + state + "[-]" : "");
        }
        else
        {
            if (nodeTitle != null) nodeTitle.Text = "";
            if (nodeDesc != null) nodeDesc.Text = "[A0A0A0]" + Localization.Get("aposMasteryPick") + "[-]";
        }
        if (learn != null) learn.Enabled = selected >= 0 && AposMastery.CanLearn(pr, selected);

        int cost = ResetCost(ic);
        SetText(reset, string.Format(Localization.Get("aposMasteryReset"), cost));
        if (reset != null) reset.Enabled = cost > 0 && nearForge && XUiC_AposForge.Count(p, AposElites.Eridium) >= cost;
        if (resetHint != null) resetHint.Text = nearForge ? "" : "[A0A0A0]" + Localization.Get("aposMasteryResetHint") + "[-]";
    }
}

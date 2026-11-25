// Weapon Mastery 2.0 (docs/Design-Mastery2.md): kills made with a weapon in your hands give mastery to its whole CLASS
// (every pistol and SMG shares one level and one tree). Levels 1..25, one point per level. Each class has a tree of
// two branches (A, B: tiers 1-5, skills with 1-3 ranks) and three hybrids in the middle. A tier opens after 0/3/6/9/12
// points in the lower tiers of its branch; hybrids need 5 (the big one 8) points in each branch.
// Alt+K opens the window for the weapon in hand (XUiC_AposMastery). Near an Eridium Forge one skill can be reset for
// 20 Eridium, the whole tree for 250.
// - The skills of a class are applied by that class's file (AposPistol.cs, ...) while a weapon of the class is held.
// - Progress is saved in <save>/AposMastery.dat (a co-op client: its local folder for that server, AposSettings.PlayerDataDir):
//   "v2", then one line per class: name, kills, ranks. A file of the old 20-node trees (item name, kills, bit mask) is
//   converted once: kills of all weapons of a class are added up, the old points come back (the trees are new).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using HarmonyLib;
using UnityEngine;

public class AposMastery : MonoBehaviour
{
    public const int MaxLevel = 25;
    public const int ResetOneCost = 20;
    public const int ResetAllCost = 250;
    public const float ForgeRange = 5f;
    const string FileName = "AposMastery.dat";
    const string Version = "v2";
    const int EliteKills = 5;
    const float SaveDelay = 10f;

    // kills needed for levels 1..25
    public static readonly int[] KillsFor =
        { 4, 12, 24, 40, 60, 90, 120, 160, 210, 260, 320, 390, 470, 560, 660, 770, 890, 1020, 1160, 1310, 1470, 1640, 1820, 2010, 2200 };
    // points needed in the lower tiers of the same branch, by tier (1..5)
    public static readonly int[] TierNeed = { 0, 0, 3, 6, 9, 12 };

    // ===== classes =====
    public const int Pistols = 0, Shotguns = 1, Rifles = 2, MG = 3, Bows = 4, Crossbows = 5,
                     Blades = 6, Clubs = 7, Sledges = 8, Spears = 9, Knuckles = 10, Batons = 11;
    public static readonly string[] Classes =
        { "Pistols", "Shotguns", "Rifles", "MG", "Bows", "Crossbows", "Blades", "Clubs", "Sledges", "Spears", "Knuckles", "Batons" };
    static readonly string[] ClassTags =
        { "perkGunslinger", "perkBoomstick", "perkDeadEye", "perkMachineGunner", "perkArchery", "perkArchery",
          "perkDeepCuts", "perkPummelPete", "perkSkullCrusher", "perkJavelinMaster", "perkBrawler", "perkFlurryOfIntellect" };
    static FastTags<TagGroup.Global>[] classTags;

    // -1 = not a mastery weapon (tools, fists, turrets, explosives)
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
            if (i == Bows || i == Crossbows)
            {
                // bows are catapults, crossbows are launchers
                bool bow = ic.Actions != null && ic.Actions.Length > 0 && ic.Actions[0] is ItemActionCatapult;
                return bow ? Bows : Crossbows;
            }
            return i;
        }
        return -1;
    }

    // ===== trees =====
    // Skill: id, branch (A / B / H = hybrid), tier (1..5; for a hybrid the row it is drawn on), ranks, hybrid points needed
    public class Skill
    {
        public string Id;
        public char Branch;
        public int Tier;
        public int Max;
        public int Need;
        public string Icon; // a vanilla UI sprite: ui_game_symbol_<Icon>
    }

    // "id branch tier ranks need icon" separated by |; order = order in the window (A1..A8, B1..B8, H1..H3);
    // need = hybrid points in each branch (0 for A / B), icon = ui_game_symbol_<icon>
    static readonly string[] TreeText =
    {
        // Pistols / SMG: A Headhunter, B Lead Storm (AposPistol.cs)
        "A1 A 1 3 0 sight|A2 A 1 3 0 hand|A3 A 2 3 0 shape_ammo|A4 A 2 3 0 cold|A5 A 3 1 0 search|A6 A 3 2 0 critical|A7 A 4 2 0 skull|A8 A 5 1 0 gunslinger|" +
        "B1 B 1 3 0 speed|B2 B 1 3 0 add|B3 B 2 3 0 swap|B4 B 2 3 0 twitch_slow|B5 B 3 1 0 hot|B6 B 3 2 0 bundle|B7 B 4 2 0 twitch_strength_ranged|B8 B 5 1 0 twitch_infinite_ammo|" +
        "H1 H 3 1 5 run_and_gun|H2 H 4 1 5 pistol|H3 H 5 1 8 explosion",
        // Shotguns: A slugs, B buckshot (AposShotgun.cs)
        "A1 A 1 3 0 shape_ammo|A2 A 1 3 0 muscle|A3 A 2 3 0 destruction|A4 A 2 3 0 arrow_right|A5 A 3 1 0 brokenbone|A6 A 3 2 0 bundle|A7 A 4 2 0 stunned|A8 A 5 1 0 hard_target|" +
        "B1 B 1 3 0 shape_round|B2 B 1 3 0 wind|B3 B 2 3 0 laceration|B4 B 2 3 0 speed|B5 B 3 1 0 broken_arm|B6 B 3 2 0 drop|B7 B 4 2 0 defense|B8 B 5 1 0 fire|" +
        "H1 H 3 1 5 concussion|H2 H 4 1 5 twitch_hurt|H3 H 5 1 8 explosion",
        // Rifles: A the shot, B the ricochet (AposRifle.cs)
        "A1 A 1 3 0 head_shot|A2 A 1 3 0 swap|A3 A 2 3 0 star|A4 A 2 3 0 long_shot|A5 A 3 1 0 sneak_attack|A6 A 3 3 0 sight|A7 A 4 1 0 trophy|A8 A 5 1 0 marksmanship|" +
        "B1 B 1 3 0 arrow_compass|B2 B 1 3 0 critical|B3 B 2 3 0 players|B4 B 2 3 0 stunned|B5 B 3 1 0 hard_target|B6 B 3 2 0 fire|B7 B 4 2 0 add|B8 B 5 1 0 explosion|" +
        "H1 H 3 1 5 percent|H2 H 4 1 5 search|H3 H 5 1 8 arrow_right",
        // Machine guns: A suppression, B spin-up (AposMG.cs)
        "A1 A 1 3 0 twitch_slow|A2 A 1 3 0 bundle|A3 A 2 3 0 hard_target|A4 A 2 3 0 twitch_weaken_ranged|A5 A 3 1 0 explosion|A6 A 3 2 0 defense|A7 A 4 2 0 players|A8 A 5 1 0 twitch_stay_still|" +
        "B1 B 1 3 0 speed|B2 B 1 3 0 hand|B3 B 2 3 0 shape_ammo|B4 B 2 3 0 twitch_strength_ranged|B5 B 3 1 0 twitch_infinite_ammo|B6 B 3 2 0 laceration|B7 B 4 2 0 run|B8 B 5 1 0 hot|" +
        "H1 H 3 1 5 run_and_gun|H2 H 4 1 5 stress_zombie_horde|H3 H 5 1 8 electric_turret",
        null, null, null, null, null, null, null, null,
    };

    public static readonly Skill[][] Trees = ParseTrees();
    static readonly Dictionary<string, int>[] SkillIndex = IndexTrees();

    static Skill[][] ParseTrees()
    {
        Skill[][] trees = new Skill[Classes.Length][];
        for (int t = 0; t < Classes.Length; t++)
        {
            string text = t < TreeText.Length ? TreeText[t] : null;
            if (text == null) { trees[t] = new Skill[0]; continue; }
            string[] parts = text.Split('|');
            trees[t] = new Skill[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string[] f = parts[i].Split(' ');
                trees[t][i] = new Skill
                {
                    Id = f[0],
                    Branch = f[1][0],
                    Tier = int.Parse(f[2], CultureInfo.InvariantCulture),
                    Max = int.Parse(f[3], CultureInfo.InvariantCulture),
                    Need = f.Length > 4 ? int.Parse(f[4], CultureInfo.InvariantCulture) : 0,
                    Icon = f.Length > 5 ? "ui_game_symbol_" + f[5] : "ui_game_symbol_skills",
                };
            }
        }
        return trees;
    }

    static Dictionary<string, int>[] IndexTrees()
    {
        Dictionary<string, int>[] idx = new Dictionary<string, int>[Classes.Length];
        for (int t = 0; t < Classes.Length; t++)
        {
            idx[t] = new Dictionary<string, int>();
            for (int i = 0; i < Trees[t].Length; i++) idx[t][Trees[t][i].Id] = i;
        }
        return idx;
    }

    public static bool HasTree(int cls) { return cls >= 0 && Trees[cls].Length > 0; }

    // class trait icon in the window
    static readonly string[] ClassIcons =
        { "head_shot", "shotgun", "rifle", "run_and_gun", "archery", "archery", "knife", "club", "sledge", "spear", "boxer", "electric_power" };
    public static string ClassIcon(int cls) { return "ui_game_symbol_" + ClassIcons[cls]; }

    // ===== progress per class =====
    public class Progress
    {
        public int Kills;
        public int[] Ranks;
    }

    static readonly Progress[] progress = NewProgress();
    static string file;
    static bool dirty;
    static float saveAt;

    static Progress[] NewProgress()
    {
        Progress[] p = new Progress[Classes.Length];
        for (int i = 0; i < p.Length; i++) p[i] = new Progress { Ranks = new int[Trees[i].Length] };
        return p;
    }

    public static Progress Get(int cls) { return progress[cls]; }

    public static int Level(Progress p)
    {
        int level = 0;
        while (level < MaxLevel && p.Kills >= KillsFor[level]) level++;
        return level;
    }

    public static int Spent(Progress p)
    {
        int n = 0;
        for (int i = 0; i < p.Ranks.Length; i++) n += p.Ranks[i];
        return n;
    }

    // unspent points of a class (0 while its tree is not ported yet)
    public static int FreePoints(int cls)
    {
        if (!HasTree(cls)) return 0;
        Progress p = progress[cls];
        return Mathf.Max(0, Level(p) - Spent(p));
    }

    // rank of a skill of the class (0 if the class has no such skill)
    public static int Rank(int cls, string id)
    {
        int i;
        return cls >= 0 && SkillIndex[cls].TryGetValue(id, out i) ? progress[cls].Ranks[i] : 0;
    }

    // points in a branch, counting only tiers below maxTier
    public static int PointsIn(int cls, char branch, int belowTier)
    {
        Skill[] tree = Trees[cls];
        int[] ranks = progress[cls].Ranks;
        int n = 0;
        for (int i = 0; i < tree.Length; i++)
        {
            if (tree[i].Branch == branch && tree[i].Tier < belowTier) n += ranks[i];
        }
        return n;
    }

    // is the skill's requirement met (tier points or hybrid points)?
    public static bool Unlocked(int cls, int skill)
    {
        Skill s = Trees[cls][skill];
        if (s.Branch == 'H') return PointsIn(cls, 'A', 99) >= s.Need && PointsIn(cls, 'B', 99) >= s.Need;
        return PointsIn(cls, s.Branch, s.Tier) >= TierNeed[s.Tier];
    }

    public static bool CanLearn(int cls, int skill)
    {
        Progress p = progress[cls];
        return p.Ranks[skill] < Trees[cls][skill].Max && Spent(p) < Level(p) && Unlocked(cls, skill);
    }

    public static void Learn(int cls, int skill)
    {
        if (!CanLearn(cls, skill)) return;
        progress[cls].Ranks[skill]++;
        Changed();
        Save();
    }

    // a skill can be reset only if every other learned skill still meets its requirement without it
    public static bool CanResetSkill(int cls, int skill)
    {
        int[] ranks = progress[cls].Ranks;
        if (ranks[skill] == 0) return false;
        int keep = ranks[skill];
        ranks[skill] = 0;
        bool ok = true;
        for (int i = 0; i < ranks.Length && ok; i++)
        {
            if (ranks[i] > 0 && !Unlocked(cls, i)) ok = false;
        }
        ranks[skill] = keep;
        return ok;
    }

    public static void ResetSkill(int cls, int skill)
    {
        if (!CanResetSkill(cls, skill)) return;
        progress[cls].Ranks[skill] = 0;
        Changed();
        Save();
    }

    public static void ResetAll(int cls)
    {
        Array.Clear(progress[cls].Ranks, 0, progress[cls].Ranks.Length);
        Changed();
        Save();
    }

    // ===== the held weapon's class =====
    public static int HeldClass = -1;
    public static int RankVersion; // bumps on every change of ranks, so class files can refresh their cached values

    static void Changed() { RankVersion++; }

    // stat cvars read by buffAposMastery (buffs.xml); the held class's file sets them every frame, otherwise they are 0
    public const int StatDmg = 0, StatSpeed = 1, StatReload = 2, StatSpread = 3, StatMag = 4,
                     StatPierce = 5, StatRays = 6, StatBuckSpread = 7, StatRange = 8;
    static readonly string[] StatVars = { "aposMDmg", "aposMSpeed", "aposMReload", "aposMSpread", "aposMMagAdd",
                                          "aposMPierce", "aposMRays", "aposMBuckSpread", "aposMRange" };
    static readonly float[] stats = new float[StatVars.Length];

    public static void SetStat(int stat, float value) { stats[stat] = value; }

    static void UpdateHeld(EntityPlayerLocal player)
    {
        HeldClass = ClassOf(player.inventory.holdingItem);
        if (!player.Buffs.HasBuff("buffAposMastery")) player.Buffs.AddBuff("buffAposMastery");
        Array.Clear(stats, 0, stats.Length);
        AposHud.Begin();
        AposPistol.Tick(player, HeldClass == Pistols);
        AposShotgun.Tick(player, HeldClass == Shotguns);
        AposRifle.Tick(player, HeldClass == Rifles);
        AposMG.Tick(player, HeldClass == MG);
        for (int i = 0; i < StatVars.Length; i++)
        {
            if (player.Buffs.GetCustomVar(StatVars[i]) != stats[i]) player.Buffs.SetCustomVar(StatVars[i], stats[i]);
        }
    }

    // ===== test helpers ("aposmastery" console command) =====
    public static void SetLevel(int cls, int level)
    {
        level = Mathf.Clamp(level, 0, MaxLevel);
        progress[cls].Kills = level == 0 ? 0 : KillsFor[level - 1];
        // fewer points than spent: the tree is cleared
        if (Spent(progress[cls]) > level) Array.Clear(progress[cls].Ranks, 0, progress[cls].Ranks.Length);
        Changed();
        Save();
    }

    // learns the whole branch (A, B or H) from the bottom up while there are free points; returns ranks learned
    public static int FillBranch(int cls, char branch)
    {
        int learned = 0;
        bool any = true;
        while (any)
        {
            any = false;
            for (int i = 0; i < Trees[cls].Length; i++)
            {
                if (Trees[cls][i].Branch != branch || !CanLearn(cls, i)) continue;
                progress[cls].Ranks[i]++;
                learned++;
                any = true;
            }
        }
        Changed();
        Save();
        return learned;
    }

    // a class asks for the hit being processed to tear a limb off (AposCombat sets DamageResponse.Dismember)
    static bool dismemberNext;
    public static void RequestDismember() { dismemberNext = true; }
    public static bool TakeDismember()
    {
        bool d = dismemberNext;
        dismemberNext = false;
        return d;
    }

    // ===== hits by the local player (AposCombatDamagePatch) =====
    public static bool IsLocalWeaponHit(EntityPlayer attacker, EntityAlive target, DamageSource source)
    {
        GameManager gm = GameManager.Instance;
        return attacker != null && target != null && !(target is EntityPlayer) && gm.World != null
            && attacker == gm.World.GetPrimaryPlayer() && attacker.inventory.holdingItem != null
            && ClassOf(attacker.inventory.holdingItem) == HeldClass && HeldClass >= 0
            && AposShadow.IsWeaponHit(source);
    }

    // Prefix: damage before it is applied
    public static int ModifyHit(EntityPlayer attacker, EntityAlive target, DamageSource source, int strength)
    {
        if (!IsLocalWeaponHit(attacker, target, source)) return strength;
        if (HeldClass == Pistols) return AposPistol.ModifyHit(attacker as EntityPlayerLocal, target, source, strength);
        if (HeldClass == Shotguns) return AposShotgun.ModifyHit(attacker as EntityPlayerLocal, target, source, strength);
        if (HeldClass == Rifles) return AposRifle.ModifyHit(attacker as EntityPlayerLocal, target, source, strength);
        if (HeldClass == MG) return AposMG.ModifyHit(attacker as EntityPlayerLocal, target, source, strength);
        return strength;
    }

    // Postfix: after the damage (dealt = health actually lost); returns an extra leech share
    public static float AfterHit(EntityPlayer attacker, EntityAlive target, DamageSource source, DamageResponse response, int dealt)
    {
        if (!IsLocalWeaponHit(attacker, target, source)) return 0f;
        if (HeldClass == Pistols) AposPistol.AfterHit(attacker as EntityPlayerLocal, target, response, dealt);
        if (HeldClass == Shotguns) AposShotgun.AfterHit(attacker as EntityPlayerLocal, target, response, dealt);
        if (HeldClass == Rifles) AposRifle.AfterHit(attacker as EntityPlayerLocal, target, response, dealt);
        if (HeldClass == MG) AposMG.AfterHit(attacker as EntityPlayerLocal, target, response, dealt);
        return 0f;
    }

    // A kill with the held weapon (any damage type: burning from it counts too) gives mastery to its class
    public static void CountKill(EntityPlayer attacker, EntityAlive target)
    {
        GameManager gm = GameManager.Instance;
        if (attacker == null || target == null || target is EntityPlayer || gm.World == null || attacker != gm.World.GetPrimaryPlayer()) return;
        ItemClass ic = attacker.inventory.holdingItem;
        int cls = ClassOf(ic);
        if (cls < 0) return;
        Progress p = progress[cls];
        int before = Level(p);
        p.Kills += AposElites.IsElite(target) ? EliteKills : 1;
        int after = Level(p);
        if (after > before)
        {
            string className = Localization.Get("aposMasteryClass" + Classes[cls]);
            GameManager.ShowTooltip(attacker as EntityPlayerLocal, string.Format(Localization.Get("aposMasteryLevelUp"), className, after));
            Audio.Manager.PlayInsidePlayerHead("ui_challenge_complete", attacker.entityId);
            bannerTitle = string.Format(Localization.Get("aposMasteryBannerTitle"), after);
            bannerText = className + "  -  " + Localization.Get(HasTree(cls) ? "aposMasteryBannerHint" : "aposMasteryBannerSoon");
            bannerStart = Time.time;
            Save();
        }
        else MarkDirty();
    }

    // ===== level-up banner: big gold text in the upper middle of the screen for a few seconds =====
    const float BannerTime = 4f;
    static string bannerTitle, bannerText;
    static float bannerStart = -100f;
    GUIStyle bannerStyle, bannerSmall;

    void OnGUI()
    {
        if (Event.current.type == EventType.Repaint)
        {
            GameManager gm = GameManager.Instance;
            EntityPlayerLocal lp = gm != null && gm.World != null ? gm.World.GetPrimaryPlayer() : null;
            AposPistol.Draw(lp);
            AposShotgun.Draw(lp);
            AposRifle.Draw(lp);
        }
        float t = Time.time - bannerStart;
        if (t > BannerTime || bannerTitle == null || Event.current.type != EventType.Repaint) return;
        if (bannerStyle == null)
        {
            bannerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 44 };
            bannerSmall = new GUIStyle(bannerStyle) { fontSize = 24 };
        }
        // pops in (scale 1.3 -> 1 in 0.25 s), fades out over the last second
        float alpha = Mathf.Clamp01(BannerTime - t);
        float pop = 1f + 0.3f * Mathf.Clamp01(1f - t / 0.25f);
        bannerStyle.fontSize = Mathf.RoundToInt(44 * pop * Screen.height / 1080f);
        bannerSmall.fontSize = Mathf.RoundToInt(24 * Screen.height / 1080f);
        Rect r = new Rect(0f, Screen.height * 0.2f, Screen.width, Screen.height * 0.07f);
        Rect r2 = new Rect(0f, r.yMax, Screen.width, Screen.height * 0.04f);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f * alpha);
        GUI.Label(new Rect(r.x + 3f, r.y + 3f, r.width, r.height), bannerTitle, bannerStyle);
        GUI.Label(new Rect(r2.x + 2f, r2.y + 2f, r2.width, r2.height), bannerText, bannerSmall);
        GUI.color = new Color(1f, 0.82f, 0.3f, alpha);
        GUI.Label(r, bannerTitle, bannerStyle);
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(r2, bannerText, bannerSmall);
        GUI.color = old;
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

    // ===== MonoBehaviour: load/save, hotkey =====
    void Update()
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        if (player == null)
        {
            if (dirty) Save();
            file = null;
            HeldClass = -1;
            AposPistol.Reset();
            AposShotgun.Reset();
            AposRifle.Reset();
            AposMG.Reset();
            AposMasteryBadgePatch.Forget();
            hintRect = null;
            return;
        }
        string dir = AposSettings.PlayerDataDir();
        if (dir == null) return; // joining: the folder is not known yet
        string path = Path.Combine(dir, FileName);
        if (path != file) Load(path);
        if (dirty && Time.time >= saveAt) Save();
        UpdateHeld(player);
        UpdateHint(player);

        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(player);
        if (ui == null || ui.windowManager.IsInputActive()) return;
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (alt && Input.GetKeyDown(KeyCode.K)) XUiC_AposMastery.OpenFor(ui, null);
    }

    // "+N / Alt+K" right of the toolbelt while the held weapon's class has unspent points (windows.xml aposMasteryHint)
    static LocalPlayerUI hintUi;
    static XUiController hintRect;
    static XUiV_Label hintLabel;
    static string hintText;

    static void UpdateHint(EntityPlayerLocal p)
    {
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(p);
        if (ui == null || ui.xui == null) return;
        if (ui != hintUi || hintRect == null)
        {
            hintUi = ui;
            hintText = null;
            XUiV_Window w = ui.xui.GetWindow("windowToolbelt");
            hintRect = w != null && w.Controller != null ? w.Controller.GetChildById("aposMasteryHint") : null;
            XUiController c = hintRect != null ? hintRect.GetChildById("aposMasteryHintLabel") : null;
            hintLabel = c != null ? c.ViewComponent as XUiV_Label : null;
            if (hintRect == null || hintLabel == null) { hintRect = null; return; }
        }
        int free = HeldClass >= 0 ? FreePoints(HeldClass) : 0;
        string text = free > 0 ? "[FFD24D]+" + free + "[-] Alt+K" : "";
        if (text == hintText) return;
        hintText = text;
        hintLabel.Text = text;
        hintRect.ViewComponent.IsVisible = free > 0;
    }

    static void MarkDirty()
    {
        if (!dirty) saveAt = Time.time + SaveDelay;
        dirty = true;
    }

    static void Clear()
    {
        for (int i = 0; i < progress.Length; i++)
        {
            progress[i].Kills = 0;
            Array.Clear(progress[i].Ranks, 0, progress[i].Ranks.Length);
        }
        Changed();
    }

    static void Load(string path)
    {
        file = path;
        Clear();
        dirty = false;
        if (!File.Exists(path)) return;
        try
        {
            string[] lines = File.ReadAllLines(path);
            if (lines.Length > 0 && lines[0].Trim() == Version)
            {
                for (int l = 1; l < lines.Length; l++)
                {
                    string[] f = lines[l].Split('\t');
                    int cls = Array.IndexOf(Classes, f[0]);
                    if (f.Length < 2 || cls < 0) continue;
                    progress[cls].Kills = int.Parse(f[1], CultureInfo.InvariantCulture);
                    if (f.Length < 3 || f[2] == "") continue;
                    string[] r = f[2].Split(',');
                    for (int i = 0; i < r.Length && i < progress[cls].Ranks.Length; i++)
                    {
                        progress[cls].Ranks[i] = Mathf.Clamp(int.Parse(r[i], CultureInfo.InvariantCulture), 0, Trees[cls][i].Max);
                    }
                }
                return;
            }
            // old format (one line per weapon: item name, kills, bit mask of the old 20-node tree): kills go to the
            // weapon's class, the old points are given back
            foreach (string line in lines)
            {
                string[] f = line.Split('\t');
                if (f.Length != 3) continue;
                int cls = ClassOf(ItemClass.GetItemClass(f[0], true));
                if (cls >= 0) progress[cls].Kills += int.Parse(f[1], CultureInfo.InvariantCulture);
            }
            File.Copy(path, path + ".v1", true);
            Debug.Log("[AposMod] Mastery file converted to per-class progress; the old one is kept as " + path + ".v1");
            Save();
        }
        catch (Exception e)
        {
            Debug.LogError("[AposMod] Mastery file could not be read, it was kept as " + path + ".bad: " + e.Message);
            try { File.Copy(path, path + ".bad", true); } catch (Exception) { }
            Clear();
        }
        finally
        {
            Changed();
        }
    }

    static void Save()
    {
        dirty = false;
        if (file == null) return;
        try
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Version).Append('\n');
            for (int c = 0; c < progress.Length; c++)
            {
                Progress p = progress[c];
                if (p.Kills == 0 && Spent(p) == 0) continue;
                sb.Append(Classes[c]).Append('\t').Append(p.Kills.ToString(CultureInfo.InvariantCulture)).Append('\t');
                for (int i = 0; i < p.Ranks.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(p.Ranks[i].ToString(CultureInfo.InvariantCulture));
                }
                sb.Append('\n');
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

    // ===== texts: aposMS_<Class>_<Id> (name), ..._D (description), ..._R1.._R3 (ranks); ..._Base = class trait =====
    public static string SkillKey(int cls, string id) { return "aposMS_" + Classes[cls] + "_" + id; }
}

// Mastery window (Alt+K): the tree of the held weapon's class. Node 0 is the class trait, nodes 1..19 the skills in
// tree order (A1..A8, B1..B8, H1..H3). Info panel: level, points, the selected skill, Learn, Reset skill / Reset all.
public class XUiC_AposMastery : XUiController
{
    const int NodeCount = 20;
    readonly XUiC_SimpleButton[] nodes = new XUiC_SimpleButton[NodeCount];
    XUiV_Label weaponLabel, levelLabel, pointsLabel, nodeTitle, nodeSub, nodeDesc, resetHint, branchA, branchB, branchH;
    XUiV_Sprite barA, barB, barLevel, detailIcon;
    readonly XUiV_Label[] tierA = new XUiV_Label[6], tierB = new XUiV_Label[6];
    // per node: border, selection frame, icon, rank pips
    readonly XUiV_Sprite[] border = new XUiV_Sprite[NodeCount], sel = new XUiV_Sprite[NodeCount], icon = new XUiV_Sprite[NodeCount];
    readonly XUiV_Sprite[,] pips = new XUiV_Sprite[NodeCount, 3];

    static readonly Color Gold = new Color(1f, 0.82f, 0.3f), GoldDim = new Color(0.75f, 0.6f, 0.25f), Green = new Color(0.45f, 1f, 0.45f),
        Grey = new Color(0.55f, 0.55f, 0.55f), Dark = new Color(0.28f, 0.28f, 0.28f), Cap = new Color(1f, 0.55f, 0.15f),
        PipOff = new Color(0.24f, 0.24f, 0.24f);
    XUiC_SimpleButton learn, resetOne, resetAll;
    int selected = -1; // node index (0 = class trait)
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
        branchA = Label(this, "lblBranchA");
        branchB = Label(this, "lblBranchB");
        branchH = Label(this, "lblBranchH");
        nodeSub = Label(this, "lblNodeSub");
        barA = Sprite(this, "barBranchA");
        barB = Sprite(this, "barBranchB");
        barLevel = Sprite(this, "barLevel");
        detailIcon = Sprite(this, "detailIcon");
        for (int t = 2; t <= 5; t++)
        {
            tierA[t] = Label(this, "lblTierA" + t);
            tierB[t] = Label(this, "lblTierB" + t);
        }
        for (int i = 0; i < nodes.Length; i++)
        {
            int n = i;
            nodes[i] = Button("btnNode" + i, () => selected = n);
            if (nodes[i] == null) continue;
            border[i] = Sprite(nodes[i], "border");
            sel[i] = Sprite(nodes[i], "sel");
            icon[i] = Sprite(nodes[i], "icon");
            for (int k = 0; k < 3; k++) pips[i, k] = Sprite(nodes[i], "pip" + (k + 1));
        }
        learn = Button("btnLearn", () =>
        {
            int cls = Class();
            if (cls >= 0 && selected > 0) AposMastery.Learn(cls, selected - 1);
        });
        resetOne = Button("btnReset", () =>
        {
            int cls = Class();
            if (cls < 0 || selected <= 0 || !AposMastery.CanResetSkill(cls, selected - 1) || !Pay(AposMastery.ResetOneCost)) return;
            AposMastery.ResetSkill(cls, selected - 1);
        });
        resetAll = Button("btnResetAll", () =>
        {
            int cls = Class();
            if (cls < 0 || AposMastery.Spent(AposMastery.Get(cls)) == 0 || !Pay(AposMastery.ResetAllCost)) return;
            AposMastery.ResetAll(cls);
        });
        SetText(learn, Localization.Get("aposMasteryLearn"));
        SetText(resetOne, string.Format(Localization.Get("aposMasteryResetOne"), AposMastery.ResetOneCost));
        SetText(resetAll, string.Format(Localization.Get("aposMasteryResetAll"), AposMastery.ResetAllCost));
    }

    bool Pay(int cost)
    {
        EntityPlayerLocal p = xui.playerUI.entityPlayer;
        if (p == null || !nearForge || XUiC_AposForge.Count(p, AposElites.Eridium) < cost) return false;
        XUiC_AposForge.Take(p, AposElites.Eridium, cost);
        return true;
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

    static XUiV_Sprite Sprite(XUiController parent, string id)
    {
        XUiController c = parent.GetChildById(id);
        return c != null ? c.ViewComponent as XUiV_Sprite : null;
    }

    static void Paint(XUiV_Sprite s, Color c)
    {
        if (s != null && s.Color != c) s.Color = c;
    }

    static void ShowView(XUiView v, bool on)
    {
        if (v != null && v.IsVisible != on) v.IsVisible = on;
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

    static void SetLabel(XUiV_Label l, string text)
    {
        if (l != null) l.Text = text;
    }

    // set by the item info window's mastery button: show that weapon's class instead of the one in hand
    public static ItemClass ViewItem;

    public static void OpenFor(LocalPlayerUI ui, ItemClass ic)
    {
        ViewItem = ic;
        ui.windowManager.Open("aposMastery", true);
    }

    int Class()
    {
        if (ViewItem != null) return AposMastery.ClassOf(ViewItem);
        EntityPlayerLocal p = xui != null && xui.playerUI != null ? xui.playerUI.entityPlayer : null;
        return p != null ? AposMastery.ClassOf(p.inventory.holdingItem) : -1;
    }

    public override void OnClose()
    {
        base.OnClose();
        ViewItem = null;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        selected = 0;
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

    static string L(string key) { return Localization.Get(key); }

    void Refresh()
    {
        EntityPlayerLocal p = xui != null && xui.playerUI != null ? xui.playerUI.entityPlayer : null;
        if (p == null) return;
        if (Time.time >= nextForgeCheck)
        {
            nextForgeCheck = Time.time + 2f;
            nearForge = AposMastery.NearForge(p);
        }
        int cls = Class();
        if (cls < 0)
        {
            SetLabel(weaponLabel, L("aposMasteryNoWeapon"));
            foreach (XUiV_Label l in new[] { levelLabel, pointsLabel, nodeTitle, nodeSub, nodeDesc, resetHint, branchA, branchB, branchH }) SetLabel(l, "");
            for (int i = 0; i < nodes.Length; i++) Show(nodes[i], false);
            Enable(learn, false); Enable(resetOne, false); Enable(resetAll, false);
            return;
        }

        AposMastery.Skill[] tree = AposMastery.Trees[cls];
        AposMastery.Progress pr = AposMastery.Get(cls);
        int level = AposMastery.Level(pr), spent = AposMastery.Spent(pr), free = level - spent;
        bool hasTree = tree.Length > 0;
        string cName = AposMastery.Classes[cls];

        // info panel header: class, level, kills, bar to the next level, free points
        SetLabel(weaponLabel, L("aposMasteryClass" + cName));
        string next = level < AposMastery.MaxLevel
            ? string.Format(L("aposMasteryKills"), pr.Kills, AposMastery.KillsFor[level])
            : string.Format(L("aposMasteryKillsMax"), pr.Kills);
        SetLabel(levelLabel, string.Format(L("aposMasteryLevel"), level, AposMastery.MaxLevel) + "\n" + next);
        if (barLevel != null)
        {
            int from = level == 0 ? 0 : AposMastery.KillsFor[level - 1];
            barLevel.Fill = level >= AposMastery.MaxLevel ? 1f : Mathf.Clamp01((pr.Kills - from) / (float)(AposMastery.KillsFor[level] - from));
        }
        SetLabel(pointsLabel, hasTree ? string.Format(L("aposMasteryPoints"), free > 0 ? "[7CFF7C]" + free + "[-]" : "0", spent) : "");

        // branch headers, bars (18 ranks per branch) and the tier thresholds on the branch spines
        string bKey = "aposMB_" + cName + "_";
        int pa = hasTree ? AposMastery.PointsIn(cls, 'A', 99) : 0, pb = hasTree ? AposMastery.PointsIn(cls, 'B', 99) : 0;
        SetLabel(branchA, hasTree ? "[E0A040]A · " + L(bKey + "A") + "[-]  " + pa : "");
        SetLabel(branchB, hasTree ? "[50A8E0]B · " + L(bKey + "B") + "[-]  " + pb : "");
        SetLabel(branchH, hasTree ? "[C0C0C0]" + L("aposMasteryHybrids") + "[-]" : "");
        if (barA != null) barA.Fill = hasTree ? Mathf.Clamp01(pa / 18f) : 0f;
        if (barB != null) barB.Fill = hasTree ? Mathf.Clamp01(pb / 18f) : 0f;
        for (int t = 2; t <= 5; t++)
        {
            int need = AposMastery.TierNeed[t];
            SetLabel(tierA[t], hasTree ? (AposMastery.PointsIn(cls, 'A', t) >= need ? "[FFD24D]" : "[808080]") + need + "[-]" : "");
            SetLabel(tierB[t], hasTree ? (AposMastery.PointsIn(cls, 'B', t) >= need ? "[FFD24D]" : "[808080]") + need + "[-]" : "");
        }

        // node 0 = class trait; nodes 1.. = skills
        Show(nodes[0], true);
        SetText(nodes[0], "[A0A0A0]" + L("aposMasteryBaseShort") + "[-]\n[FFD24D]" + L(AposMastery.SkillKey(cls, "Base")) + "[-]");
        if (icon[0] != null) icon[0].SpriteName = AposMastery.ClassIcon(cls);
        ShowView(sel[0], selected == 0);
        for (int i = 1; i < nodes.Length; i++)
        {
            int s = i - 1;
            if (s >= tree.Length) { Show(nodes[i], false); continue; }
            Show(nodes[i], true);
            AposMastery.Skill sk = tree[s];
            int r = pr.Ranks[s];
            bool can = AposMastery.CanLearn(cls, s), open = AposMastery.Unlocked(cls, s), capstone = sk.Tier == 5 && sk.Branch != 'H';
            // border: gold = all ranks, dim gold = some ranks, green = can learn now, grey = open, dark = locked;
            // a capstone not learned yet is orange
            Color frame = r >= sk.Max ? Gold : r > 0 ? GoldDim : can ? Green : open ? Grey : Dark;
            if (capstone && r == 0 && !can) frame = open ? Cap : new Color(0.45f, 0.28f, 0.1f);
            Paint(border[i], frame);
            if (icon[i] != null && icon[i].SpriteName != sk.Icon) icon[i].SpriteName = sk.Icon;
            Paint(icon[i], r > 0 ? Gold : open ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.4f, 0.4f, 0.4f));
            for (int k = 0; k < 3; k++)
            {
                ShowView(pips[i, k], k < sk.Max);
                Paint(pips[i, k], k < r ? Gold : can && k == r ? new Color(0.3f, 0.6f, 0.3f) : PipOff);
            }
            string nameColor = r > 0 ? "[FFD24D]" : can ? "[A0FFA0]" : open ? "[E0E0E0]" : "[8A8A8A]";
            SetText(nodes[i], nameColor + L(AposMastery.SkillKey(cls, sk.Id)) + "[-]");
            ShowView(sel[i], i == selected);
        }

        if (!hasTree)
        {
            if (detailIcon != null) detailIcon.SpriteName = AposMastery.ClassIcon(cls);
            SetLabel(nodeTitle, "");
            SetLabel(nodeSub, "");
            SetLabel(nodeDesc, "[A0A0A0]" + L("aposMasterySoon") + "[-]");
            Enable(learn, false); Enable(resetOne, false); Enable(resetAll, false);
            SetLabel(resetHint, "");
            return;
        }

        string hint = nearForge ? "" : "[A0A0A0]" + L("aposMasteryResetHint") + "[-]";
        if (selected == 0)
        {
            if (detailIcon != null) detailIcon.SpriteName = AposMastery.ClassIcon(cls);
            SetLabel(nodeTitle, "[FFD24D]" + L(AposMastery.SkillKey(cls, "Base")) + "[-]");
            SetLabel(nodeSub, "[A0A0A0]" + L("aposMasteryBaseHint") + "[-]");
            SetLabel(nodeDesc, L(AposMastery.SkillKey(cls, "Base") + "_D"));
            Enable(learn, false);
            Enable(resetOne, false);
        }
        else if (selected - 1 < tree.Length)
        {
            int s = selected - 1;
            AposMastery.Skill sk = tree[s];
            string key = AposMastery.SkillKey(cls, sk.Id);
            int r = pr.Ranks[s];
            if (detailIcon != null) detailIcon.SpriteName = sk.Icon;
            SetLabel(nodeTitle, "[FFD24D]" + L(key) + "[-]  " + r + "/" + sk.Max);
            string where = sk.Branch == 'H'
                ? string.Format(L("aposMasterySubHybrid"), sk.Need)
                : string.Format(L("aposMasterySubTier"), sk.Branch, L(bKey + sk.Branch), sk.Tier);
            SetLabel(nodeSub, "[A0A0A0]" + where + "[-]");
            StringBuilder sb = new StringBuilder(L(key + "_D"));
            sb.Append("\n");
            for (int k = 1; k <= sk.Max; k++)
            {
                string line = L(key + "_R" + k);
                sb.Append('\n').Append(k <= r ? "[FFD24D]+ " + line + "[-]" : k == r + 1 ? "[FFFFFF]> " + line + "[-]" : "[808080]  " + line + "[-]");
            }
            string state;
            if (r >= sk.Max) state = L("aposMasteryMaxed");
            else if (!AposMastery.Unlocked(cls, s))
                state = sk.Branch == 'H'
                    ? string.Format(L("aposMasteryNeedHybrid"), sk.Need, AposMastery.PointsIn(cls, 'A', 99), AposMastery.PointsIn(cls, 'B', 99))
                    : string.Format(L("aposMasteryNeedTier"), AposMastery.TierNeed[sk.Tier], sk.Branch, AposMastery.PointsIn(cls, sk.Branch, sk.Tier));
            else if (free <= 0) state = L("aposMasteryNoPoints");
            else state = "";
            if (state != "") sb.Append("\n\n[FF9060]").Append(state).Append("[-]");
            SetLabel(nodeDesc, sb.ToString());
            Enable(learn, AposMastery.CanLearn(cls, s));
            Enable(resetOne, nearForge && AposMastery.CanResetSkill(cls, s) && XUiC_AposForge.Count(p, AposElites.Eridium) >= AposMastery.ResetOneCost);
            if (r > 0 && !AposMastery.CanResetSkill(cls, s)) hint = "[A0A0A0]" + L("aposMasteryResetBlocked") + "[-]";
        }
        Enable(resetAll, nearForge && spent > 0 && XUiC_AposForge.Count(p, AposElites.Eridium) >= AposMastery.ResetAllCost);
        SetLabel(resetHint, hint);
    }

    static void Enable(XUiC_SimpleButton b, bool on)
    {
        if (b != null) b.Enabled = on;
    }

    static void Show(XUiC_SimpleButton b, bool on)
    {
        if (b != null) b.ViewComponent.IsVisible = on;
    }
}

// "Mastery" button next to Stats / Description in the item info panel (Config/XUi_InGame/windows.xml):
// visible for mastery weapons, opens the tree of the weapon's class
[HarmonyPatch(typeof(XUiC_ItemInfoWindow), "Init")]
public static class AposMasteryInfoInitPatch
{
    static void Postfix(XUiC_ItemInfoWindow __instance)
    {
        XUiController button = __instance.GetChildById("aposMasteryButton");
        if (button == null) return;
        button.OnPress += (_sender, _mouseButton) =>
        {
            ItemClass ic = Traverse.Create(__instance).Field("itemClass").GetValue<ItemClass>();
            if (AposMastery.ClassOf(ic) >= 0) XUiC_AposMastery.OpenFor(__instance.xui.playerUI, ic);
        };
    }
}

[HarmonyPatch(typeof(XUiC_ItemInfoWindow), "SetInfo")]
public static class AposMasteryInfoShowPatch
{
    static void Postfix(XUiC_ItemInfoWindow __instance, ItemClass ___itemClass)
    {
        int cls = AposMastery.ClassOf(___itemClass);
        XUiController button = __instance.GetChildById("aposMasteryButton");
        if (button != null) button.ViewComponent.IsVisible = cls >= 0;
        XUiController badge = __instance.GetChildById("aposMasteryButtonBadge");
        XUiV_Label label = badge != null ? badge.ViewComponent as XUiV_Label : null;
        if (label != null)
        {
            int free = cls >= 0 ? AposMastery.FreePoints(cls) : 0;
            label.Text = free > 0 ? "+" + free : "";
        }
    }
}

// "+N" in the corner of every weapon slot whose class has unspent points (templates.xml aposMasteryBadge)
[HarmonyPatch(typeof(XUiC_ItemStack), "Update")]
public static class AposMasteryBadgePatch
{
    static readonly Dictionary<XUiC_ItemStack, XUiV_Label> labels = new Dictionary<XUiC_ItemStack, XUiV_Label>();

    public static void Forget() { labels.Clear(); } // leaving the game: the slot controllers are gone

    static void Postfix(XUiC_ItemStack __instance)
    {
        XUiV_Label label;
        if (!labels.TryGetValue(__instance, out label))
        {
            XUiController c = __instance.GetChildById("aposMasteryBadge");
            label = c != null ? c.ViewComponent as XUiV_Label : null;
            labels[__instance] = label;
        }
        if (label == null) return;
        ItemStack stack = __instance.ItemStack;
        int cls = stack != null && !stack.IsEmpty() ? AposMastery.ClassOf(stack.itemValue.ItemClass) : -1;
        int free = cls >= 0 ? AposMastery.FreePoints(cls) : 0;
        string text = free > 0 ? "+" + free : "";
        if (label.Text != text) label.Text = text;
    }
}

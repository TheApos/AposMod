// Elite zombies and Eridium (design: docs/Design-Elites.md).
// - Spawn (Harmony, EntityAlive.OnAddedToWorld): a zombie or animal becomes elite with 2% -> 5% chance by the player's
//   gamestage; 1 modifier, 2 after gamestage 100. State lives in cvars and buffs, so it survives a save:
//   aposElite = number of modifiers, aposEliteMods = bit mask, buffAposElite (+100% health) + buffAposElite<Modifier>.
// - Name tag above the head (OnGUI), regeneration, giant scale, summoner: this MonoBehaviour.
// - Death (Harmony, EntityAlive.OnEntityDeath): weapon with affixes + Eridium, explosive blast.
// - Armored / vampire damage changes: Source/AposCombat.cs. Other modifiers are plain buffs in buffs.xml.
// - Eridium modules (Harmony, ItemValue.SetModification): installed like a weapon mod, applied at once, removed.
// - Scrapping a weapon with affixes gives Eridium (Harmony, ItemActionEntryScrap.OnActivated).
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

public class AposElites : MonoBehaviour
{
    public static readonly string[] Modifiers =
        { "Armored", "Fast", "Regen", "Explosive", "Vampire", "Burning", "Electric", "Acidic", "Giant", "Summoner" };

    const float TagRange = 40f;
    const float RegenDelay = 3f;
    const float RegenShare = 0.03f;
    const float ExplosiveRadius = 3f;
    const int ExplosivePlayerDamage = 40;
    const int ExplosiveZombieDamage = 150;
    const float GiantScale = 1.3f;
    public const string Eridium = "resourceAposEridium";

    static readonly System.Random rand = new System.Random();
    static readonly FastTags<TagGroup.Global> SpawnTags = FastTags<TagGroup.Global>.Parse("zombie,animal");
    public static bool SuppressRoll; // set while a summoner spawns its helpers

    // last time each entity took damage (regeneration waits 3 s after it)
    static readonly Dictionary<int, float> lastDamaged = new Dictionary<int, float>();
    static List<string> weapons;

    readonly List<EntityAlive> elites = new List<EntityAlive>();
    readonly HashSet<int> scaled = new HashSet<int>();
    EntityPlayerLocal player;
    Camera cam;
    GUIStyle style;
    float nextTick;

    public static bool IsElite(EntityAlive e)
    {
        return e != null && e.Buffs != null && e.Buffs.GetCustomVar("aposElite") > 0f;
    }

    public static bool Has(EntityAlive e, string modifier)
    {
        return e.Buffs.HasBuff("buffAposElite" + modifier);
    }

    public static void NoteDamaged(EntityAlive e)
    {
        lastDamaged[e.entityId] = Time.time;
    }

    // ===== Spawn =====
    public static void OnSpawn(EntityAlive e)
    {
        if (e == null || e is EntityPlayer || e.Buffs == null || SuppressRoll) return;
        if (!e.HasAnyTags(SpawnTags) || e.Buffs.GetCustomVar("aposElite") != 0f) return; // already decided (or loaded)
        EntityPlayerLocal p = AposRewards.Player();
        if (p == null) return;

        int stage = p.gameStage;
        float chance = Mathf.Lerp(0.02f, 0.05f, Mathf.Clamp01(stage / 200f));
        if (rand.NextDouble() >= chance)
        {
            e.Buffs.SetCustomVar("aposElite", -1f); // rolled, not elite: do not roll again after a reload
            return;
        }

        Make(e, RandomMask(e, stage >= 100 ? 2 : 1));
    }

    public static int RandomMask(EntityAlive e, int count)
    {
        int mask = 0;
        while (Bits(mask) < count)
        {
            int m = rand.Next(Modifiers.Length);
            if (Modifiers[m] == "Summoner" && e.HasAnyTags(FastTags<TagGroup.Global>.Parse("animal"))) continue;
            mask |= 1 << m;
        }
        return mask;
    }

    // Turns a zombie into an elite with the given modifiers (spawn roll and the aposelite make command)
    public static void Make(EntityAlive e, int mask)
    {
        int count = Bits(mask);
        e.Buffs.SetCustomVar("aposElite", count);
        e.Buffs.SetCustomVar("aposEliteMods", mask);
        e.Buffs.SetCustomVar("aposEliteFill", 1f);
        e.Buffs.AddBuff("buffAposElite");
        for (int i = 0; i < Modifiers.Length; i++)
        {
            if ((mask & (1 << i)) != 0) e.Buffs.AddBuff("buffAposElite" + Modifiers[i]);
        }
    }

    static int Bits(int mask)
    {
        int n = 0;
        for (; mask != 0; mask &= mask - 1) n++;
        return n;
    }

    // ===== Death =====
    public static void OnDeath(EntityAlive e)
    {
        if (!IsElite(e) || e.Buffs.GetCustomVar("aposEliteDropped") > 0f) return;
        e.Buffs.SetCustomVar("aposEliteDropped", 1f);
        int count = (int)e.Buffs.GetCustomVar("aposElite");
        Vector3 pos = e.GetPosition() + Vector3.up * 0.8f;

        // weapon with affixes: quality 3-5 (1 modifier) or 5-6 (2 modifiers); 2% legendary from 2-modifier elites
        ItemStack loot = null;
        if (count >= 2 && rand.NextDouble() < 0.02) loot = AposRewards.RandomLegendary();
        if (loot == null) loot = RandomWeapon(count >= 2 ? rand.Next(5, 7) : rand.Next(3, 6));
        if (loot != null) Drop(loot, pos);
        int eridium = count >= 2 ? rand.Next(8, 16) : rand.Next(3, 7);
        Drop(new ItemStack(ItemClass.GetItem(Eridium), eridium), pos);

        if (Has(e, "Explosive")) Blast(e);
    }

    static void Drop(ItemStack stack, Vector3 pos)
    {
        if (stack == null || stack.IsEmpty()) return;
        GameManager.Instance.ItemDropServer(stack, pos, new Vector3(0.5f, 0f, 0.5f), -1, 600f, false);
    }

    static ItemStack RandomWeapon(int quality)
    {
        if (weapons == null)
        {
            weapons = new List<string>();
            foreach (ItemClass ic in ItemClass.list)
            {
                if (ic == null || AposAffixes.Kind(ic) == 0) continue;
                string n = ic.Name;
                if (n.Contains("Admin") || n.Contains("Test") || n.Contains("Debug") || n.Contains("Candy")) continue;
                weapons.Add(n);
            }
        }
        if (weapons.Count == 0) return null;
        ItemValue iv = ItemClass.GetItem(weapons[rand.Next(weapons.Count)]);
        if (iv == null || iv.type == 0) return null;
        iv = new ItemValue(iv.type, quality, quality, true, null, 1f);
        AposAffixes.Roll(iv, true);
        return new ItemStack(iv, 1);
    }

    // Explosive: 40 damage to players and 150 to zombies within 3 m, no block damage
    static void Blast(EntityAlive dead)
    {
        dead.Buffs.AddBuff("buffAposChainBlast"); // visual only
        List<Entity> list = GameManager.Instance.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e == dead || e.IsDead()) continue;
            if ((e.position - dead.position).sqrMagnitude > ExplosiveRadius * ExplosiveRadius) continue;
            if (e is EntityPlayer)
            {
                e.DamageEntity(new DamageSource(EnumDamageSource.External, EnumDamageTypes.Heat), ExplosivePlayerDamage, false, 1f);
            }
            else
            {
                e.Buffs.SetCustomVar("aposSplashDmg", -ExplosiveZombieDamage);
                e.Buffs.AddBuff("buffAposSplash");
            }
        }
    }

    // ===== Per frame: elite list, fill health, giant scale, regeneration, summoner =====
    void Update()
    {
        GameManager gm = GameManager.Instance;
        player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        elites.Clear();
        if (player == null)
        {
            scaled.Clear();
            lastDamaged.Clear();
            return;
        }
        cam = player.playerCamera;
        bool tick = Time.time >= nextTick;
        if (tick) nextTick = Time.time + 1f;

        List<Entity> list = gm.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e is EntityPlayer || e.IsDead() || !IsElite(e)) continue;
            elites.Add(e);

            // the +100% max health buff needs a moment to apply; then start at full health
            if (e.Buffs.GetCustomVar("aposEliteFill") > 0f && e.GetMaxHealth() > e.Health)
            {
                e.Health = e.GetMaxHealth();
                e.Buffs.SetCustomVar("aposEliteFill", 0f);
            }
            if (Has(e, "Giant") && scaled.Add(e.entityId)) e.transform.localScale *= GiantScale;

            if (!tick) continue;
            float last;
            if (Has(e, "Regen") && (!lastDamaged.TryGetValue(e.entityId, out last) || Time.time - last > RegenDelay) && e.Health < e.GetMaxHealth())
            {
                e.AddHealth(Mathf.Max(1, Mathf.RoundToInt(e.GetMaxHealth() * RegenShare)));
            }
            if (Has(e, "Summoner") && e.Buffs.GetCustomVar("aposSummoned") <= 0f && e.Health < e.GetMaxHealth() * 0.5f)
            {
                e.Buffs.SetCustomVar("aposSummoned", 1f);
                Summon(gm.World, e);
            }
        }
    }

    static void Summon(World world, EntityAlive caller)
    {
        SuppressRoll = true;
        try
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f;
                Vector3 pos = caller.position + new Vector3(Mathf.Cos(a) * 2f, 0.2f, Mathf.Sin(a) * 2f);
                Entity helper = EntityFactory.CreateEntity(caller.entityClass, pos);
                if (helper != null) world.SpawnEntityInWorld(helper);
            }
        }
        finally
        {
            SuppressRoll = false;
        }
    }

    // ===== Name tag: "Armored Explosive Moe", yellow (1 modifier) or orange (2), 40 m, only in line of sight =====
    void OnGUI()
    {
        if (elites.Count == 0 || cam == null || player == null || !AposSettings.EliteTags || Event.current.type != EventType.Repaint) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
        }
        style.fontSize = Mathf.RoundToInt(22 * AposSettings.Scale);
        Color oldColor = GUI.color;
        for (int i = 0; i < elites.Count; i++)
        {
            EntityAlive e = elites[i];
            if ((e.position - player.position).sqrMagnitude > TagRange * TagRange) continue;
            if (!player.CanSee(e.getHeadPosition())) continue;
            Vector3 vp = cam.WorldToViewportPoint(e.transform.position + Vector3.up * (e.height * e.transform.localScale.y + 0.6f));
            if (vp.z <= 0f) continue;
            float x = vp.x * Screen.width, y = (1f - vp.y) * Screen.height;
            string text = Name(e);
            Rect r = new Rect(x - 250f, y - 20f, 500f, 40f);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style);
            GUI.color = e.Buffs.GetCustomVar("aposElite") >= 2f ? new Color(1f, 0.55f, 0.1f) : new Color(1f, 0.85f, 0.2f);
            GUI.Label(r, text, style);
        }
        GUI.color = oldColor;
    }

    static string Name(EntityAlive e)
    {
        int mask = (int)e.Buffs.GetCustomVar("aposEliteMods");
        string text = "";
        for (int i = 0; i < Modifiers.Length; i++)
        {
            if ((mask & (1 << i)) != 0) text += Localization.Get("aposElite" + Modifiers[i]) + " ";
        }
        return text + Localization.Get(EntityClass.list[e.entityClass].entityClassName);
    }

    // ===== Eridium modules =====
    // Called when a mod lands in a weapon. Returns true if it was one of ours (then it is removed from the weapon).
    public static bool ApplyModule(ItemValue weapon, ItemValue mod)
    {
        if (weapon == null || mod == null || mod.IsEmpty() || mod.ItemClass == null) return false;
        string name = mod.ItemClass.Name;
        bool ok;
        if (name == "modWeaponAposReroll") ok = AposAffixes.RerollOne(weapon);
        else if (name == "modWeaponAposLock") ok = AposAffixes.LockBest(weapon);
        else if (name == "modWeaponAposTemper") ok = AposAffixes.Temper(weapon);
        else return false;

        EntityPlayerLocal p = AposRewards.Player();
        if (!ok && p != null)
        {
            // nothing to change on this weapon: give the module back
            ItemStack back = new ItemStack(mod.Clone(), 1);
            if (!p.bag.AddItem(back)) p.inventory.AddItem(back);
        }
        if (p != null) GameManager.ShowTooltip(p, Localization.Get(ok ? name + "Done" : "aposModuleNothing"));
        return true;
    }

    // Eridium for scrapping a weapon: 2 per affix, x2 at quality 6; a legendary gives 50
    public static int ScrapEridium(ItemValue iv)
    {
        if (iv == null || iv.ItemClass == null) return 0;
        if (iv.ItemClass.Name.StartsWith("aposLegend")) return 50;
        int affixes = AposAffixes.Decompose(iv).Count;
        return affixes * 2 * (iv.Quality >= 6 ? 2 : 1);
    }

    public static void GiveEridium(int count)
    {
        EntityPlayerLocal p = AposRewards.Player();
        if (p == null || count <= 0) return;
        ItemStack stack = new ItemStack(ItemClass.GetItem(Eridium), count);
        if (!p.bag.AddItem(stack) && !p.inventory.AddItem(stack)) Drop(stack, p.GetPosition() + Vector3.up);
    }
}

[HarmonyPatch(typeof(EntityAlive), "OnAddedToWorld")]
public static class AposEliteSpawnPatch
{
    static void Postfix(EntityAlive __instance)
    {
        try { AposElites.OnSpawn(__instance); }
        catch (Exception e) { Debug.LogError("[AposMod] Elite spawn failed: " + e); }
    }
}

[HarmonyPatch(typeof(EntityAlive), "OnEntityDeath")]
public static class AposEliteDeathPatch
{
    static void Postfix(EntityAlive __instance)
    {
        try { AposElites.OnDeath(__instance); }
        catch (Exception e) { Debug.LogError("[AposMod] Elite death failed: " + e); }
    }
}

// A module put into a weapon (assemble window or dropped onto the weapon) is used up at once
[HarmonyPatch(typeof(ItemValue), "SetModification")]
public static class AposModulePatch
{
    static bool busy;

    static void Postfix(ItemValue __instance, int _index, ItemValue _mod)
    {
        if (busy) return;
        busy = true;
        try
        {
            if (AposElites.ApplyModule(__instance, _mod)) __instance.SetModification(_index, ItemValue.None.Clone());
        }
        catch (Exception e) { Debug.LogError("[AposMod] Module failed: " + e); }
        finally { busy = false; }
    }
}

// The assemble window keeps showing the module in its slot: empty that slot too
[HarmonyPatch(typeof(XUiC_ItemPartStackGrid), "HandleSlotChangedEvent")]
public static class AposModuleSlotPatch
{
    static void Postfix(int slotNumber, ItemStack stack, XUiController[] ___itemControllers)
    {
        if (stack == null || stack.IsEmpty() || stack.itemValue.ItemClass == null) return;
        string n = stack.itemValue.ItemClass.Name;
        if (n != "modWeaponAposReroll" && n != "modWeaponAposLock" && n != "modWeaponAposTemper") return;
        XUiC_BasePartStack slot = ___itemControllers[slotNumber] as XUiC_BasePartStack;
        if (slot != null) slot.ItemStack = ItemStack.Empty.Clone();
    }
}

// Scrapping a weapon with affixes (or a legendary) also gives Eridium
[HarmonyPatch(typeof(ItemActionEntryScrap), "OnActivated")]
public static class AposScrapPatch
{
    static void Prefix(ItemActionEntryScrap __instance, out int __state)
    {
        __state = 0;
        XUiC_ItemStack c = __instance.ItemController as XUiC_ItemStack;
        if (c != null && !c.ItemStack.IsEmpty()) __state = AposElites.ScrapEridium(c.ItemStack.itemValue);
    }

    static void Postfix(ItemActionEntryScrap __instance, int __state)
    {
        if (__state <= 0) return;
        XUiC_ItemStack c = __instance.ItemController as XUiC_ItemStack;
        // the scrap really started when the weapon left its slot
        if (c == null || c.ItemStack.IsEmpty() || AposElites.ScrapEridium(c.ItemStack.itemValue) == 0) AposElites.GiveEridium(__state);
    }
}

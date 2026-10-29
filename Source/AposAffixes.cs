// Affixes on looted / traded weapons (Harmony). The game already rolls random "boosted" stats on items from loot
// containers and traders (ItemClass.AddGSStats, items.xml <stats>); crafted items never get them. We keep the
// small unboosted base rolls, drop the vanilla boosted ones and roll our own affix list instead:
// quality 1: none, 2-3: up to 1, 4-5: up to 2, 6: up to 3, values grow with quality. An affix is stored as a boosted
// ItemValue.Stat, so the engine applies it (value = 1 + stat * 0.005 multiplier) and the UI marks the item.
// Leech (HealthSteal) is applied by AposCombat.cs because a multiplier of a zero base does nothing.
using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;

public class AposAffix
{
    public string Key;              // localization key suffix: aposAffix<Key>
    public PassiveEffects[] Types;  // all stats this affix changes (first one identifies it)
    public float[] Mid;             // typical value at quality 2,3,4,5 (+-15% spread)
    public float Min6, Max6;        // range at quality 6
    public bool Melee;              // melee list, else ranged list
    public int Only;                // 0 = any weapon of the list, 1 = shotguns only, 2 = single-projectile guns (quality 6)

    public AposAffix(string key, bool melee, float[] mid, float min6, float max6, params PassiveEffects[] types)
    {
        Key = key; Melee = melee; Mid = mid; Min6 = min6; Max6 = max6; Types = types;
    }

    public float Roll(int quality, Random rand)
    {
        if (quality >= 6) return Min6 + (float)rand.NextDouble() * (Max6 - Min6);
        float m = Mid[Math.Max(0, quality - 2)];
        return m * (0.85f + 0.3f * (float)rand.NextDouble());
    }
}

public static class AposAffixes
{
    public const float StatStep = 0.005f;      // ItemValue.Stat.value unit
    public const float ScatterDamage = -0.4f;  // single-projectile guns: each of the 2 bullets deals 40% less

    public static readonly List<AposAffix> All = new List<AposAffix>
    {
        // ranged
        new AposAffix("Rapid", false, new[] { .05f, .08f, .12f, .16f }, .20f, .25f, PassiveEffects.RoundsPerMinute),
        new AposAffix("Precise", false, new[] { -.10f, -.15f, -.20f, -.30f }, -.35f, -.40f, PassiveEffects.SpreadDegreesHorizontal, PassiveEffects.SpreadDegreesVertical),
        new AposAffix("LongShot", false, new[] { .10f, .20f, .30f, .40f }, .50f, .60f, PassiveEffects.DamageFalloffRange, PassiveEffects.MaxRange),
        new AposAffix("Brutal", false, new[] { .05f, .10f, .15f, .20f }, .25f, .35f, PassiveEffects.EntityDamage),
        new AposAffix("Quickload", false, new[] { .10f, .15f, .20f, .25f }, .30f, .40f, PassiveEffects.ReloadSpeedMultiplier),
        new AposAffix("Scatter", false, new[] { .10f, .15f, .20f, .25f }, .30f, .35f, PassiveEffects.RoundRayCount) { Only = 1 },
        new AposAffix("Twin", false, new[] { 1f, 1f, 1f, 1f }, 1f, 1f, PassiveEffects.RoundRayCount) { Only = 2 },
        new AposAffix("Deadly", false, new[] { .10f, .20f, .30f, .40f }, .50f, .60f, PassiveEffects.HeadshotDamageModifier),
        new AposAffix("Extended", false, new[] { .10f, .20f, .30f, .40f }, .50f, .60f, PassiveEffects.MagazineSize),
        new AposAffix("Swift", false, new[] { .02f, .03f, .04f, .06f }, .08f, .10f, PassiveEffects.WalkSpeed, PassiveEffects.RunSpeed),
        new AposAffix("Steady", false, new[] { -.10f, -.15f, -.25f, -.35f }, -.40f, -.50f, PassiveEffects.KickDegreesVerticalMin, PassiveEffects.KickDegreesVerticalMax, PassiveEffects.KickDegreesHorizontalMin, PassiveEffects.KickDegreesHorizontalMax),
        // melee
        new AposAffix("Brutal", true, new[] { .05f, .10f, .15f, .20f }, .25f, .35f, PassiveEffects.EntityDamage),
        new AposAffix("Fury", true, new[] { .04f, .06f, .09f, .12f }, .15f, .20f, PassiveEffects.AttacksPerMinute),
        new AposAffix("Tireless", true, new[] { -.10f, -.15f, -.20f, -.25f }, -.30f, -.40f, PassiveEffects.StaminaLoss),
        new AposAffix("Deadly", true, new[] { .10f, .20f, .30f, .40f }, .50f, .60f, PassiveEffects.HeadshotDamageModifier),
        new AposAffix("Reach", true, new[] { .05f, .08f, .10f, .15f }, .20f, .20f, PassiveEffects.MaxRange),
        new AposAffix("Swift", true, new[] { .02f, .03f, .04f, .06f }, .08f, .10f, PassiveEffects.WalkSpeed, PassiveEffects.RunSpeed),
        new AposAffix("Leech", true, new[] { .01f, .02f, .03f, .04f }, .05f, .06f, PassiveEffects.HealthSteal),
        new AposAffix("Butcher", true, new[] { .05f, .10f, .15f, .20f }, .25f, .30f, PassiveEffects.DismemberChance),
        new AposAffix("Durable", true, new[] { .20f, .30f, .40f, .50f }, .60f, .80f, PassiveEffects.DegradationMax),
        new AposAffix("Wrecker", true, new[] { .10f, .20f, .30f, .40f }, .50f, .60f, PassiveEffects.BlockDamage),
    };

    static readonly FastTags<TagGroup.Global> WeaponTag = FastTags<TagGroup.Global>.Parse("weapon");
    static readonly FastTags<TagGroup.Global> MeleeTag = FastTags<TagGroup.Global>.Parse("melee");
    static readonly FastTags<TagGroup.Global> RangedTag = FastTags<TagGroup.Global>.Parse("ranged");
    static readonly FastTags<TagGroup.Global> NoAffixTag = FastTags<TagGroup.Global>.Parse("tool,turretRanged");
    static readonly FastTags<TagGroup.Global> ShotgunTag = FastTags<TagGroup.Global>.Parse("shotgun");
    static readonly FastTags<TagGroup.Global> GunTag = FastTags<TagGroup.Global>.Parse("gun");

    static readonly Random rand = new Random();

    // set while a loot container spawns items, so Lucky Looter 5 can add an affix
    [ThreadStatic] public static EntityPlayer LootingPlayer;

    public static int Cap(int quality)
    {
        return quality >= 6 ? 3 : quality >= 4 ? 2 : quality >= 2 ? 1 : 0;
    }

    // chance of the 1st, 2nd and 3rd affix by quality
    static float SlotChance(int quality, int slot)
    {
        if (quality >= 6) return slot == 0 ? 1f : slot == 1 ? .6f : .3f;
        if (quality >= 4) return slot == 0 ? .75f : .35f;
        return .5f;
    }

    // 0 = no affixes, 1 = melee, 2 = ranged
    public static int Kind(ItemClass ic)
    {
        if (ic == null || ic.Name.StartsWith("aposLegend")) return 0;
        FastTags<TagGroup.Global> tags = ic.ItemTags;
        if (!tags.Test_AnySet(WeaponTag) || tags.Test_AnySet(NoAffixTag)) return 0;
        if (tags.Test_AnySet(MeleeTag)) return 1;
        if (tags.Test_AnySet(RangedTag)) return 2;
        return 0;
    }

    static bool Fits(AposAffix a, ItemClass ic, int kind, int quality)
    {
        if (a.Melee != (kind == 1)) return false;
        bool shotgun = ic.ItemTags.Test_AnySet(ShotgunTag);
        if (a.Only == 1) return shotgun;
        if (a.Only == 2) return quality >= 6 && !shotgun && ic.ItemTags.Test_AnySet(GunTag);
        return true;
    }

    public static AposAffix Find(PassiveEffects type, bool melee)
    {
        foreach (AposAffix a in All) if (a.Melee == melee && a.Types[0] == type) return a;
        return null;
    }

    // One affix on a weapon: which affix and its value (fraction, 0.25 = +25%)
    public class Entry
    {
        public AposAffix Affix;
        public float Value;
        public Entry(AposAffix a, float v) { Affix = a; Value = v; }
    }

    // Replaces vanilla boosted rolls with a fresh affix set. forceMax = roll the full cap (console command, elites).
    public static void Roll(ItemValue iv, bool forceMax)
    {
        ItemClass ic = iv.ItemClass;
        int kind = Kind(ic);
        if (kind == 0) return;
        int quality = iv.Quality;
        int cap = Cap(quality);

        int count = 0;
        for (int slot = 0; slot < cap; slot++)
        {
            if (!forceMax && rand.NextDouble() >= SlotChance(quality, slot)) break;
            count++;
        }
        if (!forceMax && LootingPlayer != null && Level(LootingPlayer, "perkLuckyLooter") >= 5) count = Math.Min(cap, count + 1);

        List<Entry> list = new List<Entry>();
        for (int i = 0; i < count; i++)
        {
            AposAffix a = Pick(ic, kind, quality, list);
            if (a == null) break;
            list.Add(new Entry(a, a.Roll(quality, rand)));
        }
        iv.RemoveMetaData(LockKey);
        Compose(iv, list, true);
    }

    // A random affix that fits the weapon and is not on it yet (Scatter and Twin share a stat, so only one of them)
    static AposAffix Pick(ItemClass ic, int kind, int quality, List<Entry> have)
    {
        List<AposAffix> pool = new List<AposAffix>();
        foreach (AposAffix a in All)
        {
            if (!Fits(a, ic, kind, quality)) continue;
            bool taken = false;
            foreach (Entry e in have) if (e.Affix.Types[0] == a.Types[0]) taken = true;
            if (!taken) pool.Add(a);
        }
        return pool.Count > 0 ? pool[rand.Next(pool.Count)] : null;
    }

    // Reads the affixes back from the stats (Twin's -40% damage is separated from Brutal)
    public static List<Entry> Decompose(ItemValue iv)
    {
        List<Entry> list = new List<Entry>();
        int kind = Kind(iv.ItemClass);
        if (kind == 0 || iv.Stats == null) return list;
        bool twin = IsTwin(iv);
        foreach (ItemValue.Stat s in iv.Stats)
        {
            if (!s.isBoosted) continue;
            AposAffix a = Find(s.type, kind == 1);
            if (a == null) continue;
            float v = s.value * StatStep;
            if (s.type == PassiveEffects.RoundRayCount && twin) a = FindKey("Twin");
            if (s.type == PassiveEffects.EntityDamage && twin)
            {
                v = (1f + v) / (1f + ScatterDamage) - 1f;
                if (Math.Abs(v) < 0.005f) continue;
            }
            list.Add(new Entry(a, v));
        }
        return list;
    }

    // Writes the affixes as stats; keepBase keeps the small unboosted vanilla base rolls
    public static void Compose(ItemValue iv, List<Entry> list, bool keepBase)
    {
        List<ItemValue.Stat> stats = new List<ItemValue.Stat>();
        if (keepBase && iv.Stats != null) foreach (ItemValue.Stat s in iv.Stats) if (!s.isBoosted && s.value != 0) stats.Add(s);
        foreach (Entry e in list)
        {
            foreach (PassiveEffects t in e.Affix.Types) Add(stats, t, e.Value);
            if (e.Affix.Only == 2) Add(stats, PassiveEffects.EntityDamage, ScatterDamage);
        }
        iv.Stats = stats.Count > 0 ? stats.ToArray() : null;
    }

    static AposAffix FindKey(string key)
    {
        foreach (AposAffix a in All) if (a.Key == key) return a;
        return null;
    }

    // Combines an added value with an existing stat of the same type (only the first stat of a type counts)
    static void Add(List<ItemValue.Stat> stats, PassiveEffects type, float value)
    {
        int added = (int)Math.Round(value / StatStep);
        for (int i = 0; i < stats.Count; i++)
        {
            if (stats[i].type != type) continue;
            // multiplicative: (1 + a)(1 + b) - 1
            float old = stats[i].value * StatStep;
            float merged = (1f + old) * (1f + value) - 1f;
            int baseValue = stats[i].isBoosted ? 0 : stats[i].value;
            stats[i] = new ItemValue.Stat(type, baseValue, (int)Math.Round(merged / StatStep) - baseValue);
            return;
        }
        stats.Add(new ItemValue.Stat(type, 0, added));
    }

    // After combining two weapons the game keeps the best stats of both; trim back to the quality cap
    public static void Trim(ItemValue iv)
    {
        List<Entry> list = Decompose(iv);
        int extra = list.Count - Cap(iv.Quality);
        if (extra <= 0) return;
        for (int i = 0; i < extra; i++) list.RemoveAt(rand.Next(list.Count));
        Compose(iv, list, false);
    }

    // ===== Eridium modules (Source/AposElites.cs installs them). Each returns false when it cannot do anything,
    // so the module is given back. =====
    public const string LockKey = "aposLock";

    // Reroll: one random affix that is not locked gets a new type and value (an empty weapon gets its first affix)
    public static bool RerollOne(ItemValue iv)
    {
        int kind = Kind(iv.ItemClass);
        if (kind == 0 || Cap(iv.Quality) == 0) return false;
        List<Entry> list = Decompose(iv);
        int locked = LockedType(iv);
        List<int> free = new List<int>();
        for (int i = 0; i < list.Count; i++) if ((int)list[i].Affix.Types[0] != locked) free.Add(i);
        if (list.Count > 0 && free.Count == 0) return false;

        if (list.Count > 0) list.RemoveAt(free[rand.Next(free.Count)]);
        AposAffix a = Pick(iv.ItemClass, kind, iv.Quality, list);
        if (a == null) return false;
        list.Add(new Entry(a, a.Roll(iv.Quality, rand)));
        Compose(iv, list, true);
        return true;
    }

    // Lock: protects the best affix (closest to its quality 6 maximum) from Reroll
    public static bool LockBest(ItemValue iv)
    {
        List<Entry> list = Decompose(iv);
        if (list.Count == 0) return false;
        Entry best = null;
        float bestScore = -1f;
        foreach (Entry e in list)
        {
            float score = Math.Abs(e.Value) / Math.Max(0.01f, Math.Abs(e.Affix.Max6));
            if (score > bestScore) { bestScore = score; best = e; }
        }
        iv.SetMetadata(LockKey, (int)best.Affix.Types[0]);
        return true;
    }

    // Temper: +1 quality (max 6); every affix is rolled again at the new quality, the locked one keeps its type
    public static bool Temper(ItemValue iv)
    {
        if (Kind(iv.ItemClass) == 0 || iv.Quality >= 6) return false;
        List<Entry> list = Decompose(iv);
        iv.Quality = (ushort)(iv.Quality + 1);
        foreach (Entry e in list) e.Value = Better(e.Value, e.Affix.Roll(iv.Quality, rand), Math.Sign(e.Affix.Max6));
        Compose(iv, list, true);
        return true;
    }

    // For negative affixes (spread, recoil, stamina) "bigger" means more negative
    static float Better(float a, float b, int sign) { return sign < 0 ? Math.Min(a, b) : Math.Max(a, b); }

    public static int LockedType(ItemValue iv)
    {
        int t;
        return iv.TryGetMetadata(LockKey, out t) ? t : -1;
    }

    // Affix lines for the item tooltip, e.g. "Damage +27%"; the locked affix is marked
    public static string Describe(ItemValue iv)
    {
        List<Entry> list = Decompose(iv);
        if (list.Count == 0) return null;
        int locked = LockedType(iv);
        List<string> lines = new List<string>();
        foreach (Entry e in list)
        {
            string name = Localization.Get("aposAffix" + e.Affix.Key);
            string lockMark = (int)e.Affix.Types[0] == locked ? " [BE5AFF](" + Localization.Get("aposAffixLocked") + ")[-]" : "";
            if (e.Affix.Key == "Twin")
            {
                lines.Add("[FFD24D]" + name + "[-] " + Localization.Get("aposAffixTwinDesc") + lockMark);
                continue;
            }
            float pct = e.Value * 100f;
            lines.Add("[FFD24D]" + name + "[-] " + (pct > 0 ? "+" : "") + pct.ToString("0", CultureInfo.InvariantCulture) + "%" + lockMark);
        }
        return string.Join("\n", lines.ToArray());
    }


    // single-projectile gun with the extra bullet
    static bool IsTwin(ItemValue iv)
    {
        if (iv.ItemClass.ItemTags.Test_AnySet(ShotgunTag)) return false;
        return StatFraction(iv, PassiveEffects.RoundRayCount) > 0f;
    }

    public static int Level(EntityAlive e, string progression)
    {
        if (e == null || e.Progression == null) return 0;
        ProgressionValue pv = e.Progression.GetProgressionValue(progression);
        return pv != null ? pv.Level : 0;
    }

    // Stat value of the first stat of a type, as a fraction (0.05 = 5%)
    public static float StatFraction(ItemValue iv, PassiveEffects type)
    {
        if (iv == null || iv.Stats == null) return 0f;
        foreach (ItemValue.Stat s in iv.Stats) if (s.type == type && s.isBoosted) return s.value * StatStep;
        return 0f;
    }
}

// Every vanilla stat roll (loot, traders) -> our affixes
[HarmonyPatch(typeof(ItemClass), "AddGSStats")]
public static class AposAffixRollPatch
{
    static void Postfix(ItemValue _iv)
    {
        if (_iv != null) AposAffixes.Roll(_iv, false);
    }
}

// Remember who opens a loot container (Lucky Looter 5)
[HarmonyPatch(typeof(LootContainer), "SpawnItem")]
public static class AposAffixLootPatch
{
    static void Prefix(EntityPlayer player)
    {
        AposAffixes.LootingPlayer = player;
    }

    static void Postfix()
    {
        AposAffixes.LootingPlayer = null;
    }
}

[HarmonyPatch(typeof(ItemValue), "MergeBestStats")]
public static class AposAffixMergePatch
{
    static void Postfix(ItemValue __instance)
    {
        AposAffixes.Trim(__instance);
    }
}

// Affix lines on top of the item description
[HarmonyPatch(typeof(XUiC_ItemInfoWindow), "GetBindingValueInternal")]
public static class AposAffixTooltipPatch
{
    // ___itemStack: Harmony passes the private field XUiC_ItemInfoWindow.itemStack
    static void Postfix(ref string value, string bindingName, bool __result, ItemStack ___itemStack)
    {
        if (!__result || bindingName != "itemdescription") return;
        ItemStack stack = ___itemStack;
        if (stack == null || stack.IsEmpty()) return;
        string affixes = AposAffixes.Describe(stack.itemValue);
        if (affixes != null) value = affixes + "\n\n" + value;
    }
}

// aposaffix        -> re-roll the held weapon with the full number of affixes for its quality
// aposaffix 6      -> set the held weapon to quality 6 first
public class AposAffixCommand : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "aposaffix" };
    }

    public override string getDescription()
    {
        return "AposMod: re-roll affixes on the held weapon (max count). aposaffix [quality 1-6]";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        SdtdConsole console = SingletonMonoBehaviour<SdtdConsole>.Instance;
        EntityPlayerLocal player = GameManager.Instance.World != null ? GameManager.Instance.World.GetPrimaryPlayer() : null;
        if (player == null) return;
        Inventory inv = player.inventory;
        ItemValue iv = inv.holdingItemItemValue.Clone();
        if (AposAffixes.Kind(iv.ItemClass) == 0)
        {
            console.Output("Hold a weapon (not a tool or a legendary).");
            return;
        }
        int q;
        if (_params.Count > 0 && int.TryParse(_params[0], out q)) iv.Quality = (ushort)Math.Max(1, Math.Min(6, q));
        AposAffixes.Roll(iv, true);
        inv.SetItem(inv.holdingItemIdx, new ItemStack(iv, inv.holdingCount));
        string d = AposAffixes.Describe(iv);
        console.Output("Quality " + iv.Quality + ": " + (d == null ? "no affixes" : d.Replace("\n", ", ")));
    }
}

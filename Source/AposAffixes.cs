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

    // Replaces vanilla boosted rolls with a fresh affix set. forceMax = roll the full cap (console command).
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

        List<AposAffix> pool = new List<AposAffix>();
        foreach (AposAffix a in All) if (Fits(a, ic, kind, quality)) pool.Add(a);

        List<ItemValue.Stat> stats = new List<ItemValue.Stat>();
        if (iv.Stats != null) foreach (ItemValue.Stat s in iv.Stats) if (!s.isBoosted && s.value != 0) stats.Add(s);

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            AposAffix a = pool[rand.Next(pool.Count)];
            pool.RemoveAll(p => p.Types[0] == a.Types[0]); // no duplicates (Scatter / Twin share a stat)
            float v = a.Roll(quality, rand);
            foreach (PassiveEffects t in a.Types) Add(stats, t, v);
            if (a.Only == 2) Add(stats, PassiveEffects.EntityDamage, ScatterDamage);
        }
        iv.Stats = stats.Count > 0 ? stats.ToArray() : null;
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
        int kind = Kind(iv.ItemClass);
        if (kind == 0 || iv.Stats == null) return;
        List<PassiveEffects> affixes = new List<PassiveEffects>();
        foreach (ItemValue.Stat s in iv.Stats)
        {
            if (s.isBoosted && Find(s.type, kind == 1) != null && !affixes.Contains(s.type)) affixes.Add(s.type);
        }
        int extra = affixes.Count - Cap(iv.Quality);
        if (extra <= 0) return;
        // Twin's damage penalty is mixed into the damage stat, so Twin itself is never the one dropped
        if (IsTwin(iv)) affixes.Remove(PassiveEffects.RoundRayCount);

        List<PassiveEffects> drop = new List<PassiveEffects>();
        for (int i = 0; i < extra && affixes.Count > 0; i++)
        {
            PassiveEffects t = affixes[rand.Next(affixes.Count)];
            affixes.Remove(t);
            drop.AddRange(Find(t, kind == 1).Types);
        }
        List<ItemValue.Stat> keep = new List<ItemValue.Stat>();
        foreach (ItemValue.Stat s in iv.Stats) if (!drop.Contains(s.type)) keep.Add(s);
        iv.Stats = keep.ToArray();
    }

    // Affix lines for the item tooltip, e.g. "Brutal +27%"
    public static string Describe(ItemValue iv)
    {
        int kind = Kind(iv.ItemClass);
        if (kind == 0 || iv.Stats == null) return null;
        List<string> lines = new List<string>();
        bool twin = IsTwin(iv);
        foreach (ItemValue.Stat s in iv.Stats)
        {
            if (!s.isBoosted) continue;
            AposAffix a = Find(s.type, kind == 1);
            if (a == null || (twin && s.type == PassiveEffects.RoundRayCount)) continue;
            float fraction = s.value * StatStep;
            // Twin's -40% damage is folded into the damage stat; show only the Brutal part
            if (twin && s.type == PassiveEffects.EntityDamage) fraction = (1f + fraction) / (1f + ScatterDamage) - 1f;
            float pct = fraction * 100f;
            if (Math.Abs(pct) < 0.5f) continue;
            string name = Localization.Get("aposAffix" + a.Key);
            lines.Add("[FFD24D]" + name + "[-] " + (pct > 0 ? "+" : "") + pct.ToString("0", CultureInfo.InvariantCulture) + "%");
        }
        if (twin) lines.Add("[FFD24D]" + Localization.Get("aposAffixTwin") + "[-] " + Localization.Get("aposAffixTwinDesc"));
        if (lines.Count == 0) return null;
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

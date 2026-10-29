// Quality 7-10 ("sharpening" in the Eridium Forge, design: docs/Design-Sharpening.md).
// The game only knows quality 1-6, so the item keeps Quality = 6 and the extra tier lives in its metadata
// (aposTier = 7..10). Every value that grows with quality in the item's own XML (tier="1,6" lists: damage, armor,
// durability, magazine, apparel bonuses...) continues with the same step: one tier = (value at the top tier -
// value at the first tier) / (number of steps). Done by swapping the value list for an extrapolated copy while the
// sharpened item's effects are evaluated (Harmony on ItemValue.ModifyValue / GetModifiedValueData + PassiveEffect.ModValue).
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

public static class AposTiers
{
    public const string TierKey = "aposTier";
    public const string SpentKey = "aposTierSpent"; // Eridium put into sharpening (half comes back on scrap)
    public const int MaxTier = 10;

    // extra tiers of the item whose effects are being evaluated right now (0 = none)
    [ThreadStatic] public static int Extra;

    // cache: original value list -> extrapolated lists for 1..4 extra tiers (lists are shared per effect, never change)
    static readonly ConditionalWeakTable<float[], float[][]> cache = new ConditionalWeakTable<float[], float[][]>();

    public static int Tier(ItemValue iv)
    {
        if (iv == null) return 0;
        int t;
        if (iv.Quality >= 6 && iv.TryGetMetadata(TierKey, out t) && t > 6) return Math.Min(t, MaxTier);
        return iv.Quality;
    }

    public static void SetTier(ItemValue iv, int tier)
    {
        if (tier <= 6) iv.RemoveMetaData(TierKey);
        else iv.SetMetadata(TierKey, Math.Min(tier, MaxTier));
    }

    public static int Spent(ItemValue iv)
    {
        int s;
        return iv != null && iv.TryGetMetadata(SpentKey, out s) ? s : 0;
    }

    public static void AddSpent(ItemValue iv, int eridium)
    {
        iv.SetMetadata(SpentKey, Spent(iv) + eridium);
    }

    public static float[] Extrapolate(float[] values, float[] levels, int extra)
    {
        float[][] perExtra = cache.GetValue(values, v => new float[MaxTier - 5][]);
        float[] result = perExtra[extra];
        if (result != null) return result;

        int last = values.Length - 1;
        float steps = levels[last] - levels[0];
        float step = steps > 0f ? (values[last] - values[0]) / steps : 0f;
        float top = values[last] + step * extra;
        // a value that shrinks with quality (e.g. a penalty) stops at zero instead of changing sign
        if (values[last] > 0f && top < 0f || values[last] < 0f && top > 0f) top = 0f;
        result = (float[])values.Clone();
        result[last] = top;
        perExtra[extra] = result;
        return result;
    }

    // Does the item have anything that grows with quality? (sharpening a pure cosmetic is pointless)
    public static bool HasTieredValues(ItemClass ic)
    {
        if (ic == null || ic.Effects == null || ic.Effects.EffectGroups == null) return false;
        foreach (MinEffectGroup g in ic.Effects.EffectGroups)
        {
            if (g.PassiveEffects == null) continue;
            foreach (PassiveEffect p in g.PassiveEffects)
            {
                if (p.Levels != null && p.Values != null && p.Values.Length >= 2 && p.Values.Length == p.Levels.Length
                    && p.Values[0] != p.Values[p.Values.Length - 1]) return true;
            }
        }
        return false;
    }

    // " [7]" after the item name, gold at 10
    public static string NameSuffix(ItemValue iv)
    {
        int t = Tier(iv);
        if (t <= 6) return "";
        return t >= MaxTier ? " [FFD24D][" + t + "][-]" : " [BE5AFF][" + t + "][-]";
    }
}

[HarmonyPatch(typeof(ItemValue), "ModifyValue")]
public static class AposTierModifyPatch
{
    static void Prefix(ItemValue __instance, out int __state)
    {
        __state = AposTiers.Extra;
        AposTiers.Extra = Math.Max(0, AposTiers.Tier(__instance) - 6);
    }

    static void Postfix(int __state)
    {
        AposTiers.Extra = __state;
    }
}

[HarmonyPatch(typeof(ItemValue), "GetModifiedValueData")]
public static class AposTierDataPatch
{
    static void Prefix(ItemValue __instance, out int __state)
    {
        __state = AposTiers.Extra;
        AposTiers.Extra = Math.Max(0, AposTiers.Tier(__instance) - 6);
    }

    static void Postfix(int __state)
    {
        AposTiers.Extra = __state;
    }
}

[HarmonyPatch(typeof(PassiveEffect), "ModValue")]
public static class AposTierValuePatch
{
    static void Prefix(float _level, float[] _levels, ref float[] _values)
    {
        int extra = AposTiers.Extra;
        if (extra <= 0 || _levels == null || _values == null || _values.Length < 2 || _values.Length != _levels.Length) return;
        if (_level < _levels[_levels.Length - 1]) return; // only items at the top tier (quality 6)
        _values = AposTiers.Extrapolate(_values, _levels, extra);
    }
}

// The item name in the tooltip shows the tier: "Pistol [8]"
[HarmonyPatch(typeof(XUiC_ItemInfoWindow), "GetBindingValueInternal")]
public static class AposTierNamePatch
{
    static void Postfix(ref string value, string bindingName, bool __result, ItemStack ___itemStack)
    {
        if (!__result || bindingName != "itemname" || ___itemStack == null || ___itemStack.IsEmpty()) return;
        value += AposTiers.NameSuffix(___itemStack.itemValue);
    }
}

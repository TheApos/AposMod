// Perk highlights that change rewards (Harmony):
// - Daring Adventurer 5 "Big Score": the weapon reward of a tier 5+ quest is quality 6 with 3 affixes
// - Better Barter 5 "Black Market": every trader restock has a 25% chance to add a legendary weapon (quality 6)
// Single player: the perk owner is the primary (local) player.
using System;
using System.Collections.Generic;
using HarmonyLib;

public static class AposRewards
{
    public static readonly string[] Legendaries =
    {
        "aposLegendHarold", "aposLegendLadyFist", "aposLegendDragonBreath", "aposLegendRipper", "aposLegendThunderclap",
        "aposLegendHornet", "aposLegendStormFist", "aposLegendArtemis", "aposLegendBigBoom", "aposLegendPredator"
    };

    static readonly Random rand = new Random();

    public static EntityPlayerLocal Player()
    {
        GameManager gm = GameManager.Instance;
        return (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
    }

    public static ItemStack RandomLegendary()
    {
        ItemValue iv = ItemClass.GetItem(Legendaries[rand.Next(Legendaries.Length)]);
        if (iv == null || iv.type == 0) return null;
        iv = new ItemValue(iv.type, 6, 6, true, null, 1f);
        return new ItemStack(iv, 1);
    }

    public static bool Roll(float chance)
    {
        return rand.NextDouble() < chance;
    }
}

[HarmonyPatch(typeof(LootContainer), "GetRewardItem")]
public static class AposBigScorePatch
{
    static void Postfix(float questDifficulty, ItemStack __result)
    {
        if (__result == null || __result.IsEmpty() || questDifficulty < 5f) return;
        if (AposAffixes.Kind(__result.itemValue.ItemClass) == 0) return;
        if (AposAffixes.Level(AposRewards.Player(), "perkDaringAdventurer") < 5) return;
        __result.itemValue.Quality = 6;
        AposAffixes.Roll(__result.itemValue, true);
    }
}

[HarmonyPatch(typeof(TraderInfo), "Spawn")]
public static class AposBlackMarketPatch
{
    static void Postfix(List<ItemStack> __result)
    {
        if (__result == null || AposAffixes.Level(AposRewards.Player(), "perkBetterBarter") < 5) return;
        if (!AposRewards.Roll(0.25f)) return;
        ItemStack legend = AposRewards.RandomLegendary();
        if (legend != null) __result.Add(legend);
    }
}

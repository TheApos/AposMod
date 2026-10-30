// Perk highlights that watch the local player's stats every frame:
// - Rule 1: Cardio 5 "Second Lungs": out of stamina -> +50% stamina, once per 60 s (buffAposSecondLungsCD)
// - Slow Metabolism 5 "Iron Stomach": food and water never drop below 25%
// - Master Chef 3 "Gourmet": food and drink buffs last 3 times longer (their elapsed time runs at 1/3 speed)
using System;
using System.Collections.Generic;
using UnityEngine;

public class AposSurvival : MonoBehaviour
{
    static readonly HashSet<string> FoodBuffs = new HashSet<string>
    {
        "buffAtomicSmoothieRun", "buffBeer", "buffBlackStrapCoffee", "buffBurntSmoothieRun", "buffBurnt_Hazard_Over",
        "buffChickenNuggets", "buffChickenWings", "buffCoffee", "buffDesert_Hazard_Over", "buffDrunkGrandpasAwesomeSauce",
        "buffDrunkGrandpasLearningElixir", "buffDrunkGrandpasMoonshine", "buffFrostbiteSmoothieRun", "buffMegaCrush",
        "buffOasisSmoothieRun", "buffPumpkinCheesecake", "buffRedTea", "buffRoadrunner", "buffShamChowder",
        "buffSnow_Hazard_Over", "buffWasteland_Hazard_Over", "buffYuccaJuiceSmoothie"
    };

    // last seen elapsed ticks per food buff, and the leftover fraction of a tick to give back
    readonly Dictionary<string, uint> lastTicks = new Dictionary<string, uint>();
    readonly Dictionary<string, float> carry = new Dictionary<string, float>();

    void Update()
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        if (player == null || player.IsDead()) return;
        EntityStats stats = player.Stats;

        if (AposAffixes.Level(player, "perkRuleOneCardio") >= 5 && stats.Stamina.Value < 1f && !player.Buffs.HasBuff("buffAposSecondLungsCD"))
        {
            stats.Stamina.Value += stats.Stamina.Max * 0.5f;
            player.Buffs.AddBuff("buffAposSecondLungsCD");
        }

        if (AposAffixes.Level(player, "perkSlowMetabolism") >= 5)
        {
            if (stats.Food.Value < stats.Food.Max * 0.25f) stats.Food.Value = stats.Food.Max * 0.25f;
            if (stats.Water.Value < stats.Water.Max * 0.25f) stats.Water.Value = stats.Water.Max * 0.25f;
        }

        if (AposAffixes.Level(player, "perkMasterChef") >= 3) SlowFoodBuffs(player);

        // Set bonuses grow on forge tiers 7..10: 5% per tier above 6 of the lowest worn armor piece (head, chest,
        // hands, feet), read by the set bonus buffs as aposSetExtra5
        if (Time.time >= nextSetCheck)
        {
            nextSetCheck = Time.time + 1f;
            player.Buffs.SetCustomVar("aposSetExtra5", 0.05f * LowestArmorTierAbove6(player));
        }
    }

    float nextSetCheck;

    static int LowestArmorTierAbove6(EntityPlayerLocal player)
    {
        int lowest = int.MaxValue, pieces = 0;
        Equipment eq = player.equipment;
        for (int i = 0; i < eq.GetSlotCount(); i++)
        {
            ItemValue iv = eq.GetSlotItem(i);
            if (iv == null || iv.IsEmpty() || iv.ItemClass == null || !iv.ItemClass.Properties.Contains("ArmorGroup")) continue;
            pieces++;
            lowest = Math.Min(lowest, AposTiers.Tier(iv));
        }
        return pieces >= 4 ? Math.Max(0, lowest - 6) : 0;
    }

    void SlowFoodBuffs(EntityPlayerLocal player)
    {
        List<BuffValue> buffs = player.Buffs.ActiveBuffs;
        for (int i = 0; i < buffs.Count; i++)
        {
            BuffValue b = buffs[i];
            string name = b.BuffName;
            if (!FoodBuffs.Contains(name)) continue;
            uint now = b.DurationInTicks;
            uint last;
            if (!lastTicks.TryGetValue(name, out last) || now < last)
            {
                lastTicks[name] = now; // new buff or re-applied
                carry[name] = 0f;
                continue;
            }
            // give back 2/3 of the time that passed -> the buff lasts 3x longer
            float back = (now - last) * 2f / 3f + carry[name];
            uint whole = (uint)back;
            carry[name] = back - whole;
            if (whole > now) whole = now;
            b.DurationInTicks = now - whole;
            lastTicks[name] = b.DurationInTicks;
        }
    }
}

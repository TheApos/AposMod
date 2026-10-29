// Hit effects that need the real damage of a hit (Harmony on EntityAlive.damageEntityLocal, runs on the server):
// - Leech affix: heals the attacker by a part of the damage dealt
// - Strength 10 "Overkill": damage beyond a killing blow with a Strength weapon hits the nearest zombie within 4 m
// - Penetrator 5 "Shrapnel": armor-piercing hits deal 40% of the damage to every zombie within 3 m of the target
// - Boomstick 5 "Pump Action": a shotgun kill loads 1 shell into the magazine
// - Demolitions Expert 5 "Chain Reaction": an explosive kill blows the zombie up (250 within 4 m)
// - Advanced Engineering 5 "Overclock": your traps and turret blocks deal +50%
// - Strength Mastery 5 "Crippling Blows": the flagged target becomes a crawler
// - Fortitude Mastery 5 "Second Wind" (damage to the player)
// Extra damage goes through buffAposSplash (reads the cvar aposSplashDmg), like bow splinters.
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

public static class AposCombat
{
    const float OverkillRadius = 4f;
    const float ShrapnelRadius = 3f;
    const float ShrapnelShare = 0.4f;
    const float OverclockShare = 0.5f;
    const float ChainRadius = 4f;
    const int ChainDamage = 250;

    static readonly FastTags<TagGroup.Global> StrengthTag = FastTags<TagGroup.Global>.Parse("attStrength");
    static readonly FastTags<TagGroup.Global> ShotgunTag = FastTags<TagGroup.Global>.Parse("shotgun");
    static readonly FastTags<TagGroup.Global> DemolitionsTag = FastTags<TagGroup.Global>.Parse("perkDemolitionsExpert");
    static readonly FastTags<TagGroup.Global> AmmoTag = FastTags<TagGroup.Global>.Parse("ammo");
    static readonly FastTags<TagGroup.Global> MolotovTag = FastTags<TagGroup.Global>.Parse("molotov");

    public static void OnDamaged(EntityAlive target, DamageSource source, ref DamageResponse response, int healthBefore)
    {
        if (target == null || source == null) return;
        World world = GameManager.Instance.World;
        if (world == null) return;
        if (target is EntityPlayer)
        {
            if (!response.Fatal) SecondWind(target as EntityPlayer, healthBefore - response.Strength);
            return;
        }

        int dealt = Math.Min(response.Strength, healthBefore);
        if (dealt <= 0) return;

        // traps and turret blocks: the damage carries the block position and its owner
        if (source.BlockPosition != Vector3i.zero)
        {
            EntityPlayer owner = world.GetEntity(source.CreatorEntityId) as EntityPlayer ?? world.GetEntity(source.ownerEntityId) as EntityPlayer;
            if (owner != null && AposAffixes.Level(owner, "perkAdvancedEngineering") >= 5)
            {
                Splash(target, Mathf.RoundToInt(dealt * OverclockShare)); // Overclock
            }
            return;
        }

        EntityPlayer attacker = world.GetEntity(source.getEntityId()) as EntityPlayer;
        if (attacker == null) return;

        // the last hit's damage: elemental ticks and the Explosion reaction scale with it (buffs.xml)
        target.Buffs.SetCustomVar("aposHitDmg", response.Strength);

        // Strength Mastery 5 "Crippling Blows": a club/sledge power hit flagged the target (progression.xml)
        if (!response.Fatal && target.Buffs.GetCustomVar("aposCripple") > 0f)
        {
            target.Buffs.SetCustomVar("aposCripple", 0f);
            response.TurnIntoCrawler = true;
        }
        ItemValue weapon = source.AttackingItem;
        if (weapon == null || weapon.IsEmpty()) weapon = attacker.inventory.holdingItemItemValue;
        ItemClass weaponClass = weapon != null ? weapon.ItemClass : null;

        float leech = AposAffixes.StatFraction(weapon, PassiveEffects.HealthSteal);
        if (weaponClass != null && weaponClass.Properties.Contains("AposLeech")) leech += weaponClass.Properties.GetFloat("AposLeech");
        if (leech > 0f)
        {
            int heal = Mathf.Max(1, Mathf.RoundToInt(dealt * leech));
            attacker.AddHealth(heal);
        }

        if (response.Fatal && weaponClass != null)
        {
            int excess = response.Strength - healthBefore;
            if (excess > 0 && weaponClass.ItemTags.Test_AnySet(StrengthTag) && AposAffixes.Level(attacker, "attStrength") >= 10)
            {
                EntityAlive next = Nearest(world, target, OverkillRadius);
                if (next != null) Splash(next, excess);
            }
            if (weaponClass.ItemTags.Test_AnySet(ShotgunTag) && AposAffixes.Level(attacker, "perkBoomstick") >= 5)
            {
                LoadShell(attacker as EntityPlayerLocal, weaponClass);
            }
            // Demolitions Expert 5 "Chain Reaction": a zombie killed by your explosive blows up too
            if (IsExplosive(weaponClass) && AposAffixes.Level(attacker, "perkDemolitionsExpert") >= 5)
            {
                target.Buffs.AddBuff("buffAposChainBlast"); // visual only
                foreach (EntityAlive e in Around(world, target, ChainRadius)) Splash(e, ChainDamage);
            }
        }

        if (AposAffixes.Level(attacker, "perkPenetrator") >= 5 && IsArmorPiercing(source.AttackingItem, attacker))
        {
            int share = Mathf.RoundToInt(dealt * ShrapnelShare);
            if (share > 0)
            {
                foreach (EntityAlive e in Around(world, target, ShrapnelRadius)) Splash(e, share);
            }
        }
    }

    static bool IsExplosive(ItemClass ic)
    {
        // rockets, grenades, pipe bombs, exploding arrows arrive as the ammo item; molotov fire does not count
        return ic.ItemTags.Test_AnySet(DemolitionsTag) && ic.ItemTags.Test_AnySet(AmmoTag) && !ic.ItemTags.Test_AnySet(MolotovTag);
    }

    // Fortitude Mastery 5 "Second Wind": once per 5 minutes, dropping below 20% health heals 70% of max health
    static void SecondWind(EntityPlayer player, int healthAfter)
    {
        if (AposAffixes.Level(player, "perkFortitudeMastery") < 5 || player.Buffs.HasBuff("buffAposSecondWindCD")) return;
        float max = player.Stats.Health.Max;
        if (healthAfter <= 0 || healthAfter >= max * 0.2f) return;
        player.AddHealth(Mathf.RoundToInt(max * 0.7f));
        player.Buffs.AddBuff("buffAposSecondWindCD");
    }

    static bool IsArmorPiercing(ItemValue attacking, EntityPlayer attacker)
    {
        // arrows and bolts arrive as the ammo item, bullets as the weapon with its loaded ammo
        if (attacking != null && attacking.ItemClass != null && IsApAmmo(attacking.ItemClass.Name)) return true;
        ItemClass held = attacker.inventory.holdingItem;
        ItemActionAttack attack = (held != null && held.Actions != null && held.Actions.Length > 0) ? held.Actions[0] as ItemActionAttack : null;
        if (attack == null || attack.MagazineItemNames == null || attack.MagazineItemNames.Length == 0) return false;
        int index = attacker.inventory.holdingItemItemValue.SelectedAmmoTypeIndex;
        if (index < 0 || index >= attack.MagazineItemNames.Length) index = 0;
        return IsApAmmo(attack.MagazineItemNames[index]);
    }

    static bool IsApAmmo(string name)
    {
        return name.StartsWith("ammo") && name.EndsWith("AP");
    }

    static void LoadShell(EntityPlayerLocal player, ItemClass weaponClass)
    {
        if (player == null || player.inventory.holdingItem != weaponClass) return;
        ItemActionRanged ranged = weaponClass.Actions[0] as ItemActionRanged;
        if (ranged == null) return;
        ItemInventoryData data = player.inventory.holdingItemData;
        ItemValue iv = data.itemValue;
        int max = (int)EffectManager.GetValue(PassiveEffects.MagazineSize, iv, ranged.BulletsPerMagazine, player);
        if (iv.Meta >= max) return;
        iv.Meta++;
        ItemValue hand = player.inventory.holdingItemItemValue;
        if (hand != iv) hand.Meta = iv.Meta;
        player.inventory.CallOnToolbeltChangedInternal();
    }

    static void Splash(EntityAlive e, int damage)
    {
        e.Buffs.SetCustomVar("aposSplashDmg", -damage);
        e.Buffs.AddBuff("buffAposSplash");
    }

    static IEnumerable<EntityAlive> Around(World world, EntityAlive center, float radius)
    {
        List<Entity> list = world.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e == center || e is EntityPlayer || e.IsDead()) continue;
            if ((e.position - center.position).sqrMagnitude <= radius * radius) yield return e;
        }
    }

    static EntityAlive Nearest(World world, EntityAlive center, float radius)
    {
        EntityAlive best = null;
        float bestDist = float.MaxValue;
        foreach (EntityAlive e in Around(world, center, radius))
        {
            float d = (e.position - center.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }
}

[HarmonyPatch(typeof(EntityAlive), "damageEntityLocal")]
public static class AposCombatDamagePatch
{
    static void Prefix(EntityAlive __instance, out int __state)
    {
        __state = __instance.Health;
    }

    static void Postfix(EntityAlive __instance, DamageSource _damageSource, ref DamageResponse __result, int __state)
    {
        try
        {
            AposCombat.OnDamaged(__instance, _damageSource, ref __result, __state);
        }
        catch (Exception e)
        {
            Debug.LogError("[AposMod] Combat hook failed: " + e);
        }
    }
}

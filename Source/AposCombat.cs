// Hit effects that need the real damage of a hit (Harmony on EntityAlive.damageEntityLocal, runs on the server):
// - Leech affix: heals the attacker by a part of the damage dealt
// - Strength 10 "Overkill": damage beyond a killing blow with a Strength weapon hits the nearest zombie within 4 m
// - Penetrator 5 "Shrapnel": armor-piercing hits deal 40% of the damage to every zombie within 3 m of the target
// - Boomstick 5 "Pump Action": a shotgun kill loads 1 shell into the magazine
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

    static readonly FastTags<TagGroup.Global> StrengthTag = FastTags<TagGroup.Global>.Parse("attStrength");
    static readonly FastTags<TagGroup.Global> ShotgunTag = FastTags<TagGroup.Global>.Parse("shotgun");

    public static void OnDamaged(EntityAlive target, DamageSource source, DamageResponse response, int healthBefore)
    {
        if (target == null || target is EntityPlayer || source == null) return;
        World world = GameManager.Instance.World;
        if (world == null) return;
        EntityPlayer attacker = world.GetEntity(source.getEntityId()) as EntityPlayer;
        if (attacker == null) return;

        int dealt = Math.Min(response.Strength, healthBefore);
        if (dealt <= 0) return;
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

    static void Postfix(EntityAlive __instance, DamageSource _damageSource, DamageResponse __result, int __state)
    {
        try
        {
            AposCombat.OnDamaged(__instance, _damageSource, __result, __state);
        }
        catch (Exception e)
        {
            Debug.LogError("[AposMod] Combat hook failed: " + e);
        }
    }
}

// Machine Gunner 5: every kill adds 5 to the player cvar aposAmmoRefund (entityclasses.xml).
// Here we turn it into rounds of the ammo currently loaded in the held weapon and put them in the backpack.
using UnityEngine;

public class AposAmmo : MonoBehaviour
{
    const string RefundVar = "aposAmmoRefund";
    const string MagRefundVar = "aposMagRefund";

    // Adds rounds straight into the magazine of the held ranged weapon (up to its magazine size)
    public static void LoadRounds(EntityPlayerLocal player, int rounds)
    {
        if (player == null || rounds <= 0) return;
        ItemClass held = player.inventory.holdingItem;
        ItemActionRanged ranged = (held != null && held.Actions != null && held.Actions.Length > 0) ? held.Actions[0] as ItemActionRanged : null;
        if (ranged == null) return;
        ItemValue iv = player.inventory.holdingItemData.itemValue;
        int max = (int)EffectManager.GetValue(PassiveEffects.MagazineSize, iv, ranged.BulletsPerMagazine, player);
        if (iv.Meta >= max) return;
        iv.Meta = Mathf.Min(max, iv.Meta + rounds);
        ItemValue hand = player.inventory.holdingItemItemValue;
        if (hand != iv) hand.Meta = iv.Meta;
        player.inventory.CallOnToolbeltChangedInternal();
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        if (player == null) return;

        // Suppression "Thrifty": every 4th hit puts 1 round back into the magazine (entityclasses.xml sets aposMagRefund)
        int toMag = (int)player.Buffs.GetCustomVar(MagRefundVar);
        if (toMag > 0)
        {
            player.Buffs.SetCustomVar(MagRefundVar, 0f);
            LoadRounds(player, toMag);
        }

        int count = (int)player.Buffs.GetCustomVar(RefundVar);
        if (count <= 0) return;
        player.Buffs.SetCustomVar(RefundVar, 0f);

        ItemValue weapon = player.inventory.holdingItemItemValue;
        ItemClass weaponClass = player.inventory.holdingItem;
        ItemActionAttack attack = (weaponClass != null && weaponClass.Actions != null && weaponClass.Actions.Length > 0)
            ? weaponClass.Actions[0] as ItemActionAttack : null;
        if (attack == null || attack.MagazineItemNames == null || attack.MagazineItemNames.Length == 0) return;

        int index = weapon.SelectedAmmoTypeIndex;
        if (index < 0 || index >= attack.MagazineItemNames.Length) index = 0;
        ItemValue ammo = ItemClass.GetItem(attack.MagazineItemNames[index]);
        if (ammo == null || ammo.type == 0) return;

        ItemStack stack = new ItemStack(ammo, count);
        if (!player.bag.AddItem(stack) && !player.inventory.AddItem(stack))
        {
            Debug.Log("[AposMod] Ammo refund: backpack and toolbelt are full, " + count + " rounds lost");
        }
    }
}

// Bows (ItemActionCatapult; crossbows are Launchers and keep vanilla behaviour):
// - R opens the arrow wheel at once. Vanilla waits XUiC_Radial.displayDelay (0.25 s) and treats a short tap as reload;
//   a bow reloads by itself here, so R is only for picking arrows (hold R, point, release).
// - The bow nocks the next arrow by itself while in hand. Vanilla does it only when you try to shoot an empty bow
//   (ItemActionCatapult.ExecuteAction -> GameManager.ItemReloadServer); we make the same call.
using UnityEngine;

public class AposBow : MonoBehaviour
{
    float nextCheck;

    // The bow in the local player's hand and its action data, or null
    public static ItemActionCatapult Held(EntityPlayerLocal p, out ItemActionData data)
    {
        data = null;
        if (p == null || p.inventory == null) return null;
        ItemClass ic = p.inventory.holdingItem;
        ItemActionCatapult bow = ic != null && ic.Actions != null && ic.Actions.Length > 0 ? ic.Actions[0] as ItemActionCatapult : null;
        if (bow == null || p.inventory.holdingItemData == null || p.inventory.holdingItemData.actionData == null) return null;
        data = p.inventory.holdingItemData.actionData[0];
        return data != null ? bow : null;
    }

    // A reload that did not nock an arrow is not retried until something changes: an endless reload would block
    // the backpack (EntityPlayerLocal.CancelInventoryActions waits while reloading) and vehicles (IsHoldingItemActionRunning).
    const float RetryDelay = 2f;
    float lastRequest = -100f;
    bool gaveUp;
    ItemClass lastBow;

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 0.2f;
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal p = gm != null && gm.World != null ? gm.World.GetPrimaryPlayer() : null;
        if (p == null || p.IsDead() || p.inventory.IsHandSwitching()) return;
        // opening the backpack / a window, sitting in a vehicle: leave the bow alone
        if (p.CancellingInventoryActions || p.AttachedToEntity != null) return;
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(p);
        if (ui != null && ui.windowManager.IsCursorWindowOpen()) return;

        ItemActionData data;
        ItemActionCatapult bow = Held(p, out data);
        ItemActionRanged.ItemActionDataRanged ranged = data as ItemActionRanged.ItemActionDataRanged;
        if (bow == null || ranged == null || bow.InfiniteAmmo) return;
        if (p.inventory.holdingItem != lastBow)
        {
            lastBow = p.inventory.holdingItem;
            gaveUp = false;
            lastRequest = -100f;
        }
        if (data.invData.itemValue.Meta != 0)
        {
            gaveUp = false; // nocked: the next empty bow may reload again
            lastRequest = -100f;
            return;
        }
        if (gaveUp || ItemActionRanged.Reloading(ranged) || Time.time - lastRequest < RetryDelay) return;
        if (lastRequest > 0f && Time.time - lastRequest < RetryDelay * 2f)
        {
            // the last request ended without an arrow on the string
            gaveUp = true;
            return;
        }
        if (!bow.CanReload(data)) return;
        lastRequest = Time.time;
        gm.ItemReloadServer(p.entityId);
    }
}

// Arrow wheel without the hold delay while a bow is in hand
[HarmonyLib.HarmonyPatch(typeof(XUiC_Radial), "Open")]
public static class AposBowRadialPatch
{
    static void Postfix(XUiC_Radial __instance)
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal p = gm != null && gm.World != null ? gm.World.GetPrimaryPlayer() : null;
        ItemActionData data;
        if (AposBow.Held(p, out data) != null) __instance.openTime = Time.time - __instance.displayDelay;
    }
}

// Bows (ItemActionCatapult; crossbows are Launchers and keep vanilla behaviour):
// - R opens the arrow wheel at once. Vanilla waits XUiC_Radial.displayDelay (0.25 s) and treats a short tap as reload;
//   a bow reloads by itself here, so R is only for picking arrows (hold R, point, release).
// - The bow nocks the next arrow by itself while in hand. Vanilla does it only when you try to shoot an empty bow
//   (ItemActionCatapult.ExecuteAction -> GameManager.ItemReloadServer); we make the same call.
using System.Collections.Generic;
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
    const float StuckReload = 3f;
    float reloadSince = -1f;
    public static bool AutoNock = true; // aposbow off: switch the auto-nock off to compare with vanilla
    float lastRequest = -100f;
    bool gaveUp;
    ItemClass lastBow;

    // One line with everything that can block the backpack (reload, holster) or a vehicle (an action running)
    public static string State(EntityPlayerLocal p)
    {
        ItemActionData data;
        ItemActionCatapult bow = Held(p, out data);
        ItemActionRanged.ItemActionDataRanged r = data as ItemActionRanged.ItemActionDataRanged;
        if (bow == null || r == null) return "no bow in hand";
        string running = "";
        ItemClass ic = p.inventory.holdingItem;
        for (int i = 0; i < ic.Actions.Length && i < p.inventory.holdingItemData.actionData.Count; i++)
        {
            if (ic.Actions[i] != null) running += " a" + i + "=" + ic.Actions[i].IsActionRunning(p.inventory.holdingItemData.actionData[i]);
        }
        HarmonyLib.Traverse tr = HarmonyLib.Traverse.Create(p);
        return "bow " + ic.Name + ": auto-nock " + AutoNock + ", meta " + data.invData.itemValue.Meta
            + ", requested " + r.isReloadRequested + ", reloading " + r.isReloading + ", weaponReloading " + r.isWeaponReloading
            + ", running" + running + ", cancellingInventory " + p.CancellingInventoryActions
            + ", holster " + tr.Method("getCurrentHolsterState").GetValue() + " -> " + tr.Field("desiredHolsterState").GetValue()
            + ", lastShot " + (Time.time - r.m_LastShotTime).ToString("0.0") + " s ago, delay " + r.Delay
            + ", windowOpen " + LocalPlayerUI.GetUIForPlayer(p).windowManager.IsCursorWindowOpen();
    }

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 0.2f;
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal p = gm != null && gm.World != null ? gm.World.GetPrimaryPlayer() : null;
        if (p == null || p.IsDead() || p.inventory.IsHandSwitching()) return;

        ItemActionData data;
        ItemActionCatapult bow = Held(p, out data);
        ItemActionRanged.ItemActionDataRanged ranged = data as ItemActionRanged.ItemActionDataRanged;
        if (bow == null || ranged == null) return;

        // Watchdog: a bow reload takes about a second. Reload flags that stay up longer are stuck (the game clears
        // them only when the first-person reload animation exits) and would block the backpack and vehicles forever.
        // While the game waits to open the backpack, half a second is enough.
        bool reloading = ItemActionRanged.Reloading(ranged);
        float limit = p.CancellingInventoryActions ? 0.5f : StuckReload;
        if (!reloading) reloadSince = -1f;
        else if (reloadSince < 0f) reloadSince = Time.time;
        else if (Time.time - reloadSince > limit)
        {
            Debug.Log("[AposMod] Bow reload was stuck for " + limit + " s, cancelled (meta " + data.invData.itemValue.Meta + ")");
            ranged.isReloadRequested = false;
            p.ForceCancelReload();
            reloadSince = -1f;
            gaveUp = true; // do not start it again until an arrow is nocked or the bow changes
            return;
        }

        // opening the backpack / a window, sitting in a vehicle: leave the bow alone
        if (!AutoNock || bow.InfiniteAmmo || p.CancellingInventoryActions || p.AttachedToEntity != null) return;
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(p);
        if (ui != null && ui.windowManager.IsCursorWindowOpen()) return;
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

// Arrow wheel without the hold delay. Only the ammo wheel (R): Tab and E open the same radial window, where a short tap
// is the normal action (backpack, enter a vehicle) - skipping the delay there broke them while a bow was in hand.
[HarmonyLib.HarmonyPatch(typeof(ItemActionAttack), "SetupRadial")]
public static class AposBowRadialPatch
{
    static void Postfix(ItemActionAttack __instance, XUiC_Radial _xuiRadialWindow)
    {
        if (__instance is ItemActionCatapult && _xuiRadialWindow != null) _xuiRadialWindow.openTime = Time.time - _xuiRadialWindow.displayDelay;
    }
}

// aposbow          -> the held bow's reload state (for "can't open the backpack / enter a vehicle" reports)
// aposbow off|on   -> switch the bow auto-nock off / on until the game restarts
public class AposBowCommand : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "aposbow" };
    }

    public override string getDescription()
    {
        return "AposMod bow: state of the held bow's reload | aposbow off|on (auto-nock)";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        SdtdConsole console = SingletonMonoBehaviour<SdtdConsole>.Instance;
        if (_params.Count > 0 && (_params[0] == "off" || _params[0] == "on"))
        {
            AposBow.AutoNock = _params[0] == "on";
            console.Output("Bow auto-nock: " + (AposBow.AutoNock ? "on" : "off"));
            return;
        }
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal p = gm != null && gm.World != null ? gm.World.GetPrimaryPlayer() : null;
        ItemActionData data;
        ItemActionCatapult bow = AposBow.Held(p, out data);
        ItemActionRanged.ItemActionDataRanged r = data as ItemActionRanged.ItemActionDataRanged;
        if (bow == null || r == null)
        {
            console.Output("No bow in hand.");
            return;
        }
        console.Output(AposBow.State(p));
    }
}

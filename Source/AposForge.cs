// Eridium Forge: a workstation block (blocks.xml, Class "AposEridiumForge, AposMod" -> BlockAposEridiumForge) that
// opens the window group "aposForge" (Config/XUi_InGame/xui.xml + windows.xml, controller "AposForge, AposMod" ->
// XUiC_AposForge). Three tabs:
// - Weapon (the one in your hands): per affix row Reroll (10 Eridium) / Lock (25, the locked row unlocks for free),
//   an empty row: Add (20); Scrap into Eridium; Sharpen 1 -> 10 (7..10: Source/AposTiers.cs)
// - Apparel: sharpen every worn item that has values growing with quality
// - Legendaries: 10 legendaries for 300 each, quality 6
// Sharpening (one quality at a time, always paid): up to 6 it always works; 7-10 can fail, a failure breaks the item
// (30% of its scrap value comes back). "Safe" costs 5x: a failure drops one tier instead (not below 6).
// Every sharpening attempt takes 2.5 s with anvil sounds and a progress line before the result.
// Eridium and steel are taken from the backpack and the toolbelt.
using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockAposEridiumForge : Block
{
    public override bool HasBlockActivationCommands(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return true;
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return new BlockActivationCommand[] { new BlockActivationCommand("open", "hand", true, false, null) };
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return Localization.Get("aposForgeActivate");
    }

    public override bool OnBlockActivated(string _commandName, WorldBase _world, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        return OnBlockActivated(_world, _blockPos, _blockValue, _player);
    }

    public override bool OnBlockActivated(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(_player);
        if (ui != null) ui.windowManager.Open("aposForge", true);
        return true;
    }
}

public class XUiC_AposForge : XUiController
{
    public const int RerollCost = 10, AddCost = 20, LockCost = 25, LegendCost = 300, SafeMultiplier = 5;
    const string Steel = "resourceForgedSteel";
    const int Rows = 3, WearRows = 7;
    const float SharpenTime = 2.5f;

    // Sharpening to the next quality (Eridium = forged steel): 2..6 costs 5 x target and always works;
    // 7..10 costs 35..50 with this chance; a normal failure breaks the item, a Safe one (x5) drops one tier (not below 6)
    static readonly int[] HighCost = { 35, 40, 45, 50 };
    static readonly float[] HighChance = { 0.65f, 0.5f, 0.4f, 0.3f };

    static int CostFor(int target)
    {
        return target <= 6 ? 5 * target : HighCost[target - 7];
    }

    static float ChanceFor(int target)
    {
        return target <= 6 ? 1f : HighChance[target - 7];
    }

    XUiV_Label eridiumLabel, weaponLabel, sharpenLabel, progressLabel;
    readonly XUiV_Label[] affixLabels = new XUiV_Label[Rows];
    readonly XUiC_SimpleButton[] rerollButtons = new XUiC_SimpleButton[Rows];
    readonly XUiC_SimpleButton[] lockButtons = new XUiC_SimpleButton[Rows];
    XUiC_SimpleButton scrapButton, sharpenButton, sharpenSafeButton;
    readonly XUiV_Label[] wearLabels = new XUiV_Label[WearRows];
    readonly XUiC_SimpleButton[] wearButtons = new XUiC_SimpleButton[WearRows];
    readonly XUiC_SimpleButton[] wearSafeButtons = new XUiC_SimpleButton[WearRows];
    readonly int[] wearSlots = new int[WearRows]; // equipment slot shown in each apparel row (-1 = none)
    readonly XUiC_SimpleButton[] legendButtons = new XUiC_SimpleButton[AposRewards.Legendaries.Length];
    readonly XUiC_SimpleButton[] tabButtons = new XUiC_SimpleButton[3];
    readonly XUiController[] pages = new XUiController[3];
    int tab;
    float nextRefresh;

    // running sharpening attempt
    bool busy;
    float busyStart, nextHit;
    int busySlot;    // -1 = weapon in hand, else equipment slot
    bool busySafe;
    ItemValue busyItem;
    float resultUntil;

    public override void Init()
    {
        base.Init();
        eridiumLabel = Label(this, "lblEridium");
        weaponLabel = Label(this, "lblWeapon");
        sharpenLabel = Label(this, "lblSharpen");
        progressLabel = Label(this, "lblProgress");
        string[] pageIds = { "pageWeapon", "pageApparel", "pageLegends" };
        string[] tabIds = { "btnTabWeapon", "btnTabApparel", "btnTabLegends" };
        string[] tabKeys = { "aposForgeTabWeapon", "aposForgeTabApparel", "aposForgeTabLegends" };
        for (int i = 0; i < 3; i++)
        {
            int t = i;
            pages[i] = GetChildById(pageIds[i]);
            tabButtons[i] = Button(tabIds[i], () => tab = t);
            SetText(tabButtons[i], Localization.Get(tabKeys[i]));
        }
        for (int i = 0; i < Rows; i++)
        {
            int row = i; // C# 5: capture a copy for the lambda
            affixLabels[i] = Label(this, "lblAffix" + i);
            rerollButtons[i] = Button("btnReroll" + i, () => Reroll(row));
            lockButtons[i] = Button("btnLock" + i, () => Lock(row));
        }
        scrapButton = Button("btnScrap", Scrap);
        sharpenButton = Button("btnSharpen", () => StartSharpen(-1, false));
        sharpenSafeButton = Button("btnSharpenSafe", () => StartSharpen(-1, true));
        for (int i = 0; i < WearRows; i++)
        {
            int row = i;
            wearLabels[i] = Label(this, "lblWear" + i);
            wearButtons[i] = Button("btnWear" + i, () => StartSharpen(wearSlots[row], false));
            wearSafeButtons[i] = Button("btnWearSafe" + i, () => StartSharpen(wearSlots[row], true));
        }
        for (int i = 0; i < legendButtons.Length; i++)
        {
            int k = i;
            legendButtons[i] = Button("btnLegend" + i, () => BuyLegend(k));
            SetText(legendButtons[i], Localization.Get(AposRewards.Legendaries[i]) + "  [BE5AFF]" + LegendCost + "[-]");
        }
    }

    XUiC_SimpleButton Button(string id, Action onPress)
    {
        XUiC_SimpleButton b = GetChildById(id) as XUiC_SimpleButton;
        if (b == null)
        {
            Debug.LogError("[AposMod] Forge window: no button " + id);
            return null;
        }
        b.OnPressed += (_sender, _mouseButton) =>
        {
            if (busy) return;
            onPress();
            Refresh();
        };
        return b;
    }

    static XUiV_Label Label(XUiController parent, string id)
    {
        XUiController c = parent.GetChildById(id);
        return c != null ? c.ViewComponent as XUiV_Label : null;
    }

    static void SetText(XUiC_SimpleButton b, string text)
    {
        if (b == null) return;
        XUiV_Label l = Label(b, "btnLabel");
        if (l != null) l.Text = text;
    }

    void Enable(XUiC_SimpleButton b, bool on)
    {
        if (b != null) b.Enabled = on && !busy;
    }

    static void Show(XUiController c, bool on)
    {
        if (c != null && c.ViewComponent != null) c.ViewComponent.IsVisible = on;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        busy = false;
        if (progressLabel != null) progressLabel.Text = "";
        Refresh();
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (busy) TickSharpen();
        else if (resultUntil > 0f && Time.time > resultUntil)
        {
            resultUntil = 0f;
            if (progressLabel != null) progressLabel.Text = "";
        }
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + 0.3f; // the held weapon, worn items or counts may change while the window is open
        Refresh();
    }

    EntityPlayerLocal Player
    {
        get { return xui != null && xui.playerUI != null ? xui.playerUI.entityPlayer : null; }
    }

    static readonly FastTags<TagGroup.Global> WeaponTag = FastTags<TagGroup.Global>.Parse("weapon");

    // The weapon in hand (legendaries too), or null (tools, bare hands)
    ItemValue Held()
    {
        EntityPlayerLocal p = Player;
        if (p == null) return null;
        ItemValue iv = p.inventory.holdingItemItemValue;
        if (iv == null || iv.IsEmpty() || iv.ItemClass == null || !iv.ItemClass.ItemTags.Test_AnySet(WeaponTag)) return null;
        return iv;
    }

    // The held weapon if it can carry affixes (not a legendary)
    ItemValue Weapon()
    {
        ItemValue iv = Held();
        return iv != null && AposAffixes.Kind(iv.ItemClass) != 0 ? iv : null;
    }

    // ===== refresh =====
    void Refresh()
    {
        EntityPlayerLocal p = Player;
        if (p == null) return;
        int eridium = Count(p, AposElites.Eridium), steel = Count(p, Steel);
        if (eridiumLabel != null) eridiumLabel.Text = string.Format(Localization.Get("aposForgeEridium"), eridium, steel);
        for (int i = 0; i < 3; i++)
        {
            Show(pages[i], tab == i);
            Enable(tabButtons[i], tab != i);
        }
        RefreshWeapon(p, eridium, steel);
        RefreshApparel(p, eridium, steel);
        for (int i = 0; i < legendButtons.Length; i++) Enable(legendButtons[i], eridium >= LegendCost);
    }

    void RefreshWeapon(EntityPlayerLocal p, int eridium, int steel)
    {
        ItemValue held = Held();
        ItemValue iv = Weapon(); // affix work only on non-legendary weapons
        List<AposAffixes.Entry> list = iv != null ? AposAffixes.Decompose(iv) : new List<AposAffixes.Entry>();
        int cap = iv != null ? AposAffixes.Cap(iv.Quality) : 0;
        int locked = iv != null ? AposAffixes.LockedType(iv) : -1;
        if (weaponLabel != null)
        {
            weaponLabel.Text = held == null ? Localization.Get("aposForgeNoWeapon")
                : held.ItemClass.GetLocalizedItemName() + "  [FFD24D]" + string.Format(Localization.Get("aposForgeQuality"), AposTiers.Tier(held)) + "[-]";
        }

        for (int i = 0; i < Rows; i++)
        {
            bool hasAffix = i < list.Count;
            bool isLocked = hasAffix && (int)list[i].Affix.Types[0] == locked;
            if (affixLabels[i] != null)
            {
                affixLabels[i].Text = hasAffix ? AffixText(list[i], isLocked)
                    : (iv != null && i < cap ? "[808080]" + Localization.Get("aposForgeEmpty") + "[-]" : "");
            }
            bool canAdd = iv != null && !hasAffix && i == list.Count && i < cap;
            SetText(rerollButtons[i], hasAffix ? Localization.Get("aposForgeReroll") + "  " + RerollCost : Localization.Get("aposForgeAdd") + "  " + AddCost);
            Enable(rerollButtons[i], hasAffix ? !isLocked && eridium >= RerollCost : canAdd && eridium >= AddCost);
            // on the locked row the same button removes the lock for free
            SetText(lockButtons[i], isLocked ? Localization.Get("aposForgeUnlock") : Localization.Get("aposForgeLock") + "  " + LockCost);
            Enable(lockButtons[i], isLocked || (hasAffix && eridium >= LockCost));
        }

        SetText(scrapButton, string.Format(Localization.Get("aposForgeScrap"), iv != null ? ScrapValue(iv) : 0));
        Enable(scrapButton, iv != null && ScrapValue(iv) > 0);

        int tier = held != null ? AposTiers.Tier(held) : 0;
        if (sharpenLabel != null) sharpenLabel.Text = SharpenInfo(held, tier);
        SharpenButtons(sharpenButton, sharpenSafeButton, held, tier, eridium, steel);
    }

    void RefreshApparel(EntityPlayerLocal p, int eridium, int steel)
    {
        int row = 0;
        Equipment eq = p.equipment;
        for (int slot = 0; slot < eq.GetSlotCount() && row < WearRows; slot++)
        {
            ItemValue iv = eq.GetSlotItem(slot);
            if (iv == null || iv.IsEmpty() || iv.ItemClass == null) continue;
            wearSlots[row] = slot;
            int tier = AposTiers.Tier(iv);
            bool can = AposTiers.HasTieredValues(iv.ItemClass);
            if (wearLabels[row] != null)
            {
                wearLabels[row].Text = iv.ItemClass.GetLocalizedItemName() + "  [FFD24D]" + tier + "[-]"
                    + (can ? "" : "  [808080]" + Localization.Get("aposForgeNothing") + "[-]");
            }
            if (can) SharpenButtons(wearButtons[row], wearSafeButtons[row], iv, tier, eridium, steel);
            else
            {
                SetText(wearButtons[row], "-");
                SetText(wearSafeButtons[row], "-");
                Enable(wearButtons[row], false);
                Enable(wearSafeButtons[row], false);
            }
            Show(wearButtons[row], true);
            Show(wearSafeButtons[row], true);
            row++;
        }
        for (; row < WearRows; row++)
        {
            wearSlots[row] = -1;
            if (wearLabels[row] != null) wearLabels[row].Text = row == 0 ? Localization.Get("aposForgeNoApparel") : "";
            Show(wearButtons[row], false);
            Show(wearSafeButtons[row], false);
        }
    }

    // "Sharpen to 8: 40 Eridium + 40 steel, 50%" (or why not)
    static string SharpenInfo(ItemValue iv, int tier)
    {
        if (iv == null) return "";
        if (tier >= AposTiers.MaxTier) return "[FFD24D]" + Localization.Get("aposForgeSharpenMax") + "[-]";
        int target = tier + 1;
        if (target <= 6) return string.Format(Localization.Get("aposForgeSharpenSure"), target);
        return string.Format(Localization.Get("aposForgeSharpenInfo"), target, Mathf.RoundToInt(ChanceFor(target) * 100f))
            + "  [FF4040]" + Localization.Get("aposForgeBreakInfo") + "[-]";
    }

    void SharpenButtons(XUiC_SimpleButton normal, XUiC_SimpleButton safe, ItemValue iv, int tier, int eridium, int steel)
    {
        bool can = iv != null && tier < AposTiers.MaxTier;
        int cost = can ? CostFor(tier + 1) : 0;
        bool risky = can && tier + 1 >= 7; // Safe only matters where a failure is possible
        SetText(normal, can ? Localization.Get("aposForgeSharpen") + "  " + cost + "/" + cost : Localization.Get("aposForgeSharpen"));
        SetText(safe, risky ? Localization.Get("aposForgeSharpenSafe") + "  " + cost * SafeMultiplier + "/" + cost * SafeMultiplier : Localization.Get("aposForgeSharpenSafe"));
        Enable(normal, can && eridium >= cost && steel >= cost);
        Enable(safe, risky && eridium >= cost * SafeMultiplier && steel >= cost * SafeMultiplier);
    }

    static string AffixText(AposAffixes.Entry e, bool isLocked)
    {
        string name = Localization.Get("aposAffix" + e.Affix.Key);
        string value = e.Affix.Key == "Twin" ? Localization.Get("aposAffixTwinDesc")
            : (e.Value > 0 ? "+" : "") + Mathf.RoundToInt(e.Value * 100f) + "%";
        return (isLocked ? "[BE5AFF]" : "[FFD24D]") + name + "[-] " + value;
    }

    // Scrapping in the forge: what normal scrapping would give in Eridium, at least 2 for a weapon with no affixes
    static int ScrapValue(ItemValue iv)
    {
        return Math.Max(2, AposElites.ScrapEridium(iv));
    }

    // ===== weapon actions: change a copy of the held weapon, then put it back into the toolbelt slot =====
    void Reroll(int row)
    {
        ItemValue iv = Weapon();
        if (iv == null) return;
        int cost = row < AposAffixes.Decompose(iv).Count ? RerollCost : AddCost;
        Modify(cost, w => AposAffixes.RerollAt(w, row), "aposForgeDoneReroll");
    }

    void Lock(int row)
    {
        ItemValue iv = Weapon();
        if (iv == null) return;
        if (AposAffixes.IsLockedAt(iv, row)) Modify(0, AposAffixes.Unlock, "aposForgeDoneUnlock");
        else Modify(LockCost, w => AposAffixes.LockAt(w, row), "aposForgeDoneLock");
    }

    void Modify(int cost, Func<ItemValue, bool> change, string doneKey)
    {
        EntityPlayerLocal p = Player;
        ItemValue held = Weapon();
        if (p == null || held == null || Count(p, AposElites.Eridium) < cost) return;
        ItemValue copy = held.Clone();
        if (!change(copy)) return;
        Take(p, AposElites.Eridium, cost);
        PutBack(p, -1, copy);
        GameManager.ShowTooltip(p, Localization.Get(doneKey));
    }

    void Scrap()
    {
        EntityPlayerLocal p = Player;
        ItemValue held = Weapon();
        if (p == null || held == null) return;
        int value = ScrapValue(held);
        PutBack(p, -1, null); // Scrap is for affix weapons only (Weapon()), legendaries are scrapped normally
        AposElites.GiveEridium(value);
        GameManager.ShowTooltip(p, string.Format(Localization.Get("aposForgeDoneScrap"), value));
    }

    void BuyLegend(int index)
    {
        EntityPlayerLocal p = Player;
        if (p == null || Count(p, AposElites.Eridium) < LegendCost) return;
        ItemValue iv = ItemClass.GetItem(AposRewards.Legendaries[index]);
        if (iv == null || iv.type == 0) return;
        Take(p, AposElites.Eridium, LegendCost);
        ItemStack legend = new ItemStack(new ItemValue(iv.type, 6, 6, true, null, 1f), 1);
        if (!p.bag.AddItem(legend) && !p.inventory.AddItem(legend))
        {
            GameManager.Instance.ItemDropServer(legend, p.GetPosition() + Vector3.up, Vector3.zero, -1, 600f, false);
        }
        GameManager.ShowTooltip(p, Localization.Get("aposForgeDoneLegend"));
    }

    // ===== sharpening: pay, 2.5 s of suspense, then the roll =====
    ItemValue ItemIn(EntityPlayerLocal p, int slot)
    {
        if (slot < 0) return Held();
        if (slot >= p.equipment.GetSlotCount()) return null;
        ItemValue iv = p.equipment.GetSlotItem(slot);
        return iv == null || iv.IsEmpty() ? null : iv;
    }

    // Replaces the item in hand (slot -1) or in an equipment slot; null removes it
    static void PutBack(EntityPlayerLocal p, int slot, ItemValue iv)
    {
        if (slot < 0)
        {
            Inventory inv = p.inventory;
            inv.SetItem(inv.holdingItemIdx, iv != null ? new ItemStack(iv, inv.holdingCount) : ItemStack.Empty.Clone());
        }
        else
        {
            p.equipment.SetSlotItem(slot, iv ?? ItemValue.None.Clone());
        }
    }

    void StartSharpen(int slot, bool safe)
    {
        EntityPlayerLocal p = Player;
        ItemValue iv = p != null ? ItemIn(p, slot) : null;
        if (iv == null) return;
        int tier = AposTiers.Tier(iv);
        if (tier >= AposTiers.MaxTier) return;
        if (tier + 1 <= 6) safe = false;
        int cost = CostFor(tier + 1) * (safe ? SafeMultiplier : 1);
        if (Count(p, AposElites.Eridium) < cost || Count(p, Steel) < cost) return;

        Take(p, AposElites.Eridium, cost);
        Take(p, Steel, cost);
        ItemValue copy = iv.Clone();
        AposTiers.AddSpent(copy, cost);
        PutBack(p, slot, copy);

        busy = true;
        busySlot = slot;
        busySafe = safe;
        busyItem = copy;
        busyStart = Time.time;
        nextHit = Time.time;
        resultUntil = 0f;
    }

    void TickSharpen()
    {
        EntityPlayerLocal p = Player;
        float t = (Time.time - busyStart) / SharpenTime;
        if (Time.time >= nextHit)
        {
            nextHit = Time.time + 0.5f;
            if (p != null) Audio.Manager.PlayInsidePlayerHead("metalhitmetal", p.entityId);
        }
        if (t < 1f)
        {
            // "Sharpening  ■■■■■□□□□□" with a pulsing colour
            int filled = Mathf.Clamp(Mathf.FloorToInt(t * 10f), 0, 10);
            string bar = new string('|', filled * 2) + "[505050]" + new string('|', (10 - filled) * 2) + "[-]";
            string pulse = Mathf.Sin(Time.time * 12f) > 0f ? "[FFD24D]" : "[FF9900]";
            if (progressLabel != null) progressLabel.Text = pulse + Localization.Get("aposForgeSharpening") + "[-]  " + bar;
            return;
        }

        busy = false;
        if (p == null) return;
        ItemValue now = ItemIn(p, busySlot);
        // the item left its place during the attempt (swapped weapon): no roll, the resources are spent
        if (now == null || now.type != busyItem.type) return;

        int tier = AposTiers.Tier(now);
        int target = tier + 1;
        ItemValue copy = now.Clone();
        string text;
        if (UnityEngine.Random.value < ChanceFor(target))
        {
            Raise(copy, target);
            PutBack(p, busySlot, copy);
            Audio.Manager.PlayInsidePlayerHead("levelup", p.entityId);
            text = "[40FF60]" + string.Format(Localization.Get("aposForgeSharpenOk"), target) + "[-]";
        }
        else if (!busySafe)
        {
            // normal failure: the item breaks, 30% of its scrap value comes back
            int back = Mathf.RoundToInt(AposElites.ScrapEridium(now) * 0.3f);
            PutBack(p, busySlot, null);
            AposElites.GiveEridium(back);
            Audio.Manager.PlayInsidePlayerHead("itembreak", p.entityId);
            text = "[FF2020]" + string.Format(Localization.Get("aposForgeSharpenBroke"), back) + "[-]";
        }
        else
        {
            // safe failure: one tier down, never below 6
            int down = Math.Max(6, tier - 1);
            AposTiers.SetTier(copy, down);
            PutBack(p, busySlot, copy);
            Audio.Manager.PlayInsidePlayerHead("lockpick_fail", p.entityId);
            text = "[FF6040]" + (down == tier ? Localization.Get("aposForgeSharpenFailSafe")
                : string.Format(Localization.Get("aposForgeSharpenFail"), down)) + "[-]";
        }
        if (progressLabel != null) progressLabel.Text = text;
        resultUntil = Time.time + 4f;
        Refresh();
    }

    // Quality up to 6 is the real item quality (affix values grow with it), 7..10 is the forge tier
    static void Raise(ItemValue iv, int target)
    {
        if (target <= 6)
        {
            if (AposAffixes.Kind(iv.ItemClass) != 0) AposAffixes.Temper(iv);
            else iv.Quality = (ushort)target;
        }
        else AposTiers.SetTier(iv, target);
    }

    // ===== resources from the backpack + toolbelt =====
    static int Count(EntityPlayerLocal p, string item)
    {
        ItemValue e = ItemClass.GetItem(item);
        return p.bag.GetItemCount(e) + p.inventory.GetItemCount(e);
    }

    static void Take(EntityPlayerLocal p, string item, int count)
    {
        if (count <= 0) return;
        ItemValue e = ItemClass.GetItem(item);
        int fromBag = Math.Min(count, p.bag.GetItemCount(e));
        if (fromBag > 0) p.bag.DecItem(e, fromBag);
        if (count - fromBag > 0) p.inventory.DecItem(e, count - fromBag);
    }
}

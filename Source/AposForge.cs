// Eridium Forge: a workstation block (blocks.xml, Class "AposEridiumForge, AposMod" -> BlockAposEridiumForge) that
// opens the window group "aposForge" (Config/XUi_InGame/xui.xml + windows.xml, controller "AposForge, AposMod" ->
// XUiC_AposForge). It works on the weapon in your hands:
// - per affix row: Reroll (10 Eridium) / Lock (25, the locked row unlocks for free); an empty row: Add (20)
// - Temper: +1 quality (40), Scrap: the weapon becomes Eridium
// - 10 legendaries for 300 each, quality 6
// Eridium is taken from the backpack and the toolbelt.
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
    public const int RerollCost = 10, AddCost = 20, LockCost = 25, TemperCost = 40, LegendCost = 300;
    const int Rows = 3;

    XUiV_Label weaponLabel, eridiumLabel;
    readonly XUiV_Label[] affixLabels = new XUiV_Label[Rows];
    readonly XUiC_SimpleButton[] rerollButtons = new XUiC_SimpleButton[Rows];
    readonly XUiC_SimpleButton[] lockButtons = new XUiC_SimpleButton[Rows];
    XUiC_SimpleButton temperButton, scrapButton;
    readonly XUiC_SimpleButton[] legendButtons = new XUiC_SimpleButton[AposRewards.Legendaries.Length];
    float nextRefresh;

    public override void Init()
    {
        base.Init();
        weaponLabel = Label(this, "lblWeapon");
        eridiumLabel = Label(this, "lblEridium");
        for (int i = 0; i < Rows; i++)
        {
            int row = i; // C# 5: capture a copy for the lambda
            affixLabels[i] = Label(this, "lblAffix" + i);
            rerollButtons[i] = Button("btnReroll" + i, () => Reroll(row));
            lockButtons[i] = Button("btnLock" + i, () => Lock(row));
        }
        temperButton = Button("btnTemper", Temper);
        scrapButton = Button("btnScrap", Scrap);
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
        b.OnPressed += (_sender, _mouseButton) => { onPress(); Refresh(); };
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

    static void Enable(XUiC_SimpleButton b, bool on)
    {
        if (b != null) b.Enabled = on;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        Refresh();
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + 0.3f; // the held weapon or the Eridium count may change while the window is open
        Refresh();
    }

    EntityPlayerLocal Player
    {
        get { return xui != null && xui.playerUI != null ? xui.playerUI.entityPlayer : null; }
    }

    // The weapon in hand, or null when it cannot carry affixes (tools, legendaries, bare hands)
    ItemValue Weapon()
    {
        EntityPlayerLocal p = Player;
        if (p == null) return null;
        ItemValue iv = p.inventory.holdingItemItemValue;
        if (iv == null || iv.IsEmpty() || AposAffixes.Kind(iv.ItemClass) == 0) return null;
        return iv;
    }

    void Refresh()
    {
        EntityPlayerLocal p = Player;
        if (p == null) return;
        int eridium = EridiumCount(p);
        if (eridiumLabel != null) eridiumLabel.Text = string.Format(Localization.Get("aposForgeEridium"), eridium);

        ItemValue iv = Weapon();
        List<AposAffixes.Entry> list = iv != null ? AposAffixes.Decompose(iv) : new List<AposAffixes.Entry>();
        int cap = iv != null ? AposAffixes.Cap(iv.Quality) : 0;
        int locked = iv != null ? AposAffixes.LockedType(iv) : -1;
        if (weaponLabel != null)
        {
            weaponLabel.Text = iv == null ? Localization.Get("aposForgeNoWeapon")
                : iv.ItemClass.GetLocalizedItemName() + "  [FFD24D]" + string.Format(Localization.Get("aposForgeQuality"), iv.Quality) + "[-]";
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

        SetText(temperButton, Localization.Get("aposForgeTemper") + "  " + TemperCost);
        Enable(temperButton, iv != null && iv.Quality < 6 && eridium >= TemperCost);
        SetText(scrapButton, string.Format(Localization.Get("aposForgeScrap"), iv != null ? ScrapValue(iv) : 0));
        Enable(scrapButton, iv != null && ScrapValue(iv) > 0);
        for (int i = 0; i < legendButtons.Length; i++) Enable(legendButtons[i], eridium >= LegendCost);
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

    // ===== actions: change a copy of the held weapon, then put it back into the toolbelt slot =====
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

    void Temper()
    {
        Modify(TemperCost, AposAffixes.Temper, "aposForgeDoneTemper");
    }

    void Modify(int cost, Func<ItemValue, bool> change, string doneKey)
    {
        EntityPlayerLocal p = Player;
        ItemValue held = Weapon();
        if (p == null || held == null || EridiumCount(p) < cost) return;
        ItemValue copy = held.Clone();
        if (!change(copy)) return;
        TakeEridium(p, cost);
        Inventory inv = p.inventory;
        inv.SetItem(inv.holdingItemIdx, new ItemStack(copy, inv.holdingCount));
        GameManager.ShowTooltip(p, Localization.Get(doneKey));
    }

    void Scrap()
    {
        EntityPlayerLocal p = Player;
        ItemValue held = Weapon();
        if (p == null || held == null) return;
        int value = ScrapValue(held);
        Inventory inv = p.inventory;
        inv.SetItem(inv.holdingItemIdx, ItemStack.Empty.Clone());
        AposElites.GiveEridium(value);
        GameManager.ShowTooltip(p, string.Format(Localization.Get("aposForgeDoneScrap"), value));
    }

    void BuyLegend(int index)
    {
        EntityPlayerLocal p = Player;
        if (p == null || EridiumCount(p) < LegendCost) return;
        ItemValue iv = ItemClass.GetItem(AposRewards.Legendaries[index]);
        if (iv == null || iv.type == 0) return;
        TakeEridium(p, LegendCost);
        ItemStack legend = new ItemStack(new ItemValue(iv.type, 6, 6, true, null, 1f), 1);
        if (!p.bag.AddItem(legend) && !p.inventory.AddItem(legend))
        {
            GameManager.Instance.ItemDropServer(legend, p.GetPosition() + Vector3.up, Vector3.zero, -1, 600f, false);
        }
        GameManager.ShowTooltip(p, Localization.Get("aposForgeDoneLegend"));
    }

    static ItemValue EridiumValue()
    {
        return ItemClass.GetItem(AposElites.Eridium);
    }

    static int EridiumCount(EntityPlayerLocal p)
    {
        ItemValue e = EridiumValue();
        return p.bag.GetItemCount(e) + p.inventory.GetItemCount(e);
    }

    static void TakeEridium(EntityPlayerLocal p, int count)
    {
        ItemValue e = EridiumValue();
        int fromBag = Math.Min(count, p.bag.GetItemCount(e));
        if (fromBag > 0) p.bag.DecItem(e, fromBag);
        if (count - fromBag > 0) p.inventory.DecItem(e, count - fromBag);
    }
}

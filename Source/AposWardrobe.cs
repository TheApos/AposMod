// Wardrobe: 3 outfits you switch with Alt+1 / Alt+2 / Alt+3 (design: docs/Design-Sets.md, variant B).
// What you wear is the active outfit; the other two are kept in the wardrobe, not in the backpack. Switching takes
// 1.5 s: the worn items go into the active outfit's tab, the chosen outfit is put on. Alt+0 opens the wardrobe window
// (3 tabs: what each outfit holds, put it on, or move its items to the backpack).
// The stored outfits are written to <save game folder>/AposWardrobe.dat after every change (vanilla item format).
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AposWardrobe : MonoBehaviour
{
    public const int Outfits = 3;
    const float SwitchTime = 1.5f;
    const string FileName = "AposWardrobe.dat";
    const int FileVersion = 1;

    // slot index -> item, for the outfits that are not worn right now (the active one is empty: it is on you)
    public static readonly Dictionary<int, ItemValue>[] Stored =
        { new Dictionary<int, ItemValue>(), new Dictionary<int, ItemValue>(), new Dictionary<int, ItemValue>() };
    public static int Active;
    static string file;
    static int switchTo = -1;
    static float switchStart;
    GUIStyle style;

    public static bool Switching { get { return switchTo >= 0; } }

    public static void RequestSwitch(int outfit)
    {
        if (outfit < 0 || outfit >= Outfits || outfit == Active || Switching) return;
        switchTo = outfit;
        switchStart = Time.time;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        if (player == null)
        {
            file = null;
            switchTo = -1;
            return;
        }
        string path = Path.Combine(GameIO.GetSaveGameDir(), FileName);
        if (path != file) Load(path);

        if (Switching)
        {
            if (player.IsDead()) switchTo = -1;
            else if (Time.time - switchStart >= SwitchTime)
            {
                DoSwitch(player, switchTo);
                switchTo = -1;
            }
            return;
        }

        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(player);
        if (ui == null || ui.windowManager.IsInputActive()) return; // typing in chat or the console
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (!alt) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) RequestSwitch(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) RequestSwitch(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) RequestSwitch(2);
        else if (Input.GetKeyDown(KeyCode.Alpha0)) ui.windowManager.Open("aposWardrobe", true);
    }

    // worn items -> the active outfit's tab; the chosen outfit -> equipment
    static void DoSwitch(EntityPlayerLocal player, int target)
    {
        Equipment eq = player.equipment;
        Dictionary<int, ItemValue> worn = new Dictionary<int, ItemValue>();
        for (int i = 0; i < eq.GetSlotCount(); i++)
        {
            ItemValue iv = eq.GetSlotItem(i);
            if (iv != null && !iv.IsEmpty()) worn[i] = iv.Clone();
        }
        Dictionary<int, ItemValue> next = Stored[target];
        for (int i = 0; i < eq.GetSlotCount(); i++)
        {
            ItemValue iv;
            eq.SetSlotItem(i, next.TryGetValue(i, out iv) ? iv.Clone() : ItemValue.None.Clone());
        }
        Stored[Active] = worn;
        Stored[target] = new Dictionary<int, ItemValue>();
        Active = target;
        Save();
        GameManager.ShowTooltip(player, string.Format(Localization.Get("aposWardrobeSwitched"), target + 1));
    }

    // Moves a stored (not worn) outfit's items into the backpack; what does not fit falls to the ground
    public static void TakeOut(EntityPlayerLocal player, int outfit)
    {
        if (outfit == Active || Stored[outfit].Count == 0) return;
        foreach (ItemValue iv in Stored[outfit].Values)
        {
            ItemStack stack = new ItemStack(iv.Clone(), 1);
            if (!player.bag.AddItem(stack))
            {
                GameManager.Instance.ItemDropServer(stack, player.GetPosition() + Vector3.up, Vector3.zero, -1, 600f, false);
            }
        }
        Stored[outfit].Clear();
        Save();
    }

    static void Load(string path)
    {
        file = path;
        Active = 0;
        for (int o = 0; o < Outfits; o++) Stored[o].Clear();
        if (!File.Exists(path)) return;
        try
        {
            using (FileStream fs = File.OpenRead(path))
            {
                PooledBinaryReader br = new PooledBinaryReader();
                br.SetBaseStream(fs);
                int version = br.ReadInt32();
                if (version != FileVersion) throw new Exception("unknown version " + version);
                Active = br.ReadInt32();
                for (int o = 0; o < Outfits; o++)
                {
                    int count = br.ReadInt32();
                    for (int k = 0; k < count; k++)
                    {
                        int slot = br.ReadInt32();
                        ItemValue iv = new ItemValue();
                        iv.Read(br);
                        if (!iv.IsEmpty()) Stored[o][slot] = iv;
                    }
                }
            }
        }
        catch (Exception e)
        {
            // keep the file: renaming it lets the player (or me) recover the items by hand
            Debug.LogError("[AposMod] Wardrobe file could not be read, it was kept as " + path + ".bad: " + e.Message);
            try { File.Copy(path, path + ".bad", true); } catch (Exception) { }
            for (int o = 0; o < Outfits; o++) Stored[o].Clear();
        }
    }

    static void Save()
    {
        if (file == null) return;
        try
        {
            string tmp = file + ".tmp";
            using (FileStream fs = File.Create(tmp))
            {
                PooledBinaryWriter bw = new PooledBinaryWriter();
                bw.SetBaseStream(fs);
                bw.Write(FileVersion);
                bw.Write(Active);
                for (int o = 0; o < Outfits; o++)
                {
                    bw.Write(Stored[o].Count);
                    foreach (KeyValuePair<int, ItemValue> kv in Stored[o])
                    {
                        bw.Write(kv.Key);
                        kv.Value.Write(bw);
                    }
                }
                bw.Flush();
            }
            // replace in one step so a crash mid-write never leaves a half file
            if (File.Exists(file)) File.Delete(file);
            File.Move(tmp, file);
        }
        catch (Exception e)
        {
            Debug.LogError("[AposMod] Wardrobe could not be saved: " + e);
        }
    }

    // "Changing into outfit 2  |||||||" while switching
    void OnGUI()
    {
        if (!Switching || Event.current.type != EventType.Repaint) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 24;
        }
        float t = Mathf.Clamp01((Time.time - switchStart) / SwitchTime);
        string text = string.Format(Localization.Get("aposWardrobeChanging"), switchTo + 1) + "  " + new string('|', Mathf.RoundToInt(t * 20f));
        Rect r = new Rect(Screen.width / 2f - 300f, Screen.height * 0.7f, 600f, 40f);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style);
        GUI.color = new Color(1f, 0.85f, 0.3f);
        GUI.Label(r, text, style);
        GUI.color = old;
    }
}

// Wardrobe window: 3 tabs with the items of each outfit, "Put on" and "Take to backpack"
public class XUiC_AposWardrobe : XUiController
{
    const int Rows = 8;
    readonly XUiC_SimpleButton[] tabs = new XUiC_SimpleButton[AposWardrobe.Outfits];
    readonly XUiV_Label[] rows = new XUiV_Label[Rows];
    XUiV_Label title;
    XUiC_SimpleButton wear, take;
    int tab;
    float nextRefresh;

    public override void Init()
    {
        base.Init();
        title = Label(this, "lblOutfit");
        for (int i = 0; i < AposWardrobe.Outfits; i++)
        {
            int t = i;
            tabs[i] = Button("btnTab" + i, () => tab = t);
            SetText(tabs[i], string.Format(Localization.Get("aposWardrobeTab"), i + 1));
        }
        for (int i = 0; i < Rows; i++) rows[i] = Label(this, "lblItem" + i);
        wear = Button("btnWear", () => AposWardrobe.RequestSwitch(tab));
        take = Button("btnTake", () =>
        {
            EntityPlayerLocal p = xui.playerUI.entityPlayer;
            if (p != null) AposWardrobe.TakeOut(p, tab);
        });
        SetText(wear, Localization.Get("aposWardrobeWear"));
        SetText(take, Localization.Get("aposWardrobeTake"));
    }

    XUiC_SimpleButton Button(string id, Action onPress)
    {
        XUiC_SimpleButton b = GetChildById(id) as XUiC_SimpleButton;
        if (b == null)
        {
            Debug.LogError("[AposMod] Wardrobe window: no button " + id);
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

    public override void OnOpen()
    {
        base.OnOpen();
        tab = AposWardrobe.Active;
        Refresh();
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + 0.3f;
        Refresh();
    }

    void Refresh()
    {
        EntityPlayerLocal p = xui != null && xui.playerUI != null ? xui.playerUI.entityPlayer : null;
        if (p == null) return;
        bool active = tab == AposWardrobe.Active;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] != null) tabs[i].Enabled = i != tab;
        }
        if (title != null)
        {
            title.Text = string.Format(Localization.Get("aposWardrobeTab"), tab + 1) + "  "
                + (active ? "[40FF60]" + Localization.Get("aposWardrobeWorn") + "[-]" : "");
        }

        // the active outfit is what you wear now; the others come from the wardrobe
        List<ItemValue> items = new List<ItemValue>();
        if (active)
        {
            for (int i = 0; i < p.equipment.GetSlotCount(); i++)
            {
                ItemValue iv = p.equipment.GetSlotItem(i);
                if (iv != null && !iv.IsEmpty()) items.Add(iv);
            }
        }
        else items.AddRange(AposWardrobe.Stored[tab].Values);

        for (int i = 0; i < Rows; i++)
        {
            if (rows[i] == null) continue;
            if (i < items.Count) rows[i].Text = items[i].ItemClass.GetLocalizedItemName() + "  [FFD24D]" + AposTiers.Tier(items[i]) + "[-]";
            else rows[i].Text = i == 0 && items.Count == 0 ? "[808080]" + Localization.Get("aposWardrobeEmpty") + "[-]" : "";
        }
        if (wear != null) wear.Enabled = !active && !AposWardrobe.Switching;
        if (take != null) take.Enabled = !active && AposWardrobe.Stored[tab].Count > 0;
    }
}

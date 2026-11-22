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
        string dir = AposSettings.PlayerDataDir();
        if (dir == null) return; // joining: the folder is not known yet
        string path = Path.Combine(dir, FileName);
        if (path != file) Load(path);
        Hint(player);
        UpdateHud(player);

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

    // Only the 4 armor slots belong to an outfit. Clothing under the armor (it only gives cold / heat resistance), the
    // backpack (its size decides how many bag slots you have; a smaller one drops what does not fit) and the biome
    // badges stay on.
    public static readonly EquipmentSlots[] OutfitSlots = { EquipmentSlots.Head, EquipmentSlots.Chest, EquipmentSlots.Hands, EquipmentSlots.Feet };

    public static bool Swaps(int slot)
    {
        return Array.IndexOf(OutfitSlots, (EquipmentSlots)slot) >= 0;
    }

    // what an outfit holds in a slot: the worn item for the active one, the stored item for the others
    public static ItemValue ItemIn(EntityPlayerLocal p, int outfit, EquipmentSlots slot)
    {
        ItemValue iv;
        if (outfit == Active) iv = p.equipment.GetSlotItem((int)slot);
        else Stored[outfit].TryGetValue((int)slot, out iv);
        return iv != null && !iv.IsEmpty() && iv.ItemClass != null ? iv : null;
    }

    // armor value of an item at its quality / forge tier (the item's own tiered passive, mods not counted)
    public static float ItemValueOf(ItemValue iv, PassiveEffects type)
    {
        float sum = 0f;
        if (iv == null || iv.ItemClass == null || iv.ItemClass.Effects == null || iv.ItemClass.Effects.EffectGroups == null) return 0f;
        int tier = AposTiers.Tier(iv);
        foreach (MinEffectGroup g in iv.ItemClass.Effects.EffectGroups)
        {
            if (g.PassiveEffects == null) continue;
            foreach (PassiveEffect pe in g.PassiveEffects)
            {
                if (pe.Type != type || pe.Values == null || pe.Values.Length == 0) continue;
                if (pe.Modifier != PassiveEffect.ValueModifierTypes.base_add) continue;
                if (pe.Levels != null && pe.Levels.Length == pe.Values.Length && pe.Values.Length >= 2) sum += XUiC_AposForge.TierValue(pe, tier);
                else sum += pe.Values[0];
            }
        }
        return sum;
    }

    // worn items -> the active outfit's tab; the chosen outfit -> equipment
    static void DoSwitch(EntityPlayerLocal player, int target)
    {
        Equipment eq = player.equipment;
        Dictionary<int, ItemValue> worn = new Dictionary<int, ItemValue>();
        for (int i = 0; i < eq.GetSlotCount(); i++)
        {
            if (!Swaps(i)) continue;
            ItemValue iv = eq.GetSlotItem(i);
            if (iv != null && !iv.IsEmpty()) worn[i] = iv.Clone();
        }
        Dictionary<int, ItemValue> next = Stored[target];
        for (int i = 0; i < eq.GetSlotCount(); i++)
        {
            if (!Swaps(i)) continue;
            ItemValue iv;
            eq.SetSlotItem(i, next.TryGetValue(i, out iv) ? iv.Clone() : ItemValue.None.Clone());
        }
        // an outfit stored by 1.6.15 or older may hold a backpack or a badge: it goes to the bag (or the ground)
        foreach (KeyValuePair<int, ItemValue> kv in next)
        {
            if (Swaps(kv.Key)) continue;
            ItemStack stack = new ItemStack(kv.Value.Clone(), 1);
            if (!player.bag.AddItem(stack))
            {
                GameManager.Instance.ItemDropServer(stack, player.GetPosition() + Vector3.up, Vector3.zero, -1, 600f, false);
            }
        }
        Stored[Active] = worn;
        Stored[target] = new Dictionary<int, ItemValue>();
        Active = target;
        Save();
        GameManager.ShowTooltip(player, string.Format(Localization.Get("aposWardrobeSwitched"), target + 1));
    }

    // ===== reminders: a one-time tip, the active outfit next to the toolbelt =====
    const string TipCVar = "aposWardrobeTip";
    static float nextHint;
    static readonly string[] Roman = { "I", "II", "III" };
    static XUiV_Label hudLabel;
    static LocalPlayerUI hudUi;
    static string hudText;

    // once per character (a cvar, saved with the player): when a full armor set is on, or the bag holds armor for a slot
    // that is already worn
    static void Hint(EntityPlayerLocal p)
    {
        if (Time.time < nextHint) return;
        nextHint = Time.time + 2f;
        if (p.IsDead() || p.Buffs.GetCustomVar(TipCVar) >= 1f || !WantsHint(p)) return;
        p.Buffs.SetCustomVar(TipCVar, 1f);
        GameManager.ShowTooltip(p, Localization.Get("aposWardrobeTip"), false, false, 12f);
    }

    static bool WantsHint(EntityPlayerLocal p)
    {
        foreach (BuffValue b in p.Buffs.ActiveBuffs)
        {
            string n = b.BuffName;
            if (n != null && n.StartsWith("buff") && n.EndsWith("SetBonus")) return true;
        }
        bool spare = false;
        p.bag.PerformActionOnSlots(s =>
        {
            if (spare || s == null || s.IsEmpty()) return;
            ItemClassArmor a = s.itemValue.ItemClass as ItemClassArmor;
            if (a == null || a.IsCosmetic || !Swaps((int)a.EquipSlot)) return;
            ItemValue worn = p.equipment.GetSlotItem((int)a.EquipSlot);
            if (worn != null && !worn.IsEmpty()) spare = true;
        });
        return spare;
    }

    // "II" left of the toolbelt; grey while the other outfits are empty
    static void UpdateHud(EntityPlayerLocal p)
    {
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(p);
        if (ui == null || ui.xui == null) return;
        if (ui != hudUi || hudLabel == null)
        {
            hudUi = ui;
            hudLabel = null;
            hudText = null;
            XUiV_Window w = ui.xui.GetWindow("windowToolbelt");
            XUiController c = w != null && w.Controller != null ? w.Controller.GetChildById("aposOutfitLabel") : null;
            hudLabel = c != null ? c.ViewComponent as XUiV_Label : null;
            if (hudLabel == null) return;
        }
        bool used = false;
        for (int o = 0; o < Outfits; o++) if (o != Active && Stored[o].Count > 0) used = true;
        string text = (used ? "[FFFFFF]" : "[909090]") + Roman[Active] + "[-]";
        if (text == hudText) return;
        hudText = text;
        hudLabel.Text = text;
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

// "Wardrobe (Alt+0)" button under the armor and clothing slots of the character window (Config/XUi_InGame/windows.xml)
[HarmonyLib.HarmonyPatch(typeof(XUiC_CharacterFrameWindow), "Init")]
public static class AposWardrobeCharacterPatch
{
    static void Postfix(XUiC_CharacterFrameWindow __instance)
    {
        foreach (string id in new[] { "aposWardrobeBtnArmor", "aposWardrobeBtnClothing" })
        {
            XUiC_SimpleButton b = __instance.GetChildById(id) as XUiC_SimpleButton;
            if (b == null) continue;
            XUiController label = b.GetChildById("btnLabel");
            XUiV_Label l = label != null ? label.ViewComponent as XUiV_Label : null;
            if (l != null) l.Text = Localization.Get("aposWardrobeButton");
            b.OnPressed += (_sender, _mouseButton) => __instance.xui.playerUI.windowManager.Open("aposWardrobe", true);
        }
    }
}

// Wardrobe window: 3 tabs with the items of each outfit, "Put on" and "Take to backpack"
public class XUiC_AposWardrobe : XUiController
{
    // "Ranger set 4/4 - bonus active: ..." for the armor group with the most pieces (a group with a vanilla set bonus)
    static string SetText(Dictionary<string, int> groups)
    {
        string best = null;
        int count = 0;
        BuffClass bonus = null;
        foreach (KeyValuePair<string, int> kv in groups)
        {
            if (!kv.Key.StartsWith("group") || kv.Value <= count) continue;
            BuffClass b = BuffManager.GetBuff("buff" + kv.Key.Substring(5) + "SetBonus");
            if (b == null) continue;
            best = kv.Key;
            count = kv.Value;
            bonus = b;
        }
        if (bonus == null) return "[808080]" + Localization.Get("aposWardrobeNoSet") + "[-]";
        string name = !string.IsNullOrEmpty(bonus.LocalizedName) ? bonus.LocalizedName : best.Substring(5);
        if (count < 4)
        {
            return string.Format(Localization.Get("aposWardrobeSetPartial"), name, count, 4 - count);
        }
        string desc = !string.IsNullOrEmpty(bonus.DescriptionKey) ? Localization.Get(bonus.DescriptionKey) : bonus.Description;
        if (desc != null) desc = desc.Replace("\n", " ").Replace("\n", " ");
        return string.Format(Localization.Get("aposWardrobeSetFull"), name) + (string.IsNullOrEmpty(desc) ? "" : " [C0C0C0]" + desc + "[-]");
    }

    const int Rows = 4;
    readonly XUiC_SimpleButton[] tabs = new XUiC_SimpleButton[AposWardrobe.Outfits];
    readonly XUiV_Label[] rows = new XUiV_Label[Rows];
    readonly XUiV_Sprite[] icons = new XUiV_Sprite[Rows];
    static readonly string[] SlotKeys = { "aposWardrobeSlotHead", "aposWardrobeSlotChest", "aposWardrobeSlotHands", "aposWardrobeSlotFeet" };
    XUiV_Label title, armorLabel, setLabel;
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
        for (int i = 0; i < Rows; i++)
        {
            rows[i] = Label(this, "lblItem" + i);
            XUiController c = GetChildById("icoItem" + i);
            icons[i] = c != null ? c.ViewComponent as XUiV_Sprite : null;
        }
        armorLabel = Label(this, "lblArmor");
        setLabel = Label(this, "lblSet");
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

        // one row per armor slot: icon, slot, item, quality / forge tier, its armor
        float armor = 0f, elemental = 0f;
        Dictionary<string, int> groups = new Dictionary<string, int>();
        for (int i = 0; i < Rows; i++)
        {
            ItemValue iv = AposWardrobe.ItemIn(p, tab, AposWardrobe.OutfitSlots[i]);
            string slot = "[A0A0A0]" + Localization.Get(SlotKeys[i]) + ":[-]  ";
            if (iv == null)
            {
                if (rows[i] != null) rows[i].Text = slot + "[707070]" + Localization.Get("aposWardrobeEmpty") + "[-]";
                if (icons[i] != null) icons[i].IsVisible = false;
                continue;
            }
            float a = AposWardrobe.ItemValueOf(iv, PassiveEffects.PhysicalDamageResist);
            armor += a;
            elemental += AposWardrobe.ItemValueOf(iv, PassiveEffects.ElementalDamageResist);
            if (rows[i] != null)
            {
                rows[i].Text = slot + iv.ItemClass.GetLocalizedItemName() + "  [FFD24D]" + AposTiers.Tier(iv) + "[-]"
                    + (a > 0f ? "   [80C8FF]" + string.Format(Localization.Get("aposWardrobeItemArmor"), Mathf.RoundToInt(a)) + "[-]" : "");
            }
            if (icons[i] != null)
            {
                icons[i].IsVisible = true;
                icons[i].SpriteName = iv.ItemClass.GetIconName();
                icons[i].Color = iv.ItemClass.GetIconTint(iv);
            }
            ItemClassArmor ac = iv.ItemClass as ItemClassArmor;
            if (ac != null && ac.ArmorGroup != null)
            {
                foreach (string g in ac.ArmorGroup)
                {
                    int n;
                    groups.TryGetValue(g, out n);
                    groups[g] = n + 1;
                }
            }
        }
        if (armorLabel != null)
        {
            armorLabel.Text = string.Format(Localization.Get("aposWardrobeArmor"), Mathf.RoundToInt(armor), Mathf.RoundToInt(elemental));
        }
        if (setLabel != null) setLabel.Text = SetText(groups);
        if (wear != null) wear.Enabled = !active && !AposWardrobe.Switching;
        if (take != null) take.Enabled = !active && AposWardrobe.Stored[tab].Count > 0;
    }
}

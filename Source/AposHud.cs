// Combat HUD (docs/Design-Mastery2.md 5к): counters of the held weapon's class.
// - attack (streaks, stacks, charges of a strike) next to the crosshair: window windowAposHud (Center anchor)
// - defence (shields, damage reduction, regeneration): a column at the left edge above the health bars, window
//   windowAposHudDef (BottomLeft anchor). Dim while idle; Flash(icon) (or a widget turning "full") lights it up
//   for a second, then it fades back.
// Both windows live in the vanilla "toolbelt" window group, so they show and hide with the HUD.
// Every frame AposMastery.Update calls Begin(), then the held class's file (AposPistol.Tick, ...) adds its widgets.
// Settings: aposhud on|off, size N (0.6-1.6), pos below|right|left (Settings.xml <Hud>).
using System.Collections.Generic;
using UnityEngine;

public static class AposHud
{
    public class Widget
    {
        public string Icon;   // vanilla UI sprite, e.g. ui_game_symbol_head_shot
        public string Text;
        public Color Color;
        public bool Glow;     // full stacks / mode active: brighter
    }

    // icon sprite -> time it last triggered (shown bright, then fading)
    static readonly Dictionary<string, float> flashAt = new Dictionary<string, float>();
    public static void Flash(string icon) { flashAt["ui_game_symbol_" + icon] = Time.time; }
    public static void FlashSprite(string sprite) { flashAt[sprite] = Time.time; }
    // 1 right after the trigger, held 0.8 s, then fading to 0 over 1 s
    public static float FlashLevel(string sprite)
    {
        float at;
        if (!flashAt.TryGetValue(sprite, out at)) return 0f;
        return Mathf.Clamp01(1f - (Time.time - at - 0.8f));
    }

    public static readonly List<Widget> Attack = new List<Widget>();
    public static readonly List<Widget> Defense = new List<Widget>();

    // a class may move the attack row for this frame (the bow: to the right end of the vanilla draw bar)
    public static Vector2i? AttackAt;
    public static readonly Vector2i BesideDrawBar = new Vector2i(345, -147);

    public static void Begin()
    {
        Attack.Clear();
        Defense.Clear();
        AttackAt = null;
    }

    public static void Add(bool defense, string icon, string text, Color color, bool glow)
    {
        (defense ? Defense : Attack).Add(new Widget { Icon = "ui_game_symbol_" + icon, Text = text, Color = color, Glow = glow });
    }
}

// one row of up to 4 widget slots (atk0..atk3 in windows.xml)
public class XUiC_AposHud : XUiController
{
    const int Slots = 4;
    const int SlotW = 78;
    protected virtual bool IsDefense { get { return false; } }

    readonly XUiController[] slot = new XUiController[Slots];
    readonly XUiV_Sprite[] icon = new XUiV_Sprite[Slots], glow = new XUiV_Sprite[Slots];
    readonly XUiV_Label[] text = new XUiV_Label[Slots];
    readonly Dictionary<string, bool> wasGlow = new Dictionary<string, bool>();
    float appliedScale = -1f;
    string appliedPos;

    public override void Init()
    {
        base.Init();
        for (int i = 0; i < Slots; i++)
        {
            slot[i] = GetChildById("slot" + i);
            if (slot[i] == null) continue;
            XUiController c = slot[i].GetChildById("icon");
            icon[i] = c != null ? c.ViewComponent as XUiV_Sprite : null;
            c = slot[i].GetChildById("glow");
            glow[i] = c != null ? c.ViewComponent as XUiV_Sprite : null;
            c = slot[i].GetChildById("text");
            text[i] = c != null ? c.ViewComponent as XUiV_Label : null;
        }
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        List<AposHud.Widget> list = IsDefense ? AposHud.Defense : AposHud.Attack;
        int n = AposSettings.HudEnabled ? Mathf.Min(Slots, list.Count) : 0;
        Layout(n);
        for (int i = 0; i < Slots; i++)
        {
            if (slot[i] == null) continue;
            bool on = i < n;
            if (slot[i].ViewComponent.IsVisible != on) slot[i].ViewComponent.IsVisible = on;
            if (!on) continue;
            AposHud.Widget w = list[i];
            // a defence widget that just became full / ready flashes like a trigger
            bool was;
            if (IsDefense && w.Glow && (!wasGlow.TryGetValue(w.Icon, out was) || !was)) AposHud.FlashSprite(w.Icon);
            wasGlow[w.Icon] = w.Glow;
            // dim by default (no frames): idle 55-65%, full / active 85%, a trigger 100% fading back
            float flash = AposHud.FlashLevel(w.Icon);
            float k = Mathf.Max(w.Glow ? 0.85f : (IsDefense ? 0.55f : 0.65f), flash);
            Color c = new Color(w.Color.r * k + (1f - k) * 0.35f, w.Color.g * k + (1f - k) * 0.35f, w.Color.b * k + (1f - k) * 0.35f, 0.5f + 0.5f * k);
            if (icon[i] != null)
            {
                if (icon[i].SpriteName != w.Icon) icon[i].SpriteName = w.Icon;
                if (icon[i].Color != c) icon[i].Color = c;
                if (icon[i].UiTransform != null) icon[i].UiTransform.localScale = Vector3.one * (1f + 0.25f * flash);
            }
            if (text[i] != null)
            {
                if (text[i].Text != w.Text) text[i].Text = w.Text;
                Color tc = new Color(1f, 1f, 1f, 0.55f + 0.45f * k);
                if (text[i].Color != tc) text[i].Color = tc;
            }
            if (glow[i] != null && glow[i].IsVisible) glow[i].IsVisible = false;
        }
    }

    // attack row: centred under the crosshair, or a column to the right / left of it; defence: a column going up
    int laidOut = -1;
    void Layout(int n)
    {
        float scale = AposSettings.HudScale;
        if (scale != appliedScale && ViewComponent != null && ViewComponent.UiTransform != null)
        {
            appliedScale = scale;
            ViewComponent.UiTransform.localScale = Vector3.one * scale;
        }
        string pos = IsDefense ? "column" : AposHud.AttackAt.HasValue ? "at" + AposHud.AttackAt.Value : AposSettings.HudPos;
        if (n == laidOut && pos == appliedPos) return;
        laidOut = n;
        appliedPos = pos;
        for (int i = 0; i < n; i++)
        {
            if (slot[i] == null) continue;
            Vector2i p;
            if (pos == "right") p = new Vector2i(40, 30 - i * 32);
            else if (pos == "left") p = new Vector2i(-40 - SlotW, 30 - i * 32);
            else if (pos == "column") p = new Vector2i(0, i * 32);
            else if (pos.StartsWith("at")) p = new Vector2i(AposHud.AttackAt.Value.x + i * SlotW, AposHud.AttackAt.Value.y);
            else p = new Vector2i(-n * SlotW / 2 + i * SlotW, -34);
            slot[i].ViewComponent.Position = p;
        }
    }
}

public class XUiC_AposHudDef : XUiC_AposHud
{
    protected override bool IsDefense { get { return true; } }
}

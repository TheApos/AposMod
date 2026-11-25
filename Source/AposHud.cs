// Combat HUD (docs/Design-Mastery2.md 5к): counters of the held weapon's class.
// - attack (streaks, stacks, charges of a strike) next to the crosshair: window windowAposHud (Center anchor)
// - defence (shields, damage reduction, regeneration) above the health bar: window windowAposHudDef (BottomLeft anchor)
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
        public bool Glow;     // full stacks / mode active: a coloured frame
    }

    public static readonly List<Widget> Attack = new List<Widget>();
    public static readonly List<Widget> Defense = new List<Widget>();

    public static void Begin()
    {
        Attack.Clear();
        Defense.Clear();
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
            if (icon[i] != null)
            {
                if (icon[i].SpriteName != w.Icon) icon[i].SpriteName = w.Icon;
                if (icon[i].Color != w.Color) icon[i].Color = w.Color;
            }
            if (text[i] != null && text[i].Text != w.Text) text[i].Text = w.Text;
            if (glow[i] != null)
            {
                if (glow[i].IsVisible != w.Glow) glow[i].IsVisible = w.Glow;
                if (w.Glow && glow[i].Color != w.Color) glow[i].Color = w.Color;
            }
        }
    }

    // attack row: centred under the crosshair, or a column to the right / left of it; defence row: left to right
    int laidOut = -1;
    void Layout(int n)
    {
        float scale = AposSettings.HudScale;
        if (scale != appliedScale && ViewComponent != null && ViewComponent.UiTransform != null)
        {
            appliedScale = scale;
            ViewComponent.UiTransform.localScale = Vector3.one * scale;
        }
        string pos = IsDefense ? "row" : AposSettings.HudPos;
        if (n == laidOut && pos == appliedPos) return;
        laidOut = n;
        appliedPos = pos;
        for (int i = 0; i < n; i++)
        {
            if (slot[i] == null) continue;
            Vector2i p;
            if (pos == "right") p = new Vector2i(40, 30 - i * 32);
            else if (pos == "left") p = new Vector2i(-40 - SlotW, 30 - i * 32);
            else if (pos == "row") p = new Vector2i(i * SlotW, 0);
            else p = new Vector2i(-n * SlotW / 2 + i * SlotW, -34);
            slot[i].ViewComponent.Position = p;
        }
    }
}

public class XUiC_AposHudDef : XUiC_AposHud
{
    protected override bool IsDefense { get { return true; } }
}

// Status row above zombies (drawn from AposMastery.OnGUI): the mod's own hidden debuffs on a zombie, as a vanilla UI
// icon (taken from the game's UI atlas, XUi.GetAtlasByName) plus the number that matters:
// Mark (+N% your damage), slowed (-N% speed: Shackles / Hunter's Wound / Suppression, the strongest), Wound stacks,
// Shattered, Armor Break, Concussed, Corroded, Shocked. Only within 30 m and in line of sight; settings: aposhud off hides it.
using System.Collections.Generic;
using UnityEngine;

public static class AposStatus
{
    const float Range = 30f;

    class Tag { public string Sprite, Text; public Color Color; }
    static readonly List<Tag> tags = new List<Tag>();
    static GUIStyle style;

    // ===== icons from the game's UI atlas =====
    class Icon { public Texture Tex; public Rect Uv; }
    static readonly Dictionary<string, Icon> icons = new Dictionary<string, Icon>();

    static Icon GetIcon(EntityPlayerLocal p, string sprite)
    {
        Icon ic;
        if (icons.TryGetValue(sprite, out ic)) return ic;
        ic = null;
        try
        {
            LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(p);
            INGUIAtlas atlas = ui != null && ui.xui != null ? ui.xui.GetAtlasByName("UIAtlas", sprite) : null;
            UISpriteData d = atlas != null ? atlas.GetSprite(sprite) : null;
            Texture t = atlas != null ? atlas.texture : null;
            if (d != null && t != null)
                ic = new Icon { Tex = t, Uv = new Rect((float)d.x / t.width, 1f - (float)(d.y + d.height) / t.height, (float)d.width / t.width, (float)d.height / t.height) };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AposMod] Status icon " + sprite + ": " + e.Message);
        }
        icons[sprite] = ic;
        return ic;
    }

    public static void Clear() { icons.Clear(); }

    static void Add(string icon, string text, Color c) { tags.Add(new Tag { Sprite = "ui_game_symbol_" + icon, Text = text, Color = c }); }

    static string Pct(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }

    static void Collect(EntityPlayerLocal p, EntityAlive e)
    {
        tags.Clear();
        EntityBuffs b = e.Buffs;
        if (b.HasBuff("buffAposMark")) Add("hard_target", "+" + Pct(p.Buffs.GetCustomVar("aposMarkBonus")), new Color(1f, 0.82f, 0.3f));
        float slow = 0f;
        if (b.HasBuff("buffAposShackles")) slow = Mathf.Max(slow, b.GetCustomVar("aposShackle"));
        if (b.HasBuff("buffAposWound")) slow = Mathf.Max(slow, -b.GetCustomVar("aposWoundSlow"));
        if (b.HasBuff("buffAposSuppressed")) slow = Mathf.Max(slow, -b.GetCustomVar("aposSupSlow"));
        if (slow > 0.001f) Add("run", "-" + Pct(slow), new Color(0.45f, 0.75f, 1f));
        if (b.HasBuff("buffAposWound")) Add("drop", "x" + Mathf.Max(1, (int)b.GetCustomVar("aposWound")), new Color(1f, 0.35f, 0.3f));
        if (b.HasBuff("buffAposShattered")) Add("block_damage", "+50%", new Color(1f, 0.6f, 0.2f));
        if (b.HasBuff("buffAposArmorBreak")) Add("armor_iron", "-" + Pct(b.GetCustomVar("aposArmorBreak")), new Color(0.8f, 0.8f, 0.8f));
        if (b.HasBuff("buffAposConcussed")) Add("concussion", "", new Color(1f, 0.9f, 0.4f));
        if (b.HasBuff("buffAposCorroded")) Add("drop", "", new Color(0.5f, 1f, 0.35f));
        if (b.HasBuff("buffShocked")) Add("electric_power", "", new Color(0.4f, 0.9f, 1f));
    }

    public static void Draw(EntityPlayerLocal p)
    {
        if (p == null || p.playerCamera == null || !AposSettings.HudEnabled) return;
        Camera cam = p.playerCamera;
        if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
        float k = Screen.height / 1080f * AposSettings.HudScale;
        float size = 20f * k;
        style.fontSize = Mathf.RoundToInt(15f * k);
        Color old = GUI.color;
        List<Entity> list = p.world.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e is EntityPlayer || e.IsDead() || e.Buffs == null) continue;
            if ((e.position - p.position).sqrMagnitude > Range * Range) continue;
            Collect(p, e);
            if (tags.Count == 0) continue;
            Vector3 vp = cam.WorldToViewportPoint(e.transform.position + Vector3.up * (e.height * e.transform.localScale.y + 0.2f));
            if (vp.z <= 0f || vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f) continue;
            if (!AposElites.InSight(p, e)) continue;
            // widths: icon + its number
            float total = 0f;
            float[] w = new float[tags.Count];
            for (int t = 0; t < tags.Count; t++)
            {
                w[t] = size + (tags[t].Text.Length > 0 ? style.CalcSize(new GUIContent(tags[t].Text)).x + 2f : 0f) + 4f * k;
                total += w[t];
            }
            float x = vp.x * Screen.width - total / 2f, y = (1f - vp.y) * Screen.height - size;
            for (int t = 0; t < tags.Count; t++)
            {
                Tag tg = tags[t];
                Icon ic = GetIcon(p, tg.Sprite);
                Rect ir = new Rect(x, y, size, size);
                if (ic != null)
                {
                    GUI.color = new Color(0f, 0f, 0f, 0.7f);
                    GUI.DrawTextureWithTexCoords(new Rect(ir.x + 1.5f, ir.y + 1.5f, size, size), ic.Tex, ic.Uv);
                    GUI.color = tg.Color;
                    GUI.DrawTextureWithTexCoords(ir, ic.Tex, ic.Uv);
                }
                else
                {
                    GUI.color = tg.Color;
                    GUI.DrawTexture(new Rect(x + size * 0.3f, y + size * 0.3f, size * 0.4f, size * 0.4f), Texture2D.whiteTexture);
                }
                if (tg.Text.Length > 0)
                {
                    Rect tr = new Rect(x + size + 2f, y, 80f * k, size);
                    GUI.color = new Color(0f, 0f, 0f, 0.85f);
                    GUI.Label(new Rect(tr.x + 1.5f, tr.y + 1.5f, tr.width, tr.height), tg.Text, style);
                    GUI.color = tg.Color;
                    GUI.Label(tr, tg.Text, style);
                }
                x += w[t];
            }
        }
        GUI.color = old;
    }
}

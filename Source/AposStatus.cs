// Status row above zombies (drawn from AposMastery.OnGUI): the mod's own hidden debuffs on a zombie, as a vanilla UI
// icon (taken from the game's UI atlas, XUi.GetAtlasByName) plus the number that matters:
// Mark (+N% your damage), slowed (-N% speed: Shackles / Hunter's Wound / Suppression, the strongest), Wound stacks,
// Shattered, Armor Break, Concussed; effects (burning, bleeding, stun / knockdown, crippled, shock, acid, confusion, miasma)
// with the seconds left. Only within 30 m and in line of sight; settings: aposhud off hides it.
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

    // draws a sprite of the game's UI atlas (with a soft shadow) into an IMGUI rect; used by the HUD gauges too
    public static void DrawIcon(EntityPlayerLocal p, string sprite, Rect r, Color c)
    {
        Icon ic = GetIcon(p, sprite);
        if (ic == null) return;
        GUI.color = new Color(0f, 0f, 0f, 0.7f * c.a);
        GUI.DrawTextureWithTexCoords(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), ic.Tex, ic.Uv);
        GUI.color = c;
        GUI.DrawTextureWithTexCoords(r, ic.Tex, ic.Uv);
    }

    // icon: a vanilla symbol name ("fire" = ui_game_symbol_fire) or the mod's own sprite ("ui_apos_shattered")
    static void Add(string icon, string text, Color c) { tags.Add(new Tag { Sprite = icon.StartsWith("ui_") ? icon : "ui_game_symbol_" + icon, Text = text, Color = c }); }

    // ===== stacks the weapon classes keep on one target (thrust chain, combo, momentum, Breakdown...) =====
    // Every frame AposMastery.Update calls Begin(), then the held class's Tick adds its counter with Target().
    static readonly Dictionary<int, List<Tag>> perTarget = new Dictionary<int, List<Tag>>();

    public static void Begin() { perTarget.Clear(); }

    public static void Target(int entityId, string icon, string text, Color c)
    {
        if (entityId < 0) return;
        List<Tag> l;
        if (!perTarget.TryGetValue(entityId, out l)) { l = new List<Tag>(); perTarget[entityId] = l; }
        l.Add(new Tag { Sprite = "ui_game_symbol_" + icon, Text = text, Color = c });
    }

    static string Pct(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }

    static void Collect(EntityPlayerLocal p, EntityAlive e)
    {
        tags.Clear();
        List<Tag> own;
        if (perTarget.TryGetValue(e.entityId, out own)) tags.AddRange(own);
        EntityBuffs b = e.Buffs;
        if (b.HasBuff("buffAposMark")) Add("hard_target", "+" + Pct(p.Buffs.GetCustomVar("aposMarkBonus")), new Color(1f, 0.82f, 0.3f));
        float slow = 0f;
        if (b.HasBuff("buffAposShackles")) slow = Mathf.Max(slow, b.GetCustomVar("aposShackle"));
        if (b.HasBuff("buffAposWound")) slow = Mathf.Max(slow, -b.GetCustomVar("aposWoundSlow"));
        if (slow > 0.001f) Add("run", "-" + Pct(slow), new Color(0.45f, 0.75f, 1f));
        // machine gun Suppression: its own icon (a figure shielding itself), how much it slows
        if (b.HasBuff("buffAposSuppressed") && -b.GetCustomVar("aposSupSlow") > 0.001f) Add("ui_apos_suppressed", "-" + Pct(-b.GetCustomVar("aposSupSlow")), new Color(0.45f, 0.75f, 1f));
        if (b.HasBuff("buffAposWound")) Add("drop", "x" + Mathf.Max(1, (int)b.GetCustomVar("aposWound")), new Color(1f, 0.35f, 0.3f));
        if (b.HasBuff("buffAposShattered")) Add("ui_apos_shattered", "+50%", new Color(1f, 0.6f, 0.2f));
        // spear "Tip in the Wound": +25% per hit on it, up to +100%
        if (b.GetCustomVar("aposTip") > 0f && b.GetCustomVar("aposTipHits") > 0f) Add("spear", "+" + Mathf.RoundToInt(25f * b.GetCustomVar("aposTipHits")) + "%", new Color(1f, 0.5f, 0.3f));
        if (b.HasBuff("buffAposArmorBreak")) Add("armor_iron", "-" + Pct(b.GetCustomVar("aposArmorBreak")), new Color(0.8f, 0.8f, 0.8f));
        if (b.HasBuff("buffAposConcussed")) Add("concussion", "+50%", new Color(1f, 0.9f, 0.4f));   // takes +50% from everything (AposCombat)
        Effects(b);
    }

    // ===== effects (vanilla and ours): one icon per kind with the seconds left (the longest buff of that kind) =====
    // name prefix -> kind; the first match wins. Cooldowns, particles and the player-only hazard buff are skipped.
    static readonly string[] EffectPrefix = { "buffBurning", "buffInjuryBleeding", "buffAposDeepBleed", "buffInjuryStunned0", "buffInjuryKnockdown0",
                                              "buffInjuryCrippled01", "buffShocked", "buffAposCorroded", "buffAposAcidSlow", "buffAposEmp", "buffAposConfused", "buffAposMiasma" };
    static readonly int[] EffectKind = { 0, 1, 1, 2, 2, 3, 4, 5, 5, 4, 6, 7 };
    static readonly string[] KindIcon = { "fire", "critical", "stunned", "run", "electric_power", "drop", "twitch_confused_controls", "infection" };
    static readonly Color[] KindColor =
    {
        new Color(1f, 0.5f, 0.1f), new Color(1f, 0.2f, 0.2f), new Color(1f, 0.6f, 0.1f), new Color(1f, 0.45f, 0.45f),
        new Color(0.4f, 0.9f, 1f), new Color(0.5f, 1f, 0.35f), new Color(1f, 0.6f, 1f), new Color(0.7f, 0.95f, 0.5f),
    };
    static readonly string[] BurnBuffs = { "buffBurningElement", "buffBurningFlamingArrow", "buffBurningMolotov" };
    static readonly string[] BurnVars = { "$buffBurningElementDuration", "$buffBurningFlamingArrowDuration", "%buffBurningMolotovDuration" };
    static readonly float[] kindLeft = new float[8];
    static readonly bool[] kindOn = new bool[8];

    static void Effects(EntityBuffs b)
    {
        for (int k = 0; k < kindOn.Length; k++) { kindOn[k] = false; kindLeft[k] = 0f; }
        List<BuffValue> active = b.ActiveBuffs;
        for (int i = 0; i < active.Count; i++)
        {
            BuffValue v = active[i];
            string n = v != null ? v.BuffName : null;
            if (n == null || v.Remove || n.EndsWith("Cooldown") || n.EndsWith("Particle") || n.EndsWith("CHTrigger") || n == "buffBurningHazardPlayer") continue;
            for (int k = 0; k < EffectPrefix.Length; k++)
            {
                if (!n.StartsWith(EffectPrefix[k])) continue;
                int kind = EffectKind[k];
                kindOn[kind] = true;
                BuffClass bc = v.BuffClass;
                float left = bc != null && bc.DurationMax > 0f ? bc.DurationMax - v.DurationInSeconds : 0f;
                if (left > kindLeft[kind]) kindLeft[kind] = left;
                break;
            }
        }
        for (int k = 0; k < kindOn.Length; k++)
        {
            if (!kindOn[k]) continue;
            // burning: the buffs have no duration of their own, vanilla counts the seconds left in a cvar (1 per second)
            if (k == 0)
                for (int f = 0; f < BurnBuffs.Length; f++)
                    if (b.HasBuff(BurnBuffs[f])) kindLeft[0] = Mathf.Max(kindLeft[0], b.GetCustomVar(BurnVars[f]));
            string text = kindLeft[k] >= 0.5f ? Mathf.CeilToInt(kindLeft[k]) + "s" : "";
            // bleeding: also how much it takes per second (the bigger of the vanilla bleed and the deep bleed tick)
            if (k == 1)
            {
                float tick = Mathf.Max(b.HasBuff("buffInjuryBleeding") ? -b.GetCustomVar("aposBleedTick") : 0f, b.HasBuff("buffAposDeepBleed") ? -b.GetCustomVar("aposDeepBleedTick") : 0f);
                if (tick >= 1f) text = "-" + Mathf.RoundToInt(tick) + "/s" + (text.Length > 0 ? " " + text : "");
            }
            Add(KindIcon[k], text, KindColor[k]);
        }
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

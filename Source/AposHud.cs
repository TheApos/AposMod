// Combat HUD (docs/Design-Mastery2.md 5к): counters of the held weapon's class.
// - attack (streaks, stacks, charges of a strike) next to the crosshair: window windowAposHud (Center anchor)
// - defence (shields, damage reduction, regeneration, defensive cooldowns): a row at the bottom, left of the toolbelt,
//   growing to the left, window windowAposHudDef (Bottom anchor). Dim while idle; Flash(icon) (or a widget turning
//   "full") lights it up for a second, then it fades back.
// - ModBuffs: the mod's own player buffs (attribute / perk highlights, legendaries) in the same two rows, so they are
//   seen where you look: stacks and charges at the crosshair, shields and defensive cooldowns at the bottom.
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
        public string Skill;  // localization key of the mastery skill behind it (name; + "_D" = description), or null
        public float Value;   // gauges: filled segments (Segments > 0) or a 0..1 fill (Segments = 0)
        public string Kind;   // the energy column: what the bonus speeds up (Move / AttackKind / Reload), Value = the bonus
        public int Segments;
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
        GaugeLeft = GaugeRight = null;
        Speed.Clear();
        Alerts.Clear();
    }

    // ===== urgent / important: a row centred above the crosshair (Jackpot...): bigger, on a dark pill, pulsing =====
    public static readonly List<Widget> Alerts = new List<Widget>();

    // a class's urgent widget (e.g. the bow's Shadow): the row above the crosshair, also a Status Effects line
    public static void AddAlert(string icon, string text, Color color, string skill)
    {
        Widget w = new Widget { Icon = icon.StartsWith("ui_") ? icon : "ui_game_symbol_" + icon, Text = text, Color = color, Glow = true };
        if (AposMastery.HeldClass >= 0) w.Skill = AposMastery.SkillKey(AposMastery.HeldClass, skill);
        Alerts.Add(w);
    }
    static GUIStyle alertStyle;

    public static void DrawAlerts(EntityPlayerLocal p)
    {
        if (p == null || !AposSettings.HudEnabled || Alerts.Count == 0) return;
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(p);
        if (ui == null || ui.windowManager == null || ui.windowManager.IsModalWindowOpen() || !ui.windowManager.IsHUDEnabled()) return;
        float k = Screen.height / 1080f * AposSettings.HudScale;
        if (alertStyle == null) alertStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        alertStyle.fontSize = Mathf.RoundToInt(26f * k);
        float icon = 40f * k, pad = 10f * k, gap = 16f * k, h = 52f * k;
        float[] w = new float[Alerts.Count];
        float total = -gap;
        for (int i = 0; i < Alerts.Count; i++)
        {
            w[i] = pad + icon + 8f * k + alertStyle.CalcSize(new GUIContent(Alerts[i].Text)).x + pad;
            total += w[i] + gap;
        }
        float x = Screen.width / 2f - total / 2f, y = Screen.height / 2f - 150f * k - h;
        float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 5f);
        Color old = GUI.color;
        for (int i = 0; i < Alerts.Count; i++)
        {
            Widget a = Alerts[i];
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(x, y, w[i], h), Texture2D.whiteTexture);
            GUI.color = new Color(a.Color.r, a.Color.g, a.Color.b, 0.9f * pulse);
            GUI.DrawTexture(new Rect(x, y + h - 3f * k, w[i], 3f * k), Texture2D.whiteTexture);   // a coloured underline
            Color ic = a.Color; ic.a = pulse;
            AposStatus.DrawIcon(p, a.Icon, new Rect(x + pad, y + (h - icon) / 2f, icon, icon), ic);
            Rect tr = new Rect(x + pad + icon + 8f * k, y, w[i], h);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(tr.x + 1.5f, tr.y + 1.5f, tr.width, tr.height), a.Text, alertStyle);
            GUI.color = Color.white;
            GUI.Label(tr, a.Text, alertStyle);
            x += w[i] + gap;
        }
        GUI.color = old;
    }

    // ===== the "energy" block: a column at the bottom right, mirroring the vanilla buff column at the bottom left =====
    // every mod status that changes how fast you move (Adrenaline, Evasion, spear Hit and Back, knuckle Light Feet and the
    // Knockout slow: green / red) or attack / fire / reload (Frenzy, Hot Barrel, Gunfighter, Hunter's Rhythm, Riposte:
    // gold): icon + "+N%" (+ seconds / stacks), like a vanilla buff entry. Drawn by DrawSpeed (IMGUI).
    // The screen has three themed blocks: this one, the defence row between the health bars and the toolbelt, and the
    // vanilla buff column above the health bars for everything else (the mod leaves such buffs there).
    public static readonly List<Widget> Speed = new List<Widget>();
    static readonly Color SpeedUp = new Color(0.35f, 0.9f, 0.55f), SpeedDown = new Color(1f, 0.4f, 0.35f), AttackUp = new Color(1f, 0.82f, 0.3f);

    // pct: the change as a fraction (+0.15 = 15% faster); extra: seconds / stacks after it; skill: held class skill id or null
    // Kind: what the bonus speeds up; statuses of one kind are merged into one line (their icons + the summed %)
    public const string Move = "move", AttackKind = "attack", Reload = "reload";
    static readonly string[] SpeedKinds = { Move, AttackKind, Reload };

    public static void AddSpeed(string icon, float pct, string extra, string skill)
    {
        AddSpeedOf(Move, icon, pct, extra, skill);
    }

    // attack / fire rate (gold)
    public static void AddAttackSpeed(string icon, float pct, string extra, string skill)
    {
        AddSpeedOf(AttackKind, icon, pct, extra, skill);
    }

    // reload speed (gold)
    public static void AddReloadSpeed(string icon, float pct, string extra, string skill)
    {
        AddSpeedOf(Reload, icon, pct, extra, skill);
    }

    static void AddSpeedOf(string kind, string icon, float pct, string extra, string skill)
    {
        string text = (pct >= 0f ? "+" : "") + Mathf.RoundToInt(pct * 100f) + "%" + (string.IsNullOrEmpty(extra) ? "" : "  " + extra);
        Widget w = new Widget { Icon = icon.StartsWith("ui_") ? icon : "ui_game_symbol_" + icon, Text = text, Kind = kind, Value = pct,
                                Color = kind != Move ? AttackUp : pct >= 0f ? SpeedUp : SpeedDown };
        if (skill != null && AposMastery.HeldClass >= 0) w.Skill = AposMastery.SkillKey(AposMastery.HeldClass, skill);
        Speed.Add(w);
    }

    static GUIStyle speedStyle;
    static Texture2D speedBg;
    static readonly List<Widget> speedGroup = new List<Widget>();

    // one line per kind: a single status as it is (with its timer / stacks); several: all their icons and the summed %
    public static void DrawSpeed(EntityPlayerLocal p)
    {
        if (p == null || !AposSettings.HudEnabled || Speed.Count == 0) return;
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(p);
        if (ui == null || ui.windowManager == null || ui.windowManager.IsModalWindowOpen() || !ui.windowManager.IsHUDEnabled()) return;
        float k = Screen.height / 1080f;
        if (speedStyle == null) speedStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        if (speedBg == null)
        {
            // a bar fading out to the left, like the vanilla buff entry (ui_game_popup) mirrored
            speedBg = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int px = 0; px < 64; px++) speedBg.SetPixel(px, 0, new Color(0f, 0f, 0f, 0.55f * Mathf.Clamp01(px / 24f)));
            speedBg.Apply();
        }
        speedStyle.fontSize = Mathf.RoundToInt(24f * k);
        float h = 46f * k, icon = 36f * k, gap = 4f * k;
        float right = Screen.width - 90f * k, bottom = Screen.height - 132f * k;
        Color old = GUI.color;
        int line = 0;
        foreach (string kind in SpeedKinds)
        {
            speedGroup.Clear();
            float sum = 0f;
            foreach (Widget s in Speed) if (s.Kind == kind) { speedGroup.Add(s); sum += s.Value; }
            if (speedGroup.Count == 0) continue;
            string text = speedGroup.Count == 1 ? speedGroup[0].Text : (sum >= 0f ? "+" : "") + Mathf.RoundToInt(sum * 100f) + "%";
            Color tc = speedGroup.Count == 1 ? Color.white : kind != Move ? AttackUp : sum >= 0f ? SpeedUp : SpeedDown;
            float tw = speedStyle.CalcSize(new GUIContent(text)).x;
            float iconsW = speedGroup.Count * icon + (speedGroup.Count - 1) * gap;
            float w = Mathf.Max(190f * k, iconsW + tw + 60f * k);
            float y = bottom - (++line) * h;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(right - w, y + 2f * k, w, h - 4f * k), speedBg);
            // right-aligned: the summed text, then the icons, the first one at the outer edge
            float ix = right - 6f * k - icon;
            foreach (Widget s in speedGroup)
            {
                AposStatus.DrawIcon(p, s.Icon, new Rect(ix, y + (h - icon) / 2f, icon, icon), s.Color);
                ix -= icon + gap;
            }
            Rect tr = new Rect(ix + icon + gap - 6f * k - tw, y, tw + 4f, h);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(tr.x + 1.5f, tr.y + 1.5f, tr.width, tr.height), text, speedStyle);
            GUI.color = tc;
            GUI.Label(tr, text, speedStyle);
        }
        GUI.color = old;
    }

    // ===== gauges: the weapon's key mechanic as a quarter arc around the crosshair =====
    // right: the main stack of the held class (Bloodsucker, Headhunter, Rage...), left: ability cooldowns
    // (Earthquake, Chain Lightning; defensive charges such as Counter Punch sit in the defence row). Segments light up one by one, empty ones are barely drawn, a full
    // gauge pulses. Drawn by DrawGauges (IMGUI, from AposMastery.OnGUI); the value is also a Status Effects line.
    public static Widget GaugeLeft, GaugeRight;

    // value: filled segments (segments > 0) or a 0..1 fill (segments = 0); text: the number under the arc
    public static void Gauge(bool left, float value, int segments, Color color, bool full, string icon, string text, string skill)
    {
        Widget w = new Widget { Icon = icon.StartsWith("ui_") ? icon : "ui_game_symbol_" + icon, Text = text, Color = color, Glow = full,
                                Value = value, Segments = Mathf.Max(0, segments) };
        if (AposMastery.HeldClass >= 0) w.Skill = AposMastery.SkillKey(AposMastery.HeldClass, skill);
        if (left) GaugeLeft = w; else GaugeRight = w;
    }

    const float ArcSpan = 70f, ArcGap = 3.5f;
    static GUIStyle gaugeStyle;

    public static void DrawGauges(EntityPlayerLocal p)
    {
        if (p == null || !AposSettings.HudEnabled || (GaugeLeft == null && GaugeRight == null)) return;
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(p);
        if (ui == null || ui.windowManager == null || ui.windowManager.IsModalWindowOpen() || !ui.windowManager.IsHUDEnabled()) return;
        float k = Screen.height / 1080f * AposSettings.HudScale;
        if (gaugeStyle == null) gaugeStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        gaugeStyle.fontSize = Mathf.RoundToInt(15f * k);
        Vector2 c = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Color old = GUI.color;
        Matrix4x4 m = GUI.matrix;
        if (GaugeRight != null) DrawArc(p, GaugeRight, c, k, false);
        if (GaugeLeft != null) DrawArc(p, GaugeLeft, c, k, true);
        GUI.matrix = m;
        GUI.color = old;
    }

    // angles in degrees, 0 = straight out to the side, + = down; the arc fills from the bottom up
    static void DrawArc(EntityPlayerLocal p, Widget w, Vector2 c, float k, bool left)
    {
        float r = 62f * k, thick = 5f * k;
        int segs = w.Segments > 0 ? w.Segments : 1;
        float gap = segs > 1 ? Mathf.Min(ArcGap, 30f / segs) : 0f;
        float seg = (ArcSpan - gap * (segs - 1)) / segs;
        float pulse = w.Glow ? 0.75f + 0.25f * Mathf.Sin(Time.time * 6f) : 1f;
        for (int i = 0; i < segs; i++)
        {
            float a0 = ArcSpan / 2f - i * (seg + gap);
            float fill = w.Segments > 0 ? Mathf.Clamp01(w.Value - i) : Mathf.Clamp01(w.Value);
            Piece(c, r, a0, a0 - seg, thick, new Color(1f, 1f, 1f, 0.13f), left);                      // the empty track
            if (fill <= 0f) continue;
            Color col = w.Color;
            if (w.Glow) Piece(c, r, a0, a0 - seg * fill, thick * 2.6f, new Color(col.r, col.g, col.b, 0.18f * pulse), left);   // soft glow
            Piece(c, r, a0, a0 - seg * fill, thick, new Color(col.r, col.g, col.b, 0.9f * pulse), left);
        }
        // icon + number outside the middle of the arc (not under it: the attack row sits there)
        float x = c.x + (left ? -1f : 1f) * (r + 10f * k), y = c.y;
        float size = 18f * k;
        GUI.matrix = Matrix4x4.identity;
        Color ic = w.Color; ic.a = 0.95f;
        float tw = w.Text.Length > 0 ? gaugeStyle.CalcSize(new GUIContent(w.Text)).x : 0f;
        float x0 = left ? x - size - 2f * k - tw : x;
        AposStatus.DrawIcon(p, w.Icon, new Rect(x0, y - size / 2f, size, size), ic);
        if (tw > 0f)
        {
            Rect tr = new Rect(x0 + size + 2f * k, y - size / 2f, tw + 4f, size);
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(tr.x + 1.5f, tr.y + 1.5f, tr.width, tr.height), w.Text, gaugeStyle);
            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            GUI.Label(tr, w.Text, gaugeStyle);
        }
    }

    // one piece of the arc between two angles, drawn as short rotated bars (no shaders needed)
    static void Piece(Vector2 c, float r, float from, float to, float thick, Color col, bool left)
    {
        float step = 1.5f;
        int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(from - to) / step));
        float d = (to - from) / n;
        float len = r * Mathf.Abs(d) * Mathf.Deg2Rad + 1f;
        GUI.color = col;
        for (int i = 0; i < n; i++)
        {
            float a = from + d * (i + 0.5f);
            float rad = a * Mathf.Deg2Rad;
            Vector2 at = new Vector2(c.x + (left ? -1f : 1f) * r * Mathf.Cos(rad), c.y + r * Mathf.Sin(rad));
            GUI.matrix = Matrix4x4.identity;
            GUIUtility.RotateAroundPivot(left ? 180f - a : a, at);
            GUI.DrawTexture(new Rect(at.x - thick / 2f, at.y - len / 2f, thick, len), Texture2D.whiteTexture);
        }
    }

    // icon: a vanilla symbol name ("head_shot" = ui_game_symbol_head_shot) or the mod's own sprite ("ui_apos_squad")
    public static void Add(bool defense, string icon, string text, Color color, bool glow)
    {
        AddSprite(defense, icon.StartsWith("ui_") ? icon : "ui_game_symbol_" + icon, text, color, glow);
    }

    // skill: the id of the held class's skill this counter belongs to ("Base", "A3", ...): it also gets its own line in
    // the character's Status Effects window, with the skill's name, the same value and the skill's description
    public static void Add(bool defense, string icon, string text, Color color, bool glow, string skill)
    {
        Add(defense, icon, text, color, glow);
        List<Widget> l = defense ? Defense : Attack;
        if (AposMastery.HeldClass >= 0) l[l.Count - 1].Skill = AposMastery.SkillKey(AposMastery.HeldClass, skill);
    }

    // ===== the counters in the Status Effects window =====
    // buffs.xml buffAposHudInfo0..7: empty buffs (not on the HUD column) that take the icon, name and description of a
    // counter every frame; the value comes from XUiC_ActiveBuffEntry's "buffdisplayinfo" binding (AposHudInfoPatch).
    const int InfoSlots = 8;
    public static readonly string[] InfoText = new string[InfoSlots];
    public static readonly string[] InfoName = new string[InfoSlots];

    public static int InfoSlot(string buffName)
    {
        if (buffName == null || !buffName.StartsWith("buffAposHudInfo")) return -1;
        int i;
        return int.TryParse(buffName.Substring(15), out i) && i >= 0 && i < InfoSlots ? i : -1;
    }

    // "Weapon Mastery: Knives and machetes, branch "Bloodsucker", skill A3" from a key aposMS_<Class>_<Id>
    static string SkillSource(string key)
    {
        string[] part = key.Split('_');
        if (part.Length < 3) return "";
        string cls = Localization.Get("aposMasteryClass" + part[1]), id = part[2];
        if (id == "Base") return string.Format(Localization.Get("aposHudSrcBase"), cls);
        if (id[0] == 'H') return string.Format(Localization.Get("aposHudSrcHybrid"), cls, id);
        return string.Format(Localization.Get("aposHudSrcBranch"), cls, Localization.Get("aposMB_" + part[1] + "_" + id[0]), id);
    }

    public static void StatusInfo(EntityPlayerLocal p)
    {
        int n = 0;
        List<Widget> all = new List<Widget>();
        if (GaugeRight != null) all.Add(GaugeRight);
        if (GaugeLeft != null) all.Add(GaugeLeft);
        all.AddRange(Attack);
        all.AddRange(Defense);
        all.AddRange(Speed);
        all.AddRange(Alerts);
        {
            foreach (Widget w in all)
            {
                if (w.Skill == null || n >= InfoSlots) continue;
                BuffClass c = BuffManager.GetBuff("buffAposHudInfo" + n);
                if (c == null) return;
                Color col = w.Color;
                col.a = 1f;
                c.Icon = w.Icon;
                c.IconColor = col;
                InfoName[n] = Localization.Get(w.Skill);
                InfoText[n] = w.Text;
                c.LocalizedName = InfoName[n];
                c.DescriptionKey = w.Skill + "_D";
                c.Description = SkillSource(w.Skill) + "\n" + Localization.Get(c.DescriptionKey);
                if (!p.Buffs.HasBuff(c.Name)) p.Buffs.AddBuff(c.Name);
                n++;
            }
        }
        for (int i = n; i < InfoSlots; i++)
        {
            string name = "buffAposHudInfo" + i;
            if (p.Buffs.HasBuff(name)) p.Buffs.RemoveBuff(name);
        }
    }

    public static void AddSprite(bool defense, string sprite, string text, Color color, bool glow)
    {
        (defense ? Defense : Attack).Add(new Widget { Icon = sprite, Text = text, Color = color, Glow = glow });
    }

    // ===== the mod's own player buffs =====
    // buff, defence row?, what the number is: cvar (with an optional "/max" or "%"), or "time" = seconds left, "" = icon only
    // Speed: a movement buff, shown in the speed column instead (SpeedCvar: its bonus as a fraction, or SpeedPct fixed)
    // Alert: urgent / important, the row above the crosshair (Prefix goes before the timer, e.g. "x5")
    class ModBuff { public string Buff, Cvar, Suffix, SpeedCvar, Prefix; public bool Defense, Speed, Attack, Alert; public int Full; public float SpeedPct; }
    static readonly ModBuff[] modBuffs =
    {
        new ModBuff { Buff = "buffAposThunderCharge", Cvar = "aposThunder", Suffix = "/3", Full = 3 },
        new ModBuff { Buff = "buffAposAdrenaline", Cvar = "aposAdrenaline", Suffix = "/3", Full = 3, Speed = true, SpeedCvar = "aposAdrenalineSpeed" },
        new ModBuff { Buff = "buffAposEmp", Cvar = "aposEmp", Suffix = "/10", Full = 10, Defense = true },   // discharges around you
        new ModBuff { Buff = "buffAposPipeVeteran", Cvar = "aposPipeBonus", Suffix = "%" },
        new ModBuff { Buff = "buffAposShadow", Cvar = "", Alert = true, Prefix = "x2" },   // the next hit x2: above the crosshair
        new ModBuff { Buff = "buffAposFrenzyPerception", Cvar = "time", Speed = true, Attack = true, SpeedPct = 0.15f },
        new ModBuff { Buff = "buffAposFrenzyStrength", Cvar = "time", Speed = true, Attack = true, SpeedPct = 0.15f },
        new ModBuff { Buff = "buffAposFrenzyFortitude", Cvar = "time", Speed = true, Attack = true, SpeedPct = 0.15f },
        new ModBuff { Buff = "buffAposFrenzyAgility", Cvar = "time", Speed = true, Attack = true, SpeedPct = 0.15f },
        new ModBuff { Buff = "buffAposJackpot", Cvar = "time", Alert = true, Prefix = "x5" },
        new ModBuff { Buff = "buffAposShieldUp", Cvar = "time", Defense = true, Full = -1 },
        new ModBuff { Buff = "buffAposGuardian", Cvar = "", Defense = true },
        new ModBuff { Buff = "buffAposEvasion", Cvar = "time", Speed = true, SpeedPct = 0.3f },
        new ModBuff { Buff = "buffAposVampire", Cvar = "aposVampHP", Suffix = " HP", Defense = true },
        new ModBuff { Buff = "buffAposSecondWindCD", Cvar = "time", Defense = true },
        new ModBuff { Buff = "buffAposSecondLungsCD", Cvar = "time", Defense = true },
        // Corroded Armor (an Acidic elite's hit) is a debuff, not defence: it stays in the vanilla buff column
    };

    // buffs the HUD already shows in its own way (the knife's Bloodsucker gauge and regen widget): off the column too
    static readonly string[] ColumnHidden = { "buffAposKnifeRegen" };

    public static void ModBuffs(EntityPlayerLocal p)
    {
        // while the HUD shows them, keep these buffs out of the on-screen buff column on the left (no double icons), but
        // still in the character's Status Effects window (name + description). BuffClass.Hidden would drop them from
        // both: both lists are built from the same buff notifications; ShowOnHUD is only checked by XUiC_BuffPopoutList.
        // aposhud off brings them back. Set every frame: the buff classes are rebuilt from XML when a world loads.
        foreach (ModBuff m in modBuffs)
        {
            BuffClass c = BuffManager.GetBuff(m.Buff);
            if (c != null) c.ShowOnHUD = !AposSettings.HudEnabled;
        }
        foreach (string name in ColumnHidden)
        {
            BuffClass c = BuffManager.GetBuff(name);
            if (c != null) c.ShowOnHUD = !AposSettings.HudEnabled;
        }
        EntityBuffs b = p.Buffs;
        foreach (ModBuff m in modBuffs)
        {
            BuffValue v = b.GetBuff(m.Buff);
            if (v == null || v.Remove) continue;
            BuffClass bc = v.BuffClass;
            if (bc == null || string.IsNullOrEmpty(bc.Icon)) continue;
            string text = "";
            bool full = m.Full < 0;
            if (m.Cvar == "time")
            {
                float left = bc.DurationMax > 0f ? bc.DurationMax - v.DurationInSeconds : 0f;
                if (left >= 0.5f) text = Mathf.CeilToInt(left) + "s";
            }
            else if (m.Cvar.Length > 0)
            {
                int n = Mathf.RoundToInt(b.GetCustomVar(m.Cvar));
                text = n + (m.Suffix ?? "");
                if (m.Full > 0 && n >= m.Full) full = true;
            }
            else if (m.Suffix != null) text = m.Suffix;
            if (m.Alert)
            {
                Color ac = bc.IconColor; ac.a = 1f;
                Alerts.Add(new Widget { Icon = bc.Icon, Color = ac, Text = (m.Prefix ?? "") + (text.Length > 0 ? "  " + text : "") });
                continue;
            }
            if (m.Speed)
            {
                float pct = m.SpeedCvar != null ? b.GetCustomVar(m.SpeedCvar) : m.SpeedPct;
                Speed.Add(new Widget { Icon = bc.Icon, Color = m.Attack ? AttackUp : SpeedUp, Kind = m.Attack ? AttackKind : Move, Value = pct,
                    Text = "+" + Mathf.RoundToInt(pct * 100f) + "%" + (text.Length > 0 ? "  " + text : "") });
                continue;
            }
            Color c = bc.IconColor;
            if (c.r + c.g + c.b < 0.05f) c = Color.white;
            c.a = 1f;
            AddSprite(m.Defense, bc.Icon, text, c, full);
        }
    }
}

// one row of up to 4 widget slots (atk0..atk3 in windows.xml)
public class XUiC_AposHud : XUiController
{
    const int Slots = 6;
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

    // attack row: centred under the crosshair, or a column to the right / left of it; defence: a row left of the toolbelt
    int laidOut = -1;
    void Layout(int n)
    {
        float scale = AposSettings.HudScale;
        if (scale != appliedScale && ViewComponent != null && ViewComponent.UiTransform != null)
        {
            appliedScale = scale;
            ViewComponent.UiTransform.localScale = Vector3.one * scale;
        }
        string pos = IsDefense ? "belt" : AposHud.AttackAt.HasValue ? "at" + AposHud.AttackAt.Value : AposSettings.HudPos;
        if (n == laidOut && pos == appliedPos) return;
        laidOut = n;
        appliedPos = pos;
        for (int i = 0; i < n; i++)
        {
            if (slot[i] == null) continue;
            Vector2i p;
            if (pos == "right") p = new Vector2i(40, 30 - i * 32);
            else if (pos == "left") p = new Vector2i(-40 - SlotW, 30 - i * 32);
            else if (pos == "belt") p = new Vector2i(-388 - (i + 1) * SlotW, 59);   // left of the toolbelt (753 wide, top at 84)
            else if (pos.StartsWith("at")) p = new Vector2i(AposHud.AttackAt.Value.x + i * SlotW, AposHud.AttackAt.Value.y);
            else p = new Vector2i(-n * SlotW / 2 + i * SlotW, -42);   // below the gauges' bottom ends (y +36)
            slot[i].ViewComponent.Position = p;
        }
    }
}

public class XUiC_AposHudDef : XUiC_AposHud
{
    protected override bool IsDefense { get { return true; } }
}

// The Status Effects window: a buffAposHudInfoN line shows the counter's current name and value (the entry caches the
// name when the buff appears, and its display value is numeric only)
[HarmonyLib.HarmonyPatch(typeof(XUiC_ActiveBuffEntry), "GetBindingValueInternal")]
public static class AposHudInfoPatch
{
    static void Postfix(XUiC_ActiveBuffEntry __instance, ref string value, string bindingName)
    {
        if (bindingName != "buffname" && bindingName != "buffdisplayinfo") return;
        EntityUINotification n = __instance.Notification;
        BuffValue b = n != null ? n.Buff : null;
        int slot = b != null && b.BuffClass != null ? AposHud.InfoSlot(b.BuffClass.Name) : -1;
        if (slot < 0) return;
        value = (bindingName == "buffname" ? AposHud.InfoName[slot] : AposHud.InfoText[slot]) ?? "";
    }
}

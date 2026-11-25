// Reaction feed: when an elemental reaction or a big area proc caused by the local player happens, a line appears on
// the right of the screen for a few seconds.
// Reactions: element icon + element icon = result, and what it did.
// Procs: source (INT 10, STR 10, perk 5) >> icon, damage per zombie and how many it hit:
//   EMP (Intellect 10, XML: aposSplashKind = 1 on the targets, AposSplash.ProcessXml), Overkill (Strength 10),
//   Chain Reaction (Demolitions Expert 5), Shrapnel (Penetrator 5) - all three from AposCombat.cs.
//   fire + acid  = Explosion: damage per zombie and how many it hit (AposSplinter.cs reports it)
//   fire + shock = Confusion (buffs.xml sets aposReactShow = 2 on the zombie)
//   shock + acid = Miasma    (aposReactShow = 3)
// Pictograms: the white elite badges in Icons/ (elite_Burning, elite_Acidic, elite_Electric, elite_Explosive), tinted.
using System.Collections.Generic;
using UnityEngine;

public static class AposReactions
{
    public const int Explosion = 1, Confusion = 2, Miasma = 3, Emp = 4, Overkill = 5, ChainReaction = 6, Shrapnel = 7,
                     Pierce = 8, SplinterMine = 9, StickyCharge = 10;
    const float Life = 4f;
    const int MaxLines = 4;

    class Line
    {
        public int Type;
        public int Damage;
        public int Targets;
        public float Time;
    }

    static readonly List<Line> lines = new List<Line>();
    static Texture2D fire, acid, shock, blast, carry, shield;
    static GUIStyle style;

    static readonly Color FireColor = new Color(1f, 0.45f, 0.1f);
    static readonly Color AcidColor = new Color(0.6f, 0.9f, 0.2f);
    static readonly Color ShockColor = new Color(0.35f, 0.75f, 1f);
    static readonly Color BlastColor = new Color(1f, 0.8f, 0.25f);

    // only reactions started by the local player's hits (aposHitBy on the zombie, AposCombat.cs)
    public static bool Mine(EntityAlive zombie, EntityPlayerLocal player)
    {
        return player != null && (int)zombie.Buffs.GetCustomVar("aposHitBy") == player.entityId;
    }

    public static void Add(int type, int damage, int targets)
    {
        // explosions of the same moment (a chain) merge into one line
        if (lines.Count > 0)
        {
            Line last = lines[lines.Count - 1];
            if (last.Type == type && Time.time - last.Time < 0.3f)
            {
                last.Damage = Mathf.Max(last.Damage, damage);
                last.Targets += targets;
                last.Time = Time.time;
                return;
            }
        }
        lines.Add(new Line { Type = type, Damage = damage, Targets = targets, Time = Time.time });
        if (lines.Count > MaxLines) lines.RemoveAt(0);
    }

    // Confusion and Miasma come from XML: look for the flag on zombies near the player
    public static void Watch(EntityAlive zombie, EntityPlayerLocal player)
    {
        float show = zombie.Buffs.GetCustomVar("aposReactShow");
        if (show <= 0f) return;
        zombie.Buffs.SetCustomVar("aposReactShow", 0f);
        if (Mine(zombie, player)) Add((int)show, 0, 1);
    }

    public static void Clear() { lines.Clear(); }

    static Texture2D Icon(string name)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        string path = System.IO.Path.Combine(System.IO.Path.Combine(AposSettings.ModPath ?? "", "Icons"), "elite_" + name + ".png");
        if (System.IO.File.Exists(path) && ImageConversion.LoadImage(tex, System.IO.File.ReadAllBytes(path))) return tex;
        return null;
    }

    public static void Draw()
    {
        for (int i = lines.Count - 1; i >= 0; i--) if (Time.time - lines[i].Time > Life) lines.RemoveAt(i);
        if (lines.Count == 0) return;
        if (style == null)
        {
            fire = Icon("Burning"); acid = Icon("Acidic"); shock = Icon("Electric"); blast = Icon("Explosive");
            carry = Icon("Fast"); shield = Icon("Armored");
            style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        }
        float k = Screen.height / 1080f;
        float icon = 34f * k, gap = 6f * k, rowH = 44f * k;
        style.fontSize = Mathf.RoundToInt(24 * k);
        float right = Screen.width - 24f * k;
        float y = Screen.height * 0.42f;
        Color old = GUI.color;
        for (int i = 0; i < lines.Count; i++)
        {
            Line l = lines[i];
            float age = Time.time - l.Time;
            float alpha = Mathf.Clamp01(Life - age);           // fades over the last second
            float pop = 1f + 0.25f * Mathf.Clamp01(1f - age / 0.2f);

            string count = l.Targets > 1 ? "  x" + l.Targets : "";
            float cy = y + i * rowH;
            if (l.Type >= Emp)
            {
                // proc: [source] [icon] -damage xN
                Texture2D pic; Color pc; string source;
                switch (l.Type)
                {
                    case Emp: pic = shock; pc = ShockColor; source = Localization.Get("aposProcEmp"); break;
                    case Overkill: pic = carry; pc = new Color(1f, 0.55f, 0.35f); source = Localization.Get("aposProcOverkill"); break;
                    case ChainReaction: pic = blast; pc = BlastColor; source = Localization.Get("aposProcChain"); break;
                    case Pierce: pic = carry; pc = new Color(1f, 0.82f, 0.3f); source = Localization.Get("aposMS_Bows_B2"); break;
                    case SplinterMine: pic = blast; pc = BlastColor; source = Localization.Get("aposMS_Bows_A7"); break;
                    case StickyCharge: pic = blast; pc = FireColor; source = Localization.Get("aposMS_Crossbows_B1"); break;
                    default: pic = shield; pc = new Color(0.8f, 0.8f, 0.85f); source = Localization.Get("aposProcShrapnel"); break;
                }
                string dmg = "-" + l.Damage + count;
                float sw = style.CalcSize(new GUIContent(source)).x, dw = style.CalcSize(new GUIContent(dmg)).x;
                float w = sw + icon + dw + gap * 5;
                float px = right - w;
                Background(px, cy, w, icon, gap, k, alpha);
                px = DrawText(source, new Color(1f, 0.82f, 0.3f), px, cy, icon, alpha) + gap;
                px = DrawIcon(pic, pc, px - icon * (pop - 1f) / 2f, cy - icon * (pop - 1f) / 2f, icon * pop, alpha) + gap;
                DrawText(dmg, pc, px, cy, icon, alpha);
                continue;
            }

            Texture2D a, b, r;
            Color ca, cb, cr;
            string text;
            switch (l.Type)
            {
                case Explosion:
                    a = fire; ca = FireColor; b = acid; cb = AcidColor; r = blast; cr = BlastColor;
                    text = "-" + l.Damage + count;
                    break;
                case Confusion:
                    a = fire; ca = FireColor; b = shock; cb = ShockColor; r = null; cr = new Color(1f, 0.6f, 1f);
                    text = Localization.Get("aposReactConfusion") + count;
                    break;
                default:
                    a = shock; ca = ShockColor; b = acid; cb = AcidColor; r = null; cr = new Color(0.7f, 0.95f, 0.5f);
                    text = Localization.Get("aposReactMiasma") + count;
                    break;
            }
            // laid out left to right: [a] + [b] = [result] text, right-aligned
            float textW = style.CalcSize(new GUIContent(text)).x;
            float plusW = style.CalcSize(new GUIContent("+")).x, eqW = style.CalcSize(new GUIContent("=")).x;
            float width = icon * 2 + plusW + eqW + gap * 6 + textW + (r != null ? icon + gap : 0f);
            float x = right - width;
            Background(x, cy, width, icon, gap, k, alpha);

            x = DrawIcon(a, ca, x, cy, icon, alpha) + gap;
            x = DrawText("+", Color.white, x, cy, icon, alpha) + gap;
            x = DrawIcon(b, cb, x, cy, icon, alpha) + gap;
            x = DrawText("=", Color.white, x, cy, icon, alpha) + gap;
            if (r != null) x = DrawIcon(r, cr, x - icon * (pop - 1f) / 2f, cy - icon * (pop - 1f) / 2f, icon * pop, alpha) + gap;
            DrawText(text, cr, x, cy, icon, alpha);
        }
        GUI.color = old;
    }

    static void Background(float x, float y, float w, float icon, float gap, float k, float alpha)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.45f * alpha);
        GUI.DrawTexture(new Rect(x - gap * 2, y - 4f * k, w + gap * 4, icon + 8f * k), Texture2D.whiteTexture);
    }

    static float DrawIcon(Texture2D tex, Color c, float x, float y, float size, float alpha)
    {
        if (tex == null) return x + size;
        GUI.color = new Color(c.r, c.g, c.b, alpha);
        GUI.DrawTexture(new Rect(x, y, size, size), tex);
        return x + size;
    }

    static float DrawText(string s, Color c, float x, float y, float h, float alpha)
    {
        GUIContent content = new GUIContent(s);
        float w = style.CalcSize(content).x;
        GUI.color = new Color(0f, 0f, 0f, 0.8f * alpha);
        GUI.Label(new Rect(x + 2f, y + 2f, w, h), content, style);
        GUI.color = new Color(c.r, c.g, c.b, alpha);
        GUI.Label(new Rect(x, y, w, h), content, style);
        return x + w;
    }
}

// AposMod C# part. Built with Source\build.ps1 (C# 5). Settings: AposSettings.cs. Harmony patches: AposStealth.cs,
// AposAffixes.cs, AposCombat.cs, AposRewards.cs.
using System.Collections.Generic;
using UnityEngine;

public class AposModInit : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        AposSettings.Load(_modInstance.Path);
        // Patch class by class: one patch broken by a game update must not switch off the others
        HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("apos.aposmod");
        int ok = 0;
        foreach (System.Type t in typeof(AposModInit).Assembly.GetTypes())
        {
            if (t.GetCustomAttributes(typeof(HarmonyLib.HarmonyPatch), false).Length == 0) continue;
            try
            {
                harmony.CreateClassProcessor(t).Patch();
                ok++;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AposMod] Harmony patch " + t.Name + " failed, that feature is off: " + e);
            }
        }
        Debug.Log("[AposMod] Harmony patches applied: " + ok);
        GameObject go = new GameObject("AposDamageNumbers");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AposDamageNumbers>();
        go.AddComponent<AposPin>();
        go.AddComponent<AposSplinter>();
        go.AddComponent<AposAmmo>();
        go.AddComponent<AposSurvival>();
    }
}

// Floating damage numbers. Watches the health of living non-player entities near the local
// player every frame and shows each drop as a number rising above the head.
// Headshots (yellow) are read from EntityAlive.RecordedDamage when it changed this frame.
// A killing blow adds a red "Kill!" label (text from Localization.csv, key aposKillText).
// Hits boosted by an AposMod buff are cyan. Damage over time and elemental reactions (health drops without a fresh
// hit record) take the colour of the element on the target: reaction, acid, fire, shock, bleeding.
public class AposDamageNumbers : MonoBehaviour
{
    const float Range = 60f;
    const float RiseHeight = 1.2f;

    static readonly Color NormalColor = Color.white;
    static readonly Color HeadshotColor = new Color(1f, 0.85f, 0.1f);
    static readonly Color BuffedColor = new Color(0.3f, 0.85f, 1f);
    static readonly Color KillColor = new Color(1f, 0.15f, 0.1f);
    static readonly Color ReactionColor = new Color(1f, 0.24f, 0.95f);  // magenta
    static readonly Color AcidColor = new Color(0.55f, 1f, 0.18f);      // toxic green
    static readonly Color FireColor = new Color(1f, 0.55f, 0.1f);       // orange
    static readonly Color ShockColor = new Color(0.25f, 0.45f, 1f);     // deep blue (darker than the cyan of buffed hits)
    static readonly Color MarkColor = new Color(1f, 0.2f, 0.1f);        // crossbow Mark ring
    static readonly Color BleedColor = new Color(1f, 0.45f, 0.5f);      // pink

    static readonly string[] FireBuffs = { "buffBurningElement", "buffBurningFlamingArrow", "buffBurningMolotov", "buffBurningZombie" };

    // Player buffs that raise the damage of your hits, and target markers that do the same
    static readonly string[] PlayerDamageBuffs = { "buffAposHeadhunter", "buffAposRage", "buffAposCounter" };
    static readonly string[] TargetDamageBuffs = { "buffAposMark", "buffAposCorroded" };
    const string SuppressedFullVar = "aposSupFull";

    class Popup
    {
        public Vector3 Pos;
        public string Text;
        public Color Color;
        public int Size;
        public float Start;
    }

    // Last seen values per entity id, to detect a new hit
    readonly Dictionary<int, int> lastHealth = new Dictionary<int, int>();
    readonly Dictionary<int, float> lastHitRandom = new Dictionary<int, float>();
    readonly HashSet<int> seen = new HashSet<int>();
    readonly List<int> stale = new List<int>();
    readonly List<Popup> popups = new List<Popup>();
    readonly List<EntityAlive> marked = new List<EntityAlive>();  // zombies with the crossbow Mark, drawn as a ring
    Texture2D ringTexture;
    Camera cam;
    GUIStyle style;
    string killText;

    void Awake()
    {
        // Skip the IMGUI layout pass: we only draw labels, so this avoids per-frame work and garbage
        useGUILayout = false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        if (player == null)
        {
            lastHealth.Clear();
            lastHitRandom.Clear();
            popups.Clear();
            cam = null;
            killText = null; // re-read after a language change in the menu
            return;
        }
        cam = player.playerCamera;

        seen.Clear();
        marked.Clear();
        List<Entity> list = gm.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e is EntityPlayer) continue;
            if ((e.position - player.position).sqrMagnitude > Range * Range) continue;

            int id = e.entityId;
            seen.Add(id);
            if (!e.IsDead() && e.Buffs.HasBuff("buffAposMark")) marked.Add(e);
            int hp = e.Health;
            DamageResponse rec = e.RecordedDamage;

            int prevHp;
            if (lastHealth.TryGetValue(id, out prevHp) && hp < prevHp)
            {
                float prevRandom;
                bool freshRecord = !lastHitRandom.TryGetValue(id, out prevRandom) || prevRandom != rec.Random;
                bool headshot = freshRecord && (rec.HitBodyPart & EnumBodyPartHit.Head) != 0;
                bool buffed = AposSettings.BuffColor && (HasAnyBuff(player, PlayerDamageBuffs)
                    || HasAnyBuff(e, TargetDamageBuffs) || e.Buffs.GetCustomVar(SuppressedFullVar) > 0f);
                Color? element = freshRecord ? (Color?)null : ElementColor(e);
                if (AposSettings.Enabled) AddPopup(e, prevHp - hp, headshot, buffed, element, hp <= 0);
            }
            lastHealth[id] = hp;
            lastHitRandom[id] = rec.Random;
        }

        // Forget entities that despawned or left the range
        stale.Clear();
        foreach (int id in lastHealth.Keys)
        {
            if (!seen.Contains(id)) stale.Add(id);
        }
        for (int i = 0; i < stale.Count; i++)
        {
            lastHealth.Remove(stale[i]);
            lastHitRandom.Remove(stale[i]);
        }

        // Drop expired popups (backwards loop, no allocations)
        float now = Time.time;
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            if (now - popups[i].Start > AposSettings.Lifetime) popups.RemoveAt(i);
        }
    }

    static bool HasAnyBuff(EntityAlive e, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (e.Buffs.HasBuff(names[i])) return true;
        }
        return false;
    }

    // Colour of damage that came without a hit (ticks, reactions), by what is on the target; null = plain hit
    static Color? ElementColor(EntityAlive e)
    {
        if (e.Buffs.HasBuff("buffAposReactionHit")) return ReactionColor;
        if (e.Buffs.HasBuff("buffAposCorroded")) return AcidColor;
        if (HasAnyBuff(e, FireBuffs)) return FireColor;
        if (e.Buffs.HasBuff("buffShocked")) return ShockColor;
        if (e.Buffs.HasBuff("buffInjuryBleeding")) return BleedColor;
        return null;
    }

    void AddPopup(EntityAlive e, int damage, bool headshot, bool buffed, Color? element, bool kill)
    {
        // The game shifts the render origin around the player (floating origin): Entity.position is the
        // logical world position, the camera works in scene space, so take the scene transform.
        // Small sideways jitter so fast hits do not stack on one spot.
        Vector3 pos = e.transform.position + new Vector3(Random.Range(-0.3f, 0.3f), e.height + 0.3f, Random.Range(-0.3f, 0.3f));

        Popup p = new Popup();
        p.Pos = pos;
        p.Text = headshot ? damage + "!" : damage.ToString();
        // A buffed hit shows the buff colour; "!" still marks a headshot
        p.Color = element ?? (buffed ? BuffedColor : (headshot ? HeadshotColor : NormalColor));
        p.Size = headshot ? 40 : (element.HasValue ? 26 : 30);
        p.Start = Time.time;
        popups.Add(p);

        if (kill && AposSettings.KillLabel)
        {
            Popup k = new Popup();
            k.Pos = pos + Vector3.up * 0.45f;
            k.Text = KillText();
            k.Color = KillColor;
            k.Size = 34;
            k.Start = p.Start;
            popups.Add(k);
        }
    }

    string KillText()
    {
        if (killText == null)
        {
            killText = Localization.Get("aposKillText");
            // Localization returns the key itself when it is missing
            if (string.IsNullOrEmpty(killText) || killText == "aposKillText") killText = "Kill!";
        }
        return killText;
    }

    void OnGUI()
    {
        if ((popups.Count == 0 && marked.Count == 0) || cam == null || Event.current.type != EventType.Repaint) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
        }

        float now = Time.time;
        Color oldColor = GUI.color;
        for (int i = 0; i < popups.Count; i++)
        {
            Popup p = popups[i];
            float t = (now - p.Start) / AposSettings.Lifetime;
            // Viewport (0..1) instead of camera pixels: the game may render at a lower resolution
            // than the screen (DLSS/FSR), which shifts WorldToScreenPoint results
            Vector3 vp = cam.WorldToViewportPoint(p.Pos + Vector3.up * (RiseHeight * t));
            if (vp.z <= 0f) continue; // behind the camera
            float x = vp.x * Screen.width;
            float y = (1f - vp.y) * Screen.height;

            float alpha = 1f - t * t;
            style.fontSize = Mathf.RoundToInt(p.Size * AposSettings.Scale);
            float w = 200f * AposSettings.Scale, h = 50f * AposSettings.Scale;
            Rect r = new Rect(x - w / 2f, y - h / 2f, w, h);

            // Dark outline for readability
            GUI.color = new Color(0f, 0f, 0f, alpha * 0.8f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), p.Text, style);
            GUI.Label(new Rect(r.x - 1f, r.y - 1f, r.width, r.height), p.Text, style);

            GUI.color = new Color(p.Color.r, p.Color.g, p.Color.b, alpha);
            GUI.Label(r, p.Text, style);
        }
        DrawMarks();
        GUI.color = oldColor;
    }

    // Crossbow Mark: a red target ring above every marked zombie, visible through walls like a Borderlands mark
    void DrawMarks()
    {
        if (marked.Count == 0) return;
        if (ringTexture == null) ringTexture = MakeRing(64);
        const float size = 34f;
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
        GUI.color = new Color(MarkColor.r, MarkColor.g, MarkColor.b, pulse);
        for (int i = 0; i < marked.Count; i++)
        {
            EntityAlive e = marked[i];
            if (e == null) continue;
            Vector3 vp = cam.WorldToViewportPoint(e.transform.position + Vector3.up * (e.height + 0.5f));
            if (vp.z <= 0f) continue;
            float x = vp.x * Screen.width;
            float y = (1f - vp.y) * Screen.height;
            GUI.DrawTexture(new Rect(x - size / 2f, y - size / 2f, size, size), ringTexture);
        }
    }

    // White ring with four inner ticks (tinted by GUI.color when drawn)
    static Texture2D MakeRing(int n)
    {
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float c = (n - 1) / 2f, outer = n / 2f - 1f, inner = outer - 5f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                bool ring = d <= outer && d >= inner;
                bool tick = (Mathf.Abs(dx) < 2f && Mathf.Abs(dy) > inner - 9f && Mathf.Abs(dy) < inner)
                         || (Mathf.Abs(dy) < 2f && Mathf.Abs(dx) > inner - 9f && Mathf.Abs(dx) < inner);
                t.SetPixel(x, y, (ring || tick) ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        t.Apply();
        return t;
    }
}

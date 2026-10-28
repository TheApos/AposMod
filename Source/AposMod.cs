// AposMod C# part. Built with Source\build.ps1 (C# 5). Settings: AposSettings.cs. Harmony patches: AposStealth.cs.
using System.Collections.Generic;
using UnityEngine;

public class AposModInit : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        AposSettings.Load(_modInstance.Path);
        try
        {
            new HarmonyLib.Harmony("apos.aposmod").PatchAll(typeof(AposModInit).Assembly);
        }
        catch (System.Exception e)
        {
            // the rest of the mod still works; the stealth rework is just off
            Debug.LogError("[AposMod] Harmony patches failed, stealth rework disabled: " + e);
        }
        GameObject go = new GameObject("AposDamageNumbers");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AposDamageNumbers>();
        go.AddComponent<AposPin>();
        go.AddComponent<AposSplinter>();
        go.AddComponent<AposAmmo>();
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
    static readonly Color ShockColor = new Color(0.7f, 0.55f, 1f);      // violet
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
        List<Entity> list = gm.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e is EntityPlayer) continue;
            if ((e.position - player.position).sqrMagnitude > Range * Range) continue;

            int id = e.entityId;
            seen.Add(id);
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
        if (popups.Count == 0 || cam == null || Event.current.type != EventType.Repaint) return;
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
        GUI.color = oldColor;
    }
}

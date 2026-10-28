// AposMod C# part. Built with Source\build.ps1 (C# 5, no Harmony needed).
using System.Collections.Generic;
using UnityEngine;

public class AposModInit : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        GameObject go = new GameObject("AposDamageNumbers");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AposDamageNumbers>();
    }
}

// Floating damage numbers. Watches the health of living non-player entities near the local
// player every frame and shows each drop as a number rising above the head.
// Headshots (yellow) are read from EntityAlive.RecordedDamage when it changed this frame.
public class AposDamageNumbers : MonoBehaviour
{
    const float Range = 60f;
    const float Lifetime = 1.5f;
    const float RiseHeight = 1.2f;

    static readonly Color NormalColor = Color.white;
    static readonly Color HeadshotColor = new Color(1f, 0.85f, 0.1f);
    static readonly Color KillColor = new Color(1f, 0.3f, 0.2f);

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

    // TEMP diagnostics (remove once damage numbers are confirmed in game)
    bool loggedCamera;
    bool loggedDraw;
    int debugHitsLogged;

    static string Describe(Camera c)
    {
        if (c == null) return "null";
        return c.name + "(enabled=" + c.enabled + ", rect=" + c.pixelRect + ", rt=" + (c.targetTexture != null) + ")";
    }

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
            return;
        }
        cam = player.playerCamera;
        if (!loggedCamera)
        {
            loggedCamera = true;
            Camera main = Camera.main;
            Debug.Log("[AposMod] DamageNumbers: playerCamera=" + Describe(cam) + " Camera.main=" + Describe(main)
                + " screen=" + Screen.width + "x" + Screen.height);
        }

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
                AddPopup(e, prevHp - hp, headshot, hp <= 0);
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
            if (now - popups[i].Start > Lifetime) popups.RemoveAt(i);
        }
    }

    void AddPopup(EntityAlive e, int damage, bool headshot, bool kill)
    {
        Popup p = new Popup();
        // The game shifts the render origin around the player (floating origin): Entity.position is the
        // logical world position, the camera works in scene space, so take the scene transform.
        // Small sideways jitter so fast hits do not stack on one spot.
        p.Pos = e.transform.position + new Vector3(Random.Range(-0.3f, 0.3f), e.height + 0.3f, Random.Range(-0.3f, 0.3f));
        p.Text = headshot ? damage + "!" : damage.ToString();
        p.Color = kill ? KillColor : (headshot ? HeadshotColor : NormalColor);
        p.Size = (headshot || kill) ? 40 : 30;
        p.Start = Time.time;
        popups.Add(p);

        // TEMP diagnostics: where the first hits would be drawn
        if (debugHitsLogged < 10 && cam != null)
        {
            debugHitsLogged++;
            Camera main = Camera.main;
            Debug.Log("[AposMod] DamageNumbers hit: " + damage + " on " + e.EntityName + " pos=" + p.Pos
                + " vp(player)=" + cam.WorldToViewportPoint(p.Pos)
                + " vp(main)=" + (main != null ? main.WorldToViewportPoint(p.Pos).ToString() : "null"));
        }
    }

    void OnGUI()
    {
        if (popups.Count == 0 || cam == null || Event.current.type != EventType.Repaint) return;
        if (!loggedDraw)
        {
            loggedDraw = true;
            Debug.Log("[AposMod] DamageNumbers: OnGUI drawing " + popups.Count + " popup(s)");
        }
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
            float t = (now - p.Start) / Lifetime;
            // Viewport (0..1) instead of camera pixels: the game may render at a lower resolution
            // than the screen (DLSS/FSR), which shifts WorldToScreenPoint results
            Vector3 vp = cam.WorldToViewportPoint(p.Pos + Vector3.up * (RiseHeight * t));
            if (vp.z <= 0f) continue; // behind the camera
            float x = vp.x * Screen.width;
            float y = (1f - vp.y) * Screen.height;

            float alpha = 1f - t * t;
            style.fontSize = p.Size;
            Rect r = new Rect(x - 100f, y - 25f, 200f, 50f);

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

// Small visual / sound feedback for mastery skills (drawn from AposMastery.OnGUI):
// - Blast(pos): a small explosion look and sound at a world point, no damage (the exploding-arrow particle, index 11)
// - Toast(text): a short line under the crosshair (e.g. "Parry!"), fades out
// - markers: a pulsing ring (orange by default, blue for the bow's Shock Trap) on an armed mine / charge until it goes off
using System.Collections.Generic;
using UnityEngine;

public static class AposFx
{
    public static void Blast(Vector3 worldPos)
    {
        try
        {
            GameManager gm = GameManager.Instance;
            if (gm != null) gm.ExplosionClient(worldPos, Quaternion.identity, 11, 0, 0f, 0f, -1, null);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AposMod] Blast effect failed: " + e.Message);
        }
    }

    // ===== toast =====
    static string toast;
    static Color toastColor;
    static float toastAt = -10f;
    static GUIStyle toastStyle;

    public static void Toast(string text, Color color)
    {
        toast = text; toastColor = color; toastAt = Time.time;
    }

    // ===== markers =====
    class Marker { public object Key; public Vector3 Pos; public EntityAlive Body; public Color Color; }
    static readonly List<Marker> markers = new List<Marker>();
    static Texture2D ring;

    public static void Mark(object key, Vector3 pos, EntityAlive body)
    {
        Mark(key, pos, body, new Color(1f, 0.55f, 0.15f));
    }

    public static void Mark(object key, Vector3 pos, EntityAlive body, Color color)
    {
        Unmark(key);
        markers.Add(new Marker { Key = key, Pos = pos, Body = body, Color = color });
    }

    public static void Unmark(object key)
    {
        for (int i = markers.Count - 1; i >= 0; i--) if (markers[i].Key == key) markers.RemoveAt(i);
    }

    public static void Clear() { markers.Clear(); toastAt = -10f; }

    public static void Draw(EntityPlayerLocal player)
    {
        if (player == null || player.playerCamera == null) return;
        Camera cam = player.playerCamera;
        Color old = GUI.color;
        if (markers.Count > 0)
        {
            if (ring == null)
            {
                ring = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float dx = (x + 0.5f) / 16f - 1f, dy = (y + 0.5f) / 16f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.7f) / 0.2f) + Mathf.Clamp01(1f - d / 0.25f);
                        ring.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                    }
                ring.Apply();
            }
            float beat = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                Marker m = markers[i];
                GUI.color = new Color(m.Color.r, m.Color.g, m.Color.b, beat);
                if (m.Body != null && m.Body.IsDead()) m.Body = null;
                // world (logical) position -> scene space for the camera
                Vector3 scene = m.Body != null ? m.Body.transform.position + Vector3.up * m.Body.height * 0.5f : m.Pos - Origin.position;
                Vector3 v = cam.WorldToViewportPoint(scene);
                if (v.z <= 0f) continue;
                float size = Mathf.Clamp(Screen.height * 0.5f / Mathf.Max(1f, v.z), 12f, 40f);
                GUI.DrawTexture(new Rect(v.x * Screen.width - size / 2f, (1f - v.y) * Screen.height - size / 2f, size, size), ring);
            }
        }
        float t = Time.time - toastAt;
        if (toast != null && t < 1.2f)
        {
            if (toastStyle == null) toastStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            toastStyle.fontSize = Mathf.RoundToInt(28f * Screen.height / 1080f * (1f + 0.25f * Mathf.Clamp01(1f - t / 0.15f)));
            float a = Mathf.Clamp01((1.2f - t) / 0.4f);
            Rect r = new Rect(0f, Screen.height * 0.5f + Screen.height * 0.09f, Screen.width, Screen.height * 0.05f);
            GUI.color = new Color(0f, 0f, 0f, 0.8f * a);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), toast, toastStyle);
            GUI.color = new Color(toastColor.r, toastColor.g, toastColor.b, a);
            GUI.Label(r, toast, toastStyle);
        }
        GUI.color = old;
    }
}

// 3D effects in the world, made from code (no asset bundle): LineRenderer lines with the additive beam material of the
// loot beams (AposElites.BeamMaterial) and point lights. Positions are logical world positions (entity.position);
// they are turned into scene space (minus Origin.position) when created. Each object animates itself and is destroyed.
public static class AposFx3D
{
    public static Vector3 Chest(EntityAlive e) { return e.position + Vector3.up * (e.height * 0.6f); }

    static GameObject Make(string name, Vector3 world)
    {
        GameObject go = new GameObject(name);
        go.transform.position = world - Origin.position;
        return go;
    }

    static LineRenderer Line(GameObject go, bool world)
    {
        Material m = AposElites.BeamMaterial();
        if (m == null) return null;
        LineRenderer l = go.AddComponent<LineRenderer>();
        l.useWorldSpace = world;
        l.material = m;
        l.textureMode = LineTextureMode.Stretch;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        l.receiveShadows = false;
        return l;
    }

    // a jagged electric arc between two points, re-shaped every 40 ms, fading out
    public static void Lightning(Vector3 from, Vector3 to, Color color, float life)
    {
        try
        {
            GameObject go = Make("AposLightning", from);
            LineRenderer core = Line(go, true);
            if (core == null) { Object.Destroy(go); return; }
            GameObject g2 = new GameObject("glow");
            g2.transform.SetParent(go.transform, false);
            LineRenderer glow = Line(g2, true);
            AposFxAnim a = go.AddComponent<AposFxAnim>();
            a.Kind = 0; a.Life = life; a.Color = color; a.A = from - Origin.position; a.B = to - Origin.position;
            a.L1 = core; a.L2 = glow;
            core.startWidth = core.endWidth = 0.05f;
            glow.startWidth = glow.endWidth = 0.35f;
            core.positionCount = glow.positionCount = 12;
            a.Step(0f);
        }
        catch (System.Exception e) { Debug.LogWarning("[AposMod] Lightning fx: " + e.Message); }
    }

    // a short burst of light
    public static void Flash(Vector3 at, Color color, float range, float intensity, float life)
    {
        try
        {
            GameObject go = Make("AposFlash", at);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            AposFxAnim a = go.AddComponent<AposFxAnim>();
            a.Kind = 1; a.Life = life; a.Color = color; a.Light = light; a.Size = intensity;
        }
        catch (System.Exception e) { Debug.LogWarning("[AposMod] Flash fx: " + e.Message); }
    }

    // a flat ring on the ground growing to the radius and fading
    public static void Shockwave(Vector3 at, float radius, Color color, float life)
    {
        try
        {
            GameObject go = Make("AposShockwave", at + Vector3.up * 0.15f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            LineRenderer l = Line(go, false);
            if (l == null) { Object.Destroy(go); return; }
            l.alignment = LineAlignment.TransformZ;
            l.loop = true;
            l.positionCount = 48;
            AposFxAnim a = go.AddComponent<AposFxAnim>();
            a.Kind = 2; a.Life = life; a.Color = color; a.L1 = l; a.Size = radius;
            a.Step(0f);
        }
        catch (System.Exception e) { Debug.LogWarning("[AposMod] Shockwave fx: " + e.Message); }
    }

    // the bow's Shock Trap / the baton's mass stun: a flash, a ring and an arc to every zombie hit
    public static void Discharge(Vector3 at, float radius, System.Collections.Generic.IEnumerable<EntityAlive> hit)
    {
        Color blue = new Color(0.45f, 0.8f, 1f);
        Flash(at, blue, radius * 2.5f, 6f, 0.35f);
        Shockwave(at, radius, blue, 0.4f);
        foreach (EntityAlive e in hit) if (e != null) Lightning(at, Chest(e), blue, 0.3f);
    }

    // Sticky Charge / Minefield: an orange flash and a ring
    public static void Boom(Vector3 at, float radius)
    {
        Color orange = new Color(1f, 0.55f, 0.15f);
        Flash(at, orange, radius * 3f, 8f, 0.4f);
        Shockwave(at, radius, orange, 0.45f);
    }
}

public class AposFxAnim : MonoBehaviour
{
    public int Kind;          // 0 lightning, 1 light, 2 ring
    public float Life, Size;
    public Color Color;
    public Vector3 A, B;
    public LineRenderer L1, L2;
    public Light Light;
    float age, reshape;

    void Update()
    {
        age += Time.deltaTime;
        if (age >= Life) { Destroy(gameObject); return; }
        Step(age / Life);
    }

    public void Step(float t)
    {
        float fade = 1f - t;
        if (Kind == 0)
        {
            reshape -= Time.deltaTime;
            if (reshape <= 0f)
            {
                reshape = 0.04f;
                Vector3 d = B - A;
                Vector3 side = Vector3.Cross(d, Vector3.up);
                if (side.sqrMagnitude < 0.001f) side = Vector3.right;
                side.Normalize();
                Vector3 up2 = Vector3.Cross(side, d).normalized;
                float amp = Mathf.Min(0.6f, d.magnitude * 0.08f);
                int n = L1.positionCount;
                for (int i = 0; i < n; i++)
                {
                    float k = i / (float)(n - 1);
                    Vector3 p = A + d * k;
                    if (i > 0 && i < n - 1) p += side * Random.Range(-amp, amp) + up2 * Random.Range(-amp, amp);
                    L1.SetPosition(i, p);
                    if (L2 != null) L2.SetPosition(i, p);
                }
            }
            Color core = Color.Lerp(Color, Color.white, 0.7f);
            L1.startColor = L1.endColor = new Color(core.r, core.g, core.b, fade);
            if (L2 != null) L2.startColor = L2.endColor = new Color(Color.r, Color.g, Color.b, 0.45f * fade);
        }
        else if (Kind == 1)
        {
            if (Light != null) Light.intensity = Size * fade * fade;
        }
        else
        {
            float r = Mathf.Lerp(0.3f, Size, Mathf.Sqrt(t));
            int n = L1.positionCount;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n * Mathf.PI * 2f;
                L1.SetPosition(i, new Vector3(Mathf.Cos(u) * r, Mathf.Sin(u) * r, 0f));
            }
            L1.startWidth = L1.endWidth = 0.2f * (0.5f + fade);
            L1.startColor = L1.endColor = new Color(Color.r, Color.g, Color.b, 0.9f * fade);
        }
    }
}

// aposfx shock|boom|bolt -> show an effect 4 m in front of you (to look at the effects without setting up a fight)
public class AposFxCommand : ConsoleCmdAbstract
{
    public override string[] getCommands() { return new string[] { "aposfx" }; }

    public override string getDescription() { return "AposMod: show an effect in front of you: aposfx shock|boom|bolt"; }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal p = gm != null && gm.World != null ? gm.World.GetPrimaryPlayer() : null;
        if (p == null) return;
        Vector3 f = p.GetLookVector(); f.y = 0f; f.Normalize();
        Vector3 at = p.position + f * 4f + Vector3.up * 0.5f;
        string what = _params.Count > 0 ? _params[0] : "shock";
        if (what == "boom") { AposFx.Blast(at); AposFx3D.Boom(at, 3f); }
        else if (what == "bolt") AposFx3D.Lightning(p.getHeadPosition() + f, at, new Color(0.45f, 0.8f, 1f), 0.5f);
        else
        {
            AposFx3D.Discharge(at, 3f, new List<EntityAlive>());
            Color blue = new Color(0.45f, 0.8f, 1f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2f;
                AposFx3D.Lightning(at, at + new Vector3(Mathf.Cos(a) * 2.5f, 0.8f, Mathf.Sin(a) * 2.5f), blue, 0.35f);
            }
        }
    }
}

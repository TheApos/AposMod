// Small visual / sound feedback for mastery skills (drawn from AposMastery.OnGUI):
// - Blast(pos): a small explosion look and sound at a world point, no damage (the exploding-arrow particle, index 11)
// - Toast(text): a short line under the crosshair (e.g. "Parry!"), fades out
// - markers: a pulsing orange ring on an armed mine / sticky charge until it goes off
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
    class Marker { public object Key; public Vector3 Pos; public EntityAlive Body; }
    static readonly List<Marker> markers = new List<Marker>();
    static Texture2D ring;

    public static void Mark(object key, Vector3 pos, EntityAlive body)
    {
        Unmark(key);
        markers.Add(new Marker { Key = key, Pos = pos, Body = body });
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
            GUI.color = new Color(1f, 0.55f, 0.15f, beat);
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                Marker m = markers[i];
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

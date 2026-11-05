// Agility stealth perks, level 5:
// - Hidden Strike 5 "Shadow Strike" (the sneak-damage line): crouch for 3 s without being noticed (no zombie has you as
//   its attack target) and without hitting anything -> buffAposShadow. Your next weapon hit deals x2 and uses it up;
//   if that hit kills, the Shadow comes back at once, so a room can be cleared one by one. Standing up or being
//   noticed removes it. The damage is doubled in AposCombatDamagePatch (AposCombat.cs).
// - From the Shadows 5 "Hunter in the Dark" (the stealth line): at night or in a dark place (_lightlevel <= DarkLight)
//   you sense zombies within 20 m through walls: a dim, slowly beating red glow on their bodies; sleeping ones are
//   a faint cold blue. (Silent Shadow, the crouch speed of the same level, is in AposSurvival.cs.)
using System.Collections.Generic;
using UnityEngine;

public class AposShadow : MonoBehaviour
{
    public const string Buff = "buffAposShadow";
    const float CalmTime = 3f;
    const float NoticeRange = 40f;
    const float HunterRange = 20f;
    const float DarkLight = 50f;  // vanilla stealth counts <= 65 as dark; a bit darker here
    const float ScanInterval = 0.2f;

    static float calmSince;
    static int strikeTarget = -1;

    readonly List<EntityAlive> hunted = new List<EntityAlive>();
    float nextScan;
    bool noticed;
    Camera cam;
    Texture2D glow;

    static EntityPlayerLocal Player()
    {
        GameManager gm = GameManager.Instance;
        return gm != null && gm.World != null ? gm.World.GetPrimaryPlayer() : null;
    }

    static bool HasStrike(EntityAlive p)
    {
        return AposAffixes.Level(p, "perkHiddenStrike") >= 5;
    }

    static bool HasHunter(EntityAlive p)
    {
        return AposAffixes.Level(p, "perkFromTheShadows") >= 5;
    }

    void Update()
    {
        EntityPlayerLocal p = Player();
        if (p == null || p.IsDead())
        {
            calmSince = Time.time;
            hunted.Clear();
            cam = null;
            return;
        }
        cam = p.playerCamera;
        bool strike = HasStrike(p), hunter = HasHunter(p);
        bool has = p.Buffs.HasBuff(Buff);
        if (!strike && has) p.Buffs.RemoveBuff(Buff);
        if (!strike && !hunter)
        {
            hunted.Clear();
            return;
        }

        if (Time.time >= nextScan)
        {
            nextScan = Time.time + ScanInterval;
            Scan(p, hunter);
        }
        if (!strike) return;

        if (!p.IsCrouching || noticed)
        {
            calmSince = Time.time;
            if (has) p.Buffs.RemoveBuff(Buff);
        }
        else if (!has && Time.time - calmSince >= CalmTime)
        {
            p.Buffs.AddBuff(Buff);
        }
    }

    // Who is hunting us, and which zombies to sense
    void Scan(EntityPlayerLocal p, bool hunter)
    {
        noticed = false;
        hunted.Clear();
        World world = GameManager.Instance.World;
        bool dark = hunter && (world.IsDark() || p.Buffs.GetCustomVar("_lightlevel") <= DarkLight);
        List<Entity> list = world.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityEnemy e = list[i] as EntityEnemy;
            if (e == null || e.IsDead()) continue;
            float d2 = (e.position - p.position).sqrMagnitude;
            if (d2 > NoticeRange * NoticeRange) continue;
            if (e.GetAttackTarget() == p) noticed = true;
            if (dark && d2 <= HunterRange * HunterRange) hunted.Add(e);
        }
    }

    // A hit by the local player (AposCombatDamagePatch prefix): resets the calm timer; with the Shadow, x2 damage
    public static int OnPlayerHit(EntityPlayer attacker, EntityAlive target, DamageSource source, int strength)
    {
        if (attacker != Player()) return strength;
        calmSince = Time.time;
        if (!attacker.Buffs.HasBuff(Buff) || !IsWeaponHit(source)) return strength;
        attacker.Buffs.RemoveBuff(Buff);
        strikeTarget = target.entityId;
        return strength * 2;
    }

    // After the hit: a kill by the Shadow Strike gives the Shadow back
    public static void AfterHit(EntityPlayer attacker, EntityAlive target, bool fatal)
    {
        if (strikeTarget != target.entityId) return;
        strikeTarget = -1;
        if (fatal && attacker != null && !attacker.IsDead() && HasStrike(attacker)) attacker.Buffs.AddBuff(Buff);
    }

    // Weapon damage only: burning/shock/bleeding ticks must not use up the Shadow
    public static bool IsWeaponHit(DamageSource source)
    {
        switch (source.GetDamageType())
        {
            case EnumDamageTypes.Piercing:
            case EnumDamageTypes.Slashing:
            case EnumDamageTypes.Bashing:
            case EnumDamageTypes.Crushing:
                return true;
        }
        return false;
    }

    // Soft elliptical glow, bright in the middle and fading to nothing at the edge (like a heat haze)
    static Texture2D MakeGlow(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
            }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (hunted.Count == 0 || cam == null || Event.current.type != EventType.Repaint) return;
        if (glow == null) glow = MakeGlow(64, 128);
        Color old = GUI.color;
        // a slow heartbeat: two quick pulses, then a pause
        float t = Time.time % 1.2f;
        float beat = Mathf.Max(Mathf.Exp(-t * 14f), 0.7f * Mathf.Exp(-Mathf.Max(0f, t - 0.22f) * 14f) * (t > 0.22f ? 1f : 0f));
        for (int i = 0; i < hunted.Count; i++)
        {
            EntityAlive e = hunted[i];
            if (e == null || e.IsDead()) continue;
            // scene-space positions (floating origin)
            Vector3 feet = cam.WorldToViewportPoint(e.transform.position);
            Vector3 head = cam.WorldToViewportPoint(e.transform.position + Vector3.up * e.height);
            if (feet.z <= 0f || head.z <= 0f) continue;
            float yTop = (1f - head.y) * Screen.height, yBottom = (1f - feet.y) * Screen.height;
            float h = (yBottom - yTop) * 1.25f;
            if (h < 6f) continue;
            float w = h * 0.55f, cx = feet.x * Screen.width, cy = (yTop + yBottom) / 2f;
            // farther = fainter
            float near = Mathf.Clamp01(1.15f - Mathf.Sqrt((e.position - cam.transform.position).sqrMagnitude) / HunterRange);
            if (e.sleepingOrWakingUp) GUI.color = new Color(0.35f, 0.55f, 0.85f, 0.22f * near);
            else GUI.color = new Color(0.85f, 0.12f, 0.08f, (0.28f + 0.22f * beat) * near);
            GUI.DrawTexture(new Rect(cx - w / 2f, cy - h / 2f, w, h), glow);
        }
        GUI.color = old;
    }
}

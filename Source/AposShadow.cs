// From the Shadows 5, on top of "Silent Shadow" (crouch speed, AposSurvival.cs):
// - "Shadow Strike": crouch for 3 s without being noticed (no zombie has you as its attack target) and without
//   hitting anything -> buffAposShadow. Your next weapon hit deals x2 and uses it up; if that hit kills, the Shadow
//   comes back at once, so a room can be cleared one by one. Standing up or being noticed removes it.
//   The damage is doubled in AposCombatDamagePatch (AposCombat.cs).
// - "Hunter in the Dark": at night or in a dark place (_lightlevel <= DarkLight) zombies within 20 m are drawn as
//   outlines through walls; sleeping ones are dimmer.
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
    Texture2D pixel;

    static EntityPlayerLocal Player()
    {
        GameManager gm = GameManager.Instance;
        return gm != null && gm.World != null ? gm.World.GetPrimaryPlayer() : null;
    }

    static bool HasPerk(EntityAlive p)
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
        bool perk = HasPerk(p);
        bool has = p.Buffs.HasBuff(Buff);
        if (!perk)
        {
            if (has) p.Buffs.RemoveBuff(Buff);
            hunted.Clear();
            return;
        }

        if (Time.time >= nextScan)
        {
            nextScan = Time.time + ScanInterval;
            Scan(p);
        }

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

    // Who is hunting us, and which zombies to outline
    void Scan(EntityPlayerLocal p)
    {
        noticed = false;
        hunted.Clear();
        World world = GameManager.Instance.World;
        bool dark = world.IsDark() || p.Buffs.GetCustomVar("_lightlevel") <= DarkLight;
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
        if (fatal && attacker != null && !attacker.IsDead() && HasPerk(attacker)) attacker.Buffs.AddBuff(Buff);
    }

    // Weapon damage only: burning/shock/bleeding ticks must not use up the Shadow
    static bool IsWeaponHit(DamageSource source)
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

    void OnGUI()
    {
        if (hunted.Count == 0 || cam == null || Event.current.type != EventType.Repaint) return;
        if (pixel == null)
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }
        Color old = GUI.color;
        for (int i = 0; i < hunted.Count; i++)
        {
            EntityAlive e = hunted[i];
            if (e == null || e.IsDead()) continue;
            // scene-space positions (floating origin), viewport -> screen like the damage numbers used to
            Vector3 feet = cam.WorldToViewportPoint(e.transform.position);
            Vector3 head = cam.WorldToViewportPoint(e.transform.position + Vector3.up * e.height);
            if (feet.z <= 0f || head.z <= 0f) continue;
            float yTop = (1f - head.y) * Screen.height, yBottom = (1f - feet.y) * Screen.height;
            float h = yBottom - yTop;
            if (h < 4f) continue;
            float w = h * 0.4f, x = feet.x * Screen.width - w / 2f;
            float alpha = e.sleepingOrWakingUp ? 0.35f : 0.8f;
            GUI.color = new Color(0.75f, 0.55f, 1f, alpha);
            DrawFrame(new Rect(x, yTop, w, h), 2f);
        }
        GUI.color = old;
    }

    void DrawFrame(Rect r, float t)
    {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), pixel);
        GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), pixel);
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), pixel);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), pixel);
    }
}

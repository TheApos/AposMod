// Weapon Mastery 2.0, batons (docs/Design-Mastery2.md 5й; driven by AposMastery.cs):
// - Class trait "Chain Lightning": a normal hit shocks the target for 0.5 s; a power hit is a mass stun: every zombie
//   within 3 m is shocked and stunned for 2 s (10 s cooldown, power hits +50% stamina). (It used to be XML scaled by
//   Calculated Attack.) Our shock expiry is kept on the zombie (aposShockUntil) for Discharge.
// - Branch A (Storm, crowds): Wide Arc, Sustained Charge, Arc Jump, Capacitor (x Intellect), Tesla Cage, Crowd Feed,
//   Short Circuit, capstone Thunderstorm.
// - Branch B (Overload, one target): Live Wire, Hold the Current, Breakdown, Isolation, Cardiac Arrest,
//   Residual Charge, Discharge, capstone High Voltage.
// - Hybrids: Grounding, Technician, Tesla Coil.
using System.Collections.Generic;
using UnityEngine;

public static class AposBaton
{
    static readonly float[] Radius = { 3f, 3.5f, 4f, 5f };
    static readonly float[] Stun = { 2f, 3f, 4f, 5f };
    static readonly int[] Jumps = { 0, 1, 2, 4 };
    static readonly float[] Capacitor = { 0f, 2f, 3f, 5f };      // % per Intellect level, max 50
    static readonly float[] CrowdFeed = { 0f, 2f, 5f };
    static readonly float[] ShortCircuit = { 0f, 2f, 5f };
    static readonly float[] LiveWire = { 0f, 0.1f, 0.2f, 0.4f };
    static readonly float[] NormalShock = { 0.5f, 0.75f, 1f, 1.5f };
    static readonly float[] Breakdown = { 0f, 0.5f, 1f, 2f };
    static readonly float[] Isolation = { 0f, 0.15f, 0.30f, 0.50f };
    static readonly int[] Residual = { 0, 1, 3 };
    static readonly float[] Discharge = { 0f, 0.25f, 0.6f };

    static int R(string id) { return AposMastery.Rank(AposMastery.Batons, id); }

    static float ready, cageUntil;
    static Vector3 cagePos;
    static float cageRadius;
    static int bdTarget = -1, bdHits;
    static readonly HashSet<int> caged = new HashSet<int>();

    public static void Reset() { ready = cageUntil = 0f; bdTarget = -1; bdHits = 0; caged.Clear(); }

    static float Cooldown(EntityPlayerLocal p)
    {
        return 10f * (1f - Mathf.Min(50f, Capacitor[R("A4")] * AposAffixes.Level(p, "attIntellect")) / 100f);
    }
    static float StunRadius() { return Radius[R("A1")] * (R("B8") > 0 ? 0.5f : 1f); }

    public static bool Shocked(EntityAlive e) { return e.Buffs.HasBuff("buffShocked"); }

    static void Shock(EntityAlive e, float seconds)
    {
        e.Buffs.AddBuff("buffShocked", -1, true, false, seconds);
        e.Buffs.SetCustomVar("aposShockUntil", Mathf.Max(e.Buffs.GetCustomVar("aposShockUntil"), Time.time + seconds));
    }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        // Tesla Cage: 3 s after the mass stun, anyone who walks into the radius is shocked for 1 s
        if (Time.time < cageUntil)
        {
            foreach (EntityAlive e in AposMelee.Around(cagePos, cageRadius))
                if (caged.Add(e.entityId)) Shock(e, 1f);
        }
        if (!held) return;
        AposMastery.SetStat(AposMastery.StatStamPower, 0.5f);
        float left = ready - Time.time;
        AposHud.Add(false, "electric_power", left > 0f ? Mathf.CeilToInt(left) + "s" : "OK", left > 0f ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.4f, 0.8f, 1f), left <= 0f, "Base");
        if (R("B3") > 0 && bdHits > 0)
        {
            AposHud.Add(false, "hard_target", bdHits + "/4", new Color(1f, 0.5f, 0.3f), bdHits >= 3, "B3");
            AposStatus.Target(bdTarget, "hard_target", bdHits + "/4", new Color(1f, 0.5f, 0.3f));
        }
    }

    // incoming damage for the player: Grounding (zombies you shocked hit 30% weaker)
    public static int Incoming(EntityAlive attacker, int strength)
    {
        if (R("H1") > 0 && attacker != null && Shocked(attacker)) return Mathf.RoundToInt(strength * 0.7f);
        return strength;
    }

    // Technician: your turrets and drone deal +200% to shocked zombies (AposCombat)
    public static bool Technician() { return R("H2") > 0; }

    // ===== hits =====
    static int lastTarget = -1;
    static bool lastPower, lastShocked, lastBreak;

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        bool power = AposMelee.Power(player);
        bool shocked = Shocked(target);
        float mult = 1f;
        if (shocked) mult += LiveWire[R("B1")];
        if (R("B4") > 0 && AposMelee.Nearest(target.position, 4f, target, 1).Count == 0) mult += Isolation[R("B4")];
        // Thunderstorm downside: no other zombie within 5 m -> -25%
        if (R("A8") > 0 && AposMelee.Nearest(player.position, 5f, target, 1).Count == 0) mult -= 0.25f;
        lastBreak = false;
        if (!power)
        {
            // Breakdown: every 4th normal hit in a row on the same zombie
            if (target.entityId == bdTarget) bdHits++;
            else { bdTarget = target.entityId; bdHits = 1; }
            if (R("B3") > 0 && bdHits >= 4) { mult += Breakdown[R("B3")]; bdHits = 0; lastBreak = true; }
            if (R("B8") > 0 && shocked) mult += 1f;           // High Voltage
        }
        else if (R("B7") > 0 && shocked)
        {
            // Discharge: +X% per second of shock left, the shock burns out
            float leftSec = Mathf.Max(0f, target.Buffs.GetCustomVar("aposShockUntil") - Time.time);
            mult += Discharge[R("B7")] * leftSec;
            target.Buffs.RemoveBuff("buffShocked");
        }
        lastTarget = target.entityId; lastPower = power; lastShocked = shocked;
        // Cardiac Arrest: a shocked zombie at 25% health or less dies from the next hit (an elite takes +100%)
        if (R("B5") > 0 && shocked && target.Health <= target.GetMaxHealth() * 0.25f)
        {
            if (!AposElites.IsElite(target)) return target.GetMaxHealth() * 2;
            mult += 1f;
        }
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || target.entityId != lastTarget) return;
        lastTarget = -1;
        if (response.Fatal)
        {
            if (R("B6") > 0) { bdTarget = -1; bdHits = Residual[R("B6")]; }     // Residual Charge: the next zombie starts higher
            if (lastShocked) ShortCircuitAt(player, target.position);
        }
        else if (!lastPower)
        {
            float t = NormalShock[R("B2")] + (R("B8") > 0 && lastShocked ? 0.5f : 0f);
            Shock(target, t);
        }
        // the mass stun: a power hit when ready; Tesla Coil: every Breakdown fires it without the cooldown
        if ((lastPower && Time.time >= ready) || (lastBreak && R("H3") > 0)) MassStun(player, target, Mathf.Max(1, response.Strength));
    }

    static void ShortCircuitAt(EntityPlayerLocal player, Vector3 pos)
    {
        if (R("A7") <= 0) return;
        foreach (EntityAlive e in AposMelee.Around(pos, ShortCircuit[R("A7")])) Shock(e, 1f);
    }

    static void MassStun(EntityPlayerLocal player, EntityAlive center, int blow)
    {
        ready = Time.time + Cooldown(player);
        float radius = StunRadius(), stun = Stun[R("A2")];
        List<EntityAlive> hit = new List<EntityAlive>(AposMelee.Around(center.position, radius));
        if (!hit.Contains(center) && !center.IsDead()) hit.Add(center);
        // Arc Jump: from the shocked ones to zombies beyond the radius (within 4 m of a shocked one)
        int jumps = Jumps[R("A3")];
        if (jumps > 0)
        {
            List<EntityAlive> extra = new List<EntityAlive>();
            foreach (EntityAlive s in hit)
                foreach (EntityAlive e in AposMelee.Nearest(s.position, 4f, s, 8))
                    if (extra.Count < jumps && !hit.Contains(e) && !extra.Contains(e)) extra.Add(e);
            hit.AddRange(extra);
        }
        AposFx3D.Discharge(AposFx3D.Chest(center), radius, hit);
        int stormDmg = R("A8") > 0 ? Mathf.Max(1, Mathf.RoundToInt(blow * 0.4f)) : 0;
        int fed = 0;
        bool killed = false;
        foreach (EntityAlive e in hit)
        {
            if (stormDmg > 0 && e != center)
            {
                AposSplash.Deal(e, stormDmg, player.entityId, EnumDamageTypes.Electrical);
                if (e.IsDead()) { killed = true; ShortCircuitAt(player, e.position); continue; }
            }
            Shock(e, stun);
            e.Buffs.AddBuff("buffInjuryStunned01", -1, true, false, stun);
            fed++;
        }
        if (R("A6") > 0 && fed > 0) player.AddStamina(CrowdFeed[R("A6")] * fed);
        if (killed) ready = Time.time;                        // Thunderstorm: a kill by it resets the cooldown
        if (R("A5") > 0)
        {
            cageUntil = Time.time + 3f; cagePos = center.position; cageRadius = radius;
            caged.Clear();
            foreach (EntityAlive e in hit) caged.Add(e.entityId);
        }
    }
}

// Weapon Mastery 2.0, clubs and bats (docs/Design-Mastery2.md 5є; driven by AposMastery.cs):
// - Class trait "Home Run": a normal hit stuns for 1 s with a 15% chance, a power hit to the head launches the zombie
//   (force 800; full Bruiser set +300). A stunned zombie cannot be stunned again for 3 s (buffAposStunCD).
//   (It used to be XML scaled by Pummel Pete.)
// - Branch A (power hit and control): Stunning Blow, Wind-Up (x Strength), Hard Landing, Kick Them While Down,
//   Concussion, Easy Swing, Batting Streak, capstone Grand Slam.
// - Branch B (Rage, from the old Rage perk): Rage, Heavy Hands, Frenzy, Tireless, Rampage, Hold the Rage, Rage Wave,
//   capstone Berserk. Rage = buffAposRage with aposRageStacks / aposRageTime (Rampage: aposRampage = 1).
// - Hybrids: Stunning Rage, Angry Home Run, Crowd Breaker.
using System.Collections.Generic;
using UnityEngine;

public static class AposClub
{
    const float HomeRunForce = 800f, BruiserForce = 300f, LaunchTime = 3f;
    static readonly float[] StunChance = { 0.15f, 0.20f, 0.25f, 0.40f };
    static readonly float[] WindUp = { 0f, 20f, 35f, 60f };       // x Strength, max 600
    static readonly float[] Landing = { 0f, 1.5f, 2f, 3f };
    static readonly float[] OnStunned = { 0f, 0.2f, 0.4f, 0.8f };
    static readonly float[] EasySwing = { 0f, 0.2f, 0.5f };
    static readonly float[] Streak = { 0f, 0.15f, 0.40f };
    static readonly int[] RageCap = { 0, 2, 4, 8 };
    static readonly float[] HeavyHands = { 0f, 0.1f, 0.2f, 0.4f };
    static readonly float[] Frenzy = { 0.10f, 0.15f, 0.20f, 0.30f };
    static readonly float[] Tireless = { 0f, 0.15f, 0.30f, 0.60f };
    static readonly float[] RageTime = { 8f, 12f, 20f };
    static readonly float[] Wave = { 0f, 3f, 5f };

    static int R(string id) { return AposMastery.Rank(AposMastery.Clubs, id); }

    static int streak;
    static float lastPowerHit = -10f, nextWave;

    public static void Reset() { streak = 0; lastPowerHit = -10f; nextWave = 0f; flights.Clear(); }

    static int Cap(EntityPlayerLocal p)
    {
        int cap = RageCap[R("B1")] * (R("B8") > 0 ? 2 : 1);
        return cap > 0 && p.Buffs.HasBuff("buffBikerSetBonus") ? cap + 1 : cap;   // full Bruiser set: one more stack
    }
    static int Stacks(EntityPlayerLocal p) { return p.Buffs.HasBuff("buffAposRage") ? (int)p.Buffs.GetCustomVar("aposRageStacks") : 0; }
    static bool MaxRage(EntityPlayerLocal p) { int c = Cap(p); return c > 0 && Stacks(p) >= c; }

    static void AddRage(EntityPlayerLocal p, int n, bool refresh)
    {
        int cap = Cap(p);
        if (cap <= 0) return;
        p.Buffs.SetCustomVar("aposRageStacks", Mathf.Min(cap, Stacks(p) + n));
        if (refresh || n > 0) p.Buffs.SetCustomVar("aposRageTime", RageTime[R("B6")]);
        if (!p.Buffs.HasBuff("buffAposRage")) p.Buffs.AddBuff("buffAposRage");
    }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        RunFlights(player);
        bool rampage = held && R("B5") > 0 && MaxRage(player);
        if (player.Buffs.GetCustomVar("aposRampage") != (rampage ? 1f : 0f)) player.Buffs.SetCustomVar("aposRampage", rampage ? 1f : 0f);
        if (!held) return;
        int s = Stacks(player);
        AposMastery.SetStat(AposMastery.StatSpeed, s * 0.05f);
        // stamina: power hits +30% (anti-spam), Easy Swing, Grand Slam +50%; normal hits: Tireless
        AposMastery.SetStat(AposMastery.StatStamPower, 0.3f - EasySwing[R("A6")] + (R("A8") > 0 ? 0.5f : 0f));
        AposMastery.SetStat(AposMastery.StatStamNormal, -Tireless[R("B4")]);
        // Rage: the stacks and the damage they give; their attack speed (+5% each) goes to the energy column
        if (Cap(player) > 0) AposHud.Gauge(false, s, Cap(player), new Color(1f, 0.3f, 0.1f), MaxRage(player), "berserker", s + "/" + Cap(player) + "  +" + Mathf.RoundToInt(s * Frenzy[R("B3")] * 100f) + "%", "B1");
        if (s > 0) AposHud.AddAttackSpeed("ui_apos_ragespeed", s * 0.05f, "", "B1");
        // Batting Streak: the left arc, 3 links; the number is what the NEXT power hit gets (while the 3 s window is open)
        if (R("A7") > 0 && Time.time - lastPowerHit < 3f)
        {
            int next = Mathf.Min(3, streak + 1);
            AposHud.Gauge(true, streak, 3, new Color(1f, 0.82f, 0.3f), streak >= 3, "bat", "+" + Mathf.RoundToInt(Streak[R("A7")] * next * 100f) + "%", "A7");
        }
    }

    // incoming damage for the player (Berserk: +25%)
    public static float Incoming(EntityPlayerLocal p)
    {
        return AposMastery.HeldClass == AposMastery.Clubs && R("B8") > 0 ? 1.25f : 1f;
    }

    // ===== hits =====
    static int lastTarget = -1;
    static bool lastPower, lastHead, lastStunned;

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        bool power = AposMelee.Power(player);
        bool head = (source.GetEntityDamageBodyPart(target) & EnumBodyPartHit.Head) != 0;
        bool stunned = AposMelee.Stunned(target);
        float mult = 1f + Stacks(player) * Frenzy[R("B3")];
        if (!power) mult += HeavyHands[R("B2")];
        if (power && stunned) mult += OnStunned[R("A4")];
        if (power && R("A7") > 0)
        {
            streak = Time.time - lastPowerHit <= 3f ? Mathf.Min(3, streak + 1) : 0;
            mult += Streak[R("A7")] * streak;
            lastPowerHit = Time.time;
        }
        lastTarget = target.entityId; lastPower = power; lastHead = head; lastStunned = stunned;
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || target.entityId != lastTarget) return;
        lastTarget = -1;
        bool maxRage = MaxRage(player);
        if (R("B8") > 0 && Stacks(player) >= 4) player.AddHealth(Stacks(player) / 4);     // Berserk heals
        if (response.Fatal)
        {
            AddRage(player, lastPower && R("H3") > 0 ? 2 : 1, true);
            // Rage Wave: a kill at max Rage stuns everyone around (once per 5 s)
            if (R("B7") > 0 && maxRage && Time.time >= nextWave)
            {
                nextWave = Time.time + 5f;
                foreach (EntityAlive e in AposMelee.Around(target.position, Wave[R("B7")]))
                {
                    Stun(player, e);
                    if (R("B7") >= 2) AposKnock.Ragdoll(e, target.position, 250f, 1f);
                }
            }
            return;
        }
        if (lastPower)
        {
            // Home Run: a power hit to the head launches; Grand Slam: always, elites too; Angry Home Run: any hit at max Rage
            bool elite = AposElites.IsElite(target);
            bool launch = (lastHead || R("A8") > 0 || (R("H2") > 0 && maxRage)) && (!elite || R("A8") > 0);
            if (launch) Launch(player, target, dealt, maxRage);
        }
        else
        {
            // normal hit: a chance to stun; Crowd Breaker at max Rage: always
            if (Random.value < StunChance[R("A1")] || (R("H3") > 0 && maxRage)) Stun(player, target);
        }
    }

    static void Stun(EntityPlayerLocal player, EntityAlive e)
    {
        if (e.IsDead() || e.Buffs.HasBuff("buffAposStunCD")) return;
        float time = R("A5") > 0 ? 2f : 1f;
        e.Buffs.AddBuff("buffInjuryStunned01", -1, true, false, time);
        e.Buffs.AddBuff("buffAposStunCD", -1, true, false, time + 3f);
        if (R("A5") > 0) e.Buffs.AddBuff("buffAposConcussed", -1, true, false, time);   // +50% from everything (AposCombat)
        if (R("H1") > 0) AddRage(player, 1, true);                                      // Stunning Rage
    }

    // ===== Home Run flights: Hard Landing and the Grand Slam projectile =====
    class Flight { public EntityAlive Body; public float Until, LandAt; public int Damage; public bool Wide; public HashSet<int> Hit; }
    static readonly List<Flight> flights = new List<Flight>();

    static void Launch(EntityPlayerLocal player, EntityAlive target, int dealt, bool maxRage)
    {
        float force = HomeRunForce + Mathf.Min(600f, WindUp[R("A2")] * AposAffixes.Level(player, "attStrength"))
            + (player.Buffs.HasBuff("buffBikerSetBonus") ? BruiserForce : 0f);
        AposKnock.Ragdoll(target, player.position, force, LaunchTime);
        if (R("A3") > 0 || R("A8") > 0)
            flights.Add(new Flight { Body = target, Until = Time.time + 1.2f, LandAt = Time.time + 0.9f, Damage = dealt, Wide = R("H2") > 0 && maxRage, Hit = new HashSet<int>() });
    }

    static void RunFlights(EntityPlayerLocal player)
    {
        for (int i = flights.Count - 1; i >= 0; i--)
        {
            Flight f = flights[i];
            if (f.Body == null) { flights.RemoveAt(i); continue; }
            // Grand Slam: the flying zombie hits everyone it passes: 100% of the blow and knocked down
            if (R("A8") > 0)
            {
                foreach (EntityAlive e in AposMelee.Around(f.Body.position, 1.3f))
                {
                    if (e == f.Body || f.Hit.Contains(e.entityId)) continue;
                    f.Hit.Add(e.entityId);
                    AposSplash.Deal(e, Mathf.Max(1, f.Damage), player.entityId, EnumDamageTypes.Bashing);
                    if (!e.IsDead()) AposKnock.Ragdoll(e, f.Body.position, 300f, 1.5f);
                }
            }
            if (f.LandAt > 0f && Time.time >= f.LandAt)
            {
                f.LandAt = -1f;
                int a3 = R("A3");
                if (a3 > 0)
                {
                    float r = Landing[a3] * (f.Wide ? 2f : 1f);
                    foreach (EntityAlive e in AposMelee.Around(f.Body.position, r))
                    {
                        if (e == f.Body) continue;
                        AposKnock.Ragdoll(e, f.Body.position, 300f, 1.5f);
                        if (a3 >= 3) AposSplash.Deal(e, Mathf.Max(1, f.Damage / 2), player.entityId, EnumDamageTypes.Bashing);
                    }
                }
            }
            if (Time.time >= f.Until) flights.RemoveAt(i);
        }
    }
}

// Weapon Mastery 2.0, sledgehammers (docs/Design-Mastery2.md 5ж; driven by AposMastery.cs):
// - Class trait "Earthquake": a normal hit knocks the target down with a 20% chance; a power hit knocks down every
//   zombie within 3 m (full Bruiser set +1 m) and deals each 10% of its damage (10 s cooldown; power hits +50%
//   stamina). (It used to be XML scaled by Skull Crusher.)
// - Branch A (Earthquake, crowds): Wide Quake, Shockforce, Frequent Quakes, Stagger, Elemental Quake,
//   Trembling Ground, Rift, capstone Aftershock.
// - Branch B (one target): Heavy Hammer, Iron Lungs (x Fortitude), Armor Break, Momentum, Overkill, Crush, Duel,
//   capstone Execution.
// - Hybrids: Fault Line, Heavy Quake, Doomsday.
using System.Collections.Generic;
using UnityEngine;

public static class AposSledge
{
    const float BaseDown = 2.5f;
    static readonly float[] QuakeRadius = { 3f, 3.5f, 4f, 6f };
    static readonly float[] Shockforce = { 0.1f, 0.2f, 0.3f, 0.5f };
    static readonly float[] Cooldown = { 10f, 8f, 6f, 3f };
    static readonly float[] Stagger = { 0.20f, 0.25f, 0.30f, 0.45f };
    static readonly float[] Trembling = { 0f, 1f, 2.5f };
    static readonly float[] Rift = { 0f, 0.3f, 0.6f };
    static readonly float[] Heavy = { 0f, 0.2f, 0.4f, 0.8f };
    static readonly float[] IronLungs = { 0f, 2f, 4f, 8f };       // % per Fortitude level, max 80
    static readonly float[] ArmorBreak = { 0f, 0.1f, 0.2f, 0.4f };
    static readonly float[] Momentum = { 0f, 0.10f, 0.15f, 0.25f };
    static readonly float[] Crush = { 0f, 0.5f, 1.2f };
    static readonly float[] Duel = { 0f, 0.6f, 1.5f };

    static int R(string id) { return AposMastery.Rank(AposMastery.Sledges, id); }

    static float quakeReady;
    static int momentumTarget = -1, momentumHits;
    static float momentumAt;

    class Zone { public Vector3 Pos; public float Until, Slow, Next; }
    class Echo { public Vector3 Pos; public float At; public float Damage; public EntityAlive Center; }
    static readonly List<Zone> zones = new List<Zone>();
    static readonly List<Echo> echoes = new List<Echo>();

    public static void Reset() { quakeReady = 0f; momentumTarget = -1; zones.Clear(); echoes.Clear(); }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        // Rift: the ground slows everyone who walks in for 10 s
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            Zone z = zones[i];
            if (Time.time > z.Until) { zones.RemoveAt(i); continue; }
            if (Time.time < z.Next) continue;
            z.Next = Time.time + 0.5f;
            foreach (EntityAlive e in AposMelee.Around(z.Pos, 10f)) AposPistol.Shackle(e, z.Slow, 1f);
        }
        // Aftershock: the second quake
        for (int i = echoes.Count - 1; i >= 0; i--)
        {
            if (Time.time < echoes[i].At) continue;
            Echo ec = echoes[i];
            echoes.RemoveAt(i);
            Shake(player, ec.Pos, ec.Center, ec.Damage, false);
        }
        if (!held) return;
        float lungs = Mathf.Min(80f, IronLungs[R("B2")] * AposAffixes.Level(player, "attFortitude")) / 100f;
        AposMastery.SetStat(AposMastery.StatStamPower, 0.5f + (R("B8") > 0 ? 0.5f : 0f) - lungs);
        AposMastery.SetStat(AposMastery.StatStamNormal, -lungs);
        float left = quakeReady - Time.time;
        AposHud.Add(false, "sledge", left > 0f ? Mathf.CeilToInt(left) + "s" : "OK", left > 0f ? new Color(0.6f, 0.6f, 0.6f) : new Color(1f, 0.6f, 0.2f), left <= 0f);
        if (R("B4") > 0 && momentumHits > 0 && Time.time - momentumAt < 3f) AposHud.Add(false, "twitch_strength_melee", "x" + momentumHits, new Color(0.4f, 0.75f, 1f), momentumHits >= 3);
    }

    // ===== hits =====
    static int lastTarget = -1;
    static bool lastPower;
    static float lastBase, lastFull;

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        bool power = AposMelee.Power(player);
        float mult = 1f;
        // Momentum: hits on the same zombie within 3 s, up to 3
        if (R("B4") > 0)
        {
            if (target.entityId == momentumTarget && Time.time - momentumAt <= 3f) momentumHits = Mathf.Min(3, momentumHits + 1);
            else momentumHits = 0;
            momentumTarget = target.entityId;
            momentumAt = Time.time;
            mult += Momentum[R("B4")] * momentumHits;
        }
        if (power)
        {
            mult += Heavy[R("B1")];
            if (R("B6") > 0 && AposMelee.Down(target)) mult += Crush[R("B6")];
            if (R("B7") > 0 && AposMelee.Nearest(target.position, 5f, target, 1).Count == 0) mult += Duel[R("B7")];
        }
        lastTarget = target.entityId; lastPower = power;
        lastBase = strength;
        lastFull = strength * mult;
        // Execution: a power hit kills a zombie below 40% health at once (elites below 20%)
        if (power && R("B8") > 0)
        {
            float line = AposElites.IsElite(target) ? 0.2f : 0.4f;
            if (target.Health < target.GetMaxHealth() * line) return target.GetMaxHealth() * 2;
        }
        int result = Mathf.Max(1, Mathf.RoundToInt(lastFull));
        // Armor Break on this zombie: its armor counts lower for your hits
        if (target.Buffs.HasBuff("buffAposArmorBreak")) result = Mathf.RoundToInt(result * ArmorFactor(target, target.Buffs.GetCustomVar("aposArmorBreak")));
        return result;
    }

    // how much more a hit deals when the zombie's physical armor is lowered by "cut" (and the Armored elite's halving
    // is gone at the top rank)
    public static float ArmorFactor(EntityAlive e, float cut)
    {
        float armor = Mathf.Clamp(EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, e) / 100f, 0f, 0.9f);
        float f = (1f - Mathf.Max(0f, armor - cut)) / (1f - armor);
        if (cut >= 0.4f && AposElites.IsElite(e) && AposElites.Has(e, "Armored")) f *= 2f;
        return f;
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || target.entityId != lastTarget) return;
        lastTarget = -1;
        if (R("B3") > 0 && !response.Fatal) BreakArmor(target, ArmorBreak[R("B3")]);
        if (lastPower)
        {
            float quakeDmg = R("H2") > 0 ? lastFull : lastBase;          // Heavy Quake: from the boosted hit
            if (response.Fatal && R("H3") > 0) Shake(player, target.position, target, lastFull * 1f / Mathf.Max(0.01f, Shockforce[R("A2")]), true); // Doomsday
            else if (Time.time >= quakeReady) Shake(player, target.position, target, quakeDmg, true);
            // Overkill: the power hit's excess goes to the nearest zombie within 4 m
            if (response.Fatal && R("B5") > 0)
            {
                int excess = response.Strength - dealt;
                List<EntityAlive> next = AposMelee.Nearest(target.position, 4f, target, 1);
                if (excess > 0 && next.Count > 0) AposSplash.Deal(next[0], excess, player.entityId, EnumDamageTypes.Bashing);
            }
        }
        else if (!response.Fatal && Random.value < Stagger[R("A4")])
        {
            target.Buffs.AddBuff("buffInjuryKnockdown01");
        }
    }

    static void BreakArmor(EntityAlive e, float cut)
    {
        if (e.Buffs.GetCustomVar("aposArmorBreak") < cut || !e.Buffs.HasBuff("buffAposArmorBreak")) e.Buffs.SetCustomVar("aposArmorBreak", cut);
        e.Buffs.AddBuff("buffAposArmorBreak", -1, true, false, 10f);
    }

    // the Earthquake: knock down everyone around, a share of the blow to each; first = the main one (cooldown, Rift,
    // Aftershock), not its echo
    static void Shake(EntityPlayerLocal player, Vector3 pos, EntityAlive center, float blow, bool first)
    {
        if (first)
        {
            quakeReady = Time.time + Cooldown[R("A3")] + (R("A8") > 0 ? 2f : 0f);
            if (R("A7") > 0) zones.Add(new Zone { Pos = pos, Until = Time.time + 10f, Slow = Rift[R("A7")] });
            if (R("A8") > 0) echoes.Add(new Echo { Pos = pos, At = Time.time + 1.5f, Damage = blow, Center = center });
        }
        float radius = QuakeRadius[R("A1")] + (player.Buffs.HasBuff("buffBikerSetBonus") ? 1f : 0f);
        float down = BaseDown + Trembling[R("A6")];
        int dmg = Mathf.Max(1, Mathf.RoundToInt(blow * Shockforce[R("A2")]));
        string element = null;
        if (center != null && R("A5") > 0)
        {
            foreach (string b in new[] { "buffBurningElement", "buffShocked", "buffAposCorroded", "buffInjuryBleeding" })
                if (center.Buffs.HasBuff(b)) { element = b; break; }
        }
        foreach (EntityAlive e in AposMelee.Around(pos, radius))
        {
            if (e != center) AposSplash.Deal(e, dmg, player.entityId, EnumDamageTypes.Bashing);
            if (e.IsDead()) continue;
            AposKnock.Ragdoll(e, pos, 250f, down);
            if (R("A8") > 0) e.Buffs.AddBuff("buffInjuryStunned01", -1, true, false, down + 3f);   // Aftershock: stunned after getting up
            if (element != null && e != center) e.Buffs.AddBuff(element);
            if (R("H1") > 0) BreakArmor(e, Mathf.Max(0.1f, ArmorBreak[R("B3")]));                // Fault Line
        }
    }
}

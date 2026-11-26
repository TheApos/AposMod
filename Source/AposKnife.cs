// Weapon Mastery 2.0, knives and machetes (docs/Design-Mastery2.md 5е; driven by AposMastery.cs):
// - Class trait "Bloodsucker": every hit is a regen stack (+1 HP/s, a power hit +2), stacks last 5 s from the last hit,
//   max 2 (full Assassin set +2). A power hit makes the zombie bleed. The regen is buffAposKnifeRegen (cvars
//   aposKnifeStacks / aposKnifeTime), set from here. (It used to be XML scaled by Deep Cuts.)
// - Branch A (stacks and bleeding): Thirst, Deep Cuts, Hot Blood, Open Wounds, Blood Plague, Second Wind,
//   Lasting Regen, capstone Blood Feast.
// - Branch B (crits, back hits): Keen Edge (x Agility), Backstab, Critical Power, Flurry, Coup de Grace, Handoff,
//   Execution Chain, capstone Spine Strike.
// - Hybrids: Bloody Crit, Blood Scent, Blade Dance.
using UnityEngine;

public static class AposKnife
{
    static readonly int[] Thirst = { 2, 4, 5, 8 };
    static readonly float[] DeepCuts = { 0f, 0.15f, 0.30f, 0.60f };
    static readonly float[] HotBlood = { 0f, 0.05f, 0.08f, 0.15f };
    public static readonly float[] OpenWounds = { 0f, 0.3f, 0.6f, 1.2f };
    static readonly float[] SecondWind = { 0f, 2f, 5f };
    static readonly float[] StackTime = { 5f, 8f, 15f };
    static readonly float[] KeenEdge = { 0f, 1f, 2.5f, 5f };   // % per Agility level, max 50
    static readonly float[] Backstab = { 0f, 0.3f, 0.6f, 1.2f };
    static readonly float[] CritPower = { 2f, 2.5f, 3f, 4f };
    static readonly float[] Flurry = { 0f, 10f, 25f, 50f };
    static readonly int[] ChainCrits = { 0, 1, 3 };

    public static int R(string id) { return AposMastery.Rank(AposMastery.Blades, id); }

    static int chainLeft, danceCount;

    public static void Reset() { chainLeft = danceCount = 0; }

    static int Cap(EntityPlayerLocal p) { return Thirst[R("A1")] + (p.Buffs.HasBuff("buffAssassinSetBonus") ? 2 : 0); }
    static int Stacks(EntityPlayerLocal p) { return p.Buffs.HasBuff("buffAposKnifeRegen") ? (int)p.Buffs.GetCustomVar("aposKnifeStacks") : 0; }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        if (!held) return;
        AposMastery.SetStat(AposMastery.StatSpeed, Flurry[R("B4")] / 100f);
        int s = Stacks(player), cap = Cap(player);
        Color red = new Color(0.9f, 0.25f, 0.3f);
        // stacks under the crosshair (like the other classes' main counters), the regen they give in the defence column
        AposHud.Add(false, "drop", s + "/" + cap, s > 0 ? red : new Color(0.6f, 0.6f, 0.6f), s >= cap, "Base");
        AposHud.Add(true, "drop", "+" + s + "/s", s > 0 ? red : new Color(0.6f, 0.6f, 0.6f), s >= cap, "Base");
        if (R("A3") > 0 && s > 0) AposHud.Add(false, "drop", "+" + Mathf.RoundToInt(HotBlood[R("A3")] * s * 100f) + "%", red, s >= cap, "A3");
        if (chainLeft > 0) AposHud.Add(false, "critical", "x" + chainLeft, new Color(1f, 0.82f, 0.3f), true, "B7");
    }

    static void AddStacks(EntityPlayerLocal p, int n)
    {
        int s = Mathf.Min(Cap(p), Stacks(p) + n);
        p.Buffs.SetCustomVar("aposKnifeStacks", s);
        p.Buffs.SetCustomVar("aposKnifeTime", StackTime[R("A7")]);
        if (!p.Buffs.HasBuff("buffAposKnifeRegen")) p.Buffs.AddBuff("buffAposKnifeRegen");
    }

    // ===== hits =====
    static int lastTarget = -1;
    static bool lastCrit, lastPower, lastBleeding, lastStealth;

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        bool power = AposMelee.Power(player);
        bool bleeding = target.Buffs.HasBuff("buffInjuryBleeding");
        bool behind = AposMelee.Behind(target, player);
        bool unseen = !AposMelee.SeesYou(target, player);
        float mult = 1f;
        mult += HotBlood[R("A3")] * Stacks(player);
        // Backstab: from behind or unseen; Blood Scent: a bleeding zombie counts too
        if (R("B2") > 0 && (behind || unseen || (R("H2") > 0 && bleeding))) mult += Backstab[R("B2")];
        if (R("A8") > 0 && !bleeding) mult -= 0.3f;                   // Blood Feast downside
        if (R("B8") > 0)
        {
            if (behind) mult *= 2f;                                   // Spine Strike: a real back hit x2 (through armor below)
            else if (!unseen) mult -= 0.25f;                          // in the face -25%
        }
        // crits
        bool crit = false;
        float chance = Mathf.Min(50f, KeenEdge[R("B1")] * AposAffixes.Level(player, "attAgility")) / 100f;
        if (chainLeft > 0) { crit = true; chainLeft--; }
        bool low = target.Health < target.GetMaxHealth() * 0.25f || AposMelee.Down(target);
        if (R("B5") > 0 && low) crit = true;
        if (R("H3") > 0 && Stacks(player) >= Cap(player) && ++danceCount % 3 == 0) crit = true;
        if (!crit && chance > 0f && Random.value < chance) crit = true;
        if (crit) mult *= CritPower[R("B3")];
        mult = Mathf.Max(0.1f, mult);
        lastTarget = target.entityId; lastCrit = crit; lastPower = power; lastBleeding = bleeding; lastStealth = unseen;
        // Coup de Grace: below 25% or knocked down: a non-elite dies at once
        if (R("B5") > 0 && low && !AposElites.IsElite(target)) return target.GetMaxHealth() * 2;
        int result = Mathf.Max(1, Mathf.RoundToInt(strength * mult));
        // Spine Strike goes through armor: put back roughly what the zombie's armor takes away
        if (R("B8") > 0 && behind) result = Mathf.RoundToInt(result * ArmorBack(target));
        return result;
    }

    static float ArmorBack(EntityAlive e)
    {
        float armor = EffectManager.GetValue(PassiveEffects.PhysicalDamageResist, null, 0f, e) / 100f;
        armor = Mathf.Clamp(armor, 0f, 0.9f);
        if (AposElites.IsElite(e) && AposElites.Has(e, "Armored")) return 2f / (1f - armor); // the Armored elite halves body hits
        return 1f / (1f - armor);
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || target.entityId != lastTarget) return;
        lastTarget = -1;
        AddStacks(player, lastPower ? 2 : 1);
        if (lastCrit && R("H1") > 0) AddStacks(player, 3);
        if (lastCrit && R("H3") > 0)
        {
            player.Buffs.SetCustomVar("aposKnifeTime", StackTime[R("A7")]);
            player.AddHealth(3);
        }
        if (R("A6") > 0 && player.Health < player.GetMaxHealth() * 0.3f) player.AddHealth(Mathf.RoundToInt(SecondWind[R("A6")]));
        if (R("A8") > 0 && lastBleeding && dealt > 0) player.AddHealth(Mathf.Max(1, Mathf.RoundToInt(dealt * 0.1f)));

        if (response.Fatal)
        {
            if (R("A8") > 0 && Stacks(player) >= Cap(player)) player.AddHealth(25);
            // Blood Plague: a bleeding zombie's death passes the bleeding to the 2 nearest within 4 m
            if (R("A5") > 0 && lastBleeding)
            {
                foreach (EntityAlive e in AposMelee.Nearest(target.position, 4f, target, 2)) e.Buffs.AddBuff("buffInjuryBleeding");
            }
            if (lastCrit && R("B7") > 0) chainLeft = ChainCrits[R("B7")];
            // Handoff: a stealth knife kill primes the pistol streak for 5 s
            if (lastStealth && R("B6") > 0) AposPistol.Prime(R("B6") >= 2 ? 3 : 2, R("B6") >= 2);
            return;
        }
        // bleeding: every power hit, normal hits with Deep Cuts; Bloody Crit: every crit
        if (lastPower || Random.value < DeepCuts[R("A2")] || (lastCrit && R("H1") > 0)) target.Buffs.AddBuff("buffInjuryBleeding");
        // Bloody Crit on a zombie that already bled: the wound tears open, the bleeding's damage comes at once
        if (lastCrit && R("H1") > 0 && lastBleeding) AposSplash.Deal(target, Mathf.Max(1, Mathf.RoundToInt(dealt * 0.6f)), player.entityId, EnumDamageTypes.BloodLoss);
    }
}

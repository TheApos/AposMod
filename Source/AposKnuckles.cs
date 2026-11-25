// Weapon Mastery 2.0, knuckles (docs/Design-Mastery2.md 5и; driven by AposMastery.cs):
// - Class trait "Counter Punch": every hit you take gives a charge (max 2, no timer; full Raider set +2). A normal hit
//   spends 1 and deals +25%; a power hit spends all, +50% per charge, and knocks down. (It used to be XML scaled by
//   The Brawler.) aposCounterHits / aposCounterCap stay on the player for the Raider set bonus (buffs.xml).
// - Branch A (Counter Punch, the tank): More Charges, Iron Skin (x Fortitude), Retort, Take It, Recoil, Lick Wounds,
//   Blood for Blood, capstone Knockout.
// - Branch B (Flurry, do not get hit): Combo, Light Feet, Fast Hands, Slip, Wall of Fists, Opener, Second Chance,
//   capstone Hundred Fists.
// - Hybrids: Counter, Clinch, King of the Ring.
// Hits on the player: Incoming (prefix: damage taken) and Taken (postfix: charges), both from AposCombat.
using UnityEngine;

public static class AposKnuckles
{
    static readonly int[] MoreCharges = { 2, 3, 4, 7 };
    static readonly float[] IronSkin = { 0f, 1f, 2f, 4f };       // % per Fortitude level, max 40
    static readonly float[] Retort = { 0.25f, 0.40f, 0.60f, 1f };
    static readonly float[] TakeIt = { 0f, 0.15f, 0.30f, 0.60f };
    static readonly float[] LickWounds = { 0f, 3f, 8f };
    static readonly float[] BloodForBlood = { 0f, 0.5f, 1.5f };
    static readonly float[] ComboPer = { 0f, 0.02f, 0.03f, 0.06f };
    static readonly float[] LightFeet = { 0f, 0.05f, 0.10f, 0.20f };
    static readonly float[] FastHands = { 0f, 0.10f, 0.20f, 0.40f };
    static readonly float[] Slip = { 0f, 0.15f, 0.25f, 0.40f };
    static readonly float[] Opener = { 1f, 2f, 3f };
    static readonly int[] SecondChance = { 0, 1, 2 };

    static int R(string id) { return AposMastery.Rank(AposMastery.Knuckles, id); }

    static int charges, combo, comboTarget = -1, savesLeft;
    static float lastTaken = -100f, lastHitAt = -100f, bloodBank, slowUntil;

    public static void Reset() { charges = combo = 0; comboTarget = -1; lastTaken = lastHitAt = -100f; bloodBank = 0f; slowUntil = 0f; }

    static int Cap(EntityPlayerLocal p) { return MoreCharges[R("A1")] + (p.Buffs.HasBuff("buffRaiderSetBonus") ? 2 : 0); }
    static int ComboMax() { return R("B8") > 0 ? 20 : 10; }
    static bool Held() { return AposMastery.HeldClass == AposMastery.Knuckles; }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        int cap = Cap(player);
        if (charges > cap) charges = cap;
        if (player.Buffs.GetCustomVar("aposCounterHits") != charges) player.Buffs.SetCustomVar("aposCounterHits", charges);
        if (player.Buffs.GetCustomVar("aposCounterCap") != cap) player.Buffs.SetCustomVar("aposCounterCap", cap);
        if (!held) return;
        float speed = FastHands[R("B3")] + (R("B8") > 0 && combo >= 15 ? 0.5f : 0f);
        AposMastery.SetStat(AposMastery.StatSpeed, speed);
        // Light Feet: 5 s after a hit you move faster; Knockout: 15 s after it you move 20% slower
        float move = (R("B2") > 0 && Time.time - lastHitAt < 5f ? LightFeet[R("B2")] : 0f) - (Time.time < slowUntil ? 0.2f : 0f);
        if (player.Buffs.GetCustomVar("aposKnuckleMove") != move) player.Buffs.SetCustomVar("aposKnuckleMove", move);
        if (move != 0f) player.Buffs.AddBuff("buffAposKnuckleMove"); // short, refreshed while it applies

        Color gold = new Color(1f, 0.82f, 0.3f);
        AposHud.Add(true, "boxer", charges + "/" + cap, charges > 0 ? new Color(1f, 0.6f, 0.2f) : new Color(0.6f, 0.6f, 0.6f), charges >= cap);
        if (R("A2") > 0 && charges > 0) AposHud.Add(true, "defense", "-" + Mathf.RoundToInt(SkinPct(player) * 100f) + "%", new Color(1f, 0.6f, 0.2f), false);
        if (R("B1") > 0 && combo > 0) AposHud.Add(false, "twitch_strength_melee", combo + "/" + ComboMax(), new Color(0.4f, 0.75f, 1f), combo >= 10);
        if (R("B6") > 0 && Time.time - lastTaken >= 5f) AposHud.Add(false, "sneak_attack", "x" + Opener[R("B6")], gold, true);
    }

    static float SkinPct(EntityPlayerLocal p) { return Mathf.Min(40f, IronSkin[R("A2")] * AposAffixes.Level(p, "attFortitude")) / 100f; }

    // ===== a zombie hits the player (prefix): the damage that lands =====
    public static int Incoming(EntityPlayerLocal p, EntityAlive attacker, int strength)
    {
        if (!Held()) return strength;
        float m = 1f;
        // Slip: from 5 combo hits a zombie's hit may miss you
        if (R("B4") > 0 && combo >= 5 && Random.value < Slip[R("B4")]) return 0;
        // Clinch: the zombie you keep a 5+ combo on hits 25% weaker
        if (R("H2") > 0 && combo >= 5 && attacker != null && attacker.entityId == comboTarget) m *= 0.75f;
        if (charges > 0) m *= 1f - SkinPct(p);                  // Iron Skin
        if (R("B8") > 0) m *= 1.3f;                              // Hundred Fists downside
        return Mathf.Max(0, Mathf.RoundToInt(strength * m));
    }

    // ===== after a zombie hit the player (postfix): charges, combo =====
    public static void Taken(EntityPlayerLocal p, EntityAlive attacker, int damage)
    {
        if (!Held() || damage <= 0) return;
        lastTaken = Time.time;
        bool fullBefore = charges >= Cap(p);
        // Counter: a charge burns instead of the combo
        if (combo > 0)
        {
            if (R("H1") > 0 && charges > 0) charges--;
            else if (savesLeft > 0) savesLeft--;               // Second Chance
            else { combo = 0; comboTarget = -1; }
        }
        int gain = Random.value < TakeIt[R("A4")] ? 2 : 1;
        charges = Mathf.Min(Cap(p), charges + gain);
        bloodBank += damage;
        // Recoil: a hit on you at full charges throws the attacker back and stuns it for 1 s
        if (fullBefore && R("A5") > 0 && attacker != null && !(attacker is EntityPlayer))
        {
            AposKnock.Ragdoll(attacker, p.position, 300f, 1f);
            attacker.Buffs.AddBuff("buffInjuryStunned01", -1, true, false, 1f);
        }
    }

    // ===== the player's punches =====
    static int lastTarget = -1, spent;
    static bool lastPower, lastFull;

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        bool power = AposMelee.Power(player);
        float mult = 1f;
        // Combo: every hit without being hit (the same fight), up to 10 (20)
        if (R("B1") > 0)
        {
            if (combo == 0) savesLeft = SecondChance[R("B7")];
            combo = Mathf.Min(ComboMax(), combo + 1);
            comboTarget = target.entityId;
            mult += ComboPer[R("B1")] * Mathf.Min(combo, ComboMax());
        }
        // Opener: the first hit after 5 s untouched
        if (R("B6") > 0 && Time.time - lastTaken >= 5f && Time.time - lastHitAt >= 5f) mult *= Opener[R("B6")];
        lastFull = charges >= Cap(player) && charges > 0;
        spent = 0;
        if (power)
        {
            spent = charges;
            mult += 0.5f * charges;
            if (bloodBank > 0f && R("A7") > 0) strength += Mathf.RoundToInt(bloodBank * BloodForBlood[R("A7")]);   // Blood for Blood
        }
        else if (charges > 0) { spent = 1; mult += Retort[R("A3")]; }
        lastTarget = target.entityId; lastPower = power;
        lastHitAt = Time.time;
        // Knockout: a power punch on full charges kills a non-elite at once; an elite x3
        if (power && lastFull && R("A8") > 0)
        {
            slowUntil = Time.time + 15f;
            if (!AposElites.IsElite(target)) return target.GetMaxHealth() * 2;
            mult *= 3f;
        }
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || target.entityId != lastTarget) return;
        lastTarget = -1;
        int used = spent;
        charges = Mathf.Max(0, charges - used);
        if (lastPower)
        {
            bloodBank = 0f;
            if (used > 0 && R("A6") > 0) player.AddHealth(Mathf.RoundToInt(LickWounds[R("A6")] * used));
            if (!response.Fatal && used > 0) AposKnock.Ragdoll(target, player.position, 300f, lastFull && R("A8") > 0 ? 3f : 1.5f);
            // King of the Ring: full charges and a 10+ combo: the power punch knocks down everyone within 3 m at 50%
            if (R("H3") > 0 && lastFull && combo >= 10)
            {
                foreach (EntityAlive e in AposMelee.Around(target.position, 3f))
                {
                    if (e == target) continue;
                    AposSplash.Deal(e, Mathf.Max(1, dealt / 2), player.entityId, EnumDamageTypes.Bashing);
                    if (!e.IsDead()) AposKnock.Ragdoll(e, player.position, 300f, 1.5f);
                }
            }
        }
        if (response.Fatal) { if (target.entityId == comboTarget) comboTarget = -1; return; }
        // Wall of Fists: at 10+ combo every punch interrupts the target's swing
        if (R("B5") > 0 && combo >= 10) target.Buffs.AddBuff("buffInjuryStunned01", -1, true, false, 0.4f);
        // Hundred Fists: from 15 combo every 5th punch hits everyone near
        if (R("B8") > 0 && combo >= 15 && combo % 5 == 0)
        {
            foreach (EntityAlive e in AposMelee.Around(target.position, 2.5f))
                if (e != target) AposSplash.Deal(e, Mathf.Max(1, dealt), player.entityId, EnumDamageTypes.Bashing);
        }
    }

    // Clinch: the zombie under a 5+ combo cannot knock you down or stun you
    public static bool ClinchProtects(EntityAlive attacker)
    {
        return Held() && R("H2") > 0 && combo >= 5 && attacker != null && attacker.entityId == comboTarget;
    }
}

// Weapon Mastery 2.0, spears (docs/Design-Mastery2.md 5з; driven by AposMastery.cs):
// - Class trait "Battering Ram": a normal hit pushes the target (force 120), a power hit goes through 1 zombie behind
//   (aposMPiercePower, power hits only) and throws each one back. (It used to be XML scaled by Spear Master.)
// - Branch A (the line, distance): Strong Push, Through Strike, Spear Tip, Bowling, Tip in the Wound, Hit and Back,
//   Sentry, capstone Breakthrough.
// - Branch B (fencing): Quick Thrusts (x Agility), Deep Thrust, Head Thrust, Thrust Chain, Parry, Dance, Riposte,
//   capstone Whirlwind.
// - Hybrids: Bloody Skewer, Parting Shot, Spear Master.
// Parry: the zombie's attack animation is playing and has not hit yet (AvatarController); shown as a toast + clang.
// Breakthrough: starts with the power swing itself (XML counter aposPowerN), if you were running in the last 0.6 s;
// the player is moved by SetPosition steps (motion does nothing on the local player) and stops at a wall.
using System.Collections.Generic;
using UnityEngine;

public static class AposSpear
{
    const float Reach = 2.6f;
    static readonly float[] Push = { 120f, 160f, 200f, 320f };
    static readonly int[] PiercePower = { 1, 2, 3, 6 };
    static readonly float[] Tip = { 0f, 0.2f, 0.4f, 0.8f };
    static readonly float[] Bowling = { 0f, 0.15f, 0.3f, 0.6f };
    static readonly float[] Retreat = { 0f, 0.15f, 0.40f };
    static readonly float[] SentryCD = { 0f, 10f, 5f };
    static readonly float[] Quick = { 0f, 2f, 3f, 6f };          // % per Agility level, max 60
    static readonly float[] DeepThrust = { 0f, 0.15f, 0.3f, 0.6f };
    static readonly float[] HeadThrust = { 0f, 0.3f, 0.6f, 1.2f };
    static readonly float[] Chain = { 0f, 0.05f, 0.08f, 0.15f };
    static readonly float[] Dance = { 0f, 0.1f, 0.3f };
    static readonly int[] Riposte = { 0, 2, 4 };

    static int R(string id) { return AposMastery.Rank(AposMastery.Spears, id); }

    static int chainTarget = -1, chainHits, riposteLeft;
    static float chainAt, sentryReady, dashUntil, noAttackUntil;
    static Vector3 dashDir, lastHitPos;
    static float lastRunAt = -100f, dashHit, powerN;
    static readonly HashSet<int> dashHits = new HashSet<int>();
    static bool partingReady;
    class Bowl { public EntityAlive Body; public float At; }
    static readonly List<Bowl> bowls = new List<Bowl>();

    public static void Reset() { chainTarget = -1; chainHits = riposteLeft = 0; sentryReady = dashUntil = noAttackUntil = 0f; bowls.Clear(); partingReady = false; }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        for (int i = bowls.Count - 1; i >= 0; i--)
        {
            if (Time.time < bowls[i].At) continue;
            Bowl b = bowls[i]; bowls.RemoveAt(i);
            if (b.Body == null) continue;
            // Bowling: the pushed zombie knocks down whoever it flew into
            foreach (EntityAlive e in AposMelee.Around(b.Body.position, 1.3f))
                if (e != b.Body && Random.value < Bowling[R("A4")]) AposKnock.Ragdoll(e, b.Body.position, 250f, 1.5f);
        }
        // Breakthrough: a power swing started while running (or a moment after) becomes a dash
        float n = player.Buffs.GetCustomVar("aposPowerN");
        bool swing = n != powerN;
        powerN = n;
        if (player.IsRunning || HorizontalSpeed(player) > 5f) lastRunAt = Time.time;
        if (held && swing && R("A8") > 0 && Time.time - lastRunAt < 0.6f && Time.time > dashUntil + 0.5f) StartDash(player);
        if (Time.time < dashUntil) DashStep(player);
        if (!held) return;

        float speed = Mathf.Min(60f, Quick[R("B1")] * AposAffixes.Level(player, "attAgility")) / 100f;
        if (riposteLeft > 0) speed += 0.5f;
        if (Time.time < noAttackUntil) speed = -0.95f;                    // after the dash: barely attacks
        AposMastery.SetStat(AposMastery.StatSpeed, speed);
        AposMastery.SetStat(AposMastery.StatPiercePower, PiercePower[R("A2")]);

        // Parting Shot: you stepped back to spear distance after a hit
        if (R("H2") > 0 && !partingReady && lastHitPos != Vector3.zero && (player.position - lastHitPos).magnitude >= 2.5f && player.Buffs.HasBuff("buffAposSpearRetreat")) partingReady = true;

        // Sentry: a zombie closer than 2 m gets a thrust on its own
        if (R("A7") > 0 && Time.time >= sentryReady)
        {
            foreach (EntityAlive e in AposMelee.Around(player.position, 2f))
            {
                sentryReady = Time.time + SentryCD[R("A7")];
                AposHud.Flash("defense");
                float dmg = EffectManager.GetValue(PassiveEffects.EntityDamage, player.inventory.holdingItemItemValue, 10f, player);
                AposSplash.Deal(e, Mathf.Max(1, Mathf.RoundToInt(dmg)), player.entityId, EnumDamageTypes.Piercing);
                if (!e.IsDead()) AposKnock.Ragdoll(e, player.position, Push[R("A1")], 0.2f);
                break;
            }
        }
        if (R("B4") > 0 && chainHits > 0 && Time.time - chainAt <= 1f)
        {
            AposHud.Gauge(false, chainHits, 5, new Color(0.4f, 0.75f, 1f), chainHits >= 5, "spear", "x" + chainHits, "B4");
            AposStatus.Target(chainTarget, "spear", "x" + chainHits, new Color(0.4f, 0.75f, 1f));
        }
        if (riposteLeft > 0) AposHud.AddAttackSpeed("speed", 0.5f, "x" + riposteLeft, "B7");
        BuffValue retreat = player.Buffs.GetBuff("buffAposSpearRetreat");
        if (retreat != null && !retreat.Remove && R("A6") > 0)
        {
            float rl = retreat.BuffClass != null && retreat.BuffClass.DurationMax > 0f ? retreat.BuffClass.DurationMax - retreat.DurationInSeconds : 0f;
            AposHud.AddSpeed("ui_apos_speed", Retreat[R("A6")], rl >= 0.5f ? Mathf.CeilToInt(rl) + "s" : "", "A6");
        }
        if (R("A7") > 0) AposHud.Add(true, "defense", Time.time >= sentryReady ? "OK" : Mathf.CeilToInt(sentryReady - Time.time) + "s", new Color(0.7f, 0.85f, 1f), Time.time >= sentryReady, "A7");
        if (partingReady) AposHud.AddAlert("head_shot", Localization.Get("aposPartingAlert"), new Color(1f, 0.82f, 0.3f), "H2");   // the next hit is a head thrust
    }

    public static float Incoming() { return AposMastery.HeldClass == AposMastery.Spears && R("B8") > 0 ? 1.2f : 1f; }

    static bool Winding(EntityAlive e)
    {
        AvatarController ac = e.emodel != null ? e.emodel.avatarController : null;
        return ac != null && ac.IsAnimationAttackPlaying() && !ac.IsAttackImpact();
    }

    // ===== hits =====
    static int lastTarget = -1, firstPowerTarget = -1;
    static bool lastPower, lastParry, lastWhirl;
    static float lastPowerAt;

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        bool power = AposMelee.Power(player);
        bool head = (source.GetEntityDamageBodyPart(target) & EnumBodyPartHit.Head) != 0;
        float mult = 1f;
        float dist = (target.position - player.position).magnitude;
        if (dist >= 2.5f) mult += Tip[R("A3")];
        if (partingReady) { head = true; mult *= 1.5f; partingReady = false; }
        if (head) mult += HeadThrust[R("B3")];
        // Tip in the Wound: every hit on the zombie the tip stuck in, +25% more, up to +100%
        if (R("A5") > 0 && target.Buffs.GetCustomVar("aposTip") > 0f)
        {
            float n = Mathf.Min(4f, target.Buffs.GetCustomVar("aposTipHits") + 1f);
            target.Buffs.SetCustomVar("aposTipHits", n);
            mult += 0.25f * n;
        }
        // Thrust Chain: the same zombie, at most 1 s between hits, up to 5
        if (target.entityId == chainTarget && Time.time - chainAt <= 1f) chainHits = Mathf.Min(5, chainHits + 1);
        else chainHits = 0;
        chainTarget = target.entityId; chainAt = Time.time;
        mult += Chain[R("B4")] * chainHits;
        if (R("B6") > 0 && player.GetVelocityPerSecond().sqrMagnitude > 0.5f) mult += Dance[R("B6")];
        // Parry: a hit while the zombie winds up its attack interrupts it, x2
        lastParry = R("B5") > 0 && Winding(target);
        if (lastParry) mult *= 2f;
        lastWhirl = R("B8") > 0 && chainHits > 0 && chainHits % 4 == 3;
        if (power)
        {
            // the first zombie of a power hit (within a moment) gets the tip
            if (Time.time - lastPowerAt > 0.15f) firstPowerTarget = target.entityId;
            lastPowerAt = Time.time;
        }
        lastTarget = target.entityId; lastPower = power;
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || target.entityId != lastTarget) return;
        lastTarget = -1;
        lastHitPos = target.position;
        if (R("A6") > 0)
        {
            player.Buffs.SetCustomVar("aposRetreat", Retreat[R("A6")]);
            player.Buffs.AddBuff("buffAposSpearRetreat");
        }
        if (riposteLeft > 0) riposteLeft--;
        if (lastParry)
        {
            riposteLeft = Riposte[R("B7")];
            AposFx.Toast(Localization.Get("aposParryToast"), new Color(1f, 0.85f, 0.4f));
            Audio.Manager.PlayInsidePlayerHead("metalhitmetal", player.entityId);
            if (!response.Fatal) target.Buffs.AddBuff("buffInjuryStunned01", -1, true, false, 0.6f);
            // Spear Master: a parry works like a power hit: thrown 3 m and the line behind pierced
            if (R("H3") > 0)
            {
                if (!response.Fatal) AposKnock.Ragdoll(target, player.position, 500f, 1.5f);
                PierceLine(player, target, dealt);
            }
        }
        // Whirlwind: every 4th hit of a thrust chain hits everyone in reach and makes them bleed
        if (lastWhirl)
        {
            foreach (EntityAlive e in AposMelee.Around(player.position, Reach))
            {
                if (e == target) continue;
                AposSplash.Deal(e, Mathf.Max(1, dealt), player.entityId, EnumDamageTypes.Piercing);
                if (!e.IsDead()) e.Buffs.AddBuff("buffInjuryBleeding");
            }
            if (!response.Fatal) target.Buffs.AddBuff("buffInjuryBleeding");
        }
        if (response.Fatal) return;
        if (lastPower)
        {
            AposKnock.Ragdoll(target, player.position, 400f, 1.5f);
            if (R("H1") > 0) target.Buffs.AddBuff("buffInjuryBleeding");
            if (R("A5") > 0 && target.entityId == firstPowerTarget)
            {
                target.Buffs.SetCustomVar("aposTip", 1f);
                target.Buffs.SetCustomVar("aposTipHits", 0f);
            }
            if (R("A4") > 0) bowls.Add(new Bowl { Body = target, At = Time.time + 0.4f });
        }
        else
        {
            AposKnock.Ragdoll(target, player.position, Push[R("A1")], 0.2f);
            if (Random.value < DeepThrust[R("B2")]) target.Buffs.AddBuff("buffInjuryBleeding");
            if (R("A4") > 0) bowls.Add(new Bowl { Body = target, At = Time.time + 0.4f });
        }
    }

    static void PierceLine(EntityPlayerLocal player, EntityAlive target, int dmg)
    {
        Vector3 dir = target.position - player.position; dir.y = 0f;
        float far = dir.magnitude;
        if (far < 0.1f) return;
        dir /= far;
        foreach (EntityAlive e in AposMelee.Around(target.position, 4f))
        {
            if (e == target) continue;
            Vector3 v = e.position - player.position; v.y = 0f;
            float along = Vector3.Dot(v, dir);
            if (along <= far || (v - dir * along).magnitude > 0.9f) continue;
            AposSplash.Deal(e, Mathf.Max(1, dmg), player.entityId, EnumDamageTypes.Piercing);
            if (!e.IsDead()) AposKnock.Ragdoll(e, player.position, 400f, 1.5f);
        }
    }

    static float HorizontalSpeed(EntityPlayerLocal p)
    {
        Vector3 v = p.GetVelocityPerSecond(); v.y = 0f;
        return v.magnitude;
    }

    // Breakthrough: 6 m forward through the crowd; everyone on the way x2 (of a power hit) and knocked down;
    // 1.5 s without attacks after
    const float DashTime = 0.3f, DashSpeed = 20f;
    static void StartDash(EntityPlayerLocal player)
    {
        Vector3 f = player.GetLookVector(); f.y = 0f;
        if (f.sqrMagnitude < 0.01f) return;
        dashDir = f.normalized;
        dashUntil = Time.time + DashTime;
        noAttackUntil = Time.time + DashTime + 1.5f;
        dashHits.Clear();
        float dmg = EffectManager.GetValue(PassiveEffects.EntityDamage, player.inventory.holdingItemItemValue, 10f, player);
        dashHit = dmg * 2f * 2f;   // a power hit is about x2, Breakthrough x2
        Debug.Log("[AposMod] Breakthrough: dash, hit " + Mathf.RoundToInt(dashHit));
        // feedback: a line under the crosshair, a heavy whoosh, a short gold flash in front
        AposFx.Toast(Localization.Get("aposBreakthroughToast"), new Color(1f, 0.82f, 0.3f));
        Audio.Manager.PlayInsidePlayerHead("sledge_swingheavy", player.entityId);
        AposFx3D.Flash(player.position + dashDir * 2f + Vector3.up, new Color(1f, 0.8f, 0.4f), 6f, 3f, 0.3f);
    }

    static void DashStep(EntityPlayerLocal player)
    {
        Vector3 next = player.position + dashDir * (DashSpeed * Time.deltaTime);
        if (Blocked(next + dashDir * 0.4f)) { dashUntil = 0f; return; }
        player.SetPosition(next, true);
        foreach (EntityAlive e in AposMelee.Around(next, 1.4f))
        {
            if (!dashHits.Add(e.entityId)) continue;
            AposSplash.Deal(e, Mathf.Max(1, Mathf.RoundToInt(dashHit)), player.entityId, EnumDamageTypes.Piercing);
            if (!e.IsDead()) AposKnock.Ragdoll(e, player.position, 300f, 2f);
            // every zombie on the way: a gold ring under it, a clang, a line in the feed (merged into "xN")
            AposFx3D.Shockwave(e.position, 1.5f, new Color(1f, 0.8f, 0.3f), 0.35f);
            Audio.Manager.PlayInsidePlayerHead("metalhitmetal", player.entityId);
            AposReactions.Add(AposReactions.Breakthrough, Mathf.Max(1, Mathf.RoundToInt(dashHit)), 1);
        }
    }

    // a solid block at knee or chest height in front
    static bool Blocked(Vector3 pos)
    {
        World w = GameManager.Instance.World;
        for (int i = 0; i < 2; i++)
        {
            BlockValue bv = w.GetBlock(World.worldToBlockPos(pos + Vector3.up * (i == 0 ? 0.6f : 1.4f)));
            if (!bv.isair && bv.Block != null && bv.Block.IsCollideMovement) return true;
        }
        return false;
    }
}

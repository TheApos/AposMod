// Weapon Mastery 2.0, machine guns (docs/Design-Mastery2.md 5г; driven by AposMastery.cs):
// - Class trait "Suppression": every hit slows the zombie by 5% for 8 s, up to 20%; every 4th hit puts a round back.
//   Full Commando set: the cap +20% and every 3rd hit refunds. (It used to be XML scaled by Machine Gunner.)
// - Branch A (suppression): Heavy Fire, Thrifty, Pin Down, Quick Suppression, Explosive Rounds, Hold the Line,
//   Kill Zone, capstone Wall of Fire.
// - Branch B (spin-up): Spin Up, Iron Grip (x Fortitude), Drum Mag, Build-Up, Belt Feed, Shred, Momentum,
//   capstone Minigun.
// - Hybrids: Lead Storm, Panic, Emplacement.
// The zombie's slow lives in buffAposSuppressed (buffs.xml) with cvars set here: aposSupSlow (negative), aposSupFull,
// aposSupTime. Shots come from XML (aposMShot on the player).
using System.Collections.Generic;
using UnityEngine;

public static class AposMG
{
    static readonly float[] SlowMax = { 0.20f, 0.30f, 0.50f, 0.80f };
    static readonly int[] RefundEvery = { 4, 3, 3, 2 };
    static readonly float[] PinDown = { 0f, 0.15f, 0.30f, 0.50f };
    static readonly float[] SlowPer = { 0.05f, 0.15f, 0.20f, 0.40f };
    static readonly float[] HoldDmg = { 0f, 0.15f, 0.40f }, HoldRecoil = { 0f, 0.25f, 0.60f };
    static readonly float[] Zone = { 0f, 2f, 3f };
    static readonly float[] SpinRate = { 0f, 4f, 7f, 12f };
    static readonly float[] Grip = { 0f, 3f, 5f, 8f };      // % recoil per Fortitude level, max 80
    static readonly float[] Drum = { 0f, 0.20f, 0.40f, 0.75f };
    static readonly float[] BuildUp = { 0f, 3f, 6f, 10f };
    static readonly float[] ShredBleed = { 0f, 0.5f, 0.9f }, ShredLimb = { 0f, 0.25f, 0.6f };
    static readonly float[] Momentum = { 0f, 1f, 0.5f };    // stacks lost per second when you stop (0 = all at once)

    static int R(string id) { return AposMastery.Rank(AposMastery.MG, id); }

    static float spin;             // spin-up stacks (float so Momentum can melt them)
    static float lastShot = -10f, fireStart = -10f, secondTimer;
    static int refundHits, bullet, freeShots;
    static float maxSince = -1f, overheatUntil;
    static bool wasReloading;
    static float nextCone;

    public static void Reset()
    {
        spin = 0f; lastShot = -10f; refundHits = bullet = freeShots = 0; maxSince = -1f; overheatUntil = 0f; wasReloading = false;
    }

    static int MaxStacks() { return R("B8") > 0 ? 10 : 5; }
    static bool FullSpin() { return R("B1") > 0 && spin >= MaxStacks() - 0.01f; }

    static bool StandingStill(EntityPlayerLocal p)
    {
        return p.IsCrouching || p.GetVelocityPerSecond().sqrMagnitude < 0.04f;
    }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        float shots = player.Buffs.GetCustomVar("aposMShot");
        if (shots > 0f)
        {
            player.Buffs.SetCustomVar("aposMShot", 0f);
            if (held) OnShots(player, Mathf.RoundToInt(shots));
        }
        if (!held) { spin = 0f; return; }

        bool firing = Time.time - lastShot < 0.35f;
        // spin-up: a stack for every second of continuous fire
        if (R("B1") > 0)
        {
            if (firing)
            {
                secondTimer += Time.deltaTime;
                if (secondTimer >= 1f) { secondTimer -= 1f; spin = Mathf.Min(MaxStacks(), spin + 1f); }
            }
            else
            {
                secondTimer = 0f;
                int b7 = R("B7");
                spin = b7 > 0 ? Mathf.Max(0f, spin - Momentum[b7] * Time.deltaTime) : 0f;
            }
        }
        // a reload resets the spin, unless Belt Feed at full spin
        bool reloading = Reloading(player);
        if (reloading && !wasReloading && !(R("B5") > 0 && FullSpin())) spin = 0f;
        wasReloading = reloading;
        // Minigun: 8 s at max spin overheats it for 2 s
        if (R("B8") > 0)
        {
            if (FullSpin() && firing) { if (maxSince < 0f) maxSince = Time.time; }
            else maxSince = -1f;
            if (maxSince > 0f && Time.time - maxSince >= 8f) { overheatUntil = Time.time + 2f; maxSince = -1f; spin = 0f; }
        }

        int stacks = Mathf.FloorToInt(spin);
        float speed = SpinRate[R("B1")] * stacks;
        float recoil = 0f;
        if (firing) recoil -= Mathf.Min(80f, Grip[R("B2")] * AposAffixes.Level(player, "attFortitude"));
        if (R("A6") > 0 && StandingStill(player)) recoil -= HoldRecoil[R("A6")] * 100f;
        bool emplacement = R("H3") > 0 && player.IsCrouching && FullSpin();
        if (emplacement) recoil = -100f;
        if (Time.time < overheatUntil) speed = -95f;               // overheated: barely fires
        AposMastery.SetStat(AposMastery.StatSpeed, speed / 100f);
        AposMastery.SetStat(AposMastery.StatSpread, Mathf.Max(-1f, recoil / 100f));
        AposMastery.SetStat(AposMastery.StatMag, Drum[R("B3")] > 0f ? Mathf.Round(BaseMag(player) * Drum[R("B3")]) : 0f);
        // Wall of Fire: you move 40% slower while firing
        if (R("A8") > 0 && firing) player.Buffs.AddBuff("buffAposWallSlow");

        // Emplacement: zombies in a 15 m cone in front are slowed by 30% even without a hit
        if (emplacement && Time.time >= nextCone)
        {
            nextCone = Time.time + 0.5f;
            Vector3 fwd = player.GetLookVector();
            foreach (EntityAlive e in Around(player.position, 15f))
            {
                Vector3 d = e.position - player.position;
                if (Vector3.Angle(new Vector3(fwd.x, 0f, fwd.z), new Vector3(d.x, 0f, d.z)) <= 25f) AposPistol.Shackle(e, 0.3f, 1f);
            }
        }

        // HUD: spin stacks, overheat
        if (R("B1") > 0)
        {
            Color c = Time.time < overheatUntil ? new Color(1f, 0.3f, 0.2f) : new Color(0.4f, 0.75f, 1f);
            string text = Time.time < overheatUntil ? Mathf.CeilToInt(overheatUntil - Time.time) + "s" : stacks + "/" + MaxStacks();
            bool hot = Time.time < overheatUntil;
            AposHud.Gauge(false, hot ? (overheatUntil - Time.time) / 2f : stacks, hot ? 0 : MaxStacks(), c, FullSpin(), "speed", text, "B1");
        }
        if (emplacement) AposHud.Add(false, "ui_apos_holdline", "", new Color(1f, 0.82f, 0.3f), true, "A6");
    }

    static bool Reloading(EntityPlayerLocal p)
    {
        ItemInventoryData d = p.inventory.holdingItemData;
        if (d == null || d.actionData == null || d.actionData.Count == 0) return false;
        ItemActionRanged.ItemActionDataRanged r = d.actionData[0] as ItemActionRanged.ItemActionDataRanged;
        return r != null && r.isReloading;
    }

    static int BaseMag(EntityPlayerLocal p)
    {
        ItemClass held = p.inventory.holdingItem;
        ItemActionRanged ranged = held != null && held.Actions != null && held.Actions.Length > 0 ? held.Actions[0] as ItemActionRanged : null;
        return ranged != null ? ranged.BulletsPerMagazine : 0;
    }

    static void OnShots(EntityPlayerLocal player, int n)
    {
        if (Time.time - lastShot > 0.35f) fireStart = Time.time;
        lastShot = Time.time;
        // Emplacement: every 3rd bullet is free
        if (R("H3") > 0 && player.IsCrouching && FullSpin())
        {
            freeShots += n;
            if (freeShots >= 3) { freeShots -= 3; AposAmmo.LoadRounds(player, 1); }
        }
    }

    // ===== hits =====
    static int lastTarget = -1;
    static bool lastFull, lastBlast;

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        float mult = 1f;
        lastFull = target.Buffs.GetCustomVar("aposSupFull") > 0f;
        if (lastFull) mult += PinDown[R("A3")];
        if (R("A6") > 0 && StandingStill(player)) mult += HoldDmg[R("A6")];
        mult += BuildUp[R("B4")] * Mathf.Floor(spin) / 100f;
        // Explosive Rounds: every 5th bullet hits x2 and explodes
        lastBlast = false;
        if (R("A5") > 0 && ++bullet >= 5)
        {
            bullet = 0;
            mult *= 2f;
            lastBlast = true;
        }
        lastTarget = target.entityId;
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || target.entityId != lastTarget) return;
        lastTarget = -1;
        bool full = FullSpin();
        int weight = R("H1") > 0 && full ? 2 : 1;               // Lead Storm: a hit counts twice
        bool commando = player.Buffs.HasBuff("buffCommandoSetBonus");

        // round refund
        int every = Mathf.Max(2, RefundEvery[R("A2")] - (commando ? 1 : 0));
        refundHits += weight;
        while (refundHits >= every) { refundHits -= every; AposAmmo.LoadRounds(player, 1); }

        if (response.Fatal)
        {
            if (R("A2") >= 2) AposAmmo.LoadRounds(player, 2);
            if (R("B5") > 0 && full) AposAmmo.LoadRounds(player, 3);
            // Panic: a fully suppressed zombie killed by you fully suppresses everyone within 8 m
            if (R("H2") > 0 && lastFull)
            {
                foreach (EntityAlive e in Around(target.position, 8f)) Suppress(player, e, 100, true);
            }
        }
        else
        {
            Suppress(player, target, weight, false);
            // Shred at full spin: bleeding and torn-off limbs
            int b6 = R("B6");
            if (b6 > 0 && full)
            {
                if (Random.value < ShredBleed[b6]) target.Buffs.AddBuff("buffInjuryBleeding");
                if (Random.value < ShredLimb[b6]) AposMastery.RequestDismember();
            }
        }
        // Kill Zone: the suppression spreads to neighbours at half strength
        int a7 = R("A7");
        if (a7 > 0)
        {
            foreach (EntityAlive e in Around(target.position, Zone[a7])) if (e != target) Suppress(player, e, weight, false, 0.5f);
        }
        // Explosive Rounds: 50% of the bullet to everyone within 2 m (3 m around a fully suppressed one), suppressing them
        if (lastBlast)
        {
            float r = lastFull ? 3f : 2f;
            int share = Mathf.Max(1, Mathf.RoundToInt(response.Strength * 0.5f));
            List<EntityAlive> hit = new List<EntityAlive>(Around(target.position, r));
            target.Buffs.AddBuff("buffAposChainBlast"); // the explosion look
            foreach (EntityAlive e in hit)
            {
                if (e == target) continue;
                AposSplash.Deal(e, share, player.entityId, EnumDamageTypes.Heat);
                if (!e.IsDead()) Suppress(player, e, 1, false);
            }
        }
    }

    // adds suppression stacks to a zombie (all = straight to the cap)
    static void Suppress(EntityPlayerLocal player, EntityAlive e, int hits, bool all, float strength = 1f)
    {
        if (e == null || e.IsDead() || e.EntityTags.Test_AnySet(TraderTag)) return;
        bool commando = player.Buffs.HasBuff("buffCommandoSetBonus");
        float cap = SlowMax[R("A1")] + (commando ? 0.2f : 0f);
        float slow = all ? cap : Mathf.Min(cap, -e.Buffs.GetCustomVar("aposSupSlow") + SlowPer[R("A4")] * hits * strength);
        bool full = slow >= cap - 0.001f;
        e.Buffs.SetCustomVar("aposSupSlow", -slow);
        e.Buffs.SetCustomVar("aposSupFull", full ? 1f : 0f);
        e.Buffs.SetCustomVar("aposSupTime", 8f);
        if (!e.Buffs.HasBuff("buffAposSuppressed")) e.Buffs.AddBuff("buffAposSuppressed");
        // Wall of Fire: a fully suppressed zombie stops while you keep hitting it (and 2 s more); elites only -80%
        if (full && R("A8") > 0)
        {
            e.Buffs.SetCustomVar("aposWallStop", AposElites.IsElite(e) ? -0.8f : -1f);
            e.Buffs.AddBuff("buffAposWallStop", -1, true, false, 2f);
        }
    }

    static readonly FastTags<TagGroup.Global> TraderTag = FastTags<TagGroup.Global>.Parse("trader");

    static IEnumerable<EntityAlive> Around(Vector3 pos, float radius)
    {
        List<Entity> list = GameManager.Instance.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e is EntityPlayer || e.IsDead()) continue;
            if ((e.position - pos).sqrMagnitude <= radius * radius) yield return e;
        }
    }
}

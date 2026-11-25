// Weapon Mastery 2.0, pistols and SMGs (docs/Design-Mastery2.md, section 5; driven by AposMastery.cs):
// - Class trait "Headhunter": 5 headshots in a row -> 6 empowered shots (+20% damage, almost no spread: buffAposHeadhunter).
//   A miss or a body hit breaks the streak.
// - Branch A (Headhunter): Keen Eye, Steady Hand, The Hunt Goes On, Cold Blood, Marksman's Eye, Lethal Precision,
//   Verdict, capstone Duel.
// - Branch B (Lead Storm): Quick Finger, Extended Mag, Speed Loader, Lead Shackles (x Agility), Hot Barrel,
//   Spare Rounds, Lead Rain, capstone Bottomless Mag.
// - Hybrids: Firing Squad, Gunfighter, Blood Fireworks.
// Shots and misses come from XML (entityclasses.xml "AposMod pistol events": cvars aposPShot / aposPMiss on the
// player), hits from the damage hook (AposCombat.cs). The streak and the mode live here (local player only).
using System.Collections.Generic;
using UnityEngine;

public static class AposPistol
{
    const int BaseNeed = 5;
    const int BaseShots = 6;
    const string ModeBuff = "buffAposHeadhunter";
    const float SquadHeadMult = 2f;       // Firing Squad: a body hit counted as a headshot
    const float VerdictBelow = 0.35f;
    const float MarkRange = 40f;
    const float FireworksRadius = 3f;

    static readonly float[] KeenEye = { 0f, 0.08f, 0.16f, 0.30f };
    static readonly float[] SteadyHand = { 0f, 10f, 20f, 35f };
    static readonly float[] Empower = { 0.2f, 0.4f, 0.7f };        // by Lethal Precision rank
    static readonly float[] VerdictChance = { 0f, 15f, 35f };
    static readonly float[] QuickFinger = { 0f, 18f, 36f, 55f };
    static readonly int[] MagRounds = { 0, 2, 4, 6 };
    static readonly float[] MagPct = { 0f, 0.15f, 0.30f, 0.45f };
    static readonly float[] SpeedLoader = { 0f, 12f, 24f, 45f };
    static readonly float[] Shackles = { 0f, 3f, 5f, 8f };         // % slow per Agility level, max 80%
    static readonly int[] KillRounds = { 0, 1, 2 };
    static readonly int[] LimbRounds = { 0, 3, 5 };
    static readonly float[] LeadRain = { 0f, 0.15f, 0.40f };

    static int streak, modeShots, hitsInRow;
    static float hot, lastHitTime, gunfighterUntil;
    // the hit being processed (ModifyHit -> AfterHit)
    static int lastTarget = -1;
    static bool lastHead, lastMode;

    static int R(string id) { return AposMastery.Rank(AposMastery.Pistols, id); }

    public static bool InMode { get { return modeShots > 0; } }

    public static void Reset()
    {
        streak = modeShots = hitsInRow = 0;
        hot = 0f;
        gunfighterUntil = 0f;
        lastTarget = -1;
    }

    static int Need(EntityPlayerLocal player)
    {
        int a4 = R("A4");
        int need = BaseNeed - (a4 >= 1 ? 1 : 0) - (a4 >= 3 ? 1 : 0);
        if (player.Buffs.HasBuff("buffEnforcerSetBonus")) need--; // full Gunslinger set
        return Mathf.Max(2, need);
    }

    // Cold Blood 2/3: a miss or a body hit takes away one headshot instead of the whole streak
    static void BreakStreak()
    {
        streak = R("A4") >= 2 ? Mathf.Max(0, streak - 1) : 0;
    }

    static void Activate(EntityPlayerLocal player)
    {
        streak = 0;
        modeShots = BaseShots + 3 * R("A3");
        player.Buffs.SetCustomVar("aposPShot", 0f); // the activating shot is not one of the empowered ones
        player.Buffs.SetCustomVar("aposHHShots", modeShots);
        player.Buffs.AddBuff(ModeBuff);
    }

    static void EndMode(EntityPlayerLocal player)
    {
        modeShots = 0;
        player.Buffs.RemoveBuff(ModeBuff);
    }

    // every frame from AposMastery.Update; held = a pistol / SMG is in hand
    public static void Tick(EntityPlayerLocal player, bool held)
    {
        float shots = player.Buffs.GetCustomVar("aposPShot");
        if (shots > 0f)
        {
            player.Buffs.SetCustomVar("aposPShot", 0f);
            if (held) OnShots(player, Mathf.RoundToInt(shots));
        }
        if (player.Buffs.GetCustomVar("aposPMiss") > 0f)
        {
            player.Buffs.SetCustomVar("aposPMiss", 0f);
            // one shot can fire both miss triggers: count it once
            if (held && modeShots <= 0) BreakStreak();
            hitsInRow = 0;
        }
        // Hot Barrel: melts 3% per second once you stop hitting
        if (hot > 0f && Time.time - lastHitTime > 1f) hot = Mathf.Max(0f, hot - 3f * Time.deltaTime);

        if (modeShots > 0)
        {
            if (player.Buffs.GetCustomVar("aposHHShots") != modeShots) player.Buffs.SetCustomVar("aposHHShots", modeShots);
            if (!player.Buffs.HasBuff(ModeBuff)) player.Buffs.AddBuff(ModeBuff);
        }
        else if (player.Buffs.HasBuff(ModeBuff)) player.Buffs.RemoveBuff(ModeBuff);

        if (!held) return;
        Widgets(player);
        float speed = QuickFinger[R("B1")] + hot + (Time.time < gunfighterUntil ? 15f : 0f);
        AposMastery.SetStat(AposMastery.StatSpeed, speed / 100f);
        AposMastery.SetStat(AposMastery.StatReload, SpeedLoader[R("B3")] / 100f);
        AposMastery.SetStat(AposMastery.StatSpread, (-SteadyHand[R("A2")] + (R("B8") > 0 ? 30f : 0f)) / 100f);
        AposMastery.SetStat(AposMastery.StatMag, MagBonus(player));
    }

    static readonly Color Cyan = new Color(0.25f, 0.9f, 1f), Dim = new Color(0.75f, 0.75f, 0.75f), Orange = new Color(1f, 0.55f, 0.15f);

    // combat HUD next to the crosshair (AposHud.cs)
    static void Widgets(EntityPlayerLocal player)
    {
        // Headhunter: the streak (3/5) before the mode, empowered shots left while it runs
        if (modeShots > 0) AposHud.Add(false, "head_shot", modeShots.ToString(), Cyan, true);
        else AposHud.Add(false, "head_shot", streak + "/" + Need(player), streak > 0 ? Color.white : Dim, false);
        if (R("B5") > 0 && hot >= 1f) AposHud.Add(false, "hot", "+" + Mathf.RoundToInt(hot) + "%", Orange, hot >= 29.5f);
        if (R("H1") > 0) AposHud.Add(false, "run_and_gun", hitsInRow + "/8", hitsInRow >= 7 ? Color.white : Dim, hitsInRow >= 7);
        if (R("H2") > 0 && Time.time < gunfighterUntil) AposHud.Add(false, "pistol", "+15%", Color.white, false);
    }

    // Extended Mag: +rounds or +% of the magazine, whichever is more
    static float MagBonus(EntityPlayerLocal player)
    {
        int b2 = R("B2");
        if (b2 == 0) return 0f;
        ItemActionRanged ranged = Ranged(player);
        int baseMag = ranged != null ? ranged.BulletsPerMagazine : 0;
        return Mathf.Max(MagRounds[b2], Mathf.Round(baseMag * MagPct[b2]));
    }

    static ItemActionRanged Ranged(EntityPlayerLocal player)
    {
        ItemClass held = player.inventory.holdingItem;
        return held != null && held.Actions != null && held.Actions.Length > 0 ? held.Actions[0] as ItemActionRanged : null;
    }

    static void OnShots(EntityPlayerLocal player, int n)
    {
        // Bottomless Mag: 25% of shots cost no round (Lead Rain etc. see the refilled magazine)
        if (R("B8") > 0)
        {
            for (int i = 0; i < n; i++) if (Random.value < 0.25f) AposAmmo.LoadRounds(player, 1);
        }
        if (modeShots > 0)
        {
            modeShots -= n;
            if (modeShots <= 0) EndMode(player);
        }
    }

    // Lead Rain: the second half of the magazine
    static bool MagHalfEmpty(EntityPlayerLocal player)
    {
        ItemActionRanged ranged = Ranged(player);
        if (ranged == null) return false;
        ItemValue iv = player.inventory.holdingItemItemValue;
        float max = EffectManager.GetValue(PassiveEffects.MagazineSize, iv, ranged.BulletsPerMagazine, player);
        return iv.Meta < max / 2f;
    }

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        bool head = (source.GetEntityDamageBodyPart(target) & EnumBodyPartHit.Head) != 0;
        float mult = 1f;

        // Firing Squad: every 8th hit in a row is a headshot
        bool squad = false;
        if (R("H1") > 0 && ++hitsInRow >= 8)
        {
            hitsInRow = 0;
            squad = !head;
        }
        bool asHead = head || squad;
        if (squad) mult *= SquadHeadMult;
        if (asHead) mult += KeenEye[R("A1")];

        bool mode = modeShots > 0;
        lastTarget = target.entityId;
        lastHead = asHead;
        lastMode = mode;
        int result = -1;
        if (mode)
        {
            if (!asHead && R("A8") > 0)
            {
                EndMode(player); // Duel: a body hit ends the mode, this shot is not empowered
                lastMode = false;
            }
            else
            {
                float emp = 1f + Empower[R("A6")];
                if (R("A8") > 0) emp *= 2f;
                mult *= emp;
                // Verdict: an empowered headshot may execute a weakened non-elite
                int a7 = R("A7");
                if (a7 > 0 && asHead && !AposElites.IsElite(target) && target.Health < target.GetMaxHealth() * VerdictBelow
                    && Random.value * 100f < VerdictChance[a7])
                {
                    result = target.GetMaxHealth() * 2;
                }
            }
        }
        else if (asHead)
        {
            if (++streak >= Need(player)) Activate(player);
        }
        else BreakStreak();

        int b7 = R("B7");
        if (b7 > 0 && MagHalfEmpty(player)) mult += LeadRain[b7];
        // Gunfighter: a headshot puts a round back and speeds you up for 3 s
        if (asHead && R("H2") > 0)
        {
            AposAmmo.LoadRounds(player, 1);
            gunfighterUntil = Time.time + 3f;
        }
        if (R("B5") > 0) hot = Mathf.Min(30f, hot + 3f);
        lastHitTime = Time.time;
        // Bottomless Mag on a shocked zombie: 50% free in total (25% per shot + 1/3 of the rest here)
        if (R("B8") > 0 && target.Buffs.HasBuff("buffShocked") && Random.value < 1f / 3f) AposAmmo.LoadRounds(player, 1);

        if (result > 0) return result;
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || target.entityId != lastTarget) return;
        lastTarget = -1;
        // Spare Rounds: kills and torn-off limbs load rounds
        int b6 = R("B6");
        if (b6 > 0)
        {
            int n = (response.Fatal ? KillRounds[b6] : 0) + (response.Dismember ? LimbRounds[b6] : 0);
            if (n > 0) AposAmmo.LoadRounds(player, n);
        }
        if (response.Fatal && lastHead && lastMode && modeShots > 0)
        {
            if (R("A3") >= 3) modeShots += 2; // The Hunt Goes On 3/3
            if (R("H3") > 0) Fireworks(player, target);
        }
        // Lead Shackles: every hit slows the target for 3 s (rank x Agility, up to 80%)
        int b4 = R("B4");
        if (!response.Fatal && b4 > 0)
        {
            float slow = Mathf.Min(80f, Shackles[b4] * AposAffixes.Level(player, "attAgility"));
            if (slow > 0f) Shackle(target, slow / 100f, 3f);
        }
    }

    public static void Shackle(EntityAlive target, float slow, float seconds)
    {
        if (target.Buffs.HasBuff("buffAposShackles") && target.Buffs.GetCustomVar("aposShackle") > slow) slow = target.Buffs.GetCustomVar("aposShackle");
        target.Buffs.SetCustomVar("aposShackle", slow);
        target.Buffs.AddBuff("buffAposShackles", -1, true, false, seconds);
    }

    // Blood Fireworks: a headshot kill in the mode bursts the head: zombies within 3 m slowed by 80% and stunned for 1 s, +3 rounds
    static void Fireworks(EntityPlayerLocal player, EntityAlive center)
    {
        List<Entity> list = GameManager.Instance.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e == center || e is EntityPlayer || e.IsDead()) continue;
            if ((e.position - center.position).sqrMagnitude > FireworksRadius * FireworksRadius) continue;
            e.Buffs.AddBuff("buffInjuryStunned01", -1, true, false, 1f);
            Shackle(e, 0.8f, 3f);
        }
        AposAmmo.LoadRounds(player, 3);
    }

    // ===== Marksman's Eye: while the mode runs, heads of zombies within 40 m are marked, even behind cover =====
    static readonly List<EntityAlive> marked = new List<EntityAlive>();
    static readonly List<bool> seen = new List<bool>(); // in sight: cyan ring; behind cover: dim grey (no false alarm)
    static float nextScan;
    static Texture2D ring;

    public static void Draw(EntityPlayerLocal player)
    {
        if (player == null || modeShots <= 0 || R("A5") == 0 || AposMastery.HeldClass != AposMastery.Pistols) return;
        Camera cam = player.playerCamera;
        if (cam == null) return;
        if (Time.time >= nextScan)
        {
            nextScan = Time.time + 0.2f;
            marked.Clear();
            seen.Clear();
            List<Entity> list = GameManager.Instance.World.Entities.list;
            for (int i = 0; i < list.Count; i++)
            {
                EntityEnemy e = list[i] as EntityEnemy;
                if (e == null || e.IsDead() || (e.position - player.position).sqrMagnitude > MarkRange * MarkRange) continue;
                marked.Add(e);
                seen.Add(AposElites.InSight(player, e)); // a clear line to the head, not just to some part of the body
            }
        }
        if (ring == null) ring = MakeRing(32);
        Color old = GUI.color;
        for (int i = 0; i < marked.Count; i++)
        {
            EntityAlive e = marked[i];
            if (e == null || e.IsDead()) continue;
            GUI.color = seen[i] ? new Color(0.2f, 0.9f, 1f, 0.85f) : new Color(0.7f, 0.7f, 0.7f, 0.35f);
            // the head bone (scene space, floating origin); the body height as a fallback
            Transform head = e.emodel != null ? e.emodel.GetHeadTransform() : null;
            Vector3 at = head != null ? head.position : e.transform.position + Vector3.up * e.height * 0.9f;
            Vector3 v = cam.WorldToViewportPoint(at);
            if (v.z <= 0f) continue;
            // the ring frames the head: big up close, small far away
            // the ring (80% of the texture) is ~0.3 m across, a little wider than a zombie head
            Vector3 top = cam.WorldToViewportPoint(at + cam.transform.up * 0.3f);
            float size = Mathf.Max(8f, Mathf.Abs(top.y - v.y) * Screen.height * 1.25f);
            GUI.DrawTexture(new Rect(v.x * Screen.width - size / 2f, (1f - v.y) * Screen.height - size / 2f, size, size), ring);
        }
        GUI.color = old;
    }

    static Texture2D MakeRing(int n)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.15f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return tex;
    }
}

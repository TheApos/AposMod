// Weapon Mastery 2.0, bows (docs/Design-Mastery2.md 5ґ; driven by AposMastery.cs):
// - Class trait "Splinter": an arrow hit bursts into 2 splinters, each flies to the nearest other zombie within 5 m and
//   hits for 50% of the arrow (at least 40). Full Hunter set: +2 splinters. (It used to be XML scaled by Archery.)
// - Branch A (splinters, crowds): More Splinters, Sharp Splinters, Wide Scatter, Elemental Splinters, Splinter Chain,
//   Shrapnel Wound, Shock Trap, capstone Splinter Storm.
// - Branch B (the arrow, strong targets): Full Draw (x Agility), Piercing Arrow, Hunter's Wound, Quick Draw, Shadow,
//   Marksmanship, Arrow Recovery, capstone Lance Arrow.
// - Hybrids: Barbed Splinters, Distraction, Thousand Needles.
// The draw is watched every frame: while the string is pulled (ItemActionDataCatapult.m_bActivated) it is
// (now - m_ActivateTime) / m_MaxStrainTime; when it is let go, that value is the draw of the arrow that left.
// (GetStrainPercent is the LAST shot's draw and never resets, so it is not used for the live value.)
// Hunter's Wound lives on the zombie (buffAposWound, cvars aposWound / aposWoundDmg); its damage bonus is in AposCombat.
using System.Collections.Generic;
using UnityEngine;

public static class AposBowTree
{
    static readonly int[] MoreSplinters = { 0, 2, 4, 8 };
    static readonly float[] SplShare = { 0.5f, 0.6f, 0.75f, 1f }, SplMin = { 40f, 60f, 80f, 100f };
    static readonly float[] SplRadius = { 5f, 7f, 9f, 15f };
    static readonly float[] SplElem = { 0f, 0.25f, 0.5f, 1f };
    static readonly float[] SplSlow = { 0f, 0.25f, 0.6f };
    static readonly float[] FullDraw = { 0f, 3f, 5f, 10f };          // % per Agility level, max 100
    static readonly int[] PierceCount = { 0, 1, 2, 4 };
    static readonly float[] PierceDmg = { 0f, 0.5f, 0.6f, 1f };
    static readonly float[] WoundDmg = { 0f, 0.2f, 0.3f, 0.5f }, WoundSlow = { 0f, 0.2f, 0.25f, 0.4f };
    static readonly float[] QuickDraw = { 0f, 15f, 30f, 50f };
    static readonly float[] Marksman = { 0f, 0.25f, 0.75f };
    static readonly float[] Recovery = { 0f, 0.5f, 1f };

    static int R(string id) { return AposMastery.Rank(AposMastery.Bows, id); }

    // ===== the draw =====
    static float strain, lastDraw, releasedAt = -10f;
    static bool shadowNext;

    static bool FullShot() { return lastDraw >= 0.97f && Time.time - releasedAt < 5f; }

    public static void Reset() { strain = lastDraw = 0f; releasedAt = -10f; shadowNext = false; foreach (Mine m in mines) AposFx.Unmark(m); mines.Clear(); }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        RunMines(player);
        if (!held) { strain = 0f; return; }
        ItemClass ic = player.inventory.holdingItem;
        ItemActionCatapult cat = ic != null && ic.Actions != null && ic.Actions.Length > 0 ? ic.Actions[0] as ItemActionCatapult : null;
        ItemInventoryData d = player.inventory.holdingItemData;
        ItemActionCatapult.ItemActionDataCatapult cd = cat != null && d != null && d.actionData != null && d.actionData.Count > 0
            ? d.actionData[0] as ItemActionCatapult.ItemActionDataCatapult : null;
        if (cd != null)
        {
            float s = cd.m_bActivated && cd.m_MaxStrainTime > 0f ? Mathf.Clamp01((Time.time - cd.m_ActivateTime) / cd.m_MaxStrainTime) : 0f;
            // the string went from drawn to slack: an arrow left with the draw it had a frame ago (unless cancelled)
            if (strain > 0.05f && !cd.m_bActivated && !cd.m_bCanceled) { lastDraw = strain; releasedAt = Time.time; }
            strain = s;
        }
        else strain = 0f;
        float speed = QuickDraw[R("B4")] / 100f;
        AposMastery.SetStat(AposMastery.StatSpeed, speed);
        AposMastery.SetStat(AposMastery.StatReload, speed);
        if (strain > 0f) AposHud.AttackAt = AposHud.BesideDrawBar;   // next to the vanilla draw bar, not over it
        if (player.Buffs.HasBuff("buffAposBowShadow")) AposHud.Add(false, "stealth", "", new Color(0.6f, 0.6f, 1f), true);
        if (R("B1") > 0 && strain >= 0.97f) AposHud.Add(false, "archery", "+" + Mathf.RoundToInt(DrawBonus(player) * 100f) + "%", new Color(1f, 0.82f, 0.3f), true);
    }

    static float DrawBonus(EntityPlayerLocal p) { return Mathf.Min(1f, FullDraw[R("B1")] * AposAffixes.Level(p, "attAgility") / 100f); }

    static bool IsArrow(DamageSource source)
    {
        string ammo = AposProjectiles.AmmoOf(source);
        return ammo != null && ammo.StartsWith("ammoArrow");
    }

    // ===== hits =====
    static int lastTarget = -1;
    static bool lastFull, lastHead, lastArrow;
    static float lastArrowDmg;
    static string lastAmmo;

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        lastArrow = IsArrow(source);
        if (player == null || !lastArrow) return strength;
        bool full = FullShot(), head = (source.GetEntityDamageBodyPart(target) & EnumBodyPartHit.Head) != 0;
        float mult = 1f;
        if (full)
        {
            mult += DrawBonus(player);
            if (head) mult += Marksman[R("B6")];
            if (R("B8") > 0) mult *= 2.5f;
        }
        if (shadowNext) { mult *= 2f; shadowNext = false; }   // Shadow: the next arrow counts as from stealth
        lastArrowDmg = strength * mult;                         // splinters count from the arrow before Splinter Storm
        if (R("A8") > 0) mult *= 0.5f;
        lastTarget = target.entityId; lastFull = full; lastHead = head;
        lastAmmo = AposProjectiles.AmmoOf(source);
        AposProjectiles.NoteEntityHit();
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || !lastArrow || target.entityId != lastTarget) return;
        lastTarget = -1;
        bool full = lastFull;
        if (response.Fatal)
        {
            // Arrow Recovery: the arrow that killed comes back
            if (R("B7") > 0 && Random.value < Recovery[R("B7")] && lastAmmo != null)
            {
                ItemValue arrow = ItemClass.GetItem(lastAmmo);
                if (arrow != null && !arrow.IsEmpty())
                {
                    player.bag.AddItem(new ItemStack(arrow, 1));
                    Debug.Log("[AposMod] Arrow Recovery: " + lastAmmo + " back (killed " + target.EntityName + ")");
                }
            }
            // Shadow: a silent kill (the zombie did not see you) hides you for 5 s, the next arrow counts as from stealth
            if (R("B5") > 0 && (target.IsSleeping || target.GetAttackTarget() != player))
            {
                player.Buffs.AddBuff("buffAposBowShadow");
                shadowNext = true;
            }
        }
        else if (full && R("B3") > 0) Wound(target);

        // Piercing Arrow / Lance Arrow: a full draw goes on through the zombies behind
        if (full && (R("B2") > 0 || R("B8") > 0)) Pierce(player, target);

        // Splinters (Lance Arrow: only from shots that were not fully drawn)
        if (!(full && R("B8") > 0)) Burst(player, target.position, target, Splinters(player), Element(target), full, true);
        // Shock Trap: 1.5 s later the arrow stuck in it discharges electricity around
        if (R("A7") > 0) AddMine(new Mine { At = Time.time + 1.5f, Pos = target.position, Dmg = lastArrowDmg, Body = response.Fatal ? null : target });
    }

    static int Splinters(EntityPlayerLocal player)
    {
        int n = 2 + MoreSplinters[R("A1")] + (player.Buffs.HasBuff("buffFarmerSetBonus") ? 2 : 0);
        return R("A8") > 0 ? n * 2 : n;
    }

    // which element the splinters carry: the arrow's own fire, or one the target already has (elemental weapon mods)
    static string Element(EntityAlive target)
    {
        if (lastAmmo != null && lastAmmo.Contains("Flaming")) return "buffBurningFlamingArrow";
        string[] own = { "buffAposCorroded", "buffShocked", "buffBurningElement", "buffInjuryBleeding" };
        foreach (string b in own) if (target != null && target.Buffs.HasBuff(b)) return b;
        return null;
    }

    public static void Wound(EntityAlive e)
    {
        int b3 = Mathf.Max(1, R("B3"));
        int stacks = Mathf.Min(2, (e.Buffs.HasBuff("buffAposWound") ? (int)e.Buffs.GetCustomVar("aposWound") : 0) + 1);
        e.Buffs.SetCustomVar("aposWound", stacks);
        e.Buffs.SetCustomVar("aposWoundDmg", WoundDmg[b3]);
        e.Buffs.SetCustomVar("aposWoundSlow", -WoundSlow[b3] * stacks);
        e.Buffs.AddBuff("buffAposWound", -1, true, false, 8f);
    }

    static void Pierce(EntityPlayerLocal player, EntityAlive target)
    {
        bool lance = R("B8") > 0;
        int max = lance ? 99 : PierceCount[R("B2")];
        float share = lance ? 1f : PierceDmg[R("B2")];
        // flat line from the player through the target (arrows drop, heads bob: height is ignored, 1.2 m wide)
        Vector3 from = player.position;
        Vector3 dir = target.position - from; dir.y = 0f;
        float far = dir.magnitude;
        if (far < 0.1f) return;
        dir /= far;
        List<KeyValuePair<float, EntityAlive>> line = new List<KeyValuePair<float, EntityAlive>>();
        foreach (EntityAlive e in Around(target.position, 16f))
        {
            if (e == target) continue;
            Vector3 v = e.position - from;
            if (Mathf.Abs(v.y - (target.position.y - from.y)) > 2.5f) continue;
            v.y = 0f;
            float along = Vector3.Dot(v, dir);
            if (along <= far - 0.3f || along > far + 15f) continue;
            if ((v - dir * along).magnitude <= 1.2f) line.Add(new KeyValuePair<float, EntityAlive>(along, e));
        }
        line.Sort((a, b) => a.Key.CompareTo(b.Key));
        int pierceDmg = Mathf.Max(1, Mathf.RoundToInt(lastArrowDmg * share)), n = 0;
        for (int i = 0; i < line.Count && i < max; i++)
        {
            EntityAlive e = line[i].Value;
            AposSplash.Deal(e, pierceDmg, player.entityId, EnumDamageTypes.Piercing);
            if (!e.IsDead() && (lance || R("B3") > 0)) Wound(e);
            n++;
        }
        if (n > 0) AposReactions.Add(AposReactions.Pierce, pierceDmg, n);
    }

    // splinters from a point: each to one of the nearest zombies (not the centre one); chain = may split again
    static void Burst(EntityPlayerLocal player, Vector3 pos, EntityAlive centre, int n, string element, bool full, bool chain)
    {
        float radius = SplRadius[R("A3")];
        List<EntityAlive> near = new List<EntityAlive>();
        foreach (EntityAlive e in Around(pos, radius)) if (e != centre) near.Add(e);
        near.Sort((a, b) => (a.position - pos).sqrMagnitude.CompareTo((b.position - pos).sqrMagnitude));
        float dmg = Mathf.Max(SplMin[R("A2")], lastArrowDmg * SplShare[R("A2")]);
        bool needles = full && R("H3") > 0;
        if (needles) dmg *= 1f + DrawBonus(player);           // Thousand Needles: the full draw bonus
        if (R("A8") > 0) dmg *= 2f;                             // Splinter Storm: every splinter in the head
        int hits = Mathf.Min(n, near.Count);
        for (int i = 0; i < hits; i++)
        {
            EntityAlive e = near[i];
            Splinter(player, e, dmg, element, chain);
            // Thousand Needles: the splinter goes on through one more zombie
            if (needles && i + hits < near.Count) Splinter(player, near[i + hits], dmg, element, false);
        }
        // Distraction: splinters that found no zombie hit the ground and make noise there
        if (n > hits && R("H2") > 0)
        {
            foreach (EntityAlive e in Around(pos, 8f)) e.SetInvestigatePosition(pos, 80, false);
        }
    }

    static void Splinter(EntityPlayerLocal player, EntityAlive e, float dmg, string element, bool chain)
    {
        if (e.IsDead()) return;
        AposSplash.Deal(e, Mathf.Max(1, Mathf.RoundToInt(dmg)), player.entityId, EnumDamageTypes.Piercing);
        if (e.IsDead())
        {
            // Splinter Chain: a splinter kill bursts into 2 more (they do not split again)
            if (chain && R("A5") > 0) Burst(player, e.position, e, 2, element, false, false);
            return;
        }
        if (R("A6") > 0) AposPistol.Shackle(e, SplSlow[R("A6")], 3f);
        if (element != null && R("A4") > 0 && Random.value < SplElem[R("A4")]) e.Buffs.AddBuff(element);
        if (R("H1") > 0) Wound(e);
    }

    // ===== Shock Trap: the stuck arrow discharges electricity =====
    // an arrow in a zombie discharges 1.5 s later; an arrow in a wall / the ground is a trap (Trap): it waits up to 10 s
    // for a zombie within 3 m and discharges 0.3 s later (unused, it just goes out after 10 s); one trap at a time: a new
    // one makes the old one discharge at once.
    // The discharge: everyone within 3 m (rank 2: 4.5 m) takes 50% (100%) of a splinter as electrical damage, shocked 2 (4) s.
    class Mine { public float At; public Vector3 Pos; public float Dmg; public EntityAlive Body; public bool Trap; public float Expires; }
    const float TrapWait = 10f, TrapRadius = 3f;
    const int MaxTraps = 1;   // one trap at a time (the crossbow's Minefield is the skill for many mines)
    static readonly Color TrapColor = new Color(0.4f, 0.85f, 1f);
    static readonly List<Mine> mines = new List<Mine>();

    static void RunMines(EntityPlayerLocal player)
    {
        for (int i = mines.Count - 1; i >= 0; i--)
        {
            Mine m = mines[i];
            if (m.Trap && m.At == float.MaxValue)
            {
                if (Time.time > m.Expires) { mines.RemoveAt(i); AposFx.Unmark(m); continue; }   // went out unused
                foreach (EntityAlive e in Around(m.Pos, TrapRadius)) { m.At = Time.time + 0.3f; break; }
            }
            if (Time.time < m.At) continue;
            mines.RemoveAt(i);
            AposFx.Unmark(m);
            if (m.Body != null && !m.Body.IsDead()) m.Pos = m.Body.position;
            Discharge(player, m);
        }
    }

    static void Discharge(EntityPlayerLocal player, Mine m)
    {
        bool r2 = R("A7") >= 2;
        float radius = r2 ? 4.5f : 3f;
        float dmg = Mathf.Max(SplMin[R("A2")], m.Dmg * SplShare[R("A2")]) * (r2 ? 1f : 0.5f) * (R("A8") > 0 ? 2f : 1f);
        int d = Mathf.Max(1, Mathf.RoundToInt(dmg)), hits = 0;
        Vector3 at = m.Pos + Vector3.up * (m.Body != null ? 1f : 0.2f);
        try
        {
            GameManager.Instance.SpawnParticleEffectClient(new ParticleEffect("p_electric_shock", at, Quaternion.identity, 1f, Color.white, "electric_fence_impact", null, 1f, null), player.entityId, true, true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AposMod] Shock Trap effect: " + e.Message);
        }
        List<EntityAlive> near = new List<EntityAlive>(Around(m.Pos, radius));
        AposFx3D.Discharge(at, radius, near);
        foreach (EntityAlive e in near)
        {
            AposSplash.Deal(e, d, player.entityId, EnumDamageTypes.Electrical);
            if (!e.IsDead()) e.Buffs.AddBuff("buffShocked", -1, true, false, r2 ? 4f : 2f);
            hits++;
        }
        if (hits > 0) AposReactions.Add(AposReactions.SplinterMine, d, hits);
    }

    // the armed arrow: a pulsing marker on it (on the zombie it is stuck in, or where it hit) until it bursts
    static void AddMine(Mine m)
    {
        mines.Add(m);
        AposFx.Mark(m, m.Pos, m.Body, TrapColor);   // exactly where the arrow stuck
    }

    // an arrow stuck in a wall or the ground (AposProjectiles): with Shock Trap it becomes a trap
    public static void OnGroundImpact(EntityPlayerLocal player, Vector3 pos, string ammo)
    {
        if (R("A7") <= 0) return;
        lastAmmo = ammo;
        int traps = 0;
        foreach (Mine x in mines) if (x.Trap) traps++;
        for (int i = 0; i < mines.Count && traps >= MaxTraps; i++)
        {
            if (!mines[i].Trap) continue;
            Mine old = mines[i];
            AposFx.Unmark(old); mines.RemoveAt(i); traps--; i--;
            Discharge(player, old);
        }
        AddMine(new Mine { Trap = true, At = float.MaxValue, Expires = Time.time + TrapWait, Pos = pos, Dmg = Mathf.Max(lastArrowDmg, 40f) });
    }

    static readonly FastTags<TagGroup.Global> TraderTag = FastTags<TagGroup.Global>.Parse("trader");

    public static IEnumerable<EntityAlive> Around(Vector3 pos, float radius)
    {
        List<Entity> list = GameManager.Instance.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e is EntityPlayer || e.IsDead() || e.EntityTags.Test_AnySet(TraderTag)) continue;
            if ((e.position - pos).sqrMagnitude <= radius * radius) yield return e;
        }
    }
}

// Weapon Mastery 2.0, crossbows (docs/Design-Mastery2.md 5д; driven by AposMastery.cs):
// - Class trait "Mark": a bolt marks the zombie for 10 s, all your damage to it (any weapon) +20%.
//   The bonus is the player cvar aposMarkBonus read by entityclasses.xml ("AposMod crossbow mark bonus"); the mark is
//   buffAposMark + aposMarkTime on the zombie. (It used to be XML scaled by Archery; the old Pin is gone.)
// - Branch A (the Mark): Deep Mark, Lasting Mark (x Perception), Spread Mark, Weak Spot, Elemental Mark, Mark Hop,
//   Hunter's Rhythm, capstone Hunting Party.
// - Branch B (Sticky Charge): Sticky Charge, Short Fuse, Incendiary Mix, Quick Crank, Inner Charge, Shockwave,
//   Minefield, capstone Cluster Bolt.
// - Hybrids: Marking Blast, Lure, Fireworks.
// Bolts stuck in walls / the ground come from AposProjectiles (Harmony on the projectile).
using System.Collections.Generic;
using UnityEngine;

public static class AposCrossbow
{
    static readonly float[] DeepMark = { 0.2f, 0.35f, 0.5f, 0.8f };
    static readonly float[] Lasting = { 0f, 0.5f, 1f, 2f };      // s x Perception, max +20
    static readonly float[] SpreadMark = { 0f, 2f, 3f, 5f };
    static readonly float[] WeakSpot = { 0f, 0.25f, 0.5f, 1f };
    static readonly int[] Hops = { 0, 1, 4 };
    static readonly float[] Rhythm = { 0f, 30f, 100f };
    static readonly float[] Charge = { 0f, 0.3f, 0.5f, 1f }, ChargeRadius = { 0f, 2f, 2.5f, 5f };
    static readonly float[] Fuse = { 2f, 1.5f, 1f, 0.3f };
    static readonly float[] Incendiary = { 0f, 0.25f, 0.5f, 1f };
    static readonly float[] Crank = { 0f, 15f, 30f, 60f };
    static readonly int[] MaxMines = { 0, 2, 5 };

    static int R(string id) { return AposMastery.Rank(AposMastery.Crossbows, id); }

    static float rhythmUntil;

    public static void Reset() { foreach (ChargeOn c in charges) AposFx.Unmark(c); charges.Clear(); rhythmUntil = 0f; }

    // ===== the Mark =====
    public static bool Marked(EntityAlive e) { return e != null && e.Buffs.HasBuff("buffAposMark"); }

    public static void Mark(EntityPlayerLocal player, EntityAlive e)
    {
        if (e == null || e.IsDead() || e is EntityPlayer) return;
        float time = 10f + Mathf.Min(20f, Lasting[R("A2")] * AposAffixes.Level(player, "attPerception"));
        if (!Marked(e)) e.Buffs.SetCustomVar("aposMarkHops", Hops[R("A6")]);
        e.Buffs.SetCustomVar("aposMarkTime", time);
        e.Buffs.AddBuff("buffAposMark");
    }

    // Weak Spot: your body hits on a marked zombie count as headshots for other classes (pistol streak, rifle ricochet)
    public static bool BodyAsHead(EntityAlive e)
    {
        float c = WeakSpot[R("A4")];
        return c > 0f && Marked(e) && Random.value < c;
    }

    // Elemental Mark: elements on a marked zombie hit 50% harder (AposCombat, element damage types)
    public static bool ElementBonus(EntityAlive e) { return R("A5") > 0 && Marked(e); }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        // the Mark bonus works with any weapon in hand
        float bonus = DeepMark[R("A1")] * (R("A8") > 0 ? 2f : 1f);
        if (player.Buffs.GetCustomVar("aposMarkBonus") != bonus) player.Buffs.SetCustomVar("aposMarkBonus", bonus);
        RunCharges(player);
        if (!held) return;
        float reload = Crank[R("B4")] + (Time.time < rhythmUntil ? Rhythm[R("A7")] : 0f);
        AposMastery.SetStat(AposMastery.StatReload, reload / 100f);
        if (R("B7") > 0) AposHud.Gauge(false, MinesOut(), MaxMines[R("B7")], new Color(1f, 0.6f, 0.2f), MinesOut() >= MaxMines[R("B7")], "traps", MinesOut() + "/" + MaxMines[R("B7")], "B7");
        if (Time.time < rhythmUntil) AposHud.AddAttackSpeed("swap", Rhythm[R("A7")] / 100f, Mathf.CeilToInt(rhythmUntil - Time.time) + "s", "A7");
    }

    static bool IsBolt(DamageSource source)
    {
        string ammo = AposProjectiles.AmmoOf(source);
        return ammo != null && ammo.StartsWith("ammoCrossbowBolt");
    }

    // ===== hits =====
    static int lastTarget = -1;
    static bool lastHead, lastBolt;
    static float lastBoltDmg;

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        lastBolt = IsBolt(source);
        if (player == null || !lastBolt) return strength;
        lastHead = (source.GetEntityDamageBodyPart(target) & EnumBodyPartHit.Head) != 0;
        float mult = 1f;
        if (R("A8") > 0) mult *= 0.6f;                     // Hunting Party downside (the charge counts from it too)
        lastBoltDmg = strength * mult;
        if (R("B8") > 0) mult *= 0.5f;                     // Cluster Bolt downside: the bolt itself
        lastTarget = target.entityId;
        AposProjectiles.NoteEntityHit();
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || !lastBolt || target.entityId != lastTarget) return;
        lastTarget = -1;
        if (!response.Fatal) Mark(player, target);
        int a3 = R("A3");
        if (a3 > 0) foreach (EntityAlive e in AposBowTree.Around(target.position, SpreadMark[a3])) Mark(player, e);
        if (R("B1") <= 0) return;
        if (R("B8") > 0)
        {
            // Cluster Bolt: 3 charges stick to the 3 nearest zombies within 5 m (the target first), each marked
            List<EntityAlive> near = new List<EntityAlive>();
            if (!target.IsDead()) near.Add(target);
            List<EntityAlive> rest = new List<EntityAlive>(AposBowTree.Around(target.position, 5f));
            rest.Sort((a, b) => (a.position - target.position).sqrMagnitude.CompareTo((b.position - target.position).sqrMagnitude));
            foreach (EntityAlive e in rest) if (near.Count < 3 && !near.Contains(e)) near.Add(e);
            foreach (EntityAlive e in near) { Mark(player, e); Stick(player, e, target.position, 0.6f, false); }
        }
        else Stick(player, target, target.position, 1f, lastHead && R("B5") > 0);
    }

    // ===== Sticky Charge =====
    class ChargeOn
    {
        public EntityAlive Body;      // null: stuck in a wall or the ground
        public Vector3 Pos;
        public float At, Dmg, Power;
        public bool Inner, Mine, Armed;
        public float Expires;
    }
    static readonly List<ChargeOn> charges = new List<ChargeOn>();

    static int MinesOut()
    {
        int n = 0;
        foreach (ChargeOn c in charges) if (c.Mine) n++;
        return n;
    }

    static void Stick(EntityPlayerLocal player, EntityAlive body, Vector3 pos, float power, bool inner)
    {
        float fuse = Fuse[R("B2")];
        // Lure: a charge in a marked zombie waits 3 s and the zombie screams: everyone within 8 m runs to it
        if (body != null && R("H2") > 0 && Marked(body))
        {
            fuse = 3f;
            foreach (EntityAlive e in AposBowTree.Around(body.position, 8f)) if (e != body) e.SetInvestigatePosition(body.position, 80, true);
        }
        Add(new ChargeOn { Body = body, Pos = pos, At = Time.time + fuse, Dmg = lastBoltDmg, Power = power, Inner = inner });
        Debug.Log("[AposMod] Sticky Charge: stuck in " + (body != null ? body.EntityName : "a wall") + ", " + fuse + " s, bolt " + Mathf.RoundToInt(lastBoltDmg));
    }

    // every charge waiting to go off has a pulsing marker (AposFx) until it explodes or is removed
    static void Add(ChargeOn c)
    {
        charges.Add(c);
        AposFx.Mark(c, c.Pos, c.Body);
    }

    static void RemoveAt(int i)
    {
        AposFx.Unmark(charges[i]);
        charges.RemoveAt(i);
    }

    // a bolt stuck in a wall or the ground (AposProjectiles)
    public static void OnGroundImpact(EntityPlayerLocal player, Vector3 pos)
    {
        if (R("B1") <= 0) return;
        int count = R("B8") > 0 ? 3 : 1;
        float power = R("B8") > 0 ? 0.6f : 1f;
        float dmg = Mathf.Max(lastBoltDmg, 50f);
        for (int i = 0; i < count; i++)
        {
            Vector3 p = pos + (count > 1 ? Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 1.5f : Vector3.zero);
            if (R("B7") > 0)
            {
                // Minefield: waits up to 60 s for a zombie within 5 m, then goes off 1 s later; oldest mines go first
                while (MinesOut() >= MaxMines[R("B7")]) RemoveOldestMine(player);
                Add(new ChargeOn { Pos = p, Dmg = dmg, Power = power, Mine = true, Expires = Time.time + 60f, At = float.MaxValue });
            }
            else Add(new ChargeOn { Pos = p, At = Time.time + Fuse[R("B2")], Dmg = dmg, Power = power });
        }
    }

    // over the limit: the oldest mine goes off right away (not just disappears)
    static void RemoveOldestMine(EntityPlayerLocal player)
    {
        for (int i = 0; i < charges.Count; i++)
        {
            if (!charges[i].Mine) continue;
            ChargeOn c = charges[i];
            RemoveAt(i);
            Explode(player, c);
            return;
        }
    }

    static void RunCharges(EntityPlayerLocal player)
    {
        for (int i = charges.Count - 1; i >= 0; i--)
        {
            ChargeOn c = charges[i];
            if (c.Mine && !c.Armed)
            {
                if (Time.time > c.Expires) { RemoveAt(i); Explode(player, c); continue; }   // self-destruct after 60 s
                foreach (EntityAlive e in AposBowTree.Around(c.Pos, 5f)) { c.Armed = true; c.At = Time.time + 1f; break; }
                continue;
            }
            if (Time.time < c.At) continue;
            RemoveAt(i);
            Explode(player, c);
        }
    }

    static void Explode(EntityPlayerLocal player, ChargeOn c)
    {
        Vector3 pos = c.Body != null && !c.Body.IsDead() ? c.Body.position : c.Pos;
        float radius = ChargeRadius[R("B1")];
        int dmg = Mathf.Max(1, Mathf.RoundToInt(c.Dmg * Charge[R("B1")] * c.Power));
        bool bodyMarked = c.Body != null && Marked(c.Body);
        List<EntityAlive> hit = new List<EntityAlive>(AposBowTree.Around(pos, radius));
        AposFx.Blast(pos + Vector3.up * (c.Body != null ? 1f : 0.3f));
        AposFx3D.Boom(pos + Vector3.up * 0.5f, radius);
        if (c.Body != null && !c.Body.IsDead()) c.Body.Buffs.AddBuff("buffAposChainBlast");
        else if (hit.Count > 0) hit[0].Buffs.AddBuff("buffAposChainBlast");
        int b3 = R("B3"), b6 = R("B6");
        bool innerKill = false;
        List<EntityAlive> markedDead = new List<EntityAlive>();
        foreach (EntityAlive e in hit)
        {
            bool marked = Marked(e);
            int d = (c.Inner && e == c.Body) ? dmg * 3 : dmg;    // Inner Charge: x3 on the target
            AposSplash.Deal(e, d, player.entityId, EnumDamageTypes.Heat);
            if (e.IsDead())
            {
                if (c.Inner && e == c.Body) innerKill = true;
                if (marked) markedDead.Add(e);
                continue;
            }
            if (b3 > 0 && Random.value < Incendiary[b3]) e.Buffs.AddBuff("buffBurningElement");
            if (b3 >= 3) e.Buffs.AddBuff("buffShocked", -1, true, false, 1f);
            if (b6 > 0) AposKnock.Ragdoll(e, pos, b6 >= 2 ? 400f : 250f, b6 >= 2 ? 2f : 0.5f);
            if (R("H1") > 0) Mark(player, e);                    // Marking Blast
        }
        Debug.Log("[AposMod] Sticky Charge: exploded, " + hit.Count + " zombie(s), " + dmg + " each");
        if (hit.Count > 0) AposReactions.Add(AposReactions.StickyCharge, dmg, hit.Count);
        // Inner Charge kill: the head bursts and the blast knocks down everyone in the radius
        if (innerKill) foreach (EntityAlive e in AposBowTree.Around(pos, radius)) AposKnock.Ragdoll(e, pos, 400f, 2f);
        // Fireworks: a marked zombie killed by a charge passes a new charge and the mark to the nearest one within 6 m
        if (R("H3") > 0)
        {
            foreach (EntityAlive dead in markedDead)
            {
                EntityAlive next = Nearest(dead.position, 6f);
                if (next == null) continue;
                Mark(player, next);
                Add(new ChargeOn { Body = next, Pos = next.position, At = Time.time + Fuse[R("B2")], Dmg = c.Dmg, Power = c.Power });
            }
        }
    }

    static EntityAlive Nearest(Vector3 pos, float radius)
    {
        EntityAlive best = null;
        float bd = float.MaxValue;
        foreach (EntityAlive e in AposBowTree.Around(pos, radius))
        {
            float d = (e.position - pos).sqrMagnitude;
            if (d < bd) { bd = d; best = e; }
        }
        return best;
    }

    // ===== a marked zombie died (any weapon of the local player, AposCombat) =====
    public static void OnMarkedKill(EntityPlayerLocal player, EntityAlive dead)
    {
        if (player == null || !Marked(dead)) return;
        if (R("A7") > 0) rhythmUntil = Time.time + 5f;          // Hunter's Rhythm
        // Mark Hop: the mark jumps to the nearest zombie within 6 m with full time
        int hops = (int)dead.Buffs.GetCustomVar("aposMarkHops");
        if (R("A6") > 0 && hops > 0)
        {
            EntityAlive next = Nearest(dead.position, 6f);
            if (next != null)
            {
                Mark(player, next);
                next.Buffs.SetCustomVar("aposMarkHops", hops - 1);
            }
        }
        // Hunting Party: a marked kill marks the 3 nearest within 8 m
        if (R("A8") > 0)
        {
            List<EntityAlive> near = new List<EntityAlive>(AposBowTree.Around(dead.position, 8f));
            near.Sort((a, b) => (a.position - dead.position).sqrMagnitude.CompareTo((b.position - dead.position).sqrMagnitude));
            for (int i = 0; i < near.Count && i < 3; i++) Mark(player, near[i]);
        }
    }
}

// Arrows and bolts that hit a wall or the ground: Shock Trap (bows), Sticky Charge / Minefield (crossbows).
// Harmony on ProjectileMoveScript.checkCollision: Active -> any other state (Sticky, or Dead / Destroyed when the game's
// stick roll broke it) without an entity hit in the same moment.
[HarmonyLib.HarmonyPatch(typeof(ProjectileMoveScript), "checkCollision")]
public static class AposProjectiles
{
    static float lastEntityHit = -10f;
    public static void NoteEntityHit() { lastEntityHit = Time.time; }

    // The game passes the LAUNCHER (bow / crossbow) as the attacking item of an arrow or bolt hit, not the ammo.
    // The hit happens inside checkCollision, so the ammo of the projectile being checked is kept here meanwhile.
    static string current;
    static int currentFrame = -1;

    // the ammo item name of an arrow / bolt hit (or the attacking item's own name for anything else)
    public static string AmmoOf(DamageSource source)
    {
        ItemValue iv = source.AttackingItem;
        string name = iv != null && iv.ItemClass != null ? iv.ItemClass.Name : null;
        if (current != null && Time.frameCount == currentFrame && (name == null || !name.StartsWith("ammo"))) return current;
        return name;
    }

    static void Prefix(ProjectileMoveScript __instance, out ProjectileMoveScript.State __state)
    {
        __state = __instance.state;
        currentFrame = Time.frameCount;
        current = __instance.itemValueProjectile != null && __instance.itemValueProjectile.ItemClass != null
            ? __instance.itemValueProjectile.ItemClass.Name : null;
    }

    static void Postfix(ProjectileMoveScript __instance, ProjectileMoveScript.State __state)
    {
        current = null;
        try
        {
            if (__state != ProjectileMoveScript.State.Active || __instance.state == ProjectileMoveScript.State.Active) return;
            if (Time.time - lastEntityHit < 0.1f) return;           // it stuck in a zombie: the hit hooks handle that
            World world = GameManager.Instance.World;
            EntityPlayerLocal player = world != null ? world.GetPrimaryPlayer() : null;
            if (player == null || __instance.firingEntity != player || __instance.itemValueProjectile == null) return;
            ItemClass ammo = __instance.itemValueProjectile.ItemClass;
            if (ammo == null) return;
            Vector3 pos = __instance.transform.position + Origin.position; // scene space -> world
            if (ammo.Name.StartsWith("ammoArrow") && AposMastery.HeldClass == AposMastery.Bows) AposBowTree.OnGroundImpact(player, pos, ammo.Name);
            else if (ammo.Name.StartsWith("ammoCrossbowBolt") && AposMastery.HeldClass == AposMastery.Crossbows) AposCrossbow.OnGroundImpact(player, pos);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AposMod] Projectile hook failed: " + e);
        }
    }
}

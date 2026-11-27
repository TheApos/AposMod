// Weapon Mastery 2.0, rifles (docs/Design-Mastery2.md 5в; driven by AposMastery.cs):
// - Class trait "Ricochet": a headshot hurts up to 3 zombies within 5 m of the target, each for 5% of the headshot
//   (at least 20). Full Ranger set: +3 m and +20%. Done here (it used to be XML scaled by Dead Eye).
// - Branch A (the shot): Cold Aim, Bolt Work, Opening Shot, Long Shot, Weak Spot, Focus, Big Game, capstone One Shot.
// - Branch B (the ricochet): Wide Ricochet (x Perception), Ricochet Power, More Targets, Stunning Ricochet,
//   Body Ricochet, Elemental Ricochet, Overflow, capstone Ricochet Storm.
// - Hybrids: Trick Shot, Spotter, Skewer Shot.
// Shots come from XML (aposRShot on the player, entityclasses.xml).
using System.Collections.Generic;
using UnityEngine;

public static class AposRifle
{
    const float BaseRadius = 5f, BaseShare = 0.05f, MinRicochet = 20f, BodyHeadMult = 2f;
    static readonly float[] ColdAim = { 0f, 0.15f, 0.30f, 0.50f };
    static readonly float[] BoltWork = { 0f, 20f, 35f, 50f };
    static readonly float[] Opening = { 0f, 0.3f, 0.6f, 1f };
    static readonly float[] FarPer = { 0f, 0.1f, 0.2f, 0.3f }, FarCap = { 0f, 1f, 1.5f, 2f }, FarPen = { 0f, 0.5f, 0.4f, 0.25f };
    static readonly float[] FocusPer = { 0f, 0.1f, 0.2f, 0.2f }, FocusCap = { 0f, 1f, 1f, 10f };
    static readonly float[] Wide = { 0f, 0.5f, 0.8f, 1f };   // m x Perception, max +10 m
    static readonly float[] Share = { 0.05f, 0.15f, 0.25f, 0.45f };
    static readonly int[] Targets = { 3, 5, 7, 10 };
    static readonly float[] Stun = { 0f, 0.5f, 1f, 2f };
    static readonly float[] Elemental = { 0f, 0.25f, 0.6f };
    static readonly float[] Overflow = { 0f, 0.5f, 1f };
    static readonly string[] Elements = { "buffBurningElement", "buffShocked", "buffAposCorroded", "buffInjuryBleeding" };

    static int R(string id) { return AposMastery.Rank(AposMastery.Rifles, id); }

    static float focus;           // Focus charge (fraction)
    static float fastReloadUntil; // Bolt Work 3/3 after a kill
    static bool spotterNext;      // Spotter: the next shot +50%
    static float spotterUntil;    // Spotter outlines
    static int trickCount;        // Trick Shot: every 4th ricochet in the head
    // the hit being processed
    static int lastTarget = -1;
    static bool lastHead, lastRealHead;
    static float lastFull;        // damage before Ricochet Storm halved it (ricochets count from the full shot)

    public static void Reset()
    {
        focus = 0f; fastReloadUntil = 0f; spotterNext = false; spotterUntil = 0f; trickCount = 0; lastTarget = -1;
    }

    public static void Tick(EntityPlayerLocal player, bool held)
    {
        if (player.Buffs.GetCustomVar("aposRShot") > 0f)
        {
            player.Buffs.SetCustomVar("aposRShot", 0f);
            focus = 0f; // the shot spent the charge
        }
        if (!held) { focus = 0f; return; }

        // Focus: every second aiming without shooting charges the next shot
        int a6 = R("A6");
        if (a6 > 0 && player.AimingGun) focus = Mathf.Min(FocusCap[a6], focus + FocusPer[a6] * Time.deltaTime);
        else if (!player.AimingGun) focus = 0f;

        float reload = BoltWork[R("A2")] + (Time.time < fastReloadUntil ? 100f : 0f);
        AposMastery.SetStat(AposMastery.StatReload, reload / 100f);
        if (focus > 0f) AposMastery.SetStat(AposMastery.StatSpread, -0.9f);

        if (a6 > 0 && focus > 0f) AposHud.Gauge(false, focus / FocusCap[a6], 0, new Color(0.25f, 0.9f, 1f), focus >= FocusCap[a6] - 0.001f, "sight", "+" + Mathf.RoundToInt(focus * 100f) + "%", "A6");
        if (spotterNext) AposHud.AddNextHit("search", 1.5f, "H2");
    }

    static bool Big(EntityAlive e)
    {
        if (AposElites.IsElite(e)) return true;
        EntityClass ec;
        if (!EntityClass.list.TryGetValue(e.entityClass, out ec) || ec == null) return false;
        string n = ec.entityClassName;
        return n.Contains("Feral") || n.Contains("Radiated") || n.Contains("Behemoth") || n.Contains("Demolition") || n.Contains("Charged") || n.Contains("Infernal");
    }

    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        bool realHead = (source.GetEntityDamageBodyPart(target) & EnumBodyPartHit.Head) != 0 || AposCrossbow.BodyAsHead(target); // crossbow Weak Spot
        bool oneShot = R("A8") > 0;
        bool head = realHead || oneShot;
        float mult = 1f;
        if (oneShot && !realHead) mult *= BodyHeadMult;              // One Shot: a body hit counts as a headshot
        if (head) mult += ColdAim[R("A1")];
        if (R("A3") > 0 && target.Health >= target.GetMaxHealth()) mult += Opening[R("A3")];
        int a4 = R("A4");
        if (a4 > 0)
        {
            float d = (target.position - player.position).magnitude;
            if (d < 10f) mult -= FarPen[a4];
            else mult += Mathf.Min(FarCap[a4], FarPer[a4] * Mathf.Floor(d / 10f));
        }
        // Weak Spot: a headshot on a zombie that does not see you (asleep or not after you), or on a corroded one
        if (R("A5") > 0 && head && (target.IsSleeping || target.GetAttackTarget() != player || target.Buffs.HasBuff("buffAposCorroded"))) mult *= 2f;
        if (focus > 0f) mult += focus;
        if (R("A7") > 0 && Big(target)) mult += 0.5f;
        if (oneShot && AposElites.IsElite(target)) mult *= 2f;
        if (spotterNext) { mult += 0.5f; spotterNext = false; }
        mult = Mathf.Max(0.1f, mult);
        lastFull = strength * mult;
        if (R("B8") > 0) mult *= 0.5f;                                  // Ricochet Storm: the shot itself -50%
        lastTarget = target.entityId;
        lastHead = head;
        lastRealHead = realHead;
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }

    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null || target.entityId != lastTarget) return;
        lastTarget = -1;
        if (response.Fatal)
        {
            if (R("A2") >= 3) fastReloadUntil = Time.time + 6f;
            if (lastHead && R("H2") > 0)
            {
                spotterNext = true;
                spotterUntil = Time.time + 10f;
            }
        }
        // which hits ricochet: headshots; with Body Ricochet body hits too (50%); One Shot: only real headshots, x0.5
        bool oneShot = R("A8") > 0;
        float power;
        if (lastRealHead) power = 1f;
        else if (!oneShot && R("B5") > 0) power = 0.5f;
        else return;
        if (oneShot) power *= 0.5f;
        float excess = response.Fatal ? Mathf.Max(0f, lastFull - (dealt > 0 ? dealt : 0)) : 0f;
        Ricochet(player, target, lastFull, power, excess, true);
        // Skewer Shot: a headshot goes through the whole line, every zombie on it gets the hit and its own ricochet
        if (lastRealHead && R("H3") > 0) Skewer(player, target);
    }

    static void Ricochet(EntityPlayerLocal player, EntityAlive center, float hit, float power, float excess, bool allowChain)
    {
        bool ranger = player.Buffs.HasBuff("buffRangerSetBonus");
        float radius = BaseRadius + Mathf.Min(10f, Wide[R("B1")] * AposAffixes.Level(player, "attPerception")) + (ranger ? 3f : 0f);
        List<EntityAlive> near = Near(center, radius);
        int n = Mathf.Min(Targets[R("B3")], near.Count);
        if (n == 0) return;
        float per = Mathf.Max(MinRicochet, hit * Share[R("B2")]) * power * (ranger ? 1.2f : 1f);
        per += excess * Overflow[R("B7")] / n;                         // Overflow: the excess shared between them
        bool storm = R("B8") > 0;
        if (storm) per *= 2f;
        HashSet<int> done = new HashSet<int>();
        done.Add(center.entityId);
        string element = null;
        foreach (string el in Elements) if (center.Buffs.HasBuff(el)) { element = el; break; }
        for (int i = 0; i < n; i++)
        {
            Bounce(player, near[i], per, element, done);
            if (!storm || !allowChain) continue;
            // Ricochet Storm: every ricochet jumps on to 2 more zombies (each zombie once)
            List<EntityAlive> next = Near(near[i], radius);
            int k = 0;
            for (int j = 0; j < next.Count && k < 2; j++)
            {
                if (done.Contains(next[j].entityId)) continue;
                Bounce(player, next[j], per, element, done);
                k++;
            }
        }
    }

    static void Bounce(EntityPlayerLocal player, EntityAlive e, float per, string element, HashSet<int> done)
    {
        if (e.IsDead() || done.Contains(e.entityId)) return;
        done.Add(e.entityId);
        float d = per;
        if (R("H1") > 0)
        {
            if (e.Health >= e.GetMaxHealth()) d *= 1f + Opening[R("A3")];  // Trick Shot: the Opening Shot bonus
            if (++trickCount % 4 == 0) d *= BodyHeadMult;                  // every 4th one in the head
        }
        AposSplash.Deal(e, Mathf.Max(1, Mathf.RoundToInt(d)), player.entityId, EnumDamageTypes.Piercing);
        if (e.IsDead()) return;
        int b4 = R("B4");
        if (b4 > 0 && !e.Buffs.HasBuff("buffAposRicochetCD"))
        {
            e.Buffs.AddBuff("buffInjuryStunned01", -1, true, false, Stun[b4]);
            e.Buffs.AddBuff("buffAposRicochetCD");
        }
        if (element != null && R("B6") > 0 && Random.value < Elemental[R("B6")]) e.Buffs.AddBuff(element);
    }

    static void Skewer(EntityPlayerLocal player, EntityAlive target)
    {
        Vector3 from = player.getHeadPosition();
        Vector3 dir = target.getHeadPosition() - from;
        float far = dir.magnitude;
        if (far < 0.1f) return;
        dir /= far;
        List<Entity> list = GameManager.Instance.World.Entities.list;
        List<EntityAlive> line = new List<EntityAlive>();
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e == target || e is EntityPlayer || e.IsDead()) continue;
            Vector3 v = e.getHeadPosition() - from;
            float along = Vector3.Dot(v, dir);
            if (along <= far || along > far + 60f) continue;
            if ((v - dir * along).magnitude <= 0.8f) line.Add(e);
        }
        foreach (EntityAlive e in line)
        {
            AposSplash.Deal(e, Mathf.Max(1, Mathf.RoundToInt(lastFull)), player.entityId, EnumDamageTypes.Piercing);
            if (!e.IsDead()) Ricochet(player, e, lastFull, 1f, 0f, false);
        }
    }

    static List<EntityAlive> Near(EntityAlive center, float radius)
    {
        List<EntityAlive> res = new List<EntityAlive>();
        List<Entity> list = GameManager.Instance.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e == center || e is EntityPlayer || e.IsDead() || e.EntityTags.Test_AnySet(TraderTag)) continue;
            if ((e.position - center.position).sqrMagnitude <= radius * radius) res.Add(e);
        }
        res.Sort((a, b) => (a.position - center.position).sqrMagnitude.CompareTo((b.position - center.position).sqrMagnitude));
        return res;
    }
    static readonly FastTags<TagGroup.Global> TraderTag = FastTags<TagGroup.Global>.Parse("trader");

    // ===== Spotter: outlines of all zombies within 30 m for 10 s after a headshot kill =====
    static Texture2D dot;

    public static void Draw(EntityPlayerLocal player)
    {
        if (player == null || Time.time > spotterUntil || player.playerCamera == null) return;
        Camera cam = player.playerCamera;
        if (dot == null)
        {
            dot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float dx = (x + 0.5f) / 16f - 1f, dy = (y + 0.5f) / 16f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    dot.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.Abs(d - 0.75f) / 0.2f)));
                }
            dot.Apply();
        }
        Color old = GUI.color;
        GUI.color = new Color(1f, 0.82f, 0.3f, 0.8f * Mathf.Clamp01(spotterUntil - Time.time));
        List<Entity> list = GameManager.Instance.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityEnemy e = list[i] as EntityEnemy;
            if (e == null || e.IsDead() || (e.position - player.position).sqrMagnitude > 900f) continue;
            Vector3 feet = cam.WorldToViewportPoint(e.transform.position);
            Vector3 top = cam.WorldToViewportPoint(e.transform.position + Vector3.up * e.height);
            if (feet.z <= 0f) continue;
            float h = Mathf.Abs(top.y - feet.y) * Screen.height;
            float w = Mathf.Max(10f, h * 0.6f);
            GUI.DrawTexture(new Rect(feet.x * Screen.width - w / 2f, (1f - top.y) * Screen.height, w, Mathf.Max(10f, h)), dot);
        }
        GUI.color = old;
    }
}

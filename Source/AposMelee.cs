// Shared bits of the melee mastery trees (knives, clubs, sledges, ...).
// Power or normal: XML (entityclasses.xml "AposMod melee events") sets aposPower = 1 when a power attack starts and
// 0 when a normal one starts; the hit lands later in the same swing.
using System.Collections.Generic;
using UnityEngine;

public static class AposMelee
{
    public static bool Power(EntityPlayerLocal p) { return p != null && p.Buffs.GetCustomVar("aposPower") > 0f; }

    // the zombie has its back to you (more than 110 degrees between where it faces and where you are)
    public static bool Behind(EntityAlive target, EntityAlive attacker)
    {
        Vector3 f = target.GetForwardVector();
        Vector3 to = attacker.position - target.position;
        f.y = 0f; to.y = 0f;
        if (to.sqrMagnitude < 0.0001f || f.sqrMagnitude < 0.0001f) return false;
        return Vector3.Angle(f, to) > 110f;
    }

    public static bool SeesYou(EntityAlive target, EntityAlive player)
    {
        return !target.IsSleeping && target.GetAttackTarget() == player;
    }

    public static bool Down(EntityAlive e)
    {
        return (e.emodel != null && e.emodel.IsRagdollActive) || e.Buffs.HasBuff("buffInjuryKnockdown01");
    }

    public static bool Stunned(EntityAlive e)
    {
        return e.Buffs.HasBuff("buffInjuryStunned01") || e.Buffs.GetCustomVar("_stunned") > 0f;
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

    public static List<EntityAlive> Nearest(Vector3 pos, float radius, EntityAlive except, int max)
    {
        List<EntityAlive> res = new List<EntityAlive>();
        foreach (EntityAlive e in Around(pos, radius)) if (e != except) res.Add(e);
        res.Sort((a, b) => (a.position - pos).sqrMagnitude.CompareTo((b.position - pos).sqrMagnitude));
        if (res.Count > max) res.RemoveRange(max, res.Count - max);
        return res;
    }
}

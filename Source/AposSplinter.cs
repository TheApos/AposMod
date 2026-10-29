// Bows "Splinter": an arrow hit sets aposSplinterN (fragments) and aposSplinterDmg (negative HP) on the zombie it
// hit (items.xml). Each fragment flies to one of the nearest other zombies within 5 m and deals the damage through
// buffAposSplash (which reads aposSplashDmg on its owner). More Archery levels = more fragments.
// Also the Explosion reaction: buffs.xml sets aposExplodeReq, the damage is a share of the last hit (aposHitDmg).
using System.Collections.Generic;
using UnityEngine;

public class AposSplinter : MonoBehaviour
{
    const float Range = 80f;
    const float SplinterRadius = 5f;
    const string CountVar = "aposSplinterN";
    const string DamageVar = "aposSplinterDmg";
    const string ExplodeVar = "aposExplodeReq";
    const float ExplodeShare = 0.5f;
    const float ExplodeMin = 60f;
    const float ExplodeRadius = 4f;

    readonly List<EntityAlive> near = new List<EntityAlive>();

    void Update()
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        if (player == null) return;

        List<Entity> list = gm.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive hit = list[i] as EntityAlive;
            if (hit == null || hit is EntityPlayer) continue;
            if ((hit.position - player.position).sqrMagnitude > Range * Range) continue;

            // Explosion reaction (fire + acid, buffAposElementCheck): 50% of the last hit, at least 60,
            // to the zombie and everyone within 4 m
            if (hit.Buffs.GetCustomVar(ExplodeVar) > 0f)
            {
                hit.Buffs.SetCustomVar(ExplodeVar, 0f);
                float blast = -Mathf.Max(ExplodeMin, hit.Buffs.GetCustomVar("aposHitDmg") * ExplodeShare);
                Splash(hit, blast);
                foreach (EntityAlive e in Near(list, hit, ExplodeRadius)) Splash(e, blast);
            }

            int fragments = (int)hit.Buffs.GetCustomVar(CountVar);
            if (fragments <= 0) continue;

            float damage = hit.Buffs.GetCustomVar(DamageVar);
            hit.Buffs.SetCustomVar(CountVar, 0f);
            Scatter(list, hit, fragments, damage);
        }
    }

    void Scatter(List<Entity> list, EntityAlive hit, int fragments, float damage)
    {
        List<EntityAlive> sorted = Near(list, hit, SplinterRadius);
        sorted.Sort((a, b) => (a.position - hit.position).sqrMagnitude.CompareTo((b.position - hit.position).sqrMagnitude));

        int n = Mathf.Min(fragments, sorted.Count);
        for (int i = 0; i < n; i++) Splash(sorted[i], damage);
    }

    List<EntityAlive> Near(List<Entity> list, EntityAlive center, float radius)
    {
        near.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e == center || e is EntityPlayer || e.IsDead()) continue;
            if ((e.position - center.position).sqrMagnitude > radius * radius) continue;
            near.Add(e);
        }
        return near;
    }

    static void Splash(EntityAlive e, float damage)
    {
        e.Buffs.SetCustomVar("aposSplashDmg", damage);
        e.Buffs.AddBuff("buffAposSplash");
    }
}

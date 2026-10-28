// Bows "Splinter": an arrow hit sets aposSplinterN (fragments) and aposSplinterDmg (negative HP) on the zombie it
// hit (items.xml). Each fragment flies to one of the nearest other zombies within 5 m and deals the damage through
// buffAposSplash (which reads aposSplashDmg on its owner). More Archery levels = more fragments.
using System.Collections.Generic;
using UnityEngine;

public class AposSplinter : MonoBehaviour
{
    const float Range = 80f;
    const float SplinterRadius = 5f;
    const string CountVar = "aposSplinterN";
    const string DamageVar = "aposSplinterDmg";

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
            int fragments = (int)hit.Buffs.GetCustomVar(CountVar);
            if (fragments <= 0) continue;

            float damage = hit.Buffs.GetCustomVar(DamageVar);
            hit.Buffs.SetCustomVar(CountVar, 0f);
            Scatter(list, hit, fragments, damage);
        }
    }

    void Scatter(List<Entity> list, EntityAlive hit, int fragments, float damage)
    {
        near.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e == hit || e is EntityPlayer || e.IsDead()) continue;
            if ((e.position - hit.position).sqrMagnitude > SplinterRadius * SplinterRadius) continue;
            near.Add(e);
        }
        near.Sort((a, b) => (a.position - hit.position).sqrMagnitude.CompareTo((b.position - hit.position).sqrMagnitude));

        int n = Mathf.Min(fragments, near.Count);
        for (int i = 0; i < n; i++)
        {
            near[i].Buffs.SetCustomVar("aposSplashDmg", damage);
            near[i].Buffs.AddBuff("buffAposSplash");
        }
    }
}

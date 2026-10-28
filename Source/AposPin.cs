// Crossbow "Pin": a bolt hit sets the cvar aposPinCheck on the zombie (items.xml). Here we look for a wall
// up to 3 m behind it (along the line from the player through the zombie). With a wall the zombie gets
// buffAposPinned (cannot move until it dies) or, for elites, buffAposPinnedElite (10 s).
using System.Collections.Generic;
using UnityEngine;

public class AposPin : MonoBehaviour
{
    const float Range = 80f;
    const float WallDistance = 3f;
    const string CheckVar = "aposPinCheck";
    static readonly FastTags<TagGroup.Global> EliteTags =
        FastTags<TagGroup.Global>.Parse("feral,radiated,infernal,charged,special,boss");

    void Update()
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        if (player == null) return;

        List<Entity> list = gm.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e is EntityPlayer || e.IsDead()) continue;
            if ((e.position - player.position).sqrMagnitude > Range * Range) continue;
            if (e.Buffs.GetCustomVar(CheckVar) <= 0f) continue;

            e.Buffs.SetCustomVar(CheckVar, 0f);
            if (WallBehind(e, player))
            {
                e.Buffs.AddBuff(e.HasAnyTags(EliteTags) ? "buffAposPinnedElite" : "buffAposPinned");
            }
        }
    }

    // Scene-space ray from the zombie's chest away from the player; the first non-entity collider is a wall
    static bool WallBehind(EntityAlive e, EntityPlayerLocal player)
    {
        Vector3 dir = e.position - player.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return false;
        dir.Normalize();
        Vector3 origin = e.transform.position + Vector3.up * (e.height * 0.6f);

        RaycastHit[] hits = Physics.RaycastAll(origin, dir, WallDistance);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
            if (hits[i].collider.GetComponentInParent<Entity>() != null) continue; // zombies, the bolt's target itself
            return true;
        }
        return false;
    }
}

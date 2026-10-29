// Crossbow "Pin": a bolt hit sets the cvar aposPinCheck on the zombie (items.xml). Here we look for a wall
// up to 3 m behind it (along the line from the player through the zombie). With a wall the zombie gets
// buffAposPinned (cannot move until it dies) or, for elites, buffAposPinnedElite (10 s).
// The wall test walks the game's block grid (world coordinates), not Unity physics: chunk colliders are not
// reliably hit by Physics raycasts, and block solidity is what stops a zombie anyway.
using System.Collections.Generic;
using UnityEngine;

public class AposPin : MonoBehaviour
{
    const float Range = 80f;
    const float WallDistance = 3f;
    const float Step = 0.25f;
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
            float wall = WallBehind(gm.World, e, player);
            bool elite = e.HasAnyTags(EliteTags);
            if (wall >= 0f) e.Buffs.AddBuff(elite ? "buffAposPinnedElite" : "buffAposPinned");
            Debug.Log("[AposMod] Pin check on " + e.EntityName + ": " + (wall >= 0f ? "wall at " + wall.ToString("0.0") + " m, pinned" + (elite ? " (elite, 10 s)" : "") : "no wall within 3 m"));
        }
    }

    // Distance to the first movement-blocking block behind the zombie (knee and chest height), or -1
    static float WallBehind(World world, EntityAlive e, EntityPlayerLocal player)
    {
        Vector3 dir = e.position - player.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return -1f;
        dir.Normalize();

        for (float d = Step; d <= WallDistance; d += Step)
        {
            Vector3 p = e.position + dir * d;
            if (Solid(world, p + Vector3.up * 0.5f) || Solid(world, p + Vector3.up * 1.3f)) return d;
        }
        return -1f;
    }

    static bool Solid(World world, Vector3 p)
    {
        BlockValue bv = world.GetBlock(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
        if (bv.isair) return false;
        Block b = bv.Block;
        return b != null && b.IsCollideMovement;
    }
}

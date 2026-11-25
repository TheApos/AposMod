// Weapon Mastery 2.0, shotguns (docs/Design-Mastery2.md 5б; driven by AposMastery.cs):
// - Class trait "Sledgehammer in the Barrel": slugs throw zombies back and down (force 1000), buckshot does it point
//   blank (within 3 m, force 300). Elites only with Bouncer 2/2. The throw is done here (AposKnock), not in XML.
// - Branch A (slugs): Heavy Slug, Brawn (x Strength), Battering Ram, Through and Through, Shattered, Pump Action,
//   Bouncer, capstone Cannonball.
// - Branch B (buckshot): Extra Pellets, Wide Choke, Ragged Wounds, Quick Pump, Shred, Bloodletting, Lead Wall,
//   capstone Dragon's Breath.
// - Hybrids: Shockwave, Blood and Bone, Frag Slug.
// Every pellet is its own hit (damageEntityLocal); shots come from XML (aposSShot on the player, entityclasses.xml).
using System.Collections.Generic;
using UnityEngine;

public static class AposShotgun
{
    const float SlugForce = 1000f, BuckForce = 300f, PointBlank = 3f;
    const float SlugDown = 2.5f, BuckDown = 2f;
    const float PelletBase = 10.1f;          // vanilla buckshot pellet damage
    const string Shattered = "buffAposShattered";

    static readonly float[] HeavySlug = { 0f, 0.15f, 0.30f, 0.50f };
    static readonly float[] Brawn = { 0f, 20f, 35f, 50f };          // x Strength, max 500
    static readonly float[] RamRadius = { 0f, 1.5f, 2f, 2.5f };
    static readonly int[] Pierce = { 0, 3, 6, 10 };
    static readonly int[] PumpRounds = { 0, 1, 2 };
    static readonly float[] Bouncer = { 0f, 1.5f, 3f };
    static readonly int[] ExtraPellets = { 0, 3, 6, 10 };
    static readonly float[] PelletMult = { 1f, 0.75f, 0.5f, 0.4f };
    static readonly float[] Choke = { 0f, 0.3f, 0.6f, 1f };
    static readonly float[] ChokeRange = { 0f, 2f, 4f, 6f };
    static readonly float[] Ragged = { 0f, 5f, 10f, 20f };
    static readonly float[] QuickPump = { 0f, 15f, 30f, 50f };
    static readonly float[] Bloodletting = { 0f, 0.3f, 0.8f };

    static int R(string id) { return AposMastery.Rank(AposMastery.Shotguns, id); }

    // ===== the current shot =====
    static readonly Dictionary<int, int> pellets = new Dictionary<int, int>(); // target id -> pellets this shot
    static readonly HashSet<int> knocked = new HashSet<int>();                // thrown this shot
    static bool wallBonus;                                                     // Lead Wall 2/2: the next shot +50%

    // Battering Ram: zombies near the landing spot fall too, a moment after the throw
    class Landing { public EntityAlive Body; public float At; public int Damage; }
    static readonly List<Landing> landings = new List<Landing>();

    public static void Reset()
    {
        pellets.Clear(); knocked.Clear(); landings.Clear();
        wallBonus = false;
    }

    // ===== ammo =====
    static bool SlugLoaded(EntityPlayerLocal player)
    {
        ItemClass held = player.inventory.holdingItem;
        ItemActionAttack attack = held != null && held.Actions != null && held.Actions.Length > 0 ? held.Actions[0] as ItemActionAttack : null;
        if (attack == null || attack.MagazineItemNames == null || attack.MagazineItemNames.Length == 0) return false;
        int i = player.inventory.holdingItemItemValue.SelectedAmmoTypeIndex;
        if (i < 0 || i >= attack.MagazineItemNames.Length) i = 0;
        return attack.MagazineItemNames[i].Contains("Slug");
    }

    // ===== every frame (AposMastery.Update) =====
    public static void Tick(EntityPlayerLocal player, bool held)
    {
        float shots = player.Buffs.GetCustomVar("aposSShot");
        if (shots > 0f)
        {
            player.Buffs.SetCustomVar("aposSShot", 0f);
            if (held) OnShot(player);
        }
        Settle(player);
        for (int i = landings.Count - 1; i >= 0; i--)
        {
            if (Time.time < landings[i].At) continue;
            Land(player, landings[i]);
            landings.RemoveAt(i);
        }
        if (!held) return;

        bool slug = SlugLoaded(player);
        bool cannon = R("A8") > 0;
        AposMastery.SetStat(AposMastery.StatSpeed, QuickPump[R("B4")] / 100f);
        AposMastery.SetStat(AposMastery.StatReload, QuickPump[R("B4")] / 100f);
        if (slug)
        {
            AposMastery.SetStat(AposMastery.StatPierce, cannon ? 99f : Pierce[R("A4")]);
        }
        else
        {
            AposMastery.SetStat(AposMastery.StatRays, ExtraPellets[R("B1")]);
            AposMastery.SetStat(AposMastery.StatBuckSpread, Choke[R("B2")]);
            AposMastery.SetStat(AposMastery.StatRange, ChokeRange[R("B2")]);
        }
        if (wallBonus) AposHud.Add(false, "shotgun", "+50%", new Color(1f, 0.82f, 0.3f), true);
    }

    // a new shot: the Lead Wall bonus (if armed) is for this shot; start counting its pellets
    static bool shotBonus;      // Lead Wall 2/2 applies to the shot being fired
    static bool settled = true; // this shot's Lead Wall is decided
    static float lastShot;

    static void OnShot(EntityPlayerLocal player)
    {
        shotBonus = wallBonus;
        wallBonus = false;
        pellets.Clear();
        knocked.Clear();
        settled = false;
        lastShot = Time.time;
    }

    // a quarter of a second after the shot all its pellets have landed: 5+ zombies hit -> Lead Wall
    static void Settle(EntityPlayerLocal player)
    {
        if (settled || Time.time - lastShot < 0.25f) return;
        settled = true;
        if (pellets.Count >= 5 && R("B7") > 0)
        {
            AposAmmo.LoadRounds(player, 1);
            wallBonus = R("B7") >= 2;
        }
    }

    // ===== hits =====
    public static int ModifyHit(EntityPlayerLocal player, EntityAlive target, DamageSource source, int strength)
    {
        if (player == null) return strength;
        bool slug = SlugLoaded(player);
        float mult = 1f;
        if (slug)
        {
            mult *= 1f + HeavySlug[R("A1")];
            if (R("A8") > 0) mult *= 2f;            // Cannonball
            if (R("B8") > 0) mult *= 0.6f;          // Dragon's Breath downside
        }
        else
        {
            mult *= PelletMult[R("B1")];             // more pellets, each weaker
            if (R("A8") > 0) mult *= 0.6f;          // Cannonball downside
            int n;
            pellets.TryGetValue(target.entityId, out n);
            pellets[target.entityId] = ++n;
            // Shred: the 7th pellet in one zombie tears a limb off and stuns it
            if (n == 7 && R("B5") > 0)
            {
                AposMastery.RequestDismember();
                target.Buffs.AddBuff("buffInjuryStunned01", -1, true, false, 1f);
            }
        }
        if (R("B6") > 0 && target.Buffs.HasBuff("buffInjuryBleeding")) mult += Bloodletting[R("B6")];
        if (shotBonus) mult += 0.5f;
        return Mathf.Max(1, Mathf.RoundToInt(strength * mult));
    }


    public static void AfterHit(EntityPlayerLocal player, EntityAlive target, DamageResponse response, int dealt)
    {
        if (player == null) return;
        bool slug = SlugLoaded(player);
        if (slug)
        {
            if (response.Fatal && R("A6") > 0) AposAmmo.LoadRounds(player, PumpRounds[R("A6")]);
            if (!response.Fatal) Throw(player, target, (SlugForce + BrawnForce(player)) * (R("A8") > 0 ? 2f : 1f), SlugDown, dealt);
            if (R("H3") > 0) Frag(player, target);
        }
        else
        {
            if (!response.Fatal)
            {
                if (Random.value * 100f < Ragged[R("B3")]) target.Buffs.AddBuff("buffInjuryBleeding");
                if (R("B8") > 0 && Random.value < 0.2f) target.Buffs.AddBuff("buffBurningElement");
                if (!knocked.Contains(target.entityId) && (target.position - player.position).magnitude <= PointBlank)
                    Throw(player, target, BuckForce + BrawnForce(player), BuckDown, dealt);
            }
        }
        if (response.Fatal)
        {
            // Shockwave: any shotgun kill knocks down everyone within 2.5 m of the body
            if (R("H1") > 0)
            {
                foreach (EntityAlive e in Around(target, 2.5f)) AposKnock.Ragdoll(e, target.position, 300f, 1.5f);
            }
            // Blood and Bone: a zombie that dies bleeding sprays everyone within 3 m
            if (R("H2") > 0 && target.Buffs.HasBuff("buffInjuryBleeding"))
            {
                foreach (EntityAlive e in Around(target, 3f)) e.Buffs.AddBuff("buffInjuryBleeding");
            }
        }
    }

    static float BrawnForce(EntityPlayerLocal player)
    {
        return Mathf.Min(500f, Brawn[R("A2")] * AposAffixes.Level(player, "attStrength"));
    }

    // throw back and down; Shattered, Bouncer, Battering Ram, Blood and Bone hang on it
    static void Throw(EntityPlayerLocal player, EntityAlive target, float force, float down, int dealt)
    {
        if (target.IsDead() || knocked.Contains(target.entityId)) return;
        bool elite = AposElites.IsElite(target);
        int bouncer = R("A7");
        if (elite && bouncer < 2) return;
        knocked.Add(target.entityId);
        float time = elite ? 1.5f : down + Bouncer[bouncer];
        AposKnock.Ragdoll(target, player.position, force, time);
        if (R("A5") > 0) target.Buffs.AddBuff(Shattered, -1, true, false, time + 3f);
        if (R("H2") > 0) target.Buffs.AddBuff("buffInjuryBleeding");
        int ram = R("A3");
        if (ram > 0) landings.Add(new Landing { Body = target, At = Time.time + 0.6f, Damage = ram >= 3 ? Mathf.RoundToInt(dealt * 0.25f) : 0 });
    }

    static void Land(EntityPlayerLocal player, Landing l)
    {
        if (l.Body == null) return;
        float r = RamRadius[R("A3")];
        foreach (EntityAlive e in Around(l.Body, r))
        {
            AposKnock.Ragdoll(e, l.Body.position, 300f, 1.5f);
            if (R("A5") > 0) e.Buffs.AddBuff(Shattered, -1, true, false, 4.5f);
            if (l.Damage > 0) AposSplash.Deal(e, l.Damage, player.entityId, EnumDamageTypes.Bashing);
        }
    }

    // Frag Slug: the slug bursts into 6 fragments at the nearest zombies within 5 m, each like a pellet with branch B
    static void Frag(EntityPlayerLocal player, EntityAlive center)
    {
        List<EntityAlive> near = new List<EntityAlive>(Around(center, 5f));
        near.Sort((a, b) => (a.position - center.position).sqrMagnitude.CompareTo((b.position - center.position).sqrMagnitude));
        int dmg = Mathf.Max(1, Mathf.RoundToInt(PelletBase * PelletMult[R("B1")] * 2f));
        for (int i = 0; i < near.Count && i < 6; i++)
        {
            EntityAlive e = near[i];
            int d = dmg;
            if (R("B6") > 0 && e.Buffs.HasBuff("buffInjuryBleeding")) d = Mathf.RoundToInt(d * (1f + Bloodletting[R("B6")]));
            AposSplash.Deal(e, d, player.entityId, EnumDamageTypes.Piercing);
            if (e.IsDead()) continue;
            if (Random.value * 100f < Ragged[R("B3")]) e.Buffs.AddBuff("buffInjuryBleeding");
            if (R("B8") > 0 && Random.value < 0.2f) e.Buffs.AddBuff("buffBurningElement");
        }
    }

    // "Shattered" mark above the head of every shattered zombie within 40 m: a red "x1.5" (AposMastery.OnGUI)
    static GUIStyle markStyle;

    public static void Draw(EntityPlayerLocal player)
    {
        if (player == null || player.playerCamera == null) return;
        Camera cam = player.playerCamera;
        List<Entity> list = GameManager.Instance.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e.IsDead() || e is EntityPlayer || !e.Buffs.HasBuff(Shattered)) continue;
            if ((e.position - player.position).sqrMagnitude > 40f * 40f) continue;
            Transform head = e.emodel != null ? e.emodel.GetHeadTransform() : null;
            Vector3 at = (head != null ? head.position : e.transform.position + Vector3.up * e.height) + Vector3.up * 0.35f;
            Vector3 v = cam.WorldToViewportPoint(at);
            if (v.z <= 0f) continue;
            if (markStyle == null) markStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            markStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(26f * Screen.height / 1080f * 6f / Mathf.Max(3f, v.z), 12f, 30f));
            Rect r = new Rect(v.x * Screen.width - 40f, (1f - v.y) * Screen.height - 15f, 80f, 30f);
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), "x1.5", markStyle);
            GUI.color = new Color(1f, 0.3f, 0.2f, 1f);
            GUI.Label(r, "x1.5", markStyle);
            GUI.color = old;
        }
    }

    static IEnumerable<EntityAlive> Around(EntityAlive center, float radius)
    {
        List<Entity> list = GameManager.Instance.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e == center || e is EntityPlayer || e.IsDead()) continue;
            if ((e.position - center.position).sqrMagnitude <= radius * radius) yield return e;
        }
    }
}

// Throwing a zombie back and down from C#, the same way the XML "Ragdoll" action does (MinEventActionRagdoll):
// the force can then come from ranks and attributes.
public static class AposKnock
{
    public static void Ragdoll(EntityAlive target, Vector3 from, float force, float seconds)
    {
        if (target == null || target.IsDead() || target is EntityPlayer || target.EntityTags.Test_AnySet(TraderTag)) return;
        if (target.AttachedToEntity != null) target.Detach();
        Vector3 dir = target.position - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        DamageResponse dr = DamageResponse.New(false);
        dr.StunDuration = seconds;
        dr.Strength = (int)force;
        dr.Source = new DamageSource(EnumDamageSource.External, EnumDamageTypes.Bashing, dir.normalized);
        target.DoRagdoll(ref dr, EModelBase.RagdollMode.Default);
    }

    static readonly FastTags<TagGroup.Global> TraderTag = FastTags<TagGroup.Global>.Parse("trader");
}

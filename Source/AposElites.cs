// Elite zombies and Eridium (design: docs/Design-Elites.md).
// - Spawn (Harmony, EntityAlive.OnAddedToWorld): a zombie or animal becomes elite with 2% -> 5% chance by the player's
//   gamestage; 1 modifier, 2 after gamestage 100. State lives in cvars and buffs, so it survives a save:
//   aposElite = number of modifiers, aposEliteMods = bit mask, buffAposElite (+100% health) + buffAposElite<Modifier>.
// - Name tag above the head (OnGUI), regeneration, giant scale, summoner: this MonoBehaviour.
// - Death (Harmony, EntityAlive.OnEntityDeath): weapon with affixes + Eridium, explosive blast.
// - Armored / vampire damage changes: Source/AposCombat.cs. Other modifiers are plain buffs in buffs.xml.
// - Eridium is spent in the Eridium Forge (Source/AposForge.cs).
// - Scrapping a weapon with affixes gives Eridium when the scrap finishes: open crafting window
//   (XUiC_RecipeStack.outputStack) or a closed workstation (TileEntityWorkstation.HandleRecipeQueue).
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

public class AposElites : MonoBehaviour
{
    public static readonly string[] Modifiers =
        { "Armored", "Fast", "Regen", "Explosive", "Vampire", "Burning", "Electric", "Acidic", "Giant", "Summoner" };

    const float TagRange = 40f;
    const string HealthVar = "aposEliteHP";  // max health bonus read by buffAposElite: Settings.xml health<stars> - 1
    const string DamageVar = "aposEliteDmg"; // damage bonus read by buffAposElite: Settings.xml damage<stars> - 1
    const float RegenDelay = 3f;
    // Regenerating and Explosive grow with the stars (index = stars - 1)
    static readonly float[] RegenShare = { 0.03f, 0.045f, 0.065f };
    static readonly float[] ExplosiveRadius = { 3f, 4.5f, 6f };
    static readonly int[] ExplosivePlayerDamage = { 100, 250, 500 };
    static readonly int[] ExplosiveZombieDamage = { 150, 250, 400 };

    static int StarIndex(EntityAlive e)
    {
        return Mathf.Clamp((int)e.Buffs.GetCustomVar("aposElite"), 1, 3) - 1;
    }
    const float GiantScale = 1.3f;
    public const string Eridium = "resourceAposEridium";

    static readonly System.Random rand = new System.Random();
    static readonly FastTags<TagGroup.Global> SpawnTags = FastTags<TagGroup.Global>.Parse("zombie,animal");
    public static bool SuppressRoll; // set while a summoner spawns its helpers

    // last time each entity took damage (regeneration waits 3 s after it)
    static readonly Dictionary<int, float> lastDamaged = new Dictionary<int, float>();
    static List<string> weapons;
    static string weaponsExclude;

    readonly List<EntityAlive> elites = new List<EntityAlive>();
    readonly HashSet<int> scaled = new HashSet<int>();
    EntityPlayerLocal player;
    Camera cam;
    GUIStyle style;
    float nextTick;

    public static bool IsElite(EntityAlive e)
    {
        return e != null && e.Buffs != null && e.Buffs.GetCustomVar("aposElite") > 0f;
    }

    public static bool Has(EntityAlive e, string modifier)
    {
        return e.Buffs.HasBuff("buffAposElite" + modifier);
    }

    public static void NoteDamaged(EntityAlive e)
    {
        lastDamaged[e.entityId] = Time.time;
    }

    // ===== Spawn =====
    public static void OnSpawn(EntityAlive e)
    {
        if (e == null || e is EntityPlayer || e.Buffs == null || SuppressRoll) return;
        if (!e.HasAnyTags(SpawnTags) || e.Buffs.GetCustomVar("aposElite") != 0f) return; // already decided (or loaded)
        EntityPlayerLocal p = AposRewards.Player();
        if (p == null) return;

        int stage = p.gameStage;
        float chance = Mathf.Lerp(0.02f, 0.05f, Mathf.Clamp01(stage / 200f));
        if (rand.NextDouble() >= chance)
        {
            e.Buffs.SetCustomVar("aposElite", -1f); // rolled, not elite: do not roll again after a reload
            return;
        }

        // 1 modifier (1 star); 2 after gamestage 100; after gamestage 200 a quarter of them get 3
        int count = stage >= 100 ? 2 : 1;
        if (stage >= 200 && rand.NextDouble() < 0.25) count = 3;
        Make(e, RandomMask(e, count));
    }

    public static int RandomMask(EntityAlive e, int count)
    {
        int mask = 0;
        while (Bits(mask) < count)
        {
            int m = rand.Next(Modifiers.Length);
            if (Modifiers[m] == "Summoner" && e.HasAnyTags(FastTags<TagGroup.Global>.Parse("animal"))) continue;
            mask |= 1 << m;
        }
        return mask;
    }

    // Turns a zombie into an elite with the given modifiers (spawn roll and the aposelite make command)
    public static void Make(EntityAlive e, int mask)
    {
        int count = Bits(mask);
        e.Buffs.SetCustomVar("aposElite", count);
        e.Buffs.SetCustomVar("aposEliteMods", mask);
        e.Buffs.SetCustomVar("aposEliteFill", 1f);
        e.Buffs.SetCustomVar(HealthVar, AposSettings.EliteHealth[StarIndex(e)] - 1f);
        e.Buffs.SetCustomVar(DamageVar, AposSettings.EliteDamage[StarIndex(e)] - 1f);
        e.Buffs.AddBuff("buffAposElite");
        for (int i = 0; i < Modifiers.Length; i++)
        {
            if ((mask & (1 << i)) != 0) e.Buffs.AddBuff("buffAposElite" + Modifiers[i]);
        }
    }

    static int Bits(int mask)
    {
        int n = 0;
        for (; mask != 0; mask &= mask - 1) n++;
        return n;
    }

    // ===== Death =====
    public static void OnDeath(EntityAlive e)
    {
        if (!IsElite(e) || e.Buffs.GetCustomVar("aposEliteDropped") > 0f) return;
        e.Buffs.SetCustomVar("aposEliteDropped", 1f);
        int count = (int)e.Buffs.GetCustomVar("aposElite");
        Vector3 pos = e.GetPosition() + Vector3.up * 0.8f;

        // weapon with affixes: quality 3-5 (1 star), 5-6 (2 stars), 6 (3 stars); legendary 2% (2 stars) / 10% (3 stars)
        // Settings.xml weaponDrop + drop<Biome> / "aposelite drop [biome] N": chance of the weapon (Eridium always drops)
        ItemStack loot = null;
        bool weapon = rand.NextDouble() * 100 < AposSettings.EliteDropChance(BiomeName(e));
        double legendChance = count >= 3 ? 0.1 : count == 2 ? 0.02 : 0;
        if (weapon && rand.NextDouble() < legendChance) loot = AposRewards.RandomLegendary();
        if (weapon && loot == null) loot = RandomWeapon(count >= 3 ? 6 : count == 2 ? rand.Next(5, 7) : rand.Next(3, 6));
        if (loot != null) Drop(loot, pos);
        int eridium = count >= 3 ? rand.Next(20, 31) : count == 2 ? rand.Next(8, 16) : rand.Next(3, 7);
        Drop(new ItemStack(ItemClass.GetItem(Eridium), eridium), pos);

        if (Has(e, "Explosive")) Blast(e);
    }

    // biome the elite died in: the one it stood on, else the biome map at its position
    static string BiomeName(EntityAlive e)
    {
        BiomeDefinition b = e.biomeStandingOn;
        if (b == null)
        {
            World world = GameManager.Instance.World;
            Vector3i p = World.worldToBlockPos(e.GetPosition());
            if (world != null) b = world.GetBiome(p.x, p.z);
        }
        return b != null ? b.m_sBiomeName : null;
    }

    public static void Drop(ItemStack stack, Vector3 pos)
    {
        if (stack == null || stack.IsEmpty()) return;
        GameManager.Instance.ItemDropServer(stack, pos, new Vector3(0.5f, 0f, 0.5f), -1, 600f, false);
    }

    // Weapons an elite can drop: player weapons that take affixes. Items hidden from the creative menu (NPC-only guns
    // of NPC mods, zombie hands, admin tools) or without a translated name are left out, and so is anything matching
    // Settings.xml weaponExclude (comma-separated item names or name starts). "aposelite weapons" lists the pool.
    public static List<string> WeaponPool()
    {
        if (weapons != null && weaponsExclude == AposSettings.EliteWeaponExclude) return weapons;
        weaponsExclude = AposSettings.EliteWeaponExclude;
        weapons = new List<string>();
        foreach (string n in AllWeapons()) if (!IsExcluded(n)) weapons.Add(n);
        return weapons;
    }

    // every player weapon an elite could drop before weaponExclude (no NPC-only, admin or test items)
    public static List<string> AllWeapons()
    {
        List<string> all = new List<string>();
        foreach (ItemClass ic in ItemClass.list)
        {
            if (ic == null || AposAffixes.Kind(ic) == 0) continue;
            if (ic.CreativeMode != EnumCreativeMode.Player && ic.CreativeMode != EnumCreativeMode.All) continue;
            string n = ic.Name;
            if (n.Contains("Admin") || n.Contains("Test") || n.Contains("Debug") || n.Contains("Candy")) continue;
            if (!Localization.Exists(n, false)) continue;
            all.Add(n);
        }
        return all;
    }

    // weaponExclude entries: comma-separated item names or name starts
    public static List<string> ExcludeList()
    {
        List<string> list = new List<string>();
        foreach (string part in (AposSettings.EliteWeaponExclude ?? "").Split(','))
        {
            string t = part.Trim();
            if (t.Length > 0) list.Add(t);
        }
        return list;
    }

    // would another weaponExclude entry still exclude this name?
    public static bool IsExcludedBy(string name, List<string> entries)
    {
        foreach (string x in entries)
        {
            if (name.StartsWith(x, System.StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static bool IsExcluded(string name)
    {
        foreach (string x in ExcludeList())
        {
            if (name.StartsWith(x, System.StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    static ItemStack RandomWeapon(int quality)
    {
        List<string> pool = WeaponPool();
        if (pool.Count == 0) return null;
        ItemValue iv = ItemClass.GetItem(pool[rand.Next(pool.Count)]);
        if (iv == null || iv.type == 0) return null;
        iv = new ItemValue(iv.type, quality, quality, true, null, 1f);
        AposAffixes.Roll(iv, true);
        return new ItemStack(iv, 1);
    }

    // Explosive: 100/250/500 damage to players and 150/250/400 to zombies within 3/4.5/6 m by stars, no block damage
    static void Blast(EntityAlive dead)
    {
        int s = StarIndex(dead);
        dead.Buffs.AddBuff("buffAposChainBlast"); // visual only
        List<Entity> list = GameManager.Instance.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e == dead || e.IsDead()) continue;
            if ((e.position - dead.position).sqrMagnitude > ExplosiveRadius[s] * ExplosiveRadius[s]) continue;
            if (e is EntityPlayer)
            {
                e.DamageEntity(new DamageSource(EnumDamageSource.External, EnumDamageTypes.Heat), ExplosivePlayerDamage[s], false, 1f);
            }
            else
            {
                AposSplash.Deal(e, ExplosiveZombieDamage[s], -1, EnumDamageTypes.Heat);
            }
        }
    }

    // ===== Per frame: elite list, fill health, giant scale, regeneration, summoner =====
    void Update()
    {
        GameManager gm = GameManager.Instance;
        player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        elites.Clear();
        if (player == null)
        {
            scaled.Clear();
            lastDamaged.Clear();
            return;
        }
        cam = player.playerCamera;
        bool tick = Time.time >= nextTick;
        if (tick) nextTick = Time.time + 1f;

        List<Entity> list = gm.World.Entities.list;
        UpdateGlows(list, tick);
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e is EntityPlayer || e.IsDead() || !IsElite(e)) continue;
            elites.Add(e);

            // max health and damage by stars from Settings.xml (also for elites from an older save or after
            // "aposelite health / damage")
            float hpBonus = AposSettings.EliteHealth[StarIndex(e)] - 1f;
            if (e.Buffs.GetCustomVar(HealthVar) != hpBonus) e.Buffs.SetCustomVar(HealthVar, hpBonus);
            float dmgBonus = AposSettings.EliteDamage[StarIndex(e)] - 1f;
            if (e.Buffs.GetCustomVar(DamageVar) != dmgBonus) e.Buffs.SetCustomVar(DamageVar, dmgBonus);

            // the max health buff needs a moment to apply; then start at full health
            if (e.Buffs.GetCustomVar("aposEliteFill") > 0f && e.GetMaxHealth() > e.Health)
            {
                e.Health = e.GetMaxHealth();
                e.Buffs.SetCustomVar("aposEliteFill", 0f);
            }
            if (Has(e, "Giant") && scaled.Add(e.entityId)) e.transform.localScale *= GiantScale;

            if (!tick) continue;
            float last;
            if (Has(e, "Regen") && (!lastDamaged.TryGetValue(e.entityId, out last) || Time.time - last > RegenDelay) && e.Health < e.GetMaxHealth())
            {
                e.AddHealth(Mathf.Max(1, Mathf.RoundToInt(e.GetMaxHealth() * RegenShare[StarIndex(e)])));
            }
            if (Has(e, "Summoner") && e.Buffs.GetCustomVar("aposSummoned") <= 0f && e.Health < e.GetMaxHealth() * 0.5f)
            {
                e.Buffs.SetCustomVar("aposSummoned", 1f);
                Summon(gm.World, e);
            }
        }
        if (tick) Announce();
    }

    // ===== "Elite nearby" banner: once per elite (cvar on the zombie, saved with it), when it first comes within 40 m
    // in line of sight; several at once share one banner. Settings.xml Elites announce / "aposelite announce on|off". =====
    const float BannerTime = 7f;
    const string SeenVar = "aposEliteSeen";
    float bannerUntil;
    string bannerTitle, bannerText;
    int bannerStars;
    GUIStyle bannerTitleStyle, bannerStyle;

    void Announce()
    {
        if (player == null) return;
        List<EntityAlive> fresh = new List<EntityAlive>();
        for (int i = 0; i < elites.Count; i++)
        {
            EntityAlive e = elites[i];
            if (e.Buffs.GetCustomVar(SeenVar) > 0f) continue;
            if ((e.position - player.position).sqrMagnitude > TagRange * TagRange || !InSight(player, e)) continue;
            e.Buffs.SetCustomVar(SeenVar, 1f);
            fresh.Add(e);
        }
        if (fresh.Count == 0 || !AposSettings.EliteAnnounce) return;

        int stars = 1, mask = 0;
        List<string> names = new List<string>();
        foreach (EntityAlive e in fresh)
        {
            int s = StarIndex(e) + 1;
            stars = Math.Max(stars, s);
            mask |= (int)e.Buffs.GetCustomVar("aposEliteMods");
            names.Add(Localization.Get(EntityClass.list[e.entityClass].entityClassName)
                + (fresh.Count > 1 ? " (" + string.Format(Localization.Get("aposEliteStarsN"), s) + ")" : ""));
        }
        bannerTitle = fresh.Count == 1 ? string.Format(Localization.Get("aposEliteNear"), names[0])
            : string.Format(Localization.Get("aposEliteNearMany"), string.Join(", ", names.ToArray()));
        List<string> tips = new List<string>();
        for (int i = 0; i < Modifiers.Length; i++)
        {
            if ((mask & (1 << i)) == 0) continue;
            string tip = Localization.Get("aposEliteTip" + Modifiers[i]);
            if (Modifiers[i] == "Explosive") tip = string.Format(tip, ExplosivePlayerDamage[stars - 1], ExplosiveRadius[stars - 1]);
            if (Modifiers[i] == "Regen") tip = string.Format(tip, Mathf.RoundToInt(RegenShare[stars - 1] * 1000f) / 10f);
            tips.Add("[" + Localization.Get("aposElite" + Modifiers[i]) + "] " + tip);
        }
        bannerText = string.Join("\n", tips.ToArray());
        bannerStars = stars;
        bannerUntil = Time.time + BannerTime;
        Audio.Manager.PlayInsidePlayerHead("keystone_build_warning", player.entityId);
    }

    void DrawBanner()
    {
        if (Time.time > bannerUntil || bannerTitle == null) return;
        if (bannerTitleStyle == null)
        {
            bannerTitleStyle = new GUIStyle(GUI.skin.label);
            bannerTitleStyle.alignment = TextAnchor.MiddleCenter;
            bannerTitleStyle.fontStyle = FontStyle.Bold;
            bannerTitleStyle.wordWrap = true;
            bannerStyle = new GUIStyle(GUI.skin.label);
            bannerStyle.alignment = TextAnchor.UpperCenter;
            bannerStyle.wordWrap = true;
        }
        float k = AposSettings.Scale;
        bannerTitleStyle.fontSize = Mathf.RoundToInt(26 * k);
        bannerStyle.fontSize = Mathf.RoundToInt(19 * k);
        float alpha = Mathf.Clamp01(bannerUntil - Time.time); // fades out in the last second
        float w = Mathf.Min(Screen.width - 40f, 900f * k);
        float titleH = bannerTitleStyle.CalcHeight(new GUIContent(bannerTitle), w - 20f) + 6f;
        float textH = string.IsNullOrEmpty(bannerText) ? 0f : bannerStyle.CalcHeight(new GUIContent(bannerText), w - 20f) + 8f;
        Rect box = new Rect((Screen.width - w) / 2f, Screen.height * 0.12f, w, titleH + textH + 14f);
        Color color = bannerStars >= 3 ? new Color(1f, 0.25f, 0.25f) : bannerStars == 2 ? new Color(1f, 0.55f, 0.1f) : new Color(1f, 0.85f, 0.2f);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f * alpha);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = new Color(color.r, color.g, color.b, alpha);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 3f), Texture2D.whiteTexture);
        DrawStars(Screen.width / 2f, box.y + 4f, bannerStars, GUI.color);
        GUI.Label(new Rect(box.x + 10f, box.y + 6f, w - 20f, titleH), bannerTitle, bannerTitleStyle);
        GUI.color = new Color(1f, 1f, 1f, alpha);
        if (textH > 0f) GUI.Label(new Rect(box.x + 10f, box.y + 8f + titleH, w - 20f, textH), bannerText, bannerStyle);
        GUI.color = old;
    }

    // ===== Loot on the ground: a light and a Borderlands-style loot beam above Eridium (purple), weapons with
    // affixes (gold) and legendaries (orange), so a weapon drop next to the Eridium sack is easy to see; optionally
    // (Settings.xml LootBeam bags / "aposbags on") white above the vanilla zombie loot bags =====
    const float BeamHeight = 5f;
    static readonly Color EridiumColor = new Color(0.75f, 0.35f, 1f);
    static readonly Color AffixColor = new Color(1f, 0.85f, 0.3f);
    static readonly Color LegendaryColor = new Color(1f, 0.45f, 0.05f);
    static readonly Color BagColor = new Color(0.95f, 0.95f, 0.95f);
    readonly Dictionary<int, Transform> glows = new Dictionary<int, Transform>();
    readonly HashSet<int> bagGlows = new HashSet<int>();
    readonly List<int> goneGlows = new List<int>();
    static Material beamMaterial;
    static ItemValue eridiumValue;

    void UpdateGlows(List<Entity> list, bool scan)
    {
        if (scan)
        {
            if (eridiumValue == null) eridiumValue = ItemClass.GetItem(Eridium);
            for (int i = 0; i < list.Count; i++)
            {
                EntityItem item = list[i] as EntityItem;
                if (item == null || item.IsDead() || glows.ContainsKey(item.entityId) || item.itemStack == null) continue;
                Color color;
                if (!GlowColor(item.itemStack.itemValue, out color)) continue;
                glows[item.entityId] = AddGlow(item.transform, color);
            }
            if (AposSettings.LootBagBeam)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    EntityLootContainer bag = list[i] as EntityLootContainer;
                    // no IsDead check: a bag is "dead" from its first frame (EntityContainerAbs.Start calls SetDead)
                    if (bag == null || glows.ContainsKey(bag.entityId)) continue;
                    glows[bag.entityId] = AddGlow(bag.transform, BagColor);
                    bagGlows.Add(bag.entityId);
                }
            }
        }
        // switched off in game: take the beams off the bags
        if (!AposSettings.LootBagBeam && bagGlows.Count > 0)
        {
            foreach (int id in bagGlows)
            {
                Transform t;
                if (glows.TryGetValue(id, out t))
                {
                    if (t != null) Destroy(t.gameObject);
                    glows.Remove(id);
                }
            }
            bagGlows.Clear();
        }
        // the sack rolls; keep the beam pointing straight up, forget picked up / despawned sacks
        goneGlows.Clear();
        foreach (KeyValuePair<int, Transform> kv in glows)
        {
            if (kv.Value == null) goneGlows.Add(kv.Key);
            else
            {
                kv.Value.rotation = Quaternion.identity;
                Pulse(kv.Value, kv.Key * 0.7f);
            }
        }
        for (int i = 0; i < goneGlows.Count; i++)
        {
            glows.Remove(goneGlows[i]);
            bagGlows.Remove(goneGlows[i]);
        }
    }

    static bool GlowColor(ItemValue iv, out Color color)
    {
        color = EridiumColor;
        if (iv == null || iv.type == 0) return false;
        if (iv.type == eridiumValue.type) return true;
        ItemClass ic = iv.ItemClass;
        if (ic == null) return false;
        color = LegendaryColor;
        if (ic.Name.StartsWith("aposLegend")) return true;
        color = AffixColor;
        return AposAffixes.Kind(ic) != 0 && iv.Stats != null && AposAffixes.Decompose(iv).Count > 0;
    }

    // Borderlands-style beam: a soft wide glow (gradient texture across its width) with a bright thin core, both fading
    // upwards, a thin ring on the ground and a slow pulse (Pulse, every frame). Additive when the shader exists.
    static Texture2D beamTex;

    public static Material BeamMaterial()
    {
        if (beamMaterial != null) return beamMaterial;
        Shader shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
        if (shader == null) return null;
        beamTex = new Texture2D(4, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        {
            float v = Mathf.Abs((y + 0.5f) / 64f * 2f - 1f);   // 0 in the middle of the beam's width, 1 at its edges
            float a = Mathf.Pow(1f - v, 2.2f);
            for (int x = 0; x < 4; x++) beamTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        beamTex.wrapMode = TextureWrapMode.Clamp;
        beamTex.Apply();
        beamMaterial = new Material(shader);
        beamMaterial.mainTexture = beamTex;
        return beamMaterial;
    }

    static LineRenderer Line(GameObject go, Material m)
    {
        LineRenderer l = go.AddComponent<LineRenderer>();
        l.useWorldSpace = false;
        l.material = m;
        l.textureMode = LineTextureMode.Stretch;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        l.receiveShadows = false;
        return l;
    }

    static Transform AddGlow(Transform parent, Color color)
    {
        GameObject go = new GameObject("AposEridiumGlow");
        go.transform.SetParent(parent, false);
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = 4f;
        light.intensity = 3f;

        Material m = BeamMaterial();
        if (m == null) return go.transform;
        // wide soft glow
        LineRenderer glow = Line(go, m);
        glow.positionCount = 2;
        glow.SetPosition(0, Vector3.zero);
        glow.SetPosition(1, Vector3.up * BeamHeight);
        glow.startWidth = 0.55f;
        glow.endWidth = 0.2f;
        glow.startColor = new Color(color.r, color.g, color.b, 0.55f);
        glow.endColor = new Color(color.r, color.g, color.b, 0f);
        // bright core
        GameObject core = new GameObject("core");
        core.transform.SetParent(go.transform, false);
        LineRenderer c = Line(core, m);
        c.positionCount = 2;
        c.SetPosition(0, Vector3.zero);
        c.SetPosition(1, Vector3.up * BeamHeight * 0.8f);
        c.startWidth = 0.12f;
        c.endWidth = 0.03f;
        Color bright = Color.Lerp(color, Color.white, 0.6f);
        c.startColor = new Color(bright.r, bright.g, bright.b, 1f);
        c.endColor = new Color(bright.r, bright.g, bright.b, 0f);
        // ring on the ground (lies flat: the line faces the ring object's Z, turned to point up)
        GameObject ring = new GameObject("ring");
        ring.transform.SetParent(go.transform, false);
        ring.transform.localPosition = Vector3.up * 0.05f;
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        LineRenderer r = Line(ring, m);
        r.alignment = LineAlignment.TransformZ;
        r.loop = true;
        r.positionCount = 32;
        for (int i = 0; i < 32; i++)
        {
            float t = i / 32f * Mathf.PI * 2f;
            r.SetPosition(i, new Vector3(Mathf.Cos(t) * 0.45f, Mathf.Sin(t) * 0.45f, 0f));
        }
        r.startWidth = r.endWidth = 0.1f;
        r.startColor = r.endColor = new Color(color.r, color.g, color.b, 0.8f);
        return go.transform;
    }

    // slow breathing of the glow and the ring
    static void Pulse(Transform t, float phase)
    {
        float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.4f + phase);
        LineRenderer glow = t.GetComponent<LineRenderer>();
        if (glow != null)
        {
            Color c = glow.startColor;
            c.a = 0.4f + 0.25f * k;
            glow.startColor = c;
            glow.startWidth = 0.5f + 0.12f * k;
        }
        Transform ring = t.Find("ring");
        if (ring != null) ring.localScale = Vector3.one * (0.9f + 0.2f * k);
    }

    static void Summon(World world, EntityAlive caller)
    {
        SuppressRoll = true;
        try
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f;
                Vector3 pos = caller.position + new Vector3(Mathf.Cos(a) * 2f, 0.2f, Mathf.Sin(a) * 2f);
                Entity helper = EntityFactory.CreateEntity(caller.entityClass, pos);
                if (helper != null) world.SpawnEntityInWorld(helper);
            }
        }
        finally
        {
            SuppressRoll = false;
        }
    }

    // Line of sight for the name tag: nothing between the player's head and the elite's head. (The vanilla
    // EntityAlive.CanSee is the AI's sight: it depends on sight range and light and hits the target's own collider,
    // so the tag almost never showed.) The ray starts a bit in front of the player and stops short of the target.
    public static bool InSight(EntityPlayerLocal p, EntityAlive e)
    {
        Vector3 from = p.getHeadPosition(), to = e.getHeadPosition();
        Vector3 d = to - from;
        float dist = d.magnitude;
        if (dist < 1.5f) return true;
        Vector3 dir = d / dist;
        return !Voxel.Raycast(p.world, new Ray(from + dir * 0.6f, dir), dist - 1.2f, false, false);
    }

    // ===== Name tag: "Armored Explosive Moe", yellow (1 modifier) or orange (2), 40 m, only in line of sight =====
    void OnGUI()
    {
        if (Event.current.type == EventType.Repaint) DrawBanner();
        if (elites.Count == 0 || cam == null || player == null || !AposSettings.EliteTags || Event.current.type != EventType.Repaint) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
        }
        style.fontSize = Mathf.RoundToInt(22 * AposSettings.Scale);
        Color oldColor = GUI.color;
        for (int i = 0; i < elites.Count; i++)
        {
            EntityAlive e = elites[i];
            if ((e.position - player.position).sqrMagnitude > TagRange * TagRange) continue;
            if (!InSight(player, e)) continue;
            Vector3 vp = cam.WorldToViewportPoint(e.transform.position + Vector3.up * (e.height * e.transform.localScale.y + 0.6f));
            if (vp.z <= 0f) continue;
            float x = vp.x * Screen.width, y = (1f - vp.y) * Screen.height;
            string text = Name(e);
            int stars = Mathf.Clamp((int)e.Buffs.GetCustomVar("aposElite"), 1, 3);
            Color color = stars >= 3 ? new Color(1f, 0.25f, 0.25f) : stars == 2 ? new Color(1f, 0.55f, 0.1f) : new Color(1f, 0.85f, 0.2f);
            // a long name (3 modifiers) wraps: grow the box upwards so the stars stay above and the badges below it
            float h = Mathf.Max(40f, style.CalcHeight(new GUIContent(text), 500f) + 8f);
            Rect r = new Rect(x - 250f, y + 20f - h, 500f, h);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style);
            GUI.color = color;
            GUI.Label(r, text, style);
            DrawStars(x, r.y, stars, color);
            DrawBadges(x, r.yMax - 6f * AposSettings.Scale, (int)e.Buffs.GetCustomVar("aposEliteMods"), stars - 1);
        }
        GUI.color = oldColor;
    }

    // ===== modifier badges under the name: pictogram (Icons/elite_<Modifier>.png) + the number that matters =====
    static readonly string[] BadgeValue = { "x0.5", "+60%", "3%", "40", "50%", "", "", "-20", "+50%", "x3" };
    static readonly Color[] BadgeColor =
    {
        new Color(0.7f, 0.8f, 0.95f), new Color(0.3f, 0.9f, 1f), new Color(0.4f, 1f, 0.45f), new Color(1f, 0.6f, 0.15f),
        new Color(0.95f, 0.2f, 0.3f), new Color(1f, 0.45f, 0.1f), new Color(0.5f, 0.75f, 1f), new Color(0.6f, 1f, 0.2f),
        new Color(0.8f, 0.5f, 1f), new Color(0.7f, 0.55f, 1f),
    };
    static Texture2D[] badgeIcons;
    GUIStyle badgeStyle;

    static Texture2D BadgeIcon(int i)
    {
        if (badgeIcons == null)
        {
            badgeIcons = new Texture2D[Modifiers.Length];
            for (int k = 0; k < Modifiers.Length; k++)
            {
                string path = System.IO.Path.Combine(System.IO.Path.Combine(AposSettings.ModPath ?? "", "Icons"), "elite_" + Modifiers[k] + ".png");
                if (!System.IO.File.Exists(path)) continue;
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(tex, System.IO.File.ReadAllBytes(path))) badgeIcons[k] = tex;
            }
        }
        return badgeIcons[i];
    }

    // Regenerating and Explosive show the value of this elite's stars
    static string Badge(int i, int s)
    {
        if (Modifiers[i] == "Regen") return Mathf.RoundToInt(RegenShare[s] * 1000f) / 10f + "%";
        if (Modifiers[i] == "Explosive") return ExplosivePlayerDamage[s].ToString();
        return BadgeValue[i];
    }

    void DrawBadges(float centerX, float top, int mask, int s)
    {
        if (badgeStyle == null)
        {
            badgeStyle = new GUIStyle(GUI.skin.label);
            badgeStyle.alignment = TextAnchor.MiddleLeft;
            badgeStyle.fontStyle = FontStyle.Bold;
        }
        badgeStyle.fontSize = Mathf.RoundToInt(17 * AposSettings.Scale);
        float icon = 24f * AposSettings.Scale, gap = 10f * AposSettings.Scale;
        // measure the row first to center it
        float width = 0f;
        for (int i = 0; i < Modifiers.Length; i++)
        {
            if ((mask & (1 << i)) == 0) continue;
            string value = Badge(i, s);
            width += icon + gap + (value != "" ? badgeStyle.CalcSize(new GUIContent(value)).x + 2f : 0f);
        }
        float x = centerX - (width - gap) / 2f;
        for (int i = 0; i < Modifiers.Length; i++)
        {
            if ((mask & (1 << i)) == 0) continue;
            Texture2D tex = BadgeIcon(i);
            Rect ir = new Rect(x, top, icon, icon);
            if (tex != null)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                GUI.DrawTexture(new Rect(ir.x + 2f, ir.y + 2f, icon, icon), tex);
                GUI.color = BadgeColor[i];
                GUI.DrawTexture(ir, tex);
            }
            x += icon + 2f;
            string text = Badge(i, s);
            if (text != "")
            {
                float w = badgeStyle.CalcSize(new GUIContent(text)).x;
                Rect tr = new Rect(x, top, w, icon);
                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                GUI.Label(new Rect(tr.x + 2f, tr.y + 2f, w, icon), text, badgeStyle);
                GUI.color = BadgeColor[i];
                GUI.Label(tr, text, badgeStyle);
                x += w;
            }
            x += gap;
        }
    }

    // 1-3 stars above the name: how strong the elite is (and how good its loot)
    static Texture2D star;

    static void DrawStars(float centerX, float nameTop, int count, Color color)
    {
        if (star == null) star = MakeStar(64);
        float size = 26f * AposSettings.Scale, gap = 4f * AposSettings.Scale;
        float width = count * size + (count - 1) * gap;
        float left = centerX - width / 2f, top = nameTop - size;
        for (int i = 0; i < count; i++)
        {
            Rect s = new Rect(left + i * (size + gap), top, size, size);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(s.x + 2f, s.y + 2f, s.width, s.height), star);
            GUI.color = color;
            GUI.DrawTexture(s, star);
        }
    }

    // a white five-pointed star with soft edges (2x2 samples per pixel)
    static Texture2D MakeStar(int n)
    {
        Vector2[] poly = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float rad = (i % 2 == 0 ? 0.48f : 0.2f) * n;
            poly[i] = new Vector2(n / 2f + Mathf.Cos(a) * rad, n / 2f + Mathf.Sin(a) * rad - n * 0.03f);
        }
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                        if (Inside(poly, new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f))) inside++;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, inside / 4f));
            }
        tex.Apply();
        return tex;
    }

    static bool Inside(Vector2[] poly, Vector2 p)
    {
        bool c = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y)
                && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c;
        }
        return c;
    }

    static string Name(EntityAlive e)
    {
        int mask = (int)e.Buffs.GetCustomVar("aposEliteMods");
        string text = "";
        for (int i = 0; i < Modifiers.Length; i++)
        {
            if ((mask & (1 << i)) != 0) text += Localization.Get("aposElite" + Modifiers[i]) + " ";
        }
        return text + Localization.Get(EntityClass.list[e.entityClass].entityClassName);
    }

    // Eridium for scrapping a weapon: 2 per affix, x2 at quality 6; a legendary gives 50
    public static int ScrapEridium(ItemValue iv)
    {
        if (iv == null || iv.ItemClass == null) return 0;
        // half of the Eridium put into sharpening comes back
        int sharpened = AposTiers.Spent(iv) / 2;
        if (iv.ItemClass.Name.StartsWith("aposLegend")) return 50 + sharpened;
        int affixes = AposAffixes.Decompose(iv).Count;
        return affixes * 2 * (iv.Quality >= 6 ? 2 : 1) + sharpened;
    }

    public static void GiveEridium(int count)
    {
        EntityPlayerLocal p = AposRewards.Player();
        if (p == null || count <= 0) return;
        ItemStack stack = new ItemStack(ItemClass.GetItem(Eridium), count);
        // the "+N item" line in the lower right corner, like any picked up loot
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(p);
        if (ui != null && ui.xui != null && ui.xui.CollectedItemList != null) ui.xui.CollectedItemList.AddItemStack(stack.Clone(), false);
        if (!p.bag.AddItem(stack) && !p.inventory.AddItem(stack)) Drop(stack, p.GetPosition() + Vector3.up);
    }
}

[HarmonyPatch(typeof(EntityAlive), "OnAddedToWorld")]
public static class AposEliteSpawnPatch
{
    static void Postfix(EntityAlive __instance)
    {
        try { AposElites.OnSpawn(__instance); }
        catch (Exception e) { Debug.LogError("[AposMod] Elite spawn failed: " + e); }
    }
}

[HarmonyPatch(typeof(EntityAlive), "OnEntityDeath")]
public static class AposEliteDeathPatch
{
    static void Postfix(EntityAlive __instance)
    {
        try { AposElites.OnDeath(__instance); }
        catch (Exception e) { Debug.LogError("[AposMod] Elite death failed: " + e); }
    }
}

// Scrapping a weapon with affixes (or a legendary) also gives Eridium. Paid when the scrap finishes (the crafting
// queue outputs the materials), so cancelling a queued scrap gives nothing. The scrap recipe keeps the weapon as
// its only ingredient (ItemActionEntryScrap.OnActivated).
[HarmonyPatch(typeof(XUiC_RecipeStack), "outputStack")]
public static class AposScrapPatch
{
    static void Postfix(XUiC_RecipeStack __instance, Recipe ___recipe, ItemValue ___outputItemValue, bool __result)
    {
        if (!__result || ___recipe == null) return;
        if (!___recipe.IsScrap || ___recipe.ingredients.Count == 0) return;
        AposElites.GiveEridium(AposElites.ScrapEridium(___recipe.ingredients[0].itemValue));
    }
}

// A closed workstation finishes its queue by itself (the window path above does not run). The Eridium of every scrap
// that finished during this tick goes into the workstation output, like the scrap materials, and waits there.
// Finished = the scrap recipe's multiplier went down or its queue slot was cleared / moved on.
[HarmonyPatch(typeof(TileEntityWorkstation), "HandleRecipeQueue")]
public static class AposWorkstationScrapPatch
{
    static void Prefix(RecipeQueueItem[] ___queue, out Dictionary<Recipe, int> __state)
    {
        __state = Scraps(___queue);
        // Intellect Mastery 5 "Inventor": raised qualities while the workstation creates items (AposInventor)
        AposInventor.BeginQueue(___queue);
    }

    static void Finalizer()
    {
        AposInventor.EndQueue();
    }

    static void Postfix(TileEntityWorkstation __instance, RecipeQueueItem[] ___queue, ItemStack[] ___output, Dictionary<Recipe, int> __state)
    {
        if (__state == null) return;
        try
        {
            Dictionary<Recipe, int> left = Scraps(___queue);
            int eridium = 0;
            foreach (KeyValuePair<Recipe, int> kv in __state)
            {
                int now;
                if (left == null || !left.TryGetValue(kv.Key, out now)) now = 0;
                if (now < kv.Value) eridium += (kv.Value - now) * AposElites.ScrapEridium(kv.Key.ingredients[0].itemValue);
            }
            if (eridium <= 0) return;
            ItemStack stack = new ItemStack(ItemClass.GetItem(AposElites.Eridium), eridium);
            if (ItemStack.AddToItemStackArray(___output, stack, -1) < 0)
            {
                AposElites.Drop(stack, __instance.ToWorldPos().ToVector3() + new Vector3(0.5f, 1.5f, 0.5f));
            }
            Traverse.Create(__instance).Method("setModified").GetValue();
        }
        catch (Exception e)
        {
            Debug.LogError("[AposMod] Workstation scrap Eridium failed: " + e);
        }
    }

    // queued scraps that give Eridium -> how many are still to be made
    static Dictionary<Recipe, int> Scraps(RecipeQueueItem[] queue)
    {
        Dictionary<Recipe, int> d = null;
        if (queue == null) return d;
        for (int i = 0; i < queue.Length; i++)
        {
            RecipeQueueItem q = queue[i];
            if (q == null || q.Recipe == null || !q.Recipe.IsScrap || q.Multiplier <= 0 || q.Recipe.ingredients.Count == 0) continue;
            if (AposElites.ScrapEridium(q.Recipe.ingredients[0].itemValue) <= 0) continue;
            if (d == null) d = new Dictionary<Recipe, int>();
            int n;
            d.TryGetValue(q.Recipe, out n);
            d[q.Recipe] = n + q.Multiplier;
        }
        return d;
    }
}

// Vanilla bug: an arrow/bolt still flying when its shooter died reads the dead shooter's hand for the dismember
// chance and throws (ProjectileMoveScript.checkCollision). No dismember for such a hit instead.
[HarmonyPatch(typeof(ItemAction), "GetDismemberChance")]
public static class AposDismemberGuardPatch
{
    static Exception Finalizer(Exception __exception, ref float __result)
    {
        if (__exception is NullReferenceException) __result = 0f;
        return __exception is NullReferenceException ? null : __exception;
    }
}

// Intellect Mastery 5 "Inventor": a crafted item comes out one quality level above the quality picked in the recipe
// (6 stays 6 and becomes forge tier 7 instead), and crafted weapons roll affixes like loot of that quality.
// The picked quality is raised only while the game creates the item, so the crafting window keeps showing the pick.
public static class AposInventor
{
    // the queue of the workstation that is finishing items right now (AposWorkstationScrapPatch), with the picked qualities
    static RecipeQueueItem[] queue;
    static readonly Dictionary<RecipeQueueItem, byte> picked = new Dictionary<RecipeQueueItem, byte>();
    // the quality picked for the item the open crafting window is making now (AposInventorCraftPatch), -1 = none
    public static int WindowPick = -1;

    public static bool Has(EntityAlive crafter)
    {
        return crafter != null && AposAffixes.Level(crafter, "perkIntellectMastery") >= 5;
    }

    public static bool Raises(Recipe r)
    {
        if (r == null || r.IsScrap || r.itemValueType <= 0 || r.itemValueType >= ItemClass.list.Length) return false;
        ItemClass ic = ItemClass.list[r.itemValueType];
        return ic != null && ic.HasQuality;
    }

    public static int Raise(int quality)
    {
        return quality > 0 && quality < 6 ? quality + 1 : quality;
    }

    public static void Apply(ItemValue iv, EntityAlive crafter, int pick)
    {
        if (iv == null || iv.ItemClass == null || !iv.ItemClass.HasQuality || !Has(crafter)) return;
        // crafted weapons never get affixes in vanilla; the Inventor's do, rolled like loot of that quality
        if (AposAffixes.Kind(iv.ItemClass) != 0 && AposAffixes.Decompose(iv).Count == 0) AposAffixes.Roll(iv, false);
        if (pick >= 6 && AposTiers.Tier(iv) < 7 && AposTiers.HasTieredValues(iv.ItemClass)) AposTiers.SetTier(iv, 7);    }

    public static void BeginQueue(RecipeQueueItem[] q)
    {
        EndQueue();
        World world = GameManager.Instance.World;
        if (q == null || world == null) return;
        queue = q;
        for (int i = 0; i < q.Length; i++)
        {
            RecipeQueueItem item = q[i];
            if (item == null || item.Multiplier <= 0 || !Raises(item.Recipe)) continue;
            if (item.RepairItem != null && !item.RepairItem.IsEmpty()) continue;
            if (!Has(world.GetEntity(item.StartingEntityId) as EntityAlive)) continue;
            picked[item] = item.Quality;
            item.Quality = (byte)Raise(item.Quality);
        }
    }

    public static void EndQueue()
    {
        foreach (KeyValuePair<RecipeQueueItem, byte> kv in picked) kv.Key.Quality = kv.Value;
        picked.Clear();
        queue = null;
    }

    // the workstation puts the item its active (last) queue slot finished into the output
    public static void OnQueueOutput(ItemValue iv)
    {
        if (queue == null || queue.Length == 0 || GameManager.Instance.World == null) return;
        RecipeQueueItem active = queue[queue.Length - 1];
        byte pick;
        if (active == null || !picked.TryGetValue(active, out pick)) return;
        Apply(iv, GameManager.Instance.World.GetEntity(active.StartingEntityId) as EntityAlive, pick);
    }
}

// Crafting with the window open: outputStack creates the item at outputQuality, raised here for the Inventor and put back
// afterwards (the finalizer also runs when outputStack throws). Repairs and scrapping are left alone.
[HarmonyPatch(typeof(XUiC_RecipeStack), "outputStack")]
public static class AposInventorCraftPatch
{
    static void Prefix(XUiC_RecipeStack __instance, Recipe ___recipe, ItemValue ___originalItem, int ___startingEntityId, ref int ___outputQuality, out int __state)
    {
        __state = -1;
        AposInventor.WindowPick = -1;
        if (___originalItem != null && !___originalItem.IsEmpty()) return;
        if (!AposInventor.Raises(___recipe)) return;
        EntityPlayerLocal p = __instance.xui.playerUI.entityPlayer;
        if (p == null || p.entityId != ___startingEntityId || !AposInventor.Has(p)) return;
        __state = ___outputQuality;
        AposInventor.WindowPick = ___outputQuality;
        ___outputQuality = AposInventor.Raise(___outputQuality);
    }

    static void Finalizer(ref int ___outputQuality, int __state)
    {
        if (__state >= 0) ___outputQuality = __state;
        AposInventor.WindowPick = -1;
    }
}

// giveExp runs right after outputStack creates the item and before the item goes into the backpack, which stores a copy
// (ItemStackGrid.AddItem clones the ItemValue), so the affixes are added here. Only for the player who queued it.
[HarmonyPatch(typeof(XUiC_RecipeStack), "giveExp")]
public static class AposInventorExpPatch
{
    static void Prefix(XUiC_RecipeStack __instance, ItemValue _iv)
    {
        if (AposInventor.WindowPick < 0) return;
        AposInventor.Apply(_iv, __instance.xui.playerUI.entityPlayer, AposInventor.WindowPick);
    }
}

// A closed workstation puts finished items into its output with ItemStack.AddToItemStackArray
[HarmonyPatch(typeof(ItemStack), "AddToItemStackArray")]
public static class AposInventorStackPatch
{
    static void Prefix(ItemStack _itemStack)
    {
        if (_itemStack != null) AposInventor.OnQueueOutput(_itemStack.itemValue);
    }
}

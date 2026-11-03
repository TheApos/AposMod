// AposMod C# part. Built with Source\build.ps1 (C# 5). Settings: AposSettings.cs. Harmony patches: AposStealth.cs,
// AposAffixes.cs, AposCombat.cs, AposRewards.cs.
using System.Collections.Generic;
using UnityEngine;

public class AposModInit : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        AposSettings.Load(_modInstance.Path);
        AposStealth.Init();
        // Patch class by class: one patch broken by a game update must not switch off the others
        HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("apos.aposmod");
        int ok = 0;
        foreach (System.Type t in typeof(AposModInit).Assembly.GetTypes())
        {
            if (t.GetCustomAttributes(typeof(HarmonyLib.HarmonyPatch), false).Length == 0) continue;
            try
            {
                harmony.CreateClassProcessor(t).Patch();
                ok++;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AposMod] Harmony patch " + t.Name + " failed, that feature is off: " + e);
            }
        }
        Debug.Log("[AposMod] Harmony patches applied: " + ok);
        GameObject go = new GameObject("AposDamageNumbers");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AposDamageNumbers>();
        go.AddComponent<AposPin>();
        go.AddComponent<AposSplinter>();
        go.AddComponent<AposAmmo>();
        go.AddComponent<AposSurvival>();
        go.AddComponent<AposElites>();
        go.AddComponent<AposWardrobe>();
        go.AddComponent<AposBow>();
        go.AddComponent<AposShadow>();
        go.AddComponent<AposMastery>();
    }
}

// Floating damage numbers: the game's own 3D numbers (DamageText, normally a world option) in AposMod colours.
// - Direct hits go through EntityAlive.ProcessDamageResponseLocal -> DamageText.Create; we restyle that call:
//   white hit, yellow "N!" headshot, cyan buffed hit, element colour by damage type, red "Kill!" on a killing
//   blow, light red for damage you take. The vanilla 15 m limit is raised to Range (sniper rifles, bows) and far
//   numbers grow with distance so they stay readable.
// - Damage without a hit (AposMod element ticks, reactions, splash: ModifyStats Health) is found by watching the
//   health of nearby entities and shown with the same DamageText in the colour of the element on the target.
// - Numbers live AposSettings.Lifetime seconds (vanilla 1.5 s).
// Also draws the crossbow Mark ring.
public class AposDamageNumbers : MonoBehaviour
{
    public const float Range = 120f;
    public const float VanillaLifetime = 1.5f;
    const float VanillaScale = 0.22f;
    const float FullSizeDistance = 10f; // beyond this, numbers grow with distance

    static readonly Color NormalColor = Color.white;
    static readonly Color HeadshotColor = new Color(1f, 0.85f, 0.1f);
    static readonly Color BuffedColor = new Color(0.3f, 0.85f, 1f);
    static readonly Color KillColor = new Color(1f, 0.15f, 0.1f);
    static readonly Color HurtColor = new Color(1f, 0.45f, 0.45f);      // damage to players
    static readonly Color ReactionColor = new Color(1f, 0.24f, 0.95f);  // magenta
    static readonly Color AcidColor = new Color(0.55f, 1f, 0.18f);      // toxic green
    static readonly Color FireColor = new Color(1f, 0.55f, 0.1f);       // orange
    static readonly Color ShockColor = new Color(0.25f, 0.45f, 1f);     // deep blue (darker than the cyan of buffed hits)
    static readonly Color MarkColor = new Color(1f, 0.2f, 0.1f);        // crossbow Mark ring
    static readonly Color BleedColor = new Color(1f, 0.45f, 0.5f);      // pink

    static readonly string[] FireBuffs = { "buffBurningElement", "buffBurningFlamingArrow", "buffBurningMolotov", "buffBurningZombie" };

    // Player buffs that raise the damage of your hits, and target markers that do the same
    static readonly string[] PlayerDamageBuffs = { "buffAposHeadhunter", "buffAposRage", "buffAposCounter" };
    static readonly string[] TargetDamageBuffs = { "buffAposMark", "buffAposCorroded" };
    const string SuppressedFullVar = "aposSupFull";

    // The hit being processed by ProcessDamageResponseLocal (set by its prefix, main thread only)
    static EntityAlive hitTarget;
    static DamageResponse hitResponse;
    static bool ownCreate; // our own DamageText.Create call: already styled
    // entities whose killing blow was shown: hits on the corpse (harvesting) show nothing
    static readonly HashSet<int> killShown = new HashSet<int>();

    // Last seen values per entity id, to detect damage without a hit
    readonly Dictionary<int, int> lastHealth = new Dictionary<int, int>();
    readonly Dictionary<int, float> lastHitRandom = new Dictionary<int, float>();
    readonly HashSet<int> seen = new HashSet<int>();
    readonly List<int> stale = new List<int>();
    readonly List<EntityAlive> marked = new List<EntityAlive>();  // zombies with the crossbow Mark, drawn as a ring
    Texture2D ringTexture;
    Camera cam;
    static string killText;

    void Awake()
    {
        // Skip the IMGUI layout pass: we only draw the Mark rings
        useGUILayout = false;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        EntityPlayerLocal player = (gm != null && gm.World != null) ? gm.World.GetPrimaryPlayer() : null;
        if (player == null)
        {
            lastHealth.Clear();
            lastHitRandom.Clear();
            killShown.Clear();
            cam = null;
            killText = null; // re-read after a language change in the menu
            return;
        }
        cam = player.playerCamera;
        // our setting switches the numbers on even when the world option is off
        if (AposSettings.Enabled && !DamageText.Enabled) DamageText.Enabled = true;

        seen.Clear();
        marked.Clear();
        List<Entity> list = gm.World.Entities.list;
        for (int i = 0; i < list.Count; i++)
        {
            EntityAlive e = list[i] as EntityAlive;
            if (e == null || e is EntityPlayer) continue;
            if ((e.position - player.position).sqrMagnitude > Range * Range) continue;

            int id = e.entityId;
            seen.Add(id);
            if (!e.IsDead() && e.Buffs.HasBuff("buffAposMark")) marked.Add(e);
            int hp = e.Health;
            DamageResponse rec = e.RecordedDamage;

            int prevHp;
            float prevRandom;
            // a fresh hit record means the game already showed this damage
            if (lastHealth.TryGetValue(id, out prevHp) && hp < prevHp
                && lastHitRandom.TryGetValue(id, out prevRandom) && prevRandom == rec.Random && AposSettings.Enabled)
            {
                Vector3 pos = e.getHeadPosition() + new Vector3(0f, 0.1f, 0f);
                Show((prevHp - hp).ToString(), ElementColor(e) ?? NormalColor, pos, 0.85f, false);
                if (hp <= 0) ShowKill(e, pos);
            }
            lastHealth[id] = hp;
            lastHitRandom[id] = rec.Random;
        }

        // Forget entities that despawned or left the range
        stale.Clear();
        foreach (int id in lastHealth.Keys)
        {
            if (!seen.Contains(id)) stale.Add(id);
        }
        for (int i = 0; i < stale.Count; i++)
        {
            lastHealth.Remove(stale[i]);
            lastHitRandom.Remove(stale[i]);
            killShown.Remove(stale[i]);
        }
    }

    public static void BeginHit(EntityAlive target, DamageResponse response)
    {
        hitTarget = target;
        hitResponse = response;
    }

    public static void EndHit()
    {
        hitTarget = null;
    }

    // DamageText.Create prefix: true lets the call through (ours), false replaces the vanilla number with ours
    public static bool OnCreate(Vector3 worldPos)
    {
        if (ownCreate) return true;
        if (!AposSettings.Enabled || hitTarget == null) return false;
        EntityAlive e = hitTarget;
        DamageResponse r = hitResponse;
        if (killShown.Contains(e.entityId) || (!r.Fatal && e.IsDead())) return false; // a corpse
        if (e is EntityPlayer)
        {
            Show(r.Strength.ToString(), HurtColor, worldPos, 1f, false);
            return false;
        }

        bool headshot = (r.HitBodyPart & EnumBodyPartHit.Head) != 0;
        Color? element = TypeColor(r.Source != null ? r.Source.GetDamageType() : EnumDamageTypes.None);
        EntityPlayerLocal player = GameManager.Instance.World.GetPrimaryPlayer();
        bool buffed = AposSettings.BuffColor && player != null && (HasAnyBuff(player, PlayerDamageBuffs)
            || HasAnyBuff(e, TargetDamageBuffs) || e.Buffs.GetCustomVar(SuppressedFullVar) > 0f);
        // A buffed hit shows the buff colour; "!" still marks a headshot
        Color c = element ?? (buffed ? BuffedColor : (headshot ? HeadshotColor : NormalColor));
        Show(headshot ? r.Strength + "!" : r.Strength.ToString(), c, worldPos, headshot ? 1.3f : 1f, false);
        if (r.Fatal) ShowKill(e, worldPos);
        return false;
    }

    static void ShowKill(EntityAlive e, Vector3 pos)
    {
        if (!killShown.Add(e.entityId)) return;
        if (AposSettings.KillLabel) Show(KillText(), KillColor, pos + new Vector3(0f, 0.45f, 0f), 1.1f, true);
    }

    // Spawns a game damage number; size is relative to the vanilla one
    static void Show(string text, Color color, Vector3 worldPos, float size, bool straightUp)
    {
        EntityPlayerLocal player = GameManager.Instance.World.GetPrimaryPlayer();
        if (player == null || player.cameraTransform == null) return;
        // the same camera position the game uses for its 15 m check (logical world space)
        Vector3 camPos = player.cameraTransform.position + Origin.position;
        float grow = Mathf.Max(1f, Vector3.Distance(camPos, worldPos) / FullSizeDistance);
        Vector3 velocity = straightUp ? new Vector3(0f, 0.8f, 0f)
            : new Vector3(Random.Range(-0.7f, 0.7f), 0.8f, Random.Range(-0.7f, 0.7f));
        ownCreate = true;
        try
        {
            DamageText.Create(text, color, worldPos, velocity * grow, VanillaScale * size * AposSettings.Scale * grow);
        }
        finally
        {
            ownCreate = false;
        }
    }

    static bool HasAnyBuff(EntityAlive e, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (e.Buffs.HasBuff(names[i])) return true;
        }
        return false;
    }

    // Colour of a hit by its damage type; null = plain hit
    static Color? TypeColor(EnumDamageTypes type)
    {
        switch (type)
        {
            case EnumDamageTypes.Heat: return FireColor;
            case EnumDamageTypes.Electrical: return ShockColor;
            case EnumDamageTypes.Corrosive:
            case EnumDamageTypes.Toxic: return AcidColor;
            case EnumDamageTypes.BloodLoss: return BleedColor;
        }
        return null;
    }

    // Colour of damage that came without a hit (ticks, reactions), by what is on the target; null = plain
    static Color? ElementColor(EntityAlive e)
    {
        if (e.Buffs.HasBuff("buffAposReactionHit")) return ReactionColor;
        if (e.Buffs.HasBuff("buffAposCorroded")) return AcidColor;
        if (HasAnyBuff(e, FireBuffs)) return FireColor;
        if (e.Buffs.HasBuff("buffShocked")) return ShockColor;
        if (e.Buffs.HasBuff("buffInjuryBleeding") || e.Buffs.HasBuff("buffAposDeepBleed")) return BleedColor;
        return null;
    }

    static string KillText()
    {
        if (killText == null)
        {
            killText = Localization.Get("aposKillText");
            // Localization returns the key itself when it is missing
            if (string.IsNullOrEmpty(killText) || killText == "aposKillText") killText = "Kill!";
        }
        return killText;
    }

    void OnGUI()
    {
        if (marked.Count == 0 || cam == null || Event.current.type != EventType.Repaint) return;
        Color oldColor = GUI.color;
        DrawMarks();
        GUI.color = oldColor;
    }

    // Crossbow Mark: a red target ring above every marked zombie, visible through walls like a Borderlands mark
    void DrawMarks()
    {
        if (marked.Count == 0) return;
        if (ringTexture == null) ringTexture = MakeRing(64);
        const float size = 34f;
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 6f);
        GUI.color = new Color(MarkColor.r, MarkColor.g, MarkColor.b, pulse);
        for (int i = 0; i < marked.Count; i++)
        {
            EntityAlive e = marked[i];
            if (e == null) continue;
            Vector3 vp = cam.WorldToViewportPoint(e.transform.position + Vector3.up * (e.height + 0.5f));
            if (vp.z <= 0f) continue;
            float x = vp.x * Screen.width;
            float y = (1f - vp.y) * Screen.height;
            GUI.DrawTexture(new Rect(x - size / 2f, y - size / 2f, size, size), ringTexture);
        }
    }

    // White ring with four inner ticks (tinted by GUI.color when drawn)
    static Texture2D MakeRing(int n)
    {
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float c = (n - 1) / 2f, outer = n / 2f - 1f, inner = outer - 5f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                bool ring = d <= outer && d >= inner;
                bool tick = (Mathf.Abs(dx) < 2f && Mathf.Abs(dy) > inner - 9f && Mathf.Abs(dy) < inner)
                         || (Mathf.Abs(dy) < 2f && Mathf.Abs(dx) > inner - 9f && Mathf.Abs(dx) < inner);
                t.SetPixel(x, y, (ring || tick) ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        t.Apply();
        return t;
    }
}

// Hits: remember the target for DamageText.Create and raise the vanilla 15 m limit (225 = 15^2) to Range
[HarmonyLib.HarmonyPatch(typeof(EntityAlive), "ProcessDamageResponseLocal")]
public static class AposDamageNumbersHitPatch
{
    static void Prefix(EntityAlive __instance, DamageResponse _dmResponse)
    {
        AposDamageNumbers.BeginHit(__instance, _dmResponse);
    }

    static void Postfix()
    {
        AposDamageNumbers.EndHit();
    }

    static IEnumerable<HarmonyLib.CodeInstruction> Transpiler(IEnumerable<HarmonyLib.CodeInstruction> code)
    {
        foreach (HarmonyLib.CodeInstruction ins in code)
        {
            if (ins.opcode == System.Reflection.Emit.OpCodes.Ldc_R4 && ins.operand is float && (float)ins.operand == 225f)
            {
                ins.operand = AposDamageNumbers.Range * AposDamageNumbers.Range;
            }
            yield return ins;
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(DamageText), "Create")]
public static class AposDamageTextCreatePatch
{
    static bool Prefix(Vector3 _worldPos)
    {
        return AposDamageNumbers.OnCreate(_worldPos);
    }
}

// Lifetime: the game counts 1.5 s down; giving back part of each frame stretches it to AposSettings.Lifetime
[HarmonyLib.HarmonyPatch(typeof(DamageText), "LateUpdate")]
public static class AposDamageTextLifetimePatch
{
    static void Prefix(DamageText __instance)
    {
        __instance.TimeDuration += Time.deltaTime * (1f - AposDamageNumbers.VanillaLifetime / AposSettings.Lifetime);
    }
}

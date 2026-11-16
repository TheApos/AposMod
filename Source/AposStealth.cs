// Stealth rework (Harmony). Vanilla POI sleeper volumes of type Trigger (3) stay empty until a scripted
// trigger fires and then spawn awake attackers ("waves"); type Attack (2) wakes everyone the moment you
// step in. We turn both into Active (0): zombies spawn asleep in place as you approach and wake only from
// noise or sight, so sneaking matters again. Passive (1) and Wander (4) volumes keep their type.
// Any volume (of any type) can also be linked to a trigger (TriggeredByIndices): when the trigger fires,
// SleeperVolume.OnTriggered remembers the player and every zombie of that volume spawns awake and attacks
// him (WakeAttackLater). We drop those links and skip OnTriggered, so triggers only open doors / drop walls.
// Switch: <Stealth rework="false"/> in Settings.xml restores vanilla. The conversion is stored in the save (the
// volume's own flags), and a quest POI reset only resets the existing volume, so with the rework off every
// volume is set back from its prefab when the world attaches it to its POI and on every reset (Sync).
using System.Collections.Generic;
using HarmonyLib;
using PrefabVolumes;

public static class AposStealth
{
    const int TypeMask = 7;      // SleeperVolume.flags & 7 = ETriggerType
    const int TypeActive = 0;
    const int TypeAttack = 2;
    const int TypeTrigger = 3;

    static int converted, restored;

    // Vanilla spawns a volume's zombies when you are within 8 m of it on the same floor (the 0.7 m vertical
    // padding rounds down to 0), so they pop in at the door like a trigger. 12 m and +-3 m (next floor) puts them
    // in place, asleep, before you get there; it stays under the 14 m at which the game despawns them again.
    public static void Init()
    {
        if (!AposSettings.StealthRework) return;
        SleeperVolume.triggerPaddingMin = new Vector3i(12, 3, 12);
        SleeperVolume.triggerPaddingMax = new Vector3i(12, 3, 12);
    }

    public static void Fix(SleeperVolume v)
    {
        if (v == null || !AposSettings.StealthRework) return;
        int type = v.flags & TypeMask;
        bool linked = v.TriggeredByIndices != null && v.TriggeredByIndices.Count > 0;
        if (type != TypeAttack && type != TypeTrigger && !linked) return;

        if (type == TypeAttack || type == TypeTrigger) v.flags = (v.flags & ~TypeMask) | TypeActive;
        // a new list: Create shares the prefab's list, clearing it would change the prefab itself
        if (linked) v.TriggeredByIndices = new List<byte>(); // IsTrigger == false: no waiting for a trigger

        converted++;
        if (converted == 1 || converted % 50 == 0)
        {
            UnityEngine.Debug.Log("[AposMod] Stealth rework: converted " + converted + " trigger/attack sleeper volumes to sleeping spawns");
        }
    }

    // Brings a volume in line with the setting: rework on -> Fix; off -> the type and trigger links of its prefab
    // volume (undoes a conversion saved while the rework was on). Needs v.prefabInstance (set by AddToPrefabInstance).
    public static void Sync(SleeperVolume v)
    {
        if (v == null) return;
        if (AposSettings.StealthRework)
        {
            Fix(v);
            return;
        }
        PrefabSleeperVolume pv = PrefabVolume(v);
        if (pv == null) return;
        bool changed = false;
        int type = pv.flags & TypeMask;
        if ((v.flags & TypeMask) != type)
        {
            v.flags = (v.flags & ~TypeMask) | type;
            changed = true;
        }
        bool linked = v.TriggeredByIndices != null && v.TriggeredByIndices.Count > 0;
        if (!linked && pv.triggeredByIndices != null && pv.triggeredByIndices.Count > 0)
        {
            v.TriggeredByIndices = new List<byte>(pv.triggeredByIndices);
            changed = true;
        }
        if (!changed) return;
        restored++;
        if (restored == 1 || restored % 50 == 0)
        {
            UnityEngine.Debug.Log("[AposMod] Stealth rework off: restored " + restored + " sleeper volumes to vanilla");
        }
    }

    // The prefab volume this world volume was made from: same rule as DynamicPrefabDecorator.FindVolumeOwner and
    // World.ResetPOIS (POI position + volume start = world box start)
    static PrefabSleeperVolume PrefabVolume(SleeperVolume v)
    {
        PrefabInstance pi = v.prefabInstance;
        if (pi == null || pi.prefab == null || pi.prefab.SleeperVolumeList == null) return null;
        PrefabSleeperVolumeList list = pi.prefab.SleeperVolumeList;
        for (int i = 0; i < list.Count; i++)
        {
            PrefabSleeperVolume pv = list[i];
            if (pi.boundingBoxPosition + pv.startPos == v.BoxMin) return pv;
        }
        return null;
    }
}

// New volumes (POI placed in a fresh or newly generated area)
[HarmonyPatch(typeof(SleeperVolume), "Create")]
public static class AposStealthCreatePatch
{
    static void Postfix(SleeperVolume __result)
    {
        AposStealth.Fix(__result);
    }
}

// Volumes loaded from a save
[HarmonyPatch(typeof(SleeperVolume), "Read")]
public static class AposStealthReadPatch
{
    static void Postfix(SleeperVolume __result)
    {
        AposStealth.Fix(__result);
    }
}

// World load: every volume read from the save is attached to its POI here (World.SetupSleeperVolumes)
[HarmonyPatch(typeof(SleeperVolume), "AddToPrefabInstance")]
public static class AposStealthAttachPatch
{
    static void Postfix(SleeperVolume __instance)
    {
        AposStealth.Sync(__instance);
    }
}

// Quest POI reset (World.ResetPOIS) and other resets keep the volume object: re-check it
[HarmonyPatch(typeof(SleeperVolume), "DespawnAndReset")]
public static class AposStealthResetPatch
{
    static void Postfix(SleeperVolume __instance)
    {
        AposStealth.Sync(__instance);
    }
}

// Safety net for links registered before the fix (e.g. POI data built earlier): a fired trigger does not
// mark the volume's zombies as awake attackers
[HarmonyPatch(typeof(SleeperVolume), "OnTriggered")]
public static class AposStealthTriggeredPatch
{
    static bool Prefix()
    {
        return !AposSettings.StealthRework;
    }
}

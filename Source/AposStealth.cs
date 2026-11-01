// Stealth rework (Harmony). Vanilla POI sleeper volumes of type Trigger (3) stay empty until a scripted
// trigger fires and then spawn awake attackers ("waves"); type Attack (2) wakes everyone the moment you
// step in. We turn both into Active (0): zombies spawn asleep in place as you approach and wake only from
// noise or sight, so sneaking matters again. Passive (1) and Wander (4) volumes keep their type.
// Any volume (of any type) can also be linked to a trigger (TriggeredByIndices): when the trigger fires,
// SleeperVolume.OnTriggered remembers the player and every zombie of that volume spawns awake and attacks
// him (WakeAttackLater). We drop those links and skip OnTriggered, so triggers only open doors / drop walls.
// Switch: <Stealth rework="false"/> in Settings.xml restores vanilla (for newly loaded POIs).
using System.Collections.Generic;
using HarmonyLib;

public static class AposStealth
{
    const int TypeMask = 7;      // SleeperVolume.flags & 7 = ETriggerType
    const int TypeActive = 0;
    const int TypeAttack = 2;
    const int TypeTrigger = 3;

    static int converted;

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

// Stealth rework (Harmony). Vanilla POI sleeper volumes of type Trigger (3) stay empty until a scripted
// trigger fires and then spawn awake attackers ("waves"); type Attack (2) wakes everyone the moment you
// step in. We turn both into Active (0): zombies spawn asleep in place as you approach and wake only from
// noise or sight, so sneaking matters again. Passive (1) and Wander (4) volumes are left alone.
// Switch: <Stealth rework="false"/> in Settings.xml restores vanilla (for newly loaded POIs).
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
        if (type != TypeAttack && type != TypeTrigger) return;

        v.flags = (v.flags & ~TypeMask) | TypeActive;
        if (v.TriggeredByIndices != null) v.TriggeredByIndices.Clear(); // IsTrigger == false: no waiting for a trigger

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

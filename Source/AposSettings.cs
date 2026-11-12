// Damage number settings: Settings.xml in the mod folder + the "aposdmg" console command.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

public static class AposSettings
{
    public static bool Enabled = true;
    public static float Scale = 1f;       // text size multiplier
    public static float Lifetime = 3f;    // seconds a number stays on screen (vanilla 1.5)
    public static bool KillLabel = true;  // red "Kill!" above killing blows
    public static bool BuffColor = true;  // cyan numbers for buffed hits
    public static bool StealthRework = true; // POI trigger/attack volumes spawn sleeping zombies (AposStealth.cs)
    public static bool EliteTags = true;     // names above elite zombies (AposElites.cs)
    public static float EliteHealth = 2f;    // elite max health = normal x this (AposElites.cs, buffAposElite)
    public static float EliteWeaponDrop = 100f; // % chance an elite drops a weapon (Eridium always drops; AposElites.OnDeath)

    static string file;

    public static string ModPath;

    public static void Load(string modPath)
    {
        ModPath = modPath;
        file = Path.Combine(modPath, "Settings.xml");
        if (!File.Exists(file))
        {
            Save();
            return;
        }
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(file);
            XmlElement s = doc.DocumentElement["Stealth"];
            if (s != null) StealthRework = ReadBool(s, "rework", StealthRework);
            XmlElement el = doc.DocumentElement["Elites"];
            if (el != null)
            {
                EliteTags = ReadBool(el, "tags", EliteTags);
                EliteHealth = Clamp(ReadFloat(el, "health", EliteHealth), 1f, 50f);
                EliteWeaponDrop = Clamp(ReadFloat(el, "weaponDrop", EliteWeaponDrop), 0f, 100f);
            }
            XmlElement d = doc.DocumentElement["DamageNumbers"];
            if (d != null)
            {
                Enabled = ReadBool(d, "enabled", Enabled);
                Scale = Clamp(ReadFloat(d, "scale", Scale), 0.5f, 3f);
                Lifetime = Clamp(ReadFloat(d, "lifetime", Lifetime), 0.5f, 5f);
                KillLabel = ReadBool(d, "killLabel", KillLabel);
                BuffColor = ReadBool(d, "buffColor", BuffColor);
            }
            // rewrite with the same values: a file from an older version gains the new options
            Save();
        }
        catch (Exception e)
        {
            // keep the defaults, but tell the player why their settings were ignored
            UnityEngine.Debug.LogWarning("[AposMod] Could not read " + file + ": " + e.Message);
        }
    }

    public static void Save()
    {
        if (file == null) return;
        try
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("AposMod");
            doc.AppendChild(root);
            root.AppendChild(doc.CreateComment(" Damage numbers. Change here or in game with the console command: aposdmg "));
            XmlElement d = doc.CreateElement("DamageNumbers");
            d.SetAttribute("enabled", Enabled ? "true" : "false");
            d.SetAttribute("scale", Scale.ToString("0.##", CultureInfo.InvariantCulture));
            d.SetAttribute("lifetime", Lifetime.ToString("0.##", CultureInfo.InvariantCulture));
            d.SetAttribute("killLabel", KillLabel ? "true" : "false");
            d.SetAttribute("buffColor", BuffColor ? "true" : "false");
            root.AppendChild(d);
            root.AppendChild(doc.CreateComment(" Stealth rework: POI zombies spawn asleep instead of triggered waves. Needs a game restart. "));
            XmlElement s = doc.CreateElement("Stealth");
            s.SetAttribute("rework", StealthRework ? "true" : "false");
            root.AppendChild(s);
            root.AppendChild(doc.CreateComment(" Elites. tags: names above them (in game: aposelite on|off). health: their max health as a multiple of a normal zombie, 1-50, 2 = double (in game: aposelite health N). weaponDrop: % chance they drop a weapon, 0-100, 0 = never; Eridium always drops (in game: aposelite drop N) "));
            XmlElement el = doc.CreateElement("Elites");
            el.SetAttribute("tags", EliteTags ? "true" : "false");
            el.SetAttribute("health", EliteHealth.ToString("0.##", CultureInfo.InvariantCulture));
            el.SetAttribute("weaponDrop", EliteWeaponDrop.ToString("0.##", CultureInfo.InvariantCulture));
            root.AppendChild(el);
            doc.Save(file);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[AposMod] Could not save " + file + ": " + e.Message);
        }
    }

    public static string Describe()
    {
        return string.Format(CultureInfo.InvariantCulture,
            "Damage numbers: {0}, size {1:0.##}, time {2:0.##} s, kill label {3}, buff colour {4}",
            Enabled ? "on" : "off", Scale, Lifetime, KillLabel ? "on" : "off", BuffColor ? "on" : "off");
    }

    static bool ReadBool(XmlElement e, string name, bool fallback)
    {
        string v = e.GetAttribute(name);
        bool b;
        return bool.TryParse(v, out b) ? b : fallback;
    }

    static float ReadFloat(XmlElement e, string name, float fallback)
    {
        string v = e.GetAttribute(name);
        float f;
        return float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f) ? f : fallback;
    }

    // Folder for per-player data (Weapon Mastery, Wardrobe). Single player and the host: the world's save folder.
    // A client joining someone else's game has no such folder (the world lives on the host), so writing there
    // failed and progress was lost on rejoin; a client uses its local folder for that server instead.
    public static string PlayerDataDir()
    {
        GameManager gm = GameManager.Instance;
        bool client = gm != null && gm.World != null && gm.World.IsRemote();
        string dir = GameIO.GetSaveGameDir();
        if (client)
        {
            // throws while the server's game id is not known yet (very start of joining)
            try { dir = GameIO.GetSaveGameLocalDir(); }
            catch (Exception) { return null; }
        }
        if (dir != checkedDir)
        {
            // called every frame: touch the disk only when the folder changes (another world or server)
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            checkedDir = dir;
        }
        return dir;
    }

    static string checkedDir;

    public static float Clamp(float v, float min, float max)
    {
        return v < min ? min : (v > max ? max : v);
    }
}

// aposdmg                -> show settings
// aposdmg on|off         -> toggle numbers
// aposdmg size 1.3       -> text size (0.5..3)
// aposdmg time 2         -> seconds on screen (0.5..5)
// aposdmg kill on|off    -> "Kill!" label
// aposdmg color on|off   -> cyan colour for buffed hits
public class AposDamageNumbersCommand : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "aposdmg" };
    }

    public override string getDescription()
    {
        return "AposMod damage numbers: aposdmg [on|off | size N | time N | kill on|off | color on|off]";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        SdtdConsole console = SingletonMonoBehaviour<SdtdConsole>.Instance;
        if (_params.Count > 0)
        {
            string cmd = _params[0].ToLowerInvariant();
            string arg = _params.Count > 1 ? _params[1].ToLowerInvariant() : "";
            float f;
            bool numeric = float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
            if (cmd == "on") AposSettings.Enabled = true;
            else if (cmd == "off") AposSettings.Enabled = false;
            else if (cmd == "size" && numeric) AposSettings.Scale = AposSettings.Clamp(f, 0.5f, 3f);
            else if (cmd == "time" && numeric) AposSettings.Lifetime = AposSettings.Clamp(f, 0.5f, 5f);
            else if (cmd == "kill" && (arg == "on" || arg == "off")) AposSettings.KillLabel = arg == "on";
            else if (cmd == "color" && (arg == "on" || arg == "off")) AposSettings.BuffColor = arg == "on";
            else
            {
                console.Output(getDescription());
                return;
            }
            AposSettings.Save();
        }
        console.Output(AposSettings.Describe());
    }
}

// aposelite on|off                 -> names above elite zombies
// aposelite drop N                 -> weapon drop chance in % (0 = never)
// aposelite make                   -> nearest zombie within 20 m becomes an elite with 1 random modifier
// aposelite make 2|3               -> ... with 2 or 3 random modifiers
// aposelite make Armored Giant     -> ... with these modifiers
public class AposEliteCommand : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "aposelite" };
    }

    public override string getDescription()
    {
        return "AposMod elites: aposelite on|off | health N (max health x N, 1-50) | drop N (weapon drop chance %, 0-100) | make [1|2|3|modifier names]. Modifiers: " + string.Join(", ", AposElites.Modifiers);
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        SdtdConsole console = SingletonMonoBehaviour<SdtdConsole>.Instance;
        if (_params.Count > 0 && _params[0] == "make")
        {
            EntityAlive e = Nearest();
            if (e == null)
            {
                console.Output("No zombie within 20 m.");
                return;
            }
            int mask = 0, count = 1;
            for (int i = 1; i < _params.Count; i++)
            {
                int n;
                if (int.TryParse(_params[i], out n)) { count = Math.Max(1, Math.Min(3, n)); continue; }
                int m = Array.FindIndex(AposElites.Modifiers, x => string.Equals(x, _params[i], StringComparison.OrdinalIgnoreCase));
                if (m >= 0) mask |= 1 << m;
                else console.Output("Unknown modifier: " + _params[i]);
            }
            if (mask == 0) mask = AposElites.RandomMask(e, count);
            AposElites.Make(e, mask);
            console.Output("Elite: " + e.EntityName);
            return;
        }
        if (_params.Count > 0 && _params[0] == "health")
        {
            float h;
            if (_params.Count < 2 || !float.TryParse(_params[1], NumberStyles.Float, CultureInfo.InvariantCulture, out h))
            {
                console.Output("Usage: aposelite health N  (1-50, 2 = double health)");
                return;
            }
            AposSettings.EliteHealth = AposSettings.Clamp(h, 1f, 50f);
            AposSettings.Save();
        }
        if (_params.Count > 0 && _params[0] == "drop")
        {
            float d;
            if (_params.Count < 2 || !float.TryParse(_params[1], NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                console.Output("Usage: aposelite drop N  (weapon drop chance in %, 0-100, 0 = never)");
                return;
            }
            AposSettings.EliteWeaponDrop = AposSettings.Clamp(d, 0f, 100f);
            AposSettings.Save();
        }
        if (_params.Count > 0 && (_params[0] == "on" || _params[0] == "off"))
        {
            AposSettings.EliteTags = _params[0] == "on";
            AposSettings.Save();
        }
        console.Output("Elite names: " + (AposSettings.EliteTags ? "on" : "off") + ", elite health: x"
            + AposSettings.EliteHealth.ToString("0.##", CultureInfo.InvariantCulture) + ", weapon drop: "
            + AposSettings.EliteWeaponDrop.ToString("0.##", CultureInfo.InvariantCulture) + "%");
    }

    static EntityAlive Nearest()
    {
        EntityPlayerLocal p = AposRewards.Player();
        if (p == null) return null;
        EntityAlive best = null;
        float bestDist = 400f; // 20 m squared
        foreach (Entity ent in GameManager.Instance.World.Entities.list)
        {
            EntityAlive e = ent as EntityAlive;
            if (e == null || e is EntityPlayer || e.IsDead() || AposElites.IsElite(e)) continue;
            float d = (e.position - p.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }
}
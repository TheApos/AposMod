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
    public static bool EliteAnnounce = true; // "elite nearby" banner with tips (AposElites.Announce)
    // elite max health and damage = normal x this, by stars (AposElites.cs, buffAposElite)
    public static readonly float[] EliteHealth = { 3f, 6f, 12f };
    public static readonly float[] EliteDamage = { 2f, 4f, 6f };
    public static float EliteWeaponDrop = 100f; // % chance an elite drops a weapon (Eridium always drops; AposElites.OnDeath)
    // per-biome weapon drop chance (%), same order as DropBiomes; other biomes use EliteWeaponDrop.
    // Default 10/20/30/40/50: harder biomes (tougher elites) drop more often.
    public static readonly string[] DropBiomes = { "pine_forest", "burnt_forest", "desert", "snow", "wasteland" };
    static readonly string[] DropAttrs = { "dropPineForest", "dropBurntForest", "dropDesert", "dropSnow", "dropWasteland" };
    public static readonly float[] EliteBiomeDrop = { 10f, 20f, 30f, 40f, 50f };
    public static string EliteWeaponExclude = "";
    public static bool LootBagBeam = false; // loot beam above vanilla zombie loot bags (AposElites.UpdateGlows) // item names / name starts elites never drop (AposElites.WeaponPool)

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
                EliteAnnounce = ReadBool(el, "announce", EliteAnnounce);
                // "health" (one value for all stars) from 1.6.16 and older is ignored: the stars have their own now
                for (int i = 0; i < 3; i++)
                {
                    EliteHealth[i] = Clamp(ReadFloat(el, "health" + (i + 1), EliteHealth[i]), 1f, 100f);
                    EliteDamage[i] = Clamp(ReadFloat(el, "damage" + (i + 1), EliteDamage[i]), 1f, 100f);
                }
                EliteWeaponDrop = Clamp(ReadFloat(el, "weaponDrop", EliteWeaponDrop), 0f, 100f);
                // a file from before 1.6.12 has no biome values: they get the defaults, or weaponDrop if the player
                // changed it (e.g. weaponDrop="0" to turn weapon drops off stays off)
                EliteWeaponExclude = el.GetAttribute("weaponExclude");
                bool customDrop = EliteWeaponDrop != 100f;
                for (int i = 0; i < DropAttrs.Length; i++)
                {
                    float fallback = customDrop ? EliteWeaponDrop : EliteBiomeDrop[i];
                    EliteBiomeDrop[i] = Clamp(ReadFloat(el, DropAttrs[i], fallback), 0f, 100f);
                }
            }
            XmlElement lb = doc.DocumentElement["LootBeam"];
            if (lb != null) LootBagBeam = ReadBool(lb, "bags", LootBagBeam);
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
            root.AppendChild(doc.CreateComment(" Elites. tags: names above them (in game: aposelite on|off). announce: a banner with tips when an elite first comes into view (in game: aposelite announce on|off). health1/2/3, damage1/2/3: max health and damage of 1, 2 and 3 star elites as a multiple of a normal zombie, 1-100 (in game: aposelite health 3 6 12, aposelite damage 2 4 6). weaponDrop: % chance they drop a weapon, 0-100, 0 = never; Eridium always drops. drop<Biome>: the same chance for elites killed in that biome; weaponDrop is used elsewhere (in game: aposelite drop N for all, aposelite drop wasteland N for one). weaponExclude: comma-separated item names or name starts elites never drop, e.g. 'gunExplosives,meleeToolAxe' (in game: aposelite weapons lists what they can drop) "));
            XmlElement el = doc.CreateElement("Elites");
            el.SetAttribute("tags", EliteTags ? "true" : "false");
            el.SetAttribute("announce", EliteAnnounce ? "true" : "false");
            for (int i = 0; i < 3; i++)
            {
                el.SetAttribute("health" + (i + 1), EliteHealth[i].ToString("0.##", CultureInfo.InvariantCulture));
                el.SetAttribute("damage" + (i + 1), EliteDamage[i].ToString("0.##", CultureInfo.InvariantCulture));
            }
            el.SetAttribute("weaponDrop", EliteWeaponDrop.ToString("0.##", CultureInfo.InvariantCulture));
            el.SetAttribute("weaponExclude", EliteWeaponExclude ?? "");
            for (int i = 0; i < DropAttrs.Length; i++)
            {
                el.SetAttribute(DropAttrs[i], EliteBiomeDrop[i].ToString("0.##", CultureInfo.InvariantCulture));
            }
            root.AppendChild(el);
            root.AppendChild(doc.CreateComment(" Loot beam (like the one above Eridium) above vanilla zombie loot bags. Many beams on blood moons (in game: aposbags on|off) "));
            XmlElement lb = doc.CreateElement("LootBeam");
            lb.SetAttribute("bags", LootBagBeam ? "true" : "false");
            root.AppendChild(lb);
            doc.Save(file);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[AposMod] Could not save " + file + ": " + e.Message);
        }
    }

    // weapon drop chance (%) for an elite killed in this biome (biome name from biomes.xml, may be null)
    public static float EliteDropChance(string biome)
    {
        int i = biome == null ? -1 : Array.IndexOf(DropBiomes, biome);
        return i >= 0 ? EliteBiomeDrop[i] : EliteWeaponDrop;
    }

    // "pine", "burnt", "wasteland"... -> index in DropBiomes, -1 if unknown
    public static int FindDropBiome(string name)
    {
        for (int i = 0; i < DropBiomes.Length; i++)
        {
            if (DropBiomes[i].StartsWith(name, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }

    public static string DescribeDrop()
    {
        string s = "weapon drop: " + EliteWeaponDrop.ToString("0.##", CultureInfo.InvariantCulture) + "% (other biomes)";
        for (int i = 0; i < DropBiomes.Length; i++)
        {
            s += ", " + DropBiomes[i] + " " + EliteBiomeDrop[i].ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }
        return s;
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

// aposbags on|off -> loot beam above vanilla zombie loot bags
public class AposLootBagCommand : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "aposbags" };
    }

    public override string getDescription()
    {
        return "AposMod: aposbags on|off - loot beam above zombie loot bags";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        SdtdConsole console = SingletonMonoBehaviour<SdtdConsole>.Instance;
        if (_params.Count > 0 && (_params[0] == "on" || _params[0] == "off"))
        {
            AposSettings.LootBagBeam = _params[0] == "on";
            AposSettings.Save();
        }
        console.Output("Loot bag beams: " + (AposSettings.LootBagBeam ? "on" : "off"));
    }
}

// aposelite on|off                 -> names above elite zombies
// aposelite drop N                 -> weapon drop chance in % (0 = never), all biomes
// aposelite drop wasteland N       -> ... in one biome (pine, burnt, desert, snow, wasteland)
// aposelite weapons                -> list the weapons elites can drop (Settings.xml weaponExclude filters it)
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
        return "AposMod elites: aposelite on|off | announce on|off | health A B C / damage A B C (x for 1/2/3 stars) | drop [biome] N (weapon drop chance %, 0-100; biomes: pine, burnt, desert, snow, wasteland) | weapons [search] (what elites can drop, one per line) | exclude <name> / include <name> (weaponExclude) | make [1|2|3|modifier names]. Modifiers: " + string.Join(", ", AposElites.Modifiers);
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
        if (_params.Count > 0 && _params[0] == "weapons")
        {
            ListWeapons(console, _params.Count > 1 ? string.Join(" ", _params.GetRange(1, _params.Count - 1).ToArray()) : null);
            return;
        }
        if (_params.Count > 0 && (_params[0] == "exclude" || _params[0] == "include"))
        {
            if (_params.Count != 2)
            {
                console.Output("Usage: aposelite " + _params[0] + " <item name or name start>, e.g. aposelite " + _params[0] + " gunExplosives"
                    + "  (names: aposelite weapons [search])");
                return;
            }
            ChangeExclude(console, _params[1], _params[0] == "exclude");
            return;
        }
        if (_params.Count > 0 && (_params[0] == "health" || _params[0] == "damage"))
        {
            float[] target = _params[0] == "health" ? AposSettings.EliteHealth : AposSettings.EliteDamage;
            float[] v = new float[3];
            bool ok = _params.Count == 4;
            for (int i = 0; ok && i < 3; i++) ok = float.TryParse(_params[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]);
            if (!ok)
            {
                console.Output("Usage: aposelite " + _params[0] + " A B C  (x for 1, 2 and 3 star elites, 1-100; default "
                    + (_params[0] == "health" ? "3 6 12" : "2 4 6") + ")");
                return;
            }
            for (int i = 0; i < 3; i++) target[i] = AposSettings.Clamp(v[i], 1f, 100f);
            AposSettings.Save();
        }
        if (_params.Count > 0 && _params[0] == "drop")
        {
            float d;
            int biome = _params.Count > 2 ? AposSettings.FindDropBiome(_params[1]) : -1;
            bool ok = _params.Count == 2 || (_params.Count == 3 && biome >= 0);
            if (!ok || !float.TryParse(_params[_params.Count - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                console.Output("Usage: aposelite drop N  or  aposelite drop <pine|burnt|desert|snow|wasteland> N  (weapon drop chance in %, 0-100, 0 = never)");
                return;
            }
            d = AposSettings.Clamp(d, 0f, 100f);
            if (biome >= 0) AposSettings.EliteBiomeDrop[biome] = d;
            else
            {
                AposSettings.EliteWeaponDrop = d;
                for (int i = 0; i < AposSettings.EliteBiomeDrop.Length; i++) AposSettings.EliteBiomeDrop[i] = d;
            }
            AposSettings.Save();
        }
        if (_params.Count == 2 && _params[0] == "announce" && (_params[1] == "on" || _params[1] == "off"))
        {
            AposSettings.EliteAnnounce = _params[1] == "on";
            AposSettings.Save();
        }
        if (_params.Count > 0 && (_params[0] == "on" || _params[0] == "off"))
        {
            AposSettings.EliteTags = _params[0] == "on";
            AposSettings.Save();
        }
        console.Output("Elite names: " + (AposSettings.EliteTags ? "on" : "off") + ", banner: " + (AposSettings.EliteAnnounce ? "on" : "off") + ", health by stars: x" + Stars(AposSettings.EliteHealth)
            + ", damage by stars: x" + Stars(AposSettings.EliteDamage) + ", " + AposSettings.DescribeDrop());
    }

    // One weapon per line ("  Pistol  (gunHandgunT1Pistol)"), sorted by the in-game name; an optional search matches
    // the in-game or the internal name. Excluded weapons are listed after the pool.
    static void ListWeapons(SdtdConsole console, string search)
    {
        List<string> all = AposElites.AllWeapons();
        List<KeyValuePair<string, string>> pool = new List<KeyValuePair<string, string>>();
        List<KeyValuePair<string, string>> excluded = new List<KeyValuePair<string, string>>();
        foreach (string n in all)
        {
            string shown = Localization.Get(n);
            if (search != null && n.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0
                && shown.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            (AposElites.IsExcluded(n) ? excluded : pool).Add(new KeyValuePair<string, string>(shown, n));
        }
        pool.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.CurrentCultureIgnoreCase));
        excluded.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.CurrentCultureIgnoreCase));
        int total = AposElites.WeaponPool().Count;
        console.Output(search == null
            ? "Elites can drop " + total + " weapons:"
            : "Elites can drop " + pool.Count + " of " + total + " weapons matching \"" + search + "\":");
        foreach (KeyValuePair<string, string> w in pool) console.Output("  " + w.Key + "  (" + w.Value + ")");
        if (excluded.Count > 0)
        {
            console.Output("Excluded by weaponExclude (" + excluded.Count + "):");
            foreach (KeyValuePair<string, string> w in excluded) console.Output("  " + w.Key + "  (" + w.Value + ")");
        }
        console.Output("weaponExclude=\"" + AposSettings.EliteWeaponExclude + "\". Search: aposelite weapons <text>. Remove from the pool: aposelite exclude <name or name start>; bring back: aposelite include <name>.");
    }

    // aposelite exclude gunExplosives / include gunExplosives: edits weaponExclude and saves Settings.xml
    static void ChangeExclude(SdtdConsole console, string entry, bool add)
    {
        List<string> list = AposElites.ExcludeList();
        int at = list.FindIndex(x => string.Equals(x, entry, StringComparison.OrdinalIgnoreCase));
        if (add)
        {
            int hits = AposElites.AllWeapons().FindAll(n => n.StartsWith(entry, StringComparison.OrdinalIgnoreCase)).Count;
            if (hits == 0)
            {
                console.Output("No weapon name starts with \"" + entry + "\". See the internal names in brackets: aposelite weapons [search]");
                return;
            }
            if (at < 0) list.Add(entry);
            console.Output("Excluded \"" + entry + "\" (" + hits + " weapon" + (hits == 1 ? "" : "s") + ").");
        }
        else
        {
            if (at < 0)
            {
                console.Output("\"" + entry + "\" is not in weaponExclude=\"" + AposSettings.EliteWeaponExclude + "\". Use one of its entries.");
                return;
            }
            list.RemoveAt(at);
            if (AposElites.IsExcludedBy(entry, list)) console.Output("Removed \"" + entry + "\", but another entry still excludes it.");
            else console.Output("\"" + entry + "\" can drop again.");
        }
        AposSettings.EliteWeaponExclude = string.Join(",", list.ToArray());
        AposSettings.Save();
        console.Output("Elites can drop " + AposElites.WeaponPool().Count + " weapons. weaponExclude=\"" + AposSettings.EliteWeaponExclude + "\"");
    }

    static string Stars(float[] v)
    {
        return v[0].ToString("0.##", CultureInfo.InvariantCulture) + " / " + v[1].ToString("0.##", CultureInfo.InvariantCulture)
            + " / " + v[2].ToString("0.##", CultureInfo.InvariantCulture);
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

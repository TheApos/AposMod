# AposMod — project notes for Claude

7 Days to Die mod: Borderlands-style traits for each weapon class (shotgun slugs knock back, grenades x3 radius, melee normal/power effects, pipe-weapon "veteran" bonus, early rocket launcher, floating damage numbers).

- Game version: **V3.4.0 (b6)**. Always verify against the installed game files in `../../Data/Config` (items.xml, buffs.xml, loot.xml, entityclasses.xml, Localization.csv) — do not guess XML names or engine behavior.
- The owner speaks Ukrainian (answer in Ukrainian) and is learning modding — explain briefly.
- Ideas and implementation status: `docs/WeaponIdeas.md`. In-game test steps: `docs/TestChecklist.md`. Weapon list: `docs/Weapons.md`. Balance review: `docs/BalanceAnalysis.md`.
- Player-facing reference page: `docs/Reference.html`, published as a private artifact at https://claude.ai/artifact/C9rHeAvrTK86bwcwhjAHvm — keep it in sync with gameplay changes and republish (same file path or that URL).

## Layout
- `ModInfo.xml` — mod name/version (bump on releases).
- `Config/*.xml` — XPath patches (`set`, `append`, `insertBefore`, `remove`, `csv`) over vanilla configs. `progression.xml` replaces Grand Slam with Rage, Intellect Mastery 4-5 with Field Repair / Inventor, and removes vanilla knockdowns of Pummel Pete / Skull Crusher / The Brawler that duplicated mod traits (replacements in entityclasses.xml).
- `Config/Localization.csv` — must be **.csv** (game 3.x ignores Localization.txt). Columns `english` and `russian`; **fill both** (the game client runs in Russian; an empty column shows a blank text).
- `Source/*.cs` — C# part. Build: `& Source/build.ps1` → `AposMod.dll` (gitignored).
- `Source/gen_loc.ps1` — regenerates `Config/Localization.csv` (vanilla text + trait, EN/RU). Edit trait texts there, not in the CSV. Keep it UTF-8 **with BOM**.
- Damage numbers: draw at `entity.transform.position` (scene space). `Entity.position` is logical world space; the game uses a floating origin, so it is off by hundreds of meters on screen.
- Perk scaling: one triggered/passive effect per perk level with `ProgressionLevel ... Equals N` (Ragdoll force/range cannot read `@cvar`; CVarCompare, ModifyStats, RandomRoll, AddBuff duration and passive values can).

## Build
- Uses the Windows .NET Framework compiler `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` → **C# 5 only** (no `$""`, `?.`, `=>` members).
- References the game's `7DaysToDie_Data/Managed` DLLs via a path relative to the repo, so the repo must live in `<game>/Mods/AposMod`.
- A running game locks `AposMod.dll`; close the game before building.
- Run scripts with `& script.ps1`; `-ExecutionPolicy Bypass` is blocked.
- Runtime reflection on Assembly-CSharp fails under .NET Framework. To read game code use **Mono.Cecil** from `Mods/0_TFP_Harmony/Mono.Cecil.dll` in PowerShell (`Add-Type`, `AssemblyDefinition.ReadAssembly`): fields, method signatures and IL. To confirm an API, test-compile a small probe `.cs` against the Managed DLLs.
- C# files: `AposMod.cs` (init + damage numbers), `AposSettings.cs` (Settings.xml + `aposdmg` console command), `AposPin.cs` (crossbow pin raycast), `AposStealth.cs` (Harmony: SleeperVolume Create/Read turn Trigger/Attack volumes into Active). `Source/gen_legend.ps1` regenerates the legendary weapons block in items.xml.

## Gotchas found the hard way
- A lootgroup must be **defined before it is referenced**; otherwise loot.xml fails to load and world generation breaks (player falls through the ground).
- A mod with a DLL is skipped entirely (XML too) when the game runs with EasyAntiCheat. Launch `7DaysToDie.exe -noeac`. The log shows `Loaded Mod: AposMod` when it worked.
- Ammo item effect groups fire together with the weapon's events (ItemValue.FireEvent forwards to the loaded ammo) — that is how the slug-only knockback works.
- Harmony (HarmonyX) lives in `Mods/0_TFP_Harmony/0Harmony.dll` (restored by a full Steam "verify files"; it was missing before). Reference it from there when writing Harmony patches.
- Ranged AOE: `target="positionAOE"` hits around the impact/hit point (ammo `onProjectileImpact`, player `onSelfPrimaryActionRayHit`); arrows/bolts cannot pierce via XML (projectiles ignore EntityPenetrationCount).
- Game logs: `%APPDATA%/7DaysToDie/logs/output_log_client__*.txt`.

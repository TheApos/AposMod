# Builds ..\AposMod.dll with the C# 5 compiler that ships with Windows (.NET Framework 4).
# Run from anywhere: powershell -File build.ps1
$ErrorActionPreference = 'Stop'
$managed = Join-Path $PSScriptRoot '..\..\..\7DaysToDie_Data\Managed'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$refs = 'mscorlib', 'netstandard', 'System', 'System.Core', 'System.Xml', 'Assembly-CSharp', 'UnityEngine', 'UnityEngine.CoreModule', 'UnityEngine.PhysicsModule', 'UnityEngine.IMGUIModule', 'UnityEngine.TextRenderingModule', 'MemoryPack', 'UnityEngine.InputLegacyModule' |
  ForEach-Object { "/r:$(Join-Path $managed "$_.dll")" }
# Harmony ships with the game in Mods\0_TFP_Harmony (loaded before AposMod)
$refs += "/r:$(Join-Path $PSScriptRoot '..\..\0_TFP_Harmony\0Harmony.dll')"
& $csc /nologo /nostdlib /noconfig /optimize /target:library "/out:$(Join-Path $PSScriptRoot '..\AposMod.dll')" @refs (Join-Path $PSScriptRoot '*.cs')
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }
'Built AposMod.dll'

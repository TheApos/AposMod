param([string]$Game = (Join-Path $PSScriptRoot '..\..\..'))
# Regenerates the legendary weapons block in Config\items.xml (between the LEGENDARY markers).
# Each legendary is a full copy of a vanilla weapon: in 7D2D "Extends" does not carry effect_groups
# (vanilla extended weapons repeat all of them), so we clone the whole item and add one unique effect.
# Run: & Source\gen_legend.ps1   (then Source\gen_loc.ps1 for the texts)
$ErrorActionPreference = 'Stop'
[xml]$vanilla = Get-Content (Join-Path $Game 'Data\Config\items.xml') -Raw -Encoding UTF8
$target = Join-Path $PSScriptRoot '..\Config\items.xml'

# name -> base weapon and the unique effect group (XML fragment)
$legends = [ordered]@{
  aposLegendHarold = @{ Base = 'gunHandgunT3DesertVulture'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<passive_effect name="RoundRayCount" operation="base_set" value="3"/>
	<passive_effect name="SpreadDegreesHorizontal" operation="perc_add" value="1.5"/>
</effect_group>
'@ }
  aposLegendLadyFist = @{ Base = 'gunHandgunT1Pistol'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<passive_effect name="EntityDamage" operation="perc_add" value=".5"/>
	<triggered_effect trigger="onSelfPrimaryActionRayHit" action="ModifyStats" stat="Health" operation="add" value="5">
		<requirement name="HitLocation" body_parts="Head"/>
		<requirement name="IsAlive" target="other"/>
	</triggered_effect>
</effect_group>
'@ }
  aposLegendDragonBreath = @{ Base = 'gunShotgunT2PumpShotgun'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<passive_effect name="EntityDamage" operation="perc_add" value=".2"/>
	<triggered_effect trigger="onSelfAttackedOther" action="AddBuff" target="other" buff="buffBurningElement">
		<requirement name="IsAlive" target="other"/>
		<requirement name="CVarCompare" target="other" cvar="_underwater" operation="LT" value="0.3"/>
	</triggered_effect>
</effect_group>
'@ }
  aposLegendRipper = @{ Base = 'meleeWpnBladeT3Machete'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<passive_effect name="EntityDamage" operation="perc_add" value=".4"/>
	<!-- +50% more against a target that is already bleeding -->
	<passive_effect name="EntityDamage" operation="perc_add" value=".5">
		<requirement name="HasBuff" target="other" buff="buffInjuryBleeding"/>
	</passive_effect>
	<triggered_effect trigger="onSelfAttackedOther" action="AddBuff" target="other" buff="buffInjuryBleeding">
		<requirement name="IsAlive" target="other"/>
		<requirement name="!EntityTagCompare" target="other" tags="trader"/>
	</triggered_effect>
	<triggered_effect trigger="onSelfKilledOther" action="ModifyStats" stat="Health" operation="add" value="20"/>
</effect_group>
'@ }
  aposLegendThunderclap = @{ Base = 'meleeWpnSledgeT3SteelSledgehammer'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<passive_effect name="EntityDamage" operation="perc_add" value=".25"/>
	<triggered_effect trigger="onSelfSecondaryActionRayHit" action="AddBuff" target="otherAOE" range="4" buff="buffShocked" duration="2" target_tags="zombie,animal"/>
	<!-- Storm charges: every kill +1 (max 3); at 3 the next power hit calls lightning: 150 to all within 6 m + 3 s shock -->
	<triggered_effect trigger="onSelfKilledOther" action="ModifyCVar" cvar="aposThunder" operation="add" value="1">
		<requirement name="CVarCompare" cvar="aposThunder" operation="LT" value="3"/>
	</triggered_effect>
	<triggered_effect trigger="onSelfKilledOther" action="AddBuff" buff="buffAposThunderCharge"/>
	<triggered_effect trigger="onSelfSecondaryActionRayHit" action="ModifyCVar" target="otherAOE" range="6" target_tags="zombie,animal" cvar="aposSplashDmg" operation="set" value="-150">
		<requirement name="CVarCompare" cvar="aposThunder" operation="GTE" value="3"/>
	</triggered_effect>
	<triggered_effect trigger="onSelfSecondaryActionRayHit" action="AddBuff" target="otherAOE" range="6" target_tags="zombie,animal" buff="buffAposSplash">
		<requirement name="CVarCompare" cvar="aposThunder" operation="GTE" value="3"/>
	</triggered_effect>
	<triggered_effect trigger="onSelfSecondaryActionRayHit" action="AddBuff" target="otherAOE" range="6" target_tags="zombie,animal" buff="buffShocked" duration="3">
		<requirement name="CVarCompare" cvar="aposThunder" operation="GTE" value="3"/>
	</triggered_effect>
	<triggered_effect trigger="onSelfSecondaryActionRayHit" action="ModifyCVar" cvar="aposThunder" operation="set" value="0">
		<requirement name="CVarCompare" cvar="aposThunder" operation="GTE" value="3"/>
	</triggered_effect>
</effect_group>
'@ }
  aposLegendHornet = @{ Base = 'gunHandgunT3SMG5'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<passive_effect name="EntityDamage" operation="perc_add" value=".15"/>
	<triggered_effect trigger="onSelfAttackedOther" action="ModifyCVar" target="other" cvar="aposCorrodeTime" operation="set" value="6">
		<requirement name="IsAlive" target="other"/>
		<requirement name="RandomRoll" seed_type="Random" min_max="0,100" operation="LTE" value="30"/>
	</triggered_effect>
	<triggered_effect trigger="onSelfAttackedOther" action="AddBuff" target="other" buff="buffAposCorroded">
		<requirement name="IsAlive" target="other"/>
		<requirement name="CVarCompare" target="other" cvar="aposCorrodeTime" operation="GT" value="0"/>
	</triggered_effect>
</effect_group>
'@ }
  aposLegendStormFist = @{ Base = 'meleeWpnKnucklesT3SteelKnuckles'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<passive_effect name="EntityDamage" operation="perc_add" value=".2"/>
	<triggered_effect trigger="onSelfAttackedOther" action="AddBuff" target="other" buff="buffBurningElement">
		<requirement name="IsAlive" target="other"/>
		<requirement name="CVarCompare" target="other" cvar="_underwater" operation="LT" value="0.3"/>
		<requirement name="RandomRoll" seed_type="Random" min_max="0,100" operation="LTE" value="25"/>
	</triggered_effect>
	<triggered_effect trigger="onSelfAttackedOther" action="AddBuff" target="other" buff="buffShocked" duration="1.5">
		<requirement name="IsAlive" target="other"/>
		<requirement name="RandomRoll" seed_type="Random" min_max="0,100" operation="LTE" value="25"/>
	</triggered_effect>
</effect_group>
'@ }
  aposLegendArtemis = @{ Base = 'gunBowT3CompoundBow'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<!-- every arrow: 8 fragments of 50 (as Archery 5) and fire -->
	<triggered_effect trigger="onSelfDamagedOther" action="ModifyCVar" target="other" cvar="aposSplinterN" operation="set" value="8"/>
	<triggered_effect trigger="onSelfDamagedOther" action="ModifyCVar" target="other" cvar="aposSplinterDmg" operation="set" value="-50"/>
	<triggered_effect trigger="onSelfDamagedOther" action="AddBuff" target="other" buff="buffBurningFlamingArrow">
		<requirement name="CVarCompare" target="other" cvar="_underwater" operation="LT" value="0.3"/>
	</triggered_effect>
</effect_group>
'@ }
  aposLegendBigBoom = @{ Base = 'gunExplosivesT3RocketLauncher'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<!-- 50% chance that the next rocket costs nothing (the free shot ends the buff, then rolls again) -->
	<triggered_effect trigger="onSelfRangedBurstShotEnd" action="RemoveBuff" buff="buffAposFreeRocket"/>
	<triggered_effect trigger="onSelfRangedBurstShotEnd" action="AddBuff" buff="buffAposFreeRocket">
		<requirement name="RandomRoll" seed_type="Random" min_max="0,100" operation="LTE" value="50"/>
	</triggered_effect>
</effect_group>
'@ }
  aposLegendPredator = @{ Base = 'gunRifleT3SniperRifle'; Effect = @'
<effect_group name="AposMod legendary" tiered="false">
	<passive_effect name="EntityDamage" operation="perc_add" value=".25"/>
	<!-- every headshot ricochets: 100 to all within 15 m of the hit, 2 s stun -->
	<triggered_effect trigger="onSelfPrimaryActionRayHit" action="ModifyCVar" target="positionAOE" range="15" target_tags="zombie,animal" cvar="aposSplashDmg" operation="set" value="-100">
		<requirement name="HitLocation" body_parts="Head"/>
	</triggered_effect>
	<triggered_effect trigger="onSelfPrimaryActionRayHit" action="AddBuff" target="positionAOE" range="15" target_tags="zombie,animal" buff="buffAposSplash">
		<requirement name="HitLocation" body_parts="Head"/>
	</triggered_effect>
	<triggered_effect trigger="onSelfPrimaryActionRayHit" action="AddBuff" target="positionAOE" range="15" target_tags="zombie,animal" buff="buffInjuryStunned01" duration="2">
		<requirement name="HitLocation" body_parts="Head"/>
	</triggered_effect>
</effect_group>
'@ }
}

# Legendaries never roll random stats: they always drop at quality 6 (QLTemplateApos6 in loot.xml) and carry
# three fixed affixes at the quality 6 maximum (same values as AposAffixes.cs), so they are never worse than the best
# random roll. Leech is read from the AposLeech property by AposCombat.cs.
$affix = @{
  Brutal    = @(,@('EntityDamage', '.35'))
  Rapid     = @(,@('RoundsPerMinute', '.25'))
  Precise   = @(@('SpreadDegreesHorizontal', '-.4'), @('SpreadDegreesVertical', '-.4'))
  LongShot  = @(@('DamageFalloffRange', '.6'), @('MaxRange', '.6'))
  Quickload = @(,@('ReloadSpeedMultiplier', '.4'))
  Scatter   = @(,@('RoundRayCount', '.35'))
  Deadly    = @(,@('HeadshotDamageModifier', '.6'))
  Extended  = @(,@('MagazineSize', '.6'))
  Steady    = @(@('KickDegreesVerticalMin', '-.5'), @('KickDegreesVerticalMax', '-.5'), @('KickDegreesHorizontalMin', '-.5'), @('KickDegreesHorizontalMax', '-.5'))
  Fury      = @(,@('AttacksPerMinute', '.2'))
  Tireless  = @(,@('StaminaLoss', '-.4'))
  Swift     = @(@('WalkSpeed', '.1'), @('RunSpeed', '.1'))
  Leech     = @()
}
$legendAffixes = @{
  aposLegendHarold = 'Brutal', 'Rapid', 'Extended'
  aposLegendLadyFist = 'Deadly', 'Precise', 'Quickload'
  aposLegendDragonBreath = 'Scatter', 'Brutal', 'Quickload'
  aposLegendRipper = 'Brutal', 'Fury', 'Leech'
  aposLegendThunderclap = 'Brutal', 'Tireless', 'Deadly'
  aposLegendHornet = 'Extended', 'Rapid', 'Steady'
  aposLegendStormFist = 'Fury', 'Brutal', 'Swift'
  aposLegendArtemis = 'Brutal', 'Deadly', 'Quickload'
  aposLegendBigBoom = 'Brutal', 'Quickload', 'LongShot'
  aposLegendPredator = 'Deadly', 'LongShot', 'Steady'
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("`t<!-- LEGENDARY BEGIN (generated by Source/gen_legend.ps1, do not edit by hand) -->")
[void]$sb.AppendLine("`t<append xpath=`"/items`">")
foreach ($name in $legends.Keys) {
  $base = $legends[$name].Base
  $src = $vanilla.SelectSingleNode("/items/item[@name='$base']")
  if (-not $src) { throw "No vanilla item $base" }
  $item = $src.CloneNode($true)
  $item.SetAttribute('name', $name)
  # the clone drops "Extends", so copy in what the base inherits (e.g. steel knuckles get Action0 Class from iron ones)
  $ext = $src.SelectSingleNode("property[@name='Extends']")
  $skip = @()
  while ($ext) {
    if ($ext.GetAttribute('param1')) { $skip += $ext.GetAttribute('param1').Split(',') }
    $parent = $vanilla.SelectSingleNode("/items/item[@name='$($ext.GetAttribute('value'))']")
    if (-not $parent) { throw "No vanilla parent $($ext.GetAttribute('value')) of $base" }
    foreach ($p in @($parent.SelectNodes('property'))) {
      $pn = $p.GetAttribute('name'); $pc = $p.GetAttribute('class')
      if ($pn -and $pn -ne 'Extends' -and $skip -notcontains $pn -and -not $item.SelectSingleNode("property[@name='$pn']")) {
        [void]$item.AppendChild($p.CloneNode($true))
      } elseif ($pc) {
        $own = $item.SelectSingleNode("property[@class='$pc']")
        if (-not $own) { [void]$item.AppendChild($p.CloneNode($true)); continue }
        foreach ($sub in @($p.SelectNodes('property'))) {
          if (-not $own.SelectSingleNode("property[@name='$($sub.GetAttribute('name'))']")) { [void]$own.AppendChild($sub.CloneNode($true)) }
        }
      }
    }
    $ext = $parent.SelectSingleNode("property[@name='Extends']")
  }
  # not sold by traders, no crafting unlock; keep the base icon, tint it gold
  foreach ($p in 'TraderStageTemplate', 'UnlockedBy', 'Extends', 'CustomIcon', 'CustomIconTint', 'DescriptionKey', 'ItemTypeIcon') {
    foreach ($n in @($item.SelectNodes("property[@name='$p']"))) { [void]$item.RemoveChild($n) }
  }
  foreach ($n in @($item.SelectNodes('stats'))) { [void]$item.RemoveChild($n) }
  # aposLegend tag: marks legendaries (fixed bonuses, the Eridium Forge skips them)
  $tags = $item.SelectSingleNode("property[@name='Tags']")
  $tags.SetAttribute('value', $tags.GetAttribute('value') + ',aposLegend')
  $props = @(@('CustomIcon', $base), @('CustomIconTint', '255,180,0'), @('DescriptionKey', "${name}Desc"), @('ItemTypeIcon', 'trophy'))
  if ($legendAffixes[$name] -contains 'Leech') { $props += , @('AposLeech', '.06') }
  foreach ($kv in $props) {
    $prop = $vanilla.CreateElement('property'); $prop.SetAttribute('name', $kv[0]); $prop.SetAttribute('value', $kv[1])
    [void]$item.PrependChild($prop)
  }
  $frag = $vanilla.CreateDocumentFragment(); $frag.InnerXml = $legends[$name].Effect
  [void]$item.AppendChild($frag)
  $lines = foreach ($a in $legendAffixes[$name]) { foreach ($pe in $affix[$a]) { "`t<passive_effect name=`"$($pe[0])`" operation=`"perc_add`" value=`"$($pe[1])`"/>" } }
  $frag = $vanilla.CreateDocumentFragment()
  $frag.InnerXml = "<effect_group name=`"AposMod legendary affixes`" tiered=`"false`">`n" + ($lines -join "`n") + "`n</effect_group>"
  [void]$item.AppendChild($frag)
  # pretty print the cloned item
  $settings = New-Object System.Xml.XmlWriterSettings; $settings.Indent = $true; $settings.IndentChars = "`t"; $settings.OmitXmlDeclaration = $true; $settings.ConformanceLevel = 'Fragment'
  $sw = New-Object System.IO.StringWriter; $xw = [System.Xml.XmlWriter]::Create($sw, $settings); $item.WriteTo($xw); $xw.Flush()
  foreach ($line in ($sw.ToString() -split "`r?`n")) { [void]$sb.AppendLine("`t`t" + $line) }
}
[void]$sb.AppendLine("`t</append>")
[void]$sb.Append("`t<!-- LEGENDARY END -->")

$text = [IO.File]::ReadAllText($target, [Text.Encoding]::UTF8)
$begin = $text.IndexOf("`t<!-- LEGENDARY BEGIN"); $end = $text.IndexOf('<!-- LEGENDARY END -->')
if ($begin -ge 0 -and $end -gt $begin) {
  $text = $text.Substring(0, $begin) + $sb.ToString() + $text.Substring($end + '<!-- LEGENDARY END -->'.Length)
} else {
  $close = $text.LastIndexOf('</configs>')
  $text = $text.Substring(0, $close) + $sb.ToString() + "`n" + $text.Substring($close)
}
[IO.File]::WriteAllText($target, $text, [Text.UTF8Encoding]::new($false))
"Legendary items: $($legends.Count)"

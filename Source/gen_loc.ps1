param([string]$Game = (Join-Path $PSScriptRoot '..\..\..'))
# Regenerates Config\Localization.csv: every weapon description = vanilla text + the class trait (EN + RU),
# plus buff names/descriptions and messages. Run: & Source\gen_loc.ps1
# Keep this file saved as UTF-8 WITH BOM, otherwise Windows PowerShell 5.1 garbles the Cyrillic text.
$cfg = Join-Path $Game 'Data\Config'
$out = Join-Path $PSScriptRoot '..\Config\Localization.csv'
$loc = @{}; $locRu = @{}
Import-Csv (Join-Path $cfg 'Localization.csv') -Encoding UTF8 | ForEach-Object { $loc[$_.Key] = $_.english; $locRu[$_.Key] = $_.russian }

function T($title, $body) { "[FFB400]AposMod: $title[-]\n$body" }
$traits = @{
  knife  = T 'Bloodsucker' 'Every hit adds a regeneration stack (power attack: +2): +1 HP/s per stack, refreshed to 5 s by each hit.\nMax stacks: 2 + Deep Cuts level (like bleeding wounds).\nPower attack: the target bleeds.'
  club   = T 'Home Run' 'Normal hit: 15% chance to stun for 1 s (+5% per Pummel Pete level).\nPower attack to the head: launches the zombie and knocks it down (further with Pummel Pete).'
  sledge = T 'Earthquake' 'Normal hit: 20% chance to knock the target down (+5% per Skull Crusher level).\nPower attack: knocks down every zombie within 3 m (+0.5 m per Skull Crusher level).'
  baton  = T 'Chain Lightning' 'Normal hit: short shock (longer with Calculated Attack).\nPower attack: shocks every zombie within 3 m for 2 s (+0.5 m and +0.4 s per Calculated Attack level).'
  spear  = T 'Battering Ram' 'Normal hit: pushes the target back a little.\nPower attack: pierces 1 extra zombie per Spear Master level (up to 5) and throws each of them back.'
  knuck  = T 'Counter Punch' 'Taking damage fills Counter Punch charges (2 + The Brawler level), no time limit:\n- normal punch: +25% damage, spends 1 charge;\n- power attack: spends all charges, +50% damage per charge, knocks down.'
  pipe   = T 'Veteran Junk' 'Every kill with a pipe weapon: +2% damage for all pipe weapons (max +1000%).\nRepair with Scrap Iron.'
  slug   = T 'Slug Knockback' 'Slug and Breaching Slug rounds throw zombies back and knock them down. Buckshot is unchanged.'
  nade   = T 'Wide Blast' 'Blast radius against zombies x3. Block damage radius is unchanged.'
  molo   = T 'Busy Burning' 'Burning zombies move 70% slower. The fire lasts twice as long.'
  rocket = T 'Early Artillery' 'Sold by traders and found in loot from the start of the game.\nThe final loot chest of every POI contains rockets: 1 at tier 1 up to 4-6 at tier 5.'
}
$traitsRu = @{
  knife  = T 'Кровопийца' 'Каждый удар добавляет стак регенерации (силовая атака: +2): +1 HP/с за стак, каждый удар продлевает до 5 с.\nМаксимум стаков: 2 + уровень «Глубоких ран» (как кровоточащие раны).\nСиловая атака: цель кровоточит.'
  club   = T 'Хоумран' 'Обычный удар: 15% шанс оглушить на 1 с (+5% за уровень «Громилы Пита»).\nСиловая атака в голову: отправляет зомби в полёт и сбивает с ног (дальше с «Громилой Питом»).'
  sledge = T 'Землетрясение' 'Обычный удар: 20% шанс сбить цель с ног (+5% за уровень «Дробителя черепов»).\nСиловая атака: сбивает с ног всех зомби в радиусе 3 м (+0.5 м за уровень «Дробителя черепов»).'
  baton  = T 'Цепная молния' 'Обычный удар: короткий шок (дольше с «Просчитанной атакой»).\nСиловая атака: шок всем зомби в радиусе 3 м на 2 с (+0.5 м и +0.4 с за уровень «Просчитанной атаки»).'
  spear  = T 'Таран' 'Обычный удар: немного отталкивает цель.\nСиловая атака: пробивает на 1 зомби больше за каждый уровень «Мастера копья» (до 5) и отбрасывает каждого.'
  knuck  = T 'Контрудар' 'Получая урон, вы набираете заряды Контрудара (2 + уровень «Драчуна»), без ограничения по времени:\n- обычный удар: +25% урона, тратит 1 заряд;\n- силовая атака: тратит все заряды, +50% урона за заряд, сбивает с ног.'
  pipe   = T 'Ветеран-самоделка' 'Каждое убийство самодельным оружием: +2% урона всему самодельному оружию (максимум +1000%).\nЧинится металлоломом.'
  slug   = T 'Отбрасывание пулей' 'Пули (Slug и Breaching Slug) отбрасывают зомби и сбивают с ног. Дробь без изменений.'
  nade   = T 'Широкий взрыв' 'Радиус поражения зомби x3. Радиус разрушения блоков без изменений.'
  molo   = T 'Заняты горением' 'Горящие зомби двигаются на 70% медленнее. Огонь горит вдвое дольше.'
  rocket = T 'Ранняя артиллерия' 'Продаётся у торговцев и встречается в луте с начала игры.\nВ финальном сундуке каждого здания есть ракеты: 1 на уровне 1, до 4-6 на уровне 5.'
}
$map = [ordered]@{
  meleeWpnBladeT0BoneKnife = 'knife'; meleeWpnBladeT1HuntingKnife = 'knife'; meleeWpnBladeT1CandyKnife = 'knife'; meleeWpnBladeT3Machete = 'knife'
  meleeWpnClubT0WoodenClub = 'club'; meleeWpnClubT1BaseballBat = 'club'; meleeWpnClubT1CandyClub = 'club'; meleeWpnClubT3SteelClub = 'club'
  meleeWpnSledgeT0StoneSledgehammer = 'sledge'; meleeWpnSledgeT1IronSledgehammer = 'sledge'; meleeWpnSledgeT3SteelSledgehammer = 'sledge'
  meleeWpnBatonT0PipeBaton = 'baton,pipe'; meleeWpnBatonT2StunBaton = 'baton'
  meleeWpnSpearT0StoneSpear = 'spear'; meleeWpnSpearT1IronSpear = 'spear'; meleeWpnSpearT3SteelSpear = 'spear'
  meleeWpnKnucklesT0LeatherKnuckles = 'knuck'; meleeWpnKnucklesT1IronKnuckles = 'knuck'; meleeWpnKnucklesT3SteelKnuckles = 'knuck'
  gunHandgunT0PipePistol = 'pipe'; gunRifleT0PipeRifle = 'pipe'; gunMGT0PipeMachineGun = 'pipe'
  gunShotgunT0PipeShotgun = 'slug,pipe'; gunShotgunT1DoubleBarrel = 'slug'; gunShotgunT2PumpShotgun = 'slug'; gunShotgunT3AutoShotgun = 'slug'
  ammoShotgunSlug = 'slug'; ammoShotgunBreachingSlug = 'slug'
  thrownGrenade = 'nade'; thrownGrenadeContact = 'nade'; thrownAmmoPipeBomb = 'nade'
  thrownAmmoMolotovCocktail = 'molo'; gunExplosivesT3RocketLauncher = 'rocket'
}
# Buff names/descriptions and messages: key = English, Russian
$extra = [ordered]@{
  buffAposCounterName     = 'Counter Punch', 'Контрудар'
  buffAposCounterDesc     = 'Counter charges (number). Normal punch: +25% damage, spends 1 charge. Power attack: spends all charges, +50% damage per charge, knocks down.', 'Заряды контрудара (число). Обычный удар: +25% урона, тратит 1 заряд. Силовая атака: тратит все заряды, +50% урона за заряд, сбивает с ног.'
  buffAposKnifeRegenName  = 'Bloodsucker', 'Кровопийца'
  buffAposKnifeRegenDesc  = 'Regenerating HP per second (number = stacks). Every knife hit refreshes it.', 'Восстановление HP в секунду (число = стаки). Каждый удар ножом продлевает эффект.'
  buffAposPipeVeteranName = 'Veteran Junk', 'Ветеран-самоделка'
  buffAposPipeVeteranDesc = 'Pipe weapon damage bonus in % (number). +2% per kill with pipe weapons, max +1000%.', 'Бонус урона самодельного оружия в % (число). +2% за каждое убийство самодельным оружием, максимум +1000%.'
  aposPipeMaxed           = 'Veteran Junk: maximum bonus reached (+1000%)!', 'Ветеран-самоделка: достигнут максимальный бонус (+1000%)!'
  buffAposRageName        = 'Rage', 'Ярость'
  buffAposRageDesc        = 'Rage stacks (number): +6% damage and +4% attack speed per stack with clubs and sledgehammers. Every kill refreshes it for 8 s.', 'Стаки ярости (число): +6% урона и +4% скорости атаки за стак с дубинками, битами и кувалдами. Каждое убийство продлевает на 8 с.'
  # Grand Slam is replaced by Rage (full texts)
  perkGrandSlamName       = 'Rage', 'Ярость'
  perkGrandSlamDesc       = 'Every kill with a club, bat or sledgehammer fuels your rage: more damage and faster attacks for a short time.', 'Каждое убийство дубинкой, битой или кувалдой разжигает ярость: больше урона и быстрее атаки на короткое время.'
  # Intellect Mastery 4-5 are replaced (full texts)
  perkIntellectMasteryRank4Desc     = 'Field Repair', 'Полевой ремонт'
  perkIntellectMasteryRank4LongDesc = 'Field Repair: all your weapons, tools and armor wear 30% slower.', 'Полевой ремонт: всё ваше оружие, инструменты и броня изнашиваются на 30% медленнее.'
  perkIntellectMasteryRank5Desc     = 'Inventor', 'Изобретатель'
  perkIntellectMasteryRank5LongDesc = 'Inventor: everything you craft comes out one quality level higher.', 'Изобретатель: всё, что вы создаёте, получается на один уровень качества выше.'
}
for ($L = 1; $L -le 5; $L++) {
  $extra["perkGrandSlamRank${L}Desc"] = "Rage $L", "Ярость $L"
  $extra["perkGrandSlamRank${L}LongDesc"] = "Kills with clubs, bats or sledgehammers give a Rage stack for 8 s (max $L): +6% damage and +4% attack speed per stack with these weapons.", "Убийства дубинкой, битой или кувалдой дают стак Ярости на 8 с (максимум $L): +6% урона и +4% скорости атаки за стак с этим оружием."
}

# AposMod line appended to the vanilla text of every rank of the perks that scale our traits ($L = rank)
$perkLines = [ordered]@{
  perkDeepCuts          = { param($L) "Bloodsucker (knives): up to $(2 + $L) regeneration stacks (+1 HP/s each).", "Кровопийца (ножи): до $(2 + $L) стаков регенерации (+1 HP/с каждый)." }
  perkPummelPete        = { param($L) "Home Run (clubs): $(15 + 5 * $L)% stun chance on normal hits, head power attack launches $([int](150 * $L / 8))% further.", "Хоумран (дубинки и биты): $(15 + 5 * $L)% шанс оглушить обычным ударом, силовая в голову отбрасывает на $([int](150 * $L / 8))% дальше." }
  perkSkullCrusher      = { param($L) "Earthquake (sledgehammers): $(20 + 5 * $L)% knockdown chance on normal hits, power attack radius $(3 + 0.5 * $L) m.", "Землетрясение (кувалды): $(20 + 5 * $L)% шанс сбить с ног обычным ударом, радиус силовой атаки $(3 + 0.5 * $L) м." }
  perkFlurryOfIntellect = { param($L) "Chain Lightning (batons): power attack shocks zombies within $(3 + 0.5 * $L) m for $([math]::Round(2 + 0.4 * $L, 1)) s.", "Цепная молния (жезлы и дубинка из трубы): силовая атака шокирует зомби в радиусе $(3 + 0.5 * $L) м на $([math]::Round(2 + 0.4 * $L, 1)) с." }
  perkJavelinMaster     = { param($L) "Battering Ram (spears): power attack pierces $L zombie(s) behind the target.", "Таран (копья): силовая атака пробивает $L зомби за целью." }
  perkBrawler           = { param($L) "Counter Punch (knuckles): up to $(2 + $L) counter charges.", "Контрудар (кастеты): до $(2 + $L) зарядов контрудара." }
}
foreach ($p in $perkLines.Keys) {
  for ($L = 1; $L -le 5; $L++) {
    $key = "${p}Rank${L}LongDesc"
    if (-not $loc[$key]) { throw "No vanilla text for $key" }
    $line = & $perkLines[$p] $L
    $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] ' + $line[0]), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] ' + $line[1])
  }
}

function Csv($s) { '"' + $s.Replace('"', '""') + '"' }
$rows = @('Key,File,Type,UsedInMainMenu,NoTranslate,english,russian')
foreach ($item in $map.Keys) {
  $vanilla = $loc["${item}Desc"]; $vanillaRu = $locRu["${item}Desc"]
  if (-not $vanilla -or -not $vanillaRu) { throw "No vanilla description for $item" }
  $en = $vanilla + '\n\n' + (($map[$item] -split ',' | ForEach-Object { $traits[$_] }) -join '\n\n')
  $ru = $vanillaRu + '\n\n' + (($map[$item] -split ',' | ForEach-Object { $traitsRu[$_] }) -join '\n\n')
  $rows += "aposDesc_$item,items,Item,,,$(Csv $en),$(Csv $ru)"
}
foreach ($k in $extra.Keys) { $file = if ($k -like 'perk*') { 'progression,Perk' } else { 'buffs,Buff' }; $rows += "$k,$file,,,$(Csv $extra[$k][0]),$(Csv $extra[$k][1])" }
[IO.File]::WriteAllLines($out, $rows, [Text.UTF8Encoding]::new($false))
"Localization.csv: $($rows.Count - 1) keys"

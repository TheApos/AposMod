param([string]$Game = (Join-Path $PSScriptRoot '..\..\..'))
# Regenerates Config\Localization.csv: every weapon description = vanilla text + the class trait (EN + RU),
# plus buff names/descriptions and messages. Run: & Source\gen_loc.ps1
# Keep this file saved as UTF-8 WITH BOM, otherwise Windows PowerShell 5.1 garbles the Cyrillic text.
$cfg = Join-Path $Game 'Data\Config'
$out = Join-Path $PSScriptRoot '..\Config\Localization.csv'
[xml]$itemsXml = Get-Content (Join-Path $cfg 'items.xml') -Raw -Encoding UTF8
$loc = @{}; $locRu = @{}
Import-Csv (Join-Path $cfg 'Localization.csv') -Encoding UTF8 | ForEach-Object { $loc[$_.Key] = $_.english; $locRu[$_.Key] = $_.russian }

function T($title, $body) { "[FFB400]AposMod: $title[-]\n$body" }
$traits = @{
  knife  = T 'Bloodsucker' 'Every hit adds a regeneration stack (power attack: +2): +1 HP/s per stack, refreshed to 5 s by each hit.\nMax stacks: 2 + Deep Cuts level (like bleeding wounds).\nPower attack: the target bleeds.'
  club   = T 'Home Run' 'Normal hit: 15% chance to stun for 1 s (+5% per Pummel Pete level).\nPower attack to the head: launches the zombie and knocks it down (further with Pummel Pete; any body part at Pummel Pete 5).\nPower attacks cost 30% more stamina.'
  sledge = T 'Earthquake' 'Normal hit: 20% chance to knock the target down (+5% per Skull Crusher level).\nPower attack: knocks down every zombie within 3 m (+0.5 m and +10 damage to them per Skull Crusher level).\nEarthquake has a 4 s cooldown; power attacks cost 50% more stamina.'
  baton  = T 'Chain Lightning' 'Normal hit: short shock (longer with Calculated Attack).\nPower attack: shocks every zombie within 3 m for 2 s (+0.5 m and +0.4 s per Calculated Attack level).\nChain Lightning has a 3 s cooldown; power attacks cost 50% more stamina.'
  spear  = T 'Battering Ram' 'Normal hit: pushes the target back (harder with Spear Master).\nPower attack: pierces 1 zombie behind the target, +1 per Spear Master level (up to 6), and throws each of them back.\nNormal attacks cost 25% more stamina.'
  knuck  = T 'Counter Punch' 'Taking damage fills Counter Punch charges (2 + The Brawler level), no time limit:\n- normal punch: +25% damage, spends 1 charge (The Brawler: chance to gain a charge);\n- power attack: spends all charges, +50% damage per charge, knocks down.'
  pipe   = T 'Veteran Junk' 'Every kill with a pipe weapon: +2% damage for all pipe weapons (max +1000%).\nRepair with Scrap Iron.'
  slug   = T 'Slug Knockback' 'Slug and Breaching Slug rounds throw zombies back and knock them down. Buckshot is unchanged.'
  nade   = T 'Wide Blast' 'Blast radius against zombies x3. Block damage radius is unchanged.'
  molo   = T 'Busy Burning' 'Burning zombies move 70% slower. The fire lasts twice as long.'
  rocket = T 'Early Artillery' 'Sold by traders and found in loot from the start of the game.\nThe final loot chest of every POI contains rockets: 1 at tier 1 up to 4-6 at tier 5.'
  pistol = T 'Headhunter' '3 headshots in a row start Headhunter: the next 10 shots (+2 per Gunslinger level) deal +25% damage (+10% per level) with almost no spread. Their damage numbers are cyan.'
  rifle  = T 'Ricochet' 'A headshot hurts every zombie near the hit: 4 m and 20 HP, with Dead Eye up to 15 m and 70 HP.'
  mg     = T 'Suppression' 'Every hit slows the target by 5% for 8 s (max 20%, +6% per Machine Gunner level).\nEvery 4th hit: the next shot costs no ammo.'
  bow    = T 'Splinter' 'Every arrow hurts zombies within 3 m of the impact for 25 HP (+15 HP and +0.4 m per Archery level).'
  xbow   = T 'Mark' 'A bolt marks the zombie for 10 s: all your damage to it +30% (+14% per Archery level, +100% at level 5).'
}
$traitsRu = @{
  knife  = T 'Кровопийца' 'Каждый удар добавляет стак регенерации (силовая атака: +2): +1 HP/с за стак, каждый удар продлевает до 5 с.\nМаксимум стаков: 2 + уровень «Глубоких ран» (как кровоточащие раны).\nСиловая атака: цель кровоточит.'
  club   = T 'Хоумран' 'Обычный удар: 15% шанс оглушить на 1 с (+5% за уровень «Громилы Пита»).\nСиловая атака в голову: отправляет зомби в полёт и сбивает с ног (дальше с «Громилой Питом»; на 5 уровне — в любую часть тела).\nСиловые атаки тратят на 30% больше выносливости.'
  sledge = T 'Землетрясение' 'Обычный удар: 20% шанс сбить цель с ног (+5% за уровень «Дробителя черепов»).\nСиловая атака: сбивает с ног всех зомби в радиусе 3 м (+0.5 м и +10 урона им за уровень «Дробителя черепов»).\nУ Землетрясения перезарядка 4 с; силовые атаки тратят на 50% больше выносливости.'
  baton  = T 'Цепная молния' 'Обычный удар: короткий шок (дольше с «Просчитанной атакой»).\nСиловая атака: шок всем зомби в радиусе 3 м на 2 с (+0.5 м и +0.4 с за уровень «Просчитанной атаки»).\nУ Цепной молнии перезарядка 3 с; силовые атаки тратят на 50% больше выносливости.'
  spear  = T 'Таран' 'Обычный удар: отталкивает цель (сильнее с «Мастером копья»).\nСиловая атака: пробивает 1 зомби за целью, +1 за каждый уровень «Мастера копья» (до 6), и отбрасывает каждого.\nОбычные атаки тратят на 25% больше выносливости.'
  knuck  = T 'Контрудар' 'Получая урон, вы набираете заряды Контрудара (2 + уровень «Драчуна»), без ограничения по времени:\n- обычный удар: +25% урона, тратит 1 заряд («Драчун»: шанс получить заряд);\n- силовая атака: тратит все заряды, +50% урона за заряд, сбивает с ног.'
  pipe   = T 'Ветеран-самоделка' 'Каждое убийство самодельным оружием: +2% урона всему самодельному оружию (максимум +1000%).\nЧинится металлоломом.'
  slug   = T 'Отбрасывание пулей' 'Пули (Slug и Breaching Slug) отбрасывают зомби и сбивают с ног. Дробь без изменений.'
  nade   = T 'Широкий взрыв' 'Радиус поражения зомби x3. Радиус разрушения блоков без изменений.'
  molo   = T 'Заняты горением' 'Горящие зомби двигаются на 70% медленнее. Огонь горит вдвое дольше.'
  rocket = T 'Ранняя артиллерия' 'Продаётся у торговцев и встречается в луте с начала игры.\nВ финальном сундуке каждого здания есть ракеты: 1 на уровне 1, до 4-6 на уровне 5.'
  pistol = T 'Охотник за головами' '3 хедшота подряд включают Охотника за головами: следующие 10 выстрелов (+2 за уровень «Меткого стрелка») наносят +25% урона (+10% за уровень) почти без разброса. Их цифры урона голубые.'
  rifle  = T 'Рикошет' 'Хедшот ранит всех зомби рядом с попаданием: 4 м и 20 HP, с «Безжалостным снайпером» до 15 м и 70 HP.'
  mg     = T 'Подавление' 'Каждое попадание замедляет цель на 5% на 8 с (максимум 20%, +6% за уровень «Пулеметчика»).\nКаждое 4-е попадание: следующий выстрел не тратит патрон.'
  bow    = T 'Расщепление' 'Каждая стрела ранит зомби в радиусе 3 м от попадания на 25 HP (+15 HP и +0.4 м за уровень «Стрельбы из лука»).'
  xbow   = T 'Метка' 'Болт помечает зомби на 10 с: весь ваш урон по нему +30% (+14% за уровень «Стрельбы из лука», +100% на 5 уровне).'
}
$map = [ordered]@{
  meleeWpnBladeT0BoneKnife = 'knife'; meleeWpnBladeT1HuntingKnife = 'knife'; meleeWpnBladeT1CandyKnife = 'knife'; meleeWpnBladeT3Machete = 'knife'
  meleeWpnClubT0WoodenClub = 'club'; meleeWpnClubT1BaseballBat = 'club'; meleeWpnClubT1CandyClub = 'club'; meleeWpnClubT3SteelClub = 'club'
  meleeWpnSledgeT0StoneSledgehammer = 'sledge'; meleeWpnSledgeT1IronSledgehammer = 'sledge'; meleeWpnSledgeT3SteelSledgehammer = 'sledge'
  meleeWpnBatonT0PipeBaton = 'baton,pipe'; meleeWpnBatonT2StunBaton = 'baton'
  meleeWpnSpearT0StoneSpear = 'spear'; meleeWpnSpearT1IronSpear = 'spear'; meleeWpnSpearT3SteelSpear = 'spear'
  meleeWpnKnucklesT0LeatherKnuckles = 'knuck'; meleeWpnKnucklesT1IronKnuckles = 'knuck'; meleeWpnKnucklesT3SteelKnuckles = 'knuck'
  gunHandgunT0PipePistol = 'pistol,pipe'; gunRifleT0PipeRifle = 'rifle,pipe'; gunMGT0PipeMachineGun = 'mg,pipe'
  gunHandgunT1Pistol = 'pistol'; gunHandgunT3SMG5 = 'pistol'; gunHandgunT2Magnum44 = 'pistol'; gunHandgunT3DesertVulture = 'pistol'
  gunRifleT1HuntingRifle = 'rifle'; gunRifleT2LeverActionRifle = 'rifle'; gunRifleT3SniperRifle = 'rifle'
  gunMGT1AK47 = 'mg'; gunMGT2TacticalAR = 'mg'; gunMGT3M60 = 'mg'
  gunBowT0PrimitiveBow = 'bow'; gunBowT1WoodenBow = 'bow'; gunBowT3CompoundBow = 'bow'
  gunBowT1IronCrossbow = 'xbow'; gunBowT3CompoundCrossbow = 'xbow'
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
  aposKillText            = 'Kill!', 'Труп!'
  buffAposHeadhunterName  = 'Headhunter', 'Охотник за головами'
  buffAposHeadhunterDesc  = 'Empowered shots left (number): more damage, almost no spread.', 'Осталось усиленных выстрелов (число): больше урона, почти без разброса.'
  buffAposRageName        = 'Rage', 'Ярость'
  buffAposRageDesc        = 'Rage stacks (number): +10% damage and +5% attack speed per stack with clubs and sledgehammers. Every kill refreshes it for 8 s.', 'Стаки ярости (число): +10% урона и +5% скорости атаки за стак с дубинками, битами и кувалдами. Каждое убийство продлевает на 8 с.'
  buffAposQuakeCDName     = 'Earthquake: cooldown', 'Землетрясение: перезарядка'
  buffAposQuakeCDDesc     = 'Earthquake is recharging.', 'Землетрясение перезаряжается.'
  buffAposChainCDName     = 'Chain Lightning: cooldown', 'Цепная молния: перезарядка'
  buffAposChainCDDesc     = 'Chain Lightning is recharging.', 'Цепная молния перезаряжается.'
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
  $extra["perkGrandSlamRank${L}LongDesc"] = "Kills with clubs, bats or sledgehammers give a Rage stack for 8 s (max $L): +10% damage and +5% attack speed per stack with these weapons.", "Убийства дубинкой, битой или кувалдой дают стак Ярости на 8 с (максимум $L): +10% урона и +5% скорости атаки за стак с этим оружием."
}
$extra['perkGrandSlamRank5LongDesc'] = ($extra['perkGrandSlamRank5LongDesc'][0] + '\n[FFB400]Rampage:[-] at 5 stacks club and sledgehammer attacks cost no stamina and you cannot be stunned or knocked down.'), ($extra['perkGrandSlamRank5LongDesc'][1] + '\n[FFB400]Буйство:[-] на 5 стаках атаки дубинками и кувалдами не тратят выносливость, а вас нельзя оглушить или сбить с ног.')

# AposMod line appended to the vanilla text of every rank of the perks that scale our traits ($L = rank).
# "Replaces ..." notes tell the player which vanilla knockdown above no longer works (removed in progression.xml).
$perkLines = [ordered]@{
  perkDeepCuts          = { param($L) "Bloodsucker (knives): up to $(2 + $L) regeneration stacks (+1 HP/s each).", "Кровопийца (ножи): до $(2 + $L) стаков регенерации (+1 HP/с каждый)." }
  perkPummelPete        = { param($L) "Home Run (clubs and bats): $(15 + 5 * $L)% stun chance on normal hits, head power attack launches $([int](150 * $L / 8))% further. Replaces the power-attack knockdown above: power attacks deal +$(10 * $L)% damage to stunned enemies.", "Хоумран (дубинки и биты): $(15 + 5 * $L)% шанс оглушить обычным ударом, силовая в голову отбрасывает на $([int](150 * $L / 8))% дальше. Заменяет сбивание с ног силовой атакой выше: силовые атаки наносят +$(10 * $L)% урона оглушённым." }
  perkSkullCrusher      = { param($L) "Earthquake (sledgehammers): $(20 + 5 * $L)% knockdown chance on normal hits, power attack radius $(3 + 0.5 * $L) m. Replaces the power-attack knockdown chances above: Earthquake deals $(10 * $L) damage to every zombie it knocks down.", "Землетрясение (кувалды): $(20 + 5 * $L)% шанс сбить с ног обычным ударом, радиус силовой атаки $(3 + 0.5 * $L) м. Заменяет шансы сбить с ног силовой атакой выше: Землетрясение наносит $(10 * $L) урона каждому сбитому зомби." }
  perkFlurryOfIntellect = { param($L) "Chain Lightning (batons): power attack shocks zombies within $(3 + 0.5 * $L) m for $([math]::Round(2 + 0.4 * $L, 1)) s.", "Цепная молния (жезлы и дубинка из трубы): силовая атака шокирует зомби в радиусе $(3 + 0.5 * $L) м на $([math]::Round(2 + 0.4 * $L, 1)) с." }
  perkJavelinMaster     = { param($L) "Battering Ram (spears): power attack pierces $(1 + $L) zombies behind the target, normal hits push $([int](40 * $L / 1.2))% harder.", "Таран (копья): силовая атака пробивает $(1 + $L) зомби за целью, обычный удар отталкивает на $([int](40 * $L / 1.2))% сильнее." }
  perkBrawler           = { param($L) "Counter Punch (knuckles): up to $(2 + $L) counter charges. Replaces the power stagger / knockdown above: normal punches have a $(10 * $L)% chance to give +1 charge.", "Контрудар (кастеты): до $(2 + $L) зарядов. Заменяет оглушение / сбивание с ног силовой атакой выше: обычный удар с шансом $(10 * $L)% даёт +1 заряд." }
  perkGunslinger        = { param($L) "Headhunter (handguns): $(10 + 2 * $L) empowered shots with +$(25 + 10 * $L)% damage after 3 headshots in a row.", "Охотник за головами (пистолеты): $(10 + 2 * $L) усиленных выстрелов с +$(25 + 10 * $L)% урона после 3 хедшотов подряд." }
  perkDeadEye           = { param($L) "Ricochet (rifles): a headshot hurts zombies within $(@(4,5,7,9,12,15)[$L]) m for $(20 + 10 * $L) HP.", "Рикошет (винтовки): хедшот ранит зомби в радиусе $(@(4,5,7,9,12,15)[$L]) м на $(20 + 10 * $L) HP." }
  perkMachineGunner     = { param($L) "Suppression (machine guns): hits slow targets up to $(20 + 6 * $L)%; every 4th hit makes the next shot free.", "Подавление (автоматы): попадания замедляют цель до $(20 + 6 * $L)%; каждое 4-е попадание делает следующий выстрел бесплатным." }
  perkArchery           = { param($L) "Splinter (bows): arrows hurt zombies within $([math]::Round(3 + 0.4 * $L, 1)) m for $(25 + 15 * $L) HP. Mark (crossbows): your damage to marked zombies +$(30 + 14 * $L)%.", "Расщепление (луки): стрелы ранят зомби в радиусе $([math]::Round(3 + 0.4 * $L, 1)) м на $(25 + 15 * $L) HP. Метка (арбалеты): ваш урон по помеченным зомби +$(30 + 14 * $L)%." }
}
# Rank 5 highlights, appended after the scaling line
$perkFinale = @{
  perkDeepCuts          = 'Blood Feast: a knife kill at max stacks heals 25 HP at once.', 'Пир крови: убийство ножом на максимуме стаков сразу лечит 25 HP.'
  perkPummelPete        = 'Home Run works on a power hit to any body part, not only the head.', 'Хоумран срабатывает от силовой атаки в любую часть тела, не только в голову.'
  perkSkullCrusher      = 'Aftershock: zombies knocked down by Earthquake stay stunned for 3 s after getting up.', 'Афтершок: сбитые Землетрясением зомби ещё 3 с оглушены после подъёма.'
  perkFlurryOfIntellect = 'Every 4th normal baton hit also fires Chain Lightning.', 'Каждый 4-й обычный удар жезлом тоже запускает Цепную молнию.'
  perkJavelinMaster     = 'Skewer: every zombie hit by a spear power attack is knocked down.', 'Насадить: каждый зомби, задетый силовой атакой копья, сбит с ног.'
  perkBrawler           = 'Knockout: a power punch on full charges kills any non-elite zombie.', 'Нокаут: силовой удар на полных зарядах убивает любого не-элитного зомби.'
  perkGunslinger        = 'Every kill during Headhunter adds 2 more empowered shots.', 'Каждое убийство во время Охотника за головами добавляет 2 усиленных выстрела.'
  perkDeadEye           = 'Ricochet also stuns every zombie it hits for 2 s.', 'Рикошет также оглушает всех задетых зомби на 2 с.'
  perkMachineGunner     = 'Fully suppressed targets take +25% damage from you.', 'Полностью подавленные цели получают от вас +25% урона.'
  perkArchery           = 'A headshot arrow sets the target on fire; Mark gives +100% damage.', 'Стрела в голову поджигает цель; Метка даёт +100% урона.'
}
foreach ($p in $perkLines.Keys) {
  for ($L = 1; $L -le 5; $L++) {
    $key = "${p}Rank${L}LongDesc"
    if (-not $loc[$key]) { throw "No vanilla text for $key" }
    $line = & $perkLines[$p] $L
    $en = $loc[$key] + '\n\n[FFB400]AposMod:[-] ' + $line[0]
    $ru = $locRu[$key] + '\n\n[FFB400]AposMod:[-] ' + $line[1]
    if ($L -eq 5) { $en += '\n[FFB400]Level 5:[-] ' + $perkFinale[$p][0]; $ru += '\n[FFB400]Уровень 5:[-] ' + $perkFinale[$p][1] }
    $extra[$key] = $en, $ru
  }
}

function Csv($s) { '"' + $s.Replace('"', '""') + '"' }
$rows = @('Key,File,Type,UsedInMainMenu,NoTranslate,english,russian')
foreach ($item in $map.Keys) {
  # vanilla description key: the item's own DescriptionKey (e.g. the compound crossbow reuses the iron one) or <name>Desc
  $descKey = ($itemsXml.SelectSingleNode("/items/item[@name='$item']/property[@name='DescriptionKey']")).value
  if (-not $descKey) { $descKey = "${item}Desc" }
  $vanilla = $loc[$descKey]; $vanillaRu = $locRu[$descKey]
  if (-not $vanilla -or -not $vanillaRu) { throw "No vanilla description for $item" }
  $en = $vanilla + '\n\n' + (($map[$item] -split ',' | ForEach-Object { $traits[$_] }) -join '\n\n')
  $ru = $vanillaRu + '\n\n' + (($map[$item] -split ',' | ForEach-Object { $traitsRu[$_] }) -join '\n\n')
  $rows += "aposDesc_$item,items,Item,,,$(Csv $en),$(Csv $ru)"
}
foreach ($k in $extra.Keys) { $file = if ($k -like 'perk*') { 'progression,Perk' } else { 'buffs,Buff' }; $rows += "$k,$file,,,$(Csv $extra[$k][0]),$(Csv $extra[$k][1])" }
[IO.File]::WriteAllLines($out, $rows, [Text.UTF8Encoding]::new($false))
"Localization.csv: $($rows.Count - 1) keys"

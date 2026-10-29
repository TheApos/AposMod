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
  club   = T 'Home Run' 'Normal hit: 15% chance to stun for 1 s (+5% per Pummel Pete level).\nPower attack to the head: launches the zombie and knocks it down (further with Pummel Pete; at level 5 any body part, and every 3rd normal hit is a Home Run too).\nPower attacks cost 30% more stamina.'
  sledge = T 'Earthquake' 'Normal hit: 20% chance to knock the target down (+5% per Skull Crusher level).\nPower attack: knocks down every zombie within 3 m (+0.5 m and +10 damage to them per Skull Crusher level).\nEarthquake has a 4 s cooldown; power attacks cost 50% more stamina.'
  baton  = T 'Chain Lightning' 'Normal hit: short shock (longer with Calculated Attack).\nPower attack: shocks every zombie within 3 m for 2 s (+0.5 m and +0.4 s per Calculated Attack level).\nChain Lightning has a 3 s cooldown; power attacks cost 50% more stamina.'
  spear  = T 'Battering Ram' 'Normal hit: pushes the target back (harder with Spear Master).\nPower attack: pierces 1 zombie behind the target, +1 per Spear Master level (up to 6), and throws each of them back.'
  knuck  = T 'Counter Punch' 'Taking damage fills Counter Punch charges (2 + The Brawler level), no time limit:\n- normal punch: +25% damage, spends 1 charge (The Brawler: chance to gain a charge);\n- power attack: spends all charges, +50% damage per charge, knocks down.'
  pipe   = T 'Veteran Junk' 'Every kill with a pipe weapon: +2% damage for all pipe weapons (max +1000%).\nRepair with Scrap Iron.'
  slug   = T 'Slug Knockback' 'Slug and Breaching Slug rounds throw zombies back and knock them down. Buckshot is unchanged.'
  nade   = T 'Wide Blast' 'Blast radius against zombies x3. Block damage radius is unchanged.'
  molo   = T 'Busy Burning' 'Burning zombies move 70% slower and lose 4% of their max health every second (at least 15), on top of the normal fire. The fire lasts twice as long.'
  rocket = T 'Early Artillery' 'Sold by traders and found in loot from the start of the game.\nThe final loot chest of every POI contains rockets: 1 at tier 1 up to 4-6 at tier 5.'
  pistol = T 'Headhunter' '3 headshots in a row start Headhunter: the next 10 shots (+2 per Gunslinger level) deal +25% damage (+10% per level) with almost no spread.'
  rifle  = T 'Ricochet' 'A headshot hurts every zombie near the hit: 5 m and 20 HP, +2 m and +10 HP per Dead Eye level (up to 15 m and 70 HP).'
  mg     = T 'Suppression' 'Every hit adds 5% slow to the target for 8 s, up to 10% (+10% per Machine Gunner level, 60% at level 5).\nEvery 4th hit: the next shot costs no ammo.'
  bow    = T 'Splinter' 'On impact the arrow bursts into fragments that hit the nearest other zombies within 5 m: 2 fragments of 20 HP, more and stronger with Archery (8 of 50 at level 5).'
  xbow   = T 'Mark' 'A bolt marks the zombie for 10 s: all your damage to it +20% (+20% per Archery level, +120% at level 5). At level 5 the bolt marks every zombie within 4 m.\nPin: if a wall is within 3 m behind the zombie, the bolt pins it in place until it dies (elites for 10 s).'
  robot  = T 'Guardian Protocol' 'While your health is below 30%, your turrets and robotic sledges attack 30% faster and deal 10% more damage (+10% speed and +5% damage per Robotics Inventor level; at level 5 below 50% health: +80% / +35%).'}
$traitsRu = @{
  knife  = T 'Кровопийца' 'Каждый удар добавляет стак регенерации (силовая атака: +2): +1 HP/с за стак, каждый удар продлевает до 5 с.\nМаксимум стаков: 2 + уровень «Глубоких ран» (как кровоточащие раны).\nСиловая атака: цель кровоточит.'
  club   = T 'Хоумран' 'Обычный удар: 15% шанс оглушить на 1 с (+5% за уровень «Громилы Пита»).\nСиловая атака в голову: отправляет зомби в полёт и сбивает с ног (дальше с «Громилой Питом»; на 5 уровне — в любую часть тела, и каждый 3-й обычный удар — тоже Хоумран).\nСиловые атаки тратят на 30% больше выносливости.'
  sledge = T 'Землетрясение' 'Обычный удар: 20% шанс сбить цель с ног (+5% за уровень «Дробителя черепов»).\nСиловая атака: сбивает с ног всех зомби в радиусе 3 м (+0.5 м и +10 урона им за уровень «Дробителя черепов»).\nУ Землетрясения перезарядка 4 с; силовые атаки тратят на 50% больше выносливости.'
  baton  = T 'Цепная молния' 'Обычный удар: короткий шок (дольше с «Просчитанной атакой»).\nСиловая атака: шок всем зомби в радиусе 3 м на 2 с (+0.5 м и +0.4 с за уровень «Просчитанной атаки»).\nУ Цепной молнии перезарядка 3 с; силовые атаки тратят на 50% больше выносливости.'
  spear  = T 'Таран' 'Обычный удар: отталкивает цель (сильнее с «Мастером копья»).\nСиловая атака: пробивает 1 зомби за целью, +1 за каждый уровень «Мастера копья» (до 6), и отбрасывает каждого.'
  knuck  = T 'Контрудар' 'Получая урон, вы набираете заряды Контрудара (2 + уровень «Драчуна»), без ограничения по времени:\n- обычный удар: +25% урона, тратит 1 заряд («Драчун»: шанс получить заряд);\n- силовая атака: тратит все заряды, +50% урона за заряд, сбивает с ног.'
  pipe   = T 'Ветеран-самоделка' 'Каждое убийство самодельным оружием: +2% урона всему самодельному оружию (максимум +1000%).\nЧинится металлоломом.'
  slug   = T 'Отбрасывание пулей' 'Пули (Slug и Breaching Slug) отбрасывают зомби и сбивают с ног. Дробь без изменений.'
  nade   = T 'Широкий взрыв' 'Радиус поражения зомби x3. Радиус разрушения блоков без изменений.'
  molo   = T 'Заняты горением' 'Горящие зомби двигаются на 70% медленнее и каждую секунду теряют 4% максимального здоровья (не меньше 15) сверх обычного огня. Огонь горит вдвое дольше.'
  rocket = T 'Ранняя артиллерия' 'Продаётся у торговцев и встречается в луте с начала игры.\nВ финальном сундуке каждого здания есть ракеты: 1 на уровне 1, до 4-6 на уровне 5.'
  pistol = T 'Охотник за головами' '3 хедшота подряд включают Охотника за головами: следующие 10 выстрелов (+2 за уровень «Меткого стрелка») наносят +25% урона (+10% за уровень) почти без разброса.'
  rifle  = T 'Рикошет' 'Хедшот ранит всех зомби рядом с попаданием: 5 м и 20 HP, +2 м и +10 HP за уровень «Безжалостного снайпера» (до 15 м и 70 HP).'
  mg     = T 'Подавление' 'Каждое попадание добавляет цели 5% замедления на 8 с, максимум 10% (+10% за уровень «Пулеметчика», 60% на 5 уровне).\nКаждое 4-е попадание: следующий выстрел не тратит патрон.'
  bow    = T 'Расщепление' 'При попадании стрела разлетается на осколки, которые бьют ближайших других зомби в радиусе 5 м: 2 осколка по 20 HP, с «Стрельбой из лука» больше и сильнее (8 по 50 на 5 уровне).'
  xbow   = T 'Метка' 'Болт помечает зомби на 10 с: весь ваш урон по нему +20% (+20% за уровень «Стрельбы из лука», +120% на 5 уровне). На 5 уровне болт помечает всех зомби в радиусе 4 м.\nПригвоздить: если в 3 м за зомби стена, болт пригвождает его на месте до смерти (элиту — на 10 с).'
  robot  = T 'Протокол защиты' 'Пока ваше здоровье ниже 30%, ваши турели и робо-кувалды атакуют на 30% быстрее и наносят на 10% больше урона (+10% скорости и +5% урона за уровень «Изобретателя роботов»; на 5 уровне при здоровье ниже 50%: +80% / +35%).'}
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
  gunBotT1JunkSledge = 'robot'; gunBotT2JunkTurret = 'robot'
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
  buffAposGuardianName    = 'Guardian Protocol', 'Протокол защиты'
  buffAposGuardianDesc    = 'Your health is low: your robots attack faster and harder.', 'Ваше здоровье низкое: ваши роботы атакуют быстрее и сильнее.'
  modArmorAposEnergyShield     = 'Energy Shield Mod', 'Мод: энергощит'
  modArmorAposEnergyShieldDesc = '[FFB400]AposMod: Energy Shield[-]\nChest armor mod. While charged, ranged attacks (bullets, arrows, spit) deal no damage to you.\nAny hit drains the shield; it recharges after 5 s without taking damage.\nCraft at a workbench or find in loot.', '[FFB400]AposMod: Энергощит[-]\nМод для нагрудника. Пока заряжен, дальние атаки (пули, стрелы, плевки) не наносят вам урона.\nЛюбое попадание разряжает щит; он заряжается через 5 с без получения урона.\nСоздаётся на верстаке или находится в луте.'
  buffAposShieldUpName    = 'Energy Shield', 'Энергощит'
  buffAposShieldUpDesc    = 'Charged: ranged attacks deal no damage. Any hit drains it.', 'Заряжен: дальние атаки не наносят урона. Любое попадание разряжает его.'
  modWeaponAposFire       = 'Incendiary Mod', 'Мод: зажигательный'
  modWeaponAposFireDesc   = '[FFB400]AposMod: Fire[-]\nAny weapon. Hits have a 30% chance to set the target on fire for 10 s: every second it loses 10% of your last hit (at least 10).\nOnly one elemental mod per weapon.\n[FF3DF2]Reactions[-] (two elements on one zombie): fire + acid = Explosion (50% of your last hit, at least 60, within 4 m), fire + shock = Confusion, shock + acid = Miasma.', '[FFB400]AposMod: Огонь[-]\nЛюбое оружие. Попадания с шансом 30% поджигают цель на 10 с: каждую секунду она теряет 10% урона вашего последнего удара (не меньше 10).\nНа оружие — только один стихийный мод.\n[FF3DF2]Реакции[-] (две стихии на одном зомби): огонь + кислота = Взрыв (50% урона последнего удара, не меньше 60, в радиусе 4 м), огонь + ток = Замешательство, ток + кислота = Миазмы.'
  modWeaponAposShock      = 'Tesla Mod', 'Мод: тесла'
  modWeaponAposShockDesc  = '[FFB400]AposMod: Shock[-]\nAny weapon. Hits have a 20% chance to shock the target for 1.5 s: every second 15% of your last hit (at least 10).\nOnly one elemental mod per weapon.\n[FF3DF2]Reactions[-] (two elements on one zombie): fire + acid = Explosion (50% of your last hit, at least 60, within 4 m), fire + shock = Confusion, shock + acid = Miasma.', '[FFB400]AposMod: Ток[-]\nЛюбое оружие. Попадания с шансом 20% шокируют цель на 1.5 с: каждую секунду 15% урона вашего последнего удара (не меньше 10).\nНа оружие — только один стихийный мод.\n[FF3DF2]Реакции[-] (две стихии на одном зомби): огонь + кислота = Взрыв (50% урона последнего удара, не меньше 60, в радиусе 4 м), огонь + ток = Замешательство, ток + кислота = Миазмы.'
  modWeaponAposAcid       = 'Corrosive Mod', 'Мод: кислотный'
  modWeaponAposAcidDesc   = '[FFB400]AposMod: Acid[-]\nAny weapon. Hits have a 25% chance to corrode the target for 6 s: it takes more physical damage and every second loses 8% of your last hit (at least 8).\nOnly one elemental mod per weapon.\n[FF3DF2]Reactions[-] (two elements on one zombie): fire + acid = Explosion (50% of your last hit, at least 60, within 4 m), fire + shock = Confusion, shock + acid = Miasma.', '[FFB400]AposMod: Кислота[-]\nЛюбое оружие. Попадания с шансом 25% разъедают цель на 6 с: она получает больше физического урона и каждую секунду теряет 8% урона вашего последнего удара (не меньше 8).\nНа оружие — только один стихийный мод.\n[FF3DF2]Реакции[-] (две стихии на одном зомби): огонь + кислота = Взрыв (50% урона последнего удара, не меньше 60, в радиусе 4 м), огонь + ток = Замешательство, ток + кислота = Миазмы.'
  aposLegendHarold           = '[FF9900]Unkempt Harold[-]', '[FF9900]Растрёпанный Гарольд[-]'
  aposLegendHaroldDesc       = '[FF9900]Legendary .44 Desert Vulture.[-]\nEvery shot fires 3 bullets in a wide fan. Why aim when you can do everything at once?', '[FF9900]Легендарный .44 «Пустынный стервятник».[-]\nКаждый выстрел — 3 пули широким веером. Зачем целиться, если можно всё и сразу?'
  aposLegendLadyFist         = '[FF9900]Lady Fist[-]', '[FF9900]Леди Кулак[-]'
  aposLegendLadyFistDesc     = '[FF9900]Legendary 9mm pistol.[-]\n+50% damage. Every headshot heals you for 5 HP.', '[FF9900]Легендарный 9-мм пистолет.[-]\n+50% урона. Каждый хедшот лечит вас на 5 HP.'
  aposLegendDragonBreath     = '[FF9900]Dragon''s Breath[-]', '[FF9900]Дыхание дракона[-]'
  aposLegendDragonBreathDesc = '[FF9900]Legendary pump shotgun.[-]\n+20% damage. Every hit sets the target on fire.', '[FF9900]Легендарный помповый дробовик.[-]\n+20% урона. Каждое попадание поджигает цель.'
  aposLegendRipper           = '[FF9900]The Ripper[-]', '[FF9900]Потрошитель[-]'
  aposLegendRipperDesc       = '[FF9900]Legendary machete.[-]\n+40% damage, every hit causes bleeding, +50% more against bleeding targets. Every kill heals 20 HP.', '[FF9900]Легендарное мачете.[-]\n+40% урона, каждый удар вызывает кровотечение, по кровоточащим ещё +50%. Каждое убийство лечит 20 HP.'
  aposLegendThunderclap      = '[FF9900]Thunderclap[-]', '[FF9900]Раскат грома[-]'
  aposLegendThunderclapDesc  = '[FF9900]Legendary steel sledgehammer.[-]\n+25% damage, power attacks shock zombies within 4 m.\nStorm charges: every kill gives a charge; at 3 charges the next power attack calls lightning: 150 damage and a 3 s shock to all zombies within 6 m.', '[FF9900]Легендарная стальная кувалда.[-]\n+25% урона, силовые атаки шокируют зомби в радиусе 4 м.\nГрозовые заряды: каждое убийство даёт заряд; на 3 зарядах следующая силовая атака вызывает молнию: 150 урона и шок 3 с всем зомби в радиусе 6 м.'
  aposLegendHornet           = '[FF9900]Hornet[-]', '[FF9900]Шершень[-]'
  aposLegendHornetDesc       = '[FF9900]Legendary SMG-5.[-]\n+15% damage. Hits have a 30% chance to corrode the target (acid: fuel for reactions).', '[FF9900]Легендарный SMG-5.[-]\n+15% урона. Попадания с шансом 30% разъедают цель кислотой (топливо для реакций).'
  aposLegendStormFist        = '[FF9900]Storm Fist[-]', '[FF9900]Кулак бури[-]'
  aposLegendStormFistDesc    = '[FF9900]Legendary steel knuckles.[-]\n+20% damage. Every punch has a 25% chance to ignite and a 25% chance to shock: Confusion comes by itself.', '[FF9900]Легендарный стальной кастет.[-]\n+20% урона. Каждый удар с шансом 25% поджигает и с шансом 25% шокирует: Замешательство приходит само.'
  aposLegendArtemis          = '[FF9900]Artemis[-]', '[FF9900]Артемида[-]'
  aposLegendArtemisDesc      = '[FF9900]Legendary compound bow.[-]\nEvery arrow sets the target on fire and bursts into 8 fragments of 50 HP, at any Archery level.', '[FF9900]Легендарный блочный лук.[-]\nКаждая стрела поджигает цель и разлетается на 8 осколков по 50 HP при любом уровне «Стрельбы из лука».'
  aposLegendBigBoom          = '[FF9900]Big Boom[-]', '[FF9900]Большой бабах[-]'
  aposLegendBigBoomDesc      = '[FF9900]Legendary rocket launcher.[-]\nAfter every shot there is a 50% chance that the next rocket costs nothing.', '[FF9900]Легендарная ракетница.[-]\nПосле каждого выстрела с шансом 50% следующая ракета бесплатна.'
  aposLegendPredator         = '[FF9900]Predator[-]', '[FF9900]Хищник[-]'
  aposLegendPredatorDesc     = '[FF9900]Legendary sniper rifle.[-]\n+25% damage. Every headshot ricochets: 100 damage and a 2 s stun to all zombies within 15 m of the hit, at any Dead Eye level.', '[FF9900]Легендарная снайперская винтовка.[-]\n+25% урона. Каждый хедшот рикошетит: 100 урона и оглушение 2 с всем зомби в радиусе 15 м от попадания при любом уровне «Безжалостного снайпера».'
  buffAposThunderChargeName  = 'Storm Charges', 'Грозовые заряды'
  buffAposThunderChargeDesc  = 'Thunderclap: at 3 charges the next power attack calls lightning.', 'Раскат грома: на 3 зарядах следующая силовая атака вызывает молнию.'
  buffAposFreeRocketName     = 'Free Rocket', 'Бесплатная ракета'
  buffAposFreeRocketDesc     = 'Big Boom: the next rocket costs no ammo.', 'Большой бабах: следующая ракета не тратит боеприпас.'
  buffAposFrenzyName         = 'Frenzy', 'Неистовство'
  buffAposFrenzyDesc         = '+25% attack speed with these weapons.', '+25% скорости атаки этим оружием.'
  buffAposAdrenalineName     = 'Adrenaline', 'Адреналин'
  buffAposAdrenalineDesc     = '+10% move speed per stack. 15 s after the last kill.', '+10% скорости передвижения за стак. 15 с после последнего убийства.'
  buffAposEmpName            = 'EMP Charges', 'Заряды ЭМИ'
  buffAposEmpDesc            = 'At 10 charges your next hit releases an EMP around you.', 'На 10 зарядах следующий удар выпускает ЭМИ вокруг вас.'
  buffAposJackpotName        = 'Jackpot!', 'Джекпот!'
  buffAposJackpotDesc        = 'Mining yields x5.', 'Добыча x5.'
  buffAposEvasionName        = 'Evasion', 'Уклонение'
  buffAposEvasionDesc        = '+30% move speed.', '+30% скорости передвижения.'
  buffAposSecondWindName     = 'Second Wind (recharging)', 'Второе дыхание (перезарядка)'
  buffAposSecondWindDesc     = 'Second Wind is ready again when this ends.', 'Второе дыхание снова готово, когда это закончится.'
  buffAposSecondLungsName    = 'Second Lungs (recharging)', 'Второе дыхание лёгких (перезарядка)'
  buffAposSecondLungsDesc    = 'Second Lungs is ready again when this ends.', 'Второе дыхание лёгких снова готово, когда это закончится.'
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
  perkDeadEye           = { param($L) "Ricochet (rifles): a headshot hurts zombies within $(5 + 2 * $L) m for $(20 + 10 * $L) HP.", "Рикошет (винтовки): хедшот ранит зомби в радиусе $(5 + 2 * $L) м на $(20 + 10 * $L) HP." }
  perkMachineGunner     = { param($L) "Suppression (machine guns): hits slow targets up to $(10 + 10 * $L)%; every 4th hit makes the next shot free.", "Подавление (автоматы): попадания замедляют цель до $(10 + 10 * $L)%; каждое 4-е попадание делает следующий выстрел бесплатным." }
  perkArchery           = { param($L) "Splinter (bows): $(@(2,3,4,5,6,8)[$L]) fragments of $(20 + 5 * $L + $(if ($L -eq 5) { 5 } else { 0 })) HP. Mark (crossbows): your damage to marked zombies +$(20 + 20 * $L)%.", "Расщепление (луки): осколков — $(@(2,3,4,5,6,8)[$L]), по $(20 + 5 * $L + $(if ($L -eq 5) { 5 } else { 0 })) HP. Метка (арбалеты): ваш урон по помеченным зомби +$(20 + 20 * $L)%." }
  perkTurrets           = { param($L) "Guardian Protocol (robots): below 30% health your robots attack $(30 + 10 * $L)% faster and deal $(10 + 5 * $L)% more damage.", "Протокол защиты (роботы): при здоровье ниже 30% ваши роботы атакуют на $(30 + 10 * $L)% быстрее и наносят на $(10 + 5 * $L)% больше урона." }}
# Rank 5 highlights, appended after the scaling line
$perkFinale = @{
  perkDeepCuts          = 'Blood Feast: a knife kill at max stacks heals 25 HP at once.', 'Пир крови: убийство ножом на максимуме стаков сразу лечит 25 HP.'
  perkPummelPete        = 'Home Run works on a power hit to any body part, and every 3rd normal hit is a Home Run too.', 'Хоумран срабатывает от силовой атаки в любую часть тела, а каждый 3-й обычный удар — тоже Хоумран.'
  perkSkullCrusher      = 'Aftershock: zombies knocked down by Earthquake stay stunned for 3 s after getting up.', 'Афтершок: сбитые Землетрясением зомби ещё 3 с оглушены после подъёма.'
  perkFlurryOfIntellect = 'Every 4th normal baton hit also fires Chain Lightning.', 'Каждый 4-й обычный удар жезлом тоже запускает Цепную молнию.'
  perkJavelinMaster     = 'Skewer: every zombie hit by a spear power attack is knocked down.', 'Насадить: каждый зомби, задетый силовой атакой копья, сбит с ног.'
  perkBrawler           = 'Knockout: a power punch on full charges kills any non-elite zombie.', 'Нокаут: силовой удар на полных зарядах убивает любого не-элитного зомби.'
  perkGunslinger        = 'Every kill during Headhunter adds 2 more empowered shots.', 'Каждое убийство во время Охотника за головами добавляет 2 усиленных выстрела.'
  perkDeadEye           = 'Ricochet also stuns every zombie it hits for 2 s.', 'Рикошет также оглушает всех задетых зомби на 2 с.'
  perkMachineGunner     = 'Fully suppressed targets take +25% damage from you; every kill gives 5 rounds of the loaded ammo.', 'Полностью подавленные цели получают от вас +25% урона; каждое убийство даёт 5 патронов заряженного типа.'
  perkArchery           = 'A headshot arrow sets the target on fire; a bolt marks every zombie within 4 m.', 'Стрела в голову поджигает цель; болт помечает всех зомби в радиусе 4 м.'
  perkTurrets           = 'Guardian Protocol starts below 50% health (instead of 30%).', 'Протокол защиты включается при здоровье ниже 50% (вместо 30%).'}
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

# ===== 1.0: max-rank highlights of the other perks (perk -> max rank, EN, RU), appended to that rank's vanilla text =====
$perkHighlights = [ordered]@{
  perkFlurryOfPerception = 5, 'Frenzy: a kill with a spear gives +25% attack speed for 5 s.', 'Неистовство: убийство копьём даёт +25% скорости атаки на 5 с.'
  perkFlurryOfStrength   = 5, 'Frenzy: a kill with clubs or sledgehammers gives +25% attack speed for 5 s.', 'Неистовство: убийство дубинкой или кувалдой даёт +25% скорости атаки на 5 с.'
  perkFlurryOfFortitude  = 5, 'Frenzy: a kill with knuckles or bare hands gives +25% attack speed for 5 s.', 'Неистовство: убийство кастетом или голыми руками даёт +25% скорости атаки на 5 с.'
  perkFlurryOfAgility    = 5, 'Frenzy: a kill with knives gives +25% attack speed for 5 s.', 'Неистовство: убийство ножом даёт +25% скорости атаки на 5 с.'
  perkDemolitionsExpert  = 5, 'Chain Reaction: a zombie killed by your explosive blows up too: 250 damage within 4 m.', 'Цепная реакция: зомби, убитый вашей взрывчаткой, сам взрывается: 250 урона в радиусе 4 м.'
  perkPenetrator         = 5, 'Shrapnel: armor-piercing hits deal 40% of their damage to every zombie within 3 m of the target.', 'Шрапнель: бронебойные попадания наносят 40% своего урона всем зомби в радиусе 3 м от цели.'
  perkInfiltrator        = 5, 'Ghost: crouching halves your noise and visibility, hits on zombies that have not noticed you deal x2 damage, mines and traps cannot hurt you.', 'Призрак: в приседе вы вдвое тише и незаметнее, удары по зомби, которые вас не заметили, наносят x2 урона, мины и ловушки вам не вредят.'
  perkAnimalTracker      = 5, "Hunter's Mark: hitting an animal marks it for 10 s like a crossbow bolt (red ring, more damage).", 'Метка охотника: попадание по животному помечает его на 10 с, как болт арбалета (красное кольцо, больше урона).'
  perkTreasureHunter     = 5, 'X Marks the Spot: the treasure search circle shrinks almost to a point, and buried treasure also holds a quality 5-6 weapon with affixes.', 'Крестик на карте: круг поиска клада сжимается почти до точки, а в кладе ещё и оружие 5-6 качества с аффиксами.'
  perkSalvageOperations  = 5, 'Junk Genius: salvage tools wear 90% slower.', 'Гений хлама: инструменты для разборки изнашиваются на 90% медленнее.'
  perkPerceptionMastery  = 5, 'Lucky Shot: a hit with a Perception weapon has a 10% chance to deal +100% damage.', 'Удачный выстрел: попадание оружием Восприятия с шансом 10% наносит +100% урона.'
  perkBoomstick          = 5, 'Pump Action: every shotgun kill loads 1 shell into the magazine.', 'Помпа: каждое убийство из дробовика заряжает 1 патрон в магазин.'
  perkPackMule           = 5, 'Beast of Burden: an overloaded backpack no longer slows you down.', 'Вьючный зверь: перегруз больше не замедляет вас.'
  perkStrengthMastery    = 5, 'Crippling Blows: a club or sledgehammer power attack breaks the legs: the zombie crawls.', 'Калечащие удары: силовая атака дубинкой или кувалдой ломает ноги — зомби ползёт.'
  perkMiner69r           = 5, 'Tunnel Rat: mining tools cost 50% less stamina.', 'Туннельная крыса: инструменты добычи тратят на 50% меньше выносливости.'
  perkMotherLode         = 5, 'Jackpot: a hit on ore or wood has a 5% chance to make the next 2 s of mining yield x5.', 'Джекпот: удар по руде или дереву с шансом 5% даёт x5 добычи на 2 с.'
  perkJunkMiner          = 5, 'Scrap King: +50% more from junk piles and salvage.', 'Король хлама: +50% добычи с куч хлама и разборки.'
  perkTheHuntsman        = 5, 'Butcher: harvesting a carcass heals 30 HP (once per 5 s).', 'Мясник: разделка туши лечит 30 HP (раз в 5 с).'
  perkPainTolerance      = 5, 'Unbreakable: below 25% health you take 50% less damage.', 'Несгибаемый: при здоровье ниже 25% вы получаете на 50% меньше урона.'
  perkFortitudeMastery   = 5, 'Second Wind: once per 5 minutes, dropping below 20% health instantly heals 70% of your max health.', 'Второе дыхание: раз в 5 минут, когда здоровье падает ниже 20%, вы сразу восстанавливаете 70% максимума.'
  perkSiphoningStrikes   = 5, 'Vampire: melee kills also restore 20 stamina.', 'Вампир: убийства в ближнем бою ещё и восстанавливают 20 выносливости.'
  perkHealingFactor      = 5, 'Regeneration: after 10 s without hitting or being hit you regenerate 3 HP/s.', 'Регенерация: через 10 с без ударов (ваших и по вам) вы восстанавливаете 3 HP/с.'
  perkSlowMetabolism     = 5, 'Iron Stomach: no food poisoning or dysentery (murky water is safe), and food and water never drop below 25%.', 'Железный желудок: никаких отравлений и дизентерии (мутная вода безопасна), а еда и вода не падают ниже 25%.'
  perkRuleOneCardio      = 5, 'Second Lungs: when your stamina runs out you instantly get 50% back (once per 60 s).', 'Второе дыхание лёгких: когда выносливость кончается, вы сразу получаете 50% обратно (раз в 60 с).'
  perkRunAndGun          = 5, 'Gun Kata: shooting on the move: no hip spread and +30% damage.', 'Ган-ката: стрельба на ходу без разброса от бедра и с +30% урона.'
  perkHardTarget         = 5, 'Blur: while running you take 50% less damage.', 'Размытие: на бегу вы получаете на 50% меньше урона.'
  perkParkour            = 5, 'Cat Landing: 99% less fall damage.', 'Кошачье приземление: на 99% меньше урона от падения.'
  perkAgilityMastery     = 5, 'Evasion: after taking a hit you move 30% faster for 5 s.', 'Уклонение: после полученного удара вы двигаетесь на 30% быстрее 5 с.'
  perkHiddenStrike       = 5, 'Assassinate: a sneak attack kills any non-elite zombie.', 'Убийство из тени: скрытная атака убивает любого не-элитного зомби.'
  perkFromTheShadows     = 5, 'Vanish: zombies that lost you give up the search almost at once.', 'Исчезновение: зомби, потерявшие вас, почти сразу прекращают поиск.'
  perkElectrocutioner    = 5, 'Overcharge: you deal +25% damage (any weapon) to shocked zombies.', 'Перегрузка: вы наносите +25% урона (любым оружием) шокированным зомби.'
  perkBetterBarter       = 5, 'Black Market: every trader restock has a 25% chance to include a legendary weapon.', 'Чёрный рынок: при каждом обновлении товара торговца с шансом 25% появляется легендарное оружие.'
  perkDaringAdventurer   = 5, 'Big Score: the weapon reward of a tier 5+ quest is always quality 6 with 3 affixes.', 'Крупный куш: оружие в награду за квест 5+ уровня всегда 6 качества с 3 аффиксами.'
  perkCharismaticNature  = 5, 'Leader: your own Charismatic Nature bonuses count twice for you.', 'Лидер: бонусы вашей «Харизмы» действуют на вас вдвойне.'
  perkPhysician          = 5, 'Field Medic: all healing +50%; healing with a medical item also stops bleeding.', 'Полевой медик: всё лечение +50%; лечение медицинским предметом ещё и останавливает кровотечение.'
  perkAdvancedEngineering = 5, 'Overclock: your traps and turret blocks deal +50% damage.', 'Разгон: ваши ловушки и турели-блоки наносят +50% урона.'
  perkGreaseMonkey       = 5, 'Mechanic: vehicles use 50% less fuel.', 'Механик: транспорт тратит на 50% меньше топлива.'
  perkLightArmor         = 4, 'Nimble: +15% move speed.', 'Проворство: +15% скорости передвижения.'
  perkMediumArmor        = 4, 'Balanced: attacks cost 20% less stamina.', 'Баланс: атаки тратят на 20% меньше выносливости.'
  perkHeavyArmor         = 4, 'Fortress: you cannot be stunned or knocked down.', 'Крепость: вас нельзя оглушить или сбить с ног.'
  perkMasterChef         = 3, 'Gourmet: food and drink buffs last 3 times longer.', 'Гурман: эффекты еды и напитков длятся втрое дольше.'
  perkLivingOffTheLand   = 3, 'Green Thumb: a harvested crop gives its seed back with a 50% chance.', 'Зелёные руки: собранный урожай с шансом 50% возвращает семя.'
  perkLockPicking        = 3, 'Safecracker: lockpicks almost never break (-90%).', 'Медвежатник: отмычки почти не ломаются (-90%).'
  perkLuckyLooter        = 5, 'Fortune: weapons you loot get +1 affix (within the quality limit).', 'Фортуна: найденное вами оружие получает +1 аффикс (в пределах лимита качества).'
}
[xml]$progXml = Get-Content (Join-Path $cfg 'progression.xml') -Raw -Encoding UTF8
foreach ($p in $perkHighlights.Keys) {
  $h = $perkHighlights[$p]
  # the long description key of that rank as progression.xml names it (some perks do not follow <perk>RankNLongDesc)
  $key = ($progXml.SelectSingleNode("//perk[@name='$p']//effect_description[@level='$($h[0])']")).long_desc_key
  if (-not $key) { $key = "${p}Rank$($h[0])LongDesc" }
  if (-not $loc[$key]) { throw "No vanilla text for $key" }
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod — Level ' + $h[0] + ':[-] ' + $h[1]), ($locRu[$key] + '\n\n[FFB400]AposMod — Уровень ' + $h[0] + ':[-] ' + $h[2])
}
# Attribute level 10 highlights
$attrHighlights = [ordered]@{
  attPerception = 'Executioner: Perception weapons (rifles, spears, explosives) deal +100% damage to targets below 30% health.', 'Палач: оружие Восприятия (винтовки, копья, взрывчатка) наносит +100% урона целям, у которых меньше 30% здоровья.'
  attStrength   = 'Overkill: damage beyond a killing blow with a Strength weapon (shotguns, clubs, sledgehammers) hits the nearest zombie within 4 m.', 'Перебор: лишний урон от добивающего удара оружием Силы (дробовики, дубинки, кувалды) переходит на ближайшего зомби в радиусе 4 м.'
  attFortitude  = 'Iron Body: you cannot bleed or get infected, and an existing infection is cured.', 'Железное тело: вы не кровоточите и не заражаетесь, а текущая инфекция излечивается.'
  attAgility    = 'Adrenaline: kills with Agility weapons (pistols, bows, knives) give +10% move speed for 15 s, up to 3 stacks.', 'Адреналин: убийства оружием Ловкости (пистолеты, луки, ножи) дают +10% скорости передвижения на 15 с, до 3 стаков.'
  attIntellect  = 'EMP: every kill gives a charge; at 10 charges your next hit releases an EMP: 200 damage and a 4 s shock to every zombie within 8 m of you.', 'ЭМИ: каждое убийство даёт заряд; на 10 зарядах следующий удар выпускает ЭМИ: 200 урона и шок на 4 с всем зомби в радиусе 8 м от вас.'
}
foreach ($a in $attrHighlights.Keys) {
  $key = "${a}Rank10LongDesc"
  if (-not $loc[$key]) { throw "No vanilla text for $key" }
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod — Level 10:[-] ' + $attrHighlights[$a][0]), ($locRu[$key] + '\n\n[FFB400]AposMod — Уровень 10:[-] ' + $attrHighlights[$a][1])
}

# Affix names for the item tooltip (Source/AposAffixes.cs: "aposAffix" + key, followed by the value)
# Plain stat names (no flavour names: the tooltip line reads "Damage +35%")
$affixNames = [ordered]@{
  Rapid = 'Fire rate', 'Скорострельность'
  Precise = 'Spread', 'Разброс'
  LongShot = 'Range', 'Дальность'
  Brutal = 'Damage', 'Урон'
  Quickload = 'Reload speed', 'Скорость перезарядки'
  Scatter = 'Pellets', 'Дробины'
  Twin = 'Second bullet', 'Вторая пуля'
  TwinDesc = '2 bullets per shot, each -40% damage', '2 пули за выстрел, каждая -40% урона'
  Deadly = 'Headshot damage', 'Урон в голову'
  Extended = 'Magazine size', 'Размер магазина'
  Swift = 'Move speed', 'Скорость передвижения'
  Steady = 'Recoil', 'Отдача'
  Fury = 'Attack speed', 'Скорость атаки'
  Tireless = 'Stamina per attack', 'Выносливость за удар'
  Reach = 'Attack range', 'Дальность удара'
  Leech = 'Life steal (share of damage)', 'Лечение от урона'
  Butcher = 'Dismember chance', 'Шанс отрубить конечность'
  Durable = 'Durability', 'Прочность'
  Wrecker = 'Block damage', 'Урон по блокам'
}
foreach ($k in $affixNames.Keys) { $extra["aposAffix$k"] = $affixNames[$k] }

# Legendaries: always quality 6 + fixed affixes (values as in Source/gen_legend.ps1)
$legendAffixes = [ordered]@{
  aposLegendHarold = @(@('Brutal', '+35%'), @('Rapid', '+25%'), @('Extended', '+60%'))
  aposLegendLadyFist = @(@('Deadly', '+60%'), @('Precise', '-40%'), @('Quickload', '+40%'))
  aposLegendDragonBreath = @(@('Scatter', '+35%'), @('Brutal', '+35%'), @('Quickload', '+40%'))
  aposLegendRipper = @(@('Brutal', '+35%'), @('Fury', '+20%'), @('Leech', '6%'))
  aposLegendThunderclap = @(@('Brutal', '+35%'), @('Tireless', '-40%'), @('Deadly', '+60%'))
  aposLegendHornet = @(@('Extended', '+60%'), @('Rapid', '+25%'), @('Steady', '-50%'))
  aposLegendStormFist = @(@('Fury', '+20%'), @('Brutal', '+35%'), @('Swift', '+10%'))
  aposLegendArtemis = @(@('Brutal', '+35%'), @('Deadly', '+60%'), @('Quickload', '+40%'))
  aposLegendBigBoom = @(@('Brutal', '+35%'), @('Quickload', '+40%'), @('LongShot', '+60%'))
  aposLegendPredator = @(@('Deadly', '+60%'), @('LongShot', '+60%'), @('Steady', '-50%'))
}
foreach ($k in $legendAffixes.Keys) {
  $en = ($legendAffixes[$k] | ForEach-Object { $affixNames[$_[0]][0] + ' ' + $_[1] }) -join ', '
  $ru = ($legendAffixes[$k] | ForEach-Object { $affixNames[$_[0]][1] + ' ' + $_[1] }) -join ', '
  $d = $extra["${k}Desc"]
  $extra["${k}Desc"] = ($d[0] + '\n[FFD24D]Always quality 6. Bonuses: ' + $en + '.[-]'), ($d[1] + '\n[FFD24D]Всегда 6 качества. Бонусы: ' + $ru + '.[-]')
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

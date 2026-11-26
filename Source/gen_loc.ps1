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
  knife  = T 'Bloodsucker' 'Every hit adds a regeneration stack (power attack: +2): +1 HP/s per stack for 5 s, max 2. A power attack makes the zombie bleed. Upgrades: the knife mastery tree (Alt+K).'
  club   = T 'Home Run' 'Normal hit: 15% chance to stun for 1 s. Power attack to the head: launches the zombie and knocks it down. Upgrades: the club mastery tree (Alt+K).'
  sledge = T 'Earthquake' 'Normal hit: 20% chance to knock the target down. Power attack: knocks down every zombie within 3 m and deals each 10% of the hit (10 s cooldown). Upgrades: the sledgehammer mastery tree (Alt+K).'
  baton  = T 'Chain Lightning' 'A normal hit shocks the target for 0.5 s; a power hit shocks and stuns every zombie within 3 m for 2 s (10 s cooldown). Upgrades: the baton mastery tree (Alt+K).'
  spear  = T 'Battering Ram' 'A normal hit pushes the target back, a power hit goes through one zombie behind and throws each one back. Upgrades: the spear mastery tree (Alt+K).'
  knuck  = T 'Counter Punch' 'Every hit you take gives a charge (max 2). A normal hit spends 1 (+25%), a power hit spends all (+50% each) and knocks down. Upgrades: the knuckle mastery tree (Alt+K).'
  pipe   = T 'Veteran Junk' 'Every kill with a pipe weapon: +2% damage for all pipe weapons (max +1000%).\nRepair with Scrap Iron.'
  slug   = T 'Slug Knockback' 'Slug and Breaching Slug rounds throw zombies back and knock them down.\nBuckshot does the same point blank (within 3 m). Upgrades: the shotgun mastery tree (Alt+K).'
  nade   = T 'Wide Blast' 'Blast radius against zombies x3. Block damage radius is unchanged.'
  molo   = T 'Busy Burning' 'Burning zombies move 70% slower and lose 4% of their max health every second (at least 15), on top of the normal fire. The fire lasts twice as long.'
  rocket = T 'Early Artillery' 'Sold by traders and found in loot from the start of the game.\nThe final loot chest of every POI contains rockets: 1 at tier 1 up to 4-6 at tier 5.'
  pistol = T 'Headhunter' '5 headshots in a row start Headhunter: the next 10 shots (+2 per Gunslinger level) deal +25% damage (+10% per level) with almost no spread.'
  rifle  = T 'Ricochet' 'A headshot hurts up to 3 zombies near the hit (5 m) for 5% of the headshot, at least 20. Upgrades: the rifle mastery tree (Alt+K).'
  mg     = T 'Suppression' 'Every hit slows the target by 5% for 8 s, up to 20%; every 4th hit puts a round back into the magazine. Upgrades: the machine gun mastery tree (Alt+K).'
  bow    = T 'Splinter' 'On impact the arrow bursts into 2 splinters that hit the nearest other zombies within 5 m for 50% of the arrow (at least 40). Upgrades: the bow mastery tree (Alt+K).'
  xbow   = T 'Mark' 'A bolt marks the zombie for 10 s: all your damage to it (any weapon) +20%. Upgrades: the crossbow mastery tree (Alt+K).'
  robot  = T 'Guardian Protocol' 'While your health is below 50%, your turrets and robotic sledges attack 30% faster and deal 10% more damage (+10% speed and +5% damage per Robotics Inventor level; at level 5 below 70% health: +80% / +35%).'}
$traitsRu = @{
  knife  = T 'Кровопийца' 'Каждый удар добавляет стак регенерации (силовой: +2): +1 HP/с за стак на 5 с, максимум 2. Силовой удар вызывает кровотечение. Усиления — в дереве мастерства ножа (Alt+K).'
  club   = T 'Хоумран' 'Обычный удар: 15% шанс оглушить на 1 с. Силовой удар в голову: подбрасывает зомби и сбивает с ног. Усиления — в дереве мастерства дубинки (Alt+K).'
  sledge = T 'Землетрясение' 'Обычный удар: 20% шанс сбить цель с ног. Силовой удар: сбивает всех зомби в 3 м и наносит каждому 10% удара (перезарядка 10 с). Усиления — в дереве мастерства кувалды (Alt+K).'
  baton  = T 'Цепная молния' 'Обычный удар шокирует цель на 0.5 с; силовой шокирует и оглушает всех зомби в 3 м на 2 с (перезарядка 10 с). Усиления — в дереве мастерства жезла (Alt+K).'
  spear  = T 'Таран' 'Обычный удар отталкивает цель, силовой пробивает одного зомби позади и отбрасывает каждого. Усиления — в дереве мастерства копья (Alt+K).'
  knuck  = T 'Контрудар' 'Каждый полученный удар даёт заряд (максимум 2). Обычный удар тратит 1 (+25%), силовой — все (+50% за каждый) и сбивает с ног. Усиления — в дереве мастерства кастетов (Alt+K).'
  pipe   = T 'Ветеран-самоделка' 'Каждое убийство самодельным оружием: +2% урона всему самодельному оружию (максимум +1000%).\nЧинится металлоломом.'
  slug   = T 'Отбрасывание пулей' 'Пули (Slug и Breaching Slug) отбрасывают зомби и сбивают с ног.\nДробь делает то же в упор (до 3 м). Усиления — в дереве мастерства дробовика (Alt+K).'
  nade   = T 'Широкий взрыв' 'Радиус поражения зомби x3. Радиус разрушения блоков без изменений.'
  molo   = T 'Заняты горением' 'Горящие зомби двигаются на 70% медленнее и каждую секунду теряют 4% максимального здоровья (не меньше 15) сверх обычного огня. Огонь горит вдвое дольше.'
  rocket = T 'Ранняя артиллерия' 'Продаётся у торговцев и встречается в луте с начала игры.\nВ финальном сундуке каждого здания есть ракеты: 1 на уровне 1, до 4-6 на уровне 5.'
  pistol = T 'Охотник за головами' '5 хедшотов подряд включают Охотника за головами: следующие 10 выстрелов (+2 за уровень «Меткого стрелка») наносят +25% урона (+10% за уровень) почти без разброса.'
  rifle  = T 'Рикошет' 'Выстрел в голову ранит до 3 зомби рядом (5 м) на 5% урона выстрела, минимум 20. Усиления — в дереве мастерства винтовки (Alt+K).'
  mg     = T 'Подавление' 'Каждое попадание замедляет цель на 5% на 8 с, до 20%; каждое 4-е попадание возвращает патрон в магазин. Усиления — в дереве мастерства автомата (Alt+K).'
  bow    = T 'Расщепление' 'При попадании стрела разлетается на 2 осколка, которые бьют ближайших других зомби в 5 м на 50% урона стрелы (минимум 40). Усиления — в дереве мастерства лука (Alt+K).'
  xbow   = T 'Метка' 'Болт помечает зомби на 10 с: весь ваш урон по нему (любым оружием) +20%. Усиления — в дереве мастерства арбалета (Alt+K).'
  robot  = T 'Протокол защиты' 'Пока ваше здоровье ниже 50%, ваши турели и робо-кувалды атакуют на 30% быстрее и наносят на 10% больше урона (+10% скорости и +5% урона за уровень «Изобретателя роботов»; на 5 уровне при здоровье ниже 70%: +80% / +35%).'}
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
  buffAposRicochetCDName  = 'Ricochet stun: cooldown', 'Оглушение рикошетом: перезарядка'
  buffAposRicochetCDDesc  = 'The Ricochet stun is recharging. Ricochet damage still works.', 'Оглушение Рикошета перезаряжается. Урон Рикошета работает.'
  buffAposGuardianName    = 'Guardian Protocol', 'Протокол защиты'
  buffAposGuardianDesc    = 'Your health is low: your robots attack faster and harder.', 'Ваше здоровье низкое: ваши роботы атакуют быстрее и сильнее.'
  modArmorAposEnergyShield     = 'Energy Shield Mod', 'Мод: энергощит'
  modArmorAposEnergyShieldDesc = '[FFB400]AposMod: Energy Shield[-]\nChest armor mod. While charged, ranged attacks (bullets, arrows, spit) deal no damage to you.\nAny hit drains the shield; it recharges after 5 s without taking damage.\nCraft at a workbench or find in loot.', '[FFB400]AposMod: Энергощит[-]\nМод для нагрудника. Пока заряжен, дальние атаки (пули, стрелы, плевки) не наносят вам урона.\nЛюбое попадание разряжает щит; он заряжается через 5 с без получения урона.\nСоздаётся на верстаке или находится в луте.'
  buffAposShieldUpName    = 'Energy Shield', 'Энергощит'
  buffAposShieldUpDesc    = 'Charged: ranged attacks deal no damage. Any hit drains it.', 'Заряжен: дальние атаки не наносят урона. Любое попадание разряжает его.'
  modWeaponAposFire       = 'Incendiary Mod', 'Мод: зажигательный'
  modWeaponAposFireDesc   = '[FFB400]AposMod: Fire[-]\nAny weapon. Hits have a 30% chance to set the target on fire for 10 s: every second it loses 10% of your last hit (at least 10).\nOnly one elemental mod per weapon.\n[FF3DF2]Reactions[-] (two elements on one zombie): fire + acid = Explosion (50% of your last hit, at least 60, within 4 m), fire + shock = Confusion, shock + acid = Miasma.', '[FFB400]AposMod: Огонь[-]\nЛюбое оружие. Попадания с шансом 30% поджигают цель на 10 с: каждую секунду она теряет 10% урона вашего последнего удара (не меньше 10).\nНа оружие — только один стихийный мод.\n[FF3DF2]Реакции[-] (две стихии на одном зомби): огонь + кислота = Взрыв (50% урона последнего удара, не меньше 60, в радиусе 4 м), огонь + ток = Замешательство, ток + кислота = Миазмы.'
  modWeaponAposShock      = 'Tesla Mod', 'Мод: тесла'
  modWeaponAposShockDesc  = '[FFB400]AposMod: Shock[-]\nAny weapon except batons (they shock anyway). Hits have a 20% chance to shock the target for 1.5 s: every second 15% of your last hit (at least 10).\nOnly one elemental mod per weapon.\n[FF3DF2]Reactions[-] (two elements on one zombie): fire + acid = Explosion (50% of your last hit, at least 60, within 4 m), fire + shock = Confusion, shock + acid = Miasma.', '[FFB400]AposMod: Ток[-]\nЛюбое оружие, кроме жезлов (они и так шокируют). Попадания с шансом 20% шокируют цель на 1.5 с: каждую секунду 15% урона вашего последнего удара (не меньше 10).\nНа оружие — только один стихийный мод.\n[FF3DF2]Реакции[-] (две стихии на одном зомби): огонь + кислота = Взрыв (50% урона последнего удара, не меньше 60, в радиусе 4 м), огонь + ток = Замешательство, ток + кислота = Миазмы.'
  modWeaponAposAcid       = 'Corrosive Mod', 'Мод: кислотный'
  modWeaponAposAcidDesc   = '[FFB400]AposMod: Acid[-]\nAny weapon. Hits have a 25% chance to corrode the target for 6 s: it takes more physical damage and every second loses 8% of your last hit (at least 8).\nOnly one elemental mod per weapon.\n[FF3DF2]Reactions[-] (two elements on one zombie): fire + acid = Explosion (50% of your last hit, at least 60, within 4 m), fire + shock = Confusion, shock + acid = Miasma.', '[FFB400]AposMod: Кислота[-]\nЛюбое оружие. Попадания с шансом 25% разъедают цель на 6 с: она получает больше физического урона и каждую секунду теряет 8% урона вашего последнего удара (не меньше 8).\nНа оружие — только один стихийный мод.\n[FF3DF2]Реакции[-] (две стихии на одном зомби): огонь + кислота = Взрыв (50% урона последнего удара, не меньше 60, в радиусе 4 м), огонь + ток = Замешательство, ток + кислота = Миазмы.'
  aposLegendHarold           = '[FF9900]Unkempt Harold[-]', '[FF9900]Растрёпанный Гарольд[-]'
  aposLegendHaroldDesc       = '[FF9900]Legendary .44 Desert Vulture.[-]\nEvery shot fires 3 bullets in a wide fan. Why aim when you can do everything at once?', '[FF9900]Легендарный .44 «Пустынный стервятник».[-]\nКаждый выстрел — 3 пули широким веером. Зачем целиться, если можно всё и сразу?'
  aposLegendLadyFist         = '[FF9900]Lady Fist[-]', '[FF9900]Леди Кулак[-]'
  aposLegendLadyFistDesc     = '[FF9900]Legendary 9mm pistol.[-]\n+50% damage. Every hit heals you for 5 HP, every headshot for 25 HP.', '[FF9900]Легендарный 9-мм пистолет.[-]\n+50% урона. Каждое попадание лечит вас на 5 HP, каждый хедшот - на 25 HP.'
  aposLegendDragonBreath     = '[FF9900]Dragon''s Breath[-]', '[FF9900]Дыхание дракона[-]'
  aposLegendDragonBreathDesc = '[FF9900]Legendary pump shotgun.[-]\n+20% damage. Every hit sets the target on fire.', '[FF9900]Легендарный помповый дробовик.[-]\n+20% урона. Каждое попадание поджигает цель.'
  aposLegendRipper           = '[FF9900]The Ripper[-]', '[FF9900]Потрошитель[-]'
  aposLegendRipperDesc       = '[FF9900]Legendary machete.[-]\n+40% damage, every hit causes bleeding, +50% more against bleeding targets. Every kill heals 20 HP.', '[FF9900]Легендарное мачете.[-]\n+40% урона, каждый удар вызывает кровотечение, по кровоточащим ещё +50%. Каждое убийство лечит 20 HP.'
  aposLegendThunderclap      = '[FF9900]Thunderclap[-]', '[FF9900]Раскат грома[-]'
  aposLegendThunderclapDesc  = '[FF9900]Legendary steel sledgehammer.[-]\n+25% damage, power attacks shock zombies within 4 m.\nStorm charges: every kill gives a charge; at 3 charges the next power attack calls lightning: 150 damage and a 3 s shock to all zombies within 6 m.', '[FF9900]Легендарная стальная кувалда.[-]\n+25% урона, силовые атаки шокируют зомби в радиусе 4 м.\nГрозовые заряды: каждое убийство даёт заряд; на 3 зарядах следующая силовая атака вызывает молнию: 150 урона и шок 3 с всем зомби в радиусе 6 м.'
  aposLegendHornet           = '[FF9900]Hornet[-]', '[FF9900]Шершень[-]'
  aposLegendHornetDesc       = '[FF9900]Legendary SMG-5.[-]\n+40% damage. Hits have a 50% chance to corrode the target (acid: fuel for reactions), and you deal +25% more to corroded targets.', '[FF9900]Легендарный SMG-5.[-]\n+40% урона. Попадания с шансом 50% разъедают цель кислотой (топливо для реакций), а по разъеденным целям вы наносите ещё +25%.'
  aposLegendStormFist        = '[FF9900]Storm Fist[-]', '[FF9900]Кулак бури[-]'
  aposLegendStormFistDesc    = '[FF9900]Legendary steel knuckles.[-]\n+20% damage. Every punch has a 25% chance to ignite and a 25% chance to shock: Confusion comes by itself.', '[FF9900]Легендарный стальной кастет.[-]\n+20% урона. Каждый удар с шансом 25% поджигает и с шансом 25% шокирует: Замешательство приходит само.'
  aposLegendArtemis          = '[FF9900]Artemis[-]', '[FF9900]Артемида[-]'
  aposLegendArtemisDesc      = '[FF9900]Legendary compound bow.[-]\n+40% damage. Every arrow sets the target on fire and bursts into 10 fragments of 100 HP (or half the arrow''s hit), at any Archery level.', '[FF9900]Легендарный блочный лук.[-]\n+40% урона. Каждая стрела поджигает цель и разлетается на 10 осколков по 100 HP (или половине урона стрелы) при любом уровне «Стрельбы из лука».'
  aposLegendBigBoom          = '[FF9900]Big Boom[-]', '[FF9900]Большой бабах[-]'
  aposLegendBigBoomDesc      = '[FF9900]Legendary rocket launcher.[-]\nAfter every shot there is a 50% chance that the next rocket costs nothing.', '[FF9900]Легендарная ракетница.[-]\nПосле каждого выстрела с шансом 50% следующая ракета бесплатна.'
  aposLegendPredator         = '[FF9900]Predator[-]', '[FF9900]Хищник[-]'
  aposLegendPredatorDesc     = '[FF9900]Legendary sniper rifle.[-]\n+25% damage. Every headshot ricochets: 100 damage and a 2 s stun to all zombies within 15 m of the hit, at any Dead Eye level.', '[FF9900]Легендарная снайперская винтовка.[-]\n+25% урона. Каждый хедшот рикошетит: 100 урона и оглушение 2 с всем зомби в радиусе 15 м от попадания при любом уровне «Безжалостного снайпера».'
  buffAposThunderChargeName  = 'Storm Charges', 'Грозовые заряды'
  buffAposThunderChargeDesc  = 'Thunderclap: at 3 charges the next power attack calls lightning.', 'Раскат грома: на 3 зарядах следующая силовая атака вызывает молнию.'
  buffAposFreeRocketName     = 'Free Rocket', 'Бесплатная ракета'
  buffAposFreeRocketDesc     = 'Big Boom: the next rocket costs no ammo.', 'Большой бабах: следующая ракета не тратит боеприпас.'
  # 1.1 elites and Eridium (Source/AposElites.cs)
  aposEliteArmored           = 'Armored', 'Бронированный'
  aposEliteNear              = 'ELITE NEARBY: {0}', 'ЭЛИТА РЯДОМ: {0}'
  aposEliteNearMany          = 'ELITES NEARBY: {0}', 'ЭЛИТЫ РЯДОМ: {0}'
  aposEliteStarsN            = '{0} stars', 'звёзд: {0}'
  aposEliteTipArmored        = 'aim for the head: body hits deal half damage', 'цельтесь в голову: по телу урон вдвое меньше'
  aposEliteTipFast           = 'runs fast, you will not outrun it on foot', 'бегает быстро, пешком не убежать'
  aposEliteTipRegen          = 'heals {0}% a second after 3 s without damage: do not give it a break', 'лечит {0}% в секунду после 3 с без урона: не давайте передышки'
  aposEliteTipExplosive      = 'kill it from afar: it explodes on death for {0} damage within {1} m', 'добивайте издалека: при смерти взрыв {0} урона в {1} м'
  aposEliteTipVampire        = 'heals from the damage it deals to you: do not let it close', 'лечится вашим уроном: не подпускайте близко'
  aposEliteTipBurning        = 'sets you on fire with every hit, fire does not hurt it', 'поджигает каждым ударом, огонь его не берёт'
  aposEliteTipElectric       = 'shocks you with every hit, shock does not affect it', 'бьёт током каждым ударом, шок его не берёт'
  aposEliteTipAcidic         = 'every hit eats your armor (-20)', 'каждый удар разъедает броню (-20)'
  aposEliteTipGiant          = 'giant: +50% health and damage', 'великан: +50% здоровья и урона'
  aposEliteTipSummoner       = 'at half health calls 3 zombies: finish it fast', 'на половине здоровья зовёт 3 зомби: добивайте быстро'
  aposEliteFast              = 'Fast', 'Быстрый'
  aposEliteRegen             = 'Regenerating', 'Регенерирующий'
  aposEliteExplosive         = 'Explosive', 'Взрывной'
  aposEliteVampire           = 'Vampiric', 'Вампирский'
  aposEliteBurning           = 'Burning', 'Пылающий'
  aposEliteElectric          = 'Electric', 'Электрический'
  aposEliteAcidic            = 'Acidic', 'Кислотный'
  aposEliteGiant             = 'Giant', 'Гигантский'
  aposEliteSummoner          = 'Summoner', 'Призывающий'
  resourceAposEridium        = 'Eridium', 'Эридий'
  resourceAposEridiumDesc    = '[BE5AFF]AposMod: Eridium[-]\nA strange purple crystal. Drops from elite zombies (names above their heads) and comes out of scrapping weapons with affixes (2 per affix, x2 at quality 6, a legendary gives 50).\nSpend it at the Eridium Forge: reroll, lock and add affixes, raise quality, buy legendaries.', '[BE5AFF]AposMod: Эридий[-]\nСтранный фиолетовый кристалл. Падает с элитных зомби (имя над головой) и получается при разборке оружия с аффиксами (2 за аффикс, x2 на 6 качестве, легендарка даёт 50).\nТратится в Эридиевой кузне: перебор, фиксация и добавление аффиксов, повышение качества, покупка легендарок.'
  ammoArrowAposAcid          = 'Acid Arrow', 'Кислотная стрела'
  ammoArrowAposAcidDesc      = '[78FF3C]AposMod: Acid Arrow[-]\nCorrodes the zombie (-25% physical resistance, acid damage every second) and slows it by 90% for 5-10 s (longer with Archery: 5 s at 0-1, 6/7/8 s at 2/3/4, 10 s at 5).\nAcid also starts reactions with fire and shock.', '[78FF3C]AposMod: Кислотная стрела[-]\nРазъедает зомби (-25% физической защиты, урон кислотой каждую секунду) и замедляет его на 90% на 5-10 с (дольше с Archery: 5 с на 0-1, 6/7/8 с на 2/3/4, 10 с на 5).\nКислота запускает реакции с огнём и током.'
  ammoArrowAposShock         = 'Electric Arrow', 'Электрическая стрела'
  ammoArrowAposShockDesc     = '[508CFF]AposMod: Electric Arrow[-]\nShocks the zombie for 4 s: electric damage every second that grows with your hit.\nShock starts reactions with fire and acid.', '[508CFF]AposMod: Электрическая стрела[-]\nБьёт током на 4 с: урон током каждую секунду, растёт от силы вашего попадания.\nТок запускает реакции с огнём и кислотой.'
  ammoArrowAposBleed         = 'Serrated Arrow', 'Зазубренная стрела'
  ammoArrowAposBleedDesc     = '[E6283C]AposMod: Serrated Arrow[-]\nDeep bleeding for 10 s: 15% of your hit every second (at least 12), on top of normal bleeding - about 1.5 hits more in total.\nShoot, then keep your distance.', '[E6283C]AposMod: Зазубренная стрела[-]\nГлубокое кровотечение на 10 с: 15% от вашего попадания каждую секунду (не меньше 12), плюс обычное кровотечение - в сумме ещё примерно 1.5 попадания.\nВыстрелите и держите дистанцию.'
  aposEridiumForge           = 'Eridium Forge', 'Эридиевая кузня'
  aposEridiumForgeDesc       = '[BE5AFF]AposMod: Eridium Forge[-]\nA workstation for Eridium. Take a weapon in your hands and open the forge: reroll any affix (10), lock one so it never changes (25, unlocking is free), add an affix to an empty row (20), sharpen weapons and worn apparel one quality at a time up to 10 (Eridium + forged steel; up to 6 it always works, 7-10 can fail and a failure breaks the item; Safe costs 5x and only drops one quality, not below 6), scrap into Eridium, buy any legendary (300, quality 6).', '[BE5AFF]AposMod: Эридиевая кузня[-]\nВерстак для Эридия. Возьмите оружие в руки и откройте кузню: перебор любого аффикса (10), фиксация одного, чтобы он не менялся (25, снять — бесплатно), новый аффикс в пустую строку (20), заточка оружия и надетой одежды по одному качеству до 10 (эридий + кованая сталь; до 6 всегда успешно, 7-10 может не получиться, и тогда предмет ломается; «Безопасно» в 5 раз дороже и при неудаче лишь снижает качество на 1, не ниже 6), разборка на эридий, покупка любой легендарки (300, качество 6).'
  aposForgeActivate          = 'Press [E] to use the Eridium Forge', 'Нажмите [E], чтобы открыть Эридиевую кузню'
  aposForgeTitle             = 'ERIDIUM FORGE', 'ЭРИДИЕВАЯ КУЗНЯ'
  aposForgeEridium           = 'Eridium: [BE5AFF]{0}[-]     Forged steel: [B0B0B0]{1}[-]     [808080](sharpening costs both)[-]', 'Эридий: [BE5AFF]{0}[-]     Кованая сталь: [B0B0B0]{1}[-]     [808080](заточка стоит и то, и другое)[-]'
  aposForgeMissing           = 'Missing: {0}', 'Не хватает: {0}'
  aposForgeMissingEridium    = '{0} Eridium', '{0} эридия'
  aposForgeMissingSteel      = '{0} forged steel', '{0} кованой стали'
  aposForgeTabWeapon         = 'Weapon', 'Оружие'
  aposForgeTabApparel        = 'Apparel', 'Одежда'
  aposForgeTabLegends        = 'Legendaries', 'Легендарки'
  aposForgeNoApparel         = 'You wear nothing that can be sharpened', 'На вас нет вещей для заточки'
  aposForgeNothing           = 'nothing to sharpen', 'нечего точить'
  aposForgeSharpen           = 'Sharpen', 'Заточить'
  aposForgeSharpenSafe       = 'Safe', 'Безопасно'
  aposForgeSharpenSure       = 'Sharpen to quality {0}: always succeeds', 'Заточка до {0} качества: всегда успешно'
  aposForgeSharpenMax        = 'Maximum quality 10 reached', 'Достигнуто максимальное качество 10'
  aposForgeSharpenInfo       = 'Sharpen to quality {0}: success {1}%', 'Заточка до {0} качества: успех {1}%'
  aposForgeBreakInfo         = 'failure breaks the item (Safe: quality -1)', 'неудача ломает предмет («Безопасно»: качество -1)'
  aposForgeSharpening        = 'SHARPENING', 'ЗАТОЧКА'
  aposForgeSharpeningTo      = 'Sharpening to {0}...', 'Заточка до {0}...'
  aposForgeGain              = 'Level {0} gives:', 'Уровень {0} даст:'
  aposForgeGainShort         = 'at {0}:', 'на {0}:'
  aposForgeLegendHover       = 'Point at a legendary weapon to see what it does', 'Наведите на легендарку, чтобы увидеть, что она умеет'
  aposForgeGainDamage        = 'Damage', 'Урон'
  aposForgeGainBlock         = 'Block damage', 'Урон по блокам'
  aposForgeGainExplosion     = 'Explosion', 'Взрыв'
  aposForgeGainArmor         = 'Armor', 'Броня'
  aposForgeGainElemental     = 'Elemental', 'От стихий'
  aposForgeGainDurability    = 'Durability', 'Прочность'
  aposForgeGainModSlots      = 'Mod slots', 'Слоты модов'
  aposForgeGainMagazine      = 'Magazine', 'Магазин'
  aposForgeGainCold          = 'Cold', 'Холод'
  aposForgeGainHeat          = 'Heat', 'Жара'
  aposForgeSharpenOk         = 'SUCCESS! Quality {0}', 'УСПЕХ! Качество {0}'
  aposForgeSharpenFail       = 'Failure... quality dropped to {0}', 'Неудача... качество упало до {0}'
  aposForgeSharpenFailSafe   = 'Failure, the quality is kept', 'Неудача, качество сохранено'
  aposForgeSharpenBroke      = 'BROKEN! +{0} Eridium from the remains', 'СЛОМАНО! +{0} эридия из обломков'
  aposForgeNoWeapon          = 'Take a weapon in your hands (not a tool or a legendary)', 'Возьмите оружие в руки (не инструмент и не легендарку)'
  aposForgeQuality           = 'quality {0}', 'качество {0}'
  aposForgeEmpty             = 'empty row', 'пустая строка'
  aposForgeReroll            = 'Reroll', 'Перебрать'
  aposForgeAdd               = 'Add', 'Добавить'
  aposForgeLock              = 'Lock', 'Зафиксировать'
  aposForgeUnlock            = 'Unlock (free)', 'Снять фиксацию'
  aposForgeDoneUnlock        = 'Lock removed', 'Фиксация снята'
  aposForgeScrap             = 'Scrap: +{0} Eridium', 'Разобрать: +{0} эридия'
  aposForgeLegends           = 'Legendary weapons (300 Eridium, quality 6):', 'Легендарное оружие (300 эридия, качество 6):'
  aposForgeDoneReroll        = 'Affix changed', 'Аффикс изменён'
  aposForgeDoneLock          = 'Affix locked', 'Аффикс зафиксирован'
  aposForgeDoneScrap         = 'Weapon scrapped: +{0} Eridium', 'Оружие разобрано: +{0} эридия'
  aposForgeDoneLegend        = 'Legendary weapon added to your backpack', 'Легендарное оружие добавлено в рюкзак'
  aposAffixLocked            = 'locked', 'зафиксирован'
  aposCombineResult          = 'Result: {0}, quality {1}', 'Результат: {0}, качество {1}'
  aposCombineNoAffixes       = 'No affixes', 'Без аффиксов'
  aposCombineDropped         = 'Removed (quality {1} keeps {0}):', 'Будут убраны (на качестве {1} остаётся {0}):'
  buffAposAcidArmorName      = 'Corroded Armor', 'Разъеденная броня'
  buffAposAcidArmorDesc      = 'An acidic elite ate through your armor: -20 physical resistance.', 'Кислотная элита разъела броню: -20 к физической защите.'
  buffAposFrenzyName         = 'Frenzy', 'Неистовство'
  buffAposFrenzyDesc         = '+15% attack speed with these weapons.', '+15% скорости атаки этим оружием.'
  buffAposAdrenalineName     = 'Adrenaline', 'Адреналин'
  buffAposAdrenalineDesc     = '+10% move speed and +5% damage per stack. 15 s after the last kill.', '+10% скорости передвижения и +5% урона за стак. 15 с после последнего убийства.'
  buffAposEmpName            = 'Lightning Charges', 'Заряды молнии'
  buffAposEmpDesc            = 'At 10 charges your next hit fires a Charged Chain Lightning around you.', 'На 10 зарядах следующий удар выпускает Заряженную цепную молнию вокруг вас.'
  buffAposJackpotName        = 'Jackpot!', 'Джекпот!'
  buffAposJackpotDesc        = 'Mining yields x5.', 'Добыча x5.'
  buffAposEvasionName        = 'Evasion', 'Уклонение'
  buffAposEvasionDesc        = '+30% move speed.', '+30% скорости передвижения.'
  buffAposShadowName         = 'Shadow', 'Тень'
  buffAposShadowDesc         = 'Your next weapon hit deals x2. A kill with it gives the Shadow back. Lost when you stand up or a zombie notices you.', 'Следующий удар оружием наносит x2. Убийство им возвращает Тень. Пропадает, если встать или вас заметили.'
  buffAposSecondWindName     = 'Second Wind (recharging)', 'Второе дыхание (перезарядка)'
  buffAposSecondWindDesc     = 'Second Wind is ready again when this ends.', 'Второе дыхание снова готово, когда это закончится.'
  buffAposVampireName        = 'Vampire', 'Вампир'
  buffAposVampireDesc        = 'Bonus max health from melee kills (the number). A melee kill adds +50 and restarts the timer.', 'Бонус к максимуму здоровья за убийства в ближнем бою (число). Убийство добавляет +50 и перезапускает таймер.'
  buffAposSecondLungsName   = 'Second Lungs (recharging)', 'Второе дыхание лёгких (перезарядка)'
  buffAposSecondLungsDesc    = 'Second Lungs is ready again when this ends.', 'Второе дыхание лёгких снова готово, когда это закончится.'
  # Grand Slam is replaced by Rage (full texts)
  perkGrandSlamName       = 'Rage', 'Ярость'
  perkGrandSlamDesc       = 'Every kill with a club, bat or sledgehammer fuels your rage: more damage and faster attacks for a short time.', 'Каждое убийство дубинкой, битой или кувалдой разжигает ярость: больше урона и быстрее атаки на короткое время.'
  # Intellect Mastery 4-5 are replaced (full texts)
  perkIntellectMasteryRank4Desc     = 'Field Repair', 'Полевой ремонт'
  perkIntellectMasteryRank4LongDesc = '[FFB400]AposMod — Level 4: Field Repair[-]\nAll your weapons, tools and armor wear 30% slower.', '[FFB400]AposMod — Уровень 4: Полевой ремонт[-]\nВсё ваше оружие, инструменты и броня изнашиваются на 30% медленнее.'
  perkIntellectMasteryRank5Desc     = 'Inventor', 'Изобретатель'
  perkIntellectMasteryRank5LongDesc = '[FFB400]AposMod — Level 5: Inventor[-]\nWeapons you craft get random affixes, like weapons from loot of that quality. Everything you craft comes out one quality level above the quality you pick; quality 6 stays 6, and weapons, tools and armor become forge tier 7.\nField Repair stays: everything wears 30% slower.', '[FFB400]AposMod — Уровень 5: Изобретатель[-]\nСкрафченное оружие получает случайные аффиксы, как оружие из добычи того же качества. Всё, что вы создаёте, выходит на уровень качества выше выбранного; качество 6 остаётся 6, а оружие, инструменты и броня становятся 7-го уровня кузни.\nПолевой ремонт остаётся: всё изнашивается на 30% медленнее.'
}
for ($L = 1; $L -le 5; $L++) {
  $extra["perkGrandSlamRank${L}Desc"] = "Rage $L", "Ярость $L"
  $extra["perkGrandSlamRank${L}LongDesc"] = "Kills with clubs, bats or sledgehammers give a Rage stack for 8 s (max $L): +10% damage and +5% attack speed per stack with these weapons.", "Убийства дубинкой, битой или кувалдой дают стак Ярости на 8 с (максимум $L): +10% урона и +5% скорости атаки за стак с этим оружием."
}
$extra['perkGrandSlamRank5LongDesc'] = ($extra['perkGrandSlamRank5LongDesc'][0] + '\n[FFB400]Rampage:[-] at 5 stacks club and sledgehammer attacks cost no stamina and you cannot be stunned or knocked down.'), ($extra['perkGrandSlamRank5LongDesc'][1] + '\n[FFB400]Буйство:[-] на 5 стаках атаки дубинками и кувалдами не тратят выносливость, а вас нельзя оглушить или сбить с ног.')

# AposMod line appended to the vanilla text of every rank of the perks that scale our traits ($L = rank).
# "Replaces ..." notes tell the player which vanilla knockdown above no longer works (removed in progression.xml).
$perkLines = [ordered]@{
  perkTurrets           = { param($L) "Guardian Protocol (robots): below 50% health your robots attack $(30 + 10 * $L)% faster and deal $(10 + 5 * $L)% more damage.", "Протокол защиты (роботы): при здоровье ниже 50% ваши роботы атакуют на $(30 + 10 * $L)% быстрее и наносят на $(10 + 5 * $L)% больше урона." }}
# Rank 5 highlights, appended after the scaling line
$perkFinale = @{
  perkTurrets           = 'Guardian Protocol starts below 70% health (instead of 50%).', 'Протокол защиты включается при здоровье ниже 70% (вместо 50%).'}
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

# Mastery 2.0: Gunslinger is vanilla again without its "successive hits" crit (removed in progression.xml, ranks 3-5);
# Headhunter lives in the pistol mastery tree
function Remove-LastSentence($t) {
  # the texts hold a literal backslash-n (the game turns it into a line break)
  $parts = $t -split '\\n', 2
  $first = $parts[0]
  $i = $first.LastIndexOf('. ', $first.Length - 2)
  if ($i -lt 0) { throw "No sentence to drop in: $t" }
  $first.Substring(0, $i + 1) + '\n' + $parts[1]
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkBoomstickRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] the slug throw and its upgrades are in the shotgun mastery tree: Alt+K.'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] отброс пулей и его усиления теперь в дереве мастерства дробовика: Alt+K.')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkDeadEyeRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] the Ricochet and its upgrades are in the rifle mastery tree: Alt+K.'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] Рикошет и его усиления теперь в дереве мастерства винтовки: Alt+K.')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkMachineGunnerRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] Suppression and its upgrades are in the machine gun mastery tree: Alt+K.'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] Подавление и его усиления теперь в дереве мастерства автомата: Alt+K.')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkArcheryRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] Splinter (bows), the Mark (crossbows) and their upgrades are in the bow and crossbow mastery trees: Alt+K.'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] Расщепление (луки), Метка (арбалеты) и их усиления теперь в деревьях мастерства лука и арбалета: Alt+K.')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkDeepCutsRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] Bloodsucker and its upgrades are in the knife mastery tree: Alt+K. Normal knife hits no longer bleed by themselves (power hits do).'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] Кровопийца и его усиления теперь в дереве мастерства ножа: Alt+K. Обычные удары ножом сами больше не вызывают кровотечение (силовые — вызывают).')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkPummelPeteRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] Home Run and its upgrades are in the club mastery tree: Alt+K. The vanilla bonuses to stunned zombies and for successive hits are in that tree too.'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] Хоумран и его усиления теперь в дереве мастерства дубинки: Alt+K. Ванильные бонусы по оглушённым и за серию ударов — тоже в этом дереве.')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkSkullCrusherRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] The Earthquake and its upgrades are in the sledgehammer mastery tree: Alt+K.'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] Землетрясение и его усиления теперь в дереве мастерства кувалды: Alt+K.')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkJavelinMasterRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] Battering Ram and its upgrades are in the spear mastery tree: Alt+K.'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] Таран и его усиления теперь в дереве мастерства копья: Alt+K.')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkBrawlerRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] Counter Punch and its upgrades are in the knuckle mastery tree: Alt+K.'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] Контрудар и его усиления теперь в дереве мастерства кастетов: Alt+K.')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkFlurryOfIntellectRank${L}LongDesc"
  $extra[$key] = ($loc[$key] + '\n\n[FFB400]AposMod:[-] Chain Lightning and its upgrades are in the baton mastery tree: Alt+K.'), ($locRu[$key] + '\n\n[FFB400]AposMod:[-] Цепная молния и её усиления теперь в дереве мастерства жезла: Alt+K.')
}
for ($L = 1; $L -le 5; $L++) {
  $key = "perkGunslingerRank${L}LongDesc"
  $en = $loc[$key]; $ru = $locRu[$key]
  if ($L -ge 3) { $en = Remove-LastSentence $en; $ru = Remove-LastSentence $ru }
  $extra[$key] = ($en + '\n\n[FFB400]AposMod:[-] Headhunter (pistols and SMGs) now grows in the weapon mastery tree: Alt+K.'), ($ru + '\n\n[FFB400]AposMod:[-] Охотник за головами (пистолеты и ПП) теперь растёт в дереве мастерства оружия: Alt+K.')
}

# ===== 1.0: max-rank highlights of the other perks (perk -> max rank, EN, RU), appended to that rank's vanilla text =====
$perkHighlights = [ordered]@{
  perkFlurryOfPerception = 5, 'Frenzy: a kill with a spear gives +15% attack speed for 5 s.', 'Неистовство: убийство копьём даёт +15% скорости атаки на 5 с.'
  perkFlurryOfStrength   = 5, 'Frenzy: a kill with clubs or sledgehammers gives +15% attack speed for 5 s.', 'Неистовство: убийство дубинкой или кувалдой даёт +15% скорости атаки на 5 с.'
  perkFlurryOfFortitude  = 5, 'Frenzy: a kill with knuckles or bare hands gives +15% attack speed for 5 s.', 'Неистовство: убийство кастетом или голыми руками даёт +15% скорости атаки на 5 с.'
  perkFlurryOfAgility    = 5, 'Frenzy: a kill with knives gives +15% attack speed for 5 s.', 'Неистовство: убийство ножом даёт +15% скорости атаки на 5 с.'
  perkDemolitionsExpert  = 5, 'Chain Reaction: a zombie killed by your explosive blows up too: 250 damage within 4 m, or half of the killing hit if that is more.', 'Цепная реакция: зомби, убитый вашей взрывчаткой, сам взрывается: 250 урона в радиусе 4 м или половина добивающего удара, если это больше.'
  perkPenetrator         = 5, 'Shrapnel: armor-piercing hits deal 40% of their damage to every zombie within 3 m of the target.', 'Шрапнель: бронебойные попадания наносят 40% своего урона всем зомби в радиусе 3 м от цели.'
  perkInfiltrator        = 5, 'Ghost: crouching halves your noise and visibility, hits on zombies that have not noticed you deal x2 damage, mines and traps cannot hurt you.', 'Призрак: в приседе вы вдвое тише и незаметнее, удары по зомби, которые вас не заметили, наносят x2 урона, мины и ловушки вам не вредят.'
  perkAnimalTracker      = 5, "Hunter's Mark: hitting an animal marks it for 10 s like a crossbow bolt (red ring, more damage).", 'Метка охотника: попадание по животному помечает его на 10 с, как болт арбалета (красное кольцо, больше урона).'
  perkTreasureHunter     = 5, 'X Marks the Spot: the treasure search circle shrinks almost to a point, and buried treasure also holds a quality 5-6 weapon with affixes.', 'Крестик на карте: круг поиска клада сжимается почти до точки, а в кладе ещё и оружие 5-6 качества с аффиксами.'
  perkSalvageOperations  = 5, 'Junk Genius: salvage tools wear 90% slower.', 'Гений хлама: инструменты для разборки изнашиваются на 90% медленнее.'
  perkPerceptionMastery  = 5, 'Lucky Shot: a hit with a Perception weapon has a 15% chance to deal +100% damage.', 'Удачный выстрел: попадание оружием Восприятия с шансом 15% наносит +100% урона.'
  perkPackMule           = 5, 'Beast of Burden: an overloaded backpack no longer slows you down.', 'Вьючный зверь: перегруз больше не замедляет вас.'
  perkStrengthMastery    = 5, 'Crippling Blows: a club or sledgehammer power attack breaks the legs: the zombie crawls.', 'Калечащие удары: силовая атака дубинкой или кувалдой ломает ноги — зомби ползёт.'
  perkMiner69r           = 5, 'Tunnel Rat: mining tools cost 50% less stamina.', 'Туннельная крыса: инструменты добычи тратят на 50% меньше выносливости.'
  perkMotherLode         = 5, 'Jackpot: a hit on ore or wood has a 5% chance to make the next 2 s of mining yield x5.', 'Джекпот: удар по руде или дереву с шансом 5% даёт x5 добычи на 2 с.'
  perkJunkMiner          = 5, 'Scrap King: +50% more from junk piles and salvage.', 'Король хлама: +50% добычи с куч хлама и разборки.'
  perkTheHuntsman        = 5, 'Butcher: harvesting a carcass heals 30 HP (once per 5 s).', 'Мясник: разделка туши лечит 30 HP (раз в 5 с).'
  perkPainTolerance      = 5, 'Unbreakable: below 25% health you take 45% less damage.', 'Несгибаемый: при здоровье ниже 25% вы получаете на 45% меньше урона.'
  perkFortitudeMastery   = 5, 'Second Wind: once per 5 minutes, dropping below 20% health instantly heals 70% of your max health.', 'Второе дыхание: раз в 5 минут, когда здоровье падает ниже 20%, вы сразу восстанавливаете 70% максимума.'
  perkSiphoningStrikes   = 5, 'Vampire: a melee kill gives +50 max health for 15 s. Every kill in that time adds another +50 and restarts the 15 s, without a limit.', 'Вампир: убийство в ближнем бою даёт +50 к максимуму здоровья на 15 с. Каждое убийство за это время добавляет ещё +50 и перезапускает 15 с, без предела.'
  perkHealingFactor      = 5, 'Regeneration: after 6 s without hitting or being hit you regenerate 4 HP/s more.', 'Регенерация: через 6 с без ударов (ваших и по вам) вы восстанавливаете ещё 4 HP/с.'
  perkSlowMetabolism     = 5, 'Iron Stomach: no food poisoning or dysentery (murky water is safe), and food and water never drop below 25%.', 'Железный желудок: никаких отравлений и дизентерии (мутная вода безопасна), а еда и вода не падают ниже 25%.'
  perkRuleOneCardio      = 5, 'Second Lungs: when your stamina runs out you instantly get 50% back (once per 60 s).', 'Второе дыхание лёгких: когда выносливость кончается, вы сразу получаете 50% обратно (раз в 60 с).'
  perkRunAndGun          = 5, 'Gun Kata: shooting on the move: no hip spread and +30% damage.', 'Ган-ката: стрельба на ходу без разброса от бедра и с +30% урона.'
  perkHardTarget         = 5, 'Blur: while running you take 45% less damage.', 'Размытие: на бегу вы получаете на 45% меньше урона.'
  perkParkour            = 5, 'Cat Landing: 99% less fall damage.', 'Кошачье приземление: на 99% меньше урона от падения.'
  perkAgilityMastery     = 5, 'Evasion: after taking a hit you move 30% faster for 5 s.', 'Уклонение: после полученного удара вы двигаетесь на 30% быстрее 5 с.'
  perkHiddenStrike       = 5, 'Shadow Strike: crouch 3 s unnoticed and without attacking - your next hit deals x2; a kill with it gives the Shadow back at once, so a room can be cleared one by one.', 'Удар из тени: 3 с в приседе незамеченным и без атак - следующий удар x2; убийство им сразу возвращает Тень, так можно зачистить комнату по одному.'
  perkFromTheShadows     = 5, 'Silent Shadow: crouching costs no speed. Hunter in the Dark: at night or in dark places you sense zombies within 20 m through walls (a dim beating glow; sleeping ones faint blue). Zombies that lost you give up the search almost at once.', 'Бесшумная тень: присед не замедляет. Охотник во тьме: ночью и в тёмных местах вы чувствуете зомби в 20 м сквозь стены (тусклое пульсирующее свечение; спящие - бледно-синие). Зомби, потерявшие вас, почти сразу прекращают поиск.'
  perkElectrocutioner    = 5, 'Overcharge: you deal +25% damage (any weapon) to shocked zombies.', 'Перегрузка: вы наносите +25% урона (любым оружием) шокированным зомби.'
  perkBetterBarter       = 5, 'Black Market: every trader restock has a 25% chance to include a legendary weapon.', 'Чёрный рынок: при каждом обновлении товара торговца с шансом 25% появляется легендарное оружие.'
  perkDaringAdventurer   = 5, 'Big Score: the weapon reward of a tier 5+ quest is always quality 6 with 3 affixes.', 'Крупный куш: оружие в награду за квест 5+ уровня всегда 6 качества с 3 аффиксами.'
  perkCharismaticNature  = 5, 'Leader: you get the Charismatic Nature bonuses too (vanilla gives them only to others): regen +50%, melee and block damage +20%, damage resist 10%, loot +30%, all attributes +1.', 'Лидер: вы тоже получаете бонусы «Харизмы» (в ванили — только другие): регенерация +50%, урон ближнего боя и по блокам +20%, сопротивление урону 10%, лут +30%, все атрибуты +1.'
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
# Siphoning Strikes: the vanilla kill heal is removed (progression.xml), every melee hit heals instead (AposCombat.cs)
for ($L = 1; $L -le 5; $L++) {
  $key = "perkSiphoningStrikesRank${L}LongDesc"
  $en = $loc[$key] -replace '\\nMelee kills grant \d+ HP\.', ''
  $ru = $locRu[$key] -replace '\\nУбийства в ближнем бою дают \d+ ОЖ\.', ''
  if ($en -eq $loc[$key] -or $ru -eq $locRu[$key]) { throw "Siphoning Strikes text changed: $key" }
  $loc[$key] = $en + '\n\n[FFB400]AposMod:[-] every melee hit heals ' + (4 * $L) + '% of the damage dealt (all life steal together: 30% at most).'
  $locRu[$key] = $ru + '\n\n[FFB400]AposMod:[-] каждый удар в ближнем бою лечит ' + (4 * $L) + '% нанесённого урона (весь вампиризм вместе: не больше 30%).'
  if ($L -lt 5) { $extra[$key] = $loc[$key], $locRu[$key] }
}
# Healing Factor: stronger natural regeneration per level (progression.xml)
$regen = '0.2', '0.4', '0.6', '1', '1.5'
for ($L = 1; $L -le 5; $L++) {
  $key = "perkHealingFactorRank${L}LongDesc"
  $en = $loc[$key] -replace 'Gain 1 health every \d+ seconds with natural healing\.', ('Regenerate ' + $regen[$L - 1] + ' HP per second with natural healing.')
  $ru = $locRu[$key] -replace 'Каждые \d+ секунд вы получаете по 1 очку здоровья за счет естественного восстановления\.', ('Вы естественным образом восстанавливаете ' + $regen[$L - 1].Replace('.', ',') + ' ОЖ в секунду.')
  if ($en -eq $loc[$key] -or $ru -eq $locRu[$key]) { throw "Healing Factor text changed: $key" }
  $loc[$key] = $en; $locRu[$key] = $ru
  if ($L -lt 5) { $extra[$key] = $en, $ru }
}
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
  attAgility    = 'Adrenaline: kills with Agility weapons (pistols, bows, knives) give +10% move speed and +5% damage for 15 s, up to 3 stacks.', 'Адреналин: убийства оружием Ловкости (пистолеты, луки, ножи) дают +10% скорости передвижения и +5% урона на 15 с, до 3 стаков.'
  attIntellect  = 'Charged Chain Lightning: every kill gives a lightning charge; at 10 charges your next hit with any weapon fires Chain Lightning around you that also deals damage: shock for 4 s and 200 damage (or half of that hit if it is more) to every zombie within 8 m.', 'Заряженная цепная молния: каждое убийство даёт заряд молнии; на 10 зарядах следующий удар любым оружием выпускает вокруг вас Цепную молнию, которая ещё и наносит урон: шок на 4 с и 200 урона (или половина этого удара, если это больше) всем зомби в радиусе 8 м.'
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

# ===== 1.4: armor set rework (docs/Design-Sets.md) =====
$tierNote = '\n[808080]On forge tiers 7-10 percentage bonuses grow by 5% per tier above 6 (lowest worn piece).[-]', '\n[808080]На качестве 7-10 процентные бонусы растут на 5% за каждый уровень выше 6 (по худшей вещи).[-]'
$setTexts = [ordered]@{
  Biker    = 'Bruiser Full Set Bonus', 'Бонус полного комплекта громилы', 'Clubs and sledgehammers: Earthquake reaches 1 m further, Home Run launches further, Rage holds 1 more stack.', 'Дубинки и кувалды: Землетрясение достаёт на 1 м дальше, Хоумран отправляет дальше, Ярость держит на 1 стак больше.'
  Enforcer = 'Gunslinger Full Set Bonus', 'Бонус полного комплекта стрелка', 'Pistols: Headhunter starts after 4 headshots instead of 5 and deals +30% more damage while it runs.', 'Пистолеты: Охотник за головами включается после 4 хедшотов вместо 5 и пока действует, наносит ещё +30% урона.'
  Ranger   = 'Ranger Full Set Bonus', 'Бонус полного комплекта рейнджера', 'Rifles: Ricochet reaches 3 m further and deals 20% more damage.', 'Винтовки: Рикошет достаёт на 3 м дальше и наносит на 20% больше урона.'
  Commando = 'Commando Full Set Bonus', 'Бонус полного комплекта коммандос', 'Machine guns: Suppression slows up to 20% more, every 3rd hit puts a round back into the magazine.', 'Автоматы: Подавление замедляет ещё на 20% сильнее, каждое 3-е попадание возвращает патрон в магазин.'
  Farmer   = 'Hunter Full Set Bonus', 'Бонус полного комплекта охотника', 'Bows and crossbows: Splinter +2 fragments, your damage to marked zombies +20%.', 'Луки и арбалеты: Расщепление +2 осколка, ваш урон по помеченным зомби +20%.'
  Preacher = 'Preacher Full Set Bonus', 'Бонус полного комплекта проповедника', 'Elements: fire, shock, acid and bleeding deal 50% more, reactions recharge in 1.5 s instead of 3 s. The gloves raise the chance of elemental mods.', 'Стихии: огонь, ток, кислота и кровотечение наносят на 50% больше, реакции перезаряжаются за 1.5 с вместо 3. Перчатки повышают шанс стихийных модов.'
  Nerd     = 'Nerd Full Set Bonus', 'Бонус полного комплекта ботана', 'Batons and robots: Chain Lightning reaches 1 m further, lightning charges come twice as fast.', 'Жезлы и роботы: Цепная молния достаёт на 1 м дальше, заряды молнии копятся вдвое быстрее.'
}
foreach ($s in $setTexts.Keys) {
  $v = $setTexts[$s]
  $extra["aposSet${s}Name"] = $v[0], $v[1]
  $extra["aposSet${s}Desc"] = ('[FFB400]AposMod:[-] ' + $v[2] + $tierNote[0]), ('[FFB400]AposMod:[-] ' + $v[3] + $tierNote[1])
}
# kept vanilla sets get an AposMod line on top of their own text
$setAdds = [ordered]@{
  buffAssassinSetBonusDesc = 'Knives: +2 Bloodsucker stacks, hits on zombies that have not noticed you deal x1.5.', 'Ножи: +2 стака Кровопийцы, удары по зомби, которые вас не заметили, x1.5.'
  buffRaiderSetBonusDesc   = 'Knuckles: +2 Counter Punch charges; with full charges you take 15% less damage.', 'Кастеты: +2 заряда Контрудара; с полными зарядами вы получаете на 15% меньше урона.'
  buffNomadSetBonusDesc    = 'Vehicles use 50% less fuel and drive 10% faster.', 'Транспорт тратит на 50% меньше топлива и едет на 10% быстрее.'
}
foreach ($k in $setAdds.Keys) {
  if (-not $loc[$k]) { throw "No vanilla text for $k" }
  $extra[$k] = ($loc[$k] + '\n\n[FFB400]AposMod:[-] ' + $setAdds[$k][0]), ($locRu[$k] + '\n\n[FFB400]AposMod:[-] ' + $setAdds[$k][1])
}
# Item descriptions of set pieces (inventory, traders): the piece's AposMod bonus and the AposMod full set bonus.
# Reworked sets lost their vanilla set bonus, so its sentence is replaced; kept sets get the AposMod line added.
$pieceTexts = @{
  armorBikerHelmet    = 'clubs and sledgehammers +5-30% damage', 'дубинки и кувалды +5-30% урона'
  armorBikerGloves    = 'power attacks cost 5-30% less stamina', 'силовые атаки тратят на 5-30% меньше выносливости'
  armorBikerBoots     = '+2-20% run speed', '+2-20% к скорости бега'
  armorEnforcerHelmet = 'pistols and SMGs: 5-30% less spread', 'пистолеты и ПП: разброс меньше на 5-30%'
  armorEnforcerOutfit = 'pistols and SMGs +5-30% damage', 'пистолеты и ПП +5-30% урона'
  armorEnforcerGloves = 'pistols and SMGs reload 5-30% faster', 'пистолеты и ПП перезаряжаются на 5-30% быстрее'
  armorRangerHelmet   = 'rifles: 5-30% less recoil', 'винтовки: отдача меньше на 5-30%'
  armorRangerGloves   = 'rifles +5-50% damage', 'винтовки +5-50% урона'
  armorCommandoHelmet = 'machine guns: +5-30% magazine', 'автоматы: магазин +5-30%'
  armorCommandoOutfit = 'machine guns: 5-30% less recoil', 'автоматы: отдача меньше на 5-30%'
  armorCommandoGloves = 'machine guns +5-50% damage', 'автоматы +5-50% урона'
  armorFarmerHelmet   = 'bows and crossbows +5-30% damage', 'луки и арбалеты +5-30% урона'
  armorFarmerOutfit   = '5-30% less noise', 'шум меньше на 5-30%'
  armorFarmerGloves   = 'bows and crossbows reload 5-30% faster and shoot 2-20% faster', 'луки и арбалеты перезаряжаются на 5-30% и стреляют на 2-20% быстрее'
  armorFarmerBoots    = '+2-20% run speed', '+2-20% к скорости бега'
  armorPreacherHelmet = '+5-10 fire and shock resistance', '+5-10 к сопротивлению огню и току'
  armorPreacherOutfit = '+2-20 max health', '+2-20 к максимуму здоровья'
  armorPreacherGloves = 'elemental mods trigger 1-10% more often, instead of the vanilla damage bonus', 'стихийные моды срабатывают на 1-10% чаще, вместо ванильного бонуса к урону'
  armorNerdGloves     = 'batons and robots +5-50% damage, instead of the vanilla damage bonus', 'жезлы и роботы +5-50% урона, вместо ванильного бонуса к урону'
}
$qualityNote = ' (grows with quality 1-6)', ' (растёт с качеством 1-6)'
$pieces = 'Helmet', 'Outfit', 'Gloves', 'Boots'
foreach ($s in @($setTexts.Keys) + @('Assassin', 'Raider', 'Nomad')) {
  foreach ($pc in $pieces) {
    $item = "armor$s$pc"; $key = "${item}Desc"
    if (-not $loc[$key]) { throw "No vanilla text for $key" }
    $en = $loc[$key]; $ru = $locRu[$key]
    $addEn = ''; $addRu = ''
    if ($pieceTexts[$item]) { $addEn += '\n[FFB400]AposMod piece bonus:[-] ' + $pieceTexts[$item][0] + $qualityNote[0] + '.'; $addRu += '\n[FFB400]AposMod, бонус вещи:[-] ' + $pieceTexts[$item][1] + $qualityNote[1] + '.' }
    if ($setTexts.Contains($s)) {
      # replace the vanilla full set sentence (the set lost that bonus)
      $iEn = $en.IndexOf('\n\nFull Set Bonus'); $iRu = $ru.IndexOf('\n\nБонус за полный комплект')
      if ($iEn -lt 0 -or $iRu -lt 0) { throw "No vanilla set sentence in $key" }
      $en = $en.Substring(0, $iEn); $ru = $ru.Substring(0, $iRu)
      $addEn += '\n[FFB400]Full set (AposMod):[-] ' + $setTexts[$s][2]; $addRu += '\n[FFB400]Полный комплект (AposMod):[-] ' + $setTexts[$s][3]
    } else {
      $addEn += '\n[FFB400]Full set, AposMod adds:[-] ' + $setAdds["buff${s}SetBonusDesc"][0]; $addRu += '\n[FFB400]Полный комплект, AposMod добавляет:[-] ' + $setAdds["buff${s}SetBonusDesc"][1]
    }
    $extra[$key] = ($en + '\n' + $addEn), ($ru + '\n' + $addRu)
  }
}
# renamed sets (the look stays)
$renamed = [ordered]@{
  armorBikerHelmet = 'Bruiser Helmet', 'Шлем громилы'; armorBikerOutfit = 'Bruiser Outfit', 'Костюм громилы'
  armorBikerGloves = 'Bruiser Gloves', 'Перчатки громилы'; armorBikerBoots = 'Bruiser Boots', 'Ботинки громилы'
  armorEnforcerHelmet = 'Gunslinger Sunglasses', 'Очки стрелка'; armorEnforcerOutfit = 'Gunslinger Suit', 'Костюм стрелка'
  armorEnforcerGloves = 'Gunslinger Gloves', 'Перчатки стрелка'; armorEnforcerBoots = 'Gunslinger Shoes', 'Туфли стрелка'
  armorFarmerHelmet = 'Hunter Hat', 'Шляпа охотника'; armorFarmerOutfit = 'Hunter Outfit', 'Костюм охотника'
  armorFarmerGloves = 'Hunter Gloves', 'Перчатки охотника'; armorFarmerBoots = 'Hunter Boots', 'Сапоги охотника'
}
foreach ($k in $renamed.Keys) { $extra[$k] = $renamed[$k] }

# Wardrobe (Source/AposWardrobe.cs)
$extra['aposParryToast']       = 'Parry!', 'Парирование!'
$extra['aposBreakthroughToast'] = 'Breakthrough!', 'Прорыв!'
$extra['aposWardrobeTitle']    = 'WARDROBE', 'ГАРДЕРОБ'
$extra['aposWardrobeTab']      = 'Outfit {0}', 'Комплект {0}'
$extra['aposWardrobeWorn']     = 'worn now', 'надет сейчас'
$extra['aposWardrobeEmpty']    = 'empty', 'пусто'
$extra['aposWardrobeWear']     = 'Put on', 'Надеть'
$extra['aposWardrobeTake']     = 'Take to backpack', 'Забрать в рюкзак'
$extra['aposWardrobeHint']     = 'Alt+1 / Alt+2 / Alt+3 - change outfit, Alt+0 - this window', 'Alt+1 / Alt+2 / Alt+3 - сменить комплект, Alt+0 - это окно'
$extra['aposWardrobeChanging'] = 'Changing into outfit {0}', 'Переодевание в комплект {0}'
$extra['aposWardrobeSwitched'] = 'Outfit {0} is on', 'Надет комплект {0}'
$extra['aposWardrobeButton']   = 'Wardrobe (Alt+0)', 'Гардероб (Alt+0)'
$extra['aposWardrobeSlotHead']  = 'Head', 'Голова'
$extra['aposWardrobeSlotChest'] = 'Body', 'Тело'
$extra['aposWardrobeSlotHands'] = 'Hands', 'Руки'
$extra['aposWardrobeSlotFeet']  = 'Feet', 'Ноги'
$extra['aposWardrobeItemArmor'] = 'armor {0}', 'броня {0}'
$extra['aposWardrobeArmor']     = 'Armor: [80C8FF]{0}[-]     Elemental: [FFB060]{1}[-]', 'Броня: [80C8FF]{0}[-]     От стихий: [FFB060]{1}[-]'
$extra['aposWardrobeNoSet']     = 'No armor set', 'Без комплекта брони'
$extra['aposWardrobeSetPartial'] = 'Set "{0}": {1}/4, {2} more for the set bonus', 'Сет «{0}»: {1}/4, до бонуса не хватает {2}'
$extra['aposWardrobeSetFull']   = '[40FF60]Set "{0}" 4/4, bonus active:[-]', '[40FF60]Сет «{0}» 4/4, бонус активен:[-]'
$extra['aposWardrobeTip']      = 'Tip: keep up to 3 outfits and change in 1.5 s with Alt+1 / Alt+2 / Alt+3. Alt+0 or the Wardrobe button in the character window shows them.', 'Совет: можно держать до 3 комплектов одежды и переодеваться за 1.5 с клавишами Alt+1 / Alt+2 / Alt+3. Alt+0 или кнопка «Гардероб» в окне персонажа показывает их.'

# Reaction feed on the right of the screen (Source/AposReactions.cs)
$extra['aposReactConfusion'] = 'Confusion', 'Замешательство'
$extra['aposReactMiasma']    = 'Miasma', 'Миазмы'
$extra['aposProcEmp']        = 'INT 10', 'ИНТ 10'
$extra['aposProcOverkill']   = 'STR 10', 'СИЛ 10'
$extra['aposProcChain']      = ($loc['perkDemolitionsExpertName'] + ' 5'), ($locRu['perkDemolitionsExpertName'] + ' 5')
$extra['aposProcShrapnel']   = ($loc['perkPenetratorName'] + ' 5'), ($locRu['perkPenetratorName'] + ' 5')

# Weapon Mastery 2.0 (Source/AposMastery.cs): window, classes; skill texts aposMS_<Class>_<Id> (name), _D (description), _R1.._R3 (ranks)
$extra['aposMasteryTitle']      = 'WEAPON MASTERY', 'МАСТЕРСТВО ОРУЖИЯ'
$extra['aposMasteryNoWeapon']   = 'Take a weapon in your hands.', 'Возьмите оружие в руки.'
$extra['aposMasteryLevel']      = 'Mastery {0} / {1}', 'Мастерство {0} / {1}'
$extra['aposMasteryKills']      = 'Kills: {0} / {1}', 'Убийств: {0} / {1}'
$extra['aposMasteryKillsMax']   = 'Kills: {0} (max level)', 'Убийств: {0} (максимум)'
$extra['aposMasteryPoints']     = 'Free points: {0}   (spent {1})', 'Свободных очков: {0}   (вложено {1})'
$extra['aposMasteryLearn']      = 'Learn a rank', 'Изучить ранг'
$extra['aposMasteryMaxed']      = 'All ranks learned.', 'Все ранги изучены.'
$extra['aposMasteryNeedTier']   = 'Needs {0} points in the lower tiers of branch {1} (now {2}).', 'Нужно {0} очков в нижних ярусах ветки {1} (сейчас {2}).'
$extra['aposMasteryNeedHybrid'] = 'Hybrid: needs {0} points in each branch (A: {1}, B: {2}).', 'Гибрид: нужно {0} очков в каждой ветке (A: {1}, B: {2}).'
$extra['aposMasteryNoPoints']   = 'No free points: kill more with this class of weapon.', 'Нет свободных очков: убивайте оружием этого класса.'
$extra['aposMasteryResetOne']   = 'Reset this skill ({0} Eridium)', 'Сбросить навык ({0} эридия)'
$extra['aposMasteryResetAll']   = 'Reset the whole tree ({0} Eridium)', 'Сбросить всё дерево ({0} эридия)'
$extra['aposMasteryResetHint']  = 'Reset works within 5 m of an Eridium Forge.', 'Сброс работает в 5 м от Кузни эридия.'
$extra['aposMasteryResetBlocked'] = 'This skill cannot be reset alone: a learned skill above needs its points.', 'Этот навык не сбросить отдельно: на его очках держится изученный навык выше.'
$extra['aposMasteryBaseHint']   = 'Class trait: always on, the tree makes it stronger.', 'Особенность класса: работает всегда, дерево её усиливает.'
$extra['aposMasteryBaseShort']  = 'Class trait', 'Особенность класса'
$extra['aposMasteryHybrids']    = 'Hybrids', 'Гибриды'
$extra['aposMasterySubTier']    = 'Branch {0} · {1} · tier {2}', 'Ветка {0} · {1} · ярус {2}'
$extra['aposMasterySubHybrid']  = 'Hybrid: {0} points in each branch', 'Гибрид: {0} очков в каждой ветке'
$extra['aposMasterySoon']       = 'The skill tree of this class comes in a later update. Kills already count towards its level.', 'Дерево навыков этого класса появится в следующем обновлении. Убийства уже идут в его уровень.'
$extra['aposMasteryLevelUp']    = '{0}: mastery {1}! Alt+K - skill tree', '{0}: мастерство {1}! Alt+K - дерево навыков'
$extra['aposMasteryBannerTitle'] = 'WEAPON MASTERY {0}!', 'МАСТЕРСТВО ОРУЖИЯ {0}!'
$extra['aposMasteryBannerHint']  = 'new skill point: Alt+K or the item info panel', 'новое очко навыка: Alt+K или панель предмета'
$extra['aposMasteryBannerSoon']  = 'the tree of this class comes in a later update', 'дерево этого класса появится в следующем обновлении'
$masteryClasses = [ordered]@{
  Pistols = 'Pistols and SMGs', 'Пистолеты и ПП'; Shotguns = 'Shotguns', 'Дробовики'; Rifles = 'Rifles', 'Винтовки'
  MG = 'Machine guns', 'Автоматы'; Bows = 'Bows', 'Луки'; Crossbows = 'Crossbows', 'Арбалеты'; Blades = 'Knives and machetes', 'Ножи и мачете'
  Clubs = 'Clubs and bats', 'Дубинки и биты'; Sledges = 'Sledgehammers', 'Кувалды'; Spears = 'Spears', 'Копья'
  Knuckles = 'Knuckles', 'Кастеты'; Batons = 'Batons', 'Жезлы' }
foreach ($k in $masteryClasses.Keys) { $extra["aposMasteryClass$k"] = $masteryClasses[$k] }

# Pistols / SMGs (Source/AposPistol.cs)
$extra['aposMB_Pistols_A'] = 'Headhunter', 'Охотник за головами'
$extra['aposMB_Pistols_B'] = 'Lead Storm', 'Свинцовый шквал'
$extra['aposMB_Shotguns_A'] = 'Slugs', 'Пули'
$extra['aposMB_Shotguns_B'] = 'Buckshot', 'Картечь'
$extra['aposMB_Rifles_A'] = 'The Shot', 'Выстрел'
$extra['aposMB_Rifles_B'] = 'Ricochet', 'Рикошет'
$extra['aposMB_MG_A'] = 'Suppression', 'Подавление'
$extra['aposMB_MG_B'] = 'Spin-up', 'Раскрутка'
$extra['aposMB_Bows_A'] = 'Splinters', 'Осколки'
$extra['aposMB_Bows_B'] = 'The Arrow', 'Стрела'
$extra['aposMB_Crossbows_A'] = 'The Mark', 'Метка'
$extra['aposMB_Crossbows_B'] = 'Sticky Charge', 'Липкий заряд'
$extra['aposMB_Blades_A'] = 'Bloodsucker', 'Кровопийца'
$extra['aposMB_Blades_B'] = 'The Killer', 'Убийца'
$extra['aposMB_Clubs_A'] = 'Home Run', 'Хоумран'
$extra['aposMB_Clubs_B'] = 'Rage', 'Ярость'
$extra['aposMB_Sledges_A'] = 'Earthquake', 'Землетрясение'
$extra['aposMB_Sledges_B'] = 'The Blow', 'Удар'
$extra['aposMB_Spears_A'] = 'Battering Ram', 'Таран'
$extra['aposMB_Spears_B'] = 'Fencing', 'Фехтование'
$extra['aposMB_Knuckles_A'] = 'Counter Punch', 'Контрудар'
$extra['aposMB_Knuckles_B'] = 'Flurry', 'Шквал'
$extra['aposMB_Batons_A'] = 'Storm', 'Буря'
$extra['aposMB_Batons_B'] = 'Overload', 'Перегрузка'
# id = name EN, name RU, description EN, description RU, then ranks: EN, RU pairs
$masterySkills = [ordered]@{
  Pistols_Base = 'Headhunter', 'Охотник за головами', '5 headshots in a row start the Headhunter mode: the next 6 shots are empowered (+20% damage, almost no spread). A miss or a body hit breaks the streak. Branch A makes the mode stronger, branch B and the hybrids feed it from the side.', '5 попаданий в голову подряд включают режим Охотника: следующие 6 выстрелов усилены (+20% урона, почти без разброса). Промах или попадание в тело обрывает серию. Ветка A усиливает режим, ветка B и гибриды подпитывают его сбоку.'
  Pistols_A1 = 'Keen Eye', 'Меткий глаз', 'More headshot damage with any shot, not only in the mode.', 'Больше урона в голову любым выстрелом, не только в режиме.', '+8% headshot damage', '+8% урона в голову', '+16% headshot damage', '+16% урона в голову', '+30% headshot damage', '+30% урона в голову'
  Pistols_A2 = 'Steady Hand', 'Твёрдая рука', 'Less spread: fewer stray misses, so the streak breaks less often.', 'Меньше разброс: меньше случайных промахов, серия рвётся реже.', '-10% spread', '-10% разброса', '-20% spread', '-20% разброса', '-35% spread', '-35% разброса'
  Pistols_A3 = 'The Hunt Goes On', 'Охота продолжается', 'More empowered shots in one mode.', 'Больше усиленных выстрелов за один режим.', '+3 empowered shots (9)', '+3 усиленных выстрела (9)', '+6 empowered shots (12)', '+6 усиленных выстрелов (12)', '+9 empowered shots (15); a headshot kill in the mode adds 2 more', '+9 усиленных (15); убийство в голову в режиме даёт ещё +2'
  Pistols_A4 = 'Cold Blood', 'Хладнокровие', 'The streak is easier to start and harder to lose.', 'Серию легче начать и труднее потерять.', 'The mode needs 4 headshots, not 5', 'Режиму нужно 4 попадания в голову, а не 5', 'A miss or a body hit takes away 1 headshot instead of the whole streak', 'Промах или попадание в тело снимает 1 попадание, а не всю серию', 'The mode needs 3 headshots', 'Режиму нужно 3 попадания в голову'
  Pistols_A5 = "Marksman's Eye", 'Глаз стрелка', 'In the Headhunter mode the heads of zombies within 40 m are marked, even behind cover.', 'В режиме Охотника головы зомби в 40 м отмечены, даже за укрытием.', 'Head marks in the mode', 'Метки голов в режиме'
  Pistols_A6 = 'Lethal Precision', 'Смертельная точность', 'Empowered shots hit harder.', 'Усиленные выстрелы бьют сильнее.', 'Empowered shots +40% instead of +20%', 'Усиленные выстрелы +40% вместо +20%', 'Empowered shots +70%', 'Усиленные выстрелы +70%'
  Pistols_A7 = 'Verdict', 'Приговор', 'An empowered headshot on a zombie below 35% health may kill it at once. Not elites.', 'Усиленный выстрел в голову по зомби с здоровьем ниже 35% может убить его сразу. Не элиту.', '15% chance', 'Шанс 15%', '35% chance', 'Шанс 35%'
  Pistols_A8 = 'Duel', 'Дуэль', 'Capstone. Empowered shots deal x2. Downside: a body hit in the mode ends the mode at once.', 'Капстоун. Усиленные выстрелы наносят x2. Минус: попадание в тело в режиме сразу обрывает режим.', 'Empowered x2; a body hit ends the mode', 'Усиленные x2; попадание в тело обрывает режим'
  Pistols_B1 = 'Quick Finger', 'Быстрый палец', 'Higher fire rate.', 'Выше темп стрельбы.', '+18% fire rate', '+18% темпа', '+36% fire rate', '+36% темпа', '+55% fire rate', '+55% темпа'
  Pistols_B2 = 'Extended Mag', 'Расширенный магазин', 'More rounds in the magazine: rounds or percent, whichever is more. Pistols gain more from rounds, SMGs from percent.', 'Больше патронов в магазине: штуки или проценты, что больше. Пистолетам выгоднее штуки, ПП — проценты.', '+2 rounds or +15%', '+2 патрона или +15%', '+4 rounds or +30%', '+4 патрона или +30%', '+6 rounds or +45%', '+6 патронов или +45%'
  Pistols_B3 = 'Speed Loader', 'Быстрая перезарядка', 'Faster reload.', 'Быстрее перезарядка.', '+12% reload speed', '+12% скорости перезарядки', '+24% reload speed', '+24% скорости перезарядки', '+45% reload speed', '+45% скорости перезарядки'
  Pistols_B4 = 'Lead Shackles', 'Свинцовые кандалы', 'Every hit slows the target for 3 s. Strength = rank x your Agility level, up to 80%.', 'Каждое попадание замедляет цель на 3 с. Сила = ранг x уровень вашей Ловкости, до 80%.', '3% x Agility (max 30%)', '3% x Ловкость (макс. 30%)', '5% x Agility (max 50%)', '5% x Ловкость (макс. 50%)', '8% x Agility (max 80%)', '8% x Ловкость (макс. 80%)'
  Pistols_B5 = 'Hot Barrel', 'Горячий ствол', 'Every hit: +3% fire rate, up to +30%. Without hits the bonus melts by 3% per second instead of vanishing.', 'Каждое попадание: +3% темпа, до +30%. Без попаданий бонус тает на 3% в секунду, а не пропадает сразу.', '+3% per hit, up to +30%', '+3% за попадание, до +30%'
  Pistols_B6 = 'Spare Rounds', 'Запасная обойма', 'Kills and torn-off limbs put rounds back into the magazine (never above full).', 'Убийства и оторванные конечности возвращают патроны в магазин (не больше полного).', '+1 per kill, +3 per limb', '+1 за убийство, +3 за конечность', '+2 per kill, +5 per limb', '+2 за убийство, +5 за конечность'
  Pistols_B7 = 'Lead Rain', 'Свинцовый дождь', 'The second half of the magazine hits harder.', 'Вторая половина магазина бьёт сильнее.', 'Last 50% of the magazine: +15%', 'Последние 50% магазина: +15%', 'Last 50% of the magazine: +40%', 'Последние 50% магазина: +40%'
  Pistols_B8 = 'Bottomless Mag', 'Бездонная обойма', 'Capstone. 25% of shots cost no round, 50% against shocked zombies. Downside: +30% spread.', 'Капстоун. 25% выстрелов не тратят патрон, по шокированным зомби — 50%. Минус: +30% разброса.', '25% free shots (50% vs shocked); +30% spread', '25% бесплатных выстрелов (50% по шокированным); +30% разброса'
  Pistols_H1 = 'Firing Squad', 'Расстрел', 'Hybrid. Every 8th hit in a row without a miss counts as a headshot: headshot damage and +1 to the Headhunter streak. A hail of bullets starts the mode by itself.', 'Гибрид. Каждое 8-е попадание подряд без промаха считается выстрелом в голову: урон как в голову и +1 к серии Охотника. Шквал пуль сам включает режим.', 'Every 8th hit in a row = a headshot', 'Каждое 8-е попадание подряд = в голову'
  Pistols_H2 = 'Gunfighter', 'Ганфайтер', 'Hybrid. A headshot puts 1 round back into the magazine and gives +15% fire rate for 3 s.', 'Гибрид. Попадание в голову возвращает 1 патрон в магазин и даёт +15% темпа на 3 с.', 'Headshot: +1 round, +15% fire rate for 3 s', 'В голову: +1 патрон, +15% темпа на 3 с'
  Pistols_H3 = 'Blood Fireworks', 'Кровавый фейерверк', 'Big hybrid (8 points in each branch). A headshot kill in the Headhunter mode bursts the head: zombies within 3 m are slowed by 80% and stunned for 1 s, and you get +3 rounds in the magazine.', 'Большой гибрид (8 очков в каждой ветке). Убийство в голову в режиме Охотника разрывает голову: зомби в 3 м замедлены на 80% и оглушены на 1 с, а вам +3 патрона в магазин.', 'Headshot kill in the mode: head burst, 3 m slow and stun, +3 rounds', 'Убийство в голову в режиме: взрыв головы, 3 м замедление и оглушение, +3 патрона'
  Shotguns_Base = 'Sledgehammer in the Barrel', 'Кувалда в стволе', 'Slugs (Slug, Breaching Slug) throw zombies back and knock them down (force 1000). Buckshot does it point blank (within 3 m, force 300). Elites stay on their feet. Branch A makes slugs stronger, branch B buckshot.', 'Пули (Slug, Breaching Slug) отбрасывают и сбивают зомби с ног (сила 1000). Картечь делает это в упор (до 3 м, сила 300). Элиты устоят. Ветка A усиливает пули, ветка B — картечь.'
  Shotguns_A1 = 'Heavy Slug', 'Тяжёлая пуля', 'More slug damage (Slug and Breaching Slug).', 'Больше урона пулями (Slug и Breaching Slug).', 'Slugs +15% damage', 'Пули +15% урона', 'Slugs +30% damage', 'Пули +30% урона', 'Slugs +50% damage', 'Пули +50% урона'
  Shotguns_A2 = 'Brawn', 'Мускулы', 'Stronger throw by slugs and point-blank buckshot. Strength = rank x your Strength level.', 'Сильнее отброс пулей и картечью в упор. Сила = ранг x уровень вашей Силы.', '+20 force x Strength (max +200)', '+20 силы x Сила (до +200)', '+35 x Strength (max +350)', '+35 x Сила (до +350)', '+50 x Strength (max +500)', '+50 x Сила (до +500)'
  Shotguns_A3 = 'Battering Ram', 'Таран', 'A zombie thrown by a slug knocks down other zombies near where it lands.', 'Отброшенный пулей зомби сбивает с ног других зомби рядом с местом, куда упал.', 'Knocks down zombies within 1.5 m', 'Сбивает зомби в 1.5 м', 'Within 2 m', 'В 2 м', 'Within 2.5 m, each takes 25% of the slug damage', 'В 2.5 м, каждый получает 25% урона пули'
  Shotguns_A4 = 'Through and Through', 'Навылет', 'A slug goes through zombies and flies on.', 'Пуля пробивает зомби и летит дальше.', 'Pierces 3 zombies', 'Пробивает 3 зомби', 'Pierces 6 zombies', 'Пробивает 6 зомби', 'Pierces 10 zombies', 'Пробивает 10 зомби'
  Shotguns_A5 = 'Shattered', 'Разбитый', 'A zombie knocked down by a shotgun is Shattered (a red "x1.5" above it): while it lies and 3 s after it gets up, all damage to it is +50%, from anyone and any weapon.', 'Зомби, сбитый дробовиком, становится «Разбитым» (красное «x1.5» над ним): пока лежит и ещё 3 с после подъёма, весь урон по нему +50% — от кого угодно и любым оружием.', 'Knocked down by a shotgun: Shattered, +50% damage from everyone', 'Сбит дробовиком: «Разбитый», +50% урона от всех'
  Shotguns_A6 = 'Pump Action', 'Помпа', 'A slug kill loads shells into the magazine.', 'Убийство пулей заряжает патроны в магазин.', 'Slug kill: +1 shell', 'Убийство пулей: +1 патрон', 'Slug kill: +2 shells', 'Убийство пулей: +2 патрона'
  Shotguns_A7 = 'Bouncer', 'Вышибала', 'Zombies thrown by a slug stay down longer; at 2/2 elites fall too.', 'Отброшенные пулей зомби лежат дольше; на 2/2 падают и элиты.', 'Down 1.5 s longer', 'Лежат на 1.5 с дольше', 'Down 3 s longer; elites are knocked down too (1.5 s)', 'Лежат на 3 с дольше; элиты тоже падают (1.5 с)'
  Shotguns_A8 = 'Cannonball', 'Пушечное ядро', 'Capstone. Slugs deal x2 damage, throw x2 and pierce everyone in the line. Downside: buckshot from this weapon -40% damage.', 'Капстоун. Пули наносят x2 урона, отбрасывают x2 и пробивают всех на линии. Минус: картечь этим оружием -40% урона.', 'Slugs x2 damage and throw, unlimited pierce; buckshot -40%', 'Пули x2 урона и отброса, пробивание без лимита; картечь -40%'
  Shotguns_B1 = 'Extra Pellets', 'Больше картечи', 'More pellets, each weaker: about the same total damage, but more hits - more bleeding, fire and Shred, more zombies in the cone.', 'Больше картечин, каждая слабее: урон почти тот же, но больше попаданий — больше кровотечения, поджога и Разрыва, больше зомби в снопе.', '+3 pellets (13), each -25%', '+3 картечины (13), каждая -25%', '+6 pellets (16), each -50%', '+6 картечин (16), каждая -50%', '+10 pellets (20), each -60%', '+10 картечин (20), каждая -60%'
  Shotguns_B2 = 'Wide Choke', 'Широкий сноп', 'Buckshot spreads wider and flies farther: more zombies in one shot.', 'Картечь разлетается шире и дальше: больше зомби за один выстрел.', '+30% spread, +2 m range', '+30% разлёта, +2 м дальности', '+60% spread, +4 m', '+60% разлёта, +4 м', '+100% spread, +6 m (10 -> 16 m)', '+100% разлёта, +6 м (10 -> 16 м)'
  Shotguns_B3 = 'Ragged Wounds', 'Рваные раны', 'Every pellet may cause bleeding.', 'Каждая картечина может вызвать кровотечение.', '5% per pellet', '5% за картечину', '10% per pellet', '10% за картечину', '20% per pellet', '20% за картечину'
  Shotguns_B4 = 'Quick Pump', 'Быстрая помпа', 'Fire rate and reload.', 'Темп стрельбы и перезарядка.', '+15% fire rate and reload', '+15% темпа и перезарядки', '+30%', '+30%', '+50%', '+50%'
  Shotguns_B5 = 'Shred', 'Разрыв', 'If 7 or more pellets of one shot hit one zombie, it loses a limb and is stunned for 1 s.', 'Если 7 и больше картечин одного выстрела попали в одного зомби — он теряет конечность и оглушён на 1 с.', '7+ pellets in one zombie: a limb torn off, 1 s stun', '7+ картечин в одного: оторвана конечность, оглушение 1 с'
  Shotguns_B6 = 'Bloodletting', 'Кровопуск', 'More damage to bleeding zombies.', 'Больше урона по кровоточащим зомби.', '+30% damage to bleeding', '+30% урона по кровоточащим', '+80%', '+80%'
  Shotguns_B7 = 'Lead Wall', 'Свинцовая стена', 'A shot that hits 5 or more zombies pays off.', 'Выстрел, задевший 5 и больше зомби, вознаграждает.', '5+ zombies in one shot: +1 shell into the magazine', '5+ зомби за выстрел: +1 патрон в магазин', '+1 shell and the next shot +50% damage', '+1 патрон и следующий выстрел +50% урона'
  Shotguns_B8 = 'Dragon''s Breath', 'Дыхание дракона', 'Capstone. Every pellet has a 20% chance to set the zombie on fire. Downside: slugs from this weapon -40% damage.', 'Капстоун. Каждая картечина с шансом 20% поджигает. Минус: пули этим оружием -40% урона.', 'Pellet: 20% to ignite; slugs -40%', 'Картечина: 20% поджога; пули -40%'
  Shotguns_H1 = 'Shockwave', 'Ударная волна', 'Hybrid. A shotgun kill (any ammo) knocks down every zombie within 2.5 m of the body for 1.5 s.', 'Гибрид. Убийство из дробовика (любыми патронами) сбивает с ног всех зомби в 2.5 м от тела на 1.5 с.', 'Kill: knockdown within 2.5 m for 1.5 s', 'Убийство: сбивает всех в 2.5 м на 1.5 с'
  Shotguns_H2 = 'Blood and Bone', 'Кровь и кости', 'Hybrid. Zombies knocked down by a shotgun start bleeding. A zombie that dies bleeding sprays blood: everyone within 3 m bleeds too.', 'Гибрид. Сбитые дробовиком зомби начинают кровоточить. Зомби, умерший с кровотечением, брызжет кровью: все в 3 м тоже кровоточат.', 'Knockdown -> bleeding; a death with bleeding spreads it within 3 m', 'Сбит -> кровотечение; смерть с кровотечением заражает всех в 3 м'
  Shotguns_H3 = 'Frag Slug', 'Осколочная пуля', 'Big hybrid (8 points in each branch). A slug hit bursts into 6 fragments at the nearest zombies within 5 m: each like a pellet with branch B effects (bleeding, fire, Bloodletting).', 'Большой гибрид (8 очков в каждой ветке). Пуля после попадания разлетается на 6 осколков в ближайших зомби в 5 м: каждый как картечина с эффектами ветки B (кровотечение, поджог, Кровопуск).', 'Slug -> 6 fragments within 5 m with buckshot effects', 'Пуля -> 6 осколков в 5 м с эффектами картечи'
  Rifles_Base = 'Ricochet', 'Рикошет', 'A headshot hurts up to 3 zombies within 5 m of the target, each for 5% of the headshot (at least 20). Branch B grows the ricochet, branch A the shot itself.', 'Выстрел в голову ранит до 3 зомби в 5 м от цели — каждого на 5% урона выстрела (минимум 20). Ветка B развивает рикошет, ветка A — сам выстрел.'
  Rifles_A1 = 'Cold Aim', 'Хладнокровный прицел', 'More headshot damage.', 'Больше урона в голову.', '+15% headshot damage', '+15% урона в голову', '+30%', '+30%', '+50%', '+50%'
  Rifles_A2 = 'Bolt Work', 'Затвор', 'Faster reload. Best on early single-shot rifles that reload after every shot.', 'Быстрее перезарядка. Лучше всего на ранних однозарядных винтовках, где перезаряжаешься после каждого выстрела.', '+20% reload speed', '+20% скорости перезарядки', '+35%', '+35%', '+50%, and for 6 s after a kill reloads are twice as fast', '+50%, и 6 с после убийства перезарядка вдвое быстрее'
  Rifles_A3 = 'Opening Shot', 'Первый выстрел', 'The first hit on a zombie at full health hits harder.', 'Первое попадание по зомби с полным здоровьем бьёт сильнее.', '+30% on an unhurt zombie', '+30% по целому зомби', '+60%', '+60%', '+100%', '+100%'
  Rifles_A4 = 'Long Shot', 'Дальний выстрел', 'The farther the target, the stronger the shot: a bonus for every full 10 m. Closer than 10 m - a penalty.', 'Чем дальше цель, тем сильнее выстрел: бонус за каждые полные 10 м. Ближе 10 м — штраф.', '+10% per 10 m (max +100%); closer than 10 m -50%', '+10% за 10 м (до +100%); ближе 10 м -50%', '+20% per 10 m (max +150%); closer -40%', '+20% за 10 м (до +150%); ближе -40%', '+30% per 10 m (max +200%); closer -25%', '+30% за 10 м (до +200%); ближе -25%'
  Rifles_A5 = 'Weak Spot', 'Слабое место', 'A headshot on a zombie that does not see you (asleep or not after you) deals x2. On a zombie corroded by acid - always x2.', 'Выстрел в голову по зомби, который вас не видит (спит или не гонится за вами), — x2. По разъеденному кислотой — x2 всегда.', 'x2 on those that do not see you and on corroded ones', 'x2 по тем, кто не видит, и по разъеденным'
  Rifles_A6 = 'Focus', 'Сосредоточенность', 'Every second of aiming without a shot charges the next shot; a charged shot also has no spread. A shot or leaving the sights resets the charge.', 'Каждая секунда в прицеле без выстрела заряжает следующий выстрел; заряженный выстрел ещё и без разброса. Выстрел или выход из прицела сбрасывает заряд.', '+10% per second, max +100%', '+10% в секунду, до +100%', '+20% per second, max +100%', '+20% в секунду, до +100%', '+20% per second, max +1000%', '+20% в секунду, до +1000%'
  Rifles_A7 = 'Big Game', 'Крупная дичь', 'More damage to elites and big zombies (ferals, radiated, Behemoth...).', 'Больше урона по элитам и крупным зомби (дикие, радиоактивные, Бегемот...).', '+50% to elites and big ones', '+50% по элитам и крупным'
  Rifles_A8 = 'One Shot', 'Один выстрел', 'Capstone. A hit anywhere counts as a headshot (with all headshot bonuses). Elites x2. Downside: the Ricochet is half as strong and only real headshots start it.', 'Капстоун. Попадание куда угодно считается выстрелом в голову (со всеми бонусами). Элиты x2. Минус: Рикошет вдвое слабее и запускается только настоящим выстрелом в голову.', 'Any hit = headshot; elites x2; Ricochet x0.5, only from real headshots', 'Любое попадание = в голову; элиты x2; Рикошет x0.5, только от настоящих'
  Rifles_B1 = 'Wide Ricochet', 'Широкий рикошет', 'A bigger ricochet radius. Strength = rank x your Perception level.', 'Больше радиус рикошета. Сила = ранг x уровень вашего Восприятия.', '+0.5 m per Perception level', '+0.5 м за уровень Восприятия', '+0.8 m per Perception level', '+0.8 м за уровень Восприятия', '+1 m per Perception level (max +10 m)', '+1 м за уровень Восприятия (до +10 м)'
  Rifles_B2 = 'Ricochet Power', 'Сила рикошета', 'The ricochet hits each zombie for a share of your headshot (without the skill 5%, at least 20).', 'Рикошет бьёт каждого зомби на долю урона вашего выстрела (без навыка 5%, минимум 20).', '15% of the headshot', '15% урона выстрела', '25% of the headshot', '25% урона выстрела', '45% of the headshot', '45% урона выстрела'
  Rifles_B3 = 'More Targets', 'Больше целей', 'The ricochet hits more zombies (3 by default).', 'Рикошет задевает больше зомби (по умолчанию 3).', '5 targets', '5 целей', '7 targets', '7 целей', '10 targets', '10 целей'
  Rifles_B4 = 'Stunning Ricochet', 'Оглушающий рикошет', 'The ricochet stuns (one zombie not more often than once per 4 s).', 'Рикошет оглушает (одного зомби не чаще раза в 4 с).', '0.5 s stun', 'Оглушение 0.5 с', '1 s', '1 с', '2 s', '2 с'
  Rifles_B5 = 'Body Ricochet', 'Рикошет от тела', 'Body hits ricochet too, at 50% strength. The rifle works against crowds even without perfect headshots.', 'Попадания в тело тоже рикошетят, на 50% силы. Винтовка работает по толпе и без идеальных выстрелов в голову.', 'Body hits ricochet (50%)', 'Попадания в тело рикошетят (50%)'
  Rifles_B6 = 'Elemental Ricochet', 'Стихийный рикошет', 'The ricochet carries an element of the main target (fire, shock, acid, bleeding) to those it hits.', 'Рикошет переносит стихию с основной цели (огонь, ток, кислота, кровотечение) на задетых.', '25% chance to carry it', 'Шанс 25%', '60%', '60%'
  Rifles_B7 = 'Overflow', 'Избыток', 'A rifle headshot nearly always kills with damage to spare. The excess is not lost: it is shared between the ricochets.', 'Выстрел в голову почти всегда убивает с запасом. Лишний урон не пропадает, а делится между рикошетами.', '50% of the excess into the ricochets', '50% избытка в рикошеты', '100% of the excess', '100% избытка'
  Rifles_B8 = 'Ricochet Storm', 'Буря рикошетов', 'Capstone. Ricochets deal x2 and each jumps on to 2 more zombies (each zombie once). Downside: the shot itself -50% (ricochets count from the full damage).', 'Капстоун. Рикошеты бьют x2, и каждый прыгает ещё на 2 зомби (каждого не больше раза). Минус: сам выстрел -50% (рикошеты считаются от полного урона).', 'Ricochets x2, 3 hits each; the shot -50%', 'Рикошеты x2, по 3 попадания; выстрел -50%'
  Rifles_H1 = 'Trick Shot', 'Трюковый выстрел', 'Hybrid. A ricochet on a zombie at full health gets the Opening Shot bonus, and every 4th ricochet hits the head (x2).', 'Гибрид. Рикошет по зомби с полным здоровьем получает бонус Первого выстрела, а каждый 4-й рикошет — в голову (x2).', 'Ricochets: Opening Shot bonus, every 4th in the head', 'Рикошеты: бонус Первого выстрела, каждый 4-й в голову'
  Rifles_H2 = 'Spotter', 'Наводчик', 'Hybrid. A headshot kill outlines every zombie within 30 m for 10 s, and your next shot +50%.', 'Гибрид. Убийство в голову подсвечивает всех зомби в 30 м на 10 с, а следующий выстрел +50%.', 'Headshot kill: outlines within 30 m for 10 s, next shot +50%', 'Убийство в голову: контуры в 30 м на 10 с, следующий выстрел +50%'
  Rifles_H3 = 'Skewer Shot', 'Пронзатель', 'Big hybrid (8 points in each branch). A headshot goes on through the whole line: every zombie on it takes the hit, and each starts its own ricochet.', 'Большой гибрид (8 очков в каждой ветке). Выстрел в голову летит дальше через всю линию: каждый зомби на ней получает удар, и от каждого свой рикошет.', 'A headshot pierces the line; each pierced zombie ricochets', 'Выстрел в голову пробивает линию; от каждого рикошет'
  MG_Base = 'Suppression', 'Подавление', 'Every hit slows the zombie by 5% for 8 s, up to 20%. Every 4th hit puts a round back into the magazine. Branch A makes the suppression stronger, branch B the spin-up.', 'Каждое попадание замедляет зомби на 5% на 8 с, до 20%. Каждое 4-е попадание возвращает патрон в магазин. Ветка A усиливает подавление, ветка B — раскрутку.'
  MG_A1 = 'Heavy Fire', 'Плотный огонь', 'A stronger maximum slow from suppression (20% by default).', 'Сильнее максимальное замедление от подавления (по умолчанию 20%).', 'Max 30%', 'Максимум 30%', 'Max 50%', 'Максимум 50%', 'Max 80%', 'Максимум 80%'
  MG_A2 = 'Thrifty', 'Экономный стрелок', 'Rounds come back into the magazine more often (every 4th hit by default).', 'Патроны возвращаются в магазин чаще (по умолчанию каждое 4-е попадание).', 'Every 3rd hit', 'Каждое 3-е попадание', 'Every 3rd, and a kill +2 rounds', 'Каждое 3-е, и убийство +2 патрона', 'Every 2nd, and a kill +2 rounds', 'Каждое 2-е, и убийство +2 патрона'
  MG_A3 = 'Pin Down', 'Прижать', 'Fully suppressed zombies (at the maximum slow) take more damage.', 'Полностью подавленные зомби (на максимуме замедления) получают больше урона.', '+15% to suppressed', '+15% по подавленным', '+30%', '+30%', '+50%', '+50%'
  MG_A4 = 'Quick Suppression', 'Быстрое подавление', 'Every hit slows more (5% by default): fewer bullets to the maximum.', 'Каждое попадание замедляет сильнее (по умолчанию 5%): до максимума меньше пуль.', '15% per hit', '15% за попадание', '20% per hit', '20% за попадание', '40% per hit', '40% за попадание'
  MG_A5 = 'Explosive Rounds', 'Разрывные пули', 'Every 5th bullet is explosive: x2 to the target and a blast of 50% of the bullet to every zombie within 2 m (3 m around a fully suppressed one). The blast suppresses them too.', 'Каждая 5-я пуля разрывная: x2 цели и взрыв на 50% урона пули всем зомби в 2 м (3 м вокруг полностью подавленного). Взрыв тоже подавляет.', 'Every 5th bullet: x2, blast 50% within 2 m (3 m on suppressed)', 'Каждая 5-я пуля: x2, взрыв 50% в 2 м (3 м по подавленным)'
  MG_A6 = 'Hold the Line', 'Удержание позиции', 'While you crouch or stand still: less recoil and more damage.', 'Пока вы присели или стоите на месте: меньше отдачи и больше урона.', 'Still: recoil -25%, damage +15%', 'На месте: отдача -25%, урон +15%', 'Recoil -60%, damage +40%', 'Отдача -60%, урон +40%'
  MG_A7 = 'Kill Zone', 'Зона поражения', 'Suppression spreads: every hit also slows zombies next to the target at half strength.', 'Подавление расходится: каждое попадание замедляет и зомби рядом с целью на половину силы.', 'Neighbours within 2 m', 'Соседи в 2 м', 'Neighbours within 3 m', 'Соседи в 3 м'
  MG_A8 = 'Wall of Fire', 'Стена огня', 'Capstone. Fully suppressed zombies stop where they are while you keep hitting them (and 2 s more). Elites only slow by 80%. Downside: you move 40% slower while firing.', 'Капстоун. Полностью подавленные зомби останавливаются, пока вы по ним попадаете (и ещё 2 с). Элиты лишь замедлены на 80%. Минус: во время стрельбы вы двигаетесь на 40% медленнее.', 'Suppressed stand still; elites -80%; you -40% speed while firing', 'Подавленные стоят; элиты -80%; вы -40% скорости при стрельбе'
  MG_B1 = 'Spin Up', 'Раскрутка', 'Every second of continuous fire gives a spin-up stack (up to 5): higher fire rate.', 'Каждая секунда непрерывного огня даёт стак раскрутки (до 5): выше темп.', '+4% fire rate per stack (max +20%)', '+4% темпа за стак (до +20%)', '+7% per stack (max +35%)', '+7% за стак (до +35%)', '+12% per stack (max +60%)', '+12% за стак (до +60%)'
  MG_B2 = 'Iron Grip', 'Железная хватка', 'Less recoil in continuous fire. Strength = rank x your Fortitude level.', 'Меньше отдачи при непрерывном огне. Сила = ранг x уровень вашей Стойкости.', '-3% recoil per Fortitude level', '-3% отдачи за уровень Стойкости', '-5% per level', '-5% за уровень', '-8% per level (max -80%)', '-8% за уровень (до -80%)'
  MG_B3 = 'Drum Mag', 'Тяжёлый барабан', 'A bigger magazine: you keep the spin-up longer.', 'Больше магазин — дольше держишь раскрутку.', '+20% magazine', '+20% магазина', '+40%', '+40%', '+75%', '+75%'
  MG_B4 = 'Build-Up', 'Нарастание', 'Every spin-up stack also adds damage.', 'Каждый стак раскрутки ещё и добавляет урон.', '+3% damage per stack', '+3% урона за стак', '+6% per stack', '+6% за стак', '+10% per stack', '+10% за стак'
  MG_B5 = 'Belt Feed', 'Пулемётная лента', 'At full spin every kill loads 3 rounds into the magazine, and a reload does not reset the spin.', 'На полной раскрутке каждое убийство заряжает 3 патрона в магазин, а перезарядка не сбрасывает раскрутку.', 'Full spin: kill +3 rounds; a reload keeps the stacks', 'Полная раскрутка: убийство +3 патрона; перезарядка не сбрасывает стаки'
  MG_B6 = 'Shred', 'Кромсание', 'At full spin bullets tear flesh: a chance of bleeding and of torn-off limbs.', 'На полной раскрутке пули рвут плоть: шанс кровотечения и отрыва конечностей.', '50% bleeding, 25% limb', '50% кровотечения, 25% конечности', '90% bleeding, 60% limb', '90% кровотечения, 60% конечности'
  MG_B7 = 'Momentum', 'Без остановки', 'The spin-up does not vanish when you let go of the trigger: it melts slowly.', 'Раскрутка не пропадает, когда отпускаешь спуск, а тает постепенно.', 'Melts 1 stack per second', 'Тает на 1 стак в секунду', 'Melts 1 stack per 2 seconds', 'Тает на 1 стак за 2 секунды'
  MG_B8 = 'Minigun', 'Миниган', 'Capstone. Up to 10 spin-up stacks (twice as many). Downside: after 8 s of fire at the maximum it overheats and barely fires for 2 s.', 'Капстоун. До 10 стаков раскрутки (вдвое больше). Минус: после 8 с огня на максимуме — перегрев, 2 с почти не стреляет.', '10 stacks; overheats after 8 s at max (2 s)', '10 стаков; перегрев после 8 с на максимуме (2 с)'
  MG_H1 = 'Lead Storm', 'Свинцовый шторм', 'Hybrid. At full spin every hit counts twice: suppression builds twice as fast and rounds come back twice as often.', 'Гибрид. На полной раскрутке каждое попадание считается за два: подавление набирается вдвое быстрее, патроны возвращаются вдвое чаще.', 'Full spin: hits x2 for suppression and refunds', 'Полная раскрутка: попадания x2 для подавления и возврата'
  MG_H2 = 'Panic', 'Паника', 'Hybrid. A fully suppressed zombie killed by you spreads panic: zombies within 8 m of it are fully suppressed at once.', 'Гибрид. Полностью подавленный зомби, убитый вами, сеет панику: зомби в 8 м от него сразу полностью подавлены.', 'Killing a suppressed one suppresses everyone within 8 m', 'Убийство подавленного подавляет всех в 8 м'
  MG_H3 = 'Emplacement', 'Стационарная турель', 'Big hybrid (8 points in each branch). Crouched at full spin you become a turret: no recoil, every 3rd bullet free, zombies in a 15 m cone in front of you slowed by 30% even without a hit.', 'Большой гибрид (8 очков в каждой ветке). Присев на полной раскрутке, вы становитесь турелью: без отдачи, каждая 3-я пуля бесплатно, зомби в конусе 15 м перед вами замедлены на 30% даже без попадания.', 'Crouched + full spin: no recoil, every 3rd bullet free, cone -30%', 'Присед + полная раскрутка: без отдачи, каждая 3-я пуля бесплатно, конус -30%'
  Bows_Base = 'Splinter', 'Расщепление', 'On impact the arrow bursts into 2 splinters, each flies to the nearest other zombie within 5 m and hits for 50% of the arrow (at least 40). Branch A grows the splinters (crowds), branch B the arrow itself: full draw, piercing and the Hunter''s Wound (strong targets).', 'При попадании стрела разлетается на 2 осколка, каждый летит в ближайшего другого зомби в 5 м и бьёт на 50% урона стрелы (минимум 40). Ветка A развивает осколки (толпа), ветка B — саму стрелу: полное натяжение, пробивание и Охотничья рана (сильные цели).'
  Bows_A1 = 'More Splinters', 'Больше осколков', 'The arrow bursts into more splinters (2 by default).', 'Стрела разлетается на больше осколков (по умолчанию 2).', '4 splinters', '4 осколка', '6 splinters', '6 осколков', '10 splinters', '10 осколков'
  Bows_A2 = 'Sharp Splinters', 'Острые осколки', 'Splinters hit harder: a bigger share of the arrow and a higher minimum.', 'Осколки бьют сильнее: больше доля урона стрелы и выше минимум.', '60% of the arrow, at least 60', '60% урона стрелы, минимум 60', '75%, at least 80', '75%, минимум 80', '100%, at least 100', '100%, минимум 100'
  Bows_A3 = 'Wide Scatter', 'Широкий разлёт', 'Splinters look for targets farther away (5 m by default).', 'Осколки ищут цели дальше (по умолчанию 5 м).', '7 m', '7 м', '9 m', '9 м', '15 m', '15 м'
  Bows_A4 = 'Elemental Splinters', 'Стихийные осколки', 'If the arrow carries an element (a flaming arrow, or fire / acid / shock from the bow''s mod already on the target), splinters may pass it on. The chance is rolled for each splinter.', 'Если стрела несёт стихию (огненная стрела или огонь / кислота / ток от мода лука уже на цели), осколки могут передать её. Шанс бросается для каждого осколка.', '25% per splinter', '25% за осколок', '50% per splinter', '50% за осколок', 'Every splinter', 'Каждый осколок'
  Bows_A5 = 'Splinter Chain', 'Цепь осколков', 'A splinter that kills a zombie bursts into 2 more splinters (they do not split again).', 'Осколок, убивший зомби, разлетается ещё на 2 осколка (они дальше не делятся).', 'A splinter kill -> 2 more splinters', 'Убийство осколком -> ещё 2 осколка'
  Bows_A6 = 'Shrapnel Wound', 'Шрапнельная рана', 'Splinters slow zombies for 3 s.', 'Осколки замедляют зомби на 3 с.', '-25% speed', '-25% скорости', '-60% speed', '-60% скорости'
  Bows_A7 = 'Shock Trap', 'Электроловушка', 'An arrow that hits a zombie discharges electricity at once: everyone around is hit and shocked. An arrow in a wall or the ground is a trap: it waits up to 10 s and discharges when a zombie comes within 3 m (one trap at a time: a new one sets off the old one).', 'Стрела, попавшая в зомби, сразу бьёт током: все вокруг получают урон и шок. Стрела в стене или земле — ловушка: ждёт до 10 с и бьёт током, когда зомби подходит ближе 3 м (одна ловушка за раз: новая взрывает старую).', 'Discharge within 3 m: 50% of a splinter, shock 2 s', 'Разряд в 3 м: 50% осколка, шок 2 с', 'Discharge within 4.5 m: 100% of a splinter, shock 4 s', 'Разряд в 4.5 м: 100% осколка, шок 4 с'
  Bows_A8 = 'Splinter Storm', 'Осколочная буря', 'Capstone. Twice as many splinters, each one hits the head (x2). Downside: the arrow itself -50% damage.', 'Капстоун. Осколков вдвое больше, и каждый летит в голову (x2). Минус: сама стрела -50% урона.', 'Splinters x2 and in the head; the arrow -50%', 'Осколки x2 и в голову; стрела -50%'
  Bows_B1 = 'Full Draw', 'Полное натяжение', 'A fully drawn arrow hits harder. Strength = rank x your Agility level.', 'Полностью натянутая стрела бьёт сильнее. Сила = ранг x уровень вашей Ловкости.', '+3% per Agility level', '+3% за уровень Ловкости', '+5% per Agility level', '+5% за уровень Ловкости', '+10% per Agility level (max +100%)', '+10% за уровень Ловкости (до +100%)'
  Bows_B2 = 'Piercing Arrow', 'Сквозная стрела', 'A fully drawn arrow goes through the zombie and hits those behind it in a line (up to 15 m further).', 'Полностью натянутая стрела прошивает зомби и бьёт тех, кто стоит за ним в линию (до 15 м дальше).', '+1 zombie behind, 50% damage', '+1 зомби за целью, 50% урона', '+2 zombies, 60%', '+2 зомби, 60%', '+4 zombies, full damage', '+4 зомби, полный урон'
  Bows_B3 = 'Hunter''s Wound', 'Охотничья рана', 'A fully drawn hit leaves a wound for 8 s: the zombie takes more damage from all your attacks (any weapon) and moves slower. Up to 2 wounds.', 'Попадание полностью натянутой стрелой оставляет рану на 8 с: зомби получает больше урона от всех ваших атак (любым оружием) и медленнее двигается. До 2 ран.', 'Per wound +20% damage, -20% speed', 'За рану +20% урона, -20% скорости', 'Per wound +30%, -25%', 'За рану +30%, -25%', 'Per wound +50%, -40%', 'За рану +50%, -40%'
  Bows_B4 = 'Quick Draw', 'Быстрое натяжение', 'The bow draws faster.', 'Лук натягивается быстрее.', '+15% draw speed', '+15% скорости натяжения', '+30%', '+30%', '+50%', '+50%'
  Bows_B5 = 'Shadow', 'Тень', 'After a silent kill (the zombie did not see you) zombies do not notice you for 5 s, and the next arrow counts as from stealth (x2).', 'После тихого убийства (зомби вас не видел) 5 с вас не замечают, а следующая стрела считается из скрытности (x2).', 'Silent kill -> 5 s unnoticed, the next arrow x2', 'Тихое убийство -> 5 с незаметности, следующая стрела x2'
  Bows_B6 = 'Marksmanship', 'Меткость', 'A fully drawn headshot hits harder.', 'Выстрел в голову полностью натянутой стрелой сильнее.', '+25%', '+25%', '+75%', '+75%'
  Bows_B7 = 'Arrow Recovery', 'Сборщик стрел', 'The arrow that killed a zombie goes straight back into your backpack.', 'Стрела, убившая зомби, сразу возвращается в рюкзак.', '50% chance', 'Шанс 50%', 'Always', 'Всегда'
  Bows_B8 = 'Lance Arrow', 'Стрела-копьё', 'Capstone. A fully drawn arrow deals x2.5 and pierces every zombie in the line, wounding each. Downside: a fully drawn arrow does not splinter; splinters come only from quick (not fully drawn) shots.', 'Капстоун. Полностью натянутая стрела x2.5 урона и прошивает всех зомби в линии, каждому — Охотничья рана. Минус: полностью натянутая стрела не разлетается на осколки — осколки только от быстрых выстрелов.', 'Full draw x2.5, pierces the line with a wound; splinters only from quick shots', 'Полное натяжение x2.5, прошивает линию с раной; осколки только от быстрых выстрелов'
  Bows_H1 = 'Barbed Splinters', 'Зазубренные осколки', 'Hybrid. Every splinter leaves a Hunter''s Wound too (1 wound), so one arrow into a crowd prepares the whole group.', 'Гибрид. Каждый осколок тоже оставляет Охотничью рану (1 рану), так что одна стрела в толпу «готовит» всю группу.', 'Splinter = 1 Hunter''s Wound', 'Осколок = 1 Охотничья рана'
  Bows_H2 = 'Distraction', 'Отвлечение', 'Hybrid. Splinters that find no zombie hit the ground and make noise: zombies within 8 m go to look at the spot.', 'Гибрид. Осколки, не нашедшие зомби, бьют в землю и шумят: зомби в 8 м идут проверить место.', 'A missed splinter -> zombies go to the noise', 'Промах осколка -> зомби идут на шум'
  Bows_H3 = 'Thousand Needles', 'Тысяча игл', 'Big hybrid (8 points in each branch). Splinters of a fully drawn arrow get the Full Draw bonus and each pierces one more zombie.', 'Большой гибрид (8 очков в каждой ветке). Осколки полностью натянутой стрелы получают бонус Полного натяжения и каждый прошивает ещё одного зомби.', 'Full-draw splinters: draw bonus, pierce 1 zombie', 'Осколки с полного натяжения: бонус натяжения, прошивают 1 зомби'
  Crossbows_Base = 'Mark', 'Метка', 'A bolt marks the zombie for 10 s: all your damage to it (any weapon) +20%. Branch A grows the Mark (support for every weapon), branch B turns the bolt into a sticky charge that explodes.', 'Болт помечает зомби на 10 с: весь ваш урон по нему (любым оружием) +20%. Ветка A развивает Метку (поддержка всего оружия), ветка B делает из болта липкий заряд, который взрывается.'
  Crossbows_A1 = 'Deep Mark', 'Глубокая метка', 'A marked zombie takes even more damage from all your attacks (+20% by default).', 'Помеченный зомби получает ещё больше урона от всех ваших атак (по умолчанию +20%).', '+35%', '+35%', '+50%', '+50%', '+80%', '+80%'
  Crossbows_A2 = 'Lasting Mark', 'Долгая метка', 'The Mark lasts longer. Strength = rank x your Perception level, seconds on top of the base 10 s.', 'Метка держится дольше. Сила = ранг x уровень вашего Восприятия, секунд к базовым 10 с.', '+0.5 s x Perception (max +5 s)', '+0.5 с x Восприятие (до +5 с)', '+1 s x Perception (max +10 s)', '+1 с x Восприятие (до +10 с)', '+2 s x Perception (max +20 s)', '+2 с x Восприятие (до +20 с)'
  Crossbows_A3 = 'Spread Mark', 'Широкая метка', 'The bolt marks every zombie around the hit.', 'Болт помечает всех зомби вокруг места попадания.', 'Radius 2 m', 'Радиус 2 м', 'Radius 3 m', 'Радиус 3 м', 'Radius 5 m', 'Радиус 5 м'
  Crossbows_A4 = 'Weak Spot', 'Слабое место', 'Your body hits on a marked zombie count as headshots for other classes: the pistol Headhunter streak, the rifle Ricochet.', 'Ваши попадания в тело по помеченному зомби считаются выстрелами в голову для других классов: серия Охотника пистолета, Рикошет винтовки.', '25% of body hits', '25% попаданий в тело', '50% of body hits', '50% попаданий в тело', 'All body hits', 'Все попадания в тело'
  Crossbows_A5 = 'Elemental Mark', 'Метка стихий', 'Elements on a marked zombie (fire, shock, acid, bleeding) hit 50% harder.', 'Стихии на помеченном зомби (огонь, ток, кислота, кровотечение) бьют на 50% сильнее.', 'Elements on the marked +50%', 'Стихии на помеченном +50%'
  Crossbows_A6 = 'Mark Hop', 'Прыжок метки', 'When a marked zombie dies, the Mark jumps to the nearest one within 6 m with full time.', 'Когда помеченный зомби умирает, метка перескакивает на ближайшего в 6 м с полной длительностью.', 'Hops once', 'Перескакивает 1 раз', 'Hops up to 4 times', 'Перескакивает до 4 раз'
  Crossbows_A7 = 'Hunter''s Rhythm', 'Охотничий ритм', 'Killing a marked zombie (any weapon) speeds up crossbow reloads for 5 s.', 'Убийство помеченного зомби (любым оружием) ускоряет перезарядку арбалета на 5 с.', 'Reload +30%', 'Перезарядка +30%', 'Reload +100% (twice as fast)', 'Перезарядка +100% (вдвое быстрее)'
  Crossbows_A8 = 'Hunting Party', 'Охотничья стая', 'Capstone. The Mark bonus doubles, and every marked kill marks the 3 nearest zombies within 8 m. Downside: the bolt -40% damage (and the charge, it counts from the bolt) - the crossbow becomes support.', 'Капстоун. Бонус Метки удваивается, а каждое убийство помеченного помечает 3 ближайших зомби в 8 м. Минус: болт -40% урона (и заряд, он считается от болта) — арбалет становится поддержкой.', 'Mark x2, a marked kill marks 3 more; bolt and charge -40%', 'Метка x2, убийство помеченного -> метка ещё 3; болт и заряд -40%'
  Crossbows_B1 = 'Sticky Charge', 'Липкий заряд', 'The bolt sticks in the zombie (or a wall, the ground) and explodes 2 s later: damage to everyone around, as a real hit from you (gives XP, does not break blocks).', 'Болт застревает в зомби (или стене, земле) и через 2 с взрывается: урон всем вокруг как настоящий удар от вас (даёт опыт, не ломает блоки).', 'Blast: 30% of the bolt within 2 m', 'Взрыв: 30% урона болта в 2 м', '50% within 2.5 m', '50% в 2.5 м', '100% within 5 m', '100% в 5 м'
  Crossbows_B2 = 'Short Fuse', 'Короткий запал', 'The charge goes off sooner (2 s by default).', 'Заряд взрывается быстрее (по умолчанию 2 с).', '1.5 s', '1.5 с', '1 s', '1 с', '0.3 s (almost at once)', '0.3 с (почти сразу)'
  Crossbows_B3 = 'Incendiary Mix', 'Зажигательная смесь', 'The charge blast sets those it hits on fire.', 'Взрыв заряда поджигает задетых.', '25% to ignite', 'Поджог 25%', '50% to ignite', 'Поджог 50%', 'Always ignites, and a 1 s shock', 'Поджог всегда и шок 1 с'
  Crossbows_B4 = 'Quick Crank', 'Быстрый ворот', 'The crossbow reloads faster (slow in vanilla).', 'Арбалет перезаряжается быстрее (в ванили — медленно).', '+15% reload', '+15% перезарядки', '+30%', '+30%', '+60%', '+60%'
  Crossbows_B5 = 'Inner Charge', 'Внутренний заряд', 'A bolt in the head drives the charge inside: the target takes triple blast damage, and if it dies the head bursts and the blast knocks down everyone in the radius.', 'Болт в голову загоняет заряд внутрь: цель получает тройной урон взрыва, а если умирает — голова разрывается и взрыв сбивает с ног всех в радиусе.', 'Head bolt: blast x3 on the target; a kill knocks down everyone around', 'Болт в голову: взрыв x3 по цели; убийство — все вокруг сбиты с ног'
  Crossbows_B6 = 'Shockwave', 'Ударная волна', 'The blast throws zombies away from the centre.', 'Взрыв отбрасывает зомби от центра.', 'Throws back', 'Отбрасывает', 'Throws back and knocks down for 2 s', 'Отбрасывает и сбивает с ног на 2 с'
  Crossbows_B7 = 'Minefield', 'Минное поле', 'A charge in a wall or the ground does not explode at once: it waits up to 60 s (then goes off by itself; a mine over the limit sets off the oldest). When a zombie comes within 5 m, the mine goes off 1 s later - so those behind it come close too.', 'Заряд в стене или земле не взрывается сразу, а ждёт до 60 с (потом взрывается сам; мина сверх лимита взрывает самую старую). Когда зомби подходит ближе 5 м, мина взрывается через 1 с — чтобы успели подойти и идущие следом.', 'Up to 2 mines at once', 'До 2 мин одновременно', 'Up to 5 mines at once', 'До 5 мин одновременно'
  Crossbows_B8 = 'Cluster Bolt', 'Кассета', 'Capstone. The bolt splits into 3 charges that stick to the 3 nearest zombies within 5 m, each one marked. Every charge carries the whole branch; a bolt in the ground with Minefield sets 3 mines. Downside: each charge has 60% power, and the bolt itself 50%.', 'Капстоун. Болт распадается на 3 заряда, которые прилипают к трём ближайшим зомби в 5 м, и каждый помечен Меткой. Каждый заряд несёт всю ветку; болт в землю с Минным полем ставит сразу 3 мины. Минус: каждый заряд на 60% силы, а сам болт — на 50%.', '3 charges at 60% with a Mark; the bolt -50%', '3 заряда по 60% с Меткой; болт -50%'
  Crossbows_H1 = 'Marking Blast', 'Меченый взрыв', 'Hybrid. The charge blast marks everyone it hits.', 'Гибрид. Взрыв заряда помечает всех задетых.', 'Blast = Mark on everyone in the radius', 'Взрыв = метка всем в радиусе'
  Crossbows_H2 = 'Lure', 'Приманка', 'Hybrid. A charge in a marked zombie ignores Short Fuse and waits 3 s, while the zombie screams: everyone within 8 m runs to it.', 'Гибрид. Заряд в помеченном зомби игнорирует Короткий запал и ждёт 3 с, а сам зомби кричит: все в 8 м бегут к нему.', 'Marked with a charge: 3 s scream, the crowd within 8 m runs in, then the blast', 'Помеченный с зарядом: 3 с крика, толпа в 8 м бежит к нему, потом взрыв'
  Crossbows_H3 = 'Fireworks', 'Фейерверк', 'Big hybrid (8 points in each branch). A marked zombie killed by a charge blast passes a new charge and the Mark to the nearest zombie within 6 m - a chain while there is someone to kill.', 'Большой гибрид (8 очков в каждой ветке). Помеченный зомби, убитый взрывом заряда, передаёт новый заряд и метку ближайшему в 6 м — цепь, пока есть кого убивать.', 'Marked killed by a blast -> a new charge and Mark on the neighbour', 'Помеченный убит взрывом -> новый заряд и метка на соседа'
  Blades_Base = 'Bloodsucker', 'Кровопийца', 'Every hit is a regen stack (+1 HP/s, a power hit +2); stacks last 5 s from the last hit, max 2 (full Assassin set +2). A power hit makes the zombie bleed. Branch A grows the stacks and bleeding (long fights), branch B crits, back hits and finishing.', 'Каждый удар — стак регенерации (+1 HP/с, силовой +2); стаки держатся 5 с от удара, максимум 2 (полный сет Ассасина +2). Силовой удар вызывает кровотечение. Ветка A развивает стаки и кровотечение (долгий бой), ветка B — криты, удары в спину и добивание.'
  Blades_A1 = 'Thirst', 'Ненасытность', 'More Bloodsucker stacks (2 by default).', 'Больше стаков Кровопийцы (по умолчанию 2).', '4 stacks', '4 стака', '5 stacks', '5 стаков', '8 stacks', '8 стаков'
  Blades_A2 = 'Deep Cuts', 'Глубокие порезы', 'Normal hits may cause bleeding too (by default only power hits do).', 'Обычные удары тоже могут вызвать кровотечение (по умолчанию только силовые).', '15% chance', 'Шанс 15%', '30% chance', 'Шанс 30%', '60% chance', 'Шанс 60%'
  Blades_A3 = 'Hot Blood', 'Горячая кровь', 'Every Bloodsucker stack also adds damage.', 'Каждый стак Кровопийцы ещё и добавляет урон.', '+5% damage per stack', '+5% урона за стак', '+8% per stack', '+8% за стак', '+15% per stack (8 stacks = +120%)', '+15% за стак (8 стаков = +120%)'
  Blades_A4 = 'Open Wounds', 'Рваные раны', 'Your bleeding hits harder.', 'Ваше кровотечение бьёт сильнее.', '+30%', '+30%', '+60%', '+60%', '+120%', '+120%'
  Blades_A5 = 'Blood Plague', 'Зараза крови', 'Killing a bleeding zombie passes its bleeding to the 2 nearest within 4 m.', 'Убийство кровоточащего зомби передаёт его кровотечение двум ближайшим в 4 м.', 'A bleeding kill -> 2 neighbours bleed', 'Убийство кровоточащего -> кровотечение 2 соседям'
  Blades_A6 = 'Second Wind', 'Второе дыхание', 'Below 30% health every knife hit heals you.', 'При здоровье ниже 30% каждый удар ножом лечит.', '+2 HP per hit', '+2 HP за удар', '+5 HP per hit', '+5 HP за удар'
  Blades_A7 = 'Lasting Regen', 'Долгая регенерация', 'Stacks last longer (5 s by default).', 'Стаки держатся дольше (по умолчанию 5 с).', '8 s', '8 с', '15 s', '15 с'
  Blades_A8 = 'Blood Feast', 'Пир крови', 'Capstone. 10% of your damage to bleeding zombies comes back as health, a kill at max stacks heals 25 HP. Downside: hits on zombies that do not bleed -30%. Open with a power hit.', 'Капстоун. 10% урона по кровоточащим возвращается здоровьем, убийство на максимуме стаков лечит 25 HP. Минус: удары по некровоточащим -30%. Начинайте силовым.', '10% leech on bleeding, a max-stack kill +25 HP; -30% on others', 'Вампиризм 10% по кровоточащим, убийство на максимуме +25 HP; по остальным -30%'
  Blades_B1 = 'Keen Edge', 'Острое лезвие', 'A chance of a critical hit (x2). Strength = rank x your Agility level.', 'Шанс критического удара (x2). Сила = ранг x уровень вашей Ловкости.', '1% x Agility (max 10%)', '1% x Ловкость (до 10%)', '2.5% x Agility (max 25%)', '2.5% x Ловкость (до 25%)', '5% x Agility (max 50%)', '5% x Ловкость (до 50%)'
  Blades_B2 = 'Backstab', 'Удар в спину', 'More damage to a zombie with its back to you or that does not see you.', 'Больше урона по зомби, стоящему спиной или не видящему вас.', '+30%', '+30%', '+60%', '+60%', '+120%', '+120%'
  Blades_B3 = 'Critical Power', 'Сила крита', 'Crits hit harder (x2 by default).', 'Крит бьёт сильнее (по умолчанию x2).', 'x2.5', 'x2.5', 'x3', 'x3', 'x4', 'x4'
  Blades_B4 = 'Flurry', 'Шквал ударов', 'Faster attacks.', 'Быстрее удары.', '+10%', '+10%', '+25%', '+25%', '+50%', '+50%'
  Blades_B5 = 'Coup de Grace', 'Добивание', 'A hit on a zombie below 25% health or knocked down is a sure crit, and a non-elite dies at once.', 'Удар по зомби с здоровьем ниже 25% или сбитому с ног — гарантированный крит, а не-элита умирает сразу.', 'Below 25% or down: sure crit, non-elites at once', 'Ниже 25% или лежит: гарантированный крит, не-элиты сразу'
  Blades_B6 = 'Handoff', 'Перехват', 'A stealth knife kill (the zombie did not see you) primes the pistol for 5 s: the Headhunter streak does not start from zero.', 'Убийство ножом из скрытности (зомби вас не видел) на 5 с готовит пистолет: серия Охотника начинается не с нуля.', 'The pistol streak starts at 2', 'Серия пистолета начинается с 2', 'Starts at 3, and the pistol +20% damage for 5 s', 'С 3, и пистолет +20% урона на 5 с'
  Blades_B7 = 'Execution Chain', 'Цепь казней', 'A crit kill makes the next hits sure crits.', 'Убийство критом делает следующие удары гарантированными критами.', 'The next 1 hit is a crit', 'Следующий 1 удар — крит', 'The next 3 hits are crits', 'Следующие 3 удара — криты'
  Blades_B8 = 'Spine Strike', 'Удар в хребет', 'Capstone. A real back hit (the zombie faces away - any time in a fight) deals x2 and goes straight through armor and the Armored elite''s halving. Downside: hits in the face -25%.', 'Капстоун. Настоящий удар в спину (зомби стоит спиной — в любой момент боя) бьёт x2 и проходит сквозь броню и половинную защиту Бронированных элит. Минус: удары в лицо -25%.', 'A real back hit: x2 through armor; in the face -25%', 'Настоящий удар в спину: x2 сквозь броню; в лицо -25%'
  Blades_H1 = 'Bloody Crit', 'Кровавый крит', 'Hybrid. A crit always causes bleeding and gives +3 stacks. A crit on a zombie that already bleeds tears the wound open: extra bleeding damage at once.', 'Гибрид. Крит всегда вызывает кровотечение и даёт +3 стака. Крит по уже кровоточащему разрывает рану: дополнительный урон кровотечения сразу.', 'Crit: bleeding and +3 stacks; on a bleeding one - the wound tears', 'Крит: кровотечение и +3 стака; по кровоточащему — разрыв раны'
  Blades_H2 = 'Blood Scent', 'Запах крови', 'Hybrid. A bleeding zombie is "blind with pain": it gets the Backstab bonus even facing you (not the Spine Strike capstone).', 'Гибрид. Кровоточащий зомби «слепнет от боли»: получает бонус Удара в спину даже лицом к вам (не капстоун Удар в хребет).', 'A bleeding one gets the Backstab bonus', 'Кровоточащий получает бонус Удара в спину'
  Blades_H3 = 'Blade Dance', 'Танец клинка', 'Big hybrid (8 points in each branch). Every crit refreshes all stacks and heals 3 HP; at max stacks every 3rd hit is a sure crit.', 'Большой гибрид (8 очков в каждой ветке). Каждый крит обновляет все стаки и лечит 3 HP, а на максимуме стаков каждый 3-й удар — гарантированный крит.', 'Crit: stacks refreshed, +3 HP; at max stacks every 3rd hit crits', 'Крит: обновление стаков, +3 HP; на максимуме каждый 3-й удар — крит'
  Clubs_Base = 'Home Run', 'Хоумран', 'A normal hit stuns for 1 s with a 15% chance, a power hit to the head launches the zombie and knocks it down (force 800). A stunned zombie cannot be stunned again for 3 s. Branch A grows the power hit and control, branch B Rage: damage and speed stacks from kills.', 'Обычный удар с шансом 15% оглушает на 1 с, силовой в голову подбрасывает и сбивает зомби (сила 800). Оглушённого 3 с нельзя оглушить снова. Ветка A развивает силовой удар и контроль, ветка B — Ярость: стаки урона и скорости от убийств.'
  Clubs_A1 = 'Stunning Blow', 'Оглушающий удар', 'Normal hits stun more often (15%, 1 s by default).', 'Обычный удар чаще оглушает (по умолчанию 15%, 1 с).', '20%', '20%', '25%', '25%', '40%', '40%'
  Clubs_A2 = 'Wind-Up', 'Замах', 'A power hit to the head throws farther. Strength = rank x your Strength level.', 'Силовой удар в голову отбрасывает дальше. Сила = ранг x уровень вашей Силы.', '+20 x Strength (max +200)', '+20 x Сила (до +200)', '+35 x Strength (max +350)', '+35 x Сила (до +350)', '+60 x Strength (max +600)', '+60 x Сила (до +600)'
  Clubs_A3 = 'Hard Landing', 'Жёсткое приземление', 'A launched zombie knocks down those near where it lands.', 'Подброшенный зомби, падая, сбивает тех, кто рядом.', 'Within 1.5 m', 'В 1.5 м', 'Within 2 m', 'В 2 м', 'Within 3 m, each takes 50% of the blow', 'В 3 м, каждый получает 50% удара'
  Clubs_A4 = 'Kick Them While Down', 'По оглушённому', 'A power hit on a stunned zombie hits harder.', 'Силовой удар по оглушённому зомби бьёт сильнее.', '+20%', '+20%', '+40%', '+40%', '+80%', '+80%'
  Clubs_A5 = 'Concussion', 'Сотрясение', 'The stun lasts 2 s (instead of 1), and a stunned zombie takes +50% from everything - any weapon and element.', 'Оглушение длится 2 с (вместо 1), а оглушённый зомби получает +50% урона от всего — любого оружия и стихий.', '2 s stun; stunned +50% from everything', 'Оглушение 2 с; оглушённые +50% от всего'
  Clubs_A6 = 'Easy Swing', 'Лёгкий замах', 'Power hits cost less stamina.', 'Силовой удар тратит меньше выносливости.', '-20%', '-20%', '-50%', '-50%'
  Clubs_A7 = 'Batting Streak', 'Серия подач', 'Every power hit within 3 s of the last one hits harder (up to 3 in a row).', 'Каждый силовой удар в течение 3 с после предыдущего бьёт сильнее (до 3 подряд).', '+15% per hit of the streak', '+15% за удар серии', '+40% per hit of the streak', '+40% за удар серии'
  Clubs_A8 = 'Grand Slam', 'Большой шлем', 'Capstone. A power hit to the head always launches (elites too), and the launched zombie flies like a projectile: everyone it hits takes 100% of the blow and falls. Downside: power hits cost 50% more stamina.', 'Капстоун. Силовой удар в голову всегда подбрасывает (даже элит), а подброшенный зомби летит как снаряд: каждый, в кого он попал, получает 100% удара и падает. Минус: силовой удар +50% выносливости.', 'Always launches, the flying zombie hits 100% and knocks down; power +50% stamina', 'Подбрасывает всегда, летящий зомби бьёт 100% и сбивает; силовой +50% выносливости'
  Clubs_B1 = 'Rage', 'Ярость', 'A club kill gives a Rage stack for 8 s: +10% damage and +5% attack speed per stack.', 'Убийство дубинкой даёт стак Ярости на 8 с: +10% урона и +5% скорости ударов за стак.', 'Up to 2 stacks', 'До 2 стаков', 'Up to 4 stacks', 'До 4 стаков', 'Up to 8 stacks', 'До 8 стаков'
  Clubs_B2 = 'Heavy Hands', 'Тяжёлые руки', 'Normal hits hit harder.', 'Обычные удары бьют сильнее.', '+10%', '+10%', '+20%', '+20%', '+40%', '+40%'
  Clubs_B3 = 'Frenzy', 'Неистовство', 'Every Rage stack gives more damage (+10% by default).', 'Каждый стак Ярости даёт больше урона (по умолчанию +10%).', '+15% per stack', '+15% за стак', '+20% per stack', '+20% за стак', '+30% per stack', '+30% за стак'
  Clubs_B4 = 'Tireless', 'Неутомимость', 'Normal hits cost less stamina.', 'Обычные удары тратят меньше выносливости.', '-15%', '-15%', '-30%', '-30%', '-60%', '-60%'
  Clubs_B5 = 'Rampage', 'Буйство', 'At max Rage hits cost no stamina, and you cannot be stunned or knocked down.', 'На максимуме Ярости удары не тратят выносливость, а вас нельзя оглушить или сбить с ног.', 'Max Rage: no stamina, no stun, no knockdown', 'Максимум Ярости: без выносливости, не оглушить и не сбить'
  Clubs_B6 = 'Hold the Rage', 'Держи ярость', 'Rage stacks last longer (8 s by default).', 'Стаки Ярости держатся дольше (по умолчанию 8 с).', '12 s', '12 с', '20 s', '20 с'
  Clubs_B7 = 'Rage Wave', 'Волна ярости', 'A kill at max Rage stuns every zombie around (once per 5 s; a just stunned zombie is immune for 3 s).', 'Убийство на максимуме Ярости оглушает всех зомби вокруг (не чаще раза в 5 с; только что оглушённый 3 с не оглушается).', 'Within 3 m, once per 5 s', 'В 3 м, раз в 5 с', 'Within 5 m and throws back, once per 5 s', 'В 5 м и отбрасывает, раз в 5 с'
  Clubs_B8 = 'Berserk', 'Берсерк', 'Capstone. Max Rage stacks double (up to 16), and the stacks heal: +1 HP per hit for every 4 stacks. Downside: you take +25% damage.', 'Капстоун. Максимум стаков Ярости удваивается (до 16), а стаки лечат: +1 HP за удар за каждые 4 стака. Минус: получаемый урон +25%.', 'Stacks x2, healing hits; you take +25%', 'Стаков x2, лечение от ударов; получаемый урон +25%'
  Clubs_H1 = 'Stunning Rage', 'Оглушающая ярость', 'Hybrid. Stunning a zombie gives a Rage stack too (not only a kill).', 'Гибрид. Оглушение зомби тоже даёт стак Ярости (не только убийство).', 'A stun = +1 Rage stack', 'Оглушение = +1 стак Ярости'
  Clubs_H2 = 'Angry Home Run', 'Злой хоумран', 'Hybrid. At max Rage every power hit launches the zombie, not only to the head, and Hard Landing is twice as wide.', 'Гибрид. На максимуме Ярости каждый силовой удар подбрасывает зомби, не только в голову, а Жёсткое приземление вдвое шире.', 'Max Rage: every power hit launches, landing x2 radius', 'Максимум Ярости: силовой всегда подбрасывает, приземление x2 радиус'
  Clubs_H3 = 'Crowd Breaker', 'Гроза толпы', 'Big hybrid (8 points in each branch). A power kill gives 2 stacks and refreshes all; at max Rage every normal hit stuns.', 'Большой гибрид (8 очков в каждой ветке). Убийство силовым даёт 2 стака и обновляет все; на максимуме Ярости каждый обычный удар оглушает.', 'Power kill: +2 stacks; max Rage - every hit stuns', 'Убийство силовым: +2 стака; максимум Ярости — каждый удар оглушает'
  Sledges_Base = 'Earthquake', 'Землетрясение', 'A normal hit knocks the target down with a 20% chance; a power hit knocks down every zombie within 3 m and deals each 10% of its damage (10 s cooldown, power hits +50% stamina). Branch A grows the Earthquake (crowds), branch B the hit on one target.', 'Обычный удар с шансом 20% сбивает цель с ног; силовой сбивает всех зомби в 3 м и наносит каждому 10% своего урона (перезарядка 10 с, силовой +50% выносливости). Ветка A развивает Землетрясение (толпа), ветка B — удар по одной цели.'
  Sledges_A1 = 'Wide Quake', 'Широкий толчок', 'The Earthquake knocks down zombies farther away (3 m by default).', 'Землетрясение сбивает зомби дальше (по умолчанию 3 м).', '3.5 m', '3.5 м', '4 m', '4 м', '6 m', '6 м'
  Sledges_A2 = 'Shockforce', 'Ударная сила', 'Every zombie knocked down by the quake takes a bigger share of the power hit (10% by default).', 'Каждый сбитый землетрясением получает большую долю силового удара (по умолчанию 10%).', '20%', '20%', '30%', '30%', '50%', '50%'
  Sledges_A3 = 'Frequent Quakes', 'Частые толчки', 'The Earthquake recharges faster (10 s by default).', 'Землетрясение перезаряжается быстрее (по умолчанию 10 с).', '8 s', '8 с', '6 s', '6 с', '3 s', '3 с'
  Sledges_A4 = 'Stagger', 'Пошатывание', 'Normal hits knock the target down more often (20% by default).', 'Обычный удар чаще сбивает цель (по умолчанию 20%).', '25%', '25%', '30%', '30%', '45%', '45%'
  Sledges_A5 = 'Elemental Quake', 'Стихийный толчок', 'The quake spreads the target''s elements (fire, shock, acid, bleeding) to everyone it knocks down.', 'Землетрясение разносит стихии с цели (огонь, ток, кислоту, кровотечение) на всех сбитых.', 'The target''s elements -> everyone knocked down', 'Стихии цели -> всем сбитым'
  Sledges_A6 = 'Trembling Ground', 'Земля дрожит', 'Knocked-down zombies stay down longer.', 'Сбитые зомби лежат дольше.', '+1 s', '+1 с', '+2.5 s', '+2.5 с'
  Sledges_A7 = 'Rift', 'Разлом', 'After a quake the ground around (10 m) slows zombies for 10 s - also those who get up and those who come.', 'После землетрясения земля вокруг (10 м) 10 с замедляет зомби — и поднявшихся, и подходящих.', '-30% speed', '-30% скорости', '-60% speed', '-60% скорости'
  Sledges_A8 = 'Aftershock', 'Афтершок', 'Capstone. 1.5 s after a quake comes a second one of the same force, and the knocked-down zombies stay stunned 3 s after getting up. Downside: the quake cooldown +2 s.', 'Капстоун. Через 1.5 с после землетрясения — второй толчок той же силы, а сбитые ещё 3 с оглушены после подъёма. Минус: перезарядка землетрясения +2 с.', 'A second quake after 1.5 s, stunned 3 s after getting up; cooldown +2 s', 'Второй толчок через 1.5 с, оглушение 3 с после подъёма; перезарядка +2 с'
  Sledges_B1 = 'Heavy Hammer', 'Тяжёлый молот', 'Power hits hit harder.', 'Силовой удар бьёт сильнее.', '+20%', '+20%', '+40%', '+40%', '+80%', '+80%'
  Sledges_B2 = 'Iron Lungs', 'Железные лёгкие', 'Sledgehammer swings cost less stamina. Strength = rank x your Fortitude level.', 'Удары кувалдой тратят меньше выносливости. Сила = ранг x уровень вашей Стойкости.', '-2% x Fortitude (max -20%)', '-2% x Стойкость (до -20%)', '-4% x Fortitude (max -40%)', '-4% x Стойкость (до -40%)', '-8% x Fortitude (max -80%)', '-8% x Стойкость (до -80%)'
  Sledges_B3 = 'Armor Break', 'Ломка брони', 'A hit strips part of the zombie''s armor for 10 s (vanilla armor: worker and biker 20%, soldier 50%, Demolisher 60%).', 'Удар снимает с зомби часть брони на 10 с (в ванили броня: рабочий и байкер 20%, солдат 50%, Разрушитель 60%).', '-10% armor', '-10% брони', '-20% armor', '-20% брони', '-40% armor and the Armored elite loses its half protection', '-40% брони и половинная защита Бронированных элит'
  Sledges_B4 = 'Momentum', 'Размах', 'Every next hit on the same zombie hits harder (up to 3 in a row, resets after 3 s without a hit).', 'Каждый следующий удар по тому же зомби сильнее (до 3 подряд, сбрасывается через 3 с без удара).', '+10% per hit', '+10% за удар', '+15% per hit', '+15% за удар', '+25% per hit (max +75%)', '+25% за удар (до +75%)'
  Sledges_B5 = 'Overkill', 'Избыток', 'A power hit''s damage beyond the target''s health is not lost: it goes to the nearest zombie within 4 m.', 'Урон силового удара сверх здоровья цели не пропадает: переходит на ближайшего зомби в 4 м.', 'Power excess -> the nearest within 4 m', 'Избыток силового -> ближайшему в 4 м'
  Sledges_B6 = 'Crush', 'Сокрушение', 'A power hit on a knocked-down zombie hits harder.', 'Силовой удар по сбитому с ног зомби бьёт сильнее.', '+50%', '+50%', '+120%', '+120%'
  Sledges_B7 = 'Duel', 'Дуэль', 'A power hit is stronger when no other zombie is within 5 m of the target. Lure one away from the group - in a crowd the skill is silent.', 'Силовой удар сильнее, если в 5 м от цели нет других зомби. Выманивайте одного из группы — в толпе навык молчит.', 'One on one: power +60%', 'Один на один: силовой +60%', 'One on one: power +150%', 'Один на один: силовой +150%'
  Sledges_B8 = 'Execution', 'Казнь', 'Capstone. A power hit kills a zombie below 40% health at once (an elite below 20%). Downside: power hits cost another +50% stamina.', 'Капстоун. Силовой удар по зомби с здоровьем ниже 40% убивает сразу (элиту — ниже 20%). Минус: силовой удар стоит ещё +50% выносливости.', 'Power: below 40% (elites 20%) - at once; power +50% stamina', 'Силовой: ниже 40% (элиты 20%) — сразу; силовой +50% выносливости'
  Sledges_H1 = 'Fault Line', 'Трещина', 'Hybrid. The Earthquake puts Armor Break on everyone it knocks down.', 'Гибрид. Землетрясение накладывает Ломку брони на всех сбитых.', 'Knocked down by the quake - with Armor Break', 'Сбитые землетрясением — с Ломкой брони'
  Sledges_H2 = 'Heavy Quake', 'Тяжёлый толчок', 'Hybrid. Knocked-down zombies take a share of the boosted power hit (Heavy Hammer, Momentum...), not of the base one.', 'Гибрид. Сбитые получают долю усиленного силового удара (Тяжёлый молот, Размах...), а не базового.', 'Share of the boosted hit, not the base', 'Доля усиленного удара, а не базового'
  Sledges_H3 = 'Doomsday', 'Конец света', 'Big hybrid (8 points in each branch). A power kill starts a quake at once, ignoring the cooldown, and that quake hits everyone it knocks down for 100% of the blow.', 'Большой гибрид (8 очков в каждой ветке). Убийство силовым сразу запускает землетрясение без перезарядки, и этот толчок бьёт каждого сбитого на 100% удара.', 'Power kill -> a quake with no cooldown, 100% to each', 'Убийство силовым -> землетрясение без перезарядки, 100% каждому'
  Spears_Base = 'Battering Ram', 'Таран', 'A normal hit pushes the target back (force 120), a power hit goes through one zombie behind and throws each one back. Branch A is the line and the distance (keep zombies on the spear tip), branch B fencing: quick thrusts, chains, parries, fighting on the move.', 'Обычный удар отталкивает цель (сила 120), силовой пробивает одного зомби позади и отбрасывает каждого. Ветка A — линия и дистанция (держать зомби на кончике копья), ветка B — фехтование: быстрые уколы, серии, парирования, бой в движении.'
  Spears_A1 = 'Strong Push', 'Сильный толчок', 'A normal hit pushes harder (120 by default).', 'Обычный удар отталкивает сильнее (по умолчанию 120).', '160', '160', '200', '200', '320', '320'
  Spears_A2 = 'Through Strike', 'Сквозной удар', 'A power hit goes through more zombies behind (1 by default).', 'Силовой удар пробивает больше зомби позади (по умолчанию 1).', '2 zombies', '2 зомби', '3 zombies', '3 зомби', '6 zombies', '6 зомби'
  Spears_A3 = 'Spear Tip', 'Кончик копья', 'A hit on a zombie at the edge of reach (2.5 m and more) hits harder. Keep the distance: up close there is no bonus.', 'Удар по зомби на краю досягаемости (2.5 м и дальше) бьёт сильнее. Держите дистанцию: вплотную бонуса нет.', '+20%', '+20%', '+40%', '+40%', '+80%', '+80%'
  Spears_A4 = 'Bowling', 'Кегли', 'A zombie pushed by your hit knocks down the one it flies into.', 'Отброшенный вашим ударом зомби, влетев в другого, сбивает его с ног.', '15% chance', 'Шанс 15%', '30% chance', 'Шанс 30%', '60% chance', 'Шанс 60%'
  Spears_A5 = 'Tip in the Wound', 'Остриё в ране', 'After a power hit the tip stays in the first target: every next spear hit on it +25%, up to 4 times (+100%), while it lives. Finish the one with the tip in it.', 'После силового удара остриё остаётся в первой цели: каждый следующий удар копьём по ней +25%, до 4 раз (+100%), пока она жива. Добивайте ту, в которой остриё.', 'A power hit leaves the tip: hits on it +25% each, up to +100%', 'Силовой оставляет остриё: удары по ней +25% каждый, до +100%'
  Spears_A6 = 'Hit and Back', 'Удар и отход', 'For 2 s after your hit you move faster - time to step back to spear distance.', '2 с после вашего удара вы двигаетесь быстрее — успеть отступить на дистанцию копья.', '+15% speed', '+15% скорости', '+40% speed', '+40% скорости'
  Spears_A7 = 'Sentry', 'Часовой', 'A zombie that comes closer than 2 m gets a thrust by itself (normal damage and a push) - the spear keeps the distance for you.', 'Зомби, подошедший ближе 2 м, сам получает укол (обычный урон и толчок) — копьё держит дистанцию за вас.', 'Not more often than once per 10 s', 'Не чаще раза в 10 с', 'Once per 5 s', 'Раз в 5 с'
  Spears_A8 = 'Breakthrough', 'Прорыв', 'Capstone. A power hit while running forward becomes a dash: you fly 6 m through the crowd, and everyone on the way is pierced, knocked down and takes x2. Downside: for 1.5 s after the dash you can barely attack.', 'Капстоун. Силовой удар на бегу вперёд становится рывком: вы пролетаете 6 м сквозь толпу, и каждый на пути пробит, сбит с ног и получает x2. Минус: 1.5 с после рывка вы почти не можете атаковать.', 'A running power hit - a 6 m dash: x2 and knockdown on the way; no attacks for 1.5 s', 'Силовой на бегу — рывок 6 м: x2 и сбивание на пути; 1.5 с без атак'
  Spears_B1 = 'Quick Thrusts', 'Быстрые уколы', 'Faster attacks. Strength = rank x your Agility level.', 'Быстрее удары. Сила = ранг x уровень вашей Ловкости.', '+2% x Agility (max +20%)', '+2% x Ловкость (до +20%)', '+3% x Agility (max +30%)', '+3% x Ловкость (до +30%)', '+6% x Agility (max +60%)', '+6% x Ловкость (до +60%)'
  Spears_B2 = 'Deep Thrust', 'Глубокий укол', 'A normal thrust may cause bleeding.', 'Обычный укол может вызвать кровотечение.', '15% chance', 'Шанс 15%', '30% chance', 'Шанс 30%', '60% chance', 'Шанс 60%'
  Spears_B3 = 'Head Thrust', 'Укол в голову', 'A spear hit to the head hits harder - aim, do not just poke.', 'Удар копьём в голову бьёт сильнее — цельтесь, а не просто тыкайте.', '+30%', '+30%', '+60%', '+60%', '+120%', '+120%'
  Spears_B4 = 'Thrust Chain', 'Серия уколов', 'Every next hit on the same zombie (no more than 1 s apart) hits harder, up to 5 times. Another target or a pause - the chain burns out.', 'Каждый следующий удар по той же цели (с паузой не больше 1 с) сильнее, до 5 раз. Другая цель или пауза — серия сгорает.', '+5% per hit (max +25%)', '+5% за удар (до +25%)', '+8% per hit (max +40%)', '+8% за удар (до +40%)', '+15% per hit (max +75%)', '+15% за удар (до +75%)'
  Spears_B5 = 'Parry', 'Парирование', 'A hit on a zombie that is just swinging at you (a moment before its blow) interrupts its attack and deals x2. Catch the moment.', 'Удар по зомби, который как раз замахивается на вас (за миг до его удара), прерывает его атаку и бьёт x2. Ловите момент.', 'A hit during the zombie swing: attack interrupted, x2', 'Удар во время замаха зомби: атака прервана, x2'
  Spears_B6 = 'Dance', 'Танец', 'Hits made on the move (you are moving when you hit) are stronger. Standing - no bonus.', 'Удары на ходу (вы двигаетесь в момент удара) сильнее. Стоя — бонуса нет.', '+10%', '+10%', '+30%', '+30%'
  Spears_B7 = 'Riposte', 'Контрвыпад', 'After a successful parry the next thrusts are 50% faster.', 'После удачного парирования следующие уколы на 50% быстрее.', '2 thrusts', '2 укола', '4 thrusts', '4 укола'
  Spears_B8 = 'Whirlwind', 'Вихрь', 'Capstone. Every 4th hit of a thrust chain is a sweep around you (every zombie in reach) with bleeding. Downside: an open stance - you take +20% damage.', 'Капстоун. Каждый 4-й удар серии уколов — удар вокруг себя (все зомби в досягаемости) с кровотечением. Минус: открытая стойка — получаемый урон +20%.', 'Every 4th chain hit - a sweep with bleeding; you take +20%', 'Каждый 4-й удар серии — вихрь с кровотечением; получаемый урон +20%'
  Spears_H1 = 'Bloody Skewer', 'Кровавый шампур', 'Hybrid. Everyone a power hit goes through bleeds.', 'Гибрид. Все, кого пробил силовой удар, кровоточат.', 'Pierced by a power hit - bleeding', 'Пробитые силовым — кровотечение'
  Spears_H2 = 'Parting Shot', 'Удар на отходе', 'Hybrid. The first hit after Hit and Back (when you stepped 2.5 m away) counts as a head thrust.', 'Гибрид. Первый удар после «Удара и отхода» (когда вы отошли на 2.5 м) считается уколом в голову.', 'Back at distance -> the next hit as to the head', 'Отошли на дистанцию -> следующий удар как в голову'
  Spears_H3 = 'Spear Master', 'Мастер копья', 'Big hybrid (8 points in each branch). A successful parry throws the zombie 3 m back and goes through everyone behind it, like a power hit.', 'Большой гибрид (8 очков в каждой ветке). Удачное парирование отбрасывает зомби на 3 м и пробивает всех за ним, как силовой удар.', 'Parry = a power hit: thrown 3 m and the line pierced', 'Парирование = силовой удар: отброс 3 м и пробитие линии'
  Knuckles_Base = 'Counter Punch', 'Контрудар', 'Every hit you take gives a charge (max 2, no timer). A normal hit spends 1 charge and deals +25%; a power hit spends all, +50% per charge, and knocks down. Branch A is the tank: they hit you, you hit back; branch B the opposite: a combo while nothing hits you.', 'Каждый полученный удар даёт заряд (максимум 2, без таймера). Обычный удар тратит 1 заряд и бьёт +25%; силовой тратит все, +50% за каждый, и сбивает с ног. Ветка A — танк: вас бьют, вы отвечаете; ветка B — наоборот: серия, пока вас не задели.'
  Knuckles_A1 = 'More Charges', 'Больше зарядов', 'Max Counter Punch charges (2 by default).', 'Максимум зарядов Контрудара (по умолчанию 2).', '3', '3', '4', '4', '7', '7'
  Knuckles_A2 = 'Iron Skin', 'Железная кожа', 'While you have at least one charge you take less damage. Strength = rank x your Fortitude level. A power hit that spends them all leaves you without it until you are hit again.', 'Пока есть хоть один заряд, вы получаете меньше урона. Сила = ранг x уровень вашей Стойкости. Потратили все силовым — защиты нет, пока снова не ударят.', '-1% x Fortitude (max -10%)', '-1% x Стойкость (до -10%)', '-2% x Fortitude (max -20%)', '-2% x Стойкость (до -20%)', '-4% x Fortitude (max -40%)', '-4% x Стойкость (до -40%)'
  Knuckles_A3 = 'Retort', 'Ответ', 'A normal hit with a charge hits harder (+25% by default).', 'Обычный удар с зарядом бьёт сильнее (по умолчанию +25%).', '+40%', '+40%', '+60%', '+60%', '+100%', '+100%'
  Knuckles_A4 = 'Take It', 'Держи удар', 'A hit you take may give 2 charges at once.', 'Полученный удар может дать сразу 2 заряда.', '15% chance', 'Шанс 15%', '30% chance', 'Шанс 30%', '60% chance', 'Шанс 60%'
  Knuckles_A5 = 'Recoil', 'Отдача', 'If you are hit while your charges are full, the attacker is thrown back and stunned for 1 s.', 'Если вас бьют при полных зарядах, нападающего отбрасывает и оглушает на 1 с.', 'A hit on you at full charges: the attacker thrown back and stunned', 'Удар по вам при полных зарядах: нападающий отброшен и оглушён'
  Knuckles_A6 = 'Lick Wounds', 'Зализать раны', 'A power hit that spends charges heals you.', 'Силовой удар, тратящий заряды, лечит вас.', '+3 HP per charge', '+3 HP за заряд', '+8 HP per charge', '+8 HP за заряд'
  Knuckles_A7 = 'Blood for Blood', 'Кровь за кровь', 'The damage you took since your last power hit is added to the next power hit.', 'Урон, полученный после последнего силового удара, добавляется к следующему силовому.', '50% of the damage taken', '50% полученного урона', '150% of the damage taken', '150% полученного урона'
  Knuckles_A8 = 'Knockout', 'Нокаут', 'Capstone. A power hit at full charges kills any non-elite zombie; an elite takes x3 and stays down 3 s. Downside: a heavy stance - for 15 s after it you move 20% slower.', 'Капстоун. Силовой удар на полных зарядах убивает любого не-элитного зомби, а элите наносит x3 и валит на 3 с. Минус: тяжёлая стойка — 15 с после такого удара вы двигаетесь на 20% медленнее.', 'Power at full charges: non-elites at once, elites x3 and down 3 s; then 15 s -20% speed', 'Силовой на полных: не-элита сразу, элита x3 и лежит 3 с; потом 15 с -20% скорости'
  Knuckles_B1 = 'Combo', 'Серия', 'Every hit of the combo, while nothing hits you, deals more (up to 10 hits). Get hit - the combo burns out.', 'Каждый удар серии, пока вас не задели, наносит больше (до 10 ударов). Пропустили удар — серия сгорает.', '+2% per hit (max +20%)', '+2% за удар (до +20%)', '+3% per hit (max +30%)', '+3% за удар (до +30%)', '+6% per hit (max +60%)', '+6% за удар (до +60%)'
  Knuckles_B2 = 'Light Feet', 'Лёгкие ноги', 'In combat (5 s after a hit) you move faster - easier not to get hit.', 'В бою (5 с после удара) вы двигаетесь быстрее — легче не пропускать удары.', '+5%', '+5%', '+10%', '+10%', '+20%', '+20%'
  Knuckles_B3 = 'Fast Hands', 'Быстрые руки', 'Faster punches.', 'Быстрее удары.', '+10%', '+10%', '+20%', '+20%', '+40%', '+40%'
  Knuckles_B4 = 'Slip', 'Уклон', 'From a 5-hit combo a zombie blow at you may miss - the combo stays.', 'От 5 ударов серии удар зомби по вам может промахнуться — серия не сгорает.', '15% chance', 'Шанс 15%', '25% chance', 'Шанс 25%', '40% chance', 'Шанс 40%'
  Knuckles_B5 = 'Wall of Fists', 'Стена ударов', 'At a 10+ combo every punch cuts the target''s swing: while you keep hitting it, it cannot hit back. Others around can.', 'На серии 10+ каждый ваш удар сбивает замах цели: пока вы бьёте её без остановки, она не успевает ответить. Другие вокруг — могут.', 'Combo 10+: the target cannot attack while you hit', 'Серия 10+: цель не может атаковать, пока вы бьёте'
  Knuckles_B6 = 'Opener', 'Первый удар', 'The first hit after 5 s untouched hits harder - come in unexpected.', 'Первый удар после 5 с, когда вас никто не задел, бьёт сильнее — заходите внезапно.', 'x2', 'x2', 'x3', 'x3'
  Knuckles_B7 = 'Second Chance', 'Вторая попытка', 'A hit you take does not reset the combo - a few times per combo.', 'Пропущенный удар не сбрасывает серию — несколько раз за серию.', 'Once per combo', '1 раз за серию', 'Twice per combo', '2 раза за серию'
  Knuckles_B8 = 'Hundred Fists', 'Сто кулаков', 'Capstone. The combo goes up to 20; from 15 your punches are 50% faster, and every 5th hits everyone near. Downside: a glass cannon - you take +30% damage.', 'Капстоун. Серия растёт до 20 ударов; с 15-го удары на 50% быстрее, а каждый 5-й бьёт всех рядом. Минус: «стеклянная пушка» — получаемый урон +30%.', 'Combo to 20, from 15 +50% speed, every 5th hits all near; you take +30%', 'Серия до 20, с 15 удары +50% быстрее, каждый 5-й — по всем рядом; получаемый урон +30%'
  Knuckles_H1 = 'Counter', 'Удар в ответ', 'Hybrid. A hit you take does not reset the combo if you have a charge: the charge burns instead.', 'Гибрид. Пропущенный удар не сбрасывает серию, если есть заряд: сгорает заряд.', 'A hit taken eats a charge, not the combo', 'Пропущенный удар съедает заряд, а не серию'
  Knuckles_H2 = 'Clinch', 'Клинч', 'Hybrid. While you keep a 5+ combo on one zombie, its blows at you are 25% weaker. No charges - you hold it close and do not let go.', 'Гибрид. Пока вы ведёте серию 5+ по одному зомби, его удары по вам -25%. Без зарядов — держите цель вплотную.', 'Combo 5+ on a target: its blows -25%', 'Серия 5+ по цели: её удары -25%'
  Knuckles_H3 = 'King of the Ring', 'Король ринга', 'Big hybrid (8 points in each branch). At full charges and a 10+ combo a power hit knocks down everyone within 3 m and deals each 50% of its damage.', 'Большой гибрид (8 очков в каждой ветке). На полных зарядах и серии 10+ силовой удар сбивает всех в 3 м и наносит каждому 50% своего урона.', 'Full charges + combo 10+: the power hit knocks down all within 3 m, 50%', 'Полные заряды + серия 10+: силовой сбивает всех в 3 м, 50%'
  Batons_Base = 'Chain Lightning', 'Цепная молния', 'A normal hit shocks the target for 0.5 s. A power hit is a mass stun: every zombie within 3 m is shocked and stunned for 2 s (10 s cooldown, power hits +50% stamina). Branch A is the crowd: a wider, more frequent lightning with jumps; branch B one target: you keep hitting the same shocked zombie.', 'Обычный удар шокирует цель на 0.5 с. Силовой — массовый стан: все зомби в 3 м шокированы и оглушены на 2 с (перезарядка 10 с, силовой +50% выносливости). Ветка A — толпа: молния шире, чаще, с перескоками; ветка B — одна цель: бьёте того же шокированного зомби.'
  Batons_A1 = 'Wide Arc', 'Широкий разряд', 'The radius of the mass stun (3 m by default).', 'Радиус массового стана (по умолчанию 3 м).', '+0.5 m', '+0.5 м', '+1 m', '+1 м', '+2 m (5 m)', '+2 м (5 м)'
  Batons_A2 = 'Sustained Charge', 'Долгое напряжение', 'The mass stun lasts longer (2 s by default) - the crowd stands and burns longer.', 'Массовый стан длится дольше (по умолчанию 2 с) — толпа дольше стоит и горит.', '+1 s', '+1 с', '+2 s', '+2 с', '+3 s (5 s)', '+3 с (5 с)'
  Batons_A3 = 'Arc Jump', 'Перескок', 'The lightning jumps from the shocked ones to zombies beyond the radius (within 4 m of a shocked one).', 'Молния перескакивает с шокированных на зомби за радиусом (до 4 м от шокированного).', '1 more zombie', 'Ещё 1 зомби', '2 more zombies', 'Ещё 2 зомби', '4 more zombies', 'Ещё 4 зомби'
  Batons_A4 = 'Capacitor', 'Конденсатор', 'A shorter Chain Lightning cooldown. Strength = rank x your Intellect level.', 'Короче перезарядка Цепной молнии. Сила = ранг x уровень вашего Интеллекта.', '-2% x Intellect (max -20%)', '-2% x Интеллект (до -20%)', '-3% x Intellect (max -30%)', '-3% x Интеллект (до -30%)', '-5% x Intellect (max -50%)', '-5% x Интеллект (до -50%)'
  Batons_A5 = 'Tesla Cage', 'Электрическая клетка', 'For 3 s after the mass stun a field stays around: every zombie that walks into the radius is shocked for 1 s. Stand in the crowd, do not run.', '3 с после массового стана вокруг остаётся поле: каждый зомби, вошедший в радиус, шокирован на 1 с. Стойте в гуще, а не убегайте.', 'A 3 s field in the radius: walking in - 1 s shock', 'Поле 3 с в радиусе: вход — шок 1 с'
  Batons_A6 = 'Crowd Feed', 'Питание от толпы', 'Every zombie hit by the lightning gives stamina back - the bigger the crowd, the cheaper the power hit.', 'Каждый зомби, задетый молнией, возвращает выносливость — чем больше толпа, тем дешевле силовой.', '+2 per zombie', '+2 за зомби', '+5 per zombie', '+5 за зомби'
  Batons_A7 = 'Short Circuit', 'Короткое замыкание', 'A zombie that dies while shocked discharges: everyone around is shocked for 1 s.', 'Зомби, умирающий под шоком, разряжается: шокирует всех вокруг на 1 с.', 'Within 2 m', 'В 2 м', 'Within 5 m', 'В 5 м'
  Batons_A8 = 'Thunderstorm', 'Буря', 'Capstone. The Chain Lightning deals every shocked zombie 40% of the power hit, and every kill by it resets the cooldown. Downside: when no other zombie is within 5 m, your hits -25%.', 'Капстоун. Цепная молния наносит каждому шокированному 40% силового удара, а каждое убийство ею сбрасывает перезарядку. Минус: когда в 5 м нет других зомби, ваши удары -25%.', 'Lightning 40% of the hit to each, a kill resets the cooldown; nobody within 5 m - -25%', 'Молния 40% удара каждому, убийство сбрасывает перезарядку; никого в 5 м — удары -25%'
  Batons_B1 = 'Live Wire', 'Удар по шокированному', 'Normal and power hits on a shocked zombie hit harder. Shock first - then damage.', 'Обычные и силовые удары по шокированному зомби бьют сильнее. Сначала шок — потом урон.', '+10%', '+10%', '+20%', '+20%', '+40%', '+40%'
  Batons_B2 = 'Hold the Current', 'Держать под током', 'The shock of a normal hit lasts longer (0.5 s by default) - the target stays shocked while you keep hitting.', 'Шок от обычного удара длится дольше (по умолчанию 0.5 с) — цель не выходит из-под шока, пока вы бьёте.', '+0.25 s', '+0.25 с', '+0.5 s', '+0.5 с', '+1 s (1.5 s)', '+1 с (1.5 с)'
  Batons_B3 = 'Breakdown', 'Пробой', 'Every 4th normal hit in a row on the same zombie hits harder. Another target - the count starts again.', 'Каждый 4-й обычный удар подряд по тому же зомби бьёт сильнее. Сменили цель — счёт с нуля.', '+50%', '+50%', '+100%', '+100%', '+200%', '+200%'
  Batons_B4 = 'Isolation', 'Изоляция', 'When no other zombie is within 4 m of the target you hit harder - lure one away from the crowd.', 'Когда в 4 м от цели нет других зомби, вы бьёте сильнее — уводите одного от толпы.', '+15%', '+15%', '+30%', '+30%', '+50%', '+50%'
  Batons_B5 = 'Cardiac Arrest', 'Остановка сердца', 'A shocked zombie at 25% health or less dies from your next hit. An elite does not die but takes +100%. Keep the target under current to the end.', 'Шокированный зомби с 25% здоровья или меньше умирает от следующего вашего удара. Элита не умирает, а получает +100%. Держите цель под током до конца.', 'Shocked at 25% HP or less: non-elites die, elites +100%', 'Шокированный с 25% HP: не-элита умирает, элита +100%'
  Batons_B6 = 'Residual Charge', 'Остаточный заряд', 'After a kill the Breakdown count does not reset: the next target starts with a few hits.', 'После убийства счёт Пробоя не обнуляется: следующая цель начинает с нескольких ударов.', 'From 1 hit', 'С 1 удара', 'From 3 hits', 'С 3 ударов'
  Batons_B7 = 'Discharge', 'Подрыв шока', 'A power hit on a shocked zombie "detonates" the shock: a bonus for every second left, the shock burns out. Shock with normal hits - finish with a power one.', 'Силовой удар по шокированному «подрывает» шок: бонус за каждую оставшуюся секунду, шок сгорает. Шокируйте обычными — добивайте силовым.', '+25% per second of shock', '+25% за секунду шока', '+60% per second of shock', '+60% за секунду шока'
  Batons_B8 = 'High Voltage', 'Высокое напряжение', 'Capstone. A normal hit on a shocked zombie deals +100% and adds 0.5 s to its shock - while you keep hitting, the shock does not end. Downside: the mass stun radius is halved: 3 -> 1.5 m (with Wide Arc 5 -> 2.5 m).', 'Капстоун. Обычный удар по шокированному +100% и продлевает его шок на 0.5 с — пока вы не отпускаете цель, шок не кончается. Минус: радиус массового стана вдвое меньше: 3 -> 1.5 м (с Широким разрядом 5 -> 2.5 м).', 'Normal on shocked +100% and +0.5 s shock; mass stun radius x0.5', 'Обычный по шокированному +100% и +0.5 с шока; радиус массового стана x0.5'
  Batons_H1 = 'Grounding', 'Заземление', 'Hybrid. Zombies shocked by you hit you 30% weaker - in a crowd and one on one.', 'Гибрид. Шокированные вами зомби бьют вас на 30% слабее — и в толпе, и один на один.', 'Hits of shocked zombies on you -30%', 'Удары шокированных зомби по вам -30%'
  Batons_H2 = 'Technician', 'Технарь', 'Hybrid. Zombies shocked by your baton take +200% from your turrets and drone. An Intellect build: the baton holds, the turrets shoot.', 'Гибрид. Шокированные вашим жезлом получают +200% урона от ваших турелей и дрона. Билд на Интеллект: жезл держит, турели стреляют.', 'Shocked: +200% from your turrets and drone', 'Шокированные: +200% от ваших турелей и дрона'
  Batons_H3 = 'Tesla Coil', 'Катушка Теслы', 'Big hybrid (8 points in each branch). Every Breakdown (the 4th hit on a target) fires the Chain Lightning without the cooldown - one target feeds the whole storm around.', 'Большой гибрид (8 очков в каждой ветке). Каждый Пробой (4-й удар по цели) запускает Цепную молнию без перезарядки — одна цель питает всю бурю вокруг.', 'Breakdown fires the lightning without the cooldown', 'Пробой запускает молнию без перезарядки'
}
foreach ($k in $masterySkills.Keys) {
  $v = $masterySkills[$k]
  $extra["aposMS_$k"] = $v[0], $v[1]
  $extra["aposMS_${k}_D"] = $v[2], $v[3]
  for ($r = 1; 4 + 2 * $r -le $v.Count; $r++) { $extra["aposMS_${k}_R$r"] = $v[2 + 2 * $r], $v[3 + 2 * $r] }
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

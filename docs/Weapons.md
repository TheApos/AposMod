# Зброя 7 Days to Die

Згенеровано з `Data\Config\items.xml` (ванільна гра, без модів). Всього: 54.

**Колонки:** `ID` — ім'я для XPath у моді; `T` — тір; `Шкода` — EntityDamage (для вогнепальної це лише `+` до шкоди патрона); `Блоки` — BlockDamage; `Швидкість` — RoundsPerMinute (стрільба) або AttacksPerMinute (ближній); `Магазин` — MagazineSize; `Дальність` — MaxRange. Порожньо = значення задається не тут (патроном, тіром чи перком).

## Ближній бій

### Ножі та мачете

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Bone Knife | `meleeWpnBladeT0BoneKnife` | 0 | 5 | 4 | 120 |  | 2.0 | perkDeepCuts, perkTheHuntsman |  |
| Hunting Knife | `meleeWpnBladeT1HuntingKnife` | 1 | 12 | 6.5 | 120 |  | 2.0 | perkDeepCuts, perkTheHuntsman |  |
| Candy Cane Knife | `meleeWpnBladeT1CandyKnife` | 1 | 12 | 6.5 | 120 |  | 2.0 | perkDeepCuts, perkTheHuntsman |  |
| Machete | `meleeWpnBladeT3Machete` | 3 | 25 | 21 | 65 |  | 2.3 | perkDeepCuts, perkTheHuntsman |  |

### Дубинки та біти

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Wooden Club | `meleeWpnClubT0WoodenClub` | 0 | 12 | 11 | 55 |  | 2.4 | perkPummelPete |  |
| Baseball Bat | `meleeWpnClubT1BaseballBat` | 1 | 20 | 18 | 52 |  | 2.4 | perkPummelPete |  |
| Candy Cane Club | `meleeWpnClubT1CandyClub` | 1 | 20 | 18 | 52 |  | 2.4 | perkPummelPete |  |
| Steel Club | `meleeWpnClubT3SteelClub` | 3 | 32.5 | 30 | 52 |  | 2.4 | perkPummelPete |  |

### Кийки (електро)

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Pipe Baton | `meleeWpnBatonT0PipeBaton` | 0 | 12 | 12 | 60 |  | 2.4 | perkElectrocutioner |  |
| Stun Baton | `meleeWpnBatonT2StunBaton` | 2 | 20 | 12 | 70 |  | 2.4 | perkElectrocutioner |  |

### Списи

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Stone Spear | `meleeWpnSpearT0StoneSpear` | 0 | 12 | 5 | 55 |  |  | perkJavelinMaster |  |
| Iron Spear | `meleeWpnSpearT1IronSpear` | 1 | 20 | 8 | 55 |  |  | perkJavelinMaster |  |
| Steel Spear | `meleeWpnSpearT3SteelSpear` | 3 | 33 | 15 | 55 |  |  | perkJavelinMaster |  |

### Кувалди

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Stone Sledgehammer | `meleeWpnSledgeT0StoneSledgehammer` | 0 | 18 | 28 | 50 |  | 2.6 | perkSkullCrusher |  |
| Iron Sledgehammer | `meleeWpnSledgeT1IronSledgehammer` | 1 | 32 | 52 | 34 |  | 2.6 | perkSkullCrusher |  |
| Steel Sledgehammer | `meleeWpnSledgeT3SteelSledgehammer` | 3 | 55 | 85 | 34 |  | 2.6 | perkSkullCrusher |  |

### Кастети

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Knuckle Wraps | `meleeWpnKnucklesT0LeatherKnuckles` | 0 | 7 | 2.2 | 100 |  | 2.0 | perkBrawler |  |
| Iron Knuckles | `meleeWpnKnucklesT1IronKnuckles` | 1 | 11.5 | 3.5 | 100 |  | 2.0 | perkBrawler |  |
| Steel Knuckles | `meleeWpnKnucklesT3SteelKnuckles` | 3 | 19 | 6 | 100 |  | 2.0 | perkBrawler |  |

### Інше

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Torch | `meleeToolTorch` |  | 10.2 | 7 | 50 |  | 2.4 |  |  |

## Дальній бій

### Пістолети, револьвери, ПП

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Pipe Pistol | `gunHandgunT0PipePistol` | 0 | +-4 | +-2.1 | 126 | 6 | 40 | perkGunslinger | ammo9mmBulletBall, ammo9mmBulletHP, ammo9mmBulletAP |
| Pistol | `gunHandgunT1Pistol` | 1 | +5 |  | 180 | 15 | 50 | perkGunslinger | ammo9mmBulletBall, ammo9mmBulletHP, ammo9mmBulletAP |
| SMG-5 | `gunHandgunT3SMG5` | 3 | +18 |  | 480 | 30 | 65 | perkGunslinger | ammo9mmBulletBall, ammo9mmBulletHP, ammo9mmBulletAP |
| .44 Magnum | `gunHandgunT2Magnum44` | 2 | +-5 |  | 100 | 6 | 70 | perkGunslinger | ammo44MagnumBulletBall, ammo44MagnumBulletHP, ammo44MagnumBulletAP |
| .44 Desert Vulture | `gunHandgunT3DesertVulture` | 3 | +20 |  | 130 | 8.8 | 50 | perkGunslinger | ammo44MagnumBulletBall, ammo44MagnumBulletHP, ammo44MagnumBulletAP |

### Дробовики

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Pipe Shotgun | `gunShotgunT0PipeShotgun` | 0 | +-2 |  | 120 | 1 |  | perkBoomstick | ammoShotgunShell, ammoShotgunSlug, ammoShotgunBreachingSlug |
| Double Barrel Shotgun | `gunShotgunT1DoubleBarrel` | 1 | +1 |  | 1 | 2 |  | perkBoomstick | ammoShotgunShell, ammoShotgunSlug, ammoShotgunBreachingSlug |
| Pump Shotgun | `gunShotgunT2PumpShotgun` | 2 | +5 |  | 60 | 8 |  | perkBoomstick | ammoShotgunShell, ammoShotgunSlug, ammoShotgunBreachingSlug |
| Auto Shotgun | `gunShotgunT3AutoShotgun` | 3 | +10 |  | 70 | 16 |  | perkBoomstick | ammoShotgunShell, ammoShotgunSlug, ammoShotgunBreachingSlug |

### Гвинтівки

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Pipe Rifle | `gunRifleT0PipeRifle` | 0 | +-3 | +-2 | 120 | 1 | 150 | perkDeadEye | ammo762mmBulletBall, ammo762mmBulletHP, ammo762mmBulletAP |
| Hunting Rifle | `gunRifleT1HuntingRifle` | 1 | +11 | +-1 | 120 | 1 | 150 | perkDeadEye | ammo762mmBulletBall, ammo762mmBulletHP, ammo762mmBulletAP |
| Lever Action Rifle | `gunRifleT2LeverActionRifle` | 2 | +29 | +-1 | 82 | 5 | 150 | perkDeadEye | ammo762mmBulletBall, ammo762mmBulletHP, ammo762mmBulletAP |
| Sniper Rifle | `gunRifleT3SniperRifle` | 3 | +53 |  | 140 | 12 | 150 | perkDeadEye | ammo762mmBulletBall, ammo762mmBulletHP, ammo762mmBulletAP |

### Автомати та кулемети

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Pipe Machine Gun | `gunMGT0PipeMachineGun` | 0 | +-17 | +-3 | 335 | 15 | 90 | perkMachineGunner | ammo762mmBulletBall, ammo762mmBulletHP, ammo762mmBulletAP |
| KV-47 Machine Gun | `gunMGT1AK47` | 1 | +-7 | +-3 | 360 | 30 | 90 | perkMachineGunner | ammo762mmBulletBall, ammo762mmBulletHP, ammo762mmBulletAP |
| Tactical Assault Rifle | `gunMGT2TacticalAR` | 2 | +6 | +-2 | 405 | 30 | 90 | perkMachineGunner | ammo762mmBulletBall, ammo762mmBulletHP, ammo762mmBulletAP |
| M60 Machine Gun | `gunMGT3M60` | 3 | +23 | +-2 | 440 | 60 | 90 | perkMachineGunner | ammo762mmBulletBall, ammo762mmBulletHP, ammo762mmBulletAP |

### Луки

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Primitive Bow | `gunBowT0PrimitiveBow` | 0 | +-5 |  | 75 | 1 | 100 | perkArchery | ammoArrowStone, ammoArrowIron, ammoArrowSteelAP, ammoArrowFlaming, ammoArrowExploding |
| Wooden Bow | `gunBowT1WoodenBow` | 1 | +9 |  | 75 | 1 | 100 | perkArchery | ammoArrowStone, ammoArrowIron, ammoArrowSteelAP, ammoArrowFlaming, ammoArrowExploding |
| Compound Bow | `gunBowT3CompoundBow` | 3 | +29 |  | 75 | 1 | 100 | perkArchery | ammoArrowStone, ammoArrowIron, ammoArrowSteelAP, ammoArrowFlaming, ammoArrowExploding |

### Арбалети

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Iron Crossbow | `gunBowT1IronCrossbow` | 1 |  |  | 75 | 1 | 100 | perkArchery | ammoCrossbowBoltStone, ammoCrossbowBoltIron, ammoCrossbowBoltSteelAP, ammoCrossbowBoltFlaming, ammoCrossbowBoltExploding |
| Compound Crossbow | `gunBowT3CompoundCrossbow` | 3 | +25 |  | 75 | 1 | 100 | perkArchery | ammoCrossbowBoltStone, ammoCrossbowBoltIron, ammoCrossbowBoltSteelAP, ammoCrossbowBoltFlaming, ammoCrossbowBoltExploding |

### Вибухова (ракетниця)

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Rocket Launcher | `gunExplosivesT3RocketLauncher` | 3 |  |  | 75 | 1 | 100 | perkDemolitionsExpert | ammoRocketHE, ammoRocketFrag |

### Метальна (гранати, динаміт, молотов)

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Pipe Bomb (Ammo) | `thrownAmmoPipeBomb` | 1 |  |  |  |  |  | perkDemolitionsExpert |  |
| Contact Grenade (Ammo) | `thrownGrenadeContact` | 2 |  |  |  |  |  | perkDemolitionsExpert |  |
| Grenade (Ammo) | `thrownGrenade` | 1 |  |  |  |  |  | perkDemolitionsExpert |  |
| Timed Charge (Ammo) | `thrownTimedCharge` | 3 |  |  |  |  |  | perkDemolitionsExpert |  |
| Stick of Dynamite (Ammo) | `thrownDynamite` | 2 |  |  |  |  |  | perkDemolitionsExpert |  |
| Molotov Cocktail (Ammo) | `thrownAmmoMolotovCocktail` | 0 |  |  |  |  |  | perkDemolitionsExpert |  |

## Роботи (Intellect)

### Роботизовані турелі

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Robotic Sledge | `gunBotT1JunkSledge` |  | 11.5 | 17 | 30 | -1 | 2 | perkTurrets |  |
| Robotic Turret | `gunBotT2JunkTurret` | 2 |  |  |  | 62 |  | perkTurrets | ammoJunkTurretRegular, ammoJunkTurretShell, ammoJunkTurretAP |

## Адмінська (тільки для тестів)

### Адмін

| Назва | ID | T | Шкода | Блоки | Швидкість | Магазин | Дальність | Перк | Патрони |
|---|---|---|---|---|---|---|---|---|---|
| Dev: Super Digger | `gunToolDiggerAdmin` |  | 0 | 20000 | 450 |  | 200 | perkGunslinger | ammo9mmBulletBall |
| Dev: Instant Death Pistol | `gunHandgunPistolAdmin` |  | 5000 | 0 | 450 |  | 200 | perkGunslinger | ammo9mmBulletBall |
| Dev: Nuke Grenade | `thrownGrenadeNukeAdmin` | 2 |  |  |  |  |  | perkDemolitionsExpert |  |


# Builds docs/HudStatuses.html: every HUD status of AposMod per weapon class, clickable, with the in-game texts
# (skill and buff names / descriptions read from Source/gen_loc.ps1). Run: python docs/hud/gen_hud_doc.py
import base64, json, os, re

SP = os.path.dirname(os.path.abspath(__file__))
os.chdir(os.path.join(SP, '..', '..'))

# skill texts: <Class>_<Id> = 'EN', 'RU', 'desc EN', 'desc RU', ...; buffs: buffAposXName / buffAposXDesc
def q(s):
    return [x.replace("''", "'") for x in re.findall(r"'((?:[^']|'')*)'", s)]

src = open('Source/gen_loc.ps1', encoding='utf-8-sig').read()
T = {}
for m in re.finditer(r"^\s*((?:Pistols|Shotguns|Rifles|MG|Bows|Crossbows|Blades|Clubs|Sledges|Spears|Knuckles|Batons)_(?:Base|[ABH]\d))\s*=\s*(.*)$", src, re.M):
    v = q(m.group(2))
    T[m.group(1)] = {'en': v[0], 'ru': v[1], 'd': v[3] if len(v) > 3 else ''}
for m in re.finditer(r"^\s*(buffApos\w+?)Name\s*=\s*(.*)$", src, re.M):
    v = q(m.group(2)); key = m.group(1)
    d = re.search(r"^\s*" + key + r"Desc\s*=\s*(.*)$", src, re.M)
    T[key] = {'en': v[0], 'ru': v[1], 'd': q(d.group(1))[1] if d else ''}
# its English name has an apostrophe, so the line uses double quotes
T['Pistols_A5'] = {'en': "Marksman's Eye", 'ru': 'Глаз стрелка', 'd': 'В режиме Охотника головы зомби в 40 м отмечены, даже за укрытием.'}

def png(path):
    return 'data:image/png;base64,' + base64.b64encode(open(path, 'rb').read()).decode()

PNG = {}
for f in os.listdir('UIAtlases/UIAtlas'):
    if f.endswith('.png'):
        PNG[f[:-4]] = png(os.path.join('UIAtlases/UIAtlas', f))
for f in os.listdir('Icons'):
    if f.endswith('.png'):
        PNG[f[:-4]] = png(os.path.join('Icons', f))

def t(key):
    v = T.get(key)
    if not v:
        raise KeyError(key)
    return {'ru': v['ru'], 'en': v['en'], 'd': v['d']}

# item: place, icon, color, text, key (texts), what (what the number means), when (when it shows / lights up)
# places: gR / gL (gauge right / left of the crosshair), atk (row under the crosshair), def (row left of the toolbelt),
#         z (status row above the zombie), toast (line under the crosshair), mark (ring on an armed arrow / bolt),
#         feed (reaction feed on the right), over (overlay on zombies)
def I(place, icon, color, text, key, what, when, **kw):
    d = {'place': place, 'icon': icon, 'color': color, 'text': text, 'what': what, 'when': when}
    d.update(t(key) if key else {})
    d['key'] = key or ''
    d.update(kw)
    return d

GOLD, RED, BLUE, CYAN, ORANGE, GREY = '#ffd14d', '#e6404d', '#66bfff', '#40e6ff', '#ff9933', '#9a9a9a'

CLASSES = [
 ('Pistols', 'Пістолети й ПП', [
  I('gR', 'head_shot', '#ffffff', '3/5', 'Pistols_Base', 'Серія влучань у голову поспіль: скільки є / скільки треба, щоб увімкнути режим Охотника.',
    'Завжди, поки пістолет у руках. Промах або влучання в тіло скидає серію.', value=3, seg=5),
  I('gR', 'head_shot', CYAN, '6', 'Pistols_Base', 'Режим Охотника увімкнений: скільки посилених пострілів лишилось (дуга спадає з кожним пострілом).',
    'Після серії влучань у голову. Дуга бірюзова й пульсує, поки режим діє.', value=6, seg=6, full=True, variant='режим'),
  I('spd', 'ui_apos_hotbarrel', GOLD, '+60%', 'Pistols_B5', 'Горячий ствол: поточний бонус темпу стрільби (+5% за влучання).', 'Поки влучаєш; без влучань тане на 5% за секунду. Максимум +100%.'),
  I('atk', 'run_and_gun', '#ffffff', '5/8', 'Pistols_H1', 'Расстрел: скільки влучань поспіль без промаху вже є з 8.', 'Завжди з гібридом H1. Промах обнуляє лічильник.'),
  I('alert', 'ui_apos_squad', GOLD, 'x10', 'Pistols_H1', "Расстрел готовий: наступне влучання б'є на 1000% і рахується як у голову (+1 до серії Охотника). Значок змінюється з бігуна на приціл із вибухом.", 'Після 7 влучань поспіль без промаху, до наступного влучання.', full=True, variant='готовий'),
  I('spd', 'ui_apos_gunfighter', GOLD, '+15%  2s', 'Pistols_H2', 'Ганфайтер: +15% темпу стрільби і скільки секунд лишилось.', '3 секунди після влучання в голову.'),
  I('z', 'run', '#73bfff', '-30%', 'Pistols_B4', 'Над зомбі: наскільки він сповільнений (найсильніше з Кандалів, Раны лука, Подавления кулемета).', 'Поки діє сповільнення (3 с після влучання).'),
  I('over', 'ring', CYAN, '', 'Pistols_A5', 'Бірюзові кільця на головах зомбі в 40 м; за укриттям кільце сіре.', 'Тільки в режимі Охотника.'),
 ]),
 ('Shotguns', 'Дробовики', [
  I('alert', 'shotgun', GOLD, 'x1.5', 'Shotguns_B7', 'Нагорода Свинцовой стены: наступний постріл +50%.', 'Після пострілу, що зачепив 5+ зомбі.', full=True),
  I('z', 'ui_apos_shattered', '#ff9933', '+50%', 'Shotguns_A5', 'Зомбі «Разбитый»: отримує +50% від усього. Над ним ще й червоне «x1.5».', 'Поки зомбі лежить після дробовика і ще 3 с.'),
 ]),
 ('Rifles', 'Гвинтівки', [
  I('gR', 'sight', CYAN, '+60%', 'Rifles_A6', 'Заряд Сосредоточенности: бонус шкоди наступного пострілу (дуга плавно заповнюється).', 'Поки цілишся без пострілу. Пульсує на максимумі.', value=0.6, seg=0),
  I('alert', 'search', GOLD, 'x1.5', 'Rifles_H2', 'Наводчик: наступний постріл +50%.', 'Після вбивства в голову, до наступного пострілу.', full=True),
  I('over', 'outline', GOLD, '', 'Rifles_H2', 'Контури всіх зомбі в 30 м.', '10 секунд після вбивства в голову.'),
 ]),
 ('MG', 'Кулемети', [
  I('gR', 'speed', BLUE, '3/5  +21%', 'MG_B1', 'Стаки Раскрутки і темп стрільби, який вони дають (+4 / 7 / 12% за стак за рангом).', 'Під час безперервного вогню. Пульсує на 5/5.', value=3, seg=5),
  I('gR', 'speed', '#ff4d33', '2s', 'MG_B1', 'Перегрів: стаки згоріли, дуга червона й спадає до кінця перегріву.', '8 с на максимумі розкрутки поспіль → 2 с перегріву.', value=0.8, seg=0, variant='перегрів'),
  I('atk', 'ui_apos_holdline', GOLD, '', 'MG_A6', 'Удержание позиции діє: менше віддачі й більше шкоди.', 'Поки сидиш або стоїш на місці.', full=True),
  I('z', 'ui_apos_suppressed', '#73bfff', '-20%', 'MG_Base', 'Подавление на зомбі: наскільки він сповільнений.', '8 с після влучання.'),
 ]),
 ('Bows', 'Луки', [
  I('atk', 'ui_apos_fulldraw', GOLD, '+40%', 'Bows_B1', 'Бонус шкоди повного натягу (сила = ранг × Ловкість).', 'Лише коли тятива натягнута повністю: з\'являється в кінці ванільної смуги натягу.', full=True),
  I('alert', 'stealth', '#9999ff', 'x2', 'Bows_B5', 'Тень: тебе не помічають, наступна стріла ×2.', '5 с після тихого вбивства.'),
  I('z', 'drop', '#ff594d', 'x2', 'Bows_B3', 'Рана охотника: стаки рани (до 2), зомбі отримує більше шкоди й сповільнений.', '8 с після влучання повністю натягнутою стрілою.'),
  I('mark', 'ring', '#66d9ff', '', 'Bows_A7', 'Блакитне пульсуюче кільце на стрілі в землі чи стіні: пастка заряджена.', 'Поки пастка чекає (до 10 с). Одна пастка за раз.'),
  I('feed', 'feed_pierce', GOLD, '-45 ×3', 'Bows_B2', 'Сквозная стрела: шкода кожному за ціллю і скільки зомбі прошито.', 'Коли повна стріла прошила когось у лінії.'),
  I('feed', 'feed_shock', '#59bfff', '-30 ×2', 'Bows_A7', 'Электроловушка: шкода розряду і скільки зачепило.', 'Коли пастка або стріла в зомбі б\'є струмом.'),
 ]),
 ('Crossbows', 'Арбалети', [
  I('gR', 'traps', ORANGE, '2/5', 'Crossbows_B7', 'Мін Минного поля стоїть / максимум. Понад ліміт найстаріша вибухає.', 'Завжди з навичкою B7. Пульсує, коли всі міни розставлені.', value=2, seg=5),
  I('spd', 'swap', GOLD, '+30%  4s', 'Crossbows_A7', 'Охотничий ритм: бонус швидкості перезарядки і секунди, що лишились.', '5 с після вбивства поміченого зомбі.'),
  I('z', 'hard_target', GOLD, '+20%', 'Crossbows_Base', 'Метка: на скільки більше шкоди цей зомбі отримує від тебе (будь-якою зброєю).', '10 с після влучання болтом.'),
  I('mark', 'ring', ORANGE, '', 'Crossbows_B1', 'Помаранчеве пульсуюче кільце на болті: Липкий заряд або міна чекає вибуху.', 'Від влучання до вибуху (2 с, міна до 60 с).'),
  I('feed', 'feed_sticky', ORANGE, '-50 ×3', 'Crossbows_B1', 'Вибух заряду: шкода кожному і скільки зачепило.', 'Коли заряд або міна вибухнули.'),
 ]),
 ('Blades', 'Ножі й мачете', [
  I('gR', 'drop', '#e6404d', '3/8', 'Blades_Base', 'Стаки Кровопийцы: скільки є / максимум (з Ненасытностью до 8).', 'Завжди з ножем. Без ударів стаки зникають через 5 с. Пульсує на максимумі.', value=3, seg=8),
  I('def', 'drop', '#e6404d', '+3/s', 'Blades_Base', 'Регенерація від стаків: HP за секунду.', 'Завжди з ножем; сіре, коли стаків немає.'),
  I('atk', 'drop', '#e6404d', '+15%', 'Blades_A3', 'Горячая кровь: бонус шкоди від поточних стаків.', 'Коли є хоч один стак.'),
  I('atk', 'ui_apos_squad', GOLD, 'x2', 'Blades_B7', 'Цепь казней: скільки наступних ударів гарантовано будуть критами.', 'Після вбивства критом; зменшується з кожним ударом.', full=True),
  I('dmg', '', '#ffb333', 'Backstab!', 'Blades_B2', "Мітка над числом шкоди цього удару (як «Труп!»), завжди англійською.", 'Удар влучив у зомбі спиною до тебе або який тебе не бачить.'),
  I('dmg', '', '#f21f1f', 'SPINE STRIKE!!', 'Blades_B8', "Велика червона мітка над числом шкоди, під нею «x2 сквозь броню», і хруст кісток — щоб було видно, що це не звичайний удар у спину.", 'Справжній удар у спину з капстоуном B8: ×2 і крізь броню.', sub='x2 сквозь броню'),
  I('feed', 'feed_spine', '#f21f1f', '-384', 'Blades_B8', 'Рядок у стрічці реакцій праворуч: скільки шкоди завдав удар у хребет.', 'Кожен удар у хребет (капстоун B8).', ru='SPINE STRIKE!!', en='Spine Strike'),
  I('z', 'critical', '#ff3333', '-8/s 14s', 'Blades_Base', 'Кровотеча над зомбі: шкода за секунду і скільки секунд лишилось.', 'Після силового удару (і звичайного з Глубокими порезами).', ru='Кровотечение', en='Bleeding'),
 ]),
 ('Clubs', 'Дубинки й бити', [
  I('gR', 'berserker', '#ff4d1a', '4/8  +40%', 'Clubs_B1', 'Стаки Ярости і бонус шкоди від них (+10% за стак, з Неистовством до +30%). Швидкість ударів (+5% за стак) — у блоці енергії справа внизу.', 'З навичкою B1. Стак 8 с від вбивства. Пульсує на максимумі.', value=4, seg=8),
  I('gL', 'bat', GOLD, '+30%', 'Clubs_A7', 'Серия подач: ліва дуга з 3 ланок — скільки силових ударів поспіль уже є; число — бонус шкоди НАСТУПНОГО силового удару (+15 / 40% за ланку).', 'Поки не минуло 3 с після силового удару; потім ланцюжок обривається.', value=1, seg=3),
  I('spd', 'berserker', GOLD, '+20%', 'Clubs_B1', 'Ярость: швидкість ударів (+5% за стак), зливається з іншими бонусами темпу атаки.', 'Поки є стаки Ярости.'),
  I('z', 'concussion', '#ffe666', '+50%', 'Clubs_A5', 'Зомбі контужений: +50% шкоди від усього.', 'Поки діє оглушення.'),
 ]),
 ('Sledges', 'Кувалди', [
  I('gL', 'sledge', '#c08c59', '6s', 'Sledges_Base', 'Перезарядка Землетрясения: дуга заповнюється до «OK».', 'Завжди з кувалдою. На «OK» дуга помаранчева й пульсує: силовий удар збиває всіх у 3 м.', value=0.4, seg=0),
  I('atk', 'twitch_strength_melee', BLUE, 'x2', 'Sledges_B4', 'Размах: котрий удар поспіль по тому самому зомбі (до 3).', '3 с після удару. Таке саме число над цим зомбі.'),
  I('z', 'armor_iron', '#cccccc', '-30%', 'Sledges_B3', 'Ломка брони: скільки броні знято із зомбі.', '10 с після удару.'),
 ]),
 ('Spears', 'Списи', [
  I('gR', 'spear', BLUE, 'x3', 'Spears_B4', 'Серия уколов по одній цілі (до 5).', 'Поки б\'єш ту саму ціль з паузою ≤ 1 с. Те саме число над зомбі.', value=3, seg=5),
  I('spd', 'speed', GOLD, '+50%  x2', 'Spears_B7', 'Контрвыпад: уколи на 50% швидші; x2 — скільки таких уколів лишилось.', 'Після вдалого парирування.'),
  I('atk', 'head_shot', GOLD, '', 'Spears_H2', 'Удар на отходе готовий: наступний удар рахується як укол у голову.', 'Після того як відійшов на 2.5 м.', full=True),
  I('spd', 'ui_apos_speed', '#5ae68c', '+15%  2s', 'Spears_A6', 'Удар и отход: наскільки ти швидший і скільки секунд лишилось.', '2 с після удару списом.'),
  I('def', 'defense', '#b3d9ff', 'OK', 'Spears_A7', 'Часовой: готовий автоукол (або секунди до готовності).', 'Завжди з навичкою A7.'),
  I('z', 'spear', '#ff8040', '+50%', 'Spears_A5', 'Остриё в ране: бонус наступних уколів по цьому зомбі (+25% за удар, до +100%).', 'Після силового удару по першій цілі.'),
  I('toast', '', '#ffd966', 'Парирование!', 'Spears_B5', 'Напис під прицілом + дзвін металу.', 'Удар по зомбі в момент його замаху.'),
  I('toast', '', GOLD, 'Прорыв!', 'Spears_A8', 'Напис під прицілом + важкий звук, спалах і рядок у стрічці.', 'Силовий удар одразу після бігу.'),
 ]),
 ('Knuckles', 'Кастети', [
  I('def', 'boxer', ORANGE, '1/2', 'Knuckles_Base', 'Заряди Контрудара (від пропущених ударів): скільки є / максимум.', 'Завжди з кастетами; сірий без зарядів, яскравий, коли повні.'),
  I('spd', 'ui_apos_speed', '#5ae68c', '+10%', 'Knuckles_B2', 'Лёгкие ноги: наскільки ти швидший після удару.', '5 с після удару кастетами.'),
  I('spd', 'ui_apos_speed', '#ff6659', '-20%', 'Knuckles_A8', 'Нокаут: мінус до твоєї швидкості (ціна капстоуна).', '15 с після нокауту.'),
  I('def', 'defense', ORANGE, '-12%', 'Knuckles_A2', 'Железная кожа: наскільки менше шкоди ти отримуєш.', 'Поки є хоч один заряд.'),
  I('atk', 'twitch_strength_melee', BLUE, '6/10', 'Knuckles_B1', 'Серия: котрий удар поспіль без пропущеного удару.', 'Поки тебе не вдарили. Те саме число над ціллю.'),
  I('alert', 'bat', '#aa78ff', 'x2', 'buffAposShadow', 'Приклад злиття: Тень перка «Бесшумная тень» 5 теж дає ×2 наступному удару в ближньому бою. Разом із Первым ударом вони в одній плашці над прицілом, а множники перемножуються: ×2 · ×2 = ×4.', 'Коли обидва бонуси готові одночасно.'),
  I('alert', 'sneak_attack', GOLD, 'x2', 'Knuckles_B6', 'Первый удар: множник наступного удару.', 'Коли 5 с тебе ніхто не зачепив.', full=True),
 ]),
 ('Batons', 'Кийки', [
  I('gL', 'electric_power', '#66ccff', 'OK', 'Batons_Base', 'Перезарядка Цепной молнии: дуга заповнюється до «OK».', 'Завжди з кийком. На «OK» силовий удар — масове оглушення (сині блискавки).', value=1, seg=0, full=True),
  I('atk', 'hard_target', '#ff8040', '3/4', 'Batons_B3', 'Пробой: котрий удар поспіль по одному зомбі; 4-й б\'є сильніше.', 'Поки б\'єш ту саму ціль. Те саме число над нею.'),
 ]),
]

def B(place, icon, color, text, key, what, when):
    return I(place, icon, color, text, key, what, when)

GENERAL = [
 B('atk', 'electric_power', '#ffb400', '2/3', 'buffAposThunderCharge', 'Заряди грому (легендарка «Раскат грома»).', 'Яскравий на 3/3: наступна силова атака кличе блискавку.'),
 B('spd', 'ui_apos_adrenaline', '#5ae68c', '+20%  2/3', 'buffAposAdrenaline', 'Адреналін: бонус швидкості руху й бігу і стаки за вбивства (+10% за стак).', '15 с після останнього вбивства.'),
 B('def', 'electric_power', '#508cff', '8/10', 'buffAposEmp', 'Заряди молнії (Інтелект 10).', 'Яскравий на 10/10: наступний удар випускає ланцюгову блискавку.'),
 B('atk', 'pistol', '#ffb400', '40%', 'buffAposPipeVeteran', 'Бонус шкоди саморобної зброї.', 'З саморобною зброєю; росте з кожним вбивством.'),
 B('alert', 'bat', '#aa78ff', 'x2', 'buffAposShadow', 'Тень (From the Shadows 5): наступний удар ×2.', 'Поки ти в присіді й тебе не помітили.'),
 B('spd', 'ui_apos_frenzy', GOLD, '+15%  6s', 'buffAposFrenzy', 'Неистовство атрибута: +15% швидкості атаки і секунди, що лишились.', 'Після вбивства зброєю атрибута на 10.'),
 B('alert', 'ui_apos_jackpot', '#ffd200', 'x5  2s', 'buffAposJackpot', 'Джекпот: видобуток руди й дерева ×5; таймер — скільки секунд лишилось.', 'Випадково при ударах по руді чи дереву (5%); триває 2 с — бий швидше.'),
 B('def', 'defense', '#00c8ff', '', 'buffAposShieldUp', 'Энергощит заряджений.', 'Поки щит заряджений; влучання розряджає його.'),
 B('def', 'electric_turret', '#00dcff', '', 'buffAposGuardian', 'Протокол защиты: роботи б\'ють сильніше.', 'Коли твоє здоров\'я низьке.'),
 B('spd', 'ui_apos_evasion', '#5ae68c', '+30%  3s', 'buffAposEvasion', 'Уклонение: +30% швидкості руху, секунди, що лишились.', 'Після ухилення.'),
 B('def', 'siphoning_strikes', '#c8283c', '150 HP', 'buffAposVampire', 'Вампир: бонус до максимуму здоров\'я.', 'Після вбивств у ближньому бою.'),
 B('def', 'healing_factor', '#969696', '45s', 'buffAposSecondWind', 'Скільки лишилось до наступного Спасения (Мастерство Стойкости 5): коли ХП падає нижче 20%, миттєво повертається 70% максимуму.', 'Одразу після того як Спасение спрацювало; триває 5 хвилин.'),
 B('def', 'ui_apos_lungs', '#969696', '60s', 'buffAposSecondLungs', 'Скільки лишилось до наступного Запаса сил (Кардио 5): коли витривалість закінчується, миттєво повертається 50%.', 'Одразу після того як Запас сил спрацював; триває 60 с.'),
 B('col', 'ui_apos_acidarmor', '#78ff3c', '-20%', 'buffAposAcidArmor', 'Разъеденная броня (дебафф): −20% фізичного захисту, поки діє.', 'Удар Кислотної еліти; триває 10 с'),
]

ZOMBIE = [
 I('z', 'hard_target', GOLD, '+20%', 'Crossbows_Base', 'Метка арбалета: +N% твоєї шкоди по ньому.', '10 с після болта.'),
 I('z', 'run', '#73bfff', '-30%', None, 'Сповільнення: найсильніше з Кандалів пістолета й Раны лука (Подавление кулемета має свій значок).', 'Поки діє.', ru='Замедление', en='Slowed', d='', src='Майстерність: Кандали пістолета (B4), Рана лука (B3), Шрапнельная рана лука (A6)'),
 I('z', 'ui_apos_suppressed', '#73bfff', '-20%', 'MG_Base', 'Подавление кулемета: наскільки зомбі сповільнений (до -20%).', '8 с після влучання з кулемета.'),
 I('z', 'drop', '#ff594d', 'x2', 'Bows_B3', 'Стаки Раны охотника.', '8 с.'),
 I('z', 'ui_apos_shattered', ORANGE, '+50%', 'Shotguns_A5', 'Разбитый: +50% від усього.', 'Поки лежить і ще 3 с.'),
 I('z', 'spear', '#ff8040', '+50%', 'Spears_A5', 'Остриё в ране: бонус уколів.', 'Після силового удару списом.'),
 I('z', 'armor_iron', '#cccccc', '-30%', 'Sledges_B3', 'Ломка брони: знята броня.', '10 с.'),
 I('z', 'concussion', '#ffe666', '+50%', 'Clubs_A5', 'Контузія: +50% від усього.', 'Поки оглушений.'),
 I('z', 'fire', '#ff8019', '12s', None, 'Горіння: секунди, що лишились (з молотова, вогняної стріли, стихії).', 'Поки горить.', ru='Горение', en='Burning', d='', src='Молотов, вогняні стріли / болти, мод зброї «Вогонь», реакція Взрыв, Пылающая еліта'),
 I('z', 'critical', '#ff3333', '-8/s 14s', None, 'Кровотеча: шкода за секунду і секунди, що лишились.', 'Поки кровоточить.', ru='Кровотечение', en='Bleeding', d='', src='Силові удари ножа, Глубокие порезы, зазубрені стріли, Рваные раны дробовика, спис, кулемет (Разрыв), Миазмы, ванільні моди на кровотечу'),
 I('z', 'stunned', '#ff9919', '2s', None, 'Оглушення або збитий з ніг.', 'Поки діє.', ru='Оглушение', en='Stunned', d='', src='Хоумран дубинки, Землетрясение кувалди, Цепная молния кийка, Замешательство, ванільні оглушення'),
 I('z', 'run', '#ff7373', '8s', None, 'Покалічені ноги.', 'Поки діє.', ru='Покалечен', en='Crippled', d='', src='Ванільні покалічені ноги (удари по ногах, перк «Громила Пит» тощо)'),
 I('z', 'electric_power', '#66e6ff', '3s', None, 'Шок (стрілою, пасткою, кийком, модом «Ток»).', 'Поки діє.', ru='Шок', en='Shocked', d='', src='Електричні стріли, Электроловушка лука, кийки, мод зброї «Ток», Электрическая еліта'),
 I('z', 'drop', '#80ff59', '5s', None, 'Кислота: −25% фізичного опору.', '6 с після кислотного влучання.', ru='Кислота', en='Acid', d='', src='Кислотні стріли, мод зброї «Кислота», осколки з кислотою (Стихийные осколки лука)'),
 I('z', 'twitch_confused_controls', '#ff99ff', '4s', None, 'Замешательство (вогонь + шок): −60% швидкості, −30 фізичного опору.', '5 с.', ru='Замешательство', en='Confusion', d='', src='Реакція стихій: вогонь + шок на одному зомбі'),
 I('z', 'infection', '#b3f280', '5s', None, 'Миазмы (кислота + шок): −20% макс. здоров\'я і кровотеча.', '6 с.', ru='Миазмы', en='Miasma', d='', src='Реакція стихій: кислота + шок на одному зомбі'),
 I('z', 'spear', BLUE, 'x3', 'Spears_B4', 'Лічильник на цілі: Серия уколов списа.', 'Поки б\'єш цю ціль.'),
 I('z', 'twitch_strength_melee', BLUE, '6/10', 'Knuckles_B1', 'Лічильник на цілі: Серия кастетів.', 'Поки тебе не вдарили.'),
 I('z', 'twitch_strength_melee', BLUE, 'x2', 'Sledges_B4', 'Лічильник на цілі: Размах кувалди.', '3 с після удару.'),
 I('z', 'hard_target', '#ff8040', '3/4', 'Batons_B3', 'Лічильник на цілі: Пробой кийка.', 'Поки б\'єш цю ціль.'),
]

# what is left in the vanilla buff column above health / stamina (bottom left)
COLUMN = [
 I('col', 'ui_apos_rocket', '#ffb400', '', 'buffAposFreeRocket', 'Большой бабах: наступна ракета без витрати боєприпасу.',
   'Після пострілу з легендарного ракетомета, поки він у руках. У HUD його немає, тож він лишається тут.'),
 I('col', 'ui_apos_bloodsucker', '#e63c50', '2', 'buffAposKnifeRegen', 'Стаки регенерації ножа (HP за секунду).',
   'Тут лише коли HUD вимкнено (aposhud off). З увімкненим HUD його показують дуга праворуч і ряд біля поясу.'),
 I('col', 'armor_iron', '#ffffff', '', None, "Ванільний бафф повного комплекту броні. Бонуси сетів AposMod прив'язані до нього: поки він є, діє і бонус мода.",
   'Поки вдягнений повний комплект.', ru='Бонус полного комплекта', en='Full Set Bonus', d='', src="Ванільний бафф гри: повний комплект броні; до нього прив'язані бонуси сетів AposMod"),
]

FEED = [
 I('feed', 'feed_explosion', GOLD, '-120 ×3', None, 'Реакція Взрыв (вогонь + кислота): шкода кожному і скільки зачепило в 4 м.', 'Вогонь і кислота на одному зомбі.', ru='Взрыв', en='Explosion', d='', src='Реакція стихій: вогонь + кислота'),
 I('feed', 'feed_confusion', '#ff99ff', '', None, 'Реакція Замешательство (вогонь + шок).', 'Вогонь і шок на одному зомбі.', ru='Замешательство', en='Confusion', d='', src='Реакція стихій: вогонь + шок'),
 I('feed', 'feed_miasma', '#b3f280', '', None, 'Реакція Миазмы (кислота + шок).', 'Кислота і шок на одному зомбі.', ru='Миазмы', en='Miasma', d='', src='Реакція стихій: кислота + шок'),
 I('feed', 'feed_emp', '#59bfff', '-60 ×4', None, 'ИНТ 10: ланцюгова блискавка.', 'На 10 зарядах молнії.', ru='ИНТ 10', en='INT 10', d='', src='Атрибут Інтелект 10 (Заряды молнии)'),
 I('feed', 'feed_overkill', '#ff8c59', '-80 ×1', None, 'СИЛ 10: надлишок шкоди перейшов на сусіда.', 'Вбивство з надлишком шкоди.', ru='СИЛ 10', en='STR 10', d='', src='Атрибут Сила 10'),
 I('feed', 'feed_chain', GOLD, '-90 ×3', None, 'Подрывник 5: ланцюгова реакція.', 'Вбивство вибухом.', ru='Подрывник 5', en='Demolitions Expert 5', d='', src='Перк «Подрывник» 5'),
 I('feed', 'feed_shrapnel', '#ccccd9', '-25 ×2', None, 'Бронебойщик 5: шрапнель.', 'Пробиття зомбі.', ru='Бронебойщик 5', en='Penetrator 5', d='', src='Перк «Бронебойщик» 5'),
 I('feed', 'feed_pierce', GOLD, '-45 ×3', 'Bows_B2', 'Сквозная стрела лука.', 'Повна стріла прошила лінію.'),
 I('feed', 'feed_shock', '#59bfff', '-30 ×2', 'Bows_A7', 'Электроловушка лука.', 'Розряд пастки.'),
 I('feed', 'feed_sticky', ORANGE, '-50 ×3', 'Crossbows_B1', 'Липкий заряд / міна арбалета.', 'Вибух заряду.'),
 I('feed', 'feed_breakthrough', GOLD, '-140 ×1', 'Spears_A8', 'Прорыв списа.', 'Ривок крізь натовп.'),
 I('feed', 'feed_spine', '#f21f1f', '-384', 'Blades_B8', 'SPINE STRIKE!! ножа: скільки шкоди завдав удар у хребет.', 'Кожен удар у хребет (капстоун B8).', ru='SPINE STRIKE!!', en='Spine Strike'),
]

# ===== vanilla icons: where the game itself uses them (so the reader knows what they can be mixed up with) =====
import csv
VAN = os.path.join('..', '..', 'Data', 'Config')
LOC = {}
with open(os.path.join(VAN, 'Localization.csv'), encoding='utf-8-sig', newline='') as f:
    for row in csv.DictReader(f):
        LOC[row['Key']] = (row.get('russian') or row.get('english') or '', row.get('english') or '')

def loc(key, fallback):
    v = LOC.get(key)
    return v if v and v[0] else (fallback, fallback)

TAG = re.compile(r'<(\w+)\b([^>]*?)\b(?:icon|sprite)="ui_game_symbol_([a-z0-9_]+)"([^>]*)>')
ATTR = re.compile(r'(\w+)="([^"]*)"')
VANILLA = {}   # icon -> {'buffs': [...], 'perks': [...], 'ui': n, 'other': [...]}
for root, _, files in os.walk(VAN):
    for fn in files:
        if not fn.endswith('.xml'):
            continue
        text = open(os.path.join(root, fn), encoding='utf-8-sig', errors='replace').read()
        for m in TAG.finditer(text):
            tag, icon = m.group(1), m.group(3)
            a = dict(ATTR.findall(m.group(2) + ' ' + m.group(4)))
            v = VANILLA.setdefault(icon, {'buffs': [], 'perks': [], 'ui': 0, 'other': []})
            name = a.get('name', '')
            if fn == 'buffs.xml' and tag == 'buff':
                if a.get('hidden') == 'true':
                    continue   # never shown to the player
                ru, en = loc(a.get('name_key', name), name)
                col = a.get('icon_color', '255,255,255')
                v['buffs'].append({'ru': ru, 'en': en, 'id': name, 'color': 'rgb(%s)' % ','.join(col.split(',')[:3])})
            elif fn == 'progression.xml':
                ru, en = loc(a.get('name_key', name), name)
                kind = {'perk': 'перк', 'attribute': 'атрибут', 'skill': 'навичка', 'book_group': 'книги', 'crafting_skill': 'крафт'}.get(tag, tag)
                v['perks'].append({'ru': ru, 'en': en, 'kind': kind})
            elif fn in ('windows.xml', 'templates.xml'):
                v['ui'] += 1
            else:
                v['other'].append({'challenges.xml': 'випробування', 'twitch.xml': 'Twitch-події', 'entityclasses.xml': 'істоти',
                                   'nav_objects.xml': 'мітки на мапі', 'ui_display.xml': 'опис предметів'}.get(fn, fn))
for v in VANILLA.values():
    # one entry per name, keep the order
    for k in ('buffs', 'perks'):
        seen, keep = set(), []
        for e in v[k]:
            if e['ru'] not in seen:
                seen.add(e['ru']); keep.append(e)
        v[k] = keep
    v['other'] = sorted(set(v['other']))

# ===== "where does it come from" for every item =====
BUFFSRC = {
 'buffAposThunderCharge': 'Легендарна кувалда «Раскат грома»',
 'buffAposAdrenaline': 'Атрибут Ловкість 10',
 'buffAposEmp': 'Атрибут Інтелект 10',
 'buffAposPipeVeteran': 'Саморобна (pipe) зброя — риса AposMod',
 'buffAposShadow': 'Перк «Бесшумная тень» 5 (From The Shadows)',
 'buffAposFrenzy': 'Перки атрибутів на швидкість атаки на рівні 5: «Скорость и проницательность», «Большой и быстрый», «Руки-молнии», «Вихрь»',
 'buffAposJackpot': 'Перк «Драгоценная добыча» 5 (Mother Lode)',
 'buffAposShieldUp': 'Мод нагрудника «Энергощит»',
 'buffAposGuardian': 'Перк «Конструктор роботов» 1+ (Robotics Inventor)',
 'buffAposEvasion': 'Перк «Мастерство ловкости» 5',
 'buffAposVampire': 'Перк «Ураганные удары» 5 (Siphoning Strikes)',
 'buffAposSecondWind': 'Перк «Мастерство стойкости» 5',
 'buffAposSecondLungs': 'Перк «Правило 1: Кардиотренировка» 5',
 'buffAposAcidArmor': 'Удар Кислотної еліти (дебафф)',
 'buffAposFreeRocket': 'Легендарний ракетомет «Большой бабах»',
 'buffAposKnifeRegen': 'Майстерність зброї · Ножі й мачете · риса класу',
}
BRANCH = {}
for m in re.finditer(r"\$extra\['aposMB_(\w+)_([AB])'\]\s*=\s*'((?:[^']|'')*)',\s*'((?:[^']|'')*)'", src):
    BRANCH[(m.group(1), m.group(2))] = m.group(4)
CLASSNAME = {c: n for c, n, _ in CLASSES}

def source_of(it):
    if it.get('src'):
        return it['src']
    k = it.get('key') or ''
    m = re.match(r'(\w+?)_(Base|[ABH]\d)$', k)
    if m and m.group(1) in CLASSNAME:
        cls, sid = m.group(1), m.group(2)
        if sid == 'Base':
            return 'Майстерність зброї · %s · риса класу (є з першого рівня)' % CLASSNAME[cls]
        if sid[0] == 'H':
            return 'Майстерність зброї · %s · гібрид %s (потрібні очки в обох гілках)' % (CLASSNAME[cls], sid)
        return 'Майстерність зброї · %s · гілка %s «%s» · навичка %s' % (CLASSNAME[cls], sid[0], BRANCH.get((cls, sid[0]), sid[0]), sid)
    if k in BUFFSRC:
        return BUFFSRC[k]
    raise KeyError('no source for ' + (it.get('ru') or k))

for lst in [items for _, _, items in CLASSES] + [GENERAL, ZOMBIE, FEED, COLUMN]:
    for it in lst:
        it['src'] = source_of(it)
        # "the next hit is stronger": one pill above the crosshair with the combined multiplier
        if it['place'] == 'alert' and re.match(r'x[\d.]+$', it['text']) and it.get('key') != 'buffAposJackpot':
            it['kind'] = 'next'
            it['mult'] = float(it['text'][1:])
        if it['place'] == 'spd':
            it['kind'] = 'reload' if it.get('key') == 'Crossbows_A7' else ('attack' if it['color'] == GOLD else 'move')
            it['pct'] = int(re.match(r'([+-]?\d+)%', it['text']).group(1))

DATA = {
  'classes': [{'id': c, 'name': n, 'items': items} for c, n, items in CLASSES],
  'general': GENERAL + COLUMN, 'zombie': ZOMBIE, 'feed': FEED,
  'png': PNG,
  'vanilla': {k: VANILLA[k] for k in VANILLA if not k.startswith('apos')},
}

html = open(os.path.join(SP, 'hud_template.html'), encoding='utf-8').read()
html = html.replace('/*__DATA__*/null', json.dumps(DATA, ensure_ascii=False))
open('docs/HudStatuses.html', 'w', encoding='utf-8').write(html)
print('ok', len(html))

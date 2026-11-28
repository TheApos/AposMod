// The two interactive sections of the Reference guide, mounted after every language switch (script.js -> AposWidgets.mount):
//   #mastery-app - Weapon Mastery 2.0: class tabs, the two-branch tree with hybrids, a skill card, a points planner, builds
//   #hud-app     - the HUD status atlas: a schematic game screen per weapon class, every status clickable
// Data: window.APOS_DATA from gen_widgets.py. Colours come from the guide's tokens (head.html); the game screen stays dark.
(function () {
  'use strict';
  var D = window.APOS_DATA;
  var L = 'en';

  // ===== interface strings =====
  var UI = {
    en: {
      level: 'Mastery level', points: 'Points', left: 'left', builds: 'Builds', reset: 'Reset tree', hint: 'Click a skill to learn a rank, right-click to take it back. The planner is only on this page; in the game you learn skills in the Alt+K window.',
      branch: 'Branch', tier: 'Tier', notable: 'Notable', capstone: 'Capstone', hybrid: 'Hybrid', bigHybrid: 'Big hybrid',
      trait: 'Class trait · works from level 0', traitShort: 'Class trait',
      needTier: 'Tier {t}: needs {n} points lower in this branch', needHybrid: 'Needs at least {n} points in each branch',
      met: 'Open', notMet: 'Locked', rank: 'Rank', of: 'of', learn: '+ Rank', unlearn: '− Rank', maxed: 'All ranks learned',
      synergies: 'Synergies', with: 'with', switchesOff: 'Switches off', weakens: 'Weakens', offBy: 'Switched off by {n}', weakBy: 'Weakened by {n}',
      hybrids: 'Hybrids', big: 'Big', pts18: 'points', legend: 'Synergy types', tierShort: 'T', off: 'OFF', weak: 'WEAKER',
      treeLabel: 'Skill tree', cardLabel: 'Skill card', attr: 'Attribute'
    },
    ru: {
      level: 'Уровень мастерства', points: 'Очки', left: 'осталось', builds: 'Билды', reset: 'Сбросить дерево', hint: 'Клик по навыку — изучить ранг, правый клик — снять. Планировщик только на этой странице; в игре навыки изучаются в окне Alt+K.',
      branch: 'Ветка', tier: 'Ярус', notable: 'Ключевой', capstone: 'Капстоун', hybrid: 'Гибрид', bigHybrid: 'Большой гибрид',
      trait: 'Черта класса · работает с 0 уровня', traitShort: 'Черта класса',
      needTier: 'Ярус {t}: нужно {n} очков ниже в этой ветке', needHybrid: 'Нужно не меньше {n} очков в каждой ветке',
      met: 'Открыт', notMet: 'Закрыт', rank: 'Ранг', of: 'из', learn: '+ Ранг', unlearn: '− Ранг', maxed: 'Все ранги изучены',
      synergies: 'Синергии', with: 'с', switchesOff: 'Отключает', weakens: 'Ослабляет', offBy: 'Отключён: {n}', weakBy: 'Ослаблен: {n}',
      hybrids: 'Гибриды', big: 'Большой', pts18: 'очков', legend: 'Типы синергий', tierShort: 'Я', off: 'ВЫКЛ', weak: 'СЛАБЕЕ',
      treeLabel: 'Дерево навыков', cardLabel: 'Карточка навыка', attr: 'Атрибут'
    },
    uk: {
      level: 'Рівень майстерності', points: 'Очки', left: 'лишилось', builds: 'Білди', reset: 'Скинути дерево', hint: 'Клік по навичці — вивчити ранг, правий клік — зняти. Планувальник лише на цій сторінці; у грі навички вчаться у вікні Alt+K.',
      branch: 'Гілка', tier: 'Ярус', notable: 'Ключова', capstone: 'Капстоун', hybrid: 'Гібрид', bigHybrid: 'Великий гібрид',
      trait: 'Риса класу · працює з 0 рівня', traitShort: 'Риса класу',
      needTier: 'Ярус {t}: потрібно {n} очок нижче в цій гілці', needHybrid: 'Потрібно щонайменше {n} очок у кожній гілці',
      met: 'Відкрито', notMet: 'Закрито', rank: 'Ранг', of: 'з', learn: '+ Ранг', unlearn: '− Ранг', maxed: 'Усі ранги вивчено',
      synergies: 'Симбіози', with: 'з', switchesOff: 'Вимикає', weakens: 'Послаблює', offBy: 'Вимкнено: {n}', weakBy: 'Послаблено: {n}',
      hybrids: 'Гібриди', big: 'Великий', pts18: 'очок', legend: 'Типи симбіозу', tierShort: 'Я', off: 'ВИМК', weak: 'СЛАБШЕ',
      treeLabel: 'Дерево навичок', cardLabel: 'Картка навички', attr: 'Атрибут'
    }
  };
  var HUI = {
    en: {
      general: 'General', zombie: 'Over zombies', feed: 'Reaction feed',
      places: { gR: 'Arc right of the crosshair', gL: 'Arc left of the crosshair', atk: 'Row under the crosshair (attack)', def: 'Defence (between health and the toolbelt)',
        spd: 'Energy: movement and attack speed (bottom right)', col: 'Above health: everything else (the game\'s buff column)', alert: 'Above the crosshair: urgent and important',
        z: 'Over the zombie', toast: 'Line under the crosshair', dmg: 'Over the damage number (like "Kill!")', mark: 'Marker on an arrow / bolt', feed: 'Reaction feed on the right', over: 'Marks on zombies' },
      legend: [['#ffd14d', 'right arc: the main stack of the weapon'], ['#ff9933', 'left arc: ability cooldowns'], ['#66bfff', 'row under the crosshair: attack'],
        ['#9aa197', 'between health and the toolbelt: defence'], ['#5ae68c', 'bottom right: energy, alike buffs merged with a sum'], ['#c8c8c8', 'above health: everything else'],
        ['#ffd200', 'above the crosshair: urgent; next-hit bonuses share one pill with the product']],
      note: 'The screen is schematic: game icons are drawn alike, not pixel-exact, and the HUD is 1.5x larger so the numbers read. Pulsing = full / ready. Tag "vanilla": the icon is the game\'s, the card lists the game\'s statuses with the same icon; "own": drawn for the mod.',
      all: 'All statuses', from: 'Where from', example: 'On screen', what: 'What it shows', when: 'When it appears', game: 'In-game description',
      vanUses: 'Where else the game uses this icon', modUses: 'The same icon in the mod', vanIcon: 'vanilla icon', ownIcon: 'own mod icon', van: 'vanilla', own: 'own',
      risk: 'Game statuses with this icon (buff column and Status Effects window) - easy to mix up:', perks: 'Game perks and skills (only in the skills menu):',
      also: 'Also', ui: 'game interface (buttons, windows)', nowhere: 'The game uses this icon nowhere else.', trait: 'class trait', skill: 'skill',
      hp: 'Health', st: 'Stamina',
      src: { base: 'Weapon Mastery · {c} · class trait (from level 0)', hybrid: 'Weapon Mastery · {c} · hybrid {id} (points in both branches)', branch: 'Weapon Mastery · {c} · branch {b} "{n}" · skill {id}' }
    },
    ru: {
      general: 'Общее', zombie: 'Над зомби', feed: 'Лента реакций',
      places: { gR: 'Дуга справа от прицела', gL: 'Дуга слева от прицела', atk: 'Ряд под прицелом (атака)', def: 'Защита (между здоровьем и поясом)',
        spd: 'Энергия: скорость движения и атаки (справа внизу)', col: 'Над здоровьем: всё остальное (колонка баффов игры)', alert: 'Над прицелом: срочное и важное',
        z: 'Над зомби', toast: 'Надпись под прицелом', dmg: 'Над числом урона (как «Труп!»)', mark: 'Метка на стреле / болте', feed: 'Лента реакций справа', over: 'Отметки на зомби' },
      legend: [['#ffd14d', 'дуга справа: главный стак оружия'], ['#ff9933', 'дуга слева: перезарядки умений'], ['#66bfff', 'ряд под прицелом: атака'],
        ['#9aa197', 'между здоровьем и поясом: защита'], ['#5ae68c', 'справа внизу: энергия, одинаковые баффы в одну строку с суммой'], ['#c8c8c8', 'над здоровьем: всё остальное'],
        ['#ffd200', 'над прицелом: срочное; бонусы следующего удара в одной плашке с произведением']],
      note: 'Сцена схематичная: значки игры нарисованы похоже, но не один в один, а HUD увеличен в 1.5 раза, чтобы читались числа. Пульсация — состояние «полный / готов». Метка «ваниль»: значок из игры, в карточке видно, какие её статусы с таким же значком; «свой» — нарисован для мода.',
      all: 'Все статусы', from: 'Откуда', example: 'На экране', what: 'Что показывает', when: 'Когда появляется', game: 'Описание в игре',
      vanUses: 'Где ещё этот значок в игре', modUses: 'Этот же значок в моде', vanIcon: 'значок игры', ownIcon: 'свой значок мода', van: 'ваниль', own: 'свой',
      risk: 'Статусы игры с этим значком (колонка баффов и окно статусов) — их легко спутать:', perks: 'Перки и навыки игры (только в меню навыков):',
      also: 'Также', ui: 'интерфейс игры (кнопки, окна)', nowhere: 'Больше нигде в игре этот значок не используется.', trait: 'черта класса', skill: 'навык',
      hp: 'Здоровье', st: 'Выносливость',
      src: { base: 'Мастерство оружия · {c} · черта класса (с 0 уровня)', hybrid: 'Мастерство оружия · {c} · гибрид {id} (очки в обеих ветках)', branch: 'Мастерство оружия · {c} · ветка {b} «{n}» · навык {id}' }
    },
    uk: {
      general: 'Загальне', zombie: 'Над зомбі', feed: 'Стрічка реакцій',
      places: { gR: 'Дуга праворуч від прицілу', gL: 'Дуга ліворуч від прицілу', atk: 'Ряд під прицілом (атака)', def: 'Захист (між ХП і поясом)',
        spd: 'Енергія: швидкість руху й атаки (справа внизу)', col: 'Над ХП: усе інше (колонка баффів гри)', alert: 'Над прицілом: екстрене й важливе',
        z: 'Над зомбі', toast: 'Напис під прицілом', dmg: 'Над числом шкоди (як «Труп!»)', mark: 'Мітка на стрілі / болті', feed: 'Стрічка реакцій праворуч', over: 'Позначки на зомбі' },
      legend: [['#ffd14d', 'дуга справа: головний стак зброї'], ['#ff9933', 'дуга зліва: перезарядки вмінь'], ['#66bfff', 'ряд під прицілом: атака'],
        ['#9aa197', 'між ХП і поясом: захист'], ['#5ae68c', 'справа внизу: енергія, однакові бафи в один рядок із сумою'], ['#c8c8c8', 'над ХП: усе інше'],
        ['#ffd200', 'над прицілом: екстрене; бонуси наступного удару — одна плашка з добутком']],
      note: 'Сцена схематична: значки гри намальовані схоже, але не один в один, а HUD збільшено в 1.5 раза, щоб числа читались. Пульсація — стан «повний / готовий». Позначка «ваніль»: значок із гри, у картці видно, які її статуси мають такий самий; «свій» — намальований для мода.',
      all: 'Усі статуси', from: 'Звідки', example: 'На екрані', what: 'Що показує', when: 'Коли з\'являється', game: 'Опис у грі',
      vanUses: 'Де ще цей значок у грі', modUses: 'Цей самий значок у моді', vanIcon: 'ванільний значок', ownIcon: 'власний значок мода', van: 'ваніль', own: 'свій',
      risk: 'Статуси гри з цим значком (колонка баффів і вікно статусів) — їх можна сплутати:', perks: 'Перки й навички гри (лише в меню навичок):',
      also: 'Також', ui: 'інтерфейс гри (кнопки, вікна)', nowhere: 'У грі цей значок більше ніде не використовується.', trait: 'риса класу', skill: 'навичка',
      hp: 'ХП', st: 'Витривалість',
      src: { base: 'Майстерність зброї · {c} · риса класу (з 0 рівня)', hybrid: 'Майстерність зброї · {c} · гібрид {id} (очки в обох гілках)', branch: 'Майстерність зброї · {c} · гілка {b} «{n}» · навичка {id}' }
    }
  };
  var SYN = {
    S1: { c: 'var(--mw-s1)', n: { en: 'Hybrid', ru: 'Гибрид', uk: 'Гібрид' }, d: { en: 'A skill between the branches: needs ≥5 (or ≥8) points in both.', ru: 'Навык между ветками: нужно ≥5 (или ≥8) очков в обеих.', uk: 'Навичка між гілками: потрібно ≥5 (або ≥8) очок в обох.' } },
    S2: { c: 'var(--mw-s2)', n: { en: 'Attribute resonance', ru: 'Резонанс атрибута', uk: 'Резонанс атрибута' }, d: { en: 'Strength = attribute level (1-10) × rank.', ru: 'Сила = уровень атрибута (1–10) × ранг.', uk: 'Сила = рівень атрибута (1–10) × ранг.' } },
    S3: { c: 'var(--mw-s3)', n: { en: 'Element', ru: 'Стихия', uk: 'Стихія' }, d: { en: 'Applies an element or hits harder on a target that has it. Bleeding is the 4th element.', ru: 'Накладывает стихию или бьёт сильнее по цели с ней. Кровотечение — 4-я стихия.', uk: 'Накладає стихію або б\'є сильніше по цілі з нею. Кровотеча — 4-та стихія.' } },
    S4: { c: 'var(--mw-s4)', n: { en: 'Weapon tandem', ru: 'Тандем оружия', uk: 'Тандем зброї' }, d: { en: 'Works together with another weapon class.', ru: 'Работает вместе с другим классом оружия.', uk: 'Працює разом з іншим класом зброї.' } },
    S5: { c: 'var(--mw-s5)', n: { en: 'Rank threshold', ru: 'Порог ранга', uk: 'Поріг рангу' }, d: { en: 'The last rank adds a new mechanic, not just %.', ru: 'Последний ранг даёт новую механику, а не только %.', uk: 'Останній ранг дає нову механіку, а не лише %.' } },
    S6: { c: 'var(--mw-s6)', n: { en: 'Trade-off', ru: 'Компромисс', uk: 'Компроміс' }, d: { en: 'A strong bonus with a downside; another skill offsets it.', ru: 'Сильный бонус с минусом; другой навык его гасит.', uk: 'Сильний бонус з мінусом; інша навичка його гасить.' } }
  };

  function tx(o) { return o == null ? '' : typeof o === 'string' ? o : (o[L] != null && o[L] !== '' ? o[L] : o.en); }
  function u(k) { return UI[L][k] || UI.en[k] || k; }
  function h(k) { return HUI[L][k] != null ? HUI[L][k] : HUI.en[k]; }
  function fmt(s, o) { return s.replace(/\{(\w+)\}/g, function (m, k) { return o[k] != null ? o[k] : m; }); }
  function esc(s) { return String(s == null ? '' : s).replace(/[&<>"]/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]; }); }
  function store(key, def) { try { var v = JSON.parse(localStorage.getItem(key) || 'null'); return v || def; } catch (e) { return def; } }
  function keep(key, v) { try { localStorage.setItem(key, JSON.stringify(v)); } catch (e) { /* storage blocked: fine */ } }

  // =====================================================================================================
  // Weapon Mastery 2.0
  // =====================================================================================================
  var M = D.mastery;
  var TIER_NEED = { 1: 0, 2: 3, 3: 6, 4: 9, 5: 12 }, ROW = { 5: 1, 4: 2, 3: 3, 2: 4, 1: 5 };
  var ms = store('aposRefMastery', { cls: 'pistols', level: 25, ranksBy: {}, selBy: {} });
  if (!M.classes[ms.cls]) ms.cls = M.order[0];
  var C, byId, mroot;

  function mIcon(n) {
    return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' + (M.icons[n] || M.icons.star) + '</svg>';
  }
  function setClass(k) {
    ms.cls = k; C = M.classes[k]; byId = {};
    C.skills.forEach(function (s) { byId[s.id] = s; });
    if (!ms.ranksBy[k]) ms.ranksBy[k] = Object.assign({}, (C.presets[C.presets.length - 1] || { r: {} }).r);
    if (!ms.selBy[k]) ms.selBy[k] = 'base';
  }
  function R() { return ms.ranksBy[ms.cls]; }
  function sel() { return ms.selBy[ms.cls]; }
  function rk(id) { return R()[id] || 0; }
  function spentIn(br, below, ranks) {
    ranks = ranks || R(); var n = 0;
    C.skills.forEach(function (s) { if (s.br === br && (below == null || s.tier < below)) n += ranks[s.id] || 0; });
    return n;
  }
  function spent(ranks) { ranks = ranks || R(); var n = 0; for (var k in ranks) n += ranks[k] || 0; return n; }
  function reqMet(s, ranks) {
    if (s.br === 'H') return spentIn('A', null, ranks) >= s.need && spentIn('B', null, ranks) >= s.need;
    return spentIn(s.br, s.tier, ranks) >= TIER_NEED[s.tier];
  }
  function canLearn(s) { return rk(s.id) < s.max && reqMet(s) && spent() < ms.level; }
  function canUnlearn(s) {
    if (!rk(s.id)) return false;
    var t = Object.assign({}, R()); t[s.id]--;
    return C.skills.every(function (o) { return !(t[o.id] > 0) || reqMet(o, t); });
  }
  function learn(id) { var s = byId[id]; if (s && canLearn(s)) { R()[id] = rk(id) + 1; keep('aposRefMastery', ms); } renderM(); }
  function unlearn(id) { var s = byId[id]; if (s && canUnlearn(s)) { R()[id] = rk(id) - 1; if (!R()[id]) delete R()[id]; keep('aposRefMastery', ms); } renderM(); }
  // a learned capstone may switch off ("dead") or weaken ("weak") other skills
  function brokenBy(s) {
    var out = null;
    C.skills.forEach(function (c) {
      if (!c.breaks || !rk(c.id)) return;
      c.breaks.forEach(function (b) { if (b.ids.indexOf(s.id) >= 0 && (!out || b.lvl === 'dead')) out = { lvl: b.lvl, why: tx(b.why), by: tx(c.name) }; });
    });
    return out;
  }
  function nameOf(id) {
    if (byId[id]) return tx(byId[id].name);
    if (C.external[id]) return tx(C.external[id].name);
    if (id === 'ATTR') return u('attr') + ': ' + tx(C.attr);
    if (id === 'base') return tx(C.base.name);
    return id;
  }
  function linked(id) {
    var out = {}, s = byId[id];
    (s && s.syn || []).forEach(function (y) { if (y.w) out[y.w] = SYN[y.t].c; });
    C.skills.forEach(function (o) { (o.syn || []).forEach(function (y) { if (y.w === id) out[o.id] = SYN[y.t].c; }); });
    return out;
  }

  function nodeHtml(s, links) {
    var r = rk(s.id), cls = ['mw-node', 'mw-' + s.br];
    if (r) cls.push('has');
    if (!reqMet(s)) cls.push('locked'); else if (canLearn(s)) cls.push('can');
    if (s.notable) cls.push('notable');
    if (s.cap || s.big) cls.push('cap');
    if (sel() === s.id) cls.push('sel');
    var bk = brokenBy(s); if (bk) cls.push(bk.lvl);
    var lc = links[s.id]; if (lc) cls.push('link');
    var col, row;
    if (s.br === 'H') { col = '4'; row = s.row; }
    else { row = ROW[s.tier]; var base = s.br === 'A' ? 2 : 5; col = s.span ? base + ' / span 2' : String(base + s.col); }
    var pips = ''; for (var i = 0; i < s.max; i++) pips += '<i class="' + (i < r ? 'on' : '') + '"></i>';
    return '<button type="button" class="' + cls.join(' ') + '" data-id="' + s.id + '" style="grid-column:' + col + ';grid-row:' + row + (lc ? ';--lc:' + lc : '') +
      '" aria-label="' + esc(tx(s.name)) + ', ' + u('rank') + ' ' + r + ' ' + u('of') + ' ' + s.max + '">' +
      (s.notable ? '<span class="mw-star">★</span>' : '') + (s.cap ? '<span class="mw-star">CAP</span>' : '') + (s.big ? '<span class="mw-star">≥8</span>' : '') +
      (bk ? '<span class="mw-brk">' + (bk.lvl === 'dead' ? u('off') : u('weak')) + '</span>' : '') +
      '<span class="mw-ic">' + mIcon(s.icon) + '</span><span class="mw-pips">' + pips + '</span><span class="mw-nm">' + esc(tx(s.name)) + '</span></button>';
  }

  function renderTree() {
    var links = linked(sel()), out = '<div class="mw-center" aria-hidden="true"></div>';
    [5, 4, 3, 2, 1].forEach(function (t) {
      ['A', 'B'].forEach(function (b) {
        var on = spentIn(b, t) >= TIER_NEED[t];
        out += '<div class="mw-rail mw-' + b + (b === 'B' ? ' r' : '') + (on ? ' on' : '') + '" style="grid-column:' + (b === 'A' ? 1 : 7) + ';grid-row:' + ROW[t] +
          '" title="' + esc(fmt(u('needTier'), { t: t, n: TIER_NEED[t] })) + '"><small>' + u('tierShort') + t + '</small>' + (on ? TIER_NEED[t] : '🔒' + TIER_NEED[t]) + '</div>';
      });
    });
    var a = spentIn('A'), b = spentIn('B');
    out += '<div class="mw-hynote">' + u('hybrids') + '<br><b>' + Math.min(a, 5) + '/5 · ' + Math.min(b, 5) + '/5</b><br>' + u('big') + '<br><b>' + Math.min(a, 8) + '/8 · ' + Math.min(b, 8) + '/8</b></div>';
    C.skills.forEach(function (s) { out += nodeHtml(s, links); });
    out += '<button type="button" class="mw-base' + (sel() === 'base' ? ' sel' : '') + '" data-id="base"><span class="t">◆ ' + u('traitShort') + ' · ' + esc(tx(C.base.name)) + '</span><span class="d">' + esc(tx(C.base.desc)) + '</span></button>';
    mroot.querySelector('.mw-tree').innerHTML = out;
    mroot.querySelector('.mw-bheads').innerHTML = ['A', 'B'].map(function (k) {
      var n = spentIn(k), seg = '';
      for (var i = 0; i < 18; i++) seg += '<i class="' + (i < n ? 'on' : '') + ([3, 6, 9, 12].indexOf(i) >= 0 ? ' tick' : '') + '"></i>';
      return '<div class="mw-bh mw-' + k + '"><div class="mw-eyebrow">' + u('branch') + ' ' + k + '</div><h4>' + esc(tx(C.br[k].name)) + '</h4><p>' + esc(tx(C.br[k].idea)) +
        '</p><div class="mw-seg">' + seg + '</div><div class="mw-cnt">' + n + ' / 18 ' + u('pts18') + '</div></div>';
    }).join('');
  }

  function renderDetail() {
    var d = mroot.querySelector('.mw-detail'), s = byId[sel()];
    if (!s) {
      d.innerHTML = '<div class="mw-dhead"><span class="mw-dic trait">' + mIcon(C.base.icon) + '</span><div><div class="mw-eyebrow">' + u('trait') + '</div><h4>' + esc(tx(C.base.name)) + '</h4>' +
        (L !== 'en' ? '<div class="mw-en">' + esc(C.base.name.en) + '</div>' : '') + '</div></div><p class="mw-desc">' + esc(tx(C.base.desc)) + '</p>';
      return;
    }
    var r = rk(s.id), badges = [];
    badges.push(s.br === 'H' ? (s.big ? u('bigHybrid') : u('hybrid')) : u('branch') + ' ' + s.br + ' · ' + tx(C.br[s.br].name));
    if (s.br !== 'H') badges.push(u('tier') + ' ' + s.tier);
    if (s.notable) badges.push(u('notable'));
    if (s.cap) badges.push(u('capstone'));
    var met = reqMet(s);
    var req = s.br === 'H' ? fmt(u('needHybrid'), { n: s.need }) : fmt(u('needTier'), { t: s.tier, n: TIER_NEED[s.tier] });
    var ranks = tx(s.ranks).map(function (t, i) {
      return '<div class="mw-rk' + (i < r ? ' got' : '') + (i === r ? ' next' : '') + '"><span class="n">' + (i + 1) + '/' + s.max + '</span><span>' + esc(t) + '</span></div>';
    }).join('');
    var syn = (s.syn || []).map(function (y) {
      var who = y.w ? ' ' + u('with') + ' ' + (byId[y.w] || y.w === 'base' ? '<button type="button" class="mw-go" data-go="' + y.w + '">' + esc(nameOf(y.w)) + '</button>' : '<b>' + esc(nameOf(y.w)) + '</b>') : '';
      return '<div class="mw-syn"><span class="mw-tag" style="background:' + SYN[y.t].c + '">' + esc(tx(SYN[y.t].n)) + '</span><p>' + who + (who ? ': ' : '') + esc(tx(y.x)) + '</p></div>';
    }).join('');
    var brk = '';
    (s.breaks || []).forEach(function (b) {
      brk += '<div class="mw-note ' + b.lvl + '"><b>' + (b.lvl === 'dead' ? u('switchesOff') : u('weakens')) + ':</b> ' + esc(b.ids.map(nameOf).join(', ')) + '. ' + esc(tx(b.why)) + '</div>';
    });
    var bk = brokenBy(s);
    if (bk) brk += '<div class="mw-note ' + bk.lvl + '"><b>' + esc(fmt(bk.lvl === 'dead' ? u('offBy') : u('weakBy'), { n: bk.by })) + '.</b> ' + esc(bk.why) + '</div>';
    d.innerHTML = '<div class="mw-dhead"><span class="mw-dic mw-' + s.br + (r ? ' has' : '') + '">' + mIcon(s.icon) + '</span><div><h4>' + esc(tx(s.name)) + '</h4>' +
      (L !== 'en' ? '<div class="mw-en">' + esc(s.name.en) + ' · ' + s.id + '</div>' : '<div class="mw-en">' + s.id + '</div>') +
      '<div class="mw-badges">' + badges.map(function (b) { return '<span>' + esc(b) + '</span>'; }).join('') + '</div></div></div>' +
      '<div class="mw-req ' + (met ? 'ok' : 'no') + '">' + (met ? '✓ ' + u('met') : '🔒 ' + u('notMet')) + ' · ' + esc(req) + '</div>' +
      '<p class="mw-desc">' + esc(tx(s.desc)) + '</p><div class="mw-ranks">' + ranks + '</div>' +
      '<div class="mw-actions"><button type="button" class="mw-btn" data-learn="' + s.id + '"' + (canLearn(s) ? '' : ' disabled') + '>' + u('learn') + '</button>' +
      '<button type="button" class="mw-btn" data-unlearn="' + s.id + '"' + (canUnlearn(s) ? '' : ' disabled') + '>' + u('unlearn') + '</button>' +
      '<span class="mw-muted">' + u('rank') + ' ' + r + ' ' + u('of') + ' ' + s.max + (r >= s.max ? ' · ' + u('maxed') : '') + '</span></div>' + brk +
      (syn ? '<div class="mw-eyebrow">' + u('synergies') + '</div><div class="mw-syns">' + syn + '</div>' : '');
  }

  function renderM() {
    if (!mroot) return;
    mroot.querySelectorAll('.mw-tab').forEach(function (b) { b.setAttribute('aria-selected', b.dataset.cls === ms.cls ? 'true' : 'false'); });
    var left = Math.max(0, ms.level - spent());
    mroot.querySelector('.mw-lvlv').textContent = ms.level;
    mroot.querySelector('.mw-lvl').value = ms.level;
    mroot.querySelector('.mw-pts').textContent = spent() + ' / ' + ms.level + ' · ' + left + ' ' + u('left');
    mroot.querySelector('.mw-pts').classList.toggle('bad', spent() > ms.level);
    mroot.querySelector('.mw-presets').innerHTML = C.presets.map(function (p, i) { return '<button type="button" class="mw-btn" data-preset="' + i + '">' + esc(tx(p.n)) + '</button>'; }).join('');
    renderTree();
    renderDetail();
  }

  function mountMastery(root) {
    mroot = root;
    setClass(ms.cls);
    root.innerHTML =
      '<div class="mw-tabs" role="tablist">' + M.order.map(function (k) { return '<button type="button" class="mw-tab" role="tab" data-cls="' + k + '">' + esc(tx(M.classes[k].name)) + '</button>'; }).join('') + '</div>' +
      '<div class="mw-tools"><label class="mw-lvlbox"><span class="mw-eyebrow">' + u('level') + '</span><input type="range" class="mw-lvl" min="0" max="25" aria-label="' + esc(u('level')) + '"><b class="mw-lvlv"></b></label>' +
      '<span class="mw-chip">' + u('points') + ' <b class="mw-pts"></b></span>' +
      '<span class="mw-eyebrow">' + u('builds') + '</span><span class="mw-presets"></span><button type="button" class="mw-btn mw-reset">' + u('reset') + '</button></div>' +
      '<div class="mw-body"><div class="mw-treepane" aria-label="' + esc(u('treeLabel')) + '"><div class="mw-bheads"></div><div class="mw-scroll"><div class="mw-tree"></div></div><p class="mw-hint">' + esc(u('hint')) + '</p></div>' +
      '<div class="mw-detail" aria-live="polite" aria-label="' + esc(u('cardLabel')) + '"></div></div>' +
      '<div class="mw-legend"><div class="mw-eyebrow">' + u('legend') + '</div><div class="mw-lg">' + Object.keys(SYN).map(function (k) {
        return '<div><span class="mw-tag" style="background:' + SYN[k].c + '">' + esc(tx(SYN[k].n)) + '</span> ' + esc(tx(SYN[k].d)) + '</div>';
      }).join('') + '</div></div>';
    root.querySelector('.mw-tabs').addEventListener('click', function (e) { var b = e.target.closest('[data-cls]'); if (!b) return; setClass(b.dataset.cls); keep('aposRefMastery', ms); renderM(); });
    root.querySelector('.mw-lvl').addEventListener('input', function (e) { ms.level = +e.target.value; keep('aposRefMastery', ms); renderM(); });
    root.querySelector('.mw-presets').addEventListener('click', function (e) {
      var b = e.target.closest('[data-preset]'); if (!b) return;
      ms.ranksBy[ms.cls] = Object.assign({}, C.presets[+b.dataset.preset].r); ms.level = 25; keep('aposRefMastery', ms); renderM();
    });
    root.querySelector('.mw-reset').addEventListener('click', function () { ms.ranksBy[ms.cls] = {}; keep('aposRefMastery', ms); renderM(); });
    var tree = root.querySelector('.mw-tree');
    tree.addEventListener('click', function (e) {
      var n = e.target.closest('[data-id]'); if (!n) return;
      ms.selBy[ms.cls] = n.dataset.id;
      if (byId[n.dataset.id] && canLearn(byId[n.dataset.id])) learn(n.dataset.id); else { keep('aposRefMastery', ms); renderM(); }
    });
    tree.addEventListener('contextmenu', function (e) {
      var n = e.target.closest('[data-id]'); if (!n) return;
      e.preventDefault(); ms.selBy[ms.cls] = n.dataset.id;
      if (byId[n.dataset.id] && canUnlearn(byId[n.dataset.id])) unlearn(n.dataset.id); else renderM();
    });
    root.querySelector('.mw-detail').addEventListener('click', function (e) {
      var b = e.target.closest('button'); if (!b) return;
      if (b.dataset.learn) learn(b.dataset.learn);
      else if (b.dataset.unlearn) unlearn(b.dataset.unlearn);
      else if (b.dataset.go) { ms.selBy[ms.cls] = b.dataset.go; keep('aposRefMastery', ms); renderM(); }
    });
    renderM();
  }

  // =====================================================================================================
  // HUD status atlas
  // =====================================================================================================
  var H = D.hud;
  var hs = store('aposRefHud', { tab: 'knives' });
  var hroot, items = [];
  var U = function (n) { return (n / 1440 * 100).toFixed(3) + 'cqw'; };
  var S = 1.5, CX = 860, CY = 210;
  var FEEDICONS = {
    feed_explosion: [['elite_Burning', '#ff7319'], '+', ['elite_Acidic', '#99e633'], '=', ['elite_Explosive', '#ffcc40']],
    feed_confusion: [['elite_Burning', '#ff7319'], '+', ['elite_Electric', '#59bfff'], '='],
    feed_miasma: [['elite_Acidic', '#99e633'], '+', ['elite_Electric', '#59bfff'], '='],
    feed_emp: [['elite_Electric', '#59bfff']], feed_overkill: [['elite_Fast', '#ff8c59']], feed_chain: [['elite_Explosive', '#ffcc40']],
    feed_shrapnel: [['elite_Armored', '#ccccd9']], feed_pierce: [['elite_Fast', '#ffd14d']], feed_shock: [['elite_Electric', '#59bfff']],
    feed_sticky: [['elite_Explosive', '#ff7319']], feed_breakthrough: [['elite_Fast', '#ffd14d']], feed_spine: [['ui_apos_backstab', '#f21f1f']]
  };

  function el(tag, attrs, kids) {
    var e = document.createElement(tag);
    for (var k in attrs || {}) {
      if (k === 'style') Object.assign(e.style, attrs[k]);
      else if (k === 'text') e.textContent = attrs[k];
      else if (k === 'cls') e.className = attrs[k];
      else e.setAttribute(k, attrs[k]);
    }
    (kids || []).forEach(function (c) { if (c) e.appendChild(c); });
    return e;
  }
  function hIcon(name, color, size) {
    var st = { width: size, height: size };
    if (H.png[name]) return el('span', { cls: 'hw-ic png', style: Object.assign(st, { background: color, webkitMaskImage: 'url(' + H.png[name] + ')', maskImage: 'url(' + H.png[name] + ')' }) });
    var sp = el('span', { cls: 'hw-ic', style: Object.assign(st, { color: color }) });
    sp.innerHTML = '<svg viewBox="0 0 24 24"><use href="#h-' + name + '"/></svg>';
    return sp;
  }
  function feedIcons(it, size) {
    return (FEEDICONS[it.icon] || []).map(function (p) { return typeof p === 'string' ? el('span', { cls: 'op', text: p }) : hIcon(p[0], p[1], size); });
  }
  function iconKind(it) {
    if (!it.icon || it.place === 'toast' || it.place === 'mark' || it.place === 'over' || it.place === 'dmg') return null;
    if (it.place === 'feed' || H.png[it.icon]) return 'own';
    return 'van';
  }
  function srcOf(it) {
    if (it.src) return tx(it.src);
    if (!it.skill) return '';
    var c = M.classes[it.skill.cls], cn = tx(c.name);
    if (it.skill.id === 'Base') return fmt(h('src').base, { c: cn });
    if (it.skill.br === 'H') return fmt(h('src').hybrid, { c: cn, id: it.skill.id });
    return fmt(h('src').branch, { c: cn, b: it.skill.br, n: tx(c.br[it.skill.br].name), id: it.skill.id });
  }
  function tabsList() {
    return [{ id: 'general', name: h('general') }].concat(M.order.map(function (k) { return { id: k, name: tx(M.classes[k].name) }; }))
      .concat([{ id: 'zombie', name: h('zombie') }, { id: 'feed', name: h('feed') }]);
  }
  function itemsOf(id) {
    if (id === 'general') return H.general;
    if (id === 'zombie') return H.zombie;
    if (id === 'feed') return H.feed;
    return H.classes[id] || [];
  }

  function gaugeSvg(it, left) {
    var r = 62 * S, thick = 5 * S, span = 70, segs = it.seg > 0 ? it.seg : 1;
    var gap = segs > 1 ? Math.min(3.5, 30 / segs) : 0, seg = (span - gap * (segs - 1)) / segs;
    function pt(a) { var rad = a * Math.PI / 180; return [CX + (left ? -1 : 1) * r * Math.cos(rad), CY + r * Math.sin(rad)]; }
    function arc(a0, a1) { var p0 = pt(a0), p1 = pt(a1); return 'M' + p0[0].toFixed(1) + ' ' + p0[1].toFixed(1) + ' A' + r + ' ' + r + ' 0 0 ' + (left ? 1 : 0) + ' ' + p1[0].toFixed(1) + ' ' + p1[1].toFixed(1); }
    var out = '';
    for (var i = 0; i < segs; i++) {
      var a0 = span / 2 - i * (seg + gap);
      var fill = it.seg > 0 ? Math.max(0, Math.min(1, it.value - i)) : Math.max(0, Math.min(1, it.value));
      out += '<path d="' + arc(a0, a0 - seg) + '" stroke="rgba(255,255,255,.14)" stroke-width="' + thick + '" fill="none"/>';
      if (fill > 0) {
        if (it.full) out += '<path class="hw-glow" d="' + arc(a0, a0 - seg * fill) + '" stroke="' + it.color + '" stroke-opacity=".2" stroke-width="' + thick * 2.6 + '" fill="none"/>';
        out += '<path class="' + (it.full ? 'hw-glow' : '') + '" d="' + arc(a0, a0 - seg * fill) + '" stroke="' + it.color + '" stroke-opacity=".92" stroke-width="' + thick + '" fill="none"/>';
      }
    }
    return out;
  }
  function hit(idx, x, y, kid, extra) {
    var it = items[idx];
    var e = el('div', { cls: 'hw-hit', tabindex: '0', role: 'button', 'aria-label': tx(it.name) + ' ' + (it.text || ''), 'data-i': idx,
      style: Object.assign({ left: U(x), top: U(y) }, extra || {}) }, [kid]);
    e.addEventListener('click', function () { select(idx); });
    e.addEventListener('keydown', function (ev) { if (ev.key === 'Enter' || ev.key === ' ') { ev.preventDefault(); select(idx); } });
    return e;
  }
  function inline(e) { e.style.position = 'relative'; e.style.left = e.style.top = '0'; return e; }
  function slot(it) { return el('div', { cls: 'hw-slot' + (it.full ? ' hw-glow' : '') }, [hIcon(it.icon, it.color, U(22 * S)), it.text ? el('span', { cls: 't', text: it.text }) : null]); }

  function drawScene() {
    var sc = hroot.querySelector('.hw-scene');
    sc.innerHTML = '';
    var svg = '<g stroke="#fff" stroke-width="2.4" opacity=".85"><path d="M' + (CX - 16) + ' ' + CY + 'h10M' + (CX + 6) + ' ' + CY + 'h10M' + CX + ' ' + (CY - 16) + 'v10M' + CX + ' ' + (CY + 6) + 'v10"/></g>';
    var g = { gR: [], gL: [], atk: [], def: [], spd: [], col: [], alert: [], z: [], toast: [], dmg: [], mark: [], feed: [], over: [] };
    items.forEach(function (it, i) { if (g[it.place]) g[it.place].push(i); });
    var BARS_X = 16, BARS_W = 300, beltW = 400, beltX = CX - beltW / 2, beltY = 750 - 52, i, k;
    for (i = 0; i < 10; i++) svg += '<rect x="' + (beltX + i * 40 + 2) + '" y="' + beltY + '" width="36" height="44" fill="rgba(0,0,0,.42)" stroke="rgba(255,255,255,.12)"/>';
    var zx = 1180, zy = 300;
    if (g.z.length || g.over.length || g.mark.length || g.dmg.length) {
      svg += '<g fill="#151a14" opacity=".92"><circle cx="' + zx + '" cy="' + zy + '" r="26"/><path d="M' + (zx - 34) + ' ' + (zy + 30) + 'h68l12 120-14 4-8-60-6 70 10 150h-28l-14-130-14 130h-28l10-150-6-70-8 60-14-4z"/><path d="M' + (zx + 32) + ' ' + (zy + 40) + 'l70 34-6 12-70-26z"/></g>';
      svg += '<ellipse cx="' + zx + '" cy="' + (zy + 350) + '" rx="70" ry="10" fill="rgba(0,0,0,.35)"/>';
    }
    ['gR', 'gL'].forEach(function (p) { if (g[p].length) svg += gaugeSvg(items[g[p][0]], p === 'gL'); });
    g.over.forEach(function (i) {
      var it = items[i];
      svg += it.icon === 'outline'
        ? '<path d="M' + (zx - 38) + ' ' + (zy + 28) + 'h76l14 124-16 6-8-56-4 66 10 156h-34l-12-126-12 126h-34l10-156-4-66-8 56-16-6z" fill="none" stroke="' + it.color + '" stroke-width="3" opacity=".8"/><circle cx="' + zx + '" cy="' + zy + '" r="30" fill="none" stroke="' + it.color + '" stroke-width="3" opacity=".8"/>'
        : '<circle class="hw-glow" cx="' + zx + '" cy="' + zy + '" r="20" fill="none" stroke="' + it.color + '" stroke-width="3"/>';
    });
    g.mark.forEach(function (i, k) {
      var mx = zx - 220 + k * 70, my = 640;
      svg += '<ellipse class="hw-glow" cx="' + mx + '" cy="' + my + '" rx="22" ry="9" fill="none" stroke="' + items[i].color + '" stroke-width="3"/><path d="M' + (mx - 2) + ' ' + my + 'l-14 -20" stroke="#c9b48a" stroke-width="3"/>';
    });
    var layer = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    layer.setAttribute('class', 'hw-layer'); layer.setAttribute('viewBox', '0 0 1440 750'); layer.innerHTML = svg;
    sc.appendChild(layer);

    ['gR', 'gL'].forEach(function (p) {
      if (!g[p].length) return;
      var i = g[p][0], it = items[i], left = p === 'gL', r = 62 * S, x = CX + (left ? -1 : 1) * (r + 10 * S);
      var kids = [hIcon(it.icon, it.color, U(18 * S)), el('span', { cls: 'hw-ztag' }, [el('span', { cls: 't', text: it.text, style: { color: '#fff' } })])];
      sc.appendChild(hit(i, x, CY - 14, el('div', { cls: 'hw-zrow' }, left ? kids.reverse() : kids), left ? { transform: 'translateX(-100%)' } : null));
      sc.appendChild(hit(i, left ? CX - r - 16 : CX + r * 0.8, CY - r * 0.45, el('div', { style: { width: U(36), height: U(r * 0.9) } })));
    });
    if (g.alert.length) {
      var box = el('div', { style: { position: 'absolute', left: U(CX - 400), width: U(800), top: U(CY - 150), display: 'flex', justifyContent: 'center', gap: U(16) } });
      var next = g.alert.filter(function (i) { return items[i].kind === 'next'; });
      if (next.length > 1) {
        var m = next.reduce(function (s, i) { return s * items[i].mult; }, 1);
        var pill = el('div', { cls: 'hw-pill hw-glow', style: { borderBottomColor: '#ffd14d' } });
        next.forEach(function (i) { pill.appendChild(inline(hit(i, 0, 0, hIcon(items[i].icon, items[i].color, U(40 * 1.3))))); });
        pill.appendChild(el('span', { cls: 't', text: 'x' + Math.round(m * 10) / 10 }));
        box.appendChild(pill);
      }
      g.alert.filter(function (i) { return next.length < 2 || items[i].kind !== 'next'; }).forEach(function (i) {
        var it = items[i];
        box.appendChild(inline(hit(i, 0, 0, el('div', { cls: 'hw-pill hw-glow', style: { borderBottomColor: it.color } }, [hIcon(it.icon, it.color, U(40 * 1.3)), el('span', { cls: 't', text: it.text })]))));
      });
      sc.appendChild(box);
    }
    var w = 78 * S;
    g.atk.forEach(function (i, k) { sc.appendChild(hit(i, CX - g.atk.length * w / 2 + k * w, CY + 42 * S, slot(items[i]))); });
    var perRow = Math.max(1, Math.floor((beltX - 12 - (BARS_X + BARS_W + 16)) / w));
    g.def.forEach(function (i, k) { sc.appendChild(hit(i, beltX - 12 - (k % perRow + 1) * w, 750 - 50 - Math.floor(k / perRow) * 48, slot(items[i]))); });
    if (g.z.length) {
      var zb = el('div', { style: { position: 'absolute', left: U(zx - 250), width: U(500), bottom: U(750 - zy + 40), display: 'flex', flexWrap: 'wrap-reverse', justifyContent: 'center', gap: U(6) + ' ' + U(14) } });
      g.z.forEach(function (i) {
        var it = items[i];
        zb.appendChild(inline(hit(i, 0, 0, el('div', { cls: 'hw-ztag' }, [hIcon(it.icon, it.color, U(20 * S)), it.text ? el('span', { cls: 't', text: it.text, style: { color: it.color } }) : null]))));
      });
      sc.appendChild(zb);
    }
    g.dmg.forEach(function (i, k) {
      var it = items[i], big = it.text.length > 10;
      var kids = [el('div', { cls: 'hw-dmglabel', text: it.text, style: { color: it.color, fontSize: U(big ? 32 : 28) } })];
      if (it.sub) kids.push(el('div', { cls: 'hw-dmglabel', text: tx(it.sub), style: { color: it.color, fontSize: U(17) } }));
      kids.push(el('div', { cls: 'hw-dmgnum', text: big ? '184' : '92' }));
      sc.appendChild(hit(i, zx + 120, (k === 0 ? zy - 110 : zy + 110) - 60, el('div', { cls: 'hw-dmgstack' }, kids), { transform: 'translateX(-50%)' }));
    });
    g.toast.forEach(function (i, k) { sc.appendChild(hit(i, CX, CY + 42 * S + 60 + k * 44, el('div', { cls: 'hw-toast', text: items[i].text, style: { color: items[i].color } }), { transform: 'translateX(-50%)' })); });
    g.feed.forEach(function (i, k) {
      var it = items[i];
      var line = el('div', { cls: 'hw-feed' }, [el('span', { cls: 'src', text: tx(it.name) })].concat(feedIcons(it, U(22 * S))).concat(it.text ? [el('span', { cls: 't', text: it.text })] : []));
      sc.appendChild(hit(i, 1432, 26 + k * 52, line, { transform: 'translateX(-100%)' }));
    });
    var bars = el('div', { cls: 'hw-bars' }, [el('div', { cls: 'hw-bar hp' }, [el('span', { text: h('hp') })]), el('div', { cls: 'hw-bar st' }, [el('span', { text: h('st') })])]);
    bars.style.left = U(BARS_X); bars.style.top = U(750 - 62); bars.style.width = U(BARS_W);
    sc.appendChild(bars);
    g.col.forEach(function (i, k) {
      var it = items[i];
      sc.appendChild(hit(i, BARS_X, 750 - 72 - (k + 1) * 50, el('div', { cls: 'hw-colrow' }, [hIcon(it.icon, it.color, U(40)), el('span', { cls: 't', text: tx(it.name) + (it.text ? '  ' + it.text : '') })])));
    });
    var line = 0;
    ['move', 'attack', 'reload'].forEach(function (kind) {
      var grp = g.spd.filter(function (i) { return items[i].kind === kind; });
      if (!grp.length) return;
      var y = 750 - 10 - (++line) * 56;
      if (grp.length === 1) {
        var it = items[grp[0]];
        sc.appendChild(hit(grp[0], 1432, y, el('div', { cls: 'hw-spd' }, [el('span', { cls: 't', text: it.text, style: { color: '#fff' } }), hIcon(it.icon, it.color, U(36 * 1.2))]), { transform: 'translateX(-100%)' }));
        return;
      }
      var sum = grp.reduce(function (s, i) { return s + items[i].pct; }, 0);
      var col = kind !== 'move' ? '#ffd14d' : sum >= 0 ? '#5ae68c' : '#ff6659';
      var row = el('div', { cls: 'hw-spd', style: { position: 'absolute', left: U(1432), top: U(y), transform: 'translateX(-100%)' } }, [el('span', { cls: 't', text: (sum >= 0 ? '+' : '') + sum + '%', style: { color: col } })]);
      grp.slice().reverse().forEach(function (i) { row.appendChild(inline(hit(i, 0, 0, hIcon(items[i].icon, items[i].color, U(36 * 1.2))))); });
      sc.appendChild(row);
    });
    g.mark.forEach(function (i, k) { sc.appendChild(hit(i, zx - 220 + k * 70 - 26, 620, el('div', { style: { width: U(52), height: U(40) } }))); });
    g.over.forEach(function (i) { sc.appendChild(hit(i, zx - 40, zy - 34, el('div', { style: { width: U(80), height: U(68) } }))); });
  }

  function bigIcon(it, px) {
    var box = el('div', { cls: 'hw-big' });
    if (it.place === 'toast' || it.place === 'dmg') box.appendChild(el('span', { text: '!', style: { font: '700 ' + px * 0.7 + 'px var(--f-display)', color: it.color } }));
    else if (it.place === 'mark' || it.place === 'over') box.innerHTML = '<svg width="' + px * 0.8 + '" height="' + px * 0.8 + '" viewBox="0 0 24 24"><circle cx="12" cy="12" r="8" fill="none" stroke="' + it.color + '" stroke-width="2.6"/></svg>';
    else if (it.place === 'feed') feedIcons(it, px * 0.45 + 'px').slice(-1).forEach(function (n) { box.appendChild(n); });
    else box.appendChild(hIcon(it.icon, it.color, px * 0.7 + 'px'));
    return box;
  }
  function vanillaUses(it) {
    var v = H.vanilla[it.icon] || { buffs: [], perks: [], ui: 0, other: [] };
    var box = el('div', { cls: 'hw-uses' });
    function li(t, color, title) { return el('li', title ? { title: title } : {}, [color ? el('i', { style: { background: color } }) : null, document.createTextNode(t)]); }
    var nm = function (o) { return L === 'en' ? o.en : o.ru; };
    if (v.buffs.length) {
      box.appendChild(el('div', { cls: 'lbl risk', text: h('risk') }));
      box.appendChild(el('ul', {}, v.buffs.map(function (b) { return li(nm(b), b.color, b.id); })));
    }
    if (v.perks.length) {
      box.appendChild(el('div', { cls: 'lbl', text: h('perks') }));
      box.appendChild(el('ul', {}, v.perks.map(function (p) { return li(nm(p)); })));
    }
    var extra = [];
    if (v.ui) extra.push(h('ui') + ': ' + v.ui);
    if (extra.length) box.appendChild(el('div', { cls: 'lbl', text: h('also') + ': ' + extra.join('; ') + '.' }));
    if (!v.buffs.length && !v.perks.length && !extra.length) box.appendChild(el('div', { cls: 'lbl', text: h('nowhere') }));
    return box;
  }
  function modUses(it) {
    var lists = [['general', h('general'), H.general]].concat(M.order.map(function (k) { return [k, tx(M.classes[k].name), H.classes[k] || []]; })).concat([['zombie', h('zombie'), H.zombie]]);
    var out = [], seen = {};
    lists.forEach(function (l) {
      l[2].forEach(function (o) {
        if (o === it || o.icon !== it.icon) return;
        var key = l[1] + tx(o.name);
        if (!seen[key]) { seen[key] = 1; out.push({ n: tx(o.name), tab: l[1], color: o.color }); }
      });
    });
    return out;
  }

  var selI = -1;
  function select(i) {
    selI = i;
    hroot.querySelectorAll('.hw-hit, .hw-card').forEach(function (e) { e.classList.toggle('sel', +e.dataset.i === i); });
    var d = hroot.querySelector('.hw-detail'); d.innerHTML = '';
    var it = items[i];
    if (!it) return;
    var head = el('div', { cls: 'hw-dhead' }, [bigIcon(it, 48), el('div', {}, [el('h4', { text: tx(it.name) + (it.variant ? ' · ' + tx(it.variant) : '') }), L !== 'en' && it.name.en ? el('div', { cls: 'hw-en', text: it.name.en }) : null])]);
    d.appendChild(head);
    var chips = el('div', { cls: 'hw-chips' }, [el('span', { cls: 'hw-chip', text: h('places')[it.place] })]);
    if (it.skill) chips.appendChild(el('span', { cls: 'hw-chip k', text: it.skill.id === 'Base' ? h('trait') : h('skill') + ' ' + it.skill.id }));
    var kind = iconKind(it);
    if (kind) chips.appendChild(el('span', { cls: 'hw-chip ' + kind, text: kind === 'van' ? h('vanIcon') : h('ownIcon') }));
    d.appendChild(chips);
    var dl = el('dl');
    function row(label, node) { dl.appendChild(el('div', {}, [el('dt', { text: label }), node])); }
    var src = srcOf(it);
    if (src) row(h('from'), el('dd', { cls: 'hw-src', text: src }));
    if (it.text) row(h('example'), el('dd', {}, [el('span', { cls: 'hw-sample', text: it.text, style: { color: it.color } })]));
    row(h('what'), el('dd', { text: tx(it.what) }));
    row(h('when'), el('dd', { text: tx(it.when) }));
    if (it.d && tx(it.d)) row(h('game'), el('dd', { cls: 'hw-game', text: tx(it.d) }));
    if (kind === 'van') row(h('vanUses'), vanillaUses(it));
    var mine = kind ? modUses(it) : [];
    if (mine.length) row(h('modUses'), el('div', { cls: 'hw-uses' }, [el('ul', {}, mine.map(function (o) { return el('li', {}, [el('i', { style: { background: o.color } }), document.createTextNode(o.n + ' · ' + o.tab)]); }))]));
    d.appendChild(dl);
  }
  function drawCards(tabName) {
    var c = hroot.querySelector('.hw-cards'); c.innerHTML = '';
    items.forEach(function (it, i) {
      var kind = iconKind(it);
      var b = el('button', { cls: 'hw-card', 'data-i': i, type: 'button' }, [
        bigIcon(it, 34),
        el('div', { style: { minWidth: '0' } }, [
          el('div', { cls: 'nm' }, [document.createTextNode(tx(it.name) + (it.variant ? ' · ' + tx(it.variant) : '')), kind ? el('span', { cls: 'tag ' + kind, text: kind === 'van' ? h('van') : h('own') }) : null]),
          el('div', { cls: 'wh', text: tx(it.what) }),
          el('div', { cls: 'sr', text: srcOf(it) }),
          el('div', { cls: 'pl', text: h('places')[it.place] })
        ])
      ]);
      b.addEventListener('click', function () { select(i); hroot.querySelector('.hw-scene').scrollIntoView({ behavior: 'smooth', block: 'nearest' }); });
      c.appendChild(b);
    });
    hroot.querySelector('.hw-listtitle').textContent = h('all') + ': ' + tabName;
  }
  function renderH() {
    var tabs = tabsList();
    if (!tabs.some(function (t) { return t.id === hs.tab; })) hs.tab = 'knives';
    items = itemsOf(hs.tab);
    hroot.querySelectorAll('.hw-tab').forEach(function (b) { b.setAttribute('aria-selected', b.dataset.id === hs.tab ? 'true' : 'false'); });
    drawScene();
    drawCards(tabs.filter(function (t) { return t.id === hs.tab; })[0].name);
    var first = -1;
    items.forEach(function (it, i) { if (first < 0 && (it.place === 'gR' || it.place === 'gL')) first = i; });
    select(first >= 0 ? first : 0);
  }
  function mountHud(root) {
    hroot = root;
    if (!document.getElementById('hw-sprite')) {
      var sp = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
      sp.setAttribute('id', 'hw-sprite'); sp.setAttribute('width', '0'); sp.setAttribute('height', '0'); sp.setAttribute('aria-hidden', 'true');
      sp.style.position = 'absolute'; sp.innerHTML = '<defs>' + H.defs + '</defs>';
      document.body.appendChild(sp);
    }
    root.innerHTML = '';
    var nav = el('div', { cls: 'hw-tabs', role: 'tablist' });
    tabsList().forEach(function (t, k) {
      if (k === 1 || t.id === 'zombie') nav.appendChild(el('span', { cls: 'hw-sep' }));
      var b = el('button', { type: 'button', cls: 'hw-tab', role: 'tab', 'data-id': t.id, text: t.name });
      b.addEventListener('click', function () { hs.tab = t.id; keep('aposRefHud', hs); renderH(); });
      nav.appendChild(b);
    });
    root.appendChild(nav);
    var legend = el('div', { cls: 'hw-legend' }, h('legend').map(function (l) { return el('span', { style: { '--c': l[0] }, text: l[1] }); }));
    root.appendChild(el('div', { cls: 'hw-main' }, [
      el('div', {}, [el('div', { cls: 'hw-scene', role: 'img' }), legend, el('p', { cls: 'hw-note', text: h('note') })]),
      el('aside', { cls: 'hw-detail', 'aria-live': 'polite' })
    ]));
    root.appendChild(el('h4', { cls: 'hw-listtitle' }));
    root.appendChild(el('div', { cls: 'hw-cards' }));
    renderH();
  }

  window.AposWidgets = {
    mount: function (app, lang) {
      L = UI[lang] ? lang : 'en';
      var m = app.querySelector('#mastery-app'); if (m) mountMastery(m);
      var hh = app.querySelector('#hud-app'); if (hh) mountHud(hh);
    }
  };
})();

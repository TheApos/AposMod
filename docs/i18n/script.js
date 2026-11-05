(function () {
  var LANGS = ['en', 'ru', 'uk'], KEY = 'aposRefLang';
  var app = document.getElementById('app');

  // heading icons: first keyword (any of the three languages) that matches the heading text
  var map = [
    [/ерідій|эридий|eridium/i, 'gem'], [/значки|модифікатор|модификатор|modifier|еліт|элит|elite/i, 'star'],
    [/майстерн|як працює|мастерство оружия|как работает|weapon mastery|how it works/i, 'cap'],
    [/клинк|ніж|нож|мачете|machete|knives|blade/i, 'knife'], [/кувалд|sledge/i, 'sledge'],
    [/дубин|біт|бити|бит|\bclubs?\b|\bbats?\b|pummel|grand slam/i, 'club'], [/спис|копь|копья|spear/i, 'spear'],
    [/кастет|knuckle|brawler/i, 'fist'], [/жезл|baton|струм|электр|(^|\s)ток(\s|$)|shock|chain lightning/i, 'bolt'],
    [/пістолет|пистолет|pistol|пп\b|smg/i, 'pistol'], [/дробовик|shotgun/i, 'shotgun'], [/гвинтів|винтов|rifle/i, 'rifle'],
    [/автомат|кулемет|пулемет|machine gun/i, 'mg'], [/арбалет|crossbow/i, 'crossbow'], [/лук|стріл|стрел|\bbows?\b|arrow/i, 'bow'],
    [/гранат|grenade|ракет|rocket|вибух|взрыв|explos|молотов|molotov/i, 'bomb'], [/робот|robot/i, 'robot'],
    [/вогонь|огонь|\bfire\b|стихі|стихи|element/i, 'fire'], [/саморобн|самодел|pipe weapon|junk/i, 'gear'],
    [/афікс|аффикс|affix/i, 'dice'], [/легенд|legend/i, 'crown'], [/energy shield|мод|\bmods?\b/i, 'shield'],
    [/сет|гардероб|wardrobe|\bsets?\b|outfit/i, 'shirt'], [/стелс|stealth/i, 'eye'],
    [/перк|perk|атрибут|attribute|perception|strength|fortitude|agility|intellect|skull crusher|mastery/i, 'book'],
    [/гнізд|гнезд|гнёзд|nest/i, 'feather'], [/цифр|damage number|aposdmg/i, 'target'],
    [/aposaffix|aposelite|aposbow|налашт|настрой|систем|system|setting/i, 'terminal'],
    [/баф|buff/i, 'skull'], [/запуск|launch/i, 'play'], [/ближній бій|ближний бой|melee/i, 'knife'], [/дальній бій|дальний бой|ranged/i, 'rifle']
  ];
  var tileMap = map.concat([
    [/здоров|лікув|леч|health|heal|hp\b/i, 'heart'], [/швидк|скорост|біг|бег|рух|speed|\brun|move/i, 'run'],
    [/торгов|trader|ціна|цен|price|знижк|скидк|discount|bartering/i, 'coin'], [/секунд|second|час|время|перезаряд|reload/i, 'clock'],
    [/відмичк|отмычк|lock|замок|сейф|safe/i, 'key'], [/руд|ore|копа|добич|добыч|mining|miner/i, 'pick'],
    [/шкод|урон|damage/i, 'target']
  ]);

  function icon(name) {
    var ns = 'http://www.w3.org/2000/svg';
    var s = document.createElementNS(ns, 'svg'); s.setAttribute('class', 'hi'); s.setAttribute('aria-hidden', 'true');
    var u = document.createElementNS(ns, 'use'); u.setAttribute('href', '#i-' + name); s.appendChild(u); return s;
  }
  function pick(text, fallback) {
    for (var i = 0; i < tileMap.length; i++) if (tileMap[i][0].test(text)) return tileMap[i][1];
    return fallback;
  }

  function setup() {
    // search
    var q = document.getElementById('q'), none = document.getElementById('none');
    var cards = Array.prototype.slice.call(app.querySelectorAll('.card, .dmg'));
    var secs = Array.prototype.slice.call(app.querySelectorAll('[data-sec]'));
    function apply() {
      var term = q.value.trim().toLowerCase();
      document.body.classList.toggle('searching', !!term);
      var shown = 0;
      cards.forEach(function (c) {
        var hit = !term || c.textContent.toLowerCase().indexOf(term) !== -1;
        c.classList.toggle('hide', !hit);
        if (hit) shown++;
      });
      secs.forEach(function (s) {
        var any = s.querySelector('.card:not(.hide), .dmg:not(.hide)');
        s.classList.toggle('hide', !!term && !any);
      });
      if (none) none.classList.toggle('hide', shown > 0);
    }
    if (q) q.addEventListener('input', apply);
    document.body.classList.remove('searching');

    app.querySelectorAll('h2, .card h3').forEach(function (h) {
      var t = h.textContent;
      for (var i = 0; i < map.length; i++) if (map[i][0].test(t)) { h.insertBefore(icon(map[i][1]), h.firstChild); return; }
    });

    // lists of named things (legendaries, sets, perks, affix stats) -> tile grid; full text on hover / tap
    app.querySelectorAll('.card > ul').forEach(function (ul) {
      var items = Array.prototype.slice.call(ul.children);
      if (items.length < 4) return;
      var named = items.every(function (li) { var f = li.firstElementChild; return f && f.tagName === 'B' && li.textContent.trim().indexOf(f.textContent.trim()) === 0; });
      if (!named) return;
      var card = ul.closest('.card'), headUse = card.querySelector('h3 .hi use');
      var fallback = headUse ? headUse.getAttribute('href').slice(3) : 'star';
      var grid = document.createElement('div'); grid.className = 'tiles';
      items.forEach(function (li) {
        var label = li.firstElementChild.textContent.replace(/[:\s]+$/, '');
        var parts = label.split(/\s+[—→]\s+/), main = parts[parts.length - 1], sub = parts.length > 1 ? parts.slice(0, -1).join(' — ') : '';
        var tile = document.createElement('div'); tile.className = 'tile'; tile.tabIndex = 0;
        tile.appendChild(icon(pick(li.textContent, fallback)));
        var m = document.createElement('span'); m.className = 't-main'; m.textContent = main; tile.appendChild(m);
        if (sub) { var s = document.createElement('span'); s.className = 't-sub'; s.textContent = sub; tile.appendChild(s); }
        var code = li.querySelector('code');
        if (code && code.textContent.length <= 14) { var v = document.createElement('span'); v.className = 't-val'; v.textContent = code.textContent; tile.appendChild(v); }
        var pop = document.createElement('span'); pop.className = 'pop'; pop.setAttribute('role', 'tooltip');
        Array.prototype.forEach.call(li.childNodes, function (n) { pop.appendChild(n.cloneNode(true)); });
        tile.appendChild(pop);
        grid.appendChild(tile);
      });
      ul.parentNode.replaceChild(grid, ul);
    });

    // long bullets: keep the first sentence, the rest goes into a hover / tap hint (still searchable)
    var LIMIT = 80;
    app.querySelectorAll('.card li').forEach(function (li) {
      if (li.textContent.length <= LIMIT || li.querySelector('ul, .mi, table')) return;
      var walker = document.createTreeWalker(li, NodeFilter.SHOW_TEXT), node, seen = 0, cut = null;
      while ((node = walker.nextNode())) {
        var re = /[.;!?](\s)/g, r, txt = node.nodeValue;
        while ((r = re.exec(txt))) {
          if (seen + r.index >= 25 && node.parentNode === li) { cut = { node: node, at: r.index + 1 }; break; }
        }
        if (cut) break;
        seen += txt.length;
      }
      if (!cut || li.textContent.length - (seen + cut.at) < 20) return;
      var rest = cut.node.splitText(cut.at);
      var more = document.createElement('span'); more.className = 'more';
      while (rest) { var next = rest.nextSibling; more.appendChild(rest); rest = next; }
      li.appendChild(more);
      var dots = document.createElement('span'); dots.className = 'dots'; dots.textContent = 'i'; li.appendChild(dots);
      var pop = document.createElement('span'); pop.className = 'pop'; pop.setAttribute('role', 'tooltip');
      Array.prototype.forEach.call(li.childNodes, function (n) { if (n !== dots) pop.appendChild(n.cloneNode(true)); });
      pop.querySelectorAll('.more').forEach(function (x) { x.className = ''; });
      li.appendChild(pop);
      li.classList.add('tipped'); li.tabIndex = 0;
    });

    // keep tile hints inside the screen: shift sideways when they would cross an edge
    function fit(e) {
      var pop = e.currentTarget.querySelector('.pop'); if (!pop) return;
      pop.style.transform = 'translateX(-50%)';
      requestAnimationFrame(function () {
        var r = pop.getBoundingClientRect(), dx = 0, pad = 8;
        if (r.right > window.innerWidth - pad) dx = window.innerWidth - pad - r.right;
        if (r.left + dx < pad) dx = pad - r.left;
        pop.style.transform = 'translateX(calc(-50% + ' + Math.round(dx) + 'px))';
      });
    }
    app.querySelectorAll('.tile, .mil').forEach(function (t) { t.addEventListener('mouseenter', fit); t.addEventListener('focus', fit); });

    // language switch in the header line
    var meta = app.querySelector('.meta');
    if (meta) {
      var box = document.createElement('span'); box.className = 'langs'; box.setAttribute('role', 'group'); box.setAttribute('aria-label', 'Language');
      LANGS.forEach(function (l) {
        var b = document.createElement('button'); b.type = 'button'; b.textContent = l.toUpperCase();
        b.setAttribute('aria-pressed', l === current ? 'true' : 'false');
        b.addEventListener('click', function () { show(l); });
        box.appendChild(b);
      });
      meta.appendChild(box);
    }
  }

  var current = null;
  function show(lang) {
    if (LANGS.indexOf(lang) < 0) lang = 'en';
    if (lang === current) return;
    var tpl = document.getElementById('lang-' + lang);
    current = lang;
    app.innerHTML = '';
    app.appendChild(tpl.content.cloneNode(true));
    document.documentElement.lang = lang;
    try { localStorage.setItem(KEY, lang); } catch (e) { }
    setup();
  }

  var saved = null;
  try { saved = localStorage.getItem(KEY); } catch (e) { }
  show(saved || 'en');
})();

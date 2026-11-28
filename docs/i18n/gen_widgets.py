# Builds docs/i18n/widgets.js: the data + code of the two interactive sections of the Reference guide
#   - Weapon Mastery 2.0 trees (layout and Ukrainian texts from mastery_tree.json, EN / RU texts from the game)
#   - the HUD status atlas (hud_data.json from docs/hud/gen_hud_doc.py)
# in English, Russian and Ukrainian. Texts that exist only in Ukrainian are translated in tr/*.json.
# Run: python docs/i18n/gen_widgets.py   (then & docs/i18n/build_reference.ps1)
import csv, json, os, re

HERE = os.path.dirname(os.path.abspath(__file__))
MOD = os.path.join(HERE, '..', '..')
LANGS = ('en', 'ru', 'uk')

def load(name, default=None):
    p = os.path.join(HERE, name)
    if not os.path.exists(p):
        if default is not None:
            return default
        raise FileNotFoundError(p)
    return json.load(open(p, encoding='utf-8'))

tree = load('mastery_tree.json')
hud = load('hud_data.json')
tr_ua = load('tr/mastery_tr.json', {})      # Ukrainian-only texts -> {en, ru}
tr_uk = load('tr/mastery_uk.json', {})      # game Russian -> Ukrainian (stale prototype skills, class traits)
tr_hud = load('tr/hud_tr.json', {})         # atlas texts -> {en: {...}, ru: {...}}
missing = []

# ===== game texts (EN / RU) =====
loc = {}
with open(os.path.join(MOD, 'Config', 'Localization.csv'), encoding='utf-8-sig', newline='') as f:
    for row in csv.DictReader(f):
        loc[row['Key']] = {'en': row.get('english') or '', 'ru': row.get('russian') or ''}
src = open(os.path.join(MOD, 'Source', 'gen_loc.ps1'), encoding='utf-8-sig').read()

def q(s):
    return [(m.group(1) if m.group(1) is not None else m.group(2)).replace("''", "'") for m in re.finditer(r"'((?:[^']|'')*)'|\"([^\"]*)\"", s)]

game = {}
for m in re.finditer(r"^\s*(\w+)_(Base|[ABH]\d)\s*=\s*(.*)$", src, re.M):
    v = q(m.group(3))
    game[(m.group(1), m.group(2))] = {'en': v[0], 'ru': v[1], 'den': v[2] if len(v) > 2 else '', 'dru': v[3] if len(v) > 3 else '',
                                      'ren': v[4::2], 'rru': v[5::2]}

KEY = {'pistols': 'Pistols', 'shotguns': 'Shotguns', 'rifles': 'Rifles', 'mgs': 'MG', 'bows': 'Bows', 'xbows': 'Crossbows',
       'knives': 'Blades', 'clubs': 'Clubs', 'sledges': 'Sledges', 'spears': 'Spears', 'knuckles': 'Knuckles', 'batons': 'Batons'}
CLASS_UK = {'pistols': 'Пістолети й ПП', 'shotguns': 'Дробовики', 'rifles': 'Гвинтівки', 'mgs': 'Автомати', 'bows': 'Луки',
            'xbows': 'Арбалети', 'knives': 'Ножі й мачете', 'clubs': 'Дубинки й бити', 'sledges': 'Кувалди', 'spears': 'Списи',
            'knuckles': 'Кастети', 'batons': 'Кийки'}
BRANCH_UK = {('pistols', 'A'): 'Мисливець за головами', ('pistols', 'B'): 'Свинцевий шквал', ('shotguns', 'A'): 'Кулі',
             ('shotguns', 'B'): 'Картеч', ('rifles', 'A'): 'Постріл', ('rifles', 'B'): 'Рикошет', ('mgs', 'A'): 'Подавлення',
             ('mgs', 'B'): 'Розкрутка', ('bows', 'A'): 'Уламки', ('bows', 'B'): 'Стріла', ('xbows', 'A'): 'Мітка',
             ('xbows', 'B'): 'Липкий заряд', ('knives', 'A'): 'Кровопивця', ('knives', 'B'): 'Убивця', ('clubs', 'A'): 'Хоумран',
             ('clubs', 'B'): 'Лють', ('sledges', 'A'): 'Землетрус', ('sledges', 'B'): 'Удар', ('spears', 'A'): 'Таран',
             ('spears', 'B'): 'Фехтування', ('knuckles', 'A'): 'Контрудар', ('knuckles', 'B'): 'Шквал', ('batons', 'A'): 'Буря',
             ('batons', 'B'): 'Перевантаження'}
ATTR = {'Ловкість': ('Agility', 'Ловкость'), 'Сила': ('Strength', 'Сила'), 'Сприйняття': ('Perception', 'Восприятие'),
        'Стійкість': ('Fortitude', 'Стойкость'), 'Інтелект': ('Intellect', 'Интеллект')}

def ua3(key, uk):
    t = tr_ua.get(key)
    if not t:
        missing.append(key)
        return {'en': uk, 'ru': uk, 'uk': uk}
    return {'en': t['en'], 'ru': t['ru'], 'uk': uk}

# ===== mastery =====
M = {'order': tree['order'], 'icons': tree['icons'], 'classes': {}}
for k in tree['order']:
    c = tree['classes'][k]; gk = KEY[k]
    cls = {'key': gk, 'name': {'en': loc['aposMasteryClass' + gk]['en'], 'ru': loc['aposMasteryClass' + gk]['ru'], 'uk': CLASS_UK[k]}}
    gb = game[(gk, 'Base')]; ub = tr_uk.get('base|' + k)
    if not ub: missing.append('base|' + k)
    cls['base'] = {'icon': c['base']['icon'],
                   'name': {'en': gb['en'], 'ru': gb['ru'], 'uk': (ub or {}).get('name', c['base']['name'])},
                   'desc': {'en': gb['den'], 'ru': gb['dru'], 'uk': (ub or {}).get('desc', c['base']['text'])}}
    cls['br'] = {b: {'name': {'en': loc['aposMB_%s_%s' % (gk, b)]['en'], 'ru': loc['aposMB_%s_%s' % (gk, b)]['ru'], 'uk': BRANCH_UK[(k, b)]},
                     'idea': ua3('idea|%s|%s' % (k, b), c['br'][b]['idea'])} for b in ('A', 'B')}
    a = c.get('attr')
    cls['attr'] = {'en': ATTR[a['name']][0], 'ru': ATTR[a['name']][1], 'uk': a['name']} if a else None
    cls['external'] = {eid: {'cls': e['cls'], 'icon': e['icon'], 'name': ua3('ext|%s|%s' % (k, eid), e['name'])} for eid, e in c['external'].items()}
    skills = []
    for s in c['skills']:
        g = game[(gk, s['id'])]
        st = tr_uk.get('skill|%s|%s' % (k, s['id']))
        uk = st or {'name': s['name'], 'desc': s['desc'], 'ranks': s['ranks']}
        o = {f: s[f] for f in ('id', 'br', 'tier', 'col', 'span', 'row', 'need', 'max', 'icon', 'notable', 'cap', 'big') if f in s}
        o['max'] = len(g['ren']) or s['max']
        o['name'] = {'en': g['en'], 'ru': g['ru'], 'uk': uk['name']}
        o['desc'] = {'en': g['den'], 'ru': g['dru'], 'uk': uk['desc']}
        o['ranks'] = {'en': g['ren'], 'ru': g['rru'], 'uk': uk['ranks'] if len(uk['ranks']) == len(g['ren']) else g['rru']}
        o['syn'] = [{'t': y['t'], 'w': y.get('w'), 'x': ua3('syn|%s|%s|%d' % (k, s['id'], i), y['x'])} for i, y in enumerate(s.get('syn', []))]
        if s.get('breaks'):
            o['breaks'] = [{'ids': b['ids'], 'lvl': b['lvl'], 'why': ua3('break|%s|%s|%d' % (k, s['id'], i), b['why'])} for i, b in enumerate(s['breaks'])]
        skills.append(o)
    cls['skills'] = skills
    cls['presets'] = [{'n': ua3('preset|%s|%d' % (k, i), p['n']), 'r': p['r']} for i, p in enumerate(c['presets'])]
    M['classes'][k] = cls

# ===== HUD atlas =====
skill_by_key = {}
for k in tree['order']:
    for s in M['classes'][k]['skills']:
        skill_by_key['%s_%s' % (KEY[k], s['id'])] = (k, s)
    skill_by_key['%s_Base' % KEY[k]] = (k, None)

def hud_item(it):
    o = {f: it[f] for f in ('place', 'icon', 'color', 'text', 'value', 'seg', 'full', 'kind', 'pct', 'mult') if f in it}
    o['k'] = it['k']
    key = it.get('key') or ''
    t = tr_hud.get(it['k'])
    if not t: missing.append('hud|' + it['k'])
    for f in ('what', 'when', 'variant', 'sub'):
        if f in it and it[f]:
            o[f] = {'uk': it[f], 'en': (t or {}).get('en', {}).get(f, it[f]), 'ru': (t or {}).get('ru', {}).get(f, it[f])}
    # name and game description: the mastery data for skills and traits, the game for the mod's buffs; an item with a
    # name of its own (an override, e.g. "Bleeding" shown for the knife trait) keeps it
    if key in skill_by_key:
        k, s = skill_by_key[key]
        c = M['classes'][k]
        srcs = s or c['base']
        o['skill'] = {'cls': k, 'id': s['id'] if s else 'Base', 'br': s['br'] if s else None}
        if it.get('ru') and it['ru'] != srcs['name']['ru']:
            o['name'] = {'en': it.get('en') or it['ru'], 'ru': it['ru'], 'uk': it['ru']}
        else:
            o['name'] = dict(srcs['name'])
            o['d'] = dict(srcs['desc'])
    elif key.startswith('buffApos') and key + 'Name' in loc:
        o['name'] = {'en': loc[key + 'Name']['en'], 'ru': loc[key + 'Name']['ru'], 'uk': it.get('ru') or loc[key + 'Name']['ru']}
        if key + 'Desc' in loc:
            clean = lambda s: re.sub(r'\[[0-9A-Fa-f]{6}\]|\[-\]', '', s).replace('\n', ' · ')
            o['d'] = {'en': clean(loc[key + 'Desc']['en']), 'ru': clean(loc[key + 'Desc']['ru'])}
            o['d']['uk'] = o['d']['ru']
    else:
        o['name'] = {'en': it.get('en') or it.get('ru', ''), 'ru': it.get('ru', ''), 'uk': it.get('ru', '')}
    if it.get('src_manual') or not o.get('skill'):
        s = it.get('src', '')
        o['src'] = {'uk': s, 'en': (t or {}).get('en', {}).get('src', s), 'ru': (t or {}).get('ru', {}).get('src', s)}
    return o

defs = re.search(r'<defs>(.*?)</defs>', open(os.path.join(MOD, 'docs', 'hud', 'hud_template.html'), encoding='utf-8').read(), re.S).group(1)
H = {'png': hud['png'], 'vanilla': hud['vanilla'], 'defs': defs.replace('id="i-', 'id="h-').replace('href="#i-', 'href="#h-')}
H['general'] = [hud_item(it) for it in hud['general']]
H['zombie'] = [hud_item(it) for it in hud['zombie']]
H['feed'] = [hud_item(it) for it in hud['feed']]
H['classes'] = {}
ATLAS2TREE = {'Pistols': 'pistols', 'Shotguns': 'shotguns', 'Rifles': 'rifles', 'MG': 'mgs', 'Bows': 'bows', 'Crossbows': 'xbows',
              'Blades': 'knives', 'Clubs': 'clubs', 'Sledges': 'sledges', 'Spears': 'spears', 'Knuckles': 'knuckles', 'Batons': 'batons'}
for c in hud['classes']:
    H['classes'][ATLAS2TREE[c['id']]] = [hud_item(it) for it in c['items']]

data = {'mastery': M, 'hud': H}
code = open(os.path.join(HERE, 'widgets_src.js'), encoding='utf-8').read()
out = '// Generated by docs/i18n/gen_widgets.py - edit widgets_src.js and the data sources, not this file\nwindow.APOS_DATA = ' + \
      json.dumps(data, ensure_ascii=False, separators=(',', ':')) + ';\n' + code
open(os.path.join(HERE, 'widgets.js'), 'w', encoding='utf-8').write(out)
print('widgets.js: %d chars' % len(out))
if missing:
    print('MISSING translations: %d (first: %s)' % (len(missing), ', '.join(missing[:6])))

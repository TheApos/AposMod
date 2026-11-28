// Pulls the mastery trees out of docs/MasteryPrototype.html (layout, Ukrainian texts, synergies, builds) into docs/i18n/mastery_tree.json.
// Run: node docs/i18n/extract_mastery.js docs/MasteryPrototype.html docs/i18n/mastery_tree.json
const fs = require('fs');
const vm = require('vm');
const src = fs.readFileSync(process.argv[2], 'utf8');
const s0 = src.indexOf('var P = {');
const s1 = src.indexOf('var ORDER = [');
const end = src.indexOf('\n', s1);
let code = src.slice(s0, end);
code += '\n;__out = { CLASSES: CLASSES, ORDER: ORDER, P: P, SYN: SYN };';
const ctx = { __out: null, document: {}, window: {}, Math, String, JSON, Object, Array, console };
vm.createContext(ctx);
vm.runInContext(code, ctx);
const o = ctx.__out;
const out = { order: o.ORDER, icons: o.P, synTypes: o.SYN, classes: {} };
for (const k of o.ORDER) {
  const c = o.CLASSES[k];
  const ext = c.external || {};
  out.classes[k] = {
    tab: c.tab, title: c.title, sub: c.sub,
    base: c.base ? { name: c.base.name, en: c.base.en, icon: c.base.icon, text: c.base.text } : null,
    attr: c.attr ? { name: c.attr.name, skill: c.attr.skill } : null,
    br: c.br,
    external: Object.fromEntries(Object.entries(ext).map(([id, e]) => [id, { cls: e.cls, icon: e.icon, name: e.name }])),
    skills: c.skills.map(s => {
      const r = {};
      for (const f of ['id', 'br', 'tier', 'col', 'span', 'row', 'need', 'max', 'icon', 'notable', 'cap', 'big', 'name', 'en', 'desc', 'ranks', 'thr', 'syn', 'breaks'])
        if (s[f] !== undefined) r[f] = s[f];
      return r;
    }),
    presets: (c.presets || []).map(p => ({ n: p.n, r: p.r })),
  };
}
fs.writeFileSync(process.argv[3], JSON.stringify(out, null, 1));
let skills = 0, syn = 0, synChars = 0;
for (const k of o.ORDER) for (const s of out.classes[k].skills) { skills++; for (const y of (s.syn || [])) { syn++; synChars += y.x.length; } }
console.log('classes', o.ORDER.length, 'skills', skills, 'syn', syn, 'synChars', synChars);

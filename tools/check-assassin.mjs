// Browser check: Assassin class. Picks Assassin on the class-select screen (5 cards), opens its skill tree, then casts
// every active skill in the live simulation against dummy slimes and verifies the effect (blink/backstab/poison/slow/...).
// Fails on any page error. Screenshots -> tools/screenshots/assassin/. Needs the dev server (port 5173);
// starts one itself when nothing answers.
import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
import { spawn } from 'node:child_process';

const URL = process.env.ASSASSIN_CHECK_URL || 'http://127.0.0.1:5173';
const out = 'tools/screenshots/assassin';
mkdirSync(out, { recursive: true });
const wait = (ms) => new Promise((r) => setTimeout(r, ms));

let server = null;
try { await fetch(URL); } catch {
  server = spawn('npx', ['serve', '.', '-l', '5173'], { shell: true, stdio: 'ignore' });
  for (let i = 0; i < 40; i++) { try { await fetch(URL); break; } catch { await wait(500); } }
}

const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
const errors = [];
try {
  const page = await browser.newPage();
  page.on('pageerror', (e) => errors.push(String(e)));
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  await page.goto(URL);
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await wait(300);
  await page.screenshot({ path: `${out}/class-select-5-cards.png` });
  for (let i = 0; i < 4; i++) await page.keyboard.press('ArrowRight', { delay: 60 });
  await wait(300);
  await page.screenshot({ path: `${out}/class-select-assassin-selected.png` });
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');

  const setup = await page.evaluate(async () => {
    const { SKILL_DEFINITIONS } = await import('/src/data/skill-definitions.js');
    const { CLASS_DEFINITIONS } = await import('/src/data/class-definitions.js');
    const g = window.__zenonia, s = g.session, p = s.player;
    if (p.classId !== 'assassin') throw Error(`expected assassin, got ${p.classId}`);
    s.enterMap('frostHollow'); g.banner.lifeMs = 0;
    const ids = Object.keys(SKILL_DEFINITIONS).filter((id) => CLASS_DEFINITIONS.assassin.branches.includes(SKILL_DEFINITIONS[id].branch));
    p.level = 30; p.recomputeStats();
    for (const id of ids) p.skills[id] = 3;
    p.recomputeStats(); p.hp = p.maxHp; p.mp = p.maxMp;
    const frames = g.registry.heroes.assassin.down.idle;
    return { ids, actives: ids.filter((id) => SKILL_DEFINITIONS[id].type === 'active'),
      idleFrames: frames.length, icons: ids.every((id) => g.registry.icons[id]) };
  });
  if (!setup.idleFrames || !setup.icons || setup.ids.length !== 15) throw Error(`sprites/icons missing ${JSON.stringify(setup)}`);
  await page.keyboard.press('Tab', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'skills');
  await wait(300);
  await page.screenshot({ path: `${out}/skill-tree.png` });
  await page.keyboard.press('Escape', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');

  const results = [];
  for (const id of setup.actives) {
    const before = await page.evaluate(async (id) => {
      const { tryCastSkill } = await import('/src/systems/skill-cast-system.js');
      const { Monster } = await import('/src/entities/monster.js');
      const g = window.__zenonia, w = g.session.world, p = w.player;
      w.hitRoll = () => 0.5; w.texts = []; w.effects = []; w.projectiles = []; w.skillPulses = [];
      p.x = 160; p.y = 224; p.dir = 'right'; p.cooldowns = {}; p.mp = p.maxMp; p.hp = p.maxHp;
      p.shield = null; p.haste = null; p.stealth = null; p.poisonBlade = null; p.invulnTimer = 0; p.dead = false;
      g.session.camera.snapTo(p.x + 30, p.y - 25);
      w.monsters = [[185, 222], [205, 232], [225, 224], [175, 240]].map(([x, y]) => {
        const m = new Monster('frostSlime', x, y); m.hp = m.maxHp = 5000; m.animTimer = 0; m.dir = 'right'; return m;
      });
      const res = tryCastSkill(w, id);
      if (!res.ok) throw Error(`cast ${id}: ${res.reason}`);
      return { foe: 20000, px: p.x };
    }, id);
    await wait(id === 'venomCloud' ? 1600 : id === 'shadowClone' ? 900 : 700);
    await page.screenshot({ path: `${out}/cast-${id}.png` });
    const after = await page.evaluate(() => {
      const w = window.__zenonia.session.world, p = w.player, ms = w.monsters;
      return { foe: ms.reduce((a, m) => a + m.hp, 0), px: p.x, haste: p.haste?.mult ?? 1, stealth: p.stealth?.evasion ?? 0,
        blade: p.poisonBlade?.charges ?? 0, slowed: ms.some((m) => m.status?.slow), blind: ms.some((m) => m.status?.blind),
        poisoned: ms.some((m) => m.status?.poison), marked: ms.some((m) => m.status?.mark),
        zone: w.effects.some((e) => e.kind === 'venomZone'), clone: w.effects.some((e) => e.kind === 'shadowClone') };
    });
    results.push({ id, before, after });
  }
  const res = Object.fromEntries(results.map((r) => [r.id, r]));
  const check = (id, ok) => { if (!ok) throw Error(`effect check failed: ${id} ${JSON.stringify(res[id])}`); };
  for (const id of ['shadowStep', 'backstab', 'phantomAssault', 'crippleStrike', 'bladeDance']) check(id, res[id].after.foe < res[id].before.foe);
  check('shadowStep', res.shadowStep.after.px !== 160);
  check('smokeBomb', res.smokeBomb.after.stealth > 0 && res.smokeBomb.after.slowed && res.smokeBomb.after.blind);
  check('poisonBlade', res.poisonBlade.after.blade > 0);
  check('venomCloud', res.venomCloud.after.zone && res.venomCloud.after.poisoned);
  check('crippleStrike', res.crippleStrike.after.slowed);
  check('deathMark', res.deathMark.after.marked);
  check('adrenaline', res.adrenaline.after.haste > 1);
  check('shadowClone', res.shadowClone.after.clone);

  // a real basic-attack combo from the live game, with Poison Blade armed and the clone out
  await page.evaluate(async () => {
    const { tryCastSkill } = await import('/src/systems/skill-cast-system.js');
    const { Monster } = await import('/src/entities/monster.js');
    const g = window.__zenonia, w = g.session.world, p = w.player;
    w.effects = []; w.texts = []; p.x = 160; p.y = 224; p.dir = 'right'; p.cooldowns = {}; p.mp = p.maxMp;
    const m = new Monster('frostSlime', 180, 224); m.hp = m.maxHp = 9000; w.monsters = [m];
    tryCastSkill(w, 'poisonBlade'); tryCastSkill(w, 'shadowClone');
  });
  await page.keyboard.down('KeyJ'); await wait(420);
  await page.screenshot({ path: `${out}/basic-combo.png` });
  await wait(500); await page.keyboard.up('KeyJ');
  const combo = await page.evaluate(() => window.__zenonia.session.world.monsters.map((m) => ({ hp: m.hp, poison: Boolean(m.status?.poison) })));
  if (!combo.length || combo[0].hp >= 9000 || !combo[0].poison) throw Error(`basic combo did not damage/poison ${JSON.stringify(combo)}`);
  if (errors.length) throw Error(`page errors:\n${errors.join('\n')}`);
  console.log(JSON.stringify({ pass: true, casted: results.length, combo }));
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
} finally {
  await browser.close();
  server?.kill();
}

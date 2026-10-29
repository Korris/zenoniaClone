// Browser check: Cleric class. Picks Cleric on the class-select screen (4 cards), opens its skill tree, then casts
// every active skill in the live simulation against dummy slimes and verifies the effect (heal/shield/haste/zone/...).
// Fails on any page error. Screenshots -> tools/screenshots/cleric/. Needs the dev server (npm start, port 5173);
// starts one itself when nothing answers.
import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
import { spawn } from 'node:child_process';

const URL = process.env.CLERIC_CHECK_URL || 'http://127.0.0.1:5173';
const out = 'tools/screenshots/cleric';
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
  await page.screenshot({ path: `${out}/class-select-4-cards.png` });
  for (let i = 0; i < 3; i++) await page.keyboard.press('ArrowRight', { delay: 60 });
  await wait(300);
  await page.screenshot({ path: `${out}/class-select-cleric-selected.png` });
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');

  const setup = await page.evaluate(async () => {
    const { SKILL_DEFINITIONS } = await import('/src/data/skill-definitions.js');
    const { CLASS_DEFINITIONS } = await import('/src/data/class-definitions.js');
    const g = window.__zenonia, s = g.session, p = s.player;
    if (p.classId !== 'cleric') throw Error(`expected cleric, got ${p.classId}`);
    s.enterMap('frostHollow'); g.banner.lifeMs = 0;
    const ids = Object.keys(SKILL_DEFINITIONS).filter((id) => CLASS_DEFINITIONS.cleric.branches.includes(SKILL_DEFINITIONS[id].branch));
    p.level = 30; p.recomputeStats();
    for (const id of ids) p.skills[id] = 3;
    p.recomputeStats(); p.hp = p.maxHp; p.mp = p.maxMp;
    const frames = g.registry.heroes.cleric.down.idle;
    return { ids, actives: ids.filter((id) => SKILL_DEFINITIONS[id].type === 'active'),
      idleFrames: frames.length, icons: ids.every((id) => g.registry.icons[id]) };
  });
  if (!setup.idleFrames || !setup.icons) throw Error(`sprites/icons missing ${JSON.stringify(setup)}`);
  await page.keyboard.press('Tab', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'skills');
  await wait(300);
  await page.screenshot({ path: `${out}/skill-tree.png` });
  await page.keyboard.press('Escape', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');

  const results = [];
  for (const id of setup.actives) {
    const r = await page.evaluate(async (id) => {
      const { tryCastSkill } = await import('/src/systems/skill-cast-system.js');
      const { Monster } = await import('/src/entities/monster.js');
      const g = window.__zenonia, w = g.session.world, p = w.player;
      w.hitRoll = () => 0.5; w.texts = []; w.effects = []; w.projectiles = []; w.skillPulses = [];
      p.x = 160; p.y = 224; p.dir = 'right'; p.cooldowns = {}; p.mp = p.maxMp; p.hp = Math.round(p.maxHp * 0.5);
      p.shield = null; p.haste = null; p.buffs = { atkMult: 1, timer: 0 }; p.invulnTimer = 0; p.dead = false;
      g.session.camera.snapTo(p.x + 30, p.y - 25);
      w.monsters = [[185, 222], [205, 232], [225, 224], [175, 240]].map(([x, y]) => {
        const m = new Monster('frostSlime', x, y); m.hp = m.maxHp = 5000; m.animTimer = 0; return m;
      });
      const before = { hp: p.hp, foe: w.monsters.reduce((a, m) => a + m.hp, 0) };
      const res = tryCastSkill(w, id);
      if (!res.ok) throw Error(`cast ${id}: ${res.reason}`);
      return before;
    }, id);
    await wait(id === 'healingCircle' ? 1500 : 650);
    await page.screenshot({ path: `${out}/cast-${id}.png` });
    const after = await page.evaluate((id) => {
      const w = window.__zenonia.session.world, p = w.player;
      return { hp: p.hp, foe: w.monsters.reduce((a, m) => a + m.hp, 0), shield: p.shield?.amount ?? 0,
        haste: p.haste?.mult ?? 1, atk: p.buffs.atkMult, zone: w.effects.some((e) => e.kind === 'healingZone'), id };
    }, id);
    results.push({ id, before: r, after });
  }
  const check = (id, ok) => { if (!ok) throw Error(`effect check failed: ${id} ${JSON.stringify(results.find((r) => r.id === id))}`); };
  const res = Object.fromEntries(results.map((r) => [r.id, r]));
  for (const id of ['holyBolt', 'smite', 'divineJudgment', 'heavenlyRay', 'angelsDescent']) check(id, res[id].after.foe < res[id].before.foe);
  for (const id of ['heal', 'healingCircle', 'purify']) check(id, res[id].after.hp > res[id].before.hp);
  check('healingCircle', res.healingCircle.after.zone);
  check('sacredShield', res.sacredShield.after.shield > 0);
  check('hasteHymn', res.hasteHymn.after.haste > 1);
  check('blessingOfMight', res.blessingOfMight.after.atk > 1);

  // a real basic-attack combo from the live game
  await page.evaluate(() => { const w = window.__zenonia.session.world; w.effects = []; w.monsters = []; w.player.x = 160; w.player.y = 224; w.player.dir = 'right'; });
  await page.keyboard.down('KeyJ'); await wait(500); await page.keyboard.up('KeyJ');
  await page.screenshot({ path: `${out}/basic-combo.png` });
  await wait(300);
  if (errors.length) throw Error(`page errors:\n${errors.join('\n')}`);
  console.log(JSON.stringify({ pass: true, casted: results.length, summary: results.map((r) => [r.id, r.before.hp, r.after.hp]) }));
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
} finally {
  await browser.close();
  server?.kill();
}

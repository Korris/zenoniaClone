// Browser check for the PixiJS (WebGL) world renderer, side by side with Canvas2D.
//  1. For each renderer (pixi, canvas): New Game warrior via the real menu, Frost Hollow, walk, then a full 4-hit combo on three
//     tough monsters; the simulation is frozen at each impact (+0 and +60ms) and the composited page is captured to
//     tools/screenshots/pixi-renderer/<renderer>-hit<N>-<phase>[-zoom].png.
//  2. Smoke all 5 classes in pixi mode: New Game, 3 attacks, one skill, screenshot <class>-smoke.png.
//  Fails on page errors, console errors or WebGL errors (gl.getError after the run).
// Usage: node tools/check-pixi-renderer.mjs   (server on http://127.0.0.1:5173)   env: CLASSES=warrior,wizard  SKIP_AB=1
import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';

const BASE = process.env.PIXI_CHECK_URL || 'http://127.0.0.1:5173';
const OUT = 'tools/screenshots/pixi-renderer';
const ALL_CLASSES = ['warrior', 'wizard', 'mechanic', 'cleric', 'assassin'];
const CLASSES = (process.env.CLASSES || ALL_CLASSES.join(',')).split(',');
mkdirSync(OUT, { recursive: true });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true,
  args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
const problems = [];

async function openGame(renderer, classId) {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 640 });
  page.setDefaultTimeout(30000);
  page.on('pageerror', (e) => problems.push(`[${renderer}/${classId}] pageerror ${e}`));
  page.on('console', (m) => { if (['error', 'warning'].includes(m.type()) && !/favicon|GPU stall|swiftshader/i.test(m.text())) problems.push(`[${renderer}/${classId}] console.${m.type()} ${m.text()}`); });
  await page.goto(`${BASE}/?renderer=${renderer}`);
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 80 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  for (let i = 0; i < ALL_CLASSES.indexOf(classId); i++) await page.keyboard.press('ArrowRight', { delay: 50 });
  await page.keyboard.press('Enter', { delay: 80 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  return page;
}

/** Frost Hollow arena: invulnerable level-30 hero facing right, `types` dummies in a row, everything else removed. */
async function setupArena(page, types) {
  await page.evaluate(async (types) => {
    const { Monster } = await import('/src/entities/monster.js');
    const c = window.__zenonia;
    c.session.enterMap('frostHollow');
    const w = c.session.world, p = w.player;
    p.level = 30; p.maxHp = p.hp = 1e6; p.mp = p.maxMp = 1e6; p.dir = 'right'; p.invulnTimer = 1e12;
    w.spawner.update = () => [];
    w.monsters = [];
    types.forEach((type, i) => {
      const m = new Monster(type, p.x + 24 + (i % 2) * 14, p.y + [-6, 8, 0][i % 3]);
      m.maxHp = m.hp = 1e6; m.def = { ...m.def, atk: 0, speed: 0 }; m.home = { x: m.x, y: m.y };
      w.monsters.push(m);
    });
    w.camera.snapTo(p.x, p.y);
    c.banner.lifeMs = 0;
  }, types);
  await sleep(600);
}

async function shot(page, name, zoom) {
  const clip = zoom
    ? await page.evaluate(() => { const w = window.__zenonia.session.world, p = w.player;
      return { x: Math.max(0, (p.x - w.camera.renderX) * 4 - 90), y: Math.max(0, (p.y - w.camera.renderY) * 4 - 190), width: 300, height: 240, scale: 2.5 }; })
    : { x: 0, y: 0, width: 960, height: 640 };
  await page.screenshot({ path: `${OUT}/${name}.png`, clip });
}

/** Presses J once the previous swing is done and freezes the sim at the first impact; resolves true if it happened. */
async function swingAndFreeze(page) {
  return page.evaluate(() => new Promise((resolve) => {
    const c = window.__zenonia, w = c.session.world, swing0 = w.swingCounter;
    for (const m of w.monsters) { m.x = m.home.x; m.y = m.home.y; m.knockback.timer = 0; }
    const original = c.update.bind(c);
    window.__unfreeze = () => { c.update = original; };
    const watch = () => {
      if (w.swingCounter > swing0 && w.feel.stars.activeCount > 0) { c.update = () => {}; resolve(true); return; }
      requestAnimationFrame(watch);
    };
    const press = () => {
      if (w.player.combo.isAttacking) return requestAnimationFrame(press);
      requestAnimationFrame(watch);
      window.dispatchEvent(new KeyboardEvent('keydown', { code: 'KeyJ' }));
    };
    setTimeout(() => resolve(false), 3000);
    press();
  }));
}

async function comboShots(renderer) {
  const page = await openGame(renderer, 'warrior');
  await setupArena(page, ['frostSlime', 'iceGoblin', 'spectralWolf']);
  await page.keyboard.down('KeyD'); await sleep(250); await page.keyboard.up('KeyD'); // short walk
  await setupArena(page, ['frostSlime', 'iceGoblin', 'spectralWolf']);
  for (let hit = 1; hit <= 4; hit++) {
    if (!(await swingAndFreeze(page))) { problems.push(`[${renderer}] hit ${hit}: no impact detected`); continue; }
    await sleep(120);
    await shot(page, `${renderer}-hit${hit}-0`); await shot(page, `${renderer}-hit${hit}-0-zoom`, true);
    await page.evaluate(() => window.__unfreeze()); await sleep(70);
    await page.evaluate(() => { const c = window.__zenonia, o = c.update; window.__unfreeze = () => { c.update = o; }; c.update = () => {}; });
    await shot(page, `${renderer}-hit${hit}-60`, false);
    await page.evaluate(() => window.__unfreeze()); await sleep(40);
  }
  const gl = renderer === 'pixi' ? await page.evaluate(() => { const g = window.__pixiView.app.renderer.gl; return g ? g.getError() : 'no-gl'; }) : 0;
  if (gl) problems.push(`[${renderer}] gl.getError() = ${gl}`);
  await page.close();
}

async function classSmoke(classId) {
  const page = await openGame('pixi', classId);
  await setupArena(page, ['frostSlime', 'iceGoblin']);
  const skill = await page.evaluate(async () => {
    const { SKILL_DEFINITIONS } = await import('/src/data/skill-definitions.js');
    const { CLASS_DEFINITIONS } = await import('/src/data/class-definitions.js');
    const p = window.__zenonia.session.player;
    const actives = Object.values(SKILL_DEFINITIONS).filter((d) => d.type === 'active' && CLASS_DEFINITIONS[p.classId].branches.includes(d.branch));
    for (const d of actives) p.skills[d.id] = 3;
    p.hotbar = actives.slice(0, 3).map((d) => d.id);
    return p.hotbar[0];
  });
  for (let i = 0; i < 3; i++) { await page.keyboard.press('KeyJ', { delay: 60 }); await sleep(230); }
  await page.keyboard.press('KeyK', { delay: 60 });
  await sleep(160);
  await shot(page, `${classId}-smoke`, false);
  await shot(page, `${classId}-smoke-zoom`, true);
  const info = await page.evaluate(() => { const w = window.__zenonia.session.world; return { effects: w.effects.length, hp: w.monsters.map((m) => Math.round(m.hp)) }; });
  console.log(`${classId}: skill ${skill}`, JSON.stringify(info));
  const gl = await page.evaluate(() => window.__pixiView.app.renderer.gl.getError());
  if (gl) problems.push(`[pixi/${classId}] gl.getError() = ${gl}`);
  await page.close();
}

try {
  if (!process.env.SKIP_AB) for (const renderer of ['pixi', 'canvas']) { await comboShots(renderer); console.log(`${renderer} combo shots done`); }
  for (const classId of CLASSES) await classSmoke(classId);
} finally {
  await browser.close();
}
console.log(problems.length ? `PROBLEMS:\n${problems.join('\n')}` : 'no page errors, no WebGL errors');
if (problems.length) process.exitCode = 1;

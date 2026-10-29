// Browser check for melee game feel: warrior combos on a row of tough Frost Hollow monsters.
// Captures canvas frames at the hit, +40ms and +100ms of combo hit 1 and of the finisher (hit 4) into
// tools/screenshots/combat-feel/ (full frame + 3x crop around the hero), and reports page errors.
// Usage: node tools/check-combat-feel.mjs   (server on http://127.0.0.1:5173)
import puppeteer from 'puppeteer-core';
import { mkdirSync, writeFileSync } from 'node:fs';

const OUT = process.env.OUT || 'tools/screenshots/combat-feel';
const OVERRIDES = process.env.FEEL ? JSON.parse(process.env.FEEL) : null; // e.g. FEEL='{"slashAdditiveAlpha":0}' for A/B shots
mkdirSync(OUT, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
const save = (name, dataUrl) => writeFileSync(`${OUT}/${name}.png`, Buffer.from(dataUrl.split(',')[1], 'base64'));
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 640 });
  const errors = [];
  page.on('pageerror', (e) => errors.push(String(e)));
  page.setDefaultTimeout(20000);
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');

  await page.evaluate(async (overrides) => {
    const { COMBAT_FEEL } = await import('/src/data/combat-feel-config.js');
    Object.assign(COMBAT_FEEL, overrides || {});
    const { Monster } = await import('/src/entities/monster.js');
    const c = window.__zenonia;
    c.session.startNewGame('warrior');
    c.state = 'playing';
    c.session.enterMap('frostHollow');
    const w = c.session.world, p = w.player;
    p.level = 20; p.maxHp = p.hp = 1e6; p.dir = "right"; p.invulnTimer = 1e12; // monsters must not interrupt the combo
    w.spawner.update = () => []; // no respawns while testing
    w.monsters = [];
    for (const [dx, dy, type] of [[22, -6, 'frostSlime'], [24, 8, 'iceGoblin'], [38, 0, 'spectralWolf']]) {
      const m = new Monster(type, p.x + dx, p.y + dy);
      m.maxHp = m.hp = 1e6;
      m.def = { ...m.def, atk: 0, speed: 0 };
      m.home = { x: p.x + dx, y: p.y + dy };
      w.monsters.push(m);
    }
    w.camera.snapTo(p.x, p.y);
  }, OVERRIDES);
  await new Promise((r) => setTimeout(r, 500));

  /** Press J, wait for the first impact (stars active), then grab frames at +0 / +40 / +100ms. */
  async function captureHit(label) {
    return page.evaluate((label) => new Promise((resolve) => {
      const c = window.__zenonia, w = c.session.world, canvas = document.getElementById('game-canvas');
      for (const m of w.monsters) { m.x = m.home.x; m.y = m.home.y; m.knockback.timer = 0; }
      const out = {}; let t0 = null; const swing0 = w.swingCounter; const wanted = [0, 40, 100];
      const crop = document.createElement('canvas'); crop.width = 720; crop.height = 540;
      function grab(name) {
        const p = w.player, s = canvas.width / 960;
        const x = (p.x - w.camera.renderX) * 4 * s, y = (p.y - w.camera.renderY) * 4 * s;
        const cx = crop.getContext('2d'); cx.imageSmoothingEnabled = true;
        cx.drawImage(canvas, x - 70 * s, y - 100 * s, 240 * s, 180 * s, 0, 0, 720, 540);
        out[`${label}-${name}`] = { full: canvas.toDataURL('image/png'), zoom: crop.toDataURL('image/png') };
      }
      function tick(now) {
        if (t0 === null && w.swingCounter > swing0 && w.feel.stars.activeCount > 0) t0 = now;
        if (t0 !== null) {
          while (wanted.length && now - t0 >= wanted[0]) grab(`+${wanted.shift()}ms`);
          if (!wanted.length) return resolve(out);
        }
        requestAnimationFrame(tick);
      }
      // Wait for the previous swing to finish (still inside the 400ms chain window), then press.
      const press = () => {
        if (w.player.combo.isAttacking) return requestAnimationFrame(press);
        requestAnimationFrame(tick);
        window.dispatchEvent(new KeyboardEvent('keydown', { code: 'KeyJ' }));
      };
      setTimeout(() => resolve(out), 3000);
      press();
    }), label);
  }

  const results = {};
  for (let step = 1; step <= 4; step++) {
    const frames = await captureHit(`hit${step}`);
    Object.assign(results, frames); console.log(step, Object.keys(frames).length, await page.evaluate(() => { const c = window.__zenonia.session.world.player.combo; return [c.state, c.step, c.lastStep, c.chainTimer]; }));
    await new Promise((r) => setTimeout(r, 30));
  }
  for (const [name, { full, zoom }] of Object.entries(results)) { save(`${name}-full`, full); save(`${name}-zoom`, zoom); }
  const stats = await page.evaluate(() => {
    const w = window.__zenonia.session.world;
    return { monsters: w.monsters.map((m) => m.hp), pool: w.feel.streaks.items.length, hitStop: w.hitStopMs };
  });
  console.log('frames:', Object.keys(results).length, JSON.stringify(stats));
  console.log(errors.length ? `PAGE ERRORS:\n${errors.join('\n')}` : 'no page errors');
  if (errors.length) process.exitCode = 1;
} finally {
  await browser.close();
}

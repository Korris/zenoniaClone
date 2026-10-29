import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
const out = 'tools/screenshots/warrior-range'; mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage(), errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  await page.evaluate(() => { const g = window.__zenonia; g.update = () => {}; g.banner.lifeMs = 0; });
  for (const dir of ['right', 'left', 'up', 'down']) for (const step of [1, 2, 4]) {
    const result = await page.evaluate(async ({ dir, step }) => {
      const { performComboHit } = await import('/src/systems/combo-hit-system.js');
      const { Monster } = await import('/src/entities/monster.js');
      const { DIRECTION_VECTORS } = await import('/src/core/constants.js');
      const g = window.__zenonia, w = g.session.world, p = w.player, v = DIRECTION_VECTORS[dir];
      w.hitRoll = () => 0.5;
      p.dir = dir; p.combo.startHit(step); p.combo.state = 'active'; p.combo.timer = 30;
      w.effects = []; w.particles = []; w.texts = []; w.npcs = [];
      const place = distance => { const m = new Monster('frostSlime', p.x + v.x * distance, p.y - 2 + v.y * distance); m.hp = m.maxHp = 1000; m.animTimer = 0; return m; };
      const near = place(step === 4 ? 42 : 30), far = place(64); w.monsters = [near, far];
      performComboHit(w, step);
      for (const e of w.effects) e.update(e.maxLifeMs * 0.3);
      g.session.camera.snapTo(p.x + v.x * 15, p.y - 10 + v.y * 15);
      if (near.hp === 1000 || far.hp !== 1000) throw Error(`Range mismatch ${dir}/${step}`);
      return { dir, step, reach: p.combo.currentHit.reach, width: p.combo.currentHit.width };
    }, { dir, step });
    await new Promise(resolve => setTimeout(resolve, 100));
    await page.screenshot({ path: `${out}/${dir}-hit${step}.png` });
    console.log(JSON.stringify(result));
  }
  if (errors.length) throw Error(errors.join('\n'));
  console.log('PASS: near target hit and distant target excluded in all four directions, normal hit and finisher; no page errors.');
} finally { await browser.close(); }

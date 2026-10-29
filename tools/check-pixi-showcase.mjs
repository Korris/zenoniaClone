// Browser check for the PixiJS showcase effects: live GlowFilter on enhanced held gear (by enhance band), the F8 A/B against the
// baked rings, and the finisher shockwave. Captures zoomed hero crops into tools/screenshots/pixi-renderer/showcase-*.png.
// Usage: node tools/check-pixi-showcase.mjs   (server on http://127.0.0.1:5173)   env: CLASS=warrior
import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';

const BASE = process.env.PIXI_CHECK_URL || 'http://127.0.0.1:5173';
const OUT = 'tools/screenshots/pixi-renderer';
const CLASS = process.env.CLASS || 'warrior';
const ALL = ['warrior', 'wizard', 'mechanic', 'cleric', 'assassin'];
const WEAPON = { warrior: 'sword', wizard: 'staff', mechanic: 'gun', cleric: 'staff', assassin: 'dagger' };
mkdirSync(OUT, { recursive: true });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true,
  args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist'] });
const problems = [];
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 640 });
  page.on('pageerror', (e) => problems.push(String(e)));
  page.on('console', (m) => { if (m.type() === 'error') problems.push(m.text()); });
  await page.goto(`${BASE}/?renderer=pixi`);
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 80 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  for (let i = 0; i < ALL.indexOf(CLASS); i++) await page.keyboard.press('ArrowRight', { delay: 50 });
  await page.keyboard.press('Enter', { delay: 80 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  await page.evaluate(() => { const c = window.__zenonia; c.session.enterMap('frostHollow'); c.session.world.monsters = []; c.session.world.spawner.update = () => []; c.banner.lifeMs = 0; });
  const zoom = () => page.evaluate(() => { const w = window.__zenonia.session.world, p = w.player;
    return { x: Math.max(0, (p.x - w.camera.renderX) * 4 - 110), y: Math.max(0, (p.y - w.camera.renderY) * 4 - 150), width: 220, height: 200, scale: 3 }; });
  for (const fx of [true, false]) {
    await page.evaluate((fx) => { window.__zenoniaFx = fx; import('/src/data/combat-feel-config.js').then((m) => { m.COMBAT_FEEL.pixi.fxEnabled = fx; window.__pixiView.applyToggles(); }); }, fx);
    for (const enhance of [0, 7, 9, 11, 15]) {
      await page.evaluate((enhance, weapon) => {
        const p = window.__zenonia.session.player;
        p.equipment.weapon = { slot: 'weapon', itemLevel: 65, enhance, templateId: weapon, classId: p.classId, stats: { atk: 1 } };
        if (p.equipment.offhand) p.equipment.offhand = { ...p.equipment.offhand, itemLevel: 65, enhance };
        p.dir = 'down'; p.invulnTimer = 1e12;
      }, enhance, WEAPON[CLASS]);
      await sleep(350);
      await page.screenshot({ path: `${OUT}/showcase-${CLASS}-${fx ? 'live' : 'baked'}-plus${enhance}.png`, clip: await zoom() });
    }
  }
  await page.evaluate(() => import('/src/data/combat-feel-config.js').then((m) => { m.COMBAT_FEEL.pixi.fxEnabled = true; window.__pixiView.applyToggles(); }));
  console.log('gl error', await page.evaluate(() => window.__pixiView.app.renderer.gl.getError()));
} finally {
  await browser.close();
}
console.log(problems.length ? `PROBLEMS:\n${problems.join('\n')}` : 'no page errors');
if (problems.length) process.exitCode = 1;

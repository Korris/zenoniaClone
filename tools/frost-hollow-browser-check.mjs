// Headless smoke test for the Frost Hollow battle map: boots the game with the locally installed
// Chrome, starts a new game, walks through the village south gate portal for real, screenshots the
// entrance / bridge / clearings / a combat hit, and reports console errors plus whether the painted
// backdrop and the HD monster sheets loaded.
// Usage: node tools/frost-hollow-browser-check.mjs [outDir]   (server must run on http://127.0.0.1:5173)
import puppeteer from 'puppeteer-core';
import { existsSync, mkdirSync } from 'node:fs';
import path from 'node:path';

const OUT = process.argv[2] || 'tools/screenshots';
const CHROME_PATHS = [
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
];
const executablePath = CHROME_PATHS.find((p) => existsSync(p));
if (!executablePath) throw new Error('No Chrome/Edge found');
mkdirSync(OUT, { recursive: true });

const browser = await puppeteer.launch({ executablePath, headless: true, args: ['--window-size=960,640'] });
const page = await browser.newPage();
await page.setViewport({ width: 960, height: 640 });
const errors = [];
page.on('pageerror', (e) => errors.push(String(e)));
page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

await page.goto('http://127.0.0.1:5173/index.html', { waitUntil: 'load' });
await page.evaluate(() => localStorage.clear());
await page.reload({ waitUntil: 'load' });
await page.waitForFunction(() => window.__zenonia && window.__zenonia.state === 'title', { timeout: 15000 });
await page.keyboard.press('Enter'); // New Game -> class select
await page.waitForFunction(() => window.__zenonia.state === 'classSelect', { timeout: 5000 });
await page.keyboard.press('Enter'); // confirm the warrior
await page.waitForFunction(() => window.__zenonia.state === 'playing', { timeout: 10000 });
await sleep(300);

const feetAt = (tx, ty) => ({ x: (tx + 0.5) * 16, y: (ty + 1) * 16 - 2 });
async function teleport(tx, ty) {
  await page.evaluate(({ x, y }) => {
    const g = window.__zenonia; const p = g.session.player; p.x = x; p.y = y; p.dash = null;
    g.session.world.camera.snapTo(p.x, p.y - 8);
  }, feetAt(tx, ty));
  await sleep(250);
}
const snapshot = () => page.evaluate(() => {
  const g = window.__zenonia; const w = g.session.world; const p = g.session.player;
  return {
    map: w.mapId, tx: Math.floor(p.x / 16), ty: Math.floor((p.y - 4) / 16),
    monsters: w.monsters.map((m) => m.type).reduce((a, t) => ((a[t] = (a[t] || 0) + 1), a), {}),
    hd: Object.keys(g.registry?.monstersHd || {}), backdrop: Boolean(g.registry?.backdrops?.hdFrostHollowGround),
  };
});

// Walk onto the village south gate portal for real (level 1 -> allowed).
await teleport(16, 20);
await page.keyboard.down('ArrowDown');
await sleep(500);
await page.keyboard.up('ArrowDown');
await sleep(700);
const arrived = await snapshot();
console.log('after portal', JSON.stringify(arrived));
if (arrived.map !== 'frostHollow') errors.push('village portal did not lead to Frost Hollow');
if (!arrived.backdrop) errors.push('Frost Hollow backdrop not loaded');
if (arrived.hd.length !== 3) errors.push('HD monster sheets missing: ' + JSON.stringify(arrived.hd));
await page.screenshot({ path: path.join(OUT, '30-frost-entrance.png') });
await teleport(9, 6); await page.screenshot({ path: path.join(OUT, '31-frost-nw-clearing.png') });
await teleport(18, 12); await page.screenshot({ path: path.join(OUT, '32-frost-bridge.png') });
await teleport(28, 7); await page.screenshot({ path: path.join(OUT, '33-frost-ne-clearing.png') });
await teleport(27, 16); await page.screenshot({ path: path.join(OUT, '34-frost-se-clearing.png') });

// Combat: stand left of a goblin, swing once, screenshot the hit flash and the aftermath.
await page.evaluate(() => {
  const g = window.__zenonia; const w = g.session.world; const p = g.session.player;
  const m = w.monsters.find((x) => x.type === 'iceGoblin') || w.monsters[0];
  if (m) { p.x = m.x - 14; p.y = m.y; p.dir = 'right'; w.camera.snapTo(p.x, p.y - 8); }
});
await sleep(200);
await page.keyboard.press('KeyJ');
await sleep(120);
await page.screenshot({ path: path.join(OUT, '35-frost-combat.png') });
await sleep(1500);
await page.screenshot({ path: path.join(OUT, '36-frost-combat-after.png') });

// Exit portal (level gated to 2): a level-1 hero must bounce, then level up and pass.
await teleport(33, 4);
await page.keyboard.down('ArrowUp'); await sleep(350); await page.keyboard.up('ArrowUp'); await sleep(500);
const sealed = await snapshot();
if (sealed.map !== 'frostHollow') errors.push('exit portal let a level-1 hero through');
await page.evaluate(() => { const p = window.__zenonia.session.player; p.level = 2; });
await teleport(33, 4);
await page.keyboard.down('ArrowUp'); await sleep(350); await page.keyboard.up('ArrowUp'); await sleep(700);
const meadow = await snapshot();
console.log('after exit portal', JSON.stringify(meadow));
if (meadow.map !== 'verdantMeadow') errors.push('exit portal did not lead to Verdant Meadow');

console.log('errors', JSON.stringify(errors));
await browser.close();
process.exit(errors.length ? 1 : 0);

// Headless check of lazy per-class art: counts images/bytes fetched before the title screen, then starts a New Game
// for each of the five classes, screenshots gameplay, and reports page errors + the fetch delta per class.
// Usage: node tools/check-class-lazy-load.mjs [outDir]   (server must run on http://127.0.0.1:5173)
import puppeteer from 'puppeteer-core';
import { existsSync, mkdirSync } from 'node:fs';
import path from 'node:path';

const OUT = process.argv[2] || 'tools/screenshots/class-lazy-load';
const CHROME = ['C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find((p) => existsSync(p));
if (!CHROME) throw new Error('No Chrome/Edge found');
mkdirSync(OUT, { recursive: true });
const browser = await puppeteer.launch({ executablePath: CHROME, headless: true, args: ['--window-size=960,640'] });
const errors = [];
let images = 0, bytes = 0;
const classes = ['warrior', 'wizard', 'mechanic', 'cleric', 'assassin'];
for (const [index, classId] of classes.entries()) {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 640 });
  page.on('pageerror', (e) => errors.push(`${classId}: ${e}`));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(`${classId}: ${m.text()}`); });
  let phase = { images: 0, bytes: 0 };
  page.on('response', async (res) => {
    if (!/\.(png|webp)$/i.test(res.url())) return;
    phase.images += 1;
    phase.bytes += Number(res.headers()['content-length'] || (await res.buffer().catch(() => Buffer.alloc(0))).length);
  });
  await page.goto('http://127.0.0.1:5173/index.html', { waitUntil: 'load' });
  await page.evaluate(() => localStorage.clear());
  await page.reload({ waitUntil: 'load' });
  await page.waitForFunction(() => window.__zenonia?.state === 'title', { timeout: 60000 });
  const title = { ...phase };
  phase = { images: 0, bytes: 0 };
  await page.keyboard.press('Enter');
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.evaluate(() => { window.__zenoniaClassIds = ['warrior', 'wizard', 'mechanic', 'cleric', 'assassin']; });
  if (index === 0) await page.screenshot({ path: path.join(OUT, 'class-select.png') });
  await page.evaluate((id) => { const c = window.__zenonia.classSelect; c.selected = Math.max(0, window.__zenoniaClassIds.indexOf(id)); }, classId);
  await page.keyboard.press('Enter');
  const sawLoading = await page.waitForFunction(() => window.__zenonia.state === 'loading' || window.__zenonia.state === 'playing', { timeout: 60000 }).then(() => true);
  await page.waitForFunction(() => window.__zenonia.state === 'playing', { timeout: 60000 });
  await new Promise((r) => setTimeout(r, 1200));
  const info = await page.evaluate(() => ({ cls: window.__zenonia.session.player.classId, loaded: [...window.__zenonia.registry.loadedClasses] }));
  await page.screenshot({ path: path.join(OUT, `${classId}-play.png`) });
  console.log(classId, JSON.stringify({ title, afterPick: phase, info, sawLoading }));
  await page.close();
}
console.log('errors', JSON.stringify(errors));
await browser.close();
process.exit(errors.length ? 1 : 0);

import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
const out = 'tools/screenshots/frostfang-boss';
mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage(), errors = [];
  page.on('pageerror', e => { errors.push(String(e)); console.log(String(e)); });
  page.on('console', m => { if (m.type() === 'error') console.log(m.text()); });
  page.on('requestfailed', r => console.log('Request failed', r.url(), r.failure()?.errorText));
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  await page.goto('http://127.0.0.1:5173');
  console.log('Page loaded', await page.title(), await page.evaluate(() => document.body.innerText.slice(0, 500)));
  await page.waitForFunction(() => window.__zenonia?.state === 'title' || document.body.innerText.includes('Failed to start game'));
  const bootError = await page.evaluate(() => document.body.innerText);
  if (bootError.includes('Failed to start game')) throw Error(bootError);
  console.log('Title ready');
  await page.keyboard.press('Enter');
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  console.log('Class selection ready');
  await page.keyboard.press('Enter');
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  const stats = await page.evaluate(() => {
    const g = window.__zenonia; g.update = () => {}; g.session.enterMap('frostHollow');
    g.banner.lifeMs = 0;
    const w = g.session.world, p = w.player, boss = w.livingBoss;
    if (boss?.type !== 'frostfangKing' || boss.maxHp !== 580 || boss.def.atk !== 33) throw Error('Wrong boss stats');
    const hd = g.registry.monstersHd[boss.type];
    if (!hd || Object.values(hd.frames).some(frames => frames.length !== 4)) throw Error('Missing boss art');
    p.x = boss.x - 34; p.y = boss.y + 26; p.dir = 'right';
    boss.animTimer = 0; boss.ai.state = 'idle';
    g.session.camera.snapTo(boss.x, boss.y);
    return { hp: boss.hp, atk: boss.def.atk, x: boss.x, y: boss.y };
  });
  for (const dir of ['down', 'up', 'left', 'right']) {
    await page.evaluate(dir => { window.__zenonia.session.world.livingBoss.dir = dir; }, dir);
    await new Promise(r => setTimeout(r, 100));
    await page.screenshot({ path: `${out}/${dir}.png` });
  }
  if (errors.length) throw Error(errors.join('\n'));
  console.log(JSON.stringify({ pass: true, ...stats, directions: 4, errors }));
} finally { await browser.close(); }

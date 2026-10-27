// Frame-hitch check: casts every active skill of each class twice in Frost Hollow and reports the
// worst frame (ms) in the second after each cast. A first cast much slower than the second means
// something is built lazily on first use (sprite levels in graphics/smooth-sprite-draw.js, audio).
// Usage: node tools/check-first-cast-hitch.mjs   (server must run on http://127.0.0.1:5173)
import puppeteer from 'puppeteer-core';
import { existsSync } from 'node:fs';

const CHROME_PATHS = [
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
];
const executablePath = CHROME_PATHS.find((p) => existsSync(p));
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const browser = await puppeteer.launch({ executablePath, headless: false, args: ['--window-size=1000,720'] });
const errors = [];

for (const classId of ['warrior', 'wizard', 'mechanic']) {
  const page = await browser.newPage();
  page.on('pageerror', (e) => errors.push(String(e)));
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 1.25 });
  await page.goto('http://127.0.0.1:5173/index.html', { waitUntil: 'load' });
  await page.waitForFunction(() => window.__zenonia?.state === 'title', { timeout: 30000 });
  const skills = await page.evaluate(async (cls) => {
    const c = window.__zenonia;
    c.session.startNewGame(cls);
    c.state = 'playing';
    c.session.enterMap('frostHollow');
    const mod = await import('/src/data/skill-definitions.js');
    const p = c.session.player;
    const ids = Object.values(mod.SKILL_DEFINITIONS).filter((d) => d.type === 'active').map((d) => d.id);
    p.level = 40; p.maxHp = p.hp = 1e6; p.maxMp = p.mp = 1e6;
    window.__frames = [];
    let last = performance.now();
    const tick = (now) => { window.__frames.push([now, now - last]); last = now; requestAnimationFrame(tick); };
    requestAnimationFrame(tick);
    return ids;
  }, classId);
  await sleep(1500);
  const rows = [];
  for (const id of skills) {
    const worst = [];
    for (let round = 0; round < 2; round++) {
      await page.evaluate((skillId) => {
        const c = window.__zenonia, p = c.session.player, w = c.session.world;
        const m = w.monsters.find((x) => !x.dead);
        if (m) { p.x = m.x - 14; p.y = m.y; }
        p.skills[skillId] = 5; p.hotbar = [skillId, null, null]; p.cooldowns = {}; p.mp = 1e6;
        p.cast = null; p.combo?.reset?.();
        window.__castAt = performance.now();
      }, id);
      await page.keyboard.down('KeyK'); await sleep(50); await page.keyboard.up('KeyK');
      await sleep(1200);
      worst.push(await page.evaluate(() => {
        const used = window.__zenonia.session.player.cooldowns;
        const f = window.__frames.filter(([t]) => t > window.__castAt && t < window.__castAt + 1200);
        return { ms: Math.round(Math.max(0, ...f.map(([, d]) => d))), cast: Object.keys(used).length > 0 };
      }));
      await sleep(600);
    }
    if (worst[0]?.cast) rows.push(`${id.padEnd(16)} first ${String(worst[0].ms).padStart(3)}ms   again ${String(worst[1].ms).padStart(3)}ms`);
  }
  console.log(`\n== ${classId}\n${rows.join('\n')}`);
  await page.close();
}
console.log('\nerrors', errors);
await browser.close();

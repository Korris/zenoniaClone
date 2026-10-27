import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';

const baseline = process.argv.includes('--before');
const out = 'tools/screenshots/display-quality';
mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  for (const dpr of [1, 1.5, 2]) {
    const context = await browser.createBrowserContext();
    const page = await context.newPage();
    await page.setViewport({ width: 960, height: 640, deviceScaleFactor: dpr });
    const errors = [];
    page.on('pageerror', e => errors.push(String(e)));
    await page.goto('http://127.0.0.1:5173');
    await page.waitForFunction(() => window.__zenonia?.state === 'title');
    await page.keyboard.press('Enter', { delay: 100 });
    await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
    await page.keyboard.press('Enter', { delay: 100 });
    await page.waitForFunction(() => window.__zenonia.state === 'playing');
    await page.evaluate(() => {
      const g = window.__zenonia;
      g.banner.lifeMs = 0;
      g.session.world.hitStopMs = 1e8;
      g.session.player.idleTimer = 0;
      g.session.player.animTimer = 0;
    });
    const info = await page.evaluate(() => {
      const canvas = document.getElementById('game-canvas'), rect = canvas.getBoundingClientRect();
      return { width: canvas.width, height: canvas.height, cssWidth: rect.width, cssHeight: rect.height,
        frame: window.__zenonia.registry.hero.down.idle[0].width,
        npcFrames: Object.values(window.__zenonia.registry.npcsHd).map(npc => npc.down.idle[0].width) };
    });
    if (!baseline && (info.width !== Math.round(info.cssWidth * dpr) || info.height !== Math.round(info.cssHeight * dpr) || info.frame !== 192)) throw Error(JSON.stringify(info));
    if (!baseline && (info.npcFrames.length !== 10 || info.npcFrames.some(size => size !== 192))) throw Error('NPCs did not load their high-resolution sheets');
    await page.screenshot({ path: `${out}/${baseline ? 'before' : 'after'}-dpr${dpr}.png` });
    if (!baseline) {
      await page.keyboard.press('b', { delay: 100 });
      await page.waitForFunction(() => window.__zenonia.state === 'inventory');
      await page.mouse.click(410, 116);
      await page.waitForFunction(() => window.__zenonia.inventoryPanel.mode === 'Equipment');
      await page.setViewport({ width: 768, height: 512, deviceScaleFactor: dpr });
      await page.waitForFunction(() => document.getElementById('game-canvas').getBoundingClientRect().width <= 768);
      await page.mouse.click(496, 92.8);
      await page.waitForFunction(() => window.__zenonia.inventoryPanel.mode === 'Inventory');
      await page.mouse.click(728, 51.2);
      await page.waitForFunction(() => window.__zenonia.state === 'playing');
    }
    if (errors.length) throw Error(errors.join('\n'));
    console.log(JSON.stringify({ phase: baseline ? 'before' : 'after', dpr, ...info, errors }));
    await context.close();
  }
} finally { await browser.close(); }

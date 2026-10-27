import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';

const out = 'tools/screenshots/walk-v2';
mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 640 });
  const errors = [];
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  for (const cls of ['warrior', 'wizard', 'mechanic']) {
    await page.evaluate(cls => { window.__zenonia.session.player.classId = cls; }, cls);
    for (const [key, dir] of [['ArrowLeft', 'left'], ['ArrowRight', 'right']]) {
      await page.keyboard.down(key);
      await new Promise(resolve => setTimeout(resolve, 180));
      const info = await page.evaluate(() => {
        const g = window.__zenonia, p = g.session.player;
        return { dir: p.dir, moving: p.moving, frames: g.registry.heroes[p.classId][p.dir].walk.length };
      });
      if (info.dir !== dir || !info.moving || info.frames !== 4) throw Error(JSON.stringify(info));
      await page.screenshot({ path: `${out}/${cls}-${dir}.png` });
      await page.keyboard.up(key);
      await new Promise(resolve => setTimeout(resolve, 80));
    }
  }
  // Compare the old and revised cycles at native game-frame resolution.
  await page.setViewport({ width: 840, height: 1320 });
  await page.evaluate(async () => {
    const canvas = document.createElement('canvas');
    canvas.width = 840; canvas.height = 1320;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#1d2534'; ctx.fillRect(0, 0, 840, 1320);
    ctx.font = '16px sans-serif';
    for (const [row, cls] of ['hero', 'wizard', 'mechanic'].entries()) {
      for (const [col, file] of ['walk', 'walk-v2'].entries()) {
        const img = new Image();
        img.src = `/assets/hd/${cls}/normalized/${file}.png`;
        await img.decode();
        ctx.fillStyle = '#ffffff'; ctx.fillText(`${cls} / ${col ? 'NEW' : 'OLD'}`, 16 + col * 420, 24 + row * 440);
        ctx.drawImage(img, 16 + col * 420, 36 + row * 440);
      }
    }
    document.body.replaceChildren(canvas);
    document.body.style.margin = '0';
  });
  await page.screenshot({ path: `${out}/old-vs-new.png` });
  if (errors.length) throw Error(errors.join('\n'));
  console.log('PASS: revised 4-frame walks loaded for 3 classes; left/right movement and rendering; no page errors.');
} finally { await browser.close(); }

import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
const out = 'tools/screenshots/skill-icons-chibi'; mkdirSync(out, { recursive: true });
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
  const result = await page.evaluate(async () => {
    const { SKILL_ORDER } = await import('/src/data/skill-definitions.js');
    const { FX_STRIPS } = await import('/src/graphics/asset-atlas.js');
    const { PAINTED_FX_ORDER } = await import('/src/graphics/painted-combat-assets.js');
    const g = window.__zenonia;
    for (const id of SKILL_ORDER) {
      const icon = g.registry.icons[id];
      if (!icon || Math.min(icon.width, icon.height) < 100) throw Error(`Pixel/missing icon ${id}`);
      const data = icon.getContext('2d').getImageData(0, 0, icon.width, icon.height).data;
      if (!data.some((v, i) => i % 4 === 3 && v > 100)) throw Error(`Empty icon ${id}`);
    }
    for (const key of PAINTED_FX_ORDER) {
      const frames = g.registry.effects[key];
      if (frames.length !== 4 || frames.some(f => f.pixelsPerUnit !== 8 || f.width / 8 !== FX_STRIPS[key].w || f.height / 8 !== FX_STRIPS[key].h)) throw Error(`FX sizing ${key}`);
    }
    for (const icon of Object.values(g.registry.effects.markers)) if (!(icon.pixelsPerUnit > 1)) throw Error('Pixel quest marker');
    if (!g.registry.effects.hitParticle) throw Error('Pixel particles');
    return { skills: SKILL_ORDER.length, effects: PAINTED_FX_ORDER.length, markers: Object.keys(g.registry.effects.markers).length };
  });
  await page.keyboard.press('Tab', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'skills');
  for (const classId of ['warrior', 'wizard', 'mechanic']) {
    await page.evaluate(classId => { window.__zenonia.session.player.classId = classId; }, classId);
    await new Promise(resolve => setTimeout(resolve, 100));
    await page.screenshot({ path: `${out}/${classId}-tree.png` });
  }
  await page.evaluate(async () => {
    const { SKILL_ORDER } = await import('/src/data/skill-definitions.js');
    const { PAINTED_FX_ORDER } = await import('/src/graphics/painted-combat-assets.js');
    const { drawEffects, drawParticles } = await import('/src/graphics/effects-renderer.js');
    const g = window.__zenonia; g.update = () => {}; g.render = () => {};
    const canvas = document.createElement('canvas'); canvas.width = 960; canvas.height = 1100;
    const ctx = canvas.getContext('2d'); ctx.fillStyle = '#172c41'; ctx.fillRect(0, 0, 960, 1100);
    ctx.font = '14px Segoe UI'; ctx.fillStyle = '#f4f1da';
    SKILL_ORDER.forEach((id, i) => {
      const x = (i % 10) * 96, y = Math.floor(i / 10) * 102;
      ctx.drawImage(g.registry.icons[id], x + 10, y + 4, 76, 76);
      ctx.fillText(id, x + 3, y + 98, 92);
    });
    PAINTED_FX_ORDER.forEach((spriteKey, row) => {
      const y = 548 + row * 68; ctx.fillStyle = '#f4f1da'; ctx.fillText(spriteKey, 8, y + 30);
      for (let i = 0; i < 4; i++) {
        ctx.save(); ctx.translate(220 + i * 190, y + 25); ctx.scale(1.7, 1.7);
        drawEffects(ctx, [{ spriteKey, progress: (i + 0.2) / 4, x: 0, y: 0, rotation: 0, scale: 1, alpha: 1 }], g.registry);
        ctx.restore();
      }
    });
    const spark = g.registry.effects.hitParticle; ctx.drawImage(spark, 850, 1030, 36, 36);
    drawParticles(ctx, [{ x: 900, y: 1050, lifeMs: 100 }], g.registry);
    document.body.replaceChildren(canvas); document.body.style.margin = '0';
  });
  await page.setViewport({ width: 960, height: 1100, deviceScaleFactor: 2 });
  await page.screenshot({ path: `${out}/all-icons-effects.png` });
  await page.screenshot({ path: `${out}/all-skill-icons.png`, clip: { x: 0, y: 0, width: 960, height: 510 } });
  if (errors.length) throw Error(errors.join('\n'));
  console.log(JSON.stringify({ pass: true, ...result, errors }));
} finally { await browser.close(); }

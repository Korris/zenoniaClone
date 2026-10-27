import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
const out = 'tools/screenshots/bag-monsters';
mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage(), errors = [];
  page.on('pageerror', e => { errors.push(String(e)); console.log('PAGE ERROR', String(e)); });
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  await page.goto('http://127.0.0.1:5173');
  console.log('Loaded page');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  console.log('Title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  console.log('Playing');
  await page.evaluate(async () => {
    const { generateItem } = await import('/src/systems/equipment-system.js');
    const g = window.__zenonia, p = g.session.player;
    p.level = 95;
    p.potions.hpPotion = 2; p.hp = 1;
    p.inventory = Array.from({ length: 20 }, (_, i) => generateItem(() => 0, i * 4 + 1, ['weapon', 'helmet', 'armor', 'gloves', 'boots'][i % 5], 'rare', p.classId));
    g.banner.lifeMs = 0;
  });
  await page.keyboard.press('b', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'inventory');
  await page.screenshot({ path: `${out}/equipment.png` });
  await page.mouse.click(620, 116);
  await page.waitForFunction(() => window.__zenonia.inventoryPanel.mode === 'Inventory');
  await page.mouse.click(453, 205);
  await page.mouse.click(650, 550);
  await page.waitForFunction(() => window.__zenonia.session.player.potions.hpPotion === 1);
  await page.mouse.click(363, 295);
  await page.screenshot({ path: `${out}/inventory.png` });
  await page.mouse.click(650, 550);
  await page.waitForFunction(() => window.__zenonia.session.player.equipment.weapon !== null);
  await page.mouse.click(410, 116);
  await page.waitForFunction(() => window.__zenonia.inventoryPanel.mode === 'Equipment');
  await page.mouse.click(800, 550);
  await page.waitForFunction(() => window.__zenonia.session.player.equipment.weapon === null);
  await page.mouse.click(620, 116);
  await page.waitForFunction(() => window.__zenonia.inventoryPanel.mode === 'Inventory');
  await page.mouse.click(450, 550);
  await page.waitForFunction(() => window.__zenonia.inventoryPanel.page === 1);
  await page.screenshot({ path: `${out}/inventory-page2.png` });
  await page.mouse.click(820, 116);
  await page.waitForFunction(() => window.__zenonia.state === 'skills');
  for (const classId of ['warrior', 'wizard', 'mechanic']) {
    await page.evaluate(id => { window.__zenonia.session.player.classId = id; }, classId);
    await new Promise(resolve => setTimeout(resolve, 100));
    await page.screenshot({ path: `${out}/${classId}-tree.png` });
  }
  await page.mouse.click(910, 64);
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  await page.evaluate(async () => {
    const { drawMonster } = await import('/src/graphics/monster-renderer.js');
    const g = window.__zenonia, canvas = document.createElement('canvas');
    canvas.width = 960; canvas.height = 640;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#213447'; ctx.fillRect(0, 0, 960, 640);
    ctx.font = '18px sans-serif'; ctx.fillStyle = '#fff';
    const dirs = ['down', 'up', 'left', 'right'];
    for (const [row, type] of ['frostSlime', 'iceGoblin', 'spectralWolf'].entries()) {
      const hd = g.registry.monstersHd[type];
      if (!hd || hd.frames.up[0].width !== 192 || hd.frames.up[0].toDataURL() === hd.frames.down[0].toDataURL()) throw Error(`Invalid rear art: ${type}`);
      ctx.fillText(type, 12, row * 200 + 35);
      for (const [col, dir] of dirs.entries()) {
        ctx.save(); ctx.translate(210 + col * 190, 165 + row * 200); ctx.scale(6, 6);
        drawMonster(ctx, { type, dir, x: 0, y: 0, ai: { state: 'chase' }, animTimer: 0, hitFlashTimer: 0, hp: 1, maxHp: 1 }, g.registry);
        ctx.restore(); ctx.fillText(dir, 195 + col * 190, 190 + row * 200);
      }
    }
    document.body.replaceChildren(canvas); document.body.style.margin = '0';
  });
  await page.screenshot({ path: `${out}/monster-directions.png` });
  if (errors.length) throw Error(errors.join('\n'));
  console.log('PASS: Equipment summary, unified Inventory gear/supplies/equip/page 2, all three skill trees, close; all HD monsters have distinct 192px rear art; no page errors.');
} finally { await browser.close(); }

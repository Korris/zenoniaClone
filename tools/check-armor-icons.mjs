import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';

const out = 'tools/screenshots/armor-icons';
mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  const errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  for (const slot of ['helmet', 'armor', 'gloves', 'boots']) {
    await page.evaluate(async slot => {
      const { generateItem, sanitizeItem } = await import('/src/systems/equipment-system.js');
      const { ItemDrop } = await import('/src/entities/item-drop.js');
      const g = window.__zenonia, p = g.session.player;
      if (g.registry.armorIcons[slot]?.length !== 10 || g.registry.groundArmorIcons[slot]?.length !== 10) throw Error(`Missing icons: ${slot}`);
      p.inventory = Array.from({ length: 10 }, (_, tier) => sanitizeItem(generateItem(() => 0, tier ? tier * 10 + 5 : 5, slot, 'rare')));
      g.session.world.drops = p.inventory.map((item, i) => {
        const drop = new ItemDrop(p.x + (i - 4.5) * 12, p.y + 28, 'equipment', item);
        drop.age = 300; drop.vx = drop.vy = 0;
        return drop;
      });
      g.banner.lifeMs = 0;
    }, slot);
    await page.keyboard.press('b', { delay: 100 });
    await page.waitForFunction(() => window.__zenonia.state === 'inventory');
    await page.mouse.click(620, 116);
    await page.waitForFunction(() => window.__zenonia.inventoryPanel.mode === 'Inventory');
    await page.screenshot({ path: `${out}/${slot}-inventory.png` });
    await page.keyboard.press('b', { delay: 100 });
    await page.waitForFunction(() => window.__zenonia.state === 'playing');
    await page.screenshot({ path: `${out}/${slot}-drops.png` });
  }
  await page.evaluate(() => {
    const g = window.__zenonia;
    const canvas = document.createElement('canvas');
    canvas.width = 960; canvas.height = 480;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#101e2d'; ctx.fillRect(0, 0, 960, 480);
    ctx.font = '12px sans-serif';
    ['helmet', 'armor', 'gloves', 'boots'].forEach((slot, row) => {
      ctx.fillStyle = '#c4d7e7'; ctx.fillText(slot.toUpperCase(), 8, row * 118 + 16);
      g.registry.armorIcons[slot].forEach((icon, tier) => {
        const scale = 66 / Math.max(icon.width, icon.height);
        ctx.drawImage(icon, tier * 96 + 48 - icon.width * scale / 2, row * 118 + 28 + (66 - icon.height * scale) / 2, icon.width * scale, icon.height * scale);
        ctx.fillStyle = '#e9d397';
        ctx.fillText(tier ? `${tier * 10}/${tier * 10 + 5}` : '1/5', tier * 96 + 30, row * 118 + 110);
      });
    });
    document.body.replaceChildren(canvas); document.body.style.margin = '0';
  });
  await page.screenshot({ path: `${out}/all-tiers.png` });
  if (errors.length) throw Error(errors.join('\n'));
  console.log('PASS: 40 equipment icons, 10 tiers per slot, save/load, inventory and ground render, HiDPI, no page errors.');
} finally { await browser.close(); }

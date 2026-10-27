import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';

mkdirSync('tools/screenshots/weapon-icons', { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 640 });
  const errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  page.setDefaultTimeout(10000);
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  console.log('Booted title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  console.log('Class selection');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  console.log('Playing');
  await new Promise(resolve => setTimeout(resolve, 300));
  for (const classId of ['warrior', 'wizard', 'mechanic']) {
    await page.evaluate(async classId => {
      const { generateItem, sanitizeItem } = await import('/src/systems/equipment-system.js');
      const { ItemDrop } = await import('/src/entities/item-drop.js');
      const g = window.__zenonia;
      const player = g.session.player;
      if (g.registry.weaponIcons?.length !== 30) throw Error('Missing weapon atlas');
      if (g.registry.groundWeaponIcons?.length !== 30) throw Error('Missing compact drop icons');
      if (g.registry.groundWeaponIcons.some((icon, i) => icon === g.registry.weaponIcons[i]
        || Math.max(icon.width, icon.height) !== 27)) throw Error('Invalid compact drop dimensions');
      player.classId = classId;
      player.inventory = Array.from({ length: 10 }, (_, tier) =>
        sanitizeItem(generateItem(() => 0, tier ? tier * 10 + 5 : 5, 'weapon', 'rare', classId)));
      player.equipment.weapon = player.inventory[0];
      g.session.world.drops = player.inventory.map((item, i) => new ItemDrop(player.x + (i - 5) * 20, player.y + 28, 'equipment', item));
      for (const [i, slot] of ['helmet', 'armor', 'gloves', 'boots'].entries()) {
        const item = generateItem(() => 0, 5, slot, 'common', classId);
        g.session.world.drops.push(new ItemDrop(player.x + (i - 2) * 20, player.y + 55, 'equipment', item));
      }
    }, classId);
    await page.keyboard.press('b', { delay: 100 });
    await page.waitForFunction(() => window.__zenonia.state === 'inventory');
    await page.screenshot({ path: `tools/screenshots/weapon-icons/${classId}-inventory.png` });
    await page.keyboard.press('b', { delay: 100 });
    await page.waitForFunction(() => window.__zenonia.state === 'playing');
    await page.screenshot({ path: `tools/screenshots/weapon-icons/${classId}-drops.png` });
  }
  const pulse = await page.evaluate(async () => {
    const { drawDrops } = await import('/src/graphics/effects-renderer.js');
    const g = window.__zenonia;
    const canvas = document.createElement('canvas');
    canvas.width = canvas.height = 64;
    const ctx = canvas.getContext('2d');
    const item = { ...g.session.player.inventory[0], rarity: 'common' };
    const samples = [0, 450, 900].map(age => {
      ctx.clearRect(0, 0, 64, 64);
      drawDrops(ctx, [{ kind: 'equipment', item, x: 32, y: 32, bobOffset: 0, age, lifeMs: 10000 }], g.registry, 'mechanic');
      const pixels = ctx.getImageData(0, 0, 64, 64).data;
      let alpha = 0;
      for (let i = 3; i < pixels.length; i += 4) alpha += pixels[i];
      if (ctx.globalAlpha !== 1) throw Error('Drop pulse leaked opacity');
      return alpha;
    });
    if (!(samples[1] > 0 && samples[1] < samples[0] && Math.abs(samples[2] - samples[0]) < samples[0] * 0.02)) throw Error(`Invalid smooth drop pulse: ${samples}`);
    return samples;
  });
  if (errors.length) throw Error(errors.join('\n'));
  console.log('Smooth pulse alpha totals:', pulse.join(', '));
  console.log('PASS: 30 atlas sprites; inventory and drops rendered for all 3 classes; no page errors.');
} finally {
  await browser.close();
}

import puppeteer from 'puppeteer-core';
import { mkdirSync, writeFileSync } from 'node:fs';

const out = 'tools/screenshots/loot-effects';
mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 640 });
  const errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  await page.evaluate(async () => {
    const { ItemDrop } = await import('/src/entities/item-drop.js');
    const { generateItem } = await import('/src/systems/equipment-system.js');
    const g = window.__zenonia, w = g.session.world, p = w.player;
    if (g.registry.lootEffects) throw Error('Loot glow atlas must not be built or loaded');
    g.banner.lifeMs = 0;
    p.inventory = [];
    w.drops = [
      new ItemDrop(p.x - 42, p.y + 30, 'coin', 120),
      new ItemDrop(p.x - 21, p.y + 30, 'hpPotion', 0.3),
      new ItemDrop(p.x, p.y + 30, 'equipment', generateItem(() => 0, 15, 'weapon', 'uncommon', 'warrior')),
      new ItemDrop(p.x + 21, p.y + 30, 'equipment', generateItem(() => 0, 15, 'helmet', 'rare', 'warrior')),
      new ItemDrop(p.x + 42, p.y + 30, 'equipment', generateItem(() => 0, 25, 'weapon', 'epic', 'warrior')),
    ];
    for (const drop of w.drops) { drop.age = 300; drop.vx = drop.vy = 0; }
    window.fxChunks = [];
    window.fxRecorder = new MediaRecorder(document.getElementById('game-canvas').captureStream(30));
    window.fxRecorder.ondataavailable = event => window.fxChunks.push(event.data);
    window.fxRecorder.start();
  });
  await new Promise(resolve => setTimeout(resolve, 600));
  await page.screenshot({ path: `${out}/01-ground-glows.png` });
  await page.evaluate(async () => {
    const { updatePickups } = await import('/src/game/world-update.js');
    const w = window.__zenonia.session.world;
    const drop = w.drops[3];
    w.player.x = drop.x; w.player.y = drop.y;
    updatePickups(w, true);
    if (w.player.inventory.length !== 1 || w.lootPickupEffects?.length) throw Error('Pickup should grant one item without an absorption effect');
  });
  await new Promise(resolve => setTimeout(resolve, 65));
  await page.screenshot({ path: `${out}/02-collection-burst.png` });
  await new Promise(resolve => setTimeout(resolve, 130));
  await page.screenshot({ path: `${out}/03-absorption.png` });
  await new Promise(resolve => setTimeout(resolve, 600));
  await page.evaluate(() => {
    const w = window.__zenonia.session.world;
    if (w.lootPickupEffects?.length || w.player.inventory.length !== 1 || w.drops.length !== 4) throw Error('Loot FX still active or loot duplicated');
  });
  await page.screenshot({ path: `${out}/04-collected.png` });
  const video = await page.evaluate(async () => {
    const stopped = new Promise(resolve => { window.fxRecorder.onstop = resolve; });
    window.fxRecorder.stop(); await stopped;
    const bytes = new Uint8Array(await new Blob(window.fxChunks).arrayBuffer());
    let binary = '';
    for (const byte of bytes) binary += String.fromCharCode(byte);
    return btoa(binary);
  });
  writeFileSync(`${out}/loot-effects.webm`, Buffer.from(video, 'base64'));
  if (errors.length) throw Error(errors.join('\n'));
  console.log('PASS: loot glow/absorption disabled, no FX atlas built, exactly one immediate pickup, no page errors.');
} finally { await browser.close(); }

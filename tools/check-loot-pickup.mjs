import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';

const out = 'tools/screenshots/loot-pickup';
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
    const { Player } = await import('/src/entities/player.js');
    const { generateItem } = await import('/src/systems/equipment-system.js');
    const g = window.__zenonia, w = g.session.world;
    const p = new Player('wizard');
    p.x = w.player.x; p.y = w.player.y;
    g.session.player = w.player = p;
    g.banner.lifeMs = 0;
    w.drops = [
      new ItemDrop(p.x, p.y, 'equipment', generateItem(() => 0, 15, 'weapon', 'rare', 'wizard')),
      new ItemDrop(p.x, p.y, 'hpPotion', 0.3),
      new ItemDrop(p.x, p.y, 'mpPotion', 0.3),
    ];
    for (const drop of w.drops) { drop.age = 300; drop.vx = drop.vy = 0; }
  });
  await page.waitForFunction(() => window.__zenonia.session.world.lootFeedback?.mode === 'preview');
  await new Promise(resolve => setTimeout(resolve, 250));
  await page.screenshot({ path: `${out}/01-nearby.png` });
  await page.keyboard.down('KeyJ');
  await new Promise(resolve => setTimeout(resolve, 550));
  const info = await page.evaluate(() => {
    const w = window.__zenonia.session.world;
    return { drops: w.drops.length, inventory: w.player.inventory.length, mode: w.lootFeedback?.mode, text: w.lootFeedback?.text };
  });
  if (info.drops !== 2 || info.inventory !== 1 || info.mode !== 'picked') throw Error(JSON.stringify(info));
  await page.screenshot({ path: `${out}/02-picked-up.png` });
  await page.keyboard.up('KeyJ');
  await page.waitForFunction(() => window.__zenonia.session.world.lootFeedback?.mode === 'preview');
  await page.keyboard.press('KeyJ', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.session.world.player.potions.hpPotion === 1);
  const remaining = await page.evaluate(() => window.__zenonia.session.world.drops.length);
  if (remaining !== 1) throw Error(`Expected one uncollected item, got ${remaining}`);
  await page.evaluate(async () => {
    const { onMonsterKilledForQuests } = await import('/src/game/quest-flow.js');
    const s = window.__zenonia.session;
    s.quests.active.slimeTrouble = { progress: 2 };
    s.world.lootFeedback = null;
    s.world.noticeQueue = [];
    onMonsterKilledForQuests(s, 'slime');
    if (s.world.lootFeedback.mode !== 'quest' || s.world.lootFeedback.current !== 3) throw Error('Missing quest notice');
  });
  await new Promise(resolve => setTimeout(resolve, 250));
  await page.screenshot({ path: `${out}/03-quest-progress.png` });
  if (errors.length) throw Error(errors.join('\n'));
  console.log('PASS: one right-side card for nearby/pickup/quest; one item per press; holding attack does not collect the stack; no page errors.');
} finally { await browser.close(); }

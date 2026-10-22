// Headless smoke test with the locally installed Chrome: boots the game, starts a new game,
// performs a combo hit next to a slime, screenshots frozen frames, and reports console errors.
// Usage: node tools/browser-check.mjs [outDir]   (server must run on http://127.0.0.1:5173)
import puppeteer from 'puppeteer-core';
import { existsSync, mkdirSync } from 'node:fs';
import path from 'node:path';

const OUT = process.argv[2] || 'tools/screenshots';
const CHROME_PATHS = [
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
];
const executablePath = CHROME_PATHS.find((p) => existsSync(p));
if (!executablePath) throw new Error('No Chrome/Edge found');
mkdirSync(OUT, { recursive: true });

const browser = await puppeteer.launch({ executablePath, headless: true, args: ['--window-size=960,640'] });
const page = await browser.newPage();
await page.setViewport({ width: 960, height: 640 });
const errors = [];
page.on('pageerror', (e) => errors.push(String(e)));
page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });

process.on('uncaughtException', (e) => { console.log('FAILED', String(e).slice(0, 200)); console.log('errors', JSON.stringify(errors)); process.exit(1); });
process.on('unhandledRejection', (e) => { console.log('FAILED', String(e).slice(0, 200)); console.log('errors', JSON.stringify(errors)); process.exit(1); });

await page.goto('http://127.0.0.1:5173/index.html', { waitUntil: 'load' });
await page.evaluate(() => localStorage.clear());
await page.reload({ waitUntil: 'load' });
await page.waitForFunction(() => window.__zenonia && window.__zenonia.state === 'title', { timeout: 15000 });
await page.screenshot({ path: path.join(OUT, '01-title.png') });

await page.keyboard.press('Enter');
await page.waitForFunction(() => window.__zenonia.state === 'playing');
await new Promise((r) => setTimeout(r, 400));
await page.screenshot({ path: path.join(OUT, '15-village.png') }); // New Game now starts in Oakhaven Village

// Village layout walk-checks: put the hero's feet on a tile, hold a direction key for `ms`, and
// report the feet tile afterwards -- real input + real collision (tilemap.moveBox), so a fence,
// the river edge or an open gate behave exactly as they do for a player. Hero speed is 80 px/s
// (5 tiles/s), so 700 ms covers ~3.5 tiles when nothing blocks.
const feetAt = (tx, ty) => ({ x: (tx + 0.5) * 16, y: (ty + 1) * 16 - 2 });
async function walk(tx, ty, key, ms, shot) {
  await page.evaluate(({ x, y }) => { const p = window.__zenonia.session.player; p.x = x; p.y = y; p.dash = null; p.combo.reset(); }, feetAt(tx, ty));
  await new Promise((r) => setTimeout(r, 80));
  if (shot) await page.screenshot({ path: path.join(OUT, shot) });
  await page.keyboard.down(key);
  await new Promise((r) => setTimeout(r, ms));
  await page.keyboard.up(key);
  await new Promise((r) => setTimeout(r, 80));
  return page.evaluate(() => { const p = window.__zenonia.session.player; return { tx: Math.floor(p.x / 16), ty: Math.floor((p.y - 4) / 16), map: window.__zenonia.session.world.mapId }; });
}
// All 10 villagers present, each rendering from its HD sheet (not the pixel fallback), on its tile.
const npcs = await page.evaluate(() => {
  const g = window.__zenonia; const w = g.session.world;
  return w.npcs.map((n) => ({ id: n.id, tx: Math.floor(n.x / 16), ty: Math.floor((n.y - 2) / 16), hd: Boolean(g.registry?.npcsHd?.[n.id]) }));
});
console.log('npcs', JSON.stringify(npcs));
if (npcs.length !== 10 || npcs.some((n) => !n.hd)) errors.push('village NPC check failed: ' + JSON.stringify(npcs));

const village = {
  plazaUp: await walk(16, 17, 'ArrowUp', 700, '16-village-plaza.png'), // through the (still empty) well spot
  plazaRight: await walk(14, 14, 'ArrowRight', 1000), // along the plaza's top row, past the elder
  farmGate: await walk(12, 20, 'ArrowLeft', 900, '17-village-farm.png'), // in through the east gate
  farmFence: await walk(12, 22, 'ArrowLeft', 700), // below the gate: the fence post at (10,22) must stop us at col 11
  market: await walk(6, 14, 'ArrowLeft', 500, '17b-village-market.png'), // west end of the crossbar, by Dale
  bridge: await walk(30, 15, 'ArrowRight', 1200, '18-village-bridge.png'), // across to the east bank
  dock: await walk(30, 20, 'ArrowRight', 1200, '19-village-dock.png'), // planks end at col 32, water at 33
  north: await walk(16, 9, 'ArrowUp', 300, '20-village-north.png'), // residential lane, brown cottage + Mia
  gate: await walk(16, 20, 'ArrowDown', 200, '21-village-gate.png'), // south gate: Rook, Lena, signpost, portal
};
// Talk to the elder: HD portrait in the dialog box, then close it (E pages through the lines).
await page.evaluate(({ x, y }) => { const p = window.__zenonia.session.player; p.x = x; p.y = y; }, feetAt(17, 15));
await new Promise((r) => setTimeout(r, 120));
await page.keyboard.press('KeyE');
await page.waitForFunction(() => window.__zenonia.state === 'dialog', { timeout: 3000 }).catch(() => errors.push('dialog did not open near the elder'));
await new Promise((r) => setTimeout(r, 900));
await page.screenshot({ path: path.join(OUT, '22-village-dialog.png') });
for (let i = 0; i < 6 && (await page.evaluate(() => window.__zenonia.state)) === 'dialog'; i++) {
  await page.keyboard.press('KeyE');
  await new Promise((r) => setTimeout(r, 250));
}
village.portal = await walk(16, 21, 'ArrowDown', 700); // start tile -> south gate portal -> Verdant Meadow
console.log('village', JSON.stringify(village));
const villageOk = village.plazaUp.ty <= 14 && village.plazaRight.tx === 17 && village.farmGate.tx <= 9
  && village.farmFence.tx === 11 && village.bridge.tx >= 34 && village.dock.tx === 32 && village.portal.map === 'verdantMeadow';
if (!villageOk) errors.push('village walk-check failed: ' + JSON.stringify(village));
await new Promise((r) => setTimeout(r, 300));
await page.screenshot({ path: path.join(OUT, '02-meadow.png') });

// HD ground tiles + HD props: jump to the cave, castle and desert maps to check the HD tile/prop
// sets away from the meadow, then jump back to the meadow (the rest of this script assumes
// Verdant Meadow).
await page.evaluate(() => { window.__zenonia.session.enterMap('darkCave'); });
await new Promise((r) => setTimeout(r, 300));
await page.screenshot({ path: path.join(OUT, '12-hd-cave.png') });
await page.evaluate(() => { window.__zenonia.session.enterMap('ruinedCastle'); });
await new Promise((r) => setTimeout(r, 300));
await page.screenshot({ path: path.join(OUT, '13-hd-castle.png') });
await page.evaluate(() => { window.__zenonia.session.enterMap('scorchedDesert'); });
await new Promise((r) => setTimeout(r, 300));
await page.screenshot({ path: path.join(OUT, '14-hd-desert.png') });
await page.evaluate(() => { window.__zenonia.session.enterMap('verdantMeadow'); });
await new Promise((r) => setTimeout(r, 300));

// teleport next to a slime, then chain the 3-hit combo, freezing a frame mid-swing each time
await page.evaluate(() => {
  const g = window.__zenonia; const p = g.session.player; const w = g.session.world;
  const m = w.monsters.find((x) => x.type === 'slime' && !x.dead);
  if (m) { p.x = m.x - 26; p.y = m.y; m.ai.state = 'idle'; m.ai.timer = 9e9; m.hp = 9999; m.maxHp = 9999; }
  p.dir = 'right';
});
for (const step of [1, 2, 3]) {
  await page.keyboard.press('KeyJ');
  await new Promise((r) => setTimeout(r, step === 3 ? 150 : 80));
  const info = await page.evaluate(() => {
    const g = window.__zenonia; const w = g.session.world; w.hitStopMs = 60000;
    return { step: g.session.player.combo.step, progress: Number(g.session.player.combo.phaseProgress.toFixed(2)), fx: w.effects.map((e) => e.spriteKey) };
  });
  console.log('combo', JSON.stringify(info));
  await page.screenshot({ path: path.join(OUT, `03-combo-${step}.png`) });
  await page.evaluate(() => { window.__zenonia.session.world.hitStopMs = 0; });
  await new Promise((r) => setTimeout(r, step === 3 ? 500 : 200));
}

// double-tap dash (free, no MP)
await page.evaluate(() => { const g = window.__zenonia; g.session.player.freeDashCooldown = 0; g.session.player.combo.reset(); });
await page.keyboard.press('ArrowRight');
await new Promise((r) => setTimeout(r, 90));
await page.keyboard.press('ArrowRight');
await new Promise((r) => setTimeout(r, 30));
console.log('doubleTapDash', JSON.stringify(await page.evaluate(() => { const p = window.__zenonia.session.player; return { dashing: Boolean(p.dash), free: p.dash ? p.dash.damageMult === 0 : null, cooldown: p.freeDashCooldown }; })));
await page.evaluate(() => { window.__zenonia.session.world.hitStopMs = 60000; });
await page.screenshot({ path: path.join(OUT, '04b-double-tap-dash.png') });
await page.evaluate(() => { window.__zenonia.session.world.hitStopMs = 0; });
await new Promise((r) => setTimeout(r, 400));

// dash with trail
await page.evaluate(() => { const p = window.__zenonia.session.player; p.skills.dashStrike = 1; p.level = 10; p.recomputeStats(); p.mp = p.maxMp; });
await page.keyboard.press('KeyI');
await new Promise((r) => setTimeout(r, 90));
await page.evaluate(() => { window.__zenonia.session.world.hitStopMs = 60000; });
await page.screenshot({ path: path.join(OUT, '04-dash.png') });
await page.evaluate(() => { window.__zenonia.session.world.hitStopMs = 0; });

// skill tree: open (Tab), screenshot, upgrade a node with Enter, bind + cast it from the hotbar
await page.evaluate(() => {
  const p = window.__zenonia.session.player;
  p.dash = null; p.combo.reset(); // cancel any dash/combo left over from the previous steps
  p.level = 20; p.skillPoints = 5; p.recomputeStats(); p.mp = p.maxMp; p.hp = p.maxHp;
});
await page.keyboard.press('Tab');
await page.waitForFunction(() => window.__zenonia.state === 'skills');
await new Promise((r) => setTimeout(r, 120));
await page.screenshot({ path: path.join(OUT, '06-skill-tree.png') });

await page.keyboard.press('Enter'); // upgrade the selected node (Slash Wave, top-left by default)
await new Promise((r) => setTimeout(r, 120));
const upgraded = await page.evaluate(() => window.__zenonia.session.player.skills.slashWave || 0);
console.log('skillUpgraded', JSON.stringify({ slashWaveLevel: upgraded }));
await page.screenshot({ path: path.join(OUT, '07-skill-tree-upgraded.png') });

await page.keyboard.press('Tab'); // close
await page.waitForFunction(() => window.__zenonia.state === 'playing');
await new Promise((r) => setTimeout(r, 100));

await page.keyboard.press('KeyK'); // cast whatever auto-bound to hotbar slot 0 (Slash Wave)
await new Promise((r) => setTimeout(r, 90));
const cast = await page.evaluate(() => { const w = window.__zenonia.session.world; return { hotbar: w.player.hotbar, projectiles: w.projectiles.length, mp: w.player.mp }; });
console.log('hotbarCast', JSON.stringify(cast));
await page.screenshot({ path: path.join(OUT, '08-hotbar-cast.png') });

// inventory: force-drop an epic weapon at the hero's feet, walk over it, open the bag (B),
// equip it with Enter, and confirm player.atk went up.
await page.evaluate(async () => {
  const g = window.__zenonia;
  const { ItemDrop } = await import('./src/entities/item-drop.js');
  const { generateItem } = await import('./src/systems/equipment-system.js');
  const p = g.session.player;
  const w = g.session.world;
  p.dash = null; p.combo.reset(); p.hp = p.maxHp;
  const item = generateItem(Math.random, 6, 'weapon', 'epic');
  w.drops.push(new ItemDrop(p.x, p.y, 'equipment', item));
});
await new Promise((r) => setTimeout(r, 350)); // clear the drop's pickup-immunity window + let updatePickups run
console.log('pickup', JSON.stringify(await page.evaluate(() => {
  const w = window.__zenonia.session.world;
  return { inventory: w.player.inventory.length, hasUnseenItem: w.player.hasUnseenItem, drops: w.drops.length };
})));

await page.keyboard.press('KeyB');
await page.waitForFunction(() => window.__zenonia.state === 'inventory');
await new Promise((r) => setTimeout(r, 120));
await page.screenshot({ path: path.join(OUT, '09-inventory.png') });

const beforeAtk = await page.evaluate(() => window.__zenonia.session.player.atk);
await page.keyboard.press('Enter'); // equip the selected bag item (first slot, our epic weapon)
await new Promise((r) => setTimeout(r, 100));
const afterAtk = await page.evaluate(() => window.__zenonia.session.player.atk);
console.log('equip', JSON.stringify({ beforeAtk, afterAtk, weapon: await page.evaluate(() => window.__zenonia.session.player.equipment.weapon?.name) }));
await page.screenshot({ path: path.join(OUT, '10-equipped.png') });

await page.keyboard.press('KeyB');
await page.waitForFunction(() => window.__zenonia.state === 'playing');

// death pose
await page.evaluate(() => { const g = window.__zenonia; g.session.player.hp = 1; g.session.player.invulnTimer = 0; g.session.player.takeDamage(999); });
await new Promise((r) => setTimeout(r, 700));
await page.screenshot({ path: path.join(OUT, '11-death.png') });

console.log('state', await page.evaluate(() => window.__zenonia.state));
console.log('errors', JSON.stringify(errors));
await browser.close();

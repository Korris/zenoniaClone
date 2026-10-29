// Browser check: the mechanic holds its equipped gun (paper-doll) and the offhand robot floats beside it. Captures
// idle/walk/attack1-3/dash in 4 directions for tiers 0/4/9 and enhance +0/+7/+15 into tools/screenshots/mechanic-art/ (raw/ =
// single clips, sheet-*.png = stitched by tools/stitch-wizard-held-staff.py tools/screenshots/mechanic-art). Needs the dev
// server (port 5173); starts one itself when nothing answers. Also checks the empty-offhand case (no robot).
import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
import { spawn } from 'node:child_process';

const URL = process.env.MECHANIC_ART_URL || 'http://127.0.0.1:5173';
const out = 'tools/screenshots/mechanic-art';
mkdirSync(`${out}/raw`, { recursive: true });

let server = null;
try { await fetch(URL); } catch {
  server = spawn('npx', ['serve', '.', '-l', '5173'], { shell: true, stdio: 'ignore' });
  for (let i = 0; i < 40; i++) { try { await fetch(URL); break; } catch { await new Promise((r) => setTimeout(r, 500)); } }
}

const CONFIGS = [ // [label, itemLevel (tier = level/10), enhance, offhand?]
  ['t0+0', 5, 0, true], ['t4+0', 45, 0, true], ['t9+0', 95, 0, true], ['t4+7', 45, 7, true],
  ['t4+15', 45, 15, true], ['t9+15', 95, 15, true], ['t4-norobot', 45, 0, false],
];
const DIRS = ['down', 'up', 'left', 'right'];
const POSES = [
  ['idle', { moving: false, idleTimer: 0 }],
  ['walk0', { moving: true, animTimer: 0 }], ['walk1', { moving: true, animTimer: 140 }],
  ['walk2', { moving: true, animTimer: 280 }], ['walk3', { moving: true, animTimer: 420 }],
  ['atk1a', { attack: [1, 0.3] }], ['atk1b', { attack: [1, 0.7] }], ['atk2', { attack: [2, 0.5] }],
  ['atk3a', { attack: [4, 0.3] }], ['atk3b', { attack: [4, 0.8] }], ['dash', { dash: true, animTimer: 0 }],
];

const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage(), errors = [];
  page.on('pageerror', (e) => errors.push(String(e)));
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  await page.goto(URL);
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  for (let i = 0; i < 2; i++) await page.keyboard.press('ArrowRight', { delay: 60 }); // 3rd card = mechanic
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  await page.evaluate(() => {
    const g = window.__zenonia;
    g.update = () => {}; // freeze simulation; the render loop keeps drawing the poses we set
    g.banner.lifeMs = 0;
    if (g.session.player.classId !== 'mechanic') throw Error(`expected mechanic, got ${g.session.player.classId}`);
    if (!g.registry.heldGear?.mechanic) throw Error('registry.heldGear.mechanic missing (body sheets / overlays not loaded)');
    if (!g.registry.companions?.mechanic) throw Error('registry.companions.mechanic missing (robot art not loaded)');
  });
  const total = { shots: 0 };
  for (const [label, itemLevel, enhance, robot] of CONFIGS) {
    const full = ['t0+0', 't4+0', 't9+0'].includes(label);
    for (const dir of DIRS) {
      for (const [name, patch] of POSES) {
        if (!full && !['idle', 'walk1', 'atk1b', 'atk3a'].includes(name)) continue;
        await page.evaluate((cfg) => {
          const p = window.__zenonia.session.player, c = p.combo;
          p.equipment.weapon = { slot: 'weapon', itemLevel: cfg.itemLevel, enhance: cfg.enhance, templateId: 'gun', stats: { atk: 1 } };
          p.equipment.offhand = cfg.robot ? { slot: 'offhand', itemLevel: cfg.itemLevel, enhance: cfg.enhance, templateId: 'robot', stats: { def: 1 } } : null;
          p.dir = cfg.dir; p.dash = cfg.patch.dash ? { dirX: 0, dirY: 0 } : null; p.dead = false; p.hitFlashTimer = 0; p.ultimateCast = null;
          p.moving = !!cfg.patch.moving; p.animTimer = cfg.patch.animTimer || 0; p.idleTimer = cfg.patch.idleTimer || 0;
          if (cfg.patch.attack) {
            c.step = Math.min(cfg.patch.attack[0], c.config.hits.length);
            const hit = c.config.hits[c.step - 1];
            c.state = 'windup';
            c.timer = cfg.patch.attack[1] * (hit.windupMs + hit.activeMs + hit.recoverMs);
          } else { c.state = 'idle'; c.step = 0; }
        }, { itemLevel, enhance, robot, dir, patch });
        await new Promise((r) => setTimeout(r, 450));
        await page.screenshot({ path: `${out}/raw/${label}-${name}-${dir}.png`, clip: { x: 385, y: 240, width: 190, height: 150 } });
        total.shots++;
      }
    }
  }
  if (errors.length) throw Error(errors.join('\n'));
  console.log(`PASS: ${total.shots} mechanic clips saved to ${out}/raw; no page errors.`);
} finally {
  await browser.close();
  server?.kill();
}

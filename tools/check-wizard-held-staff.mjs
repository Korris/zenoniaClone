// Browser check: the wizard holds its equipped staff (paper-doll). Captures idle/walk/attack in 4 directions for
// staff tiers 0/5/9 and enhance +0/+7/+11/+15 into tools/screenshots/wizard-held-staff/ (raw/ = single clips,
// sheet-*.png = stitched by tools/stitch-wizard-held-staff.py). Needs the dev server (npm start, port 5173);
// starts one itself when nothing answers.
import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
import { spawn } from 'node:child_process';

const URL = process.env.WIZARD_CHECK_URL || 'http://127.0.0.1:5173';
const out = 'tools/screenshots/wizard-held-staff';
mkdirSync(`${out}/raw`, { recursive: true });

let server = null;
try { await fetch(URL); } catch {
  server = spawn('npx', ['serve', '.', '-l', '5173'], { shell: true, stdio: 'ignore' });
  for (let i = 0; i < 40; i++) { try { await fetch(URL); break; } catch { await new Promise((r) => setTimeout(r, 500)); } }
}

const CONFIGS = [ // [label, itemLevel (tier = level/10), enhance]
  ['t0+0', 5, 0], ['t5+0', 55, 0], ['t9+0', 95, 0], ['t5+7', 55, 7], ['t5+11', 55, 11], ['t5+15', 55, 15],
];
const DIRS = ['down', 'up', 'left', 'right'];
const POSES = [ // [name, patch applied to the frozen player]
  ['idle', { moving: false, idleTimer: 0 }],
  ['walk0', { moving: true, animTimer: 0 }], ['walk1', { moving: true, animTimer: 140 }],
  ['walk2', { moving: true, animTimer: 280 }], ['walk3', { moving: true, animTimer: 420 }],
  ['atk1a', { attack: [1, 0.3] }], ['atk1b', { attack: [1, 0.7] }], ['atk2', { attack: [2, 0.5] }],
  ['atk3a', { attack: [9, 0.4] }], ['atk3b', { attack: [9, 0.85] }],
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
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  await page.evaluate(() => {
    const g = window.__zenonia;
    g.update = () => {}; // freeze simulation; the render loop keeps drawing the poses we set
    g.banner.lifeMs = 0;
    g.session.player.classId = 'wizard';
    if (!g.registry.heldGear?.wizard) throw Error('registry.heldGear.wizard missing (weaponless art / overlays not loaded)');
  });
  const pos = await page.evaluate(() => { const p = window.__zenonia.session.player; return { x: p.x, y: p.y }; });
  const total = { shots: 0 };
  for (const [label, itemLevel, enhance] of CONFIGS) {
    const full = label === 't0+0' || label === 't5+0' || label === 't9+0';
    for (const dir of DIRS) {
      for (const [name, patch] of POSES) {
        if (!full && !['idle', 'walk1', 'atk1b', 'atk3a'].includes(name)) continue; // enhance configs: fewer poses
        await page.evaluate((cfg) => {
          const p = window.__zenonia.session.player, c = p.combo;
          p.equipment.weapon = { slot: 'weapon', itemLevel: cfg.itemLevel, enhance: cfg.enhance, templateId: 'staff', stats: { atk: 1 } };
          p.dir = cfg.dir; p.dash = null; p.dead = false; p.hitFlashTimer = 0; p.ultimateCast = null;
          p.moving = !!cfg.patch.moving; p.animTimer = cfg.patch.animTimer || 0; p.idleTimer = cfg.patch.idleTimer || 0;
          if (cfg.patch.attack) { // 'windup' branch reports timer / total, so timer = progress * total
            c.step = Math.min(cfg.patch.attack[0], c.config.hits.length);
            const hit = c.config.hits[c.step - 1];
            c.state = 'windup';
            c.timer = cfg.patch.attack[1] * (hit.windupMs + hit.activeMs + hit.recoverMs);
          } else { c.state = 'idle'; c.step = 0; }
        }, { itemLevel, enhance, dir, patch });
        await new Promise((r) => setTimeout(r, 90));
        await page.screenshot({ path: `${out}/raw/${label}-${name}-${dir}.png`, clip: { x: 405, y: 262, width: 150, height: 120 } });
        total.shots++;
      }
    }
  }
  if (errors.length) throw Error(errors.join('\n'));
  console.log(`PASS: ${total.shots} wizard held-staff clips saved to ${out}/raw (player at ${pos.x.toFixed(0)},${pos.y.toFixed(0)}); no page errors.`);
} finally {
  await browser.close();
  server?.kill();
}

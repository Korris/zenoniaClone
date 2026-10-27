import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
const out = 'tools/screenshots/monster-attacks';
mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage(), errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter');
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('Enter');
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  await page.evaluate(async () => {
    const { Monster } = await import('/src/entities/monster.js');
    const g = window.__zenonia; g.update = () => {}; g.session.enterMap('frostHollow'); g.banner.lifeMs = 0;
    const w = g.session.world, p = w.player;
    const types = ['frostSlime', 'iceGoblin', 'spectralWolf', 'frostfangKing'];
    w.monsters = types.map((type, i) => new Monster(type, 90 + i * 43, 100));
    for (const m of w.monsters) {
      const hd = g.registry.monstersHd[m.type];
      if (!hd?.attack || Object.keys(hd.attack).length !== 4) throw Error(`Missing attacks: ${m.type}`);
      for (const frames of Object.values(hd.attack)) for (const frame of frames) {
        if (frame.width !== 256 || frame.height !== 256) throw Error('Wrong attack cell size');
        const data = frame.getContext('2d').getImageData(0, 0, 256, 256).data;
        let pixels = 0;
        for (let i = 3; i < data.length; i += 4) if (data[i] > 40) pixels++;
        if (pixels < 500) throw Error(`Empty attack frame: ${m.type}`);
        for (let i = 0; i < 256; i++) {
          if (data[(i * 256) * 4 + 3] || data[(i * 256 + 255) * 4 + 3]
            || data[i * 4 + 3] || data[(255 * 256 + i) * 4 + 3]) throw Error(`Clipped frame: ${m.type}`);
        }
      }
    }
    p.x = 145; p.y = 140; p.dir = 'up';
    g.session.camera.snapTo(150, 105);
  });
  for (const dir of ['down', 'up', 'left', 'right']) for (const stage of ['windup', 'strike', 'recovery']) {
    await page.evaluate(({ dir, stage }) => {
      for (const m of window.__zenonia.session.world.monsters) {
        m.dir = dir; m.hitFlashTimer = 0;
        m.ai.state = stage === 'windup' ? 'windup' : 'cooldown';
        m.ai.timer = stage === 'windup' ? m.def.attackWindupMs / 2
          : m.def.attackCooldownMs - (stage === 'recovery' ? 150 : 0);
      }
    }, { dir, stage });
    await new Promise(r => setTimeout(r, 100));
    await page.screenshot({ path: `${out}/${dir}-${stage}.png` });
  }
  if (errors.length) throw Error(errors.join('\n'));
  console.log('PASS: four Frost Hollow monsters, 64 attack frames, all directions and phases, no clipping or page errors.');
} finally { await browser.close(); }

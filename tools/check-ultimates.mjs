import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
const out = 'tools/screenshots/ultimates'; mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage(), errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  page.on('console', m => { if (m.type() === 'warn' && /earthBreak_|rocketBarrage|ultimateIcons/.test(m.text())) errors.push(m.text()); });
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  for (const [classId, id, stages] of [
    ['warrior', 'bladeStorm', [['charge', 300], ['sweep', 680], ['slam', 1250], ['fissures', 2450]]],
    ['mechanic', 'carpetBomb', [['lock', 250], ['launch', 750], ['descending', 1500], ['chain', 2000]]],
  ]) {
    await page.evaluate(async ({ classId, id }) => {
      const { tryCastSkill } = await import('/src/systems/skill-cast-system.js');
      const { Monster } = await import('/src/entities/monster.js');
      const g = window.__zenonia; g.update = () => {}; g.session.startNewGame(classId);
      g.session.enterMap('frostHollow'); g.banner.lifeMs = 0;
      const w = g.session.world, p = w.player; w.hitRoll = () => 0.5;
      p.x = 160; p.y = 224; p.dir = 'right'; p.skills[id] = 1; p.hotbar[0] = id;
      g.session.camera.snapTo(p.x + 30, p.y - 25);
      w.monsters = [[192, 220], [210, 235], [200, 246]].map(([x, y]) => {
        const m = new Monster('frostSlime', x, y); m.hp = m.maxHp = 1000; m.animTimer = 0; return m;
      });
      if (!tryCastSkill(w, id).ok) throw Error(`Cannot cast ${id}`);
      window.__ultimate = w.effects.find(e => e.kind === (classId === 'warrior' ? 'earthBreak' : 'rocketBarrage'));
      const frames = g.registry.effects[window.__ultimate.kind];
      if (!frames || Object.values(frames).some(strip => strip.length !== 6)) throw Error('Missing ultimate frames');
      if (classId === 'warrior') for (const strip of Object.values(frames)) for (const f of strip) {
        if (f.width !== 384 || !f.effectAnchor) throw Error('Still using raw Earth Break atlas cuts');
        const ctx = f.getContext('2d'), data = ctx.getImageData(0, 0, f.width, f.height).data;
        for (let x = 0; x < f.width; x++) if (data[x * 4 + 3] || data[((f.height - 1) * f.width + x) * 4 + 3]) throw Error('Frame touches vertical canvas edge');
      }
    }, { classId, id });
    for (const [stage, ms] of stages) {
      await page.evaluate(ms => {
        const g = window.__zenonia, e = window.__ultimate; g.session.world.texts = [];
        e.update(ms - e.age);
        for (const m of g.session.world.monsters) {
          m.hitFlashTimer = 0;
          if (e.kind === 'earthBreak' && ms >= 1000) m.airborneMs = Math.max(0, 1500 - (ms - 1000));
        }
      }, ms);
      await new Promise(resolve => setTimeout(resolve, 100));
      await page.screenshot({ path: `${out}/${classId}-${stage}.png` });
    }
    const damage = await page.evaluate(() => window.__zenonia.session.world.monsters.map(m => m.hp));
    if (!damage.some(hp => hp < 1000)) throw Error(`No damage ${classId}`);
    await page.evaluate(() => {
      const w = window.__zenonia.session.world; window.__ultimate.update(10000); w.removeDead();
      if (w.effects.some(e => e.kind === window.__ultimate.kind)) throw Error('Ultimate leaked');
    });
    console.log(JSON.stringify({ classId, damage, pass: true }));
  }
  await page.evaluate(async () => {
    const { hitPlayer, damageMonster } = await import('/src/systems/combat-system.js');
    const g = window.__zenonia, w = g.session.world, p = w.player;
    p.hp = p.maxHp; p.invulnTimer = 0; w.hitRoll = () => 0.99;
    hitPlayer(w, 10, p.x - 20, p.y);
    if (p.hp !== p.maxHp || p.hitFlashTimer > 0) throw Error('Incoming miss applied damage/hurt');
    const m = w.monsters[0], hp = m.hp; damageMonster(w, m, 10, 0, 0, '#fff', false, true);
    if (m.hp !== hp) throw Error('Outgoing miss applied damage');
    w.hitRoll = () => 0.5; hitPlayer(w, 10, p.x - 20, p.y);
    if (p.hitFlashTimer !== 220 || p.knockback.timer !== 100) throw Error('Missing hurt/knockback');
  });
  await new Promise(resolve => setTimeout(resolve, 100));
  await page.screenshot({ path: `${out}/hurt-and-miss.png` });
  if (errors.length) throw Error(errors.join('\n'));
  console.log('PASS: independent Earth Break frames, Rocket Barrage, lifetime cleanup, incoming/outgoing MISS and hurt; no page errors.');
} finally { await browser.close(); }

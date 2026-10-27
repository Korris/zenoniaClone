import puppeteer from 'puppeteer-core';
import { mkdirSync } from 'node:fs';
const out = 'tools/screenshots/ice-storm'; mkdirSync(out, { recursive: true });
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage(), errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('ArrowRight', { delay: 100 });
  await page.keyboard.press('Enter', { delay: 100 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  const report = await page.evaluate(async () => {
    const { upgradeSkill, bindSkillToSlot } = await import('/src/systems/skill-system.js');
    const { tryCastSkill } = await import('/src/systems/skill-cast-system.js');
    const { Monster } = await import('/src/entities/monster.js');
    const g = window.__zenonia; g.update = () => {};
    const p = g.session.player;
    if (p.classId !== 'wizard' || p.skillPoints !== 75 || !p.skillTestMode) throw Error('New Game preview not enabled');
    for (const [id, count] of [['iceShard', 2], ['frostArmor', 1], ['frostNova', 3], ['glacialHeart', 2], ['blizzard', 1]]) {
      for (let i = 0; i < count; i++) if (!upgradeSkill(p, id).ok) throw Error(`Cannot learn ${id} at level 1`);
    }
    if (!bindSkillToSlot(p, 'blizzard', 0).ok) throw Error('Cannot bind storm');
    g.session.enterMap('frostHollow'); g.banner.lifeMs = 0;
    p.x = 190; p.y = 245; p.dir = 'right'; p.mp = p.maxMp;
    g.session.camera.snapTo(p.x + 25, p.y - 25);
    const w = g.session.world;
    w.monsters = [new Monster('frostSlime', 218, 232), new Monster('frostSlime', 240, 252), new Monster('frostSlime', 211, 263)];
    for (const m of w.monsters) { m.hp = m.maxHp = 500; m.animTimer = 0; }
    if (!tryCastSkill(w, 'blizzard').ok) throw Error('Cast failed');
    const storm = w.effects.find(e => e.kind === 'iceStorm');
    if (!g.registry.effects.iceStorm?.impact?.length) throw Error('VFX missing');
    window.__storm = storm;
    return { points: p.skillPoints, startingPoints: 75, level: p.level, frames: Object.values(g.registry.effects.iceStorm).map(a => a.length) };
  });
  for (const [name, ms] of [['cast', 350], ['falling', 580], ['impact', 1200], ['fading', 2200]]) {
    await page.evaluate(ms => {
      const g = window.__zenonia, storm = window.__storm;
      storm.update(ms - storm.age);
    }, ms);
    await new Promise(resolve => setTimeout(resolve, 100));
    await page.screenshot({ path: `${out}/${name}.png` });
  }
  const damage = await page.evaluate(() => window.__zenonia.session.world.monsters.map(m => m.hp));
  if (!damage.some(hp => hp < 500)) throw Error('No area damage');
  await page.evaluate(() => {
    const g = window.__zenonia; window.__storm.update(1000); g.session.world.removeDead();
    if (g.session.world.effects.some(e => e.kind === 'iceStorm')) throw Error('Storm leaked');
  });
  if (errors.length) throw Error(errors.join('\n'));
  console.log(JSON.stringify({ pass: true, ...report, damage, errors }));
} finally { await browser.close(); }

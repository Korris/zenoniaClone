import puppeteer from 'puppeteer-core';
import { mkdirSync, writeFileSync } from 'node:fs';
const browser = await puppeteer.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 2 });
  await page.goto('http://127.0.0.1:5173');
  await page.waitForFunction(() => window.__zenonia?.state === 'title');
  await page.keyboard.press('Enter', { delay: 80 });
  await page.waitForFunction(() => window.__zenonia.state === 'classSelect');
  await page.keyboard.press('Enter', { delay: 80 });
  await page.waitForFunction(() => window.__zenonia.state === 'playing');
  const report = await page.evaluate(async () => {
    const { tryCastSkill } = await import('/src/systems/skill-cast-system.js');
    const { configureCanvasDisplay } = await import('/src/core/canvas-display.js');
    const g = window.__zenonia, canvas = document.getElementById('game-canvas'), ctx = canvas.getContext('2d');
    const render = g.render.bind(g); g.update = () => {}; g.render = () => {};
    const seen = new WeakSet(); let imageCount = 0, imageBytes = 0;
    function walk(value) {
      if (!value || typeof value !== 'object' || seen.has(value)) return;
      seen.add(value);
      if (value instanceof HTMLCanvasElement || value instanceof HTMLImageElement) { imageCount++; imageBytes += value.width * value.height * 4; return; }
      for (const item of Object.values(value)) walk(item);
    }
    walk(g.registry);
    let calls = 0, area = 0;
    const draw = ctx.drawImage.bind(ctx);
    ctx.drawImage = (...args) => {
      calls++;
      const t = ctx.getTransform(), w = args.length === 9 ? args[7] : args.length === 5 ? args[3] : args[0].width;
      const h = args.length === 9 ? args[8] : args.length === 5 ? args[4] : args[0].height;
      area += Math.abs(w * h * (t.a * t.d - t.b * t.c));
      return draw(...args);
    };
    const cases = [];
    for (const dpr of [1, 2]) for (const kind of ['idle', 'earthBreak', 'rocketBarrage', 'iceStorm']) {
      Object.defineProperty(window, 'devicePixelRatio', { configurable: true, value: dpr });
      const classId = kind === 'rocketBarrage' ? 'mechanic' : kind === 'iceStorm' ? 'wizard' : 'warrior';
      const id = kind === 'earthBreak' ? 'bladeStorm' : kind === 'rocketBarrage' ? 'carpetBomb' : 'blizzard';
      g.session.startNewGame(classId); g.session.enterMap('frostHollow'); g.banner.lifeMs = 0;
      const w = g.session.world, p = w.player; p.x = 160; p.y = 224; p.dir = 'right';
      w.monsters = []; g.session.camera.snapTo(p.x + 30, p.y - 25);
      if (kind !== 'idle') { p.skills[id] = 1; tryCastSkill(w, id); w.effects[0].update(kind === 'earthBreak' ? 1250 : kind === 'rocketBarrage' ? 2000 : 1200); }
      const times = [], gaps = []; let last = 0;
      for (let i = 0; i < 65; i++) {
        await new Promise(requestAnimationFrame);
        configureCanvasDisplay(canvas, 960, 640, dpr);
        calls = area = 0;
        const start = performance.now(); render(ctx); const end = performance.now();
        if (i >= 5) { times.push(end - start); if (last) gaps.push(start - last); }
        last = start;
      }
      times.sort((a, b) => a - b); gaps.sort((a, b) => a - b);
      cases.push({ kind, dpr, renderMedianMs: +times[30].toFixed(2), renderP95Ms: +times[57].toFixed(2), frameMedianMs: +gaps[30].toFixed(2), frameP95Ms: +gaps[57].toFixed(2), drawCalls: calls, drawnMegapixels: +(area / 1e6).toFixed(2) });
    }
    return { imageCount, registryDecodedMiB: +(imageBytes / 1048576).toFixed(1), cases, note: 'Headless browser on this machine; not a measurement of the user device. Decoded estimate excludes driver copies/source sheets retained by browser.' };
  });
  mkdirSync('tools/profiles', { recursive: true });
  writeFileSync('tools/profiles/render.json', JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report));
} finally { await browser.close(); }

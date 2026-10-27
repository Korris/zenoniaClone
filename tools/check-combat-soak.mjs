// Co-op load simulation (test harness only -- the game itself is single-player): puts a SECOND hero
// into the same Frost Hollow world, then lets in-page bots drive both heroes for DURATION seconds:
// walk to the nearest monster, combo it, and cast every hotbar skill the moment it is off cooldown.
// Hero 2 is stepped with the game's own updatePlayer() (world.player swapped in for that call) and
// drawn with drawPlayer(); its projectiles/effects live in the shared world lists like hero 1's.
// Runs "solo" then "duo" and prints frame-time stats + GPU/JS thread load for comparison.
// --boss: heroes start at the map's own boss (Frost Hollow: Frostfang King; maps without one get the
//         Demon Cyclops), boss HP is boosted and it respawns if killed so the run never stalls,
//         and ~30 monsters keep swarming around the heroes.
// Usage: node tools/check-combat-soak.mjs [seconds=60] [hero1=warrior] [hero2=wizard] [duo-video.mp4] [--boss]
//        (a video path skips SOLO and screen-records the DUO window at 60fps with ffmpeg gdigrab, like a
//         player's own recorder; ffmpeg from FFMPEG_PATH or the imageio-ffmpeg binary)
//        (server must run on http://127.0.0.1:5173)
import puppeteer from 'puppeteer-core';
import { existsSync } from 'node:fs';
import { spawn } from 'node:child_process';

const ARGS = process.argv.slice(2).filter((a) => !a.startsWith('--'));
const BOSS = process.argv.includes('--boss');
const DURATION_S = Number(ARGS[0] || 60);
const HERO1 = ARGS[1] || 'warrior';
const HERO2 = ARGS[2] || 'wizard';
const VIDEO = ARGS[3] || null;
const SWARM_SIZE = 30;
const BOSS_HP = 200000;
const FFMPEG = process.env.FFMPEG_PATH
  || 'C:/Users/korri/AppData/Local/Programs/Python/Python312/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe';
const executablePath = ['C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find((p) => existsSync(p));
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/** Runs inside the page: builds the bots (and hero 2 when `duo`), starts frame recording. */
async function setupInPage({ hero1, hero2, duo, boss, swarmSize, bossHp }) {
  const c = window.__zenonia;
  const [{ Player }, { updatePlayer }, { renderWorld }, { drawHud }, { drawPlayer }, canvas, interp,
    { SKILL_DEFINITIONS }, { CLASS_DEFINITIONS }, { Monster }, { BOSS_TYPE }] = await Promise.all([
    import('/src/entities/player.js'), import('/src/game/player-controller.js'),
    import('/src/graphics/world-renderer.js'), import('/src/ui/hud-renderer.js'),
    import('/src/graphics/player-renderer.js'), import('/src/core/canvas-display.js'),
    import('/src/game/render-interpolation.js'), import('/src/data/skill-definitions.js'),
    import('/src/data/class-definitions.js'), import('/src/entities/monster.js'),
    import('/src/data/monster-definitions.js')]);

  c.session.startNewGame(hero1);
  c.state = 'playing';
  c.session.enterMap('frostHollow');
  const world = c.session.world;

  const equip = (p) => {
    const branches = CLASS_DEFINITIONS[p.classId].branches;
    const actives = Object.values(SKILL_DEFINITIONS).filter((d) => d.type === 'active' && branches.includes(d.branch));
    p.level = 30; p.maxHp = p.hp = 1e6;
    for (const d of actives) p.skills[d.id] = 5;
    p.hotbar = actives.sort((a, b) => b.tier - a.tier).slice(0, 3).map((d) => d.id); // heaviest skills
    return p;
  };

  /** Fake input device with the same surface the game reads (wasPressed / isDown / getMoveVector). */
  function makeBot(getHero) {
    const bot = { pressed: new Set(), move: { x: 0, y: 0 }, lastAttack: 0, casts: 0 };
    bot.think = (now) => {
      const p = getHero();
      p.mp = p.maxMp;
      bot.pressed.clear();
      let best = null, bestD = Infinity;
      for (const m of world.monsters) {
        if (m.dead) continue;
        const d = Math.hypot(m.x - p.x, m.y - p.y) - (m.isBoss ? 120 : 0); // a nearby boss wins
        if (d < bestD) { bestD = d; best = m; }
      }
      if (best) bestD = Math.hypot(best.x - p.x, best.y - p.y);
      const range = p.classId === 'warrior' ? 14 : 60;
      bot.move = { x: 0, y: 0 };
      if (!best) return;
      if (bestD > range) bot.move = { x: (best.x - p.x) / bestD, y: (best.y - p.y) / bestD };
      if (bestD <= range + 24) {
        for (const action of ['skill1', 'skill2', 'skill3']) {
          const id = p.hotbar[Number(action.at(-1)) - 1];
          if (id && !(p.cooldowns[id] > 0)) { bot.pressed.add(action); bot.casts++; }
        }
        if (now - bot.lastAttack > 220) { bot.pressed.add('attack'); bot.lastAttack = now; }
      }
    };
    bot.input = {
      wasPressed: (a) => bot.pressed.has(a),
      isDown: (a) => a === 'attack' && bot.pressed.has('attack'),
      getMoveVector: () => bot.move,
      takeClicks: () => [],
      endFrame: () => bot.pressed.clear(),
    };
    return bot;
  }

  const hero1Player = equip(c.session.player);
  const swarmTypes = [...new Set(world.monsters.filter((m) => !m.isBoss).map((m) => m.type))];
  const freeSpotNear = (cx, cy, rx, ry) => {
    for (let i = 0; i < 40; i++) {
      const x = cx + (Math.random() * 2 - 1) * rx, y = cy + (Math.random() * 2 - 1) * ry;
      if (!world.tilemap.isRectBlocked(x - 8, y - 10, 16, 10)) return { x, y };
    }
    return null;
  };
  let bossMonster = null;
  let lastUpkeep = 0;
  /** --boss mode: keep the boss alive and ~swarmSize monsters around the heroes. */
  const upkeepBossAndSwarm = () => {
    if (!boss) return;
    if (c.state === 'win') c.state = 'playing'; // a boss kill must not freeze the run on the victory menu
    const now = performance.now();
    if (now - lastUpkeep < 500) return;
    lastUpkeep = now;
    const hero = world.player;
    if (!bossMonster || bossMonster.dead || !world.monsters.includes(bossMonster)) {
      // Adopt the map's own boss first; afterwards respawn that same type beside the heroes.
      bossMonster = world.monsters.find((m) => m.isBoss && !m.dead) || null;
      if (!bossMonster) {
        const at = freeSpotNear(hero.x + 30, hero.y - 10, 20, 20) || { x: hero.x + 30, y: hero.y };
        bossMonster = new Monster(world.mapDef.boss?.type || BOSS_TYPE, at.x, at.y, -1);
        world.monsters.push(bossMonster);
        window.__soak && window.__soak.bossSpawns++;
      }
      bossMonster.maxHp = bossMonster.hp = bossHp;
      window.__soak && (window.__soak.bossName = bossMonster.def.name);
    }
    const alive = world.monsters.filter((m) => !m.dead && !m.isBoss).length;
    for (let i = alive; i < swarmSize; i++) {
      const at = freeSpotNear(hero.x, hero.y, 140, 90);
      if (at) world.monsters.push(new Monster(swarmTypes[i % swarmTypes.length], at.x, at.y, -1));
    }
  };
  // Start beside the monsters (the map entrance is next to the portal back to the village).
  const startNear = (p, dx) => {
    const m = (boss && world.monsters.find((x) => x.isBoss && !x.dead)) || world.monsters.find((x) => !x.dead);
    if (m) { p.x = m.x + dx; p.y = m.y + 6; }
  };
  startNear(hero1Player, -20);
  // A bot chasing a monster may step on the portal: bring everyone straight back to Frost Hollow.
  const stayInFrostHollow = () => {
    if (world.mapId === 'frostHollow') return;
    c.session.enterMap('frostHollow');
    startNear(c.session.player, -20);
    if (window.__soak) window.__soak.portalReturns++;
  };
  const bots = [makeBot(() => c.session.player)];
  c.input = bots[0].input; // hero 1 now driven by its bot through the normal controller path

  let second = null;
  if (duo) {
    second = equip(new Player(hero2));
    startNear(second, 20);
    bots.push(makeBot(() => second));
    const originalUpdate = c.update.bind(c);
    c.update = (dt) => {
      const now = performance.now();
      for (const b of bots) b.think(now);
      originalUpdate(dt);
      stayInFrostHollow();
      upkeepBossAndSwarm();
      if (second.x < 0 || Math.hypot(second.x - world.player.x, second.y - world.player.y) > 200) startNear(second, 20);
      if (c.state !== 'playing' || world.hitStopMs > 0) return;
      // Step hero 2 with the real controller code: world.player is swapped in only for this call.
      const main = world.player;
      world.player = second;
      try { updatePlayer(world, bots[1].input, dt, c.banner); } finally { world.player = main; }
      bots[1].input.endFrame();
    };
    c.render = (ctx, alpha = 0) => {
      canvas.setScreenTransform(ctx);
      ctx.fillStyle = '#000'; ctx.fillRect(0, 0, ctx.canvas.width, ctx.canvas.height);
      const view = { npcMarkers: c.npcMarkers, nearbyNpc: null, questState: c.session.quests };
      interp.renderInterpolated(world, alpha, () => {
        renderWorld(ctx, world, c.registry, view);
        canvas.setWorldTransform(ctx, world.camera);
        drawPlayer(ctx, second, c.registry);
        canvas.setScreenTransform(ctx); // HUD is laid out in screen space
        drawHud(ctx, world, c.registry, view);
      });
    };
  } else {
    const originalUpdate = c.update.bind(c);
    c.update = (dt) => { bots[0].think(performance.now()); originalUpdate(dt); stayInFrostHollow(); upkeepBossAndSwarm(); };
  }

  window.__soak = { bots, frames: [], portalReturns: 0, bossSpawns: 0, peakMonsters: 0, peakEffects: 0, peakProjectiles: 0, startExp: [hero1Player, second].map((p) => p?.exp ?? 0) };
  let last = performance.now();
  const tick = (now) => {
    window.__soak.frames.push(now - last); last = now;
    window.__soak.peakEffects = Math.max(window.__soak.peakEffects, world.effects.length + world.particles.length);
    window.__soak.peakProjectiles = Math.max(window.__soak.peakProjectiles, world.projectiles.length);
    window.__soak.peakMonsters = Math.max(window.__soak.peakMonsters, world.monsters.filter((m) => !m.dead).length);
    requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
  return [hero1Player.hotbar, second?.hotbar || []];
}

async function run(duo) {
  const recording = duo && VIDEO;
  // Recording: real (non-emulated) viewport, no automation infobar / translate popup over the game.
  const browser = await puppeteer.launch({ executablePath, headless: false,
    defaultViewport: recording ? null : undefined, ignoreDefaultArgs: recording ? ['--enable-automation'] : [],
    args: ['--window-size=1000,780', '--window-position=40,40', '--disable-features=Translate,TranslateUI', '--lang=en-US', '--accept-lang=en-US'] });
  const page = (await browser.pages())[0] || await browser.newPage();
  if (!recording) await page.setViewport({ width: 960, height: 640, deviceScaleFactor: 1.25 });
  const errors = [];
  page.on('pageerror', (e) => errors.push(String(e)));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await page.evaluateOnNewDocument(() => {
    window.__long = [];
    new PerformanceObserver((l) => l.getEntries().forEach((e) => window.__long.push(Math.round(e.duration))))
      .observe({ type: 'longtask', buffered: true });
  });
  await page.goto('http://127.0.0.1:5173/index.html', { waitUntil: 'load' });
  await page.waitForFunction(() => window.__zenonia?.state === 'title', { timeout: 30000 });
  await page.keyboard.press('Shift'); // real gesture on the title: unlocks audio like a player would
  const hotbars = await page.evaluate(setupInPage,
    { hero1: HERO1, hero2: HERO2, duo, boss: BOSS, swarmSize: SWARM_SIZE, bossHp: BOSS_HP });
  await sleep(1000);
  await page.evaluate(() => { window.__soak.frames.length = 0; window.__long.length = 0; });

  let recorder = null;
  if (recording) {
    // Capture the desktop region under the game canvas (capturing by window title yields black
    // frames for GPU-composited Chrome). Screen coords = window origin + frame chrome + canvas rect.
    await page.bringToFront();
    const r = await page.evaluate(() => {
      const box = document.getElementById('game-canvas').getBoundingClientRect();
      const border = (window.outerWidth - window.innerWidth) / 2;
      const dpr = window.devicePixelRatio;
      return { x: (window.screenX + border + box.left) * dpr, y: (window.screenY + window.outerHeight - window.innerHeight - border + box.top) * dpr,
        w: box.width * dpr, h: box.height * dpr };
    });
    const even = (n) => Math.floor(n / 2) * 2;
    recorder = spawn(FFMPEG, ['-y', '-f', 'gdigrab', '-framerate', '60', '-draw_mouse', '0',
      '-offset_x', String(Math.round(r.x)), '-offset_y', String(Math.round(r.y)),
      '-video_size', `${even(r.w)}x${even(r.h)}`, '-i', 'desktop',
      '-t', String(DURATION_S), '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '23', '-pix_fmt', 'yuv420p',
      VIDEO], { stdio: ['pipe', 'ignore', 'pipe'] });
    recorder.stderr.on('data', (d) => { if (/error/i.test(String(d))) console.error(String(d).trim()); });
  }
  await page.tracing.start({ categories: ['toplevel'] });
  const t0 = Date.now();
  await sleep(DURATION_S * 1000);
  if (recorder) await new Promise((r) => { recorder.on('close', r); setTimeout(() => recorder.stdin.end('q'), 1500); });
  const windowMs = Date.now() - t0;
  const trace = JSON.parse(Buffer.from(await page.tracing.stop()).toString()).traceEvents;
  const threads = {};
  trace.filter((e) => e.name === 'thread_name').forEach((e) => { threads[`${e.pid}:${e.tid}`] = e.args.name; });
  const busy = { CrGpuMain: 0, CrRendererMain: 0 };
  for (const e of trace) {
    const t = threads[`${e.pid}:${e.tid}`];
    if (e.ph === 'X' && e.dur && e.name === 'ThreadControllerImpl::RunTask' && t in busy) busy[t] += e.dur / 1000;
  }

  const r = await page.evaluate(() => {
    const s = window.__soak, f = s.frames.slice(1).sort((a, b) => a - b);
    const q = (p) => f[Math.min(f.length - 1, Math.floor(f.length * p))];
    const w = window.__zenonia.session.world;
    return {
      frames: f.length, avgFps: (1000 / (f.reduce((a, b) => a + b, 0) / f.length)).toFixed(1),
      p50: q(0.5).toFixed(1), p95: q(0.95).toFixed(1), p99: q(0.99).toFixed(1), max: f.at(-1).toFixed(0),
      over25ms: f.filter((x) => x > 25).length, over50ms: f.filter((x) => x > 50).length,
      longTasks: window.__long.length, worstLongTask: Math.max(0, ...window.__long),
      skillPresses: s.bots.map((b) => b.casts), peakEffects: s.peakEffects, peakProjectiles: s.peakProjectiles,
      monstersAlive: w.monsters.filter((m) => !m.dead).length, portalReturns: s.portalReturns,
      peakMonsters: s.peakMonsters, bossSpawns: s.bossSpawns, bossName: s.bossName,
      bossHp: w.monsters.find((m) => m.isBoss && !m.dead)?.hp ?? null,
    };
  });
  await page.screenshot({ path: `tools/screenshots/combat-soak-${duo ? 'duo' : 'solo'}${BOSS ? '-boss' : ''}.png` });
  await browser.close();
  return { label: `${duo ? `DUO  ${HERO1}+${HERO2}` : `SOLO ${HERO1}`}${BOSS ? ' + boss + swarm' : ''}`, hotbars, gpu: busy.CrGpuMain / windowMs * 100,
    js: busy.CrRendererMain / windowMs * 100, r, errors };
}

for (const duo of VIDEO ? [true] : [false, true]) {
  const { label, hotbars, gpu, js, r, errors } = await run(duo);
  console.log(`\n== ${label}  (${DURATION_S}s)  hotbars ${JSON.stringify(hotbars)}`);
  console.log(`FPS ${r.avgFps} | frame p50 ${r.p50} p95 ${r.p95} p99 ${r.p99} max ${r.max}ms | >25ms ${r.over25ms}  >50ms ${r.over50ms} of ${r.frames}`);
  console.log(`GPU ${gpu.toFixed(1)}%  JS ${js.toFixed(1)}% | long tasks ${r.longTasks} (worst ${r.worstLongTask}ms)`);
  console.log(`skill presses ${JSON.stringify(r.skillPresses)} | peak effects ${r.peakEffects} projectiles ${r.peakProjectiles} | monsters alive ${r.monstersAlive} (peak ${r.peakMonsters}) | portal returns ${r.portalReturns}`);
  if (BOSS) console.log(`boss ${r.bossName} | respawns ${r.bossSpawns} | boss HP now ${r.bossHp} / ${BOSS_HP}`);
  console.log(`errors ${JSON.stringify(errors).slice(0, 300)}`);
}

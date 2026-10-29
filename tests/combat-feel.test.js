import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Player } from '../src/entities/player.js';
import { Monster } from '../src/entities/monster.js';
import { Camera } from '../src/core/camera.js';
import { WorldState } from '../src/game/world-state.js';
import { COMBAT_FEEL, setReduceEffects } from '../src/data/combat-feel-config.js';
import { CombatFeel, Pool, WeaponTrail, stackHitStop, hitStopFor } from '../src/systems/combat-feel.js';
import { swingOnce } from '../src/systems/combo-hit-system.js';
import { trailPolygon } from '../src/graphics/combat-feel-renderer.js';
import { damagePopScale } from '../src/graphics/damage-number-renderer.js';
import { hitSquash } from '../src/graphics/monster-renderer.js';
import { pitchJitter } from '../src/audio/combat-audio.js';

function meleeWorld({ hitRoll = () => 0.5, crit = false } = {}) {
  const player = new Player('warrior'); player.x = 100; player.y = 100; player.dir = 'right';
  player.passiveMods.critChance = crit ? 1 : 0;
  const world = new WorldState(player, new Camera());
  world.hitRoll = hitRoll;
  const monster = new Monster('slime', 118, 100);
  world.monsters.push(monster);
  return { world, player, monster };
}

test('hit-stop durations: normal, finisher, crit bonus', () => {
  assert.equal(hitStopFor({}), 45);
  assert.equal(hitStopFor({ finisher: true }), 80);
  assert.equal(hitStopFor({ crit: true }), 65);
  assert.equal(hitStopFor({ finisher: true, crit: true }), 100);
});

test('hit-stop stacks but never exceeds 100ms', () => {
  assert.equal(stackHitStop(0, 45), 45);
  assert.equal(stackHitStop(45, 45), 90);
  assert.equal(stackHitStop(90, 80), 100);
  assert.equal(stackHitStop(100, 100), 100);
  assert.equal(stackHitStop(-5, 0), 0);
  const { world } = meleeWorld();
  world.addHitStop(80); world.addHitStop(80); world.addHitStop(80);
  assert.equal(world.hitStopMs, 100);
});

test('landed melee swing freezes the world; MISS does not', () => {
  const a = meleeWorld();
  swingOnce(a.world, 1, a.player.combo.config.hits[0], 1);
  assert.equal(a.world.hitStopMs, 45);
  assert.ok(a.monster.hp < a.monster.maxHp);

  const m = meleeWorld({ hitRoll: () => 0.999 }); // forces MISS
  swingOnce(m.world, 1, m.player.combo.config.hits[0], 1);
  assert.equal(m.world.hitStopMs, 0);
  assert.equal(m.world.camera.trauma, 0);
  assert.equal(m.world.feel.streaks.activeCount, 0, 'no impact sparks on a miss');
});

test('finisher uses the long freeze and adds screen shake; normal hit has none', () => {
  const normal = meleeWorld();
  swingOnce(normal.world, 1, normal.player.combo.config.hits[0], 1);
  assert.equal(normal.world.camera.trauma, 0);
  const fin = meleeWorld();
  const hits = fin.player.combo.config.hits;
  swingOnce(fin.world, hits.length, hits.at(-1), 1);
  assert.equal(fin.world.hitStopMs, 80);
  assert.equal(fin.world.camera.trauma, COMBAT_FEEL.shake.trauma.finisher);
  assert.ok(fin.world.feel.rings.activeCount >= 1, 'finisher spawns a shock ring');
});

test('trauma shake decays to rest and is bounded by maxOffset', () => {
  const cam = new Camera();
  cam.addTrauma(0.8);
  cam.update(16, () => 1);
  const max = COMBAT_FEEL.shake.maxOffset;
  assert.ok(cam.shakeOffsetX > 0 && cam.shakeOffsetX <= max * 0.64 + 1e-9);
  const first = cam.trauma;
  cam.update(16, () => 1);
  assert.ok(cam.trauma < first, 'trauma decays');
  for (let i = 0; i < 60; i++) cam.update(16, () => 1);
  assert.equal(cam.trauma, 0);
  assert.equal(cam.shakeOffsetX, 0);
  cam.addTrauma(5);
  assert.equal(cam.trauma, 1, 'trauma capped at 1');
});

test('reduce effects disables shake and monster flash', () => {
  setReduceEffects(true);
  try {
    const cam = new Camera();
    cam.addTrauma(1); cam.shake(4, 100);
    cam.update(16, () => 1);
    assert.equal(cam.trauma, 0);
    assert.equal(cam.shakeOffsetX, 0);
    const monster = new Monster('slime', 0, 0);
    monster.takeDamage(1);
    assert.equal(monster.hitFlashTimer, 0);
    const feel = new CombatFeel();
    feel.impact({ x: 0, y: 0, finisher: true });
    assert.equal(feel.rings.activeCount, 0);
  } finally { setReduceEffects(false); }
  const monster = new Monster('slime', 0, 0);
  monster.takeDamage(1);
  assert.equal(monster.hitFlashTimer, COMBAT_FEEL.hitReaction.flashMs);
});

test('hit squash peaks at impact and relaxes to 1', () => {
  const m = { hitFlashTimer: COMBAT_FEEL.hitReaction.flashMs };
  const peak = hitSquash(m);
  assert.ok(peak.sx > 1 && peak.sy < 1);
  assert.deepEqual(hitSquash({ hitFlashTimer: 0 }), { sx: 1, sy: 1 });
});

test('pool is bounded: exhausted pool drops spawns and slots are reused after expiry', () => {
  const pool = new Pool(3, () => ({ v: 0 }));
  assert.ok(pool.acquire() && pool.acquire() && pool.acquire());
  assert.equal(pool.acquire(), null);
  assert.equal(pool.activeCount, 3);
  pool.clear();
  assert.equal(pool.activeCount, 0);

  const feel = new CombatFeel();
  for (let i = 0; i < 40; i++) feel.impact({ x: 0, y: 0, crit: true });
  assert.ok(feel.streaks.activeCount <= COMBAT_FEEL.impact.poolSize);
  assert.ok(feel.rings.activeCount <= COMBAT_FEEL.impact.ringPoolSize);
  assert.ok(feel.stars.activeCount <= COMBAT_FEEL.impact.starPoolSize);
  assert.equal(feel.streaks.items.length, COMBAT_FEEL.impact.poolSize, 'never grows');
});

test('impact particles clean up after their lifetime', () => {
  const feel = new CombatFeel();
  feel.impact({ x: 5, y: 5, angle: 0, finisher: true });
  assert.ok(feel.streaks.activeCount >= 7);
  for (let t = 0; t < 400; t += 16) feel.updateImpacts(16);
  assert.equal(feel.streaks.activeCount, 0);
  assert.equal(feel.rings.activeCount, 0);
  assert.equal(feel.stars.activeCount, 0);
});

test('spark streaks move away from the hit point and slow down', () => {
  const feel = new CombatFeel();
  feel.impact({ x: 0, y: 0, angle: 0 }, () => 0.5);
  const s = feel.streaks.items.find((p) => p.active);
  const speed0 = Math.hypot(s.vx, s.vy);
  feel.updateImpacts(16);
  assert.ok(s.x > 0);
  assert.ok(Math.hypot(s.vx, s.vy) < speed0);
});

test('weapon trail sweeps then lives ~120ms after the tip stops, then dies', () => {
  const trail = new WeaponTrail({ follow: { x: 0, y: 0 }, startAngle: -1, endAngle: 1, radius: 30, width: 10, sweepMs: 90, colors: { edge: '#f00', core: '#fff' } });
  assert.equal(trailPolygon(trail, 0, 0), null, 'nothing before the tip moves');
  trail.update(45);
  assert.equal(trail.head, 0.5);
  assert.equal(trail.tail, 0);
  assert.ok(trailPolygon(trail, 0, 0).length > 8);
  trail.update(45); // sweep finished
  assert.equal(trail.head, 1);
  assert.equal(trail.dead, false);
  trail.update(COMBAT_FEEL.trail.lifeMs - 1);
  assert.equal(trail.dead, false);
  assert.ok(trail.tail > 0.5, 'tail eats the ribbon');
  trail.update(2);
  assert.equal(trail.dead, true);
});

test('a melee swing creates a trail that updateTrails removes after its lifetime', () => {
  const { world, player } = meleeWorld();
  swingOnce(world, 1, player.combo.config.hits[0], 1);
  assert.equal(world.feel.trails.length, 1);
  assert.equal(world.feel.trails[0].colors.edge, COMBAT_FEEL.trail.colors.warrior.edge);
  for (let t = 0; t < 400; t += 16) world.feel.updateTrails(16);
  assert.equal(world.feel.trails.length, 0);
});

test('swing sweeps alternate direction between combo steps', () => {
  const { world, player } = meleeWorld();
  const hits = player.combo.config.hits;
  swingOnce(world, 1, hits[0], 1); swingOnce(world, 2, hits[1], 1);
  const [a, b] = world.feel.trails;
  assert.ok(a.endAngle > a.startAngle);
  assert.ok(b.endAngle < b.startAngle);
});

test('damage pop scale starts at 1.4, overshoots below 1 slightly, settles at 1', () => {
  assert.ok(Math.abs(damagePopScale(0) - 1.4) < 1e-9);
  assert.equal(damagePopScale(COMBAT_FEEL.damageNumber.popMs), 1);
  const mid = damagePopScale(COMBAT_FEEL.damageNumber.popMs * 0.8);
  assert.ok(mid < 1.05 && mid > 0.9);
});

test('impact pitch jitter stays within the configured range', () => {
  assert.equal(pitchJitter(() => 0.5), 1);
  assert.ok(pitchJitter(() => 0) >= 1 - COMBAT_FEEL.audio.pitchJitter - 1e-9);
  assert.ok(pitchJitter(() => 1) <= 1 + COMBAT_FEEL.audio.pitchJitter + 1e-9);
});

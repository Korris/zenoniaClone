import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Player } from '../src/entities/player.js';
import { Monster } from '../src/entities/monster.js';
import { WorldState } from '../src/game/world-state.js';
import { tryCastSkill } from '../src/systems/skill-cast-system.js';
import { UltimateEffect } from '../src/entities/ultimate-effect.js';
import { SKILL_DEFINITIONS } from '../src/data/skill-definitions.js';

test('Earth Break charges, sweeps, slams with launch, then damages only its fixed fissure area', () => {
  const p = new Player('warrior'); p.dir = 'right'; p.skills.bladeStorm = 1;
  const w = new WorldState(p, { shake() {} });
  w.hitRoll = () => 0.5;
  const near = new Monster('slime', 32, 0), far = new Monster('slime', 150, 0);
  near.hp = near.maxHp = far.hp = far.maxHp = 1000; w.monsters = [near, far];
  assert.equal(tryCastSkill(w, 'bladeStorm').ok, true); assert.equal(p.mp, p.maxMp - 25);
  const e = w.effects[0]; assert.equal(p.ultimateCast, e);
  assert.equal(tryCastSkill(w, 'bladeStorm').reason, 'Busy');
  e.update(559); assert.equal(near.hp, 1000);
  e.update(1); const afterSweep = near.hp; assert.ok(afterSweep < 1000);
  e.update(889); assert.equal(near.hp, afterSweep); assert.equal(near.airborneMs || 0, 0);
  e.update(1); assert.ok(near.hp < afterSweep); assert.equal(near.airborneMs, 1500);
  const afterSlam = near.hp; p.x += 100;
  e.update(250); assert.ok(near.hp < afterSlam); assert.equal(far.hp, 1000);
  e.update(800); assert.equal(e.dead, true);
});
test('Rocket Barrage uses rank count, forward fan, delayed local explosions and one MP charge', () => {
  const p = new Player('mechanic'); p.dir = 'right'; p.skills.carpetBomb = 5; p.mp = 100;
  const w = new WorldState(p, { shake() {} });
  w.hitRoll = () => 0.5;
  assert.equal(tryCastSkill(w, 'carpetBomb').ok, true); assert.equal(p.mp, 40);
  const e = w.effects[0]; assert.equal(e.rockets.length, 12);
  assert.ok(e.rockets.every(r => r.x > p.x && Math.abs(Math.atan2(r.y + 6, r.x)) <= Math.PI / 6 + 1e-6));
  const r = e.rockets[0], near = new Monster('slime', r.x, r.y + 4), rear = new Monster('slime', -50, 0);
  near.hp = near.maxHp = rear.hp = rear.maxHp = 1000; w.monsters = [near, rear];
  e.update(1549); assert.equal(near.hp, 1000);
  e.update(1); assert.ok(near.hp < 1000); assert.equal(rear.hp, 1000);
  e.update(10000); assert.equal(e.dead, true);
  assert.equal(p.mp, 40);
});
test('ultimate timelines consume every event once across large frames and stop if caster dies', () => {
  const caster = { x: 0, y: 0, dead: false }, events = [];
  const e = new UltimateEffect({ kind: 'earthBreak', caster, dir: 'down', radius: 40, onEvent: event => events.push(event.type) });
  e.update(10000); e.update(10000);
  assert.deepEqual(events, ['sweep', 'slam', 'burn', 'burn', 'burn', 'burn']);
  const canceled = new UltimateEffect({ kind: 'rocketBarrage', caster, dir: 'down', radius: 20, onEvent() { assert.fail('Damage after death'); } });
  caster.dead = true; canceled.update(2000); assert.equal(canceled.dead, true);
});
test('ultimate save ids remain stable and clearing the map releases casting lock', () => {
  assert.equal(SKILL_DEFINITIONS.bladeStorm.name, 'Earth Break');
  assert.equal(SKILL_DEFINITIONS.carpetBomb.name, 'Rocket Barrage');
  const p = new Player(), w = new WorldState(p, {}); p.ultimateCast = { age: 0, lockMs: 1000 };
  w.clearMapEntities(); assert.equal(p.ultimateCast, null);
});

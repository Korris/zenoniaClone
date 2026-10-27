import { test } from 'node:test';
import assert from 'node:assert/strict';
import { rollHit } from '../src/systems/hit-chance.js';
import { hitPlayer, damageMonster, performRadialHit } from '../src/systems/combat-system.js';
import { tryCastSkill } from '../src/systems/skill-cast-system.js';
import { fireComboBolt, resolveProjectileHits } from '../src/systems/projectile-hit-system.js';
import { Player } from '../src/entities/player.js';
import { Monster } from '../src/entities/monster.js';
import { WorldState } from '../src/game/world-state.js';
import { drawPlayer } from '../src/graphics/player-renderer.js';

test('default accuracy gives exactly five misses in 100 evenly sampled rolls', () => {
  assert.equal(Array.from({ length: 100 }, (_, i) => rollHit({}, {}, () => (i + 0.5) / 100)).filter(Boolean).length, 95);
  assert.equal(rollHit({ accuracy: 0.9 }, { evasion: 0.1 }, () => 0.85), false);
});
test('misses cause no damage, knockback, hurt or immunity; landed incoming hit interrupts combo and gives hurt', () => {
  const p = new Player(), w = new WorldState(p, { shake() {} }); w.hitRoll = () => 0.99;
  const m = new Monster('slime', 10, 10), hp = m.hp;
  damageMonster(w, m, 10, 40, 0, '#fff', false, true);
  assert.equal(m.hp, hp); assert.equal(m.knockback.timer, 0);
  hitPlayer(w, 10, -10, 0);
  assert.equal(p.hp, p.maxHp); assert.equal(p.hitFlashTimer, 0); assert.equal(p.invulnTimer, 0);
  assert.equal(w.texts.length, 2);
  w.hitRoll = () => 0.5; p.combo.pressAttack(); hitPlayer(w, 10, -10, 0);
  assert.equal(p.hp, p.maxHp - 10); assert.equal(p.hitFlashTimer, 220);
  assert.equal(p.knockback.timer, 100); assert.equal(p.combo.isAttacking, false);
});
test('hurt uses original frames without tint; immunity never hides the player', () => {
  const p = new Player(), hurt = { pixelsPerUnit: 8 }, idle = { pixelsPerUnit: 8 }, drawn = [];
  const ctx = { getTransform: () => ({ a: 1, d: 1, e: 0, f: 0 }), beginPath() {}, ellipse() {}, fill() {}, drawImage(frame) { drawn.push(frame); } };
  const registry = { hero: { down: { idle: [idle], hurt: [hurt, hurt] } } };
  p.hitFlashTimer = 220; p.invulnTimer = 500; drawPlayer(ctx, p, registry);
  assert.equal(drawn.pop(), hurt);
  p.hitFlashTimer = 0;
  for (const time of [480, 400, 320, 240, 160, 80]) {
    p.invulnTimer = time; drawPlayer(ctx, p, registry); assert.equal(drawn.pop(), idle);
  }
});

test('wizard and mechanic basic shots can miss, but skill projectiles and area damage cannot', () => {
  for (const classId of ['wizard', 'mechanic']) {
    const p = new Player(classId), w = new WorldState(p, { shake() {} });
    w.hitRoll = () => 0.99; p.dir = 'right'; p.combo.startHit(1); fireComboBolt(w, 1);
    const shot = w.projectiles[0], m = new Monster('slime', shot.x, shot.y + 4);
    m.hp = m.maxHp = 1000; w.monsters = [m]; resolveProjectileHits(w);
    assert.equal(shot.canMiss, true); assert.equal(m.hp, 1000, classId);
    w.projectiles = []; p.combo.reset();
    const id = classId === 'wizard' ? 'iceShard' : 'piercingShot'; p.skills[id] = 1;
    assert.equal(tryCastSkill(w, id).ok, true);
    const spell = w.projectiles[0]; m.x = spell.x; m.y = spell.y + 4;
    resolveProjectileHits(w); assert.equal(spell.canMiss, false); assert.ok(m.hp < 1000, id);
    const hp = m.hp; performRadialHit(w, 50, 1, 0); assert.ok(m.hp < hp);
  }
});

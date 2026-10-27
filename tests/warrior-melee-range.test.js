import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Player } from '../src/entities/player.js';
import { Monster } from '../src/entities/monster.js';
import { WorldState } from '../src/game/world-state.js';
import { performComboHit } from '../src/systems/combat-system.js';
import { DIRECTION_VECTORS } from '../src/core/constants.js';

test('warrior hits beyond the previous reach in all four directions but excludes distant and rear enemies', () => {
  for (const dir of ['right', 'left', 'up', 'down']) for (const step of [1, 4]) {
    const p = new Player('warrior'); p.x = 100; p.y = 100; p.dir = dir; p.combo.startHit(step);
    const world = new WorldState(p, { shake() {} }), v = DIRECTION_VECTORS[dir];
    world.hitRoll = () => 0.5;
    const place = distance => {
      const m = new Monster('slime', p.x + v.x * distance, p.y - 6 + v.y * distance + 4);
      m.hp = m.maxHp = 1000; return m;
    };
    const near = place(step === 4 ? 42 : 30), far = place(64), rear = place(-20);
    world.monsters = [near, far, rear];
    performComboHit(world, step);
    assert.ok(near.hp < 1000, dir);
    assert.equal(far.hp, 1000, dir); assert.equal(rear.hp, 1000, dir);
    assert.ok(world.effects[0].scale > 1);
  }
});

import test from 'node:test';
import assert from 'node:assert/strict';
import { LootPickupEffect } from '../src/entities/loot-pickup-effect.js';
import { drawLootGroundFx, lootRarity } from '../src/graphics/loot-effects.js';

test('pickup visual lasts 480ms without modifying the collected item or player', () => {
  const drop = { x: 20, y: 30, kind: 'coin', value: 10 };
  const player = { x: 25, y: 32, gold: 10 };
  const effect = new LootPickupEffect(drop, player);
  assert.equal(effect.progress, 0);
  effect.update(240);
  assert.equal(effect.progress, 0.5);
  assert.equal(effect.dead, false);
  effect.update(240);
  assert.equal(effect.progress, 1);
  assert.equal(effect.dead, true);
  assert.equal(player.gold, 10);
  assert.equal(drop.value, 10);
});

test('loot beams use rarity-colored frames and animate without changing item brightness', () => {
  const frames = Array.from({ length: 6 }, (_, id) => ({ id }));
  const registry = { lootEffects: { rare: { idle: frames } } };
  const drawn = [], stack = [];
  const ctx = {
    globalAlpha: 1, globalCompositeOperation: 'source-over',
    save() { stack.push([this.globalAlpha, this.globalCompositeOperation]); },
    restore() { [this.globalAlpha, this.globalCompositeOperation] = stack.pop(); },
    drawImage(frame) { drawn.push(frame); },
  };
  const drop = { kind: 'equipment', item: { rarity: 'rare' }, x: 0, y: 0, age: 0 };
  drawLootGroundFx(ctx, drop, registry);
  drop.age = 150;
  drawLootGroundFx(ctx, drop, registry);
  assert.deepEqual(drawn, [frames[0], frames[0], frames[1], frames[1]]);
  assert.equal(ctx.globalAlpha, 1);
  assert.equal(ctx.globalCompositeOperation, 'source-over');
  assert.equal(lootRarity({ kind: 'coin' }), 'gold');
  assert.equal(lootRarity({ kind: 'hpPotion' }), 'common');
  assert.equal(lootRarity({ kind: 'mpPotion' }), 'common');
});

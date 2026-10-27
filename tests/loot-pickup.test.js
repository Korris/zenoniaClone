import test from 'node:test';
import assert from 'node:assert/strict';
import { Player } from '../src/entities/player.js';
import { ItemDrop } from '../src/entities/item-drop.js';
import { WorldState } from '../src/game/world-state.js';
import { updatePickups } from '../src/game/world-update.js';
import { generateItem, INVENTORY_SIZE } from '../src/systems/equipment-system.js';

function setup() {
  const player = new Player();
  player.x = player.y = 100;
  const world = new WorldState(player, {});
  const drop = (kind, value, x = 100) => {
    const d = new ItemDrop(x, 100, kind, value);
    d.age = 300;
    world.drops.push(d);
    return d;
  };
  return { player, world, drop };
}

test('overlapping loot previews the nearest item; each press collects exactly one', () => {
  const { player, world, drop } = setup();
  const far = drop('coin', 7, 103);
  const hp = drop('hpPotion', 0.3);
  const mp = drop('mpPotion', 0.3, 101);
  updatePickups(world);
  assert.equal(world.nearbyDrop, hp);
  assert.equal(world.lootFeedback.text, 'HP Potion');
  assert.equal(world.lootFeedback.mode, 'preview');
  assert.equal(player.potions.hpPotion, 0);
  updatePickups(world, true);
  assert.equal(hp.dead, true);
  assert.equal(mp.dead, false);
  assert.equal(far.dead, false);
  assert.equal(player.potions.hpPotion, 1);
  assert.equal(world.lootFeedback.mode, 'picked');
  updatePickups(world);
  assert.equal(world.nearbyDrop, mp);
  assert.equal(world.lootFeedback.text, 'HP Potion', 'confirmation is not overwritten by next preview');
  world.lootFeedback.lifeMs = 0;
  updatePickups(world);
  assert.equal(world.lootFeedback.text, 'MP Potion');
  updatePickups(world, true);
  assert.equal(player.potions.mpPotion, 1);
  assert.equal(far.dead, false);
  updatePickups(world, true);
  assert.equal(player.gold, 7);
});

test('full bag leaves equipment on ground and reports a small error notice', () => {
  const { player, world, drop } = setup();
  player.inventory = Array.from({ length: INVENTORY_SIZE }, () => ({}));
  const item = generateItem(() => 0, 1, 'weapon');
  const equipment = drop('equipment', item);
  drop('coin', 5, 101);
  updatePickups(world, true);
  assert.equal(equipment.dead, false);
  assert.equal(player.gold, 0);
  assert.equal(world.lootFeedback.text, 'Inventory full');
  player.inventory.pop();
  updatePickups(world, true);
  assert.equal(equipment.dead, true);
  assert.equal(player.inventory.at(-1), item);
  assert.equal(world.lootFeedback.text, item.name);
  assert.equal(world.lootFeedback.mode, 'picked');
});

test('leaving loot clears preview; newly dropped, dead and distant items are ignored', () => {
  const { player, world, drop } = setup();
  const hp = drop('hpPotion', 0.3);
  hp.age = 100;
  const coin = drop('coin', 1);
  coin.dead = true;
  drop('mpPotion', 0.3, 200);
  updatePickups(world, true);
  assert.equal(world.nearbyDrop, null);
  assert.equal(world.lootFeedback, null);
  hp.age = 300;
  updatePickups(world);
  assert.equal(world.lootFeedback.mode, 'preview');
  player.x = 300;
  updatePickups(world);
  assert.equal(world.lootFeedback, null);
});

// Drop rolls on monster death. Pure -> unit-testable with an injected RNG.
import { ItemDrop } from '../entities/item-drop.js';

const POTION_HEAL_FRACTION = 0.3; // potions restore 30% of max

/**
 * @returns {{kind:string, value:number}[]} list of drops to spawn
 */
export function rollDrops(monsterDef, rand = Math.random) {
  const drops = [];
  const [minGold, maxGold] = monsterDef.gold;
  const gold = minGold + Math.floor(rand() * (maxGold - minGold + 1));
  if (gold > 0) drops.push({ kind: 'coin', value: gold });
  if (rand() < monsterDef.drops.hpPotion) drops.push({ kind: 'hpPotion', value: POTION_HEAL_FRACTION });
  if (rand() < monsterDef.drops.mpPotion) drops.push({ kind: 'mpPotion', value: POTION_HEAL_FRACTION });
  return drops;
}

/** Instantiate ItemDrop entities at a position from rolled drops. */
export function spawnDrops(monsterDef, x, y, rand = Math.random) {
  return rollDrops(monsterDef, rand).map((d) => new ItemDrop(x, y, d.kind, d.value));
}

/**
 * Apply a picked-up drop to the player. Returns a label for floating text.
 */
export function applyPickup(player, drop) {
  switch (drop.kind) {
    case 'hpPotion': {
      const amount = Math.round(player.maxHp * drop.value);
      player.heal(amount);
      return { text: `+${amount} HP`, color: '#ff6b6b' };
    }
    case 'mpPotion': {
      const amount = Math.round(player.maxMp * drop.value);
      player.restoreMp(amount);
      return { text: `+${amount} MP`, color: '#6ba8ff' };
    }
    case 'coin':
      player.gold += drop.value;
      return { text: `+${drop.value} G`, color: '#ffd84a' };
    default:
      return { text: '', color: '#fff' };
  }
}

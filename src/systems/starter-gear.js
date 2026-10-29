// Default starter gear: every class begins with one main weapon + one offhand, already equipped.
// Pure (no DOM/RNG); used by new-game creation and to repair older saves with empty slots.
import { generateItem } from './equipment-system.js';

const STARTER_WEAPON_NAMES = Object.freeze({
  warrior: 'Sword', wizard: 'Staff', mechanic: 'Gun', assassin: 'Dagger', cleric: 'Holy Staff',
});
const STARTER_SLOTS = Object.freeze(['weapon', 'offhand']);

/** Level-1 common, +0 starter for `slot` ('weapon' | 'offhand') with a stable uid `starter-<slot>`. */
export function createStarterItem(slot, classId = 'warrior') {
  const item = generateItem(() => 0, 1, slot, 'common', classId);
  item.uid = `starter-${slot}`;
  if (slot === 'weapon') item.name = STARTER_WEAPON_NAMES[classId] || STARTER_WEAPON_NAMES.warrior;
  return item;
}

/**
 * Fills EMPTY weapon/offhand slots with the class starter. Never replaces an equipped item and
 * never duplicates a starter already sitting in the bag. Recomputes stats when anything changed.
 * @returns {boolean} true when at least one slot was filled
 */
export function equipStarterGear(player) {
  let changed = false;
  for (const slot of STARTER_SLOTS) {
    if (player.equipment[slot]) continue;
    if (player.inventory.some((i) => i.uid === `starter-${slot}`)) continue;
    player.equipment[slot] = createStarterItem(slot, player.classId);
    changed = true;
  }
  if (changed) player.recomputeStats();
  return changed;
}

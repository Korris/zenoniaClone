import { z } from 'zod';
import { ApiError } from '../errors.js';
import { itemSchema } from './item-validation.js';
import { INVENTORY_SIZE, MAX_INT, MAX_STONES, SLOTS } from './rpg-constants.js';

const delta = z.number().int().min(-MAX_INT).max(MAX_INT);

/** Server-to-server inventory / equipment / currency operations, applied atomically as one batch. */
export const opSchema = z.discriminatedUnion('type', [
  z.object({ type: z.literal('addItem'), item: itemSchema }),
  z.object({ type: z.literal('removeItem'), uid: z.string().min(1).max(64) }),
  z.object({ type: z.literal('equip'), uid: z.string().min(1).max(64) }),
  z.object({ type: z.literal('unequip'), slot: z.enum(SLOTS) }),
  z.object({ type: z.literal('adjust'), gold: delta.optional(), hpPotion: delta.optional(), mpPotion: delta.optional(), upgradeStone: delta.optional() }),
]);
export const opsSchema = z.array(opSchema).min(1).max(20);

const reject = (code, message) => new ApiError(422, code, message);
const hasUid = (s, uid) => [...s.inventory, ...s.equipment].some((i) => i.uid === uid);

const handlers = {
  addItem(s, op) {
    if (op.item.enhance !== 0) throw reject('enhance_not_allowed', 'Granted items must be +0; only the enhance endpoint raises levels');
    if (hasUid(s, op.item.uid)) throw reject('duplicate_item', `Item ${op.item.uid} already exists`);
    if (s.inventory.length >= INVENTORY_SIZE) throw reject('inventory_full', 'Inventory full');
    s.inventory.push(op.item);
  },
  removeItem(s, op) {
    const idx = s.inventory.findIndex((i) => i.uid === op.uid);
    if (idx < 0) throw reject('item_not_found', `Item ${op.uid} is not in the inventory`);
    s.inventory.splice(idx, 1);
  },
  equip(s, op) {
    const idx = s.inventory.findIndex((i) => i.uid === op.uid);
    if (idx < 0) throw reject('item_not_found', `Item ${op.uid} is not in the inventory`);
    const item = s.inventory[idx];
    if (item.classId && item.classId !== s.classId) throw reject('wrong_class', 'Item is locked to another class');
    s.inventory.splice(idx, 1);
    const prevIdx = s.equipment.findIndex((e) => e.slot === item.slot);
    if (prevIdx >= 0) s.inventory.push(...s.equipment.splice(prevIdx, 1));
    s.equipment.push(item);
  },
  unequip(s, op) {
    const idx = s.equipment.findIndex((e) => e.slot === op.slot);
    if (idx < 0) throw reject('slot_empty', `Nothing equipped in ${op.slot}`);
    if (s.inventory.length >= INVENTORY_SIZE) throw reject('inventory_full', 'Inventory full');
    s.inventory.push(...s.equipment.splice(idx, 1));
  },
  adjust(s, op) {
    for (const key of ['gold', 'hpPotion', 'mpPotion', 'upgradeStone']) {
      if (op[key] === undefined) continue;
      const v = s[key] + op[key];
      if (v < 0) throw reject('insufficient_funds', `Not enough ${key}`);
      s[key] = key === 'upgradeStone' ? Math.min(MAX_STONES, v) : Math.min(MAX_INT, v);
    }
  },
};

/** Applies the ops in order to a clone of `state`; throws a 422 ApiError on the first invalid op (nothing is applied). */
export function applyOps(state, ops) {
  const next = structuredClone(state);
  for (const op of ops) handlers[op.type](next, op);
  return next;
}

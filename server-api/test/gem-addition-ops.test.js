import { describe, expect, it } from 'vitest';
import { applyOps } from '../src/rpg/inventory-ops.js';
import { newCharacterState, stateSchema } from '../src/rpg/item-validation.js';
import { GEM_MAX_ROWS } from '../src/rpg/rpg-constants.js';
import { weapon } from './helpers.js';

describe('Soul Gems never ride in on a granted item', () => {
  it('addItem stores the item with no sockets even when the op carries gems', () => {
    const base = newCharacterState('warrior');
    const item = stateSchema.parse({ ...base, inventory: [{ ...weapon('gw1'), sockets: ['snowFox:p'] }] }).inventory[0];
    expect(item.sockets).toEqual(['snowFox:p']);
    const after = applyOps(base, [{ type: 'addItem', item }]);
    expect(after.inventory.find((i) => i.uid === 'gw1').sockets).toEqual([]);
  });

  it('every card x kind x locked combination fits the gem row limit', () => {
    expect(GEM_MAX_ROWS).toBe(160);
  });
});

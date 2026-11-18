import { describe, expect, it } from 'vitest';
import { newCharacterState, stateSchema } from '../src/rpg/item-validation.js';
import { HEADGEAR_COUNT, HEADGEAR_TEMPLATE_IDS, requiredEquipLevel, SLOTS } from '../src/rpg/rpg-constants.js';

// Cosmetic hats (HeadgearCatalog.cs, Data/hero-layered/headgear.json): slot 'headgear', templates 'hat-<key>', no stats / class / enhance.
const base = () => newCharacterState('cleric');
const hat = (extra = {}) => ({ uid: 'hat-bunny-ears-123', templateId: 'hat-bunny-ears', slot: 'headgear', name: 'Bunny Ears', rarity: 'uncommon', classId: '', itemLevel: 1, ...extra });

describe('headgear', () => {
  it('mirrors the C# catalog: 19 hats, slot 7', () => {
    expect(HEADGEAR_COUNT).toBe(19);
    expect(HEADGEAR_TEMPLATE_IDS[0]).toBe('hat-iron-helmet');
    expect(HEADGEAR_TEMPLATE_IDS[18]).toBe('hat-ultimate-wizard-hat');
    expect(SLOTS.indexOf('headgear')).toBe(7);
    expect(SLOTS.indexOf('helmet')).toBe(1); // the necklace slot keeps its id
  });

  it('keeps a worn hat, a hat in the bag and the look id', () => {
    const s = stateSchema.parse({
      ...base(),
      equipment: [hat()],
      inventory: [hat({ uid: 'hat-straw-hat-9', templateId: 'hat-straw-hat', name: 'Straw Hat', rarity: 'common' })],
      appearance: { stored: true, equippedHeadgearId: 10 },
    });
    expect(s.equipment[0].slot).toBe('headgear');
    expect(s.inventory[0].templateId).toBe('hat-straw-hat');
    expect(s.appearance.equippedHeadgearId).toBe(10);
  });

  it('needs no level', () => {
    expect(requiredEquipLevel({ slot: 'headgear', itemLevel: 95 })).toBe(1);
  });

  it.each([
    ['an unknown hat', { templateId: 'hat-golden-crown' }],
    ['a hat in the necklace slot', { slot: 'helmet' }],
    ['a necklace in the hat slot', { templateId: 'leatherCap' }],
    ['a hat with stats', { stats: { def: 5 } }],
    ['a class hat', { classId: 'wizard' }],
    ['an enhanced hat', { enhance: 3 }],
  ])('refuses %s', (_name, bad) => {
    expect(stateSchema.safeParse({ ...base(), inventory: [hat(bad)] }).success).toBe(false);
  });

  it('refuses two hats worn and a look id past the catalog', () => {
    expect(stateSchema.safeParse({ ...base(), equipment: [hat(), hat({ uid: 'hat-x-2' })] }).success).toBe(false);
    expect(stateSchema.safeParse({ ...base(), appearance: { stored: true, equippedHeadgearId: HEADGEAR_COUNT + 1 } }).success).toBe(false);
  });
});

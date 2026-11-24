import { describe, expect, it } from 'vitest';
import { newCharacterState, stateSchema } from '../src/rpg/item-validation.js';

// SaveData.appearance (CharacterAppearanceData, docs/character-appearance-layered-spec.md): kept through the strip, absent stays absent, ranges checked.
const base = () => newCharacterState('wizard');
const look = (extra = {}) => ({ stored: true, hairStyle: 3, hairColorIndex: 5, outfitColorIndex: 2, equippedHeadgearId: 7, ...extra });

describe('appearance', () => {
  it('is kept as sent', () => {
    expect(stateSchema.parse({ ...base(), appearance: look() }).appearance).toEqual(look());
  });
  it('stays absent in an old save', () => {
    expect(stateSchema.parse(base()).appearance).toBeUndefined();
  });
  it('fills defaults: nothing chosen', () => {
    expect(stateSchema.parse({ ...base(), appearance: {} }).appearance).toEqual({ stored: false, hairStyle: 0, hairColorIndex: -1, outfitColorIndex: -1, equippedHeadgearId: 0 });
  });
  it('drops unknown keys (classId lives on the save)', () => {
    expect(stateSchema.parse({ ...base(), appearance: look({ classId: 'wizard' }) }).appearance.classId).toBeUndefined();
  });
  it.each([
    ['a hair colour past the palette', { hairColorIndex: 9 }],
    ['an outfit colour past the palette', { outfitColorIndex: 6 }],
    ['a negative style', { hairStyle: -1 }],
    ['a headgear id above one byte', { equippedHeadgearId: 256 }],
    ['a colour below -1', { hairColorIndex: -2 }],
  ])('refuses %s', (_name, bad) => {
    expect(stateSchema.safeParse({ ...base(), appearance: look(bad) }).success).toBe(false);
  });
});

import { describe, expect, it } from 'vitest';
import { applyOps } from '../src/rpg/inventory-ops.js';
import { newCharacterState } from '../src/rpg/item-validation.js';
import { EQUIP_LEVEL_BY_TIER, requiredEquipLevel } from '../src/rpg/rpg-constants.js';

// Mirrors Assets/Scripts/Rpg/EquipLevel.cs (tests in Tests/dotnet/ZenoniaSkills.Tests/EquipLevelTests.cs).
const stats = { atk: 10, def: 0, maxHp: 0, maxMp: 0, critChance: 0, speedMult: 0 };
const weapon = (uid) => ({ uid, templateId: 'sword', slot: 'weapon', name: 'Test Sword', rarity: 'common', classId: 'warrior', itemLevel: 1, enhance: 0, stats });
const armor = (uid) => ({ uid, templateId: 'leatherVest', slot: 'armor', name: 'Test Armor', rarity: 'rare', classId: '', itemLevel: 3, enhance: 0, stats });
const stateAt = (level, inventory, equipment = []) => ({ ...newCharacterState('warrior'), level, inventory, equipment });
const at = (uid, itemLevel, extra = {}) => ({ ...weapon(uid), itemLevel, ...extra });

describe('equip level by art tier', () => {
  it('has 10 tiers ending at the max level', () => {
    expect(EQUIP_LEVEL_BY_TIER).toEqual([1, 5, 10, 15, 20, 30, 35, 40, 50, 60]);
  });
  it('tier = itemLevel / 10, enhancement ignored, armor free', () => {
    expect(requiredEquipLevel(at('a', 1))).toBe(1);
    expect(requiredEquipLevel(at('a', 59))).toBe(30);
    expect(requiredEquipLevel(at('a', 95, { enhance: 9 }))).toBe(60);
    expect(requiredEquipLevel({ ...armor('x'), itemLevel: 90 })).toBe(1);
  });
});

describe('equip op', () => {
  it('rejects below the level, nothing applied', () => {
    expect(() => applyOps(stateAt(29, [at('w', 50)]), [{ type: 'equip', uid: 'w' }])).toThrow(/Requires level 30/);
  });
  it('accepts at the level', () => {
    const next = applyOps(stateAt(30, [at('w', 50)]), [{ type: 'equip', uid: 'w' }]);
    expect(next.equipment.map((e) => e.uid)).toEqual(['w']);
  });
  it('already-worn gear above the level stays; unequip works', () => {
    const s = stateAt(5, [], [at('w', 90)]);
    expect(applyOps(s, [{ type: 'unequip', slot: 'weapon' }]).inventory.map((i) => i.uid)).toEqual(['w']);
  });
});

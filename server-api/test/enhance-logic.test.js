import { describe, expect, it } from 'vitest';
import { ATK_PCT, attemptEnhance, enhanceBonus, successChance } from '../src/rpg/enhance-logic.js';
import { MAX_ENHANCE, SUCCESS_BY_TARGET } from '../src/rpg/rpg-constants.js';
import { newCharacterState } from '../src/rpg/item-validation.js';
import { weapon } from './helpers.js';

// Golden values copied from Assets/Scripts/Rpg/Enhance.cs (SuccessByTarget table, 2026-10-30 ATK curve).

const stateWith = (equipment, inventory = [], upgradeStone = 10) => ({ ...newCharacterState('warrior'), equipment, inventory, upgradeStone });

describe('success table (C# StoneUpgradeMethod.SuccessByTarget)', () => {
  it('matches the C# numbers level by level', () => {
    const expected = [1, 1, 1, 0.9, 0.85, 0.8, 0.7, 0.65, 0.6, 0.5, 0.45, 0.4, 0.3, 0.25, 0.2]; // +0->1 ... +14->15
    expected.forEach((p, level) => expect(successChance(level)).toBe(p));
    expect(SUCCESS_BY_TARGET).toHaveLength(MAX_ENHANCE + 1);
  });
});

describe('ATK curve (10/20/35/60 % per level, +1/level floor)', () => {
  it('cumulative percentages', () => {
    const golden = { 1: 0.1, 6: 0.6, 7: 0.8, 9: 1.2, 10: 1.55, 12: 2.25, 13: 2.85, 15: 4.05 };
    for (const [lvl, v] of Object.entries(golden)) expect(ATK_PCT[lvl]).toBeCloseTo(v, 10);
  });
  it('bonus uses the larger of % and the +1/level floor, scales for offhands', () => {
    expect(enhanceBonus(100, 15)).toBe(405);
    expect(enhanceBonus(100, 0)).toBe(0);
    expect(enhanceBonus(10, 1)).toBe(1);
    expect(enhanceBonus(2, 10)).toBe(Math.max(Math.ceil(10 * 0.5), Math.floor(2 * 1.55 + 0.5))); // weak base: floor scaled by base/4
    expect(enhanceBonus(3, 2, 0.5)).toBe(1); // offhand: ceil(2*0.75*0.5)=1
    expect(enhanceBonus(100, 7, 0.5)).toBe(40);
  });
});

describe('attemptEnhance (deterministic rng)', () => {
  it('succeeds iff roll < chance, strictly, for every level; always costs exactly 1 stone', () => {
    for (let level = 0; level < MAX_ENHANCE; level++) {
      const p = successChance(level);
      const s = stateWith([weapon('w', level)]);
      const win = attemptEnhance(s, 'w', () => Math.max(0, p - 1e-9));
      expect(win).toMatchObject({ ok: true, success: true, level: level + 1 });
      expect(win.state.equipment[0].enhance).toBe(level + 1);
      expect(win.state.upgradeStone).toBe(9);
      const lose = attemptEnhance(s, 'w', () => p);
      if (p < 1) expect(lose).toMatchObject({ ok: true, success: false, level });
      expect(lose.state.upgradeStone).toBe(9);
    }
  });

  it('does not mutate the input state', () => {
    const s = stateWith([weapon('w', 0)]);
    const snapshot = JSON.stringify(s);
    attemptEnhance(s, 'w', () => 0);
    expect(JSON.stringify(s)).toBe(snapshot);
  });

  it('refusals match the C# reasons and do not spend stones', () => {
    const maxed = attemptEnhance(stateWith([weapon('w', 15)]), 'w', () => 0);
    expect(maxed).toMatchObject({ ok: false, reason: 'max-level' });
    expect(attemptEnhance(stateWith([weapon('w', 0)], [], 0), 'w', () => 0)).toMatchObject({ ok: false, reason: 'no-cost' });
    expect(attemptEnhance(stateWith([weapon('w', 0)]), 'ghost', () => 0)).toMatchObject({ ok: false, reason: 'not-owned' });
    const armorItem = { ...weapon('a'), slot: 'armor', templateId: 'armor-1' };
    expect(attemptEnhance(stateWith([], [armorItem]), 'a', () => 0)).toMatchObject({ ok: false, reason: 'not-weapon' });
    expect(maxed.state.upgradeStone).toBe(10);
  });

  it('works on bag items and offhands', () => {
    const off = { ...weapon('o'), slot: 'offhand', templateId: 'shield-1' };
    const r = attemptEnhance(stateWith([], [off]), 'o', () => 0);
    expect(r).toMatchObject({ ok: true, success: true, level: 1, equipped: false });
    expect(r.state.inventory[0].enhance).toBe(1);
  });

  it('empirical odds with a real PRNG stay near the table (sanity)', () => {
    let seed = 12345;
    const rand = () => ((seed = (seed * 1664525 + 1013904223) % 4294967296) / 4294967296);
    let wins = 0;
    const n = 20000;
    for (let i = 0; i < n; i++) if (attemptEnhance(stateWith([weapon('w', 12)]), 'w', rand).success) wins++;
    expect(wins / n).toBeGreaterThan(0.27); // chance(12->13) = 0.30
    expect(wins / n).toBeLessThan(0.33);
  });
});

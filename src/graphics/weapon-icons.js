import { cropSprite } from './sprite-cell.js';
import { equipmentTier } from '../data/item-definitions.js';
import { gearIconRef } from './gear-icons.js';

// Each class occupies two rows of the painted 5x6 atlas.
export const WEAPON_CLASS_OFFSETS = Object.freeze({ warrior: 0, wizard: 10, mechanic: 20, cleric: 10, assassin: 0 }); // cleric reuses the wizard staff icons; assassin daggers use the cut overlay icons (gear-icons.js), these offsets are only the painted fallback

/** Find isolated silhouettes: painted rows have slightly uneven spacing. */
export function buildWeaponIcons(sheet) {
  const canvas = cropSprite(sheet, 0, 0, sheet.width, sheet.height);
  const { width, height } = canvas;
  const pixels = canvas.getContext('2d').getImageData(0, 0, width, height).data;
  const visited = new Uint8Array(width * height);
  const queue = new Int32Array(width * height);
  const components = [];
  for (let start = 0; start < visited.length; start++) {
    if (visited[start] || pixels[start * 4 + 3] < 32) continue;
    let head = 0, tail = 1;
    queue[0] = start;
    visited[start] = 1;
    let left = width, top = height, right = 0, bottom = 0;
    while (head < tail) {
      const pos = queue[head++], x = pos % width, y = Math.floor(pos / width);
      left = Math.min(left, x); right = Math.max(right, x);
      top = Math.min(top, y); bottom = Math.max(bottom, y);
      for (const next of [x > 0 ? pos - 1 : -1, x + 1 < width ? pos + 1 : -1, pos - width, pos + width]) {
        if (next < 0 || next >= visited.length || visited[next] || pixels[next * 4 + 3] < 32) continue;
        visited[next] = 1;
        queue[tail++] = next;
      }
    }
    if (tail > 1000) components.push({ left, top, right, bottom, area: tail });
  }
  const bounds = components.sort((a, b) => b.area - a.area).slice(0, 30)
    .sort((a, b) => (a.top + a.bottom) - (b.top + b.bottom));
  if (bounds.length !== 30) return []; // existing item sprites remain available
  const icons = [];
  for (let row = 0; row < 6; row++) {
    for (const b of bounds.slice(row * 5, row * 5 + 5).sort((a, b) => a.left - b.left)) {
      icons.push(cropSprite(sheet, b.left, b.top, b.right - b.left + 1, b.bottom - b.top + 1));
    }
  }
  return icons;
}

export function weaponIconIndex(itemLevel, classId = 'warrior') {
  return (WEAPON_CLASS_OFFSETS[classId] ?? 0) + equipmentTier(itemLevel);
}

/** Cut-overlay art for class gear (registry.gearIcons[classId][slot][tier], see gear-icons.js), or undefined when that class's art is not loaded. */
function classGearSprite(registry, item, classId, ground) {
  const ref = gearIconRef(item, classId);
  return ref ? (ground ? registry.groundGearIcons : registry.gearIcons)?.[ref.classId]?.[ref.slot]?.[ref.tier] : undefined;
}

/** Ground-drop variant of itemSprite's choice (undefined => caller falls back to its own ground icon lookup). */
export function groundGearSprite(registry, item, classId = 'warrior') {
  return classGearSprite(registry, item, classId, true);
}

export function itemSprite(registry, item, classId = 'warrior', inventory = false) {
  const gear = classGearSprite(registry, item, classId, false);
  if (gear) return gear;
  if (item.slot === 'weapon') {
    const sprite = registry.weaponIcons?.[weaponIconIndex(item.itemLevel, item.classId || classId)];
    if (sprite) return sprite;
  }
  const armor = registry.armorIcons?.[item.slot]?.[equipmentTier(item.itemLevel)];
  if (armor) return armor;
  return (inventory && registry.inventoryItems?.[item.templateId]) || registry.items[item.templateId];
}

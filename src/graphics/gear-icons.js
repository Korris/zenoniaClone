// Class gear icons (bag, ground loot, attack button) drawn from the cut held-gear overlay art (base variant), one per
// equipment tier. GEAR_ICON_SPECS is the single table: warrior sword + shield, mechanic gun + robot, assassin dagger pair.
// Swords are tilted 45 deg like the other weapon icons, guns (barrel -> right) are tilted to point up-right. Classes
// without a spec (wizard, cleric) -- and any class whose overlay art is not loaded -- keep their painted atlas icons.
import { WARRIOR_SWORD_OVERLAYS } from '../data/warrior-sword-overlay-manifest-generated.js';
import { WARRIOR_SHIELD_OVERLAYS } from '../data/warrior-shield-overlay-manifest-generated.js';
import { MECHANIC_GUN_OVERLAYS } from '../data/mechanic-gun-overlay-manifest-generated.js';
import { MECHANIC_ROBOT_OVERLAYS } from '../data/mechanic-robot-overlay-manifest-generated.js';
import { ASSASSIN_DAGGER_OVERLAYS } from '../data/assassin-dagger-overlay-manifest-generated.js';
import { warriorSwordImageKey, warriorShieldImageKey, mechanicGunImageKey, mechanicRobotImageKey, assassinDaggerImageKey } from './asset-atlas-held-weapon.js';
import { equipmentTier, OFFHAND_BY_CLASS } from '../data/item-definitions.js';

const SWORD_TILT = Math.PI / 4; // clockwise: upright blade -> blade pointing up-right
const GUN_TILT = -Math.PI / 4; // counter-clockwise: barrel pointing right -> up-right
const PAD = 2;

/** Copy of `canvas` cropped to its non-transparent bounds (plus PAD). */
function trimToContent(canvas) {
  const { width, height } = canvas;
  const px = canvas.getContext('2d').getImageData(0, 0, width, height).data;
  let left = width, top = height, right = -1, bottom = -1;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (px[(y * width + x) * 4 + 3] < 16) continue;
      left = Math.min(left, x); right = Math.max(right, x); top = Math.min(top, y); bottom = Math.max(bottom, y);
    }
  }
  if (right < 0) return canvas;
  const out = document.createElement('canvas');
  out.width = right - left + 1 + PAD * 2;
  out.height = bottom - top + 1 + PAD * 2;
  out.getContext('2d').drawImage(canvas, left, top, out.width - PAD * 2, out.height - PAD * 2, PAD, PAD, out.width - PAD * 2, out.height - PAD * 2);
  out.pixelsPerUnit = 1;
  return out;
}

function iconFrom(img, tilt) {
  const size = Math.ceil(Math.hypot(img.width, img.height)) + 2;
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = size;
  const ctx = canvas.getContext('2d');
  ctx.imageSmoothingQuality = 'high';
  ctx.translate(size / 2, size / 2);
  ctx.rotate(tilt);
  ctx.drawImage(img, -img.width / 2, -img.height / 2);
  return trimToContent(canvas);
}

/** Per class: weapon + offhand overlay manifest, image-key function and icon tilt (radians). */
export const GEAR_ICON_SPECS = Object.freeze({
  warrior: {
    weapon: { manifest: WARRIOR_SWORD_OVERLAYS, keyOf: warriorSwordImageKey, tilt: SWORD_TILT },
    offhand: { manifest: WARRIOR_SHIELD_OVERLAYS, keyOf: warriorShieldImageKey, tilt: 0 },
  },
  mechanic: {
    weapon: { manifest: MECHANIC_GUN_OVERLAYS, keyOf: mechanicGunImageKey, tilt: GUN_TILT },
    offhand: { manifest: MECHANIC_ROBOT_OVERLAYS, keyOf: mechanicRobotImageKey, tilt: 0 },
  },
  assassin: { // offhand = the same dagger tilted up-left (a pair)
    weapon: { manifest: ASSASSIN_DAGGER_OVERLAYS, keyOf: assassinDaggerImageKey, tilt: SWORD_TILT },
    offhand: { manifest: ASSASSIN_DAGGER_OVERLAYS, keyOf: assassinDaggerImageKey, tilt: -SWORD_TILT },
  },
});

const OFFHAND_OWNER = Object.freeze(Object.fromEntries(Object.entries(OFFHAND_BY_CLASS).map(([cls, o]) => [o.type, cls])));

/** {weapon: canvas[tier], offhand: canvas[tier]} for `classId`, or null when it has no spec. Tiers whose art did not load stay undefined. */
export function buildGearIcons(images, classId) {
  const spec = GEAR_ICON_SPECS[classId];
  if (!spec) return null;
  return Object.fromEntries(Object.entries(spec).map(([slot, { manifest, keyOf, tilt }]) => [slot, manifest.map((_, tier) => {
    const img = images[keyOf(tier, 'base')];
    return img ? iconFrom(img, tilt) : undefined;
  })]));
}

/** { classId, slot, tier } of the class-art icon an item uses (class weapon, or the offhand of a class that has a spec), else null. */
export function gearIconRef(item, classId) {
  const tier = equipmentTier(item.itemLevel);
  const owner = item.slot === 'weapon' ? (item.classId || classId) : item.slot === 'offhand' ? OFFHAND_OWNER[item.templateId] : null;
  return owner && GEAR_ICON_SPECS[owner] ? { classId: owner, slot: item.slot, tier } : null;
}

// Builds everything one class owns in the sprite registry once its image set (asset-atlas-class-files.js) is loaded:
// hero sheets, held-gear overlays, companion, class FX / ultimates, painted skill icons, cut gear icons and the HUD portrait.
// Anything whose art failed to load degrades to the shared base set / painted atlas icons instead of throwing.
import { SKILL_DEFINITIONS } from '../data/skill-definitions.js';
import { buildHeroHd } from './character-sprite-builders.js';
import { HD_CLASS_SHEETS } from './asset-atlas-class-files.js';
import { buildHeldGear } from './held-weapon-registry.js';
import { buildCompanion } from './companion-renderer.js';
import { COMPANION_SPECS } from './companion-specs.js';
import { buildGearIcons } from './gear-icons.js';
import { buildGroundSprite } from './ground-sprite-builder.js';
import { buildPaintedSkillIcons } from './painted-combat-assets.js';
import { buildClassEffects } from './effects-sprite-builder.js';

/** Hero set for `classId`: its HD sheets over `baseSet` (the shared pixel/warrior-idle hero) for any animation not painted. */
function buildHeroSet(images, classId, baseSet) {
  const { animations, sideRows } = HD_CLASS_SHEETS[classId];
  const hd = buildHeroHd(images, animations, sideRows);
  return Object.fromEntries(Object.keys(baseSet).map((dir) => [dir, { ...baseSet[dir], ...hd[dir] }]));
}

/**
 * Adds `classId`'s sprites to `registry` (mutates it). Safe to call once per class; a second call just rebuilds.
 * @param {object} registry from buildSpriteRegistry (needs `hero`, `faces`)
 * @param {Record<string, HTMLImageElement>} images shared + class images
 */
export function registerClassSprites(registry, images, classId) {
  const sheets = HD_CLASS_SHEETS[classId];
  if (!sheets) return registry;
  const set = buildHeroSet(images, classId, registry.hero);
  registry.heroes[classId] = set;
  registry.heldGear[classId] = buildHeldGear(images, classId, set, buildHeroHd, sheets.sideRows);
  const spec = COMPANION_SPECS[classId];
  const companion = spec && buildCompanion(images, spec);
  if (companion) registry.companions[classId] = companion;
  registry.faces.heroByClass[classId] = images[sheets.faceKey] || registry.faces.heroByClass[classId] || registry.faces.hero;

  const gear = buildGearIcons(images, classId);
  if (gear) {
    registry.gearIcons[classId] = gear;
    registry.groundGearIcons[classId] = Object.fromEntries(Object.entries(gear)
      .map(([slot, list]) => [slot, list.map((sprite) => sprite && buildGroundSprite(sprite))]));
  }
  Object.assign(registry.effects, buildClassEffects(images));
  // Skill icons: the painted roster (if it loaded) over the pack `icon` fallbacks, for every skill the images can now resolve.
  for (const [id, def] of Object.entries(SKILL_DEFINITIONS)) if (images[def.icon]) registry.icons[id] = images[def.icon];
  Object.assign(registry.icons, buildPaintedSkillIcons(images));
  registry.loadedClasses.add(classId);
  return registry;
}

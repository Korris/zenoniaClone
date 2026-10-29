// Shared constants/helpers for the paper-doll held gear (wizard staff; warrior sword + shield).
import { equipmentTier } from '../data/item-definitions.js';
import { enhanceVisualBand } from '../systems/weapon-enhance-stats.js';

/** Master switch: false => exact legacy rendering (baked gear inside the class sheets). */
export const HELD_WEAPON_ENABLED = true;
/** Classes that draw held gear overlays (specs in held-gear-specs.js). */
export const HELD_WEAPON_CLASSES = Object.freeze(['wizard', 'warrior', 'cleric', 'mechanic', 'assassin']);

/** Overlay art tier 0..9 from the equipped item (same tiering as its icon); nothing equipped => tier 0. */
export const heldTierOf = (item) => (item ? equipmentTier(item.itemLevel) : 0);
/** Enhance visual band 0..4 (0 => base art, >=1 => enhanced art + baked glow). */
export const heldBandOf = (item) => (item ? enhanceVisualBand(item) : 0);

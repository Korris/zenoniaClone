// Per-class companion art description (cleric spirit, mechanic robot): 10 tier overlays (base + enhanced) and a tier-1 float loop.
// `scale` = frame px per art px (the companion is drawn ~60 / ~52 of the 192 frame px across).
import { CLERIC_SPIRIT_OVERLAYS } from '../data/cleric-spirit-overlay-manifest-generated.js';
import { CLERIC_SPIRIT_FLOAT } from '../data/cleric-spirit-float-manifest-generated.js';
import { MECHANIC_ROBOT_OVERLAYS } from '../data/mechanic-robot-overlay-manifest-generated.js';
import { MECHANIC_ROBOT_FLOAT } from '../data/mechanic-robot-float-manifest-generated.js';
import { clericSpiritImageKey, clericSpiritFloatKey, mechanicRobotImageKey, mechanicRobotFloatKey } from './asset-atlas-held-weapon.js';

export const COMPANION_SPECS = Object.freeze({
  cleric: { manifest: CLERIC_SPIRIT_OVERLAYS, float: CLERIC_SPIRIT_FLOAT, imageKey: clericSpiritImageKey, floatKey: clericSpiritFloatKey, scale: 0.3 },
  mechanic: { manifest: MECHANIC_ROBOT_OVERLAYS, float: MECHANIC_ROBOT_FLOAT, imageKey: mechanicRobotImageKey, floatKey: mechanicRobotFloatKey, scale: 0.26 },
});

// Image files for held gear: body sheets without the gear + overlay art, per class. Spread into
// asset-atlas.js ASSET_FILES. Missing files are dropped by asset-loader, and the renderer then falls
// back to the baked sheets (see held-weapon-registry.js).
import { WIZARD_STAFF_OVERLAYS } from '../data/wizard-staff-overlay-manifest-generated.js';
import { WARRIOR_SWORD_OVERLAYS } from '../data/warrior-sword-overlay-manifest-generated.js';
import { WARRIOR_SHIELD_OVERLAYS } from '../data/warrior-shield-overlay-manifest-generated.js';
import { CLERIC_STAFF_OVERLAYS } from '../data/cleric-staff-overlay-manifest-generated.js';
import { CLERIC_SPIRIT_OVERLAYS } from '../data/cleric-spirit-overlay-manifest-generated.js';
import { CLERIC_SPIRIT_FLOAT } from '../data/cleric-spirit-float-manifest-generated.js';
import { MECHANIC_GUN_OVERLAYS } from '../data/mechanic-gun-overlay-manifest-generated.js';
import { MECHANIC_ROBOT_OVERLAYS } from '../data/mechanic-robot-overlay-manifest-generated.js';
import { MECHANIC_ROBOT_FLOAT } from '../data/mechanic-robot-float-manifest-generated.js';

import { ASSASSIN_DAGGER_OVERLAYS } from '../data/assassin-dagger-overlay-manifest-generated.js';

const cap = (s) => s[0].toUpperCase() + s.slice(1);
const ANIMS = ['idle', 'walk', 'attack1', 'attack2', 'attack3', 'dash', 'hurt'];
const sheets = (prefix, anims) => Object.freeze(Object.fromEntries(anims.map((a) => [a, `${prefix}${cap(a)}`])));

export const HELD_BARE_ANIMATIONS = Object.freeze({ ...sheets('hdWizardBare', ANIMS), death: 'hdWizardBareDeath' });
// Warrior: `bare` = sword AND shield removed, `weaponless` = sword removed (shield kept).
// No death sheets: death is drawn from the baked armed set.
export const WARRIOR_BARE_ANIMATIONS = sheets('hdWarriorBare', ANIMS);
export const WARRIOR_WEAPONLESS_ANIMATIONS = sheets('hdWarriorWeaponless', ANIMS);
// Cleric: `bare` = holy staff removed (aligned per frame onto the armed set). No death sheet (armed death has no staff).
export const CLERIC_BARE_ANIMATIONS = sheets('hdClericWeaponless', ANIMS);
// Mechanic: `bare` = gun removed. attack2 (weaponless sheet has other poses) and death are absent: drawn from the baked armed set.
export const MECHANIC_BARE_ANIMATIONS = sheets('hdMechanicWeaponless', ['idle', 'walk', 'attack1', 'attack3', 'dash', 'hurt']);

// Assassin: `bare` = both daggers removed. walk / dash / hurt have no armed sheet at all (the baked set there IS the bare body);
// death is the weaponless art. Daggers are always overlays.
export const ASSASSIN_BARE_ANIMATIONS = sheets('hdAssassinWeaponless', ANIMS);

/** Image keys of overlay art (variant 'base' | 'enhanced'). */
export const staffImageKey = (tier, variant) => `wizardStaff${tier}${variant}`;
export const warriorSwordImageKey = (tier, variant) => `warriorSword${tier}${variant}`;
export const warriorShieldImageKey = (tier, variant) => `warriorShield${tier}${variant}`;
export const clericStaffImageKey = (tier, variant) => `clericStaff${tier}${variant}`;
export const clericSpiritImageKey = (tier, variant) => `clericSpirit${tier}${variant}`;
export const clericSpiritFloatKey = (dir, i) => `clericSpiritFloat${dir}${i}`;
export const mechanicGunImageKey = (tier, variant) => `mechanicGun${tier}${variant}`;
export const mechanicRobotImageKey = (tier, variant) => `mechanicRobot${tier}${variant}`;
export const assassinDaggerImageKey = (tier, variant) => `assassinDagger${tier}${variant}`;
export const mechanicRobotFloatKey = (dir, i) => `mechanicRobotFloat${dir}${i}`;

const sheetFiles = (map, dir) => Object.entries(map).map(([anim, key]) => [key, `${dir}${anim}.png`]);
const floatFiles = (float, keyOf) => Object.entries(float).flatMap(([dir, frames]) => frames.map((f, i) => [keyOf(dir, i), f.src.replace(/^assets\//, '')]));
const overlayFiles = (manifest, keyOf) => manifest.flatMap((entry, tier) => ['base', 'enhanced']
  .map((variant) => [keyOf(tier, variant), entry[variant].src.replace(/^assets\//, '')]));

const toObject = (entries) => Object.freeze(Object.fromEntries(entries));

/** Held-gear + companion files per class, so a session only fetches its own class's overlays. */
export const HELD_ASSET_FILES_BY_CLASS = Object.freeze({
  wizard: toObject([
    ...sheetFiles(HELD_BARE_ANIMATIONS, 'hd/wizard/weaponless/normalized-hd/'),
    ...overlayFiles(WIZARD_STAFF_OVERLAYS, staffImageKey),
  ]),
  warrior: toObject([
    ...sheetFiles(WARRIOR_BARE_ANIMATIONS, 'hd/warrior-v2/bare/normalized-hd/'),
    ...sheetFiles(WARRIOR_WEAPONLESS_ANIMATIONS, 'hd/warrior-v2/weaponless/normalized-hd/'),
    ...overlayFiles(WARRIOR_SWORD_OVERLAYS, warriorSwordImageKey),
    ...overlayFiles(WARRIOR_SHIELD_OVERLAYS, warriorShieldImageKey),
  ]),
  cleric: toObject([
    ...sheetFiles(CLERIC_BARE_ANIMATIONS, 'hd/cleric/weaponless/normalized-hd/'),
    ...overlayFiles(CLERIC_STAFF_OVERLAYS, clericStaffImageKey),
    ...overlayFiles(CLERIC_SPIRIT_OVERLAYS, clericSpiritImageKey),
    ...floatFiles(CLERIC_SPIRIT_FLOAT, clericSpiritFloatKey),
  ]),
  mechanic: toObject([
    ...sheetFiles(MECHANIC_BARE_ANIMATIONS, 'hd/mechanic/weaponless/normalized-hd/'),
    ...overlayFiles(MECHANIC_GUN_OVERLAYS, mechanicGunImageKey),
    ...overlayFiles(MECHANIC_ROBOT_OVERLAYS, mechanicRobotImageKey),
    ...floatFiles(MECHANIC_ROBOT_FLOAT, mechanicRobotFloatKey),
  ]),
  assassin: toObject([
    ...sheetFiles(ASSASSIN_BARE_ANIMATIONS, 'hd/assassin/weaponless/normalized-hd/'),
    ...overlayFiles(ASSASSIN_DAGGER_OVERLAYS, assassinDaggerImageKey),
  ]),
});

/** Every class's held-gear files (union of HELD_ASSET_FILES_BY_CLASS). */
export const HELD_WEAPON_ASSET_FILES = Object.freeze(Object.assign({}, ...Object.values(HELD_ASSET_FILES_BY_CLASS)));

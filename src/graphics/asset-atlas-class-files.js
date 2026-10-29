// Per-class image sets. A session only fetches the shared atlas (asset-atlas.js SHARED_ASSET_FILES) plus the
// files of the class it plays: HD hero sheets, held-gear overlays, companion art, painted skill roster and
// ultimate FX. `classAssetFiles(classId)` is the single source for the lazy loader (class-asset-loader.js).
import { HD_WARRIOR_ASSET_FILES, HD_HERO_ANIMATIONS, HD_HERO_SIDE_ROWS, HD_WARRIOR_FACE_KEY } from './asset-atlas-hd-warrior.js';
import { HD_WIZARD_ASSET_FILES, HD_WIZARD_ANIMATIONS, HD_WIZARD_SIDE_ROWS, HD_WIZARD_FACE_KEY } from './asset-atlas-hd-wizard.js';
import { HD_MECHANIC_ASSET_FILES, HD_MECHANIC_ANIMATIONS, HD_MECHANIC_SIDE_ROWS, HD_MECHANIC_FACE_KEY } from './asset-atlas-hd-mechanic.js';
import { HD_CLERIC_ASSET_FILES, HD_CLERIC_ANIMATIONS, HD_CLERIC_SIDE_ROWS, HD_CLERIC_FACE_KEY } from './asset-atlas-hd-cleric.js';
import { HD_ASSASSIN_ASSET_FILES, HD_ASSASSIN_ANIMATIONS, HD_ASSASSIN_SIDE_ROWS, HD_ASSASSIN_FACE_KEY } from './asset-atlas-hd-assassin.js';
import { HELD_ASSET_FILES_BY_CLASS } from './asset-atlas-held-weapon.js';

export const CLASS_IDS_WITH_ART = Object.freeze(['warrior', 'wizard', 'mechanic', 'cleric', 'assassin']);

/** HD sheet tables per class: animation -> image key, side-row table and the portrait key. */
export const HD_CLASS_SHEETS = Object.freeze({
  warrior: { animations: HD_HERO_ANIMATIONS, sideRows: HD_HERO_SIDE_ROWS, faceKey: HD_WARRIOR_FACE_KEY },
  wizard: { animations: HD_WIZARD_ANIMATIONS, sideRows: HD_WIZARD_SIDE_ROWS, faceKey: HD_WIZARD_FACE_KEY },
  mechanic: { animations: HD_MECHANIC_ANIMATIONS, sideRows: HD_MECHANIC_SIDE_ROWS, faceKey: HD_MECHANIC_FACE_KEY },
  cleric: { animations: HD_CLERIC_ANIMATIONS, sideRows: HD_CLERIC_SIDE_ROWS, faceKey: HD_CLERIC_FACE_KEY },
  assassin: { animations: HD_ASSASSIN_ANIMATIONS, sideRows: HD_ASSASSIN_SIDE_ROWS, faceKey: HD_ASSASSIN_FACE_KEY },
});

const HD_FILES = Object.freeze({
  warrior: HD_WARRIOR_ASSET_FILES, wizard: HD_WIZARD_ASSET_FILES, mechanic: HD_MECHANIC_ASSET_FILES,
  cleric: HD_CLERIC_ASSET_FILES, assassin: HD_ASSASSIN_ASSET_FILES,
});

/** Portrait files of all five classes: small (20-55 KB), loaded up front for the hero-select cards. */
export const CLASS_PREVIEW_FILES = Object.freeze(Object.fromEntries(
  CLASS_IDS_WITH_ART.map((id) => [HD_CLASS_SHEETS[id].faceKey, HD_FILES[id][HD_CLASS_SHEETS[id].faceKey]]).filter(([, path]) => path),
));
/** The title screen shows the warrior's idle pose, so that one sheet is part of the shared set. */
export const TITLE_HERO_FILES = Object.freeze({ hdHeroIdle: HD_WARRIOR_ASSET_FILES.hdHeroIdle });

// Class-owned art that is not a hero sheet: painted skill roster, ultimate FX (iceStorm / earthBreak frames / rocketBarrage).
const ULTIMATE_ICONS = { ultimateIcons: 'custom/ultimate-icons-chibi.png' }; // fallback skill icons when a roster is missing
const earthBreakFrames = Object.fromEntries(['charge', 'sweep', 'slam', 'fissures'].flatMap((stage) =>
  Array.from({ length: 6 }, (_, i) => [`earthBreak_${stage}_${i + 1}`, `custom/earth-break-frames/${stage}-${i + 1}.png`])));
const roster = (classId) => ({ [`paintedSkills${classId}`]: `custom/skill-icons-${classId}-roster.png` });

const CLASS_EXTRA_FILES = Object.freeze({
  warrior: { ...roster('warrior'), ...ULTIMATE_ICONS, ...earthBreakFrames },
  wizard: { ...roster('wizard'), iceStorm: 'hd/wizard/fx/ice-storm.png' },
  mechanic: { ...roster('mechanic'), ...ULTIMATE_ICONS, rocketBarrage: 'custom/rocket-barrage-fx.png' },
  cleric: roster('cleric'), // a missing roster file => pixel `icon` fallbacks (the loader tolerates it)
  assassin: roster('assassin'),
});

/** @returns {Record<string,string>} image key -> path for everything only `classId` needs ({} for an unknown class). */
export function classAssetFiles(classId) {
  if (!HD_FILES[classId]) return {};
  return { ...HD_FILES[classId], ...HELD_ASSET_FILES_BY_CLASS[classId], ...CLASS_EXTRA_FILES[classId] };
}

/** Every class-owned file (union) -- what the game used to load at boot. */
export const ALL_CLASS_ASSET_FILES = Object.freeze(Object.assign({}, ...CLASS_IDS_WITH_ART.map(classAssetFiles)));

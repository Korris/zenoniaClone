// Exports the web game's held-gear data to JsonUtility-friendly array-form JSON in Assets/Resources/Data/:
//   held-gear.json     warrior sword/shield overlay manifests + per-frame grip anchors
//   held-wizard.json   wizard staff manifest + anchors (part "staff")
//   held-mechanic.json mechanic gun manifest + anchors (part "gun") + robot companion manifest + float loop
// Run:  node Tools/export-held-gear.mjs
import { writeFileSync, mkdirSync } from 'node:fs';
import { pathToFileURL } from 'node:url';
import path from 'node:path';

const WEB = process.env.ZENONIA_WEB || 'C:/Project/zenoniaClone';
const load = (rel) => import(pathToFileURL(path.join(WEB, rel)).href);
const { WARRIOR_HELD_ANCHORS } = await load('src/data/warrior-held-anchors-generated.js');
const { WARRIOR_SWORD_OVERLAYS } = await load('src/data/warrior-sword-overlay-manifest-generated.js');
const { WARRIOR_SHIELD_OVERLAYS } = await load('src/data/warrior-shield-overlay-manifest-generated.js');

const { WIZARD_STAFF_ANCHORS } = await load('src/data/wizard-staff-anchors-generated.js');
const { WIZARD_STAFF_OVERLAYS } = await load('src/data/wizard-staff-overlay-manifest-generated.js');
const { MECHANIC_GUN_ANCHORS } = await load('src/data/mechanic-held-anchors-generated.js');
const { MECHANIC_GUN_OVERLAYS } = await load('src/data/mechanic-gun-overlay-manifest-generated.js');
const { MECHANIC_ROBOT_OVERLAYS } = await load('src/data/mechanic-robot-overlay-manifest-generated.js');
const { MECHANIC_ROBOT_FLOAT } = await load('src/data/mechanic-robot-float-manifest-generated.js');

const grip = (v) => ({ w: v.w, h: v.h, gripX: v.gripX, gripY: v.gripY });
const overlays = (list) => list.map((e) => ({ name: e.name, glow: e.glow, baseGrip: grip(e.base), enhancedGrip: grip(e.enhanced) }));
const frameOut = (f) => ({
  x: f.x ?? 0, y: f.y ?? 0, angle: f.angle ?? 0, s: f.s ?? 1, sx: f.sx ?? 1,
  behind: !!f.behind, hidden: !!f.hidden, fallback: !!f.fallback,
});
/** anims = { anim: { dir: frame[] } } -> [{part, anim, dir, frames}] */
const flatten = (part, anims, out) => {
  for (const [anim, dirs] of Object.entries(anims)) {
    for (const [dir, frames] of Object.entries(dirs)) out.push({ part, anim, dir, frames: frames.map(frameOut) });
  }
};
const anchors = [];
for (const [part, anims] of Object.entries(WARRIOR_HELD_ANCHORS)) flatten(part, anims, anchors);
const out = { sword: overlays(WARRIOR_SWORD_OVERLAYS), shield: overlays(WARRIOR_SHIELD_OVERLAYS), anchors };

const dataDir = path.join(import.meta.dirname, '..', 'Assets', 'Resources', 'Data');
mkdirSync(dataDir, { recursive: true });
const write = (file, obj, summary) => {
  writeFileSync(path.join(dataDir, file), JSON.stringify(obj));
  console.log('wrote', file, summary);
};
write('held-gear.json', out, `sword ${out.sword.length} shield ${out.shield.length} anchors ${anchors.length}`);

const wizardAnchors = [];
flatten('staff', WIZARD_STAFF_ANCHORS, wizardAnchors);
write('held-wizard.json', { weapon: overlays(WIZARD_STAFF_OVERLAYS), anchors: wizardAnchors }, `staff ${WIZARD_STAFF_OVERLAYS.length} anchors ${wizardAnchors.length}`);

const gunAnchors = [];
flatten('gun', MECHANIC_GUN_ANCHORS, gunAnchors);
const robotFloat = Object.entries(MECHANIC_ROBOT_FLOAT).map(([dir, frames]) => ({ dir, frames: frames.map(grip) }));
write('held-mechanic.json', {
  weapon: overlays(MECHANIC_GUN_OVERLAYS), robot: overlays(MECHANIC_ROBOT_OVERLAYS), robotFloat, anchors: gunAnchors,
}, `gun ${MECHANIC_GUN_OVERLAYS.length} robot ${MECHANIC_ROBOT_OVERLAYS.length} float ${robotFloat.length} anchors ${gunAnchors.length}`);

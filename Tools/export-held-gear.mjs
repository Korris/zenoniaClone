// Exports the web game's warrior held-gear data (anchors + sword/shield overlay manifests) to a JsonUtility-friendly
// array-form JSON in Assets/Resources/Data/held-gear.json.  Run:  node Tools/export-held-gear.mjs
import { writeFileSync, mkdirSync } from 'node:fs';
import { pathToFileURL } from 'node:url';
import path from 'node:path';

const WEB = process.env.ZENONIA_WEB || 'C:/Project/zenoniaClone';
const load = (rel) => import(pathToFileURL(path.join(WEB, rel)).href);
const { WARRIOR_HELD_ANCHORS } = await load('src/data/warrior-held-anchors-generated.js');
const { WARRIOR_SWORD_OVERLAYS } = await load('src/data/warrior-sword-overlay-manifest-generated.js');
const { WARRIOR_SHIELD_OVERLAYS } = await load('src/data/warrior-shield-overlay-manifest-generated.js');

const grip = (v) => ({ w: v.w, h: v.h, gripX: v.gripX, gripY: v.gripY });
const overlays = (list) => list.map((e) => ({ name: e.name, glow: e.glow, baseGrip: grip(e.base), enhancedGrip: grip(e.enhanced) }));
const anchors = [];
for (const [part, anims] of Object.entries(WARRIOR_HELD_ANCHORS)) {
  for (const [anim, dirs] of Object.entries(anims)) {
    for (const [dir, frames] of Object.entries(dirs)) {
      anchors.push({
        part, anim, dir,
        frames: frames.map((f) => ({
          x: f.x ?? 0, y: f.y ?? 0, angle: f.angle ?? 0, s: f.s ?? 1, sx: f.sx ?? 1,
          behind: !!f.behind, hidden: !!f.hidden, fallback: !!f.fallback,
        })),
      });
    }
  }
}
const out = { sword: overlays(WARRIOR_SWORD_OVERLAYS), shield: overlays(WARRIOR_SHIELD_OVERLAYS), anchors };
const dest = path.join(import.meta.dirname, '..', 'Assets', 'Resources', 'Data', 'held-gear.json');
mkdirSync(path.dirname(dest), { recursive: true });
writeFileSync(dest, JSON.stringify(out));
console.log('wrote', dest, 'sword', out.sword.length, 'shield', out.shield.length, 'anchors', anchors.length);

// Per-class description of the paper-doll held gear. `parts` are listed in draw order (back to front within a layer).
//  slot: equipment slot that picks the overlay tier + enhance band (nothing equipped => tier 0 art)
//  optional: nothing equipped => the part is not drawn at all (empty offhand)
//  bodyFallback: no anchors for this part in an animation => draw `partial` body (gear painted into it) instead of `bare`
import { WIZARD_STAFF_ANCHORS } from '../data/wizard-staff-anchors-generated.js';
import { WIZARD_STAFF_OVERLAYS } from '../data/wizard-staff-overlay-manifest-generated.js';
import { WARRIOR_HELD_ANCHORS } from '../data/warrior-held-anchors-generated.js';
import { WARRIOR_SWORD_OVERLAYS } from '../data/warrior-sword-overlay-manifest-generated.js';
import { WARRIOR_SHIELD_OVERLAYS } from '../data/warrior-shield-overlay-manifest-generated.js';
import { CLERIC_STAFF_ANCHORS } from '../data/cleric-held-anchors-generated.js';
import { CLERIC_STAFF_OVERLAYS } from '../data/cleric-staff-overlay-manifest-generated.js';
import { MECHANIC_GUN_ANCHORS } from '../data/mechanic-held-anchors-generated.js';
import { MECHANIC_GUN_OVERLAYS } from '../data/mechanic-gun-overlay-manifest-generated.js';
import { ASSASSIN_HELD_ANCHORS } from '../data/assassin-held-anchors-generated.js';
import { ASSASSIN_DAGGER_OVERLAYS } from '../data/assassin-dagger-overlay-manifest-generated.js';
import {
  HELD_BARE_ANIMATIONS, WARRIOR_BARE_ANIMATIONS, WARRIOR_WEAPONLESS_ANIMATIONS, CLERIC_BARE_ANIMATIONS, MECHANIC_BARE_ANIMATIONS, ASSASSIN_BARE_ANIMATIONS,
  assassinDaggerImageKey, mechanicGunImageKey, staffImageKey, clericStaffImageKey, warriorSwordImageKey, warriorShieldImageKey,
} from './asset-atlas-held-weapon.js';

export const HELD_GEAR_SPECS = Object.freeze({
  wizard: {
    bare: HELD_BARE_ANIMATIONS,
    parts: [{ id: 'staff', slot: 'weapon', anchors: WIZARD_STAFF_ANCHORS, manifest: WIZARD_STAFF_OVERLAYS, imageKey: staffImageKey }],
  },
  cleric: { // holy staff on the weaponless body; frames whose anchor is {fallback} draw the armed baked frame
    bare: CLERIC_BARE_ANIMATIONS,
    parts: [{ id: 'staff', slot: 'weapon', anchors: CLERIC_STAFF_ANCHORS, manifest: CLERIC_STAFF_OVERLAYS, imageKey: clericStaffImageKey }],
  },
  mechanic: { // gun on the weaponless body; attack2 / death have no bare sheet and fallback frames draw the armed baked frame
    bare: MECHANIC_BARE_ANIMATIONS,
    parts: [{ id: 'gun', slot: 'weapon', anchors: MECHANIC_GUN_ANCHORS, manifest: MECHANIC_GUN_OVERLAYS, imageKey: mechanicGunImageKey }],
  },
  assassin: { // two daggers on the weaponless body: off hand (optional, offhand slot) behind-first, then main hand (weapon slot).
    // A frame whose anchors are {fallback} on both hands draws the armed baked frame (idle / attack only; walk / dash / hurt
    // have no armed art, their baked frame is the bare body). Whole-frame fallback: a pair is drawn together or not at all.
    bare: ASSASSIN_BARE_ANIMATIONS,
    parts: [
      { id: 'off', slot: 'offhand', optional: true, anchors: ASSASSIN_HELD_ANCHORS.off, manifest: ASSASSIN_DAGGER_OVERLAYS, imageKey: assassinDaggerImageKey },
      { id: 'main', slot: 'weapon', anchors: ASSASSIN_HELD_ANCHORS.main, manifest: ASSASSIN_DAGGER_OVERLAYS, imageKey: assassinDaggerImageKey },
    ],
  },
  warrior: {
    bare: WARRIOR_BARE_ANIMATIONS,
    partial: WARRIOR_WEAPONLESS_ANIMATIONS,
    parts: [
      { id: 'shield', slot: 'offhand', optional: true, bodyFallback: true, anchors: WARRIOR_HELD_ANCHORS.shield,
        manifest: WARRIOR_SHIELD_OVERLAYS, imageKey: warriorShieldImageKey },
      // drawScale: the cut sword art is shorter/slimmer than the roster broadsword; enlarge it around the grip.
      // minSx: cap the side-view horizontal squeeze so the blade keeps its weight.
      { id: 'sword', slot: 'weapon', anchors: WARRIOR_HELD_ANCHORS.sword, manifest: WARRIOR_SWORD_OVERLAYS, imageKey: warriorSwordImageKey,
        drawScale: 1.4, minSx: 0.85 },
    ],
  },
});

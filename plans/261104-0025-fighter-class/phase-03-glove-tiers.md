# Phase 3: Fighter glove tiers and icons

Context: [plan.md](plan.md), `docs/sprite-rules.md` (read all), `Tools/build-assassin-new-style.py` dagger section (`key_pair`, `cut_pair`, `dagger_install`, `install_icons`). Priority: high. Status: **done 2026-11-04 01:10** (agent), report `plans/reports/agent-261104-0030-fighter-glove-tiers.md`. Not seen in game yet (no body sheet, no anchors at the time).

## Requirements
- 10 tiers, each one ChatGPT image: ONE glove, closed fist, side view with the knuckles pointing RIGHT, Normal on the LEFT and Enhanced (brighter, energy) on the RIGHT, flat #00FF00 (magenta for a green glove), no text, 1536x1024. Designs clearly different (MU-like variety): leather (plain brown boxing glove), iron (riveted steel gauntlet), bronze (bronze cestus with studs), tiger (claw-studded tiger-striped), flame (the hero-select flaming red/gold gauntlet), frost (ice crystal gauntlet), thunder (blue lightning coil gauntlet), shadow (black spiked with purple smoke), holy (white-gold winged gauntlet), dragon (red dragon-head gauntlet).
- Cut: `Assets/Resources/Sprites/classes/fighter/weapons/glove/<tier>-base.png` and `-enhanced.png`, 200x200 canvas, glove fitted to ~170 px wide, grip = centre of the fist mass (solid alpha centroid), written to `held-fighter.json` `weapon[i].baseGrip/enhancedGrip` (re-read the file before writing; edit only the grips and `glow`).
- Icons: `Assets/Resources/Sprites/icons/weapon-fighter-0..9.png` and `offhand-fighter-0..9.png` (96x96, same framing as `install_icons`).
- Tool `Tools/build-fighter-gloves.py` (`check OUT`, `install [tier...]`, `icons`, `sheet OUT`).
- Sources in `Assets/ArtLibrary/classes/fighter/new-style/glove-src/<tier>-pair-chatgpt.png`.

## Result
- 10/10 tiers drawn in one chat on the Tay account, 10 image turns, no retry: https://chatgpt.com/c/6aea18f3-4748-83ec-b4f7-379f0507523f ("Vẽ găng boxing"). All on #00FF00 (no green tier, `MAGENTA_KEY` empty). Every image looked at before cutting: both copies side view, knuckles right, thumb up.
- Installed: 20 png + meta in `weapons/glove/`, 20 icons + meta, folder metas `classes/fighter.meta`, `fighter/weapons.meta`, `glove.meta` (copied from the Assassin dagger metas with new GUIDs). `held-fighter.json` grips written (compact one-line format kept, `anchors` untouched, was empty at write time).
- Grips (base = enhanced): leather (122,100), iron (121,100), bronze (123,100), tiger (114,100), flame (121,100), frost (121,100), thunder (125,100), shadow (119,100), holy (126,100), dragon (117,100). The grip is the centroid of the knuckle end (right 60% of the solid width), not of the whole glove, so cuffs and laces do not pull it back to the wrist; the enhanced copy is laid over the base by silhouette match so +N does not move the glove.
- Checked by eye: `sheet` on dark and light (all 20 cut clean, no green fringe, grips on the fist), icons strip (offhand mirrored), 2x check images per tier.

## Success criteria
- `sheet` shows 10 base + 10 enhanced gloves cut clean on dark and light backgrounds, same size, grips marked at the fist centre; icons readable. Met.

## Known limits
- `dragon` enhanced: the flame breath makes it 220 px wide, the last 20 px of the flame are clipped at the canvas edge.
- `flame`: the base's flames count in the 170 px width, so the gauntlet body is about 12% smaller than the other tiers. `thunder` and `holy` have long forearms, so their fists are smaller too (fist about 95 px wide vs 120 for leather).
- `bronze` shows bare skin (a cestus): the hand colour is the glove's, not the hero's.
- Not seen in game: anchor `s` (plan says about 0.18) and `behind` order are phase 2's; the first in-game look must check the glove covers the fist of `body/bare/` and does not float.

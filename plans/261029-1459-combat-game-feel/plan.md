# Combat game feel (melee) - 2026-10-29
Status: done (not committed). Warrior first; systems generic (assassin daggers, skills reuse `world.feel`).

## Design
- `src/data/combat-feel-config.js` - all tuning + `reduceEffects` (shake/flash/ring/squash off).
- `src/systems/combat-feel.js` - pure state: capped hit-stop, weapon trails, pooled spark/ring/star impacts. Lives on `world.feel`.
- `src/graphics/combat-feel-renderer.js` - additive ('lighter') trail ribbon, streak sparks, shock ring, impact star. No blur.
- Camera: trauma shake (quadratic, linear decay) added on top of legacy `shake()`; render-only offset.
- Monster: white opaque flash silhouette, squash+knockback boost. Slash sprites: additive 2nd pass.
- Damage numbers: pop 1.4->1.0, crit bigger + outline (own renderer file).
- Audio: impact sound with random pitch, heavy variant (finisher/crit).

## Phases
1. Config + feel state + tests  2. Camera trauma + hit-stop wiring  3. Renderers (trail, impact, flash, squash, additive slash, numbers)  4. Audio  5. Screens + soak

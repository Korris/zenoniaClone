# Phase 3 - Showcase effects (done, godray skipped)

- Bloom: pixi-filters AdvancedBloomFilter on the FX layer only; runs only while the FX layer has content (cost control). Tuning in `COMBAT_FEEL.pixi.bloom`.
- GPU particles: ParticleContainer batches for spark streaks (+ faded echoes) and hit particles. Ribbon trail: Mesh with preallocated geometry + gradient texture, edge + additive core.
- Finisher/crit shockwave: ShockwaveFilter on the stage for 420 ms, localized by `radius` (a clipped filterArea blanks the rest of the screen, so it is not used).
- Enhanced gear glow: GlowFilter per gear slot, colour = overlay manifest `glow`, strength by enhance band 1..4, pulse on top bands; replaces baked rings; F8 off => baked rings (A/B).
- Not done: GodrayFilter for cleric holy skills (optional).

# PixiJS v8 WebGL world renderer (alongside Canvas2D)

Goal: compare the web game with the Unity prototype (bloom, GPU particles, sword trail, flash). User decision is final: PixiJS.
Status: core + showcase DONE, default stays Canvas2D. Not committed.

| Phase | File | Status |
|-------|------|--------|
| 1 Vendor libs, mode toggle, canvas stacking | [phase-01](phase-01-foundation.md) | done |
| 2 World scene: ground, actors, drops, FX | [phase-02](phase-02-world-scene.md) | done |
| 3 WebGL showcase (bloom, particles, trail, shockwave, glow) | [phase-03](phase-03-showcase-effects.md) | done (godray skipped) |
| 4 Verify: tests, browser checks, soak | [phase-04](phase-04-verification.md) | done |

Key decisions
- WebGL canvas BELOW the 2D canvas; 2D canvas transparent over the world, keeps HUD/UI + legacy overlays (zones, Ice Storm, ultimates, damage numbers).
- No game-logic fork: same state, interpolation, camera, hit-stop. Sprites reuse registry canvases -> Texture once (pre-shrunk mip levels from smooth-sprite-draw).
- Retained nodes (RetainedSet + NodePool), depth via zIndex = y + kind rank.
- Libs vendored in `vendor/pixi/` (pixi 8.22.0, pixi-filters 6.1.5) via import map.

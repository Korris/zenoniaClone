# Phase 4 - Verification (done)

- `npm test`: 377 pass (6 new in tests/pixi-renderer-helpers.test.js: mode selection, depth keys, RetainedSet/NodePool, monsterSpec).
- `node tools/check-pixi-renderer.mjs`: A/B combo shots + 5-class smoke -> tools/screenshots/pixi-renderer/. No page/WebGL errors.
- `node tools/check-pixi-showcase.mjs`: enhance +0/7/9/11/15 live glow vs baked.
- Soak: `node tools/check-combat-soak.mjs 30 warrior wizard --boss --solo --renderer=canvas|pixi` (headed Chrome, noisy machine).

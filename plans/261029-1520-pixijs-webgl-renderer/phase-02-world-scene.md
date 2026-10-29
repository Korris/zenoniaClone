# Phase 2 - World scene (done)

Files: `src/graphics/pixi/` pixi-world-view (stage/layers/camera), pixi-ground-layer (backdrop sprite or baked tilemap + live portals), pixi-drops-layer, pixi-actor-layer + pixi-actor-node + pixi-hero-node (props, NPCs, monsters, hero, held gear, companion, shadow clone), pixi-monster-spec (pure frame rules), pixi-effects-layer (projectiles + VisualEffect strips), pixi-legacy-overlay (2D leftovers).
Small exports added to existing renderers: monster-renderer, player-renderer, prop-renderer, effects-renderer (equipmentDropArt, drawGoldCoin, pickFrame).
Hit flash = swap to the existing white silhouette texture (+ rim), squash via sprite scale; no filter needed.

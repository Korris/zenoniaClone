# Phase 1 - Foundation (done)

- Vendor pixi.js 8.22.0 + pixi-filters 6.1.5 in `vendor/pixi/` (+ licenses); import map in `index.html`; README "Vendored libs".
- `renderer-mode.js` (pure): `?renderer=` > localStorage `zenonia.renderer` > canvas; F9 flips + reloads.
- `main.js`: 2D context `alpha:true` only in pixi mode; lazy `import()` of the backend; F7/F8/F9 keys. `game-controller.js`: optional `worldBackend`, transparent clear while a world is shown.
- `styles.css`: `#pixi-canvas` stacked under `#game-canvas`.
- Hotkey deviation: B is the inventory key, so bloom = F7, FX = F8.

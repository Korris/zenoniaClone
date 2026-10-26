# HD hero sheets (normalized)

Generated from the raw ChatGPT sheets in `assets/hd/hero/*.png` by `tools/build-hd-hero.py`.
Hand-painted HD art (soft alpha, no outline, no palette quantization) -- not the pixel-art
pipeline in `assets/custom/hero/`. Frames are 96x96 px = 24 world units * pixelsPerUnit (4),
so the engine can draw them at the exact same world-space size as the 24x24 pixel hero set.

## Sheets

| File | Frames/row | Size | Scale (per-sheet) |
|------|-----------:|------|--------------------|
| idle.png | 4 | 384x384 | 0.2929 (head x1.00) |
| walk.png | 6 | 576x384 | 0.3571 (head x0.82) |
| attack1.png | 4 | 384x384 | 0.2988 (head x0.98) |
| attack2.png | 4 | 384x384 | 0.3405 (head x0.86) |
| attack3.png | 6 | 576x384 | 0.3755 (head x0.78) |
| dash.png | 3 | 288x384 | 0.2816 (head x1.04) |
| hurt.png | 2 | 192x384 | 0.2153 (head x1.36) |
| death.png | 5 | 480x384 | 0.3405 (head x0.86) |

- Rows: `0 = DOWN`, `1 = UP`, rows 2/3 = the side views. Which side row faces left/right differs
  per sheet (some sheets paint both facing right) -- the engine maps them via `HD_HERO_SIDE_ROWS`
  in `src/graphics/asset-atlas.js`, mirroring the right-facing row where no left one exists.
- Pivot: feet on the bottom row (y = 95), body centered horizontally (x = 48).
- Scale is computed PER SHEET so the HEAD is the same size in every animation: idle's front body is
  82px tall, every other sheet is matched to idle's head by multi-scale template matching
  (tools/hero_head_scale.py). Body height can't be used -- crouched poses (dash, lunges) have short
  bodies and matching them inflated the head. See `scale.json`.
- `hero_face.png` (152x152): HUD portrait + inventory faceset, centered (not bottom-pivoted).

## Rebuild

```
python tools/build-hd-hero.py
# or: npm run hd:build
```

Add a new raw sheet to `assets/hd/hero/<anim>.png` (4 rows x N cols, see FRAME_COUNTS in the
script) and rerun -- missing files are skipped with a warning, existing ones are all reprocessed.
Drop `assets/hd/hero/hero_face.png` (any resolution, square-ish) to (re)generate the portrait.

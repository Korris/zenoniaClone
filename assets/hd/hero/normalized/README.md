# HD hero sheets (normalized)

Generated from the raw ChatGPT sheets in `assets/hd/hero/*.png` by `tools/build-hd-hero.py`.
Hand-painted HD art (soft alpha, no outline, no palette quantization) -- not the pixel-art
pipeline in `assets/custom/hero/`. Frames are 96x96 px = 24 world units * pixelsPerUnit (4),
so the engine can draw them at the exact same world-space size as the 24x24 pixel hero set.

## Sheets

| File | Frames/row | Size | Scale (per-sheet) |
|------|-----------:|------|--------------------|
| idle.png | 4 | 384x384 | 0.2780 (ref 295px) |
| walk.png | 6 | 576x384 | 0.3431 (ref 239px) |
| attack1.png | 4 | 384x384 | 0.2671 (ref 307px) |
| attack2.png | 4 | 384x384 | 0.3071 (ref 267px) |
| attack3.png | 6 | 576x384 | 0.3710 (ref 221px) |
| dash.png | 3 | 288x384 | 0.2752 (ref 298px) |
| hurt.png | 2 | 192x384 | 0.1981 (ref 414px) |
| death.png | 5 | 480x384 | 0.3203 (ref 256px) |

- Rows: `0 = DOWN`, `1 = UP`, `2 = LEFT`, `3 = RIGHT` (same order as the pixel-art hero set).
- Pivot: feet on the bottom row (y = 95), body centered horizontally (x = 48).
- Scale is computed PER SHEET from that sheet's own row0/col0 (DOWN, frame 0) frame height, not
  shared globally -- raw ChatGPT sheets arrive at different native resolutions per file, so a
  single scale derived from idle.png would shrink/grow other animations. See `scale.json`.
- `hero_face.png` (152x152): HUD portrait + inventory faceset, centered (not bottom-pivoted).

## Rebuild

```
python tools/build-hd-hero.py
# or: npm run hd:build
```

Add a new raw sheet to `assets/hd/hero/<anim>.png` (4 rows x N cols, see FRAME_COUNTS in the
script) and rerun -- missing files are skipped with a warning, existing ones are all reprocessed.
Drop `assets/hd/hero/hero_face.png` (any resolution, square-ish) to (re)generate the portrait.

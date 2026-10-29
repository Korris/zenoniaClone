# HD ground tile sheets (normalized)

Generated from the raw sheets in `assets/hd/tiles/*.png` by `tools/build-hd-tiles.py`
(`npm run hd:tiles`). Every cell is cropped inside its painted panel border, LANCZOS-resized to
exactly 64x64px = 16 world units * pixelsPerUnit (4) and seam-healed so it tiles without a
visible lattice -- see the script docstring and `tools/hd_tile_texture_utils.py`. Matches `HD_TILES`
in `src/graphics/asset-atlas.js`.

## Cell map

### grass.png (256x128, 4x2 cells)
- row0: ONE seamless 2x2 grass macro, quadrants in macro order (x0y0, x1y0, x0y1, x1y1); the renderer
- picks col = tx % 2, row = ty % 2 so any 2x2 group of tiles is continuous
- row1: the same 4 quadrants with the painted flowers composited on top (flower tiles, same macro order)

### path.png (320x384, 5x6 cells)
- rows0-2 cols0-2: 3x3 dirt-on-grass auto-tile block, RGBA (grass = transparent, base grass shows through)
- row3 cols0-3: seamless 2x2 dirt macro for open road centers (macro order, picked by tile position)
- col4 rows0-3: inner-corner grass wedge masks TL, TR, BL, BR (alpha only) for concave road corners
- rows4-5: pieces the 3x3 block lacks, keyed by OPEN sides -- (0,4) TB vertical pipe, (1,4) LR horizontal
- pipe, (2,4) T cap, (3,4) B cap, (4,4) L cap, (0,5) R cap, (1,5) island (RGBA, synthesized from edge alphas)

### water.png (192x256, 3x4 cells)
- rows0-2: 3x3 water-on-grass auto-tile block (banks), RGBA (grass = transparent)
- row3: col0-1 = open-water animation variants (brightness-matched to the center), col2 = wooden bridge

### floors.png (256x128, 4x2 cells)
- row0: sand x2, castle stone floor x2 -- each pair is a seamless 2x1 macro
- row1: cave rock floor x2, lava x2 (same)

### plaza.png (128x128, 2x2 cells)
- seamless 2x2 stone macro (castle floor cells + mirrored copies) for the Oakhaven plaza fill

### walls.png (320x64, 5x1 cells)
- col0 cliff top (grass lip), col1 cliff rock face, col2 cliff bottom (shadow), col3 castle brick wall, col4 cave rock wall

### portal.png (192x64, 3x1 cells)
- 3 magic-circle animation frames (real alpha)

## Rebuild

```
python tools/build-hd-tiles.py
# or: npm run hd:tiles
```

Missing raw files are skipped with a warning -- the set can grow one sheet at a time.
# Oakhaven Village visual pass

This pass focuses on reducing the rigid test-map appearance while preserving the 36x26 world and gameplay IDs.

## Changes
- Reworked the main dirt routes into narrower one-tile lanes with stepped turns and short district branches.
- Replaced the square 7x7 plaza fill with an irregular stepped plaza shape around the well.
- Added small flower/grass transition patches at the plaza edge.
- Moved the elder's house closer to the plaza so the central landmark grouping is more coherent.
- Updated `tools/build-hd-tiles.py` so the repeated center dirt tile is rebuilt from the clean inner texture, reducing the baked square-grid stroke on interior path tiles.
- Regenerated normalized HD tile and village-landmark assets.

## Validation
- `npm test`: 116/116 tests pass.
- `python tools/build-hd-tiles.py`: completed.
- `python tools/build-village-landmarks.py`: completed.

## Known limitations
- The generated map overview is an offline composition using the map's current tile and sprite assets, not a screenshot from the running browser game.
- The market stall and other recently added village landmarks are simple vector-style sprites and do not yet fully match the hand-painted fidelity of the houses, trees, and NPCs.
- The path center is less grid-like, but the available dirt artwork still limits how organic the path edges can look. A stronger next pass should author a dedicated seamless dirt texture and more edge/corner path variants.

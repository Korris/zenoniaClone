# Frost Hollow: cold-palette Zenonia-5-style battle map + HD monsters

Date: 2026-10-26. Branch: main. Village (Oakhaven) untouched (only its south-gate portal target changed).

## Goal
New monster-hunting map "Frost Hollow" painted by ChatGPT (cold teal/navy/ice palette, Zenonia 5 look),
wired as the first battle zone after the village, with 3 new HD monsters (frost slime, ice goblin,
spectral wolf) painted in the same chat for a consistent style.

## Source art (ChatGPT, chat "Design Map And Sprites", 2026-10-26)
- `assets/hd/map/FrostHollow/chatgpt-frost-battle-map-full-v1.png` 1536x1024 composite (map + monster strip), plus the two crops
- `assets/hd/map/FrostHollow/ground-raw-v3.png` map-only, full 1536x1024 canvas -> backdrop source (36x24 tiles like Oakhaven)
- `assets/hd/monsters/raw/frost-monsters-sheet-raw.png` 1024x1536 portrait, alpha recovered from black/white captures; split into
  `frostSlime.png`, `iceGoblin.png`, `spectralWolf.png` (4 cols x 3 rows each)
- Capture method: Claude-in-Chrome blocks downloads/base64/signed URLs, so each image was isolated in the page at 1 CSS px =
  1 capture px and zoom-captured in tiles, stitched pixel-exact (overlap diff = 0). ChatGPT free tier hit its image limit
  after these 3 generations.

## Phases
| # | Phase | Status |
|---|-------|--------|
| 1 | Map-only painting (v2) + monster sheets from ChatGPT, captured to disk | done |
| 2 | `tools/build-frost-hollow-art.py` (`npm run hd:frost`): 2304x1536 ground.webp + labelled tile-grid preview; GROUND_ROWS classified by eye | done |
| 3 | `src/world/maps/frost-hollow-map.js` (backdrop, rows, 2 portals, 6 spawn zones), map-registry, village portal -> frostHollow (4,21), meadow west portal -> frostHollow (32,4), exit gated Lv 2 | done |
| 4 | HD monsters: `tools/build-hd-monsters.py` (`npm run hd:monsters`), `asset-atlas-hd-monsters.js`, `hd-monster-sprite-builder.js`, `monstersHd` in sprite-registry, HD path in monster-renderer, 3 MONSTER_DEFINITIONS + pixel fallbacks in MONSTER_SHEETS | done |
| 5 | Tests: `frost-hollow-layout.test.js`, `hd-monsters-config.test.js` (143 pass); `tools/frost-hollow-browser-check.mjs` headless run clean; `browser-check.mjs` fixed for class select | done |
| 6 | Docs: README, system-architecture (backdrop maps + HD monsters), codebase-summary, project-roadmap v0.12 | done |

## Verification
- `npm test`: 143/143 pass.
- `node tools/frost-hollow-browser-check.mjs`: village portal -> Frost Hollow (4,21); 12 frost slimes, 4 ice goblins, 4 spectral
  wolves spawned; backdrop + 3 HD sheets loaded; Lv 1 hero bounced at the exit, Lv 2 passed to Verdant Meadow; 0 console errors.
- `node tools/browser-check.mjs`: boots and plays through; its village walk / elder-dialog assertions fail identically on the
  pre-change code (verified via git stash), i.e. pre-existing since the class-select commit.

## Unresolved / follow-ups
- Pre-existing `browser-check.mjs` village walk failures (key input ignored right after new game?) need their own debug pass.
- Monsters have no rear (UP) pose; the front row is shown, like the HD NPCs. Ask ChatGPT for back views if it ever matters.
- ChatGPT could not be asked for a higher-res redraw (image quota); backdrop is the 1.5x upscale path (softer than Oakhaven's stitched quadrants).

## Revision v3 (2026-10-26 late, ChatGPT Plus account "Tan Pham", Chrome "Browser 1")
- User feedback: v2 did not look like Zenonia 5. Repo refs zenonia1a/b/c are Zenonia 1 (pixel remake), not 5.
- ChatGPT researched real Zenonia 5 screenshots + the original environment artist portfolio (Sunmin Kim, "The Art of Zenonia 4, 5, S") and painted a bright hand-painted winter field: `ground-raw-v3.png`.
- New monster sheets in the same style: `assets/hd/monsters/raw/{frostSlime,iceGoblin,spectralWolf}.png` (old ones in `raw/v1/`). Wolf's left row was painted facing right -> mirrored from the right row (`MIRROR_LEFT_FROM_RIGHT`).
- GROUND_ROWS fully re-classified (entrance portal (5,19), exit (33,2), bridge row 12 cols 17-21, 5 spawn zones). 143/143 tests, headless check clean.

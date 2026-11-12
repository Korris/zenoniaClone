# Phase 02: UI art (cut / generate)

## Context
- Read `docs/sprite-rules.md` fully before any art work (ChatGPT checkerboard / too-transparent errors; log new errors in its "So loi" section).
- Mockups in `docs/reference/`; source + prompt `Assets/ArtLibrary/custom/character-flow/`.
- ChatGPT: account Tay (Plus); fallback Tan Pham. Never xaochai / Tieu Duy Phuong.

## Overview
Priority P2. Art only, no code. Can run parallel with phase 01 (no shared files).

## Reuse (no new art)
| Need | Existing sprite |
|------|-----------------|
| class banners | `ui/hero-select/banner-<class>.png` |
| portraits (info panel) | `ui/faces/<class>.png` (boxing: check exists, else hero-select art crop) |
| panels, frames | `ui/win-panel.png`, `win-section.png` (9-slice) |
| gold / navy buttons | `ui/win-btn-gold.png`, `win-btn-navy.png`, `hero-select/btn-start.png` |
| hairstyle tile frame | `ui/win-slot.png` or `hero-select/icon-frame.png` |
| name field | `ui/win-chat-input.png` |
| gear | `ui/hud-settings.png` |
| paging arrow | `hero-select/arrow-left.png` (mirrored) |
| background | `hero-select/bg.png` if it matches the ruins look; else new `bg.png` |

## New art -> `Assets/Resources/Sprites/ui/character-flow/`
- `pedestal.png` (stone pedestal), `pedestal-glow.png` (selected, blue ring) - generate, transparent bg
- `create-slot.png` (arched glass tablet with gold "+", no text: text drawn by code)
- `btn-red.png` (Delete, 9-slice-able)
- `icon-class.png`, `icon-level.png`, `icon-location.png` (info rows) - cut from mockup, 32-48 px OK
- `dice.png` (random name)
- `swatch.png` (white disc, tinted in code) + `swatch-ring.png` (selected ring)
- `label-next.png` (if hero-select keeps image labels; else text on btn)
Sources + prompts -> `Assets/ArtLibrary/custom/character-flow/` (`prompt-02-*.txt`, raw PNGs). Move with `.meta`; set import: Sprite, no compression for UI, FilterMode as other `ui/` sprites.

## Steps
1. Read sprite-rules.md. 2. Compare `hero-select/bg.png` to mockup bg. 3. Cut small icons from `character-flow-board-chatgpt-1.png` (Python + PIL, script in scratchpad or `Tools/` if reusable). 4. Generate pedestal, create-slot, btn-red, dice on ChatGPT (attach mockup). 5. Check alpha edges on dark + light backgrounds. 6. Copy import settings from an existing `ui/*.png.meta`.

## Todo
- [ ] bg decision
- [ ] cut icons
- [ ] generate pedestal/glow/create-slot/btn-red/dice
- [ ] swatch + ring
- [ ] import settings, sources archived

## Success criteria
All files above exist, alpha clean (no checkerboard, not washed out), viewed by eye on a dark composite.

## Risks
ChatGPT art too transparent / baked checkerboard (known). Mitigation: rules doc + alpha check script.

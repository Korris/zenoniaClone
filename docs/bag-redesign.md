# Bag redesign — 2026-11-05

Implemented from the user's supplied navy-and-gold three-column reference. Uses existing painted window, button, item and character assets; no new image generation was needed.

- Rebalanced character / inventory / detail widths to 300 / 350 / 290 canvas units. The 1000 x 556 window scales down to fit the canvas.
- Added character backdrop, gold dividers, stronger rarity outlines, quantity shadows and corner locks for unavailable potions.
- Detail pane has a 96-unit item portrait, rarity label and STAT / ITEM / EQUIPPED / DIFF table. Values use effective enhanced stats. Enhancement preview and Discard / Equip / Enhance share the lower pane.
- Existing inventory, filtering, paging, equipment, potion, enhancement and online actions remain wired to their original handlers. Dev-only downgrade stays separate.
- Hidden-window inventory screenshots render the portrait and UI canvas explicitly into render targets, restoring camera/canvas state afterward.

Validation: Windows build from `C:/temp/zu-copy` succeeded with zero errors. Captured Mechanic at 1672 x 941 and Warrior at 960 x 640. The smaller capture exposed sword/class-label overlap; moved the label below the portrait. Player logs showed no exceptions. `git diff --check` passed.

Test player: `Build/bag-redesign/Zenonia.exe`. Review images: `docs/visuals/bag-redesign/`.

## Visual correction after user review

The first pass was insufficiently close to the reference. Added bundled Crimson Text Regular/Bold (Google Fonts, SIL OFL; license included) specifically for the bag's serif typography. This is a close visual substitute, not an identified original font file from the generated reference.

Increased inventory cells from 62 to 72 units and equipment icon boxes from 50 to 68 units; centre pane is now 402 units wide and the full window 1052. Worn cells are 68 with 62-unit icons. Added a pointed gold rarity plaque; the pane title strips the rarity prefix while retaining the weapon's tier name and enhancement suffix. Underlying item names and save data are unchanged. The plaque uses the existing rarity's display prefix (epic -> LEGENDARY).

Replaced the incorrect composite-frame bag crop with the standalone transparent `bag-icon-v3.png`, generated using built-in Imagegen from the supplied reference. Its prompt is in `Assets/ArtLibrary/custom/inventory-ui/bag-icon-v3-prompt.txt`. Sprite cropping removes only its own transparent margins. Captured review: `visuals/bag-redesign/mechanic-v2_1.png`.

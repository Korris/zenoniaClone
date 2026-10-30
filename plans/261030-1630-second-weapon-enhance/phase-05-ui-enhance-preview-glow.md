# Phase 05 - UI (bag / loadout / enhance preview) + second-weapon glow

## Context links
- `InventoryPanel.cs` (263 lines; `EnhanceSelected`, `EnhanceView Preview` at ~212-237), `InventoryPanel.Bag.cs` (248; item detail + enhance line
  213-232), `InventoryPanel.Loadout.cs` (113; equip slots incl. offhand, loadout Enhance button + text), `BagWidgets.cs`, `Rpg/SecondWeaponText.cs` (phase 01)
- Visuals: `HeroClassDef.cs:62` warrior `GearPartSpec("shield","shield")` (glow=false), assassin `off` part (glow=true), `HeroRenderer.cs:109-125,179-199`
  (glow per part, companion tier/enhanced), `HeroCompanion.cs` (robot/spirit art, enhanced variant at band > 0), `HeldGlowBaker.cs` (`Get(folder, entry, band)`)

## Overview
P2 | after 01 | parallel with lane A | Loadout already shows both weapons and Enhance works on whichever equip slot is selected; this phase makes
the offhand preview speak its own stats and gives offhand visuals the enhance glow.

## Key insights
- No new enhance window needed: selecting the Weapon or Offhand equip slot (or a bag item) already targets the Enhance button.
  Add a clear "Main weapon" / "Second weapon" caption so players see which one they enhance.
- `InventoryPanel.cs` > 200 lines and owns Preview -> extract `InventoryPanel.Enhance.cs` partial (EnhanceSelected, EnhanceView, Preview) while editing.
- Glow today: sword/staff/gun/daggers yes, warrior shield NO (spec glow=false), companions only swap to enhanced art (no band rings), spellbook has no held art.
- Remote viewers already receive offhand enhance (Appearance nibble) -> glow shows for other players with zero net work.

## Requirements
- Offhand preview: "+6 -> +7  80%  Stone x12" + per-stat lines from `SecondWeaponText.Preview` ("DEF +5.5% -> +7.3%"), also combo line
  ("Earth Break shield 5.5% -> 5.9% HP").
- Bag detail for offhand: specialised stat lines instead of the old scaled base-stat delta.
- Status table: unchanged columns (DEF/HP/MP/crit already reflect phase 01).
- Glow: warrior shield gets band glow; robot/spirit companions get band rings (+7/+9/+11/+13); wizard spellbook: icon "+N" only.

## Architecture / files
Create
- `Assets/Scripts/InventoryPanel.Enhance.cs` - moved Preview/EnhanceSelected; offhand branch uses `SecondWeaponText`.
Modify
- `InventoryPanel.cs` - remove moved block (gets < 200 lines).
- `InventoryPanel.Bag.cs` - offhand detail lines via `SecondWeaponText.Lines(item, classId)`.
- `InventoryPanel.Loadout.cs` - caption "Main weapon" / "Second weapon" on the enhance text for slots Weapon / Offhand.
- `HeroClassDef.cs` - warrior shield `GearPartSpec("shield","shield", 1f, 0f, true)` (needs `glow` colour in held-gear manifest for shield tiers; verify `HeldGearData`).
- `HeldGlowBaker.cs` - `Get(Sprite src, Color glow, int band)` overload (cache by sprite instance id + band), reusing the ring bake.
- `HeroCompanion.cs` - child glow SpriteRenderer, band from `HeroRenderer` (`EnhanceStats.VisualBand(offhand)`), pulse at band 4 like held gear.
- `HeroRenderer.cs` - expose `CompanionBand` next to `companionEnhanced` (2 lines).

## Implementation steps
1. Extract InventoryPanel.Enhance.cs (pure move) -> compile.
2. Offhand preview + bag lines + loadout caption.
3. Shield glow flag (+ manifest check); HeldGlowBaker sprite overload; companion glow.
4. Captures: `DebugCapture --plus 7/9/11/13` per class -> `plans/261030-1630-second-weapon-enhance/visuals/` (screens for review).

## Todo
- [ ] InventoryPanel.Enhance.cs split
- [ ] offhand preview / bag lines / caption
- [ ] shield glow, baker overload, companion glow
- [ ] glow captures 5 classes x 4 bands

## Success criteria
- Selecting offhand slot shows specialised stats and enhances the offhand (offline + client via InvOp.Enhance).
- Captures: shield + companions show 4 distinct bands; main weapon glow unchanged (pixel diff on sword capture = 0).

## Risks
| Risk | LxI | Mitigation |
|---|---|---|
| Shield art lacks glow colour in manifest | M x L | default to class palette colour |
| Companion glow cost on mobile (bake per frame of animated companion) | M x M | bake only the static tier art; animated tier 0 gets no rings |
| UI text overflow on 1-line enhance label | M x L | two-line label like ItemInfo.EnhancePreview |

## Security
UI only; enhance still validated server-side.

## Rollback
Revert commit; the move-only split can stay.

## Next steps
07 captures + APK.

## Unresolved questions
- Spellbook held/floating art (none exists): order a ChatGPT batch later, or icon-only is fine?

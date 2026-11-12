# Phase 04: Choose Class (Back/Next) + Customize and Name

## Context
- `HeroSelectScreen.cs` (`OpenOnlineCreate(taken, picked, back)`, `ShowState` greys taken classes, `OnBack`, `OnStart`), `HeroSelectScreen.Build.cs` (`BuildStartPanel`).
- `HeroHairLayers` (`CountStyles`, `Face`, `LoadCell`, presets), `HairDye.Apply`, `HeroHeadLayer` (full heads `heads/<n>` for assassin/cleric/boxing).
- Mockup panels 2 and 3 of the board.

## Requirements
Choose Class (online mode only; offline look unchanged):
- Start button label -> NEXT; visible BACK button (navy) -> `onlineBack`. Taken class: NEXT disabled + "You already have this hero" (as today).
Customize and Name:
- Left panel HAIRSTYLE grid (2 cols x 3 rows, 6 tiles; full-head classes show their 5). Tile = head preview of that style with current dye.
- HAIR COLOR swatches: 9 presets from `hair-dyes.json` + "default" swatch (-1). Disabled with note "Colour not available for this class yet" when class has no layered hair.
- Centre: class banner + full-body preview (HeroPreviewStage, or fallback big head preview) updated live.
- Right: CHARACTER NAME field (max 16), dice -> `AccountFlow.RandomName()`, hint "3-16 letters, digits or _", inline `NameProblem` error.
- BACK -> reopen Choose Class with same class selected; CREATE -> `AccountFlow.Create(name, class, style, dye)`; Enter key submits. Busy disables; server errors (`name_taken`, `class_taken`) shown in the status line, stay on page with input kept.
- Defaults on open: style 0, dye -1, empty name.

## Architecture
- `HairPreviewSprites` static: `Layers(HeroClass, style, dye)` -> ordered sprites (hair-under, hair-back, face, hair front) for layered classes, or one full head sprite; cached per (class, style, dye). Reuses `HeroHairLayers.LoadCell` + `HairDye.Apply` (DRY, no new tinting code). Needs `HeroHairLayers` to expose presets read-only (`PresetColor(i)`) or a static `LoadPresets(classRoot)`.
- `CharacterCustomizeScreen` uGUI (same pattern as phase 03); visible when `Visible && Page == Customize`. IMGUI `DrawNaming` deleted.

## Files
Create:
- `Assets/Scripts/Net/Client/CharacterCustomizeScreen.cs` (logic)
- `Assets/Scripts/Net/Client/CharacterCustomizeScreen.Build.cs` (layout)
- `Assets/Scripts/HairPreviewSprites.cs`
Modify:
- `Assets/Scripts/HeroSelectScreen.cs` / `HeroSelectScreen.Build.cs` (online: NEXT label, BACK button; keep under 330 lines, no growth beyond ~15 lines)
- `Assets/Scripts/HeroHairLayers.cs` (static preset loader, small)
- `Assets/Scripts/Net/Client/AccountScreenGui.cs` (delete DrawNaming)
- `Assets/Scripts/Net/Client/ConnectScreenGui.cs` (show/hide Customize; `CreateHero` back path returns to list)

## Steps
1. HeroSelectScreen online Back/Next; run `--hero-select-test` (offline path must be unchanged).
2. HairPreviewSprites + preset loader; check one class of each kind by capture.
3. Customize layout + wiring; delete DrawNaming.
4. Compile clean.

## Todo
- [ ] Back/Next in HeroSelectScreen
- [ ] HairPreviewSprites + presets
- [ ] Customize layout
- [ ] wiring + errors
- [ ] remove IMGUI naming

## Success criteria
Create a hero of a layered class with style 3 + preset 2 -> list shows it with that look -> Enter -> own hero shows it, other client shows style 3 (colour default, expected until phase 07). Full-head class: 5 tiles, colour row disabled.

## Risks
- Head preview pivot: cells are 192 px with bottom-centre pivot; UI Image needs `preserveAspect` and a crop rect on the head area (compare with `docs/sprite-rules.md` head anchor notes). Med x Low.
- Dye cache memory: 6 styles x 10 dyes x 4 layers per class max, small; clear on hide.

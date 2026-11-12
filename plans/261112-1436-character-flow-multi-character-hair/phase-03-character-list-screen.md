# Phase 03: Character List screen (uGUI)

## Context
- Pattern: `Assets/Scripts/HeroSelectScreen.cs` + `HeroSelectScreen.Build.cs` (`UiKit.Canvas`, `UiKit.Fill`, `UiKit.Rect/Label/Button`, `FitStage`).
- `ConnectScreenGui.Draw` -> `AccountScreenGui.Draw` (IMGUI) routes by `AccountFlow.Page`.
- Mockup: `docs/reference/character-flow-screen-1-character-list.png`.

## Overview
Priority P2. Replace IMGUI `DrawCharacters` with a uGUI screen. Depends on 01 (prefs, EnteredId) and 02 (art).

## Requirements
- Title plaque "CHARACTER LIST", gear top-right (menu: Log out, Play offline, Direct connect (dev) when `ConnectScreenGui.DevPageAvailable`).
- 4 pedestals per page, paging arrows when more. Each owned hero: banner of its class, full-body preview with its look, nameplate NAME + "Lv N", "(online)" tag when `online`.
- Empty pedestal: "Create Character" tablet -> `ConnectScreenGui.CreateHero(Characters)`; disabled when all 6 classes owned (one-per-class rule today).
- Selected hero: glow pedestal; bottom panel portrait, name, Class, Level, Last Location (`mapId` -> display name; find existing map name lookup, else raw id), ENTER GAME (`AccountFlow.Enter`), DELETE (`AccountFlow.ShowDelete` -> IMGUI confirm page stays).
- Empty account: nothing selected, panel says "No heroes yet: create your first." Loading: "Loading ...". `AccountFlow.Status` shown (bad in red), buttons disabled while `Busy`.
- Selection remembered across refresh (by id); default first hero.
- Screen visible only when `ConnectScreenGui.Visible && Page == Characters`; IMGUI skips that page.

## Architecture
- `CharacterListScreen` MonoBehaviour, singleton like HeroSelectScreen; `Show()/Hide()`, rebuilds pedestals on `Characters` change (compare count + ids + rev each frame or a `AccountFlow.ListVersion` int bumped in Refresh - prefer the int, cheaper).
- Full-body preview: `HeroPreviewStage` renders hero(es) into a RenderTexture shown by RawImage. Spike first (1h): can a bare `HeroRenderer` be created without `HeroController` and set class + `HairStyle` + `HairDye`, idle facing down? If yes: one preview camera far off-world (e.g. x=10000), orthographic, renders up to 4 heroes side by side into one RT. If no: fallback = static `hero-select/hero-<class>.png` on pedestal and hair shown only in Customize (state this in report).

## Files
Create:
- `Assets/Scripts/Net/Client/CharacterListScreen.cs` (logic, <200)
- `Assets/Scripts/Net/Client/CharacterListScreen.Build.cs` (layout, <200)
- `Assets/Scripts/HeroPreviewStage.cs` (shared with phase 04)
Modify:
- `Assets/Scripts/Net/Client/AccountScreenGui.cs` (remove `DrawCharacters`; Characters page drawn by uGUI)
- `Assets/Scripts/Net/Client/ConnectScreenGui.cs` (show/hide CharacterListScreen with page + Visible)
- `Assets/Scripts/Net/Client/AccountFlow.Characters.cs` (`ListVersion` bump)

## Steps
1. Spike HeroPreviewStage; decide live vs fallback.
2. Build layout (scale with `FitStage` approach, 16:9 and 4:3 checked).
3. Wire data + buttons; gear menu.
4. Remove IMGUI DrawCharacters.
5. Compile clean; capture via phase 05 flag.

## Todo
- [ ] preview spike + decision
- [ ] layout
- [ ] wiring, paging, gear
- [ ] IMGUI removal
- [ ] compile clean

## Success criteria
Screenshot matches mockup layout (title, pedestals, panel, buttons); enter/delete/create/logout work against a local server-api + zone.

## Risks
- Canvas sort vs IMGUI overlay (IMGUI draws on top of uGUI): status/confirm pages must hide the list. Med x Low.
- RenderTexture lifetime leak on scene reload: release in OnDestroy; `UiKit.ResetState` pattern.
- Touch input: buttons are uGUI so TouchControls must not eat taps (HeroSelectScreen already works the same way).

# Phase 01: flow logic + local look prefs

## Context
- `Assets/Scripts/Net/Client/AccountFlow.cs` (395 lines; pages, Create/Delete/Enter, `enteredId` private)
- `Assets/Scripts/Net/Client/ConnectScreenGui.cs` (`CreateHero` -> `HeroSelectScreen.OpenOnlineCreate` -> `ShowNaming`)
- `Assets/Scripts/HeroController.Net.cs:30` (`if (local) LocalPlayer.Set(this)`), `SyncHairStyleOnline`
- `Assets/Scripts/HeroRenderer.cs` (`HairStyle`, `HairColor`), `HeroHeadLayer.cs` (`HairDye`), `HeroHairLayers.cs`

## Overview
Priority P2. No UI change visible yet except the old IMGUI Naming page now also carries style/dye defaults. Compiles and is testable alone.

## Requirements
- `AccountPage.Naming` renamed `Customize`. `AccountFlow` holds `CustomizeClass`, `PendingStyle` (int, default 0), `PendingDye` (int, -1 = style colour).
- `Create(name, class)` -> `Create(name, class, style, dye)`; on 201 store look under `r.Data.character.id`. Request body unchanged (server not touched).
- `Delete` success -> remove prefs of that id.
- `public static string EnteredId` getter (existing `enteredId`).
- `RandomName()` for the dice: syllable list, 4-10 chars, passes `NameProblem`. No server round trip; a taken name still returns `name_taken` -> existing friendly message.
- Local hero online applies prefs on spawn; style then syncs by the existing `SyncHairStyleOnline`. Colour local only.
- Offline flow untouched.

## Architecture
`CharacterLookPrefs` (static): key `zenonia.look.<characterId>` -> `"style,dye"`. `Parse(string)`/`Format(int,int)` pure (testable); `Get(id, out style, out dye)` returns defaults (0, -1) when missing; `Set`, `Remove`. Clamp on read (style wraps in renderer already; dye outside -1..presets-1 -> -1).
`HeroRenderer.HairDye` passthrough to `HeroHeadLayer.HairDye` (preset index).

## Files
Create:
- `Assets/Scripts/CharacterLookPrefs.cs`
- `Assets/Scripts/Net/Client/AccountFlow.Characters.cs` (partial: move Refresh/Create/Delete/Enter + RandomName out of AccountFlow.cs; brings both files under ~250/200)
Modify:
- `Assets/Scripts/Net/Client/AccountFlow.cs` (partial, page rename, `EnteredId`)
- `Assets/Scripts/Net/Client/AccountScreenGui.cs` (`DrawNaming` -> calls new Create signature; kept as fallback until phase 04)
- `Assets/Scripts/Net/Client/ConnectScreenGui.cs` (`ShowNaming` -> `ShowCustomize`)
- `Assets/Scripts/HeroController.Net.cs` (local + online + `AccountFlow.EnteredId != null` -> `CharacterLookPrefs.ApplyTo(view, id)`)
- `Assets/Scripts/HeroRenderer.cs` (+`HairDye` property, ~4 lines)
- Any test bot / DebugCapture call of `AccountFlow.Create(` (grep) -> new signature

## Steps
1. Split AccountFlow into partial (pure move, compile).
2. Add CharacterLookPrefs + HeroRenderer.HairDye.
3. Page rename, pending look fields, Create/Delete changes, EnteredId, RandomName.
4. Apply prefs on local spawn (HeroController.Net). Verify `LeaveWorld` clears `enteredId` after the hero is gone, not before spawn.
5. Grep callers; check `Editor.log` latest compile clean.

## Todo
- [ ] partial split
- [ ] CharacterLookPrefs
- [ ] HeroRenderer.HairDye
- [ ] Create/Delete/EnteredId/RandomName
- [ ] apply on spawn
- [ ] callers updated, compile clean

## Success criteria
- Compile clean (latest compile in Editor.log). `dotnet test` both suites green.
- Create via old IMGUI page -> PlayerPrefs has `zenonia.look.<id>`; Enter -> hero shows that style; other client sees the style.

## Risks
- `HeroController.Net.cs:30` may run before `view` exists -> apply where `view` is set (check order). Low x Med.
- Proxy spawn of a party/remote hero must not get prefs: guard on `local`.

## Security
Prefs hold only ints, no tokens. Values clamped on read.

## Next
Phase 03 uses `CharacterLookPrefs.Get` for pedestals.

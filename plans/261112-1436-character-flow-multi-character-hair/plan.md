---
title: "Character flow redraw: list, choose class, customize and name (client first)"
description: "Redraw the online character pages per the approved mockups, add a hairstyle + hair colour step kept locally per character; server persistence deferred."
status: pending
priority: P2
effort: 16h (client 01-05); deferred 06-07 ~8h
branch: unity
tags: [client, ui, account-flow, hair, online]
created: 2026-11-12
---

# Character flow (client UI now, server later)

Mockups: `docs/reference/character-flow-list-class-customize-board.png`, `docs/reference/character-flow-screen-1-character-list.png`
Art source + prompt: `Assets/ArtLibrary/custom/character-flow/`

## Scope (2026-11-12, user)
- NOW: client only. Redraw Character List, Choose Class (Back/Next), Customize and Name. Wired to the EXISTING `AccountFlow` calls (list / create(name, class) / delete / enter).
- One hero per class still applies: owned classes greyed out, `class_taken` still shown.
- Hairstyle + colour chosen in Customize: kept in PlayerPrefs per character id, applied to the local hero after entering. Style already reaches other players via `InvOp.HairStyle`; colour stays local until phase 07.
- Offline mode: unchanged (keeps the current hero select, H key). Reason: offline save has no hair field; adding it twice is waste before the server shape is fixed.

## Key decisions
- Account pages stay IMGUI (`AccountScreenGui`) for Login/Register/Forgot/ConfirmDelete. Characters + Customize become uGUI built in code with `UiKit`, the same way `HeroSelectScreen(.Build).cs` is built.
- Choose Class = the existing `HeroSelectScreen` online mode (`OpenOnlineCreate`), relabelled Start -> Next, Back added. No new class screen (DRY).
- Hair colour = preset index (`-1` = the style's own colour, `0..8` = `hair-dyes.json` presets), same as `HeroHairLayers.Dye`. Bounded, 1 byte on the wire later, validatable by the server, no free colour to moderate.
- Name rule unchanged: 3-16 letters, digits, `_` (server `createCharacterSchema`). The mockup says "3-12 letters": label follows the real rule.

## Phases
| # | Phase | Status | Effort | Depends |
|---|-------|--------|--------|---------|
| 01 | [Flow logic + local look prefs](phase-01-flow-logic-and-local-look-prefs.md) | pending | 3h | - |
| 02 | [UI art: cut / generate sprites](phase-02-character-flow-ui-art.md) | pending | 3h | - (parallel with 01) |
| 03 | [Character List screen](phase-03-character-list-screen.md) | pending | 4h | 01, 02 |
| 04 | [Choose Class Back/Next + Customize and Name](phase-04-choose-class-and-customize-screen.md) | pending | 4h | 03 |
| 05 | [Verification and captures](phase-05-verification-and-captures.md) | pending | 2h | 04 |
| 06 | [DEFERRED, not approved: server-api multi-character + persisted look](phase-06-deferred-server-api-multi-character-and-look.md) | deferred | 4h | user approval |
| 07 | [DEFERRED, not approved: protocol + zone hair colour and load-on-join](phase-07-deferred-protocol-and-zone-look.md) | deferred | 4h | 06 |

## Data flow (client now)
List: `AccountFlow.Refresh` -> `Characters` -> CharacterListScreen pedestals (look from `CharacterLookPrefs`).
Create: List "+" -> HeroSelectScreen (taken classes greyed) -> Next -> Customize (style, dye, name) -> `AccountFlow.Create(name, class, style, dye)` -> API create -> on 201 save prefs under the new `character.id` -> back to List.
Enter: `AccountFlow.Enter` -> `EnteredId` -> local `HeroController` spawn applies prefs -> `SyncHairStyleOnline` sends style.
Delete: `AccountFlow.Delete` -> 200 -> prefs for that id removed.

## Risks (top)
- Classes without layered hair (assassin, cleric, boxing): 5 full heads, not dyeable. Customize shows their 5 heads, colour row disabled with a note. (Med x Low)
- Live full-body preview: `HeroRenderer` off-scene may need a controller; spike first, fallback to static art (phase 03). (Med x Med)
- Wire protocol and Mongo index: untouched now; only deferred phases carry them (see 06/07).
- Skill table shared with server: no skill numbers change in any phase.
- Files over 200 lines touched: `AccountFlow.cs` (395) split, `AccountScreenGui.cs` (277) shrinks, `HeroRenderer.cs` (350) +4 lines only, `DebugCapture.NetPersist.cs` (662) not grown (new partial).

## Rollback
Each client phase is one commit; revert restores IMGUI pages. Prefs keys are namespaced (`zenonia.look.*`) and harmless if left.

## Unresolved questions
1. Pedestal count: 4 per page (mockup) with paging, or 6 on one row (all classes fit today)? Plan assumes 4 + paging.
2. Gear button on the list: plan puts Log out / Play offline / server URL (dev) there. OK?
3. Slot cap for phase 06: recommend 6 (one per class today, no account has more, mockup pages of 4). Confirm or pick 8.
4. Should the in-game H key keep changing the hairstyle once looks are persisted (phase 07), or become dev-only?
5. Hair art for assassin, cleric, boxing (layered + dyeable): schedule as a separate art task?

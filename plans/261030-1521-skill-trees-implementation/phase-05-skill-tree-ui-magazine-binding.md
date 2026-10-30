# Phase 05 - Skill tree window (gold ChatGPT style) + magazine binding

## Context links
- Window style: `UiWinKit.cs` (Anchored, Sliced, ArtButton, CloseButton, Gold/Cream/Dim), `UiArt.Win(name)`, sprites `Resources/Sprites/ui/win-{panel,section,slot,tab-active,tab-inactive,btn-gold,btn-navy,close,nameplate}.png`
- Reference windows: `QuestOfferWindow.cs`, `WindowsHost.cs`, `UiWindows.cs` (`BlocksGameplay`), `LockGlyph.cs`
- Entry today: `InventoryPanel.cs:144` (Skill tab flashes "not in the Unity port yet")
- Magazine: `SkillMagazine.cs` (static table, `MaxPages = 2`), `SkillMagazineView.cs`, `SkillMagazineSlot.cs` (reads `cls.SkillMp`, `hero.SkillCooldownLeftMs`), `TouchControls.cs` (`Press`, `SkillSprite`)
- Online request path: `NetClient.SendInvAction(op, uid, slot)` (as in `InventoryPanel.Bag.cs:107`)

## Overview
Priority P1 | Lane C | window part after 01; magazine part after 04 (needs `GameKey.Skill1..8`). Owns UI files only.

## Key insights
- Dash + 8 skills = exactly 3 pages x 3 slots: page 1 slot 1 = Dash (fixed), other 8 slots = `SkillLoadout` (player-assignable). `MaxPages` 2 -> 3.
- Window works the same offline/online: an `ISkillTreeActions` seam: offline = `PlayerRpg.ApplyInvAction` + save; client = `NetClient.SendInvAction` and refresh on RpgState.
- Panel order on screen = preview/icon order (2 rows x 4) so art, design table and UI line up; tier shown by node frame colour + row header band.

## Requirements
- Header: class name, "SP 3 / 24", Close; Lv shown. Grid 2 x 4 nodes (icon 72 px in `win-slot`, rank badge "3/10", lock glyph + "Lv 26" when locked, gold pulse when learnable).
- Prereq lines between nodes (thin stretched `Image`, grey unmet / gold met), drawn under nodes.
- Detail pane (right): name, tier, unlock level, prereqs (red when unmet), MP/CD/cast, effect lines current -> next rank (`SkillText`), buttons `Learn` / `Rank up` (gold, disabled with reason), `Assign`.
- Assign mode: strip of the 8 assignable magazine slots (3 pages, slot 1 of page 1 shows the fixed Dash); tap a slot to place/swap.
- Open: Skill tab in the bag, key T (`GameKey.SkillTree`), touch button "Skills" (top-right next to Bag). Pauses offline like the bag; `UiWindows.BlocksGameplay` while open.
- Landscape phones (960x640 reference) and 1280x720 editor: fits without scrolling.

## Architecture
```
SkillTreeWindow (WindowsHost child) --reads--> PlayerState.Skills/Loadout + SkillTable.ForClass + SkillText
   node tap -> SkillTreeDetailPane.Show(def)  -> Learn -> ISkillTreeActions.Learn(index) -> (offline) PlayerRpg / (client) SendInvAction(SkillLearn)
   Assign -> SkillLoadoutStrip -> ISkillTreeActions.Assign(index, slot)
RpgState / local change -> SkillTreeWindow.Refresh()  ;  SkillMagazine reads PlayerState.Loadout -> SkillMagazineView rebinds
SkillMagazineSlot: icon from Resources/Sprites/skills/<class>/<skill>/icon (fallback skill-icons/<class>-N), MP = def.MpAt(rank), cd = hero.Skills.CooldownLeft(i)
```

## Related code files
Create: `SkillTreeWindow.cs`, `SkillTreeNodeView.cs`, `SkillTreeLinks.cs`, `SkillTreeDetailPane.cs`, `SkillLoadoutStrip.cs`, `SkillTreeActions.cs` (interface + offline/online impls), `SkillIcons.cs` (icon lookup + fallback)
Modify: `SkillMagazine.cs` (table from loadout, 3 pages, `SkillSlot.SkillIndex`), `SkillMagazineSlot.cs` (per-skill MP/cd/icon, locked when rank 0), `SkillMagazineView.cs` (3 dots, rebind on loadout change),
`InventoryPanel.cs` (Skill tab opens `SkillTreeWindow`: 1 line), `WindowsHost.cs` (host + key T), `TouchControls.Menu.cs` ("Skills" button), `Hud.cs` (debug skill line lists magazine slots), `RpgHud.cs` (toast "+1 SP" on level-up, SP badge on the Skills button)

## Implementation steps
1. `SkillIcons` + `SkillTreeActions` (offline impl first).
2. Window frame + node grid + links + detail pane with `UiWinKit`; refresh on `PlayerState` change event (add `SkillsChanged` event in PlayerRpg if missing - World/ file, coordinate with phase 04 owner: phase 04 adds it).
3. Assign strip; magazine table from loadout; Live() per skill.
4. Online impl via `SendInvAction`; refresh on `ClientRpgMirror` update.
5. Screenshots via DebugCapture `--shots` at 960x640 and 1280x720 (`--open-skill-tree` flag in a new `DebugCapture.Skills.cs` partial, shared with phase 07).

## Todo
- [x] icons (`Tools/build-skill-tree-icons.py --from-preview`, 40 icons from the preview crops) + actions seam (`ISkillTreeActions`)
- [x] window, nodes, links, detail (+ respec: 50 G x level, two-tap confirm)
- [x] assign strip + magazine 3 pages (slot 1 page 1 = Dash, loadout slots 1-8; swap on assign)
- [x] entry points (bag Skill tab, T, touch "Skills" button + SP badge), "+1 skill point [T]" on the level-up banner
- [ ] online path: `OnlineActions` refuses with a message until phase 04 adds the InvOp ops (seam in `SkillTreeActions.cs`); magazine cast hook `SkillMagazine.CastRequested`
- [ ] screenshots reviewed by user (`visuals/skill-ui-*.png`, 960x444 Windows player; 960x640 / 1280x720 not shot, window is 920x540 on the 640-high canvas)

## Success criteria
- Learn/rank-up/assign work offline and online, persist after reload; disabled buttons always state the reason.
- Magazine shows learned skills with correct MP/cd sweep; swipe through 3 pages; cooldowns keep running across pages.
- Every file < 200 lines.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Prereq lines messy across rows | M x L | straight segments node-centre to node-centre, under nodes; tested with all 5 classes |
| Small icons unreadable on phones | M x M | 72 px nodes, detail pane big icon 128 px |
| Online latency on Learn button | L x L | button shows spinner until RpgState reply (ReqId) |

## Security
UI never mutates ranks directly online; server reply is the truth.

## Next steps
Phase 07 validates the window with the wizard art.

## Unresolved questions
1. Should the tree also show the passive (Mechanic Overcharge, companions) as a non-learnable info node? Default: info line in header only.

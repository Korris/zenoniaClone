---
title: "Unity RPG systems port (kill > EXP > loot > equip > enhance > save)"
status: phase 1-6 code done (compile-checked, not yet run in Unity), phase 7 open
created: 2026-10-29
---

Source of truth: web game `C:\Project\zenoniaClone` (branch `web`, read-only). Unity project `C:\Project\zenoniaUnity`, branch `unity`.

| Phase | Scope | Status |
|---|---|---|
| 1 Item icons | `Tools/build-item-icons.py` -> `Resources/Sprites/icons/{helmet,armor,gloves,boots}-N, weapon-<class>-N, offhand-<class>-N` (96px, 140 PNGs) | DONE |
| 2 Pure logic (`Assets/Scripts/Rpg`, asmdef `ZenoniaProto.Rpg`) | Leveling, class stats, Item/ItemDefs/ItemFactory (generate, rarity, tiers, drops, starter, sanitize), Enhance (+0..15, swappable `IUpgradeMethod`, stone method), PlayerState (stats aggregation, equip/class lock, potions, pickup), LootTable (Frost Hollow monsters), SaveSerializer + SaveStore (JSON, version, tolerant) | DONE, verified by `Assets/Editor/RpgLogicCheck.cs` (16 parity checks, run by FrostSmokeTest) |
| 3 Game wiring | `RpgSession` (load/new game per class, autosave 30 s, save on death/quit), hero stats from PlayerState (HeroController hp/mp/atk/def/speed/crit), monsters grant EXP/gold + spawn loot, death penalty + respawn at start, key remap (Bloom B -> F6, B = inventory, 1/2 potions), `GameClock.Paused` while panel open | CODE DONE 2026-10-29 (cloud session: compiled with mcs against Unity 2021 reference DLLs, never run in Unity) |
| 4 Loot on ground | `LootDrop` MonoBehaviour (icons from `Resources/Sprites/icons`, procedural potion / stone / coin, rarity glow, auto pickup) | CODE DONE (same caveat) |
| 5 UI (UGUI runtime canvas) | HUD (HP/MP/EXP bars, level, gold, potions) + level-up banner; Inventory/Equipment panel on B (20 slots, 6 equipment slots, tooltip, equip/unequip, Enhance button with % and before/after); replace HP/MP block in IMGUI `Hud.cs` | CODE DONE (same caveat): `RpgHud`, `InventoryPanel`, `UiKit` |
| 6 Gear sync | `HeroRenderer.SetEquipmentGear(weapon, offhand)` per-part tier + enhanced (>= +7) + glow band; companion uses offhand tier; Q/E/R stay debug overrides | CODE DONE (same caveat) |
| 7 Tests + build | EditMode test assembly (add `com.unity.test-framework` 1.6.0 to manifest; it is already in PackageCache as a transitive dep), Windows player build, screenshots to `plans/reports/unity-rpg-shots/` | NOT STARTED (logic checks live in FrostSmokeTest meanwhile) |

## Design decisions for the next session
- Pickup: auto on touch for everything (web: press attack on top of the drop). Stones/potions/coins always collected, equipment only when the bag has room.
- Frost Hollow itemLevel 1-3 => equipmentTier 0 for all drops, so tier art never changes from drops; enhance level (+7 enhanced art, glow bands) is what changes the look.
- Skill-tree passives do not exist in Unity: multipliers are 1.
- Save one file per class (`zenonia-save-<class>.json`); `SaveStore.Enabled = false` for DebugCapture / smoke runs (add flags `--level N --give-stones N --drop-loot --open-inventory` for screenshots).
- Death: respawn at map start, -10% EXP (`PlayerState.Revive`).
- Input is legacy Input Manager (`activeInputHandler: 0`), so `StandaloneInputModule` works for UGUI clicks.

## 2026-10-29 cloud session: phases 3-6 written
No Unity editor in that session, so nothing was run. What was checked:
- All game scripts + `ZenoniaProto.Rpg` + `FrostSmokeTest` / `RpgLogicCheck` compile with mcs against the UnityEngine 2021.3 module DLLs (NuGet `UnityEngine.Modules`) and UGUI 2020.3.
- `RpgLogicCheck` run under mono: the 14 old pure checks + 3 new `ItemInfo` checks pass (the save round-trip needs Unity's JsonUtility, so it only runs in the editor).
- `FrostSmokeTest.CheckRpg` (new, per class): stats from PlayerState, kill => EXP + loot drops, coin / potion pickup, tier 9 +9 weapon drives the held art, unequipped offhand hides the companion, inventory open / close toggles `GameClock.Paused`.

First thing next time in Unity: run the smoke test, then play: kill monsters, pick up loot, open B, equip / enhance, switch class and back (save), die (EXP penalty).
Screenshot flags: `--class warrior --level 8 --give-stones 20 --drop-loot --open-inventory --shots 3 --shot-path P`.

Open / follow-ups:
- Visual layout of the UGUI HUD + inventory has not been seen yet; expect spacing tweaks.
- Boss kill is stored per class save; a class without the flag does not bring a killed boss back in the same run.
- Phase 7 (EditMode test assembly, player build, screenshots) still open.

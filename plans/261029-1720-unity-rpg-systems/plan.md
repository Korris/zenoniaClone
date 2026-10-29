---
title: "Unity RPG systems port (kill > EXP > loot > equip > enhance > save)"
status: partial (phase 1-2 done, 3-6 not started)
created: 2026-10-29
---

Source of truth: web game `C:\Project\zenoniaClone` (branch `web`, read-only). Unity project `C:\Project\zenoniaUnity`, branch `unity`.

| Phase | Scope | Status |
|---|---|---|
| 1 Item icons | `Tools/build-item-icons.py` -> `Resources/Sprites/icons/{helmet,armor,gloves,boots}-N, weapon-<class>-N, offhand-<class>-N` (96px, 140 PNGs) | DONE |
| 2 Pure logic (`Assets/Scripts/Rpg`, asmdef `ZenoniaProto.Rpg`) | Leveling, class stats, Item/ItemDefs/ItemFactory (generate, rarity, tiers, drops, starter, sanitize), Enhance (+0..15, swappable `IUpgradeMethod`, stone method), PlayerState (stats aggregation, equip/class lock, potions, pickup), LootTable (Frost Hollow monsters), SaveSerializer + SaveStore (JSON, version, tolerant) | DONE, verified by `Assets/Editor/RpgLogicCheck.cs` (16 parity checks, run by FrostSmokeTest) |
| 3 Game wiring | `RpgSession` (load/new game per class, autosave 30 s, save on death/quit), hero stats from PlayerState (HeroController hp/mp/atk/def/speed/crit), monsters grant EXP/gold + spawn loot, death penalty + respawn at start, key remap (Bloom B -> F6, B = inventory, 1/2 potions), `GameClock.Paused` while panel open | NOT STARTED |
| 4 Loot on ground | `LootDrop` MonoBehaviour (icons from `Resources/Sprites/icons`, procedural potion / stone / coin, rarity glow, auto pickup) | NOT STARTED |
| 5 UI (UGUI runtime canvas) | HUD (HP/MP/EXP bars, level, gold, potions) + level-up banner; Inventory/Equipment panel on B (20 slots, 6 equipment slots, tooltip, equip/unequip, Enhance button with % and before/after); replace HP/MP block in IMGUI `Hud.cs` | NOT STARTED |
| 6 Gear sync | `HeroRenderer.SetEquipmentGear(weapon, offhand)` per-part tier + enhanced (>= +7) + glow band; companion uses offhand tier; Q/E/R stay debug overrides | NOT STARTED |
| 7 Tests + build | EditMode test assembly (add `com.unity.test-framework` 1.6.0 to manifest; it is already in PackageCache as a transitive dep), Windows player build, screenshots to `plans/reports/unity-rpg-shots/` | NOT STARTED (logic checks live in FrostSmokeTest meanwhile) |

## Design decisions for the next session
- Pickup: auto on touch for everything (web: press attack on top of the drop). Stones/potions/coins always collected, equipment only when the bag has room.
- Frost Hollow itemLevel 1-3 => equipmentTier 0 for all drops, so tier art never changes from drops; enhance level (+7 enhanced art, glow bands) is what changes the look.
- Skill-tree passives do not exist in Unity: multipliers are 1.
- Save one file per class (`zenonia-save-<class>.json`); `SaveStore.Enabled = false` for DebugCapture / smoke runs (add flags `--level N --give-stones N --drop-loot --open-inventory` for screenshots).
- Death: respawn at map start, -10% EXP (`PlayerState.Revive`).
- Input is legacy Input Manager (`activeInputHandler: 0`), so `StandaloneInputModule` works for UGUI clicks.

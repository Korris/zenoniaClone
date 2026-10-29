# Unity RPG systems port: PARTIAL (core logic done, wiring/UI not started)

Status: DONE_WITH_CONCERNS. Stopped early at the user's request; project compiles, smoke PASS. Plan + remaining work: `plans/261029-1720-unity-rpg-systems/plan.md`.

## Done (commit on `unity`)
- `Tools/build-item-icons.py` -> 140 icons in `Assets/Resources/Sprites/icons/` (helmet/armor/gloves/boots tiers 0-9 sliced from the web painted atlases; weapon-<class>-N / offhand-<class>-N from the cut held-gear overlays, tilted like the web; wizard spellbook drawn). Checked visually: clean.
- `Assets/Scripts/Rpg/` (own asmdef `ZenoniaProto.Rpg`, no MonoBehaviour, JS-compatible rounding): RpgClasses, Leveling (EXP curve, stats, death penalty), Item/ItemDefs/ItemFactory (rarity, tiers, generate with web rand() order, random-class drops, starter gear, sanitize), Enhance (+0..15 table, scaled floor, offhand half, `IUpgradeMethod` + `StoneUpgradeMethod`, `EnhanceSystem.Attempt`), PlayerState (stat aggregation, equip/unequip, class lock, potions, pickups), LootTable (Frost Hollow monsters + boss), SaveSerializer + SaveStore (per-class JSON, version, tolerant, atomic write, .bad copy).
- `Assets/Editor/RpgLogicCheck.cs` (called by FrostSmokeTest): 16 checks with expected numbers computed by node from the web modules (EXP 50/559/7808, warrior lv2 / assassin lv10 stats, enhance 4@1=1, 4@15=8, 12@15=17, 10@7=7, 2@3=1, offhand 3@15=6, generated items identical to web, enhance spends stone + raises ATK, class lock, boss drop, save round-trip, old/garbage/future saves).

## Verification
Copy `...\scratchpad\unity-rpg` (batchmode): zero error CS, `SMOKE RESULT: PASS` (all existing checks + 16 rpg checks). No player build or screenshots (nothing visible changed yet).

## Not done (see plan.md)
Phase 3 game wiring (RpgSession, hero stats from PlayerState, monster EXP/gold/loot, death penalty, key remap: bloom B -> F6, B inventory, 1/2 potions), 4 ground loot, 5 UGUI HUD + inventory/enhance panel + level-up banner, 6 per-part gear sync in HeroRenderer, 7 EditMode test assembly (test-framework 1.6.0 is in PackageCache, add to manifest) + player build + screenshots. None of this is partially wired: the game behaves exactly as before.

## Concerns / decisions
- B is still Bloom until phase 3 remaps it.
- Frost Hollow itemLevel 1-3 => tier 0 for all drops; enhance level is what changes the held art.
- Item icons in the real project have generated .meta files copied from the verified copy.

## Unresolved
- Pickup rule: auto on touch (planned) vs web press-attack.
- Whether to keep random-class equipment drops (web behaviour, wrong-class gear is bag clutter in single-class saves).

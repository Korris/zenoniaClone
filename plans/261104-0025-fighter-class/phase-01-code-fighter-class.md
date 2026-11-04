# Phase 1: Fighter class in code

Context: [plan.md](plan.md). Priority: high. Status: **done in code (2026-11-04 01:10, agent)**, not built in Unity yet. Report: [agent-261104-0030-fighter-class-code.md](../reports/agent-261104-0030-fighter-class-code.md).

## Todo
- [x] `HeroClass.Fighter` (last), `HeroClassDef.Fighter`, `All`
- [x] `RpgClasses` id + stats, offhand type `glove`; `ItemDefs` 10 + 10 tier names, `Off-hand Glove` template; `SecondWeapon*` fighter case (crit rate, max HP)
- [x] `ComboConfigs.FighterHits` (jab, cross, hook, uppercut), `ClassSfx` entry (warrior sounds + assassin shout)
- [x] Hero select: boxing slot = Fighter, `DebugCapture.HeroSelect` test adjusted, `--class fighter` doc; key 6 / F11 (`GameKey.Class6`, `Switch6`), HUD line
- [x] `SkillTable.Fighter.cs`: 8 generic-effect skills, Dash Punch on L; registry + tests (48 skills)
- [x] Net: nothing hardcodes 5 (byte / string ids, `NetLootCodec` 3-bit class index fits 6)
- [x] Missing-art guards reviewed: `SpriteSheet` / `GearOverlays` / `HeldGlowBaker` / `LootSprites` / `SkillIcons` all null-safe
- [x] `dotnet test` both suites pass (on net9.0 copies: only .NET SDK 9.0.203 is installed here)
- [ ] Unity build (main session) and `--class fighter` start
- [ ] `server-api` `CLASS_IDS` + node tests (outside this phase's file scope)

## Requirements
- `HeroClass.Fighter` appended after Cleric; `HeroClassDef.Fighter` + `All`; `RpgClasses.Ids` + stat def (brawler: HP between Warrior and Assassin, Atk high, Def mid, Speed 88, CritBonus 0.04, WeaponName "Gloves", offhand = second glove like the Assassin).
- `ItemDefs`: 10 weapon names and 10 offhand names for `fighter` (tier order: leather, iron, bronze, tiger, flame, frost, thunder, shadow, holy, dragon).
- Hero select: boxing slot becomes `(int)HeroClass.Fighter`, playable; `DebugCapture.HeroSelect` test adjusted; `--class fighter` works in `DebugCapture`.
- Combo: `ComboConfigs.FighterHits` (jab, cross, hook, uppercut), anims `melee1..melee4`, melee FX reuse (punch impact = existing hit spark, class tint orange), `ClassSfx` entry reusing warrior/assassin sounds.
- Skills: minimal valid set in the shared skill table (ZenoniaSkills), signature skill on L reusing an existing effect/choreo; `SkillExecutor.Preload` safe; skill icons point at existing icons.
- Net: everything keyed by class id string or enum int works for the 6th class (login/character create, server inventory `Generate(... classId)`, loot tables, `ZonePersistence`).
- Sprites/data paths the renderer loads: `classes/fighter/body/{armed,bare}`, no `HandFolder`, `GearFile = "classes/fighter/held-fighter"`, parts `off` + `main` folder `classes/fighter/weapons/glove`, DrawScale 1.0.
- Until phase 2/3 land: the game must not crash when the sheets are missing (check `HeroRenderer` null paths) — or create placeholder sheets by copying the Assassin's and note it.

## Files
Modify: `HeroClassDef.cs`, `Rpg/RpgClasses.cs`, `Rpg/ItemDefs.cs`, `ComboConfigs.cs`, `ClassSfx.cs`, `HeroSelectScreen.Build.cs`, `HeroSelectScreen.cs`, `DebugCapture*.cs`, skill table files, net/server files that switch on class. Create: `Assets/Resources/Data/classes/fighter/held-fighter.json` (weapon list of 10 names with placeholder grips `{"w":200,"h":200,"gripX":100,"gripY":100}`, glow colours, `"anchors": []`).

## Success criteria
- `dotnet test Tests/dotnet/ZenoniaSkills.Tests` and `ZenoniaNet.Tests` pass.
- `grep -rn "Cleric" Assets/Scripts` reviewed: every switch/array over classes has a Fighter entry.
- The main session builds the exe and starts `--class fighter`.

## Risks
- Shared skill table: changing class count may change server packets; keep ids strings. Enum appended last keeps saved ints valid.

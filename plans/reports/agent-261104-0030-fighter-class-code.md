# Fighter class: code phase (agent, 2026-11-04 00:30 - 01:10)

Plan: `plans/261104-0025-fighter-class/phase-01-code-fighter-class.md`. Scope: `Assets/Scripts/**`, `Tests/dotnet/**`. Art (body sheets, glove tiers, icons, anchors) belongs to the two art agents and is NOT here.

## Files changed
- `Assets/Scripts/HeroClassDef.cs`: `HeroClass.Fighter` appended last; `HeroClassDef.Fighter` (sheets `classes/fighter/body/{armed,bare}`, no `HandFolder`, `GearFile classes/fighter/held-fighter`, parts `off` + `main` in `classes/fighter/weapons/glove` DrawScale 1.0, `ComboAnims melee1..4`, `BareAnims idle/walk/dash/hurt`, palette Orange, HP 95 / MP 50 / Atk 13 / Def 4 / Speed 88 / crit +0.04, `SkillName "Dash Punch"`); added to `All`.
- `Assets/Scripts/ComboConfigs.cs`: `FighterHits` jab / cross / hook / uppercut (reach 20-26 vs daggers 22-30, 200-210 ms per punch, finisher 2.2x, knockback 95, StepPx 7-12, FxScale 0.75-1.1).
- `Assets/Scripts/Rpg/RpgClasses.cs`: `"fighter"` appended to `Ids`; stat def (offhand type `glove`, "Off-hand Glove", weapon "Gloves").
- `Assets/Scripts/Rpg/ItemDefs.cs`: `WeaponTierNames["fighter"]` Leather Gloves ... Dragon Gauntlets, `OffhandTierNames["fighter"]` Leather Glove ... Dragon Gauntlet (tier order leather, iron, bronze, tiger, flame, frost, thunder, shadow, holy, dragon); offhand template `O("glove", ...)` appended after spellbook.
- `Assets/Scripts/Rpg/SecondWeapon.cs`, `SecondWeaponText.cs`, `SecondWeaponStats.cs`: fighter second glove = CritRate 0.06 f + HpPct 0.15 f (both already applied in `PlayerState.RecomputeStats`).
- `Assets/Scripts/ClassSfx.cs`: Fighter set reusing `warrior/slashN`, `warrior/hit-*`, `warrior/crit-*`, shout `assassin/shout4-a`.
- `Assets/Scripts/ClassPalette.cs`: doc line.
- `Assets/Scripts/Rpg/Skills/SkillTable.cs` + new `SkillTable.Fighter.cs` (+ `.meta`): 8 skills from generic effects: 0 Dash Punch (L: dash 3 tiles + 180% line), 1 Iron Fist (+20% ATK 10 s), 2 Ground Slam (220% circle r2.5, Launch), 3 Rapid Jabs (5 x 70% cone), 4 Shockwave (220% piercing projectile, knockback), 5 Tiger Rush (3 dashes x 130% line), 6 Iron Body (shield 60% + DEF 30% 8 s), 7 Dragon Fist (leap 4 + 600% circle r3, stun 1.5 s; Lv 50 ultimate, prereqs 5 and 6).
- `Assets/Scripts/HeroSelectScreen.Build.cs`: slot 4 `"boxing"` -> `(int)HeroClass.Fighter`. `HeroSelectScreen.cs`: comments only (`NotPlayable` kept).
- `Assets/Scripts/DebugCapture.HeroSelect.cs`: the "Boxing cannot be picked" step became "pick the Cleric again" (the loop over `All` now ends on the Fighter); start still has to spawn the Cleric. `DebugCapture.cs`: `--class` doc lists all six.
- `Assets/Scripts/InputAdapter.cs`: `GameKey.Class6` (6 / numpad 6), `GameKey.Switch6` (F11; F6 is bloom) appended to the enum and both key maps. `GameSession.cs`: `ClassKeys` / `SwitchKeys` arrays replace the enum arithmetic (which would have mapped index 5 to `Switch4`). `Hud.cs`: help line adds F11 Fighter.
- `Tests/dotnet/ZenoniaSkills.Tests/SkillTableTests.cs`: 40 -> `RpgClasses.Ids.Length * PerClass` (= 48), fighter in the cast-lock class list, Dash Punch / Dragon Fist rows. `SecondWeaponTests.cs`: fighter second-glove keys; new `FighterStarterGearIsAPairOfGlovesWithTierNames` (starter "Gloves" / "Leather Glove", template `glove`, `ClassForOffhand`, tier 9 name, icon name `weapon-fighter-9`).
- Docs: `docs/pending-tasks.md` Fighter section; phase file status / todo.

## Stubbed / depends on the art agents
- `Data/classes/fighter/held-fighter.json` untouched (10 names, placeholder grips, `anchors: []`): with no anchors `HeroRenderer` draws the armed sheet only and hides the glove parts.
- Missing sheets: `SpriteSheet.Load` logs `Missing texture` per anim at `Init` (idle, walk, attack1-3, dash, hurt, death, melee1-4; attack1-3 and death will always log for this class unless the body tool writes them) and the hero is invisible; no exception path found (`HeroRenderer.Show` null-guards rows, `GearOverlays` / `HeldGlowBaker` / `LootSprites` / `SkillIcons` / `TouchControls.SkillSprite` return null, `HeroSelectPreview.Tick` tolerates `FrameCount 0`).
- No fighter skill art (`SkillVfx` ring fallback in orange), no choreos (a `Choreo` needs painted art anyway), no skill icons (`skill-icons/<n>.png`), no own sounds. Punch FX = warrior crescent slash + ribbon tinted orange via `ClassPalette.Orange`.

## Tests
- Only .NET SDK 9.0.203 is installed; the csproj files target net10.0 (`NETSDK1045`). Ran both suites through net9.0 copies of the csproj in the scratchpad (absolute source paths, csproj in the repo unchanged): `ZenoniaSkills.Tests` 188 passed, `ZenoniaNet.Tests` 279 passed.
- Unity compile not run (no Unity here); Unity-only edits reviewed by diff.

## Verified for 6 classes (nothing hardcodes 5)
`NetLootCodec` class index is 3 bits (0-7), `BuffSource.Pack` `(c << 3) | index` in a byte, `HelloMsg.HeroClass` byte + `DevLoginStub.TryClass` bounds on `All.Length`, `ZonePersistence.DefOf` / `AccountScreenGui.HeroClassName` by id string, `ProxySpawner` / `HeadlessBench` / `ConnectScreenGui` / `HeroPortraitView` / `HeroSelectPreview` / `ClassSfx` sized by `All.Length`, `ItemFactory.RollEquipmentDrop` picks from `Ids` (fighter gear now drops), `DevStartConfig.allWeapons` is per class id.

## Unresolved questions
1. `server-api/src/rpg/rpg-constants.js` `CLASS_IDS` has no `fighter` (zod enums for character create and item `classId`): online creation / saves of a Fighter will be rejected until `'fighter'` is appended and `server-api/test` is adjusted. Outside this phase's file scope; one-line change plus tests.
2. Should attack1-3 / death sheets be written by the body tool (copies of melee1-3 / hurt) to silence the `Missing texture` log, or should `HeroRenderer.AnimNames` become per class?
3. The test projects target net10.0 but this machine has only SDK 9: install .NET 10 or lower the target? (Not changed.)
4. Fighter skills have no painted art or icons; `--record-party` still records five classes; README controls table not updated (main session / docs phase).

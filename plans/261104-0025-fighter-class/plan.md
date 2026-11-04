# Fighter (Boxing) class: 6th playable hero

Date: 2026-11-04 00:25. User request (going to sleep, wants it all done): "làm luôn nhân vật fighter, đủ hết sprite, vũ khí là bao tay nhiều kiểu khác nhau, mày tự làm toàn bộ".

## What exists
- Art: hero-select portrait `Assets/ArtLibrary/custom/hero-select/boxing.png` (1254 px, male, spiky dark-brown hair, red headband, bare chest with harness, black pants with red/gold armour, big flaming red/gold gauntlets), banner `banner-boxing.png`, painted idle-down frame `custom/idle-down-set/boxing.png` (192 px, gloves painted in), source painting `six-heroes-src.png` (x 1815-2135).
- Hero select: slot 4 "boxing" exists but is `NotPlayable` (`HeroSelectScreen.Build.cs:114`, `HeroSelectScreen.cs:163`).
- Pipelines to copy: Assassin (`Tools/build-assassin-new-style.py`: body rows from ChatGPT strips, FISTS table, two held parts `off`/`main`, armed sheets), dagger tiers (`dagger-install`), idle-down doll tools.

## Decisions (fixed here so three agents can work in parallel)
- Class id `fighter`, enum `HeroClass.Fighter` (appended LAST, after Cleric: never renumber the others), display name "Fighter", hero-select slot = the boxing slot.
- Weapon = a pair of gloves, ONE item (like the Assassin's daggers: weapon slot draws both parts; offhand = second glove item of the same tier, as `RpgClasses` does for the Assassin). Parts: `off` (left glove, drawn behind) and `main` (right glove, in front), folder `classes/fighter/weapons/glove`, manifest in `Assets/Resources/Data/classes/fighter/held-fighter.json` (same schema as held-assassin.json: `weapon` list with `name`, `glow`, `baseGrip`, `enhancedGrip`; `anchors` list).
- Glove art convention: one glove, closed fist, seen from the side with the knuckles pointing RIGHT, on a square canvas; `base`/`enhanced` PNG 200x200, grip = centre of the fist (gripX, gripY written by the tool). The game mirrors it with `sx` for the other hand. DrawScale 1.0, anchor `s` ≈ 0.18 so the glove is about 1.6x a bare fist (36 frame px).
- Tier names (10): `leather`, `iron`, `bronze`, `tiger`, `flame`, `frost`, `thunder`, `shadow`, `holy`, `dragon`. Tier 1 `leather` is a plain brown leather boxing glove; tier 5 `flame` matches the hero-select gauntlets.
- Body: no `hand` overlay sheet (the glove covers the fist). Anims like the Assassin: `idle`, `walk`, `hurt`, `melee1`..`melee4` (jab, cross, hook, uppercut finisher), `dash`, `death` (death may reuse hurt). 4 directions each.
- Skills: signature skill on L = a dash punch reusing existing FX (no new VFX art tonight). Skill tree: the smallest set that keeps the UI and the shared `SkillTable` working.

## Phases
| # | Phase | Owner | Status |
|---|---|---|---|
| 1 | [Code: class, items, hero select, net](phase-01-code-fighter-class.md) | agent code | code done 01:10, tests pass, Unity build pending |
| 2 | [Body sprites + anchors tool](phase-02-body-sprites.md) | agent art body | in progress |
| 3 | [Glove tiers + icons](phase-03-glove-tiers.md) | agent art gloves | in progress |
| 4 | Build exe, captures, docs | main session | done 01:45: build OK, in-game captures `C:	emp\cap-fighter\` (idle, attacks, skill hits, inventory) look right, glove `s` 0.24 |

## File ownership
- Phase 1: `Assets/Scripts/**` only (+ `Assets/Resources/Data/classes/fighter/held-fighter.json` manifest `weapon` list stub if missing).
- Phase 2: `Assets/ArtLibrary/classes/fighter/new-style/**`, `Assets/Resources/Sprites/classes/fighter/body/**`, `Data/classes/fighter/held-fighter.json` `anchors`, `Tools/build-fighter-new-style.py`.
- Phase 3: `Assets/ArtLibrary/classes/fighter/new-style/glove-src/**`, `Assets/Resources/Sprites/classes/fighter/weapons/glove/**`, `Assets/Resources/Sprites/icons/weapon-fighter-*.png`, `offhand-fighter-*.png`, `Data/classes/fighter/held-fighter.json` `weapon` grips, `Tools/build-fighter-gloves.py`.
- Shared JSON `held-fighter.json`: phase 1 creates it with the 10-name `weapon` list and empty `anchors`; phases 2 and 3 edit only their own keys, re-reading the file right before writing.

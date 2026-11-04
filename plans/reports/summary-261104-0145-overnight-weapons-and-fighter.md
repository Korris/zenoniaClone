# Overnight summary 2026-11-03 21:55 -> 2026-11-04 01:45

All uncommitted on branch `unity`. Test exe: `C:	emp\player-dagger\Zenonia.exe` (built from `C:	emp\zu-copy`). Captures: `C:	emp\cap-dagger`, `cap-inv`, `cap-final`, `cap-fighter`.

## Done
- Assassin: 10 dagger tiers v2 (ChatGPT chat "Tạo warrior 2D"), art 400 px (126 frame px), fixed grip rows (`GRIP_Y`), idle-down paper doll (`Tools/build-assassin-idle-down-doll.py`).
- Equipment window: hero faces down and shows worn gear for all 6 classes; idle-down dolls for Wizard and Cleric (`Tools/build-idle-down-doll-from-redraw.py`).
- Mechanic: 10 pistol + 9 cannon tiers from the user's sheet (agent), pistol `s` 0.15 -> 0.26, `pistol-front/back` 0.195, `HeroFx` muzzle tables recomputed.
- Wizard: 9 staff tiers from the user's sheet (agent), staff art 263 -> 390 px (174 frame px), `attack3` down/up/left redrawn in the hooded design (agent).
- Fighter (6th class): code (enum last, items, skills 8, hero select slot, keys 6/F11, tests 188+279 pass on net9 copies), 24 body strips + anchors (`Tools/build-fighter-new-style.py`), 10 glove tiers + icons (`Tools/build-fighter-gloves.py`), glove `s` 0.24, `server-api` `CLASS_IDS` + test (vitest not installed, not run). Plan: `plans/261104-0025-fighter-class/`.

## Open
- Assassin enhance glow fainter than the Warrior's; Shadow Clone clips 100 frames; `melee3/4`, `nightfall` old style.
- Wizard: staff head on the face in `walk`/`idle` right frame 1; `crystal` lost floating shards; `attack3` down frames 3-5 staff crosses the legs; up rows show white hair under the hood.
- Mechanic: pistol front view shared by all tiers; cannon has no enhance effect; bullet origin not checked in play after the pistol resize.
- Cleric doll lost the small wings; Wizard/Cleric `armed` row 0 still painted (used only without gear data).
- Fighter: `death` = hurt copy, no own skill VFX/icons/sounds (generic effects, orange), back rows hair has red highlights, front punches frame 2 bare fist peeks under the glove, `bronze` cestus skin colour.
- Test projects target net10 but only .NET SDK 9 is installed; `server-api` vitest missing; README key table and `--record-party` still list 5 classes.
- Nothing committed; Unity will create `.meta` for new ArtLibrary folders.

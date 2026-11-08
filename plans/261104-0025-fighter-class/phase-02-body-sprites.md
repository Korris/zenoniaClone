# Phase 2: Fighter body sprites and glove anchors

Context: [plan.md](plan.md), `docs/sprite-rules.md` (read all), `Tools/build-assassin-new-style.py` (the model), `Tools/build-assassin-idle-down-doll.py`. Priority: high. Status: **done 2026-11-04 01:30** (agent art body; report `plans/reports/agent-261104-0030-fighter-body-sprites.md`). Not yet seen in the game.

## Requirements
- Rows (192 px cells, hero 164 px tall standing, soles on row 188-191 like the other classes): `idle`, `walk` (4 frames), `hurt` (2), `melee1`..`melee4` (6 each: jab, cross, hook, uppercut), `dash` (3), each in down / up / left / right; `death` may copy the Assassin's layout with the fighter art or be the hurt pose.
- Hands EMPTY (bare fists, no gloves): the game draws the worn gloves through anchors. Body sheets `body/bare/<anim>.png` and `body/armed/<anim>.png` (armed = bare + tier-1 glove baked, like the Assassin's `armed` command, used only without gear data).
- Anchors in `Data/classes/fighter/held-fighter.json` `anchors`: part `main` (right glove, front) and `off` (left glove, behind when the hand is on the far side), per frame `x`,`y` = fist centre, `angle` = punch direction (0 = knuckles up on the art? define: glove art has knuckles pointing right, so angle = clockwise degrees from pointing-right to the fist's direction), `s` = 0.18, `sx` = -1 for the mirrored hand, `behind`, `hidden`, `fallback`=false. Read fists per frame from a grid like the Assassin's FISTS table.
- Tool `Tools/build-fighter-new-style.py` (copy and trim the Assassin's: key green, cut rows, FISTS, anchors, armed bake, preview).
- Character: see `plans/261104-0025-fighter-class/plan.md` "What exists"; male, copy face/hair/outfit of `custom/hero-select/boxing.png`; idle down may start from `custom/idle-down-set/boxing.png` (gloves must be erased or redrawn empty-handed).

## Done (2026-11-04)
- 24 strips drawn by ChatGPT (account Tay "Tân Korris", one chat https://chatgpt.com/c/6aea1911-3668-83ec-ab96-0fafd90ba8a6) into `Assets/ArtLibrary/classes/fighter/new-style/src/<anim>-<dir>.png`: idle, walk, hurt, melee1-4, dash x down/up/right. Left = right mirrored by the tool.
- `Tools/build-fighter-new-style.py` (+ `Tools/fighter-strip-helpers.py`): `cut`, `fists`, `preview`, `write`, `armed`. FISTS table read by eye off the `cut` grids (zoomed crops), ~3 px.
- Written: `Sprites/classes/fighter/body/{bare,armed}/{idle,walk,hurt,death,melee1-4,dash}.png` + metas (copied from the Assassin's, new guids), `death` = `hurt` copy; `held-fighter.json` `anchors` 64 entries (8 anims x 4 dirs x 2 parts), `s` 0.18, `sx` -1 on the far-side fist of front/back rows and on mirrored rows.
- Preview composites of every row looked at with the installed `leather-base.png` glove.

## Todo
- [x] Strips right (8) / down (8) / up (8)
- [x] FISTS, angles, behind flags, preview of every row
- [x] `write` sheets, metas, anchors
- [ ] Look in the game (the user, or main session captures): glove size `s` 0.18, punch direction, `armed/death` pose
- [ ] Back rows: hair painted with bright red highlights (same colour as the headband); `match_hair` only fixes the dark reds, so the up rows stay redder than the side rows. Redraw the 8 up strips in a new chat asking "dark brown hair, no red highlights" when quota allows.
- [ ] Front punches (melee1/2 down frame 2): the foreshortened bare fist is bigger than the glove and peeks out under it (one glove size, rule 3).

## Success criteria
- `preview` composites of every row with a glove at the fists look right (glove on the fist, punch direction, far hand behind the body).
- No frame draws the glove on the face.

## Risks
- ChatGPT quota: Tay first, Tan Pham if Tay runs out (allowed). Row order/direction mistakes: check each strip, mirror if needed.

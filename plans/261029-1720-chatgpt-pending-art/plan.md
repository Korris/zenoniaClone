---
title: "ChatGPT art still to generate (after quota reset)"
status: pending
created: 2026-10-29
---

# ChatGPT art todo, after the quota reset

The ChatGPT Plus image quota ran out on 2026-10-29. It resets **2026-10-30 at 11:42**.

All prompt templates are in `C:\Project\zenoniaUnity\Assets\ArtLibrary\anim\generation-prompts.txt`:
- "WEAPONLESS edit template - warrior/assassin"
- "SOLID-BLADE FIX"
- the PENDING section

## Rules (must follow)
- In ARMED sheets the weapon is drawn SOLID and fully visible in every frame. The smear may only trail behind it. Use the same weapon design and size in every frame, since the armed sheets exist only to measure the weapon anchor.
- WEAPONLESS sheets remove the weapon AND the smear completely. Hands stay as fists. Warrior keeps the shield.
- Always make a WEAPONLESS sheet from the FINAL armed sheet.
- Each sheet is 6 frames x 3 rows (down, up, right side), on a real transparent background. Leave a 30px+ empty band between figures; asking for figures about 20% smaller helps.
- Use a new chat per sheet, or at least per class. Big chats freeze.
- Check every sheet: RGBA, transparent share above 0.3, and a clean 6x3 split by the gutters.

## Order (12 images)
| # | Task | Input to attach | Output |
|---|---|---|---|
| 1 | SOLID-BLADE FIX: assassin attack2 | `anim/_pending/assassin-attack2-armed.png` | `anim/assassin/attack2-armed.png` |
| 2 | SOLID-BLADE FIX: assassin attack3 | `anim/_pending/assassin-attack3-armed.png` | `anim/assassin/attack3-armed.png` |
| 3 | WEAPONLESS: warrior attack1 | `anim/warrior/attack1-armed.png` | `anim/warrior/attack1-weaponless.png` |
| 4 | WEAPONLESS: warrior attack3 (add "remove the spin ring") | `anim/warrior/attack3-armed.png` | `anim/warrior/attack3-weaponless.png` |
| 5 | WEAPONLESS: warrior attack4 | `anim/warrior/attack4-armed.png` | `anim/warrior/attack4-weaponless.png` |
| 6 | WEAPONLESS: assassin attack1 | `anim/assassin/attack1-armed.png` | `anim/assassin/attack1-weaponless.png` |
| 7 | WEAPONLESS: assassin attack2 (after #1) | fixed attack2-armed | `anim/assassin/attack2-weaponless.png` |
| 8 | WEAPONLESS: assassin attack3 (after #2) | fixed attack3-armed | `anim/assassin/attack3-weaponless.png` |
| 9 | WEAPONLESS: assassin attack4 | `anim/assassin/attack4-armed.png` | `anim/assassin/attack4-weaponless.png` |

All paths are under `C:\Project\zenoniaUnity\Assets\ArtLibrary\`.
Optional, if quota is left: a spacing retry on warrior attack1 (in row 2 the sword tip touches the next frame).

## After the art is done (no ChatGPT needed)
- Integrate the new warrior and assassin attack animations in Unity: weaponless body, then the tier weapon overlay at the per-frame anchors measured from the armed sheets, then a smear/slash VFX layer from `vfx/warrior/warrior-slash-vfx.png` and `vfx/assassin/assassin-slash-vfx.png`. Trigger the VFX on the exact swing frame, oriented along the swing direction.
- Make the assassin daggers larger, about 40% of body height.
- Record combo videos for warrior and assassin.

## 2026-10-30: integration pipeline built (warrior attack2 = first sheet in game)
- `Tools/build-melee-sheets.py <class> <N>`: slices `anim/<class>/attack<N>-armed.png` + `-weaponless.png` (6x3, gutter-aware cuts),
  scales to the bare idle body height, writes `Resources/Sprites/<armed>/melee<N>.png` + `<bare>/melee<N>.png` (192px, rows down/up/left(mirrored right)/right),
  measures the weapon per frame (RANSAC line on armed-minus-weaponless pixels, grip = gold crossguard end, behind = body covers blade)
  into `Resources/Data/<gearFile>-melee.json` (shield hidden: it is baked into the warrior weaponless body), preview in `plans/reports/melee-shots/`.
- Unity: `HeroClassDef.ComboAnims` (warrior = attack1, melee2, attack1, attack3), HeroRenderer loads them, HeldGearData merges `-melee.json`.
- SlashEffect now uses the painted `vfx/warrior/warrior-slash-vfx.png` (copied to Resources/Sprites/fx): row 0 slash turned +90 deg, row 1 finisher ring.
- When a new weaponless sheet lands: run the tool, check the preview, put `meleeN` in `ComboAnims`. Assassin needs `grip` / dagger art values in the tool's CLASSES table (two daggers = two parts: `main` + `off`).

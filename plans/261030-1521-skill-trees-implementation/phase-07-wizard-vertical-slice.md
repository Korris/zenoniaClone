# Phase 07 - Wizard vertical slice (end to end) + APK milestone #1

## Context links
- Phases 03/04/05/06 outputs; `SkillTable.Wizard.cs`; preview `Assets/ArtLibrary/custom/skill-vfx-preview/wizard.png`
- `IceStormUltimate.cs` (legacy, kept), `DebugCapture.cs` (`--record-ultimate`, `--skill-at`, `--dummies`), `Assets/Editor/PlayerBuildScript.cs` (BuildAndroid)

## Overview
Priority P1 | after 04 + 05 + wizard art batch. Proves every framework path before scaling to 4 more classes.

## Why wizard (not warrior/assassin)
Wizard exercises the most effect kinds at once: Projectile (Arcane Bolt), Projectile -> chained AoE + Burn (Fireball), Legacy adapter (Ice Storm),
Pierce (Arcane Lance), Shield buff (Mana Shield), self AoE + Freeze (Frost Nova), conditional bonus vs status (Elemental Burst), seeded Rain (Astral Cataclysm),
plus the painted ranged VFX pipeline it already uses. Missing kinds (Motion, Summon, Zone, ally Heal) are covered next by Warrior/Assassin/Cleric.

## Requirements
- 8 wizard skills castable offline and online with final art, icons and SFX; tree window + magazine fully usable on a phone.
- Debug: `DebugCapture.Skills.cs` partial: `--skill wizard.fireball` (cast at t), `--record-skill <id>` (60 fps frames like `--record-ultimate`), `--learn-all` (Lv60, rank 1..10 via `--rank N`), `--open-skill-tree`.

## Related code files
Modify: `Rpg/Skills/SkillTable.Wizard.cs` (tuning), `Skills/SkillVfx.cs` (wizard-specific scale/pivot table entries if needed)
Create: `DebugCapture.Skills.cs`
Art/audio (from phase 06): `Resources/Sprites/skills/wizard/**`, `Resources/Data/skills/wizard-*.json`, `Resources/Audio/sfx/skills/wizard/**`

## Implementation steps
1. Import wizard batch; hook VFX rows per effect (`Vfx` keys) and SFX keys in the table.
2. Record each skill (`--record-skill`, ffmpeg to mp4); user review against preview panels; iterate scale/timing.
3. Online check: host + client, both see all 8; Ice Storm unchanged visually.
4. Tune ranks 1/5/10 numbers vs Frost Hollow monsters (time-to-kill sheet in the phase report).
5. APK #1 (`Zenonia > Build Android APK`), test on phone: 60 fps with Astral Cataclysm + 10 monsters; touch tree window usability.

## Todo
- [ ] art/sfx wired
- [ ] DebugCapture.Skills.cs
- [ ] 8 recordings reviewed
- [ ] online check
- [ ] tuning pass
- [ ] APK #1 on device

## Success criteria
- User approves the 8 recordings; FrostSmokeTest green; APK holds 60 fps (`--perf` log: no frame > 25 ms during casts).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Framework gaps found late | M x H | that is the point of the slice: fix in phase 03 files before phase 08 starts |
| Overdraw on mobile from big additive layers | M x M | cap additive glow copies, 256 px frames |

## Security
n/a beyond phase 04.

## Next steps
Phases 08-11 (one class each, same recipe).

## Unresolved questions
1. Ice Storm unlock: design says Lv 15 (today Lv 1). Existing Lv 1-14 wizards lose it until Lv 15 - accepted?

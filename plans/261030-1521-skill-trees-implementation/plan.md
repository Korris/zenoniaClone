---
title: "Skill trees: 5 classes x 8 active skills (data, executor, UI, VFX, SFX)"
description: "Data-driven SkillDef table + SP/ranks in the save, composable server-authoritative skill executor with monster status effects, gold skill-tree window bound to the magazine, ChatGPT VFX + ElevenLabs SFX per skill."
status: pending
priority: P1
effort: 115h
branch: unity
tags: [skills, skill-tree, combat, netcode, ui, vfx, sfx, unity]
created: 2026-10-30
---

# Skill trees implementation

Design: [skill-tree-chatgpt.md](../261030-1508-skill-tree-design/skill-tree-chatgpt.md) (approved). VFX look: `Assets/ArtLibrary/custom/skill-vfx-preview/<class>.png`
(4x2 panels, order = R1C1..R2C4). Kept as-is: Wizard **Ice Storm** (`IceStormUltimate.cs`), Warrior **Earth Break** (`EarthBreakUltimate.cs`) -> legacy adapters.
38 new skills (Shadow Step + Heal redone). Mechanic robot / cleric spirit stay passive companions.

## Phases

| # | Phase | Effort | Depends on | Lane | Status |
|---|---|---|---|---|---|
| 01 | [Skill data, SP/ranks, save + API, Lv cap 60](phase-01-skill-data-sp-ranks-save-api.md) | 9h | - | A (now) | completed |
| 02 | [Monster status effects + area damage](phase-02-monster-status-effects-area-damage.md) | 7h | 01 step 1 (enums) | B (now) | completed |
| 03 | [Skill executor runtime + effect behaviours](phase-03-skill-executor-runtime-effects.md) | 15h | 01, 02 | A | pending |
| 04 | [Hero + netcode integration (touch points)](phase-04-hero-netcode-integration.md) | 12h | 03 + **netcode agent landed** | A | pending |
| 05 | [Skill tree window + magazine binding](phase-05-skill-tree-ui-magazine-binding.md) | 12h | 01 (04 for live keys) | C | pending |
| 06 | [VFX / icon / SFX pipeline tooling](phase-06-vfx-icon-sfx-pipeline.md) | 8h | - | D (now) | pending |
| 07 | [Wizard vertical slice + APK #1](phase-07-wizard-vertical-slice.md) | 11h | 04, 05, 06 | - | pending |
| 08 | [Warrior](phase-08-warrior-skills.md) | 8h | 07 | - | pending |
| 09 | [Assassin](phase-09-assassin-skills.md) | 8h | 07 | - | pending |
| 10 | [Cleric](phase-10-cleric-skills.md) | 8h | 07 | - | pending |
| 11 | [Mechanic (+ Overcharge)](phase-11-mechanic-skills.md) | 9h | 07 | - | pending |
| 12 | [Balance, cleanup, full tests, APK #2, docs](phase-12-balance-cleanup-tests-apk.md) | 8h | 08-11 | - | pending |

Lanes A/B/C/D are file-disjoint (ownership in each phase). 08-11: one class at a time (they share the same data/registry files only via
per-class partial files, so two classes CAN run in parallel). Art batches (06 tooling, then one ChatGPT batch per class) run ahead of code.

## Netcode coordination (hard gate)
The online-netcode agent owns `HeroController*.cs`, `ComboSystem.cs`, `ComboProjectile*.cs`, `Net/**`. Phases 01/02/03/05/06 touch **none** of them.
Phase 04 is the ONLY phase that edits them and starts after that agent merges to `unity`. `ComboSystem.cs` and `ComboProjectile*.cs` are never edited.
Exact touch list + line budget: phase 04 "Touch points".

## Key decisions
- Skills = data (`SkillDef` + composable `SkillEffect[]`) in engine-free `Rpg/Skills/` (dotnet-testable, same code on server/client).
- One `SkillRun` timeline MonoBehaviour per cast; damage/heal/status only when `NetMode.IsAuthority`; art/audio only when `NetMode.RendersFx`.
- Client sends only "skill index pressed" (3 bits in the existing 16-bit input frame); server picks targets. No client positions trusted.
- SP is derived (`SP = level - sum(ranks)`), only ranks + loadout are saved -> no SP dupes possible.
- Units: design tiles x `GameConfig.TileUnits` (16).

## Global success criteria
- 40 skills castable offline, host, client; FrostSmokeTest `CheckSkills` green; dotnet tests green; APK 60 fps on target phone in a 10-monster fight.
- Learning/rank-up survives save/reload and API save round-trip; server rejects illegal learn/cast.

## Rollback
Branch per lane (`skills/p01-data` ...), merge when green. Phase 04 is one commit set reverting cleanly to the legacy L skill (legacy fields stay until phase 12).

## Unresolved questions
See bottom of each phase; top ones: rank-up level gating, auto-learn skill 1 at Lv1, respec, keyboard keys, Lv cap 60 rebalance, ElevenLabs API vs web UI.

## User decisions (2026-10-30)
- Ice Storm / Earth Break / all skills unlock by level as designed (no Lv 1 exceptions); new chars auto-learn skill 1 at Lv 1.
- Respec: costs gold (scales with level).
- Bosses: freeze/stun become a slow.
- Ally skills (heal/buff/shield) hit every player in range until a party system exists.
- Defaults taken by lead: no extra min level for rank-ups; keys U/I/O + L alias + T tree; EXP retune deferred to new maps; Death Mark charges own hits only; Overcharge +4 basic/+2 skill, passive robot hides during Robot Assault; ElevenLabs via web UI; passives shown as info nodes. Kept Ice Storm/Earth Break use tree MP/CD values.

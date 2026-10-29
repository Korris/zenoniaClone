# Unity: Assassin + Cleric (+ ultimates, palettes, reference-style ranged FX)

Project `C:\Project\zenoniaUnity`, commits `a4667a3` (ultimates), `2bca78f` (record flag), `9bd13e2` (this work). Pre-existing Packages/ProjectSettings changes and `Assets/ArtLibrary` left out.

**Status:** DONE_WITH_CONCERNS
**Summary:** 5 playable classes (keys 1-5, F1-F5). Assassin (daggers in both fists, X-slash FX, double stab, Shadow Step) and Cleric (staff + spirit companion, light arrows, gold-pillar impact, Heal) added; RobotCompanion generalised to HeroCompanion; Earth Break / Ice Storm ultimates; one palette table; first-cast hitch fixed. Compile clean, smoke PASS, player builds verified by screenshots.
**Concerns:** see bottom.

## Built
- Data: `Tools/export-held-gear.mjs` -> `held-assassin.json` (parts `main`+`off`), `held-cleric.json` (+companion/companionFloat; mechanic JSON renamed fields). Sprites copied to Resources/Sprites/{assassin,assassin-bare,assassin-dagger,cleric,cleric-bare,cleric-staff,cleric-spirit,cleric-spirit-float}.
- `HeroClassDef` (5 classes, palette, companion fields, MP + skill), `HeldGearData` (one weapon manifest for all parts), `HeroCompanion` (shared robot/spirit), `GameSession`/`ClassSelectGui` (5 cards), HUD (MP bar, skill cooldown, `[C] Mechanic FX`).
- Assassin: `DaggerFx` (twin X cross-slashes, afterimage lines, thrust streaks on the double stab, violet wisps), double stab 90 ms apart (`MeleeStrike`), +6% crit, Shadow Step (`HeroController.ClassSkills.cs`: blink behind nearest foe, forced crit, refused for free with no target).
- Cleric: `StreakBoltRig` light arrow, `HolySmite` gold pillar + cross star + green shards + halo ring impact, leaf motes, Heal 38% (refused at full HP), spirit follows/bobs, Q/E/R apply.
- Wizard: `ArcaneBoltRig` (halo, orb, rune star + ring, 2 orbiting motes, comet trail, beam tail, sparkles), `RuneFx` cast flash / impact / finisher circle + 8 rays.
- Mechanic: `MechanicFx` (muzzle blast, starburst, hot sparks, brown debris + dust), plasma bolt via `StreakBoltRig`; orange default, **C** toggles blue.
- Warrior: red crescent (`SpriteTint` recolour), red trail via shader colour props, red sparks/ring. Bloom threshold 1.0 -> 1.25 (knee 0.15) and normal-blend rings so FX keep colour on snow. `ClassPalette.cs` = single palette table.
- Ultimates (earlier): Earth Break, Ice Storm, MP/cooldown HUD, launch/chill, boss immune; videos recorded.

## Verification
Copy: `...\scratchpad\unity-classes5\proj` (+ scripts). Batchmode compile: zero error CS, no shader errors. `FrostSmokeTest` PASS for all 5 classes (world, 19 monsters, gear JSON, 512 frames each, skills incl. refusal cases, companions, tint check). Windows player build + `DebugCapture` screenshots per class.
Hitch re-measure (normal build, no capture, `--perf` logs frames >16 ms; before = first-cast without prewarm):
- before: warrior cast 21 ms at cast, 28 ms 2nd cast; wizard 42/28/28 ms around 2nd cast.
- after (`Preload()` of skill art + `UltimateFx.Prewarm()` at class spawn): no frame >21 ms, nothing >33 ms, none tied to a cast (remaining 21 ms blips random, 2 players ran at once). The 56-95 ms in stills was ScreenCapture overhead.

## Screenshots (copied to `unity-classes5-shots\`, ultimates in `unity-ultimates-shots\`)
`class-colours.png`, `ranged-attack-vs-ref.png` (reference on top, in-game rows below), `wizard-bolt.png`, `cleric-attack.png`, `mechanic-orange.png`, `mechanic-blue.png`, `gear-both.png` (assassin + cleric 4 facings), `skills-shadowstep-heal.png`, `class-select.png`, `cc-grid.png`, `perf-hitch-before-after.txt`; videos `warrior-earth-break.mp4`, `wizard-ice-storm.mp4`.

## User should
Open the project in Unity (imports the new PNGs, a few minutes), press Play, pick 1-5. Try L on each class, C on the mechanic, Q/E/R on dagger/staff/spirit.

## Concerns
- Ranged FX are an approximation of the reference, not a match (no swirling body ribbons, simpler shards); big white halos remain around hit monsters on snow (existing white hit-flash + bloom).
- Only snow exists in Frost Hollow, so the "grass" comparison was not possible.
- Wizard is violet-blue (reference) per latest note; earlier "blue" and cleric "smite" iterations were replaced. Assassin purple is close to wizard violet.
- Assassin left-facing / whole-frame fallback rule follows web; both daggers draw in all checked facings.
- Heal and Shadow Step use rank-3 values, no levelling/EXP. No `code-reviewer` run, no unit tests beyond edit-mode smoke.
- Mechanic painted `bullet`/`plasmaShot`/`muzzleFlash` art no longer used (still in Resources).

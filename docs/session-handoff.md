# Session handoff — 2026-10-29


The user said they will continue "tối nay" (tonight, 2026-10-29). On resume, read this file first, then C:\Project\zenoniaUnity\docs\session-handoff.md and the plans in C:\Project\zenoniaUnity\plans\.

**State at stop (Unity, C:\Project\zenoniaUnity, branch `unity`, pushed to github.com/Korris/zenoniaClone):**
- Classes: all 5 playable. Class select on keys 1-5; F1-F5 switch class.
- Map: Frost Hollow, with wandering monsters and the Frostfang King boss.
- Game feel: hit-stop, shake, sword trail, bloom (F6 toggles it), F toggles all effects.
- Ultimates on L: warrior Earth Break, wizard Ice Storm. Class skills on L: assassin Shadow Step, cleric Heal. Recording flags: `--record-ultimate`, `--record-combo`.
- Ranged basic attacks (cleric, wizard, mechanic) use painted VFX sprite sheets (Assets/ArtLibrary/vfx). Mechanic C toggles orange/blue.
- Colours: warrior red, wizard violet, mechanic orange (C for blue), assassin purple X-slashes, cleric green-gold.
- Monster hit flash is capped at 50%.
- Assets/ArtLibrary holds all the web art (806 files).
- RPG systems: phases 1-6 are written (2026-10-29 cloud session) but have NOT been run in Unity yet; they were only compile-checked.
  - The hero's HP/MP/ATK/DEF/speed/crit now come from PlayerState (level + equipment). `RpgSession` loads or creates a per-class save, autosaves every 30 s and also saves on respawn, level-up, boss kill, class switch and quit. Kills grant EXP and drop loot on the ground (`LootDrop`), picked up by touch. Death costs 10% of EXP.
  - Keys: B/I opens the inventory (game paused), 1/2 are HP/MP potions, F6 toggles bloom (moved off B), Esc closes the panel. Q/E/R are now only a debug look override.
  - UI: the UGUI `RpgHud` shows level, HP/MP/EXP bars, gold, potions and stones, plus a level-up banner and toasts. `InventoryPanel` (+ .Loadout / .Bag partials, BagWidgets, BagSprites) is a port of the web bag: painted bag frame (Resources/Sprites/ui/bag-frame), Equipment / Inventory / Skill tabs (Skill = not ported yet), Equipment = worn slots around the hero portrait + Status table + Unequip / Enhance, Inventory = 3x4 paged grid (gold, potions, stones, then the bag) + item detail with Equip / Use, Enhance, Sort, Discard. Pinned to the right edge in web 960x640 coordinates; not yet seen in the editor. The IMGUI `Hud` keeps the skill line, FPS and help.
  - The held weapon, offhand and companion art follow the equipped items (tier from item level, enhanced art from +7, brighter glow at +9/+11/+13).
  - Next time in Unity: run FrostSmokeTest (it gained CheckRpg), then play-test and tweak the UI layout. Phase 7 (test assembly, build, screenshots) is still open. Details are in plans/261029-1720-unity-rpg-systems/plan.md.
  - Touch controls (TouchControls + UiSprites, web HUD style): D-pad bottom-left, attack disc + Dash [K] / skill [L] (cooldown sweep, MP) / Bag [B] arc bottom-right, potion buttons; F7 hides them. Skill icons sliced from the painted rosters by Tools/build-skill-icons.py into Resources/Sprites/skill-icons.
  - Music (2026-10-30): GameMusic (2 AudioSources, crossfade, unscaled) plays Resources/Audio/music/frost-hollow.mp3 (45 s loop, user-made in ElevenLabs) from Bootstrap; oakhaven-village.mp3 is ready for the village map. Editor/AudioImportSettings: music = Vorbis Streaming, sfx = DecompressOnLoad mono.
  - Sound (2026-10-30): GameSfx (Bootstrap-inited AudioSource pool, clips from Resources/Audio/sfx). Warrior combo: slash1-4.mp3 per step (ElevenLabs, user picked 1d 2d 3a 4b), shout4-a on the finisher, hit-a/b or crit-a/b when a swing lands. Unused variants moved out of the project.
  - Melee anim pipeline (2026-10-30): Tools/build-melee-sheets.py -> Sprites/<folder>/meleeN.png + Data/<gear>-melee.json; warrior combo step 2 = melee2 (weaponless body + equipped sword at measured anchors). SlashEffect = painted warrior-slash-vfx. See plans/261029-1720-chatgpt-pending-art/plan.md.
  - Android: menu Zenonia > Build Android APK (or -executeMethod ZenoniaProto.EditorTools.PlayerBuildScript.BuildAndroid -buildOut X.apk); landscape, IL2CPP ARM64, debug-signed; needs the Android Build Support module. 60 fps target on phones.
  - Decisions: random-class drops stay (the user chose this for trading). Pickup is auto on touch.

**Next steps:**
1. ChatGPT art. The quota resets 2026-10-30 at 11:42, so tonight it may still be blocked. The list is in C:\Project\zenoniaUnity\plans\261029-1720-chatgpt-pending-art\plan.md: assassin attack2/3 solid-blade fix, and weaponless sheets for warrior attack1/3/4 and assassin attack1-4.
2. Integrate the new warrior and assassin melee attack animations (art in Assets/ArtLibrary/anim). The pipeline is the weaponless body, then the EQUIPPED weapon overlay at per-frame anchors measured from the armed sheets, then the slash VFX (vfx/warrior, vfx/assassin) on the swing frame, oriented along the swing. Make the assassin daggers bigger. Record combo videos.
3. Verify the RPG port in Unity (smoke test + play) and finish phase 7. Then: other maps (Oakhaven and the rest), NPCs and quests, the skill trees (15 skills per class), the rest of the ultimates, a real UI, a URP switch, and a WebGL build test.

The web repo (C:\Project\zenoniaClone, branch `web`) is a read-only reference. Never commit or push without the user asking (they have asked each time so far).

Related: [[unity-is-main-workspace]], [[unity-prototype-preferred-visuals]], [[chatgpt-art-via-chrome]], [[weapon-art-match-hero-sprites]], [[five-class-art-overhaul]], [[work-autonomously-no-questions]].

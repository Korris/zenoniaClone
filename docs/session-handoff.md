# Session handoff — 2026-10-30 (evening)

The user (Tay Gân) continues on ANOTHER machine. Read this file first, then the plans named below. Branch `unity` on github.com/Korris/zenoniaClone.
Reply in Vietnamese. Never build/edit through the user's open Unity Editor project: build from a copy (Tools/build-from-copy.ps1 / a HEAD export into a scratch copy).
ChatGPT accounts: art/skills on "Tan Pham" (also "Tay" is the user's); never "xaochai" or "Tieu Duy Phuong".

## Done (all committed + pushed, last commit 5ce520c "skills wired into the hero and the netcode")
- Oakhaven village map (offline only), portal to/from Frost Hollow, 10 villagers, Elder Thomas Frost Slime quest; quest offer / NPC dialogue / chat / toast windows (ChatGPT gold sprites); gold mobile HUD, floating joystick, 3-slot swipe skill magazine.
- Class combat SFX for all 5 classes (ClassSfx); 45 ElevenLabs skill SFX.
- Skill trees (plans/261030-1521-skill-trees-implementation/): 40 skills (8/class, Lv cap 60, SP, ranks, respec 50 G x level), monster statuses (bosses: freeze/stun -> slow), data-driven skill executor, skill tree window (T), 38 skill VFX sheets sliced + 40 icons, phase 04 = TryCastSkill offline + online (protocol v3: skill index in input frames, SkillCast event, replicated cooldowns + monster status, online learn/assign/respec). Tests: ZenoniaNet 70, ZenoniaSkills 82 pass.
- Online phase 2 (plans/261030-1150-online-mmo-world/): prediction + reconciliation, delta snapshots, lag harness, 200-bot load test (tick 4.85 ms).
- Second weapon (hybrid design, plans/261030-1630-second-weapon-enhance/): phase 01 stat core done.
- Wizard VFX choreography layer (Assets/Scripts/SkillChoreo/*, spec plans/261030-1521-skill-trees-implementation/vfx-choreography-wizard.md, video .../visuals/wizard-choreo-vs-earthbreak.mp4). NOT hooked in yet.

## Next, in order
1. Apply the choreography hook, build from a copy, play-test:
   - Skills/SkillExecutor.Cast(ISkillBody ...): after `SkillRun.Begin(ctx);` add `SkillChoreoCastHook.Play(ctx);`
   - Skills/SkillVfx.Play: after `if (!ctx.View) return;` add `if (SkillChoreo.Has(ctx.Def.Class, ctx.Def.Id)) return;`
   - SkillVfx.Loop: first line also returns null when `SkillChoreo.Has(...)`
   - Skills/SkillBolt.StartArt: return when `SkillChoreo.Has(...)` (else the tinted dot fallback draws)
   - Mana Shield: keep the player from SkillChoreoCastHook.Play; End(true) on break, End(false) on expire.
   - Note: the sim bolt's own hit-stop/trauma stack with the choreography; consider syncing bolt speed to the script (~33 tiles/s).
2. Show the user the wizard video. If approved, ask ChatGPT (Tan Pham, skill chat https://chatgpt.com/c/6ae450c1-7638-83ec-a5ea-717aa58678ea) for the same spec for warrior / assassin / cleric / mechanic. Rule: self-buff VFX (Mana Shield, Heal, Sanctuary, Spirit Blessing, Guardian Spirit, Berserker) must be translucent and never hide the hero (user complaint). Implement as SkillChoreoDefs<Class>.cs, record videos (`--record-choreo`).
3. Skill phases 07-12 (per-class polish, balance, APK). Open items: dash/leap/blink rubber-band online (OwnerSim MoveLeft not built); key I is now skill slot 2 (bag = B); `--dev-level N` server hook must never be set on a public server; HeroController.cs is 265 lines (split it).
4. Second-weapon phases 02-07 (decisions in that plan.md: mechanic drone auto-attack from Lv1, 20% ATK / 2 s; floating wizard spellbook, 8 frames via ChatGPT; server-api OFFHAND_SCALE still 0.5 -> phase 06).
5. Online: multi-zone (village has no zone server yet: zone id per map + transfer), ApiClient wiring to the connect screen, Android IL2CPP check of the new netcode, then a new APK.

## Machine notes
- Unity 6000.3.9f1 (+ Android Build Support), dotnet SDK (net10 tests), node (server-api), Python with PIL/numpy/cv2, ImageMagick, ffmpeg are needed on the new machine.
- Scratch build copies (zu-copy*) and raw art/SFX variants lived in the old machine's temp folder and are NOT in git; recreate a copy with robocopy (exclude Library/Temp/Logs/Build/UserSettings).
- Build outputs (Build/) are gitignored.

# Session handoff — 2026-10-29


The user said they will continue "tối nay" (tonight, 2026-10-29). On resume, read this file first, then C:\Project\zenoniaUnity\docs\session-handoff.md and the plans in C:\Project\zenoniaUnity\plans\.

**State at stop (Unity, C:\Project\zenoniaUnity, branch `unity`, pushed to github.com/Korris/zenoniaClone):**
- Classes: all 5 playable. Class select on keys 1-5; F1-F5 switch class.
- Map: Frost Hollow, with wandering monsters and the Frostfang King boss.
- Game feel: hit-stop, shake, sword trail, bloom (B toggles it), F toggles all effects.
- Ultimates on L: warrior Earth Break, wizard Ice Storm. Class skills on L: assassin Shadow Step, cleric Heal. Recording flags: `--record-ultimate`, `--record-combo`.
- Ranged basic attacks (cleric, wizard, mechanic) use painted VFX sprite sheets (Assets/ArtLibrary/vfx). Mechanic C toggles orange/blue.
- Colours: warrior red, wizard violet, mechanic orange (C for blue), assassin purple X-slashes, cleric green-gold.
- Monster hit flash is capped at 50%.
- Assets/ArtLibrary holds all the web art (806 files).
- RPG systems port (stats/EXP, loot, 6-slot equipment, enhance +15, potions, save, inventory UI on B): an agent was told to wrap up and commit. Check plans/261029-1720-unity-rpg-systems/plan.md for what is done and what remains.

**Next steps:**
1. ChatGPT art. The quota resets 2026-10-30 at 11:42, so tonight it may still be blocked. The list is in C:\Project\zenoniaUnity\plans\261029-1720-chatgpt-pending-art\plan.md: assassin attack2/3 solid-blade fix, and weaponless sheets for warrior attack1/3/4 and assassin attack1-4.
2. Integrate the new warrior and assassin melee attack animations (art in Assets/ArtLibrary/anim). The pipeline is the weaponless body, then the EQUIPPED weapon overlay at per-frame anchors measured from the armed sheets, then the slash VFX (vfx/warrior, vfx/assassin) on the swing frame, oriented along the swing. Make the assassin daggers bigger. Record combo videos.
3. Finish the RPG systems port if it is incomplete. Then: other maps (Oakhaven and the rest), NPCs and quests, the skill trees (15 skills per class), the rest of the ultimates, a real UI, a URP switch, and a WebGL build test.

The web repo (C:\Project\zenoniaClone, branch `web`) is a read-only reference. Never commit or push without the user asking (they have asked each time so far).

Related: [[unity-is-main-workspace]], [[unity-prototype-preferred-visuals]], [[chatgpt-art-via-chrome]], [[weapon-art-match-hero-sprites]], [[five-class-art-overhaul]], [[work-autonomously-no-questions]].

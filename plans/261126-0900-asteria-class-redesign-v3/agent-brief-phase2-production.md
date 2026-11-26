# Brief phase 2: Asteria class V3 production (one agent per class, parallel, 26/11/2026)

Work context: D:\zenoniaClone (Unity 6000.3.9f1 RPG, branch ro-soft-adventure). Reports: D:\zenoniaClone\plans\reports\ . Plans: D:\zenoniaClone\plans\ . Windows 11, PowerShell + Git Bash. Final report to the lead in Vietnamese, short, unresolved questions at the end.

## MANDATORY first
1. Read ALL of `docs/sprite-rules.md` (project rule). It now holds the V3 standard (top of the "hình mẫu" section): Knight silhouette + numbers, user decisions.
2. Read `CLAUDE.md` fully, and in `docs/pending-tasks.md`: the 26/11 entries (design phase of your class: files, chat link, open issues) and the Knight V3 / Golden Paladin entries (~lines 31-40): that is the pipeline that turned the Warrior into Knight V3 (idle 5 dirs, hair styles in grey, weapons, walk 8 frames, attack 8 frames x 8 dirs, `Tools/build-ro-fantasy-hero-game-assets.py` with `write`, `fists`, `grips`, `check`, the `ro_fantasy_*` helper modules). Read that tool and its helpers before changing anything.
3. Your design-phase files: `Assets/ArtLibrary/ro-fantasy-heroes/src/<class>-v3/` and `docs/reference/character-v3/<class>-v3/` (approved idle sheet named in your class's pending-tasks line).

## User decisions (binding)
- Wizard, Assassin, Cleric are FEMALE; Mechanic, Archer male. Female skulls 4-6 % narrower than the Knight are accepted.
- NO class wears a hat / helmet by default (hats are only equipment): hair layers are drawn WITHOUT hat (Cleric's white mitre and Archer's feather hat are NOT part of the base look).
- Archer holds the bow in the LEFT hand like a real archer.
- References for ChatGPT: the Knight proportion silhouette `docs/reference/character-v3/shared-v3-refs/knight-proportion-silhouette-5dir.png` + numbers, never old sheets of any class. Your approved V3 idle is the size standard for every later sheet of your class (sprite-rules rule 8: send the idle of the same direction and state the head size).

## Goal: your class playable in game with its V3 look (archer: art + cut sheets only)
Same scope as Knight V3, in this order, each step reviewed by ChatGPT (>= 8.5) and checked by YOU on composited images like the game draws them (sprite-rules: "sửa xong phải tự xem ảnh"):
1. Hair: grey dye-able hair layer(s) for 5 dirs on the bald head (no hat), as many styles as the class has now (check `class.json` / `HAIR_STYLES_OF`; at least 2). Grey value rules are in sprite-rules (highlight area reaching the Light node, etc.).
2. Weapon layer(s) separate (staff / cannon / daggers / holy staff / bow + quiver), grip anchors per the existing weapon pipeline.
3. Walk 8 frames x 5 dirs, attack 8 frames x 8 dirs (as Knight V3), cut with the pipeline; fists / grips tables for your class.
4. Install into `Assets/Resources/Sprites/classes/<class>/` and `Assets/Resources/Data/classes/<class>/` (archer: do NOT install or write code: leave cut sheets ready under ArtLibrary and say what code the class needs).
5. Build and capture in game (`--class <class> --map tauWillowbrookVillage --shots ...`), look at every direction / animation, send to ChatGPT for the in-game verdict.
Keep old files: never delete the class's previous art; put any file you replace in a backup folder next to it or under `D:/temp/<class>-v2-backup/` and write how to revert.

## Parallel safety (5 agents at once)
- File ownership: only your class's folders (`src/<class>-v3/`, `Sprites/classes/<class>/`, `Data/classes/<class>/`, `docs/reference/character-v3/<class>-v3/`, new tool modules named with your class). Shared files (`Tools/build-ro-fantasy-hero-game-assets.py`, `ro_fantasy_*` modules, C# code, `docs/sprite-rules.md`, `docs/pending-tasks.md`): change only when unavoidable, minimal, re-read right before editing, prefer per-class tables / new per-class modules; never reformat or revert others' edits.
- Unity builds: ONE at a time. Before `Tools/build-from-copy.ps1`, take the lock: `mkdir D:\temp\zu-build-lock` (fails if it exists: wait and retry every 60 s); remove the folder when the build is done (also on failure). Build: `Tools/build-from-copy.ps1 -CopyDir D:\temp\zu-copy-b -OutExe D:\temp\player-<class>-v3\Zenonia.exe`. Run your exe for captures without the lock (captures are fine in parallel). The user may have Unity open on the project: never build into the project itself.
- ChatGPT: start on account Tay ("Tân Korris", Plus: verify with fetch('/api/auth/session')); your OWN tab + continue your class's design chat (link in pending-tasks).
- Tay out of images (user 26/11: "nếu Tay Plus hết hạn thì mang qua Tan Pham Plus làm tiếp"): all 5 agents move TOGETHER, never one account per tab:
  1. The first agent that hits the Tay image limit creates the flag file `D:\temp\chatgpt-use-tanpham.flag` (write the time + your class in it).
  2. EVERY agent checks that flag before each ChatGPT request. Once it exists: use account Tan Pham (Plus). It may already be signed in in another connected Chrome (use list_connected_browsers / select_browser, check the account name with fetch('/api/auth/session') = Tan Pham, Plus) or in the same browser after the user switches. Open a NEW chat there for your class (re-upload your approved V3 idle, turnaround, the Knight silhouette and the art direction sheet; say it continues the design), keep its link in your report.
  3. NEVER type passwords or log in / log out yourself (a sign-in is the user's job): if no browser shows Tan Pham signed in, stop drawing, finish what you can without new images, and report exactly where you stopped.
  4. Never switch back and forth: once on Tan Pham, stay there.
- Chrome tips: tools in ONE ToolSearch call; composer `.ProseMirror` + execCommand insertText + `button[aria-label="Send"]`; uploads via file_upload to the hidden "Attach photos" input (files inside D:\zenoniaClone); download generated images with fetch(img.src)->blob-><a download> using a filename with your class, move only your file out of %USERPROFILE%\Downloads; scroll scrollables to bottom before reading; JS waits < 40 s; "Analysis paused / Skip" -> click Skip; no dialogs; close only your tab.

## Docs and report
- Log every new sprite error in sprite-rules "Sổ lỗi" (cause + fix), re-reading the file before editing.
- One updated line for your class in `docs/pending-tasks.md` (26/11 section): what is done, files, exe path, revert note.
- Do not commit. Report: steps done / scores, exe path, what you looked at in game and what you did NOT look at, open issues.

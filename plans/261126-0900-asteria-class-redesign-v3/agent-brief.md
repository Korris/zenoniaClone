# Brief: Asteria class redesign V3 (one agent per class, 26/11/2026)

Work context: D:\zenoniaClone (Unity 6000.3.9f1 RPG, branch ro-soft-adventure). Reports: D:\zenoniaClone\plans\reports\ . Plans: D:\zenoniaClone\plans\ . Windows 11, PowerShell + Git Bash. Final report to the lead in Vietnamese, short, unresolved questions at the end.

## MANDATORY before anything
1. Read ALL of `docs/sprite-rules.md` (project rule for every sprite / character task). Note the "art ChatGPT" rules (head size rule 8), the character template section ("Hình mẫu nhân vật": body/bare + body/headless + head anchor) and the "Sổ lỗi" rows about Starward Knight V3 / Golden Paladin (24/11): sending an OLD sheet of a character as a "technical template" made ChatGPT copy the old design. A redesign must NOT send old sheets of that class; give numbers instead.
2. Read `CLAUDE.md` ("Sprite và VFX", "Art trên ChatGPT") and `docs/pending-tasks.md` ~lines 31-35 (Knight V3 / Golden Paladin pipeline and chats).

## Task (user)
"Design lại các nhân vật còn lại, có cả archer. Tỉ lệ cơ thể, chiều cao phải giống warrior hiện tại; bỏ knight (nó là warrior hiện tại)." Then: one agent per class, in parallel.
Reference: `docs/reference/character-v3/asteria-character-art-direction-v1-6-classes.png` (ChatGPT's "Asteria Character Art Direction V1": Knight, Mage, Engineer, Rogue, Priest, Ranger + rules: ~2.7 heads tall, head ~37 % of height, hair on its own layer, 192x192 cells feet on the bottom edge, weapon / shield separate layers anchored to the hand, T1-T9 gear tiers).
Class map: Mage = `wizard` (NO witch hat: hair fully visible), Engineer = `mechanic`, Rogue = `assassin`, Priest = `cleric`, Ranger = new class `archer` (no game code yet: art only).

## Hard requirement: same proportions and height as the current Warrior
Approved Warrior idle source: `Assets/ArtLibrary/ro-fantasy-heroes/src/body-warrior-idle-5dir-chatgpt-6.png` (in game: `Assets/Resources/Sprites/classes/warrior/body/`). Measure it (head-to-feet px in its cell, head-height ratio, shoulder width) and give ChatGPT those NUMBERS, plus the Knight idle as proportion / height reference only ("chỉ để so tỉ lệ / chiều cao, KHÔNG chép thiết kế"). The new class must match height and head ratio within ~3 %: verify by measuring the output and by a side-by-side image at the same scale. Also attach `docs/reference/hero-idle-down-5-classes.png` (CLAUDE.md rule).

## Scope = DESIGN phase only (no install, no code changes)
1. ChatGPT designs the class in the Asteria style of the reference sheet (its version of the class is the starting point; ChatGPT decides the visual design, do not dictate screen contents beyond the requirements).
2. Turnaround (Front, 3/4 Front, Side, Back, 3/4 Back) like the Knight brief row of the sheet, and an IDLE 5-direction sheet with the same layout / cell rules as `body-warrior-idle-5dir-chatgpt-6.png` (copy its direction order, background, spacing). Weapons as sprite-rules.md says (separate layer); if unsure get both an idle with weapon (for review) and without.
3. ChatGPT scores each result /10 against the art direction and the Knight's proportions; redo once or twice if < 8.5 or the height measurement is off.
Save every image via fetch blob + `<a download>` (never screenshots), names always containing the class:
- sources: `Assets/ArtLibrary/ro-fantasy-heroes/src/<class>-v3/` (e.g. `turnaround-<class>-v3-chatgpt-1.png`, `body-<class>-idle-5dir-v3-chatgpt-1.png`)
- review images + verdict text + prompt used: `docs/reference/character-v3/<class>-v3/`
Never overwrite existing files in `src/`.

## ChatGPT via Claude in Chrome: SHARED BROWSER, 5 agents in parallel
- Open YOUR OWN tab (tabs_create_mcp) and YOUR OWN new chat in account Tay (Plus, "Tân Korris": verify with `fetch('/api/auth/session')`). Never touch / close other tabs. Close only your tab at the end.
- NEVER switch ChatGPT account (it would log out the other agents' tabs). If Tay runs out of image generations, stop and report.
- Tips: load the chrome tools in ONE ToolSearch call; composer `.ProseMirror` + `document.execCommand('insertText', ...)` + click `button[aria-label="Send"]`; upload via file_upload to the hidden "Attach photos" input (files must be inside D:\zenoniaClone: copy references there); generated images = `img` alt "Generated image 1" (~1448x1086 / 1536x1024): download with fetch(img.src)->blob->`<a download>` using a filename containing your class, then move it from %USERPROFILE%\Downloads (move only YOUR file); scroll every scrollable element to the bottom before reading (virtualised DOM); keep each JS wait under ~40 s; `get_page_text` reads answers; "Analysis paused / Skip": click Skip. No browser dialogs.

## Report
Final files, measured height / head ratio vs Knight, ChatGPT score, chat link, what is still off. Add ONE line for your class to `docs/pending-tasks.md` (top section, 26/11) with the chat link and file folder (edit carefully: other agents edit the same file; re-read right before editing). Do not commit.

# Phase 06 - VFX / icon / SFX asset pipeline (tooling + prompt templates)

## Context links
- Approved look: `Assets/ArtLibrary/custom/skill-vfx-preview/<class>.png` (1774x887, 4x2 panels, panel order = skill index; each panel also has a clean ~70 px icon bottom-left)
- Icon reference: `Assets/ArtLibrary/62121b6b-e18b-489a-99e4-93e54a28993f.png` (only Mechanic/Wizard have 8 icons; Warrior/Assassin/Cleric show 6 -> not enough)
- Existing tools: `Tools/slice-vfx-sheets.py` (3 rows x 4 cols, gutter detection, pivots, JSON), `Tools/build-skill-icons.py` (roster -> 128 px icons), `Assets/Editor/ResourceTextureImportSettings.cs`
- ChatGPT: account **Tan Pham**, same chat as the previews: https://chatgpt.com/c/6ae450c1-7638-83ec-a5ea-717aa58678ea (never xaochai / Tieu Duy Phuong)
- SFX: ElevenLabs Sound Effects, same flow as `ClassSfx` (user picks variants), `Editor/AudioImportSettings` (sfx = DecompressOnLoad mono)

## Overview
Priority P1 | Lane D | can start NOW (tools + art only; no C# gameplay files). Produces per-skill sprite sets, icons and clips that `SkillVfx` / `SkillSfx` / `SkillIcons` load by convention.

## Key insights
- The existing slicer already solves transparent-gutter slicing + pivots; generalise, don't rewrite (DRY): move its functions into `Tools/vfx_slice_common.py`, keep `slice-vfx-sheets.py` behaviour identical.
- ChatGPT image output is sometimes not truly transparent: support a `--from-black` path (pure black background -> alpha = max(R,G,B), unpremultiply). Glowing VFX survive this well and render additive anyway.
- Cells from a 1536x1024 sheet are ~384x341 px; downscale frames to max 256 px (memory on Android; 38 sets x 12 frames).
- Ice Storm / Earth Break keep their existing art (`Sprites/fx/wizard/ice-storm`, `fx/earthbreak`) -> 38 new sheets.

## Asset conventions
```
Source (not shipped): Assets/ArtLibrary/custom/skill-vfx/<class>/<skill-id>.png        (skill-id = kebab, e.g. fireball)
Sprites:             Assets/Resources/Sprites/skills/<class>/<skill-id>/{cast,loop,impact}_<n>.png  + icon.png (128 px)
Data:                Assets/Resources/Data/skills/<class>-<skill-id>.json   (frames: name, w, h, px, py; rows present; fps hint)
Audio:               Assets/Resources/Audio/sfx/skills/<class>/<skill-id>-{cast,impact,loop}.mp3
```
Row meaning: `cast` = at hero/hand (windup, 4 f), `loop` = projectile / beam / zone / summon loop (4 f), `impact` = hit/explosion/area burst (4 f).
Skills without a projectile use `loop` for the persistent part (zone ring, clone shimmer, shield bubble) or leave it empty.

## ChatGPT prompt template (one message per skill, 8 per class batch, same chat)
```
Game-ready VFX sprite sheet for the skill "<NAME>" (<CLASS>), matching panel <N> of the preview you made earlier in this chat
(same colours: <PALETTE>, same painted chibi-RPG style). TRANSPARENT background (PNG alpha); if transparency is impossible use pure #000000 black.
Layout: 3 rows x 4 columns, 1536x1024, equal cells, generous empty gutters, every frame fully inside its cell, nothing touching edges.
Row 1 "cast": 4 frames of the <cast description> at the caster's hand, growing.
Row 2 "loop": 4 frames of <projectile/beam/zone description>, travelling to the RIGHT, seamless loop.
Row 3 "impact": 4 frames of <impact description>, big flash -> dissipating.
No characters, no monsters, no ground, no text, no UI, no icons. Top-down 3/4 view like the preview.
```
Icons: one extra message per class: "8 skill icons, 2 rows x 4, 1024x512, transparent, same icons as the preview panels, square gold-trim frames removed".
Fallback icons: crop the panel icons from the preview sheet (`--from-preview`).

## Related code files (create)
- `Tools/vfx_slice_common.py` (moved helpers: clean_mask, spans, trim, pivots, write_frame)
- `Tools/slice-skill-vfx.py` (args: `<class> [skill-id ...] [--from-black] [--max 256]`; reads source sheets, writes sprites + JSON, prints per-row frame counts; non-zero exit on wrong grid)
- `Tools/build-skill-tree-icons.py` (8-icon class sheet -> `icon.png` per skill; `--from-preview` crops panel icons from the preview sheet at fixed rects, upscales to 128 with Lanczos)
- `Tools/normalize-skill-sfx.ps1` (ffmpeg: trim silence, mono 44.1 kHz, loudnorm -16 LUFS, peak -1 dB, mp3 160k)
- Prompts live in this phase file (no extra doc). Note: the existing slicer imports `cv2` (opencv-python); confirm `py -3 -E -c "import cv2"` or replace the connected-components call with a numpy/PIL flood fill in the common module.
Modify: `Tools/slice-vfx-sheets.py` (import common helpers; output byte-identical - verify with a diff of the regenerated `Resources/Data/vfx-*.json`)
`Assets/Editor/ResourceTextureImportSettings.cs` only if `Sprites/skills/` needs different settings (expected: none; it covers `Resources/Sprites/`).

## SFX list (ElevenLabs; 2-3 variants each, user picks; -cast unless noted)
| Class | Clips |
|---|---|
| Warrior | power-slash (heavy whoosh), whirlwind-loop (spinning blade wind), flame-cleave (fire whoosh + crackle), charging-strike (rush + armour clank), blade-cyclone-loop, inferno-crash (leap grunt) + -impact (fiery ground slam), berserker-awakening (roar + flame burst). Earth Break: reuse existing shout + add earth-break-impact (rock slam) |
| Wizard | arcane-bolt (zap), fireball + -impact (explosion), ice-storm (keep; add ice-storm-impact shatter), arcane-lance (charged beam), mana-shield (glassy hum), frost-nova (freeze crack burst), elemental-burst (fire+ice boom), astral-cataclysm (rumble) + -impact (meteor hit) |
| Assassin | shadow-strike (double blade swish), shadow-step (dark whoosh teleport), blade-dance-loop, phantom-rush (3 quick dashes), venom-daggers (stab + poison hiss), shadow-clone (dark shimmer), death-mark (ominous ping), nightfall-execution (crescendo slashes) |
| Cleric | heal (soft chime rise), spirit-blessing (choir swell), holy-ray (light beam), divine-light (bell + burst), sanctuary-loop (warm hum), guardian-spirit (wing flap + shield ring), purification (cleansing wave), divine-ascension (angelic choir + boom) |
| Mechanic | power-shot (charged cannon), homing-rockets (launch x3) + -impact, plasma-barrage (rapid plasma), target-lock (lock-on beeps), rocket-punch (rocket fist + clang), robot-assault (servo + laser), emp-burst (electric discharge), orbital-cannon (satellite charge) + -impact (laser strike) |
Prompt template: "<description>, short 0.6-1.2 s game sound effect, punchy, no music, no voice, dry, mobile action RPG". Female voice none; reuse class shouts for ultimates.

## Implementation steps
1. Refactor slicer helpers into `vfx_slice_common.py`; regenerate existing sets; diff = no change.
2. `slice-skill-vfx.py` + `--from-black`; dry run on a test sheet cropped from the preview (sanity only).
3. Icons tool; produce wizard icons from the preview crop immediately (unblocks phase 05 visuals).
4. SFX normalize script.
5. Batch order = class order of phases 07-11 (wizard first). Per class: 8 VFX prompts + 1 icon prompt in the Tan Pham chat; save PNGs to the source folder; slice; user reviews a contact sheet (`slice-skill-vfx.py --contact` writes `plans/.../visuals/<class>-contact.png`).
6. ElevenLabs per class after the VFX batch; user picks; normalize; drop unused variants.

## Todo
- [ ] common helpers + regression diff
- [ ] slice-skill-vfx.py (+from-black, contact sheet)
- [ ] build-skill-tree-icons.py (+from-preview)
- [ ] normalize-skill-sfx.ps1
- [ ] wizard batch (VFX, icons, SFX) -> then warrior, assassin, cleric, mechanic

## Success criteria
- Each class: 8 icons (128 px), 7-8 VFX sets with 3 rows sliced (counts printed = 4/4/4 or documented), JSON valid, contact sheet approved by the user.
- Existing painted sets unchanged after refactor.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| ChatGPT quota / daily limits | H x M | one class per day; code uses procedural fallback until art lands |
| Non-transparent or touching frames | H x M | `--from-black`, stricter prompt, manual gutter fix in ImageMagick (`-crop` per cell) |
| Style drift from previews | M x M | same chat, reference panel number in every prompt, user review gate |
| APK size growth | M x L | 256 px cap, trimmed frames, sfx mono 160k |

## Security
No credentials in repo; ElevenLabs/ChatGPT used via the user's browser sessions (API key only via env var if the API route is chosen).

## Next steps
Phase 07 consumes the wizard batch.

## Unresolved questions
1. ElevenLabs via web UI (user picks) or via API with a key (scripted 3 variants each)? Default: web UI as before.
2. Clean 40-icon sheet from ChatGPT, or are the preview-panel crops good enough? Default: try crops first, request sheet if blurry.

# World Map 2.1: 9 new monsters installed + Verdant re-level (2026-11-13)

Status: DONE_WITH_CONCERNS (not seen in game: no map spawns them yet and no debug option spawns a monster by type).

## Files

- `Assets/Scripts/MonsterDef.cs`: 9 new defs, appended to `All` (wire indices 27-35, old indices unchanged) and `ByType`; 4 Verdant monsters re-levelled. Wanted bosses untouched.
- `Assets/Scripts/Rpg/LootTable.cs`: 9 new loot rows, 4 re-levelled rows.
- `Tools/build-meadow-monsters.py`: table extended (`WORLD2_DIRS`, `DROP_SHADOW`, `FLYERS`), baked-shadow removal, flyer ground anchor. Old ids cut exactly as before (new code only runs for the new ids).
- `Assets/Resources/Sprites/monsters/<id>.png` + `<id>-attack.png` + `.meta` (copied from snowFox, new guid) for the 9 ids.
- `docs/reference/world2-monsters-installed-check.png` (contact sheet), `docs/sprite-rules.md` (2 rows in "Sổ lỗi").
- Previews: `plans/visuals/world2-monsters/`.

## Curve (reused from step 1, nothing new)

Line HP 14+22L, Atk 3+4L, Def 1+L, EXP 14L, gold L..2L+1, times the role factor of an existing monster. Bosses: Glacier Warden / Mirefang Hydra rule (HP x7, Atk x1.5, Def x0.75, EXP and gold x6.67, sure drops, 2 stones). HP below is the ctor input (game doubles it).

## New monsters

| id | Lv | HP / Atk / Def / Speed | Role copied from | EXP, gold | RenderScale, height vs hero (164 px) | Baked shadow |
|---|---|---|---|---|---|---|
| forestWolf | 7 | 139 / 28 / 3 / 62 | Spectral Wolf | 91, 5-12 | 1.1, 1.03x | none in art |
| thornSprite | 9 | 170 / 47 / 3 / 66 | Ice Wraith | 126, 9-19 | 1.0, 0.91x (hovers ~10 px) | removed, used as ground anchor |
| thornfangAlpha (map boss) | 10 | 1638 / 65 / 8 / 44 | boss rule | 934, 67-140 | 1.65, 1.90x | none in art |
| thornheartTreant (dungeon boss) | 10 | 1638 / 65 / 8 / 28 | boss rule | 934, 67-140 | 2.0, 2.17x | small patches removed |
| highlandIbex | 16 | 366 / 74 / 17 / 48 | line, Atk x1.1 | 224, 16-33 | 1.0, 1.01x | none (attack: small) |
| dustscaleLizard | 17 | 310 / 85 / 6 / 66 | Ice Wraith | 238, 17-35 | 1.15, 1.03x down (side lower) | removed (attack only) |
| cliffHarpy | 18 | 308 / 86 / 6 / 58 | brittle flyer, reach 22 | 252, 18-37 | 1.0, 1.02x (hovers ~15 px) | removed, used as ground anchor |
| stonebackGuardian | 20 | 817 / 91 / 34 / 26 | Crystal Golem | 448, 32-66 | 1.4, 1.38x | removed |
| ridgehornBehemoth (map boss) | 20 | 3200 / 125 / 16 / 36 | = Glacier Warden | 1865, 133-273 | 1.8, 1.99x | removed |

All baked shadows removed: the game already draws a code shadow under every monster (`Monster.cs`).

## Verdant levels (final)

| Map | Monster | Lv (was) | HP / Atk / Def | EXP, gold |
|---|---|---|---|---|
| Willowbrook 1-5 | meadowSlime | 1 (1) | 32 / 6 / 1 | 12, 1-3 |
| | hornRabbit | 3 (2) | 64 / 14 / 1 | 39, 2-6 |
| | thornBoar | 5 (3) | 161 / 26 / 6 | 84, 6-13 |
| Bramblewood 6-10 | meadowMantis | 6 (7) | 117 / 35 / 3 | 96, 7-14 |
| | forestWolf | 7 | new | |
| | brambleTreant | 8 (10) | 342 / 46 / 13 | 179, 13-27 |
| | thornSprite | 9 | new | |
| | thornfangAlpha | 10 | new, map boss | |
| Rootburrow | thornheartTreant | 10 | new, dungeon boss | |

## Sprite checks (looked at, on dark + light + green)

- Contact sheet panels of all 9 (every walk and attack frame at half size, plus game-scale line-up with hero).
- 2x zooms: Thorn Sprite (3 cells), Ridgehorn attack (3 impact cells), Stoneback walk (3) + attack (1), Treant walk (1).
- No frame clipped, no neighbour pixels, no split warning from the cutter. Harpy feathers in the UP row fly up.
- Found and fixed: Stoneback walk kept grey-purple shadow patches under the feet with the first threshold (0.7); lowered to 0.55, re-cut all 9, re-checked.
- Left as is: Thorn Sprite light dots have a pink halo (hollow pink rings on a light backdrop; reads as pink sparkles on green / dark). Thin pink streaks at the edge of Ridgehorn / Stoneback impact bursts (few px).

## Not verified

- In game: nothing. New maps are not registered and `--dummies` only spawns frost slimes; I did not build (would only have confirmed compile, and the build copy is shared). Compile is covered by the dotnet tests, which include both C# files; Editor.log shows no compile error.
- Code shadow size / position under the flyers and the 2.0x treant, HP bar height (`bodyHeightPx`), blur at RenderScale 1.8-2.0.
- Balance in play (boss damage vs a Lv10 hero).

## Tests

`dotnet test Tests/dotnet/ZenoniaSkills.Tests` 297 passed; `Tests/dotnet/ZenoniaNet.Tests` 335 passed (after the final edit). `~/.dotnet/dotnet.exe` does not exist on this machine; `dotnet` on PATH was used.

Server must be rebuilt (monster table and loot table are shared with the zone server). Nothing deployed.

## Open questions

- Willowbrook: rabbit 3 / boar 5 chosen to fill the 1-5 band (brief gave only the band). OK?
- Cliff Harpy is melee with reach 22: monster AI has no ranged attack. Add a projectile attack later?
- Thornfang Alpha and Thornheart Treant share HP / Atk / Def (same boss rule at Lv10), differ in speed, timings, reach, size. Want the dungeon boss tougher?
- Stoneback EXP uses the Bramble Treant tank factor (x1.6); Crystal Golem, same role, has flat 14L. Which is intended?
- `docs/world-map-2-art-progress.md` not updated (not my file): its "Quái mới" table could note these 9 as installed.

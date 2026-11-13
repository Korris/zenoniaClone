# New maps: Bramblewood Forest, Rootburrow, Thornheart Chamber (13/11/2026)

## Done

- 3 maps registered (`MapRegistry`): `bramblewoodForest` 36x24, `rootburrow` 36x24, `thornheartChamber` 24x16. All Hostile, music = frost-hollow (no own track).
- Ground: `Sprites/village/tiles/{bramblewood-forest,rootburrow,thornheart-chamber}-ground.png` (2304x1536, 2304x1536, 1536x1024 no upscale). Single image, so minimap uses the ground (no `-minimap` needed).
- Props: 20 `bw*` sprites (`Tools/cut-bramblewood-props.py`, trees x1.05 = 0.7 x 1.5, small things x0.8, stump/mushrooms x0.6), defs `BramblewoodForestPropDefs.cs`, 78 placements `BramblewoodForestProps.cs`. Trees + 2 big rocks solid, rest walk-through. Rootburrow / chamber: no props.
- Rows: hand-classified per tile (`plans/261113-1010-world-map-new-maps/layout/*-rows-hand.txt` = source of truth; `Tools/build-bramblewood-rootburrow-rows.py` expands + flood-fills + writes grounds). No colour classifier: walls, dirt, trunks share browns.
- Portals (always open):
  - Willowbrook (46,44)/(47,44) -> Bramblewood (17,2)/(18,2); Bramblewood (17,0)/(18,0) -> Willowbrook (47,42)
  - Bramblewood cave (2,13) -> Rootburrow (32,13); Rootburrow (35,12)/(35,13) -> Bramblewood (4,13)
  - Rootburrow arch (4,3)/(5,3) -> Chamber (11,10)/(12,10); Chamber (11,11)/(12,11) -> Rootburrow (4,5)/(5,5)
- Spawns (`MapSpawnTable`):
  - Bramblewood 30: NW mantis 5 + wolf 5, NE wolf 4 + treant 6, S treant 4 + sprite 6; boss Thornfang Alpha at (24,18). Respawn 6-9 s.
  - Rootburrow 16: east hall wolf 2 + treant 3, north hall sprite 5, west hall sprite 3 + treant 3. Respawn 10-11 s (like Frostfang Den; no instances).
  - Chamber: Thornheart Treant at (12,5), no zones.
  - `HasOwnBoss` true for Bramblewood + Chamber. Rootburrow `BossFor` = null.
  - Willowbrook: treant 8 -> thornBoar 8, mantis 8 -> hornRabbit 8, mantis 7 -> thornBoar 7. Total stays 70.
- Shared-file edits (own lines only): `MapRegistry.cs`, `MapSpawnTable.cs`, `WillowbrookMeadowsMap.cs` (2 portal entries + comment), `VillagePropDefs.cs` (TryGet chain, one expression), tests `WillowbrookLargeMapTests.cs` (portal count 2 -> 4, clearance list), `MapGraphTests.cs` (Willowbrook no longer dead end).
- New tests: `Tests/dotnet/ZenoniaNet.Tests/BramblewoodRootburrowMapTests.cs`.

## Checks

- `Tools/preview-bramblewood-rootburrow-composite.py`: 0 PROBLEM (reachability after props, HUD rows, portal/arrival/start/boss tiles open, no solid prop on road/bridge/zone, zones >= 2x free tiles and >= 5 tiles from start/portals/arrivals).
- `dotnet test`: ZenoniaNet 386/386, ZenoniaSkills 297/297 (last run).
- Build from copy (lock taken/released): 0 errors, `C:/temp/player-bramble/Zenonia.exe`.

## Seen in game (offline, warrior, `--god`)

Images in `docs/reference/`: `bramblewood-ingame-*.png`, `rootburrow-ingame-*.png`, `thornheart-chamber-ingame-*.png`, `willowbrook-ingame-south-gate.png`, plus `*-solids-overlay.png`, `bramblewood-forest-props-composite.png`.

- Bramblewood: whole map plain + `--solids`, top arrival, cave portal, bridge, NW / NE / S clearings with monsters, Thornfang Alpha, east road end with solids.
- Rootburrow: whole map plain + solids, entrance, boss arch, NW corridor solids, west and north halls.
- Chamber: arrival with boss, whole room solids.
- Transfers by `--tp` onto the trigger tile, landing confirmed from log position + monster count: Bramblewood->Willowbrook, Willowbrook->Bramblewood, Bramblewood->Rootburrow, Rootburrow->Bramblewood, Rootburrow->Chamber, Chamber->Rootburrow.

## Not verified

- Walking by hand through a portal (only teleport onto trigger tiles). Reachability of triggers is covered by flood-fill tests only.
- Online / zone server (build not run as server; server must be rebuilt, spawn table changed).
- Fighting: boss skills, drops, respawn timing, monster balance.
- Screens other than 1280x720; Unity Editor import (new PNG/CS have no `.meta` yet, Unity will create them).
- Images of `b-nw`, `r-west`, `r-north`, `b-to-*` runs were captured (C:/temp/cap-bramble) but not all opened: opened overview, solids, top, cave, bridge, NE, S, east end, Willowbrook gate, Rootburrow overview-solids / corridor / entrance / arch, chamber x2.

## Notes / deviations

- Rootburrow NW corridor (cols 5-6, rows 7-9) is 2 tiles; the east stone column covers about half of col 6. Passage (10-11, rows 7-9) likewise. Hero can overlap stone rims slightly.
- Bramblewood bridge: rows 12-13 both open; row 13 includes the lower rail.
- East road end: cols 34-35 solid, bramble props drawn over it.
- Chamber is 24x16 (art), not 24x18. Camera shows the painted arch below the corridor mouth.
- Boss HP shown 3276 in the build vs 1638 in `MonsterDef` (dev scaling, not touched).
- Design doc says dungeon monsters do not respawn and Willowbrook has 90: not applied (task said copy Frostfang Den, keep count).

## Unresolved questions

1. Bramblewood east exit: which map goes there (or repaint the road end)?
2. Willowbrook south gate chosen at trail end (46-47,44) east of the pond: OK, or should it be a wider marked gate?
3. Rootburrow respawn: keep 10-11 s until instances exist?
4. `docs/pending-tasks.md`, `docs/world-map-2-art-progress.md` not updated (other agents editing them).

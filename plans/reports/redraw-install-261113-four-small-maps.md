# Redraw install: Amber Dunes, Emberfen Marsh, Sunspire Oasis, Sunken Reliquary (2026-11-13)

## Done

- Ground textures overwritten in place (same path, same `.meta`, RGBA 2304x1536, Lanczos upscale of the 1536x1024 redraw):
  `Assets/Resources/Sprites/village/tiles/{amber-dunes,emberfen-marsh,sunspire-oasis,sunken-reliquary}-ground.png`
  - Amber source = `amber-dunes-redraw-chatgpt-2.png` (v1 ignored).
- Minimap: none of the 4 maps has a `-minimap` file; `Minimap.cs` bakes from the single ground image, so it follows the new art (seen in game).
- Collision rows: only `EmberfenMarshMap.cs` changed, 2 tiles:
  - (18,2) g/d -> T: new rock by the north thicket spills half into col 18.
  - (11,11) g -> w: mid-west pond is one tile wider on the east side.
- Amber, Sunspire, Reliquary rows untouched (new art matches the old layout tile for tile). `SunspireOasisMap.cs` not edited at all.
- No portals, zones, props, Width/Height touched. Not committed.

## Checks

- Blend old/new + red collision overlay per map: viewed all (`docs/reference/*-redraw-old-new-blend.jpg`, `*-redraw-solids-overlay.jpg`).
- Flood fill (rows + prop + NPC footprints): 0 unreachable tiles on all 4; all portals on `d` tiles and reachable; no prop footprint on road/bridge tiles; every monster zone has >= 2x count free reachable tiles (lowest: Reliquary shrine room 9 tiles for 2, Emberfen marshStalker east 15 for 3).
- Bottom 3 rows blocked: Amber, Emberfen, Reliquary yes. **Sunspire row 21 is partly walkable (cols 6-10, 14-19): this was already so before the redraw** (class comment says only rows 22-23 blocked). Left as is, see questions.
- Caveats from the brief:
  - Emberfen bottom-right road: in the redraw it ends at ~col 31, row 16 inside the field, does not reach an edge; no portal there. East portal (35,2) sits on the top road at the east edge; west portal (0,18) on the road. Unused stub (35,10) still on road; the painted road there is 1.5 tiles wide and its lower half (35,11) stays blocked as before.
  - Sunspire: 5 buildings sit on the 4 pads, doorsteps free; palms, stalls, wagon, jars, fountain, notice board OK. No bush props exist in this map (old bushes were painted), nothing to fix.
  - Reliquary: boss room pattern fine, pillars/nest around ring OK.
  - Amber: east exit road reaches the edge at row 17 = portal (35,17).
- Build: `Tools/build-from-copy.ps1` -> `C:\temp\player-redraw4\Zenonia.exe`, 0 errors, lock taken and released.
- Tests: `ZenoniaSkills.Tests` 297/297 pass. `ZenoniaNet.Tests` 354/357: the 3 failures are not these maps (`willowbrookMeadows` tile 20,19 cut off; `WorldMapStep1RouteGoesBothWays`; `WillowbrookLargeMapTests.WestGateRows...`) = other agents' work in progress (Willowbrook / portals). `MapGraphTests` cases for the four maps here pass.

## Seen in game (offline, warrior, 1280x720; shots in `C:\temp\cap-redraw4\`, selection in `docs/reference/*-redraw-ingame-*.jpg`)

- Amber: full map with solids; spawn + west portal; north road stub not opened (file captured, not viewed); east portal close-up; walked into (35,17) -> landed in Emberfen on the road.
- Emberfen: full map with solids; both bridges (hero standing on each); east portal area; mid-west pond with solids (new blocked tile); south-east pond/road end; walked into (35,2) -> landed in Reliquary corridor.
- Sunspire: full map with solids; spawn/plaza; west portal + stall + wagon; east road with blacksmith, bounty NPC, notice board; inn; north road + elder + residences + forge.
- Reliquary: full map with solids; boss room with hydra; corridor to hall; south hall with pillars.

## Not verified

- Captured but not opened: `amber-north-stub`, `amber-zone-east`, `amber-east-portal-solids`, `emberfen-west-arrival`, `reliquary-spawn` (same spots are visible in other shots I did view).
- Portal travel into Sunspire / out of Sunspire and Reliquary -> Emberfen (portals owned by another agent / unchanged).
- Online (zone server) play; Android look (texture max size 2048, ETC2 override); server rebuild not done (Emberfen rows changed 2 tiles: if the server uses the same map classes for collision it needs a rebuild).
- Walking by hand along every edge; only teleport + screenshots.

## Props clashing with the new ground

None badly. Mild: Emberfen `swampTree` / `mangroveRoots` are darker and more muted than the brighter new grass (readable, acceptable). Not redrawn.

## Unresolved questions

1. Sunspire row 21 walkable under the HUD (pre-existing, rule 8 of map rules): block it? Not done because the portal agent is editing that file and a south exit may be planned on the painted south road.
2. `docs/world-map-2-art-progress.md` not updated (file has other agents' uncommitted edits): owner should mark the 4 maps as installed.

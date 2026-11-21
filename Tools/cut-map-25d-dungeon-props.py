"""Cuts the ChatGPT prop sheets of a 2.5D map with standing walls (first: Rootburrow, 21/11/2026; Frost Hollow + Frostfang Den snow / ice 21/11)
and sizes every prop to its PropDef width.

    python Tools/cut-map-25d-dungeon-props.py <rootburrow|frost-hollow|frostfang-den> [sheet-name-substring]   (e.g. "chest-warp" re-cuts only that sheet)

1. Tools/cut-magenta-prop-sheet.py keys each sheet (#FF00FF) into one PNG per prop (names in the order the prompt listed them) in a temp folder;
2. each PNG is scaled so its drawn width (or height, for a negative number) is the size wanted below (tiles x 64 px at 4 px per world unit, MapScenery), keeping the bottom-centre foot;
3. written to Assets/Resources/Sprites/village/props/<name>.png; prints the PropDef line with the real height (paste into VillagePropDefs if it changed).
"""
import os, sys, subprocess, tempfile
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHEETS = {
    "rootburrow": [
        ("rootburrow-dark-props-tall-chatgpt-2.png", "3x3", [("rbCaveWall", -2.6), ("rbCaveWallB", -2.6), ("rbStonePillar", -3.0), ("rbHangingRoots", -2.6),
                                                      ("rbPurpleCrystal", -2.4), ("rbPinkCrystal", 1.8), ("rbRuneObelisk", -2.2), ("rbRuneOrb", -2.3), ("rbCrystalNest", 1.5)]),
        ("rootburrow-dark-props-low-chatgpt-2.png", "2x3", [("rbCaveBoulder", 2.25), ("rbStoneCluster", 1.5), ("rbGlowShrooms", 1.0), ("rbPebbles", 1.5),
                                                     ("rbStoneSlab", 2.5), ("rbRuneSeal", 3.0)]),
        ("rootburrow-dark-props-stairs-chatgpt-1.png", "1x2", [("rbStairsDown", 3.6), ("rbStairsUp", 3.6)]),   # V4 floor stairs (21/11/2026)
        ("rootburrow-chest-warp-props-58-chatgpt-1.png", "1x5", [("rbChest", 1.3), ("rbChestOpen", 1.3), ("rbChestRare", 1.5), ("rbChestRareOpen", 1.5),
                                                               ("rbWarpCrystal", -2.4)]),   # V5 alcove chests + warp crystal (DungeonPoints, 21/11/2026)
    ],
    # Frost Hollow 2.5D snow (21/11/2026, docs/reference/frost-hollow-25d/snow-25d-art-direction-chatgpt.md): standing ice-rock cliffs on the solid
    # tiles north of the snow (Tools/place-map-25d-dungeon-props.py frost-hollow); trees / low props redrawn for 58 deg over the old fh* PNGs.
    "frost-hollow": [
        ("fh25d-cliffs-chatgpt-1.png", "2x4", [("fhCliffA", 2.3), ("fhCliffB", 2.3), ("fhCliffWide", 3.3), ("fhCliffTall", 2.9),
                                               ("fhCliffEndL", 2.0), ("fhCliffEndR", 2.0), ("fhCliffPillar", 1.4), ("fhCliffLow", 2.8)]),
        ("fh25d-trees-chatgpt-1.png", "1x6", [("fhPineBig", 3.4), ("fhPineMid", 2.7), ("fhPineSlim", 1.9), ("fhPineSapling", 1.4),
                                              ("fhDeadTree", 3.0), ("fhDeadTreeWide", 2.6)]),
        ("fh25d-low-props-chatgpt-1.png", "3x4", [("fhBoulder", 3.0), ("fhRock", 1.6), ("fhRockSmall", 1.3), ("fhRockSlab", 2.2),
                                                  ("fhLogSnow", 2.6), ("fhStump", 1.3), ("fhCrystalSmall", 1.0), ("fhCrystalMid", 1.3),
                                                  ("fhRuneTall", 1.4), ("fhRuneShort", 1.2), ("fhBushSnow", 1.4), ("fhIceCave", 3.6)]),
    ],
    # Frostfang Den 2.5D ice cave (21/11/2026): walls / crystals (crystals are DungeonLights by name) + props; icePillar, frozenStalagmite, frostCrystal and
    # frozenBones redrawn for 58 deg over the old PNGs (same names, same tiles in FrostfangDenMap).
    "frostfang-den": [
        ("fd25d-walls-chatgpt-1.png", "2x4", [("fdIceWallA", 3.2), ("fdIceWallB", 3.2), ("fdCrystalWall", 3.2), ("fdIcePillar", 1.3),
                                              ("fdIcicleWall", 2.3), ("fdIceWallEnd", 1.5), ("fdCrystalTall", 1.8), ("fdCrystalTeal", 1.4)]),
        ("fd25d-props-chatgpt-1.png", "2x4", [("icePillar", 2.0), ("frozenStalagmite", 1.6), ("frostCrystal", 2.0), ("fdCrystalShards", 1.2),
                                              ("frozenBones", 2.2), ("fdIceBoulder", 2.2), ("fdRockCluster", 1.6), ("fdRuneMarker", 1.3)]),
    ],
}
# Value pass after cutting (ChatGPT snow art direction section 5: cliff faces Y 0.09-0.22, darker base): rgb x a vertical ramp top -> bottom.
# The ChatGPT cliffs came out lighter than the snow ground, so they stood out brighter than the floor the FX lie on.
SHADE = {"fhCliff": (0.83, 0.70),   # review 21/11: caps -10 % (#A4B1C4-ish), faces kept
          "fhIceCave": (0.92, 0.75)}
SRC = {"rootburrow": "dark-25d", "frost-hollow": "snow-25d", "frostfang-den": "ice-25d"}   # Assets/ArtLibrary/maps/<key>/<this>


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "rootburrow"
    src = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", key, SRC[key])
    out = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props")
    only = sys.argv[2] if len(sys.argv) > 2 else None
    for sheet, grid, items in SHEETS[key]:
        if only and only not in sheet: continue
        path = os.path.join(src, sheet)
        if not os.path.exists(path): print("missing", sheet); continue
        tmp = tempfile.mkdtemp()  # V2 violet sheets on #00FF00 (the cutter reads the key colour from the corner)
        subprocess.check_call([sys.executable, os.path.join(ROOT, "Tools", "cut-magenta-prop-sheet.py"), path] + [n for n, _ in items] + ["--out", tmp, "--grid", grid], stdout=subprocess.DEVNULL)
        for name, wt in items:
            im = Image.open(os.path.join(tmp, name + ".png")).convert("RGBA")
            a = np.asarray(im)[..., 3]
            cols = np.nonzero(a.max(0) > 40)[0]; rws = np.nonzero(a.max(1) > 40)[0]
            drawn = cols[-1] - cols[0] + 1 if wt > 0 else rws[-1] - rws[0] + 1   # negative = a wanted HEIGHT (walls / pillars: the cave must frame the halls)
            k = abs(wt) * 64.0 / drawn
            im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
            ramp = next((v for p, v in SHADE.items() if name.startswith(p)), None)
            if ramp:
                a4 = np.asarray(im).astype(np.float32)
                g = np.linspace(ramp[0], ramp[1], a4.shape[0])[:, None, None]
                a4[..., :3] *= g
                im = Image.fromarray(np.clip(a4, 0, 255).astype(np.uint8), "RGBA")
            W = int(np.ceil(im.width / 16.0) * 16); H = int(np.ceil(im.height / 16.0) * 16)
            canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            canvas.alpha_composite(im, ((W - im.width) // 2, H - im.height))
            canvas.save(os.path.join(out, name + ".png"), optimize=True)
            print('{ "%s", new PropDef(%gf, %gf, ...) },' % (name, W / 64.0, H / 64.0))


if __name__ == "__main__":
    main()

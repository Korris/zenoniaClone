"""Cuts the two ChatGPT prop sheets of a dark 2.5D dungeon (first: Rootburrow, 21/11/2026) and sizes every prop to its PropDef width.

    python Tools/cut-map-25d-dungeon-props.py rootburrow

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
    ],
}


def main():
    key = sys.argv[1] if len(sys.argv) > 1 else "rootburrow"
    src = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", key, "dark-25d")
    out = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props")
    for sheet, grid, items in SHEETS[key]:
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
            W = int(np.ceil(im.width / 16.0) * 16); H = int(np.ceil(im.height / 16.0) * 16)
            canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            canvas.alpha_composite(im, ((W - im.width) // 2, H - im.height))
            canvas.save(os.path.join(out, name + ".png"), optimize=True)
            print('{ "%s", new PropDef(%gf, %gf, ...) },' % (name, W / 64.0, H / 64.0))


if __name__ == "__main__":
    main()

"""Cuts the 45 old-section Willowbrook props (wb*) out of the 4 ChatGPT redraw sheets and updates their sprite size in VillagePropDefs.cs.

Usage: python Tools/cut-willowbrook-old-props-from-redraw-sheets.py [--check] [group ...]   (groups: oaks-a oaks-b structures rocks; default every group with a sheet)
Input: Assets/ArtLibrary/maps/willowbrook-meadows/redraw/old-section-props-sheets/sheet-<group>-chatgpt-1.png (redrawn, 1536x1024 on #FF00FF) and layout.json
 from Tools/build-willowbrook-old-props-redraw-sheets.py. ChatGPT keeps the number of objects per shelf row and their left-to-right order, but NOT their exact
 position or size (13/11: oaks moved, clumps came back as single trees), so objects are matched by order: shelf rows of the reference sheet (slot y) against
 rows of alpha blobs on the redrawn sheet (split where the centre y jumps), then left to right. Each object is scaled to the old prop's opaque height and cut
 like the wbx* props (Tools/cut-willowbrook-extra-props.py cut(): centred on the footprint, 16 px under the feet), so the trunk stands on the footprint tile.
 Keying, rim despill and the dark-magenta contact shadow turned brown are that tool's clean(). Footprints (FootW, FootH) stay as they are; W and H are rewritten.
Output: Assets/Resources/Sprites/village/props/wb*.png + VillagePropDefs.cs (--check: only the comparison sheet in the plan's visuals folder).
Replaces Tools/grade-willowbrook-old-section-props-to-redraw.py for the props it cuts; do not run that one afterwards.
"""
import importlib.util, json, os, re, sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
spec = importlib.util.spec_from_file_location("wbx", os.path.join(HERE, "cut-willowbrook-extra-props.py"))
wbx = importlib.util.module_from_spec(spec); spec.loader.exec_module(wbx)
SHEETS = os.path.join(ROOT, "Assets/ArtLibrary/maps/willowbrook-meadows/redraw/old-section-props-sheets")
OLD = os.path.join(ROOT, "Assets/ArtLibrary/maps/willowbrook-meadows/redraw/old-section-props-src")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/village/props")
DEFS = os.path.join(ROOT, "Assets/Scripts/Maps/VillagePropDefs.cs")
VIS = os.path.join(ROOT, "plans/261113-1010-world-map-new-maps/visuals")
DEF_RE = r'\{ "%s", new PropDef\(([\d.]+)f, ([\d.]+)f, (\d+), (\d+)\) \}'


def ref_rows(layout, group):
    """Reference slots of a group as shelf rows (same slot top), each sorted left to right."""
    rows = {}
    for name, e in layout.items():
        if e["sheet"] == group: rows.setdefault(e["slot"][1], []).append((e["slot"][0], name))
    return [[n for _, n in sorted(r)] for _, r in sorted(rows.items())]


def blob_rows(rgba, counts):
    a = rgba[..., 3] > 0.55
    lab, n = ndi.label(ndi.binary_dilation(a, iterations=4))
    objs = []
    for i in range(1, n + 1):
        ys, xs = np.where((lab == i) & a)
        if len(xs) < 300: continue
        objs.append((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    objs.sort(key=lambda b: (b[1] + b[3]) / 2)
    rows, cur = [], []
    for b in objs:   # a new row starts where the next centre is below the current row's bottom
        if cur and (b[1] + b[3]) / 2 > max(c[3] for c in cur): rows.append(cur); cur = []
        cur.append(b)
    if cur: rows.append(cur)
    got = [len(r) for r in rows]
    if got != counts: raise SystemExit("objects per row %s, reference has %s: check the sheet by eye" % (got, counts))
    return [sorted(r, key=lambda b: b[0]) for r in rows]


def cut_group(group, layout, defs):
    path = os.path.join(SHEETS, "sheet-%s-chatgpt-1.png" % group)
    if not os.path.exists(path): return []
    wbx.SHEET = path
    rgba = wbx.clean(wbx.mm.key_magenta(path))
    refs = ref_rows(layout, group)
    out = []
    for names, boxes in zip(refs, blob_rows(rgba, [len(r) for r in refs])):
        for name, box in zip(names, boxes):
            ob = layout[name]["bbox"]
            fw = int(re.search(DEF_RE % name, defs).group(3))
            # wbOakTop*: the old prop was a canopy cut flat by the map's top edge; the redraw is a whole tree, matched by width (its crown runs off the map top)
            scale = (ob[2] - ob[0]) / float(box[2] - box[0]) if name.startswith("wbOakTop") else (ob[3] - ob[1]) / float(box[3] - box[1])
            out.append((name, wbx.cut(rgba, box, scale, fw)))
    return out


def main():
    with open(os.path.join(SHEETS, "layout.json")) as f: layout = json.load(f)
    defs = open(DEFS, encoding="utf-8").read()
    groups = [g for g in sys.argv[1:] if not g.startswith("--")] or ["oaks-a", "oaks-b", "structures", "rocks"]
    sprites = []
    for g in groups: sprites += cut_group(g, layout, defs)
    if "--check" not in sys.argv:
        for name, img in sprites:
            img.save(os.path.join(OUT, name + ".png"), optimize=True)
            defs = re.sub(DEF_RE % name, lambda m: '{ "%s", new PropDef(%sf, %sf, %s, %s) }' % (name, img.width / 64.0, img.height / 64.0, m.group(3), m.group(4)), defs)
        open(DEFS, "w", encoding="utf-8").write(defs)
    os.makedirs(VIS, exist_ok=True)
    tiles = []   # new (left) and old (right) on grass, bottoms aligned
    for name, img in sprites:
        old = Image.open(os.path.join(OLD, name + ".png")).convert("RGBA")
        h = max(img.height, old.height)
        t = Image.new("RGBA", (img.width + old.width + 8, h), (96, 128, 60, 255))
        t.alpha_composite(img, (0, h - img.height)); t.alpha_composite(old, (img.width + 8, h - old.height)); tiles.append(t)
    if tiles:
        W = 2400; x = y = rh = 0; pos = []
        for t in tiles:
            if x + t.width > W: x, y, rh = 0, y + rh + 12, 0
            pos.append((x, y)); x += t.width + 16; rh = max(rh, t.height)
        sheet = Image.new("RGB", (W, y + rh), (30, 30, 30))
        for t, p in zip(tiles, pos): sheet.paste(t, p, t)
        sheet.save(os.path.join(VIS, "wb-old-props-redraw-compare-%s.png" % "-".join(groups)))
    for name, img in sprites: print(name, img.size)


if __name__ == "__main__":
    main()

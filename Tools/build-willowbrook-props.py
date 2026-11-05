"""Cuts Willowbrook Meadows into a ground-only backdrop + separate y-sorted prop sprites (like Oakhaven Village).

Usage (Pillow, numpy, scipy, opencv-python):
  python Tools/build-willowbrook-props.py build     # backdrop + prop PNGs + C# snippets + composite/diff/sheet
  python Tools/build-willowbrook-props.py check     # footprint / collision overlay over the composite (reads rows from WillowbrookMeadowsMap.cs)

Inputs (1536x1024): ORIGINAL (with objects) and GROUND (objects removed, regenerated so every pixel differs a bit).
The ground image becomes the 2304x1536 backdrop (x1.5). Each prop is cut out of the ORIGINAL inside a hand-chosen box (ORIG px), its alpha
comes from a mask recipe per MODE, and the sprite is padded so the footprint centre / bottom edge sit exactly where MapScenery.BuildProp
anchors it (pivot bottom-centre of the footprint, 4 px per unit => 64 px per tile). Placing every prop at its (tx, ty) then reproduces the painting.
"""
import math
import os
import re
import sys
import importlib.util
import numpy as np
import cv2
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
ART = os.path.join(ROOT, "Assets/ArtLibrary/hd/map/willowbrook-meadows")
ORIG = os.path.join(ART, "willowbrook-meadows-chatgpt.png")
GROUND = os.path.join(ART, "willowbrook-meadows-ground-chatgpt.png")
BACKDROP = os.path.join(ROOT, "Assets/Resources/Sprites/village/tiles/willowbrook-meadows-ground.png")
PROP_DIR = os.path.join(ROOT, "Assets/Resources/Sprites/village/props")
VIS = os.path.join(ROOT, "plans/261105-1006-thatch-village-willowbrook-maps/visuals")
SCRATCH = os.path.join(ROOT, "plans/261105-1006-thatch-village-willowbrook-maps/visuals/props-debug")
MAP_CS = os.path.join(ROOT, "Assets/Scripts/Maps/WillowbrookMeadowsMap.cs")
S = 1.5   # art scale: 1536 -> 2304
T = 64    # px per tile in the backdrop

# name, mode, box (x0,y0,x1,y1 in ORIG px), fx (footprint centre x in ORIG px), fw, fh (tiles), options
# modes: tree (dark canopy density), rock (colour difference), struct (wood / straw: colour difference, thin parts kept)
# options: ty = force footprint bottom row, by = clip content below this ORIG y, th = tree density threshold
PROPS = [
    # --- north-west
    ("wbOakNW",        "tree",   (0, 0, 205, 315),       60, 1, 1, {}),
    ("wbCliffNW",      "rock",   (140, 0, 355, 100),    240, 2, 1, {}),
    ("wbRockNW",       "rock",   (322, 52, 425, 102),   372, 2, 1, {}),
    ("wbRockNW2",      "rock",   (160, 92, 258, 152),   215, 2, 1, {}),
    ("wbOakTop2",      "tree",   (338, 0, 520, 118),    470, 1, 1, {}),
    # --- north centre clump
    ("wbOakClumpW",    "tree",   (420, 0, 680, 218),    585, 1, 1, {}),
    ("wbOakBig",       "tree",   (600, 22, 842, 278),   700, 1, 1, {}),
    ("wbOakTopMid",    "tree",   (728, 0, 885, 88),     790, 1, 1, {}),
    ("wbRockTop",      "rock",   (522, 170, 642, 234),  580, 2, 1, {}),
    ("wbCliffStreamN", "rock",   (812, 58, 932, 158),   870, 2, 1, {}),
    ("wbCliffMid",     "rock",   (738, 195, 908, 372),  820, 3, 1, {}),
    ("wbRocksStreamE", "rock",   (928, 120, 1118, 202), 1030, 3, 1, {}),
    # --- north-east
    ("wbOakTopRight",  "tree",   (940, 0, 1212, 142),   1065, 1, 1, {}),
    ("wbOakTopFar",    "tree",   (1188, 0, 1312, 72),   1240, 1, 1, {}),
    ("wbRockTopEdge",  "rock",   (1298, 0, 1478, 50),   1380, 3, 1, {}),
    ("wbRockNE",       "rock",   (1098, 70, 1182, 120), 1140, 1, 1, {}),
    ("wbOakEastA",     "tree",   (1372, 0, 1536, 248),  1491, 1, 1, {}),
    ("wbOakEastB",     "tree",   (1378, 214, 1536, 412), 1517, 1, 1, {}),
    ("wbRockEast",     "rock",   (1393, 338, 1492, 402), 1440, 2, 1, {}),
    ("wbOakEastMid",   "tree",   (1372, 474, 1536, 688), 1494, 1, 1, {}),
    # --- west and centre
    ("wbOakWest",      "tree",   (52, 278, 258, 432),   135, 1, 1, {}),
    ("wbRockWest",     "rock",   (383, 368, 508, 432),  450, 2, 1, {}),
    ("wbSignpost",     "struct", (594, 360, 668, 442),  627, 1, 1, {}),
    ("wbOakMid",       "tree",   (688, 303, 858, 452),  757, 1, 1, {}),
    ("wbRockMid",      "rock",   (828, 386, 928, 442),  870, 2, 1, {}),
    ("wbRocksStreamS", "rock",   (948, 518, 1158, 638), 1050, 3, 1, {}),
    ("wbRockStream1",  "rock",   (1188, 462, 1272, 532), 1228, 1, 1, {}),
    ("wbRockStream2",  "rock",   (1272, 522, 1392, 602), 1330, 2, 1, {}),
    ("wbRockStream3",  "rock",   (1250, 688, 1358, 764), 1305, 2, 1, {}),
    # --- palisade and gate (collision comes from the rows: the road under the gate must stay open)
    ("wbPalisade",     "struct", (0, 300, 138, 620),     60, 1, 0, {}),
    # --- south-west clump and rocks
    ("wbOakSWClump",   "tree",   (36, 532, 335, 835),   190, 3, 1, {}),
    ("wbOakSWTree",    "tree",   (300, 560, 495, 745),  385, 1, 1, {}),
    ("wbOakSWLow",     "tree",   (0, 640, 172, 935),     45, 1, 1, {}),
    ("wbRockSW",       "rock",   (340, 766, 462, 832),  400, 2, 1, {}),
    ("wbRockCliffSW",  "rock",   (44, 912, 258, 1024),  150, 3, 1, {}),
    # --- south
    ("wbOakSouth",     "tree",   (412, 780, 662, 968),  520, 1, 1, {}),
    ("wbFenceW",       "struct", (244, 878, 470, 968),  350, 4, 1, {}),
    ("wbFenceE",       "struct", (578, 878, 757, 972),  668, 3, 1, {}),
    ("wbRockLog",      "rock",   (738, 906, 806, 958),  775, 1, 1, {}),
    ("wbRockSouth",    "rock",   (648, 958, 722, 998),  685, 1, 1, {}),
    ("wbLog",          "struct", (792, 892, 996, 978),  890, 3, 1, {}),
    ("wbFenceHay",     "struct", (818, 818, 968, 898),  890, 2, 1, {}),
    ("wbHaystack",     "struct", (952, 808, 1094, 928), 1022, 2, 1, {}),
    ("wbShed",         "struct", (1028, 652, 1325, 925), 1190, 4, 2, {}),
    ("wbOakSE",        "tree",   (1284, 800, 1536, 1024), 1420, 2, 1, {}),
]


def load():
    o = np.array(Image.open(ORIG).convert("RGB")).astype(np.float32)
    g = np.array(Image.open(GROUND).convert("RGB")).astype(np.float32)
    return o, g


def feature_maps(o, g):
    ob2, gb2 = cv2.GaussianBlur(o, (0, 0), 1.5), cv2.GaussianBlur(g, (0, 0), 1.5)
    lo2, lg2 = ob2.mean(2), gb2.mean(2)
    k = np.clip(lo2[..., None] / (lg2[..., None] + 1), 0.3, 1.2)
    dn = np.abs(ob2 - gb2 * k).sum(2)        # colour difference after removing a plain darkening (= tree shadows on grass)
    dr = np.abs(ob2 - gb2).sum(2)
    lo1, lg1 = cv2.GaussianBlur(o, (0, 0), 1).mean(2), cv2.GaussianBlur(g, (0, 0), 1).mean(2)
    dark = ((lo1 < lg1 - 45) | (lo1 < 85)).astype(np.float32)
    dens = cv2.GaussianBlur(dark, (0, 0), 5)
    return dn, dr, dens


def ell(n):
    return cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (n, n))


def keep_components(m, min_frac=0.12):
    lab, n = ndi.label(m)
    if n == 0:
        return m
    sizes = ndi.sum(m, lab, range(1, n + 1))
    big = sizes.max()
    keep = [i + 1 for i, s in enumerate(sizes) if s >= max(60, big * min_frac)]
    return np.isin(lab, keep)


def make_mask(mode, box, feats, opts):
    dn, dr, dens = feats
    x0, y0, x1, y1 = box
    if mode == "tree":
        m = (dens > opts.get("th", 0.22)) | ((dn > 60) & (dr > 110))
        close, opn = 11, 7
    elif mode == "rock":
        m = (dn > 30) | (dr > 150)
        close, opn = 11, 5
    else:  # struct
        m = (dn > 30) | (dr > 95)
        close, opn = 5, 3
    clip = np.zeros_like(m)
    clip[y0:y1, x0:x1] = True
    if "by" in opts:
        clip[opts["by"]:, :] = False
    m = m & clip
    m = cv2.morphologyEx(m.astype(np.uint8), cv2.MORPH_CLOSE, ell(close)) > 0
    m = ndi.binary_fill_holes(m)
    m = cv2.morphologyEx(m.astype(np.uint8), cv2.MORPH_OPEN, ell(opn)) > 0
    m = keep_components(m & clip, 0.05 if mode == "rock" else 0.12)
    return m


def edge_fade(box, shape, w=16):
    """Ramp alpha to 0 at box sides that are not the image border (neighbouring props overlap there and cover the seam)."""
    x0, y0, x1, y1 = box
    H, W = shape
    f = np.ones(shape, np.float32)
    xs, ys = np.arange(W, dtype=np.float32), np.arange(H, dtype=np.float32)
    if x0 > 0: f *= np.clip((xs - x0) / w, 0, 1)[None, :]
    if x1 < W: f *= np.clip((x1 - 1 - xs) / w, 0, 1)[None, :]
    if y0 > 0: f *= np.clip((ys - y0) / w, 0, 1)[:, None]
    if y1 < H: f *= np.clip((y1 - 1 - ys) / w, 0, 1)[:, None]
    return f


def soft_alpha(m):
    """Erode 1 px (drops the ground halo), then feather ~1.3 px."""
    a = cv2.erode(m.astype(np.uint8), ell(3)).astype(np.float32)
    a = cv2.GaussianBlur(a, (0, 0), 1.3)
    return np.clip((a - 0.15) / 0.7, 0, 1)


def cut_prop(name, mode, box, fx, fw, fh, opts, o, feats):
    m = make_mask(mode, box, feats, opts)
    a = soft_alpha(m) * edge_fade(box, m.shape)
    ys, xs = np.where(a > 0.03)
    if len(xs) == 0:
        raise SystemExit("empty mask for " + name)
    bx0, bx1, by0, by1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    rgb = o[by0:by1, bx0:bx1].astype(np.uint8)
    al = (a[by0:by1, bx0:bx1] * 255).astype(np.uint8)
    w, h = int(round((bx1 - bx0) * S)), int(round((by1 - by0) * S))
    im = Image.fromarray(rgb).resize((w, h), Image.LANCZOS)
    alpha = Image.fromarray(al).resize((w, h), Image.LANCZOS)
    im.putalpha(alpha)
    X0, Ybot = int(round(bx0 * S)), int(round(by1 * S))
    tx = int(round(fx * S / T - fw / 2.0))
    ty = max((Ybot - 1) // T, opts.get("ty", 0))
    C = (tx + fw / 2.0) * T
    half = max(C - X0, X0 + w - C)
    half = int(math.ceil(half / 8.0) * 8)     # canvas width = 2*half, a multiple of 16 px (quarter tiles)
    cw = 2 * half
    pad_b = (ty + 1) * T - Ybot
    ch = h + pad_b
    ch = int(math.ceil(ch / 16.0) * 16)       # height in quarter tiles; extra goes on top
    canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    left = int(round(X0 - (C - half)))
    canvas.alpha_composite(im, (left, ch - pad_b - h))
    place_x = int(round(C - half))             # canvas left in backdrop px
    place_y = (ty + 1) * T - ch               # canvas top in backdrop px
    return dict(name=name, img=canvas, tx=tx, ty=ty, fw=fw, fh=fh, x=place_x, y=place_y, w=cw, h=ch)


def build_all():
    o, g = load()
    feats = feature_maps(o, g)
    os.makedirs(SCRATCH, exist_ok=True)
    os.makedirs(VIS, exist_ok=True)
    back = Image.open(GROUND).convert("RGB").resize((2304, 1536), Image.LANCZOS)
    back.save(BACKDROP)
    comp = back.convert("RGBA")
    sprites = []
    for p in PROPS:
        sp = cut_prop(*p, o, feats)
        sp["img"].save(os.path.join(PROP_DIR, sp["name"] + ".png"))
        sprites.append(sp)
    # y-sort like the game: bottom of the footprint row
    for sp in sorted(sprites, key=lambda s: ((s["ty"] + 1), s["tx"])):
        comp.alpha_composite(sp["img"], (sp["x"], sp["y"])) if sp["x"] >= 0 and sp["y"] >= 0 else paste_clipped(comp, sp)
    comp = comp.convert("RGB")
    comp.save(os.path.join(VIS, "willowbrook-props-composite.png"))
    # diff against the original painting
    orig15 = Image.open(ORIG).convert("RGB").resize((2304, 1536), Image.LANCZOS)
    diff = np.abs(np.array(comp).astype(np.int16) - np.array(orig15).astype(np.int16)).sum(2)
    Image.fromarray(np.clip(diff * 2, 0, 255).astype(np.uint8)).resize((1536, 1024)).save(os.path.join(SCRATCH, "diff.png"))
    side = Image.new("RGB", (3072, 1024))
    side.paste(orig15.resize((1536, 1024), Image.LANCZOS), (0, 0))
    side.paste(comp.resize((1536, 1024), Image.LANCZOS), (1536, 0))
    side.save(os.path.join(SCRATCH, "side-by-side.png"))
    # sprite sheets on magenta and dark
    for tag, col in (("magenta", (255, 0, 255)), ("dark", (20, 20, 28))):
        make_sheet(sprites, col).save(os.path.join(SCRATCH, "sheet-%s.png" % tag))
    # C# snippets
    defs, plist = [], []
    for sp in sprites:
        defs.append('{ "%s", new PropDef(%sf, %sf, %d, %d) },' % (sp["name"], fmt(sp["w"] / T), fmt(sp["h"] / T), sp["fw"], sp["fh"]))
        plist.append('new MapProp("%s", %d, %d),' % (sp["name"], sp["tx"], sp["ty"]))
    with open(os.path.join(SCRATCH, "snippets.txt"), "w") as f:
        f.write("// VillagePropDefs\n" + "\n".join(defs) + "\n\n// PropList\n" + "\n".join(plist) + "\n")
    print("props:", len(sprites))
    for sp in sprites:
        print("%-16s %4dx%-4d tile(%2d,%2d) foot %dx%d" % (sp["name"], sp["w"], sp["h"], sp["tx"], sp["ty"], sp["fw"], sp["fh"]))


def paste_clipped(comp, sp):
    layer = Image.new("RGBA", comp.size, (0, 0, 0, 0))
    layer.paste(sp["img"], (sp["x"], sp["y"]))
    comp.alpha_composite(layer)


def fmt(v):
    return ("%.4f" % v).rstrip("0").rstrip(".")


def make_sheet(sprites, bg, cols=6, cell=300):
    rows = (len(sprites) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell, rows * cell), bg)
    d = ImageDraw.Draw(sheet)
    for i, sp in enumerate(sprites):
        im = sp["img"].copy()
        sc = min((cell - 8) / im.width, (cell - 24) / im.height, 1.0)
        if sc < 1:
            im = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.LANCZOS)
        cx, cy = (i % cols) * cell, (i // cols) * cell
        sheet.paste(im, (cx + 4, cy + 20), im)
        d.text((cx + 4, cy + 4), sp["name"], fill=(255, 255, 0))
    return sheet


def parse_rows():
    src = open(MAP_CS, encoding="utf-8").read()
    body = src[src.index("Rows ="):src.index("};", src.index("Rows ="))]
    rows = re.findall(r'"([TgdpwB]{36})"', body)
    if len(rows) != 24:
        raise SystemExit("could not parse 24 rows from " + MAP_CS)
    return rows


def parse_props():
    src = open(MAP_CS, encoding="utf-8").read()
    return [(m.group(1), int(m.group(2)), int(m.group(3))) for m in re.finditer(r'new MapProp\("(\w+)", (\d+), (\d+)\)', src)]


def prop_def_table():
    src = open(os.path.join(ROOT, "Assets/Scripts/Maps/VillagePropDefs.cs"), encoding="utf-8").read()
    return {m.group(1): (int(m.group(2)), int(m.group(3))) for m in re.finditer(r'\{ "(\w+)", new PropDef\([\d.]+f, [\d.]+f, (\d+), (\d+)\) \}', src)}


def cmd_check():
    spec = importlib.util.spec_from_file_location("bmb", os.path.join(ROOT, "Tools/build-map-backdrop.py"))
    bmb = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(bmb)
    rows = parse_rows()
    defs = prop_def_table()
    props = parse_props()
    solid = [[rows[r][c] in "Tw" for c in range(36)] for r in range(24)]
    foot = []
    for name, tx, ty in props:
        fw, fh = defs[name]
        foot.append((tx, ty - fh + 1, fw, fh))
        for y in range(ty - fh + 1, ty + 1):
            for x in range(tx, tx + fw):
                if 0 <= x < 36 and 0 <= y < 24:
                    solid[y][x] = True
    eff = ["".join("T" if solid[r][c] and rows[r][c] not in "wB" else rows[r][c] for c in range(36)) for r in range(24)]
    seen = bmb.flood(eff, [(3, 11)])
    lost = [(c, r) for r in range(24) for c in range(36) if eff[r][c] not in "Tw" and (c, r) not in seen]
    comp = Image.open(os.path.join(VIS, "willowbrook-props-composite.png")).convert("RGBA")
    ov = Image.new("RGBA", comp.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(ov)
    for r in range(24):
        for c in range(36):
            col = {"T": (255, 0, 0, 90), "w": (0, 80, 255, 90), "B": (0, 220, 0, 80)}.get(rows[r][c])
            if col:
                d.rectangle([c * T, r * T, (c + 1) * T, (r + 1) * T], fill=col)
    for (tx, ty, fw, fh) in [f for f in foot if f[3] > 0]:
        d.rectangle([tx * T, ty * T, (tx + fw) * T - 1, (ty + fh) * T - 1], fill=(255, 140, 0, 110), outline=(255, 255, 0, 255), width=3)
    for (c, r) in lost:
        d.ellipse([c * T + 14, r * T + 14, (c + 1) * T - 14, (r + 1) * T - 14], fill=(255, 0, 255, 220))
    for (c, r) in ((0, 11), (0, 12)):
        d.rectangle([c * T, r * T, (c + 1) * T, (r + 1) * T], outline=(0, 255, 255, 255), width=4)
    d.rectangle([3 * T, 11 * T, 4 * T, 12 * T], outline=(255, 255, 255, 255), width=4)
    out = Image.alpha_composite(comp, ov)
    bmb.draw_grid(out, labels=False)
    out.convert("RGB").resize((1536, 1024), Image.LANCZOS).save(os.path.join(VIS, "willowbrook-props-check.png"))
    print("unreachable walkable tiles (%d): %s" % (len(lost), lost))
    print("reachable walkable tiles:", len(seen))


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "build":
        build_all()
    elif cmd == "check":
        cmd_check()
    else:
        sys.exit(__doc__)

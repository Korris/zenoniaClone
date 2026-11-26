"""Cuts the ChatGPT skill-target indicator sheet (docs/reference/skill-targeting/, flat #FF00FF) into centred transparent PNGs.

    python Tools/cut-skill-target-indicator-sheet.py <sheet.png> [--out Assets/Resources/Sprites/fx/skill-target]
        [--json Assets/Resources/Data/skill-target-indicator.json] [--preview docs/reference/skill-targeting/cut-preview.png]

- Pieces = groups of opaque blobs (blobs closer than GROUP_PX join, so the dashed leap path and the sparkles inside a ring stay one piece),
  read row by row (top to bottom, then left to right) and named by NAMES (the order of the sheet ChatGPT drew, 26/11/2026).
- Alpha is UNMIXED from the magenta key (C = a * F + (1 - a) * M): the soft glow inside the rings becomes a light, low-alpha colour instead of a pink cast.
- Each PNG is a square centred on the piece's bbox centre (the ring centre), size rounded up to 8 px.
- Rings: the JSON stores `bandPx` = radius (px, in the PNG) of the ring's outer band, measured as the median over 360 angles of the farthest opaque
  pixel, so the star spikes poking out at 4 points do not count. Unity scales a ring so this band lands exactly on the skill's real AoE radius.
"""
import json, os, sys
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
NAMES = ["area-ring", "placement-ring", "invalid-ring", "range-ring", "leap-ring", "turret-ring", "reticle",
         "rune-flower", "rune-leaf", "rune-star", "rune-wind", "rune-diamond", "rune-sprout", "rune-sparkle", "rune-lotus",
         "leap-path", "direction-arrow"]
RINGS = {"area-ring", "placement-ring", "invalid-ring", "range-ring", "leap-ring", "turret-ring"}
GROUP_PX = (30, 1)  # (wide, tall): the leap path dashes sit side by side
M = np.array([255.0, 0.0, 255.0])


def arg(args, flag, default):
    if flag in args:
        i = args.index(flag); v = args[i + 1]; del args[i:i + 2]; return v
    return default


def main():
    args = sys.argv[1:]
    out = arg(args, "--out", os.path.join(ROOT, "Assets", "Resources", "Sprites", "fx", "skill-target"))
    js = arg(args, "--json", os.path.join(ROOT, "Assets", "Resources", "Data", "skill-target-indicator.json"))
    prev = arg(args, "--preview", None)
    im = np.asarray(Image.open(args[0]).convert("RGB")).astype(np.float32)
    R, G, B = im[..., 0], im[..., 1], im[..., 2]
    # key-ness: magenta = R and B high, G low. Every art colour here (teal, cream, coral) has G well above 0.
    mag = np.clip((np.minimum(R, B) - G - 40.0) / 175.0, 0, 1)
    alpha = 1.0 - mag
    alpha[alpha < 0.04] = 0.0
    a3 = np.maximum(alpha, 1e-3)[..., None]
    fg = np.clip((im - (1.0 - alpha)[..., None] * M) / a3, 0, 255)
    solid = (alpha > 0.35).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid)
    keep = np.zeros_like(solid)
    for i in range(1, n):
        if st[i, 4] >= 25: x, y, w, h = st[i, :4]; keep[y:y + h, x:x + w] = 1  # filled bbox: a ring swallows the marks drawn inside it
    grow = cv2.dilate(keep, np.ones((GROUP_PX[1], GROUP_PX[0]), np.uint8))
    gn, glab, gst, gcen = cv2.connectedComponentsWithStats(grow)
    groups = [i for i in range(1, gn) if gst[i, 4] > 800]
    if len(groups) != len(NAMES): [print(gst[i]) for i in groups]; raise SystemExit("found %d pieces, expected %d" % (len(groups), len(NAMES)))
    groups.sort(key=lambda i: gcen[i][1])
    rows, cur = [], [groups[0]]
    for i in groups[1:]:
        if abs(gcen[i][1] - gcen[cur[-1]][1]) < 90: cur.append(i)
        else: rows.append(cur); cur = [i]
    rows.append(cur)
    order = [i for r in rows for i in sorted(r, key=lambda j: gcen[j][0])]
    os.makedirs(out, exist_ok=True)
    meta = {"source": os.path.relpath(args[0], ROOT).replace("\\", "/"), "pieces": {}}
    tiles = []
    for name, g in zip(NAMES, order):
        mask = (glab == g)
        ys, xs = np.nonzero(mask & (alpha > 0))
        x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
        cx, cy = (x0 + x1) / 2.0, (y0 + y1) / 2.0
        half = int(np.ceil(max(x1 - x0, y1 - y0) / 2.0)) + 4
        size = int(np.ceil(2 * half / 8.0) * 8)
        rgba = np.zeros((size, size, 4), np.float32)
        ox, oy = int(round(cx - size / 2.0)), int(round(cy - size / 2.0))
        sx0, sy0 = max(0, ox), max(0, oy)
        sx1, sy1 = min(im.shape[1], ox + size), min(im.shape[0], oy + size)
        sub_a = np.where(mask[sy0:sy1, sx0:sx1], alpha[sy0:sy1, sx0:sx1], 0)
        rgba[sy0 - oy:sy1 - oy, sx0 - ox:sx1 - ox, :3] = fg[sy0:sy1, sx0:sx1]
        rgba[sy0 - oy:sy1 - oy, sx0 - ox:sx1 - ox, 3] = sub_a * 255.0
        png = Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), "RGBA")
        png.save(os.path.join(out, name + ".png"))
        entry = {"size": size, "w": int(x1 - x0 + 1), "h": int(y1 - y0 + 1)}  # w / h = opaque bbox (the leap path is stretched by its length)
        if name in RINGS:
            a = rgba[..., 3]
            c = size / 2.0
            far = []
            for k in range(360):
                t = np.radians(k + 0.5)
                rr = np.arange(0, c, 0.5)
                px = np.clip((c + np.cos(t) * rr).astype(int), 0, size - 1)
                py = np.clip((c + np.sin(t) * rr).astype(int), 0, size - 1)
                hit = np.nonzero(a[py, px] > 128)[0]
                if len(hit): far.append(rr[hit[-1]])
            entry["bandPx"] = round(float(np.median(far)), 1)
        meta["pieces"][name] = entry
        tiles.append(png)
        print("%-16s %4d px  %s" % (name, size, ("band %.1f px" % entry["bandPx"]) if "bandPx" in entry else ""))
    os.makedirs(os.path.dirname(js), exist_ok=True)
    with open(js, "w", encoding="utf-8") as f: json.dump(meta, f, indent=1)
    if prev:  # contact sheet on a dark and a light ground (check the unmixed glow reads on both)
        w = sum(t.width for t in tiles) // 2 + 40
        sheet = Image.new("RGBA", (w, 2 * 460), (0, 0, 0, 255))
        for band, col in ((0, (52, 47, 43, 255)), (1, (214, 222, 228, 255))):
            sheet.paste(Image.new("RGBA", (w, 460), col), (0, band * 460))
            x, y = 10, band * 460 + 10
            for t in tiles:
                s = t if t.width <= 220 else t.resize((220, 220), Image.LANCZOS)
                if x + s.width > w: x, y = 10, y + 230
                sheet.alpha_composite(s, (x, y)); x += s.width + 10
        sheet.convert("RGB").save(prev)


if __name__ == "__main__":
    main()

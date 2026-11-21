"""Cuts a ChatGPT prop sheet drawn on flat magenta (#FF00FF, or #00FF00: read from the top-left pixel) into one PNG per prop for MapScenery (first used for the Willowbrook 2.5D forest, 21/11/2026).

    python Tools/cut-magenta-prop-sheet.py <sheet.png> <name1> <name2> ... [--scale 1.0] [--out Assets/Resources/Sprites/village/props]

- Props are the large non-magenta blobs (with --grid RxC: everything inside one cell of an R x C layout is one prop), read row by row (top to bottom, then left to right), so names follow the order asked in the prompt.
- Magenta is keyed out with a soft edge and the pink fringe is despilled (the magenta share is removed from the edge colours).
- Each PNG is padded so the TRUNK of a *Tree* prop (middle of the lowest opaque rows; other props: their bbox middle) sits on the horizontal centre and the lowest opaque pixel on the bottom
  edge: MapScenery draws a prop with pivot (0.5, 0) on the bottom edge of its footprint, so the trunk lands on its tile (and a 2.5D billboard
  stands on its foot line). Size is rounded up to 16 px (a quarter tile).
- Prints the PropDef line of each prop (W, H in tiles = px / 64; footprint left for you to choose: trees 1x1, low walk-through decor FootH 0).
"""
import os, sys
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def main():
    args = [a for a in sys.argv[1:]]
    scale = float(args[args.index("--scale") + 1]) if "--scale" in args else 1.0
    out = args[args.index("--out") + 1] if "--out" in args else os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "props")
    grid = tuple(int(v) for v in args[args.index("--grid") + 1].split("x")) if "--grid" in args else None
    for flag in ("--scale", "--out", "--grid"):
        if flag in args: i = args.index(flag); del args[i:i + 2]
    sheet, names = args[0], args[1:]
    im = np.asarray(Image.open(sheet).convert("RGB")).astype(np.float32)
    R, G, B = im[..., 0], im[..., 1], im[..., 2]
    green = G[2, 2] > 200 and R[2, 2] < 80 and B[2, 2] < 80   # sheet drawn on #00FF00 (purple / pink art, 21/11/2026): key green instead
    # key-ness: magenta = high R and B, low G; green = G above both
    mag = np.clip(((G - np.maximum(R, B)) if green else (np.minimum(R, B) - G)) - 60, 0, None) / 90.0
    mag = np.clip(mag, 0, 1)
    alpha = 1 - mag
    solid = (alpha > 0.5).astype(np.uint8)
    solid = cv2.morphologyEx(solid, cv2.MORPH_CLOSE, np.ones((9, 9), np.uint8))
    n, lab, st, cen = cv2.connectedComponentsWithStats(solid)
    if grid:  # --grid RxC: the sheet is a rows x cols layout; every blob joins the cell of its centre (props made of separate pieces, e.g. a floating orb over its base)
        gr, gc = grid
        H0, W0 = solid.shape
        cell = np.zeros(n, int) - 1
        for i in range(1, n):
            if st[i, 4] > 150: cell[i] = min(gr - 1, int(cen[i][1] * gr / H0)) * gc + min(gc - 1, int(cen[i][0] * gc / W0))
        lab = np.where(lab > 0, cell[lab] + 1, 0).astype(np.int32)
        n = gr * gc + 1
        st = np.zeros((n, 5), int); cen = np.zeros((n, 2))
        for i in range(1, n):
            ys, xs = np.nonzero(lab == i)
            if len(xs): st[i] = [xs.min(), ys.min(), xs.max() - xs.min() + 1, ys.max() - ys.min() + 1, len(xs)]; cen[i] = [xs.mean(), ys.mean()]
    blobs = [i for i in range(1, n) if st[i, 4] > 1500]
    if len(blobs) != len(names): raise SystemExit("found %d props, %d names" % (len(blobs), len(names)))
    # rows: group by vertical centre
    blobs.sort(key=lambda i: cen[i][1])
    rows, cur = [], [blobs[0]]
    for i in blobs[1:]:
        if abs(cen[i][1] - cen[cur[-1]][1]) < 120: cur.append(i)
        else: rows.append(cur); cur = [i]
    rows.append(cur)
    order = [i for r in rows for i in sorted(r, key=lambda j: cen[j][0])]
    # despill: remove the magenta share from the colours near the edge
    if green:
        spill = np.clip(G - np.maximum(R, B), 0, None) * (1 - alpha)
        rgb = np.clip(np.stack([R, G - spill, B], -1), 0, 255)
    else:
        spill = np.clip(np.minimum(R, B) - G, 0, None) * (1 - alpha)
        rgb = np.clip(np.stack([R - spill, G, B - spill], -1), 0, 255)
    os.makedirs(out, exist_ok=True)
    for name, i in zip(names, order):
        x, y, w, h = st[i, :4]
        m = (lab[y:y + h, x:x + w] == i)
        m = cv2.dilate(m.astype(np.uint8), np.ones((5, 5), np.uint8)) > 0
        a = alpha[y:y + h, x:x + w] * m
        a[a < 0.06] = 0
        crop = np.dstack([rgb[y:y + h, x:x + w], a * 255]).astype(np.uint8)
        pim = Image.fromarray(crop, "RGBA")
        if scale != 1.0: pim = pim.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.LANCZOS)
        A = np.asarray(pim)[..., 3]
        ys = np.nonzero(A.max(1) > 40)[0]; bottom = ys[-1]
        foot = np.nonzero(A[max(0, bottom - max(4, A.shape[0] // 16)):bottom + 1].max(0) > 40)[0]
        cols = np.nonzero(A.max(0) > 40)[0]
        cx = (foot[0] + foot[-1]) / 2.0 if "tree" in name.lower() else (cols[0] + cols[-1]) / 2.0  # trees: on the trunk; rocks, logs, bushes: on their middle
        half = int(np.ceil(max(cx, pim.width - cx)))
        W = int(np.ceil(2 * half / 16.0) * 16); H = int(np.ceil((bottom + 1) / 16.0) * 16)
        canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        canvas.alpha_composite(pim.crop((0, 0, pim.width, bottom + 1)), (int(round(W / 2.0 - cx)), H - bottom - 1))
        canvas.save(os.path.join(out, name + ".png"), optimize=True)
        print('{ "%s", new PropDef(%sf, %sf, 1, 1) },' % (name, ("%g" % (W / 64.0)), ("%g" % (H / 64.0))))


if __name__ == "__main__":
    main()

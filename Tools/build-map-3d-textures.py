"""Runtime textures of the 3D map prototype (Map3D, 22/11/2026) from the ChatGPT paintings in Assets/ArtLibrary/maps/<map>/map3d/.

    python Tools/build-map-3d-textures.py [willowbrook-meadows] [--sheet]

Leaf / needle atlases (map3d-leaf-<kind>-atlas-chatgpt-N.png, 4x4 clusters on #FF00FF, the grid is never exact): the magenta is keyed to alpha
(soft edge + despill), every cluster is found as a blob, sorted by row / column and packed into an exact 4x4 1024 px RGBA atlas (12 px padding,
pine sprays left-aligned so the spray's base sits on the cell's left edge = the branch, grass tufts bottom-aligned on the cell's lower edge), transparent texels get the colour of the nearest leaf
(edge bleed, no pink fringe in the mips).
Seamless swatches (grass, dirt, rock, bark, paving...): the swatch blended with its x / y / xy half-rolls, each weighted to 0 on its own seam
(the 4-roll trick of build-map-25d-snow-ground.py), resized to 1024 (bark strips keep their aspect).
Outputs go to Assets/Resources/Map3D/ (PNG, loaded with Resources.Load; the build imports them with default settings: mipmaps, repeat wrap).
--sheet writes a contact sheet of every output to plans/261122-1100-map-3d-proto/textures-sheet.jpg.
"""
import os, re, sys, glob
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Map3D")
CELL, PAD = 256, 12


def src_dir(key): return os.path.join(ROOT, "Assets", "ArtLibrary", "maps", key, "map3d")


def latest(folder, stem):
    files = glob.glob(os.path.join(folder, stem + "-chatgpt-*.png"))
    if not files: return None
    return max(files, key=lambda p: int(re.findall(r"-(\d+)\.png$", p)[0]))


def key_magenta(rgb):
    """Alpha from the distance to #FF00FF (soft over ~40 levels), magenta spill pulled out of the edge pixels."""
    f = rgb.astype(np.float32)
    r, g, b = f[..., 0], f[..., 1], f[..., 2]
    spill = np.minimum(r, b) - g                       # > 0 where pink
    a = np.clip((120 - spill) / 60.0, 0, 1)            # pure magenta spill ~255 -> 0, leaves (g >> r,b) -> 1
    k = np.clip(b - 0.55 * g, 0, None)                # magenta mixed into an edge adds red and blue alike; leaves have little blue
    out = f.copy()
    out[..., 0] -= k; out[..., 2] -= k
    return np.clip(out, 0, 255), a


def bleed(rgb, a, iters=24):
    """Fill fully transparent texels with the mean colour of their opaque neighbours, growing outward (no fringe in mips)."""
    rgb = rgb.copy(); filled = a > 0.5
    k = np.ones((3, 3), np.float32)
    for _ in range(iters):
        if filled.all(): break
        m = filled.astype(np.float32)
        s = cv2.filter2D(rgb * m[..., None], -1, k, borderType=cv2.BORDER_CONSTANT)
        c = cv2.filter2D(m, -1, k, borderType=cv2.BORDER_CONSTANT)
        grow = (~filled) & (c > 0)
        rgb[grow] = s[grow] / c[grow][..., None]
        filled |= grow
    return rgb


def pack_atlas(path, out_name, align="center"):
    rgb, a = key_magenta(np.asarray(Image.open(path).convert("RGB")))
    mask = (a > 0.3).astype(np.uint8)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((9, 9), np.uint8))
    n, lab, stats, cent = cv2.connectedComponentsWithStats(mask, 8)
    blobs = [i for i in range(1, n) if stats[i, cv2.CC_STAT_AREA] > mask.size * 0.004]
    H = mask.shape[0]
    blobs.sort(key=lambda i: (int(cent[i][1] / (H / 4)), cent[i][0]))
    if len(blobs) < 16: print("  warning: %d clusters in %s (want 16), cells repeat" % (len(blobs), os.path.basename(path)))
    atlas_rgb = np.zeros((CELL * 4, CELL * 4, 3), np.float32); atlas_a = np.zeros((CELL * 4, CELL * 4), np.float32)
    for k in range(16):
        i = blobs[k % len(blobs)]
        x, y, w, h = stats[i, 0], stats[i, 1], stats[i, 2], stats[i, 3]
        sub_a = a[y:y + h, x:x + w] * (lab[y:y + h, x:x + w] == i).astype(np.float32)
        sub_a = np.maximum(sub_a, a[y:y + h, x:x + w] * (cv2.dilate((lab[y:y + h, x:x + w] == i).astype(np.uint8), np.ones((5, 5), np.uint8)) > 0))
        sub_rgb = rgb[y:y + h, x:x + w]
        s = (CELL - 2 * PAD) / max(w, h)
        nw, nh = max(1, int(w * s)), max(1, int(h * s))
        rr = cv2.resize(sub_rgb, (nw, nh), interpolation=cv2.INTER_AREA); aa = cv2.resize(sub_a, (nw, nh), interpolation=cv2.INTER_AREA)
        cx, cy = (k % 4) * CELL, (k // 4) * CELL
        ox = cx + (PAD if align == "left" else (CELL - nw) // 2); oy = cy + ((CELL - nh) // 2 if align != "bottom" else CELL - nh - 2)
        atlas_rgb[oy:oy + nh, ox:ox + nw] = rr; atlas_a[oy:oy + nh, ox:ox + nw] = aa
    atlas_rgb = bleed(atlas_rgb, atlas_a)
    img = np.dstack([np.clip(atlas_rgb, 0, 255), np.clip(atlas_a * 255, 0, 255)]).astype(np.uint8)
    Image.fromarray(img, "RGBA").save(os.path.join(OUT, out_name))
    print("  atlas", out_name, "from", os.path.basename(path), "(%d clusters)" % len(blobs))
    return img


def seamless(tex):
    h, w = tex.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    fx = np.minimum(xx, w - 1 - xx) / (w * 0.5); fy = np.minimum(yy, h - 1 - yy) / (h * 0.5)  # 0 on the image edge, 1 in the middle
    gx, gy = 1 - fx, 1 - fy                                                                   # 0 in the middle (seam of the rolled copy)
    copies = [(tex, fx * fy), (np.roll(tex, w // 2, 1), gx * fy), (np.roll(tex, h // 2, 0), fx * gy), (np.roll(np.roll(tex, h // 2, 0), w // 2, 1), gx * gy)]
    acc = np.zeros_like(tex, np.float32); wsum = np.zeros((h, w), np.float32)
    for t, wt in copies:
        wt = np.clip(wt, 1e-4, None) ** 1.5
        acc += t.astype(np.float32) * wt[..., None]; wsum += wt
    return acc / wsum[..., None]


def swatch(path, out_name, size=1024, crop=None, grade=None):
    im = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    if crop: x, y, w, h = crop; im = im[y:y + h, x:x + w]
    s = min(im.shape[0], im.shape[1]); im = im[:s, :s]  # square
    im = seamless(im)
    if grade is not None:  # move the mean to a target colour, keep 90 % of the contrast
        m = im.reshape(-1, 3).mean(0); im = (im - m) * 0.9 + np.array(grade, np.float32)
    im = cv2.resize(np.clip(im, 0, 255), (size, size), interpolation=cv2.INTER_AREA)
    Image.fromarray(im.astype(np.uint8)).save(os.path.join(OUT, out_name))
    print("  swatch", out_name, "from", os.path.basename(path))
    return im.astype(np.uint8)


def main():
    key = next((a for a in sys.argv[1:] if not a.startswith("--")), "willowbrook-meadows")
    d = src_dir(key); os.makedirs(OUT, exist_ok=True)
    done = []
    for kind, align in (("oak", "center"), ("pine", "left"), ("willow", "center"), ("grass", "bottom")):
        p = latest(d, "map3d-leaf-%s-atlas" % kind)
        if p: done.append(pack_atlas(p, "leaf-%s.png" % kind, align))
    lib25 = os.path.join(ROOT, "Assets", "ArtLibrary", "maps", key, "forest-25d")
    for stem, out, grade in (("willowbrook-forest-25d-grass", "ground-grass.png", None), ("willowbrook-forest-25d-dirt", "ground-dirt.png", None)):
        p = latest(lib25, stem)
        if p: done.append(swatch(p, out, grade=grade))
    for stem, out in (("map3d-rock", "rock.png"), ("map3d-paving", "ground-paving.png"), ("map3d-ground-forest", "ground-forest.png")):
        p = latest(d, stem)
        if p: done.append(swatch(p, out))
    p = latest(d, "map3d-bark-atlas")
    if p:  # 4 vertical strips side by side: keep the atlas whole, make each strip tile vertically
        im = np.asarray(Image.open(p).convert("RGB")).astype(np.float32)
        im = cv2.resize(im, (1024, 1024), interpolation=cv2.INTER_AREA)
        h = im.shape[0]; yy = np.arange(h, dtype=np.float32)
        wy = (np.minimum(yy, h - 1 - yy) / (h * 0.5))[:, None, None]
        im = (im * wy + np.roll(im, h // 2, 0) * (1 - wy))
        Image.fromarray(np.clip(im, 0, 255).astype(np.uint8)).save(os.path.join(OUT, "bark.png")); done.append(im.astype(np.uint8))
        print("  bark atlas from", os.path.basename(p))
    if "--sheet" in sys.argv and done:
        tiles = []
        for t in done:
            t = t[..., :3] if t.shape[2] == 4 else t
            tiles.append(cv2.resize(t, (384, 384), interpolation=cv2.INTER_AREA))
        sheet = np.concatenate(tiles, 1)
        dst = os.path.join(ROOT, "plans", "261122-1100-map-3d-proto", "textures-sheet.jpg")
        Image.fromarray(sheet).save(dst, quality=88); print("sheet", dst)


if __name__ == "__main__":
    main()

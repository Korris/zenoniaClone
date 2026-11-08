"""Mechanic basic-attack VFX v2, cut from the ChatGPT sheet drawn after the approved board docs/reference/mechanic-basic-attack-board.png
(2026-11-08, designer "ok làm đi"). Source: Assets/ArtLibrary/classes/mechanic/vfx/attack-v2/mechanic-attack-fx-v2-chatgpt-1.png, 5 rows x 4
frames on flat #FF00FF (chat Tay https://chatgpt.com/c/6aeff3a3-e734-83ec-86d1-5b303f71ee07, ref board crop in ref/).
Rows of the sheet -> rows of VfxSprites set "mechanic-v2":
  0 muzzle flash  -> cast_N    (pivot = left end of the hot core: the muzzle; drawn pointing right, rotated to the aim)
  1 bullet        -> proj_N    (pivot = the head; blue parts recoloured to yellow-orange, the board's approved bullet is warm; loop of the 2 longest frames)
  2 big blue bolt -> bolt_N    (hit 3, pivot = the head; loop of frames 1-3)
  3 cannon blast  -> blast_N   (hit 4 explosion, centred)
  4 impact        -> impact_N  (centred)
Output: Assets/Resources/Sprites/classes/mechanic/fx/attack-v2/<row>_N.png (+ .meta cloned from fx/attack-orange) and
Assets/Resources/Data/classes/mechanic/vfx-attack-v2.json (w, h, pivot px from the bottom-left). One output px = VfxSprites.UnitsPerPx (0.058) units.
  python Tools/build-mechanic-attack-fx-v2.py cut       write frames, json, metas
  python Tools/build-mechanic-attack-fx-v2.py preview   Tools/out/mechanic-attack-fx-v2-preview.png (dark + light, next to the hero and the old set)
"""
import colorsys, json, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

os.chdir("D:/zenoniaClone")
sys.path.insert(0, "Tools")
from wizard_attack_fx_common import to_image, cells, resize_rgba, trim, clone_meta

SRC = "Assets/ArtLibrary/classes/mechanic/vfx/attack-v2/mechanic-attack-fx-v2-chatgpt-1.png"
OUT = "Assets/Resources/Sprites/classes/mechanic/fx/attack-v2"
OLD = "Assets/Resources/Sprites/classes/mechanic/fx/attack-orange"
DATA = "Assets/Resources/Data/classes/mechanic"
SHEET_ROWS = ["cast", "proj", "bolt", "blast", "impact"]
# which sheet frames make the 4 game frames of each row
PICK = {"cast": [0, 1, 2, 3], "proj": [1, 2, 2, 1], "bolt": [1, 2, 3, 2], "blast": [0, 1, 2, 3], "impact": [0, 1, 2, 3]}
# source px -> output px. Game sizes (units = px x scale x 0.058): muzzle flash ~16 long, bullet ~14, bolt ~18 (code scales it), blast ~18
# (code scales it to the blast radius), impact ~22.
ROW_SCALE = {"cast": 0.9, "proj": 1.0, "bolt": 1.0, "blast": 1.0, "impact": 1.3}
UNITS_PER_PX = 0.058


def key_magenta(path, min_blob=40):
    """Premultiplied float RGBA (0..255) from art on flat #FF00FF: alpha from the magenta excess min(R, B) - G, key spill pulled down,
    6 px border cleared, specks under min_blob px dropped."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    excess = np.minimum(r, b) - g
    alpha = 1 - np.clip((excess - 40) / 150, 0, 1)
    a = alpha[..., None]
    key = np.array([255.0, 0.0, 255.0])
    straight = np.clip(np.where(a > 0.02, (rgb - (1 - a) * key) / np.maximum(a, 0.02), 0), 0, 255)
    spill = (excess > 0) & (alpha < 0.98)
    for i in (0, 2):
        straight[..., i] = np.where(spill, np.minimum(straight[..., i], np.maximum(g, straight[..., 1]) + 40), straight[..., i])
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    lab, n = ndimage.label(alpha > 0.3, structure=np.ones((3, 3)))
    if n:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        small = np.isin(lab, 1 + np.flatnonzero(sizes < min_blob))
        alpha[small] = 0
    return np.dstack([straight * alpha[..., None], alpha * 255])


def warm_bullet(img):
    """Blue / cyan of the bullet frames -> yellow-orange (same value and saturation), white core kept."""
    a = np.asarray(img).astype(np.float64) / 255
    out = a.copy()
    h, w = a.shape[:2]
    rgb = a[..., :3].reshape(-1, 3)
    res = np.empty_like(rgb)
    for i, (r, g, b) in enumerate(rgb):
        hh, ss, vv = colorsys.rgb_to_hsv(r, g, b)
        if 0.45 < hh < 0.75 and ss > 0.15:
            hh = 0.11 + (0.75 - hh) * 0.15  # cyan -> yellow, deep blue -> orange
        res[i] = colorsys.hsv_to_rgb(hh, ss, vv)
    out[..., :3] = res.reshape(h, w, 3)
    return Image.fromarray((out * 255).round().astype(np.uint8), "RGBA")


def head_pivot(img):
    """Right-most opaque column (the head, art flies right), vertical centre of the opaque pixels near it."""
    al = np.asarray(img)[..., 3] > 128
    xs = np.flatnonzero(al.any(0))
    x1 = xs.max()
    band = al[:, max(0, x1 - 25):x1 + 1]
    ys = np.flatnonzero(band.any(1))
    return float(x1 - 6), float((ys.min() + ys.max()) / 2)


def muzzle_pivot(img):
    """Left end of the white-hot core (the muzzle), at the core's vertical centre."""
    a = np.asarray(img).astype(np.float64)
    hot = (a[..., 3] > 200) & (a[..., :3].min(-1) > 225)
    if hot.sum() < 10:
        hot = a[..., 3] > 200
    ys, xs = np.nonzero(hot)
    return float(np.percentile(xs, 3)), float(np.median(ys))


def build():
    """{(row, n): (PIL RGBA, (px, py from top))}"""
    pm = key_magenta(SRC)
    boxes = cells(pm, rows=5, cols=4)
    raw = {}
    for i, (y0, y1, x0, x1) in enumerate(boxes):
        r, c = divmod(i, 4)
        raw[(SHEET_ROWS[r], c)] = to_image(pm[y0:y1, x0:x1])
    out = {}
    for row in SHEET_ROWS:
        for n, c in enumerate(PICK[row]):
            img = raw[(row, c)]
            if row == "proj":
                img = warm_bullet(img)
            img, _ = trim(resize_rgba(img, ROW_SCALE[row]), pad=2)
            if row in ("proj", "bolt"):
                piv = head_pivot(img)
            elif row == "cast":
                piv = muzzle_pivot(img)
            else:
                piv = (img.width / 2.0, img.height / 2.0)
            out[(row, n)] = (img, piv)
    return out


def cut():
    os.makedirs(OUT, exist_ok=True)
    meta = {"frames": []}
    for (row, n), (img, (px, py)) in sorted(build().items()):
        name = "%s_%d" % (row, n)
        img.save(os.path.join(OUT, name + ".png"))
        clone_meta(os.path.join(OLD, "cast_0.png.meta"), os.path.join(OUT, name + ".png.meta"))
        meta["frames"].append({"name": name, "w": img.width, "h": img.height, "px": round(px, 1), "py": round(img.height - py, 1)})
        print("%-9s %4d x %-4d pivot (%.1f, %.1f)  %.1f x %.1f units" % (name, img.width, img.height, px, py, img.width * UNITS_PER_PX, img.height * UNITS_PER_PX))
    with open(os.path.join(DATA, "vfx-attack-v2.json"), "w", newline="\n") as f:
        json.dump(meta, f, indent=1)
    clone_meta(os.path.join(DATA, "vfx-attack-orange.json.meta"), os.path.join(DATA, "vfx-attack-v2.json.meta"))
    clone_meta(OLD + ".meta", OUT + ".meta")
    print("wrote", OUT, "and", os.path.join(DATA, "vfx-attack-v2.json"))


# ---- preview: game size, normal blend + faint additive copy like VfxAnim -----------------------------------------------------
PPU = 8.0  # preview px per world unit (hero frame: 8 frame px per unit)
GLOW = 0.22


def draw_fx(canvas, img, pivot, at, scale=1.0, angle=0.0):
    s = PPU * UNITS_PER_PX * scale
    im = resize_rgba(img, s)
    pv = (pivot[0] * s, pivot[1] * s)
    if angle:
        big = Image.new("RGBA", (im.width * 3, im.height * 3 + 400), (0, 0, 0, 0))
        off = (big.width // 2 - round(pv[0]), big.height // 2 - round(pv[1]))
        big.alpha_composite(im, off)
        im = big.rotate(angle, resample=Image.BICUBIC, center=(big.width // 2, big.height // 2))
        pv = (big.width / 2, big.height / 2)
    x, y = int(round(at[0] - pv[0])), int(round(at[1] - pv[1]))
    canvas.alpha_composite(im, (x, y))
    a = np.asarray(im).astype(np.float64)
    region = np.asarray(canvas.crop((x, y, x + im.width, y + im.height))).astype(np.float64)
    region[..., :3] = np.clip(region[..., :3] + a[..., :3] * a[..., 3:4] / 255 * GLOW, 0, 255)
    canvas.paste(Image.fromarray(region.round().astype(np.uint8), "RGBA"), (x, y))


def preview():
    new = build()
    hero = Image.open("Assets/Resources/Sprites/classes/mechanic/body/bare/attack1.png").convert("RGBA").crop((192, 3 * 192, 384, 4 * 192))
    old = json.load(open(os.path.join(DATA, "vfx-attack-orange.json")))
    oldf = {f["name"]: (Image.open(os.path.join(OLD, f["name"] + ".png")).convert("RGBA"), (f["px"], f["h"] - f["py"])) for f in old["frames"]}
    slot, rowh = int(26 * PPU), int(34 * PPU)
    W = slot * 9
    panels = []
    for bg in [(38, 44, 58, 255), (226, 234, 242, 255)]:
        c = Image.new("RGBA", (W, rowh * 6), bg)
        d = ImageDraw.Draw(c)
        ink = (255, 255, 255, 255) if bg[0] < 128 else (0, 0, 0, 255)
        for r, row in enumerate(SHEET_ROWS):
            y = r * rowh + rowh // 2
            c.alpha_composite(hero, (0, int(y - 150)))
            d.text((4, r * rowh + 4), row + (" (x2.6 = r24 blast)" if row == "blast" else " (x1.2 hit 3)" if row == "bolt" else ""), fill=ink)
            sc = 2.6 if row == "blast" else 1.2 if row == "bolt" else 1.0
            for n in range(4):
                img, pv = new[(row, n)]
                draw_fx(c, img, pv, (slot * (1.6 + n * (1.9 if row == "blast" else 1.2)), y), sc)
        y = 5 * rowh + rowh // 2
        d.text((4, 5 * rowh + 4), "old attack-orange: cast_2, proj_0, impact_2", fill=ink)
        for i, nm in enumerate(["cast_2", "proj_0", "impact_2"]):
            img, pv = oldf[nm]
            draw_fx(c, img, pv, (slot * (1.6 + i * 2), y))
        panels.append(c)
    out = Image.new("RGBA", (W * 2, rowh * 6))
    for i, p in enumerate(panels):
        out.paste(p, (i * W, 0))
    os.makedirs("Tools/out", exist_ok=True)
    out.convert("RGB").save("Tools/out/mechanic-attack-fx-v2-preview.png")
    print("wrote Tools/out/mechanic-attack-fx-v2-preview.png", out.size)


if __name__ == "__main__":
    {"cut": cut, "preview": preview}[sys.argv[1] if len(sys.argv) > 1 else "preview"]()

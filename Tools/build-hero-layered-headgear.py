"""Layered hero hats (step D, art batch 3): src/headgear-<a>--<b>-chatgpt-N.png -> per-direction hat strips + icons + headgear.json art fields.

  python Tools/build-hero-layered-headgear.py write   Sprites/hero-layered/headgear/<key>.png (5 cells: down, down-right, right, up-right, up),
                                                      Sprites/icons/headgear-<key>.png (front view, 96 px), art fields of Data/hero-layered/headgear.json
  python Tools/build-hero-layered-headgear.py check   docs/reference/hero-layered-real/headgear-on-heads.png (every hat on the male idle head, 5 dirs,
                                                      drawn as HeroLayeredView does: seat point on skull top + drop * skull width, scale w / refW)

Each source has 2 rows (hat a, hat b; b = none: 1 row) of 5 views in the order and spacing of ref/bald-head-male-5dir.png (some images came back
1536x1024 instead of 2172x724, so nothing is assumed about the image size). Scale = distance view 1 -> view 5 / distance skull 1 -> skull 5 of the
reference strip; each view's head centre = a straight fit of the 5 view centres on the reference skull centres. Seat line of a row = median bottom
of its 5 views (ChatGPT kept the contact edge on one line). DROP = where that line sits below the skull top, in skull widths, read by eye on
`check`. Specks under 3 % of a view (the slime's stray eye dot, the crowns' far-rim strokes when detached) are dropped.
"""
import json
import re
import sys
import uuid
from importlib import import_module
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hero_recolor_core as core  # noqa: E402
import hero_layered_hair as hair  # noqa: E402
sheets_tool = import_module("build-hero-layered-sheets")

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/ArtLibrary/hero-layered"
SPR = ROOT / "Assets/Resources/Sprites"
JSON = ROOT / "Assets/Resources/Data/hero-layered/headgear.json"
CW, CH, AX, AY, REF_W = 256, 224, 128, 176, hair.REF_W
DROP = {"iron-helmet": 0.45, "wizard-hat": 0.52, "copper-goggles": 0.36, "ninja-headband": 0.74, "nun-veil": 1.02, "straw-hat": 0.5,
        "cloth-headband": 0.74, "pompom-beanie": 0.51, "aviator-cap": 0.62, "bunny-ears": 0.51, "slime-cap": 0.44, "fox-ears": 0.46,
        "devil-horns": 0.44, "frog-hat": 0.40, "wildflower-wreath": 0.36, "silver-wolf-crown": 0.38, "ice-crystal-tiara": 0.38,
        "serpent-crown": 0.38, "ultimate-wizard-hat": 0.52}


def key_any(img):
    """RGBA from a flat #00FF00 or #FF00FF background (picked from the corner)."""
    a = np.asarray(img.convert("RGB")).astype(np.float32)
    r, g, b = a[2, 2]
    if g > 200 and r < 80:
        return core.key_green(img)
    excess = np.minimum(a[..., 0], a[..., 2]) - a[..., 1]  # magenta key
    alpha = np.clip(1.0 - (excess - 40.0) / 120.0, 0, 1)
    soft = alpha < 0.98  # despill only the soft edge: no edge pixel more magenta than green allows
    lim = a[..., 1] + 12
    a[..., 0] = np.where(soft, np.minimum(a[..., 0], lim), a[..., 0])
    a[..., 2] = np.where(soft, np.minimum(a[..., 2], lim), a[..., 2])
    out = np.dstack([a, alpha * 255]).clip(0, 255).astype(np.uint8)
    out[alpha <= 0.02] = 0
    return out


def ref_skulls():
    """Skull centre x of the 5 heads in reference-strip px (cell px units) and the 5 idle skull widths (male)."""
    sh = Image.open(ART / "sheets/male/idle.png").convert("RGBA")
    infos = [sheets_tool.head_anchor(sh.crop((0, r * 256, 320, r * 256 + 256))) for r in range(5)]
    crops = [hair.head_crop(i) for i in infos]
    hw = max(c[2] - c[0] for c in crops)
    xs = [40 + i * (hw + 40) + (hw - (c[2] - c[0])) // 2 + (inf[0] - c[0]) for i, (c, inf) in enumerate(zip(crops, infos))]
    return np.array(xs, float), [inf[2] for inf in infos]


def views(rgba, rows):
    """[[(x0, y0, x1, y1, mask) x 5] per row]: 5 views per row, specks dropped."""
    al = rgba[..., 3] > 40
    occ = al.any(1)
    ys = np.nonzero(occ)[0]
    if rows == 2:
        mid = (ys[0] + ys[-1]) // 2
        empty = [y for y in range(ys[0], ys[-1]) if not occ[y]]
        mid = min(empty, key=lambda y: abs(y - mid)) if empty else mid
        bands = [(0, mid), (mid, rgba.shape[0])]
    else:
        bands = [(0, rgba.shape[0])]
    out = []
    for y0, y1 in bands:
        band = al.copy()
        band[:y0] = band[y1:] = False
        lab, n = ndimage.label(ndimage.binary_dilation(band, iterations=6))
        objs = ndimage.find_objects(lab)
        sizes = ndimage.sum(band, lab, range(1, n + 1))
        big = sorted([i for i in range(n) if sizes[i] > 0.15 * sizes.max()], key=lambda i: objs[i][1].start)
        if len(big) != 5:
            raise SystemExit(f"row {y0}-{y1}: {len(big)} views, expected 5")
        row = []
        for i in big:
            m = (lab == i + 1) & band
            sub, k = ndimage.label(m)
            if k > 1:
                ss = ndimage.sum(m, sub, range(1, k + 1))
                m &= np.isin(sub, [j + 1 for j, z in enumerate(ss) if z >= 0.03 * ss.max()])
            yy, xx = np.nonzero(m)
            row.append((xx.min(), yy.min(), xx.max() + 1, yy.max() + 1, m))
        out.append(row)
    return out


def cut_view(rgba, m, sx, sy, S, w):
    """Hat cell: source point (sx, sy) on (AX, AY), skull width w (male idle of that dir) scaled to REF_W."""
    k = w / REF_W
    a = rgba.astype(np.float32) * m[..., None]
    pad = 600
    a = np.pad(a, ((pad, pad), (pad, pad), (0, 0)))
    pm = Image.fromarray(np.dstack([a[..., :3] * a[..., 3:4] / 255.0, a[..., 3]]).clip(0, 255).astype(np.uint8))
    x0, y0 = sx - AX * k * S + pad, sy - AY * k * S + pad
    r = np.asarray(pm.resize((CW, CH), Image.LANCZOS, box=(x0, y0, x0 + CW * k * S, y0 + CH * k * S))).astype(np.float32)
    al = r[..., 3:4]
    rgb = np.where(al > 0, r[..., :3] * 255.0 / np.maximum(al, 1), 0)
    out = np.dstack([rgb, al]).clip(0, 255).astype(np.uint8)
    out[out[..., 3] < 6] = 0
    return out


def strips():
    """{key: [5 cells]} for every hat, latest chatgpt-N of each source pair."""
    skull_x, widths = ref_skulls()
    by_pair = {}
    for p in ART.glob("src/headgear-*-chatgpt-*.png"):
        m = re.match(r"headgear-(.+?)--(.+?)-chatgpt-(\d+)\.png$", p.name)
        if m and int(m[3]) >= by_pair.get((m[1], m[2]), (0, None))[0]:
            by_pair[(m[1], m[2])] = (int(m[3]), p)
    out = {}
    for (a, b), (_, p) in sorted(by_pair.items()):
        rgba = key_any(Image.open(p))
        keys = [a] + ([b] if b != "none" else [])
        for key, row in zip(keys, views(rgba, len(keys))):
            cx = np.array([(v[0] + v[2]) / 2 for v in row])
            S = (cx[4] - cx[0]) / (skull_x[4] - skull_x[0])
            off = float(np.mean(cx - S * skull_x))
            base = float(np.median([v[3] for v in row]))
            out[key] = [cut_view(rgba, v[4], off + S * skull_x[i], base, S, widths[i]) for i, v in enumerate(row)]
            edge = [i for i, c in enumerate(out[key]) if (c[[0, -1]][..., 3] > 20).any() or (c[:, [0, -1]][..., 3] > 20).any()]
            print(f"{key}: from {p.name}, scale {S:.3f}{'  WARNING: cut at the cell edge, views ' + str(edge) if edge else ''}")
    return out


def meta_like(dst, like):
    text = Path(str(like) + ".meta").read_text(encoding="utf-8")
    m = Path(str(dst) + ".meta")
    guid = re.search(r"guid: (\w+)", m.read_text(encoding="utf-8")).group(1) if m.exists() else uuid.uuid4().hex
    m.write_text(re.sub(r"guid: \w+", "guid: " + guid, text, count=1), encoding="utf-8")


def icon(cell):
    """96 px icon from the front view: trimmed, centred, 6 px margin."""
    im = Image.fromarray(cell)
    im = im.crop(im.getbbox())
    s = 84 / max(im.size)
    im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
    out = Image.new("RGBA", (96, 96))
    out.alpha_composite(im, ((96 - im.width) // 2, (96 - im.height) // 2))
    return out


def write():
    hats = strips()
    d = SPR / "hero-layered/headgear"
    d.mkdir(parents=True, exist_ok=True)
    fm = Path(str(d) + ".meta")
    if not fm.exists():
        fm.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n"
                      "  assetBundleName: \n  assetBundleVariant: \n", encoding="utf-8")
    for key, cells in hats.items():
        Image.fromarray(np.concatenate(cells, axis=1)).save(d / f"{key}.png")
        meta_like(d / f"{key}.png", SPR / "hero-layered/test/headgear/1.png")
        icon(cells[0]).save(SPR / f"icons/headgear-{key}.png")
        meta_like(SPR / f"icons/headgear-{key}.png", SPR / "icons/armor-0.png")
    text = JSON.read_text(encoding="utf-8")
    for key in hats:  # only the art fields of each hat line change (the rule fields mirror HeadgearCatalog.cs)
        text = re.sub(r'("key": "%s".*?"cellW": )\d+(, "cellH": )\d+(, "ax": )[\d.]+(, "ay": )[\d.]+(, "refW": )[\d.]+(, "drop": )[\d.]+(?=, "hidesHair")' % re.escape(key),
                      lambda m: f'{m[1]}{CW}{m[2]}{CH}{m[3]}{AX}{m[4]}{AY}{m[5]}{REF_W:g}{m[6]}{DROP[key]}', text)
    JSON.write_text(text, encoding="utf-8")
    print(f"wrote {len(hats)} hats")


def check():
    hats = {k: np.asarray(Image.open(SPR / f"hero-layered/headgear/{k}.png").convert("RGBA")) for k in DROP}
    sh = Image.open(ART / "sheets/male/idle.png").convert("RGBA")
    cells = [sh.crop((0, r * 256, 320, r * 256 + 256)) for r in range(5)]
    infos = [sheets_tool.head_anchor(c) for c in cells]
    tile = 140
    out = Image.new("RGBA", (tile * 5 + 130, 160 * len(hats)), (196, 204, 188, 255))
    dr = ImageDraw.Draw(out)
    for r, (key, strip) in enumerate(hats.items()):
        dr.text((4, r * 160 + 70), key, fill=(20, 20, 20, 255))
        for d, (c, (x, top, w)) in enumerate(zip(cells, infos)):
            base = Image.new("RGBA", (320, 296))
            base.alpha_composite(c, (0, 40))  # 40 px of headroom for tall hats
            k = w / REF_W
            hc = Image.fromarray(strip[:, d * CW:(d + 1) * CW]).resize((round(CW * k), round(CH * k)), Image.LANCZOS)
            lay = Image.new("RGBA", (320 + 600, 256 + 600))
            lay.alpha_composite(hc, (int(round(x - AX * k)) + 300, int(round(top + DROP[key] * w - AY * k)) + 340))
            base.alpha_composite(lay.crop((300, 300, 620, 596)))
            out.alpha_composite(base.crop((90, 0, 230, 160)), (130 + d * tile, r * 160))
    dst = ROOT / "docs/reference/hero-layered-real/headgear-on-heads.png"
    out.convert("RGB").save(dst)
    print("wrote", dst)


if __name__ == "__main__":
    {"write": write, "check": check}.get(sys.argv[1] if len(sys.argv) > 1 else "", lambda: print(__doc__))()

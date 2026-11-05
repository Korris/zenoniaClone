"""Cuts the extra Willowbrook props (wbx*) from the ChatGPT prop sheet (21 objects on #FF00FF) into Assets/Resources/Sprites/village/props/.

Usage: python Tools/cut-willowbrook-extra-props.py [--check]    (--check writes only the preview sheet on grass to plans/.../visuals)
Source: Assets/ArtLibrary/hd/map/willowbrook-meadows/large/prop-sheet-chatgpt.png (1536x1024). Keying/despill = build-meadow-monsters.key_magenta.
The sheet's objects are ~1.46x bigger than the same kind in the first Willowbrook painting (big oak 372 vs wbOakBig 254 source px), so each prop is
scaled by SCALE (x1.5 would give 8.7-tile oaks) to land in the 4-7 tile range of the wb* trees. Sprite canvas = like build-willowbrook-props.py:
width a multiple of 16 centred on the footprint centre, height a multiple of 16, bottom = bottom of the footprint row, content lifted PAD_B px
(the contact shadow stays in the tile). Prints the PropDef rows (W,H in tiles) to paste into VillagePropDefs / WillowbrookMeadowsProps.
"""
import importlib.util, math, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
SHEET = os.path.join(ROOT, "Assets/ArtLibrary/hd/map/willowbrook-meadows/large/prop-sheet-chatgpt.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/village/props")
VIS = os.path.join(ROOT, "plans/261105-1052-willowbrook-large-hunting-map/visuals")
spec = importlib.util.spec_from_file_location("mm", os.path.join(os.path.dirname(__file__), "build-meadow-monsters.py"))
mm = importlib.util.module_from_spec(spec); spec.loader.exec_module(mm)
PAD_B = 16
T = 64
# row 1 trees, row 2 bushes + rocks, row 3 stump, log, pillars, walls, reeds, barricade: (name, scale, footprint w, footprint h in tiles)
ROWS = [
    [("wbxOakBig", 1.10, 1, 1), ("wbxOakMid", 1.10, 1, 1), ("wbxOakSmall", 1.10, 1, 1), ("wbxOakSlim", 1.10, 1, 1), ("wbxPine", 1.10, 1, 1), ("wbxPineSlim", 1.10, 1, 1)],
    [("wbxBush", 0.85, 1, 1), ("wbxBushWhite", 0.85, 1, 1), ("wbxBushLilac", 0.85, 1, 1), ("wbxBoulder", 1.10, 3, 1), ("wbxRock", 1.10, 2, 1), ("wbxRocks", 1.10, 1, 1), ("wbxRockTiny", 1.10, 1, 1)],
    [("wbxStump", 1.10, 1, 1), ("wbxLogHollow", 1.10, 3, 1), ("wbxPillarTall", 1.10, 1, 1), ("wbxPillarShort", 1.10, 1, 1), ("wbxWallLow", 1.10, 2, 1), ("wbxWallCorner", 1.10, 2, 1), ("wbxReeds", 1.0, 1, 0), ("wbxBarricade", 1.10, 4, 3)],
]


def clean(rgba):
    """Stronger despill on the object rim (pink R/B above G within 4 px of transparency) and the sheet's dark-magenta contact shadows turned into a soft brown shadow."""
    rgb, a = rgba[..., :3].copy(), rgba[..., 3].copy()
    px0 = np.asarray(Image.open(SHEET).convert("RGB"), dtype=np.float32) / 255.0
    a = np.where(np.minimum(px0[..., 0], px0[..., 2]) - px0[..., 1] > 0.45, 0.0, a)   # strongly magenta = background, also inside holes (barricade)
    solid = a > 0.5
    dist = ndi.distance_transform_edt(solid)
    a = np.where((np.minimum(px0[..., 0], px0[..., 2]) - px0[..., 1] > 0.25) & (dist < 7), 0.0, a)   # pink rim pixels (half-mixed with the key)
    solid = a > 0.5
    dist = ndi.distance_transform_edt(solid)
    rim = (a > 0) & (dist < 5)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    pink = rim & (np.minimum(r, b) - g > 0.04)
    rgb[..., 0] = np.where(pink, np.minimum(r, g + 0.12), r)
    rgb[..., 2] = np.where(pink, g * 0.8, b)
    # shadow: pixels the key called background but that are darker than #FF00FF, near an object
    px = np.asarray(Image.open(SHEET).convert("RGB"), dtype=np.float32) / 255.0
    near = ndi.binary_dilation(solid, iterations=40) & (a < 0.3)
    dark = np.clip((1.0 - (px[..., 0] + px[..., 2]) / 2.0) * 1.35, 0, 0.5) * (px[..., 1] < 0.25)
    sh = np.where(near, ndi.gaussian_filter(dark, 1.5), 0.0)
    take = sh > a
    rgb = np.where(take[..., None], np.array([0.12, 0.09, 0.05], np.float32), rgb)
    a = np.where(take, sh, a)
    return np.dstack([rgb * (a > 0)[..., None], a]).astype(np.float32)


def components(rgba):
    """Objects on the sheet: alpha blobs merged across gaps < 3 px (vines, soft shadows), sorted into rows by centre y, then by x."""
    a = rgba[..., 3] > 0.55
    lab, n = ndi.label(ndi.binary_dilation(a, iterations=3))
    objs = []
    for i in range(1, n + 1):
        ys, xs = np.where((lab == i) & a)
        if len(xs) < 400: continue
        objs.append((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    objs.sort(key=lambda b: (b[1] + b[3]) / 2)
    want = [len(r) for r in ROWS]
    if len(objs) != sum(want): raise SystemExit("found %d objects, expected %d" % (len(objs), sum(want)))
    out, k = [], 0
    for n_ in want:
        out.append(sorted(objs[k:k + n_], key=lambda b: b[0])); k += n_
    return out


def cut(rgba, box, scale, fw):
    x0, y0, x1, y1 = box
    im = Image.fromarray((np.clip(rgba[y0:y1, x0:x1], 0, 1) * 255).astype(np.uint8), "RGBA")
    w, h = max(1, int(round(im.width * scale))), max(1, int(round(im.height * scale)))
    # premultiply before resizing so transparent pixels do not bleed magenta/black into the edge
    arr = np.asarray(im).astype(np.float32); a = arr[..., 3:] / 255.0
    pm = Image.fromarray(np.dstack([arr[..., :3] * a, arr[..., 3:]]).astype(np.uint8), "RGBA").resize((w, h), Image.LANCZOS)
    p = np.asarray(pm).astype(np.float32); pa = p[..., 3:] / 255.0
    rgb = np.where(pa > 0.004, p[..., :3] / np.maximum(pa, 0.004), 0)
    sp = Image.fromarray(np.dstack([np.clip(rgb, 0, 255), p[..., 3:]]).astype(np.uint8), "RGBA")
    cw = int(math.ceil(max(sp.width, fw * T) / 16.0) * 16)
    if (cw // 16) % 2: cw += 16     # centre on a tile edge/centre like the wb* canvases
    ch = int(math.ceil((sp.height + PAD_B) / 16.0) * 16)
    canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    canvas.alpha_composite(sp, ((cw - sp.width) // 2, ch - PAD_B - sp.height))
    return canvas


REF_OAKS = ["wbOakBig", "wbOakMid", "wbOakNW", "wbOakSWTree", "wbOakSouth"]


def foliage_stats(rgba_img):
    """Mean and std (per channel) of the opaque upper 60% of a sprite = its canopy."""
    a = np.asarray(rgba_img.convert("RGBA")).astype(np.float32)
    m = a[..., 3] > 200; m[int(a.shape[0] * 0.6):] = False
    return a[m][:, :3].mean(0), a[m][:, :3].std(0)


def grade_to_old_oaks(img, ref):
    """The sheet is lit warmer/brighter and higher contrast than the first painting's oaks: pull mean and contrast of the canopy to the old oaks'."""
    mu_o, sd_o = ref
    mu_n, sd_n = foliage_stats(img)
    k = np.clip(sd_o / np.maximum(sd_n, 1e-3), 0.6, 1.1)
    a = np.asarray(img).astype(np.float32)
    a[..., :3] = np.clip((a[..., :3] - mu_n) * k + mu_o, 0, 255)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def old_oak_reference():
    st = [foliage_stats(Image.open(os.path.join(OUT, n + ".png"))) for n in REF_OAKS]
    return np.mean([s[0] for s in st], 0), np.mean([s[1] for s in st], 0)


def main():
    rgba = mm.key_magenta(SHEET)
    rgba = clean(rgba)
    groups = components(rgba)
    sprites = []
    ref = old_oak_reference()
    for row, boxes in zip(ROWS, groups):
        for (name, sc, fw, fh), box in zip(row, boxes):
            img = cut(rgba, box, sc, fw)
            if name.startswith(("wbxOak", "wbxPine", "wbxBush")): img = grade_to_old_oaks(img, ref)
            sprites.append((name, img, fw, fh))
    os.makedirs(VIS, exist_ok=True)
    if "--check" not in sys.argv:
        for name, img, fw, fh in sprites: img.save(os.path.join(OUT, name + ".png"), optimize=True)
    for name, img, fw, fh in sprites:
        print('{ "%s", new PropDef(%sf, %sf, %d, %d) },' % (name, img.width / 64.0, img.height / 64.0, fw, fh))
    sheet = Image.new("RGBA", (1800, 1100), (88, 120, 40, 255)); x = y = 10; rowh = 0
    for name, img, fw, fh in sprites:
        if x + img.width > 1790: x = 10; y += rowh + 10; rowh = 0
        sheet.alpha_composite(img, (x, y)); x += img.width + 10; rowh = max(rowh, img.height)
    sheet.convert("RGB").save(os.path.join(VIS, "wbx-props-sheet.png"))


if __name__ == "__main__":
    main()

"""Cut the ChatGPT damage-number glyph sheets into the game's sprite font (DamageNumberFont.cs).

Sources (Assets/ArtLibrary/ui/damage-font/, chat https://chatgpt.com/c/6af98129-e4e8-83ec-88ef-2c765b7f4edf):
  damage-font-sheet-a-chatgpt-1.png  rows: 0-4 / 5-9 / + - ! % / MISS EXP / HP MP, white+grey sticker glyphs on #00FF00
  damage-font-sheet-b-chatgpt-1.png  rows: 0-4 / 5-9 / ! / CRITICAL, pre-coloured crit glyphs on #00FF00
Sheet A is recoloured per number kind with the spec palette (docs/reference/damage-numbers-chatgpt-spec.md): black outline and
inner contours -> outline colour, white / grey body -> fill (white = top colour, grey #C8C8C8 = bottom colour: the 2-tone cel shade).
A drop shadow (offset + opacity of the spec) is baked into every glyph. Writes
  Assets/Resources/Sprites/ui/damage-font/base-<style>.png (same layout for all styles), crit.png
  Assets/Resources/Data/ui/damage-font-glyphs.json
  damage-font-sigil-sheet-chatgpt-1.png  Arcane Sigil theme, same rows as sheet A -> Sprites/ui/damage-font-sigil/, crit = gold recolour
Usage: python Tools/build-damage-number-font.py cut [comic|sigil]
"""
import json, sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

REPO = Path(__file__).resolve().parent.parent
SRC = REPO / "Assets/ArtLibrary/ui/damage-font"
OUT_TEX = REPO / "Assets/Resources/Sprites/ui/damage-font"
OUT_JSON = REPO / "Assets/Resources/Data/ui/damage-font-glyphs.json"
CAP = 96  # digit height in the atlas, px (largest on-screen size is crit 43 px x 1.5 pop at 720p, x2 on a 1440p screen)
ROWS_A = [list("01234"), list("56789"), list("+-!%"), ["MISS", "EXP"], ["HP", "MP"]]
ROWS_B = [list("01234"), list("56789"), ["!"], ["CRITICAL"]]
# style: fill top, fill bottom, outline, shadow, shadow offset (fraction of cap), shadow opacity
STYLES = {
    "normal":    ("FFFFFF", "DCE8F6", "26364E", "111B30", (0.05, 0.08), 0.65),
    "finisher":  ("FFE58A", "FF941F", "713015", "321914", (0.05, 0.08), 0.65),
    "other":     ("F2F5FA", "B9C8DA", "314259", "111B30", (0.05, 0.08), 0.45),  # spec V2: brighter than V1 grey so it reads on snow
    "hurt":      ("FFB0A2", "FF354B", "64172C", "280C18", (0.05, 0.08), 0.65),
    "heal":      ("E7FFB0", "42DC79", "145B42", "103D35", (0.05, 0.08), 0.65),
    "potion-hp": ("FFFFFF", "55F28B", "166A43", "103D35", (0.05, 0.08), 0.65),
    "potion-mp": ("EDFFFF", "33C8FF", "175385", "102944", (0.05, 0.08), 0.65),
    "exp":       ("F7E8FF", "B56CFF", "4A2C73", "241A40", (0.05, 0.08), 0.65),  # spec V2
}
CRIT_SHADOW = ("2A1018", (0.05, 0.07), 0.85)


def rgb(h): return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def key_green(path):
    """RGBA float array: alpha from how green a pixel is, green spill pulled out of the colour."""
    a = np.asarray(Image.open(path).convert("RGB"), np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    green = g - np.maximum(r, b)
    alpha = np.clip((150 - green) / 110, 0, 1)
    g2 = np.minimum(g, np.maximum(r, b))
    return np.dstack([r, g2, b, alpha * 255])


def find_glyphs(rgba, rows, grow):
    """Connected blobs (dilated by `grow` px so letters / ! dot merge; sheet B glyphs almost touch, so 0 there), grouped into rows by centre y, sorted left to right."""
    solid = rgba[..., 3] > 128
    lab, n = ndi.label(ndi.binary_dilation(solid, iterations=grow) if grow else solid)
    boxes = [s for s in ndi.find_objects(lab) if (s[0].stop - s[0].start) * (s[1].stop - s[1].start) > 900]
    boxes.sort(key=lambda s: (s[0].start + s[0].stop) / 2)
    out, i = [], 0
    for row in rows:
        group = sorted(boxes[i:i + len(row)], key=lambda s: s[1].start)
        i += len(row)
        cy = [(s[0].start + s[0].stop) / 2 for s in group]
        if max(cy) - min(cy) > 120: sys.exit(f"row {row}: blobs not on one line {cy} (found {len(boxes)} blobs)")
        out.append(list(zip(row, group)))
    if i != len(boxes): sys.exit(f"expected {i} glyphs, found {len(boxes)} blobs")
    return out


def recolour(crop, style):
    """Sheet A glyph -> style colours (see module doc)."""
    top, bottom, line = rgb(style[0]), rgb(style[1]), rgb(style[2])
    a = crop[..., 3] / 255
    lum = crop[..., :3].mean(axis=2)
    t = np.clip((255 - lum) / 55, 0, 1)[..., None]  # white -> 0 (top colour), grey #C8C8C8 -> 1 (bottom colour)
    w = np.clip((lum - 50) / 150, 0, 1)[..., None]   # black outline / inner contours -> outline colour, soft edge into the body
    w = np.where((a < 0.5)[..., None], 0, w)          # anti-aliased rim against the background stays outline colour
    col = line * (1 - w) + (top * (1 - t) + bottom * t) * w
    return np.dstack([col, crop[..., 3]])


def with_shadow(img, colour, off, opacity):
    """Pads the glyph and composites a flat shadow copy under it at (off * cap) px."""
    if colour is None: return img, 0, 0
    dx, dy = int(round(off[0] * CAP)), int(round(off[1] * CAP))
    h, w = img.shape[:2]
    out = np.zeros((h + dy, w + dx, 4), np.float32)
    out[dy:, dx:, :3] = rgb(colour)
    out[dy:, dx:, 3] = img[..., 3] * opacity
    fa, ba = img[..., 3:4] / 255, out[:h, :w, 3:4] / 255
    oa = fa + ba * (1 - fa)
    out[:h, :w, :3] = np.where(oa > 0, (img[..., :3] * fa + out[:h, :w, :3] * ba * (1 - fa)) / np.maximum(oa, 1e-6), 0)
    out[:h, :w, 3:4] = oa * 255
    return out, dx, dy


def scaled(arr, k):
    im = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")
    return np.asarray(im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS), np.float32)


def glyph_metrics(rows):
    """Per glyph: (key, slice, bottom offset below the row baseline in source px); baseline = median digit bottom of that row."""
    digit_h = [s[0].stop - s[0].start for row in rows for c, s in row if c.isdigit()]
    base_rows = [np.median([s[0].stop for c, s in row]) for row in rows]
    out = []
    for row, base in zip(rows, base_rows):
        for c, s in row:
            out.append((c, s, base))
    return out, float(np.median(digit_h))


def build(sheet, rows, styles, grow):
    """Returns {style: [(key, image, top, bottom)]} with top / bottom in atlas px above the baseline."""
    rgba = key_green(SRC / sheet)
    glyphs, digit_h = glyph_metrics(find_glyphs(rgba, rows, grow))
    k = CAP / digit_h
    res = {name: [] for name in styles}
    for c, s, base in glyphs:
        crop = rgba[s[0], s[1]].copy()
        h = crop.shape[0]
        if c in "+-": bottom = (digit_h - h) / 2  # centred on the digit height
        elif c.isdigit(): bottom = base - s[0].stop
        else: bottom = 0.0
        for name, st in styles.items():
            img = recolour(crop, st) if st[0] else crop
            img, _, dy = with_shadow(scaled(img, k), st[3], st[4], st[5])
            top_px = (bottom + h) * k
            res[name].append((c, img, top_px, top_px - img.shape[0]))
    return res, CAP


def pack(glyphs, width=1024, pad=4):
    """Shelf packing; returns atlas array and rects."""
    x = y = shelf = 0
    rects = []
    for c, img, top, bot in glyphs:
        h, w = img.shape[:2]
        if x + w + pad > width: x, y, shelf = 0, y + shelf + pad, 0
        rects.append((c, x, y, w, h, top, bot))
        x += w + pad
        shelf = max(shelf, h)
    atlas = np.zeros((y + shelf + pad, width, 4), np.float32)
    for (c, img, _, _), (_, gx, gy, w, h, _, _) in zip(glyphs, rects):
        atlas[gy:gy + h, gx:gx + w] = img
    return atlas, rects


def save(atlas, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.clip(atlas, 0, 255).astype(np.uint8), "RGBA").save(path)


# Arcane Sigil theme (ChatGPT, round 3; mockup ArtLibrary/ui/damage-font/damage-numbers-v3-mockup-chatgpt-1.png): serif italic glyphs,
# one white/grey sheet; the crit set is the same glyphs in gold. Finisher stays white (user 15/11: 4th hit white).
SIGIL_STYLES = {
    "normal":    ("F7FAFF", "B4CDEE", "1B2A4E", "0E1730", (0.04, 0.06), 0.55),
    "finisher":  ("F7FAFF", "B4CDEE", "1B2A4E", "0E1730", (0.04, 0.06), 0.55),
    "other":     ("EEF3FA", "B9C8DA", "2C3C58", "0E1730", (0.04, 0.06), 0.35),
    "hurt":      ("FFF0EE", "FFB3AE", "6E1020", "2A0710", (0.04, 0.06), 0.55),
    "heal":      ("EEFFD8", "74E07E", "16502F", "0C2E1E", (0.04, 0.06), 0.55),
    "potion-hp": ("EEFFD8", "74E07E", "16502F", "0C2E1E", (0.04, 0.06), 0.55),
    "potion-mp": ("E6F8FF", "70CBFF", "163E73", "0B2142", (0.04, 0.06), 0.55),
    "exp":       ("FFFFFF", "E6D3FF", "3B2163", "1E1036", (0.04, 0.06), 0.45),
}
SIGIL_CRIT = ("FFF6C8", "F0A830", "4E1F08", "2A1004", (0.05, 0.07), 0.8)
THEMES = {  # theme: (output folder, glyph json)
    "comic": (OUT_TEX, OUT_JSON),
    "sigil": (REPO / "Assets/Resources/Sprites/ui/damage-font-sigil", REPO / "Assets/Resources/Data/ui/damage-font-sigil-glyphs.json"),
}


def cut(theme):
    out_tex, out_json = THEMES[theme]
    if theme == "comic":
        base, cap = build("damage-font-sheet-a-chatgpt-1.png", ROWS_A, STYLES, 9)
        crit, _ = build("damage-font-sheet-b-chatgpt-1.png", ROWS_B, {"crit": (None, None, None) + CRIT_SHADOW}, 0)
    else:
        styles = dict(SIGIL_STYLES, crit=SIGIL_CRIT)
        both, cap = build("damage-font-sigil-sheet-chatgpt-1.png", ROWS_A, styles, 9)
        crit = {"crit": both.pop("crit")}
        base = both
    sets, atlas_h, written = [], 0, []
    for name, glyphs in list(base.items()) + list(crit.items()):
        atlas, rects = pack(glyphs)
        atlas_h = max(atlas_h, atlas.shape[0])
        path = out_tex / ("crit.png" if name == "crit" else f"base-{name}.png")
        save(atlas, path); written.append(path)
        if name in ("normal", "crit"):
            sets.append({"name": "crit" if name == "crit" else "base", "capHeight": cap,
                         "glyphs": [{"c": c, "x": x, "y": y, "w": w, "h": h, "top": round(t, 2), "bottom": round(b, 2)} for c, x, y, w, h, t, b in rects]})
        print(name, atlas.shape[1], "x", atlas.shape[0])
    for p in written:  # all glyph atlases one height so the json UVs (atlasH) fit every style (only these files: fx-*.png keep their size)
        im = Image.open(p)
        if im.height != atlas_h:
            canvas = Image.new("RGBA", (im.width, atlas_h)); canvas.paste(im, (0, 0)); canvas.save(p)
    out_json.parent.mkdir(parents=True, exist_ok=True)
    out_json.write_text(json.dumps({"atlasW": 1024, "atlasH": atlas_h, "sets": sets}, indent=1), encoding="utf-8")
    print("wrote", out_json)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "cut": cut(sys.argv[2] if len(sys.argv) > 2 else "comic")
    else: print(__doc__)

"""Packs the 45 old-section Willowbrook props (wb*, cut from the first painting) into 4 reference sheets for ChatGPT to redraw in the Art Bible style.

Usage: python Tools/build-willowbrook-old-props-redraw-sheets.py
Source: Assets/ArtLibrary/maps/willowbrook-meadows/redraw/old-section-props-src/wb*.png (never overwritten).
Output: Assets/ArtLibrary/maps/willowbrook-meadows/redraw/old-section-props-sheets/sheet-<group>-ref.png (1536x1024 on #FF00FF) + layout.json
 (per prop: sheet, scale, the slot rect on the sheet and the prop's opaque bbox inside its own canvas). ChatGPT is asked to redraw every object in place,
 same size and silhouette; Tools/cut-willowbrook-old-props-from-redraw-sheets.py then cuts each slot back to the original canvas so PropDefs stay valid.
"""
import glob, json, os
import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
SRC = os.path.join(ROOT, "Assets/ArtLibrary/maps/willowbrook-meadows/redraw/old-section-props-src")
OUT = os.path.join(ROOT, "Assets/ArtLibrary/maps/willowbrook-meadows/redraw/old-section-props-sheets")
W, H, GAP, MARGIN = 1536, 1024, 36, 40
KEY = (255, 0, 255, 255)


def group_of(name):
    if "Oak" in name: return "oaks-a" if name in OAKS_A else "oaks-b"
    if name.startswith("wbRock"): return "rocks"
    return "structures"   # shed, palisade, fences, haystack, log, signpost, cliffs


OAKS_A = {"wbOakBig", "wbOakClumpW", "wbOakEastA", "wbOakEastB", "wbOakEastMid", "wbOakMid", "wbOakNW", "wbOakSE", "wbOakSWClump"}


def crop_opaque(path):
    im = Image.open(path).convert("RGBA")
    a = np.asarray(im)[..., 3]
    ys, xs = np.where(a > 20)
    box = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
    return im.crop(box), box, im.size


def shelf_pack(items, scale):
    """Places items (name, img) left to right in shelves at `scale`; returns slots or None when they do not fit."""
    x, y, shelf_h, slots = MARGIN, MARGIN, 0, []
    for name, img in sorted(items, key=lambda t: -t[1].height):
        w, h = int(round(img.width * scale)), int(round(img.height * scale))
        if x + w > W - MARGIN: x, y, shelf_h = MARGIN, y + shelf_h + GAP, 0
        if y + h > H - MARGIN or w > W - 2 * MARGIN: return None
        slots.append((name, (x, y, x + w, y + h))); x += w + GAP; shelf_h = max(shelf_h, h)
    return slots


def main():
    os.makedirs(OUT, exist_ok=True)
    groups, meta = {}, {}
    for p in sorted(glob.glob(os.path.join(SRC, "wb*.png"))):
        name = os.path.basename(p)[:-4]
        img, box, size = crop_opaque(p)
        groups.setdefault(group_of(name), []).append((name, img))
        meta[name] = {"bbox": box, "canvas": size}
    layout = {}
    for g, items in groups.items():
        scale = 1.0
        while (slots := shelf_pack(items, scale)) is None: scale -= 0.02   # largest uniform scale that fits keeps relative sizes between props
        sheet = Image.new("RGBA", (W, H), KEY)
        imgs = dict(items)
        for name, (x0, y0, x1, y1) in slots:
            sheet.alpha_composite(imgs[name].resize((x1 - x0, y1 - y0), Image.LANCZOS), (x0, y0))
            layout[name] = {"sheet": g, "scale": scale, "slot": [x0, y0, x1, y1], **meta[name]}
        sheet.convert("RGB").save(os.path.join(OUT, "sheet-%s-ref.png" % g))
        print(g, len(items), "props, scale %.2f" % scale)
    with open(os.path.join(OUT, "layout.json"), "w") as f: json.dump(layout, f, indent=1)


if __name__ == "__main__":
    main()

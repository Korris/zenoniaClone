"""Cuts the ChatGPT HUD V2 sheets (flat key background) into separate transparent PNGs for the 3D prototype.

  python cut-hud3d-parts.py parts   -> parts/<name>.png from hud3d-parts-sheet-v2-ro3-chatgpt.png (#00FF00 key)
  python cut-hud3d-parts.py icons   -> parts/icon-<skill>.png from hud3d-skill-icons-v2-ro3-chatgpt.png (#FF00FF key)
  python cut-hud3d-parts.py preview -> parts-preview.png (every piece on dark and light grey)

Alpha comes from how far a pixel is from the key colour (soft edges), then the key colour is removed from edge pixels (despill).
Pieces are found as connected blobs and named by the nearest expected centre (sheet coordinates read off the image).
setup-proto3d.ps1 copies parts/*.png into the Unity project (Assets/Resources/ProtoHud).
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = Path(__file__).parent
OUT = HERE / "parts"

PARTS = {  # name: expected centre (x, y) on the 1536x1024 parts sheet
    "portrait-ring": (205, 158), "portrait-warrior": (535, 158), "class-crest-warrior": (840, 155), "lv-tag": (1188, 160),
    "status-panel": (340, 408), "bar-groove": (862, 390), "bar-fill-hp": (1283, 358), "bar-fill-mp": (1283, 445),
    "icon-heart": (105, 640), "icon-mana-drop": (260, 630), "skillbar-panel": (940, 632),
    "slot-frame": (148, 880), "key-tag": (387, 880), "mp-cost-pill": (630, 887), "slot-cooldown": (903, 882),
    "slot-pressed": (1150, 882), "slot-ready-flash": (1395, 882),
}
ICONS = {"icon-power-slash": (0, 0), "icon-whirlwind": (1, 0), "icon-charging-strike": (0, 1), "icon-inferno-crash": (1, 1)}  # grid cell


def key_alpha(rgb, key):
    r, g, b = (rgb[..., i].astype(np.float32) for i in range(3))
    if key == "green":
        spill = g - np.maximum(r, b)
    else:  # magenta
        spill = np.minimum(r, b) - g
    alpha = 1.0 - np.clip((spill - 40.0) / 140.0, 0.0, 1.0)
    keyc = np.array([0, 255, 0] if key == "green" else [255, 0, 255], np.float32)
    a3 = np.maximum(alpha, 0.05)[..., None]
    out = (rgb.astype(np.float32) - (1.0 - a3) * keyc) / a3   # un-mix the key colour from soft edge pixels
    return np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8)


def blobs(rgba, min_area):
    mask = rgba[..., 3] > 40
    mask = ndimage.binary_closing(mask, iterations=2)
    lab, n = ndimage.label(mask)
    found = []
    for i, sl in enumerate(ndimage.find_objects(lab)):
        if sl is None:
            continue
        area = int((lab[sl] == i + 1).sum())
        if area >= min_area:
            found.append((sl, i + 1))
    return lab, found


def crop(rgba, sl, pad=3):
    y0, y1 = max(0, sl[0].start - pad), min(rgba.shape[0], sl[0].stop + pad)
    x0, x1 = max(0, sl[1].start - pad), min(rgba.shape[1], sl[1].stop + pad)
    return rgba[y0:y1, x0:x1]


def neutral_glass(piece):
    """The skill bar is a see-through glass plate, so the green key showed through it on the sheet: grey it back to the mockup's
    cool pearl grey (#637385..#D8DEE7) by luminance, and keep its middle at least 55 % opaque like the mockup."""
    rgb = piece[..., :3].astype(np.float32)
    lum = rgb @ np.array([0.3, 0.45, 0.25], np.float32)
    tint = np.array([0.93, 0.97, 1.04], np.float32)
    out = np.clip(lum[..., None] * tint, 0, 255)
    a = piece[..., 3].astype(np.float32)
    a = np.where(a > 30, np.maximum(a, 140), a)
    return np.dstack([out, a]).astype(np.uint8)


def cut_parts():
    rgba = key_alpha(np.array(Image.open(HERE / "hud3d-parts-sheet-v2-ro3-chatgpt.png").convert("RGB")), "green")
    _, found = blobs(rgba, 400)
    centres = [((sl[1].start + sl[1].stop) / 2, (sl[0].start + sl[0].stop) / 2, sl) for sl, _ in found]
    OUT.mkdir(exist_ok=True)
    for name, (ex, ey) in PARTS.items():
        cx, cy, sl = min(centres, key=lambda c: (c[0] - ex) ** 2 + (c[1] - ey) ** 2)
        piece = crop(rgba, sl)
        if name == "skillbar-panel": piece = neutral_glass(piece)
        Image.fromarray(piece).save(OUT / f"{name}.png")
        print(f"{name:22s} {piece.shape[1]}x{piece.shape[0]} centre {cx:.0f},{cy:.0f}")
    print(len(found), "blobs on the sheet,", len(PARTS), "named")


def round_corners(im, radius01):
    n = im.size[0]
    yy, xx = np.mgrid[0:n, 0:n] + 0.5
    r = radius01 * n
    qx = np.maximum(0, np.abs(xx - n / 2) - (n / 2 - r)); qy = np.maximum(0, np.abs(yy - n / 2) - (n / 2 - r))
    mask = np.clip((r - np.sqrt(qx ** 2 + qy ** 2)) / 1.5, 0, 1)
    a = np.array(im.convert("RGBA")).astype(np.float32)
    a[..., 3] *= mask
    return Image.fromarray(a.astype(np.uint8))


def cut_icons():
    rgba = key_alpha(np.array(Image.open(HERE / "hud3d-skill-icons-v2-ro3-chatgpt.png").convert("RGB")), "magenta")
    _, found = blobs(rgba, 20000)
    h, w = rgba.shape[:2]
    OUT.mkdir(exist_ok=True)
    for name, (gx, gy) in ICONS.items():
        cell = [sl for sl, _ in found if ((sl[1].start + sl[1].stop) / 2 > w / 2) == bool(gx) and ((sl[0].start + sl[0].stop) / 2 > h / 2) == bool(gy)]
        sl = max(cell, key=lambda s: (s[0].stop - s[0].start) * (s[1].stop - s[1].start))
        piece = Image.fromarray(crop(rgba, sl, pad=0)).resize((256, 256), Image.LANCZOS)
        piece = round_corners(piece, 0.16)   # so the square painting does not poke out of the slot frame's rounded corners
        piece.save(OUT / f"{name}.png")
        print(name, sl)


def preview():
    files = sorted(OUT.glob("*.png"))
    tiles = []
    for f in files:
        im = Image.open(f).convert("RGBA")
        im.thumbnail((260, 160))
        tile = Image.new("RGBA", (540, 170), (40, 44, 52, 255))
        tile.paste(Image.new("RGBA", (270, 170), (200, 205, 210, 255)), (270, 0))
        tile.alpha_composite(im, (5, 5)); tile.alpha_composite(im, (275, 5))
        tiles.append(tile)
    sheet = Image.new("RGBA", (1080, 170 * ((len(tiles) + 1) // 2)), (0, 0, 0, 255))
    for i, t in enumerate(tiles):
        sheet.paste(t, ((i % 2) * 540, (i // 2) * 170))
    sheet.save(HERE / "parts-preview.png")


if __name__ == "__main__":
    {"parts": cut_parts, "icons": cut_icons, "preview": preview}[sys.argv[1]]()

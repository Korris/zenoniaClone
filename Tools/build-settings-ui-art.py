"""Cuts the Settings window art (music / speaker badges, switch on / off) from the ChatGPT sheet on flat #00FF00.

Source: Assets/ArtLibrary/custom/settings-ui/settings-art-sheet.png (2 x 2 grid: left column music, speaker; right column switch on, switch off),
chat https://chatgpt.com/c/6ae9758d-9b2c-83ec-8410-c3d399d4b91f (Tay account).
Output: Assets/Resources/Sprites/ui/settings/settings-{music,sound,toggle-on,toggle-off}.png, read by SettingsWindow / SettingsToggle.

  python Tools/build-settings-ui-art.py            cut and write, then a preview on dark and light ground in the scratch path given by PREVIEW (optional)

Keying: the art has no green, so every pixel with a green excess (G - max(R, B)) is background (key_all, as in the Trade art);
edge pixels get alpha from that excess and the green removed. 6 px along the sheet edge are cleared before the bounding box
(stray pixels there stretched boxes before, see the Sổ lỗi rows of UI Chọn hero). Items are the largest blobs, assigned to
grid cells by centre. The two switches are cut to one common box size so on / off never jump.
"""
import os
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Assets/ArtLibrary/custom/settings-ui/settings-art-sheet.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/ui/settings")
BADGE_PX = 128   # badges drawn at 42 canvas units: 3x for sharp high-dpi screens
SWITCH_H = 96    # switches drawn 38 units high


def key_green(rgb):
    """Alpha from the green excess: 0 on pure #00FF00, 1 once green no longer dominates; colour with the green spill removed."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    excess = g - np.maximum(r, b)
    alpha = np.clip(1.0 - (excess - 20.0) / 120.0, 0.0, 1.0)
    out = rgb.copy()
    spill = np.maximum(r, b)
    out[..., 1] = np.where(excess > 0, np.minimum(g, spill + 0.15 * excess), g)  # pull green back to the other channels at the rim
    return out, alpha


def blobs(alpha):
    mask = alpha > 0.5
    mask[:6, :] = mask[-6:, :] = False
    mask[:, :6] = mask[:, -6:] = False
    lab, n = ndimage.label(mask)
    sizes = ndimage.sum(mask, lab, range(1, n + 1))
    keep = [i + 1 for i in np.argsort(sizes)[::-1][:4]]
    return lab, keep


def main():
    im = np.asarray(Image.open(SRC).convert("RGB")).astype(np.float32)
    h, w = im.shape[:2]
    rgb, alpha = key_green(im)
    lab, keep = blobs(alpha)
    cells = {}
    for k in keep:
        ys, xs = np.nonzero(lab == k)
        cy, cx = ys.mean(), xs.mean()
        cells[(int(cy > h / 2), int(cx > w / 2))] = (ys.min(), ys.max() + 1, xs.min(), xs.max() + 1, k)
    names = {(0, 0): "settings-music", (1, 0): "settings-sound", (0, 1): "settings-toggle-on", (1, 1): "settings-toggle-off"}
    if len(cells) != 4:
        sys.exit("expected one item per grid cell, got %s" % sorted(cells))
    # one box size for both switches (and both badges), centred on each item
    os.makedirs(OUT, exist_ok=True)
    pairs = [((0, 0), (1, 0)), ((0, 1), (1, 1))]  # badges (left column), switches (right column)
    for pair in pairs:
        bh = max(cells[c][1] - cells[c][0] for c in pair) + 8
        bw = max(cells[c][3] - cells[c][2] for c in pair) + 8
        for c in pair:
            y0, y1, x0, x1, k = cells[c]
            cy, cx = (y0 + y1) // 2, (x0 + x1) // 2
            grow = ndimage.binary_dilation(lab == k, iterations=3)  # the soft rim belongs to the item
            a = np.where(grow, alpha, 0.0)
            rgba = np.dstack([rgb, a * 255.0]).clip(0, 255).astype(np.uint8)
            top, left = cy - bh // 2, cx - bw // 2
            piece = Image.fromarray(rgba).crop((left, top, left + bw, top + bh))
            if c[1] == 0:  # badge
                piece = piece.resize((BADGE_PX, round(BADGE_PX * bh / bw)), Image.LANCZOS)
            else:
                piece = piece.resize((round(SWITCH_H * bw / bh), SWITCH_H), Image.LANCZOS)
            path = os.path.join(OUT, names[c] + ".png")
            piece.save(path)
            print("%-22s src box %dx%d -> %dx%d" % (names[c], x1 - x0, y1 - y0, piece.width, piece.height))
    preview = os.environ.get("PREVIEW")
    if preview:
        sheet = Image.new("RGBA", (900, 260), (14, 24, 44, 255))
        sheet.paste((235, 240, 245, 255), (450, 0, 900, 260))
        for i, n in enumerate(names.values()):
            p = Image.open(os.path.join(OUT, n + ".png"))
            for ox in (0, 450):
                sheet.alpha_composite(p, (ox + 20 + (i // 2) * 150, 20 + (i % 2) * 130))
        sheet.save(preview)


if __name__ == "__main__":
    main()

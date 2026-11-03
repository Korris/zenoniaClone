"""Warrior hit 4 (jump cleave) slash VFX: a vertical top-to-bottom chop, 4 frames, drawn by ChatGPT on flat black
(chat https://chatgpt.com/c/6ae97092-8cbc-83ec-8e9c-25967fe224cd, 2026-11-03), in the style of warrior-slash-vfx.png.

Source: Assets/ArtLibrary/classes/warrior/fx-src/cleave-chatgpt.png
Writes:  Assets/Resources/Sprites/classes/warrior/fx/warrior-cleave-vfx.png   1 x 4 cells of 384 px, each frame centred on the
         same point (the column's art centre), straight alpha from the glow (alpha = brightest channel, colour un-multiplied).
SlashEffect plays it for the combo finisher instead of the finisher ring (falls back to the ring if the file is missing).

  python Tools/build-warrior-cleave-vfx.py
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Assets/ArtLibrary/classes/warrior/fx-src/cleave-chatgpt.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/classes/warrior/fx/warrior-cleave-vfx.png")
REF_META = os.path.join(ROOT, "Assets/Resources/Sprites/classes/warrior/fx/warrior-slash-vfx.png.meta")
CELL, N = 384, 4
BLACK_CUT = 18  # 0..255: darker than this is background noise


def unblack(rgb):
    """Flat black render -> straight-alpha RGBA: alpha from the brightest channel, colour divided back out."""
    rgb = rgb.astype(np.float32)
    a = np.clip((rgb.max(-1) - BLACK_CUT) / (255 - BLACK_CUT), 0, 1)
    col = np.where(a[..., None] > 0.01, rgb / np.maximum(a[..., None], 0.01), 0)
    return np.dstack([np.clip(col, 0, 255), a * 255]).astype(np.uint8)


def main():
    rgba = unblack(np.asarray(Image.open(SRC).convert("RGB")))
    h, w = rgba.shape[:2]
    rgba[:6], rgba[-6:], rgba[:, :6], rgba[:, -6:] = 0, 0, 0, 0
    col_w = w / N
    boxes = []
    for i in range(N):
        part = rgba[:, int(i * col_w):int((i + 1) * col_w)]
        ys, xs = np.nonzero(part[..., 3] > 25)
        boxes.append((xs.min(), xs.max(), ys.min(), ys.max()))
    # One scale for all frames: the tallest art fills 92% of a cell, so the frames keep their relative size.
    tallest = max(max(b[1] - b[0], b[3] - b[2]) for b in boxes)
    k = CELL * 0.92 / tallest
    # All frames share the row's vertical centre (the full chop of frame 2), so the slash does not jump between frames.
    y0 = min(b[2] for b in boxes); y1 = max(b[3] for b in boxes)
    sheet = Image.new("RGBA", (CELL * N, CELL))
    for i in range(N):
        part = rgba[:, int(i * col_w):int((i + 1) * col_w)]
        x0, x1 = boxes[1][0], boxes[1][1]  # horizontal centre of the full chop, same column offset in every frame
        cx = (x0 + x1) / 2
        crop = Image.fromarray(part[y0:y1 + 1])
        im = crop.resize((max(1, round(crop.width * k)), max(1, round(crop.height * k))), Image.LANCZOS)
        ox = round(i * CELL + CELL / 2 - cx * k)
        oy = round((CELL - im.height) / 2)
        cell = Image.new("RGBA", (CELL * N, CELL))
        cell.paste(im, (ox, oy))
        sheet.alpha_composite(cell.crop((i * CELL, 0, (i + 1) * CELL, CELL)), (i * CELL, 0))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.save(OUT)
    if not os.path.exists(OUT + ".meta") and os.path.exists(REF_META):
        import re, uuid
        meta = open(REF_META, encoding="utf-8").read()
        open(OUT + ".meta", "w", encoding="utf-8", newline="\n").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, meta, count=1))
    print("wrote", os.path.relpath(OUT, ROOT), sheet.size, "scale", round(k, 3), "boxes", boxes)


if __name__ == "__main__":
    main()

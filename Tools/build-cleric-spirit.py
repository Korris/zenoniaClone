"""Helpers for the spirit pet art (Tools/build-pet-art.py): key the ChatGPT pair images on flat #0000FF, split base / enhanced, fit to the
acolyte template, item icon. Blue key because the tiers hold green (gems, bows, leaves) and pink-peach (fault log: pink ears beside #FF00FF).

This was the tool of the Cleric spirit companion (Resources/Sprites/classes/cleric/companion/, companion grips in held-cleric.json, float
frames, offhand icons). The companion became the spirit pet (Resources/Sprites/pets/spirit/) and its Resources art, the backup and the
backup / install / float / icons / preview / drafts commands were removed 16/11 (git 4c527c36 has them). No commands left.
Sources: Assets/ArtLibrary/classes/cleric/companion-src/src/<tier>-pair.png."""
import numpy as np
from PIL import Image
SRC_ROOT = "Assets/ArtLibrary/classes/cleric/companion-src"
SRC = f"{SRC_ROOT}/src"
CANVAS = 300                 # working square; the pivot of every tier is its centre
K = 0.29                     # source px -> art px for acolyte, gives about 213 px tall like the old art


def key_blue(path):
    """RGBA from the flat blue image. Blue is in no tier's palette, so every bluish pixel is background, holes included."""
    a = np.array(Image.open(path).convert("RGB")).astype(float)
    a[:6], a[-6:], a[:, :6], a[:, -6:] = 0, 0, 0, 0
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    excess = np.clip(b - np.maximum(r, g), 0, None)
    bg = np.median(b[excess > 150]) if (excess > 150).any() else 255.0
    alpha = 1 - np.clip((excess - 14) / (bg * 0.82), 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    safe = np.maximum(alpha, 0.01)
    # the fringe mixes into the black outline: take the blue back out, colour stays dark
    out = np.empty(a.shape[:2] + (4,), np.uint8)
    out[..., 0] = np.clip(r, 0, 255)
    out[..., 1] = np.clip(g, 0, 255)
    out[..., 2] = np.clip(np.where(alpha < 1, np.minimum(b - (1 - alpha) * bg, np.maximum(r, g)), b), 0, 255)
    out[..., 3] = np.round(alpha * 255)
    del safe
    return out


def components(mask):
    """Connected areas of a boolean array as (area, x0, y0, x1, y1, label array index), 4-connected, without scipy."""
    h, w = mask.shape
    labels = np.zeros((h, w), np.int32)
    found = []
    for y0, x0 in zip(*np.nonzero(mask)):
        if labels[y0, x0]:
            continue
        n = len(found) + 1
        stack, area = [(y0, x0)], 0
        labels[y0, x0] = n
        x_lo, x_hi, y_lo, y_hi = x0, x0, y0, y0
        while stack:
            y, x = stack.pop()
            area += 1
            x_lo, x_hi, y_lo, y_hi = min(x_lo, x), max(x_hi, x), min(y_lo, y), max(y_hi, y)
            for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not labels[ny, nx]:
                    labels[ny, nx] = n
                    stack.append((ny, nx))
        found.append((area, x_lo, y_lo, x_hi + 1, y_hi + 1))
    return labels, found


def split_pair(path):
    """Left and right creature. Cuts at the emptiest column near the middle; drops specks."""
    rgba = key_blue(path)
    h, w = rgba.shape[:2]
    solid = rgba[..., 3] > 128
    cols = solid.sum(axis=0)
    lo, hi = int(w * 0.38), int(w * 0.62)
    empty = np.where(cols[lo:hi] == 0)[0]
    if len(empty) == 0:
        print(path, "copies touch: no empty column between them")
        cut = lo + int(np.argmin(cols[lo:hi]))
    else:
        cut = lo + int(np.median(empty))
    pair = []
    for part in (rgba[:, :cut], rgba[:, cut:]):
        part = part.copy()
        labels, found = components(part[..., 3] > 128)
        biggest = max(f[0] for f in found)
        keep = np.isin(labels, [i + 1 for i, f in enumerate(found) if f[0] >= max(60, biggest * 0.0008)])
        # keep the soft fringe around kept areas: grow by 3 px
        grown = keep.copy()
        for _ in range(3):
            g = grown.copy()
            g[1:] |= grown[:-1]; g[:-1] |= grown[1:]; g[:, 1:] |= grown[:, :-1]; g[:, :-1] |= grown[:, 1:]
            grown = g
        part[..., 3] = np.where(grown, part[..., 3], 0)
        ys, xs = np.nonzero(part[..., 3])
        pair.append(Image.fromarray(part[ys.min():ys.max() + 1, xs.min():xs.max() + 1], "RGBA"))
    return pair


def mask_of(im):
    return np.array(im.getchannel("A")) > 128


def place_on_canvas(im, k, ox, oy):
    """im scaled by k, its centre at canvas centre + (ox, oy)."""
    w, h = max(1, round(im.width * k)), max(1, round(im.height * k))
    small = im.resize((w, h), Image.LANCZOS)
    out = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    out.alpha_composite(small, (CANVAS // 2 - w // 2 + ox, CANVAS // 2 - h // 2 + oy)) if (CANVAS // 2 - w // 2 + ox >= 0 and CANVAS // 2 - h // 2 + oy >= 0) else None
    return out


def best_fit(im, template):
    """Scale and offset where im overlaps the acolyte silhouette most. Motifs outside it only lower the score a little."""
    best = (-1, K, 0, 0)
    for s in np.arange(0.82, 1.2001, 0.02):
        k = K * s
        m = mask_of(place_on_canvas(im, k, 0, 0))
        for dy in range(-40, 41, 4):
            my = np.roll(m, dy, axis=0)
            for dx in range(-24, 25, 4):
                mxy = np.roll(my, dx, axis=1)
                iou = (mxy & template).sum() / max(1, (mxy | template).sum())
                if iou > best[0]:
                    best = (iou, k, dx, dy)
    iou, k, dx, dy = best
    for ddy in range(-3, 4):
        for ddx in range(-3, 4):
            m = np.roll(np.roll(mask_of(place_on_canvas(im, k, 0, 0)), dy + ddy, 0), dx + ddx, 1)
            v = (m & template).sum() / max(1, (m | template).sum())
            if v > best[0]:
                best = (v, k, dx + ddx, dy + ddy)
    return best


def checks(tier, kind, art):
    a = np.array(art).astype(int)
    solid = a[..., 3] > 200
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    pink = (np.minimum(r, b) - g > 15) & solid
    blue = (b - np.maximum(r, g) > 30) & solid
    semi = ((a[..., 3] > 20) & (a[..., 3] < 235)).sum()
    print(f"  {tier}-{kind}: size {art.size} pink {pink.sum()} blue {blue.sum()} soft-edge {semi}")


def cut(tier, template):
    """Returns {kind: (cropped art, gripX, gripY)}."""
    base, enhanced = split_pair(f"{SRC}/{tier}-pair.png")
    result = {}
    for kind, im in (("base", base), ("enhanced", enhanced)):
        iou, k, dx, dy = (1.0, K, 0, 0) if tier == "acolyte" and kind == "base" else best_fit(im, template)
        placed = place_on_canvas(im, k, dx, dy)
        x0, y0, x1, y1 = placed.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
        x0, y0, x1, y1 = max(0, x0 - 3), max(0, y0 - 3), min(CANVAS, x1 + 3), min(CANVAS, y1 + 3)
        art = placed.crop((x0, y0, x1, y1))
        grip_x, grip_y = CANVAS // 2 - x0, CANVAS // 2 - y0
        print(f"  {tier}-{kind}: overlap with acolyte {iou:.3f}, scale {k:.3f}, offset ({dx}, {dy}), grip ({grip_x}, {grip_y})")
        checks(tier, kind, art)
        result[kind] = (art, grip_x, grip_y)
    return result


def icon(path):
    """Same steps as fit() in build-item-icons.py with tilt 0."""
    im = Image.open(path).convert("RGBA")
    im = im.crop(im.getbbox())
    k = 88 / max(im.width, im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    out.paste(im, ((96 - im.width) // 2, (96 - im.height) // 2), im)
    return out

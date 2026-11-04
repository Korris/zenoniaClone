"""Strip helpers shared by Tools/build-fighter-new-style.py: keying ChatGPT art off flat #00FF00, splitting a row into frames,
placing frames in 192 px cells, breathing, grid sheets for reading fists, Unity meta copies. Lifted from Tools/build-assassin-new-style.py."""
import os, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

FRAME = 192
LIFT = [0.0, -0.7, -1.4, -0.7]  # breathing, frame px
ANKLE_Y, WAIST_Y = 174.0, 129.0


def key_green(path):
    """Premultiplied RGBA floats from art on flat green. The art has no green, so every green pixel counts, holes between the legs too."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    alpha = 1 - np.clip((g - np.maximum(r, b) - 12) / 60, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0  # ChatGPT leaves off-colour pixels at the image edge
    fg, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(fg), fg, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(np.isin(fg, 1 + np.where(sizes >= 400)[0]), iterations=3)
    g2 = np.minimum(g, np.maximum(r, b))
    return np.dstack([r * alpha, g2 * alpha, b * alpha, alpha * 255])


def split(pm, count):
    """(art, top row) of count boxes of connected art, left to right, each masked to its own blobs."""
    mask = pm[..., 3] > 128
    labels, n = ndimage.label(ndimage.binary_dilation(mask, iterations=2))
    objs = ndimage.find_objects(labels)
    sizes = ndimage.sum(mask, labels, range(1, n + 1))
    big = sorted([(o, i + 1) for i, (o, s) in enumerate(zip(objs, sizes)) if s > 0.15 * max(sizes)], key=lambda t: t[0][1].start)
    if len(big) != count:
        raise SystemExit(f"{len(big)} blobs, expected {count}")
    return [(pm[o] * (labels[o] == label)[..., None], o[0].start) for o, label in big]


def resize_pm(pm, scale):
    w, h = round(pm.shape[1] * scale), round(pm.shape[0] * scale)
    return np.clip(np.dstack([np.array(Image.fromarray(pm[..., c].astype(np.float32), "F").resize((w, h), Image.LANCZOS)) for c in range(4)]), 0, 255)


def to_rgba(pm):
    out = pm.copy()
    a = np.maximum(pm[..., 3:4], 1e-3) / 255
    out[..., :3] = np.clip(pm[..., :3] / a, 0, 255)
    out[pm[..., 3] < 2] = 0
    return out.round().astype(np.uint8)


def box(alpha, thr=128):
    ys, xs = np.where(alpha > thr)
    return (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1) if len(xs) else None


def place(part, y, x):
    cell = np.zeros((FRAME, FRAME, 4))
    sx0, sy0 = max(0, -x), max(0, -y)
    ex, ey = min(FRAME, x + part.shape[1]), min(FRAME, y + part.shape[0])
    cell[y + sy0:ey, x + sx0:ex] = part[sy0:ey - y, sx0:ex - x]
    return cell


def centre_x(part, lo=0.75):
    xs = np.where((part[int(part.shape[0] * lo):, :, 3] > 128).any(0))[0]
    return (xs[0] + xs[-1]) / 2


def lift_at(y):
    t = np.clip((ANKLE_Y - y) / (ANKLE_Y - WAIST_Y), 0, 1)
    return t * t * (3 - 2 * t)


def breathe(pm, f):
    y, x = np.mgrid[0:FRAME, 0:FRAME].astype(np.float64)
    src_y = y - LIFT[f] * lift_at(y)
    return np.clip(np.dstack([ndimage.map_coordinates(pm[..., c], [src_y, x], order=3, mode="constant") for c in range(4)]), 0, 255)


def grid_sheet(cells, path, marks=None, z=3):
    w = Image.new("RGB", (FRAME * z * len(cells), FRAME * z), (58, 62, 70))
    d = ImageDraw.Draw(w)
    for i, c in enumerate(cells):
        im = Image.fromarray(to_rgba(c), "RGBA").resize((FRAME * z, FRAME * z), Image.NEAREST)
        w.paste(im, (i * FRAME * z, 0), im)
        for v in range(0, FRAME, 24):
            col = (255, 255, 0) if v % 48 == 0 else (110, 110, 110)
            d.line([(i * FRAME * z + v * z, 0), (i * FRAME * z + v * z, FRAME * z)], fill=col)
            d.line([(i * FRAME * z, v * z), ((i + 1) * FRAME * z, v * z)], fill=col)
            if v % 48 == 0:
                d.text((i * FRAME * z + v * z + 2, 2), str(v), fill=(255, 255, 0))
                d.text((i * FRAME * z + 2, v * z + 2), str(v), fill=(255, 255, 0))
        for x, y, label, col in (marks or {}).get(i, []):
            px, py = i * FRAME * z + x * z, y * z
            d.ellipse((px - 6, py - 6, px + 6, py + 6), outline=col, width=2)
            d.text((px + 8, py - 6), label, fill=col)
    w.save(path)


def new_guid_copy(model, path):
    """A sheet Unity imports by itself comes out compressed and capped at 2048: copy the settings of one of the same kind, with a new guid."""
    if os.path.exists(path) or not os.path.exists(model):
        return
    lines = open(model, encoding="utf-8", newline="").read().split("\n")
    lines[1] = f"guid: {uuid.uuid4().hex}" + ("\r" if lines[1].endswith("\r") else "")
    open(path, "w", encoding="utf-8", newline="").write("\n".join(lines))

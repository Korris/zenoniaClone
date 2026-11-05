"""Helpers for Tools/build-wizard-attack-fx.py: green-screen keying of the ChatGPT sheet, cell split by cell index,
solid-disc fill of enclosed background, the spinning rune-disc core, Lanczos resize and Unity .meta cloning."""
import os, re, uuid
import numpy as np
from PIL import Image
from scipy import ndimage

DEEP_VIOLET = np.array([42.0, 28.0, 140.0])  # fill behind translucent disc interiors (#2A1C8C)


def key_green(path, min_blob=60):
    """Premultiplied RGBA floats (0..255) from art on flat #00FF00: alpha from the green excess over max(R, B),
    6 px image border forced empty (sheet edge noise), blobs under `min_blob` px dropped (specks)."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    alpha = 1 - np.clip((g - np.maximum(r, b) - 12) / 60, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    lab, n = ndimage.label(alpha > 0.5)
    if n:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(np.isin(lab, 1 + np.where(sizes >= min_blob)[0]), iterations=3)
    g2 = np.minimum(g, np.maximum(r, b))  # strip the green spill from the edge pixels
    teal = (b > r + 30) & (g2 > r + 20)   # background glow painted into blue shading reads cyan: pull it back to blue-violet
    g2 = np.where(teal, r + (g2 - r) * 0.35, g2)
    return np.dstack([r * alpha, g2 * alpha, b * alpha, alpha * 255])


def to_image(pm):
    """Premultiplied float RGBA -> PIL RGBA (straight alpha)."""
    a = np.maximum(pm[..., 3:4], 1e-6)
    rgb = np.clip(pm[..., :3] / a * 255, 0, 255)
    return Image.fromarray(np.dstack([rgb, np.clip(pm[..., 3], 0, 255)]).round().astype(np.uint8), "RGBA")


def spans(proj, gap=6):
    """Ink spans of a 1-D projection, merged across gaps <= gap px."""
    idx = np.flatnonzero(proj)
    if not len(idx): return []
    out, s, p = [], idx[0], idx[0]
    for i in idx[1:]:
        if i - p > gap: out.append((s, p + 1)); s = i
        p = i
    out.append((s, p + 1))
    return out


def cells(pm, rows=3, cols=4):
    """Boxes (y0, y1, x0, x1) of the 12 frames: rows by horizontal gutters, columns by assigning every ink span of a row to
    the equal cell its centre falls in (a frame drawn as several pieces, like scattered fragments, stays one frame)."""
    ink = pm[..., 3] > 128
    h, w = ink.shape
    out = []
    for (y0, y1) in spans(ink.sum(1)):
        boxes = {}
        for (x0, x1) in spans(ink[y0:y1].sum(0)):
            c = min(cols - 1, int(((x0 + x1) / 2) / (w / cols)))
            b = boxes.get(c)
            boxes[c] = (min(b[0], x0), max(b[1], x1)) if b else (x0, x1)
        if len(boxes) != cols: raise SystemExit("row %d-%d: %d frames found, want %d: %s" % (y0, y1, len(boxes), cols, boxes))
        for c in range(cols):
            x0, x1 = boxes[c]
            ys = np.where(ink[y0:y1, x0:x1].any(1))[0]
            out.append((y0 + ys[0], y0 + ys[-1] + 1, x0, x1))
    if len(out) != rows * cols: raise SystemExit("%d rows found, want %d" % (len(out) // cols, rows))
    return out


def fill_enclosed(pm):
    """Background that is not reachable from the frame border is disc interior painted translucent over the green:
    composite it over deep violet so the disc is solid (rule: shapes must be opaque)."""
    a = pm[..., 3] / 255.0
    outside = ndimage.binary_fill_holes(a > 0.5) & ~(a > 0.5)  # holes = enclosed, not-opaque pixels
    enclosed = outside & (a < 0.999)
    bg = (1 - a)[..., None] * DEEP_VIOLET
    out = pm.copy()
    out[enclosed, :3] = pm[enclosed, :3] + bg[enclosed]
    out[enclosed, 3] = 255
    return out


def outline(pm, px, colour=(90.0, 59.0, 214.0)):
    """Saturated violet rim `px` wide under a frame drawn as thin white lines, so it still reads on snow (no additive white on white)."""
    a = pm[..., 3] / 255.0
    dist = ndimage.distance_transform_edt(a < 0.5)
    rim = np.clip(px + 0.5 - dist, 0, 1) * (1 - a)  # premultiplied rim alpha under the art
    out = pm.copy()
    out[..., :3] += rim[..., None] * np.array(colour)
    out[..., 3] += rim * 255
    return out


def disc_centre_radius(pm):
    """Centre of the white-hot core and the outer radius of the rune ring, measured on the trail-free right side."""
    a = pm[..., 3] > 200
    rgb = pm[..., :3] / np.maximum(pm[..., 3:4], 1) * 255
    hot = a & (rgb.min(axis=2) > 225)
    ys, xs = np.nonzero(hot)
    cy, cx = ys.mean(), xs.mean()
    radii = []
    for deg in list(range(15, 76, 3)) + list(range(-75, -14, 3)):
        t = np.deg2rad(deg)
        rr = np.arange(0, 400)
        px, py = (cx + rr * np.cos(t)).round().astype(int), (cy - rr * np.sin(t)).round().astype(int)
        ok = (px >= 0) & (px < a.shape[1]) & (py >= 0) & (py < a.shape[0])
        hit = rr[ok][a[py[ok], px[ok]]]
        if len(hit): radii.append(hit.max())
    return cx, cy, float(np.median(radii))


def spin_frames(pm, count, step_deg):
    """`count` frames from one projectile frame: the rune ring + interior (a circle just inside the ring's outer edge) rotates
    `step_deg` more each frame, the four bezel spikes and the crescent trail stay put."""
    cx, cy, R = disc_centre_radius(pm)
    img = to_image(pm)
    h, w = pm.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    core = np.clip(R - 1.5 - np.hypot(xx - cx, yy - cy), 0, 1)[..., None]  # feathered 1 px
    base = np.asarray(img).astype(np.float64)
    out = []
    for k in range(count):
        rot = np.asarray(img.rotate(-k * step_deg, Image.BICUBIC, center=(cx, cy))).astype(np.float64)
        out.append(Image.fromarray(np.clip(rot * core + base * (1 - core), 0, 255).round().astype(np.uint8), "RGBA"))
    return out, (cx, cy, R)


def resize_rgba(img, scale):
    """Lanczos resize through premultiplied alpha (no dark fringe)."""
    if abs(scale - 1) < 1e-6: return img
    a = np.asarray(img).astype(np.float64)
    pm = np.dstack([a[..., :3] * a[..., 3:4] / 255, a[..., 3]])
    w, h = max(1, round(img.width * scale)), max(1, round(img.height * scale))
    ch = [Image.fromarray(pm[..., i]).resize((w, h), Image.LANCZOS) for i in range(4)]
    return to_image(np.dstack([np.asarray(c, dtype=np.float64) for c in ch]))


def trim(img, pad=0):
    """Crop to the opaque-ish bounding box, keeping `pad` px around it."""
    a = np.asarray(img)[..., 3] > 8
    ys, xs = np.nonzero(a)
    x0, x1 = max(0, xs.min() - pad), min(img.width, xs.max() + 1 + pad)
    y0, y1 = max(0, ys.min() - pad), min(img.height, ys.max() + 1 + pad)
    return img.crop((x0, y0, x1, y1)), (x0, y0)


def clone_meta(src_meta, dst_meta):
    """Copy a Unity .meta (import settings) under a fresh guid; keeps an existing guid when the file is already there."""
    text = open(src_meta, encoding="utf-8").read()
    guid = uuid.uuid4().hex
    if os.path.exists(dst_meta):
        m = re.search(r"^guid: ([0-9a-f]{32})", open(dst_meta, encoding="utf-8").read(), re.M)
        if m: guid = m.group(1)
    text = re.sub(r"^guid: [0-9a-f]{32}", "guid: " + guid, text, count=1, flags=re.M)
    with open(dst_meta, "w", encoding="utf-8", newline="\n") as f: f.write(text)

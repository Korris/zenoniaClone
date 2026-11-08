"""Cuts the hero-select screen art. Run from anywhere: python Tools/build-hero-select-art.py (Python 3 + Pillow + numpy + scipy).

Reads the ChatGPT originals in Assets/ArtLibrary/custom/hero-select/ and writes Assets/Resources/Sprites/ui/hero-select/.
The art copies the mock Assets/ArtLibrary/custom/2b14c1e0-73f8-4c0b-b278-eb1c83c76e34.png piece by piece; ref/ holds the crops
that were uploaded with each prompt.

Keying (docs/sprite-rules.md, "Quy tắc art lấy từ ChatGPT" and the UI rows of "Sổ lỗi"):
- None of these sheets uses its key colour inside the art (green on magenta only, purple on green only), so every key-coloured
  area counts as background, holes between arm and body included. Cleric is the only hero on magenta.
- Opacity comes from how much of the key colour a pixel still holds, and only in a thin band next to the background, so a black
  outline is never eaten. Heroes on green also treat a strong green excess anywhere, for the glow around blue flames.
- Heroes are placed by their boots, not by their bounds: a sword or a wing must not move the body.
"""
import json
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "Assets/ArtLibrary/custom/hero-select"
OUT = ROOT / "Assets/Resources/Sprites/ui/hero-select"

GREEN, MAGENTA = "green", "magenta"
BAND_PX = 6
TRIM_MARGIN = 6
HERO_SIZE = 768
PORTRAIT_SIZE = 256
PORTRAIT_EYE_GAP = 0.25  # distance between the eyes as a fraction of the portrait
PORTRAIT_EYE_ROW = 0.56  # height of the eyes in the portrait, from the top
# Output row of the soles, and the eye-to-sole distance every hero is scaled to. Cleric's wing and Assassin's hair set the limit.
FEET_ROW = 744
EYE_TO_FEET = 388

# key, boot column window for feet and centre, eye row (source pixels), and the face for the portrait or None:
# (x between the eyes, eye row, distance between the eyes), read off the finished 768 px hero. Portraits are framed by it, so every head is the same size.
HEROES = {
    "mechanic": dict(key=GREEN, boots=(435, 820), eye=480, face=(381, 356, 97)),
    "wizard": dict(key=GREEN, boots=(415, 870), eye=465, face=(416, 400, 64)),
    "warrior": dict(key=GREEN, boots=(305, 830), eye=415, face=(446, 352, 85)),
    "assassin": dict(key=GREEN, boots=(285, 990), eye=570, face=(426, 360, 100)),
    "boxing": dict(key=GREEN, boots=(235, 1020), eye=490, face=None),
    "cleric": dict(key=GREEN, boots=(590, 800), eye=555, face=(379, 356, 90)),
}
# Each banner is its own original, copied from its own crop of the mock, so each keeps that crop's proportions.
BANNER_KEYS = {"mechanic": GREEN, "wizard": GREEN, "warrior": GREEN, "assassin": GREEN, "boxing": GREEN, "cleric": MAGENTA}
NAME_ROWS = ["name-mechanic", "name-wizard", "name-warrior", "name-assassin", "name-boxing", "name-cleric"]
LABEL_ROWS = ["label-start", "label-continue", "label-switch", "label-playing"]
MISC_ROWS = ["arrow-left", "slider", "slider-knob"]
STATS = ["stat-atk", "stat-def", "stat-hp", "stat-mp", "stat-crit"]
BANNER_H = 768

# Pedestal original: rows of the far and near rim of the top surface, and the plaque's inner rectangle (x0, y0, x1, y1).
PED_TOP_ROWS = (165, 262)
PED_PLAQUE = (466, 344, 1340, 497)


def excess(rgb, key):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    if key == GREEN:
        return g - np.maximum(r, b)
    return np.minimum(r, b) - g


def key_out(path, key, glow=False):
    """Returns straight-alpha RGBA floats in 0..255."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    h, w = rgb.shape[:2]
    corners = np.concatenate([rgb[:20, :20].reshape(-1, 3), rgb[:20, -20:].reshape(-1, 3), rgb[-20:, :20].reshape(-1, 3), rgb[-20:, -20:].reshape(-1, 3)])
    key_rgb = np.median(corners, axis=0)
    e = excess(rgb, key)
    e_bg = float(excess(key_rgb[None, None, :], key)[0, 0])
    lo, hi = 0.12 * e_bg, 0.85 * e_bg

    core = e >= 0.6 * e_bg
    band = ndimage.binary_dilation(core, structure=ndimage.generate_binary_structure(2, 2), iterations=BAND_PX)
    if glow:
        band |= e > lo

    alpha = np.ones((h, w), np.float32)
    alpha[band] = 1 - np.clip((e[band] - lo) / (hi - lo), 0, 1)
    a = alpha[..., None]
    out = np.where(a > 0.02, (rgb - (1 - a) * key_rgb) / np.maximum(a, 0.02), 0)
    out = np.clip(out, 0, 255)
    # Whatever key tint is left in the band goes too.
    left = np.maximum(excess(out, key), 0)
    left[~band] = 0
    if key == GREEN:
        out[..., 1] -= left
    else:
        out[..., 0] -= left
        out[..., 2] -= left

    # Art never touches the image edge, so anything there is a resampling artefact that would stretch the trim.
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    # Specks left by compression noise in the flat background.
    labels, n = ndimage.label(alpha > 0.05, structure=np.ones((3, 3)))
    if n:
        sizes = ndimage.sum_labels(np.ones_like(labels), labels, index=np.arange(1, n + 1))
        drop = np.isin(labels, np.flatnonzero(sizes < 30) + 1)
        alpha[drop] = 0
    alpha[alpha < 0.02] = 0
    return np.dstack([out, alpha * 255])


def to_image(rgba):
    return Image.fromarray(np.clip(rgba + 0.5, 0, 255).astype(np.uint8))


def trim(rgba, margin=TRIM_MARGIN):
    ys, xs = np.nonzero(rgba[..., 3] > 8)
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    part = rgba[y0:y1, x0:x1]
    if margin == 0:
        return part
    canvas = np.zeros((part.shape[0] + margin * 2, part.shape[1] + margin * 2, 4), np.float32)
    canvas[margin:-margin, margin:-margin] = part
    return canvas


def resize(rgba, size):
    """Premultiplied, so the transparent black around the art does not bleed into the edge."""
    a = rgba[..., 3:] / 255
    pre = np.dstack([rgba[..., :3] * a, rgba[..., 3]])
    bands = [np.asarray(Image.fromarray(pre[..., i]).resize(size, Image.LANCZOS)) for i in range(4)]
    res = np.dstack(bands)
    al = np.clip(res[..., 3], 0, 255)
    rgb = np.where(al[..., None] > 1, res[..., :3] / np.maximum(al[..., None] / 255, 1e-3), 0)
    return np.dstack([np.clip(rgb, 0, 255), al])


def fit_width(rgba, width):
    return resize(rgba, (width, round(rgba.shape[0] * width / rgba.shape[1])))


def fit_height(rgba, height):
    return resize(rgba, (round(rgba.shape[1] * height / rgba.shape[0]), height))


def boots(rgba, window):
    """Sole row and the middle of the boots, in the array's own pixels."""
    x0, x1 = window
    solid = rgba[..., 3] > 128
    rows = np.flatnonzero(solid[:, x0:x1].any(axis=1))
    feet = int(rows.max())
    cols = np.flatnonzero(solid[feet - 40: feet + 1, x0:x1].any(axis=0)) + x0
    return feet, (int(cols.min()) + int(cols.max()) + 1) / 2


def split(rgba, count, axis):
    """Cuts a sheet into its pieces at the empty rows (axis 1) or columns (axis 0)."""
    used = (rgba[..., 3] > 8).any(axis=axis)
    labels, n = ndimage.label(used)
    out = []
    for i in range(1, n + 1):
        idx = np.flatnonzero(labels == i)
        # A thin run is a speck the noise filter let through, not a piece.
        if len(idx) >= 8:
            out.append(rgba[idx[0]: idx[-1] + 1] if axis == 1 else rgba[:, idx[0]: idx[-1] + 1])
    if len(out) != count:
        raise SystemExit(f"expected {count} pieces, found {len(out)}")
    return out


def save(rgba, name, sizes):
    to_image(rgba).save(OUT / f"{name}.png")
    sizes.append((name, rgba.shape[1], rgba.shape[0]))


def build_hero(name, cfg, sizes):
    src = key_out(SRC / f"{name}.png", cfg["key"], glow=cfg["key"] == GREEN)
    if cfg["key"] == MAGENTA:
        # ChatGPT leaves pink specks on staff, hair ornament and spirits even when told not to. Pink turns peach; pale lilac shading stays.
        pink = (src[..., 3] > 0) & (excess(src[..., :3], MAGENTA) > 15)
        src[..., 2][pink] = np.minimum(src[..., 2], src[..., 1] * 0.8)[pink]
    feet, _ = boots(src, cfg["boots"])
    scale = EYE_TO_FEET / (feet - cfg["eye"])

    # Scale at a whole-pixel source size, then place on the square so soles and boot centre land on fixed pixels.
    w = round(src.shape[1] * scale)
    h = round(src.shape[0] * scale)
    scaled = resize(src, (w, h))
    window = tuple(round(v * w / src.shape[1]) for v in cfg["boots"])
    feet_s, mid_s = boots(scaled, window)
    dx, dy = round(HERO_SIZE / 2 - mid_s), FEET_ROW - feet_s

    canvas = np.zeros((HERO_SIZE, HERO_SIZE, 4), np.float32)
    x0, y0 = max(dx, 0), max(dy, 0)
    x1, y1 = min(dx + w, HERO_SIZE), min(dy + h, HERO_SIZE)
    canvas[y0:y1, x0:x1] = scaled[y0 - dy: y1 - dy, x0 - dx: x1 - dx]
    lost = float(scaled[..., 3].sum() - canvas[..., 3].sum())
    save(canvas, f"hero-{name}", sizes)

    if cfg["face"]:
        ex, ey, gap = cfg["face"]
        side = gap / PORTRAIT_EYE_GAP
        left, top = ex - side / 2, ey - PORTRAIT_EYE_ROW * side
        cx, cy, cs = round((left - dx) / scale), round((top - dy) / scale), round(side / scale)
        box = np.zeros((cs, cs, 4), np.float32)  # the framing may reach past the source's edge
        sx0, sy0, sx1, sy1 = max(cx, 0), max(cy, 0), min(cx + cs, src.shape[1]), min(cy + cs, src.shape[0])
        box[sy0 - cy: sy1 - cy, sx0 - cx: sx1 - cx] = src[sy0: sy1, sx0: sx1]
        save(resize(box, (PORTRAIT_SIZE, PORTRAIT_SIZE)), f"portrait-{name}", sizes)

    final_feet, final_mid = boots(canvas, (window[0] + dx, window[1] + dx))
    ys, xs = np.nonzero(canvas[..., 3] > 8)
    return dict(
        scale=round(scale, 3),
        feet_row=final_feet,
        centre_x=final_mid,
        bounds=[int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())],
        cut_off_alpha=round(lost / 255),
        key_left=int(((canvas[..., 3] > 128) & (excess(canvas[..., :3], cfg["key"]) > 60)).sum()),
    )


def keep_largest(rgba):
    """Drops everything but the biggest piece, for a sheet that holds one object."""
    labels, n = ndimage.label(rgba[..., 3] > 8, structure=np.ones((3, 3)))
    sizes = ndimage.sum_labels(np.ones_like(labels), labels, index=np.arange(1, n + 1))
    out = rgba.copy()
    out[labels != int(np.argmax(sizes)) + 1] = 0
    return out


def slice_border(rgba):
    """How far the corner ornaments reach from the panel edge, ignoring the straight border lines."""
    gold = rgba[..., 0] > 110
    h, w = gold.shape
    top_left = gold[: h // 3, : w // 3]
    lines_r = top_left.mean(axis=1) > 0.5
    lines_c = top_left.mean(axis=0) > 0.5
    clean = top_left.copy()
    clean[lines_r, :] = False
    clean[:, lines_c] = False
    ys, xs = np.nonzero(clean)
    return int(xs.max()) + 1, int(ys.max()) + 1


def build_banners(sizes):
    for name, key in BANNER_KEYS.items():
        save(fit_height(trim(keep_largest(key_out(SRC / f"banner-{name}.png", key))), BANNER_H), f"banner-{name}", sizes)


def build_underline_glow(sizes):
    """Blurred white copy of name-underline, 48 px wider on every side: the game tints it and adds it as light around the line."""
    from PIL import Image, ImageFilter
    line = Image.open(OUT / "name-underline.png").convert("RGBA").getchannel("A")
    pad = 48
    canvas = Image.new("L", (line.width + pad * 2, line.height + pad * 2), 0)
    canvas.paste(line, (pad, pad))
    tight = np.asarray(canvas.filter(ImageFilter.GaussianBlur(7)), dtype=np.float32) / 255
    wide = np.asarray(canvas.filter(ImageFilter.GaussianBlur(20)), dtype=np.float32) / 255
    alpha = np.clip(tight * 2.4 + wide * 2.2, 0, 1)
    rgba = np.full(alpha.shape + (4,), 255, dtype=np.uint8)
    rgba[..., 3] = (alpha * 255).astype(np.uint8)
    save(rgba, "name-underline-glow", sizes)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sizes = []
    report = {}

    for name in ("bg", "preview-ground"):
        im = Image.open(SRC / f"{name}.png").convert("RGB")
        im.save(OUT / f"{name}.png")
        sizes.append((name, *im.size))

    # Pedestal.
    raw = keep_largest(key_out(SRC / "pedestal.png", MAGENTA))
    ys, xs = np.nonzero(raw[..., 3] > 8)
    top_src = int(ys.min())
    ped = trim(raw)
    k = 1024 / ped.shape[1]
    ped = fit_width(ped, 1024)
    save(ped, "pedestal", sizes)
    left_src = int(xs.min())
    x0, y0, x1, y1 = [(v - o + TRIM_MARGIN) * k for v, o in zip(PED_PLAQUE, (left_src, top_src, left_src, top_src))]
    far = (PED_TOP_ROWS[0] - top_src + TRIM_MARGIN) * k
    near = (PED_TOP_ROWS[1] - top_src + TRIM_MARGIN) * k
    report["pedestal"] = dict(
        plaque_centre=[round((x0 + x1) / 2), round((y0 + y1) / 2)],
        plaque_size=[round(x1 - x0), round(y1 - y0)],
        top_surface_rows=[round(far), round(near)],
        feet_stand_row=round((far + near) / 2),
    )

    build_banners(sizes)

    # Text and labels.
    for name, part in zip(NAME_ROWS, split(key_out(SRC / "names.png", MAGENTA), len(NAME_ROWS), 1)):
        save(trim(part), name, sizes)
    for name, part in zip(LABEL_ROWS, split(key_out(SRC / "labels.png", MAGENTA), len(LABEL_ROWS), 1)):
        save(trim(part), name, sizes)
    save(fit_width(trim(key_out(SRC / "title.png", MAGENTA)), 1024), "title", sizes)
    save(fit_width(trim(key_out(SRC / "label-skill-preview.png", MAGENTA)), 1024), "label-skill-preview", sizes)

    # UI pieces.
    save(fit_width(trim(key_out(SRC / "btn-start.png", MAGENTA)), 1024), "btn-start", sizes)
    panel = resize(trim(key_out(SRC / "panel-frame.png", MAGENTA), margin=0), (512, 512))
    panel[..., 3] = np.where(panel[..., 3] > 128, 255, panel[..., 3])
    save(panel, "panel-frame", sizes)
    bx, by = slice_border(panel)
    report["panel-frame"] = dict(ornament_reach=[bx, by], safe_slice_border=max(bx, by) + 4)
    save(resize(trim(key_out(SRC / "icon-frame.png", MAGENTA)), (256, 256)), "icon-frame", sizes)
    save(fit_width(trim(key_out(SRC / "name-underline.png", MAGENTA)), 1024), "name-underline", sizes)
    build_underline_glow(sizes)
    misc = split(key_out(SRC / "arrow-slider-knob.png", MAGENTA), len(MISC_ROWS), 1)
    save(trim(misc[0]), "arrow-left", sizes)
    save(fit_width(trim(misc[1]), 1024), "slider", sizes)
    save(trim(misc[2]), "slider-knob", sizes)
    for name, part in zip(STATS, split(key_out(SRC / "stat-icons.png", MAGENTA), len(STATS), 0)):
        save(fit_height(trim(part), 128), name, sizes)
    save(resize(trim(key_out(SRC / "play-button.png", MAGENTA)), (256, 256)), "play-button", sizes)

    report["heroes"] = {name: build_hero(name, cfg, sizes) for name, cfg in HEROES.items()}

    for row in sizes:
        print(*row)
    print(json.dumps(report))


if __name__ == "__main__":
    main()

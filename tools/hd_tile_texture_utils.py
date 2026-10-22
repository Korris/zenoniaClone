"""Texture helpers for tools/build-hd-tiles.py: make hand-painted ground cells seamless.

The raw ChatGPT tile sheets are illustrations of a grid: every cell has a dark panel border
baked in, neighbouring cells are separate paintings (no edge continuity) and sibling variants
differ in overall brightness. Tiled in-game that reads as a conspicuous square lattice. These
helpers fix that at build time (not at render time):

- split_cells: even grid split with an inset margin that drops the painted panel border.
- equalize_means: per-channel gain so sibling variants share one mean colour (no checkerboard).
- heal_seams: blends a feathered strip of clean interior texture over every cell boundary
  (including the wrap-around edge) so a cell tiles against itself / its macro neighbours.
- material_alpha: soft "is this pixel dirt/water/stone (opaque) or grass (transparent)" mask
  from hue, so painted transition cells become RGBA overlays over the seamless base grass.
- flower_decals: lifts the painted flowers off a grass cell so they can be stamped on the
  seamless plain grass instead of using a separately painted (non-matching) grass variant.
"""
import numpy as np
from PIL import Image, ImageFilter


def bounds(total, count):
    """`count` evenly-spaced cell boundaries over [0, total]."""
    return [round(i * total / count) for i in range(count + 1)]


def split_cells(img, cols, rows, margin_frac=0.0):
    """Even grid split -> dict[(col, row)] = PIL cell, inset by margin_frac of the cell size on every
    side (drops the baked-in panel border + its shadow). Cells are NOT resized here."""
    cb, rb = bounds(img.width, cols), bounds(img.height, rows)
    cells = {}
    for r in range(rows):
        for c in range(cols):
            mx = round((cb[c + 1] - cb[c]) * margin_frac)
            my = round((rb[r + 1] - rb[r]) * margin_frac)
            cells[(c, r)] = img.crop((cb[c] + mx, rb[r] + my, cb[c + 1] - mx, rb[r + 1] - my))
    return cells


def to_f32(img):
    return np.asarray(img.convert('RGBA')).astype(np.float32)


def to_img(arr):
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), 'RGBA')


def equalize_means(arrs, strength=1.0):
    """Scale each RGBA array so its mean RGB matches the group mean (alpha untouched).
    strength 1 = full match, 0 = unchanged."""
    means = [a[..., :3].reshape(-1, 3).mean(axis=0) for a in arrs]
    target = np.mean(means, axis=0)
    out = []
    for a, m in zip(arrs, means):
        gain = 1 + (target / np.maximum(m, 1) - 1) * strength
        b = a.copy()
        b[..., :3] = np.clip(a[..., :3] * gain, 0, 255)
        out.append(b)
    return out


def _heal_axis(arr, seams, feather, axis):
    """Heal seams along one axis (axis=1 -> vertical seams at x positions, axis=0 -> horizontal
    seams at y positions). Each seam (incl. the wrap at 0) is rolled to the middle, then a strip
    of clean texture taken half a cell away is blended over it with a triangular weight."""
    n = arr.shape[axis]
    f = feather
    w = 1 - np.abs(np.arange(-f, f + 1)) / f  # 1 at the seam line, exactly 0 at BOTH strip ends
    shape = [1, 1, 1]
    shape[axis] = 2 * f + 1
    w = w.reshape(shape)
    cell = n // max(1, len(seams))  # distance between seams
    for seam in seams:
        shift = n // 2 - seam
        rolled = np.roll(arr, shift, axis=axis)
        mid = n // 2
        src = mid - cell // 2  # interior of the previous cell: never straddles another seam
        dst_slice = [slice(None)] * 3
        dst_slice[axis] = slice(mid - f, mid + f + 1)
        # wrap-around take: for a single-cell sheet the clean strip sits across the (healed) wrap
        clean = np.take(rolled, np.arange(src - f, src + f + 1), axis=axis, mode='wrap')
        rolled[tuple(dst_slice)] = rolled[tuple(dst_slice)] * (1 - w) + clean * w
        arr = np.roll(rolled, -shift, axis=axis)
    return arr


def heal_seams(arr, cell, feather=10, horizontal=True, vertical=True):
    """Make an RGBA array seamless on a `cell` grid: every internal boundary and the wrap edge
    gets healed. `horizontal` heals seams crossed when tiling left/right (vertical lines),
    `vertical` those crossed when tiling up/down (horizontal lines)."""
    h, w = arr.shape[:2]
    if horizontal:
        arr = _heal_axis(arr, list(range(0, w, cell)), feather, axis=1)
    if vertical:
        arr = _heal_axis(arr, list(range(0, h, cell)), feather, axis=0)
    return arr


def material_alpha(img, soft=0.25, blur=3.0):
    """Soft alpha (0..255 float array) = how much a pixel is NOT grass. Grass is green-dominant
    (G clearly above R, and not blue like water); dirt, stone, rock and water keep alpha 1.
    `soft` is the G/R excess over 1.0 at which a pixel counts as fully grass."""
    a = to_f32(img)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    greenness = g / np.maximum(r, 1) - 1
    greenness = np.where(b >= g * 0.95, 0, greenness)  # blue/grey/white are never grass
    alpha = np.clip(1 - greenness / soft, 0, 1) * 255
    alpha_img = Image.fromarray(alpha.astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(blur))
    return np.asarray(alpha_img).astype(np.float32)


def open_alpha(arr, size=5):
    """Morphological opening (min then max filter) on an RGBA float array's alpha: drops isolated
    flecks (painted flowers the hue mask kept) without moving the main dirt/water silhouette."""
    a = Image.fromarray(np.clip(arr[..., 3], 0, 255).astype(np.uint8), 'L')
    a = a.filter(ImageFilter.MinFilter(size)).filter(ImageFilter.MaxFilter(size))
    out = arr.copy()
    out[..., 3] = np.asarray(a).astype(np.float32)
    return out


def with_alpha(img, alpha):
    """RGB image + float alpha array -> RGBA float array."""
    a = to_f32(img)
    a[..., 3] = alpha
    return a


def premultiplied_resize(arr, size):
    """Premultiplied-alpha LANCZOS resize of an RGBA float array -> RGBA float array."""
    alpha = arr[..., 3:4] / 255.0
    premult = np.concatenate([arr[..., :3] * alpha, arr[..., 3:4]], axis=2)
    small = Image.fromarray(np.clip(premult, 0, 255).astype(np.uint8), 'RGBA').resize(size, Image.LANCZOS)
    s = np.asarray(small).astype(np.float32)
    a = s[..., 3:4]
    rgb = np.where(a > 0, s[..., :3] / np.maximum(a / 255.0, 1e-6), 0)
    return np.clip(np.concatenate([rgb, a], axis=2), 0, 255)


def flower_decals(img, blur=1.2):
    """RGBA float array of just the painted flowers (white / yellow / pink blobs) on a grass cell:
    everything green-dominant becomes transparent, the rest keeps a slightly feathered alpha."""
    a = to_f32(img)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    not_green = (r >= g * 0.97) | (np.minimum(np.minimum(r, g), b) > 165)
    mask = Image.fromarray((not_green * 255).astype(np.uint8), 'L')
    mask = mask.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(blur))
    a[..., 3] = np.asarray(mask).astype(np.float32)
    return a


def composite(base, overlay):
    """Alpha-composite RGBA float `overlay` over RGBA float `base` (same size)."""
    oa = overlay[..., 3:4] / 255.0
    out = base.copy()
    out[..., :3] = overlay[..., :3] * oa + base[..., :3] * (1 - oa)
    out[..., 3] = np.maximum(base[..., 3], overlay[..., 3])
    return out


def macro_from_cells(cells, cols, rows, cell, feather=10):
    """Lay `cells` (list of RGBA float arrays, cell x cell) out on a cols x rows grid and heal every
    seam so the whole sheet tiles with itself. Returns the RGBA float macro array."""
    sheet = np.zeros((rows * cell, cols * cell, 4), np.float32)
    for i, c in enumerate(cells):
        x, y = (i % cols) * cell, (i // cols) * cell
        sheet[y:y + cell, x:x + cell] = c
    return heal_seams(sheet, cell, feather)


def flips(arr):
    """[arr, flipX, flipY, flipXY] -- 4 pseudo-variants of a single seamless cell for a 2x2 macro."""
    return [arr, arr[:, ::-1], arr[::-1, :], arr[::-1, ::-1]]


def inner_corner_masks(top_alpha, left_alpha):
    """Grass wedge alphas for concave (inner) corners of an auto-tile block, from the dirt alpha of
    the T and L edge cells: grass only where BOTH the top band and the left band are grass.
    Returns 4 RGBA float arrays [TL, TR, BL, BR] (white RGB, alpha = wedge)."""
    cell = top_alpha.shape[0]
    grass_top = 1 - top_alpha / 255.0
    grass_left = 1 - left_alpha / 255.0
    tl = grass_top * grass_left
    wedges = [tl, tl[:, ::-1], tl[::-1, :], tl[::-1, ::-1]]
    out = []
    for wdg in wedges:
        a = np.full((cell, cell, 4), 255, np.float32)
        a[..., 3] = wdg * 255
        out.append(a)
    return out

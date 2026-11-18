"""Shared core of the layered hero recolour (docs/character-appearance-layered-spec.md, "Quy tac to mau").

Source art painted by the colour rules: background flat #00FF00, outfit only in magenta #FF8CFF / #FF00FF / #A000A0, hair (own layer)
only in 4 greys #242424 / #555555 / #999999 / #E0E0E0. ChatGPT never hits the exact hexes, so every test here is hue / chroma based.

to_game(): keyed RGBA with the outfit turned into neutral grey (#555555..#E0E0E0 by its lightness) and a mask RGBA (R = hair, G = outfit).
ramp(): the same ramp as Assets/Resources/Shaders/HeroRecolor.shader (change both together); recolour() applies it for previews.
"""
import numpy as np

OUTFIT_HUE = (285.0, 320.0)   # degrees: #FF00FF is 300; skin (~25), blush (orange ~20) and leather (~28) are far away
OUTFIT_SAT = 0.30             # HSV saturation above this (the spec light tone #FF8CFF itself is only 0.45; skin ~0.22 is out by hue anyway)
OUTFIT_VAL = 0.18             # and not near-black (an anti-aliased outline pixel)
HAIR_CHROMA = 30              # max-min channel spread (0-255) still grey: same rule as HairDye.cs / the shader's _HairAll
# magenta lightness ((max+min)/2) -> grey of the game sheet: #A000A0 (0.31) -> #555555, #FF00FF (0.5) -> #999999, #FF8CFF (0.77) -> #E0E0E0
OUTFIT_L_IN = [0.0, 0.31, 0.50, 0.775, 1.0]
OUTFIT_L_OUT = [0.08, 0.333, 0.60, 0.878, 0.97]


def hsv(rgb):
    """rgb float array (..., 3) in 0..1 -> hue degrees, saturation, value."""
    mx = rgb.max(-1)
    mn = rgb.min(-1)
    d = mx - mn
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    h = np.zeros_like(mx)
    nz = d > 1e-6
    rm = nz & (mx == r)
    gm = nz & (mx == g) & ~rm
    bm = nz & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    s = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0)
    return h * 60.0, s, mx


def key_green(img):
    """RGBA uint8 from an RGB(A) image on #00FF00: green-dominant pixels become clear, the green fringe on edges is removed (despill)."""
    a = np.asarray(img.convert('RGBA')).astype(np.float32)
    rgb = a[..., :3]
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    excess = g - np.maximum(r, b)                      # 255 on pure key, ~0 on art
    alpha = np.clip(1.0 - (excess - 40.0) / 120.0, 0, 1) * (a[..., 3] / 255.0)
    rgb[..., 1] = np.minimum(g, np.maximum(r, b) + 12)  # despill: no pixel greener than its other channels
    out = np.dstack([rgb, alpha * 255.0])
    out[alpha <= 0.02] = 0
    return out.clip(0, 255).astype(np.uint8)


def outfit_mask(rgba):
    """Bool mask of magenta outfit pixels (hue tolerant)."""
    rgb = rgba[..., :3].astype(np.float32) / 255.0
    h, s, v = hsv(rgb)
    return (rgba[..., 3] > 8) & (h >= OUTFIT_HUE[0]) & (h <= OUTFIT_HUE[1]) & (s > OUTFIT_SAT) & (v > OUTFIT_VAL)


def hair_mask(rgba):
    """Bool mask of grey hair pixels (on a hair layer: coloured accents such as goggles or a ribbon stay out)."""
    c = rgba[..., :3].astype(np.int32)
    return (rgba[..., 3] > 8) & (c.max(-1) - c.min(-1) <= HAIR_CHROMA)


def to_game(rgba, hair_layer=False):
    """(sheet RGBA, mask RGBA). Outfit pixels -> neutral grey by lightness; hair layer: grey pixels -> mask R (kept as painted)."""
    out = rgba.copy()
    mask = np.zeros_like(rgba)
    if hair_layer:
        m = hair_mask(rgba)
        mask[..., 0] = np.where(m, 255, 0)
    else:
        m = outfit_mask(rgba)
        rgb = rgba[..., :3].astype(np.float32) / 255.0
        light = (rgb.max(-1) + rgb.min(-1)) / 2.0
        grey = np.interp(light, OUTFIT_L_IN, OUTFIT_L_OUT) * 255.0
        for ch in range(3):
            out[..., ch] = np.where(m, grey, out[..., ch]).astype(np.uint8)
        mask[..., 1] = np.where(m, 255, 0)
    mask[..., 3] = 255
    return out, mask


def ramp(l, c):
    """Grey lightness l (array 0..1) + base colour c (3 floats 0..1) -> rgb (..., 3). Mirrors HeroRecolor.shader Ramp()."""
    c = np.asarray(c, np.float32)
    k0, k1 = c * 0.12, c * 0.24
    k2 = c * 0.62 * 0.7 + c * c * 0.9 * 0.3
    k4 = c + (1 - c) * 0.38
    k5 = c + (1 - c) * 0.65
    xs = [0.0, 0.14, 0.33, 0.60, 0.88, 1.0]
    ks = [k0, k1, k2, c, k4, k5]
    return np.stack([np.interp(l, xs, [k[i] for k in ks]) for i in range(3)], -1)


def recolour(sheet, mask, hair_rgb=None, outfit_rgb=None, hair_all=False):
    """What the shader draws: sheet RGBA uint8 + mask -> RGBA uint8 with the chosen colours (hex strings or None = untouched)."""
    rgb = sheet[..., :3].astype(np.float32) / 255.0
    l = rgb @ np.array([0.299, 0.587, 0.114], np.float32)
    if hair_all:
        chroma = rgb.max(-1) - rgb.min(-1)
        hw = np.clip(1 - (chroma - 0.08) / 0.06, 0, 1)
        ow = np.zeros_like(l)
    else:
        hw = mask[..., 0] / 255.0
        ow = mask[..., 1] / 255.0
    if outfit_rgb is not None:
        rgb = rgb * (1 - ow[..., None]) + ramp(l, hex_rgb(outfit_rgb)) * ow[..., None]
    if hair_rgb is not None:
        rgb = rgb * (1 - hw[..., None]) + ramp(l, hex_rgb(hair_rgb)) * hw[..., None]
    out = sheet.copy()
    out[..., :3] = (rgb.clip(0, 1) * 255 + 0.5).astype(np.uint8)
    return out


def hex_rgb(h):
    h = h.lstrip('#')
    return [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]

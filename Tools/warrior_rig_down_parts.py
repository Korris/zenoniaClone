"""Cut the ChatGPT rig parts sheet of the Warrior (Starward Knight V3), facing camera (TEST rig, 25/11).

Source: Assets/ArtLibrary/ro-fantasy-heroes/src/rig-down/rig-down-parts-chatgpt-1.png (Tan Pham Plus, chat "Ve sheet manh rig 2D").
Each part = one connected blob on #00FF00; scaled to the game cell (head with ears 78 px like the approved idle-down cell),
green keyed + despilled; the skin-coloured ball joints ChatGPT painted on the limb ends are recoloured to the limb's own navy
(they stay as hidden overlap under the neighbour part, without a skin dot showing when a joint turns).
Side names are SCREEN sides: L = screen left = the hero's right hand (sword), R = screen right (shield).
"""
import numpy as np
from PIL import Image
from scipy import ndimage

SRC = "Assets/ArtLibrary/ro-fantasy-heroes/src/rig-down/rig-down-parts-chatgpt-1.png"
HEAD_EARS_GAME = 78.0          # idle-down cell, ear to ear (y 82)
HEAD_EARS_SHEET = 208.0        # blob 1 width
K = HEAD_EARS_GAME / HEAD_EARS_SHEET

# blob bbox x0 on the sheet -> part name (blobs found by ndimage.label, sorted by x0 / y0 in `blobs`)
BLOB_BY_X = {
    46: "head", 311: "torso", 548: "hips", 812: "padR", 989: "padL",
    1152: "upperArmL", 1370: "upperArmR", 45: "forearmL", 199: "forearmR",
    362: "thighL", 505: "thighR", 657: "shinL", 815: "shinR",
    974: "capeL", 1122: "capeM", 1363: "capeR",
}
# limbs whose skin ball joints are recoloured: name -> which ends ("top", "bottom")
KNOBS = {"upperArmL": ("top",), "upperArmR": ("top",), "forearmL": ("top",), "forearmR": ("top",),
         "thighL": ("top", "bottom"), "thighR": ("top", "bottom"), "shinL": ("top",), "shinR": ("top",)}


def load_sheet(path=SRC):
    return np.array(Image.open(path).convert("RGB")).astype(np.int32)


def green_mask(im):
    r, g, b = im[..., 0], im[..., 1], im[..., 2]
    return (g > 150) & (g - np.maximum(r, b) > 60)


def blobs(im):
    """name -> (y0, y1, x0, x1, own-pixel mask) of every part on the sheet."""
    fg = ~green_mask(im)
    lab, n = ndimage.label(fg)
    out = {}
    for i, s in enumerate(ndimage.find_objects(lab)):
        h, w = s[0].stop - s[0].start, s[1].stop - s[1].start
        if h * w < 2000:
            continue
        x0 = s[1].start
        # the two left-most blobs share x0 ~45: tell them apart by row
        name = BLOB_BY_X.get(x0) if not (x0 in (45, 46)) else ("head" if s[0].start < 150 else "forearmL")
        if name is None:
            continue
        out[name] = (s[0].start, s[0].stop, s[1].start, s[1].stop, lab[s] == i + 1)
    return out


def is_skin(rgb):
    """Peach skin / its pink shading; not white plate (r ~ b), not gold (g >> b), not navy."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return (r > 140) & (r - b > 25) & (r - b < 125) & (g - b < 60) & (r >= g)


NAVY = np.array([72.0, 68.0, 96.0])   # sleeve / trouser navy (median of the thigh parts)


def recolor_knobs(rgba, ends):
    """Skin pixels in the top / bottom 30 % of a limb -> sleeve navy, shading kept (luma ratio)."""
    rgb = rgba[..., :3].astype(np.float32)
    a = rgba[..., 3]
    h = rgb.shape[0]
    skin = is_skin(rgb) & (a > 0)
    navy = NAVY
    rows = np.zeros(h, bool)
    if "top" in ends:
        rows[: int(h * 0.30)] = True
    if "bottom" in ends:
        rows[int(h * 0.70):] = True
    m = skin & rows[:, None]
    luma = rgb.mean(axis=2) / 215.0
    out = rgba.copy()
    for c in range(3):
        out[..., c][m] = np.clip(navy[c] * luma[m] * 1.1, 0, 255)
    return out


def drop_shoulder_balls(rgba):
    """Torso: the skin shoulder balls in the top corners (and their thin outline rings) would peek above the pads near the neck;
    the centre neck stub / scarf and the chest plate stay. Works on the full-size cut (before scaling)."""
    h, w = rgba.shape[:2]
    a = rgba[..., 3] > 0
    corner = (np.abs(np.arange(w) - w / 2) > w * 0.2)[None, :] & (np.arange(h) < h * 0.32)[:, None]
    skin = is_skin(rgba[..., :3]) & a & corner
    out = rgba.copy()
    out[..., 3][skin] = 0
    # what is left in the corners: keep only solid areas (opening kills the 2-4 px outline rings)
    solid = ndimage.binary_opening(out[..., 3] > 0, iterations=4)
    out[..., 3][corner & ~solid] = 0
    return out


def cut_part(im, box):
    y0, y1, x0, x1, own = box
    pad = 4
    crop = im[y0:y1, x0:x1]
    rgba = np.zeros((y1 - y0, x1 - x0, 4), np.float32)
    rgba[..., :3] = crop
    # alpha: own blob, soft edge from the green distance (anti-aliased outline over green)
    g = crop[..., 1] - np.maximum(crop[..., 0], crop[..., 2])
    alpha = np.clip(1.0 - (g - 20) / 120.0, 0, 1) * own
    rgba[..., 3] = alpha * 255
    # despill: clamp green to max(r, b) on the edge
    spill = rgba[..., 1] > np.maximum(rgba[..., 0], rgba[..., 2])
    rgba[..., 1][spill] = np.maximum(rgba[..., 0], rgba[..., 2])[spill]
    big = np.zeros((rgba.shape[0] + 2 * pad, rgba.shape[1] + 2 * pad, 4), np.float32)
    big[pad:-pad, pad:-pad] = rgba
    return big


def scaled(rgba, k=K):
    """Premultiplied Lanczos down-scale by k, back to straight alpha."""
    a = rgba[..., 3:4] / 255.0
    pm = np.concatenate([rgba[..., :3] * a, rgba[..., 3:4]], axis=2).astype(np.float32)
    h, w = rgba.shape[:2]
    nw, nh = max(1, round(w * k)), max(1, round(h * k))
    chans = [np.array(Image.fromarray(pm[..., c]).resize((nw, nh), Image.LANCZOS)) for c in range(4)]
    out = np.stack(chans, axis=2)
    al = np.clip(out[..., 3], 0, 255)
    rgb = np.where(al[..., None] > 0.5, out[..., :3] / np.maximum(al[..., None] / 255.0, 1e-3), 0)
    res = np.concatenate([np.clip(rgb, 0, 255), al[..., None]], axis=2)
    res[..., 3][res[..., 3] < 8] = 0
    return res.astype(np.uint8)


def all_parts(path=SRC, scale=None):
    """name -> RGBA uint8 array at game scale (knobs recoloured); scale: name -> factor on K (default 1)."""
    im = load_sheet(path)
    out = {}
    for name, box in blobs(im).items():
        rgba = cut_part(im, box)
        if name == "torso":
            rgba = drop_shoulder_balls(rgba)
        if name in KNOBS:
            rgba = recolor_knobs(rgba.astype(np.uint8), KNOBS[name]).astype(np.float32)
        out[name] = scaled(rgba, K * (scale or {}).get(name, 1.0))
    return out

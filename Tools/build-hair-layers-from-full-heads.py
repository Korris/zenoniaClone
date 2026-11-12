"""Layered, dyeable heads for a class straight from its existing full heads (heads/<n>/<dir>.png), nothing redrawn: the look the user
approved stays pixel for pixel (2026-11-12, after the Warrior: a face or hair redrawn by ChatGPT read as another character).

The full heads of a class all carry the SAME face at the same place and differ only in the hair. So:
  heads/face/<dir>.png      the pixels on which at least two hairstyles agree (face, eyes, mask, ears, headband), the part connected to
                            the face. No skull is needed underneath: every hairstyle covers it.
  heads/hair/<n>/<dir>.png  everything of head n that is not that face, turned GRAY so HairDye can colour it: lightness scaled so the
                            hair's mid tone lands on HairDye's base tone. Accents of another hue (ribbon, bow) keep their colour.
  Data/classes/<class>/hair-dyes.json   "styles" = the mid tone of each original hair (the default dye gives the old colour back),
                            "presets" = the shared dye cycle.
The old heads/<n> stay in place: they are the source and are no longer drawn once heads/face + heads/hair/0 exist (HeroHairLayers).

  py Tools/build-hair-layers-from-full-heads.py <class> check OUT.png   originals | rebuilt with the default dye | dyed blue, no game files
  py Tools/build-hair-layers-from-full-heads.py <class> write
"""
import colorsys, json, os, sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
DIRS = ["down", "right", "up"]
F = 192
TOL = 26            # two styles "agree" on a pixel when no channel differs by more than this
BASE = 153          # HairDye's base tone (#999999): a gray of this lightness becomes exactly the dye colour
PRESETS = ["#C8323C", "#E07A2E", "#E8C445", "#4FA84A", "#2F7FD8", "#7A4FC8", "#E07AB0", "#34343C", "#F2F2F2"]


def heads_dir(cls): return f"{ROOT}/Assets/Resources/Sprites/classes/{cls}/heads"


def load(cls):
    hd = heads_dir(cls); n = 0; out = []
    while os.path.exists(f"{hd}/{n}/down.png"):
        out.append({d: np.asarray(Image.open(f"{hd}/{n}/{d}.png").convert("RGBA")).astype(np.int32) for d in DIRS}); n += 1
    return out


def shared_face(cells):
    """The face of one facing: per pixel the colour two or more heads agree on, kept where it is connected to the biggest such
    region (stray outline pixels that happen to match elsewhere drop out)."""
    face = np.zeros((F, F, 4), np.int32); votes = np.zeros((F, F), int)
    for i, a in enumerate(cells):
        agree = np.zeros((F, F), int)
        for j, b in enumerate(cells):
            if i != j:
                agree += (a[..., 3] > 128) & (b[..., 3] > 128) & (np.abs(a[..., :3] - b[..., :3]).max(-1) <= TOL)
        better = agree > votes
        face[better] = a[better]; votes[better] = agree[better]
    ok = votes >= 1
    lab, k = ndi.label(ndi.binary_closing(ok, iterations=1))
    if k == 0:
        return np.zeros((F, F, 4), np.int32)
    sizes = ndi.sum(ok, lab, range(1, k + 1))
    keep = np.isin(lab, 1 + np.nonzero(sizes >= max(40, 0.25 * sizes.max()))[0]) & ok
    face[~keep] = 0; face[keep, 3] = 255
    return face


def hue_sat(rgb):
    r, g, b = (rgb[..., i] / 255.0 for i in range(3))
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-6
    rr = m & (mx == r); gg = m & (mx == g) & ~rr; bb = m & ~rr & ~gg
    h[rr] = ((g - b)[rr] / d[rr]) % 6; h[gg] = (b - r)[gg] / d[gg] + 2; h[bb] = (r - g)[bb] / d[bb] + 4
    return h * 60, np.where(mx > 0, d / np.maximum(mx, 1e-6), 0)


def gray_hair(head, face):
    """(hair cell in gray, the hair's mid-tone colour). Hair = pixels of the head that are not the shared face."""
    diff = np.abs(head[..., :3] - face[..., :3]).max(-1)
    hair = (head[..., 3] > 128) & ~((face[..., 3] > 0) & (diff <= TOL))
    # Inside the face (eyes, headband, mask) a pixel that is only a little off the shared face is still the face: left in the hair
    # layer it was dyed with it (red hair + red headband, pink hair + red eyes turned blue under a blue dye).
    inner = ndi.binary_erosion(ndi.binary_fill_holes(face[..., 3] > 0), iterations=2)
    hair &= ~(inner & (face[..., 3] > 0) & (diff <= TOL * 3))
    rgb = head[..., :3].astype(float)
    lum = rgb.mean(-1)
    hue, sat = hue_sat(rgb)
    coloured = hair & (sat > 0.3) & (lum > 60)
    accent = np.zeros((F, F), bool)
    if coloured.sum() > 0.2 * hair.sum():                                  # a coloured hairstyle: its dominant hue is the hair
        hist, _ = np.histogram(hue[coloured], bins=36, range=(0, 360))
        peak = (np.argmax(hist) + 0.5) * 10
        far = np.abs((hue - peak + 180) % 360 - 180) > 45
        accent = coloured & far & (sat > 0.45)                             # pale highlights drift in hue: they are hair
    else:                                                                  # white / gray / black hair: anything vivid is an accent
        accent = hair & (sat > 0.45) & (lum > 60)
    lab, k = ndi.label(accent)                                             # an accent is a patch, not a stray pixel of a highlight
    if k:
        accent = np.isin(lab, 1 + np.nonzero(ndi.sum(accent, lab, range(1, k + 1)) >= 14)[0])
    body = hair & ~accent
    mid = body & (lum > 70)
    med = float(np.median(lum[mid])) if mid.any() else BASE
    g = np.clip(lum * BASE / max(med, 1.0), 0, 255)
    out = np.zeros((F, F, 4), np.int32)
    for c in range(3):
        out[..., c] = np.where(accent, head[..., c], g)
    out[..., 3] = np.where(hair, 255, 0)
    # The default dye: the colour that, run through HairDye's ramp over this gray, lands closest to the original hair (least squares
    # per channel). The plain mid tone came out paler and hue-shifted (orange read yellow, brown read red).
    l = g[body] / 255.0
    low = l <= 0.6
    A = np.where(low, 0.16 + 0.84 * np.clip(l / 0.6, 0, 1) ** 0.9, 1 - np.clip((l - 0.6) / 0.4, 0, 1) * 0.75)
    B = np.where(low, 0.0, np.clip((l - 0.6) / 0.4, 0, 1) * 0.75)
    orig = rgb[body] / 255.0
    colour = np.clip(((orig - B[:, None]) * A[:, None]).sum(0) / max((A * A).sum(), 1e-6), 0, 1) * 255 if body.any() else np.array([BASE] * 3.0)
    return out, colour


def build(cls):
    heads = load(cls)
    faces = {d: shared_face([h[d] for h in heads]) for d in DIRS}
    hairs, dyes = [], []
    for h in heads:
        cells = {}; cols = []
        for d in DIRS:
            cells[d], col = gray_hair(h[d], faces[d]); cols.append(col)
        hairs.append(cells)
        dyes.append("#%02X%02X%02X" % tuple(int(round(v)) for v in cols[0]))   # the front view's mid tone
    return heads, faces, hairs, dyes


def dyed(cell, hexcol):
    """HairDye.Remap in numpy: gray pixels onto the dye ramp, coloured ones kept."""
    a = cell.astype(float).copy()
    dc = np.array([int(hexcol[i:i + 2], 16) for i in (1, 3, 5)]) / 255.0
    c = a[..., :3]; l = c.mean(-1) / 255.0; gray = (c.max(-1) - c.min(-1)) <= 30
    lo = dc * 0.16 + (dc - dc * 0.16) * (np.clip(l / 0.6, 0, 1) ** 0.9)[..., None]
    hi = dc + (1 - dc) * (np.clip((l - 0.6) / 0.4, 0, 1) * 0.75)[..., None]
    a[..., :3] = np.where(gray[..., None], np.where((l <= 0.6)[..., None], lo, hi) * 255, c)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def check(cls, path):
    heads, faces, hairs, dyes = build(cls)
    print("default dyes", dyes)
    W = 150
    sheet = Image.new("RGBA", (W * len(heads), W * 3 * 3), (58, 62, 70, 255))
    for n in range(len(heads)):
        for j, d in enumerate(DIRS):
            for k in range(3):
                c = Image.new("RGBA", (F, F), (58, 62, 70, 255))
                if k == 0:
                    c.alpha_composite(Image.fromarray(heads[n][d].astype(np.uint8), "RGBA"))
                else:
                    c.alpha_composite(Image.fromarray(faces[d].astype(np.uint8), "RGBA"))
                    c.alpha_composite(dyed(hairs[n][d], dyes[n] if k == 1 else "#2F7FD8"))
                sheet.paste(c.crop((21, 0, 21 + W, W)), (n * W, (j * 3 + k) * W))
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).convert("RGB").save(path); print(path)


def write(cls):
    heads, faces, hairs, dyes = build(cls)
    hd = heads_dir(cls)
    for d in DIRS:
        os.makedirs(f"{hd}/face", exist_ok=True)
        Image.fromarray(faces[d].astype(np.uint8), "RGBA").save(f"{hd}/face/{d}.png")
    for n, cells in enumerate(hairs):
        os.makedirs(f"{hd}/hair/{n}", exist_ok=True)
        for d in DIRS:
            Image.fromarray(cells[d].astype(np.uint8), "RGBA").save(f"{hd}/hair/{n}/{d}.png")   # no .meta: Unity writes it on import
    data = f"{ROOT}/Assets/Resources/Data/classes/{cls}/hair-dyes.json"
    json.dump({"note": "Layered heads (HeroHairLayers): styles = default dye of hair/<n> (the mid tone of the original hair; Tools/build-hair-layers-from-full-heads.py), presets = Shift+H dye cycle. Gray hair pixels are remapped by lightness onto the dye (HairDye.cs); coloured accents keep their colour.",
               "styles": dyes, "presets": PRESETS}, open(data, "w", encoding="utf-8", newline="\n"), indent=2)
    print("wrote", f"{hd}/face, hair/0..{len(hairs) - 1},", data)


if __name__ == "__main__":
    if len(sys.argv) < 3: print(__doc__); sys.exit()
    cls, cmd = sys.argv[1], sys.argv[2]
    if cmd == "check": check(cls, sys.argv[3])
    elif cmd == "write": write(cls)
    else: print(__doc__)

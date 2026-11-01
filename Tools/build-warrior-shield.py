"""Warrior shields in the new style, cut from ChatGPT art on flat #00FF00.

Sources: Assets/ArtLibrary/classes/warrior/shield-src/<tier>-base-chatgpt.png, <tier>-enhanced-chatgpt.png. Old PNGs are kept in shield-src/old/.
Writes:  Resources/Sprites/classes/warrior/weapons/shield/<tier>-base.png, <tier>-enhanced.png (same canvas and grip for both)
         the tier's baseGrip and enhancedGrip in Resources/Data/classes/warrior/held-gear.json, nothing else in that file.

Size: new poses draw the shield with s = 0.27 and DrawScale 0.8, so 0.216 frame px per art px. In the reference the shield is 112 x 170 px on a hero 378 px tall; the hero is 164 frame px tall,
which gives about 49 x 74 frame px. The art is a little wider than the reference shield (0.70 against 0.66), so HEIGHT = 330 art px: about 50 x 71 frame px. The width follows the art.
Grip: centre of the base shield's box, the same for the enhanced one.

The other tiers come from one image, shield-src/<tier>-pair-chatgpt.png: base on the left, enhanced on the right, painted from iron-pair.png (ref/) and the old tier (ref/old-<tier>-pair.png).
Both go into the iron shield's box, so every tier has the same size and grip. The tool prints how much of the iron silhouette each one covers: under about 0.93 means repaint.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-warrior-shield.py <tier>            cut and install
  python Tools/build-warrior-shield.py <tier> OUT_DIR    cut into OUT_DIR, no game file touched
  python Tools/build-warrior-shield.py icons             rebuild offhand-warrior-<n> for the tiers with new art, as Tools/build-item-icons.py does
"""
import json, os, re, sys
import numpy as np
from PIL import Image
from scipy import ndimage

SRC = "Assets/ArtLibrary/classes/warrior/shield-src"
OUT = "Assets/Resources/Sprites/classes/warrior/weapons/shield"
GEAR = "Assets/Resources/Data/classes/warrior/held-gear.json"
HEIGHT, PAD = 330, 3


def key_green(path):
    """Straight-alpha floats. Only green joined to the image edge is background; edge pixels are unmixed from the key, then despilled."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    excess = g - np.maximum(r, b)
    key = np.median(np.concatenate([rgb[:20, :20].reshape(-1, 3), rgb[-20:, -20:].reshape(-1, 3), rgb[:20, -20:].reshape(-1, 3), rgb[-20:, :20].reshape(-1, 3)]), axis=0)
    e_bg = float(key[1] - max(key[0], key[2]))
    lab, _ = ndimage.label(excess > 0.5 * e_bg)
    edge = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    outside = np.isin(lab, edge[edge > 0])
    band = ndimage.binary_dilation(outside, iterations=5)
    alpha = np.ones(excess.shape)
    alpha[band] = 1 - np.clip((excess[band] - 0.10 * e_bg) / (0.75 * e_bg), 0, 1)
    alpha[outside] = 0
    a = alpha[..., None]
    out = np.where(a > 0.02, (rgb - (1 - a) * key) / np.maximum(a, 0.02), 0)
    out = np.clip(out, 0, 255)
    # Green left in the band goes too, so no fringe shows on a light or dark ground.
    spill = band & (out[..., 1] > np.maximum(out[..., 0], out[..., 2]))
    out[..., 1][spill] = np.maximum(out[..., 0], out[..., 2])[spill]
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    return np.dstack([out, alpha * 255])


def largest(rgba):
    """The one big blob, cropped: specks near the edge must not stretch the box."""
    lab, n = ndimage.label(rgba[..., 3] > 40)
    keep = lab == 1 + int(np.argmax(ndimage.sum(lab > 0, lab, range(1, n + 1))))
    near = ndimage.binary_dilation(keep, iterations=3)
    rgba = rgba.copy()
    rgba[..., 3][~near] = 0
    y0, y1 = np.where(keep.any(1))[0][[0, -1]]
    x0, x1 = np.where(keep.any(0))[0][[0, -1]]
    return rgba[y0:y1 + 1, x0:x1 + 1]


def resize(rgba, w, h):
    """Lanczos on premultiplied channels, so transparent pixels leak no colour into the edge."""
    a = rgba[..., 3:] / 255
    bands = [Image.fromarray((rgba[..., i] * a[..., 0]).astype(np.float32), "F").resize((w, h), Image.LANCZOS) for i in range(3)]
    alpha = np.asarray(Image.fromarray(rgba[..., 3].astype(np.float32), "F").resize((w, h), Image.LANCZOS)).astype(np.float64)
    alpha = np.clip(alpha, 0, 255)
    k = np.maximum(alpha / 255, 1e-3)
    return np.dstack([np.clip(np.asarray(b) / k, 0, 255) for b in bands] + [alpha])


def cut(path, size):
    art = largest(key_green(path))
    w, h = size if size else (round(art.shape[1] * HEIGHT / art.shape[0]), HEIGHT)
    return resize(art, w, h), (w, h)


def on_canvas(art, w, h):
    out = np.zeros((h + 2 * PAD, w + 2 * PAD, 4))
    out[PAD:PAD + h, PAD:PAD + w] = art
    return Image.fromarray(np.round(out).astype(np.uint8), "RGBA")


def cut_pair(path, size):
    """The two biggest blobs of one image, left to right, each forced into size."""
    rgba = key_green(path)
    lab, n = ndimage.label(rgba[..., 3] > 40)
    sizes = ndimage.sum(lab > 0, lab, range(1, n + 1))
    two = sorted(1 + np.argsort(sizes)[-2:], key=lambda i: np.where(lab == i)[1].mean())
    out = []
    for i in two:
        one = rgba.copy()
        one[..., 3][~ndimage.binary_dilation(lab == i, iterations=3)] = 0
        out.append(resize(largest(one), *size))
    return out


def overlap(art, iron):
    """Intersection over union of two silhouettes of the same size."""
    a, b = art[..., 3] > 128, iron[..., 3] > 128
    return (a & b).sum() / (a | b).sum()


def write_icons():
    """Same steps as fit() in build-item-icons.py, which rebuilds every class's icons when run. Tiers not repainted yet keep their icon, so icon and shield match."""
    names = [e["name"] for e in json.load(open(GEAR, encoding="utf-8"))["shield"]]
    for n, name in enumerate(names):
        if not (os.path.exists(f"{SRC}/{name}-pair-chatgpt.png") or os.path.exists(f"{SRC}/{name}-base-chatgpt.png")):
            continue
        im = Image.open(f"{OUT}/{name}-base.png").convert("RGBA")
        im = im.crop(im.getbbox())
        k = 88 / max(im.width, im.height)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        icon = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
        icon.paste(im, ((96 - im.width) // 2, (96 - im.height) // 2), im)
        icon.save(f"Assets/Resources/Sprites/icons/offhand-warrior-{n}.png")
        print("icon", n, name)


def main():
    tier = sys.argv[1]
    if tier == "icons":
        return write_icons()
    if os.path.exists(f"{SRC}/{tier}-pair-chatgpt.png"):
        iron, size = cut(f"{SRC}/iron-base-chatgpt.png", None)
        base, enhanced = cut_pair(f"{SRC}/{tier}-pair-chatgpt.png", size)
        print(tier, "silhouette against iron: base %.3f enhanced %.3f" % (overlap(base, iron), overlap(enhanced, iron)))
    else:
        base, size = cut(f"{SRC}/{tier}-base-chatgpt.png", None)
        # Enhanced is forced into the base's box: same silhouette, same size, same grip.
        enhanced, _ = cut(f"{SRC}/{tier}-enhanced-chatgpt.png", size)
    w, h = size[0] + 2 * PAD, size[1] + 2 * PAD
    grip = {"w": w, "h": h, "gripX": w // 2, "gripY": h // 2}
    target = sys.argv[2] if len(sys.argv) > 2 else OUT
    os.makedirs(target, exist_ok=True)
    on_canvas(base, *size).save(f"{target}/{tier}-base.png")
    on_canvas(enhanced, *size).save(f"{target}/{tier}-enhanced.png")
    print(tier, "size", w, h, "grip", grip["gripX"], grip["gripY"])
    if len(sys.argv) > 2:
        return
    # Text edit of this one entry: the file is shared, and the sword and anchor parts must stay byte for byte.
    text = open(GEAR, encoding="utf-8", newline="").read()
    start = text.index('"shield":[')
    entry = re.compile(r'(\{"name":"%s","glow":"[^"]+",)"baseGrip":\{[^}]*\},"enhancedGrip":\{[^}]*\}\}' % tier)
    g = json.dumps(grip, separators=(",", ":"))
    new, n = entry.subn(lambda m: f'{m.group(1)}"baseGrip":{g},"enhancedGrip":{g}}}', text[start:], count=1)
    if n != 1:
        raise SystemExit("no shield entry for " + tier)
    with open(GEAR, "w", encoding="utf-8", newline="") as f:
        f.write(text[:start] + new)


if __name__ == "__main__":
    main()

"""Layered hero weapons (step D, art batch 3): src/weapons-5-classes-chatgpt-N.png (2 rows x 4 upright items) -> one sprite per item at the spec
size relative to the 200 px figure (ChatGPT drew them all about the same size) + a grip point, ready for the weapon layer.

  python Tools/build-hero-layered-weapons.py write   Sprites/hero-layered/weapons/<item>.png + Data/hero-layered/weapons.json {item: [gx, gy]}
  python Tools/build-hero-layered-weapons.py check   docs/reference/hero-layered-real/weapons-sizes.png (items beside the male idle body, same px)

Not on the heroes yet: the per-frame grips (fist position + angle for 5 anims x 8 dirs x 4 frames) are the next step (TODO).
"""
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hero_recolor_core as core  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/ArtLibrary/hero-layered"
OUT = ROOT / "Assets/Resources/Sprites/hero-layered/weapons"
DATA = ROOT / "Assets/Resources/Data/hero-layered/weapons.json"
META = ROOT / "Assets/Resources/Sprites/hero-layered/test/weapons/sword.png.meta"
# reading order of the sheet, target height in px of the 200 px figure (spec Prompt 7), grip: (kind) handle row / centre / fraction from the top
ITEMS = [("sword", 150, "handle"), ("warrior-shield", 80, "centre"), ("wizard-staff", 190, 0.62), ("wizard-book", 45, "centre"),
         ("pistol", 70, "butt"), ("dagger", 75, "handle"), ("cleric-staff", 190, 0.62), ("cleric-shield", 80, "centre")]


def pieces(rgba):
    al = rgba[..., 3] > 40
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=8))
    objs = ndimage.find_objects(lab)
    sizes = ndimage.sum(al, lab, range(1, n + 1))
    keep = [i for i in range(n) if sizes[i] > 0.1 * sizes.max()]
    if len(keep) != 8:
        raise SystemExit(f"{len(keep)} items, expected 8")
    rows = sorted(keep, key=lambda i: objs[i][0].start)
    top, bottom = sorted(rows[:4], key=lambda i: objs[i][1].start), sorted(rows[4:], key=lambda i: objs[i][1].start)
    out = []
    for i in top + bottom:
        sl = objs[i]
        m = (lab[sl] == i + 1) & al[sl]
        a = rgba[sl].copy()
        a[~m] = 0
        out.append(Image.fromarray(a))
    return out


def grip(img, kind):
    a = np.asarray(img)[..., 3] > 100
    h = a.shape[0]
    if kind == "centre":
        return img.width / 2, h / 2
    if isinstance(kind, float):
        y = int(h * kind)
        xs = np.nonzero(a[y])[0]
        return float(xs.mean()), float(y)
    if kind == "butt":  # pistol held upright: the fist on the lower third
        ys, xs = np.nonzero(a[int(h * 0.62):])
        return float(xs.mean()), float(ys.mean() + int(h * 0.62))
    widths = [(np.count_nonzero(a[y]), y) for y in range(int(h * 0.62), int(h * 0.9))]  # handle: the narrowest rows under the guard
    w0 = min(w for w, _ in widths)
    rows = [y for w, y in widths if w <= w0 + 2]
    y = float(np.median(rows))
    xs = np.nonzero(a[int(y)])[0]
    return float(xs.mean()), y


def write():
    src = sorted(ART.glob("src/weapons-5-classes-chatgpt-*.png"), key=lambda p: int(re.search(r"-(\d+)\.png$", p.name).group(1)))[-1]
    OUT.mkdir(parents=True, exist_ok=True)
    table = {}
    for (name, px, kind), img in zip(ITEMS, pieces(core.key_green(Image.open(src)))):
        s = px / img.height
        img = img.resize((max(1, round(img.width * s)), px), Image.LANCZOS)
        gx, gy = grip(img, kind)
        img.save(OUT / f"{name}.png")
        meta = Path(str(OUT / f"{name}.png") + ".meta")
        if not meta.exists():
            import uuid
            meta.write_text(re.sub(r"guid: \w+", "guid: " + uuid.uuid4().hex, META.read_text(encoding="utf-8"), count=1), encoding="utf-8")
        table[name] = [round(gx, 1), round(gy, 1)]
        print(f"{name}: {img.width}x{img.height}, grip {table[name]}")
    DATA.write_text(json.dumps({"note": f"Upright weapon sprites from {src.name} (Tools/build-hero-layered-weapons.py): grip px (x, y down).",
                                "grips": table}, indent=1), encoding="utf-8")


def check():
    body = Image.open(ART / "sheets/male/idle.png").convert("RGBA").crop((0, 512, 320, 768))  # right-facing idle
    out = Image.new("RGBA", (320 + 8 * 70, 256), (196, 204, 188, 255))
    out.alpha_composite(body)
    for i, (name, _, _) in enumerate(ITEMS):
        w = Image.open(OUT / f"{name}.png").convert("RGBA")
        gx, gy = json.loads(DATA.read_text(encoding="utf-8"))["grips"][name]
        x = 320 + i * 70 + 35
        out.alpha_composite(w, (int(x - gx), 244 - w.height))
        out.putpixel((int(x), int(244 - w.height + gy)), (255, 0, 0, 255))
    dst = ROOT / "docs/reference/hero-layered-real/weapons-sizes.png"
    out.resize((out.width * 2, out.height * 2), Image.NEAREST).convert("RGB").save(dst)
    print("wrote", dst)


if __name__ == "__main__":
    {"write": write, "check": check}.get(sys.argv[1] if len(sys.argv) > 1 else "", lambda: print(__doc__))()

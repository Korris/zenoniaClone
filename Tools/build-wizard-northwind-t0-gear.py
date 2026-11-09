"""Wizard T0 ("apprentice") staff + book redrawn as the Northwind original (twisted driftwood staff with an ice crystal head,
navy snowflake spell book).

Source: ArtLibrary/classes/wizard/outfits/northwind-t0-gear/src/wizard-northwind-t0-staff-book-chatgpt-1.png
(ChatGPT, staff upright on the left, open book on the right, #00FF00).

Fit (keeps every anchor valid, rule "one weapon one size"): the staff art gets the old art's height (alpha 358 px in a
364 px canvas) and its grip row stays at the old gripY on the plain shaft; the book gets the old book's alpha width and its grip
keeps the same place relative to the book's bottom-left (the fist holds the lower inner edge). Writes <name>-base / -enhanced
(same art, as before), <name>/idle-stance.png (grip-centred, idle down outfit anchors), icons weapon/offhand-wizard-0 and the
two grips in held-wizard.json. Old files are copied to northwind-t0-gear/old/ once.

  py Tools/build-wizard-northwind-t0-gear.py fit       -> staging/ + preview.png
  py Tools/build-wizard-northwind-t0-gear.py install
"""
import json, os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
WORK = f"{ROOT}/Assets/ArtLibrary/classes/wizard/outfits/northwind-t0-gear"
SRC = f"{WORK}/src/wizard-northwind-t0-staff-book-chatgpt-1.png"
STAGE, OLD = f"{WORK}/staging", f"{WORK}/old"
RES = f"{ROOT}/Assets/Resources"
WEAP = f"{RES}/Sprites/classes/wizard/weapons"
ICONS = f"{RES}/Sprites/icons"
MANIFEST = f"{RES}/Data/classes/wizard/held-wizard.json"
NAME = "apprentice"                     # tier 0 entry of both manifests
SHAFT_MAX = 14                          # art px at the grip (s 0.447: about 6.3 frame px, thinner than the fist)


def keyed():
    a = np.asarray(Image.open(SRC).convert("RGB")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    alpha = np.clip((np.maximum.reduce([r, b, 255 - g]) - 40) / 80.0, 0, 1) * 255
    g = np.minimum(g, np.maximum(r, b) + 10)                     # no green in either item
    out = np.dstack([r, g, b, alpha]).astype(np.uint8)
    lab, _ = ndimage.label(ndimage.binary_dilation(alpha > 128, iterations=3))
    objs = [(i + 1, s) for i, s in enumerate(ndimage.find_objects(lab)) if s is not None and (lab[s] == i + 1).sum() > 5000]
    objs.sort(key=lambda o: o[1][1].start)
    assert len(objs) == 2, f"expected staff + book, got {len(objs)}"
    crops = []
    for n, s in objs:
        c = out[s].copy(); c[lab[s] != n] = 0                     # only this object's pixels
        crops.append(Image.fromarray(c))
    return crops                                                 # staff, book


def resized(im, w, h):
    a = np.asarray(im).astype(np.float32); a[..., :3] *= a[..., 3:4] / 255
    o = np.dstack([np.asarray(Image.fromarray(a[..., c]).resize((w, h), Image.LANCZOS)) for c in range(4)])
    o = np.clip(o, 0, 255); al = o[..., 3:4]
    o[..., :3] = np.where(al > 0.5, o[..., :3] * 255 / np.maximum(al, 1e-3), 0); o[..., 3] = np.where(o[..., 3] < 16, 0, o[..., 3])
    return Image.fromarray(o.astype(np.uint8))


def old_grip(part):
    m = json.load(open(MANIFEST, encoding="utf-8"))
    return next(e for e in m["weapon" if part == "staff" else "book"] if e["name"] == NAME)["baseGrip"]


def fit_staff(im):
    g = old_grip("staff"); old = Image.open(f"{WEAP}/staff/{NAME}-base.png"); bx = old.getbbox()
    im = im.crop(im.getbbox()); H = bx[3] - bx[1]
    im = resized(im, max(1, round(im.width * H / im.height)), H)
    a = np.asarray(im)[..., 3] > 128
    row = g["gripY"] - bx[1]
    xs = np.where(a[row])[0]
    width = xs.max() - xs.min() + 1
    cv = Image.new("RGBA", (im.width + 6, g["h"])); cv.alpha_composite(im, (3, bx[1]))
    grip = {"w": cv.width, "h": cv.height, "gripX": int(round(3 + (xs.min() + xs.max()) / 2)), "gripY": g["gripY"]}
    print(f"staff {cv.size}, shaft at grip {width} px (max {SHAFT_MAX})", "OK" if width <= SHAFT_MAX else "TOO THICK")
    return cv, grip


def fit_book(im):
    g = old_grip("book"); old = Image.open(f"{WEAP}/book/{NAME}-base.png"); bx = old.getbbox()
    oa = np.asarray(old)[..., 3] > 128
    fx = (g["gripX"] - bx[0]) / (bx[2] - bx[0])                  # grip column as a fraction of the book width
    up = np.where(oa[:, g["gripX"]])[0].max() - g["gripY"]       # grip height above the book's lower edge there
    im = im.crop(im.getbbox()); W = bx[2] - bx[0]
    im = resized(im, W, max(1, round(im.height * W / im.width)))
    cv = Image.new("RGBA", (im.width + 6, im.height + 6)); cv.alpha_composite(im, (3, 3))
    a = np.asarray(cv)[..., 3] > 128
    gx = int(round(3 + fx * W)); gy = int(np.where(a[:, gx])[0].max() - up)
    print(f"book {cv.size}, grip ({gx}, {gy})")
    return cv, {"w": cv.width, "h": cv.height, "gripX": gx, "gripY": gy}


def centered(im, g):                                             # as install-wizard-current-reference.centered_art
    gx, gy = g["gripX"], g["gripY"]
    w = 2 * int(np.ceil(max(gx, im.width - gx))) + 6; h = 2 * int(np.ceil(max(gy, im.height - gy))) + 6
    out = Image.new("RGBA", (w, h)); out.alpha_composite(im, (w // 2 - gx, h // 2 - gy)); return out


def icon(im):                                                    # as install-wizard-current-reference.icon
    im = im.crop(im.getbbox()); k = min(56 / im.width, 56 / im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new("RGBA", (64, 64)); out.alpha_composite(im, ((64 - im.width) // 2, (64 - im.height) // 2)); return out


def cmd_fit():
    os.makedirs(STAGE, exist_ok=True)
    staff, book = keyed()
    (s, sg), (b, bg) = fit_staff(staff), fit_book(book)
    for part, im, g in (("staff", s, sg), ("book", b, bg)):
        im.save(f"{STAGE}/{part}-base.png"); centered(im, g).save(f"{STAGE}/{part}-idle-stance.png")
        icon(im).save(f"{STAGE}/{'weapon' if part == 'staff' else 'offhand'}-wizard-0.png")
    json.dump({"staff": sg, "book": bg}, open(f"{STAGE}/grips.json", "w"), indent=1)


def cmd_install():
    grips = json.load(open(f"{STAGE}/grips.json"))
    os.makedirs(OLD, exist_ok=True)
    targets = {}
    for part in ("staff", "book"):
        targets[f"{WEAP}/{part}/{NAME}-base.png"] = f"{STAGE}/{part}-base.png"
        targets[f"{WEAP}/{part}/{NAME}-enhanced.png"] = f"{STAGE}/{part}-base.png"
        targets[f"{WEAP}/{part}/{NAME}/idle-stance.png"] = f"{STAGE}/{part}-idle-stance.png"
        icon_name = f"{'weapon' if part == 'staff' else 'offhand'}-wizard-0.png"
        targets[f"{ICONS}/{icon_name}"] = f"{STAGE}/{icon_name}"
    targets[MANIFEST] = None
    for dst, src in targets.items():
        keep = f"{OLD}/{os.path.relpath(dst, RES).replace(os.sep, '__').replace('/', '__')}"
        if not os.path.exists(keep):
            shutil.copy(dst, keep)
        if src:
            shutil.copy(src, dst)
    m = json.load(open(MANIFEST, encoding="utf-8"))
    for key, part in (("weapon", "staff"), ("book", "book")):
        e = next(x for x in m[key] if x["name"] == NAME)
        e["baseGrip"] = dict(grips[part]); e["enhancedGrip"] = dict(grips[part])
    json.dump(m, open(MANIFEST, "w", encoding="utf-8"), indent=2)
    print("installed T0 staff + book; old copies in", OLD)


if __name__ == "__main__":
    {"fit": cmd_fit, "install": cmd_install}[sys.argv[1]]()

"""Mechanic pistol tiers: 10 tiers, base and enhanced, every one in the silhouette of the master pistol. Run from the project root; Pillow, numpy, scipy.

Originals: Assets/ArtLibrary/classes/mechanic/new-style/dual-src/tiers/
  <tier>-pair-chatgpt.png     one portrait image, base above enhanced, flat #00FF00

  python Tools/build-mechanic-pistol-tiers.py check <tier>...        cut and measure pistols, write nothing to the game. Previews under OUT_CHECK
  python Tools/build-mechanic-pistol-tiers.py install <tier>...      write pistol/<tier>-base|enhanced.png, grips.json, the manifest
  python Tools/build-mechanic-pistol-tiers.py manifest               every tier gets its art (the master where none is drawn yet) and its manifest entry
  python Tools/build-mechanic-pistol-tiers.py icons                  weapon-mechanic-0..9, one pistol each. Old ones kept once in tiers/old-icons/
  python Tools/build-mechanic-pistol-tiers.py sheet OUT              pistols of every tier on the hero, composed as the game draws, under OUT only

Tools/build-mechanic-dual.py reads tiers/grips.json, so its `write` keeps these entries. The `name` of each entry is also the folder of that tier's cannon.
"""
import importlib.util, json, os, re, shutil, sys, uuid
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

spec = importlib.util.spec_from_file_location("bd", "Tools/build-mechanic-dual.py")
bd = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bd)

TIERS = ["scrap", "brass", "copper", "plasma", "ember", "frost", "volt", "gatling", "star", "dragon"]
ORIG = f"{bd.SRC}/tiers"
GRIPS = bd.TIER_GRIPS
PISTOL = f"{bd.WEAPONS}/pistol"
ICONS = f"{bd.RES}/Sprites/icons"
MASTER_GRIP = {"gripX": bd.PISTOL_GRIP[0], "gripY": bd.PISTOL_GRIP[1]}
OUT_CHECK = os.environ.get("TIER_CHECK_OUT", "plans/visuals/pistol-tier-check")
MIP = 4
ICON_SIZE, ICON_BOX, ICON_TILT = 96, 77, 45  # about 10% empty on every side, room for the enhance halo in the slot


def key_pieces(path, count):
    """The `count` biggest solid pieces of a flat #00FF00 image, top to bottom: straight RGBA with unmixed edges, each cropped to its box."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    g, mx = rgb[..., 1], np.maximum(rgb[..., 0], rgb[..., 2])
    alpha = 1 - np.clip((g - mx - 30) / 150, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    a = alpha[..., None]
    k = np.array([0, 255, 0], np.float32)
    out = np.clip(np.where(a > 0.02, (rgb - (1 - a) * k) / np.maximum(a, 0.02), 0), 0, 255)
    spill = (g > mx) & (alpha < 0.98)
    out[..., 1] = np.where(spill, mx, out[..., 1])
    lab, n = ndimage.label(alpha > 0.5, structure=np.ones((3, 3)))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    best = 1 + np.argsort(sizes)[::-1][:count]
    pieces = []
    for i in best:
        keep = ndimage.binary_dilation(lab == i, iterations=3)
        ys, xs = np.nonzero(keep)
        box = (ys.min(), ys.max() + 1, xs.min(), xs.max() + 1)
        part = np.dstack([out, alpha * keep * 255])[box[0]:box[1], box[2]:box[3]]
        pieces.append((box[0], box[2], Image.fromarray(part.astype(np.uint8), "RGBA"), int(sizes[i - 1])))
    print(os.path.basename(path), "pieces", n, "kept px", [p[3] for p in pieces], "next biggest", int(np.sort(sizes)[::-1][count]) if n > count else 0)
    return pieces


def fit(part, target_rgba):
    """Resize `part` with LANCZOS into the master's exact box, so every tier has one size and the muzzle and butt on the same rows. ChatGPT's proportions differ from the master by up to 3%."""
    target = np.asarray(target_rgba)[..., 3] > 128
    ty, tx = np.nonzero(target)
    x0, y0, w, h = int(tx.min()), int(ty.min()), int(tx.max() - tx.min() + 1), int(ty.max() - ty.min() + 1)
    py, px = np.nonzero(np.asarray(part)[..., 3] > 128)
    part = part.crop((px.min(), py.min(), px.max() + 1, py.max() + 1))
    canvas = Image.new("RGBA", target_rgba.size, (0, 0, 0, 0))
    canvas.paste(part.resize((w, h), Image.LANCZOS), (x0, y0))
    solid = np.asarray(canvas)[..., 3] > 128
    iou = (solid & target).sum() / (solid | target).sum()
    outside = int((solid & ~ndimage.binary_dilation(target, iterations=3)).sum())
    return canvas, dict(iou=round(float(iou), 3), source=[part.width, part.height], outside=outside)


def handle_centre(rgba):
    """Middle of the handle: the first solid run from the left on each low row, without the wider joint above it and the butt plate below it."""
    solid = np.asarray(rgba)[..., 3] > 128
    h = solid.shape[0]
    rows = {}
    for y in range(int(h * 0.55), h):
        xs = np.flatnonzero(solid[y])
        if not len(xs):
            continue
        end = xs[0]
        while end + 1 < solid.shape[1] and solid[y, end + 1]:
            end += 1
        rows[y] = (int(xs[0]), int(end))
    widths = {y: b - a + 1 for y, (a, b) in rows.items()}
    core = float(np.median([widths[y] for y in rows if h * 0.68 <= y <= h * 0.85]))
    run = [y for y in rows if abs(widths[y] - core) <= core * 0.2 and h * 0.6 <= y <= h * 0.92]
    mid = (min(run) + max(run)) // 2
    a, b = rows[mid]
    return (a + b + 1) // 2, mid


def master_pistol():
    return bd.arts()["pistol"]


def cut_pair(tier):
    pieces = key_pieces(f"{ORIG}/{tier}-pair-chatgpt.png", 2)
    pieces.sort(key=lambda p: p[0])
    master = master_pistol()
    mx, my = handle_centre(master)
    done = {}
    for kind, (_, _, part, _) in zip(("base", "enhanced"), pieces):
        im, info = fit(part, master)
        gx, gy = handle_centre(im)
        grip = {"w": im.width, "h": im.height, "gripX": MASTER_GRIP["gripX"] + gx - mx, "gripY": MASTER_GRIP["gripY"] + gy - my}
        print(tier, kind, json.dumps(info), json.dumps(grip))
        done[kind] = (im, grip, info)
    if tier == "scrap":  # the master itself is tier 0's base
        grip = {"w": master.width, "h": master.height, **MASTER_GRIP}
        done["base"] = (master, grip, dict(iou=1.0))
    return done


def check_picture(tier, done):
    os.makedirs(OUT_CHECK, exist_ok=True)
    master = master_pistol()
    z = 3
    w, h = master.size
    s = Image.new("RGB", (3 * w * z, h * z), (58, 62, 70))
    for i, kind in enumerate(("base", "enhanced")):
        im = done[kind][0].resize((w * z, h * z), Image.NEAREST)
        s.paste(im, (i * w * z, 0), im)
    # enhanced over the master's outline: red = art outside the master, blue = master not covered
    a = np.asarray(done["enhanced"][0])[..., 3] > 128
    b = np.asarray(master)[..., 3] > 128
    diff = np.zeros((h, w, 3), np.uint8)
    diff[a & b] = (90, 90, 90)
    diff[a & ~b] = (255, 60, 60)
    diff[~a & b] = (60, 120, 255)
    s.paste(Image.fromarray(diff).resize((w * z, h * z), Image.NEAREST), (2 * w * z, 0))
    s.save(f"{OUT_CHECK}/{tier}.png")


def write_png_with_meta(im, path, meta_from):
    """The .meta goes first: Unity, often open on this project, would otherwise make its own for the new PNG."""
    if not os.path.exists(path + ".meta"):
        text = open(meta_from, "rb").read().decode()
        with open(path + ".meta", "wb") as fp:
            fp.write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, text, count=1).encode())
    im.save(path)


def read_grips():
    return json.load(open(GRIPS, encoding="utf-8")) if os.path.exists(GRIPS) else {}


def save_tier(tier, done, grips):
    for kind, (im, grip, _) in done.items():
        write_png_with_meta(im, f"{PISTOL}/{tier}-{kind}.png", f"{PISTOL}/pistol-base.png.meta")
    grips[tier] = {"base": done["base"][1], "enhanced": done["enhanced"][1]}


def write_manifest():
    """Every tier by its own name, because the cannon's art folder follows the name too. A tier not drawn yet gets the master under its name. Only `weapon` changes in the file."""
    master = master_pistol()
    grips = read_grips()
    for tier in TIERS:
        if tier not in grips:
            for kind in ("base", "enhanced"):
                write_png_with_meta(master, f"{PISTOL}/{tier}-{kind}.png", f"{PISTOL}/pistol-base.png.meta")
    with open(GRIPS, "w", encoding="utf-8") as fp:
        json.dump(grips, fp, indent=1)
    data = json.load(open(bd.GEAR_DUAL, encoding="utf-8"))
    data["weapon"] = bd.weapon_manifest(json.load(open(bd.GEAR, encoding="utf-8")), master)
    with open(bd.GEAR_DUAL, "w", encoding="utf-8") as fp:
        json.dump(data, fp, separators=(",", ":"))


# ---- icons ----
def icon_frame(reference):
    """Scale and offset from the master, so every tier has one size and one centre whatever its art does at the edge."""
    box = reference.rotate(ICON_TILT, expand=True, resample=Image.BICUBIC).getbbox()
    k = ICON_BOX / max(box[2] - box[0], box[3] - box[1])
    return k, ((ICON_SIZE - (box[2] - box[0]) * k) / 2 - box[0] * k, (ICON_SIZE - (box[3] - box[1]) * k) / 2 - box[1] * k)


def icon(path, frame):
    k, (ox, oy) = frame
    im = Image.open(path).convert("RGBA").rotate(ICON_TILT, expand=True, resample=Image.BICUBIC)
    small = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
    out = Image.new("RGBA", (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
    out.paste(small, (round(ox), round(oy)))
    return out


def write_icons():
    keep = f"{ORIG}/old-icons"
    os.makedirs(keep, exist_ok=True)
    weapon = icon_frame(master_pistol())
    for i, tier in enumerate(TIERS):
        path = f"{ICONS}/weapon-mechanic-{i}.png"
        if os.path.exists(path) and not os.path.exists(f"{keep}/weapon-mechanic-{i}.png"):
            shutil.copy2(path, f"{keep}/weapon-mechanic-{i}.png")
        im = icon(f"{PISTOL}/{tier}-base.png", weapon)
        im.save(path)
        print(path, "box", im.getbbox())


# ---- pictures, as HeroRenderer draws ----
def live_anchors():
    data = json.load(open(bd.GEAR_DUAL, encoding="utf-8"))
    return {(a["part"], a["anim"], a["dir"]): a["frames"] for a in data["anchors"]}, data["weapon"]


def hero(art, anchors, anim, d, f, grip=None):
    if grip:
        bd.PISTOL_GRIP = (grip["gripX"], grip["gripY"])
    return bd.compose(bd.RES, art, anchors, anim, d, f)


def label_row(sheet, text, y):
    ImageDraw.Draw(sheet).text((4, y + 4), text, fill=(255, 255, 0))


def pistol_sheet(out):
    os.makedirs(out, exist_ok=True)
    anchors, weapon = live_anchors()
    # The game minifies through mipmaps; a plain bicubic from 258 px to about 39 aliases into noise the game does not show. So the pistol is drawn from a 1/MIP LANCZOS copy.
    anchors = {k: [dict(f, s=f["s"] * MIP) for f in v] if k[0] in ("gun", "gunL") else v for k, v in anchors.items()}
    art = bd.arts()
    poses = [("idle", "right", 0), ("attack2", "left", 1), ("attack3", "right", 1), ("idle", "down", 0)]
    crop = (50, 40, 282, 272)  # frame plus padding, around the hero
    cw, ch, z = crop[2] - crop[0], crop[3] - crop[1], 2
    rows = []
    for e in weapon:
        for kind in ("base", "enhanced"):
            grip = e[kind + "Grip"]
            gun = Image.open(f"{PISTOL}/{e['name']}-{kind}.png").convert("RGBA")
            art["pistol"] = gun.resize((gun.width // MIP, gun.height // MIP), Image.LANCZOS)
            row = Image.new("RGB", (90 + 300 + len(poses) * cw * z, ch * z), (58, 62, 70))
            label_row(row, f"{e['name']}\n{kind}", 0)
            row.paste(gun, (90, (ch * z - gun.height) // 2), gun)
            for i, (anim, d, f) in enumerate(poses):
                c = hero(art, anchors, anim, d, f, {"gripX": grip["gripX"] / MIP, "gripY": grip["gripY"] / MIP}).crop(crop).resize((cw * z, ch * z), Image.LANCZOS)
                row.paste(c, (390 + i * cw * z, 0), c)
            rows.append(row)
    W, H = rows[0].size
    contact = Image.new("RGB", (W * 2, H * 10), (30, 30, 30))
    for i, r in enumerate(rows):
        contact.paste(r, ((i % 2) * W, (i // 2) * H))
    contact.save(f"{out}/contact-pistol-tiers.png")
    for t in range(0, 10, 5):
        contact.crop((0, t * H, W * 2, (t + 5) * H)).save(f"{out}/contact-pistol-tiers-{t}-{t + 4}.png")


if __name__ == "__main__":
    cmd, args = sys.argv[1], sys.argv[2:]
    if cmd == "check":
        for t in args:
            check_picture(t, cut_pair(t))
    elif cmd == "install":
        grips = read_grips()
        for t in args:
            save_tier(t, cut_pair(t), grips)
        with open(GRIPS, "w", encoding="utf-8") as fp:
            json.dump(grips, fp, indent=1)
        write_manifest()
    elif cmd == "manifest":
        write_manifest()
    elif cmd == "icons":
        write_icons()
    elif cmd == "sheet":
        pistol_sheet(args[0])
    else:
        raise SystemExit(__doc__)

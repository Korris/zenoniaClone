"""Mechanic two pistols + back cannon: shared constants and helpers (pistol / cannon art views, cannon anchors, weapon manifest, a compose like
HeroRenderer) for Tools/build-mechanic-pistol-tiers.py, build-mechanic-cannon-tiers.py and build-mechanic-attack-down-repaint.py.
Sources: Assets/ArtLibrary/classes/mechanic/new-style/dual-src/ (cannon-*, pistol-side, tiers/).

History: this was the tool that built the two-pistol trial (2026-11-01) from the single-gun bodies in new-style/pre-dual/. Its backup / grid /
preview / write / muzzles commands, the fist and anchor tables they used, the way back to the single gun (held-mechanic.json, weapons/gun,
the robot companion) and new-style/pre-dual/ were removed 16/11: the body sheets have been repainted since (attack-down-repaint, idle doll,
outfits), so `write` would only have overwritten them. git 4c527c36 has the full tool. Muzzle offsets: Tools/measure-mechanic-attack-muzzles.py."""
import json, math, os
import numpy as np
from PIL import Image
from scipy import ndimage
FRAME = 192
SRC = "Assets/ArtLibrary/classes/mechanic/new-style/dual-src"
RES = "Assets/Resources"
BODY = f"{RES}/Sprites/classes/mechanic/body"
WEAPONS = f"{RES}/Sprites/classes/mechanic/weapons"
GEAR_DUAL = f"{RES}/Data/classes/mechanic/held-mechanic-dual.json"
TIER_GRIPS = f"{SRC}/tiers/grips.json"
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
DIRS = ["down", "up", "left", "right"]
MIRRORED = ("attack3", "attack4", "rocket", "dash", "hurt")
PISTOL_S, PISTOL_LEN = 0.26, 258  # 2026-11-05: tried 0.20 so both pistols fit side by side in the new idle, the user wants 0.26 back; 2026-11-04: s 0.15 -> 0.26 (x1.73), 39 -> 67 frame px long: the user found the pistols too small. The anchors in held-mechanic-dual.json were scaled the same way; `muzzles` then gives the HeroFx tables
CANNON_LEN = 168.0  # frame px, tip to tail
CANNON_S = 0.5  # art is cut at twice its size on screen
SIDE_SX = 0.9  # the barrel lies across the back: a little shorter seen from the side
VIEW_W, VIEW_GRIP = 125, 0.67  # pistol seen along the barrel: art px wide, fist's place down the height (just under the trigger guard, keeps the ring off the chin)
GRIP_PAINTED = (185, 640, 933)  # middle of the pistol's handle in the 933 px wide cut


def key(path):
    """Straight RGBA from flat #00FF00; edges unmixed; only what joins the biggest piece stays."""
    rgb = np.asarray((path if isinstance(path, Image.Image) else Image.open(path)).convert("RGB")).astype(np.float32)
    k = np.array([0, 255, 0], np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    alpha = 1 - np.clip((g - np.maximum(r, b) - 30) / 150, 0, 1)
    a = alpha[..., None]
    out = np.clip(np.where(a > 0.02, (rgb - (1 - a) * k) / np.maximum(a, 0.02), 0), 0, 255)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    lab, n = ndimage.label(alpha > 0.5)
    if n > 1:
        sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
        alpha = alpha * ndimage.binary_dilation(lab == 1 + int(np.argmax(sizes)), iterations=3)
    spill = (g > np.maximum(r, b)) & (alpha < 0.98)
    out[..., 1] = np.where(spill, np.maximum(out[..., 0], out[..., 2]), out[..., 1])
    im = Image.fromarray(np.dstack([out, alpha * 255]).astype(np.uint8), "RGBA")
    return im.crop(im.getbbox())


def axis_length(im):
    ys, xs = np.nonzero(np.asarray(im)[..., 3] > 128)
    pts = np.stack([xs, ys], 1).astype(float)
    c = pts.mean(0)
    _, _, vt = np.linalg.svd(pts[::37] - c, full_matrices=False)
    p = (pts - c) @ vt[0]
    return p.max() - p.min()


def pistol_view(raw, half):
    """A pistol seen along its barrel, from one half of pistol-front-back.png, padded so the fist's place on the handle is the image centre: anchors with `art` pivot at the centre."""
    w, h = raw.size
    im = key(raw.crop((0, 0, w // 2, h) if half == 0 else (w // 2, 0, w, h)))
    k = VIEW_W / im.width
    im = im.resize((VIEW_W, round(im.height * k)), Image.LANCZOS)
    grip = round(im.height * VIEW_GRIP)
    out = Image.new("RGBA", (im.width, 2 * max(grip, im.height - grip)), (0, 0, 0, 0))
    out.alpha_composite(im, (0, out.height // 2 - grip))
    return out


def arts():
    a = {}
    raw = Image.open(f"{SRC}/pistol-front-back.png")
    a["pistol-front"], a["pistol-back"] = pistol_view(raw, 0), pistol_view(raw, 1)
    for n in ("front", "back", "side"):
        im = key(f"{SRC}/cannon-{n}.png")
        k = 2 * CANNON_LEN / axis_length(im)
        a[n] = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
    im = key(f"{SRC}/pistol-side.png")
    k = PISTOL_LEN / im.width
    a["pistol"] = im.resize((PISTOL_LEN, round(im.height * k)), Image.LANCZOS)
    return a


PISTOL_GRIP = (round(GRIP_PAINTED[0] * PISTOL_LEN / GRIP_PAINTED[2]), round(GRIP_PAINTED[1] * PISTOL_LEN / GRIP_PAINTED[2]))
# muzzle direction of each cannon art, degrees clockwise from +x, measured on the cuts
CANNON_BASE = {"front": -44.0, "back": -135.0, "side": 0.0}


def cannon_anchor(art, x, y, aim, flip, behind, length=1.0):
    base = CANNON_BASE[art] if flip > 0 else 180 - CANNON_BASE[art]
    angle = aim - base
    k = SIDE_SX if art == "side" else length
    return dict(x=round(float(x), 2), y=round(float(y), 2), angle=round(float(((angle + 180) % 360) - 180), 2), s=CANNON_S, sx=flip * k, behind=behind, hidden=False, fallback=False, art=f"{art}")


def along(rear, fore, frac_from_tail):
    """Centre of a cannon that passes through both fists, the rear fist at frac_from_tail of its length from the tail."""
    dx, dy = fore[0] - rear[0], fore[1] - rear[1]
    d = math.hypot(dx, dy) or 1
    aim = math.degrees(math.atan2(dy, dx))
    off = (0.5 - frac_from_tail) * CANNON_LEN
    return rear[0] + dx / d * off, rear[1] + dy / d * off, aim


def weapon_manifest(live, pistol):
    """One entry per tier under the tier's name: pistol/<name>-base|enhanced.png, and the cannon's cannon/<name>/ follows it. Grips measured by Tools/build-mechanic-pistol-tiers.py; a tier not drawn yet holds the master, so the master's grip."""
    grip = {"w": pistol.width, "h": pistol.height, "gripX": PISTOL_GRIP[0], "gripY": PISTOL_GRIP[1]}
    tiers = json.load(open(TIER_GRIPS, encoding="utf-8")) if os.path.exists(TIER_GRIPS) else {}
    return [{"name": e["name"], "glow": e["glow"], "baseGrip": tiers.get(e["name"], {}).get("base", grip), "enhancedGrip": tiers.get(e["name"], {}).get("enhanced", grip)} for e in live["weapon"]]


# ---- pictures, as HeroRenderer draws ----
def place(canvas, art, grip, a, pad):
    k = a["s"]
    sx = a.get("sx", 1) or 1
    th = math.radians(a["angle"])
    cos, sin = math.cos(th), math.sin(th)
    ax, bx = cos / (k * sx), sin / (k * sx)
    ay, by = -sin / k, cos / k
    px, py = a["x"] + pad, a["y"] + pad
    canvas.alpha_composite(art.transform(canvas.size, Image.AFFINE, (ax, bx, grip[0] - ax * px - bx * py, ay, by, grip[1] - ay * px - by * py), Image.BICUBIC))


def compose(root, art, anchors, anim, d, f, pad=70):
    mirrored = d == "left" and anim in MIRRORED
    row = 3 if mirrored else ROWS[d]
    body = f"{root}/Sprites/classes/mechanic/body"
    def sheet(layer):
        path = f"{body}/{layer}/{anim}.png"
        if not os.path.exists(path):
            path = f"{BODY}/{layer}/{anim}.png"
        im = Image.open(path).convert("RGBA").crop((f * FRAME, row * FRAME, f * FRAME + FRAME, row * FRAME + FRAME))
        return im.transpose(Image.FLIP_LEFT_RIGHT) if mirrored else im
    canvas = Image.new("RGBA", (FRAME + 2 * pad,) * 2, (0, 0, 0, 0))
    def part(p, behind):
        a = anchors[(p, anim, d)][f]
        if a.get("hidden") or bool(a["behind"]) != behind:
            return
        if p == "cannon":
            im = art[a["art"]]
            place(canvas, im, (im.width / 2, im.height / 2), a, pad)
        else:
            im = art[a["art"]] if a.get("art") else art["pistol"]
            place(canvas, im, (im.width / 2, im.height / 2) if a.get("art") else PISTOL_GRIP, a, pad)
    for behind in (True, False):
        for p in ("cannon", "gunL", "gun"):
            part(p, behind)
        if behind:
            canvas.alpha_composite(sheet("bare"), (pad, pad))
    canvas.alpha_composite(sheet("hand"), (pad, pad))
    return canvas

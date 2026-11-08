"""Mechanic pistols + back cannon sized and placed per tier like the design sheet (2026-11-08, the designer: "same as the design",
outfit, weapons AND their size; exception to "one weapon size" like the Wizard, docs/sprite-rules.md, quy tắc tư thế cầm 3).
Design: Assets/ArtLibrary/classes/mechanic/outfits/design-ladder-v3/mechanic-10-tiers-v3-chatgpt-2.png (T0..T9 left to right).
Everything is measured in hero heights H (hair top to soles): design H ~266 px, game idle-down frame 0 H = 162 (rows 26..188).

Pistols: the art of each tier (cut from its ChatGPT pair by Tools/build-mechanic-pistol-tiers.py) is resampled so that drawn at S 0.26
its width is PISTOL_W[t] H; grip scaled with it. Every animation uses it. Not the design's size: the design's pistols (0.18-0.27 H) are half the
old in-game size and the designer found them too small (2026-11-08: "súng giữ cỡ to như cũ"), so T0 is the old 0.41 H (258 px art at S 0.26)
and the tiers grow a little from there, never smaller.
Idle facing down: the user-approved 2026-11-05 pose, NOT the design sheet's: pistols near-vertical at the hips, barrels down and a little
outward (gunL +IDLE_GUN_ANGLE, gun -IDLE_GUN_ANGLE, = PISTOL_POSE["down"] of build-mechanic-idle-doll-from-chatgpt.py), x/y/behind/art
untouched (PISTOL_SHIFT already in them), only the art is bigger. The v3 pass made them level, muzzles outward like the design; the
user rejected that again on 2026-11-08 (same as 2026-11-02, docs/sprite-rules.md).
Cannon: the three views of each tier (Tools/build-mechanic-cannon-tiers.py cut) are scaled by the tier's factor (centre kept), and a
fourth view `carry` (the front view turned to the design's angle, muzzle where the design has it, baked into the canvas around the anchor)
is what every on-the-back facing-down animation draws (idle, walk, attack1-3, dash, hurt). attack4 / rocket keep `front`.
T0 (scrap) gets its own folder cannon/scrap/ so the shared master art (fit target of the cannon tool) stays untouched.

  python Tools/build-mechanic-tier-weapon-fit.py sheet OUT.png [--captures DIR]   design (top) vs game (bottom), same hero height
  python Tools/build-mechanic-tier-weapon-fit.py write                          pistols (side + per-tier front/back views), cannons, anchors (idle down, attack1-3 down/up aim), glow
--captures DIR: bottom row from in-game shots DIR/z30-t<N>_1.png (900x900, --zoom 30, idle down, e.g. `Zenonia.exe --class mechanic
--level 100 --worn-tier N --wear-tiers N,0,0 --face down --zoom 30 --shots 5 --shot-path DIR/z30-tN`); CAP_HEAD / CAP_H place the hero.
Note: Tools/build-mechanic-dual.py `write` rebuilds the anchors and would drop `carry` and the level idle pistols: re-run `write` here after it.
"""
import importlib.util, json, math, os, re, sys, uuid
import numpy as np
from PIL import Image, ImageDraw

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


pt = load("pt", "Tools/build-mechanic-pistol-tiers.py")
ct = load("ct", "Tools/build-mechanic-cannon-tiers.py")
bd = pt.bd
TIERS = pt.TIERS
DESIGN = "Assets/ArtLibrary/classes/mechanic/outfits/design-ladder-v3/mechanic-10-tiers-v3-chatgpt-2.png"
BODY = f"{bd.RES}/Sprites/classes/mechanic/body"
HEAD0 = f"{bd.RES}/Sprites/classes/mechanic/heads/0/down.png"
H_GAME, HAIR_TOP, FACE_CX = 162.0, 26.0, 96.0
# design sheet, per tier: face centre column, hair top row, sole row (measured 2026-11-08)
D_FACE = [97, 268, 437, 611, 787, 967, 1160, 1365, 1576, 1912]
D_TOP = [250, 250, 250, 250, 250, 250, 250, 247, 250, 248]
D_SOLE = [516, 516, 516, 516, 516, 516, 516, 517, 516, 521]
# pistol width (solid box, drawn) in H, SIZE_K not applied: the old in-game size (0.41 H) at T0, a little more per tier, T9 a step up like the design.
# 2026-11-08 first pass was the design's 0.18-0.27 H (x SIZE_K): the designer found them too small.
PISTOL_W = [0.41, 0.415, 0.42, 0.425, 0.43, 0.435, 0.44, 0.445, 0.45, 0.48]
IDLE_GUN_ANGLE = 72.0  # 0 = level, muzzles outward (design sheet): rejected by the user twice
# The game hero is a chibi: head ~0.52 H wide against ~0.38 H on the design, torso ~15% wider. Cannons scaled by H alone looked a third
# smaller than the design next to head and torso, so sizes get SIZE_K and the muzzle moves out by HEAD_OUT H (half the head-width gap).
SIZE_K, HEAD_OUT = 1.15, 0.07
# cannon: muzzle centre (right of the face centre, below the hair top; H), barrel angle (deg, y down, negative = up-right), muzzle rim width (H)
CANNON_FIT = [(0.29, 0.13, -55, 0.18), (0.33, 0.05, -54, 0.15), (0.28, 0.04, -53, 0.15), (0.33, 0.01, -55, 0.20),
              (0.36, -0.01, -56, 0.21), (0.39, -0.05, -54, 0.23), (0.33, -0.07, -59, 0.26), (0.35, -0.09, -60, 0.27),
              (0.48, -0.09, -48, 0.26), (0.77, -0.34, -39, 0.36)]
CARRY_ANIMS = ("idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt")
# Pistol shots facing down / up aim where the bullet goes (designer 2026-11-08: not sideways). Superseded: the side art turned straight
# down (90/90) was rejected as "lying sideways", the up splay (-60/-120 behind the body) replaced by the back view: see FRONT_ART below.
AIM_ANIMS = ("attack1", "attack2", "attack3")
# Shots facing down / up (designer 2026-11-08: "bẻ lên thẳng về phía trước", the side art turned 90 deg read as lying sideways): per-tier
# views along the barrel, upright at the fist, the fist layer over the grip. Down: FRONT view (muzzle circle toward the viewer, grip
# hanging below), in front of the body. Up: BACK view (rear of the gun toward the viewer, muzzle hidden), in front of the body where the
# up sheet has a fist layer there, else behind it. Sources: VIEW_SHEETS (ChatGPT, 5 tiers per image, left to right, cut by key colour);
# the art is scaled so its height is FRONT_H_K x the tier's side-art height, and padded so the hand-hold point (FRONT_GRIP of the art
# height, middle of the handle under the barrel) is the canvas centre (GearOverlays.CenteredTier pivots anchor art at its centre).
# Written to pistol/<tier>/<view>.png. Which hand aims per anim: AIM_HANDS (designer 2026-11-08: attack1 = right arm, attack2 = left
# arm, attack3 = both; gun = the hero's right hand); the other hand rests like idle (side art, +-IDLE_GUN_ANGLE). Down rows were
# repainted for this by Tools/build-mechanic-attack-down-repaint.py; the up rows are the old ones, so both up hands aim.
FRONT_DIR = f"{bd.SRC}/tiers/front-view"
VIEW_SHEETS = {
    "pistol-front": [("front-a-chatgpt-1.png", (255, 0, 255), TIERS[:5]), ("front-b-chatgpt-1.png", (0, 255, 0), TIERS[5:])],
    "pistol-back": [("back-a-chatgpt-1.png", (255, 0, 255), TIERS[:5]), ("back-b-chatgpt-1.png", (0, 255, 0), TIERS[5:])],
}
FRONT_ART = {"down": "pistol-front", "up": "pistol-back"}
# Up rows repainted the same way per the approved board (2026-11-08, Tools/build-mechanic-attack-down-repaint.py): attack1 the right arm
# (screen right from behind) raised, attack2 the left, attack3 both.
AIM_HANDS = {("down", "attack1"): ("gun",), ("down", "attack2"): ("gunL",), ("down", "attack3"): ("gun", "gunL"),
             ("up", "attack1"): ("gun",), ("up", "attack2"): ("gunL",), ("up", "attack3"): ("gun", "gunL")}
# frames whose shooting arm is already back down at the side (repainted rows): that hand rests like idle
AIM_REST_FRAMES = {("down", "attack1"): (3,), ("down", "attack2"): (3,), ("down", "attack3"): (5,), ("up", "attack3"): (5,)}
FRONT_H_K, FRONT_GRIP = 1.12, 0.66
GLOW = ["#4fd0ff", "#8fd14f", "#ff8a3d", "#3f7bff", "#2fe0a0", "#ffc93d", "#59e4ff", "#ff3b3b", "#ffd95a", "#ff4fe0"]
CAP_HEAD, CAP_H = (450, 243), 349.0  # in-game shot: face centre column, hair top row, hero height (refined on the first shots)
UP, DOWN, H_OUT = 0.75, 0.08, 240
SIDE = [0.62] * 9 + [1.05]  # crop half-width per tier (T9 cannon and wings reach far)


def anchor(part, anim="idle", d="down"):
    data = json.load(open(bd.GEAR_DUAL, encoding="utf-8"))
    return next(a for a in data["anchors"] if a["part"] == part and a["anim"] == anim and a["dir"] == d)["frames"][0]


def pistol_tier(t):
    """{kind: (image, grip)} of tier t at its design width."""
    done = pt.cut_pair(TIERS[t])
    base = done["base"][0]
    xs = np.flatnonzero((np.asarray(base)[..., 3] > 128).any(0))
    k = PISTOL_W[t] * H_GAME / bd.PISTOL_S / (xs.max() - xs.min() + 1)
    out = {}
    for kind, (im, grip, _) in done.items():
        small = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        out[kind] = (small, {"w": small.width, "h": small.height, "gripX": round(grip["gripX"] * k), "gripY": round(grip["gripY"] * k)})
    return out


def key_cut(path, key, count):
    """The `count` biggest pieces of a flat key-colour image, left to right, as straight RGBA cropped to their box (key spill removed)."""
    from scipy import ndimage
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    on = [i for i in range(3) if key[i] > 128]
    off = [i for i in range(3) if key[i] <= 128]
    lo_on, hi_off = rgb[..., on].min(-1), rgb[..., off].max(-1)
    alpha = 1 - np.clip((lo_on - hi_off - 30) / 150, 0, 1)
    a = alpha[..., None]
    k = np.array(key, np.float32)
    out = np.clip(np.where(a > 0.02, (rgb - (1 - a) * k) / np.maximum(a, 0.02), 0), 0, 255)
    spill = (lo_on > hi_off) & (alpha < 0.98)
    for i in on:
        out[..., i] = np.where(spill, np.minimum(out[..., i], hi_off), out[..., i])
    lab, n = ndimage.label(alpha > 0.5, structure=np.ones((3, 3)))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    pieces = []
    for i in 1 + np.argsort(sizes)[::-1][:count]:
        keep = ndimage.binary_fill_holes(lab == i)
        keep = ndimage.binary_dilation(keep, iterations=2)
        ys, xs = np.nonzero(keep)
        part = np.dstack([out, alpha * keep * 255])[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        pieces.append((xs.min(), Image.fromarray(part.astype(np.uint8), "RGBA")))
    return [im for _, im in sorted(pieces, key=lambda p: p[0])]


def front_tier(t, view="pistol-front"):
    """(view image padded so the hand-hold point is the centre, (grip x, grip y) in the unpadded art) of tier t, sized to its side art."""
    tier = TIERS[t]
    name, key, tiers = next(sh for sh in VIEW_SHEETS[view] if tier in sh[2])
    im = key_cut(f"{FRONT_DIR}/{name}", key, len(tiers))[tiers.index(tier)]
    side = pistol_tier(t)["base"][0]
    ys = np.flatnonzero((np.asarray(side)[..., 3] > 128).any(1))
    k = FRONT_H_K * (ys.max() - ys.min() + 1) / im.height
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    m = np.asarray(im)[..., 3] > 128
    gy = round(FRONT_GRIP * im.height)
    xs = np.flatnonzero(m[gy])
    gx = (xs.min() + xs.max()) / 2 if len(xs) else im.width / 2
    hw = math.ceil(max(gx, im.width - gx)) + 1
    hh = max(gy, im.height - gy) + 1
    canvas = Image.new("RGBA", (2 * hw, 2 * hh), (0, 0, 0, 0))
    canvas.paste(im, (round(hw - gx), hh - gy), im)
    return canvas, (gx, gy)


def views_of(t):
    """Front/back/side at the master's size: tier 0 = the shared live art, others cut from their ChatGPT image."""
    if t == 0:
        return {v: ct.live(v) for v in ct.VIEWS}
    return ct.cut(TIERS[t])[0]


def rot_point(p, size, deg):
    """Where point p of an image of `size` lands after PIL rotate(deg, expand=True)."""
    w, h = size
    th = math.radians(deg)
    c, s = math.cos(th), math.sin(th)
    corners = [(-w / 2, -h / 2), (w / 2, -h / 2), (-w / 2, h / 2), (w / 2, h / 2)]
    rot = lambda x, y: (x * c + y * s, -x * s + y * c)  # PIL turns counter-clockwise on a y-down image
    rc = [rot(*q) for q in corners]
    nw = max(q[0] for q in rc) - min(q[0] for q in rc)
    nh = max(q[1] for q in rc) - min(q[1] for q in rc)
    x, y = rot(p[0] - w / 2, p[1] - h / 2)
    return x + nw / 2, y + nh / 2


def carry(front, t):
    """(carry image, scale k): the front view turned to the design angle, scaled to the design rim width, muzzle at the design spot."""
    mx, my, ang, rim = CANNON_FIT[t]
    a = ct.axis_angle(front)
    flat = front.rotate(a, expand=True, resample=Image.BICUBIC)  # muzzle (up-right in the front view) now to the right
    m = np.asarray(flat)[..., 3] > 128
    xs = np.flatnonzero(m.any(0))
    x0, x1 = xs.min(), xs.max()
    tail = m[:, x1 - max(3, (x1 - x0) // 8):x1 + 1]
    ys = np.flatnonzero(tail.any(1))
    d_art = ys.max() - ys.min() + 1
    muzzle = (x1 - d_art * 0.2, (ys.min() + ys.max()) / 2)
    k = rim * SIZE_K * H_GAME / (ct.bd.CANNON_S * d_art)
    small = flat.resize((round(flat.width * k), round(flat.height * k)), Image.LANCZOS)
    muzzle = (muzzle[0] * k, muzzle[1] * k)
    turned = small.rotate(-ang, expand=True, resample=Image.BICUBIC)
    mz = rot_point(muzzle, small.size, -ang)
    an = anchor("cannon")
    target = (FACE_CX + (mx + HEAD_OUT) * H_GAME, HAIR_TOP + my * H_GAME)
    px = mz[0] - (target[0] - an["x"]) / ct.bd.CANNON_S
    py = mz[1] - (target[1] - an["y"]) / ct.bd.CANNON_S
    hw = math.ceil(max(px, turned.width - px)) + 2
    hh = math.ceil(max(py, turned.height - py)) + 2
    canvas = Image.new("RGBA", (2 * hw, 2 * hh), (0, 0, 0, 0))
    canvas.paste(turned, (round(hw - px), round(hh - py)), turned)
    return canvas, k


def cannon_tier(t):
    v = views_of(t)
    c, k = carry(v["front"], t)
    out = {n: im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS) for n, im in v.items()}
    out["carry"] = c
    return out, k


def compose(t, guns, cannon, pad=150):
    """Idle down frame 0 as HeroRenderer draws it: cannon (behind), headless body + head 0, pistols, fist layer."""
    sub = f"{BODY}/set-t{t}" if t and os.path.exists(f"{BODY}/set-t{t}/bare/idle.png") else BODY
    c = Image.new("RGBA", (192 + 2 * pad,) * 2, (0, 0, 0, 0))
    an = dict(anchor("cannon"), angle=0.0, sx=1)
    bd.place(c, cannon, (cannon.width / 2, cannon.height / 2), an, pad)
    c.alpha_composite(Image.open(f"{sub}/headless/idle.png").convert("RGBA").crop((0, 0, 192, 192)), (pad, pad))
    c.alpha_composite(Image.open(HEAD0).convert("RGBA"), (pad, pad))
    for part, sx in (("gunL", 1), ("gun", -1)):
        im, grip = guns
        bd.place(c, im, (grip["gripX"], grip["gripY"]), dict(anchor(part), angle=IDLE_GUN_ANGLE * sx, sx=sx), pad)
    c.alpha_composite(Image.open(f"{sub}/hand/idle.png").convert("RGBA").crop((0, 0, 192, 192)), (pad, pad))
    return c, pad


def crop_hero(im, cx, top, H, side):
    box = (cx - side * H, top - UP * H, cx + side * H, top + H + DOWN * H)
    k = H_OUT / H
    return im.crop(tuple(int(round(v)) for v in box)).resize((round((box[2] - box[0]) * k), round((box[3] - box[1]) * k)), Image.LANCZOS)


def sheet(out, caps=None):
    design = Image.open(DESIGN).convert("RGB")
    cells = []
    for t in range(10):
        dc = crop_hero(design, D_FACE[t], D_TOP[t], D_SOLE[t] - D_TOP[t], SIDE[t])
        if caps:
            gc = crop_hero(Image.open(f"{caps}/z30-t{t}_1.png").convert("RGB"), CAP_HEAD[0], CAP_HEAD[1], CAP_H, SIDE[t])
        else:
            g = pistol_tier(t)["base"]
            c, pad = compose(t, g, cannon_tier(t)[0]["carry"])
            bg = Image.new("RGBA", c.size, (0, 255, 0, 255))
            bg.alpha_composite(c)
            gc = crop_hero(bg.convert("RGB"), pad + FACE_CX, pad + HAIR_TOP, H_GAME, SIDE[t])
        cells.append((dc, gc))
    h = cells[0][0].height
    xs = np.cumsum([0] + [dc.width for dc, _ in cells])
    s = Image.new("RGB", (int(xs[-1]), h * 2 + 20), (58, 62, 70))
    d = ImageDraw.Draw(s)
    for i, (dc, gc) in enumerate(cells):
        x = int(xs[i])
        s.paste(dc, (x, 0))
        s.paste(gc.resize(dc.size), (x, h + 20))
        d.text((x + 6, h + 4), f"T{i} design / {'in game' if caps else 'offline'}", fill=(255, 255, 160))
        d.line([(x, 0), (x, s.height)], fill=(58, 62, 70), width=2)
        for y0 in (0, h + 20):
            for y in (round(UP * H_OUT), round(UP * H_OUT) + H_OUT):
                d.line([(x, y0 + y), (x + 10, y0 + y)], fill=(255, 60, 60), width=2)
    s.save(out)
    print(out, s.size)


def meta_like(src, dst):
    if not os.path.exists(dst + ".meta"):
        raw = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, open(src + ".meta", encoding="utf-8").read(), count=1)
        open(dst + ".meta", "w", encoding="utf-8").write(raw)


def write():
    data = json.load(open(bd.GEAR_DUAL, encoding="utf-8"))
    grips = pt.read_grips()
    for t, tier in enumerate(TIERS):
        for kind, (im, grip) in pistol_tier(t).items():
            p = f"{pt.PISTOL}/{tier}-{kind}.png"
            meta_like(f"{pt.PISTOL}/pistol-base.png", p)
            im.save(p)
            data["weapon"][t][kind + "Grip"] = grip
            grips.setdefault(tier, {})[kind] = grip
        data["weapon"][t]["glow"] = GLOW[t]
        views, k = cannon_tier(t)
        folder = f"{ct.CANNON}/{tier}"
        if not os.path.isdir(folder):
            meta_like(f"{ct.CANNON}/brass", folder)
            os.makedirs(folder)
        for v, im in views.items():
            meta_like(f"{ct.CANNON}/side.png", f"{folder}/{v}.png")
            im.save(f"{folder}/{v}.png")
        pfolder = f"{pt.PISTOL}/{tier}"
        if not os.path.isdir(pfolder):
            meta_like(f"{ct.CANNON}/brass", pfolder)
            os.makedirs(pfolder)
        for view in VIEW_SHEETS:
            meta_like(f"{pt.PISTOL}/{view}.png", f"{pfolder}/{view}.png")
            front_tier(t, view)[0].save(f"{pfolder}/{view}.png")
        print(tier, "pistol", data["weapon"][t]["baseGrip"], "cannon k", round(k, 3), "carry", views["carry"].size)
    for a in data["anchors"]:
        if a["anim"] in AIM_ANIMS and a["part"] in ("gun", "gunL") and a["dir"] in FRONT_ART:
            hands = Image.open(f"{BODY}/hand/{a['anim']}.png").convert("RGBA")
            row = bd.ROWS[a["dir"]]
            for i, f in enumerate(a["frames"]):
                if a["part"] in AIM_HANDS[(a["dir"], a["anim"])] and i not in AIM_REST_FRAMES.get((a["dir"], a["anim"]), ()):  # upright at the fist, along the barrel
                    al = np.asarray(hands.crop((i * 192, row * 192, i * 192 + 192, row * 192 + 192)))[..., 3]
                    x, y = int(round(f["x"])), int(round(f["y"]))
                    fist = al[max(0, y - 4):y + 5, max(0, x - 4):x + 5].max() > 40 if 0 <= x < 192 and 0 <= y < 192 else False
                    f.update(art=FRONT_ART[a["dir"]], angle=0.0, sx=1.0, behind=a["dir"] == "up" and not fist)
                else:  # resting hand: side art hanging like idle down (from behind the left hand is on screen left: muzzle out = the other way)
                    f.pop("art", None)
                    sgn = (1 if a["part"] == "gunL" else -1) * (-1 if a["dir"] == "up" else 1)
                    f.update(angle=IDLE_GUN_ANGLE * sgn, sx=float(sgn), behind=False)
        if a["dir"] != "down" or a["anim"] != "idle" and a["anim"] not in CARRY_ANIMS:
            continue
        for f in a["frames"]:
            if a["part"] == "cannon" and a["anim"] in CARRY_ANIMS:
                f.update(art="carry", angle=0.0, sx=1.0)
            elif a["part"] in ("gun", "gunL") and a["anim"] == "idle":
                f.update(angle=IDLE_GUN_ANGLE * (1 if a["part"] == "gunL" else -1))
    json.dump(grips, open(pt.GRIPS, "w", encoding="utf-8"), indent=1)
    json.dump(data, open(bd.GEAR_DUAL, "w", encoding="utf-8"), separators=(",", ":"))
    print("written")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "sheet":
        sheet(sys.argv[2], sys.argv[sys.argv.index("--captures") + 1] if "--captures" in sys.argv else None)
    elif cmd == "write":
        write()
    else:
        print(__doc__)

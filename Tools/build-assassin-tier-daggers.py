"""Assassin daggers of the 10 armour tiers, sized and angled like the design sheet (2026-11-08, the designer: outfit, weapons AND their size
as docs/reference/assassin-sets-10-tiers-ladder-v3.png; exception to "one weapon size" like the Wizard and Mechanic, docs/sprite-rules.md).

ChatGPT drew one upright dagger per tier (tip up), copied from crops of the design (gear-src/design-daggers-*-x3.png):
gear-src/daggers-t1-t5-chatgpt-N.png (5 daggers on #FF00FF, T3 is green) and daggers-t6-t9-chatgpt-N.png (4 on #00FF00).
T0 keeps the current hero-select dagger (iron-kunai), only resized. OVERRIDE: one-dagger redraws of a tier (T8 v2: dagger-t8-v2-chatgpt-1.png). Old art: ArtLibrary/classes/assassin/new-style/old/dagger-pre-tier-sets/.

Size (the designer, 2026-11-08, revised the same day): daggers stay BIG as before the tier sets, not the design-sheet ratio: art length
tip to pommel DAGGER_LEN_PX[t] = 400 px (old size, 126 frame px at S 0.243 x DrawScale 1.3, every animation) + 5 px per tier.
Written over the manifest names (iron-kunai .. void-reaper),
enhanced file = base art, glow per tier palette, weapon-/offhand-assassin-N icons. Idle facing down: both daggers point down and a bit
out like the design (IDLE_ANGLE, anchors in held-assassin.json and held-assassin-melee.json, fists unchanged).

  python Tools/build-assassin-tier-daggers.py check OUT_DIR     cut + composite idle-down frame 0 per tier (outfit body if written)
  python Tools/build-assassin-tier-daggers.py write             install art, grips, glow, icons, idle-down angle
"""
import importlib.util, json, os, shutil, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
RES = "Assets/Resources"
SRC = "Assets/ArtLibrary/classes/assassin/outfits/gear-src"
LIVE = f"{RES}/Sprites/classes/assassin/weapons/dagger"
GEAR = f"{RES}/Data/classes/assassin/held-assassin.json"
MELEE = f"{RES}/Data/classes/assassin/held-assassin-melee.json"
ICONS = f"{RES}/Sprites/icons"
BODY = f"{RES}/Sprites/classes/assassin/body"
OLD = "Assets/ArtLibrary/classes/assassin/new-style/old/dagger-pre-tier-sets"
FRAME = 192
S, DRAW_SCALE = 0.243, 1.3
SHEETS = {"t1-t5": ("magenta", 5), "t6-t9": ("green", 4)}
VERSION = {"t1-t5": 1, "t6-t9": 1}  # which daggers-<group>-chatgpt-N.png is in use
# single-dagger redraws that replace a tier of the group sheets: tier -> (file in SRC, key). T8 redrawn 2026-11-08 with the grander T8 outfit
OVERRIDE = {8: ("dagger-t8-v2-chatgpt-1.png", "green")}
# art length tip to pommel in px (2026-11-08 the designer: daggers stay BIG as before, not the design-sheet ratio). The old size was
# 400 art px for every tier (build-assassin-new-style.DAGGER_LEN, 126 frame px at S x DRAW_SCALE); higher tiers a little larger, never smaller.
DAGGER_LEN_PX = [400 + 5 * t for t in range(10)]
GRIP_FROM_BOTTOM = {}  # tier: handle row as a fraction of the art height from the bottom, when the measured grip is wrong
IDLE_ANGLE = 115.0  # idle facing down: main (screen left) -IDLE_ANGLE, off +IDLE_ANGLE (tip down-outward, the pre-tier angle: big daggers at 165 reach 50 px below the soles)
GLOW = ["#c259ff", "#ffa53d", "#ffe14d", "#59e06a", "#4fd8ff", "#ff7a1f", "#ffe680", "#ff2b3b", "#c77dff", "#3fe8d0"]


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


ans = load("ans", "Tools/build-assassin-new-style.py")
chf = load("chf", "Tools/compose-hero-frame.py")


def key_out(path, key):
    """Straight RGBA floats by distance from the pure key colour, key unmixed from soft edge pixels (as build-wizard-tier-staff-book)."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    kc = np.array([0, 255, 0] if key == "green" else [255, 0, 255], np.float32)
    d = np.maximum(np.maximum(r, b), 255 - g) if key == "green" else np.maximum(np.maximum(255 - r, g), 255 - b)
    alpha = np.clip((d - 40) / 70, 0, 1)
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    a = np.maximum(alpha[..., None], 0.02)
    out = np.clip((rgb - (1 - a) * kc) / a, 0, 255)
    return np.dstack([out, alpha * 255]).astype(np.float32)


def split(a, count):
    """The count biggest pieces left to right (glow sparks within 8 px joined to their dagger), each trimmed."""
    solid = a[..., 3] > 8
    lab, n = ndimage.label(ndimage.binary_dilation(solid, iterations=8))
    sizes = ndimage.sum(solid, lab, range(1, n + 1))
    big = np.argsort(sizes)[::-1][:count]
    objs = ndimage.find_objects(lab)
    out = []
    for i in sorted(big, key=lambda i: objs[i][1].start):
        o = objs[i]
        out.append(a[o] * ((lab[o] == i + 1) & solid[o])[..., None])
    return out


def figures():
    """11 upright RGBA float arts, tier order; T0 = the live hero-select dagger (from the backup once written)."""
    t0 = f"{OLD}/iron-kunai-base.png" if os.path.exists(f"{OLD}/iron-kunai-base.png") else f"{LIVE}/iron-kunai-base.png"
    a0 = np.asarray(Image.open(t0).convert("RGBA")).astype(np.float32)
    ys, xs = np.where(a0[..., 3] > 8)
    out = [a0[ys.min():ys.max() + 1, xs.min():xs.max() + 1]]
    for g, (key, n) in SHEETS.items():
        out += split(key_out(f"{SRC}/daggers-{g}-chatgpt-{VERSION[g]}.png", key), n)
    for t, (name, key) in OVERRIDE.items():
        if os.path.exists(f"{SRC}/{name}"):
            out[t] = split(key_out(f"{SRC}/{name}", key), 1)[0]
    return out


def resize(a, w, h):
    """Premultiplied resample of straight RGBA floats."""
    pm = a.copy()
    pm[..., :3] *= a[..., 3:4] / 255
    out = np.dstack([np.array(Image.fromarray(pm[..., c], "F").resize((w, h), Image.LANCZOS)) for c in range(4)])
    out = np.clip(out, 0, 255)
    al = np.maximum(out[..., 3:4], 1e-3) / 255
    out[..., :3] = np.clip(out[..., :3] / al, 0, 255)
    out[out[..., 3] < 2] = 0
    return out


def fit(fig, t):
    """(RGBA image, grip) of tier t at DAGGER_LEN_PX[t]; grip from the handle measure (ans.measure_grip) unless overridden."""
    rows = DAGGER_LEN_PX[t]
    k = rows / fig.shape[0]
    small = resize(fig, max(1, round(fig.shape[1] * k)), round(rows))
    pad = 4
    canvas = np.zeros((small.shape[0] + 2 * pad, small.shape[1] + 2 * pad, 4), np.float32)
    canvas[pad:pad + small.shape[0], pad:pad + small.shape[1]] = small
    h = canvas.shape[0]
    if t in GRIP_FROM_BOTTOM:
        gy = int(round(h - pad - GRIP_FROM_BOTTOM[t] * small.shape[0]))
        xs = np.where(canvas[gy, :, 3] > 150)[0]
        gx = int(round((xs[0] + xs[-1]) / 2))
    else:
        gx, gy, _ = ans.measure_grip(canvas[..., 3])
    img = Image.fromarray(canvas.round().astype(np.uint8), "RGBA")
    return img, {"w": canvas.shape[1], "h": h, "gripX": int(gx), "gripY": int(gy)}


def daggers():
    return [fit(f, t) for t, f in enumerate(figures())]


def idle_anchor(part):
    x, y = (63, 148) if part == "main" else (129, 148)  # Lv1 idle-down fists (build-assassin-idle-down-doll.FISTS)
    return {"x": float(x), "y": float(y), "angle": -IDLE_ANGLE if part == "main" else IDLE_ANGLE, "s": S, "sx": 1, "behind": False, "hidden": False, "fallback": False}


def compose(t, art, grip, pad=60):
    """Idle-down frame 0 as the game draws it: outfit bare (Lv1 if none; bare = headless + head 0), daggers, fists."""
    sub = f"{BODY}/set-t{t}" if t and os.path.exists(f"{BODY}/set-t{t}/bare/idle.png") else BODY
    c = Image.new("RGBA", (FRAME + 2 * pad, FRAME + 2 * pad), (0, 0, 0, 0))
    c.alpha_composite(chf.cell(f"{sub}/bare/idle.png", 0, 0), (pad, pad))
    for part in ("off", "main"):
        chf.place(c, art, grip, idle_anchor(part), DRAW_SCALE, 0.0, pad)
    c.alpha_composite(chf.cell(f"{sub}/hand/idle.png", 0, 0), (pad, pad))
    return c


def check(out):
    os.makedirs(out, exist_ok=True)
    ds = daggers()
    size = FRAME + 120
    sheet = Image.new("RGBA", (size * 10, size), (58, 62, 70, 255))
    arts = Image.new("RGBA", (130 * 10, 260), (58, 62, 70, 255))
    d = ImageDraw.Draw(arts)
    for t, (art, grip) in enumerate(ds):
        print(t, grip)
        sheet.alpha_composite(compose(t, art, grip), (t * size, 0))
        arts.alpha_composite(art, (t * 130 + 5, 5))
        x, y = t * 130 + 5 + grip["gripX"], 5 + grip["gripY"]
        d.line([(x - 5, y), (x + 5, y)], fill=(0, 255, 255)); d.line([(x, y - 5), (x, y + 5)], fill=(0, 255, 255))
    sheet.save(f"{out}/assassin-tier-daggers-idle-down.png")
    sheet.resize((sheet.width * 85 // 174, sheet.height * 85 // 174), Image.LANCZOS).save(f"{out}/assassin-tier-daggers-idle-down-gamesize.png")
    arts.save(f"{out}/assassin-tier-daggers-art-grips.png")
    print(out)


def icon(img, path, tilt):
    im = img.rotate(tilt, expand=True, resample=Image.BICUBIC)
    im = im.crop(im.getbbox())
    k = 88 / max(im.size)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    out.paste(im, ((96 - im.width) // 2, (96 - im.height) // 2), im)
    out.save(path)


def write():
    os.makedirs(f"{OLD}/icons", exist_ok=True)
    data = json.load(open(GEAR, encoding="utf-8"))
    for t, (art, grip) in enumerate(daggers()):
        n = data["weapon"][t]["name"]
        for kind in ("base", "enhanced"):
            live = f"{LIVE}/{n}-{kind}.png"
            if not os.path.exists(f"{OLD}/{n}-{kind}.png"):
                shutil.copy2(live, f"{OLD}/{n}-{kind}.png")
            art.save(live)
        data["weapon"][t].update(glow=GLOW[t], baseGrip=grip, enhancedGrip=dict(grip))
        for prefix, tilt in (("weapon", -45), ("offhand", 45)):
            ic = f"{ICONS}/{prefix}-assassin-{t}.png"
            if not os.path.exists(f"{OLD}/icons/{prefix}-assassin-{t}.png"):
                shutil.copy2(ic, f"{OLD}/icons/{prefix}-assassin-{t}.png")
            icon(art, ic, tilt)
    open(GEAR, "w", encoding="utf-8").write(json.dumps(data, separators=(",", ":")))
    for path, kw in ((GEAR, {"separators": (",", ":")}), (MELEE, {"indent": 1})):
        m = json.load(open(path, encoding="utf-8"))
        for e in m["anchors"]:
            if e["anim"] == "idle" and e["dir"] == "down" and e["part"] in ("main", "off"):
                e["frames"] = [idle_anchor(e["part"]) for _ in e["frames"]]
        open(path, "w", encoding="utf-8").write(json.dumps(m, **kw))
    print("daggers written, idle-down angle", IDLE_ANGLE)


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "check":
        check(sys.argv[2])
    elif sys.argv[1:2] == ["write"]:
        write()
    else:
        print(__doc__)

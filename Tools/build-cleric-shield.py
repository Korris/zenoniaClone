"""Cleric shields (offhand), cut from ChatGPT art on flat #FF00FF, and the per-frame anchors of the shield part.

Sources: Assets/ArtLibrary/classes/cleric/shield-src/<tier>-pair-chatgpt.png: base on the left, enhanced on the right. Old art and data: shield-src/old/.
Writes:  Resources/Sprites/classes/cleric/weapons/shield/<tier>-base.png, <tier>-enhanced.png (same canvas and grip for both), .meta like the Warrior's shields,
         the `shield` list in held-cleric.json (only the tiers that have art: HeroRenderer clamps a higher tier to the last one),
         the shield anchors in held-cleric-melee.json, Sprites/icons/offhand-cleric-<n>.png.

Size: DrawScale 0.8 and s = 0.27 as the Warrior's shield, so 0.216 frame px per art px. HEIGHT = 232 art px gives about 50 frame px tall and wide: the Warrior's is 50 x 71, she is a caster and must not hide behind it.
Grip: centre of the shield's box; the hand is hidden behind the face.

Draw order follows rule 4 of docs/sprite-rules.md. The staff is in her right hand, so the shield is on the left arm:
  facing down: screen right, in front of the body       facing up: screen left, behind the body
  facing left: near arm, in front of the body           facing right: far arm, behind the body
The shield faces outward and hides its own hand, as the Warrior's does, so body/hand keeps only the staff fist and nothing is added for the shield.
FREE_FIST holds the free hand of each frame, read from the bare sheets. None = no place for it (the arm is across the body or hidden): the shield is hidden on that frame.

Usage (from the project root; needs Pillow, numpy and scipy):
  python Tools/build-cleric-shield.py cut OUT_DIR [tier...]      cut into OUT_DIR, prints silhouette overlap with acolyte; no game file touched
  python Tools/build-cleric-shield.py install [tier...]          art, metas, grips, icons of those tiers (default: every tier with a source)
  python Tools/build-cleric-shield.py anchors                    writes the shield anchors
  python Tools/build-cleric-shield.py sheet OUT_DIR              all tiers on dark and light, icons new over old
  python Tools/build-cleric-shield.py preview OUT_DIR [tier]     every anim and direction as the game draws it, from installed art and anchors"""
import importlib.util, json, os, re, sys, uuid
import numpy as np
from PIL import Image
from scipy import ndimage

SRC = "Assets/ArtLibrary/classes/cleric/shield-src"
RES = "Assets/Resources"
OUT = f"{RES}/Sprites/classes/cleric/weapons/shield"
ICONS = f"{RES}/Sprites/icons"
GEAR = f"{RES}/Data/classes/cleric/held-cleric.json"
MELEE = f"{RES}/Data/classes/cleric/held-cleric-melee.json"
WARRIOR_META = f"{RES}/Sprites/classes/warrior/weapons/shield/iron-base.png.meta"
TIERS = ["acolyte", "blossom", "dawn", "seraph", "moonlit", "radiant", "halo", "verdant", "celestial", "archangel"]
GLOW = {}  # filled from the staff entry of the same tier
HEIGHT, PAD = 232, 3
S = 0.27
FRAME = 192
ROWS = {"down": 0, "up": 1, "left": 2, "right": 3}
COLS = {"idle": 4, "walk": 4, "hurt": 2, "attack1": 4, "attack2": 4, "attack3": 6, "clericcast": 3, "dash": 3}
BEHIND = {"down": False, "up": True, "left": False, "right": True}
TILT = {"down": 5, "up": -5, "left": -8, "right": 8}

# Cell px of the free hand; where it is level with the chin facing down, the shield moves out and down, the hand still behind it, so it does not cover her face. idle/down is baked art (fallback): the shield is painted into that row, see Tools/build-idle-down-set.py.
# clericcast and dash are the old winged art: the cast row holds the staff in whichever hand is raised, so the shield would change hands. Hidden there.
FREE_FIST = {
    ("idle", "down"): [(140, 142)] * 4, ("idle", "up"): [(61, 138), (62, 137), (62, 137), (62, 137)], ("idle", "left"): [(105, 150)] * 4, ("idle", "right"): [(85, 149)] * 4,
    ("walk", "down"): [(131, 137), (128, 136), (133, 137), (130, 138)], ("walk", "up"): [(61, 138), (62, 138), (58, 139), (61, 138)],
    ("walk", "left"): [(115, 150), (90, 151), (120, 143), (89, 150)], ("walk", "right"): [(82, 143), (105, 148), (82, 140), (86, 149)],
    ("hurt", "down"): [(136, 130), (136, 130)], ("hurt", "up"): [(42, 112), (49, 122)], ("hurt", "left"): [(122, 138), (112, 146)], ("hurt", "right"): [(58, 84), (86, 130)],
    ("attack1", "down"): [(127, 133), (127, 134), (122, 137), (126, 134)], ("attack1", "up"): [(56, 140), (54, 140), (55, 140), (54, 140)],
    ("attack1", "left"): [(116, 148), (132, 148), (111, 148), (116, 142)], ("attack1", "right"): [(86, 146), (95, 146), (100, 146), (100, 146)],
    ("attack2", "down"): [(130, 137), (127, 135), (118, 146), (127, 138)], ("attack2", "up"): [(61, 135), (65, 135), (62, 135), (61, 135)],
    ("attack2", "left"): [(116, 146), (130, 146), (118, 141), (116, 146)], ("attack2", "right"): [(75, 142), (65, 145), (76, 142), (76, 140)],
    ("attack3", "down"): [(130, 142), (128, 142), (118, 136), (120, 142), (120, 134), (118, 146)], ("attack3", "up"): [(69, 139), (66, 139), (71, 139), (69, 139), (67, 139), (68, 139)],
    ("attack3", "left"): [(110, 150), (110, 150), (110, 150), (116, 150), (116, 150), (116, 153)], ("attack3", "right"): [(75, 143), (74, 143), (74, 143), (74, 143), (75, 143), (75, 143)],
}
IDLE_LIFT = [0.0, -0.4, -0.8, -0.4]  # the breathing of the idle rows at hand height


def key_magenta(path):
    """Straight-alpha floats. Only magenta joined to the image edge is background, edge pixels are unmixed from the key, a leftover pink lean is removed."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(np.float64)
    excess = np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1]
    key = np.median(np.concatenate([rgb[:20, :20].reshape(-1, 3), rgb[-20:, -20:].reshape(-1, 3), rgb[:20, -20:].reshape(-1, 3), rgb[-20:, :20].reshape(-1, 3)]), axis=0)
    e_bg = float(min(key[0], key[2]) - key[1])
    lab, _ = ndimage.label(excess > 0.5 * e_bg)
    edge = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    outside = np.isin(lab, edge[edge > 0])
    band = ndimage.binary_dilation(outside, iterations=5)
    alpha = np.ones(excess.shape)
    alpha[band] = 1 - np.clip((excess[band] - 0.10 * e_bg) / (0.75 * e_bg), 0, 1)
    alpha[outside] = 0
    a = alpha[..., None]
    out = np.clip(np.where(a > 0.02, (rgb - (1 - a) * key) / np.maximum(a, 0.02), 0), 0, 255)
    # The art has no pink: what is left is key colour reflected on white enamel and in soft glows.
    lean = np.maximum(np.minimum(out[..., 0], out[..., 2]) - out[..., 1] - 6, 0)
    out[..., 0] -= lean
    out[..., 2] -= lean
    alpha[:6], alpha[-6:], alpha[:, :6], alpha[:, -6:] = 0, 0, 0, 0
    return np.dstack([out, alpha * 255])


def resize(rgba, w, h):
    """Lanczos on premultiplied channels, so transparent pixels leak no colour into the edge."""
    a = rgba[..., 3:] / 255
    bands = [Image.fromarray((rgba[..., i] * a[..., 0]).astype(np.float32), "F").resize((w, h), Image.LANCZOS) for i in range(3)]
    alpha = np.clip(np.asarray(Image.fromarray(rgba[..., 3].astype(np.float32), "F").resize((w, h), Image.LANCZOS)).astype(np.float64), 0, 255)
    k = np.maximum(alpha / 255, 1e-3)
    return np.dstack([np.clip(np.asarray(b) / k, 0, 255) for b in bands] + [alpha])


def cut_pair(tier):
    """Base and enhanced, cropped, with the master's aspect once `size` is given."""
    rgba = key_magenta(f"{SRC}/{tier}-pair-chatgpt.png")
    lab, n = ndimage.label(rgba[..., 3] > 40)
    sizes = ndimage.sum(lab > 0, lab, range(1, n + 1))
    two = sorted(1 + np.argsort(sizes)[-2:], key=lambda i: np.where(lab == i)[1].mean())
    out = []
    for i in two:
        keep = lab == i
        near = ndimage.binary_dilation(keep, iterations=3)
        one = rgba.copy()
        one[..., 3][~near] = 0
        ys, xs = np.where(keep)
        out.append(one[ys.min():ys.max() + 1, xs.min():xs.max() + 1])
    return out


def master_size():
    base, _ = cut_pair("acolyte")
    return round(base.shape[1] * HEIGHT / base.shape[0]), HEIGHT


def overlap(a, b):
    x, y = a[..., 3] > 128, b[..., 3] > 128
    return (x & y).sum() / (x | y).sum()


def on_canvas(art):
    h, w = art.shape[:2]
    out = np.zeros((h + 2 * PAD, w + 2 * PAD, 4))
    out[PAD:PAD + h, PAD:PAD + w] = art
    return Image.fromarray(np.round(out).astype(np.uint8), "RGBA")


def sized(tier):
    w, h = master_size()
    return [resize(p, w, h) for p in cut_pair(tier)]


def have():
    return [t for t in TIERS if os.path.exists(f"{SRC}/{t}-pair-chatgpt.png")]


def cut(out, tiers):
    os.makedirs(out, exist_ok=True)
    master = sized("acolyte")[0]
    for tier in tiers:
        base, enhanced = sized(tier)
        print(tier, "silhouette against acolyte: base %.3f enhanced %.3f" % (overlap(base, master), overlap(enhanced, master)))
        on_canvas(base).save(f"{out}/cut-{tier}-base.png")
        on_canvas(enhanced).save(f"{out}/cut-{tier}-enhanced.png")


def write_meta(path):
    """Like the Warrior's shields: Unity's default for a new file compresses it (fault log)."""
    if not os.path.exists(path + ".meta"):
        text = open(WARRIOR_META).read()
        open(path + ".meta", "w", newline="\n").write(text.replace(text.split("guid: ")[1].split()[0], uuid.uuid4().hex))


def write_icon(tier):
    im = Image.open(f"{OUT}/{tier}-base.png").convert("RGBA")
    im = im.crop(im.getbbox())
    k = 88 / max(im.width, im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    icon = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    icon.paste(im, ((96 - im.width) // 2, (96 - im.height) // 2), im)
    icon.save(f"{ICONS}/offhand-cleric-{TIERS.index(tier)}.png")


def install(tiers):
    os.makedirs(OUT, exist_ok=True)
    if not os.path.exists(OUT + ".meta"):
        folder = f"{RES}/Sprites/classes/warrior/weapons/shield.meta"
        text = open(folder).read()
        open(OUT + ".meta", "w", newline="\n").write(text.replace(text.split("guid: ")[1].split()[0], uuid.uuid4().hex))
    for tier in tiers:
        base, enhanced = sized(tier)
        for kind, art in (("base", base), ("enhanced", enhanced)):
            on_canvas(art).save(f"{OUT}/{tier}-{kind}.png")
            write_meta(f"{OUT}/{tier}-{kind}.png")
        write_icon(tier)
    # Text edit: the file is one line and other tools edit its staff grips. Lists the tiers in order, without gaps: a tier past the list clamps to the last one.
    w, h = master_size()
    w, h = w + 2 * PAD, h + 2 * PAD
    grip = {"w": w, "h": h, "gripX": w // 2, "gripY": h // 2}
    g = json.dumps(grip, separators=(",", ":"))
    raw = open(GEAR, "rb").read().decode("utf-8")
    glow = {e["name"]: e["glow"] for e in json.loads(raw)["weapon"]}
    done = []
    for t in TIERS:
        if not os.path.exists(f"{OUT}/{t}-base.png"):
            break
        done.append('{"name":"%s","glow":"%s","baseGrip":%s,"enhancedGrip":%s}' % (t, glow[t], g, g))
    block = '"shield":[' + ",".join(done) + "],"
    raw = re.sub(r'"shield":\[.*?\],(?="companion")', "", raw)
    raw = raw.replace('"companion":[', block + '"companion":[', 1)
    json.loads(raw)
    open(GEAR, "wb").write(raw.encode("utf-8"))
    print("installed", tiers, "shield list", len(done), "grip", grip)


def anchor_frames(anim, direction):
    out = []
    for f in range(COLS[anim]):
        fist = FREE_FIST.get((anim, direction), [None] * COLS[anim])[f]
        hidden = fist is None
        x, y = fist if fist else (96, 120)
        if anim == "idle":
            y += IDLE_LIFT[f]
        out.append({"x": x, "y": y, "angle": TILT[direction], "s": S, "sx": 1, "behind": BEHIND[direction], "hidden": hidden, "fallback": anim == "idle" and direction == "down"})
    return out


def write_anchors():
    raw = open(MELEE, "rb").read()
    data = json.loads(raw)
    data["anchors"] = [a for a in data["anchors"] if a["part"] != "shield"]
    for anim in COLS:
        for direction in ROWS:
            data["anchors"].append({"part": "shield", "anim": anim, "dir": direction, "frames": anchor_frames(anim, direction)})
    text = json.dumps(data, indent=1)
    if b"\r\n" in raw:
        text = text.replace("\n", "\r\n")
    open(MELEE, "wb").write(text.encode())
    print("anchors written")


def preview(out, tier_index=0, zoom=3, pad=30):
    spec = importlib.util.spec_from_file_location("compose_hero_frame", "Tools/compose-hero-frame.py")
    comp = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(comp)
    os.makedirs(out, exist_ok=True)
    size = FRAME + 2 * pad
    for anim, cols in COLS.items():
        sheet = Image.new("RGBA", (size * cols, size * 4), (58, 62, 70, 255))
        for direction, r in ROWS.items():
            for f in range(cols):
                sheet.alpha_composite(comp.compose("cleric", anim, direction, f, tier=tier_index, pad=pad), (f * size, r * size))
        sheet.resize((sheet.width * zoom // 2, sheet.height * zoom // 2), Image.LANCZOS).convert("RGB").save(f"{out}/hero-{TIERS[tier_index]}-{anim}.png")


def sheet(out):
    """All installed tiers, base over enhanced, on dark and light, beside the staff of the tier and the icon."""
    from PIL import ImageDraw
    os.makedirs(out, exist_ok=True)
    cell = 260
    im = Image.new("RGB", (cell * 5, cell * 8), (58, 62, 70))
    d = ImageDraw.Draw(im)
    for i, t in enumerate(TIERS):
        col, band = i % 5, i // 5
        if not os.path.exists(f"{OUT}/{t}-base.png"):
            continue
        for j, kind in enumerate(("base", "enhanced")):
            for light in (0, 1):
                y = (band * 4 + light * 2 + j) * cell
                if light:
                    d.rectangle((col * cell, y, col * cell + cell - 1, y + cell - 1), fill=(205, 205, 195))
                a = Image.open(f"{OUT}/{t}-{kind}.png").convert("RGBA")
                im.paste(a, (col * cell + 7, y + 11), a)
                d.text((col * cell + 4, y + 2), f"{i} {t} {kind}", fill=(0, 0, 0) if light else (255, 255, 255))
    im.save(f"{out}/sheet-all-tiers.png")
    icons = Image.new("RGB", (96 * 10, 96 * 2), (58, 62, 70))
    for i in range(10):
        a = Image.open(f"{ICONS}/offhand-cleric-{i}.png").convert("RGBA")
        icons.paste(a, (i * 96, 0), a)
        b = Image.open(f"{SRC}/old/icons/offhand-cleric-{i}.png").convert("RGBA")
        icons.paste(b, (i * 96, 96), b)
    icons.save(f"{out}/icons-new-top-old-bottom.png")


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "cut":
        cut(sys.argv[2], sys.argv[3:] or have())
    elif cmd == "install":
        install(sys.argv[2:] or have())
    elif cmd == "anchors":
        write_anchors()
    elif cmd == "sheet":
        sheet(sys.argv[2])
    elif cmd == "preview":
        preview(sys.argv[2], TIERS.index(sys.argv[3]) if len(sys.argv) > 3 else 0)

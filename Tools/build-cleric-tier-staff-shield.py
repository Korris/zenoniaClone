"""Cleric staff + heart shield of the 10 armour tiers, sized per tier as on the design sheet (2026-11-08,
docs/reference/cleric-sets-10-tiers-ladder-v3.png; exception to "one weapon size" as the Wizard/Mechanic, docs/sprite-rules.md).

ChatGPT draws 4-5 staffs (upright) or shields per image, left to right = the tiers of the group, on a flat key: GROUP_GREEN on #00FF00,
GROUP_MAGENTA (green / teal art) on #FF00FF. Sources: Assets/ArtLibrary/classes/cleric/outfits/gear-src/{staffs,shields}-{green,magenta}-chatgpt-N.png
(the file named in SRC). T0 keeps the current art (hero-select look), only resized to the sheet's T0 size.

Sizes, measured on the sheet in hero heights H (hair top to soles, sheet ~270 px; game idle-down frame 0: hair top row 50, soles 190,
H 140, head centre column 94, chin row 115). Game heads are bigger (0.49 H vs 0.30 H on the sheet), so lengths above the chin are scaled by
SIZE_K 1.15 (as the Mechanic) and heights below the chin are mapped chin->soles, so the shield stays under the chin as on the sheet.
  staff: drawn at S 0.52 everywhere (anchor = the bare right fist), its art resampled so it spans STAFF_FIT (top above the hair top, bottom
         near the feet); idle down leans STAFF_ANGLE. The enhanced file = the base art.
  shield: drawn at 0.27 x DrawScale 0.8 everywhere; art resampled to SHIELD_FIT width; the grip is moved off the box centre so on idle down
         the shield centre lands where the sheet has it (the hand stays hidden behind the shield).
Old art + manifest: Assets/ArtLibrary/classes/cleric/outfits/gear-src/old-pre-v3/.

  python Tools/build-cleric-tier-staff-shield.py ref                 gear refs sent to ChatGPT (design crops of each group, 2x)
  python Tools/build-cleric-tier-staff-shield.py check OUT_DIR       idle-down composites per tier (outfit body if written, head 0), game size strip
  python Tools/build-cleric-tier-staff-shield.py write               install staffs + shields + icons + manifest + idle-down anchors
"""
import importlib.util, json, os, re, shutil, sys, uuid
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
RES = "Assets/Resources"
CL = f"{RES}/Sprites/classes/cleric"
STAFF_LIVE, SHIELD_LIVE = f"{CL}/weapons/staff", f"{CL}/weapons/shield"
GEAR = f"{RES}/Data/classes/cleric/held-cleric.json"
MELEE = f"{RES}/Data/classes/cleric/held-cleric-melee.json"
ICONS = f"{RES}/Sprites/icons"
SRC = "Assets/ArtLibrary/classes/cleric/outfits/gear-src"
OLD = f"{SRC}/old-pre-v3"
DESIGN = "docs/reference/cleric-sets-10-tiers-ladder-v3.png"
FRAME = 192
GROUP_GREEN, GROUP_MAGENTA = [1, 2, 4, 7, 8], [3, 5, 6, 9]
SOURCES = {  # kind -> (green image, magenta image)
    "staffs": ("staffs-green-chatgpt-1.png", "staffs-magenta-chatgpt-1.png"),
    "shields": ("shields-green-chatgpt-1.png", "shields-magenta-chatgpt-1.png"),
}
GLOW = ["#fff159", "#ff9a3c", "#4fa8ff", "#3fe0d0", "#ff3b3b", "#ffb020", "#ff8a2a", "#b05cff", "#ff4a5a", "#4dff7a"]
HEAD_X = [128, 311, 501, 685, 847, 1060, 1268, 1490, 1695, 1975]  # sheet head centre per tier

HAIR_TOP, SOLE, H_GAME, HEAD_CX, CHIN = 50.0, 190.0, 140.0, 94.0, 115.0
SIZE_K, SHEET_CHIN = 1.15, 0.31  # sheet chin at 0.31 H below the hair top
STAFF_ANCHOR, STAFF_ANGLE, STAFF_S = (65.0, 118.0), -4.0, 0.52
STAFF_FIT = [(-0.23, 0.93), (-0.23, 0.95), (-0.28, 0.96), (-0.26, 0.91), (-0.31, 0.91),
             (-0.39, 0.92), (-0.45, 0.98), (-0.37, 0.95), (-0.46, 0.98), (-0.64, 0.93)]  # (top, bottom) in H below the hair top
STAFF_HEAD = [0.285, 0.366, 0.496, 0.383, 0.406, 0.558, 0.591, 0.532, 0.587, 0.573]  # top ornament width in H (above the hair top)
SHIELD_ANCHOR, SHIELD_ANGLE, SHIELD_S, SHIELD_DRAW = (128.0, 140.0), 5.0, 0.27, 0.8
SHIELD_FIT = [(0.34, 0.175, 0.55), (0.32, 0.19, 0.54), (0.32, 0.14, 0.53), (0.32, 0.15, 0.535), (0.325, 0.225, 0.52),
              (0.35, 0.16, 0.51), (0.37, 0.16, 0.51), (0.36, 0.15, 0.52), (0.37, 0.195, 0.50), (0.47, 0.285, 0.535)]  # (width, centre right of head centre, centre below hair top) in H


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


wtsb = load("wtsb", "Tools/build-wizard-tier-staff-book.py")  # key_out, split, icon, meta_like, hs (resize, to_image)
chf = wtsb.chf
hs = wtsb.hs
os.chdir(ROOT)


def below_chin(rel):
    """Sheet height rel (H below the hair top, below the chin) -> game row, mapped chin -> soles."""
    return CHIN + (rel - SHEET_CHIN) / (1 - SHEET_CHIN) * (SOLE - CHIN)


def ref():
    os.makedirs(SRC, exist_ok=True)
    src = Image.open(DESIGN).convert("RGB")
    for name, group in (("green", GROUP_GREEN), ("magenta", GROUP_MAGENTA)):
        cells = []
        for t in group:
            l, r = (HEAD_X[t] - 160, src.width - 4) if t == 9 else (HEAD_X[t] - 105, HEAD_X[t] + 105)
            cells.append(src.crop((l, 80, r, 560)))
        w = sum(c.width for c in cells) + 20 * (len(cells) - 1)
        im = Image.new("RGB", (w, 480), (255, 0, 255))
        x = 0
        for c in cells:
            im.paste(c, (x, 0)); x += c.width + 20
        im.resize((im.width * 2, im.height * 2), Image.NEAREST).save(f"{SRC}/ref-design-{name}.png")
    print("written", f"{SRC}/ref-design-green.png", f"{SRC}/ref-design-magenta.png")


def live_t0(kind):
    """T0 art (the current acolyte art, from the backup once written) as RGBA floats, trimmed."""
    folder = STAFF_LIVE if kind == "staffs" else SHIELD_LIVE
    p = f"{OLD}/{os.path.basename(folder)}/acolyte-base.png"
    p = p if os.path.exists(p) else f"{folder}/acolyte-base.png"
    a = np.asarray(Image.open(p).convert("RGBA")).astype(np.float32)
    ys, xs = np.where(a[..., 3] > 8)
    return a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def figures(kind):
    """10 RGBA float crops in tier order: T0 from the live art, the rest from the two ChatGPT images."""
    out = {0: live_t0(kind)}
    g, m = SOURCES[kind]
    for path, key, group in ((g, "green", GROUP_GREEN), (m, "magenta", GROUP_MAGENTA)):
        for t, f in zip(group, wtsb.split(wtsb.key_out(f"{SRC}/{path}", key), len(group))):
            out[t] = f
    for t, rows in MIRROR_TOP.get(kind, {}).items():
        out[t] = mirror_top(out[t], rows)
    return [out[t] for t in range(10)]


# ChatGPT copied a neighbour's silver crescent into the left half of the T5 sun ring (the sheet crop showed a bit of T4): the head
# ornament is symmetric on the sheet, so its right half is mirrored over the left (rows = share of the staff height from the top)
MIRROR_TOP = {"staffs": {5: 0.2, 7: 0.2}}


def mirror_top(f, rows):
    a = f.copy()
    h = a.shape[0]
    solid = a[..., 3] > 128
    mids = [(np.flatnonzero(solid[y]).min() + np.flatnonzero(solid[y]).max()) / 2 for y in range(h // 2, h * 9 // 10) if solid[y].any()]
    ax = int(round(np.median(mids)))
    n = int(h * rows)
    half = min(ax, a.shape[1] - 1 - ax)
    right = a[:n, ax:ax + half + 1]
    a[:n, :ax + 1] = 0
    a[:n, ax - half:ax + 1] = right[:, ::-1]
    return a


def pad_canvas(small, pad):
    c = np.zeros((small.shape[0] + 2 * pad, small.shape[1] + 2 * pad, 4), np.float32)
    c[pad:pad + small.shape[0], pad:pad + small.shape[1]] = small
    return c


def head_width(fig):
    """Width of the staff's top ornament (widest row in the top 30%)."""
    a = fig[: int(fig.shape[0] * 0.3), :, 3] > 128
    cols = np.where(a.any(0))[0]
    return cols.max() - cols.min() + 1


def cut_shaft(a, n):
    """Remove n rows from the plain stretches of the shaft (rows like their neighbour, 30-92% down), longest stretches first,
    so the head ornament and the bottom spike keep their size (ChatGPT drew the shafts ~2x longer than the sheet)."""
    h = a.shape[0]
    plain = []
    for y in range(int(h * 0.3), int(h * 0.92)):
        if np.mean(np.abs(a[y] - a[y - 1])) < 3.0:
            plain.append(y)
    runs, cur = [], []
    for y in plain:
        if cur and y != cur[-1] + 1:
            runs.append(cur); cur = []
        cur.append(y)
    if cur:
        runs.append(cur)
    drop = set()
    n = min(n, int(0.6 * sum(max(0, len(r) - 4) for r in runs)))  # keep 40% of every plain stretch, or the shaft colour vanishes (T5 shaft went all gold rings)
    for run in sorted(runs, key=len, reverse=True):
        inner = run[2:-2]
        inner = inner[: int(0.6 * len(inner))]
        take = inner[: max(0, n - len(drop))]
        drop.update(take)
        if len(drop) >= n:
            break
    keep = [y for y in range(h) if y not in drop]
    return a[keep], len(drop)


def staff_fit(fig, t):
    top_rel, bot_rel = STAFF_FIT[t]
    top = HAIR_TOP + top_rel * H_GAME * SIZE_K
    bot = below_chin(bot_rel)
    cos = np.cos(np.radians(STAFF_ANGLE))
    rows = (bot - top) / cos / STAFF_S
    k = max(rows / fig.shape[0], STAFF_HEAD[t] * H_GAME / STAFF_S / head_width(fig))  # head ornament as wide as on the sheet (in H, no SIZE_K)
    small = hs.resize(fig, (max(1, round(fig.shape[1] * k)), max(1, round(fig.shape[0] * k))))
    if small.shape[0] > rows:
        small, got = cut_shaft(small, int(round(small.shape[0] - rows)))
        if small.shape[0] > rows + 1:  # not enough plain shaft: the rest by scaling down (head a little narrower than the sheet)
            small = hs.resize(small, (max(1, round(small.shape[1] * rows / small.shape[0])), round(rows)))
    canvas = pad_canvas(small, 4)
    gy = int(round(4 + (STAFF_ANCHOR[1] - top) / cos / STAFF_S))
    solid = canvas[..., 3] > 128
    h = canvas.shape[0]
    mids = [(np.flatnonzero(solid[y]).min() + np.flatnonzero(solid[y]).max() + 1) / 2 for y in range(h * 2 // 3, h * 9 // 10) if solid[y].any()]
    gx = int(round(np.median(mids)))  # shaft axis from the lower shaft
    return canvas, {"w": canvas.shape[1], "h": h, "gripX": gx, "gripY": gy}


def shield_fit(fig, t):
    w_rel, cx_rel, cy_rel = SHIELD_FIT[t]
    k_art = SHIELD_S * SHIELD_DRAW
    wpx = w_rel * H_GAME * SIZE_K / k_art
    k = wpx / fig.shape[1]
    canvas = pad_canvas(hs.resize(fig, (round(wpx), max(1, round(fig.shape[0] * k)))), 3)
    h, w = canvas.shape[:2]
    cx, cy = HEAD_CX + cx_rel * H_GAME * SIZE_K, below_chin(cy_rel)
    # the anchor is rotated by SHIELD_ANGLE about the grip; for 5 degrees the offset error is under 1 px
    gx = w / 2 + (SHIELD_ANCHOR[0] - cx) / k_art
    gy = h / 2 + (SHIELD_ANCHOR[1] - cy) / k_art
    return canvas, {"w": w, "h": h, "gripX": int(round(gx)), "gripY": int(round(gy))}


def staffs():
    return [staff_fit(f, t) for t, f in enumerate(figures("staffs"))]


def shields():
    return [shield_fit(f, t) for t, f in enumerate(figures("shields"))]


def compose_tier(t, staff, shield, pad=40, hair=0):
    """Idle down frame 0 of tier t as HeroRenderer draws it: shield/staff in front of the outfit headless body + head, staff fist on top."""
    body = f"{CL}/body"
    sub = f"{body}/set-t{t}" if t and os.path.exists(f"{body}/set-t{t}/bare/idle.png") else body
    hl = f"{sub}/headless/idle.png" if os.path.exists(f"{sub}/headless/idle.png") else f"{body}/headless/idle.png"
    c = Image.new("RGBA", (FRAME + 2 * pad, FRAME + 2 * pad), (0, 0, 0, 0))
    c.alpha_composite(chf.cell(hl, 0, 0), (pad, pad))
    c.alpha_composite(Image.open(f"{CL}/heads/{hair}/down.png").convert("RGBA"), (pad, pad))
    a_sh = {"x": SHIELD_ANCHOR[0], "y": SHIELD_ANCHOR[1], "angle": SHIELD_ANGLE, "s": SHIELD_S, "sx": 1}
    chf.place(c, hs.to_image(shield[0]), shield[1], a_sh, SHIELD_DRAW, 0.0, pad)
    a_st = {"x": STAFF_ANCHOR[0], "y": STAFF_ANCHOR[1], "angle": STAFF_ANGLE, "s": STAFF_S, "sx": 1}
    chf.place(c, hs.to_image(staff[0]), staff[1], a_st, 1.0, 0.0, pad)
    c.alpha_composite(chf.cell(f"{sub}/hand/idle.png", 0, 0), (pad, pad))
    return c


def check(out):
    os.makedirs(out, exist_ok=True)
    st, sh = staffs(), shields()
    for t in range(10):
        print(t, "staff", st[t][1], "shield", sh[t][1])
    size = FRAME + 160
    sheet = Image.new("RGBA", (size * 10, size), (58, 62, 70, 255))
    for t in range(10):
        sheet.alpha_composite(compose_tier(t, st[t], sh[t], pad=80), (t * size, 0))
    sheet.save(f"{out}/cleric-tiers-idle-down.png")
    sheet.resize((sheet.width * 85 // 140, sheet.height * 85 // 140), Image.LANCZOS).save(f"{out}/cleric-tiers-idle-down-gamesize.png")
    print(out)


def backup(live, kind_dir):
    os.makedirs(f"{OLD}/{kind_dir}", exist_ok=True)
    for f in os.listdir(live):
        if f.endswith(".png") and not os.path.exists(f"{OLD}/{kind_dir}/{f}"):
            shutil.copy2(f"{live}/{f}", f"{OLD}/{kind_dir}/{f}")


def write():
    backup(STAFF_LIVE, "staff")
    backup(SHIELD_LIVE, "shield")
    os.makedirs(f"{OLD}/icons", exist_ok=True)
    if not os.path.exists(f"{OLD}/held-cleric.json"):
        shutil.copy2(GEAR, f"{OLD}/held-cleric.json")
        shutil.copy2(MELEE, f"{OLD}/held-cleric-melee.json")
    data = json.load(open(GEAR, encoding="utf-8"))
    for kind, items, live, icon_name, rot in (("weapon", staffs(), STAFF_LIVE, "weapon-cleric-{}.png", True),
                                              ("shield", shields(), SHIELD_LIVE, "offhand-cleric-{}.png", False)):
        for t, (rgba, grip) in enumerate(items):
            n = data[kind][t]["name"]
            for v in ("base", "enhanced"):
                hs.to_image(rgba).save(f"{live}/{n}-{v}.png")
            data[kind][t].update(glow=GLOW[t], baseGrip=grip, enhancedGrip=dict(grip))
            ic = f"{ICONS}/{icon_name.format(t)}"
            if os.path.exists(ic) and not os.path.exists(f"{OLD}/icons/{icon_name.format(t)}"):
                shutil.copy2(ic, f"{OLD}/icons/{icon_name.format(t)}")
            wtsb.icon(rgba, ic, rot)
    open(GEAR, "w", encoding="utf-8").write(json.dumps(data, separators=(",", ":")))
    m = json.load(open(MELEE, encoding="utf-8"))
    for e in m["anchors"]:
        if e["anim"] == "idle" and e["dir"] == "down":
            for fr in e["frames"]:
                if e["part"] == "staff":
                    fr.update(x=STAFF_ANCHOR[0], y=STAFF_ANCHOR[1], angle=STAFF_ANGLE, s=STAFF_S)
                elif e["part"] == "shield":
                    fr.update(x=SHIELD_ANCHOR[0], y=SHIELD_ANCHOR[1], angle=SHIELD_ANGLE, s=SHIELD_S)
    open(MELEE, "w", encoding="utf-8").write(json.dumps(m, indent=1))
    print("staffs + shields written")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "ref":
        ref()
    elif cmd == "check":
        check(sys.argv[2])
    elif cmd == "write":
        write()
    else:
        print(__doc__)

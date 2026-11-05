"""Mechanic idle (4 directions) as a paper doll from the user-approved ChatGPT drawings in new-style/dual-src/cannon-back-lr/ (2026-11-05):
body/bare/idle.png rows 0-3 come from the bare drawings (empty fists, no cannon); the cannon and both pistols are drawn by the game through the
idle anchors of held-mechanic-dual.json (cannon / gun = right hand / gunL = left hand), measured on the armed drawings of the same views.
Each figure is scaled so head top to sole equals the current frame 0 of its row, stands on the same sole row and boot centre, and breathes
like the current rows (GROW). body/hand/idle.png rows 0-3 = fist discs cut from the new bare frames; body/armed/idle.png rows 0-3 = the game
composite with tier 0 (compose-mechanic-cannon-check.compose). Other rows, anims and anchor sets are left alone.

  python Tools/build-mechanic-idle-doll-from-chatgpt.py grid   OUT/grid-<dir>.png: fitted frame 0, bare and armed side by side, 10 px grid,
                                                             detected lens / cap / fist candidates marked (frame px printed)
  python Tools/build-mechanic-idle-doll-from-chatgpt.py check  OUT/check.png: per direction the 4 composed frames, the armed drawing at the
                                                             same figure height and the current frame 0; prints size ratios and anchors
  python Tools/build-mechanic-idle-doll-from-chatgpt.py write  backs up the 3 idle sheets + json once into cannon-back-lr/old/, then writes;
                                                             refuses when a head is over 6% off the current row unless HEAD_OK=1 is set
OUT defaults to C:/temp/cap-cannon/doll. Run from the project root; Pillow, numpy, scipy."""
import importlib.util
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

spec = importlib.util.spec_from_file_location("h", "Tools/mechanic-idle-doll-helpers.py")
h = importlib.util.module_from_spec(spec)
spec.loader.exec_module(h)

# view -> (bare sheet, panel index, armed sheet, panel index)
SOURCES = {
    "right": ("mechanic-bare-lr-chatgpt-1.png", 0, "mechanic-cannon-back-lr-chatgpt-1.png", 0),
    "left": ("mechanic-bare-lr-chatgpt-1.png", 1, "mechanic-cannon-back-lr-chatgpt-1.png", 1),
    "down": ("mechanic-bare-down-gripfist-chatgpt-1.png", 0, "ref-armed-down-single.png", 0),  # elbows-out inpaint; its fists are replaced by the armed drawing's gripping fists (GRAFT)
    "up": ("mechanic-bare-down-up-chatgpt-1.png", 1, "mechanic-cannon-back-up-strap-chatgpt-1.png", 0),
}
CANNON_ART = {"down": "front", "up": "back", "left": "side", "right": "side"}
CANNON_BASE = {"front": -44.0, "back": -135.0, "side": 0.0}  # build-mechanic-dual.py: muzzle direction of each art
CANNON_S, PISTOL_S, PISTOL_VIEW_S = 0.5, 0.26, 0.195  # the user keeps the pistols at 0.26 (2026-11-05), even though both overlap a little in the side idle
CANNON_BEHIND = {"down": True, "up": False, "left": True, "right": True}
# cannon ends in fitted-frame px of the ARMED figure, read off `grid` / the 6x zooms (lens = muzzle centre, cap = tail cap centre; the
# detected lens blobs matched within 2 px). Side views hide the tail behind the body: cap None = keep the side length of the other anims
# (CANNON_LEN x SIDE_SX of build-mechanic-dual.py) along the drawn aim; left mirrors the muzzle placement of the right (the drawing has it
# a little lower and nearer the head, which would run the tail out in front of the shins at the game's cannon length).
CANNON_SRC = {
    "down": dict(lens=(151, 56), cap=(40, 151)),
    "up": dict(lens=(50, 60), cap=(132, 143)),
    "right": dict(lens=(26, 35), cap=None, aim=-135.0),  # drawn muzzle (38, 41): moved 12 px up-left so the tail cap stays behind the hip
    "left": dict(lens=(165, 35), cap=None, aim=-45.0),  # drawn muzzle (134, 60): mirrored from the right about the boot centre
}
SIDE_SX = 0.9  # build-mechanic-dual.SIDE_SX: the side cannon of walk, attack, hurt is this much shorter; idle keeps the same size
# fists in fitted-frame px of the BARE frame 0 (right hand = gun, left hand = gunL), read off the 8x zooms (about 2 px)
FISTS = {
    "right": {"gun": (136, 133), "gunL": (88, 136)},  # facing right: the forward fist is the near (right) hand
    "left": {"gunL": (56, 134), "gun": (103, 135)},  # facing left: the forward fist is the near (left) hand
    "down": {"gun": (66, 137), "gunL": (128, 137)},  # arms-out redraw
    "up": {"gunL": (59, 134), "gun": (130, 135)},  # seen from behind the left hand is screen-left
}
# pistols: (angle, sx); the barrel points along R(angle) * (sx, 0) on the y-down frame. Side views: both pistols forward and 35 deg down
# (the user wants the two pistols pointing the same way, 2026-11-05; the far one is not mirrored backward). Down / up: the drawing shows
# side-view pistols hanging at the hips, barrels down and outward, so the side art is used there too (no front/back view art).
PISTOL_POSE = {
    "right": {"gun": (35.0, 1), "gunL": (35.0, 1)},
    "left": {"gunL": (-35.0, -1), "gun": (-35.0, -1)},
    "down": {"gun": (-72.0, -1), "gunL": (72.0, 1)},  # barrels down and 15-20 deg outward (ChatGPT analysis 2026-11-05)  # gun is screen-left: barrel down-left; gunL screen-right: down-right
    "up": {"gunL": (-72.0, -1), "gun": (72.0, 1)},  # from behind gunL is screen-left
}
# frame px added to the pistol anchor only (the hand disc stays on the fist): buries the grip in the fist instead of hanging the
# pistol off the fist's edge. From ChatGPT's analysis of the in-game shots (2026-11-05): front view up 7-10 px and 3-5 px inward,
# side view the near pistol 3-7 px back and 3 px up.
PISTOL_SHIFT = {
    "right": {"gun": (-5, -3), "gunL": (0, 0)},
    "left": {"gunL": (5, -3), "gun": (0, 0)},
    "down": {"gun": (4, -9), "gunL": (-4, -9)},
    "up": {"gunL": (4, -9), "gun": (-4, -9)},
}
GRAFT = ("down",)  # views whose fists come from the armed drawing (gripping fists, fingers visible)
PISTOL_ART = {}  # view art (pistol-front / pistol-back) no longer used on idle: every direction draws the side art
# The far pistol of the side views is drawn in front too: its fist lies over the cape (part of the bare sheet), and behind the body only its
# muzzle tip would poke out under the cape; the drawing shows the whole pistol over the cape. Set the far one True to hide it behind.
PISTOL_BEHIND = {"right": {"gun": False, "gunL": False}, "left": {"gun": False, "gunL": False}, "down": {"gun": False, "gunL": False}, "up": {"gun": False, "gunL": False}}


def fits(direction):
    """(bare Fit, armed Fit) of one view, each by its own head/sole/boots against the current row."""
    bare, bi, armed, ai = SOURCES[direction]
    target = h.current_metrics(direction)
    out = []
    for sheet, i in ((bare, bi), (armed, ai)):
        pm, x0 = h.panels(h.key_green(f"{h.SRC}/{sheet}"))[i]
        out.append(h.Fit(pm, target))
    return out


def candidates(fit):
    """Lens and cap blob centroids of a figure in frame px (biggest first)."""
    src = np.asarray(h.to_image(fit.pm))
    out = {}
    for name, mask, min_px in (("lens", h.lens_mask(src), 600), ("cap", h.cap_mask(src), 500)):
        out[name] = [(fit.to_frame(*c), n) for c, n in h.blobs(mask, min_px)[:6]]
    return out


def grid(out):
    """Fitted frame 0 of each view, bare (left) and armed (right), each on its own 10 px grid, with detected landmark candidates marked."""
    z = 5
    for d in h.DIRS:
        bare, armed = fits(d)
        im = Image.new("RGBA", (2 * h.FRAME * z, h.FRAME * z), (58, 62, 70, 255))
        for col, fit in enumerate((bare, armed)):
            im.alpha_composite(fit.cell().resize((h.FRAME * z,) * 2, Image.NEAREST), (col * h.FRAME * z, 0))
        dr = ImageDraw.Draw(im)
        print(d, "bare src", bare.m, "k %.4f" % bare.k, "| armed src", armed.m, "k %.4f" % armed.k, "| target", bare.t)
        for col, fit in enumerate((bare, armed)):
            h.draw_grid(dr, col * h.FRAME * z, z)
            fists = [(c, n) for c, n in h.blobs(h.fist_mask(fit.cell()), 60) if c[1] > 100][:6]
            h.mark(dr, col * h.FRAME * z, z, fists, (255, 255, 0, 255), "fist")
            print(f"  {d} {'bare' if col == 0 else 'armed'} fist candidates:", [(round(c[0]), round(c[1]), n) for c, n in fists])
        c = candidates(armed)
        for name, colour in (("lens", (0, 255, 255, 255)), ("cap", (255, 80, 255, 255))):
            h.mark(dr, h.FRAME * z, z, c[name], colour, name)
            print(f"  {d} armed {name}:", [(round(x, 1), round(y, 1), n) for (x, y), n in c[name]])
        im.save(f"{out}/grid-{d}.png")
        print("grid", f"{out}/grid-{d}.png")


def art_axis(art):
    """Lens centre to cap centre of a cannon art, in art px (same masks as the drawings; side/back: the muzzle lens is the 2nd blue blob)."""
    a = np.asarray(Image.open(f"{h.SPR}/weapons/cannon/{art}.png").convert("RGBA"))
    lens, cap = h.blobs(h.lens_mask(a), 100), h.blobs(h.cap_mask(a), 50)
    return math.dist(lens[0 if art == "front" else 1][0], cap[0][0])


def cannon_anchor(d):
    """Frame-0 cannon anchor of one view from CANNON_SRC (centre = midpoint, angle = aim - art base, sx keeps the drawn length)."""
    art, c = CANNON_ART[d], CANNON_SRC[d]
    lens, cap, axis = c["lens"], c["cap"], art_axis(art) * CANNON_S
    if cap is None:
        th = math.radians(c["aim"])
        cap = (lens[0] - SIDE_SX * axis * math.cos(th), lens[1] - SIDE_SX * axis * math.sin(th))
    aim = h.direction_of(cap, lens)
    sx = math.dist(lens, cap) / axis
    angle = ((aim - CANNON_BASE[art] + 180) % 360) - 180
    return dict(x=(lens[0] + cap[0]) / 2, y=(lens[1] + cap[1]) / 2, angle=round(angle, 2), s=CANNON_S, sx=round(sx, 3), behind=CANNON_BEHIND[d], hidden=False, fallback=False, art=art)


def pistol_anchor(d, part):
    x, y = FISTS[d][part]
    angle, sx = PISTOL_POSE[d][part]
    sx_, sy_ = PISTOL_SHIFT[d][part]
    x, y = x + sx_, y + sy_
    a = dict(x=float(x), y=float(y), angle=angle, s=PISTOL_VIEW_S if d in PISTOL_ART else PISTOL_S, sx=sx, behind=PISTOL_BEHIND[d][part], hidden=False, fallback=False)
    if d in PISTOL_ART:
        a["art"] = PISTOL_ART[d]
    return a


def anchors_of(fit, d):
    """(part, 'idle', d) -> 4 frames, the y of every frame breathed like the figure."""
    out = {}
    for part, a in (("cannon", cannon_anchor(d)), ("gun", pistol_anchor(d, "gun")), ("gunL", pistol_anchor(d, "gunL"))):
        frames = []
        for g in h.GROW:
            f = dict(a)
            f["x"], f["y"] = round(a["x"], 2), round(fit.breathe_y(a["y"], g), 3)
            frames.append(f)
        out[(part, "idle", d)] = frames
    return out


def build():
    """Per view: the 4 (bare, hand) cells, the new anchors and the armed drawing fitted into a cell."""
    cells, anchors, armed_cells = {}, {}, {}
    for d in h.DIRS:
        bare, armed = fits(d)
        fists = list(FISTS[d].values())
        cells[d] = []
        for g in h.GROW:
            c = h.match_leather(bare.cell(g), d)
            if d in GRAFT:  # the armed drawing's gripping fists over the bare body (see helpers.graft_fists)
                c, _ = h.graft_fists(c, h.match_leather(armed.cell(g), d), fists)
            cells[d].append((c, h.fist_disc(c, fists)))
        armed_cells[d] = armed.cell()
        anchors.update(anchors_of(bare, d))
    return cells, anchors, armed_cells


def size_report(cells):
    """Hair width (widest row) and body width at a few rows of the new frame 0 against the current frame 0 of the same row."""
    cur = Image.open(f"{h.BODY}/bare/idle.png").convert("RGBA")
    warn = []
    for d in h.DIRS:
        r = h.ROWS[d]
        old = cur.crop((0, r * h.FRAME, h.FRAME, (r + 1) * h.FRAME))
        new = cells[d][0][0]
        hw_new, hw_old = h.hair_width(new), h.hair_width(old)
        ratio = hw_new[0] / max(1, hw_old[0])
        widths = " ".join(f"y{y}:{h.width_at(new, y)}/{h.width_at(old, y)}" for y in (115, 135, 155))
        print(f"size {d}: hair width new {hw_new[0]} (row {hw_new[1]}) / current {hw_old[0]} (row {hw_old[1]}) = {ratio:.3f}; body width new/current {widths}")
        if abs(ratio - 1) > 0.06:
            warn.append(f"{d} hair ratio {ratio:.3f}")
    return warn


def write(cells, anchors, cm, file):
    old = f"{h.SRC}/old"
    os.makedirs(old, exist_ok=True)
    for name in ("bare", "hand", "armed"):
        if not os.path.exists(f"{old}/{name}-idle.png"):
            Image.open(f"{h.BODY}/{name}/idle.png").save(f"{old}/{name}-idle.png")
    if not os.path.exists(f"{old}/held-mechanic-dual.json"):
        open(f"{old}/held-mechanic-dual.json", "w", encoding="utf-8").write(open(h.GEAR, encoding="utf-8").read())
    sheets = {name: Image.open(f"{h.BODY}/{name}/idle.png").convert("RGBA") for name in ("bare", "hand", "armed")}
    for d in h.DIRS:
        r = h.ROWS[d]
        for f in range(4):
            box = (f * h.FRAME, r * h.FRAME)
            sheets["bare"].paste(cells[d][f][0], box)
            sheets["hand"].paste(cells[d][f][1], box)
            sheets["armed"].paste(h.compose_with(cm, cells, file, anchors, d, f, 0, 0), box)
    for name, im in sheets.items():
        im.save(f"{h.BODY}/{name}/idle.png")
        print("wrote", f"{h.BODY}/{name}/idle.png")
    seen = set()
    for entry in file["anchors"]:
        key = (entry["part"], entry["anim"], entry["dir"])
        if key in anchors:
            entry["frames"] = anchors[key]
            seen.add(key)
    for key in anchors:
        if key not in seen:
            file["anchors"].append({"part": key[0], "anim": key[1], "dir": key[2], "frames": anchors[key]})
    open(h.GEAR, "w", encoding="utf-8").write(json.dumps(file, separators=(",", ":")))
    print("wrote", h.GEAR)


def run(cmd, out):
    cm = h.load_composer()
    file = json.load(open(h.GEAR, encoding="utf-8"))
    cells, anchors, armed_cells = build()
    warn = size_report(cells)
    for key, frames in sorted(anchors.items(), key=lambda t: (h.DIRS.index(t[0][2]), t[0][0])):
        a = frames[0]
        print(f"anchor {key[0]:6} idle {key[2]:5}: x {a['x']:.2f} y {a['y']:.2f} angle {a['angle']} s {a['s']} sx {a['sx']} behind {a['behind']} art {a.get('art', '-')} | frame y {[round(f['y'], 2) for f in frames]}")
    h.contact_sheet(out, cells, anchors, armed_cells, cm, file)
    if cmd == "write":
        if warn and not os.environ.get("HEAD_OK"):
            raise SystemExit("not written, head size off (HEAD_OK=1 to accept): " + ", ".join(warn))
        write(cells, anchors, cm, file)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    out = os.environ.get("OUT", "C:/temp/cap-cannon/doll")
    os.makedirs(out, exist_ok=True)
    if cmd == "grid":
        grid(out)
    elif cmd in ("check", "write"):
        run(cmd, out)
    else:
        raise SystemExit(__doc__)

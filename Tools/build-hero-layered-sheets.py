"""Layered hero art (2026-11-19, spec docs/character-appearance-layered-spec.md): cut the ChatGPT base-body sheets,
mirror the 3 left directions, detect the head anchor of every frame, and write reference images for the next prompts.

Sources: Assets/ArtLibrary/hero-layered/src/
  body-<gender>-idle-5dir-chatgpt-N.png             1 row: down, down-right, right, up-right, up
  body-<gender>-<anim>-<a>--<b>-chatgpt-N.png       2 rows x 4 frames (walk, attack1-3); row 1 = direction a, row 2 = b
                                                    (b may be "none" when the image has one row)
Outputs (colour key NOT yet turned into a mask: Tools/build-hero-recolor-mask.py does that):
  Assets/ArtLibrary/hero-layered/sheets/<gender>/<anim>.png   rows in DIRS order, CELL_W x CELL_H, feet centre at (CELL_W/2, FEET_Y)
  Assets/ArtLibrary/hero-layered/sheets/<gender>/head-anchors.json   {anim: {dir: [[x, y, w], ...]}} skull top centre + skull width, cell px
  Assets/ArtLibrary/hero-layered/ref/bald-head-<gender>-5dir.png       5 bald heads on #00FF00 for the hair / headgear prompts
  Assets/ArtLibrary/hero-layered/ref/body-<gender>-idle-<a>-<b>.png    idle pairs for the walk / attack prompts
Commands: write <gender|all> | check <gender>  (check -> docs/reference/hero-layered-sheets/<gender>-check.png)
"""
import json
import re
import sys
from importlib import import_module
from pathlib import Path

import numpy as np
from scipy import ndimage
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
pilot = import_module("build-ro-soft-8dir-pilot")  # key_green, figures_with_pos

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/ArtLibrary/hero-layered"
DIRS = ["down", "down-right", "right", "up-right", "up", "up-left", "left", "down-left"]
DRAWN = DIRS[:5]
MIRROR = {"up-left": "up-right", "left": "right", "down-left": "down-right"}
CELL_W, CELL_H, FEET_Y = 320, 256, 244
BALD_PX = 188  # bald idle figure height (skull top to feet); hair adds ~12 px -> ~200 px like the RO Soft sheets
GENDERS = ["male", "female"]
SRC_RE = re.compile(r"^body-(?P<g>male|female)-(?P<anim>walk|attack[123])-(?P<a>[a-z-]+)--(?P<b>[a-z-]+)-chatgpt-(?P<n>\d+)\.png$")


def latest(pattern):
    files = sorted(ART.glob(f"src/{pattern}"), key=lambda p: int(re.search(r"chatgpt-(\d+)", p.name).group(1)))
    return files[-1] if files else None


def feet_x(fig):
    al = np.asarray(fig)[..., 3] > 128
    xs = np.nonzero(al[int(al.shape[0] * 0.9):])[1]
    return xs.mean() if len(xs) else fig.width / 2


def place(fig, scale):
    f = fig.resize((max(1, round(fig.width * scale)), max(1, round(fig.height * scale))), Image.LANCZOS)
    cell = Image.new("RGBA", (CELL_W, CELL_H))
    cell.paste(f, (round(CELL_W / 2 - feet_x(f)), FEET_Y - f.height), f)
    return cell


def skull_mask(cell):
    """Bald skull + face: the largest skin-coloured blob in the upper 60 % of the figure. Gloves are brown leather, so a
    fist raised above the head is not skin and cannot be taken for the skull top (attack3 frame 1)."""
    a = np.asarray(cell).astype(int)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3] > 128
    skin = al & (r > 200) & (g > 150) & (b > 110) & (r - b > 25) & (r - b < 110)
    ys = np.nonzero(al.any(1))[0]
    skin[ys[0] + int((ys[-1] - ys[0]) * 0.6):] = False
    lab, n = ndimage.label(ndimage.binary_closing(skin, iterations=2))
    if n == 0:
        return al
    sizes = ndimage.sum(skin, lab, range(1, n + 1))
    return lab == (int(np.argmax(sizes)) + 1)


def head_anchor(cell):
    """Skull top centre and skull width of a bald figure in a cell: widest run of the skull blob in the band 6-30 %
    of the skull height below its top (outline pixels around the skin are added back by a 2 px grow)."""
    al = ndimage.binary_dilation(skull_mask(cell), iterations=2)
    ys = np.nonzero(al.any(1))[0]
    top, h = ys[0], (ys[-1] - ys[0]) * 2.2  # the skull blob is about 45 % of a figure's height
    best, bx = 0, CELL_W / 2
    for y in range(top + int(h * 0.06), top + int(h * 0.3)):
        xs = np.nonzero(al[y])[0]
        if len(xs):
            for run in np.split(xs, np.nonzero(np.diff(xs) > 2)[0] + 1):
                if len(run) > best:
                    best, bx = len(run), (run[0] + run[-1]) / 2
    return [round(float(bx), 1), int(top), best]


def rows_of(path, per_row, n_rows):
    figs = pilot.figures_with_pos(pilot.key_green(Image.open(path)), per_row * n_rows)
    if n_rows == 1:
        return [[f for f, _, _ in figs]]
    mids = [y + f.height / 2 for f, _, y in figs]
    cut = np.median(mids)
    return [[f for (f, _, _), m in zip(figs, mids) if m < cut], [f for (f, _, _), m in zip(figs, mids) if m >= cut]]


def build(g):
    frames = {}
    idle_src = latest(f"body-{g}-idle-5dir-chatgpt-*.png")
    if idle_src is None:
        raise SystemExit(f"no idle source for {g}")
    idle = rows_of(idle_src, 5, 1)[0]
    scale_idle = {d: BALD_PX / f.height for d, f in zip(DRAWN, idle)}
    frames["idle"] = {d: [place(f, scale_idle[d])] for d, f in zip(DRAWN, idle)}
    for src in sorted(ART.glob(f"src/body-{g}-*-chatgpt-*.png")):
        m = SRC_RE.match(src.name)
        if not m:
            continue
        dirs = [d for d in (m["a"], m["b"]) if d != "none"]
        rows = rows_of(src, 4, len(dirs))
        for d, row in zip(dirs, rows):
            # walk keeps the idle height; attacks keep the idle head size: scale by the row's median height vs idle,
            # with the attack/idle ratio taken from frame heights relative to the walk-free idle of that direction
            s = BALD_PX / float(np.median([f.height for f in row])) if m["anim"] == "walk" else None
            if s is None:
                s = head_scale(row, idle[DRAWN.index(d)], scale_idle[d])
            frames.setdefault(m["anim"], {})[d] = [place(f, s) for f in row]
    for anim, by_dir in frames.items():
        for left, right in MIRROR.items():
            if right in by_dir:
                by_dir[left] = [c.transpose(Image.FLIP_LEFT_RIGHT) for c in by_dir[right]]
    return frames


def head_scale(row, idle_fig, idle_scale):
    """Scale so the skull width of the row (median) equals the idle skull width: attacks crouch and stretch, heads don't."""
    def skull_w(fig):
        cell = Image.new("RGBA", (fig.width + 4, fig.height + 4))
        cell.paste(fig, (2, 2), fig)
        al = np.asarray(cell)[..., 3] > 128
        ys = np.nonzero(al.any(1))[0]
        top, h = ys[0], ys[-1] - ys[0]
        widths = [np.count_nonzero(al[y]) for y in range(top + int(h * 0.06), top + int(h * 0.22))]
        return float(np.median(widths)) if widths else fig.width
    return idle_scale * skull_w(idle_fig) / float(np.median([skull_w(f) for f in row]))


def write(g):
    frames = build(g)
    out = ART / "sheets" / g
    out.mkdir(parents=True, exist_ok=True)
    anchors = {}
    idle_w = {d: head_anchor(frames["idle"][d][0])[2] for d in DIRS if d in frames["idle"]}
    for anim, by_dir in frames.items():
        missing = [d for d in DIRS if d not in by_dir]
        if missing:
            print(f"{g} {anim}: missing {missing}, skipped")
            continue
        n = max(len(v) for v in by_dir.values())
        sheet = Image.new("RGBA", (CELL_W * n, CELL_H * len(DIRS)))
        anchors[anim] = {}
        for r, d in enumerate(DIRS):
            anchors[anim][d] = []
            for k, cell in enumerate(by_dir[d]):
                sheet.alpha_composite(cell, (k * CELL_W, r * CELL_H))
                x, top, w = head_anchor(cell)
                anchors[anim][d].append([x, top, min(w, round(idle_w.get(d, w) * 1.1))])  # raised bare arms widen the skin blob
        sheet.save(out / f"{anim}.png")
        print(f"{g} {anim}: {n} frames x 8 directions")
    (out / "head-anchors.json").write_text(json.dumps(anchors), encoding="utf-8")
    refs(g, frames)


def refs(g, frames):
    """Bald-head strip (5 drawn directions, 3x) and idle pairs for the next ChatGPT prompts."""
    ref = ART / "ref"
    ref.mkdir(parents=True, exist_ok=True)
    heads = []
    for d in DRAWN:
        cell = frames["idle"][d][0]
        x, top, w = head_anchor(cell)
        heads.append(cell.crop((round(x - w * 0.8), top - 10, round(x + w * 0.8), top + round(w * 1.15))))
    hw, hh = max(h.width for h in heads), max(h.height for h in heads)
    strip = Image.new("RGB", ((hw + 40) * 5 + 40, hh + 80), (0, 255, 0))
    for i, h in enumerate(heads):
        strip.paste(h, (40 + i * (hw + 40) + (hw - h.width) // 2, 40), h)
    strip.resize((strip.width * 3, strip.height * 3), Image.LANCZOS).save(ref / f"bald-head-{g}-5dir.png")
    for a, b in (("down", "down-right"), ("right", "up-right"), ("up", "up")):
        pair = [frames["idle"][a][0]] + ([frames["idle"][b][0]] if b != a else [])
        img = Image.new("RGB", (CELL_W * len(pair), CELL_H), (0, 255, 0))
        for i, c in enumerate(pair):
            img.paste(c, (i * CELL_W, 0), c)
        img.resize((img.width * 2, img.height * 2), Image.LANCZOS).save(ref / f"body-{g}-idle-{a}-{b}.png")


def check(g):
    frames = build(g)
    rows = [(a, d) for a in frames for d in DIRS if d in frames[a]]
    n = max(len(frames[a][d]) for a, d in rows)
    out = Image.new("RGBA", (CELL_W * n // 2, CELL_H * len(rows) // 2), (205, 196, 176, 255))
    dr = ImageDraw.Draw(out)
    for r, (a, d) in enumerate(rows):
        y0 = r * CELL_H // 2
        dr.text((4, y0 + 4), f"{a} {d}", fill=(40, 40, 40, 255))
        for k, cell in enumerate(frames[a][d]):
            out.alpha_composite(cell.resize((CELL_W // 2, CELL_H // 2), Image.LANCZOS), (k * CELL_W // 2, y0))
            x, top, w = head_anchor(cell)
            ox = k * CELL_W // 2
            dr.line((ox + (x - w / 2) / 2, y0 + top / 2, ox + (x + w / 2) / 2, y0 + top / 2), fill=(255, 0, 0, 255))
    dst = ROOT / f"docs/reference/hero-layered-sheets/{g}-check.png"
    dst.parent.mkdir(parents=True, exist_ok=True)
    out.convert("RGB").save(dst)
    print("wrote", dst)


if __name__ == "__main__":
    cmd, which = sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "all"
    for g in GENDERS if which == "all" else [which]:
        {"write": write, "check": check}[cmd](g)

"""Assassin 10 tiers, idle facing down: design sheet (top row) over the game (bottom row), same hero height (hair top incl. ponytail
to sole), to check outfit and dagger size tier by tier (the designer, 2026-11-08). Like Tools/build-wizard-tier-design-vs-game-sheet.py.
Bottom row: composed offline from the installed/cut art (Tools/build-assassin-tier-daggers.py compose), or from in-game shots.

  python Tools/build-assassin-tier-design-vs-game-sheet.py OUT.png [--captures DIR] [--overlay]

--captures DIR: DIR/t<N>_1.png from `Zenonia.exe --class assassin --tier N --hairstyle 0 --tp 16,17 --face down --zoom 30 --shots 2.5
--shot-path DIR/tN -screen-width 960 -screen-height 640 -screen-fullscreen 0`; the hero is found in the shot by CAP_FACE / CAP_TOP / CAP_H.
--overlay: a third row, design and game blended 50% (size check).
"""
import importlib.util, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


out_tool = load("aoid", "Tools/build-assassin-outfit-idle-down.py")
dag = load("atd", "Tools/build-assassin-tier-daggers.py")
UP, DOWN, SIDE, H_OUT = 0.12, 0.06, 0.62, 300
SIDE9 = 0.7


def crop_scaled(im, cx, top, H, side, resample):
    box = (cx - side * H, top - UP * H, cx + side * H, top + H + DOWN * H)
    k = H_OUT / H
    return im.crop(tuple(int(round(v)) for v in box)).resize((round((box[2] - box[0]) * k), round((box[3] - box[1]) * k)), resample)


def design_cell(src, t):
    """The tier's figure only: columns outside its share of the sheet (+ 25 px for daggers reaching over) painted green."""
    a = np.asarray(src).copy()
    xs = np.arange(a.shape[1])
    lo, hi = out_tool.D_SPLIT[t] - (25 if t else 0), out_tool.D_SPLIT[t + 1] + 25
    a[:, (xs < lo) | (xs >= hi)] = (0, 255, 0)
    H = out_tool.D_SOLE[t] - out_tool.D_TOP[t]
    return crop_scaled(Image.fromarray(a), out_tool.D_FACE[t], out_tool.D_TOP[t], H, SIDE9 if t == 9 else SIDE, Image.LANCZOS).convert("RGBA")


def game_cell(t, arts):
    pad = 150
    art, grip = arts[t]
    c = dag.compose(t, art, grip, pad=pad)
    lv1 = out_tool.lv1_frame()
    top, bot = out_tool.hair_top(lv1), out_tool.sole(lv1)
    fx = out_tool.face_x(lv1, top, bot)
    return crop_scaled(c, pad + fx, pad + top, bot - top + 1, SIDE9 if t == 9 else SIDE, Image.NEAREST)


def find_hero(path):
    """(face x, hair top, height) of the hero in a zoomed shot: the purple hair top, the lowest body row under the face, face skin centre."""
    im = Image.open(path).convert("RGBA")
    a = np.asarray(im).astype(int)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    w, h = im.size
    purple = (b > 50) & (r > 35) & (g < r - 10) & (g < b - 5) & (np.abs(r - b) < 80)
    sub = purple[:, w // 4: 3 * w // 4]
    rows = np.where(sub.sum(1) >= 3)[0]
    top = int(rows.min())
    return im, top


def main():
    out = sys.argv[1]
    caps = sys.argv[sys.argv.index("--captures") + 1] if "--captures" in sys.argv else None
    overlay = "--overlay" in sys.argv
    src = Image.open(out_tool.DESIGN).convert("RGB")
    arts = None if caps else dag.daggers()
    cells = []
    for t in range(10):
        d = design_cell(src, t)
        if caps:
            g = capture_cell(f"{caps}/t{t}_1.png", t)
        else:
            g = game_cell(t, arts)
        cells.append((d, g))
    rows = 3 if overlay else 2
    widths = [max(d.width, g.width) for d, g in cells]
    h = cells[0][0].height
    sheet = Image.new("RGB", (sum(widths), h * rows + 24), (58, 62, 70))
    dr = ImageDraw.Draw(sheet)
    x = 0
    for t, ((d, g), w) in enumerate(zip(cells, widths)):
        sheet.paste(d.convert("RGB"), (x, 0))
        bg = Image.new("RGBA", g.size, (58, 62, 70, 255))
        bg.alpha_composite(g)
        sheet.paste(bg.convert("RGB"), (x, h + 24))
        if overlay:
            dd = d.resize(bg.size) if d.size != bg.size else d
            sheet.paste(Image.blend(dd.convert("RGB"), bg.convert("RGB"), 0.5), (x, 2 * h + 24))
        dr.text((x + 6, h + 5), f"T{t} design (top) / {'in game' if caps else 'game, offline'} (bottom)", fill=(255, 255, 160))
        for y in (round(UP * H_OUT), round(UP * H_OUT) + H_OUT):
            dr.line([(x, y), (x + 12, y)], fill=(255, 60, 60), width=2)
            dr.line([(x, h + 24 + y), (x + 12, h + 24 + y)], fill=(255, 60, 60), width=2)
        x += w
    sheet.save(out)
    print(out, sheet.size)


# In-game shot (960x640, --zoom 30, --tp 16,17): the 192 px hero cell lands at CAP_ORIGIN, scaled CAP_K (found 2026-11-08 by matching the
# offline T0 frame against t0_1.png, mean colour error 9 / 255).
CAP_ORIGIN, CAP_K = (333, 132), 1.53


def capture_cell(path, t):
    im = Image.open(path).convert("RGBA")
    lv1 = out_tool.lv1_frame()
    top, bot = out_tool.hair_top(lv1), out_tool.sole(lv1)
    fx = out_tool.face_x(lv1, top, bot)
    return crop_scaled(im, CAP_ORIGIN[0] + fx * CAP_K, CAP_ORIGIN[1] + top * CAP_K, (bot - top + 1) * CAP_K, SIDE9 if t == 9 else SIDE, Image.LANCZOS)


if __name__ == "__main__":
    main()

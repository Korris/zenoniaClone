"""Wizard 10 tiers, idle facing down: design sheet (top row) over the game frame composed offline (bottom row), same hero height.
Checks the size of staff, book and robe relative to the hero tier by tier (the designer, 2026-11-08). The game frame is composed the
way HeroRenderer draws it by Tools/build-wizard-tier-staff-book.py (cut art from the ChatGPT sources, not the installed files unless
`--live` is passed, which uses the installed staff/book PNGs + manifest grips + anchors).

  python Tools/build-wizard-tier-design-vs-game-sheet.py OUT.png [--live] [--tiers 0-4] [--captures DIR]

--captures DIR: bottom row from in-game screenshots DIR/z30-t<N>_1.png (900 x 900 window, --zoom 30, idle facing down, e.g.
`Zenonia.exe --class wizard --level 100 --worn-tier N --wear-tiers N,0,0 --face down --zoom 30 --shots 5 --shot-path DIR/z30-tN`);
the hero sits at CAP_HEAD (head centre column, hair top row) and is CAP_H px tall there (found by matching the offline frame, 2026-11-08).
"""
import importlib.util, json, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
DESIGN = "docs/reference/wizard-sets-10-tiers-ladder-v3.png"
HEAD_X = [111, 272, 452, 646, 837, 1038, 1248, 1470, 1741, 2022]  # head centre per tier on the design sheet (measured 2026-11-08)
HAIR_TOP = [289, 286, 284, 284, 283, 282, 282, 276, 277, 279]
SOLE = [533, 533, 533, 533, 534, 534, 534, 535, 535, 535]
UP, DOWN, SIDE = 0.80, 0.08, 0.62  # crop around the hero in H: above the hair top, below the soles, each side of the head centre
H_OUT = 302  # hero height in the output (2x the game's 151 px)
CAP_HEAD, CAP_H = (454, 267), 325.0


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def design_cell(im, t):
    H = SOLE[t] - HAIR_TOP[t]
    box = (HEAD_X[t] - SIDE * H, HAIR_TOP[t] - UP * H, HEAD_X[t] + SIDE * H, SOLE[t] + DOWN * H)
    k = H_OUT / H
    return im.crop(tuple(int(round(v)) for v in box)).resize((round((box[2] - box[0]) * k), round((box[3] - box[1]) * k)), Image.LANCZOS)


def live_parts(tool, t):
    """Installed staff/book art + grips of tier t."""
    data = json.load(open(tool.GEAR, encoding="utf-8"))
    out = []
    for kind, folder in (("weapon", tool.STAFF_LIVE), ("book", tool.BOOK_LIVE)):
        e = data[kind][t]
        a = np.asarray(Image.open(f"{folder}/{e['name']}-base.png").convert("RGBA")).astype(np.float32)
        out.append((a, e["baseGrip"]))
    return out


def game_cell(tool, t, staff, book):
    pad = 130
    c = tool.compose_tier(t, staff, book, pad=pad)
    H = tool.H_GAME
    box = (pad + tool.HEAD_CX - SIDE * H, pad + tool.HAIR_TOP - UP * H, pad + tool.HEAD_CX + SIDE * H, pad + tool.HAIR_TOP + H + DOWN * H)
    k = H_OUT / H
    return c.crop(tuple(int(round(v)) for v in box)).resize((round((box[2] - box[0]) * k), round((box[3] - box[1]) * k)), Image.NEAREST)


def capture_cell(path):
    im = Image.open(path).convert("RGB")
    H = CAP_H
    box = (CAP_HEAD[0] - SIDE * H, CAP_HEAD[1] - UP * H, CAP_HEAD[0] + SIDE * H, CAP_HEAD[1] + H + DOWN * H)
    k = H_OUT / H
    return im.crop(tuple(int(round(v)) for v in box)).resize((round((box[2] - box[0]) * k), round((box[3] - box[1]) * k)), Image.LANCZOS).convert("RGBA")


def main():
    out = sys.argv[1]
    live = "--live" in sys.argv
    tiers = list(range(10))
    if "--tiers" in sys.argv:
        a, b = sys.argv[sys.argv.index("--tiers") + 1].split("-")
        tiers = list(range(int(a), int(b) + 1))
    tool = load("wtsb", "Tools/build-wizard-tier-staff-book.py")
    im = Image.open(DESIGN).convert("RGB")
    caps = sys.argv[sys.argv.index("--captures") + 1] if "--captures" in sys.argv else None
    if not live and not caps:
        st, bk = tool.staffs(), tool.books()
    cells = []
    for t in tiers:
        if caps:
            cells.append((design_cell(im, t), capture_cell(f"{caps}/z30-t{t}_1.png")))
            continue
        if live:
            s, b = live_parts(tool, t)
            sp, bp = (s[0], s[1]), (b[0], b[1])
        else:
            sp, bp = (st[t][0], st[t][1]), bk[t]
        cells.append((design_cell(im, t), game_cell(tool, t, sp, bp)))
    w, h = cells[0][0].size
    sheet = Image.new("RGB", (w * len(cells), h * 2 + 24), (58, 62, 70))
    d = ImageDraw.Draw(sheet)
    for i, (dc, gc) in enumerate(cells):
        sheet.paste(dc, (i * w, 0))
        bg = Image.new("RGBA", gc.size, (58, 62, 70, 255))
        bg.alpha_composite(gc)
        sheet.paste(bg.convert("RGB"), (i * w, h + 24))
        d.text((i * w + 6, h + 5), f"T{tiers[i]}  design (top) / {'in game' if caps else 'game, offline'} (bottom)", fill=(255, 255, 160))
        for y in (round(UP * H_OUT), round(UP * H_OUT) + H_OUT):  # hair top and sole lines
            d.line([(i * w, y), (i * w + 12, y)], fill=(255, 60, 60), width=2)
            d.line([(i * w, h + 24 + y), (i * w + 12, h + 24 + y)], fill=(255, 60, 60), width=2)
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main()

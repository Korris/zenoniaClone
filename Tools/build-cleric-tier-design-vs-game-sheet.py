"""Cleric 10 tiers, idle facing down: design sheet (top row) over the game (bottom row), same hero height (hair top to soles).
Checks staff, shield and outfit size relative to the hero tier by tier, like Tools/build-wizard-tier-design-vs-game-sheet.py.
Bottom row: composed offline from the installed art (Tools/build-cleric-tier-staff-shield.py compose_tier with the live PNGs + grips),
or from in-game screenshots with --captures DIR (DIR/tN_1.png, e.g. `Zenonia.exe --class cleric --tier N --hairstyle 0 --tp 16,17
--face down --zoom 30 --shots 2.5 --shot-path DIR/tN -screen-width 960 -screen-height 640 -screen-fullscreen 0`): the hero is found
by matching the offline frame (scale + place searched once on the first capture, reused for the rest).

  python Tools/build-cleric-tier-design-vs-game-sheet.py OUT.png [--captures DIR]
"""
import importlib.util, json, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = "D:/zenoniaClone"
os.chdir(ROOT)
UP, DOWN, LEFT, RIGHT = 0.75, 0.08, 0.68, 0.56  # crop around the hero in H: above the hair top, below the soles, left / right of the head centre
H_OUT = 280


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


tool = load("ctss", "Tools/build-cleric-tier-staff-shield.py")
outfit = load("cood", "Tools/build-cleric-outfit-idle-down.py")
os.chdir(ROOT)


def crop_scaled(im, cx, top, H, resample):
    box = (cx - LEFT * H, top - UP * H, cx + RIGHT * H, top + H + DOWN * H)
    k = H_OUT / H
    return im.crop(tuple(int(round(v)) for v in box)).resize((round((box[2] - box[0]) * k), round((box[3] - box[1]) * k)), resample)


def design_cell(im, t):
    return crop_scaled(im, outfit.HEAD_X[t], outfit.HAIR_TOP[t], outfit.SOLE[t] - outfit.HAIR_TOP[t], Image.LANCZOS)


def live(t):
    data = json.load(open(tool.GEAR, encoding="utf-8"))
    out = []
    for kind, folder in (("weapon", tool.STAFF_LIVE), ("shield", tool.SHIELD_LIVE)):
        e = data[kind][t]
        out.append((np.asarray(Image.open(f"{folder}/{e['name']}-base.png").convert("RGBA")).astype(np.float32), e["baseGrip"]))
    return out


def game_cell(t):
    pad = 140
    st, sh = live(t)
    c = tool.compose_tier(t, st, sh, pad=pad)
    return crop_scaled(c, pad + tool.HEAD_CX, pad + tool.HAIR_TOP, tool.H_GAME, Image.NEAREST)


def find_hero(cap, ref, k_range=(1.4, 3.0)):
    """(err, scale, x, y) placing the offline frame (ref, RGBA, 192 cell) on the capture: masked mean abs grey difference,
    coarse search over the whole capture at 1/4 size, then fine search around the best spot."""
    g = np.asarray(cap.convert("L")).astype(np.float32)
    ra0 = np.asarray(ref).astype(np.float32)

    def score(k, xs0, ys0, step):
        r = ref.resize((max(1, round(ref.width * k)), max(1, round(ref.height * k))), Image.NEAREST)
        ra = np.asarray(r).astype(np.float32)
        m = ra[..., 3] > 220
        yy, xx = np.where(m)
        sel = slice(None, None, max(1, len(yy) // 700))
        yy, xx = yy[sel], xx[sel]
        vals = (ra[..., :3] @ np.array([0.299, 0.587, 0.114], np.float32))[yy, xx]
        best = None
        for y0 in ys0:
            for x0 in range(xs0[0], xs0[1], step):
                Y, X = yy + y0, xx + x0
                ok = (Y < g.shape[0]) & (X < g.shape[1])
                if ok.mean() < 0.98:
                    continue
                e = float(np.mean(np.abs(g[Y[ok], X[ok]] - vals[ok])))
                if best is None or e < best[0]:
                    best = (e, k, x0, y0)
        return best

    best = None
    for k in np.arange(k_range[0], k_range[1] + 1e-6, 0.1):
        b = score(k, (0, g.shape[1] - int(192 * k)), range(0, g.shape[0] - int(192 * k), 6), 6)
        if b and (best is None or b[0] < best[0]):
            best = b
    e, k0, x0, y0 = best
    for k in np.arange(k0 - 0.1, k0 + 0.1001, 0.02):
        b = score(k, (max(0, x0 - 8), x0 + 9), range(max(0, y0 - 8), y0 + 9), 1)
        if b and b[0] < best[0]:
            best = b
    return best


def main():
    out = sys.argv[1]
    caps = sys.argv[sys.argv.index("--captures") + 1] if "--captures" in sys.argv else None
    im = Image.open(outfit.DESIGN).convert("RGB")
    place = None
    cells = []
    for t in range(10):
        if caps:
            cap = Image.open(f"{caps}/t{t}_1.png").convert("RGB")
            if place is None:  # the hero stands on the same spot in every capture (same --tp, zoom, window)
                st, sh = live(t)
                ref = tool.compose_tier(t, st, sh, pad=0)
                e, k, x, y = find_hero(cap, ref)
                place = (k, x + tool.HEAD_CX * k, y + tool.HAIR_TOP * k, tool.H_GAME * k)
                print("hero in capture: scale %.2f, head centre %.0f, hair top %.0f, err %.1f" % (k, place[1], place[2], e))
            gc = crop_scaled(cap, place[1], place[2], place[3], Image.LANCZOS).convert("RGBA")
        else:
            gc = game_cell(t)
        cells.append((design_cell(im, t), gc))
    w, h = cells[0][0].size
    sheet = Image.new("RGB", (w * 10, h * 2 + 24), (58, 62, 70))
    d = ImageDraw.Draw(sheet)
    for i, (dc, gc) in enumerate(cells):
        sheet.paste(dc, (i * w, 0))
        bg = Image.new("RGBA", gc.size, (58, 62, 70, 255))
        bg.alpha_composite(gc)
        sheet.paste(bg.convert("RGB").resize((w, h)), (i * w, h + 24))
        d.text((i * w + 6, h + 5), f"T{i}  design (top) / {'in game' if caps else 'offline'} (bottom)", fill=(255, 255, 160))
        for y in (round(UP * H_OUT), round(UP * H_OUT) + H_OUT):
            d.line([(i * w, y), (i * w + 12, y)], fill=(255, 60, 60), width=2)
            d.line([(i * w, h + 24 + y), (i * w + 12, h + 24 + y)], fill=(255, 60, 60), width=2)
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main()

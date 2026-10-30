"""Slice ChatGPT skill VFX sheets (3 rows x 4 cols: cast / loop / impact) by transparent gutters.
Source:  Assets/ArtLibrary/custom/skill-vfx/<class>/<skill-id>.png
Sprites: Assets/Resources/Sprites/skills/<class>/<skill-id>/{cast,loop,impact}_<n>.png (trimmed, max side --max px)
Data:    Assets/Resources/Data/skills/<class>-<skill-id>.json  (frames: name, w, h, px, py from bottom; rows; fps)
Usage:   py -3 Tools/slice-skill-vfx.py <class> [skill-id ...] [--from-black] [--max 256] [--contact]
         (run from the project root; no skill ids = every sheet in the source folder; non-zero exit on a wrong grid)
--from-black forces the black-background path; it is also used automatically when a sheet has no real transparency.
--contact writes plans/261030-1521-skill-trees-implementation/visuals/<class>-contact.png (one row per skill)."""
import argparse, glob, os, sys
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vfx_slice_common import clean_mask, raw_spans, trim_box, from_black, write_frame, write_json

SRC = "Assets/ArtLibrary/custom/skill-vfx"
OUT = "Assets/Resources/Sprites/skills"
DATA = "Assets/Resources/Data/skills"
CONTACT = "plans/261030-1521-skill-trees-implementation/visuals"
ROWS = ["cast", "loop", "impact"]
COLS = 4
FPS = {"cast": 12, "loop": 12, "impact": 12}
MIN_FRAME_PX = 24     # a cell with less ink than MIN_FRAME_PX^2 pixels counts as empty


def load_rgba(path, force_black):
    """RGBA array; falls back to the black-key path when the sheet is (nearly) fully opaque."""
    im = Image.open(path)
    rgba = np.array(im.convert("RGBA"))
    opaque = (rgba[..., 3] > 245).mean()
    if force_black or opaque > 0.97:
        border = np.concatenate([rgba[:8].reshape(-1, 4), rgba[-8:].reshape(-1, 4)])[:, :3].max(axis=1)
        if np.median(border) > 40:   # e.g. a grey checkerboard painted into the pixels -> cannot be keyed
            raise ValueError("opaque sheet without a black background (painted checkerboard?) - regenerate it")
        return from_black(rgba), True
    return rgba, False


def grid_cuts(proj, n, window=0.3):
    """n cells along a projection: each cut is the emptiest point within +-window cell of the ideal grid line
    (the centre of the widest gutter when there is one). Returns (cells, clean) where clean = every cut hit empty space."""
    L, cell = len(proj), len(proj) / float(n)
    cuts, clean = [0], True
    for k in range(1, n):
        lo, hi = int(k * cell - window * cell), int(k * cell + window * cell)
        seg = proj[lo:hi]
        best = seg.min()
        clean = clean and best == 0
        runs = raw_spans((seg == best).astype(int), gutter_min=0)          # runs of the minimum value
        r0, r1 = max(runs, key=lambda r: r[1] - r[0])
        cuts.append(lo + (r0 + r1) // 2)
    cuts.append(L)
    return list(zip(cuts[:-1], cuts[1:])), clean


def row_frames(img, mask, y0, y1):
    """Trimmed frames of one row (empty cells skipped) + whether the column cuts all fell in clean gutters."""
    m = mask[y0:y1]
    cols, clean = grid_cuts(m.sum(axis=0), COLS)
    out = []
    for x0, x1 in cols:
        cm = m[:, x0:x1]
        if cm.sum() < MIN_FRAME_PX * MIN_FRAME_PX: continue
        ty0, ty1, tx0, tx1 = trim_box(cm)
        out.append(img[y0 + ty0:y0 + ty1, x0 + tx0:x0 + tx1].copy())
    return out, clean


def downscale(fr, max_px):
    h, w = fr.shape[:2]
    s = min(1.0, max_px / float(max(h, w)))
    if s >= 1.0: return fr
    im = Image.fromarray(fr).resize((max(1, round(w * s)), max(1, round(h * s))), Image.LANCZOS)
    return np.array(im)


def process(cls, sid, force_black, max_px):
    path = os.path.join(SRC, cls, sid + ".png")
    img, keyed = load_rgba(path, force_black)
    mask = clean_mask(img[..., 3], alpha_t=24 if keyed else 10)
    img[..., 3] = np.where(mask, img[..., 3], 0)
    rows, grid_ok = grid_cuts(mask.sum(axis=1), 3)
    folder = os.path.join(OUT, cls, sid)
    os.makedirs(folder, exist_ok=True)
    for old in glob.glob(os.path.join(folder, "*_*.png")): os.remove(old)
    meta = {"skill": sid, "class": cls, "rows": [], "fps": FPS, "frames": []}
    counts, frames_by_row = [], {}
    for rname, (y0, y1) in zip(ROWS, rows):
        frs, ok = row_frames(img, mask, y0, y1)
        grid_ok = grid_ok and ok
        counts.append(len(frs))
        if frs: meta["rows"].append(rname)
        frames_by_row[rname] = []
        for c, fr in enumerate(frs):
            fr = downscale(fr, max_px)
            h, w = fr.shape[:2]
            meta["frames"].append(write_frame(folder, "%s_%d" % (rname, c), fr, w / 2.0, h / 2.0))
            frames_by_row[rname].append(fr)
    write_json(os.path.join(DATA, "%s-%s.json" % (cls, sid)), meta)
    return counts, keyed, grid_ok, frames_by_row


def contact_sheet(cls, results, cell=128):
    """One line per skill: label + 12 frames on a dark checker so glow and alpha are both visible."""
    label_w, pad = 170, 4
    W = label_w + COLS * 3 * (cell + pad) + 2 * 12
    H = len(results) * (cell + pad) + pad
    sheet = Image.new("RGBA", (W, H), (24, 24, 30, 255))
    d = ImageDraw.Draw(sheet)
    for r, (sid, counts, fbr) in enumerate(results):
        y = pad + r * (cell + pad)
        d.text((6, y + cell // 2 - 12), sid, fill=(230, 230, 230, 255))
        d.text((6, y + cell // 2 + 2), "/".join(map(str, counts)), fill=(160, 160, 160, 255))
        x = label_w
        for rname in ROWS:
            for i in range(COLS):
                bg = (40, 40, 48, 255) if (i % 2 == 0) else (52, 52, 60, 255)
                d.rectangle([x, y, x + cell - 1, y + cell - 1], fill=bg)
                frs = fbr.get(rname, [])
                if i < len(frs):
                    im = Image.fromarray(frs[i]); im.thumbnail((cell, cell), Image.LANCZOS)
                    sheet.alpha_composite(im, (x + (cell - im.width) // 2, y + (cell - im.height) // 2))
                x += cell + pad
            x += 12
    os.makedirs(CONTACT, exist_ok=True)
    out = os.path.join(CONTACT, "%s-contact.png" % cls)
    sheet.convert("RGB").save(out)
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cls")
    ap.add_argument("skills", nargs="*")
    ap.add_argument("--from-black", action="store_true")
    ap.add_argument("--max", type=int, default=256)
    ap.add_argument("--contact", action="store_true")
    a = ap.parse_args()
    skills = a.skills or sorted(os.path.splitext(os.path.basename(p))[0] for p in glob.glob(os.path.join(SRC, a.cls, "*.png")))
    if not skills: sys.exit("no sheets in %s" % os.path.join(SRC, a.cls))
    bad, results = [], []
    for sid in skills:
        try:
            counts, keyed, grid_ok, fbr = process(a.cls, sid, a.from_black, a.max)
        except (OSError, ValueError) as e:
            print("%-22s ERROR %s" % (sid, e)); bad.append(sid); continue
        flag = "" if counts == [4, 4, 4] else "  <-- WRONG GRID"
        note = "" if grid_ok else "  (frames touch: cut at the emptiest line near the grid)"
        print("%-22s cast %d  loop %d  impact %d%s%s%s" % (sid, counts[0], counts[1], counts[2], "  (black-keyed)" if keyed else "", note, flag))
        if flag: bad.append(sid)
        results.append((sid, counts, fbr))
    if a.contact and results: print("contact:", contact_sheet(a.cls, results))
    if bad:
        print("wrong grid:", ", ".join(bad)); sys.exit(1)


if __name__ == "__main__":
    main()

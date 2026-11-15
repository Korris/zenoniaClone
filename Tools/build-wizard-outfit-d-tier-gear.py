"""Wizard outfit D: staff + spellbook art for the 10 tiers T0-T9 (14/11/2026, plans/261114-0821-wizard-outfit-d-redesign step 6).

Sources (Assets/ArtLibrary/classes/wizard/outfits/):
  outfit-d-tiers-design/wizard-d-tiers-staff-board-chatgpt-4.png   10 staffs side by side (user-approved colours)
  outfit-d-tiers-design/wizard-d-tiers-book-board-chatgpt-1.png    10 closed books side by side
  outfit-d-tiers/src/gear-override-*.png                           single items redrawn on a flat background (OVERRIDES), used instead
                                                                    of the board piece: T1 (green crystal / leaf on the #00FF00 board) and
                                                                    T4 (the board drew a crown, the design is a gold cage)
Writes (Resources, wizard): weapons/{staff,book}/<name>-{base,enhanced}.png, weapons/{staff,book}/<name>/idle-stance.png, the grips
in Data/classes/wizard/held-wizard.json, icons Sprites/icons/{weapon,offhand}-wizard-N.png. Old files backed up once to
outfit-d-tiers/old-gear/.

Sizes: staff height and head width measured on board -4 (HEAD_W, staff_height), shaft at the hold points at most MAX_SHAFT + 0.3*tier px, book 40 + 1.5*tier px wide at s 0.25.
Staff grip: every tier keeps the T0 apprentice geometry from grip to butt (GRIP_TO_BUTT art px), so the walk / attack anchors (moved
38.9 px down the shaft by the outfit D tools) and the idle-down artY (-28.6) hold every tier the same way and no butt goes lower than
T0's; taller tiers only grow upward. Book grip (base art, walk / attack / idle up-left-right) stays the old convention: lower edge at
18 % of the width; idle-stance (idle down, outfit D palm-up hand) is the OPEN book (OPEN_BOOKS, 15/11) centred on its BOTTOM CENTRE so it
rests on the palm; walk / attack / other idle facings keep the closed book.

  py Tools/build-wizard-outfit-d-tier-gear.py cut       -> outfit-d-tiers/staging/gear/ + gear-preview.png
  py Tools/build-wizard-outfit-d-tier-gear.py install   -> Resources (backs up old-gear/ once)
"""
import json, os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = "D:/zenoniaClone"
OUT = f"{ROOT}/Assets/ArtLibrary/classes/wizard/outfits"
DESIGN, WORK = f"{OUT}/outfit-d-tiers-design", f"{OUT}/outfit-d-tiers"
STAGE, OLD = f"{WORK}/staging/gear", f"{WORK}/old-gear"
RES = f"{ROOT}/Assets/Resources"
WEAP, ICONS = f"{RES}/Sprites/classes/wizard/weapons", f"{RES}/Sprites/icons"
MANIFEST = f"{RES}/Data/classes/wizard/held-wizard.json"
STAFF_S, BOOK_S, MAX_SHAFT = 0.447, 0.25, 6.0
GRIP_TO_BUTT = 167                         # apprentice-base.png before this tool: gripY 193 (196 in idle-stance), butt row 363
HOLD_ROWS = (64, 87)                       # art px below the grip the fist really holds: idle down (artY -28.6), walk / attack (38.9 px)
STAFF_SHAFTS = [76, 176, 280, 386, 490, 596, 696, 802, 918, 1058]   # shaft columns on the staff board
OVERRIDES = {("staff", 1): "gear-override-t1-staff", ("book", 1): "gear-override-t1-book", ("staff", 4): "gear-override-t4-staff"}


def keyed(path, ylim=None):
    """RGBA float, background (#00FF00 or #FF00FF, whichever fills the corners) keyed out by distance."""
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    magenta = np.median(g[:8, :8]) < 100 and np.median(r[:8, :8]) > 180
    if magenta:
        al = np.clip((np.maximum.reduce([g, 255 - r, 255 - b]) - 40) / 80.0, 0, 1)
        r = np.minimum(r, np.maximum(g, b) + 60); b = np.minimum(b, np.maximum(g, r) + 60)
    else:
        al = np.clip((np.maximum.reduce([r, b, 255 - g]) - 40) / 80.0, 0, 1)
        g = np.minimum(g, np.maximum(r, b) + 10)
    if ylim:
        al[ylim:] = 0
    return np.dstack([r, g, b, al * 255])


def crop(o):
    ys, xs = np.nonzero(o[..., 3] > 0)
    return o[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def board_staffs():
    """Split the staff board by growing each shaft column over the opaque pixels (heads of neighbours touch)."""
    a = keyed(f"{DESIGN}/wizard-d-tiers-staff-board-chatgpt-4.png", 1240)
    fg = a[..., 3] > 60
    lab = np.zeros(fg.shape, np.int16)
    for t, x in enumerate(STAFF_SHAFTS):
        lab[:, x - 3:x + 4][fg[:, x - 3:x + 4]] = t + 1
    while True:
        grown, changed = lab.copy(), False
        for t in range(1, 11):
            d = ndimage.binary_dilation(lab == t, structure=np.ones((3, 3), bool)) & fg & (grown == 0)
            if d.any():
                grown[d] = t; changed = True
        lab = grown
        if not changed:
            break
    out = []
    for t in range(10):
        m = lab == t + 1; o = a.copy()
        o[~ndimage.binary_dilation(m, iterations=1) | ((lab > 0) & ~m), 3] = 0
        out.append(crop(o))
    return out


def board_books():
    a = keyed(f"{DESIGN}/wizard-d-tiers-book-board-chatgpt-1.png", 443)
    lab, _ = ndimage.label(ndimage.binary_dilation(a[..., 3] > 60, iterations=2))
    objs = sorted([s for s in ndimage.find_objects(lab) if (lab[s] > 0).sum() > 2000], key=lambda s: s[1].start)
    assert len(objs) == 10, len(objs)
    return [crop(a[s].copy()) for s in objs]


def single(path):
    a = keyed(path)
    lab, _ = ndimage.label(ndimage.binary_dilation(a[..., 3] > 60, iterations=3))
    sizes = ndimage.sum(lab > 0, lab, range(1, lab.max() + 1)); i = 1 + int(np.argmax(sizes))
    o = a.copy(); o[lab != i, 3] = 0
    return crop(o)


def resized(o, w, h):
    pre = o.copy(); pre[..., :3] *= pre[..., 3:4] / 255.0
    im = np.dstack([np.asarray(Image.fromarray(pre[..., c]).resize((w, h), Image.LANCZOS)) for c in range(4)])
    im = np.clip(im, 0, 255); al = im[..., 3:4]
    im[..., :3] = np.where(al > 0.5, im[..., :3] * 255.0 / np.maximum(al, 1e-3), 0)
    im[..., 3] = np.where(im[..., 3] < 16, 0, im[..., 3])
    return np.clip(im, 0, 255)


def slim_shaft(o, rows, cap_px=MAX_SHAFT):
    """Narrow plain-shaft rows (width within 1.5x the median shaft width) to MAX_SHAFT frame px around their centre; ornaments keep
    their width. rows: the art rows allowed to change (below the head ornament)."""
    cap = cap_px / STAFF_S
    widths = {}
    for y in rows:
        xs = np.nonzero(o[y, :, 3] > 80)[0]
        if len(xs):
            widths[y] = xs[-1] - xs[0] + 1
    med = float(np.median(list(widths.values())))
    out = o.copy()
    for y, w in widths.items():
        if w <= 1.5 * med and w > cap:
            xs = np.nonzero(o[y, :, 3] > 80)[0]; c = (xs[0] + xs[-1]) / 2; k = cap / w
            row = Image.fromarray(o[y:y + 1].astype(np.uint8))
            row = row.transform(row.size, Image.AFFINE, (1 / k, 0, c * (1 - 1 / k), 0, 1, 0), Image.BICUBIC)
            out[y] = np.asarray(row).astype(np.float32)[0]
    return out, med * STAFF_S


def lengthen_shaft(art, h):
    """Stretch the longest plain-shaft run (rows within 1.5x the median width, between 35 % and 90 % of the height) vertically so the art is
    h rows tall: a redrawn override is drawn stubbier than the board staffs, scaling it to the board width keeps its head in proportion."""
    al = art[..., 3] > 80
    wid = np.array([(np.nonzero(r)[0][-1] - np.nonzero(r)[0][0] + 1) if r.any() else 0 for r in al])
    lo, hi = int(len(wid) * 0.35), int(len(wid) * 0.9)
    med = np.median(wid[lo:hi][wid[lo:hi] > 0])
    plain = (wid > 0) & (wid <= 1.5 * med); plain[:lo] = False; plain[hi:] = False
    best, run = (0, 0), None
    for y in range(len(wid) + 1):
        if y < len(wid) and plain[y]:
            run = y if run is None else run
        elif run is not None:
            best = max(best, (y - run, run), key=lambda b: b[0]); run = None
    n, y0 = best
    add = h - art.shape[0]
    if add <= 0 or n < 4:
        return art
    mid = resized(art[y0:y0 + n], art.shape[1], n + add)
    return np.concatenate([art[:y0], mid, art[y0 + n:]], 0)


# Staff size per tier MEASURED on the approved full-body board -4 (wizard-d-tiers-outfit-board-chatgpt-4.png, 15/11 night, user: "the staff is
# tiny, not like the board"): staff height and staff-head width over the hero's height H (hair top -> sole) read off the board for every
# tier. Height ~0.92 H (T0) -> 1.10 H (T9): smoothed to 150 + 4.5*tier px (H = 163 px in game: hair on); head width 0.17 H (T0) -> 0.41 H
# (T9, ice dragon). The first version scaled the whole staff-board art to 160 + 9*tier px tall, so its long thin shaft made the head
# fist-sized (0.08 H) -- half the board's.
GAME_H = 163
STAFF_X, FACE_X, FACE_CLEAR = 57, 80, 78   # (per tier below)
# idle-down redrawn with the board -4 arm pose (15/11, T9 first): staff fist further out, the staff head must end above the hair-side row
POSED = set(range(0, 10))                   # tiers redrawn in the board -4 arm pose (T9 first, T0-T8 15/11 later)
POSE_STAFF = {n: (52, 62, 66) for n in POSED}             # tier: (staff x, x the head may reach before it must clear, row it must end above)
# idle down: staff column, left edge of the eyes (x 83, rows 84-88 on the bald sheet), a staff head this wide must end above this row
BUTT_MAX = 24                              # frame px: the butt tip below the last plain run, at most (hands hold 36+ px above the butt)
HEAD_W = [0.17, 0.26, 0.24, 0.31, 0.31, 0.31, 0.31, 0.24, 0.34, 0.42]   # staff head width / H, board -4 (T9 re-read on the x3 crop 15/11: 0.35)


def staff_height(t):
    return 150 + 4.5 * t


def row_widths(art):
    al = art[..., 3] > 80
    return np.array([(np.nonzero(r)[0][-1] - np.nonzero(r)[0][0] + 1) if r.any() else 0 for r in al])


def plain_runs(art, lo_frac=0.15, hi_frac=0.92, min_len=8):
    """(start, length) of plain-shaft runs: rows within 1.5x the median shaft width, between lo_frac and hi_frac of the height."""
    wid = row_widths(art); n = len(wid); lo, hi = int(n * lo_frac), int(n * hi_frac)
    med = np.median(wid[int(n * 0.5):hi][wid[int(n * 0.5):hi] > 0])
    plain = (wid > 0) & (wid <= 1.5 * med); plain[:lo] = False; plain[hi:] = False
    runs, run = [], None
    for y in range(n + 1):
        if y < n and plain[y]:
            run = y if run is None else run
        elif run is not None:
            if y - run >= min_len:
                runs.append((run, y - run))
            run = None
    return runs


def head_width(art):
    """Widest row above the first plain-shaft run (the head: crystals, cage, dragon)."""
    runs = plain_runs(art)
    end = runs[0][0] if runs else int(art.shape[0] * 0.35)
    return int(row_widths(art)[:end].max())


def set_height(art, h):
    """Head (above the first plain-shaft run) + one plain shaft + butt tip (below the last run), the shaft resampled from the longest plain
    run to make the art h rows tall. Mid-shaft ornaments are dropped: with the head at board size, keeping them (and cutting only the
    plain rows) piled them into a stubby, sword-like shaft (first try 15/11)."""
    runs = plain_runs(art)
    assert runs, "no plain shaft"
    head, butt = art[:runs[0][0]], art[runs[-1][0] + runs[-1][1]:]
    cap = round(BUTT_MAX / STAFF_S)                              # the butt ornament grew with the head: the hands (walk / attack hold 39 px
    if butt.shape[0] > cap:                                      # down the shaft, 36 px above the butt) landed on it -- shrink it, centred
        bw = max(1, round(butt.shape[1] * cap / butt.shape[0])); small = resized(butt, bw, cap)
        cx = int(round(np.nonzero(butt[..., 3].max(0) > 80)[0].mean())); x0 = int(round(cx - bw / 2))
        butt = np.zeros((cap, art.shape[1], 4), np.float32)
        xs, xe = max(0, x0), min(art.shape[1], x0 + bw); butt[:, xs:xe] = small[:, xs - x0:xe - x0]
    y0, n = max(runs, key=lambda r: r[1])
    mid_h = h - head.shape[0] - butt.shape[0]
    assert mid_h >= 0.25 * h, f"head + butt too tall ({head.shape[0]} + {butt.shape[0]} of {h})"
    seg = art[y0 + 2:y0 + n - 2]
    mid = np.concatenate([seg] * int(np.ceil(mid_h / seg.shape[0])), 0)[:mid_h] if seg.shape[0] >= 4 else resized(seg, art.shape[1], mid_h)
    return np.concatenate([head, mid, butt], 0)


_LAST_H = [0]                               # staffs never get shorter tier to tier (old rule: size grows with the tier)


def staff_art(t, src, board=None):
    h = max(round(staff_height(t) / STAFF_S), _LAST_H[0] if t else 0)
    k = HEAD_W[t] * GAME_H / STAFF_S / head_width(src)           # head as wide as on the board, whatever the source's proportions
    while True:                                                  # a source whose plain shaft cannot be cut short enough: a bit smaller
        art = resized(src, max(1, round(src.shape[1] * k)), max(1, round(src.shape[0] * k)))
        try:
            art = set_height(art, h); break
        except AssertionError:
            k *= 0.97
    # idle down plants the butt on the sole (y 190) with the shaft at x ~57: a head wider than 2 * (FACE_X - 57) must end above the eyes
    # (row FACE_CLEAR), else it covers the left eye (T6-T9 first grounded try, 15/11) -- lengthen the plain shaft until it does
    runs = plain_runs(art); head_rows = runs[0][0] if runs else 0
    sx, fx, fc = POSE_STAFF.get(t, (STAFF_X, FACE_X, FACE_CLEAR))
    if head_width(art) * STAFF_S / 2 > fx - sx:
        need = round((190 - fc) / STAFF_S) + head_rows - art.shape[0]
        if need > 0:
            art = set_height(art, art.shape[0] + need); h = art.shape[0]
    _LAST_H[0] = art.shape[0]
    butt = np.nonzero(art[..., 3].max(1) > 80)[0][-1]
    gy = butt - GRIP_TO_BUTT
    art, shaft = slim_shaft(art, range(int(h * 0.3), butt - 8), cap_px=MAX_SHAFT + 0.3 * t)
    holds = [gy + d for d in HOLD_ROWS]
    hw = []
    for y in holds:
        xs = np.nonzero(art[y, :, 3] > 80)[0]; hw.append(round((xs[-1] - xs[0] + 1) * STAFF_S, 1))
    xs = np.nonzero(art[gy, :, 3] > 80)[0]; gx = (xs[0] + xs[-1]) / 2
    cv = np.zeros((art.shape[0] + 6, art.shape[1] + 6, 4), np.float32); cv[3:-3, 3:-3] = art
    grip = {"w": cv.shape[1], "h": cv.shape[0], "gripX": int(round(gx + 3)), "gripY": int(gy + 3)}
    return cv, grip, {"tier": t, "heightPx": round(h * STAFF_S, 1), "headWidthPx": round(head_width(art) * STAFF_S, 1),
                      "holdWidthsPx": hw, "medianShaftPx": round(shaft, 1)}


def book_art(t, src):
    w = round((40 + 1.5 * t) / BOOK_S)
    art = resized(src, w, max(1, round(src.shape[0] * w / src.shape[1])))
    cv = np.zeros((art.shape[0] + 6, art.shape[1] + 6, 4), np.float32); cv[3:-3, 3:-3] = art
    gx = round(w * 0.18) + 3
    gy = int(np.nonzero(cv[:, gx, 3] > 80)[0].max())
    return cv, {"w": cv.shape[1], "h": cv.shape[0], "gripX": gx, "gripY": gy}, {"tier": t, "widthPx": round(w * BOOK_S, 1)}


OPEN_BOOKS = ["wizard-d-tiers-open-books-t0-t4-chatgpt-1.png", "wizard-d-tiers-open-books-t5-t9-chatgpt-1.png"]   # src/, 5 per image
def open_width(t):
    """Open book width, frame px: 48 + 0.6*tier (it is wider than the closed one; at 1.2x the closed width T9's 64 px hid half the face).
    Tiers redrawn in the board -4 arm pose use the board's book: 0.27 H (T9, 15/11)."""
    return 35 + 0.6 * t if t in POSED else 48 + 0.6 * t     # board pose (book low on the palm): board size, T9 40 px


def open_books():
    """10 OPEN books (idle down only, user 15/11: the book lies open on the palm like the hero-select art), or None while missing."""
    out = []
    for name in OPEN_BOOKS:
        p = f"{WORK}/src/{name}"
        if not os.path.exists(p):
            return None
        a = keyed(p)
        lab, _ = ndimage.label(ndimage.binary_dilation(a[..., 3] > 60, iterations=3))
        objs = sorted([s for s in ndimage.find_objects(lab) if (lab[s] > 0).sum() > 3000], key=lambda s: s[1].start)
        assert len(objs) == 5, f"{name}: {len(objs)} books"
        out += [crop(a[s].copy()) for s in objs]
    return out


def open_book_art(t, src):
    w = round(open_width(t) / BOOK_S)
    art = resized(src, w, max(1, round(src.shape[0] * w / src.shape[1])))
    cv = np.zeros((art.shape[0] + 6, art.shape[1] + 6, 4), np.float32); cv[3:-3, 3:-3] = art
    return cv


def centered(cv, gx, gy):
    """Art on a canvas whose centre is (gx, gy): idle-stance pivots on its centre (GearOverlays.CenteredTier)."""
    w = 2 * int(np.ceil(max(gx, cv.shape[1] - gx))) + 6
    h = 2 * int(np.ceil(max(gy, cv.shape[0] - gy))) + 6
    out = Image.new("RGBA", (w, h))
    out.alpha_composite(Image.fromarray(cv.astype(np.uint8)), (int(round(w / 2 - gx)), int(round(h / 2 - gy))))
    return out


def book_seat(cv):
    """Bottom centre of the book (lowest opaque row at the middle column): where it rests on the palm."""
    a = cv[..., 3] > 80; xs = np.nonzero(a.any(0))[0]; cx = int(round((xs[0] + xs[-1]) / 2))
    return cx, int(np.nonzero(a[:, cx])[0].max())


def icon(cv):
    im = Image.fromarray(crop(cv).astype(np.uint8))
    k = min(56 / im.width, 56 / im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = Image.new("RGBA", (64, 64)); out.alpha_composite(im, ((64 - im.width) // 2, (64 - im.height) // 2))
    return out


def cmd_cut():
    os.makedirs(STAGE, exist_ok=True)
    staffs, books = board_staffs(), board_books()
    report = {"staff": [], "book": []}
    for t in range(10):
        for part, srcs, fn in (("staff", staffs, staff_art), ("book", books, book_art)):
            ov = OVERRIDES.get((part, t))
            src = single(f"{WORK}/src/{ov}.png") if ov and os.path.exists(f"{WORK}/src/{ov}.png") else srcs[t]
            cv, grip, info = fn(t, src)
            info["source"] = ov if src is not srcs[t] else "board"
            Image.fromarray(cv.astype(np.uint8)).save(f"{STAGE}/{part}-t{t}-base.png")
            seat = book_seat(cv) if part == "book" else (grip["gripX"], grip["gripY"])
            centered(cv, *seat).save(f"{STAGE}/{part}-t{t}-idle-stance.png")
            icon(cv).save(f"{STAGE}/{part}-t{t}-icon.png")
            info["grip"] = grip; report[part].append(info)
    opened = open_books()
    if opened:                                                   # the idle-down book: open, centred on its bottom centre (palm)
        for t in range(10):
            cv = open_book_art(t, opened[t])
            centered(cv, *book_seat(cv)).save(f"{STAGE}/book-t{t}-idle-stance.png")
            Image.fromarray(cv.astype(np.uint8)).save(f"{STAGE}/book-t{t}-open.png")
    print("open books:", "yes" if opened else "missing (closed book for idle)")
    json.dump(report, open(f"{STAGE}/gear.json", "w"), indent=1)
    for r in report["staff"]:
        print("staff", r["tier"], r["heightPx"], "head", r["headWidthPx"], "hold widths", r["holdWidthsPx"], "shaft", r["medianShaftPx"], r["source"])
    print("books", [r["widthPx"] for r in report["book"]])
    preview()


def preview():
    books = [f"{STAGE}/book-t{t}-open.png" if os.path.exists(f"{STAGE}/book-t{t}-open.png") else f"{STAGE}/book-t{t}-base.png" for t in range(10)]
    tiles = [Image.open(f"{STAGE}/staff-t{t}-base.png") for t in range(10)] + [Image.open(b) for b in books]
    tiles = [t.resize((max(1, round(t.width * (STAFF_S if i < 10 else BOOK_S) * 3)), max(1, round(t.height * (STAFF_S if i < 10 else BOOK_S) * 3))), Image.NEAREST)
             for i, t in enumerate(tiles)]
    W = sum(t.width + 12 for t in tiles); H = max(t.height for t in tiles) + 20
    sheet = Image.new("RGBA", (W, H), (58, 62, 70, 255)); x = 0
    for t in tiles:
        sheet.alpha_composite(t, (x, H - 10 - t.height)); x += t.width + 12
    sheet.save(f"{WORK}/staging/gear-preview.png"); print("preview", f"{WORK}/staging/gear-preview.png")


def cmd_install():
    report = json.load(open(f"{STAGE}/gear.json"))
    man = json.load(open(MANIFEST, encoding="utf-8"))
    os.makedirs(OLD, exist_ok=True)
    if not os.path.exists(f"{OLD}/held-wizard.json"):
        shutil.copy(MANIFEST, f"{OLD}/held-wizard.json")
    for part, key, icon_name in (("staff", "weapon", "weapon"), ("book", "book", "offhand")):
        for t in range(10):
            entry = man[key][t]; name = entry["name"]
            targets = {f"{WEAP}/{part}/{name}-base.png": f"{part}-t{t}-base.png", f"{WEAP}/{part}/{name}-enhanced.png": f"{part}-t{t}-base.png",
                       f"{WEAP}/{part}/{name}/idle-stance.png": f"{part}-t{t}-idle-stance.png", f"{ICONS}/{icon_name}-wizard-{t}.png": f"{part}-t{t}-icon.png"}
            for dst, src in targets.items():
                bak = f"{OLD}/{os.path.relpath(dst, RES).replace(os.sep, '__').replace('/', '__')}"
                if os.path.exists(dst) and not os.path.exists(bak):
                    shutil.copy(dst, bak)
                shutil.copy(f"{STAGE}/{src}", dst)
            g = report[part][t]["grip"]
            entry["baseGrip"] = dict(g); entry["enhancedGrip"] = dict(g); entry.pop("bobY", None)
    json.dump(man, open(MANIFEST, "w", encoding="utf-8"), indent=2)
    print("installed 10 staffs + 10 books; old copies in", OLD)


if __name__ == "__main__":
    {"cut": cmd_cut, "install": cmd_install}[sys.argv[1]]()

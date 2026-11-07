"""Hairstyles 1..4 in the side (right) and back (up) facings for the head layer (Tools/build-head-layer.py dirs-ref / dirs-sheet).

ChatGPT gets the style 0 head of that facing (4x crop on #00FF00) and the five approved facing-down heads, and draws one image with the
five styles left to right in that facing, the first a copy of style 0. That copy gives the scale (fitted onto style 0 over the whole head);
every head of the image is drawn the same size, so styles 1..4 use it too and only their position is fitted:
  right: least squares over style 0's face (eyes, cheek, chin), then style 0's face is grafted back like the facing-down styles;
  up:    no face: the head is centred on style 0's back head, its top row moved like that style's facing-down top row.
Skin below the neck cut where the headless body is opaque is dropped (the collar stays in front of a redrawn neck); long hair below the
cut stays in front of the back, as it should.
"""
import os

import numpy as np
from PIL import Image
from scipy import ndimage

import head_layer_core as core
import head_layer_dirs as hd
import head_layer_styles as st
from head_layer_classes import CLASSES, skin
from head_layer_dir_classes import DIRS

FRAME = core.FRAME
UP = 4


def bbox(a, pad=8):
    ys, xs = np.where(a > 8)
    return max(xs.min() - pad, 0), max(ys.min() - pad, 0), min(xs.max() + 1 + pad, FRAME), min(ys.max() + 1 + pad, FRAME)


def green4x(cell, box):
    x0, y0, x1, y1 = box
    im = core.on_bg(cell[y0:y1, x0:x1]).convert("RGB")
    return im.resize((im.width * UP, im.height * UP), Image.NEAREST)


def ref(cls, out):
    """Inputs for ChatGPT: style 0 head right / up at 4x on #00FF00, and the facing-down heads of styles 0..4 side by side."""
    os.makedirs(out, exist_ok=True)
    for d in ("right", "up"):
        h = hd.head_of(cls, 0, d)
        green4x(h, bbox(h[..., 3])).save(f"{out}/{cls}-style0-{d}-4x-green.png")
    downs = [hd.head_of(cls, s, "down") for s in range(5)]
    boxes = [bbox(h[..., 3]) for h in downs]
    y0, y1 = min(b[1] for b in boxes), max(b[3] for b in boxes)
    tiles = [green4x(h, (b[0], y0, b[2], y1)) for h, b in zip(downs, boxes)]
    gap = 40
    strip = Image.new("RGB", (sum(t.width for t in tiles) + gap * 4, tiles[0].height), (0, 255, 0))
    x = 0
    for t in tiles:
        strip.paste(t, (x, 0))
        x += t.width + gap
    strip.save(f"{out}/{cls}-styles-down-4x-green.png")
    print("->", out)


def fit_whole(src, h0):
    """(k, dx, dy) of the style 0 copy onto style 0's head over every head pixel (scale search, then shift)."""
    m = h0[..., 3] > 200
    ys, xs = np.where(m)
    tgt = h0[m]
    sa = src[..., 3] > 128
    sy_, sx_ = np.where(sa)
    hy, hx = np.where(m)
    k0 = (hx.max() - hx.min() + 1) / (sx_.max() - sx_.min() + 1)
    base = ((sx_.min() + sx_.max() + 1) / 2, sy_.min(), (hx.min() + hx.max() + 1) / 2, hy.min())
    best = None
    for k in k0 * np.arange(0.9, 1.101, 0.02):
        small, sf = st.sampler(src, k)
        for dx in range(-6, 7):
            for dy in range(-6, 7):
                c = st.sample(small, sf, k, base[0], base[1], base[2] + dx, base[3] + dy, ys, xs)
                e = np.abs(c[:, :3] - tgt[:, :3]).mean() + np.abs(c[:, 3] - 255).mean()
                if best is None or e < best[0]:
                    best = (e, k, dx, dy)
    e, kb, dxb, dyb = best
    for step_k, step in ((0.006, 0.5), (0.002, 0.25)):
        e, kb, dxb, dyb = best
        for k in kb * (1 + np.arange(-2, 2.01) * step_k):
            small, sf = st.sampler(src, k)
            for dx in dxb + np.arange(-2, 2.01) * step:
                for dy in dyb + np.arange(-2, 2.01) * step:
                    c = st.sample(small, sf, k, base[0], base[1], base[2] + dx, base[3] + dy, ys, xs)
                    er = np.abs(c[:, :3] - tgt[:, :3]).mean() + np.abs(c[:, 3] - 255).mean()
                    if er < best[0]:
                        best = (er, k, dx, dy)
    return best


def render(src, k, cx_src, top_src, cx, top):
    small, sf = st.sampler(src, k)
    yy, xx = np.mgrid[0:FRAME, 0:FRAME]
    return st.sample(small, sf, k, cx_src, top_src, cx, top, yy.ravel(), xx.ravel()).reshape(FRAME, FRAME, 4)


def extent(a):
    ys, xs = np.where(a > 128)
    return (xs.min() + xs.max() + 1) / 2, ys.min()


def front(a):
    """(rightmost solid column, top row): reference point of a head facing right."""
    ys, xs = np.where(a > 128)
    return xs.max() + 0.5, ys.min()


def face_mask(cls, d, h0):
    """Style 0 face pixels of the side head (graft rows down, face columns, not hair)."""
    cfg, dc = CLASSES[cls], DIRS[cls][d]
    yy, xx = np.mgrid[0:FRAME, 0:FRAME]
    hair0 = cfg["hair"](h0) & (h0[..., 3] > 8)
    light = (h0[..., 3] > 8) & (h0[..., :3] @ [0.3, 0.59, 0.11] >= 90)
    _, (iy, ix) = ndimage.distance_transform_edt(~light, return_indices=True)
    hair0 |= ~light & hair0[iy, ix]
    fx0, fx1 = dc["face_x"]
    return (h0[..., 3] > 200) & ~ndimage.binary_dilation(hair0, iterations=1) & (yy >= dc["graft"] - 4) & (xx >= fx0) & (xx < fx1)


def fit_face_shift(part, k, h0, face):
    """Shift (in cell px) placing a side head of known scale on style 0's face; returns the rendered cell and error."""
    ys, xs = np.where(face)
    ref = h0[face]
    small, sf = st.sampler(part, k)
    # reference x = the face's front edge (nose side), not the box centre: a ponytail or buns move the centre (Assassin 2026-11-07)
    cx_s, top_s = front(part[..., 3])
    cx0, top0 = front(h0[..., 3])
    best = None
    for dx in np.arange(-8, 8.01, 1):
        for dy in np.arange(-24, 24.01, 1):
            c = st.sample(small, sf, k, cx_s, top_s, cx0 + dx, top0 + dy, ys, xs)
            e = np.abs(c[:, :3] - ref[:, :3]).mean() + np.abs(c[:, 3] - 255).mean()
            if best is None or e < best[0]:
                best = (e, dx, dy)
    e0, dxb, dyb = best
    for step in (0.5, 0.25):
        e0, dxb, dyb = best
        for dx in dxb + np.arange(-2, 2.01) * step:
            for dy in dyb + np.arange(-2, 2.01) * step:
                c = st.sample(small, sf, k, cx_s, top_s, cx0 + dx, top0 + dy, ys, xs)
                e = np.abs(c[:, :3] - ref[:, :3]).mean() + np.abs(c[:, 3] - 255).mean()
                if e < best[0]:
                    best = (e, dx, dy)
    e, dx, dy = best
    return render(part, k, cx_s, top_s, cx0 + dx, top0 + dy), e


def neck_trim(cls, d, cell, h0):
    """Drop redrawn skin below style 0's neck cut where the headless idle frame 0 is opaque (collar, scarf stay in front)."""
    hl = hd.cell(hd.sheet(cls, "idle", "headless"), hd.DIR_ROW[d], 0)
    cut = core.polyline(sorted(hd.cut_cfg(cls, d)["cut"]))[None, :]
    rows = np.arange(FRAME)[:, None]
    body = hl[..., 3] > 250  # only where the body is opaque, or the dropped pixels open holes at the chin
    sk = skin(cell) & (cell[..., 3] > 8)
    light = (cell[..., 3] > 8) & (cell[..., :3] @ [0.3, 0.59, 0.11] >= 90)
    _, (iy, ix) = ndimage.distance_transform_edt(~light, return_indices=True)
    s = sk | ((cell[..., 3] > 8) & ~light & sk[iy, ix])  # skin and its outline
    # only the neck: a band just under the cut, in the neck columns (light strands of long hair hanging over the back read as skin, Cleric 3)
    kx0, kx1 = hd.cut_cfg(cls, d)["keep"]
    cols = np.arange(FRAME)[None, :]
    drop = s & body & (rows >= cut) & (rows < cut + 10) & (cols >= kx0 - 4) & (cols < kx1 + 4) & (h0[..., 3] < 128)  # never inside style 0's head: its chin is also kept on the body, and a breathing frame moves the two apart (Wizard chin holes)
    lab, n = ndimage.label(drop, np.ones((3, 3)))
    for i in range(1, n + 1):  # a real neck is one patch of skin; lone highlights in hair are a few px
        blob = lab == i
        if (blob & sk).sum() < 12:
            drop[blob] = False
    out = cell.copy()
    out[drop] = 0
    return out


def run(cls, src, d, n0, dest, match_down=False):
    """dest: an output dir (preview cells) or `write` (heads/<n>/<d>.png). The first head of the image must be the style 0 copy.
    match_down: the style 0 copy is not the new style 0 (Wizard: the copy wears the dropped hood, hair drawn up to the hood tip came out
    13 px taller than facing down): side heads also search their scale on the face; back heads take the width and top row of the same
    style's facing-down head."""
    assert d in ("right", "up")
    h0 = hd.head_of(cls, 0, d)
    old = f"{core.paths(cls)['art']}/rejected-hood/0/{d}.png"
    if match_down and os.path.isfile(old):  # the painted (hooded) style 0 head: the copy's reference, the face graft and the neck cut follow it
        h0 = np.array(Image.open(old).convert("RGBA")).astype(np.float64)
    img = st.keyed(src)
    parts = [img[:, a:b] for a, b in st.split_heads(img)]
    print(f"{len(parts)} head(s) in {src}")
    e, k, dx, dy = fit_whole(parts[0], h0)
    print(f"style 0 copy: scale {k:.4f} (dx {dx:+.2f} dy {dy:+.2f}) head error {e:.1f}")
    face = face_mask(cls, d, h0) if d == "right" else None
    cx0, top0 = extent(h0[..., 3])
    copy_top = np.where(parts[0][..., 3] > 128)[0].min()  # fit_whole's reference rows
    h0_top = np.where(h0[..., 3] > 200)[0].min()
    heads = core.paths(cls)["heads"]
    for i, part in enumerate(parts[1:]):
        n = n0 + i
        if d == "right" and match_down:
            cell, err, kk = min((fit_face_shift(part, kk, h0, face) + (kk,) for kk in k * np.arange(0.84, 1.061, 0.03)), key=lambda t: t[1])
            cell, _ = st.graft_face(cell, h0, face, dict(CLASSES[cls], graft=DIRS[cls][d]["graft"], purge=None))
            note = f"face error {err:.1f}, scale {kk / k:.3f} x the copy's"
        elif d == "up" and match_down:
            dn = hd.head_of(cls, n, "down")[..., 3] > 128
            dxs = np.where(dn.any(0))[0]
            pxs = np.where((part[..., 3] > 128).any(0))[0]
            kk = (dxs.max() - dxs.min() + 1) / (pxs.max() - pxs.min() + 1)
            cx_s, top_s = extent(part[..., 3])
            cell = render(part, kk, cx_s, top_s, (dxs.min() + dxs.max() + 1) / 2, np.where(dn.any(1))[0].min())
            note = f"width and top of the facing-down head, scale {kk / k:.3f} x the copy's"
        elif d == "right":
            cell, err = fit_face_shift(part, k, h0, face)
            cell, _ = st.graft_face(cell, h0, face, dict(CLASSES[cls], graft=DIRS[cls][d]["graft"], purge=None))
            note = f"face error {err:.1f}"
        else:
            # no face to fit: keep the row ChatGPT drew it at, relative to the style 0 copy (the heads of one image share a baseline;
            # matching the facing-down top failed for the Assassin, whose style 0 ponytail towers over every other style)
            cx_s = extent(part[..., 3])[0]
            cell = render(part, k, cx_s, copy_top, cx0 + dx, h0_top + dy)
            note = "row kept from the image"
        cell = neck_trim(cls, d, cell, h0)
        solid = core.largest(cell[..., 3] > 40, extra_min=40)
        cell[~ndimage.binary_dilation(solid, iterations=1)] = 0
        print(f"style {n} {d}: {note}")
        if dest == "write":
            os.makedirs(f"{heads}/{n}", exist_ok=True)
            Image.fromarray(core.u8(cell)).save(f"{heads}/{n}/{d}.png")
            print("wrote", f"{heads}/{n}/{d}.png")
        else:
            os.makedirs(dest, exist_ok=True)
            Image.fromarray(core.u8(cell)).save(f"{dest}/{cls}-style{n}-{d}-cell.png")

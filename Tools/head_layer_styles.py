"""New hairstyles for the head layer (Tools/build-head-layer.py `style` / `sheet`): ChatGPT edits of the 4x head crop, keyed off #00FF00,
fitted onto style 0's face (scale and move by least squares over the face pixels), then style 0's face (eyes, mouth, cheeks, chin) is
grafted back wherever the new hair does not cover it, so every style has the same face. `sheet` splits an image of several heads side by
side (left to right = styles N0, N0+1, ...) at the empty columns between them."""
import os

import numpy as np
from PIL import Image
from scipy import ndimage

import head_layer_core as core
import head_layer_masks as masks
from head_layer_classes import CLASSES, skin

FRAME = core.FRAME


def keyed(src):
    """Straight-alpha float RGBA of a ChatGPT image on flat #00FF00."""
    return core.straight(masks.warrior_move().key_green(np.array(Image.open(src).convert("RGB"))), cut=3)


def split_heads(rgba, min_share=0.08):
    """Column ranges of the heads, split at runs of empty columns; small pieces join the nearest head."""
    mass = (rgba[..., 3] > 128).sum(0)
    cols = np.where(mass > 0)[0]
    segs, start = [], cols[0]
    for a, b in zip(cols[:-1], cols[1:]):
        if b - a > 6:
            segs.append([start, a + 1])
            start = b
    segs.append([start, cols[-1] + 1])
    total = mass.sum()
    big = [s for s in segs if mass[s[0]:s[1]].sum() >= min_share * total]
    for s in segs:
        if s not in big:
            near = min(big, key=lambda t: min(abs(s[0] - t[1]), abs(t[0] - s[1])))
            near[0], near[1] = min(near[0], s[0]), max(near[1], s[1])
    return sorted(big)


def skin_box(rgba, rows_from=0):
    """(x0, x1, y1) of the largest skin region: face width and chin row."""
    m = skin(rgba) & (rgba[..., 3] > 200)
    m[:rows_from] = False
    m = core.largest(m)
    ys, xs = np.where(m)
    return xs.min(), xs.max() + 1, ys.max() + 1


def sampler(src, k):
    """Source copy resized to 4 samples per cell px (antialiased), and its scale against the source."""
    h, w = src.shape[:2]
    f = k * 4
    p = core.pm(src)
    size = (max(1, round(w * f)), max(1, round(h * f)))
    small = np.dstack([np.array(Image.fromarray(p[..., c].astype(np.float32), "F").resize(size, Image.LANCZOS)) for c in range(4)])
    return small, size[0] / w


def sample(small, sf, k, cx_src, cy_src, cx, cy, ys, xs):
    sx = ((xs + 0.5 - cx) / k + cx_src) * sf - 0.5
    sy = ((ys + 0.5 - cy) / k + cy_src) * sf - 0.5
    out = np.stack([ndimage.map_coordinates(small[..., c], [sy, sx], order=1, mode="constant") for c in range(4)], -1)
    return core.straight(np.clip(out, 0, 255)[None])[0]


def fit(src, f0, face, k_hint=None):
    """Best (k, dx, dy) placing the source head on style 0's face; k = cell px per source px. k_hint: scale of the other heads of the
    same sheet (they are drawn the same size), used when the skin box misleads (bangs or locks over the cheeks)."""
    ys0, xs0 = np.where(core.largest(face & (np.arange(FRAME)[:, None] >= np.where(face)[0].min() + 4)))
    x0, x1, y1 = xs0.min() + 1, xs0.max(), ys0.max() + 1  # style 0 face box (inside the outline)
    s0, s1, sy1 = skin_box(src)
    k0 = k_hint or (x1 - x0) / (s1 - s0)
    ys, xs = np.where(face)
    ref = f0[face]
    base = ((s0 + s1) / 2, sy1, (x0 + x1) / 2, y1)  # source centre / chin -> cell centre / chin

    def err(k, dx, dy, small, sf):
        c = sample(small, sf, k, base[0], base[1], base[2] + dx, base[3] + dy, ys, xs)
        return np.abs(c[:, :3] - ref[:, :3]).mean() + np.abs(c[:, 3] - 255).mean()

    best = None
    span, reach = (0.04, 8) if k_hint else (0.1, 4)
    for k in k0 * np.arange(1 - span, 1 + span + 0.001, 0.02 if k_hint else 0.025):
        small, sf = sampler(src, k)
        for dx in np.arange(-reach, reach + 0.01, 1):
            for dy in np.arange(-12, 12.01, 1):  # the chin of a face mask (Assassin) is not the skin's bottom row
                e = err(k, dx, dy, small, sf)
                if best is None or e < best[0]:
                    best = (e, k, dx, dy)
    for step_k, step in ((0.008, 0.5), (0.003, 0.2)):
        e0, kb, dxb, dyb = best
        for k in kb * (1 + np.arange(-2, 2.01) * step_k):
            small, sf = sampler(src, k)
            for dx in dxb + np.arange(-2, 2.01) * step:
                for dy in dyb + np.arange(-2, 2.01) * step:
                    e = err(k, dx, dy, small, sf)
                    if e < best[0]:
                        best = (e, k, dx, dy)
    e, k, dx, dy = best
    small, sf = sampler(src, k)
    yy, xx = np.mgrid[0:FRAME, 0:FRAME]
    cell = sample(small, sf, k, base[0], base[1], base[2] + dx, base[3] + dy, yy.ravel(), xx.ravel()).reshape(FRAME, FRAME, 4)
    return cell, k, dx, dy, e


def graft_face(cell, f0, face, cfg):
    """Style 0's face replaces ChatGPT's redraw below the graft row wherever the new hair does not cover it."""
    rows = np.arange(FRAME)[:, None]
    light = (cell[..., 3] > 128) & (cell[..., :3] @ [0.3, 0.59, 0.11] >= 70)
    changed = np.abs(cell[..., :3] - f0[..., :3]).max(-1) > 60
    cand = light & ~skin(cell) & (rows < cfg["graft"]) & (changed | (f0[..., 3] < 128))
    if cand.sum() < 30:
        cand = light & ~skin(cell) & ~face
    ref = np.median(cell[cand][:, :3], 0)
    hair_light = light & (np.abs(cell[..., :3] - ref).sum(-1) < 120) & ~skin(cell)
    _, (iy, ix) = ndimage.distance_transform_edt(~light, return_indices=True)
    hair = hair_light | ((cell[..., 3] > 128) & ~light & hair_light[iy, ix])
    # where the new art is clear only the inside of style 0's face is grafted (not stray outline pixels of the old hair)
    inside = ndimage.binary_dilation(ndimage.binary_fill_holes(skin(f0) & face), iterations=1)
    graft = face & (rows >= cfg["graft"]) & ~ndimage.binary_dilation(hair, iterations=1) & ((cell[..., 3] > 128) | inside)
    out = cell.copy()
    out[graft] = f0[graft]
    # lone dark specks ChatGPT left on the skin (a cut-off fringe tip on the nose, Mechanic 2026-11-07): tiny dark islands below the graft
    # row, surrounded by light skin on every side, take style 0's pixels
    dark = (out[..., 3] > 128) & (out[..., :3] @ [0.3, 0.59, 0.11] < 130) & face & (rows >= cfg["graft"])
    lab, n = ndimage.label(dark, np.ones((3, 3)))
    for i in range(1, n + 1):
        blob = lab == i
        ring = ndimage.binary_dilation(blob, np.ones((3, 3))) & ~blob
        if blob.sum() <= 4 and (out[..., :3] @ [0.3, 0.59, 0.11] >= 140)[ring].all():
            out[blob] = f0[blob]
    purge = cfg.get("purge")
    if purge is not None and not purge(ref[None, None, :])[0, 0]:
        # ChatGPT's own copy of an accessory style 0 now provides (the Assassin's headband, drawn lower): its leftovers beside the graft
        # take the nearest new hair pixel
        bad = purge(out) & ~graft & (out[..., 3] > 128) & (rows >= cfg["graft"] - 6) & (rows < cfg["graft"] + 24)
        _, (iy, ix) = ndimage.distance_transform_edt(~(hair & ~bad), return_indices=True)
        out[bad] = out[iy[bad], ix[bad]]
    return out, ref


def fill_eye_holes(cell, colour, rows):
    """Clear pixels enclosed by the opaque face within `rows` (y0, y1) take the iris colour (Cleric 2026-11-07: the painted doll's green
    irises had been keyed out with the #00FF00 background, so the eyes showed whatever was behind the head)."""
    solid = cell[..., 3] > 128
    holes = ndimage.binary_fill_holes(solid) & ~solid
    band = np.zeros_like(holes)
    band[rows[0]:rows[1]] = True
    holes &= band
    out = cell.copy()
    lab, n = ndimage.label(holes)
    c = np.array(colour, np.float64)
    for i in range(1, n + 1):  # anime iris: dark at the top, lighter toward the bottom (like the side-view eye)
        ys, xs = np.where(lab == i)
        t = (ys - ys.min()) / max(ys.max() - ys.min(), 1)
        out[ys, xs, :3] = c * (0.7 + 0.3 * t[:, None]) + np.array([30, 90, 60]) * (t[:, None] ** 2)
        out[ys, xs, 3] = 255
    return out, int(holes.sum())


def run(cls, cmd, src, n0, dest, graft=True):
    # graft=False: keep ChatGPT's whole face (Cleric 2026-11-07: the painted sprite's dark grey eyes looked dead next to the new green-eyed styles)
    cfg = CLASSES[cls]
    fr = core.frames(cls)
    head, _ = masks.head_mask(cls, cfg, fr[0])
    f0 = fr[0]
    light = (f0[..., 3] > 8) & (f0[..., :3] @ [0.3, 0.59, 0.11] >= 90)
    hair0 = cfg["hair"](f0) & (f0[..., 3] > 8)  # dark strands too: grafted they would show as old-hair crumbs
    _, (iy, ix) = ndimage.distance_transform_edt(~light, return_indices=True)
    hair0 |= ~light & hair0[iy, ix]  # dark outline pixels of the old hair are hair too
    if "shadow" in cfg:  # the old bangs' pink shadow strokes on the brow: hair, not face
        test, below = cfg["shadow"]
        hair0 |= test(f0) & (np.arange(FRAME)[:, None] < below)
    yy, xx = np.mgrid[0:FRAME, 0:FRAME]
    eyes = np.zeros((FRAME, FRAME), bool)
    for x0, y0, x1, y1 in cfg["eyes"]:
        eyes |= (xx >= x0) & (xx < x1) & (yy >= y0) & (yy < y1)
    face = head & (~ndimage.binary_dilation(hair0, iterations=1) | eyes) & (f0[..., 3] > 200) & (yy >= cfg["graft"] - 4) & (xx >= cfg["face_x"][0]) & (xx < cfg["face_x"][1])
    img = keyed(src)
    parts = [img] if cmd == "style" else [img[:, a:b] for a, b in split_heads(img)]
    print(f"{len(parts)} head(s) in {src}")
    heads = core.paths(cls)["heads"]
    fits = [fit(part, f0, face) for part in parts]
    errs = [f[4] for f in fits]
    good = [f[1] for f in fits if f[4] <= 1.5 * np.median(errs)]
    for i, part in enumerate(parts):
        if fits[i][4] > 1.5 * np.median(errs) and good:
            again = fit(part, f0, face, float(np.median(good)))
            print(f"style {n0 + i}: refit with the sheet's scale, face error {fits[i][4]:.1f} -> {again[4]:.1f}")
            fits[i] = min(fits[i], again, key=lambda f: f[4])
    for i, part in enumerate(parts):
        n = n0 + i
        cell, k, dx, dy, e = fits[i]
        cell, ref = graft_face(cell, f0, face, cfg) if graft else (cell, None)
        if "eye_fill" in cfg:
            cell, nfill = fill_eye_holes(cell, *cfg["eye_fill"])
            print(f"style {n}: {nfill} see-through eye px filled")
        if "behind_x" in cfg:  # back hair ChatGPT drew under the chin goes behind the neck and chest (it read as a beard)
            bx0, bx1 = cfg["behind_x"]
            bottom = np.where(head.any(0), FRAME - 1 - np.argmax(head[::-1], 0), 0)
            under = (np.arange(FRAME)[:, None] > bottom[None, :]) & (xx >= bx0) & (xx < bx1)
            cell[under] = 0
        solid = core.largest(cell[..., 3] > 40, extra_min=40)  # specks the keying or the graft left beside the head
        cell[~ndimage.binary_dilation(solid, iterations=1)] = 0
        print(f"style {n}: scale {k:.4f} dx {dx:+.2f} dy {dy:+.2f} face error {e:.1f}, hair colour {np.round(ref).astype(int).tolist() if ref is not None else '-'}")
        if dest == "write":
            os.makedirs(f"{heads}/{n}", exist_ok=True)
            Image.fromarray(core.u8(cell)).save(f"{heads}/{n}/down.png")
            print("wrote", f"{heads}/{n}/down.png")
        else:
            os.makedirs(dest, exist_ok=True)
            Image.fromarray(core.u8(cell)).save(f"{dest}/{cls}-style{n}-cell.png")

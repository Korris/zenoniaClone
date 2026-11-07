"""Head masks, headless body rows and per-frame anchors of the head layer tool (Tools/build-head-layer.py)."""
import importlib.util

import numpy as np
from scipy import ndimage

import head_layer_core as core

FRAME = core.FRAME
WARRIOR_BODY_ROW, WARRIOR_TOP_ROW, WARRIOR_HAIR_ROW = 105, 104, 90  # prototype cut: light components reaching BODY_ROW are body, nothing below TOP_ROW is head
_mv = None


def warrior_move():
    """Tools/build-warrior-move.py (breathing LIFT, breathe, key_green), loaded once."""
    global _mv
    if _mv is None:
        spec = importlib.util.spec_from_file_location("warrior_move", "Tools/build-warrior-move.py")
        _mv = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(_mv)
    return _mv


def _light(f0):
    r, g, b, al = [f0[..., i] for i in range(4)]
    return (al > 8) & (0.3 * r + 0.59 * g + 0.11 * b >= 90)


def warrior_mask(f0, keep_band=2):
    """The prototype's cut (2026-11-07): light colour components (red hair, fur, skin) reaching row 105 are body, dark outline pixels
    follow their nearest light pixel; keep = non-hair head pixels within keep_band px of the body."""
    r, g, b, al = [f0[..., i] for i in range(4)]
    solid, light = al > 8, _light(f0)
    red = light & (r > g + 60)
    sat = (np.max(f0[..., :3], -1) - np.min(f0[..., :3], -1)) / np.maximum(np.max(f0[..., :3], -1), 1)
    fur = light & ~red & ((g - b <= 8) | (sat < 0.13))
    skin = light & ~red & ~fur
    rows = np.arange(FRAME)[:, None] * np.ones((1, FRAME))
    body_light = np.zeros_like(light)
    for part in (red, fur, skin):
        lab, n = ndimage.label(part)
        for i in range(1, n + 1):
            comp = lab == i
            if rows[comp].max() >= WARRIOR_BODY_ROW:
                body_light |= comp
    head_light = light & ~body_light & (rows <= WARRIOR_TOP_ROW)
    _, (iy, ix) = ndimage.distance_transform_edt(~light, return_indices=True)
    head = solid & (rows <= WARRIOR_TOP_ROW) & np.where(light, head_light, head_light[iy, ix])
    head = core.largest(head)
    tips = solid & (rows <= WARRIOR_HAIR_ROW) & np.where(light, red, red[iy, ix])  # red hair tips between the fur spikes (2026-11-07 fix)
    head |= tips & ndimage.binary_dilation(head, iterations=3)
    head = core.assign_faint(head, al, WARRIOR_TOP_ROW)
    hair = (red & head) | (~light & head & red[iy, ix])
    keep = head & ~hair & (al > 250) & ndimage.binary_dilation(solid & ~head, iterations=keep_band)
    return head, keep


def polyline_mask(f0, cfg, keep_band=2):
    """Solid pixels above the cut line, plus chin-box pixels `chin_head` accepts (dark outlines follow the nearest light pixel)."""
    al = f0[..., 3]
    solid, light = al > 8, _light(f0)
    rows = np.arange(FRAME)[:, None] * np.ones((1, FRAME))
    cols = np.ones((FRAME, 1)) * np.arange(FRAME)[None, :]
    cut = core.polyline(cfg["cut"])[None, :]
    head = solid & (rows < cut)
    x0, x1, y0, y1 = cfg["chin"]
    box = (cols >= x0) & (cols < x1) & (rows >= y0) & (rows < y1)
    ok = cfg["chin_head"](f0) & light
    _, (iy, ix) = ndimage.distance_transform_edt(~light, return_indices=True)
    head |= solid & box & np.where(light, ok, ok[iy, ix])
    head = core.largest(head, extra_min=12)
    limit = int(max(np.max(cut), y1))
    head = core.assign_faint(head, al, limit)
    kx0, kx1 = cfg["keep"]
    # opaque only: a soft pixel drawn twice would darken
    keep = head & (al > 250) & (cols >= kx0) & (cols < kx1) & ~cfg["hair"](f0) & ndimage.binary_dilation(solid & ~head, iterations=keep_band)
    return head, keep


def adopt_specks(f0, head, keep, limit, max_px=24):
    """Small bits of body left beside the head (hair tips between fur spikes, outline crumbs) go with the head: a new hairstyle that does
    not cover them would show them floating. 8-connected so a thin outline still counts as joined to the body."""
    rest = (f0[..., 3] > 0) & ~head
    lab, n = ndimage.label(rest, structure=np.ones((3, 3)))
    if n:
        sizes = ndimage.sum(rest, lab, range(1, n + 1))
        tops = ndimage.minimum(np.arange(FRAME)[:, None] * np.ones((1, FRAME)), lab, range(1, n + 1))
        small = [i + 1 for i in range(n) if sizes[i] <= max_px and tops[i] <= limit]
        head = head | np.isin(lab, small)
    return head, keep


def head_mask(cls, cfg, f0):
    head, keep = warrior_mask(f0, cfg["keep_band"]) if cfg["mask"] == "warrior" else polyline_mask(f0, cfg)
    return adopt_specks(f0, head, keep, 118)


def anchors(cfg, fr):
    """Per-frame head anchors {x, y, sy, angle}: the Warrior's LIFT, or the measured breathing scale about the sole edge."""
    if cfg["breath"] == "lift":
        return [{"x": 0, "y": l, "sy": 1.0, "angle": 0} for l in warrior_move().LIFT]
    scales, edge = core.measure_scale(fr)
    # the head sprite scales about the cell bottom (row edge 192): moving it by (192 - edge)(s - 1) px makes that a scale about the sole edge
    return [{"x": 0, "y": round((FRAME - edge) * (s - 1), 4), "sy": s, "angle": 0} for s in scales]


def headless_rows(cfg, fr, head, keep, anc):
    """Frame 0 with the head erased, breathed into frames 1-3 like the sheet. Lift (Warrior): the whole headless frame is breathed
    (mv.breathe). Scale: the original frame stays wherever the head is not near, the breathed headless frame 0 fills the head's surroundings."""
    f0 = fr[0].copy()
    f0[head & ~keep] = 0
    out = [f0]
    if cfg["breath"] == "lift":
        mv = warrior_move()
        return out + [mv.to_rgba(mv.breathe(core.pm(f0), f)).astype(np.float64) for f in range(1, 4)]
    edge = core.figure_edges(fr[0])[1]
    near0 = ndimage.binary_dilation(head & ~keep, iterations=3).astype(np.float64)
    for f in range(1, 4):
        warp = core.scale_src(anc[f]["sy"], edge)
        moved = core.warp_y(f0, warp, 3)
        near = core.warp_y(np.dstack([near0] * 3 + [near0 * 255]), warp, 1)[..., 3] > 1
        o = fr[f].copy()
        o[near] = moved[near]
        out.append(o)
    return out


def head_cell(f0, head):
    o = f0.copy()
    o[~head] = 0
    return o

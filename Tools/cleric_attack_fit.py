"""Fits ChatGPT's 4-row Cleric attack sheets (rows = facing down, up, left, right) onto 192 px game cells (Tools/build-cleric-attack-body.py).

Per row: the body is scaled so its neck-to-sole length equals idle's of the same facing, then the painted head alone is shrunk to idle's
head width by the Mechanic's smooth warp above the neck (Tools/build-mechanic-attack-down-repaint.py `warp`, rule 8 of the ChatGPT art
rules: ChatGPT draws the chibi head 10-25 % too big). Soles land on idle's sole row, each frame's feet centred on idle's feet centre.
The game then draws the hairstyle head over the headless body (head layer), so the painted head only has to sit where idle's sits.
"""
import importlib.util

import numpy as np
from PIL import Image
from scipy import ndimage

import cleric_attack_compose as cc

FRAME = 192
ROOT = cc.ROOT


def _load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


mr = _load("mr", f"{ROOT}/Tools/build-mechanic-attack-down-repaint.py")  # warp(), raw_to_cell() and key_flat via mr.oi


def split_rows(path, count, rows=4):
    """{dir: [frame crops left to right]} from a sheet of `rows` rows x `count` frames on flat #FF00FF."""
    fig = mr.oi.key_flat(path, magenta=True)
    a = np.asarray(fig)
    al = a[..., 3] > 100
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=5))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    ids = [1 + i for i in np.argsort(sizes)[::-1][:rows * count]]
    boxes = {i: ndimage.find_objects((lab == i).astype(int))[0] for i in ids}
    cy = {i: (boxes[i][0].start + boxes[i][0].stop) / 2 for i in ids}
    order = sorted(ids, key=lambda i: cy[i])
    out = {}
    for r, d in enumerate(("down", "up", "left", "right")[:rows]):
        grp = sorted(order[r * count:(r + 1) * count], key=lambda i: boxes[i][1].start)
        crops = []
        for i in grp:
            part = a.copy()
            part[..., 3] = np.where(lab == i, part[..., 3], 0)
            im = Image.fromarray(part)
            crops.append(im.crop(im.getbbox()))
        out[d] = crops
    return out


def head_width(im):
    """Alpha width of the widest row in the top quarter of the figure (the hair)."""
    al = np.asarray(im)[..., 3] > 100
    ys = np.nonzero(al.any(1))[0]
    top, bot = ys.min(), ys.max()
    return float(max(np.ptp(np.nonzero(al[y])[0]) + 1 for y in range(top, top + int(0.25 * (bot - top + 1))) if al[y].any()))


def feet_x(im):
    al = np.asarray(im)[..., 3] > 100
    ys, xs = np.nonzero(al)
    sel = ys > ys.max() - 10
    return (xs[sel].min() + xs[sel].max()) / 2


def idle_ref(d):
    """Idle frame 0 of facing d with the style 0 head: (head width, hair top, neck row = top of the headless body's centre, sole, feet x)."""
    body = cc.frame("idle", d, 0, pad=0, gear=False)
    al = np.asarray(body)[..., 3] > 100
    ys = np.nonzero(al.any(1))[0]
    hl = np.asarray(Image.open(f"{cc.BODY}/headless/idle.png").convert("RGBA").crop((0, cc.ROWS[d] * FRAME, FRAME, cc.ROWS[d] * FRAME + FRAME)))[..., 3] > 100
    neck = int(np.flatnonzero(hl[:, 80:112].any(1)).min())
    return head_width(body), int(ys.min()), neck, int(ys.max()), feet_x(body)


def fit_row(figs, d, log=print):
    """Placement dicts (k, ox, oy, im, q, neck, cx) per frame, usable with mr.warp / mr.raw_to_cell."""
    hw, itop, ineck, ibot, ifx = idle_ref(d)
    k_head = hw / float(np.median([head_width(f) for f in figs]))
    head_raw = (ineck - itop) / k_head
    tall = sorted(f.height for f in figs)[len(figs) // 2:]  # standing frames only: the hit-4 crouch is shorter on purpose
    k = (ibot - ineck) / float(np.median([h - head_raw for h in tall]))
    q = k_head / k
    out = []
    for fg in figs:
        im = fg.resize((max(1, round(fg.width * k)), max(1, round(fg.height * k))), Image.LANCZOS)
        al = np.asarray(im)[..., 3] > 100
        bot = int(np.nonzero(al.any(1))[0].max())
        ox, oy = int(round(ifx - feet_x(im))), ibot - bot
        top = int(np.nonzero(al.any(1))[0].min())
        hx = np.nonzero(al[top:top + max(3, int(0.2 * im.height))].any(0))[0]
        out.append(dict(k=k, ox=ox, oy=oy, im=im, q=q, neck=oy + head_raw * k, cx=ox + (hx.min() + hx.max()) / 2))
    if abs(q - 1) > 0.01:  # the warp leaves the hair's lower edge less shrunk: correct q once so the built head measures idle's width
        built = [head_width(cell(p)) for p in out]
        q2 = q * hw / float(np.median(built))
        for p in out:
            p["q"] = q2
        q = q2
    log(f"{d}: body scale {k:.3f}, head shrink {q:.3f}")
    return out


def cell(p):
    P = 64  # room for a frame placed partly outside the cell (alpha_composite takes no negative offset)
    c = Image.new("RGBA", (FRAME + 2 * P, FRAME + 2 * P), (0, 0, 0, 0))
    c.alpha_composite(p["im"], (p["ox"] + P, p["oy"] + P))
    c = c.crop((P, P, P + FRAME, P + FRAME))
    return mr.warp(c, p) if abs(p["q"] - 1) > 0.01 else c

"""Wizard hairstyle painted straight onto the game's own bald body (15/11/2026, user: style 1 drawn on the bald-head canvas looked like a
helmet stuck on the head). The canvas sent to ChatGPT is the idle frame 0 of body/bald (down, right, up), head + shoulders cropped (BOX) and
scaled x4 nearest, three panels on one 1536x1024 #00FF00 image; ChatGPT paints gray hair on it. This tool:
  1. aligns each answer panel to its canvas panel (scale + shift that best overlaps the body below the chin, which ChatGPT keeps),
  2. takes as hair the gray pixels that differ from the bald body (or lie on the background), largest pieces only,
  3. maps them into the cell space of heads/hair/<style>/<dir> by inverting that frame's idle hair anchor (Data/.../hair-anchors.json:
     HeroHeadLayer.BaldBody places the cell scaled s about the cell bottom centre, then moved x, y), so every other animation places it with
     its own anchors like any style.
Left = right mirrored (as for every style). Writes heads/hair/<style>/{down,right,up}.png; old files backed up once to on-body/old-<style>/.

  py Tools/build-wizard-hair-on-body-canvas.py canvas                 -> heads/layered/on-body/canvas-idle-bald-down-right-up-x4.png
  py Tools/build-wizard-hair-on-body-canvas.py fit ANSWER.png STYLE   -> on-body/staging/<dir>.png + preview.png (game files untouched)
  py Tools/build-wizard-hair-on-body-canvas.py install STYLE          -> heads/hair/<STYLE>/
"""
import json, os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

ROOT = "D:/zenoniaClone"
RES = f"{ROOT}/Assets/Resources"
BALD = f"{RES}/Sprites/classes/wizard/body/bald/idle.png"
HAIR = f"{RES}/Sprites/classes/wizard/heads/hair"
ANCH = f"{RES}/Data/classes/wizard/hair-anchors.json"
WORK = f"{ROOT}/Assets/ArtLibrary/classes/wizard/heads/layered/on-body"
STAGE = f"{WORK}/staging"
F, K = 192, 4
BOX = (36, 8, 156, 136)                     # frame px crop: head + shoulders
PANELS = {"down": (0, 16), "right": (3, 528), "up": (1, 1040)}   # dir: (sheet row, panel x on the canvas); panel y = 256
PY = 256
PW, PH = (BOX[2] - BOX[0]) * K, (BOX[3] - BOX[1]) * K


def bald_panel(row):
    im = Image.open(BALD).convert("RGBA").crop((BOX[0], row * F + BOX[1], BOX[2], row * F + BOX[3]))
    return np.asarray(im.resize((PW, PH), Image.NEAREST)).astype(np.float32)


def cmd_canvas():
    can = Image.new("RGB", (1536, 1024), (0, 255, 0))
    for d, (row, x) in PANELS.items():
        p = Image.fromarray(bald_panel(row).astype(np.uint8)); bg = Image.new("RGBA", p.size, (0, 255, 0, 255)); bg.alpha_composite(p)
        can.paste(bg.convert("RGB"), (x, PY))
    os.makedirs(WORK, exist_ok=True); can.save(f"{WORK}/canvas-idle-bald-down-right-up-x4.png")


def keyed(path):
    a = np.asarray(Image.open(path).convert("RGB").resize((1536, 1024), Image.LANCZOS)).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    al = np.clip((np.maximum.reduce([r, b, 255 - g]) - 40) / 80.0, 0, 1)
    g = np.minimum(g, np.maximum(r, b) + 10)
    return np.dstack([r, g, b, al * 255])


def warp(img, k, tx, ty, size):
    """img sampled at (k * x + tx, k * y + ty) for output (x, y)."""
    pil = [Image.fromarray(img[..., c]) for c in range(4)]
    out = [np.asarray(p.transform(size, Image.AFFINE, (k, 0, tx, 0, k, ty), Image.BILINEAR)) for p in pil]
    return np.dstack(out)


def align(ans, bald, d):
    """(k, tx, ty): answer panel area sampled so its body below the chin overlaps the bald panel's (coarse grid then fine)."""
    chin = int(PH * 0.62)
    tgt = bald[chin:, :, 3] > 128
    region = ans                                                  # whole answer canvas, panel origin guess at (x, PY)
    x0 = PANELS[d][1]
    best = None
    for k in np.arange(0.9, 1.11, 0.02):
        for ty in range(-40, 41, 4):
            for tx in range(-40, 41, 4):
                w = warp(region, k, x0 + tx, PY + ty + chin * k, (PW, PH - chin))[..., 3] > 128
                iou = (w & tgt).sum() / max(1, (w | tgt).sum())
                if best is None or iou > best[0]:
                    best = (iou, k, tx, ty)
    _, k0, tx0, ty0 = best
    for k in np.arange(k0 - 0.02, k0 + 0.021, 0.005):
        for ty in range(ty0 - 4, ty0 + 5):
            for tx in range(tx0 - 4, tx0 + 5):
                w = warp(region, k, x0 + tx, PY + ty + chin * k, (PW, PH - chin))[..., 3] > 128
                iou = (w & tgt).sum() / max(1, (w | tgt).sum())
                if iou > best[0]:
                    best = (iou, k, tx, ty)
    return best


def hair_mask(p, bald):
    r, g, b, a = (p[..., i] for i in range(4))
    sat = np.maximum.reduce([r, g, b]) - np.minimum.reduce([r, g, b])
    skin = (r > 180) & (r - b > 40)
    diff = np.abs(p[..., :3] - bald[..., :3]).max(-1)
    # inside the bald skull (filled: its pale shine fails the skin test) light gray hair hardly differs from the shine, so there any
    # neutral (sat < 22) non-skin pixel is hair; elsewhere it must differ from the body (or lie on the background)
    br, bg_, bb = bald[..., 0], bald[..., 1], bald[..., 2]
    bsk = (bald[..., 3] > 128) & (br > 180) & (br - bb > 40)
    lab0, n0 = ndi.label(bsk)
    skull = ndi.binary_fill_holes(lab0 == 1 + int(np.argmax(ndi.sum(bsk, lab0, range(1, n0 + 1))))) if n0 else bsk
    skull = ndi.binary_fill_holes(ndi.binary_closing(skull, iterations=6))
    m = (a > 128) & (sat < 40) & ~skin & ((bald[..., 3] < 128) | (diff > 50) | (skull & (sat < 22)))
    m = ndi.binary_opening(m, iterations=1)
    lab, n = ndi.label(m)
    if n:
        sz = ndi.sum(m, lab, range(1, n + 1)); keep = [i + 1 for i in range(n) if sz[i] >= 0.03 * sz.max()]
        m = np.isin(lab, keep)
    m = ndi.binary_fill_holes(m) & (a > 128) & ~skin | m
    return m


def anchor(d):
    e = next(x for x in json.load(open(ANCH, encoding="utf-8"))["anims"] if x["anim"] == "idle" and x["dir"] == d)
    return e["frames"][0]


def to_cell(hair_panel, d):
    """Hair in panel px (x4 frame space from BOX) -> heads/hair cell: frame = S * cell + offset (inverse of the anchor placement)."""
    a = anchor(d); s = a.get("s") or 1.0; S = s * (a.get("sy") or 1.0)
    ox, oy = F / 2 + a["x"] - s * F / 2, F + a["y"] - S * F
    # panel = K * (frame - BOX0) = K * s * cell + K * (offset - BOX0); sample the panel at that point
    pil = [Image.fromarray(hair_panel[..., c]) for c in range(4)]
    co = (K * s, 0, K * (ox - BOX[0]), 0, K * S, K * (oy - BOX[1]))
    cell = np.dstack([np.asarray(p.transform((F, F), Image.AFFINE, co, Image.BILINEAR)) for p in pil])
    cell[..., 3] = np.where(cell[..., 3] >= 110, 255, 0)
    l = cell[..., :3].mean(-1, keepdims=True)                      # neutral gray for HairDye
    cell[..., :3] = np.repeat(l, 3, -1)
    cell[cell[..., 3] == 0] = 0
    return cell


def cmd_fit(answer, style):
    ans = keyed(answer)
    os.makedirs(STAGE, exist_ok=True)
    tiles = []
    for d, (row, _) in PANELS.items():
        bald = bald_panel(row)
        iou, k, tx, ty = align(ans, bald, d)
        p = warp(ans, k, PANELS[d][1] + tx, PY + ty, (PW, PH))
        m = hair_mask(p, bald)
        hp = p.copy(); hp[~m] = 0
        cell = to_cell(hp, d)
        Image.fromarray(cell.astype(np.uint8)).save(f"{STAGE}/{d}.png")
        print(d, "align iou", round(iou, 3), "scale", round(k, 3), "shift", tx, ty, "hair px", int((cell[..., 3] > 0).sum()))
        prev = Image.fromarray(bald.astype(np.uint8)); prev.alpha_composite(Image.fromarray(hp.astype(np.uint8)))
        tiles.append(prev)
    out = Image.new("RGBA", (PW * 3, PH), (58, 62, 70, 255))
    for i, t in enumerate(tiles):
        out.alpha_composite(t, (i * PW, 0))
    out.save(f"{STAGE}/preview.png"); print("preview", f"{STAGE}/preview.png")


def cmd_install(style):
    old = f"{WORK}/old-{style}"
    if not os.path.exists(old):
        shutil.copytree(f"{HAIR}/{style}", old)
    for d in PANELS:
        shutil.copy(f"{STAGE}/{d}.png", f"{HAIR}/{style}/{d}.png")
    print("installed", f"{HAIR}/{style}", "(old in", old + ")")


if __name__ == "__main__":
    c = sys.argv[1]
    if c == "canvas": cmd_canvas()
    elif c == "fit": cmd_fit(sys.argv[2], sys.argv[3])
    elif c == "install": cmd_install(sys.argv[2])
    else: print(__doc__)

"""RO Fantasy heroes: ChatGPT hair strips (3 styles x 5 heads per class) -> dyeable hair cells of the layered renderer.

Source: Assets/ArtLibrary/ro-fantasy-heroes/src/hair-<class>-style<1|2|3>-5dir-chatgpt-N.png (5 heads drawn on the 3x bald-head ref,
newest N). Fit of each head onto the bald body head (skin match, Tools/ro_fantasy_hair_fit.py, the same corrections as the art check
Tools/build-ro-fantasy-hero-art-check.py: one scale from the front head, side heads that lost the face keep the front height, back heads
centred on the skull). Hair = near-neutral greys (ChatGPT's greys drift beige: made exactly neutral and stretched on the HeroRecolor ramp so
every palette colour dyes cleanly); accessories keep their real colour (Assassin headband, Mechanic goggles: saturated pieces touching the
hair). Mechanic goggles go to their own strip (`accSheet`, hidden under any hat, as on the common layered hair); the hole is refilled with
the nearest hair inside the hair silhouette.

Hair cell CELL_W x CELL_H, skull top of the idle head on (AX, AY), skull width scaled to REF_W (cell px of the idle body).
"""
import numpy as np
from PIL import Image
from scipy import ndimage

import ro_fantasy_body_cut as bc
from ro_fantasy_hair_fit import fit

CELL_W, CELL_H, AX, AY, REF_W = 288, 320, 144, 64, 76.0
L_PCT, L_OUT = [0, 3, 50, 97, 100], [0.10, 0.14, 0.60, 0.88, 0.95]  # grey percentiles -> shader ramp knots (outline, base, shine)
ACC_CLASSES = ("assassin", "mechanic")  # only these styles carry coloured accessories
SPLIT_ACC = ("mechanic",)               # accessory in its own strip (hidden under hats)
ac = bc.art_check


def fits(cls, body, hr):
    """Per head i: (scale, fx, fy, span) mapping hair-image px (inside span) to body-source px: body = (fx, fy) + (hair - span origin) * scale."""
    ac.DARK_FACE = cls == "assassin"
    spans = ac.figures(body)
    heads = [ac.head_box(body, s) for s in spans]
    body_skin, body_alpha = ac.face_mask(body), body[..., 3] > 0
    hskin, hspans = ac.face_mask(hr), ac.figures(hr)
    cell = lambda m, hs: m[hs[2]:hs[3], hs[0]:hs[1]]
    sc0 = 1.0 / 3.0 * Image.open(bc.ROOT / f"Assets/ArtLibrary/ro-fantasy-heroes/ref/bald-head-{cls}-5dir.png").width / hr.shape[1]
    out, dys = [], []
    for i, hs in enumerate(hspans):
        rng = sc0 * np.arange(0.90, 1.101, 0.01) if i == 0 else [out[0][0]]
        _, sc, fx, fy = fit(body_skin, body_alpha, heads[i], cell(hskin, hs), cell(hr[..., 3] > 0, hs), rng)
        hb = heads[i]
        hh, hw = hb[3] - hb[2], hb[1] - hb[0]
        cx = np.argwhere(cell(hr[..., 3] > 0, hs))[:, 1].mean() * sc
        if 0 < i < 3 and abs(fy - hb[2] - dys[0]) > 0.12 * hh:
            fy, fx = int(hb[2] + dys[0]), int((hb[0] + hb[1]) / 2 - cx)  # side view lost the face: front-view height
        if i < 3:
            dys.append(fy - hb[2])
        elif abs(fy - hb[2] - np.median(dys)) > 0.06 * hh:
            fy = int(hb[2] + np.median(dys))  # back view drifted (shaved nape)
        if i >= 3:
            fx_c = int((hb[0] + hb[1]) / 2 - cx)
            if i == 4 or abs(fx - fx_c) > 0.1 * hw:
                fx = fx_c
        out.append((sc, fx, fy, hs))
    return out


def layers(cls, hr):
    """(hair RGBA uint8, accessory RGBA uint8 or None) at source resolution: neutral ramp greys, accessories as painted."""
    ac.DARK_FACE = cls == "assassin"
    keep, acc = ac.hair_layer(hr)
    acc &= cls in ACC_CLASSES
    acc &= ~(hr[..., 1] - np.maximum(hr[..., 0], hr[..., 2]) > 0.1)  # key-green specks are no accessory
    lab, n = ndimage.label(keep, structure=np.ones((3, 3), bool))
    if n > 1:  # dark specks around the outline (ChatGPT's broken outline strokes): drop pieces under 80 source px
        sizes = ndimage.sum(keep, lab, range(1, n + 1))
        keep &= np.isin(lab, 1 + np.nonzero(sizes >= 80)[0])
    rgb = hr[..., :3]
    lum = rgb @ np.array([0.299, 0.587, 0.114], np.float32)
    # the dark outline around the hair: ChatGPT paints it warm brown-black, so the grey test misses it (ragged pale edge without it)
    hsat = hr[..., :3].max(-1) - hr[..., :3].min(-1)
    outline = (hr[..., 3] > 0) & (lum < 0.36) & (hsat < 0.22) & ~acc & ~ac.skin_mask(hr)
    keep |= outline & ndimage.binary_dilation(keep, iterations=4)
    if cls == "assassin":  # the black face mask redrawn on every head is a solid dark area (hair outlines are thin): not hair
        mask = ndimage.binary_opening((hr[..., 3] > 0) & (lum < 0.30), iterations=4)
        keep &= ~ndimage.binary_dilation(mask, iterations=3)
    nl = np.interp(lum, np.percentile(lum[keep], L_PCT), L_OUT)
    out = np.zeros(hr.shape, np.float32)
    out[..., :3] = np.where(keep[..., None], nl[..., None], rgb)
    out[..., 3] = (keep | acc).astype(np.float32)
    hair = (out * 255).round().astype(np.uint8)
    if cls not in SPLIT_ACC or not acc.any():
        return hair, None
    acc_img = np.zeros_like(hair)
    acc_img[acc] = hair[acc]
    hair[acc] = 0
    inside = acc & ndimage.binary_fill_holes(ndimage.binary_closing(keep, iterations=8))  # under the goggles: refill with the nearest hair
    if inside.any():
        idx = ndimage.distance_transform_edt(~keep, return_distances=False, return_indices=True)
        hair[inside] = hair[idx[0][inside], idx[1][inside]]
    return hair, acc_img


def cell_of(img, f, xf, anchor):
    """Hair cell (CELL_W x CELL_H RGBA) of one head. img: hair layer at source resolution; f: fit; xf: idle body transform; anchor: (x, top, w)."""
    sc, fx, fy, hs = f
    x0, y0, s, ox, oy = xf
    X, T, w = anchor
    k = w / REF_W
    m = sc * s / k                               # hair-image px -> hair-cell px
    px = ((fx - x0) * s + ox - X) / k + AX       # hair-cell position of the span origin
    py = ((fy - y0) * s + oy - T) / k + AY
    crop = img[hs[2]:hs[3], hs[0]:hs[1]].astype(np.float32)
    pm = np.dstack([crop[..., :3] * crop[..., 3:4] / 255.0, crop[..., 3]]).clip(0, 255).astype(np.uint8)
    im = Image.fromarray(pm).resize((max(1, round(crop.shape[1] * m)), max(1, round(crop.shape[0] * m))), Image.LANCZOS)
    big = Image.new("RGBA", (CELL_W + 2 * im.width, CELL_H + 2 * im.height))
    big.paste(im, (int(round(px)) + im.width, int(round(py)) + im.height))
    r = np.asarray(big.crop((im.width, im.height, im.width + CELL_W, im.height + CELL_H))).astype(np.float32)
    al = r[..., 3:4]
    rgb = np.where(al > 0, r[..., :3] * 255.0 / np.maximum(al, 1), 0)
    out = np.dstack([rgb, al]).clip(0, 255).astype(np.uint8)
    out[out[..., 3] < 6] = 0
    lost = (crop[..., 3] > 0).sum() * m * m - (out[..., 3] > 128).sum()
    return out, lost


def face_cover(c, cell, anchor):
    """Assassin: the black face mask is part of the body. A ChatGPT bob paints it grey on the side views, so it would be dyed like a beard:
    clear hair pixels over the body's dark mask (dark pixels in the lower head band of the idle cell)."""
    X, T, w = anchor
    a = np.asarray(cell).astype(np.float32)
    lum = a[..., :3] @ np.array([0.299, 0.587, 0.114], np.float32)
    dark = (a[..., 3] > 128) & (lum < 85)
    band = np.zeros_like(dark)
    band[int(T + 0.62 * w):int(T + 1.05 * w), max(0, int(X - 0.62 * w)):int(X + 0.62 * w)] = True
    dark = ndimage.binary_opening(dark & band, iterations=2)
    k = w / REF_W
    yy, xx = np.mgrid[0:CELL_H, 0:CELL_W]
    cx = np.clip(np.round(X + (xx - AX) * k).astype(int), 0, cell.width - 1)
    cy = np.clip(np.round(T + (yy - AY) * k).astype(int), 0, cell.height - 1)
    out = c.copy()
    out[dark[cy, cx]] = 0
    return out


def behind_neck(c, cell, anchor, row, chin_scan=False):
    """Front views (down, down-right): hair painted under the chin in the torso column is the back hair the body hides (else long hair
    covers the chest like a beard, sprite-rules 2026-11-07 Cleric). Locks over the shoulders stay."""
    if row > 1:
        return c
    X, T, w = anchor
    sk = bc.sheets_tool.skull_mask(cell)
    chin = min(np.nonzero(sk.any(1))[0][-1], T + 1.02 * w)  # a cream dress can join the skin blob: the head is about 1 skull width tall
    k = w / REF_W
    yy, xx = np.mgrid[0:CELL_H, 0:CELL_W]
    cx, cy = X + (xx - AX) * k, T + (yy - AY) * k
    half = 0.36 * w
    out = c.copy()
    out[(cy > chin + 1) & (np.abs(cx - X) < half)] = 0
    if not chin_scan:  # Assassin: the face-mask hole would cut the bob / braids across the cheek
        return out
    # the skull-width chin can sit ~10 px under the painted chin (Cleric silver long hair, 19/11: a grey bar over the chin): per column of
    # the face (AX +- 0.3 skull), the first hair row under the face hole (long bangs over the eyes are kept); cut from their median
    y_top = int(AY + 0.55 * REF_W)
    al = out[y_top:int(AY + 1.3 * REF_W), :, 3] > 20
    rows = []
    for x in range(int(AX - 0.3 * REF_W), int(AX + 0.3 * REF_W)):
        gap = np.nonzero(~al[:, x])[0]
        if gap.size and al[gap[0]:, x].any():
            rows.append(y_top + gap[0] + int(np.argmax(al[gap[0]:, x])))
    if len(rows) > 0.5 * 0.6 * REF_W:
        y0 = np.median(rows)
        out[(yy >= y0) & (np.abs(cx - X) < half)] = 0
        out[(yy >= y0 - 2) & (np.abs(cx - X) < half + 4) & (out[..., 3] < 160)] = 0  # faint jaw line / cut edges left as a pale frame
    return out


def style_strips(cls, k, idle_cells):
    """(hair strip, accessory strip or None, source name) of style k (1..3), or None when the class has no art for it."""
    path = bc.newest(f"hair-{cls}-style{k}-5dir-chatgpt-*.png")
    if path is None:
        return None
    body = ac.load_rgba(str(bc.newest(f"body-{cls}-idle-5dir-chatgpt-*.png")))
    hr = ac.load_rgba(str(path))
    hair, acc = layers(cls, hr)
    cells, accs = [], []
    for i, (f, (cell, xf)) in enumerate(zip(fits(cls, body, hr), idle_cells)):
        anchor = bc.sheets_tool.head_anchor(cell)
        c, lost = cell_of(hair, f, xf, anchor)
        if cls == "assassin":
            c = face_cover(c, cell, anchor)
        c = behind_neck(c, cell, anchor, i, chin_scan=cls == "cleric")
        al = c[..., 3] > 20
        edge = al[0].any() or al[-1].any() or al[:, 0].any() or al[:, -1].any()
        print(f"{cls} style{k} dir {i}: scale {f[0]:.3f}, hair px {al.sum()}{'  WARNING: touches the cell edge' if edge or lost > 200 else ''}")
        cells.append(c)
        if acc is not None:
            accs.append(cell_of(acc, f, xf, anchor)[0])
    return np.concatenate(cells, 1), (np.concatenate(accs, 1) if accs else None), path.name

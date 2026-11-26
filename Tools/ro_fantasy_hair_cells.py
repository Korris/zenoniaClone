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
# Starward hair V2 (24/11) is painted as masses (shadow + main) for the Ramp5 knots of HeroRecolor (outline 0.12, deep 0.28,
# shadow 0.41, base 0.65, light 0.86). The median stretch above lifted the shadow masses into the base colour (flat hair), so these
# classes map the two painted mass peaks onto the shadow / base knots instead (ends: outline, shine)
PAINTED_ON_RAMP = ("warrior", "cleric", "mechanic")  # cleric, mechanic V3 (26/11): painted on the same ramp hexes as Knight hair V4
# Asteria V3 Assassin (26/11, RO_FANTASY_VARIANT=assassin-v3): no black face mask any more (bare face + red scarf), hair painted in grey
# masses on the Ramp5 knots like the Knight, the bow keeps its painted colour (accessory)
V3_ASSASSIN = bc.variant.VARIANT == "assassin-v3"
if V3_ASSASSIN:
    PAINTED_ON_RAMP += ("assassin",)


def old_face_mask(cls):
    return cls == "assassin" and not V3_ASSASSIN
HIGHLIGHT_PCT = 90  # hair pixels above this percentile are the painted shine strands (-> Ramp5 Light)


def mass_peaks_map(lum):
    """Piecewise-linear grey map: 0.5th pct -> 0.12, dark mass peak -> 0.41, main mass peak -> 0.65, 99.5th pct -> 0.88."""
    h, e = np.histogram(lum, bins=64, range=(0, 1))
    h = np.convolve(h, np.ones(5) / 5, mode="same")
    c = (e[:-1] + e[1:]) / 2
    lo, hi = np.percentile(lum, [0.5, 99.5])
    mid = np.median(lum)
    main = c[np.argmax(np.where(c >= mid, h, 0))]
    # the shadow mass lies well under the main mass: a light hair (Knight V3 style 3, 24/11, main 0.71) has a broad shadow plateau
    # 0.39-0.64 whose top bins beside the main peak won under the median split (shadow 0.65 -> the whole hair mapped dark)
    dark = c[np.argmax(np.where(c < min(mid, main - 0.15), h, 0))]
    main = max(main, dark + 0.05)
    # painted highlight strands (user 24/11: "tóc từ hình gốc suôn mượt mà vào game nhìn chán"): the top HIGHLIGHT_PCT of the hair used to
    # land at 0.66-0.72, under the Ramp5 Light knot 0.86, so the glossy strands showed only Base -> flat hair. Now that percentile -> 0.86.
    shine = max(float(np.percentile(lum, HIGHLIGHT_PCT)), main + 0.04)
    src = [min(lo, dark - 0.02), dark, main, shine, max(hi, shine + 0.02)]
    print(f"  hair greys: outline {src[0]:.2f}, shadow peak {dark:.2f}, main peak {main:.2f}, highlight p{HIGHLIGHT_PCT} {shine:.2f}, top {src[4]:.2f}")
    return src, [0.12, 0.41, 0.65, 0.86, 0.94]
ACC_CLASSES = ("assassin", "mechanic")  # only these styles carry coloured accessories
SPLIT_ACC = ("mechanic",)               # accessory in its own strip (hidden under hats)
ac = bc.art_check


def fits(cls, body, hr, ref_path=None):
    """Per head i: (scale, fx, fy, span) mapping hair-image px (inside span) to body-source px: body = (fx, fy) + (hair - span origin) * scale."""
    ac.DARK_FACE = old_face_mask(cls)
    spans = ac.figures(body)
    heads = [ac.head_box(body, s) for s in spans]
    body_skin, body_alpha = ac.face_mask(body), body[..., 3] > 0
    hskin, hspans = ac.face_mask(hr), ac.figures(hr)
    cell = lambda m, hs: m[hs[2]:hs[3], hs[0]:hs[1]]
    ref_path = ref_path or bc.variant.bald_head_ref(cls)  # the strip this hair was painted on (variant test look: its own)
    sc0 = 1.0 / 3.0 * Image.open(ref_path).width / hr.shape[1]
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
    ac.DARK_FACE = old_face_mask(cls)
    ac.HAIR_MIN_PX = 400 if cls == "mechanic" else None  # V3 ponytail / tufts split off by the strap or tie (art check HAIR_MIN_PX)
    keep, acc = ac.hair_layer(hr)
    acc &= cls in ACC_CLASSES
    acc &= ~(hr[..., 1] - np.maximum(hr[..., 0], hr[..., 2]) > 0.1)  # key-green specks are no accessory
    if V3_ASSASSIN:  # small gold clips / earrings of the V3 styles (Tools/ro_fantasy_assassin_v3_hair.py)
        from ro_fantasy_assassin_v3_hair import small_accessories
        acc |= small_accessories(hr, keep, ac.skin_mask(hr))
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
    if old_face_mask(cls):  # the black face mask redrawn on every head is a solid dark area (hair outlines are thin): not hair
        mask = ndimage.binary_opening((hr[..., 3] > 0) & (lum < 0.30), iterations=4)
        keep &= ~ndimage.binary_dilation(mask, iterations=3)
    if cls in PAINTED_ON_RAMP:
        src, dst = mass_peaks_map(lum[keep])
        nl = np.clip(np.interp(lum, src, dst, left=None, right=None), L_OUT[0], L_OUT[-1])
        top = lum > src[-1]  # keep the painted shine above the 99.5th pct (np.interp clamps it)
        nl[top] = np.clip(dst[-1] + (lum[top] - src[-1]), 0, L_OUT[-1])
    else:
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
    # RO_FANTASY_HAIR_FROM="3=golden-paladin" (24/11, Knight V3 armour + Golden Paladin hair test): style k read from src/<variant>/
    # (its own bald-head ref), fitted on the current body by skin match like any hair source
    import os
    for item in filter(None, os.environ.get("RO_FANTASY_HAIR_FROM", "").split(",")):
        kk, var = item.split("=")
        if int(kk) == k:
            files = sorted((bc.variant.SRC_MAIN / var).glob(f"hair-{cls}-style{k}-5dir-chatgpt-*.png"),
                           key=lambda q: int(q.stem.rsplit("-", 1)[1]))
            path = files[-1]
            body = ac.load_rgba(str(bc.newest(f"body-{cls}-idle-5dir-chatgpt-*.png")))
            hr = ac.load_rgba(str(path))
            ref = bc.variant.ART / "ref" / f"{var}-test" / f"bald-head-{cls}-5dir.png"
            return _strips_from(cls, k, idle_cells, path, body, hr, ref)
    if path is None:
        return None
    body = ac.load_rgba(str(bc.newest(f"body-{cls}-idle-5dir-chatgpt-*.png")))
    hr = ac.load_rgba(str(path))
    return _strips_from(cls, k, idle_cells, path, body, hr, bc.variant.bald_head_ref(cls, path))


# Golden Paladin hair (24/11, user: "tóc vàng óng như hình kia" = nana.png, ~60 % pale cream main mass + gloss): after the ramp map
# its masses sat at shadow 0.32-0.45 / main 0.61 / gloss only 0.69 -> read dark. Lift: shadow mass toward Base, main just over Base,
# the top ~15 % (>= 0.69) onto the Light knot 0.86 (HeroRecolor Ramp5: shadow 0.41, base 0.65, light 0.86)
GOLDEN_LIFT = ([0.0, 0.12, 0.41, 0.62, 0.69, 0.94, 1.0], [0.0, 0.12, 0.52, 0.70, 0.86, 0.95, 1.0])


def _strips_from(cls, k, idle_cells, path, body, hr, ref):
    hair, acc = layers(cls, hr)
    if "golden-paladin" in str(path):
        m = hair[..., 3] > 0
        g = hair[..., 0].astype(np.float32) / 255.0
        g2 = np.interp(g, *GOLDEN_LIFT)
        neutral = m & (np.abs(hair[..., 0].astype(int) - hair[..., 2].astype(int)) < 6)
        for c in range(3):
            hair[..., c] = np.where(neutral, (g2 * 255).round().astype(np.uint8), hair[..., c])
    cells, accs = [], []
    for i, (f, (cell, xf)) in enumerate(zip(fits(cls, body, hr, ref), idle_cells)):
        anchor = bc.sheets_tool.head_anchor(cell)
        c, lost = cell_of(hair, f, xf, anchor)
        if old_face_mask(cls):
            c = face_cover(c, cell, anchor)
        c = behind_neck(c, cell, anchor, i, chin_scan=cls == "cleric")
        if "golden-paladin" in str(path):
            c = seal_gaps(c, cell, anchor)
        al = c[..., 3] > 20
        edge = al[0].any() or al[-1].any() or al[:, 0].any() or al[:, -1].any()
        print(f"{cls} style{k} dir {i}: scale {f[0]:.3f}, hair px {al.sum()}{'  WARNING: touches the cell edge' if edge or lost > 200 else ''}")
        cells.append(c)
        if acc is not None:
            accs.append(cell_of(acc, f, xf, anchor)[0])
    strip = np.concatenate(cells, 1)
    if cls in PAINTED_ON_RAMP:
        strip = clean_strip(strip, soften="golden-paladin" in str(path), keep_colour=cls in ACC_CLASSES)
    return strip, (np.concatenate(accs, 1) if accs else None), path.name


def seal_gaps(c, cell, anchor, r=4):
    """Hair from another body (Golden hair on the Knight head, 24/11): where the source ear / jaw was (skin, left out of the hair) the
    Knight head is smaller -> background showed through a slit between hair and face. Pixels empty in both the hair cell and the body
    (mapped into the hair cell) but inside a closing of their union take the nearest hair grey, opaque."""
    X, T, w = anchor
    k = w / REF_W
    yy, xx = np.mgrid[0:CELL_H, 0:CELL_W]
    cx = np.clip(np.round(X + (xx - AX) * k).astype(int), 0, cell.width - 1)
    cy = np.clip(np.round(T + (yy - AY) * k).astype(int), 0, cell.height - 1)
    body = (np.asarray(cell)[..., 3] > 128)[cy, cx]
    hair = c[..., 3] > 128
    gap = ndimage.binary_closing(hair | body, iterations=r) & ~hair & ~body
    gap &= ndimage.binary_dilation(hair, iterations=r)
    if not gap.any():
        return c
    out = c.copy()
    idx = ndimage.distance_transform_edt(~hair, return_distances=False, return_indices=True)
    out[gap, :3] = c[idx[0][gap], idx[1][gap], :3]
    out[gap, 3] = 255
    return out


def clean_strip(strip, soften=False, keep_colour=False):
    """Warrior hair cells (24/11, user: "tóc dơ dơ, vỡ nét"): every opaque pixel exactly neutral grey (the shader recolours only
    chroma < 0.08-0.14); `soften`: 1-2 px dark inner strand lines (away from the silhouette) pulled 35 % toward the local median so
    they stay lines at 3x shrink instead of noisy dark dots; then edge bleed: transparent texels take the nearest opaque grey, so
    bilinear sampling at the alpha edge no longer mixes in black (dark fringe -> outline brown halo)."""
    out = strip.copy()
    op = out[..., 3] > 0
    lum = (out[..., :3].astype(np.float32) @ np.array([0.299, 0.587, 0.114], np.float32))
    if soften:
        solid = ndimage.binary_erosion(out[..., 3] > 200, iterations=2)
        med = ndimage.median_filter(lum, size=5)
        thin = solid & (lum < med - 25) & (lum < 110)
        lum = np.where(thin, lum + 0.35 * (med - lum), lum)
    g = np.clip(lum.round(), 0, 255).astype(np.uint8)
    neutral = op
    if keep_colour:  # accessories (V3 Assassin bow + clasp) keep their painted colour; only near-grey hair is made exactly neutral
        ch = out[..., :3].max(-1).astype(int) - out[..., :3].min(-1).astype(int)
        neutral = op & (ch < 24)
    for c in range(3):
        out[..., c] = np.where(neutral, g, out[..., c])
    if (~op).any() and op.any():
        idx = ndimage.distance_transform_edt(~op, return_distances=False, return_indices=True)
        for c in range(3):
            out[..., c] = np.where(op, out[..., c], out[..., c][idx[0], idx[1]])
    return out

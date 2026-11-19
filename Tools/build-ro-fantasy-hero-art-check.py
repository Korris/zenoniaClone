"""RO Fantasy heroes, art batch A: bald-head reference + check sheet.

Sources (ChatGPT, flat #00FF00 background, 5 figures/heads in one row:
down, down-right, right, up-right, up):
  Assets/ArtLibrary/ro-fantasy-heroes/src/body-<class>-idle-5dir-chatgpt-N.png
  Assets/ArtLibrary/ro-fantasy-heroes/src/hair-<class>-style<k>-5dir-chatgpt-N.png

Commands:
  heads <class>   -> ref/bald-head-<class>-5dir.png (5 bald heads, x3, on #00FF00)
  check <class>   -> docs/reference/ro-fantasy-heroes/check-<class>.png
                     row 0 body, then per hair style: dyed brown #A8693F, purple #B49CE2
The newest -chatgpt-N of each source is used.
"""
import glob, re, sys
import numpy as np
from PIL import Image
from scipy import ndimage

from ro_fantasy_hair_fit import fit

ROOT = 'D:/zenoniaClone/'
ART = ROOT + 'Assets/ArtLibrary/ro-fantasy-heroes/'
OUT = ROOT + 'docs/reference/ro-fantasy-heroes/'
GREEN = (0, 255, 0)
DYES = [(0xA8, 0x69, 0x3F), (0xB4, 0x9C, 0xE2)]
# accessory colours kept as painted (headband red, goggle brass, lens blue)
SAT_KEEP = 0.28
DARK_FACE = False  # set for Assassin (black face mask is a shared face feature, not hair)


def newest(pattern):
    files = glob.glob(pattern)
    if not files:
        return None
    return max(files, key=lambda f: int(re.search(r'chatgpt-(\d+)', f).group(1)))


def load_rgba(path):
    """Key out the green background -> RGBA float array (0..1)."""
    a = np.asarray(Image.open(path).convert('RGB')).astype(np.float32) / 255
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    green = (g > 0.55) & (g - np.maximum(r, b) > 0.30)
    alpha = (~green).astype(np.float32)
    # ChatGPT sometimes letterboxes the green strip with white bands: white touching the border = background
    white = a.min(-1) > 0.93
    wl, _ = ndimage.label(white)
    edge_ids = set(np.unique(np.r_[wl[0], wl[-1], wl[:, 0], wl[:, -1]])) - {0}
    alpha[np.isin(wl, list(edge_ids))] = 0
    alpha = ndimage.binary_opening(alpha > 0, iterations=1)
    # green irises (Mechanic, Cleric) get keyed: refill small enclosed holes that are not pure #00FF00
    holes = ndimage.binary_fill_holes(alpha) & ~alpha
    lab, n = ndimage.label(holes)
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        m = lab[sl] == i
        px = a[sl][m]
        if m.sum() < 400 and np.abs(px - [0, 1, 0]).sum(1).mean() > 0.35:
            alpha[sl] |= m
    alpha = alpha.astype(np.float32)
    # despill: soften green fringe on edges
    edge = ndimage.binary_dilation(alpha == 0, iterations=2) & (alpha > 0)
    g2 = np.where(edge, np.minimum(g, np.maximum(r, b)), g)
    return np.dstack([r, g2, b, alpha])


def figures(rgba, n=5):
    """Return n column spans (x0, x1, y0, y1) of the figures, left to right."""
    lab, cnt = ndimage.label(rgba[..., 3] > 0)
    objs = ndimage.find_objects(lab)
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, cnt + 1))
    big = [objs[i] for i in np.argsort(-sizes)[:n * 3] if sizes[i] > 1500]
    # merge pieces that overlap in x into one figure
    spans = sorted([[s[1].start, s[1].stop, s[0].start, s[0].stop] for s in big])
    merged = []
    for s in spans:
        if merged and s[0] < merged[-1][1] - 4:
            m = merged[-1]
            merged[-1] = [m[0], max(m[1], s[1]), min(m[2], s[2]), max(m[3], s[3])]
        else:
            merged.append(s)
    merged.sort(key=lambda s: -(s[1] - s[0]) * (s[3] - s[2]))
    return sorted(merged[:n])


def head_box(rgba, span):
    """Skull box inside a figure: top to the neck (narrowest row under the widest head row)."""
    x0, x1, y0, y1 = span
    al = rgba[y0:y1, x0:x1, 3] > 0
    w = al.sum(1)
    h = y1 - y0
    top_half = w[: int(h * 0.36)]
    hm = int(np.argmax(top_half))
    lo, hi = hm + 5, int(h * 0.62)
    neck = lo + int(np.argmin(w[lo:hi]))
    # skin blob touching the skull top reaches the chin (narrowest row can sit at the mouth)
    sk = skin_mask(rgba[y0:y1, x0:x1])
    lab, _ = ndimage.label(sk)
    top_ids = set(lab[: max(8, hm)].ravel()) - {0}
    if top_ids:
        rows = np.where(np.isin(lab, list(top_ids)).any(1))[0]
        neck = max(neck, min(int(rows[-1]) + 4, hi))
    # bare shoulders can join the face skin blob: chin sits < 0.6 skull width under the widest row
    neck = min(neck, hm + int(0.6 * w[hm]))
    cols = np.where(al[:neck].any(0))[0]
    return x0 + cols[0], x0 + cols[-1] + 1, y0, y0 + neck


def skin_mask(rgba):
    import colorsys  # noqa: F401  (kept simple: vectorised HSV below)
    r, g, b, a = [rgba[..., i] for i in range(4)]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    return (a > 0) & (r > g) & (g > b) & (r > 0.75) & (s > 0.15) & (s < 0.45) & ((r - g) < 0.25)


def cmd_heads(cls):
    src = newest(ART + f'src/body-{cls}-idle-5dir-chatgpt-*.png')
    rgba = load_rgba(src)
    boxes = [head_box(rgba, s) for s in figures(rgba)]
    cw = int(max(b[1] - b[0] for b in boxes) * 1.6)
    ch = int(max(b[3] - b[2] for b in boxes) * 1.5)
    out = Image.new('RGB', (cw * 5, ch), GREEN)
    for i, (x0, x1, y0, y1) in enumerate(boxes):
        part = (rgba[y0:y1, x0:x1] * 255).astype(np.uint8)
        im = Image.fromarray(part, 'RGBA')
        out.paste(im, (i * cw + (cw - (x1 - x0)) // 2, ch - (y1 - y0) - int(ch * 0.08)), im)
    out = out.resize((out.width * 3, out.height * 3), Image.NEAREST)
    path = ART + f'ref/bald-head-{cls}-5dir.png'
    out.save(path)
    print(src, '->', path, out.size)


def face_mask(rgba):
    """Skin plus thick dark areas (Assassin face mask): the parts both images share."""
    if not DARK_FACE:
        return skin_mask(rgba)
    lum = rgba[..., :3].mean(-1)
    sat = rgba[..., :3].max(-1) - rgba[..., :3].min(-1)
    # mask: very dark grey (< 0.29), or dark purple on the body; dark grey hair shadow (neutral, ~0.33) is not
    dark = (rgba[..., 3] > 0) & ((lum < 0.29) | ((lum < 0.42) & (sat > 0.1)))
    return skin_mask(rgba) | ndimage.binary_opening(dark, iterations=4)


def hair_layer(rgba):
    """Grey hair + saturated accessories; skin, eyes and face removed."""
    r, g, b, a = [rgba[..., i] for i in range(4)]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    # near-neutral (ChatGPT greys drift warm/beige); peach skin has r - b > 0.18, eyes are coloured
    grey = (a > 0) & (s < 0.30) & (np.abs(r - b) < 0.13) & (np.abs(g - (r + b) / 2) < 0.08)
    lab, cnt = ndimage.label(ndimage.binary_closing(grey, iterations=2))
    sizes = ndimage.sum(grey, lab, range(1, cnt + 1))
    keep = np.isin(lab, 1 + np.where(sizes > 0.15 * sizes.max())[0]) & grey  # drop eye whites
    keep &= ~ndimage.binary_dilation(face_mask(rgba) & ~skin_mask(rgba), iterations=2)
    near = ndimage.binary_dilation(keep, iterations=6)
    cand = (a > 0) & (s > SAT_KEEP) & ~skin_mask(rgba) & ~keep
    lab, cnt = ndimage.label(cand)
    sizes = ndimage.sum(cand, lab, range(1, cnt + 1))
    touch = ndimage.maximum(near, lab, range(1, cnt + 1))
    # headband / goggles: big pieces touching hair; eyes and collar bits are small or far
    ids = 1 + np.where((sizes > 3000) & (touch > 0))[0] if cnt else []
    return keep, np.isin(lab, ids) & cand


def dye(rgba, keep, acc, col):
    out = np.zeros_like(rgba)
    lum = rgba[..., :3].mean(-1, keepdims=True)
    # normalise hair greys: 10th/50th/90th percentile -> #555555 / #999999 / #E0E0E0 (silver drawn too light)
    q = np.percentile(lum[keep], [10, 50, 90])
    lum = np.interp(lum, [0, q[0], q[1], q[2], 1], [0, 0.333, 0.6, 0.878, 1])
    tint = np.clip(np.array(col) / 255 * lum / 0.6, 0, 1)
    out[..., :3] = np.where(keep[..., None], tint, rgba[..., :3])
    out[..., 3] = (keep | acc).astype(np.float32)
    return out


def to_im(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), 'RGBA')


def cmd_check(cls):
    global DARK_FACE
    DARK_FACE = cls == 'assassin'
    body = load_rgba(newest(ART + f'src/body-{cls}-idle-5dir-chatgpt-*.png'))
    spans = figures(body)
    heads = [head_box(body, s) for s in spans]
    body_skin = face_mask(body)
    cell_w = max(s[1] - s[0] for s in spans) + 30
    cell_h = max(s[3] - s[2] for s in spans) + 60
    styles = [newest(ART + f'src/hair-{cls}-style{k}-5dir-chatgpt-*.png') for k in (1, 2, 3)]
    styles = [s for s in styles if s]
    rows = 1 + 2 * len(styles)
    sheet = Image.new('RGBA', (cell_w * 5, cell_h * rows), (60, 64, 70, 255))
    base_y = cell_h - 20
    for i, s in enumerate(spans):
        sheet.alpha_composite(to_im(body[s[2]:s[3], s[0]:s[1]]),
                              (i * cell_w + 15, base_y - (s[3] - s[2])))
    for k, path in enumerate(styles):
        hr = load_rgba(path)
        keep, acc = hair_layer(hr)
        acc &= cls in ('assassin', 'mechanic')  # only these styles carry coloured accessories
        hskin = face_mask(hr)
        hspans = figures(hr)
        body_alpha = body[..., 3] > 0
        cell = lambda m, hs: m[hs[2]:hs[3], hs[0]:hs[1]]
        # ChatGPT keeps the strip's horizontal extent: ref (body x3) width / hair width = body px per hair px
        sc0 = Image.open(ART + f'ref/bald-head-{cls}-5dir.png').width / 3 / hr.shape[1]
        fitted, dys = [], []
        for i, hs in enumerate(hspans):
            sc_range = sc0 * np.arange(0.90, 1.101, 0.01) if i == 0 else [fitted[0]]  # one scale: front head
            f = fit(body_skin, body_alpha, heads[i], cell(hskin, hs), cell(hr[..., 3] > 0, hs), sc_range)
            fitted.append(f[1])
            _, sc, fx, fy = f
            if 0 < i < 3 and abs(fy - heads[i][2] - dys[0]) > 0.12 * (heads[i][3] - heads[i][2]):
                fy = int(heads[i][2] + dys[0])  # side view lost the face: keep the front-view height
                cx = np.argwhere(cell(hr[..., 3] > 0, hs))[:, 1].mean() * sc
                fx = int((heads[i][0] + heads[i][1]) / 2 - cx)
            if i < 3:
                dys.append(fy - heads[i][2])
            elif abs(fy - heads[i][2] - np.median(dys)) > 0.06 * (heads[i][3] - heads[i][2]):
                fy = int(heads[i][2] + np.median(dys))  # back view drifted (shaved nape): use face-view height
            if i >= 3:  # back views: centre the hair silhouette on the skull when the fit drifts
                cx = np.argwhere(cell(hr[..., 3] > 0, hs))[:, 1].mean() * sc
                fx_c = int((heads[i][0] + heads[i][1]) / 2 - cx)
                if i == 4 or abs(fx - fx_c) > 0.1 * (heads[i][1] - heads[i][0]):
                    fx = fx_c
            cols = np.where(cell(keep, hs).any(0))[0]
            ratio = (cols[-1] - cols[0] + 1) * sc / (heads[i][1] - heads[i][0]) if len(cols) else 0
            print(f'style{k + 1} head{i}: scale {sc:.3f} at {fx},{fy} hair/skull width {ratio:.2f}')
            for d, col in enumerate(DYES):
                im = to_im(dye(hr, keep, acc, col)[hs[2]:hs[3], hs[0]:hs[1]])
                im = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.LANCZOS)
                s = spans[i]
                row = 1 + 2 * k + d
                ox = i * cell_w + 15 - s[0]
                oy = row * cell_h + base_y - s[3]
                sheet.alpha_composite(to_im(body[s[2]:s[3], s[0]:s[1]]), (ox + s[0], oy + s[2]))
                sheet.alpha_composite(im, (int(ox + fx), int(oy + fy)))
    path = OUT + f'check-{cls}.png'
    sheet.convert('RGB').save(path)
    print('->', path, sheet.size, 'styles:', len(styles))


if __name__ == '__main__':
    {'heads': cmd_heads, 'check': cmd_check}[sys.argv[1]](sys.argv[2])

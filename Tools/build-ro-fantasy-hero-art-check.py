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

ROOT = 'D:/zenoniaClone/'
ART = ROOT + 'Assets/ArtLibrary/ro-fantasy-heroes/'
OUT = ROOT + 'docs/reference/ro-fantasy-heroes/'
GREEN = (0, 255, 0)
DYES = [(0xA8, 0x69, 0x3F), (0xB4, 0x9C, 0xE2)]
# accessory colours kept as painted (headband red, goggle brass, lens blue)
SAT_KEEP = 0.28


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
    alpha = ndimage.binary_opening(alpha > 0, iterations=1).astype(np.float32)
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
    return (a > 0) & (r > g) & (g > b) & (r > 0.75) & (s > 0.10) & (s < 0.45) & ((r - g) < 0.25)


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


def hair_layer(rgba):
    """Grey hair + saturated accessories; skin, eyes and face removed."""
    r, g, b, a = [rgba[..., i] for i in range(4)]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    grey = (a > 0) & (s < 0.12)
    lab, cnt = ndimage.label(ndimage.binary_closing(grey, iterations=2))
    sizes = ndimage.sum(grey, lab, range(1, cnt + 1))
    keep = np.isin(lab, 1 + np.where(sizes > 0.15 * sizes.max())[0]) & grey  # drop eye whites
    near = ndimage.binary_dilation(keep, iterations=6)
    acc = (a > 0) & (s > SAT_KEEP) & near & ~skin_mask(rgba)
    return keep, acc & ~keep


def dye(rgba, keep, acc, col):
    out = np.zeros_like(rgba)
    lum = rgba[..., :3].mean(-1, keepdims=True)
    tint = np.clip(np.array(col) / 255 * lum / 0.6, 0, 1)
    out[..., :3] = np.where(keep[..., None], tint, rgba[..., :3])
    out[..., 3] = (keep | acc).astype(np.float32)
    return out


def to_im(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), 'RGBA')


def cmd_check(cls):
    body = load_rgba(newest(ART + f'src/body-{cls}-idle-5dir-chatgpt-*.png'))
    spans = figures(body)
    heads = [head_box(body, s) for s in spans]
    body_skin = skin_mask(body)
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
        hskin = skin_mask(hr)
        for i, hs in enumerate(figures(hr)):
            bx0, bx1, by0, by1 = heads[i]
            bsk = np.argwhere(body_skin[by0:by1, bx0:bx1])
            hsk = np.argwhere(hskin[hs[2]:hs[3], hs[0]:hs[1]])
            if len(bsk) < 20 or len(hsk) < 20:
                continue
            # scale = skin width ratio, align skin box centre-x and bottom
            sc = (np.ptp(bsk[:, 1]) + 1) / (np.ptp(hsk[:, 1]) + 1)
            for d, col in enumerate(DYES):
                layer = dye(hr, keep, acc, col)[hs[2]:hs[3], hs[0]:hs[1]]
                im = to_im(layer)
                im = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.LANCZOS)
                hx = (hsk[:, 1].min() + hsk[:, 1].max()) / 2 * sc
                hy = hsk[:, 0].max() * sc
                bxc = bx0 + (bsk[:, 1].min() + bsk[:, 1].max()) / 2
                byb = by0 + bsk[:, 0].max()
                s = spans[i]
                row = 1 + 2 * k + d
                ox = i * cell_w + 15 - s[0]
                oy = row * cell_h + base_y - s[3]
                sheet.alpha_composite(to_im(body[s[2]:s[3], s[0]:s[1]]),
                                      (i * cell_w + 15, row * cell_h + base_y - (s[3] - s[2])))
                sheet.alpha_composite(im, (int(ox + bxc - hx), int(oy + byb - hy)))
    path = OUT + f'check-{cls}.png'
    sheet.convert('RGB').save(path)
    print('->', path, sheet.size, 'styles:', len(styles))


if __name__ == '__main__':
    {'heads': cmd_heads, 'check': cmd_check}[sys.argv[1]](sys.argv[2])

"""Normalize the cleric (assets/hd/cleric) into 192px frames (same recipe as tools/build-warrior-v2-sheets.py).

  raw/*.png            armed (holy staff)  -> normalized-hd/             = the baked cleric sprite set
  weaponless/raw/*.png staff removed       -> weaponless/normalized-hd/ aligned per frame onto the armed body
  raw/idle.png row0/col0 head              -> normalized/hero_face.png  (HUD portrait)
The aligned set skips `death` (armed death has no staff; overlays are hidden then).
Usage: python tools/build-cleric-sheets.py
"""
import json

import cv2
import numpy as np
from PIL import Image

from hero_set_align import FRAME, ROOT, align_sheet, compare_sheet, load_builder

CL = ROOT / 'assets' / 'hd' / 'cleric'
COUNTS = {'idle': 4, 'walk': 4, 'attack1': 4, 'attack2': 4, 'attack3': 6, 'dash': 3, 'hurt': 2, 'death': 5}
FACE = 152


def regroup_by_component(builder):
    """Wrap builder.split_sheet: sprites cross the cell lines (a staff tip pokes into the next cell), so the
    snapped grid cuts them. Instead label each row band's (dilated) connected pieces and give every piece to
    the nearest frame centre; pieces < 3% of the frame's biggest are neighbour crumbs and are dropped."""
    original = builder.split_sheet
    kernel = np.ones((3, 3), np.uint8)

    def split(img, cols):
        cells = original(img, cols)
        arr = builder.normalize_alpha(img)
        row_bounds = builder.grid_lines(arr[..., 3] > 0, 0, 4)
        out = []
        for r, row in enumerate(cells):
            y0, y1 = row_bounds[r], row_bounds[r + 1]
            band = arr[y0:y1]
            mask = (band[..., 3] > 0).astype(np.uint8)
            n, lab = cv2.connectedComponents(cv2.dilate(mask, kernel), connectivity=8)
            xs = np.arange(mask.shape[1])[None, :]
            area, cx = np.zeros(n), np.zeros(n)
            for i in range(1, n):
                m = (lab == i) & (mask > 0)
                area[i] = m.sum()
                cx[i] = (m * xs).sum() / max(area[i], 1)
            centres = []  # per frame: x centre of the biggest piece in its snapped cell
            for c, cell in enumerate(row):
                cm = cell[..., 3] > 0
                centres.append(float(np.nonzero(cm.any(axis=0))[0].mean()) if cm.any() else 0.0)
            # snapped cells all start at their own x offset: recompute centres in band coordinates
            col_bounds = builder.grid_lines(mask > 0, 1, cols)
            centres = [col_bounds[c] + centres[c] for c in range(cols)]
            owner = {i: int(np.argmin([abs(cx[i] - m) for m in centres])) for i in range(1, n) if area[i] > 0}
            boxes = {}
            for i in owner:
                ys_i, xs_i = np.nonzero((lab == i) & (mask > 0))
                boxes[i] = (xs_i.min(), ys_i.min(), xs_i.max(), ys_i.max())
            mains = {}  # frame -> its biggest piece; every smaller piece joins the frame with the nearest main bbox
            for i, o in owner.items():
                if o not in mains or area[i] > area[mains[o]]:
                    mains[o] = i

            def gap(a, b):
                dx = max(a[0] - b[2], b[0] - a[2], 0)
                dy = max(a[1] - b[3], b[1] - a[3], 0)
                return dx * dx + dy * dy

            for i in owner:
                if i not in mains.values():
                    owner[i] = min(mains, key=lambda o: gap(boxes[i], boxes[mains[o]]))
            spacing = float(np.mean(np.diff(centres))) if cols > 1 else mask.shape[1]
            split_at = {}  # merged piece (two frames touching) -> x cuts at the emptiest column between centres
            for i in owner:
                cols_i = np.nonzero(((lab == i) & (mask > 0)).any(axis=0))[0]
                if cols_i.max() - cols_i.min() < 1.4 * spacing:
                    continue
                prof = ((lab == i) & (mask > 0)).sum(axis=0)
                cuts = []
                for j, (a, b) in enumerate(zip(centres, centres[1:])):  # cut j sits between frames j and j+1
                    if cols_i.min() < a and cols_i.max() > b:
                        lo, hi = int(a + 0.55 * spacing), int(b - 0.25 * spacing)
                        cuts.append((j, lo + int(np.argmin(prof[lo:hi + 1]))))
                if cuts:
                    split_at[i] = cuts
            cells_r = []
            for c in range(cols):
                mine = [i for i, o in owner.items() if o == c and i not in split_at]
                big = max((area[i] for i in mine), default=0)
                keep = [i for i in mine if area[i] >= 0.03 * big]
                sel = np.isin(lab, keep) & (mask > 0)
                for i, cuts in split_at.items():  # frame c = the slab of a merged piece between its neighbouring cuts
                    lo = max([x for j, x in cuts if j == c - 1], default=0 if c <= cuts[0][0] else None)
                    hi = min([x for j, x in cuts if j == c], default=mask.shape[1] if c >= cuts[-1][0] else None)
                    if lo is None or hi is None or c < cuts[0][0] or c > cuts[-1][0] + 1:
                        continue
                    slab = np.zeros(mask.shape, bool)
                    slab[:, lo:hi] = True
                    sel |= (lab == i) & (mask > 0) & slab
                cell = band.copy()
                cell[~sel] = 0
                cells_r.append(cell)
            out.append(cells_r)
        return out

    builder.split_sheet = split


def build_armed(builder):
    out = CL / 'normalized-hd'
    out.mkdir(exist_ok=True)
    builder.FRAME_COUNTS.update(COUNTS)
    sheets, records = {}, {}
    for anim, cols in COUNTS.items():  # dict order: idle first => head-scale reference
        sheet, record = builder.build_sheet(anim, cols)
        arr = np.array(sheet)
        arr[..., 3][arr[..., 3] <= 48] = 0
        sheet = Image.fromarray(arr, 'RGBA')
        sheet.save(out / f'{anim}.png')
        sheets[anim], records[anim] = sheet, record
    failures = builder.validate(sheets)
    if failures:
        print('VALIDATE:\n' + '\n'.join(failures))
    (out / 'scale.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    return sheets, records


def build_weaponless(builder, armed, records):
    out = CL / 'weaponless' / 'normalized-hd'
    out.mkdir(parents=True, exist_ok=True)
    pairs, scales = {}, {}
    for anim, cols in COUNTS.items():
        if anim == 'death':
            continue
        sheet, scale = align_sheet(builder, CL / 'weaponless' / 'raw' / f'{anim}.png', armed[anim], cols, records[anim]['scale'])
        if sheet is None:
            print(f'WARN weaponless/{anim} missing')
            continue
        sheet.save(out / f'{anim}.png')
        pairs[anim], scales[anim] = (armed[anim], sheet), {'scale': scale, 'frame': FRAME}
    (out / 'scale.json').write_text(json.dumps(scales, indent=2), encoding='utf-8')
    compare_sheet(pairs, out / 'compare-contact-sheet.png', 'weaponless')


def build_face(builder):
    """Head of the idle front frame: widest span of the top rows, squared from the top, resized to FACE px."""
    cells = builder.split_sheet(Image.open(CL / 'raw' / 'idle.png').convert('RGBA'), COUNTS['idle'])
    body = builder.tight_bbox(cells[0][0])
    h = body.shape[0]
    cols = np.nonzero((body[: int(h * 0.30), :, 3] > 0).any(axis=0))[0]
    x0, x1 = int(cols.min()), int(cols.max()) + 1
    side = min(int((x1 - x0) * 1.08), h)
    cx = (x0 + x1) // 2
    left = max(0, min(body.shape[1] - side, cx - side // 2))
    crop = Image.fromarray(body[:side, left:left + side], 'RGBA')
    small = builder.downscale(crop, (FACE - 4) / side)
    face = Image.new('RGBA', (FACE, FACE), (0, 0, 0, 0))
    face.paste(small, ((FACE - small.width) // 2, (FACE - small.height) // 2), small)
    out = CL / 'normalized'
    out.mkdir(exist_ok=True)
    face.save(out / 'hero_face.png')


def main():
    builder = load_builder(CL / 'raw', CL / 'normalized-hd')
    regroup_by_component(builder)
    armed, records = build_armed(builder)
    build_weaponless(builder, armed, records)
    build_face(builder)
    print('wrote', CL)


if __name__ == '__main__':
    main()

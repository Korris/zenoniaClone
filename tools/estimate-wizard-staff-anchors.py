"""Estimate per-frame staff grip/angle/scale/layer for the wizard from baked vs weaponless frames.

Per displayed frame (anim x dir x index; attack1/attack2 'left' = mirrored 'right' row, like the runtime):
  mask = baked opaque AND NOT (weaponless opaque, dilated)  -> the held staff (+ some outline drift)
  PCA axis of the best elongated blob group; head end = bluer / wider end; grip = hand-covered point on the
  axis (axis samples inside the weaponless body) else GRIP_RATIO along the visible staff from the head.
Anchor: x,y grip in the 192px frame; angle deg (0 = staff head up, clockwise +); s = frame px per overlay px;
behind = body drawn over the staff; hidden = no staff (death). Manual fixes: tools/assets/wizard-staff-anchor-overrides.json
{"attack2.left.1": {"angle": 40, "behind": false}}. Outputs src/data/wizard-staff-anchors-generated.js + anchors.json
Usage: python tools/estimate-wizard-staff-anchors.py
"""
import json
import math
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

from wizard_staff_preview import write_preview

ROOT = Path(__file__).resolve().parents[1]
WIZ = ROOT / 'assets' / 'hd' / 'wizard'
BAKED, BARE = WIZ / 'normalized-hd', WIZ / 'weaponless' / 'normalized-hd'
OVERRIDES = ROOT / 'tools' / 'assets' / 'wizard-staff-anchor-overrides.json'
OUT_JS = ROOT / 'src' / 'data' / 'wizard-staff-anchors-generated.js'
OUT_JSON = WIZ / 'weaponless' / 'anchors.json'
FRAME, OVERLAY_H = 192, 263.0
COUNTS = {'idle': 4, 'walk': 4, 'attack1': 4, 'attack2': 4, 'attack3': 6, 'dash': 3, 'hurt': 2, 'death': 5}
DIRS = ['down', 'up', 'left', 'right']
ROW = {'down': 0, 'up': 1, 'left': 2, 'right': 3}
MIRRORED_LEFT = {'attack1', 'attack2'}  # runtime flips the right row for these (asset-atlas-hd-wizard.js)
GRIP_RATIO = 0.55  # grip distance from the staff head as a fraction of staff length (overlay grip y 145 of 263)
MIN_MASK_PX = 70
HIDDEN_ANIMS = {'death'}  # staff dropped / hidden


def frame_of(sheet, anim, direction, idx):
    row = ROW['right'] if (direction == 'left' and anim in MIRRORED_LEFT) else ROW[direction]
    img = sheet.crop((idx * FRAME, row * FRAME, (idx + 1) * FRAME, (row + 1) * FRAME))
    return img.transpose(Image.FLIP_LEFT_RIGHT) if (direction == 'left' and anim in MIRRORED_LEFT) else img


REF_LEN = 146.0  # held staff length in the idle frame (px @192), measured from the baked crystal tip to the butt
SHAFT_W, HEAD_W, HEAD_H = 5, 18, 40  # synthetic staff silhouette: shaft width, crystal head width/height
MIN_SCORE = 60.0  # explained staff pixels required, else the frame has no visible staff


def template(length, angle):
    """Synthetic staff silhouette (shaft + head blob) tilted `angle` deg (clockwise, 0 = head up).
    Returns (float32 mask cropped to its bbox, head-tip offset inside it)."""
    size = int(length * 2 + 40)
    tip = np.array([size / 2, size / 2])
    up = np.array([math.sin(math.radians(angle)), -math.cos(math.radians(angle))])  # head direction from grip
    base = tip - up * length  # head tip is at `tip`, butt at `base`
    canvas = np.zeros((size, size), np.uint8)
    cv2.line(canvas, tuple(np.round(base).astype(int)), tuple(np.round(tip).astype(int)), 1, SHAFT_W)
    center = tuple(np.round(tip - up * HEAD_H / 2).astype(int))
    cv2.ellipse(canvas, center, (HEAD_W // 2, HEAD_H // 2), math.degrees(math.atan2(up[1], up[0])) - 90 + 180, 0, 360, 1, -1)
    ys, xs = np.nonzero(canvas)
    x0, y0 = xs.min(), ys.min()
    return canvas[y0:ys.max() + 1, x0:xs.max() + 1].astype(np.float32), tip - np.array([x0, y0])


def weights(baked, bare):
    """Staff-candidate pixels +1 (gold/brown shaft & frame) / +0.4 (other opaque, e.g. crystal; baked spell VFX
    is mostly translucent blue so alpha>=235 plus the warm-colour bonus keeps it from steering the fit);
    -1 outside the baked silhouette; 0 over the body."""
    bk = np.asarray(baked).astype(int)
    b, w = bk[..., 3] > 128, np.asarray(bare)[..., 3] > 48
    body = cv2.dilate(w.astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))) > 0
    warm = bk[..., 0] > bk[..., 2] - 5
    cand = b & ~body & (bk[..., 3] >= 235)
    out = np.where(b, 0.0, -1.0)
    out = np.where(cand, np.where(warm, 1.0, 0.4), out)
    return out.astype(np.float32), cand


def best_fit(wt, angles, lengths):
    pad = int(max(lengths) * 1.3)
    padded = cv2.copyMakeBorder(wt, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=-1.0)
    best = (-1e9, None)
    for length in lengths:
        for angle in angles:
            t, off = template(length, angle)
            res = cv2.matchTemplate(padded, t, cv2.TM_CCORR)
            _, mx, _, loc = cv2.minMaxLoc(res)
            # tiny preference for longer staffs so a short template cannot hide in a sliver
            score = mx + 0.35 * length
            if score > best[0]:
                tip = np.array([loc[0] + off[0] - pad, loc[1] + off[1] - pad])
                best = (score, (tip, angle, length, mx))
    return best


def estimate(baked, bare):
    wt, cand = weights(baked, bare)
    _, (tip, angle, length, mx) = best_fit(wt, np.arange(-180, 180, 12), [REF_LEN * 0.7, REF_LEN * 0.85, REF_LEN])
    _, (tip, angle, length, mx) = best_fit_near(wt, angle, length)
    if mx < MIN_SCORE:
        return None
    up = np.array([math.sin(math.radians(angle)), -math.cos(math.radians(angle))])
    grip = tip - up * GRIP_RATIO * length
    wa = np.asarray(bare)[..., 3] > 48
    gx, gy = int(round(grip[0])), int(round(grip[1]))
    near = wa[max(0, gy - 6):gy + 7, max(0, gx - 6):gx + 7].any() if 0 <= gx < FRAME and 0 <= gy < FRAME else False
    return {'x': round(float(grip[0]), 1), 'y': round(float(grip[1]), 1), 'angle': round(float(angle), 1),
            'len': round(float(length), 1), 'hand': bool(near), 'behind': False, 'score': round(float(mx))}


def best_fit_near(wt, angle, length):
    """Refine around the coarse fit: finer angle, lengths between ~0.4..1.05 of the reference."""
    lengths = sorted({max(REF_LEN * 0.6, min(REF_LEN * 1.05, length * f)) for f in (0.85, 1.0, 1.15)})
    return best_fit(wt, np.arange(angle - 8, angle + 9, 2), lengths)


def fill_hidden(row, anim):
    """Frames where the baked art hides the staff completely (walk swing) would make it blink: reuse the
    nearest visible frame's anchor, drawn behind the body, so the staff stays in the hand."""
    if anim in HIDDEN_ANIMS or all(e.get('hidden') for e in row):
        return row
    out = []
    for i, e in enumerate(row):
        if e.get('hidden') and not e.get('forceHidden'):
            near = min((j for j, o in enumerate(row) if not o.get('hidden')), key=lambda j: min(abs(j - i), len(row) - abs(j - i)))
            e = {**row[near], 'behind': True, 'filled': True}
        out.append(e)
    return out


def carried_walk(data, sheets):
    """The baked walk hides the staff in most frames and flips it in others, so a fitted walk would flicker. Carry it
    like idle instead: idle's median grip/angle/scale per direction, shifted by how far the weaponless body's upper
    half moved relative to idle frame 0 (so the staff bobs and sways with the body)."""
    def centroid(img):
        a = np.asarray(img)[:130, :, 3] > 48
        ys, xs = np.nonzero(a)
        return np.array([xs.mean(), ys.mean()])
    for d in DIRS:
        idle = [e for e in data['idle'][d] if 'x' in e]
        ref = centroid(frame_of(sheets['idle'], 'idle', d, 0))
        base = {k: float(np.median([e[k] for e in idle])) for k in ('x', 'y', 'angle', 's')}
        for i in range(COUNTS['walk']):
            shift = centroid(frame_of(sheets['walk'], 'walk', d, i)) - ref
            data['walk'][d][i] = {'x': round(base['x'] + float(shift[0]), 1), 'y': round(base['y'] + float(shift[1]), 1),
                                  'angle': round(base['angle'], 1), 's': round(base['s'], 3), 'behind': False, 'carried': True}


def main():
    overrides = json.loads(OVERRIDES.read_text()) if OVERRIDES.exists() else {}
    data, low, bare_sheets = {}, [], {}
    for anim, cols in COUNTS.items():
        baked_sheet = Image.open(BAKED / f'{anim}.png').convert('RGBA')
        bare_sheet = Image.open(BARE / f'{anim}.png').convert('RGBA')
        bare_sheets[anim] = bare_sheet
        data[anim] = {}
        for d in DIRS:
            row = []
            for i in range(cols):
                e = {'hidden': True} if anim in HIDDEN_ANIMS else estimate(frame_of(baked_sheet, anim, d, i), frame_of(bare_sheet, anim, d, i))
                if e is None:
                    e = {'hidden': True}
                    low.append(f'{anim}.{d}.{i}: no staff mask')
                e.update(overrides.get(f'{anim}.{d}.{i}', {}))
                row.append(e)
            data[anim][d] = fill_hidden(row, anim)
    lens = [e['len'] for a in data.values() for r in a.values() for e in r if 'len' in e]
    ref = float(np.median(lens))
    low += [f'{a}.{d}.{i} weak score {e["score"]}' for a, ds in data.items() for d, r in ds.items() for i, e in enumerate(r) if e.get('score', 999) < 90]
    for anim in data.values():
        for r in anim.values():
            for e in r:
                if 'len' in e:  # visible length is foreshortened/occluded: clamp around the typical length
                    e['s'] = round(e['len'] / OVERLAY_H, 3)
    carried_walk(data, bare_sheets)  # after 's' exists on the fitted idle entries
    OUT_JSON.write_text(json.dumps(data, indent=1))
    body = json.dumps(data, separators=(',', ':'))
    OUT_JS.write_text('// GENERATED by tools/estimate-wizard-staff-anchors.py -- do not edit by hand (use tools/assets/wizard-staff-anchor-overrides.json).\n'
                      '// Staff grip anchors per displayed wizard frame: x,y px in the 192px frame, angle deg (0 = staff head up, clockwise +),\n'
                      '// s = frame px per overlay px, behind = body drawn over staff, hidden = no staff.\n'
                      f'export const WIZARD_STAFF_ANCHORS = Object.freeze({body});\n', encoding='utf-8')
    print(f'ref staff length {ref:.1f}px (s={ref / OVERLAY_H:.3f}); {len(lens)} frames with staff; wrote {OUT_JS.name}')
    for line in low[:60]:
        print('  low:', line)
    write_preview(data, COUNTS, DIRS, frame_of)


if __name__ == '__main__':
    main()

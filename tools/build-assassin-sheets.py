"""Normalize the assassin (assets/hd/assassin) into 192px frames (recipe of tools/build-cleric-sheets.py).

  raw/{idle,attack1,attack2,attack3,death}.png  armed (two daggers)  -> normalized-hd/  (death = weaponless copy)
  weaponless/raw/{idle,attack1,attack2,attack3} -> weaponless/normalized-hd/ aligned per frame onto the armed body
  weaponless/raw/{walk,dash,hurt}               -> weaponless/normalized-hd/ head-scale matched to the weaponless idle, then
                                                   brought to the armed idle scale (there are NO armed walk/dash/hurt sheets)
  normalized-hd/{walk,dash,hurt}.png            = copies of the weaponless ones (the baked set has no daggers there; the
                                                   renderer always draws weaponless body + dagger overlays)
  raw/idle.png row0/col0 head -> normalized/hero_face.png (HUD portrait)
Usage: python tools/build-assassin-sheets.py
"""
import importlib.util
import json
import shutil
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

from assassin_speck_clean import clean_specks
from hero_set_align import FRAME, ROOT, align_sheet, compare_sheet, load_builder

AS = ROOT / 'assets' / 'hd' / 'assassin'
ARMED = {'idle': 4, 'attack1': 4, 'attack2': 4, 'attack3': 6, 'death': 5}
WEAPONLESS_ALIGNED = ['idle', 'attack1', 'attack2', 'attack3']
WEAPONLESS_FREE = {'walk': 4, 'dash': 3, 'hurt': 2}
COUNTS = {**ARMED, **WEAPONLESS_FREE}

spec = importlib.util.spec_from_file_location('cleric_sheets', Path(__file__).resolve().parent / 'build-cleric-sheets.py')
cleric = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleric)
cleric.CL, cleric.COUNTS = AS, COUNTS  # reuse regroup_by_component + build_face on the assassin folder


BODY_SCALED = {'attack2', 'attack3', 'death'}  # head matching fails on these (front-row heads tilt / zoom): row0/col0 stance height instead


def use_body_height_for(builder, anims):
    """builder.compute_scale_for_sheet, but `anims` are scaled so their first (standing) front frame has the idle body height."""
    original = builder.compute_scale_for_sheet

    def compute(anim, cells):
        if anim not in anims:
            return original(anim, cells)
        return builder.TARGET_BODY_HEIGHT / float(builder.tight_bbox(cells[0][0]).shape[0]), None

    builder.compute_scale_for_sheet = compute


def drop_row_intruders(builder):
    """Wrap builder.split_sheet: a pose taller than its row band leaves a tuft of the neighbouring row's head touching the
    band's top/bottom edge; as a loose piece it would drag the frame's bbox (feet pivot) - drop small edge-touching pieces."""
    original = builder.split_sheet

    def split(img, cols):
        cells = original(img, cols)
        for row in cells:
            for cell in row:
                mask = (cell[..., 3] > 0).astype(np.uint8)
                n, lab, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
                if n < 3:
                    continue
                biggest = stats[1:, 4].max()
                for i in range(1, n):
                    x, y, w, h, area = stats[i]
                    if area < 0.10 * biggest and (y == 0 or y + h == mask.shape[0]):
                        cell[lab == i] = 0
        return cells

    builder.split_sheet = split


def clean_alpha(sheet):
    arr = np.array(sheet)
    arr[..., 3][arr[..., 3] <= 48] = 0
    return Image.fromarray(arr, 'RGBA')


def build_free(builder, anim, cols, correction):
    """Weaponless sheet without an armed twin: head-matched scale x `correction`, feet on the bottom row."""
    cells = builder.split_sheet(Image.open(AS / 'weaponless' / 'raw' / f'{anim}.png').convert('RGBA'), cols)
    scale, rel = builder.compute_scale_for_sheet(anim, cells)
    scale *= correction
    sheet = Image.new('RGBA', (cols * FRAME, 4 * FRAME), (0, 0, 0, 0))
    for r in range(4):
        for c in range(cols):
            crop = builder.tight_bbox(cells[r][c])
            if crop is None:
                print(f'WARN {anim} {r},{c} empty')
                continue
            sheet.paste(builder.place_in_frame(builder.downscale(Image.fromarray(crop, 'RGBA'), scale)), (c * FRAME, r * FRAME))
    print(f'  {anim}: scale {scale:.4f} (head x{rel})')
    return clean_alpha(sheet), scale


def clean_keyed_raw(path):
    """armed attack3 came back with a baked checkerboard (key-baked-checkerboard.py): strip the white crumbs left on the hair.
    The original is kept next to it as <name>-uncleaned.png and always used as the source, so reruns are idempotent."""
    backup = path.with_name(path.stem + '-uncleaned.png')
    if not backup.exists():
        shutil.copyfile(path, backup)
    clean_specks(Image.open(backup)).save(path)


def main():
    clean_keyed_raw(AS / 'raw' / 'attack3.png')
    clean_keyed_raw(AS / 'weaponless' / 'raw' / 'attack3.png')
    out = AS / 'normalized-hd'
    wout = AS / 'weaponless' / 'normalized-hd'
    out.mkdir(exist_ok=True)
    wout.mkdir(parents=True, exist_ok=True)
    builder = load_builder(AS / 'raw', out)
    cleric.regroup_by_component(builder)
    drop_row_intruders(builder)
    builder.FRAME_COUNTS.update(COUNTS)
    use_body_height_for(builder, BODY_SCALED)
    armed, records = {}, {}
    for anim, cols in ARMED.items():  # idle first = head-scale reference
        sheet, records[anim] = builder.build_sheet(anim, cols)
        armed[anim] = clean_alpha(sheet)
        armed[anim].save(out / f'{anim}.png')
    (out / 'scale.json').write_text(json.dumps(records, indent=2), encoding='utf-8')

    wb = load_builder(AS / 'weaponless' / 'raw', wout)
    cleric.regroup_by_component(wb)
    drop_row_intruders(wb)
    wb.FRAME_COUNTS.update(COUNTS)
    weaponless, wscales, pairs = {}, {}, {}
    for anim in WEAPONLESS_ALIGNED:
        sheet, scale = align_sheet(wb, AS / 'weaponless' / 'raw' / f'{anim}.png', armed[anim], ARMED[anim], records[anim]['scale'])
        weaponless[anim], wscales[anim] = sheet, {'scale': scale, 'frame': FRAME}
        pairs[anim] = (armed[anim], sheet)
    own_idle = wb.HEAD_REF['scale'] if wb.HEAD_REF['scale'] else None
    if own_idle is None:  # align_sheet does not run compute_scale_for_sheet: take the idle reference ourselves
        cells = wb.split_sheet(Image.open(AS / 'weaponless' / 'raw' / 'idle.png').convert('RGBA'), COUNTS['idle'])
        wb.compute_scale_for_sheet('idle', cells)
        own_idle = wb.HEAD_REF['scale']
    correction = wscales['idle']['scale'] / own_idle
    print(f'weaponless idle: own scale {own_idle:.4f} -> aligned {wscales["idle"]["scale"]:.4f} (x{correction:.3f})')
    for anim, cols in WEAPONLESS_FREE.items():
        weaponless[anim], scale = build_free(wb, anim, cols, correction)
        wscales[anim] = {'scale': scale, 'frame': FRAME}
    for anim, sheet in weaponless.items():
        sheet.save(wout / f'{anim}.png')
    for anim in WEAPONLESS_FREE:  # baked set = bare body where no armed art exists
        shutil.copyfile(wout / f'{anim}.png', out / f'{anim}.png')
        records[anim] = wscales[anim]
    (out / 'scale.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    (wout / 'scale.json').write_text(json.dumps(wscales, indent=2), encoding='utf-8')
    compare_sheet(pairs, wout / 'compare-contact-sheet.png', 'weaponless')
    (AS / 'normalized').mkdir(exist_ok=True)
    cleric.build_face(builder)
    print('wrote', AS)


if __name__ == '__main__':
    main()

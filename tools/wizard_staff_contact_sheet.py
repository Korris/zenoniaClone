"""Contact sheet for tools/cut-wizard-staff-overlays.py: grip crosshairs + staffs beside the wizard."""
from PIL import Image, ImageDraw

# Measured: the staff the wizard holds in idle frame 0 spans ~(32,53)->(68,177) of the 192px frame = ~129px.
STAFF_GAME_PX = 128
FRAME, ZOOM = 192, 2
HAND_Y = 122  # wizard hand height in the 192px idle frame 0 (measured)
INK = '#f0e3bf'


def crosshair(d, x, y, r):
    d.line([x - r, y, x + r, y], fill='#ff2bd6', width=2)
    d.line([x, y - r, x, y + r], fill='#ff2bd6', width=2)


def build(root, out_dir, manifest, images, staff_len):
    """manifest: list of entries; images[(i, tag)] = RGBA image; staff_len = base staff length in px."""
    cw, ch, bh = 260, 300, FRAME * ZOOM + 20
    cell_w = FRAME * ZOOM + 340
    top = ch * 4 + 40
    sheet = Image.new('RGB', (max(cw * 5, cell_w * 3), top + bh * 4), '#1c3143')
    d = ImageDraw.Draw(sheet)
    for i, e in enumerate(manifest):
        for j, tag in enumerate(('base', 'enhanced')):
            ox, oy = (i % 5) * cw, ((0 if i < 5 else 2) + j) * ch
            im = images[(i, tag)]
            px, py = ox + (cw - im.width) // 2, oy + 20
            sheet.paste(im, (px, py), im)
            crosshair(d, px + e[tag]['gripX'], py + e[tag]['gripY'], 9)
            d.text((ox + 6, oy + 4), f'{i + 1} {e["name"]} {tag} grip({e[tag]["gripX"]},{e[tag]["gripY"]})', fill=INK)
    wiz = Image.open(root / 'assets/hd/wizard/normalized-hd/idle.png').convert('RGBA').crop((0, 0, FRAME, FRAME))
    wiz = wiz.resize((FRAME * ZOOM, FRAME * ZOOM), Image.Resampling.NEAREST)
    d.text((6, top - 18), f'In-game scale x{ZOOM}: wizard {FRAME}px frame, staff length={STAFF_GAME_PX}px (measured from held staff); '
           'wizard | base | enhanced, grip at hand height (crosshair)', fill=INK)
    s = STAFF_GAME_PX * ZOOM / staff_len
    for i, e in enumerate(manifest):
        ox, oy = (i % 3) * cell_w, top + (i // 3) * bh
        sheet.paste(wiz, (ox, oy + 20), wiz)
        d.text((ox + 4, oy + 4), f'{i + 1} {e["name"]}', fill=INK)
        for j, tag in enumerate(('base', 'enhanced')):
            im = images[(i, tag)]
            im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.Resampling.LANCZOS)
            hx, hy = ox + FRAME * ZOOM + 60 + j * 150, oy + 20 + HAND_Y * ZOOM
            sheet.paste(im, (hx - round(e[tag]['gripX'] * s), hy - round(e[tag]['gripY'] * s)), im)
            crosshair(d, hx, hy, 6)
    sheet.save(out_dir / 'contact-sheet.png')

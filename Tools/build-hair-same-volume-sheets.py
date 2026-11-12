"""Hairstyles of one class redrawn to ONE head size and ONE hair volume (2026-11-12, after the character creator of Ragnarok: Midgard
Senki: one shared bald head + face, every hairstyle fits the same skull, long hair is only longer). ChatGPT paints the hair straight
onto a canvas of bald heads (3 rows = 3 hairstyles, columns down | right | up), so the hair is placed by the CANVAS coordinates plus a
small per-head shift (head_shift): the face matching of Tools/build-wizard-hair-layers.py fit slid the back view down and its neck
band cut the long locks. Classes: CLASSES below (wizard, mechanic).

Inputs  Assets/ArtLibrary/classes/<class>/heads/layered/
          src/<bald template>.png        3 bald heads on #00FF00 (down | right | up), drawn by ChatGPT from the head of the class
          ref/.../canvas-3-rows-bald-heads.png   the canvas sent to ChatGPT: the bald template scaled, 3 rows of 512 px (`canvas`)
          src/<answer sheets>.png        the hair painted on the canvas (CLASSES[...]["sheets"])
Output  Sprites/classes/<class>/heads/face/<dir>.png      the shared bald head (`face`; the Wizard one comes from build-wizard-hair-layers.py)
        Sprites/classes/<class>/heads/hair/<n>/<dir>.png  gray hair, one layer per style, 192 cells in the frame space of heads/face;
                                                          HeroHairLayers draws face + hair, HairDye colours the gray (hair-dyes.json)

  py Tools/build-hair-same-volume-sheets.py <class> face [OUT.png]   fit the bald template onto heads/0 (eyes); no OUT = write heads/face
  py Tools/build-hair-same-volume-sheets.py <class> canvas           write the canvas for ChatGPT
  py Tools/build-hair-same-volume-sheets.py <class> fit OUT.png      preview on the bald heads + head-with-hair sizes, no game files
  py Tools/build-hair-same-volume-sheets.py <class> install          write heads/hair, drop hair-under / hair-whole
Wizard then: py Tools/build-wizard-hair-on-bald-body.py check OUT_DIR 0,1,2,3,4,5
"""
import importlib.util, os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
CLASSES = {
    "wizard": {
        "bald": "wizard-bald-template-northwind-original-face-chatgpt-1.png",
        "canvas": "ref/v3-same-volume/canvas-3-rows-bald-heads.png",
        # hair above the brow line stretched upward per facing: ChatGPT drew the hair as a thin cap, so the head read as FLAT seen from
        # the front, where the face with both ears is wide (user, 2026-11-12). 1.3 = a round crown; 1.5 was a tower
        "crown_lift": {"down": 1.3, "right": 1.15, "up": 1.15},
        "sheets": {  # answer sheet -> hairstyle of each row
            "wizard-hair-v3-same-volume-styles-0-1-2-chatgpt-1.png": (0, 1, 2),
            "wizard-hair-v3-same-volume-styles-3-4-5-chatgpt-1.png": (3, 4, 5),
        },
    },
    "mechanic": {
        "bald": "mechanic-bald-template-chatgpt-1.png",
        "ref_eyes_below": 0.7,  # heads/0 is cut under the eyes and wears cyan goggle lenses higher up
        # ChatGPT repainted these heads bigger than the canvas heads (skin overlap fell to 0.5, shifts hit the search limit): every row
        # is brought back to the canvas by its eyes (distance = scale, centre = position) before the canvas placement
        "align": "eyes",
        "grow": 1.0,           # the hair is painted big and covers the skull: no enlarging
        # hair + goggles = whatever differs from the bald head: the colour test of hair_only read the brass buckle and rims as skin
        "extract": "difference",
        "canvas": "ref/canvas-3-rows-bald-heads.png",
        "sheets": {
            "mechanic-hair-same-volume-styles-0-1-2-chatgpt-1.png": (0, 1, 2),
            "mechanic-hair-same-volume-styles-3-4-5-chatgpt-1.png": (3, 4, 5),
        },
    },
    "warrior": {
        "bald": "warrior-bald-template-chatgpt-2.png",
        "canvas": "ref/canvas-3-rows-bald-heads.png",
        "sheets": {
            "warrior-hair-same-volume-styles-0-1-2-chatgpt-1.png": (0, 1, 2),
            "warrior-hair-same-volume-styles-3-4-5-chatgpt-1.png": (3, 4, 5),
        },
        "align": "face",        # rows brought back to the canvas by the face (ear to ear, chin), see row_to_canvas
        "grow": 1.0,
        "extract": "difference",
        "eyes": "red",          # red irises: not usable as anchors (ears, blush, mouth are red too)
        "ref_style": 2,         # the old head with a free forehead: the bald head is fitted to its face (ear to ear, chin)
        "face_fit": "skin",     # bald face fitted ear to ear + chin (see face())
        # style 0 = the gray copy of the ORIGINAL Warrior hair the user approved on 2026-11-11 (chat "Ve dau troc Warrior": "Toc rat
        # tot", then the nape fix): one row of 3 heads (front, right, back) on a blank-faced male head. It replaces row 1 of the first
        # canvas sheet, whose redraw the user rejected (smaller, less wild, forehead bare).
        "row_sheets": {"warrior-hair-gray-approved-nape-chatgpt-1.png": 0},
        "old_face": 2,          # heads/face = bald skull + the original face as on heads/2 (swept-up hair, free forehead)
        "row_sheet_gain": 0.84, # the approved gray hair is painted light: dyed, it came out salmon instead of the original deep red
        "row_sheet_offset": {"down": (0, 13), "right": (0, 13), "up": (0, 11)},   # cell px, so the fringe tips reach the eyes as on heads/0
    },
}
LAYERED = HEADS = CANVAS = BALD = None
EYES, REF_STYLE, FACE_FIT = "blue", 0, "eyes"
SHEETS, CROWN_LIFT, ALIGN, EXTRACT = {}, {}, "skin", "gray"


def use(cls):
    global LAYERED, HEADS, CANVAS, BALD, SHEETS, CROWN_LIFT, ALIGN, HAIR_GROW, EXTRACT, EYES, REF_STYLE, REF_EYES_BELOW, FACE_FIT
    cfg = CLASSES[cls]
    globals()["CFG"] = cfg
    LAYERED = f"{ROOT}/Assets/ArtLibrary/classes/{cls}/heads/layered"
    HEADS = f"{ROOT}/Assets/Resources/Sprites/classes/{cls}/heads"
    CANVAS, BALD, SHEETS = f"{LAYERED}/{cfg['canvas']}", f"{LAYERED}/src/{cfg['bald']}", cfg["sheets"]
    CROWN_LIFT = cfg.get("crown_lift", {})
    ALIGN, HAIR_GROW = cfg.get("align", "skin"), cfg.get("grow", HAIR_GROW)
    EXTRACT = cfg.get("extract", "gray")
    EYES, REF_STYLE, REF_EYES_BELOW = cfg.get("eyes", "blue"), cfg.get("ref_style", 0), cfg.get("ref_eyes_below", REF_EYES_BELOW)
    FACE_FIT = cfg.get("face_fit", "eyes")


DIRS = ["down", "right", "up"]
F, ROW_H, COL_W = 192, 512, 341
HAIR_GROW = 1.12   # the fitted hair is enlarged about the skull centre: ChatGPT drew the hair no bigger than the bald skull, whose rim
                   # then showed between the spikes (and the painted skulls of the body frames are up to 5 % bigger than heads/face)

_spec = importlib.util.spec_from_file_location("hair_layers", f"{ROOT}/Tools/build-wizard-hair-layers.py")
hl = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(hl)


def fit_faces():
    """The bald head cells the hair is fitted to and separated from: the canvas head (LAYERED/fit-bald, written by `face`), not
    heads/face when that carries the original face of the class (its eyes differ from the eyes ChatGPT repaints on the sheets)."""
    return {d: hl.rgba(f"{LAYERED}/fit-bald/{d}.png" if os.path.exists(f"{LAYERED}/fit-bald/{d}.png") else f"{HEADS}/face/{d}.png")
            for d in DIRS}


def bbox(mask):
    ys, xs = np.nonzero(mask)
    return xs.min(), xs.max(), ys.min(), ys.max()


def placement():
    """Per facing (scale, dx, dy): canvas row pixel (x, y) -> cell pixel (x * scale + dx, y * scale + dy), from the bald head of the
    canvas and the installed heads/face cell (the same picture at two sizes). One scale for the three facings."""
    can = hl.key_green(hl.rgba(CANVAS))[:ROW_H]
    faces = fit_faces()
    cb = {d: bbox(can[:, i * COL_W:(i + 1) * COL_W, 3] > 0) for i, d in enumerate(DIRS)}
    fb = {d: bbox(faces[d][..., 3] > 40) for d in DIRS}
    scale = float(np.mean([(fb[d][1] - fb[d][0] + 1) / (cb[d][1] - cb[d][0] + 1) for d in DIRS]))
    out = {}
    for i, d in enumerate(DIRS):
        cx, cy = (cb[d][0] + cb[d][1]) / 2 + i * COL_W, (cb[d][2] + cb[d][3]) / 2
        out[d] = (scale, (fb[d][0] + fb[d][1]) / 2 - cx * scale, (fb[d][2] + fb[d][3]) / 2 - cy * scale)
    return out, faces, can


def head_shift(sheet_col, canvas_col, reach_x=14, reach_y=50, dy_known=None):
    """(dx, dy) in canvas px that moves the sheet's head onto the canvas head. ChatGPT keeps the layout but repaints a row up to ~35 px
    higher and a head a few px aside. Matched on the skin still showing in the lower half of the head (jaw, chin, ears, nape): the best
    overlap of the two skin masks there, at half resolution.
    dy_known: the back view shows only the ears (overlap ~0.08, the vertical match is a guess): it takes the row's front-view dy."""
    a, b = hl.skin(canvas_col)[::2, ::2], hl.skin(sheet_col)[::2, ::2]
    ys = np.nonzero(a)[0]; lo = (ys.min() + ys.max()) // 2
    a = a.copy(); a[:lo] = False
    best = (-1.0, 0, 0)
    for dy in ([round(dy_known / 2)] if dy_known is not None else range(-reach_y // 2, reach_y // 2 + 1)):
        for dx in range(-reach_x // 2, reach_x // 2 + 1):
            m = np.roll(np.roll(b, dy, 0), dx, 1); m[:lo] = False
            iou = (m & a).sum() / max(1, (m | a).sum())
            if iou > best[0]:
                best = (iou, dx, dy)
    return best[1] * 2.0, best[2] * 2.0, best[0]


CROWN_RIM, CROWN_PART = 8, 0.45   # px band inside the skull outline, over the top part of the skull height


def crown_covered(hair, face):
    """Hair on the whole crown: where a spiky style leaves the scalp bare along the top outline of the skull, the bald skull shows as a
    bald patch (worst on the body frames, whose painted skull is a little bigger and tilted). Fill the band just inside the skull's
    top outline with the nearest hair colour (hair body, not outline). The forehead lies deeper inside the outline and stays skin."""
    skull = ndi.binary_fill_holes(face[..., 3] > 40)
    ys = np.nonzero(skull.any(1))[0]
    band = skull & ~ndi.binary_erosion(skull, iterations=CROWN_RIM)
    band[int(ys[0] + CROWN_PART * (ys[-1] - ys[0])):] = False
    has = hair[..., 3] > 40
    bare = band & ~has
    if not bare.any() or not has.any():
        return hair
    body = has & (hair[..., :3].mean(-1) > 110)        # not the dark outline: copied into the band it showed as black blotches in game
    if not body.any():
        return hair
    near = ndi.distance_transform_edt(~body, return_distances=False, return_indices=True)
    out = hair.copy()
    out[bare] = hair[near[0], near[1]][bare]
    return out


def grown(hair, face):
    """The hair cell enlarged by HAIR_GROW about the centre of the bald skull (after the hair was separated at its true size, where the
    sheet's own eyes and brows line up with the template's and drop out)."""
    if HAIR_GROW == 1.0:
        return hair
    x0, x1, y0, y1 = bbox(face[..., 3] > 40)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    im = Image.fromarray(hair.astype(np.uint8), "RGBA")
    k = 1 / HAIR_GROW
    big = im.transform((F, F), Image.AFFINE, (k, 0, cx - cx * k, 0, k, cy - cy * k), Image.BICUBIC)
    a = np.asarray(big).astype(np.int32).copy()
    a[..., 3] = np.where(a[..., 3] >= 128, 255, 0)                         # crisp edge again
    return a


BROW_LINE = 0.55   # part of the skull height (from its top) where the crown stretch starts: about the brow line


def lifted(hair, face, k):
    """The hair above the brow line stretched upward by k (the rows below keep their place): a taller, rounder crown."""
    if k == 1.0:
        return hair
    _, _, y0, y1 = bbox(face[..., 3] > 40)
    piv = int(y0 + BROW_LINE * (y1 - y0))
    top = Image.fromarray(hair[:piv].astype(np.uint8), "RGBA")
    top = np.asarray(top.resize((F, round(piv * k)), Image.BICUBIC)).astype(np.int32)[-piv:]     # what rises above the cell is cut
    out = hair.copy()
    out[piv - len(top):piv] = top
    out[:piv, :, 3] = np.where(out[:piv, :, 3] >= 128, 255, 0)
    return out


def row_to_canvas(row, can):
    """Eyes alignment: {dir: (k, ax, ay, cx, cy)} so that sheet row pixel (x, y) is canvas row pixel ((x - ax) * k + cx, (y - ay) * k + cy).
    k = eye distance of the canvas front head / of the sheet's; anchors = the eyes (front, side); seen from behind the head has no
    eyes: the centre column of the painted head on the centre column of the canvas skull, the row of the front view's eyes."""
    cols = {d: (row[:, i * COL_W:(i + 1) * COL_W], can[:, i * COL_W:(i + 1) * COL_W]) for i, d in enumerate(DIRS)}
    out = {}
    if ALIGN == "face":
        # Red eyes cannot be told from the red ears, blush and mouth: the face itself gives the fit. Scale = ear to ear (front view;
        # the ears stay visible under every hairstyle), front anchor = centre of the face + chin, side anchor = tip of the face + chin,
        # back = centre of the two ears, the row of the front view's chin.
        (s0, s1, _, sb), (c0, c1, _, cb) = bbox(hl.skin(cols["down"][0])), bbox(hl.skin(cols["down"][1]))
        k = (c1 - c0 + 1) / (s1 - s0 + 1)
        out["down"] = (k, (s0 + s1) / 2, float(sb), (c0 + c1) / 2, float(cb))
        (sx, sy), (cx, cy) = profile_anchor(cols["right"][0])[0], profile_anchor(cols["right"][1])[0]
        out["right"] = (k, sx, sy, cx, cy)
        (u0, u1, _, _), (v0, v1, _, _) = bbox(hl.skin(cols["up"][0])), bbox(hl.skin(cols["up"][1]))
        out["up"] = (k, (u0 + u1) / 2, float(sb), (v0 + v1) / 2, float(cb))
        return out
    se, ce = irises(cols["down"][0]), irises(cols["down"][1])
    k = (ce[1][0] - ce[0][0]) / (se[1][0] - se[0][0])
    for i, d in enumerate(DIRS):
        sc, cc = cols[d]
        if d == "up":
            x0, x1, _, _ = bbox(sc[..., 3] > 0); c0, c1, _, _ = bbox(cc[..., 3] > 0)
            out[d] = (k, (x0 + x1) / 2, out["down"][2], (c0 + c1) / 2, out["down"][4])
        else:
            si, ci = eye_points(sc, d), eye_points(cc, d)
            out[d] = (k, float(np.mean([q[0] for q in si])), float(np.mean([q[1] for q in si])),
                      float(np.mean([q[0] for q in ci])), float(np.mean([q[1] for q in ci])))
    return out


def different_from_bald(cell, bald, d, tol=70):
    """Hair and accessories of an aligned repainted head: every pixel that is not the bald head underneath. A pixel belongs to the bald
    head when the bald cell has skin there and the repainted pixel is skin too (the face, the ears), or when it lies in the eye / brow /
    mouth zone of the bald cell (the sheet's own features sit a pixel off and would double). Specks under 20 px are dropped."""
    out = cell.copy()
    both_skin = hl.skin(bald) & hl.skin(cell)
    close = (np.abs(cell[..., :3] - bald[..., :3]).max(-1) <= tol) & (bald[..., 3] > 0)
    out[(both_skin & close) | hl.features(bald, d)] = 0
    # the dark outline of the bald face itself (jaw, ear rim), repainted in place: dark pixels on the rim of the bald head that touch
    # no kept hair
    keep = out[..., 3] > 0
    rim = (bald[..., 3] > 0) & ~hl.skin(bald) & (cell[..., :3].mean(-1) < 90)
    lab, n = ndi.label(keep)
    sizes = ndi.sum(keep, lab, range(1, n + 1))
    for i in range(1, n + 1):
        comp = lab == i
        if sizes[i - 1] < 20 or (comp & rim).sum() > 0.6 * sizes[i - 1]:
            out[comp] = 0
    return out


def cells():
    """{style: {dir: 192 px RGBA hair cell}}."""
    place, faces, can = placement()
    out = {}
    for name, styles in SHEETS.items():
        path = f"{LAYERED}/src/{name}"
        if not os.path.exists(path):
            print("missing", name); continue
        sheet = hl.key_green(hl.rgba(path))
        for r, n in enumerate(styles):
            row = sheet[r * ROW_H:(r + 1) * ROW_H]
            out[n] = {}
            eyes = row_to_canvas(row, can) if ALIGN in ("eyes", "face") else None
            for i, d in enumerate(DIRS):
                col = row[:, i * COL_W:(i + 1) * COL_W]
                s, dx, dy = place[d]
                if eyes:
                    k, ax, ay, cx, cy = eyes[d]
                    print("style", n, d, "sheet head x", round(1 / k, 3), "of the canvas head")
                    # column pixel (x, y) -> canvas ((x - ax) * k + cx + i * COL_W, ...) -> cell (* s + dx)
                    cell = hl.place(col, s * k, (cx - ax * k + i * COL_W) * s + dx, (cy - ay * k) * s + dy)
                else:
                    mx, my, iou = head_shift(col, can[:, i * COL_W:(i + 1) * COL_W], dy_known=row_dy if d == "up" else None)
                    if d == "down":
                        row_dy = my
                    print("style", n, d, "head moved by", (mx, my), "canvas px, skin overlap", round(iou, 2))
                    cell = hl.place(col, s, dx + mx * s + i * COL_W * s, dy + my * s)
                hair = different_from_bald(cell, faces[d], d) if EXTRACT == "difference" else hl.hair_only(cell, faces[d], d)
                out[n][d] = lifted(grown(crown_covered(hl.fill_pinholes(hair), faces[d]), faces[d]), faces[d], CROWN_LIFT.get(d, 1.0))
    return out


def ear_anchor(a, d):
    """Point a head without eyes is placed by. Front / back: centre column of the skin, mean row of the ears (the skin in the outer
    tenth of its width: a neck in the middle does not count). Side: the tip of the face (front-most skin column) and its row."""
    sk = hl.skin(a)
    x0, x1, _, _ = bbox(sk)
    if d == "right":
        return float(x1), float(np.nonzero(sk[:, x1 - 2:x1 + 1].any(1))[0].mean())
    w = x1 - x0 + 1
    outer = sk.copy(); outer[:, int(x0 + 0.1 * w):int(x1 - 0.1 * w) + 1] = False
    return (x0 + x1) / 2, float(np.nonzero(outer.any(1))[0].mean())


def row_sheet_styles(faces):
    """{style: {dir: hair cell}} from sheets of ONE row of three heads (front | right | back) drawn on some other bald head
    (CLASSES[...]["row_sheets"]). Scale = ear to ear of the front view against heads/face, position by ear_anchor. The hair is
    separated by colour (gray body + outline): the sheet's own skin, face and neck drop out."""
    out = {}
    for name, n in CFG.get("row_sheets", {}).items():
        sheet = hl.key_green(hl.rgba(f"{LAYERED}/src/{name}"))
        w = sheet.shape[1] // 3
        cols = {d: sheet[:, i * w:(i + 1) * w] for i, d in enumerate(DIRS)}
        (s0, s1, _, _), (f0, f1, _, _) = bbox(hl.skin(cols["down"])), bbox(hl.skin(faces["down"]))
        k = (f1 - f0 + 1) / (s1 - s0 + 1)
        out[n] = {}
        for d in DIRS:
            (ax, ay), (cx, cy) = ear_anchor(cols[d], d), ear_anchor(faces[d], d)
            ox, oy = CFG.get("row_sheet_offset", {}).get(d, (0, 0))    # the other head's ears sit lower on its skull
            cell = hl.place(cols[d], k, cx - ax * k + ox, cy - ay * k + oy)
            # gray hair + its dark outline, nothing else: the blank face has no eyes, so the fringe may hang over the eyes of
            # heads/face (hl.hair_only clears that zone and cut the fringe)
            al = cell[..., 3] > 128
            gray = al & ((cell[..., :3].max(-1) - cell[..., :3].min(-1)) <= 34) & ~hl.skin(cell)
            lab, m = ndi.label(gray)
            gray = lab == 1 + int(np.argmax(ndi.sum(gray, lab, range(1, m + 1))))
            hair = cell.copy(); hair[~gray] = 0
            hair[..., :3] = (hair[..., :3] * CFG.get("row_sheet_gain", 1.0)).astype(hair.dtype)
            out[n][d] = hl.fill_pinholes(hair)
            print("style", n, d, "row sheet scale", round(k, 4))
    return out


def measure(hair, face):
    x0, x1, y0, y1 = bbox((hair[..., 3] > 40) | (face[..., 3] > 40))
    sx0, sx1, sy0, sy1 = bbox(face[..., 3] > 40)
    return x1 - x0 + 1, y1 - y0 + 1, sy0 - y0      # head-with-hair width, height, hair above the scalp


def fit(out_png):
    faces = {d: hl.rgba(f"{HEADS}/face/{d}.png") for d in DIRS}
    hairs = cells(); hairs.update(row_sheet_styles(fit_faces()))
    tiles = []
    for d in DIRS:
        row = []
        for n in sorted(hairs):
            im = Image.new("RGBA", (F, F), (70, 70, 80, 255))
            im.alpha_composite(Image.fromarray(faces[d].astype(np.uint8)))
            im.alpha_composite(Image.fromarray(hairs[n][d].astype(np.uint8)))
            row.append(np.array(im.crop((30, 20, 162, 170))))
            if d != "up":
                print(d, "style", n, "head+hair w, h, above scalp:", measure(hairs[n][d], faces[d]))
        tiles.append(np.hstack(row))
    sheet = Image.fromarray(np.vstack(tiles))
    sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).convert("RGB").save(out_png); print(out_png)


def install():
    hairs = cells(); hairs.update(row_sheet_styles(fit_faces()))
    for n, by_dir in hairs.items():
        for d, a in by_dir.items():
            hl.save(a, f"{HEADS}/hair/{n}/{d}.png")
    for sub in ("hair-under", "hair-whole"):                               # split pieces of the old long hair
        shutil.rmtree(f"{HEADS}/{sub}", ignore_errors=True)
        if os.path.exists(f"{HEADS}/{sub}.meta"):
            os.remove(f"{HEADS}/{sub}.meta")
    print("installed styles", sorted(hairs), "->", f"{HEADS}/hair")


def irises(a, below=0.45, count=2):
    """Centres of the irises, left to right: count = 2 in front, 1 in profile.
    Blue eyes: cyan goggle lenses are blue too and sit higher, so of the biggest blue blobs under `below` (part of the head height
    from its top) the LOWEST count are the eyes.
    Red eyes (front view only; the profile uses profile_anchor): ears, mouth and blush are red too. The eyes are the most level pair
    of big red blobs in the middle of the face (the ears are a level pair as well, but at its edges)."""
    r, g, bl = (a[..., i].astype(int) for i in range(3))
    ys = np.nonzero(a[..., 3] > 0)[0]
    if EYES == "red":
        m = (a[..., 3] > 0) & (r > 140) & (r - g > 90) & (r - bl > 80)
    else:
        m = (a[..., 3] > 0) & (bl > r + 60) & (bl > 150) & (g < bl - 25)
    m[:int(ys.min() + below * (ys.max() - ys.min()))] = False
    lab, n = ndi.label(ndi.binary_dilation(m, iterations=2))
    blobs = [(int(((lab == i) & m).sum()), float(np.nonzero((lab == i) & m)[1].mean()), float(np.nonzero((lab == i) & m)[0].mean()))
             for i in range(1, n + 1)]
    if EYES == "red":
        sx0, sx1, _, _ = bbox(hl.skin(a))
        cx, w, h = (sx0 + sx1) / 2, sx1 - sx0 + 1, ys.max() - ys.min() + 1
        mid = [q for q in blobs if abs(q[1] - cx) < 0.3 * w]
        mid = [q for q in mid if q[0] >= 0.25 * max(v[0] for v in mid)]
        pairs = [(-min(p[0], q[0]), p, q) for i, p in enumerate(mid) for q in mid[i + 1:]
                 if abs(p[2] - q[2]) < 0.05 * h and abs(p[1] - q[1]) > 0.15 * w]
        return sorted((q[1], q[2]) for q in min(pairs)[1:])
    big = sorted(blobs, key=lambda q: -q[0])[:4]
    return sorted(sorted(((q[1], q[2]) for q in big if q[0] >= 8), key=lambda c: -c[1])[:count])


def profile_anchor(a):
    """Profile without usable eyes (red eyes): the tip of the face (front-most skin column) and the chin (lowest skin row). Hair never
    covers them."""
    x0, x1, y0, y1 = bbox(hl.skin(a))
    return [(float(x1), float(y1))]


def eye_points(a, d, below=0.45):
    """The points a head is positioned by, for the front and the side view."""
    if d == "right":
        return profile_anchor(a) if EYES == "red" else irises(a, below, 1)
    return irises(a, below, 2)


def real_skin(old):
    """Pale skin of an old full head: not its red / blond / brown hair (those pass hl.skin). The face (patches from 120 px) plus the
    ears: smaller patches beside it, in the rows of the face (pale hair highlights lie above it)."""
    r, g, b = (old[..., i].astype(int) for i in range(3))
    real = (old[..., 3] > 128) & hl.skin(old) & (g > 0.55 * r) & (b > 0.5 * r)
    lab, k = ndi.label(real)
    sizes = ndi.sum(real, lab, range(1, k + 1))
    face = np.isin(lab, 1 + np.nonzero(sizes >= 120)[0])
    rows = np.nonzero(face.any(1))[0]
    out = face.copy()
    for i in np.nonzero((sizes >= 20) & (sizes < 120))[0]:
        comp = lab == i + 1
        cy = np.nonzero(comp.any(1))[0].mean()
        if rows[0] + 0.35 * (rows[-1] - rows[0]) <= cy <= rows[-1]:
            out |= comp
    return out


def ref_skin(a):
    return real_skin(a) if CFG.get("old_face") is not None else hl.skin(a)


def with_old_face(bald, old, d):
    """The bald head with the ORIGINAL face of the class on it (user 2026-11-12: the hero must keep the look of the original, a
    redrawn face made him another character). The old full heads all carry the same face at the same place; `old` is the one whose
    hair leaves the forehead free. Everything inside the convex outline of its skin (eyes, brows, mouth, ears, cheeks) is pasted pixel
    for pixel, except hair-coloured pixels at the rim; the bald head underneath only supplies the skull above the old hairline: its
    own eyes, brows and mouth are painted over with skin and its skin tone is matched to the old face."""
    from scipy.spatial import ConvexHull
    from PIL import ImageDraw
    real = real_skin(old)
    ys, xs = np.nonzero(real)
    pts = np.stack([xs, ys], 1)
    mask = Image.new("L", (F, F), 0)
    ImageDraw.Draw(mask).polygon([tuple(int(v) for v in q) for q in pts[ConvexHull(pts).vertices]], fill=255, outline=255)
    zone = (np.asarray(mask) > 0) & (old[..., 3] > 128)
    r, g, b = (old[..., i].astype(int) for i in range(3))
    zone &= ~((r > 170) & (g > 130) & (b < 110) & ~real)                      # the bright blond of heads/2 at the temples
    out = bald.copy()
    sk = hl.skin(out)
    feat = (out[..., 3] > 0) & ~sk & ndi.binary_erosion(ndi.binary_fill_holes(out[..., 3] > 0), iterations=3)   # marks inside the head
    grow = ndi.binary_dilation(feat, iterations=2) & (out[..., 3] > 0)
    near = ndi.distance_transform_edt(~(sk & ~grow), return_distances=False, return_indices=True)
    out[grow] = out[near[0], near[1]][grow]
    gain = old[real][:, :3].mean(0) / np.maximum(out[hl.skin(out)][:, :3].mean(0), 1)
    out[..., :3] = np.clip(out[..., :3] * gain, 0, 255).astype(out.dtype)
    out[zone] = old[zone]; out[zone, 3] = 255
    return out


REF_EYES_BELOW = 0.7


def face(out_png=None):
    """heads/face/<dir> from the bald template, in the frame space of the full head heads/<REF_STYLE>/<dir> (so head-anchors.json is unchanged):
    one scale from the distance between the eyes (front), positions by the eyes (front, side) and, seen from behind, by the centre
    column and the bottom row of the head. Nothing below the bottom row of heads/0 is kept: there the body (scarf, collar) shows."""
    sheet = hl.key_green(hl.rgba(BALD)); boxes = hl.heads_of(sheet)
    crops = {d: sheet[y0:y1, x0:x1] for d, (y0, y1, x0, x1) in zip(DIRS, boxes)}
    refs = {d: hl.rgba(f"{HEADS}/{REF_STYLE}/{d}.png") for d in DIRS}
    if FACE_FIT == "skin":      # ear to ear: ChatGPT's bald face has smaller, closer eyes than the game face, the eye distance lies
        cx0, cx1, _, _ = bbox(hl.skin(crops["down"])); rx0, rx1, _, _ = bbox(ref_skin(refs["down"]))
        s = (rx1 - rx0 + 1) / (cx1 - cx0 + 1)
    else:
        (ax, ay), (bx, by) = irises(crops["down"]); (rax, ray), (rbx, rby) = irises(refs["down"], REF_EYES_BELOW)
        s = (rbx - rax) / (bx - ax)
    out, plain = {}, {}
    for d in DIRS:
        cell = hl.place(crops[d], s, 0, 0); ref = refs[d]
        if d == "up":
            cx0, cx1, _, cy1 = bbox(cell[..., 3] > 0); rx0, rx1, _, ry1 = bbox(ref[..., 3] > 40)
            dx, dy = (rx0 + rx1) / 2 - (cx0 + cx1) / 2, ry1 - cy1
        elif FACE_FIT == "skin":   # front: centre of the face (ear to ear) + chin; side: tip of the face + chin
            if d == "right":
                ci, ri = profile_anchor(cell), [(float(bbox(ref_skin(ref))[1]), float(bbox(ref_skin(ref))[3]))]
            else:
                (c0, c1, _, cb), (r0, r1, _, rb) = bbox(hl.skin(cell)), bbox(ref_skin(ref))
                ci, ri = [((c0 + c1) / 2, float(cb))], [((r0 + r1) / 2, float(rb))]
            dx, dy = ri[0][0] - ci[0][0], ri[0][1] - ci[0][1]
        else:
            ci, ri = eye_points(cell, d), eye_points(ref, d, REF_EYES_BELOW)
            dx = float(np.mean([p[0] for p in ri])) - float(np.mean([p[0] for p in ci]))
            dy = float(np.mean([p[1] for p in ri])) - float(np.mean([p[1] for p in ci]))
        a = hl.place(crops[d], s, dx, dy)
        a[bbox(ref[..., 3] > 40)[3] + 1:] = 0
        plain[d] = a                                                    # the bald head as on the canvas: what the hair sheets are fitted to
        if CFG.get("old_face") is not None and d != "up":
            a = with_old_face(a, hl.rgba(f"{HEADS}/{CFG['old_face']}/{d}.png"), d)
        out[d] = a
        x0, x1, y0, y1 = bbox(a[..., 3] > 0)
        print(d, "scale", round(s, 4), "bald head w, h", x1 - x0 + 1, y1 - y0 + 1)
    if out_png:                                                           # preview: heads/0 | bald | bald under a ghost of heads/0
        tiles = []
        for d in DIRS:
            row = []
            for mode in (0, 1, 2):
                im = Image.new("RGBA", (F, F), (70, 70, 80, 255))
                if mode != 0:
                    im.alpha_composite(Image.fromarray(out[d].astype(np.uint8)))
                if mode != 1:
                    g = refs[d].copy().astype(np.uint8)
                    if mode == 2: g[..., 3] = g[..., 3] // 2
                    im.alpha_composite(Image.fromarray(g))
                row.append(np.array(im.crop((30, 10, 162, 120))))
            tiles.append(np.hstack(row))
        sh = Image.fromarray(np.vstack(tiles)); sh.resize((sh.width * 3, sh.height * 3), Image.NEAREST).convert("RGB").save(out_png)
        print(out_png); return
    for d in DIRS:
        hl.save(out[d], f"{HEADS}/face/{d}.png")
        hl.save(plain[d], f"{LAYERED}/fit-bald/{d}.png")
    print("wrote", f"{HEADS}/face")


def canvas():
    t = Image.open(BALD).convert("RGB")
    can = Image.new("RGB", (1024, ROW_H * 3), t.getpixel((5, 5)))
    row = t.resize((1024, round(t.height * 1024 / t.width)), Image.LANCZOS)
    for i in range(3):
        can.paste(row, (0, i * ROW_H + (ROW_H - row.height) // 4))
    os.makedirs(os.path.dirname(CANVAS), exist_ok=True); can.save(CANVAS); print(CANVAS, can.size)


if __name__ == "__main__":
    if len(sys.argv) < 3 or sys.argv[1] not in CLASSES:
        print(__doc__); sys.exit()
    use(sys.argv[1])
    cmd = sys.argv[2]
    if cmd == "face": face(sys.argv[3] if len(sys.argv) > 3 else None)
    elif cmd == "canvas": canvas()
    elif cmd == "fit": fit(sys.argv[3])
    elif cmd == "install": install()
    else: print(__doc__)

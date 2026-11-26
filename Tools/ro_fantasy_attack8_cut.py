"""Cut one 8-frame attack source (body-<class>-attack-<dir>-8f-chatgpt-N.png: 2 rows x 4 = frames 0-7 of ONE direction) into game cells.

Warrior heavy slash redo 22/11 (user: "chém giống đứng yên cầm kiếm múa quá"): the art now lunges (step, lean, follow-through), and the cut
adds a small body travel instead of pinning every frame on the same feet centre:
- scale: frame 7 (guard pose, like idle) gets the idle height BALD_PX, the same factor for the whole row (crouch / lunge frames stay
  shorter / wider, as painted); if the painted skull is then > HEAD_TOL off the idle skull width, only the head is rescaled
  (ro_fantasy_head_fix.scale_head) so the hairstyle layer fits.
- position: every frame is placed on its feet centre (like idle), then shifted along the facing by a fixed travel curve TRAVEL
  (anticipation back, lunge forward deepest at the impact frame 3, ease back to the guard), y at half weight (ground perspective).
  Reading the travel from ChatGPT's placement in each grid cell was tried first: it is loose (a side-view lunge drifted backward,
  rows disagreed), so the curve is designed, the same for all 8 directions.
Used by Tools/ro_fantasy_body_cut.motion_rows.
"""
from importlib import import_module

from PIL import Image

pilot = import_module("build-ro-soft-8dir-pilot")         # figures_with_pos, key_green
sheets_tool = import_module("build-hero-layered-sheets")  # head_anchor
head_fix = import_module("ro_fantasy_head_fix")

GRID_W, GRID_H = 384, 512
_D = 0.7071
FACING = {"down": (0, 1), "down-right": (_D, _D), "right": (1, 0), "up-right": (_D, -_D), "up": (0, -1), "up-left": (-_D, -_D),
          "left": (-1, 0), "down-left": (-_D, _D)}
HEAD_TOL = 0.04
# (class, direction) -> head factor forced on that 8f row (the skull band measure misses it): Cleric V3 up-right / up-left 26/11, the
# painted skull is 6.8 % / 4 % narrower than the idle up-right skull (head anchors 69 / 71 vs 74 px) while skull_w reads only 3-4 %.
HEAD_K_OF = {("cleric", "up-right"): 1.065, ("cleric", "up-left"): 1.035}
TRAVEL = [0, 0, 0, 0, 0, 0, 0, 0]  # cell px along the facing per frame. 22/11 V2: 0, the lunge is done in the engine (WarriorSlashFeel.Lunge, figure +
# shadow slide 13-19 px on screen) so art travel and root travel never stack. Was [-4, -8, 6, 12, 10, 6, 2, 0].


def frames(path):
    """Figures of frames 0-7, ordered by their 2 x 4 grid cell."""
    out = []
    for f, x, y in pilot.figures_with_pos(pilot.key_green(Image.open(path)), 8):
        out.append((int((y + f.height / 2) // GRID_H) * 4 + int((x + f.width / 2) // GRID_W), f))
    out.sort(key=lambda t: t[0])
    if [t[0] for t in out] != list(range(8)):
        raise SystemExit(f"{path.name}: figures not on a 2 x 4 grid ({[t[0] for t in out]})")
    return [f for _, f in out]


# Classes whose 8f attack cells get olive key wedges erased (Cleric V3 26/11: up-right F3-F5, a dark olive wedge (~#604508, key green
# mixed with the dark outline) enclosed between the cheek and the raised arm, 14-60 px, that despeck keeps: it is not near enough to the
# transparent edge and not green enough). Blob = g > 0.62 r, b < 0.25 g, r < 135, at least OLIVE_MIN px (lone outline pixels stay).
OLIVE_WEDGE_CLASSES = {"cleric"}
OLIVE_MIN = 8


def erase_olive(cell):
    import numpy as np
    from scipy import ndimage
    a = np.asarray(cell).copy()
    r, g, b = (a[..., i].astype(int) for i in range(3))
    m = (a[..., 3] > 0) & (g > 0.62 * r) & (b < 0.25 * g) & (r < 135) & (g > 30)
    lab, n = ndimage.label(m)
    if not n:
        return cell
    sizes = ndimage.sum(m, lab, range(1, n + 1))
    big = np.isin(lab, 1 + np.nonzero(sizes >= OLIVE_MIN)[0])
    if big.any():
        a[ndimage.binary_dilation(big) & m | big, 3] = 0
    return Image.fromarray(a, "RGBA")


def shift(cell, dx, dy):
    out = Image.new("RGBA", cell.size)
    out.paste(cell, (dx, dy), cell)  # negative offsets crop at the cell edge
    return out


def cut(path, idle_skull_w, place, skull_w, despeck, bald_px, label, facing):
    """8 cells (CW x CH) for one direction; idle_skull_w = skull width of the idle cell of the same view (game scale)."""
    figs = frames(path)
    guard = figs[7]
    s = bald_px / guard.height
    cells = [despeck(place(f, s)[0]) for f in figs]
    k = HEAD_K_OF.get((label.split()[0], facing), idle_skull_w / (skull_w(guard) * s))
    if abs(k - 1.0) > HEAD_TOL or (label.split()[0], facing) in HEAD_K_OF:
        cells = [head_fix.scale_head(c, k, *sheets_tool.head_anchor(c)[1:3]) for c in cells]
    if label.split()[0] in OLIVE_WEDGE_CLASSES:
        cells = [erase_olive(c) for c in cells]
    ux, uy = FACING[facing]
    out = [shift(c, int(round(t * ux)), int(round(t * uy * 0.5))) for c, t in zip(cells, TRAVEL)]
    print(f"{label}: 8f scale {s:.3f} (guard on idle height), head x{k:.3f}{'' if abs(k - 1) > HEAD_TOL else ' (kept)'}")
    return out

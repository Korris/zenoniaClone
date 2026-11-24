"""Taurus V4 monsters (2026-11-24): cut the ChatGPT 4 x 3 sheets (4 frames x rows down, up, right; left = right mirrored)
into game monster sheets. Same format as Tools/build-ro-soft-monsters.py (reuses its keying / splitting / placing):

Source  Assets/ArtLibrary/monsters/taurus/<id>/<id>-{walk,attack}-chatgpt-N.png   (highest N wins, flat #FF00FF or #00FF00)
Output  Assets/Resources/Sprites/monsters/taurus/<id>/<id>.png         walk 4x4 cells 192 (768x768), rows down, up, left, right, feet = last px row
        Assets/Resources/Sprites/monsters/taurus/<id>/<id>-attack.png  attack 4x4 cells 256 (1024x1024), feet y = 224
Check   Assets/ArtLibrary/monsters/taurus/<id>/check/<id>-frames.png   every frame on light meadow + dark forest, hero for scale

Body height = fraction of the hero height H (production sheet V4 MON-T0x), H = 164 px in a 192 cell (hero_ref of the ro-soft tool).
Not wired into the game here (MonsterDef / Resources paths are another task).

  python Tools/build-taurus-monsters.py build <id> [...]
  python Tools/build-taurus-monsters.py check <id> [...]
"""
import importlib.util, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
SRC = f"{ROOT}/Assets/ArtLibrary/monsters/taurus"
OUT = f"{ROOT}/Assets/Resources/Sprites/monsters/taurus"
HERO_H = 164
# body height as a fraction of H (taurus-production-sheet-v3-part1 VI)
SIZE_H = {"tauCloverSlime": 0.55, "tauHornRabbit": 0.72, "tauMossMushroom": 0.75,
          "tauReedFrog": 0.60, "tauBrambleBoar": 0.90, "tauLeafMantis": 0.85, "tauRootling": 0.85,
          "tauThornSprite": 0.75,
          # 1.20H = 197 px does not fit the 192 cell: the walk scale is capped by the cell, the game must enlarge it
          # (RenderScale = 1.20H / printed body height)
          "tauAncientBarkling": 1.20,
          # 2026-11-24 field boss + Ancient Root Shrine (taurus-bosses-dungeon-approval-animation-spec-chatgpt.txt).
          # Bosses above ~1.1H are capped by the 192 / 256 cells: the game enlarges them with RenderScale (printed by build)
          "tauAmberSporeling": 0.75, "tauRuneBarkGuardian": 1.30,
          "tauBramblebackMatriarch": 2.10, "tauElderRootGuardian": 3.75}
# boss skill sheets: source <id>-<skill>-chatgpt-N.png (4 x 3 like attack) -> <id>-<skill>.png, 4x4 cells 256, feet y = 224
SKILLS = {"tauBramblebackMatriarch": ["charge", "stomp"],
          "tauElderRootGuardian": ["sweep", "pulse", "awaken"]}
# big bosses are cut at DOUBLE cells (walk 384 / attack 512, feet 448; the def sets WalkCellPx 384 / AttackCellPx 512): the 1536 x 1024 ChatGPT sources hold ~340 px bodies,
# a 192 cell would throw half of them away and the game would then enlarge the blurry result 3.4x (Elder Root Guardian, art director note 24/11)
CELL_MUL = {"tauElderRootGuardian": 2}
# flyers: body lifted this many px (192 cell) above the feet line; the game draws the shadow at the feet
HOVER = {"tauThornSprite": 16}
# sources with a soft light haze painted in the gaps between arm and body (keys to a grey veil): drop faint, light pixels
DEHAZE = {"tauAncientBarkling"}
# sources with loose particles painted around the body (Sporeling spores: the art director wants them as a Unity Particle System):
# drop every detached blob smaller than this fraction of the 12 frames' median blob
DESPECK = {"tauAmberSporeling": 0.08}


def dehaze(arr):
    """Pixels that are both faint (alpha < 0.55) and light in every channel (min RGB > 0.55) are haze, not art."""
    m = (arr[..., 3] < 0.55) & (arr[..., :3].min(axis=2) > 0.55)
    arr[m] = 0
    return int(m.sum())

_spec = importlib.util.spec_from_file_location("rosoft", f"{ROOT}/Tools/build-ro-soft-monsters.py")
rs = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(rs)
mm = rs.mm
_key_any = rs.key_any


def despeck(rgba, frac):
    """Clear detached blobs much smaller than a frame (painted particles); the 12 largest blobs are the frames."""
    from scipy import ndimage
    lab, n = ndimage.label(rgba[..., 3] > 0.2)
    if n <= 12:
        return rgba
    sizes = ndimage.sum(np.ones(lab.shape), lab, index=np.arange(1, n + 1))
    ref = float(np.median(np.sort(sizes)[-12:]))
    small = np.where(sizes < frac * ref)[0] + 1
    rgba[np.isin(lab, small)] = 0
    print(f"  despeck: {len(small)} blobs cleared")
    return rgba


def _current_mid(path):
    return os.path.basename(os.path.dirname(path))


rs.key_any = lambda path: (despeck(_key_any(path), DESPECK[_current_mid(path)]) if _current_mid(path) in DESPECK else _key_any(path))


def build(mid):
    rs.SRC = f"{SRC}/{mid}"                       # latest() reads rs.SRC
    os.makedirs(f"{OUT}/{mid}", exist_ok=True)
    mul = CELL_MUL.get(mid, 1)
    wc, ac, feet = 192 * mul, 256 * mul, 224 * mul
    walk, atk = rs.frames_4x3(rs.latest(mid, "walk")), rs.frames_4x3(rs.latest(mid, "attack"))
    mm.WALK_CELL, mm.ATK_CELL, mm.ATK_FEET = wc, ac, feet
    target = round(SIZE_H[mid] * HERO_H) * mul
    want, cap = mm.fit_scale(walk, atk, target, wc, wc)
    s = min(want, cap)
    lift = HOVER.get(mid, 0) * mul
    if lift:
        s = min(s, (wc - lift - mm.MARGIN) / max(f["h"] for f in walk), (feet - lift - mm.MARGIN) / max(f["h"] for f in atk))
    # attack body = walk body: scale the attack frames so the guard frame (column 4) matches the walk median height (+-25 %)
    k = float(np.median([f["main_h"] for f in walk])) / float(np.median([atk[r * 4 + 3]["main_h"] for r in range(4)]))
    sa = s * min(1.25, max(0.8, k))
    sa = min([sa] + [min((ac - 2 * mm.MARGIN) / f["w"], (feet - lift - mm.MARGIN) / f["h"]) for f in atk])
    w = mm.sheet_from(mm.place(walk, s, wc, wc - lift), wc)
    a = mm.sheet_from(mm.place(atk, sa, ac, feet - lift), ac)
    if mid in DEHAZE:
        print(f"  dehaze: walk {dehaze(w)} px, attack {dehaze(a)} px")
    mm.to_image(w).save(f"{OUT}/{mid}/{mid}.png"); mm.to_image(a).save(f"{OUT}/{mid}/{mid}-attack.png")
    walk_med = float(np.median([f["main_h"] for f in walk]))
    for sk in SKILLS.get(mid, []):
        try:
            fr = rs.frames_4x3(rs.latest(mid, sk))
        except SystemExit as e:
            print(f"  skill {sk}: {e}"); continue
        # skill body = walk body: match the wind-up frame (column 1) to the walk median (+-25 %), then fit the cell
        k = walk_med / float(np.median([fr[r * 4]["main_h"] for r in range(4)]))
        ss = s * min(1.25, max(0.8, k))
        ss = min([ss] + [min((ac - 2 * mm.MARGIN) / f["w"], (feet - lift - mm.MARGIN) / f["h"]) for f in fr])
        sk_sheet = mm.sheet_from(mm.place(fr, ss, ac, feet - lift), ac)
        mm.to_image(sk_sheet).save(f"{OUT}/{mid}/{mid}-{sk}.png")
        print(f"  skill {sk}: x{ss / s:.3f} of walk scale, halo {mm.halo_count(sk_sheet)}")
    nh = int(np.median([rs.body_h(f) for row in rs.grid_frames(f"{OUT}/{mid}/{mid}.png", wc) for f in row]))
    print(f"{mid}: walk scale {s:.3f} (wanted {want:.3f}, cap {cap:.3f}), attack x{sa / s:.3f}; body {nh} px "
          f"(target {target} = {SIZE_H[mid]}H); halo walk {mm.halo_count(w)} attack {mm.halo_count(a)}"
          + (f"; RenderScale needed {target / nh:.2f}" if nh < target * 0.97 else ""))


def check(mid, second="attack"):
    """Walk (left) + attack or a skill sheet (right), 4 rows x 4 frames, on light meadow (top) and dark forest (bottom); hero beside each walk row."""
    mul = CELL_MUL.get(mid, 1)
    wc, ac, feet = 192 * mul, 256 * mul, 224 * mul
    walk = rs.grid_frames(f"{OUT}/{mid}/{mid}.png", wc)
    atk = rs.grid_frames(f"{OUT}/{mid}/{mid}-{second}.png", ac)
    hero = rs.hero_ref(wc)
    pad, top = 6, 18
    bw = (wc + pad) * 5 + pad * 3 + (ac + pad) * 4
    bh = top + 4 * (ac + pad)
    sheet = Image.new("RGBA", (bw, 2 * bh), (0, 0, 0, 255))
    d = ImageDraw.Draw(sheet)
    for bi, (bg, ink) in enumerate((((168, 206, 120, 255), (40, 50, 30, 255)), ((46, 62, 44, 255), (230, 230, 210, 255)))):
        y0 = bi * bh
        d.rectangle([0, y0, bw, y0 + bh - 1], fill=bg)
        d.text((4, y0 + 3), f"{mid}  walk 192 | {second} 256   rows: down, up, left, right   red = feet line", fill=ink)
        for r in range(4):
            ry = y0 + top + r * (ac + pad)
            sheet.alpha_composite(hero, (0, ry + ac - wc))
            for c in range(4):
                for cell, fr, x0 in ((wc, walk, wc + pad), (ac, atk, (wc + pad) * 5 + pad * 3)):
                    cx = x0 + c * (cell + pad)
                    cy = ry + ac - cell
                    d.rectangle([cx, cy, cx + cell - 1, cy + cell - 1], outline=(120, 110, 90, 255))
                    fy = cy + (cell - 1 if cell == wc else feet)
                    d.line([(cx, fy), (cx + cell - 1, fy)], fill=(200, 60, 50, 255))
                    sheet.alpha_composite(fr[r][c], (cx, cy))
    os.makedirs(f"{SRC}/{mid}/check", exist_ok=True)
    out = f"{SRC}/{mid}/check/{mid}-frames.png" if second == "attack" else f"{SRC}/{mid}/check/{mid}-{second}-frames.png"
    sheet.convert("RGB").save(out)
    print(out)


if __name__ == "__main__":
    cmd, ids = sys.argv[1], sys.argv[2:]
    for m in ids:
        if cmd == "build":
            build(m)
        else:
            for second in ["attack"] + [sk for sk in SKILLS.get(m, []) if os.path.exists(f"{OUT}/{m}/{m}-{sk}.png")]:
                check(m, second)

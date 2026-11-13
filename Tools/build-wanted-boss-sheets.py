"""Builds the sheets of the WANTED event bosses (big raid bosses, 2026-11-12) from ChatGPT strips on #FF00FF.

Source  Assets/ArtLibrary/monsters/wanted/<stem>-{walk,attack}-{down-up,left-right}-chatgpt-1.png
        each 1536x1024: 2 rows x 4 frames (row 1 = the first facing of the file name, row 2 = the second)
Output  Assets/Resources/Sprites/monsters/<id>.png         1536x1536 (4x4 cells of 384, rows down, up, left, right; feet on the cell's last row)
        Assets/Resources/Sprites/monsters/<id>-attack.png  2048x2048 (4x4 cells of 512, feet 448 px down = pivot 64 px above the bottom)
        Assets/Resources/Sprites/monsters/<id>-cast.png    2048x2048 (4x4 cells of 512, feet 448 px down; facing the viewer only):
                                                           row 0 skill 2 (targeted slam), row 1 skill 3 (stomp around the boss),
                                                           row 2 skill 4 (enrage), row 3 defeat. Skill 1 (the cone) uses <id>-attack.
        Assets/Resources/Sprites/fx/wanted/<id>-skills.png 1536x1024 (4 rows x 4 frames, cells 384x256, centred): the effect of skills 1-4
        Assets/Resources/Sprites/fx/wanted/<id>-skill<n>.png 1536x1024 (2x2 frames of 768x512, not re-centred): skill n's big effect,
                                                           cone apex at the cell's top centre (opens south), circles centred
        Assets/Resources/Sprites/fx/wanted/frozen-block.png 1536x1024 (4x2 frames of 384x512): the ice block over a frozen hero
                                                           (form 1-2, hold 3-4, crack + shatter 5-8), feet at the bottom centre
        Assets/Resources/Sprites/fx/wanted/warning-*.png   shared telegraphs + icons (white on clear, tinted in code), from the black sheet
        (+ .meta copied from frostfangKing with a new guid). Twice the cell size of the ordinary monsters: MonsterDef carries the cell sizes.
Sources of the extra sheets: <stem>-cast-<a>-<b>-chatgpt-1.png (2 rows x 4, CASTS below), ArtLibrary/custom/wanted-boss/skills/.

One scale per boss for all 32 frames (median body height of the walk frames = TARGET_H), feet of each row on the baseline, frames shifted
into the cell, never clipped. The keying / placing code is the meadow monster tool's (Tools/build-meadow-monsters.py).

  py Tools/build-wanted-boss-sheets.py check OUT.png [id ...]   the frames on dark, with a hero for scale, no game files
  py Tools/build-wanted-boss-sheets.py write [id ...]
  py Tools/build-wanted-boss-sheets.py fx [id ...]              only the big per-skill effect sheets
"""
import importlib.util, os, re, sys, uuid
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
SRC = f"{ROOT}/Assets/ArtLibrary/monsters/wanted"
OUT = f"{ROOT}/Assets/Resources/Sprites/monsters"
WALK_CELL, ATK_CELL, ATK_FEET, MARGIN = 384, 512, 448, 4
TARGET_H = 330      # median walk body height in the 384 cell (px). Hero body = 164 px of a 192 cell = 328 px at this resolution,
                    # so RenderScale 3 in MonsterDef makes the boss three hero heights.
CASTS = {"rimeclawTyrant": ("cast-chain-slam-stomp", "cast-blizzard-defeat"),
         "glacialColossus": ("cast-slam-crystal-fall", "cast-overload-defeat"),
         "mireHydraQueen": ("cast-breath-tail", "cast-reign-defeat")}   # two 2 x 4 strips -> the 4 rows of <id>-cast
SKILLS = f"{ROOT}/Assets/ArtLibrary/custom/wanted-boss/skills"
FX_OUT = f"{ROOT}/Assets/Resources/Sprites/fx/wanted"
FX_W, FX_H = 384, 256
WARNING = ["circle", "cone", "line", "safe", "alert", "enraged", "slow", "poison", "castbar-frame", "castbar-fill"]   # reading order on the sheet
BOSSES = {"rimeclawTyrant": "rimeclaw-tyrant", "glacialColossus": "glacial-colossus", "mireHydraQueen": "mire-hydra-queen"}

_spec = importlib.util.spec_from_file_location("meadow", f"{ROOT}/Tools/build-meadow-monsters.py")
mm = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(mm)
mm.MARGIN = MARGIN


def frames_of(path):
    """8 frames of a 2 x 4 strip, row-major."""
    rgba = mm.key_magenta(path)
    alpha = rgba[..., 3] > 0.2
    ys = mm.valleys(alpha.sum(1), 2)
    out = []
    for r in range(2):
        xs = mm.valleys(alpha[ys[r]:ys[r + 1]].sum(0), 4)
        for c in range(4):
            out.append(mm.extract(rgba, (ys[r], ys[r + 1], xs[c], xs[c + 1])))
    return out


def load(stem):
    """(walk, attack): 16 frames each in game row order down, up, left, right."""
    sets = []
    for kind in ("walk", "attack"):
        a = frames_of(f"{SRC}/{stem}-{kind}-down-up-chatgpt-1.png")
        b = frames_of(f"{SRC}/{stem}-{kind}-left-right-chatgpt-1.png")
        sets.append(a + b)
    return sets


def build(mid):
    walk, atk = load(BOSSES[mid])
    s = TARGET_H / float(np.median([f["main_h"] for f in walk]))
    cap = s
    for frames, cell, feet in ((walk, WALK_CELL, WALK_CELL), (atk, ATK_CELL, ATK_FEET)):
        for f in frames:
            cap = min(cap, (cell - 2 * MARGIN) / f["w"], (feet - MARGIN) / f["h"])
    if cap < s:
        print(f"  {mid}: scale capped {s:.3f} -> {cap:.3f} (a frame would not fit its cell)")
    s = min(s, cap)
    w = mm.sheet_from(mm.place(walk, s, WALK_CELL, WALK_CELL), WALK_CELL)
    a = mm.sheet_from(mm.place(atk, s, ATK_CELL, ATK_FEET), ATK_CELL)
    heights = [round(f["main_h"] * s) for f in walk]
    print(f"  {mid}: scale {s:.3f}, walk body heights {min(heights)}-{max(heights)} px, halo px walk {mm.halo_count(w)} attack {mm.halo_count(a)}")
    return mm.to_image(w), mm.to_image(a)


def build_cast(mid, scale):
    frames = []
    for part in CASTS[mid]:
        frames += frames_of(f"{SRC}/{BOSSES[mid]}-{part}-chatgpt-1.png")
    for f in frames:
        scale = min(scale, (ATK_CELL - 2 * MARGIN) / f["w"], (ATK_FEET - MARGIN) / f["h"])
    return mm.to_image(mm.sheet_from(mm.place(frames, scale, ATK_CELL, ATK_FEET), ATK_CELL))


def build_fx(mid):
    """The effect strips keyed and re-centred cell by cell (each effect keeps its drawn size: one scale for the sheet)."""
    rgba = mm.key_magenta(f"{SKILLS}/{BOSSES[mid]}-skill-fx-chatgpt-1.png")
    alpha = rgba[..., 3] > 0.2
    ys = mm.valleys(alpha.sum(1), 4)
    cells = []
    for r in range(4):
        xs = mm.valleys(alpha[ys[r]:ys[r + 1]].sum(0), 4)
        for c in range(4):
            cell = rgba[ys[r]:ys[r + 1], xs[c]:xs[c + 1]]
            yy, xx = np.where(cell[..., 3] > 0.05)
            cells.append(cell[yy.min():yy.max() + 1, xx.min():xx.max() + 1])
    k = min(1.0, min((FX_W - 8) / c.shape[1] for c in cells), min((FX_H - 8) / c.shape[0] for c in cells))
    sheet = np.zeros((FX_H * 4, FX_W * 4, 4), np.float32)
    for i, c in enumerate(cells):
        c = mm.resize(c, k) if k < 1.0 else c
        r, col = divmod(i, 4)
        h, w = c.shape[:2]
        y, x = r * FX_H + (FX_H - h) // 2, col * FX_W + (FX_W - w) // 2
        sheet[y:y + h, x:x + w] = c
    return mm.to_image(sheet)


FX_SHARED = {("mireHydraQueen", 4): 2}   # skill 4's picture is skill 2's (Toxic Reign = the same venom bloom on five circles)


def skill_fx_sources(mid):
    """{n: path} of the big per-skill effect sheets that exist (<stem>-skill<n>-fx-chatgpt-1.png, 2x2 frames of 768x512)."""
    src = lambda n: f"{SKILLS}/{BOSSES[mid]}-skill{FX_SHARED.get((mid, n), n)}-fx-chatgpt-1.png"
    return {n: src(n) for n in range(1, 5) if os.path.exists(src(n))}


def build_skill_fx(path, cell_w=768, cell_h=512):
    """One skill's 1536x1024 sheet keyed, cut on the fixed 2x2 grid and NOT re-centred: the art is drawn around the game pivot
    (cone apex at the cell's top centre, circle centre at the cell centre), so moving a frame would move the effect off its zone."""
    rgba = mm.key_magenta(path)
    h, w = rgba.shape[:2]
    if (w, h) != (1536, 1024):
        sys.exit(f"{path}: {w}x{h}, expected 1536x1024")
    sheet = np.zeros_like(rgba)
    for y in range(0, h, cell_h):
        for x in range(0, w, cell_w):
            cell = rgba[y:y + cell_h, x:x + cell_w].copy()
            cell[:3], cell[-3:], cell[:, :3], cell[:, -3:] = 0, 0, 0, 0     # a stray key-colour fringe on the grid line
            sheet[y:y + cell_h, x:x + cell_w] = cell
    return mm.to_image(sheet)


def write_skill_fx(ids):
    frozen = f"{SKILLS}/frozen-block-chatgpt-1.png"                 # shared hero freeze overlay: 4x2 cells of 384x512, feet at the cell's bottom centre
    if os.path.exists(frozen):
        out = f"{FX_OUT}/frozen-block.png"
        build_skill_fx(frozen, 384, 512).save(out); write_meta(out); print("wrote", out)
    for mid in ids:
        for n, src in skill_fx_sources(mid).items():
            out = f"{FX_OUT}/{mid}-skill{n}.png"
            build_skill_fx(src).save(out); write_meta(out); print("wrote", out)


def build_warning():
    """{name: image}: white art with the sheet's brightness as alpha (drawn additively / tinted in the game), one PNG per piece."""
    from scipy import ndimage
    lum = np.asarray(Image.open(f"{SKILLS}/boss-warning-assets-chatgpt-1.png").convert("L")).astype(np.float32) / 255.0
    lab, n = ndimage.label(ndimage.binary_closing(lum > 0.12, iterations=10))
    boxes = [b for i, b in enumerate(ndimage.find_objects(lab)) if (lab[b] == i + 1).sum() > 1500]
    top = sorted([b for b in boxes if b[0].start < lum.shape[0] * 0.55], key=lambda b: b[1].start)
    low = sorted([b for b in boxes if b[0].start >= lum.shape[0] * 0.55], key=lambda b: (b[1].start, b[0].start))
    if len(top) + len(low) != len(WARNING):
        sys.exit(f"warning sheet: {len(top)} + {len(low)} pieces, expected {len(WARNING)}")
    out = {}
    for name, (ys, xs) in zip(WARNING, top + low):
        a = np.clip(lum[max(0, ys.start - 8):ys.stop + 8, max(0, xs.start - 8):xs.stop + 8] * 1.15, 0, 1)
        img = np.dstack([np.ones_like(a)] * 3 + [a])
        out[name] = mm.to_image(img)
    return out


def write_meta(png):
    if os.path.exists(png + ".meta"):
        return                                                    # keep the guid Unity already knows
    text = open(f"{OUT}/frostfangKing-attack.png.meta", encoding="utf-8").read()
    open(png + ".meta", "w", encoding="utf-8", newline="\n").write(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, text, count=1))


def write(ids):
    os.makedirs(FX_OUT, exist_ok=True)
    for mid in ids:
        w, a = build(mid)
        todo = [(w, f"{OUT}/{mid}.png"), (a, f"{OUT}/{mid}-attack.png")]
        if mid in CASTS:
            walk, _ = load(BOSSES[mid])
            todo.append((build_cast(mid, TARGET_H / float(np.median([f["main_h"] for f in walk]))), f"{OUT}/{mid}-cast.png"))
            if os.path.exists(f"{SKILLS}/{BOSSES[mid]}-skill-fx-chatgpt-1.png"):   # the old combined sheet: only Rimeclaw has one
                todo.append((build_fx(mid), f"{FX_OUT}/{mid}-skills.png"))
        for img, path in todo:
            img.save(path); write_meta(path)
        print("wrote", mid)
    write_skill_fx(ids)
    for name, img in build_warning().items():
        img.save(f"{FX_OUT}/warning-{name}.png"); write_meta(f"{FX_OUT}/warning-{name}.png")
    print("wrote warning pieces")


def check(path, ids):
    rows = []
    hero = Image.open(f"{ROOT}/Assets/Resources/Sprites/classes/warrior/body/armed/idle.png").convert("RGBA").crop((0, 0, 192, 192)).resize((384, 384), Image.LANCZOS)
    for mid in ids:
        w, a = build(mid)
        strip = Image.new("RGBA", (WALK_CELL * 4 + 384 + ATK_CELL * 4, ATK_CELL * 4), (40, 44, 54, 255))
        strip.alpha_composite(w, (0, 0)); strip.alpha_composite(a, (WALK_CELL * 4 + 384, 0))
        for r in range(4):                                        # a hero at the same resolution beside each walk row, feet on the same line
            strip.alpha_composite(hero, (WALK_CELL * 4, r * WALK_CELL))
        rows.append(strip)
    out = Image.new("RGBA", (rows[0].width, sum(r.height for r in rows)), (40, 44, 54, 255))
    y = 0
    for r in rows:
        out.alpha_composite(r, (0, y)); y += r.height
    out.resize((out.width // 2, out.height // 2), Image.LANCZOS).convert("RGB").save(path); print(path)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "check": check(sys.argv[2], sys.argv[3:] or ["rimeclawTyrant"])
    elif cmd == "write": write(sys.argv[2:] or ["rimeclawTyrant"])
    elif cmd == "fx": write_skill_fx(sys.argv[2:] or ["rimeclawTyrant"])
    else: print(__doc__)

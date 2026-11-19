"""Layered hero, step D (2026-11-19): real art -> game assets (spec docs/character-appearance-layered-spec.md).

  python Tools/build-hero-layered-game-assets.py write     bodies (male, female) + hair of the 5 classes + json
  python Tools/build-hero-layered-game-assets.py check     offline contact sheets (Tools/hero_layered_compose_check.py)
  python Tools/build-hero-layered-game-assets.py accessory split the Mechanic goggles out of the installed hair strip (write does it too)

Bodies: Assets/ArtLibrary/hero-layered/sheets/<gender>/<anim>.png (8 rows, Tools/build-hero-layered-sheets.py); only the 5 painted rows
are kept (the game mirrors the left ones). Walk frames are re-centred on the skull (the cut centred them on the feet, so the body swayed
up to 26 px sideways between frames). Magenta outfit -> grey + mask (Tools/hero_recolor_core.py). Head anchor of every frame = skull top
centre (x, y) + skull width w (median of the anim row, so the hair does not pulse with detection noise).
  -> Sprites/hero-layered/<gender>/body/<anim>.png, <anim>-mask.png ; Data/hero-layered/<gender>/body.json
Hair: latest src/hair-<class>-5dir-chatgpt-N.png -> Sprites/hero-layered/hair/<class>/0.png (5 cells: down, down-right, right, up-right, up)
  Mechanic: goggles + strap split into hair/mechanic/0-acc.png (`accSheet`, hidden under any hat).
  -> Data/hero-layered/<class>/class.json (body, hair; weapons / grips kept: Tools/build-hero-layered-grips.py; hats: headgear.json).
"""
import json
import re
import sys
import uuid
from importlib import import_module
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hero_recolor_core as core  # noqa: E402
import hero_layered_hair as hair  # noqa: E402
import hero_layered_hair_accessory as accessory  # noqa: E402
sheets_tool = import_module("build-hero-layered-sheets")  # head_anchor

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/ArtLibrary/hero-layered"
SPR = ROOT / "Assets/Resources/Sprites/hero-layered"
DATA = ROOT / "Assets/Resources/Data/hero-layered"
META_SRC = SPR / "test/body"  # import settings of the step-B test sheets (uncompressed, 4096, mask linear)
CW, CH, FEET, BODY_PX, PAINTED = 320, 256, 244, 200, 5
ANIMS = [("idle", 1), ("walk", 4), ("attack1", 4), ("attack2", 4), ("attack3", 4)]
CLASSES = {"warrior": "male", "wizard": "male", "mechanic": "male", "assassin": "female", "cleric": "female"}


def cells_of(g, anim, n):
    sh = Image.open(ART / f"sheets/{g}/{anim}.png").convert("RGBA")
    return [[sh.crop((f * CW, r * CH, (f + 1) * CW, (r + 1) * CH)) for f in range(n)] for r in range(PAINTED)]


def shift_x(cell, dx):
    out = Image.new("RGBA", cell.size)
    out.alpha_composite(cell, (dx, 0)) if dx >= 0 else out.alpha_composite(cell.crop((-dx, 0, CW, CH)), (0, 0))
    return out


def belt_to_leather(sheet):
    """ChatGPT painted the belt ends wine-pink (hue 330-355) in some frames; the outfit mask does not take them (they are not the coat), so
    they would stay pink on every outfit colour. Below the head (cell row >= 128) such pixels get the leather hue 25, same s / v."""
    a = np.asarray(sheet).copy()
    rgb = a[..., :3].astype(np.float32) / 255.0
    h, s, v = core.hsv(rgb)
    rows = (np.arange(a.shape[0]) % CH >= 128)[:, None]
    m = rows & (a[..., 3] > 8) & (h >= 330) & (h <= 355) & (s > 0.35) & ~core.outfit_mask_with_fringe(a)
    c = v * s  # hsv -> rgb at hue 25 deg: r = v, b = v - c, g = b + c * 25 / 60
    for ch, val in enumerate((v, v - c + c * 25 / 60, v - c)):
        a[..., ch] = np.where(m, (val * 255).clip(0, 255), a[..., ch])
    return a, int(m.sum())


def body_frames(g):
    """{anim: [row][frame] cells} with walk re-centred so its skull sits on the idle skull x of the direction."""
    frames = {a: cells_of(g, a, n) for a, n in ANIMS}
    idle_x = [sheets_tool.head_anchor(frames["idle"][r][0])[0] for r in range(PAINTED)]
    for r in range(PAINTED):
        frames["walk"][r] = [shift_x(c, int(round(idle_x[r] - sheets_tool.head_anchor(c)[0]))) for c in frames["walk"][r]]
    return frames


def anchors_of(frames):
    """[(anim, dir, f, x, y, w)]: per-frame skull top, row-median skull width (raised bare arms can widen one frame's blob)."""
    out = []
    for anim, rows in frames.items():
        for r, row in enumerate(rows):
            got = [sheets_tool.head_anchor(c) for c in row]
            w = float(np.median([a[2] for a in got]))
            out += [(anim, r, f, a[0], a[1], w) for f, a in enumerate(got)]
    return out


def meta(dst, like):
    """Copy the import settings of a step-B test sheet with a fresh guid (sprite-rules: new sheets need the same .meta as their kind)."""
    text = (META_SRC / like).with_suffix(".png.meta").read_text(encoding="utf-8")
    m = Path(str(dst) + ".meta")
    guid = re.search(r"guid: (\w+)", m.read_text(encoding="utf-8")).group(1) if m.exists() else uuid.uuid4().hex
    m.write_text(re.sub(r"guid: \w+", "guid: " + guid, text, count=1), encoding="utf-8")


def folder_meta(d):
    m = Path(str(d) + ".meta")
    if not m.exists():
        m.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n"
                     "  userData: \n  assetBundleName: \n  assetBundleVariant: \n", encoding="utf-8")


def mkdirs(d):
    d.mkdir(parents=True, exist_ok=True)
    p = d
    while p != SPR.parent and p != DATA.parent:
        folder_meta(p)
        p = p.parent


def write_body(g):
    frames = body_frames(g)
    out = SPR / g / "body"
    mkdirs(out)
    for anim, rows in frames.items():
        n = len(rows[0])
        sheet = Image.new("RGBA", (CW * n, CH * PAINTED))
        for r, row in enumerate(rows):
            for f, c in enumerate(row):
                sheet.alpha_composite(c, (f * CW, r * CH))
        fixed, belt = belt_to_leather(sheet)
        grey, mask = core.to_game(fixed)
        Image.fromarray(grey).save(out / f"{anim}.png")
        Image.fromarray(mask).save(out / f"{anim}-mask.png")
        meta(out / f"{anim}.png", "idle")
        meta(out / f"{anim}-mask.png", "idle-mask")
        print(f"{g} {anim}: {n} frames, outfit px {(mask[..., 1] > 0).sum()}, wine belt px -> leather {belt}")
    head = [{"anim": a, "dir": d, "f": f, "x": x, "y": y, "w": w, "angle": 0} for a, d, f, x, y, w in anchors_of(frames)]
    mkdirs(DATA / g)
    body = {"note": "Layered base body (Tools/build-hero-layered-game-assets.py). Rows: down, down-right, right, up-right, up.",
            "cellW": CW, "cellH": CH, "feetY": FEET, "bodyPx": BODY_PX, "sheets": f"Sprites/hero-layered/{g}/body/",
            "anims": [{"name": a, "frames": n} for a, n in ANIMS], "head": head}
    (DATA / g / "body.json").write_text(json.dumps(body), encoding="utf-8")
    return frames


def latest_hair(cls):
    files = sorted(ART.glob(f"src/hair-{cls}-5dir-chatgpt-*.png"), key=lambda p: int(re.search(r"chatgpt-(\d+)", p.name).group(1)))
    return files[-1]


def hair_cells(cls, idle_cells):
    """5 hair cells (RGBA arrays) of a class, plus the fits for the report."""
    src_path = latest_hair(cls)
    src = core.key_green(Image.open(src_path))
    infos = [sheets_tool.head_anchor(c) for c in idle_cells]
    noms = hair.nominal(infos, idle_cells, (src.shape[1], src.shape[0]))
    out, fits = [], hair.fit_all(src, idle_cells, infos, noms)
    mids = [info[0] * fit[0] + fit[1] for info, fit in zip(infos, fits)]  # skull centres in source px
    lims = [((mids[i - 1] + mids[i]) / 2 if i else 0, (mids[i] + mids[i + 1]) / 2 if i < 4 else src.shape[1]) for i in range(5)]
    for i, (c, info, nom, fit) in enumerate(zip(idle_cells, infos, noms, fits)):
        only = hair.extract(src, c, info, fit, cls, lims[i])
        crown = ndimage.binary_dilation(sheets_tool.skull_mask(c), iterations=2)
        crown[int(info[1] + 0.3 * info[2]):] = False  # the top of the skull: every style covers it
        cell, dx, dy, grow, unc = hair.best_cut(only, fit, info, crown)
        al = cell[..., 3] > 20
        edge = al[0].any() or al[-1].any() or al[:, 0].any() or al[:, -1].any()
        print(f"{cls} dir {i}: S {fit[0]:.3f} (nominal {nom[2]:.3f}), hair px {al.sum()}, crown fix dx {dx} dy {dy} grow {grow} -> {unc} px bare{'  WARNING: touches the cell edge' if edge else ''}")
        out.append(cell)
    return out, src_path.name


def save_hair_strip(cls, strip):
    """Hair strip + its accessory strip (Mechanic goggles, Tools/hero_layered_hair_accessory.py); returns the accessory sheet path or None."""
    d = SPR / "hair" / cls
    mkdirs(d)
    strip, acc = accessory.split(strip, cls)
    Image.fromarray(strip).save(d / "0.png")
    meta(d / "0.png", "idle")
    if acc is None:
        return None
    Image.fromarray(acc).save(d / "0-acc.png")
    meta(d / "0-acc.png", "idle")
    return f"Sprites/hero-layered/hair/{cls}/0-acc"


def write_hair(cls, idle_cells):
    cells, name = hair_cells(cls, idle_cells)
    acc = save_hair_strip(cls, np.concatenate(cells, axis=1))
    mkdirs(DATA / cls)
    path = DATA / cls / "class.json"
    old = json.loads(path.read_text(encoding="utf-8")) if path.exists() else {}
    entry = {"style": 0, "sheet": f"Sprites/hero-layered/hair/{cls}/0", "cellW": hair.CELL_W, "cellH": hair.CELL_H,
             "ax": hair.AX, "ay": hair.AY, "refW": hair.REF_W}
    if acc:
        entry["accSheet"] = acc
    cj = {"note": f"Layered {cls} (Tools/build-hero-layered-game-assets.py, hair from {name}); hats: Data/hero-layered/headgear.json; "
                  "weapons: Tools/build-hero-layered-grips.py write.",
          "body": f"Data/hero-layered/{CLASSES[cls]}/body", "hair": [entry], "headgear": [],
          "weapons": old.get("weapons", []), "grips": old.get("grips", [])}  # weapon grips are written by another tool: keep them
    path.write_text(json.dumps(cj, indent=1), encoding="utf-8")


def split_accessory(cls):
    """Split the accessory out of the hair strip already installed (no hair re-fit); no-op when it was split before."""
    src = SPR / "hair" / cls / "0.png"
    strip = np.asarray(Image.open(src).convert("RGBA"))
    if accessory.split(strip, cls)[1] is None:
        print(cls, "no accessory left in", src.name)
        return
    acc = save_hair_strip(cls, strip)
    path = DATA / cls / "class.json"
    cj = json.loads(path.read_text(encoding="utf-8"))
    cj["hair"][0]["accSheet"] = acc
    path.write_text(json.dumps(cj, indent=1), encoding="utf-8")
    print(cls, "accessory ->", acc)


def main(cmd):
    if cmd == "write":
        idle = {}
        for g in ("male", "female"):
            idle[g] = [r[0] for r in write_body(g)["idle"]]
        for cls, g in CLASSES.items():
            write_hair(cls, idle[g])
    elif cmd == "accessory":
        split_accessory("mechanic")
    elif cmd == "check":
        import_module("hero_layered_compose_check").main(ROOT)
    else:
        print(__doc__)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "")

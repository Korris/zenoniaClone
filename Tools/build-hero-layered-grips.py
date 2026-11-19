"""Layered hero weapons on the body (2026-11-19): fists -> per-frame weapon grips + hand layer (spec docs/character-appearance-layered-spec.md,
holding rules in docs/sprite-rules.md "Quy tắc tư thế cầm").

  python Tools/build-hero-layered-grips.py fists    detect both fists on every frame (gender x anim x 5 painted dirs), apply OVERRIDES,
                                                    write Assets/ArtLibrary/hero-layered/sheets/<gender>/fists.json + marked sheets
                                                    docs/reference/hero-layered-weapons/fists-<gender>-<anim>.png (R red, L blue, hidden = hollow)
  python Tools/build-hero-layered-grips.py write    grips of the 5 classes (main + offhand, 8 Dir8 dirs, Tools/hero_layered_grip_poses.py)
                                                    into Data/hero-layered/<class>/class.json; hand layer Sprites/hero-layered/<gender>/body/hand/<anim>.png
  python Tools/build-hero-layered-grips.py check    contact sheets composed like the game (Tools/hero_layered_weapons_compose.py)
                                                    docs/reference/hero-layered-weapons/<class>-<anim>.png + rule check printout

Run `fists` after the body sheets change (build-hero-layered-game-assets.py write), then `write`, then `check`.
"""
import json
import re
import sys
import uuid
from importlib import import_module
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hero_layered_fists as hf  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
SPR = ROOT / "Assets/Resources/Sprites/hero-layered"
DATA = ROOT / "Assets/Resources/Data/hero-layered"
ART = ROOT / "Assets/ArtLibrary/hero-layered/sheets"
REF = ROOT / "docs/reference/hero-layered-weapons"
CW, CH, PAINTED = 320, 256, 5
ANIMS = [("idle", 1), ("walk", 4), ("attack1", 4), ("attack2", 4), ("attack3", 4)]
GENDERS = ["male", "female"]
BELT_Y = 166  # belt row of every idle row (read on a 3x crop); frames shift it with their skull top

# Frames the detector or the R / L rule gets wrong (read on the marked sheets): (gender, anim, painted row, frame) -> {hand: [x, y, visible]}
T, F = True, False
OVERRIDES = {
    # male: small fists half behind the body (not detected), R / L rule wrong on a lunge, low two-hand smash (under the boot cut)
    ("male", "walk", 1, 2): {"L": [188, 175, T]},
    ("male", "walk", 4, 0): {"R": [189, 175, T]},
    ("male", "walk", 4, 2): {"R": [190, 180, T]},
    ("male", "attack1", 1, 2): {"R": [202, 150, T], "L": [192, 179, T]},
    ("male", "attack1", 3, 0): {"R": [130, 138, T], "L": [194, 167, T]},  # wind-up: the right fist raised behind the head
    ("male", "attack2", 4, 1): {"R": [192, 174, T]},
    ("male", "attack2", 4, 2): {"L": [128, 170, T]},
    ("male", "attack3", 0, 2): {"R": [148, 205, T], "L": [172, 205, T]},
    ("male", "attack3", 1, 2): {"R": [171, 203, T], "L": [191, 202, T]},
    ("male", "attack3", 2, 2): {"R": [186, 207, T], "L": [180, 203, F]},
    ("male", "attack3", 2, 3): {"L": [160, 186, F]},
    ("male", "attack3", 3, 1): {"L": [205, 163, F]},
    ("male", "attack3", 3, 2): {"R": [190, 203, T], "L": [184, 200, F]},
    ("male", "attack3", 4, 1): {"R": [185, 155, T], "L": [170, 160, F]},
    ("male", "attack3", 4, 2): {"R": [165, 205, F], "L": [158, 205, F]},
    # female
    ("female", "walk", 1, 0): {"L": [187, 172, T]},
    ("female", "walk", 1, 2): {"L": [187, 172, T]},
    ("female", "walk", 2, 0): {"L": [185, 172, T]},
    ("female", "walk", 4, 0): {"R": [188, 165, T]},
    ("female", "attack1", 2, 1): {"R": [197, 149, T], "L": [186, 168, T]},
    ("female", "attack1", 3, 0): {"R": [134, 135, T], "L": [184, 153, T]},
    ("female", "attack1", 3, 1): {"R": [203, 152, T], "L": [185, 163, T]},
    ("female", "attack1", 3, 2): {"L": [180, 165, F]},
    ("female", "attack1", 3, 3): {"L": [134, 184, T]},
    ("female", "attack1", 4, 1): {"R": [195, 147, T]},
    ("female", "attack2", 1, 2): {"R": [187, 140, T], "L": [180, 162, T]},
    ("female", "attack2", 3, 0): {"L": [134, 178, T]},
    ("female", "attack2", 4, 1): {"L": [140, 160, F]},
    ("female", "attack2", 4, 2): {"L": [140, 160, F]},
}


def cells(g, anim, n):
    sh = Image.open(SPR / g / "body" / f"{anim}.png").convert("RGBA")
    return [[sh.crop((f * CW, r * CH, (f + 1) * CW, (r + 1) * CH)) for f in range(n)] for r in range(PAINTED)]


def heads(g):
    bj = json.loads((DATA / g / "body.json").read_text(encoding="utf-8"))
    return {(h["anim"], h["dir"], h["f"]): h for h in bj["head"]}


def fist_table(g):
    """{anim: [row][frame] {'R': [x, y, vis], 'L': [...]}} with OVERRIDES applied."""
    hd = heads(g)
    out = {}
    for anim, n in ANIMS:
        rows = []
        cs = cells(g, anim, n)
        for r in range(PAINTED):
            row, prev = [], None
            for f in range(n):
                h = hd[(anim, r, f)]
                belt = BELT_Y + max(-6, min(6, h["y"] - hd[("idle", r, 0)]["y"]))
                t = hf.assign(hf.fists(cs[r][f], belt), r, h["x"], prev)
                t.update({k: list(v) for k, v in OVERRIDES.get((g, anim, r, f), {}).items()})
                row.append({k: [round(v[0], 1), round(v[1], 1), bool(v[2])] for k, v in t.items()})
                prev = t
            rows.append(row)
        out[anim] = rows
    return out


def marks(g, table):
    REF.mkdir(parents=True, exist_ok=True)
    for anim, n in ANIMS:
        cs = cells(g, anim, n)
        img = Image.new("RGBA", (n * 220, PAINTED * 240), (200, 210, 190, 255))
        d = ImageDraw.Draw(img)
        for r in range(PAINTED):
            for f in range(n):
                img.alpha_composite(cs[r][f].crop((50, 10, 270, 250)), (f * 220, r * 240))
                for k, col in (("R", (230, 0, 0)), ("L", (0, 60, 255))):
                    x, y, vis = table[anim][r][f][k]
                    X, Y = f * 220 + x - 50, r * 240 + y - 10
                    d.ellipse((X - 4, Y - 4, X + 4, Y + 4), outline=col, fill=col if vis else None, width=2)
                    d.text((X + 6, Y - 6 + (8 if k == "L" else 0)), f"{k}{int(x)},{int(y)}", fill=col)
                d.text((f * 220 + 3, r * 240 + 3), f"r{r} f{f}", fill=(0, 0, 0))
        img.convert("RGB").save(REF / f"fists-{g}-{anim}.png")


def cmd_fists():
    for g in GENDERS:
        t = fist_table(g)
        (ART / g / "fists.json").write_text(json.dumps({"note": "Fists per painted row and frame (Tools/build-hero-layered-grips.py fists): "
                                                        "R = right hand (weapon), L = left hand; [x, y, visible] in cell px.", "anims": t}, indent=0),
                                            encoding="utf-8")
        marks(g, t)
        print(g, "fists written")


def load_fists(g):
    return json.loads((ART / g / "fists.json").read_text(encoding="utf-8"))["anims"]


def meta_like(dst, src_meta):
    text = Path(src_meta).read_text(encoding="utf-8")
    m = Path(str(dst) + ".meta")
    guid = re.search(r"guid: (\w+)", m.read_text(encoding="utf-8")).group(1) if m.exists() else uuid.uuid4().hex
    m.write_text(re.sub(r"guid: \w+", "guid: " + guid, text, count=1), encoding="utf-8")


def write_hands(g, table):
    """Hand layer: every visible glove blob near a table fist, cut from the body frame (same grid as the body sheet)."""
    out_dir = SPR / g / "body" / "hand"
    out_dir.mkdir(exist_ok=True)
    folder = Path(str(out_dir) + ".meta")
    if not folder.exists():
        folder.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n"
                          "  userData: \n  assetBundleName: \n  assetBundleVariant: \n", encoding="utf-8")
    hd = heads(g)
    for anim, n in ANIMS:
        cs = cells(g, anim, n)
        sheet = np.zeros((CH * PAINTED, CW * n, 4), np.uint8)
        for r in range(PAINTED):
            for f in range(n):
                c = cs[r][f]
                a = np.asarray(c)
                h = hd[(anim, r, f)]
                belt = BELT_Y + max(-6, min(6, h["y"] - hd[("idle", r, 0)]["y"]))
                m = np.zeros(a.shape[:2], bool)
                for x, y, _, blob in hf.fists(c, belt):
                    if any(v[2] and abs(v[0] - x) < 14 and abs(v[1] - y) < 14 for v in table[anim][r][f].values()):
                        m |= hf.hand_mask(c, blob)
                cell = np.zeros_like(a)
                cell[m] = a[m]
                sheet[r * CH:(r + 1) * CH, f * CW:(f + 1) * CW] = cell
        dst = out_dir / f"{anim}.png"
        Image.fromarray(sheet).save(dst)
        meta_like(dst, SPR / g / "body" / f"{anim}.png.meta")
    print(g, "hand layer written")


def cmd_write():
    poses = import_module("hero_layered_grip_poses")
    tables = {g: load_fists(g) for g in GENDERS}
    for g in GENDERS:
        write_hands(g, tables[g])
    for cls, g in poses.CLASS_GENDER.items():
        cj_path = DATA / cls / "class.json"
        cj = json.loads(cj_path.read_text(encoding="utf-8"))
        cj["weapons"], cj["grips"] = poses.class_grips(ROOT, cls, tables[g], heads(g), {a: cells(g, a, n) for a, n in ANIMS})
        cj["note"] = re.sub(r" TODO weapons:.*?;", ";", cj.get("note", "")) + " Weapons: Tools/build-hero-layered-grips.py write."
        cj_path.write_text(json.dumps(cj, indent=1), encoding="utf-8")
        print(cls, len(cj["grips"]), "grips")


def main(cmd):
    if cmd == "fists":
        cmd_fists()
    elif cmd == "write":
        cmd_write()
    elif cmd == "check":
        import_module("hero_layered_weapons_compose").main(ROOT)
    else:
        print(__doc__)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "")

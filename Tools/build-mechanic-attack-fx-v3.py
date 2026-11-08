"""Mechanic basic-attack VFX v3: class colour YELLOW (electric, brass sparks; docs/sprite-rules.md "Quy tắc VFX skill", user 2026-11-08).
Same 5 rows x 4 frames, same row meaning, same PICK / pivots / output names as v2 (Tools/build-mechanic-attack-fx-v2.py), so no code change
except VfxSprites preferring "mechanic-v3" (v2 stays as fallback). Old art is kept.
Source: Assets/ArtLibrary/classes/mechanic/vfx/attack-v3/<SRC_NAME> (ChatGPT Tay, layout ref = the v2 sheet in ref/), keyed on #FF00FF.
Each output row is scaled so its largest frame matches the v2 largest frame (MATCH_V2), so screen sizes and code constants
(MechanicCannonBlastFx.BoltArtUnits = proj 249 px, ConeAlong / ConeAcross) stay right.
`yellow_fix` pulls any leftover blue/cyan/red to the yellow-amber band (white cores and grey smoke untouched).
  python Tools/build-mechanic-attack-fx-v3.py cut       write Sprites/classes/mechanic/fx/attack-v3/*.png (+ .meta), Data/classes/mechanic/vfx-attack-v3.json,
                                                        fx/combat/muzzleFlash-yellow.png (hit-4 cannon core)
  python Tools/build-mechanic-attack-fx-v3.py preview   docs/reference/mechanic-attack-fx-yellow-preview.png (dark + snow, next to Warrior red and Wizard blue FX)
"""
import colorsys, importlib.util, json, os, sys
import numpy as np
from PIL import Image, ImageDraw

os.chdir("D:/zenoniaClone")
sys.path.insert(0, "Tools")
from wizard_attack_fx_common import to_image, cells, resize_rgba, trim, clone_meta

_spec = importlib.util.spec_from_file_location("fxv2", "Tools/build-mechanic-attack-fx-v2.py")
v2 = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(v2)

SRC_NAME = os.environ.get("MECH_FX_SRC", "mechanic-attack-fx-v3-chatgpt-1.png")
SRC = "Assets/ArtLibrary/classes/mechanic/vfx/attack-v3/" + SRC_NAME
OUT = "Assets/Resources/Sprites/classes/mechanic/fx/attack-v3"
V2_OUT = v2.OUT
DATA = v2.DATA
SHEET_ROWS = v2.SHEET_ROWS
PICK = v2.PICK
UNITS_PER_PX = v2.UNITS_PER_PX


def v2_sizes():
    """Largest (w, h) per row of the installed v2 set: the size target for v3."""
    meta = json.load(open(os.path.join(DATA, "vfx-attack-v2.json")))
    out = {}
    for f in meta["frames"]:
        row = f["name"].split("_")[0]
        w, h = out.get(row, (0, 0))
        out[row] = (max(w, f["w"]), max(h, f["h"]))
    return out


def yellow_fix(img):
    """Hue clamp to the Mechanic band: blue/cyan/violet -> yellow (same value, saturation), red/orange -> amber-yellow;
    greys (smoke), whites (cores) and dark browns (rocks) untouched."""
    a = np.asarray(img).astype(np.float64) / 255
    rgb = a[..., :3]
    mx, mn = rgb.max(-1), rgb.min(-1)
    v, s = mx, np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    flat = rgb.reshape(-1, 3)
    hsv = np.array([colorsys.rgb_to_hsv(*p) for p in flat]).reshape(rgb.shape)
    h = hsv[..., 0]
    vivid = (s > 0.25) & (v > 0.45)
    blue = vivid & (h > 0.42) & (h < 0.92)
    red = vivid & ((h >= 0.92) | (h < 0.10))
    h2 = h.copy()
    h2[blue] = 0.14
    h2[red] = 0.10 + (np.where(h[red] >= 0.92, 0.0, h[red])) * 0.3
    changed = blue | red
    out = a.copy()
    if changed.any():
        new = np.array([colorsys.hsv_to_rgb(hh, ss, vv) for hh, ss, vv in zip(h2[changed], hsv[..., 1][changed], hsv[..., 2][changed])])
        out[..., :3][changed] = new
    return Image.fromarray((out * 255).round().astype(np.uint8), "RGBA")


def build():
    """{(row, n): (PIL RGBA, (px, py from top))}, same pivot rules as v2."""
    pm = v2.key_magenta(SRC)
    boxes = cells(pm, rows=5, cols=4)
    raw = {}
    for i, (y0, y1, x0, x1) in enumerate(boxes):
        r, c = divmod(i, 4)
        raw[(SHEET_ROWS[r], c)] = yellow_fix(to_image(pm[y0:y1, x0:x1]))
    target = v2_sizes()
    out = {}
    for row in SHEET_ROWS:
        imgs = [trim(raw[(row, c)], pad=0)[0] for c in range(4)]
        big = max(imgs, key=lambda im: im.width)
        k = target[row][0] / float(big.width + 4)  # +4: trim pad 2 each side
        for n, c in enumerate(PICK[row]):
            img, _ = trim(resize_rgba(raw[(row, c)], k), pad=2)
            if row in ("proj", "bolt"):
                piv = v2.head_pivot(img)
            elif row == "cast":
                piv = v2.muzzle_pivot(img)
            else:
                piv = (img.width / 2.0, img.height / 2.0)
            out[(row, n)] = (img, piv)
    return out


def cut():
    os.makedirs(OUT, exist_ok=True)
    meta = {"frames": []}
    for (row, n), (img, (px, py)) in sorted(build().items()):
        name = "%s_%d" % (row, n)
        img.save(os.path.join(OUT, name + ".png"))
        clone_meta(os.path.join(V2_OUT, name + ".png.meta"), os.path.join(OUT, name + ".png.meta"))
        meta["frames"].append({"name": name, "w": img.width, "h": img.height, "px": round(px, 1), "py": round(img.height - py, 1)})
        print("%-9s %4d x %-4d pivot (%.1f, %.1f)" % (name, img.width, img.height, px, py))
    with open(os.path.join(DATA, "vfx-attack-v3.json"), "w", newline="\n") as f:
        json.dump(meta, f, indent=1)
    clone_meta(os.path.join(DATA, "vfx-attack-v2.json.meta"), os.path.join(DATA, "vfx-attack-v3.json.meta"))
    clone_meta(V2_OUT + ".meta", OUT + ".meta")
    print("wrote", OUT, "and", os.path.join(DATA, "vfx-attack-v3.json"))
    core()


CORE_SRC = "Assets/Resources/Sprites/classes/mechanic/fx/combat/muzzleFlash.png"        # white-blue star (old, kept)
CORE_OUT = "Assets/Resources/Sprites/classes/mechanic/fx/combat/muzzleFlash-yellow.png"  # hit 4 cannon core (MechanicCannonBlastFx.CorePath)


def core():
    """Yellow copy of the white-blue muzzle star drawn over the hit-4 cannon muzzle: blue/cyan -> yellow-amber, white kept."""
    img = yellow_fix(Image.open(CORE_SRC).convert("RGBA"))
    img.save(CORE_OUT)
    clone_meta(CORE_SRC + ".meta", CORE_OUT + ".meta")
    print("wrote", CORE_OUT)


def load_set(folder, json_path):
    meta = json.load(open(json_path))
    return {f["name"]: (Image.open(os.path.join(folder, f["name"] + ".png")).convert("RGBA"), (f["px"], f["h"] - f["py"])) for f in meta["frames"]}


def preview():
    """Rows: v3 cast / proj / bolt (x1.2) / blast (x2.6) / impact, then old v2 (same frames) for comparison, then other classes' FX
    (Wizard v3 blue, Warrior red slash) at game size, on a dark and a snow background."""
    new = build()
    old = load_set(V2_OUT, os.path.join(DATA, "vfx-attack-v2.json"))
    wiz = load_set("Assets/Resources/Sprites/classes/wizard/fx/attack-v3", "Assets/Resources/Data/classes/wizard/vfx-attack-v3.json")
    hero = Image.open("Assets/Resources/Sprites/classes/mechanic/body/bare/attack1.png").convert("RGBA").crop((192, 3 * 192, 384, 4 * 192))
    warrior = WARRIOR_FX()
    PPU = v2.PPU
    slot, rowh = int(26 * PPU), int(30 * PPU)
    W = slot * 11
    rows = ["cast", "proj", "bolt", "blast", "impact"]
    panels = []
    for bg in [(38, 44, 58, 255), (226, 234, 242, 255)]:
        nrows = len(rows) + 1
        c = Image.new("RGBA", (W, rowh * nrows), bg)
        d = ImageDraw.Draw(c)
        ink = (255, 255, 255, 255) if bg[0] < 128 else (0, 0, 0, 255)
        for r, row in enumerate(rows):
            y = r * rowh + rowh // 2
            c.alpha_composite(hero, (0, int(y - 150)))
            sc = 2.6 if row == "blast" else 1.2 if row == "bolt" else 1.0
            step = 1.7 if row == "blast" else 1.1
            d.text((4, r * rowh + 4), "v3 " + row + "   |   v2 (old)", fill=ink)
            x = slot * 1.1
            frames = [new[(row, n)] for n in range(4)] + [None, old["%s_%d" % (row, 2)]]
            for fr in frames:
                if fr is None:
                    x += slot * 0.6
                    continue
                img, pv = fr
                wpx = img.width * PPU * UNITS_PER_PX * sc
                v2.draw_fx(c, img, pv, (x + pv[0] * PPU * UNITS_PER_PX * sc, y), sc)
                x += wpx + 12
        y = len(rows) * rowh + rowh // 2
        d.text((4, len(rows) * rowh + 4), "compare: Mechanic v3 impact | Wizard v3 impact + proj (blue) | Warrior slash (red)", fill=ink)
        img, pv = new[("impact", 2)]
        v2.draw_fx(c, img, pv, (slot * 1.5, y))
        img, pv = wiz["impact_2"]
        v2.draw_fx(c, img, pv, (slot * 3.6, y))
        img, pv = wiz["proj_1"]
        v2.draw_fx(c, img, pv, (slot * 5.4, y))
        if warrior is not None:
            wimg = warrior.resize((int(warrior.width * 0.5), int(warrior.height * 0.5)), Image.LANCZOS)
            c.alpha_composite(wimg, (int(slot * 6.5), int(y - wimg.height / 2)))
        panels.append(c)
    out = Image.new("RGBA", (W, panels[0].height * 2))
    for i, p in enumerate(panels):
        out.paste(p, (0, i * p.height))
    out.convert("RGB").save("docs/reference/mechanic-attack-fx-yellow-preview.png")
    print("wrote docs/reference/mechanic-attack-fx-yellow-preview.png", out.size)


def WARRIOR_FX():
    """One frame of the Warrior's red slash art for colour comparison (None when not found)."""
    import glob
    for pat in ["Assets/Resources/Sprites/classes/warrior/fx/attack/*.png", "Assets/Resources/Sprites/classes/warrior/fx/*/*.png"]:
        files = sorted(glob.glob(pat))
        for f in files:
            im = Image.open(f).convert("RGBA")
            a = np.asarray(im).astype(np.float64)
            vis = a[..., 3] > 128
            if vis.sum() > 2000 and (a[..., 0][vis].mean() > a[..., 2][vis].mean() + 40):
                return im
    return None


if __name__ == "__main__":
    {"cut": cut, "preview": preview}[sys.argv[1] if len(sys.argv) > 1 else "preview"]()

"""Warrior (Starward Knight V3) facing-camera bone rig, TEST (25/11, plans/261125-0045-warrior-skeletal-rig-prototype).

  python Tools/build-warrior-rig-down.py rest OUT.png         rest pose next to the approved idle-down cell (3x)
  python Tools/build-warrior-rig-down.py write                part sprites + .meta, Data/.../rig-down.json
  python Tools/build-warrior-rig-down.py preview OUT_DIR      offline frames (60 fps) of idle / walk / attack, GIFs + contact sheets,
                                                              same forward kinematics as HeroRigPose.cs (hair dyed roughly, sword, shield)

Parts: Tools/warrior_rig_down_parts.py (cut + scale), layout / bones: warrior_rig_down_layout.py, keyframes: warrior_rig_down_anims.py.
Game side: HeroRigSet.cs, HeroRigPose.cs, HeroRigView.cs, flag `--hero-rig`.
"""
import json
import math
import os
import sys
import uuid

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import warrior_rig_down_anims as A  # noqa: E402
import warrior_rig_down_layout as L  # noqa: E402
import warrior_rig_down_parts as P  # noqa: E402

SPRITES = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/rig-down/"
SHEETS_RES = "Sprites/hero-layered/ro-fantasy/warrior/rig-down/"
JSON_OUT = "Assets/Resources/Data/hero-layered/ro-fantasy/warrior/rig-down.json"
META_TEMPLATE = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/body/idle.png.meta"
HAIR = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/hair/{}.png"
SWORD = ("Assets/Resources/Sprites/hero-layered/weapons/sword.png", 26.0, 118.0)
SHIELD = ("Assets/Resources/Sprites/hero-layered/weapons/warrior-shield.png", 38.0, 54.0)


def placed_parts():
    return L.placed(P.all_parts(scale=L.SCALE))


def rig_json(pl):
    bones = [{"name": n, "parent": par, "x": float(j[0]), "y": float(j[1])} for n, par, j, _lag in L.BONES]
    parts = []
    for z, n in enumerate(L.ORDER):
        _rgba, left, top = pl[n]
        parts.append({"name": n, "bone": L.PART_BONE[n], "sprite": n, "left": float(left), "top": float(top), "z": z, "hand": n in L.HAND_PARTS})
    return {"note": "Warrior facing-camera bone rig TEST (Tools/build-warrior-rig-down.py; flag --hero-rig). L = screen left = sword side.",
            "cellW": L.CELL_W, "cellH": L.CELL_H, "feetY": L.FEET_Y, "bodyPx": L.BODY_PX, "sheets": SHEETS_RES,
            "bones": bones, "parts": parts, "hair": L.HAIR, "weapon": L.WEAPON, "offhand": L.OFFHAND, "anims": A.all_anims()}


def write_meta(png):
    meta = png + ".meta"
    if os.path.exists(meta):
        return
    lines = open(META_TEMPLATE, encoding="utf-8").read().splitlines()
    lines = ["guid: " + uuid.uuid4().hex if ln.startswith("guid:") else ln for ln in lines]
    open(meta, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")


def cmd_write():
    pl = placed_parts()
    os.makedirs(SPRITES, exist_ok=True)
    for n, (rgba, _l, _t) in pl.items():
        path = SPRITES + n + ".png"
        Image.fromarray(rgba).save(path)
        write_meta(path)
    folder_meta = SPRITES.rstrip("/") + ".meta"
    if not os.path.exists(folder_meta):
        open(folder_meta, "w", newline="\n").write("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)
    j = rig_json(pl)
    with open(JSON_OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(j, f, indent=1)
    write_meta_text(JSON_OUT)
    print("wrote", len(pl), "parts,", JSON_OUT)


def write_meta_text(path):
    meta = path + ".meta"
    if not os.path.exists(meta):
        open(meta, "w", newline="\n").write("fileFormatVersion: 2\nguid: %s\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)


# ---------- offline forward kinematics (mirrors HeroRigPose.cs) ----------

def ease(e, u, pm, p0, p1, pn):
    if e == 1:
        return p0 + (p1 - p0) * u
    if e == 2:
        return p0 + (p1 - p0) * u * u
    if e == 3:
        return p0 + (p1 - p0) * (1 - (1 - u) ** 2)
    if e == 4:
        return p0
    u2, u3 = u * u, u * u * u
    return 0.5 * (2 * p0 + (-pm + p1) * u + (2 * pm - 5 * p0 + 4 * p1 - pn) * u2 + (-pm + 3 * p0 - 3 * p1 + pn) * u3)


def segment(keys, p):
    i = 0
    while i + 1 < len(keys) and keys[i + 1]["t"] <= p:
        i += 1
    return i


def neighbour(anim, i):
    k = anim["keys"]
    return k[i % len(k)] if anim["loop"] else k[max(0, min(len(k) - 1, i))]


def chan(key, bone, c):
    for b in key["b"]:
        if b["n"] == bone:
            return b[c]
    return 1.0 if c in ("sx", "sy") else 0.0


def sample(anim, p, getter):
    k = anim["keys"]
    i = segment(k, p)
    j = i + 1 if i + 1 < len(k) else (0 if anim["loop"] else i)
    if i == j:
        u = 0.0
    else:
        t0, t1 = k[i]["t"], k[j]["t"] + (1.0 if j < i else 0.0)
        pp = p + (1.0 if p < t0 else 0.0)
        u = min(1.0, max(0.0, (pp - t0) / (t1 - t0))) if t1 > t0 else 0.0
    return ease(k[i]["e"], u, getter(neighbour(anim, i - 1)), getter(k[i]), getter(k[j]), getter(neighbour(anim, j + 1)))


def rot(v, deg):
    r = math.radians(deg)
    c, s = math.cos(r), math.sin(r)
    return (v[0] * c - v[1] * s, v[0] * s + v[1] * c)


def evaluate(rig, anim, phase):
    lags = {l["n"]: l["v"] for l in anim.get("lags", [])}
    pose = {}
    names = [b["name"] for b in rig["bones"]]
    for b in rig["bones"]:
        n = b["name"]
        p = phase - lags.get(n, 0.0)
        p = p - math.floor(p) if anim["loop"] else min(1.0, max(0.0, p))
        v = {c: sample(anim, p, lambda k, c=c: chan(k, n, c)) for c in ("a", "x", "y", "sx", "sy")}
        if b["parent"]:
            pb = next(x for x in rig["bones"] if x["name"] == b["parent"])
            pp = pose[b["parent"]]
            local = (b["x"] - pb["x"] + v["x"], b["y"] - pb["y"] + v["y"])
            d = rot(local, pp["ang"])
            pose[n] = {"pos": (pp["pos"][0] + d[0], pp["pos"][1] + d[1]), "ang": pp["ang"] + v["a"], "s": (v["sx"], v["sy"])}
        else:
            pose[n] = {"pos": (b["x"] + v["x"], b["y"] + v["y"]), "ang": v["a"], "s": (v["sx"], v["sy"])}
    q = phase - math.floor(phase) if anim["loop"] else min(1.0, max(0.0, phase))
    k0 = anim["keys"][segment(anim["keys"], q)]
    wrist = sample(anim, q, lambda k: k["wa"])
    return pose, wrist, k0["wb"], names


def carry(rig, pose, bone, x, y):
    b = next(v for v in rig["bones"] if v["name"] == bone)
    d = rot((x - b["x"], y - b["y"]), pose[bone]["ang"])
    p = pose[bone]["pos"]
    return (p[0] + d[0], p[1] + d[1])


SS = 3  # supersampling of the offline renderer


def paste(canvas, img, pivot, pos, ang, scale=(1.0, 1.0)):
    """img (PIL RGBA) with its pivot (px) at pos (cell px), turned ang deg clockwise, scaled (sx, sy) in its own frame."""
    sx, sy = scale
    r = math.radians(ang)
    c, s = math.cos(r), math.sin(r)
    # forward: q = pos + R * S * (p - pivot)   ->   inverse for PIL (output -> input), at SS
    a, b_, d, e = c * sx, -s * sy, s * sx, c * sy
    det = a * e - b_ * d
    ia, ib, id_, ie = e / det, -b_ / det, -d / det, a / det
    px, py = pos[0] * SS, pos[1] * SS
    big = img.resize((img.width * SS, img.height * SS), Image.BILINEAR)
    cx, cy = pivot[0] * SS, pivot[1] * SS
    coeffs = (ia, ib, cx - ia * px - ib * py, id_, ie, cy - id_ * px - ie * py)
    layer = big.transform(canvas.size, Image.AFFINE, coeffs, resample=Image.BILINEAR)
    canvas.alpha_composite(layer)


def dyed_hair(style=3):
    """Rough preview of the dye: grey -> gold ramp (the game uses HeroRecolor Ramp5 + palettes.json)."""
    im = Image.open(HAIR.format(style)).convert("RGBA").crop((0, 0, 288, 320))
    a = np.array(im).astype(np.float32)
    g = a[..., :3].mean(axis=2) / 255.0
    lo, hi = np.array([70, 40, 20.0]), np.array([255, 236, 150.0])
    a[..., :3] = lo + (hi - lo) * g[..., None]
    return Image.fromarray(a.astype(np.uint8))


def render(rig, sprites, hair, anim, phase):
    pose, wrist, wb, _ = evaluate(rig, anim, phase)
    can = Image.new("RGBA", (L.CELL_W * SS, L.CELL_H * SS), (0, 0, 0, 0))
    sword, sgx, sgy = SWORD
    shield, hgx, hgy = SHIELD
    bone_of = {p["name"]: p for p in rig["parts"]}

    def part(n):
        p = bone_of[n]
        b = next(v for v in rig["bones"] if v["name"] == p["bone"])
        img = sprites[n]
        pv = (b["x"] - p["left"], b["y"] - p["top"])
        bp = pose[p["bone"]]
        paste(can, img, pv, bp["pos"], bp["ang"], bp["s"])

    def weapon():
        w = rig["weapon"]
        paste(can, Image.open(sword).convert("RGBA"), (sgx, sgy), carry(rig, pose, w["bone"], w["x"], w["y"]),
              w["angle"] + pose[w["bone"]]["ang"] + wrist)

    if wb:
        weapon()
    for n in [p["name"] for p in sorted(rig["parts"], key=lambda q: q["z"])]:
        if n == "forearmL":
            continue
        part(n)
        if n == "head":
            h = rig["hair"]
            k = h["w"] / 76.0
            paste(can, hair, (144, 64), carry(rig, pose, "head", h["x"], h["y"]), pose["head"]["ang"], (k, k))
    if not wb:
        weapon()
    part("forearmL")
    o = rig["offhand"]
    paste(can, Image.open(shield).convert("RGBA"), (hgx, hgy), carry(rig, pose, o["bone"], o["x"], o["y"]), o["angle"] + pose[o["bone"]]["ang"])
    return can.resize((L.CELL_W, L.CELL_H), Image.LANCZOS)


def cmd_preview(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    pl = placed_parts()
    rig = rig_json(pl)
    sprites = {n: Image.fromarray(v[0]) for n, v in pl.items()}
    hair = dyed_hair(3)
    for anim in rig["anims"]:
        n = max(2, int(round(anim["ms"] / (1000 / 60))))
        frames = []
        for i in range(n + (0 if anim["loop"] else 1)):
            ph = i / n
            f = render(rig, sprites, hair, anim, ph)
            bg = Image.new("RGBA", f.size, (92, 128, 84, 255))
            bg.alpha_composite(f)
            frames.append(bg.crop((60, 0, 260, 256)))
        big = [f.resize((f.width * 2, f.height * 2), Image.NEAREST).convert("RGB") for f in frames]
        big[0].save(os.path.join(out_dir, "rig-%s.gif" % anim["name"]), save_all=True, append_images=big[1:], duration=int(1000 / 60), loop=0)
        step = max(1, len(frames) // 12)
        pick = frames[::step]
        sheet = Image.new("RGB", (200 * len(pick), 256), (92, 128, 84))
        for i, f in enumerate(pick):
            sheet.paste(f.convert("RGB"), (200 * i, 0))
        sheet.save(os.path.join(out_dir, "rig-%s-sheet.png" % anim["name"]))
        print(anim["name"], len(frames), "frames")


def cmd_rest(out):
    pl = placed_parts()
    cell = Image.new("RGBA", (L.CELL_W, L.CELL_H), (0, 0, 0, 0))
    for n in L.ORDER:
        rgba, left, top = pl[n]
        cell.alpha_composite(Image.fromarray(rgba), (left, top))
    idle = Image.open(L.IDLE).convert("RGBA").crop((0, 0, L.CELL_W, L.CELL_H))
    o = Image.new("RGBA", (L.CELL_W * 2, L.CELL_H), (92, 128, 84, 255))
    o.alpha_composite(idle, (0, 0))
    o.alpha_composite(cell, (L.CELL_W, 0))
    o.crop((80, 15, 560, 250)).resize((960, 470), Image.NEAREST).save(out)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "write":
        cmd_write()
    elif cmd == "preview":
        cmd_preview(sys.argv[2])
    elif cmd == "rest":
        cmd_rest(sys.argv[2])
    else:
        print(__doc__)

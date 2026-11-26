"""Offline renderer of the Warrior MESH rig (linear blend skinning, as SpriteSkin / HeroMeshRigCpuSkin do in the game).

Triangles are rasterised at SS x supersampling (barycentric inverse mapping, nearest texel), then downsampled. Draw order like the
game (HeroMeshRigView): cape, arms flagged behind (lb / rb), body, hair, arms, sword behind / in front, shield.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw

import warrior_rig_mesh_pose as P
import warrior_rig_mesh_skeleton as K

SS = 3
HAIR_SHEET = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/hair/{}.png"
SWORD = ("Assets/Resources/Sprites/hero-layered/weapons/sword.png", 26.0, 118.0)
SHIELD = ("Assets/Resources/Sprites/hero-layered/weapons/warrior-shield.png", 38.0, 54.0)
BG = (92, 128, 84, 255)


def skin(pose, m):
    out = np.zeros_like(m["verts"], dtype=np.float64)
    for k in range(4):
        w = m["bw"][:, k]
        for i, (x, y) in enumerate(m["verts"]):
            if w[i] > 0:
                q = pose.affine(int(m["bi"][i, k]), (float(x), float(y)))
                out[i, 0] += w[i] * q[0]
                out[i, 1] += w[i] * q[1]
    return out


def raster(canvas, tex, uv, pos, tris):
    """canvas: H x W x 4 float (premultiplied-free 'over'); uv: texel coords; pos: posed cell px."""
    H, W = canvas.shape[:2]
    P_ = pos * SS
    th, tw = tex.shape[:2]
    for a, b, c in tris:
        x0, y0 = np.floor(np.minimum(np.minimum(P_[a], P_[b]), P_[c])).astype(int)
        x1, y1 = np.ceil(np.maximum(np.maximum(P_[a], P_[b]), P_[c])).astype(int)
        x0, y0, x1, y1 = max(0, x0), max(0, y0), min(W, x1 + 1), min(H, y1 + 1)
        if x1 <= x0 or y1 <= y0:
            continue
        ys, xs = np.mgrid[y0:y1, x0:x1]
        px, py = xs + 0.5, ys + 0.5
        (ax, ay), (bx, by), (cx, cy) = P_[a], P_[b], P_[c]
        det = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(det) < 1e-9:
            continue
        l1 = ((by - cy) * (px - cx) + (cx - bx) * (py - cy)) / det
        l2 = ((cy - ay) * (px - cx) + (ax - cx) * (py - cy)) / det
        l3 = 1 - l1 - l2
        inside = (l1 >= -1e-4) & (l2 >= -1e-4) & (l3 >= -1e-4)
        if not inside.any():
            continue
        u = l1 * uv[a, 0] + l2 * uv[b, 0] + l3 * uv[c, 0]
        v = l1 * uv[a, 1] + l2 * uv[b, 1] + l3 * uv[c, 1]
        ui = np.clip(u.astype(int), 0, tw - 1)
        vi = np.clip(v.astype(int), 0, th - 1)
        src = tex[vi, ui].astype(np.float64) / 255.0
        al = src[..., 3] * inside
        dst = canvas[y0:y1, x0:x1]
        dst[..., :3] = src[..., :3] * al[..., None] + dst[..., :3] * (1 - al[..., None])
        dst[..., 3] = al + dst[..., 3] * (1 - al)


def paste(canvas_img, img, pivot, pos, ang, scale=(1.0, 1.0)):
    sx, sy = scale
    r = math.radians(ang)
    c, s = math.cos(r), math.sin(r)
    a, b_, d, e = c * sx, -s * sy, s * sx, c * sy
    det = a * e - b_ * d
    ia, ib, id_, ie = e / det, -b_ / det, -d / det, a / det
    px, py = pos[0] * SS, pos[1] * SS
    big = img.resize((img.width * SS, img.height * SS), Image.BILINEAR)
    cx, cy = pivot[0] * SS, pivot[1] * SS
    layer = big.transform(canvas_img.size, Image.AFFINE, (ia, ib, cx - ia * px - ib * py, id_, ie, cy - id_ * px - ie * py), resample=Image.BILINEAR)
    canvas_img.alpha_composite(layer)


def dyed_hair(style=2):
    im = Image.open(HAIR_SHEET.format(style)).convert("RGBA").crop((0, 0, 288, 320))
    a = np.array(im).astype(np.float32)
    g = a[..., :3].mean(axis=2) / 255.0
    lo, hi = np.array([70, 40, 20.0]), np.array([255, 236, 150.0])
    a[..., :3] = lo + (hi - lo) * g[..., None]
    return Image.fromarray(a.astype(np.uint8))


def frame(rig, ms, pose, hair, gear=True):
    f = pose.flags
    swaps = dict(s.split("=") for s in f["sw"].split(",") if s)
    by_slot = {}
    for n, slot, tex, m in ms:
        want = slot + "@" + swaps[slot] if slot in swaps else slot
        if n == want or (slot not in by_slot and n == slot):
            by_slot[slot] = (tex, m)
    can = np.zeros((rig["cellH"] * SS, rig["cellW"] * SS, 4))

    def mesh(slot):
        tex, m = by_slot[slot]
        raster(can, tex, m.get("uv", m["verts"]), skin(pose, m), m["tris"])

    def flush():
        return Image.fromarray((np.clip(can, 0, 1) * 255).astype(np.uint8))

    def sword(img):
        w = rig["weapon"]
        b = K.BONE_INDEX[w["bone"]]
        paste(img, Image.open(SWORD[0]).convert("RGBA"), SWORD[1:], pose.affine(b, (w["x"], w["y"])), w["angle"] + pose.turn[b] + f["wa"])

    if not gear:   # bare body only (measurements): cape, body, arms
        for slot in ("cape", "body", "armR", "armL"):
            mesh(slot)
        return flush().resize((rig["cellW"], rig["cellH"]), Image.LANCZOS)
    mesh("cape")
    for s, flag in (("armL", "lb"), ("armR", "rb")):
        if f[flag]:
            mesh(s)
    img = flush()
    if f["wb"]:
        sword(img)
    can = np.array(img).astype(np.float64) / 255.0
    mesh("body")
    img = flush()
    h = rig["hair"]
    hb = K.BONE_INDEX[h["bone"]]
    k = h["w"] / 76.0
    paste(img, hair, (144, 64), pose.affine(hb, (h["x"], h["y"])), pose.turn[hb], (k, k))
    can = np.array(img).astype(np.float64) / 255.0
    for s, flag in (("armR", "rb"), ("armL", "lb")):
        if not f[flag]:
            mesh(s)
    img = flush()
    if not f["wb"]:
        sword(img)
    o = rig["offhand"]
    ob = K.BONE_INDEX[o["bone"]]
    paste(img, Image.open(SHIELD[0]).convert("RGBA"), SHIELD[1:], pose.affine(ob, (o["x"], o["y"])), o["angle"] + pose.turn[ob])
    return img.resize((rig["cellW"], rig["cellH"]), Image.LANCZOS)


def preview(rig, ms, out_dir, only=None):
    os.makedirs(out_dir, exist_ok=True)
    hair = dyed_hair()
    for anim in rig["anims"]:
        if only and anim["name"] != only:
            continue
        n = max(2, int(round(anim["ms"] / (1000 / 60))))
        pose = P.Pose()
        if anim["loop"]:
            for i in range(n):           # warm the springs up one cycle
                pose.evaluate(anim, i / n, 1 / 60)
        frames = []
        for i in range(n + (0 if anim["loop"] else 12)):
            pose.evaluate(anim, i / n, 1 / 60)
            f = frame(rig, ms, pose, hair)
            bg = Image.new("RGBA", f.size, BG)
            bg.alpha_composite(f)
            frames.append(bg.crop((60, 0, 260, 256)))
        big = [fr.resize((fr.width * 2, fr.height * 2), Image.NEAREST).convert("RGB") for fr in frames]
        big[0].save(os.path.join(out_dir, "mesh-%s.gif" % anim["name"]), save_all=True, append_images=big[1:], duration=int(1000 / 60), loop=0)
        step = max(1, len(frames) // 12)
        pick = frames[::step]
        sheet = Image.new("RGB", (200 * len(pick), 256), BG[:3])
        for i, fr in enumerate(pick):
            sheet.paste(fr.convert("RGB"), (200 * i, 0))
        sheet.save(os.path.join(out_dir, "mesh-%s-strip.png" % anim["name"]))
        for i, fr in enumerate(frames):
            fr.convert("RGB").save(os.path.join(out_dir, "%s-%03d.png" % (anim["name"], i)))
        print(anim["name"], len(frames), "frames")


def wireframe(ms, out):
    tiles = []
    colours = [(255, 80, 80), (80, 255, 80), (80, 80, 255), (255, 255, 0), (255, 0, 255), (0, 255, 255), (255, 160, 0), (160, 0, 255)]
    for n, _slot, tex, m in ms:
        im = Image.new("RGBA", (320, 256), BG)
        im.alpha_composite(Image.fromarray(tex))
        im = im.resize((960, 768), Image.NEAREST)
        d = ImageDraw.Draw(im)
        uv = m.get("uv", m["verts"])
        for a, b, c in m["tris"]:
            pts = [tuple(uv[i] * 3) for i in (a, b, c)]
            d.polygon(pts, outline=(255, 255, 255, 70))
        for i, (x, y) in enumerate(uv):
            top = int(m["bi"][i, 0])
            col = colours[top % len(colours)]
            d.ellipse([x * 3 - 2, y * 3 - 2, x * 3 + 2, y * 3 + 2], fill=col + (int(255 * m["bw"][i, 0]),))
        d.text((6, 6), n, fill=(255, 255, 255, 255))
        tiles.append(im.crop((240, 0, 720, 768)))
    sheet = Image.new("RGBA", (480 * len(tiles), 768))
    for i, t in enumerate(tiles):
        sheet.paste(t, (480 * i, 0))
    sheet.save(out)

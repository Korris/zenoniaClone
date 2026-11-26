"""Motion amplitudes of the Warrior facing-camera body: approved frame art (walk / attack row 0, 8 frames) vs the MESH rig's offline render.
Target numbers for the mesh keys come from here, not from guesses (25/11, coordinator: "lấy số đo thật từ art frame").

  python Tools/measure-warrior-down-motion-amplitude.py            prints a table (cell px, y down) for frames and mesh

Per frame: boot bottoms (left / right half, lowest opaque y), head top y and head centre x (rows top..top+40), chest centre x
(rows top+78..top+92), glove centres (brown leather pixels, left / right cluster), belt y (row with most gold/brown belt pixels).
"""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
BODY = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/body/{}.png"


def cells(anim):
    im = np.array(Image.open(BODY.format(anim)).convert("RGBA"))
    n = im.shape[1] // 320
    return [im[0:256, i * 320:(i + 1) * 320] for i in range(n)]


def metrics(a):
    al = a[..., 3] > 40
    ys, xs = np.where(al)
    top = ys.min()
    head = al[top:top + 40]
    hx = np.where(head)[1].mean()
    chest = al[top + 78:top + 92]
    cx = np.where(chest)[1].mean()
    low = al.copy()
    low[:190] = False
    fl = np.where(low[:, :160].any(axis=1))[0].max()
    fr = np.where(low[:, 160:].any(axis=1))[0].max()
    rgb = a[..., :3].astype(int)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    brown = al & (r > 70) & (r < 165) & (g > 40) & (g < 110) & (b > 15) & (b < 80) & (r - b > 45)
    brown[:top + 85] = False                       # gloves are below the chest (idle / walk); attack raises them: see 'raised'
    gy, gx = np.where(brown)
    gl = (gx[gx < 160].mean(), gy[gx < 160].mean()) if (gx < 160).sum() > 20 else (np.nan, np.nan)
    gr = (gx[gx >= 160].mean(), gy[gx >= 160].mean()) if (gx >= 160).sum() > 20 else (np.nan, np.nan)
    return {"top": top, "hx": hx, "cx": cx, "fl": fl, "fr": fr, "glx": gl[0], "gly": gl[1], "grx": gr[0], "gry": gr[1]}


def table(name, frames):
    ms = [metrics(f) for f in frames]
    print("\n" + name)
    print(" f  top    hx     cx    footL footR  gloveL(x,y)   gloveR(x,y)")
    for i, m in enumerate(ms):
        print("%2d %4d %6.1f %6.1f %5d %5d  %5.1f,%5.1f  %5.1f,%5.1f" % (i, m["top"], m["hx"], m["cx"], m["fl"], m["fr"], m["glx"], m["gly"], m["grx"], m["gry"]))
    rng = lambda k: np.nanmax([m[k] for m in ms]) - np.nanmin([m[k] for m in ms])
    print("range: head top %.1f, head x %.1f, chest x %.1f, footL %.1f, footR %.1f, gloveL x %.1f y %.1f, gloveR x %.1f y %.1f" % (
        rng("top"), rng("hx"), rng("cx"), rng("fl"), rng("fr"), rng("glx"), rng("gly"), rng("grx"), rng("gry")))
    return ms


def mesh_frames(anim_name, n):
    import importlib
    build = importlib.import_module("build-warrior-rig-mesh-down")
    import warrior_rig_mesh_pose as P
    import warrior_rig_mesh_render as V
    ms = build.meshes()
    rig = build.rig_json(ms)
    anim = next(a for a in rig["anims"] if a["name"] == anim_name)
    pose = P.Pose()
    out = []
    steps = max(2, int(round(anim["ms"] / (1000 / 60))))
    if anim["loop"]:
        for i in range(steps):
            pose.evaluate(anim, i / steps, 1 / 60)
    for i in range(n):
        ph = i / n if anim["loop"] else min(1.0, i / (n - 1))
        # step the springs up to this phase at 60 fps
        pose.evaluate(anim, ph, 1 / 60)
        out.append(np.array(V.frame(rig, ms, pose, None, gear=False)))
    return out


if __name__ == "__main__":
    table("FRAME ART idle", cells("idle")[:1])
    table("FRAME ART walk (8 f / 600 ms)", cells("walk"))
    table("FRAME ART attack (8 f / 400 ms)", cells("attack"))
    if "--mesh" in sys.argv:
        table("MESH walk (8 samples)", mesh_frames("walk", 8))
        table("MESH attack (16 samples)", mesh_frames("attack", 16))

"""Warrior (Starward Knight V3) facing-camera MESH rig with bone weights, TEST (25/11, Phase 2 of
plans/261125-0045-warrior-skeletal-rig-prototype; game flag `--hero-rig-mesh`, the cut-out rig stays on `--hero-rig`).

  python Tools/build-warrior-rig-mesh-down.py write            mesh textures + .meta, Data/.../rig-mesh-down.json
  python Tools/build-warrior-rig-mesh-down.py preview OUT_DIR [anim]  offline skinned frames (60 fps) of idle / walk / attack: GIFs + strips
  python Tools/build-warrior-rig-mesh-down.py mesh OUT.png     wireframe + weight colours of every mesh (check the joints)

Modules: warrior_rig_mesh_regions (textures), _skeleton (bones + weight rules by bone name), _geometry (grid mesh + weights),
_anims (keys), _pose (evaluation, mirror of HeroMeshRigPose.cs), _swap (extra painted piece for the sprite swap at the blow),
_render (offline skinning). Game side: HeroMeshRigSet.cs, HeroMeshRigPose.cs, HeroMeshRigView.cs (SpriteSkin, CPU fallback).
"""
import json
import os
import sys
import uuid

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import warrior_rig_mesh_anims as A  # noqa: E402
import warrior_rig_mesh_geometry as G  # noqa: E402
import warrior_rig_mesh_regions as R  # noqa: E402
import warrior_rig_mesh_render as V  # noqa: E402
import warrior_rig_mesh_skeleton as K  # noqa: E402
import warrior_rig_mesh_swap as W  # noqa: E402

SPRITES = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/rig-mesh-down/"
SHEETS_RES = "Sprites/hero-layered/ro-fantasy/warrior/rig-mesh-down/"
JSON_OUT = "Assets/Resources/Data/hero-layered/ro-fantasy/warrior/rig-mesh-down.json"
META_TEMPLATE = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/body/idle.png.meta"
ORDER = ["cape", "body", "armR", "armL"]          # back to front at rest (z index)

HAIR = {"bone": "hair", "x": 158.5, "y": 27, "w": 70.0, "angle": 0.0}
WEAPON = {"bone": "forearmL", "x": 116.0, "y": 162.0, "w": 0.0, "angle": -131.4}   # idle-down grip of the frame art
OFFHAND = {"bone": "forearmR", "x": 201.0, "y": 162.0, "w": 0.0, "angle": 0.0}


def meshes():
    """[(name, slot, rgba texture, mesh dict)] base meshes + swap variants."""
    out = []
    for n, (tex, cover) in R.textures().items():
        out.append((n, n, tex, G.build(n, cover)))
    out += W.variants()
    order = {n: i for i, n in enumerate(ORDER)}
    out.sort(key=lambda m: (order[m[1]], m[0] != m[1]))
    return out


def rig_json(ms):
    bones = [{"name": n, "parent": p, "x": float(j[0]), "y": float(j[1]), "ex": float(e[0]), "ey": float(e[1]), "spring": bool(s)}
             for n, p, j, e, s in K.BONES]
    mj = []
    for n, slot, _tex, m in ms:
        mj.append({"name": n, "slot": slot, "sprite": n.replace("@", "-"), "z": ORDER.index(slot),
                   "v": [round(float(c), 2) for c in m["verts"].reshape(-1)], "t": [int(c) for c in m["tris"].reshape(-1)],
                   "u": [round(float(c), 2) for c in m["uv"].reshape(-1)] if "uv" in m else [],
                   "bi": [int(c) for c in m["bi"].reshape(-1)], "bw": [round(float(c), 4) for c in m["bw"].reshape(-1)]})
    return {"note": "Warrior facing-camera MESH rig TEST (Tools/build-warrior-rig-mesh-down.py; flag --hero-rig-mesh). Weights by bone "
                    "name; vertices in cell px (y down). L = screen left = sword side.",
            "cellW": R.CELL_W, "cellH": R.CELL_H, "feetY": R.FEET_Y, "bodyPx": R.BODY_PX, "sheets": SHEETS_RES,
            "bones": bones, "meshes": mj, "hair": HAIR, "weapon": WEAPON, "offhand": OFFHAND, "anims": A.all_anims()}


def write_meta(path, text=False):
    meta = path + ".meta"
    if os.path.exists(meta):
        return
    if text:
        body = "fileFormatVersion: 2\nguid: %s\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        open(meta, "w", newline="\n").write(body % uuid.uuid4().hex)
        return
    lines = open(META_TEMPLATE, encoding="utf-8").read().splitlines()
    lines = ["guid: " + uuid.uuid4().hex if ln.startswith("guid:") else ln for ln in lines]
    open(meta, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")


def cmd_write():
    ms = meshes()
    os.makedirs(SPRITES, exist_ok=True)
    for n, _slot, tex, _m in ms:
        p = SPRITES + n.replace("@", "-") + ".png"
        Image.fromarray(tex).save(p)
        write_meta(p)
    folder_meta = SPRITES.rstrip("/") + ".meta"
    if not os.path.exists(folder_meta):
        open(folder_meta, "w", newline="\n").write("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
                                                   "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)
    with open(JSON_OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(rig_json(ms), f, separators=(",", ":"))
    write_meta(JSON_OUT, text=True)
    for n, _s, _t, m in ms:
        print("%-12s %4d verts %4d tris, max bones/vertex %d" % (n, len(m["verts"]), len(m["tris"]), int((m["bw"] > 0).sum(axis=1).max())))
    print("wrote", JSON_OUT)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "write":
        cmd_write()
    elif cmd == "preview":
        ms = meshes()
        V.preview(rig_json(ms), ms, sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None)
    elif cmd == "mesh":
        V.wireframe(meshes(), sys.argv[2])
    else:
        print(__doc__)

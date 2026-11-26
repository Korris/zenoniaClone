"""Sprite-swap pieces of the Warrior MESH rig (TEST 25/11, Phase 2): drawings a bent mesh cannot make.

armL@thrust: the sword forearm + fist thrust TOWARD the camera at the blow (key `sw` "armL=thrust", 120-205 ms). Bending the idle arm
cannot show a fist coming at the viewer (it only gets shorter), so the piece is cut from the APPROVED frame-art slash (body/attack.png
row 0 = facing camera, frame 4, the fist-forward frame), scaled to the idle body (the attack frames are drawn ~9 % larger), weighted
100 % to forearmL and stored in REST coordinates: the tool runs the impact pose and pulls the piece back through the forearm's affine,
so in the game it lands where the frame art draws the fist when the bones reach the blow.
"""
import numpy as np
from PIL import Image, ImageDraw

import warrior_rig_mesh_anims as A
import warrior_rig_mesh_geometry as G
import warrior_rig_mesh_pose as P
import warrior_rig_mesh_regions as R
import warrior_rig_mesh_skeleton as K

ATTACK = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/body/attack.png"
FRAME = 4
# bracer + fist of the screen-left arm in that frame (cell px of the frame), traced on the 6x zoom
THRUST_POLY = [(112, 142), (128, 139), (146, 140), (151, 148), (151, 166), (153, 180), (150, 194), (140, 199), (124, 198),
               (116, 190), (113, 172), (111, 156)]
THRUST_SCALE = 0.92          # frame-art body is drawn larger than the idle
THRUST_FIST = (134.0, 180.0)  # fist centre inside the frame cut (frame cell px)


def impact_pose():
    anim = A.slash()
    pose = P.Pose()
    t = next(k["t"] for k in anim["keys"] if k["sw"])
    pose.evaluate(anim, t, 0.0)
    return pose


def impact_pose_grip():
    """posed sword grip at the blow = where the fist of the piece goes."""
    pose = impact_pose()
    return pose.affine(K.BONE_INDEX["forearmL"], (116.0, 162.0))


def inverse_affine(pose, b, q):
    """posed point q -> rest point (bone b's affine is invertible: rotation, positive scale)."""
    _n, _par, j, _e, _s = K.BONES[b]
    d = P.rot((q[0] - pose.pos[b][0], q[1] - pose.pos[b][1]), -(P.AXIS[b] + pose.turn[b]))
    d = (d[0] / pose.scale[b][0], d[1] / pose.scale[b][1])
    d = P.rot(d, P.AXIS[b])
    return (j[0] + d[0], j[1] + d[1])


def cut_thrust():
    """-> rgba 320x256 (posed placement) and its mask."""
    sheet = Image.open(ATTACK).convert("RGBA")
    cell = sheet.crop((FRAME * 320, 0, FRAME * 320 + 320, 256))
    m = Image.new("L", cell.size, 0)
    ImageDraw.Draw(m).polygon(THRUST_POLY, fill=255)
    piece = Image.new("RGBA", cell.size, (0, 0, 0, 0))
    piece.paste(cell, (0, 0), m)
    w, h = piece.size
    big = piece.resize((int(w * THRUST_SCALE), int(h * THRUST_SCALE)), Image.LANCZOS)
    out = Image.new("RGBA", (R.CELL_W, R.CELL_H), (0, 0, 0, 0))
    at = impact_pose_grip()
    ox = int(round(at[0] - THRUST_FIST[0] * THRUST_SCALE))
    oy = int(round(at[1] - THRUST_FIST[1] * THRUST_SCALE))
    out.paste(big, (ox, oy), big)
    a = np.array(out)
    return a, a[..., 3] > 8


def variants():
    tex, cover = cut_thrust()
    mesh = G.grid_mesh(cover)
    verts, tris = mesh
    pose = impact_pose()
    b = K.BONE_INDEX["forearmL"]
    # vertices keep their texture place (uv) but are stored in rest coordinates: posed -> rest through the forearm
    rest = np.array([inverse_affine(pose, b, (float(x), float(y))) for x, y in verts], np.float32)
    bi = np.zeros((len(verts), 4), np.int32)
    bi[:, 0] = b
    bw = np.zeros((len(verts), 4), np.float32)
    bw[:, 0] = 1.0
    return [("armL@thrust", "armL", tex, {"verts": rest, "uv": verts, "tris": tris, "bi": bi, "bw": bw})]

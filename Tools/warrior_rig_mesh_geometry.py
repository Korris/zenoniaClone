"""Triangle mesh + bone weights for one part texture of the Warrior MESH rig (TEST 25/11, Phase 2).

Mesh: regular grid (STEP px) over the part's coverage mask (a quad is kept when any covered pixel falls in it, mask dilated 1 px
so the alpha edge is inside), two triangles per quad, diagonals alternating. 4 px everywhere = dense enough at every joint
(a knee / elbow blend band of 10-13 px spans 3 rows of vertices).
Weights: the bone-name rules of warrior_rig_mesh_skeleton at each vertex, then SMOOTH_ITER Laplacian passes over the mesh edges
(rigid vertices, e.g. the face, stay fixed), then the MAX_BONES strongest kept and renormalised.
"""
import numpy as np
from scipy import ndimage

import warrior_rig_mesh_skeleton as K

STEP = 4
SMOOTH_ITER = 2
MAX_BONES = 4


def grid_mesh(mask):
    """mask (H x W bool) -> (verts N x 2 float cell px, tris M x 3 int)."""
    m = ndimage.binary_dilation(mask, iterations=1)
    h, w = m.shape
    ys, xs = np.where(m)
    gx0, gy0 = (xs.min() // STEP) * STEP, (ys.min() // STEP) * STEP
    gx1, gy1 = xs.max() + 1, ys.max() + 1
    index, verts, tris = {}, [], []

    def vid(i, j):
        k = (i, j)
        if k not in index:
            index[k] = len(verts)
            verts.append((float(min(w, i)), float(min(h, j))))
        return index[k]

    for y in range(gy0, gy1, STEP):
        for x in range(gx0, gx1, STEP):
            if not m[y:y + STEP, x:x + STEP].any():
                continue
            a, b = vid(x, y), vid(x + STEP, y)
            c, d = vid(x, y + STEP), vid(x + STEP, y + STEP)
            if ((x - gx0) // STEP + (y - gy0) // STEP) % 2 == 0:
                tris += [(a, b, d), (a, d, c)]
            else:
                tris += [(a, b, c), (b, d, c)]
    return np.array(verts, np.float32), np.array(tris, np.int32)


def weights(rule, verts, tris):
    """-> (bone index N x 4 int, weight N x 4 float)."""
    nb = len(K.BONES)
    wm = np.zeros((len(verts), nb), np.float64)
    for i, (x, y) in enumerate(verts):
        for name, v in rule(float(x), float(y)).items():
            if v > 1e-4:
                wm[i, K.BONE_INDEX[name]] += v
    wm /= np.maximum(1e-9, wm.sum(axis=1, keepdims=True))
    rigid = wm.max(axis=1) > 0.999
    nbrs = [set() for _ in verts]
    for a, b, c in tris:
        nbrs[a] |= {b, c}; nbrs[b] |= {a, c}; nbrs[c] |= {a, b}
    for _ in range(SMOOTH_ITER):
        avg = np.array([wm[list(n)].mean(axis=0) if n else wm[i] for i, n in enumerate(nbrs)])
        wm = np.where(rigid[:, None], wm, 0.5 * wm + 0.5 * avg)
    idx = np.argsort(-wm, axis=1)[:, :MAX_BONES]
    w = np.take_along_axis(wm, idx, axis=1)
    w[w < 0.01] = 0.0
    w /= np.maximum(1e-9, w.sum(axis=1, keepdims=True))
    idx[w == 0.0] = 0
    return idx.astype(np.int32), w.astype(np.float32)


def build(name, mask):
    verts, tris = grid_mesh(mask)
    bi, bw = weights(K.RULES[name], verts, tris)
    return {"verts": verts, "tris": tris, "bi": bi, "bw": bw}

"""Regions + textures of the Warrior facing-camera MESH rig (TEST 25/11, Phase 2 of plans/261125-0045-warrior-skeletal-rig-prototype).

The mesh rig is skinned straight from the APPROVED idle-down cell (Sprites/hero-layered/ro-fantasy/warrior/body/idle.png row 0,
bald body with face, 320 x 256, feet y 244): no re-painted parts, so face / armour / proportions are exactly the approved art.
It is split into only 4 meshes because they have to change draw order or uncover something:
  cape  (back)  the two cape wings + a filled cloth backdrop under hips / legs / arms (shown when a leg lifts or an arm swings out)
  body          head + torso + belt + skirt + legs in ONE mesh (joints bend by weights, no seams)
  armL / armR   sword arm (screen left = the Knight's right) / shield arm: they cross in front of / go behind the body in the slash
Coordinates: cell px, x right, y DOWN. Screen-left arm polygon traced on the 8x zoom; the right arm is its mirror (x -> 319 - x).
"""
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

IDLE = "Assets/Resources/Sprites/hero-layered/ro-fantasy/warrior/body/idle.png"
CELL_W, CELL_H, FEET_Y, BODY_PX = 320, 256, 244, 200
MIRROR = 319

ARM_L = [(117, 127), (128, 127), (141, 125), (142, 131), (137, 139), (133, 146), (130, 152), (128, 160), (128, 168), (124, 174),
         (110, 174), (104, 166), (105, 150), (110, 137), (117, 128)]
ARM_R = [(MIRROR - x, y) for x, y in ARM_L]
WING_X = 125            # cape wing = left of this (and right of MIRROR - this), below WING_Y, outside the arms
WING_Y = 148
WING_Y_MAX = 224    # the boots start under it at the same x
CAPE_BACKDROP = [(112, 148), (207, 148), (223, 210), (222, 223), (176, 223), (172, 211), (147, 211), (143, 223), (98, 223), (97, 210)]  # cloth behind the body (wing outer edges); notch = ground between the boots
ARM_BAND = 4            # body pixels re-painted this far under an arm (the arm lifts off a filled edge, not a hole)


def idle_cell():
    return np.array(Image.open(IDLE).convert("RGBA").crop((0, 0, CELL_W, CELL_H)))


def poly_mask(points):
    im = Image.new("L", (CELL_W, CELL_H), 0)
    ImageDraw.Draw(im).polygon(points, fill=255)
    return np.array(im) > 0


def nearest_fill(rgba, src_mask, dst_mask):
    """RGB of dst pixels = RGB of the nearest src pixel; alpha 255."""
    out = rgba.copy()
    if not src_mask.any():
        return out
    _d, (iy, ix) = ndimage.distance_transform_edt(~src_mask, return_indices=True)
    out[dst_mask, :3] = rgba[iy[dst_mask], ix[dst_mask], :3]
    out[dst_mask, 3] = 255
    return out


def labels():
    """name -> boolean mask of the source pixels of each mesh (before fills)."""
    a = idle_cell()
    solid = a[..., 3] > 0
    arm_l, arm_r = poly_mask(ARM_L) & solid, poly_mask(ARM_R) & solid
    yy, xx = np.mgrid[0:CELL_H, 0:CELL_W]
    wing = solid & (yy >= WING_Y) & (yy <= WING_Y_MAX) & ((xx < WING_X) | (xx > MIRROR - WING_X)) & ~arm_l & ~arm_r
    body = solid & ~arm_l & ~arm_r & ~wing
    lab, n = ndimage.label(body)
    if n > 1:  # outline specks of the arms left outside the polygons
        sizes = ndimage.sum(body, lab, range(1, n + 1))
        body = lab == (1 + int(np.argmax(sizes)))
        arm_l |= solid & ~body & ~wing & (xx < 160) & ~arm_r
        arm_r |= solid & ~body & ~wing & (xx >= 160) & ~arm_l
    return {"cape": wing, "body": body, "armL": arm_l, "armR": arm_r}


def textures():
    """name -> (rgba 320x256 texture, mesh coverage mask). Fills hidden cloth / edges so nothing opens a hole when it moves."""
    a = idle_cell()
    lab = labels()
    out = {}
    # cape: wings + backdrop filled with the darkest cape navy (median of the inner wing shade)
    cape = np.zeros_like(a)
    cape[lab["cape"]] = a[lab["cape"]]
    hidden = lab["body"] | lab["armL"] | lab["armR"]
    # a smooth polygon, not the arm / leg silhouette: when an arm lifts away the uncovered cloth has a clean edge (round 2: a jagged
    # navy block shaped like the glove outline). Thin ground gaps between arm and skirt get cloth (the cape hangs there anyway).
    solid = a[..., 3] > 0
    closed = ndimage.binary_closing(solid, iterations=3) | poly_mask(ARM_L) | poly_mask(ARM_R)
    back = poly_mask(CAPE_BACKDROP) & closed & ~lab["cape"] & (hidden | ~solid)
    wing_px = a[lab["cape"]][:, :3].astype(int)
    blue = wing_px[(wing_px[:, 2] > wing_px[:, 0] + 25) & (wing_px.sum(axis=1) < 330)]
    navy = np.median(blue, axis=0)
    cape[back, :3] = (navy * 0.72).astype(np.uint8)
    cape[back, 3] = 255
    out["cape"] = (cape, lab["cape"] | back)
    # body: arms removed; a thin band under each arm re-painted from the nearest body pixel
    body = np.zeros_like(a)
    body[lab["body"]] = a[lab["body"]]
    arms = lab["armL"] | lab["armR"]
    near = ndimage.distance_transform_edt(~lab["body"]) <= ARM_BAND
    band = arms & near
    body = nearest_fill(body, lab["body"], band)
    out["body"] = (body, lab["body"] | band)
    for n in ("armL", "armR"):
        t = np.zeros_like(a)
        t[lab[n]] = a[lab[n]]
        out[n] = (t, lab[n])
    return out

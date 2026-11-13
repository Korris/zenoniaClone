"""Cuts the wooden bridge of Willowbrook Meadows out of the OLD painted ground into a flat prop (wbBridge): the redrawn ground has no bridge.

Usage: python Tools/cut-willowbrook-bridge-prop.py [--check]   (--check only writes the preview over the current ground to plans/.../visuals/wb-bridge-check.png)
 Source: Assets/ArtLibrary/maps/willowbrook-meadows/redraw/willowbrook-bridge-source-from-old-ground.png = old chunk r0c0 (64 px/tile), px 1408..1792 x 1280..1536
 (tiles 22-27, rows 20-23). Mask = hand polygon around the bridge AND wood colour (red clearly above green; the old grass is yellow-green, the water grey-blue),
 closed and feathered. The wood is graded a little cooler/darker so the old sunset tint sits on the redrawn (greener) ground.
 Sprite = 352x192 (5.5 x 3 tiles): bottom = bottom of row 23, centred on the 5-tile deck (cols 23-27, the B tiles), so P("wbBridge", 23, 23) puts every pixel
 SHIFT_Y px below where the old painting had it (the canvas may be 3 tiles high at most to be drawn flat under the hero: MapScenery.FlatDecorMaxTiles).
"""
import os, sys
import numpy as np, cv2
from PIL import Image
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
SRC = os.path.join(ROOT, "Assets/ArtLibrary/maps/willowbrook-meadows/redraw/willowbrook-bridge-source-from-old-ground.png")
OUT = os.path.join(ROOT, "Assets/Resources/Sprites/village/props/wbBridge.png")
TILES = os.path.join(ROOT, "Assets/Resources/Sprites/village/tiles")
VIS = os.path.join(ROOT, "plans/261105-1052-willowbrook-large-hunting-map/visuals")
OX, OY = 1408, 1280            # map px of the source crop's top-left
TX, TY, FW = 23, 23, 5
CW, CH = 352, 192
SHIFT_Y = 14
# outline in map px (old chunk r0c0), clockwise from the back-left post
POLY = [(1464, 1366), (1492, 1362), (1492, 1374), (1538, 1356), (1540, 1350), (1558, 1350), (1560, 1354), (1592, 1346), (1604, 1344), (1634, 1346), (1634, 1332),
        (1658, 1332), (1660, 1344), (1700, 1348), (1700, 1338), (1716, 1338), (1718, 1378), (1700, 1380), (1722, 1404), (1746, 1406), (1748, 1470), (1746, 1502),
        (1716, 1502), (1690, 1478), (1640, 1484), (1626, 1486), (1604, 1524), (1582, 1524), (1580, 1492), (1552, 1500), (1526, 1500), (1524, 1470), (1474, 1418), (1466, 1414)]


HOLE = [(1661, 1355), (1699, 1359), (1699, 1377), (1661, 1373)]   # old grass seen under the back-right rail (yellow-brown, passes the wood test)


def cut():
    src = np.asarray(Image.open(SRC).convert("RGB")).astype(np.float32)
    h, w = src.shape[:2]
    poly = np.zeros((h, w), np.uint8)
    cv2.fillPoly(poly, [np.array([(x - OX, y - OY) for x, y in POLY], np.int32)], 255)
    cv2.fillPoly(poly, [np.array([(x - OX, y - OY) for x, y in HOLE], np.int32)], 0)
    r, g, b = src[..., 0], src[..., 1], src[..., 2]
    wood = (r > g * 1.10) & (g > b * 1.05)
    m = (wood & (poly > 0)).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7)))
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3)))
    n, lab, stats, _ = cv2.connectedComponentsWithStats(m)
    for i in range(1, n):
        if stats[i, cv2.CC_STAT_AREA] < 120: m[lab == i] = 0
    a = cv2.GaussianBlur(m.astype(np.float32), (0, 0), 0.9)
    a = np.clip((a - 0.25) / 0.5, 0, 1)
    rgb = src.copy()
    rgb = (rgb - 128) * 1.08 + 128 + np.array([-10, -6, 2], np.float32)   # less yellow, a little more contrast
    rgba = np.dstack([np.clip(rgb, 0, 255), a * 255]).astype(np.uint8)
    canvas = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    left = int((TX + FW / 2.0) * 64 - CW / 2); top = (TY + 1) * 64 - CH
    canvas.alpha_composite(Image.fromarray(rgba, "RGBA"), (OX - left, OY - top + SHIFT_Y))
    return canvas, left, top


if __name__ == "__main__":
    sprite, left, top = cut()
    box = np.asarray(sprite)[..., 3] > 0
    assert not box[0].any() and not box[:, 0].any() and not box[:, -1].any(), "bridge touches the canvas edge"
    ground = Image.open(os.path.join(TILES, "willowbrook-large-ground-r0c0.png")).convert("RGBA")
    ground.alpha_composite(sprite, (left, top))
    ground.crop((1280, 1216, 1920, 1536)).resize((1280, 640), Image.LANCZOS).convert("RGB").save(os.path.join(VIS, "wb-bridge-check.png"))
    if "--check" not in sys.argv: sprite.save(OUT, optimize=True)
    print('{ "wbBridge", new PropDef(%sf, %sf, %d, 0) },' % (CW / 64.0, CH / 64.0, FW))

"""Remove checkerboard residue from a keyed sheet: near-white / light-grey blobs sitting on the silhouette edge (the
white 'highlights' on the hair of the armed attack3 sheet) and tiny floating alpha crumbs. Interior highlights (eye
shine, skin) are kept: only blobs that touch transparency are removed."""
import cv2
import numpy as np
from PIL import Image

LIGHT_MIN, LIGHT_SPREAD, MAX_BLOB, CRUMB = 165, 40, 700, 60


def clean_specks(img):
    a = np.array(img.convert('RGBA'))
    rgb, alpha = a[..., :3], a[..., 3]
    solid = alpha > 48
    light = ((rgb.min(axis=2) >= LIGHT_MIN) & ((rgb.max(axis=2) - rgb.min(axis=2)) <= LIGHT_SPREAD) & solid).astype(np.uint8)
    near_empty = cv2.dilate((~solid).astype(np.uint8), np.ones((5, 5), np.uint8)) > 0
    n, lab, stats, _ = cv2.connectedComponentsWithStats(light, connectivity=8)
    kill = np.zeros(alpha.shape, bool)
    for i in range(1, n):
        if stats[i, 4] <= MAX_BLOB:
            blob = lab == i
            if (blob & near_empty).any():
                kill |= cv2.dilate(blob.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
    a[..., 3] = np.where(kill & solid, 0, alpha)
    n, lab, stats, _ = cv2.connectedComponentsWithStats((a[..., 3] > 48).astype(np.uint8), connectivity=8)
    for i in range(1, n):
        if stats[i, 4] < CRUMB:
            a[..., 3][lab == i] = 0
    return Image.fromarray(a, 'RGBA')

"""Thử chỉnh màu ảnh nền map sang tông "RO Soft Fantasy" (sáng hơn, bớt gắt, bóng tím nhạt) để so trước / sau.

Chỉ đọc ảnh trong Assets/, KHÔNG ghi vào Assets/. Kết quả ghi vào docs/reference/map-ro-soft-tone-test/.
Cùng một hàm grade() xuất được 3D LUT (.cube) để dùng lại cho mọi map (hình học không đổi nên ô chặn / cầu thang giữ nguyên).

    python Tools/preview-map-ro-soft-tone-grade.py            # ảnh so sánh + LUT
"""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "reference", "map-ro-soft-tone-test")
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")

# (nhãn, ảnh nguồn, vùng cắt hoặc None)
SAMPLES = [
    ("sunken-reliquary", os.path.join(TILES, "sunken-reliquary-ground.png"), None),
    ("bramblewood-forest", os.path.join(TILES, "bramblewood-forest-ground.png"), None),
    ("willowbrook-large-r0c0", os.path.join(TILES, "willowbrook-large-ground-r0c0.png"), None),
    ("emberfen-marsh", os.path.join(TILES, "emberfen-marsh-ground.png"), None),
    ("ingame-wanted-mire-hydra", os.path.join(ROOT, "docs", "reference", "ui-style-ro-soft-fantasy", "wanted-boss",
                                             "ingame-40-players-vs-mire-hydra-queen.png"), (330, 110, 1210, 700)),
]


def rgb_to_hsv(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx = a.max(-1); mn = a.min(-1); d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-6
    rm = m & (mx == r); gm = m & (mx == g) & ~rm; bm = m & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    h = h / 6.0
    s = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0)
    return h, s, mx


def hsv_to_rgb(h, s, v):
    i = np.floor(h * 6) % 6; f = h * 6 - np.floor(h * 6)
    p = v * (1 - s); q = v * (1 - f * s); t = v * (1 - (1 - f) * s)
    out = np.zeros(h.shape + (3,), np.float32)
    for k, (R, G, B) in enumerate([(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]):
        m = i == k
        out[m, 0] = R[m]; out[m, 1] = G[m]; out[m, 2] = B[m]
    return out


def grade(a):
    """a: float32 HxWx3 trong [0,1]. Trả ảnh đã chỉnh. Chỉ phụ thuộc màu từng điểm nên xuất được LUT."""
    h, s, v = rgb_to_hsv(a)
    deg = h * 360
    # 1) Bão hoà: chung x0.9; vàng-xanh lá chói (45-110 độ) và cam đất (15-45) bớt hơn; nước teal / cyan (150-210) tăng nhẹ
    sat_k = np.full_like(s, 0.90)
    sat_k = np.where((deg > 45) & (deg < 110), 0.80, sat_k)
    sat_k = np.where((deg > 15) & (deg <= 45), 0.82, sat_k)
    sat_k = np.where((deg > 150) & (deg < 210), 1.08, sat_k)
    s = np.clip(s * sat_k, 0, 1)
    # 2) Xanh lá vàng (60-130) sang bạc hà, nước xanh lục (140-185) sang cyan trong, cam đất (15-45) về vàng kem
    shift = (np.where((deg > 60) & (deg < 130), 8.0, 0.0) + np.where((deg >= 140) & (deg < 185), 12.0, 0.0)
             + np.where((deg > 15) & (deg < 45), 4.0, 0.0))
    h = ((deg + shift) % 360) / 360
    out = hsv_to_rgb(h, s, v)
    # 3) Nâng bóng tối (gamma + sàn đen nhỏ) để nét viền tối mềm hơn, không bị mờ sương
    out = 0.035 + 0.965 * np.power(np.clip(out, 0, 1), 0.80)
    lum = (0.2126 * out[..., 0] + 0.7152 * out[..., 1] + 0.0722 * out[..., 2])[..., None]
    shadow_w = np.clip(1 - lum / 0.55, 0, 1)
    high_w = np.clip((lum - 0.55) / 0.45, 0, 1)
    out = out + shadow_w * np.array([0.035, 0.0, 0.06]) + high_w * np.array([0.03, 0.02, -0.015])
    return np.clip(out, 0, 1)


def stats(a):
    L = 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]
    mx = a.max(-1); mn = a.min(-1)
    S = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    return "L %.2f  S %.2f  contrast %.2f  R-B %+.2f" % (L.mean(), S.mean(), L.std(), (a[..., 0] - a[..., 2]).mean())


def label(img, text):
    d = ImageDraw.Draw(img)
    try:
        f = ImageFont.truetype("arial.ttf", 22)
    except OSError:
        f = ImageFont.load_default()
    d.rectangle((0, 0, img.width, 34), fill=(20, 20, 28))
    d.text((10, 6), text, fill=(255, 255, 255), font=f)


def side_by_side(name, path, box):
    im = Image.open(path).convert("RGB")
    if box:
        im = im.crop(box)
    im.thumbnail((1152, 1152))
    a = np.asarray(im).astype(np.float32) / 255
    g = grade(a)
    before = im.copy(); after = Image.fromarray((g * 255 + 0.5).astype(np.uint8))
    label(before, "TRUOC  " + stats(a)); label(after, "SAU (RO soft grade v2)  " + stats(g))
    W = Image.new("RGB", (before.width * 2 + 8, before.height), (255, 255, 255))
    W.paste(before, (0, 0)); W.paste(after, (before.width + 8, 0))
    p = os.path.join(OUT, "%s-before-after-ro-soft-grade-v2.jpg" % name)
    W.save(p, quality=88)
    print(p)
    print("   before:", stats(a)); print("   after: ", stats(g))


def write_cube(n=33):
    """3D LUT .cube (Resolve / Photoshop / Unity post) cùng hàm grade(): chỉnh hàng loạt bằng LUT cho mọi map."""
    r = np.linspace(0, 1, n, dtype=np.float32)
    B, G, R = np.meshgrid(r, r, r, indexing="ij")  # .cube: R chạy nhanh nhất
    lut = grade(np.stack([R, G, B], -1).reshape(1, -1, 3)).reshape(-1, 3)
    p = os.path.join(OUT, "ro-soft-grade-v2.cube")
    with open(p, "w") as f:
        f.write('TITLE "ro-soft-grade-v2"\nLUT_3D_SIZE %d\n' % n)
        for c in lut:
            f.write("%.5f %.5f %.5f\n" % tuple(c))
    print(p)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for name, path, box in SAMPLES:
        if not os.path.exists(path):
            print("missing", path, file=sys.stderr); continue
        side_by_side(name, path, box)
    write_cube()

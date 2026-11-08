"""Custom T8 v2 headless patch: crystal wing / pauldron pixels erased by outfit-headless put back. argv: out.png preview.png"""
import sys, numpy as np
from PIL import Image
from scipy import ndimage
B = "D:/zenoniaClone/Assets/Resources/Sprites/classes/assassin/body"
F = 192
bare = np.asarray(Image.open(f"{B}/set-t8/bare/idle.png").convert("RGBA"))
hl = np.asarray(Image.open(f"{B}/set-t8/headless/idle.png").convert("RGBA"))
lv1 = np.asarray(Image.open(f"{B}/bare/idle.png").convert("RGBA"))
Y0 = int(sys.argv[3]) if len(sys.argv) > 3 else 72
patch = np.zeros_like(bare)
for f in range(4):
    sl = (slice(0, F), slice(f * F, (f + 1) * F))
    px = bare[sl].astype(int); r, g, b = px[..., 0], px[..., 1], px[..., 2]
    erased = (px[..., 3] > 8) & (hl[sl][..., 3] <= 8)
    crystal = ((b > 150) & (b - g > 70)) | (px[..., :3].min(2) > 150) | ((r > 140) & (b > 150))
    hairish = ((b > 40) & (g < r - 5) & (g < b) & (np.abs(r - b) < 60) & (r + g + b < 330)) | ((r + g + b) < 120)
    outside = ~ndimage.binary_dilation(lv1[sl][..., 3] > 8, iterations=1)
    m = erased & (crystal | (outside & ~hairish))
    # dark outline pixels touching restored crystal (keeps the wing's ink line)
    m |= erased & ((r + g + b) < 160) & ndimage.binary_dilation(m, iterations=1)
    # neck strip under the mask (pink scarf) that the head art does not cover: every erased pixel there, else the ground shows through
    neck = np.zeros_like(m); neck[90:112, 64:130] = True
    m |= erased & neck
    m[:Y0] = False
    # loose specks (ponytail highlights) not joined to the kept body: drop pieces < 40 px that do not touch the headless body
    body = ndimage.binary_dilation(hl[sl][..., 3] > 8, iterations=1)
    lab, n = ndimage.label(m, np.ones((3, 3)))
    for i in range(1, n + 1):
        pc = lab == i
        if pc.sum() < 40 and not (pc & body).any():
            m[pc] = False
    patch[sl][m] = bare[sl][m]
Image.fromarray(patch).save(sys.argv[1])
print("patch px", int((patch[..., 3] > 0).sum()))
# preview: headless + patch (under) + each hairstyle, frame 0, 3x
bg = (58, 62, 70, 255)
out = Image.new("RGBA", (576 * 6, 576), bg)
base = Image.fromarray(patch[:F, :F]); h = Image.fromarray(hl[:F, :F]); base.alpha_composite(h)
mark = Image.fromarray(patch[:F, :F]).copy()
out.alpha_composite(Image.fromarray(bare[:F, :F]).resize((576, 576), Image.NEAREST), (0, 0))
for s in range(5):
    c = base.copy(); c.alpha_composite(Image.open(f"D:/zenoniaClone/Assets/Resources/Sprites/classes/assassin/heads/{s}/down.png").convert("RGBA"))
    out.alpha_composite(c.resize((576, 576), Image.NEAREST), (576 * (s + 1), 0))
out.save(sys.argv[2])

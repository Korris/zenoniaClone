"""Wizard V3 staff (Asteria Mage, 26/11/2026): ChatGPT sheet -> Sprites/hero-layered/weapons/wizard-staff.png + grip in weapons.json.

Source (read only, newest N): Assets/ArtLibrary/ro-fantasy-heroes/src/wizard-v3/weapons-wizard-staff-v3-chatgpt-N.png, flat #00FF00,
the staff UPRIGHT (head at the top), alone. Length STAFF_PX = 1.0 H (H = the 216 px figure, skull top to feet in the 320 x 256 cell):
held at 74 % from the top (hero_layered_grip_poses.STAFF_GRIP) the head ring ends ~13 px over the skull, the foot just above the feet
line, like the turnaround. Called by Tools/build-wizard-v3-game-assets.py staff; then run `grips`.
Old art: D:/temp/wizard-v2-backup/weapons/.
"""
import json

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

import ro_fantasy_body_cut as bc

ROOT = bc.ROOT
OUT = ROOT / "Assets/Resources/Sprites/hero-layered/weapons/wizard-staff.png"
DATA = ROOT / "Assets/Resources/Data/hero-layered/weapons.json"
CHECK = ROOT / "docs/reference/character-v3/wizard-v3-test/check/staff-cut-check.png"
STAFF_PX = 216
GRIP = 0.74


def build():
    src = bc.newest("weapons-wizard-staff-v3-chatgpt-*.png")
    a = np.asarray(bc.keyed(src)).copy()
    # the orb glow inside the ring is painted half-transparent over the #00FF00 key: unmix it (green excess -> transparency, de-spilled
    # colour) so the ring interior is a soft lavender glow over the scene instead of a green disc
    c = a[..., :3].astype(np.float32)
    ex = c[..., 1] - np.maximum(c[..., 0], c[..., 2])
    spill = (a[..., 3] > 0) & (ex > 12)
    k = np.clip(ex / 255.0 * 1.6, 0, 1)
    a[..., 3] = np.where(spill, (a[..., 3] * (1 - k)).astype(np.uint8), a[..., 3])
    a[..., 1] = np.where(spill, np.maximum(c[..., 0], c[..., 2]).clip(0, 255).astype(np.uint8), a[..., 1])
    al = a[..., 3] > 40
    lab, n = ndimage.label(ndimage.binary_dilation(al, iterations=6))
    sizes = ndimage.sum(al, lab, range(1, n + 1))
    i = int(np.argmax(sizes)) + 1
    sl = ndimage.find_objects(lab)[i - 1]
    crop = a[sl].copy()
    crop[lab[sl] != i] = 0
    img = Image.fromarray(crop, "RGBA")
    img = img.crop(img.getbbox())
    img = img.resize((max(1, round(img.width * STAFF_PX / img.height)), STAFF_PX), Image.LANCZOS)
    gy = round(STAFF_PX * GRIP, 1)
    gx = round(float(np.nonzero(np.asarray(img)[int(gy), :, 3] > 100)[0].mean()), 1)
    print(src.name, "->", img.size, "grip", (gx, gy))
    return img, (gx, gy)


def write():
    img, g = build()
    img.save(OUT)
    data = json.loads(DATA.read_text(encoding="utf-8"))
    data["grips"]["wizard-staff"] = list(g)
    if "Wizard V3" not in data["note"]:
        data["note"] += " Wizard V3 26/11: wizard-staff from Tools/wizard_v3_staff.py."
    DATA.write_text(json.dumps(data, indent=1), encoding="utf-8")
    k = 3
    big = img.resize((img.width * k, img.height * k), Image.NEAREST)
    out = Image.new("RGBA", (big.width + 40, big.height), (90, 90, 90, 255))
    out.alpha_composite(big, (20, 0))
    d = ImageDraw.Draw(out)
    d.ellipse((20 + g[0] * k - 5, g[1] * k - 5, 20 + g[0] * k + 5, g[1] * k + 5), outline=(255, 0, 0, 255), width=2)
    CHECK.parent.mkdir(parents=True, exist_ok=True)
    out.save(CHECK)
    print(OUT, CHECK)

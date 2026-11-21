"""Skill icons, RO Soft Fantasy (Cleric 20/11/2026, other classes 21/11/2026): cut a ChatGPT sheet of 8 round icons and preview them like the RO Soft HUD draws them.

  py -3 Tools/build-soft-fantasy-skill-icons.py cut <class> <sheet.png>
      sheet = 4 columns x 2 rows of painted discs on flat #FF00FF (prompt: ArtLibrary/classes/<class>/skill-icons-src/prompt-soft-fantasy-icons.md).
      Background = near-magenta pixels connected to the image border (purple smoke inside a disc is kept), magenta spill removed at the rim.
      Writes Assets/Resources/Sprites/classes/<class>/skills/<slug>/icon.png (128 px, order = SkillTable.<Class>.cs index 0-7; existing .meta kept,
      a missing .meta / folder .meta is created with a fresh GUID) and the keyed 8-icon strip to ArtLibrary/classes/<class>/skill-icons-src/<class>-skill-icons-soft-fantasy-keyed.png.
  py -3 Tools/build-soft-fantasy-skill-icons.py preview <class> <out.png>
      the installed 128 px icons on a light panel, then cut round at 96 / 64 px over grass from the Willowbrook screenshot
      (quick art check only; the real HUD ring / mask is checked in game). Run before `cut` for the "before" image, after it for "after".
"""
import sys
import uuid
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parent.parent
CLASSES = ROOT / "Assets/Resources/Sprites/classes"
ART = ROOT / "Assets/ArtLibrary/classes"
BG_SHOT = ROOT / "docs/reference/cleric-fx-ro3/willowbrook-meadows-ingame-cleric.jpg"
# icon.png.meta used as the import-settings template for icons that had no file yet (Fighter, Mechanic deploy-turret)
META_TEMPLATE = CLASSES / "cleric/skills/heal/icon.png.meta"
# slug = SkillDef.Id without the class prefix, in SkillTable.<Class>.cs index order 0-7
SLUGS = {
    "cleric": ["heal", "spirit-blessing", "divine-aegis", "divine-light", "sanctuary", "guardian-spirit", "purification", "divine-ascension"],
    "warrior": ["power-slash", "whirlwind", "earth-break", "flame-cleave", "charging-strike", "blade-cyclone", "inferno-crash", "berserker-awakening"],
    "wizard": ["arcane-bolt", "fireball", "ice-storm", "arcane-lance", "mana-shield", "frost-nova", "elemental-burst", "astral-cataclysm"],
    "mechanic": ["power-shot", "plasma-barrage", "rocket-barrage", "target-lock", "rocket-punch", "deploy-turret", "emp-burst", "orbital-cannon"],
    "assassin": ["shadow-strike", "shadow-step", "blade-dance", "phantom-rush", "venom-daggers", "shadow-clone", "death-mark", "nightfall-execution"],
    "fighter": ["dash-punch", "iron-fist", "ground-slam", "rapid-jabs", "shockwave", "tiger-rush", "iron-body", "dragon-fist"],
}
KEY = np.array([255, 0, 255], dtype=np.float32)
SIZE = 128
FOLDER_META = "fileFormatVersion: 2\nguid: {}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"


def key_out(im):
    """RGBA with the border-connected key colour made transparent (soft 2-step edge) and key spill pulled out of the rim.
    Key = #FF00FF, or #00FF00 when the sheet border is green (Assassin: purple art must not sit on magenta, sprite-rules art ChatGPT rule 1)."""
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    edge = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
    green = np.median(edge[:, 1]) > np.median(edge[:, 0])
    key = np.array([0, 255, 0], dtype=np.float32) if green else KEY
    dist = np.sqrt(((rgb - key) ** 2).sum(axis=2))
    near = dist < 150
    lab, _ = ndimage.label(near)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bg = np.isin(lab, list(border))
    alpha = np.where(bg, np.clip((dist - 60) / 90, 0, 1), 1.0)
    rim = ndimage.binary_dilation(bg, iterations=4) & ~bg
    # spill on the rim and on semi-transparent pixels: magenta = R and B over G -> clamp R, B toward G; green = G over R, B -> clamp G toward max(R, B)
    spill = rim | (alpha < 1)
    if green:
        ref = rgb[..., [0, 2]].max(axis=2)
        ch = rgb[..., 1]
        rgb[..., 1] = np.where(spill & (ch > ref), ref + (ch - ref) * 0.1, ch)
    else:
        g = rgb[..., 1]
        for c in (0, 2):
            ch = rgb[..., c]
            rgb[..., c] = np.where(spill & (ch > g), g + (ch - g) * 0.1, ch)
    out = np.dstack([rgb, alpha * 255]).clip(0, 255).astype(np.uint8)
    return Image.fromarray(out, "RGBA"), ~bg, green


def discs(fg):
    """Bounding boxes of the 8 biggest foreground blobs, row-major (2 rows of 4)."""
    lab, n = ndimage.label(ndimage.binary_opening(fg, iterations=2))
    sizes = ndimage.sum(fg, lab, range(1, n + 1))
    keep = np.argsort(sizes)[::-1][:8] + 1
    if len(keep) < 8:
        sys.exit("found %d icons, need 8" % len(keep))
    boxes = [s for i, s in enumerate(ndimage.find_objects(lab)) if i + 1 in keep]
    boxes = [(b[1].start, b[0].start, b[1].stop, b[0].stop) for b in boxes]
    # every disc is cut at the same size, the median blob size: a disc whose sparkles poke out past its edge (Wizard Mana Shield)
    # would otherwise get a bigger box and shrink; its box is re-centred on the blob's centre of mass (the disc dominates it)
    side = int(np.median([max(b[2] - b[0], b[3] - b[1]) for b in boxes]))
    fixed = []
    for b in boxes:
        w, h = b[2] - b[0], b[3] - b[1]
        if max(w, h) > side * 1.04:
            sub = lab[b[1]:b[3], b[0]:b[2]]
            idx = [i for i in keep if (sub == i).any()][0]
            cy, cx = ndimage.center_of_mass(lab == idx)
            b = (int(cx - side / 2), int(cy - side / 2), int(cx - side / 2) + side, int(cy - side / 2) + side)
        fixed.append(b)
    boxes = fixed
    mid = np.median([(b[1] + b[3]) / 2 for b in boxes])
    top = sorted([b for b in boxes if (b[1] + b[3]) / 2 < mid], key=lambda b: b[0])
    bot = sorted([b for b in boxes if (b[1] + b[3]) / 2 >= mid], key=lambda b: b[0])
    if len(top) != 4 or len(bot) != 4:
        sys.exit("rows are %d + %d, need 4 + 4" % (len(top), len(bot)))
    return top + bot


def square(im, box, pad=2):
    x0, y0, x1, y1 = box
    side = max(x1 - x0, y1 - y0) + 2 * pad
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    sq = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    sq.paste(im.crop((int(cx - side / 2), int(cy - side / 2), int(cx - side / 2) + side, int(cy - side / 2) + side)), (0, 0))
    # resize premultiplied: fully transparent key pixels still hold the key RGB and would otherwise bleed a faint key-coloured halo into the rim
    return sq.convert("RGBa").resize((SIZE, SIZE), Image.LANCZOS).convert("RGBA")


def despill_rim(icon, green):
    """Lanczos ringing at the disc edge leaves faint (alpha < 128) pixels tinted with the key (R, B clipped to 0, G kept on a green key):
    clamp the key channel(s) of those pixels to the others, so no key-coloured halo shows on a light panel."""
    a = np.asarray(icon, dtype=np.float32).copy()
    faint = a[..., 3] < 128
    if green:
        ref = a[..., [0, 2]].max(axis=2)
        a[..., 1] = np.where(faint, np.minimum(a[..., 1], ref), a[..., 1])
    else:
        for c in (0, 2):
            a[..., c] = np.where(faint, np.minimum(a[..., c], a[..., 1]), a[..., c])
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def ensure_meta(path, folder=False):
    """Create <path>.meta with a fresh GUID when Unity has not made one yet (new folder or new icon), so the GUID is stable in git."""
    meta = Path(str(path) + ".meta")
    if meta.exists():
        return
    guid = uuid.uuid4().hex
    if folder:
        meta.write_text(FOLDER_META.format(guid), newline="\n")
    else:
        lines = META_TEMPLATE.read_text().splitlines()
        lines = ["guid: " + guid if l.startswith("guid:") else l for l in lines]
        meta.write_text("\n".join(lines) + "\n", newline="\n")
    print("created", meta)


def cut(cls, sheet):
    skills = CLASSES / cls / "skills"
    src = ART / cls / "skill-icons-src"
    im = Image.open(sheet)
    keyed, fg, green = key_out(im)
    icons = [despill_rim(square(keyed, b), green) for b in discs(fg)]
    strip = Image.new("RGBA", (SIZE * 8 + 7 * 8, SIZE), (0, 0, 0, 0))
    for d in (skills,):
        d.mkdir(parents=True, exist_ok=True)
        ensure_meta(d, folder=True)
    for i, (slug, icon) in enumerate(zip(SLUGS[cls], icons)):
        (skills / slug).mkdir(exist_ok=True)
        ensure_meta(skills / slug, folder=True)
        icon.save(skills / slug / "icon.png")
        ensure_meta(skills / slug / "icon.png")
        strip.alpha_composite(icon, (i * (SIZE + 8), 0))
        print("wrote", skills / slug / "icon.png")
    src.mkdir(parents=True, exist_ok=True)
    strip.save(src / ("%s-skill-icons-soft-fantasy-keyed.png" % cls))


def round_crop(icon, size):
    """The icon cut to a circle at `size` px (approximation of the round HUD / skill-window button, without the ring)."""
    ic = icon.resize((size, size), Image.LANCZOS)
    m = Image.new("L", (size * 4, size * 4), 0)
    ImageDraw.Draw(m).ellipse((0, 0, size * 4 - 1, size * 4 - 1), fill=255)
    a = np.asarray(ic.getchannel("A"), dtype=np.float32) * np.asarray(m.resize((size, size), Image.LANCZOS), dtype=np.float32) / 255
    ic.putalpha(Image.fromarray(a.astype(np.uint8)))
    return ic


def preview(cls, out):
    blank = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))  # a skill with no icon yet shows as an empty slot
    paths = [CLASSES / cls / "skills" / s / "icon.png" for s in SLUGS[cls]]
    icons = [Image.open(p).convert("RGBA").resize((SIZE, SIZE), Image.LANCZOS) if p.exists() else blank for p in paths]
    bg = Image.open(BG_SHOT).convert("RGBA")
    w = 8 * 136 + 16
    sheet = Image.new("RGBA", (w, 136 + 112 + 80), (0, 0, 0, 255))
    # row 1: bare icons on the HUD's light panel colour
    sheet.paste(Image.new("RGBA", (w, 136), (238, 243, 248, 255)), (0, 0))
    for i, ic in enumerate(icons):
        sheet.alpha_composite(ic, (8 + i * 136, 4))
    # rows 2-3: HUD buttons at 96 and 64 px over the grass / path of the in-game screenshot (same crop under every button)
    tile = bg.crop((380, 420, 380 + w // 2, 420 + 96)).resize((w, 192))
    sheet.paste(tile, (0, 136))
    for i, ic in enumerate(icons):
        sheet.alpha_composite(round_crop(ic, 96), (20 + i * 136, 144))
        sheet.alpha_composite(round_crop(ic, 64), (36 + i * 136, 252))
    Path(out).parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(out)
    print("wrote", out)


if __name__ == "__main__":
    if len(sys.argv) == 4 and sys.argv[1] in ("cut", "preview") and sys.argv[2] in SLUGS:
        (cut if sys.argv[1] == "cut" else preview)(sys.argv[2], sys.argv[3])
    else:
        print(__doc__)

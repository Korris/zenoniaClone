"""Pet art for HeroPet: one static image per pet, and its item icon.

Sources: spirit = base of Assets/ArtLibrary/classes/cleric/companion-src/src/acolyte-pair.png, flat #0000FF (see Tools/build-cleric-spirit.py).
         imp = Assets/ArtLibrary/pets/imp-chatgpt.png, flat #0000FF.
Writes:  Assets/Resources/Sprites/pets/<id>/art.png, Assets/Resources/Sprites/icons/pet-<id>.png, and .meta files without compression where missing.
Every pet is scaled to the spirit's height (about 213 px) and faces the same way: left.

Pet Companion (16/11/2026): the eight gacha pets (the game's monsters / boss, shrunk) come from one ChatGPT sheet, 4 x 2 on flat #FF00FF (Assets/ArtLibrary/pets/pet-monsters-chatgpt-*.png;
the first try, six little spirits, was rejected by the user and deleted;
chat "Create Pet Sprite Sheet" on Tay https://chatgpt.com/c/6afa099f-db70-83ec-9f3c-68da18719ad0), all scaled by ONE factor so their sizes stay relative
(tallest = the spirit's height) and padded to that height, centred: HeroPet draws every art.png PetHeight tall, so the padding keeps the slime small.
Skill FX: the same chat's 4 x 3 sheet (pet-fx-chatgpt-*.png) cut into Resources/Sprites/fx/pets/<name>.png.

Usage (from the project root; needs Pillow, numpy): python Tools/build-pet-art.py [spirit|imp|sheet|fx ...]"""
import importlib.util, os, sys, uuid
import numpy as np
from PIL import Image

spec = importlib.util.spec_from_file_location("spirit_tool", "Tools/build-cleric-spirit.py")
st = importlib.util.module_from_spec(spec)
spec.loader.exec_module(st)

RES = "Assets/Resources/Sprites"
K = st.K
ART_META = f"{RES}/pets/spirit/art.png.meta"  # import settings template (no compression); the old cleric companion art was removed 16/11
ICON_META = f"{RES}/icons/offhand-warrior-0.png.meta"
FOLDER_META = f"{RES}/pets/spirit.meta"


def new_guid(text):
    return text.replace(text.split("guid: ")[1].split()[0], uuid.uuid4().hex)


def meta(path, source):
    if not os.path.exists(path + ".meta"):
        open(path + ".meta", "w", newline="\n").write(new_guid(open(source).read()))


def write(pet, art):
    folder = f"{RES}/pets/{pet}"
    os.makedirs(folder, exist_ok=True)
    meta(f"{RES}/pets", FOLDER_META)
    meta(folder, FOLDER_META)
    art.save(f"{folder}/art.png")
    meta(f"{folder}/art.png", ART_META)
    st.icon(f"{folder}/art.png").save(f"{RES}/icons/pet-{pet}.png")
    meta(f"{RES}/icons/pet-{pet}.png", ICON_META)
    print(pet, "art", art.size, "icon 96 x 96")


def spirit():
    base, _ = st.split_pair(f"{st.SRC}/acolyte-pair.png")
    w, h = round(base.width * K), round(base.height * K)
    return base.resize((w, h), Image.LANCZOS)


def imp(height):
    rgba = st.key_blue("Assets/ArtLibrary/pets/imp-chatgpt.png")
    labels, found = st.components(rgba[..., 3] > 128)
    i = max(range(len(found)), key=lambda n: found[n][0])
    keep = labels == i + 1
    for _ in range(3):
        g = keep.copy()
        g[1:] |= keep[:-1]; g[:-1] |= keep[1:]; g[:, 1:] |= keep[:, :-1]; g[:, :-1] |= keep[:, 1:]
        keep = g
    rgba = rgba.copy()
    rgba[..., 3][~keep] = 0
    # ChatGPT paints a bright red: the brief is dark red and gold. Red pixels go darker, gold (g/r above 0.45) stays.
    rgb = rgba[..., :3].astype(float)
    red = rgb[..., 1] < 0.45 * rgb[..., 0]
    rgb[red] *= 0.8
    rgba[..., :3] = rgb.round().astype(rgba.dtype)
    im = Image.fromarray(rgba, "RGBA")
    im = im.crop(im.getbbox())
    return im.resize((round(im.width * height / im.height), height), Image.LANCZOS)


SHEET = "Assets/ArtLibrary/pets/pet-monsters-chatgpt-v1.png"  # Lần 7: the game's own monsters / boss shrunk (4 x 2, refs ArtLibrary/pets/ref/pet-ref-monsters-left.png)
SHEET_IDS = [["frostSlime", "hornRabbit", "iceGoblin", "thornBoar"], ["spectralWolf", "marshStalker", "crystalGolem", "frostfangKing"]]
FX_SHEET = "Assets/ArtLibrary/pets/pet-fx-chatgpt-v1.png"
FX_IDS = [["fire", "ice", "poison", "lightning"], ["fire-hit", "ice-hit", "poison-hit", "lightning-hit"], ["shield", "heal", None, None]]


def key_magenta(path):
    """RGBA from a flat #FF00FF image: alpha from how magenta a pixel is (min(r, b) - g), the magenta fringe taken out of the outline."""
    a = np.array(Image.open(path).convert("RGB")).astype(float)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    excess = np.clip(np.minimum(r, b) - g, 0, None)
    alpha = 1 - np.clip((excess - 60) / 150.0, 0, 1)  # blush pink (r 255, g 170, b 190) has an excess of 20: kept
    out = np.empty(a.shape[:2] + (4,), np.uint8)
    m = 1 - alpha
    out[..., 0] = np.clip(r - m * 255 * (1 - alpha), 0, 255)
    out[..., 1] = np.clip(g, 0, 255)
    out[..., 2] = np.clip(b - m * 255 * (1 - alpha), 0, 255)
    out[..., 3] = np.round(alpha * 255)
    return out


def cells(path, ids):
    """One cropped RGBA per id: everything opaque whose bounding box centre falls in that grid cell (wings / sparks cut off by a gap stay with their body)."""
    rgba = key_magenta(path)
    labels, found = st.components(rgba[..., 3] > 128)
    h, w = rgba.shape[:2]
    rows, cols = len(ids), len(ids[0])
    out = {}
    for ri, row in enumerate(ids):
        for ci, pid in enumerate(row):
            if pid is None:
                continue
            keep = np.zeros((h, w), bool)
            for n, (area, x0, y0, x1, y1) in enumerate(found):
                cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
                if area >= 40 and int(cx * cols / w) == ci and int(cy * rows / h) == ri:
                    keep |= labels == n + 1
            grow = keep.copy()
            for _ in range(3):  # the anti-aliased rim around the opaque core
                g = grow.copy(); g[1:] |= grow[:-1]; g[:-1] |= grow[1:]; g[:, 1:] |= grow[:, :-1]; g[:, :-1] |= grow[:, 1:]; grow = g
            part = rgba.copy()
            part[..., 3][~grow] = 0
            im = Image.fromarray(part, "RGBA")
            out[pid] = im.crop(im.getbbox())
    return out


def sheet(height):
    pets = cells(SHEET, SHEET_IDS)
    k = height / max(im.height for im in pets.values())
    for pid, im in pets.items():
        im = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
        canvas = Image.new("RGBA", (im.width, height), (0, 0, 0, 0))
        canvas.paste(im, (0, (height - im.height) // 2), im)
        write(pid, canvas)


def fx():
    folder = f"{RES}/fx/pets"
    os.makedirs(folder, exist_ok=True)
    for name, im in cells(FX_SHEET, FX_IDS).items():
        k = 96 / max(im.width, im.height)  # 96 px on the long side; PetSkillFx sizes them in world units
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        im.save(f"{folder}/{name}.png")
        meta(f"{folder}/{name}.png", ICON_META)
        print("fx", name, im.size)


if __name__ == "__main__":
    which = sys.argv[1:] or ["spirit", "imp"]
    sp = spirit()
    if "spirit" in which:
        write("spirit", sp)
    if "imp" in which:
        write("imp", imp(sp.height))
    if "sheet" in which:
        sheet(sp.height)
    if "fx" in which:
        fx()

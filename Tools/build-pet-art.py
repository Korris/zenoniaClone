"""Pet art for HeroPet: one static image per pet, and its item icon.

Sources: spirit = base of Assets/ArtLibrary/classes/cleric/companion-src/src/acolyte-pair.png, flat #0000FF (see Tools/build-cleric-spirit.py).
         imp = Assets/ArtLibrary/pets/imp-chatgpt.png, flat #0000FF.
Writes:  Assets/Resources/Sprites/pets/<id>/art.png, Assets/Resources/Sprites/icons/pet-<id>.png, and .meta files without compression where missing.
Every pet is scaled to the spirit's height (about 213 px) and faces the same way: left.

Usage (from the project root; needs Pillow, numpy): python Tools/build-pet-art.py [spirit|imp ...]"""
import importlib.util, os, sys, uuid
from PIL import Image

spec = importlib.util.spec_from_file_location("spirit_tool", "Tools/build-cleric-spirit.py")
st = importlib.util.module_from_spec(spec)
spec.loader.exec_module(st)

RES = "Assets/Resources/Sprites"
K = st.K
ART_META = f"{RES}/classes/cleric/companion/art/acolyte-base.png.meta"
ICON_META = f"{RES}/icons/offhand-warrior-0.png.meta"
FOLDER_META = f"{RES}/classes/cleric/companion/art.meta"


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


if __name__ == "__main__":
    which = sys.argv[1:] or ["spirit", "imp"]
    sp = spirit()
    if "spirit" in which:
        write("spirit", sp)
    if "imp" in which:
        write("imp", imp(sp.height))

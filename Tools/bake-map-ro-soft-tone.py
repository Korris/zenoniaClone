"""Bakes the "RO Soft Fantasy" tone into every map's art (Built-in RP has no post-process, so the grade lives in the PNGs).

    python Tools/bake-map-ro-soft-tone.py apply      # grade ground, minimap and props of all 14 maps FROM THE ORIGINALS
    python Tools/bake-map-ro-soft-tone.py revert     # put the originals back
    python Tools/bake-map-ro-soft-tone.py status     # which files are graded, with which profile
    python Tools/bake-map-ro-soft-tone.py luts       # one .cube per profile in docs/reference/maps-ro-soft-fantasy/luts/
    python Tools/bake-map-ro-soft-tone.py preview    # before / after sheet per map in docs/reference/maps-ro-soft-fantasy/ (ground + props)

Originals: Assets/ArtLibrary/maps/ro-soft-originals~/<path under Assets>/ (Unity skips folders ending in "~"). `apply` copies a file there the first
time it touches it and always grades from that copy, so running it twice never grades twice. Tools that rebuild a ground (e.g.
build-sunken-reliquary-wide-arena.py) write their ungraded output into that folder too, then `apply` again.
Only colours change: rows, props, coordinates are untouched (nothing to rebuild on the server for the tone).

The grade is Tools/preview-map-ro-soft-tone-grade.py v2 (desaturate, leaf -> mint, teal water -> clear cyan, lift shadows, lavender shadows, cream
highlights) with one profile per biome: snow is barely lifted, caves stay darker, deserts get less cream. A prop gets the profile of the maps that
place it (most placements win), so ground and props of a map go through the same function = the same LUT.
"""
import os, re, sys, json, shutil, hashlib
from collections import Counter
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "Assets")
BACKUP = os.path.join(ASSETS, "ArtLibrary", "maps", "ro-soft-originals~")
MANIFEST = os.path.join(BACKUP, "manifest.json")
REF = os.path.join(ROOT, "docs", "reference", "maps-ro-soft-fantasy")
MAPS_CS = os.path.join(ASSETS, "Scripts", "Maps")
PROPS = "Resources/Sprites/village/props"

# gamma < 1 lifts the shadows; sat scales the desaturation (1 = v2); shadow / high scale the lavender-shadow and cream-highlight tints
PROFILES = {
    "ruins":   dict(gamma=0.80, floor=0.035, sat=1.00, shadow=1.0, high=1.0),   # the v2 test grade: Sunken, Emberfen, Bramblewood
    "meadow":  dict(gamma=0.88, floor=0.030, sat=1.08, shadow=0.8, high=0.9),   # already bright, too vivid: Willowbrook, Sunbreak, villages
    "desert":  dict(gamma=0.92, floor=0.025, sat=1.12, shadow=0.7, high=0.5),   # bright sand: less cream on top
    "snow":    dict(gamma=0.95, floor=0.020, sat=1.04, shadow=0.7, high=0.3),   # Frost Hollow is already L 0.70: tint, barely any lift
    "icecave": dict(gamma=0.86, floor=0.030, sat=1.00, shadow=1.0, high=0.5),
    "cave":    dict(gamma=0.90, floor=0.020, sat=1.00, shadow=0.7, high=0.5),   # Rootburrow / Thornheart keep their gloom
}

# map key -> (profile, ground files under Assets, source files that place its props)
T1 = "Resources/Sprites/village/tiles/"
MAPS = {
    "frost-hollow": ("snow", ["Resources/Sprites/tiles/frost-hollow-large-ground-r%dc%d.png" % (r, c) for r in range(2) for c in range(2)]
                     + ["Resources/Sprites/tiles/frost-hollow-large-ground-minimap.png"], ["FrostHollowProps.cs", "FrostHollowFrameProps.cs", "FrostHollowScatterProps.cs"]),
    "oakhaven-village": ("meadow", [T1 + "oakhaven-ground.png"], ["OakhavenVillageMap.cs"]),
    "thatch-village": ("meadow", [T1 + "thatch-village-ground.png"], ["ThatchVillageMap.cs"]),
    "willowbrook-meadows": ("meadow", [T1 + "willowbrook-large-ground-r%dc%d.png" % (r, c) for r in range(2) for c in range(2)] + [T1 + "willowbrook-large-ground-minimap.png"],
                            ["WillowbrookMeadowsProps.cs", "WillowbrookMeadowsFrameProps.cs", "WillowbrookMeadowsScatterProps.cs"]),
    "sunbreak-foothills": ("meadow", [T1 + "sunbreak-foothills-ground-r%dc%d.png" % (r, c) for r in range(2) for c in range(2)] + [T1 + "sunbreak-foothills-ground-minimap.png"],
                           ["SunbreakFoothillsProps.cs"]),
    "bramblewood-forest": ("ruins", [T1 + "bramblewood-forest-ground.png"], ["BramblewoodForestProps.cs"]),
    "rootburrow": ("cave", [T1 + "rootburrow-ground.png"], ["RootburrowMap.cs"]),
    "thornheart-chamber": ("cave", [T1 + "thornheart-chamber-ground.png"], ["ThornheartChamberMap.cs"]),
    "sunspire-oasis": ("desert", [T1 + "sunspire-oasis-ground.png"], ["SunspireOasisMap.cs"]),
    "amber-dunes": ("desert", [T1 + "amber-dunes-ground.png"], ["AmberDunesMap.cs"]),
    "scorpion-gorge": ("desert", [T1 + "scorpion-gorge-ground-r%dc%d.png" % (r, c) for r in range(2) for c in range(2)] + [T1 + "scorpion-gorge-ground-minimap.png"],
                       ["ScorpionGorgeProps.cs"]),
    "emberfen-marsh": ("ruins", [T1 + "emberfen-marsh-ground.png"], ["EmberfenMarshMap.cs"]),
    "sunken-reliquary": ("ruins", [T1 + "sunken-reliquary-ground-r0c0.png", T1 + "sunken-reliquary-ground-r0c1.png", T1 + "sunken-reliquary-ground-minimap.png"],
                         ["SunkenReliquaryMap.cs"]),
    "frostfang-den": ("icecave", [T1 + "frostfang-den-ground.png"], ["FrostfangDenMap.cs"]),
}


# ---------- the grade (same maths as Tools/preview-map-ro-soft-tone-grade.py v2, with profile knobs) ----------
def rgb_to_hsv(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx = a.max(-1); mn = a.min(-1); d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-6
    rm = m & (mx == r); gm = m & (mx == g) & ~rm; bm = m & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    s = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0)
    return h / 6.0, s, mx


def hsv_to_rgb(h, s, v):
    i = np.floor(h * 6) % 6; f = h * 6 - np.floor(h * 6)
    p = v * (1 - s); q = v * (1 - f * s); t = v * (1 - (1 - f) * s)
    out = np.zeros(h.shape + (3,), np.float32)
    for k, (R, G, B) in enumerate([(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]):
        m = i == k
        out[m, 0] = R[m]; out[m, 1] = G[m]; out[m, 2] = B[m]
    return out


def grade(a, p):
    """a: float32 HxWx3 in [0,1]; p: a PROFILES entry. Per-pixel function of the colour only, i.e. a 3D LUT."""
    h, s, v = rgb_to_hsv(a)
    deg = h * 360
    sat_k = np.full_like(s, 0.90)
    sat_k = np.where((deg > 45) & (deg < 110), 0.80, sat_k)
    sat_k = np.where((deg > 15) & (deg <= 45), 0.82, sat_k)
    sat_k = np.where((deg > 150) & (deg < 210), 1.08, sat_k)
    s = np.clip(s * np.clip(sat_k * p["sat"], 0, 1.2), 0, 1)
    shift = (np.where((deg > 60) & (deg < 130), 8.0, 0.0) + np.where((deg >= 140) & (deg < 185), 12.0, 0.0)
             + np.where((deg > 15) & (deg < 45), 4.0, 0.0))
    out = hsv_to_rgb(((deg + shift) % 360) / 360, s, v)
    out = p["floor"] + (1 - p["floor"]) * np.power(np.clip(out, 0, 1), p["gamma"])
    lum = (0.2126 * out[..., 0] + 0.7152 * out[..., 1] + 0.0722 * out[..., 2])[..., None]
    shadow_w = np.clip(1 - lum / 0.55, 0, 1) * p["shadow"]
    high_w = np.clip((lum - 0.55) / 0.45, 0, 1) * p["high"]
    out = out + shadow_w * np.array([0.035, 0.0, 0.06]) + high_w * np.array([0.03, 0.02, -0.015])
    return np.clip(out, 0, 1).astype(np.float32)


def grade_image(im, p):
    """PIL image -> graded PIL image, alpha kept untouched."""
    mode = im.mode
    rgba = im.convert("RGBA")
    a = np.asarray(rgba).astype(np.float32) / 255
    rgb = grade(a[..., :3], p)
    out = np.concatenate([rgb, a[..., 3:]], -1)
    res = Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA")
    return res.convert("RGB") if mode == "RGB" else res


# ---------- file plan ----------
def prop_profiles():
    """prop type -> profile, by the maps that place it (most placements win; ties: first map in MAPS)."""
    votes = {}
    for key, (prof, _, sources) in MAPS.items():
        for f in sources:
            path = os.path.join(MAPS_CS, f)
            if not os.path.exists(path): continue
            for t in re.findall(r'\(\s*"([A-Za-z0-9_]+)"\s*,\s*-?\d+', open(path, encoding="utf-8").read()):
                votes.setdefault(t, Counter())[prof] += 1
    out = {}
    for t, c in votes.items():
        if os.path.exists(os.path.join(ASSETS, PROPS, t + ".png")): out[t] = c.most_common(1)[0][0]
    # props no map places today (spares in the PropDefs tables): by name prefix, so a later placement already matches its map
    for f in os.listdir(os.path.join(ASSETS, PROPS)):
        t = f[:-4]
        if f.endswith(".png") and t not in out:
            out[t] = next((p for pre, p in UNPLACED_PREFIX if t.startswith(pre)), "meadow")
    return out


UNPLACED_PREFIX = [("bw", "ruins"), ("fh", "snow"), ("sg", "desert"), ("wb", "meadow"), ("sb", "meadow")]


# Grounds redrawn by ChatGPT in the RO tone (Tools/install-map-ro-redraws.py): not graded again, only their props are. Their pre-redraw
# originals stay in ro-soft-originals~ for reference; going back to them is a git revert of the redraw commit.
REDRAWN = {"oakhaven-village", "sunken-reliquary", "willowbrook-meadows", "thatch-village", "frost-hollow", "frostfang-den", "sunspire-oasis", "amber-dunes", "scorpion-gorge"}


def plan():
    """[(relative path under Assets, profile)] for every file the tone touches."""
    items = []
    for key, (prof, grounds, _) in MAPS.items():
        if key in REDRAWN: continue
        for g in grounds: items.append((g, prof))
    for t, prof in sorted(prop_profiles().items()): items.append(("%s/%s.png" % (PROPS, t), prof))
    return items


def sha(path):
    return hashlib.sha1(open(path, "rb").read()).hexdigest()[:16]


def load_manifest():
    return json.load(open(MANIFEST)) if os.path.exists(MANIFEST) else {}


def apply():
    man = load_manifest()
    items = plan()
    for rel, prof in items:
        live = os.path.join(ASSETS, rel); orig = os.path.join(BACKUP, rel)
        if not os.path.exists(live) and not os.path.exists(orig):
            print("missing", rel); continue
        if not os.path.exists(orig):
            if rel in man: raise SystemExit("%s is in the manifest but its original is gone; restore it from git first" % rel)
            os.makedirs(os.path.dirname(orig), exist_ok=True)
            shutil.copyfile(live, orig)
        graded = grade_image(Image.open(orig), PROFILES[prof])
        graded.save(live, optimize=True)
        man[rel] = {"profile": prof, "graded": sha(live)}
    json.dump(man, open(MANIFEST, "w"), indent=1, sort_keys=True)
    print("graded %d files (%d props)" % (len(items), sum(1 for r, _ in items if r.startswith(PROPS))))


def revert():
    man = load_manifest()
    n = 0
    for rel in list(man):
        orig = os.path.join(BACKUP, rel)
        if os.path.exists(orig):
            shutil.copyfile(orig, os.path.join(ASSETS, rel)); n += 1
        del man[rel]
    json.dump(man, open(MANIFEST, "w"), indent=1, sort_keys=True)
    print("restored %d originals (the copies stay in %s)" % (n, os.path.relpath(BACKUP, ROOT)))


def status():
    man = load_manifest()
    by = Counter()
    for rel, prof in plan():
        live = os.path.join(ASSETS, rel)
        e = man.get(rel)
        state = "graded" if e and os.path.exists(live) and sha(live) == e["graded"] else ("changed-after-grade" if e else "original")
        by[(prof, state)] += 1
        if state == "changed-after-grade": print("changed since apply:", rel)
    for (prof, state), n in sorted(by.items()): print("%-8s %-20s %d" % (prof, state, n))


def luts(n=33):
    d = os.path.join(REF, "luts"); os.makedirs(d, exist_ok=True)
    r = np.linspace(0, 1, n, dtype=np.float32)
    B, G, R = np.meshgrid(r, r, r, indexing="ij")
    cube = np.stack([R, G, B], -1).reshape(1, -1, 3)
    for name, p in PROFILES.items():
        lut = grade(cube, p).reshape(-1, 3)
        path = os.path.join(d, "ro-soft-%s.cube" % name)
        with open(path, "w") as f:
            f.write('TITLE "ro-soft-%s"\nLUT_3D_SIZE %d\n' % (name, n))
            for c in lut: f.write("%.5f %.5f %.5f\n" % tuple(c))
        print(path)


# ---------- preview: before / after per map (ground + a strip of its props) ----------
def _font(sz):
    try: return ImageFont.truetype("arial.ttf", sz)
    except OSError: return ImageFont.load_default()


def _ground(paths):
    """Whole ground from its chunks (minimap skipped)."""
    chunks = [p for p in paths if "minimap" not in p]
    if len(chunks) == 1: return Image.open(chunks[0]).convert("RGB")
    ims = {}
    for p in chunks:
        m = re.search(r"-r(\d)c(\d)\.png$", p); ims[(int(m.group(1)), int(m.group(2)))] = Image.open(p).convert("RGB")
    rows = max(k[0] for k in ims) + 1; cols = max(k[1] for k in ims) + 1
    w, h = ims[(0, 0)].size
    out = Image.new("RGB", (w * cols, h * rows))
    for (r, c), im in ims.items(): out.paste(im, (c * w, r * h))
    return out


def preview():
    os.makedirs(REF, exist_ok=True)
    pp = prop_profiles()
    for key, (prof, grounds, _) in MAPS.items():
        sides = []
        for root, label in ((BACKUP, "TRUOC (goc)"), (ASSETS, ("SAU (ChatGPT ve lai RO Soft Fantasy)" if key in REDRAWN else "SAU (RO Soft Fantasy, profile %s)" % prof))):
            g = _ground([os.path.join(root, x) for x in grounds])
            g.thumbnail((1400, 1400))
            props = [t for t, p in sorted(pp.items()) if p == prof and t in _props_of(key)][:14]
            strip = Image.new("RGB", (g.width, 150), (238, 236, 232))
            x = 6
            for t in props:
                f = os.path.join(root, PROPS, t + ".png")
                if not os.path.exists(f): continue
                im = Image.open(f).convert("RGBA"); im.thumbnail((140, 140))
                if x + im.width > strip.width: break
                strip.paste(im, (x, 145 - im.height), im); x += im.width + 6
            panel = Image.new("RGB", (g.width, g.height + 150 + 36), (20, 20, 28))
            ImageDraw.Draw(panel).text((10, 6), label + "  " + key, fill=(255, 255, 255), font=_font(22))
            panel.paste(g, (0, 36)); panel.paste(strip, (0, 36 + g.height))
            sides.append(panel)
        W = Image.new("RGB", (sides[0].width + sides[1].width + 10, max(s.height for s in sides)), (255, 255, 255))
        W.paste(sides[0], (0, 0)); W.paste(sides[1], (sides[0].width + 10, 0))
        p = os.path.join(REF, "%s-before-after-ro-soft.jpg" % key)
        W.save(p, quality=86); print(p)


def _props_of(key):
    out = set()
    for f in MAPS[key][2]:
        path = os.path.join(MAPS_CS, f)
        if os.path.exists(path): out.update(re.findall(r'\(\s*"([A-Za-z0-9_]+)"\s*,\s*-?\d+', open(path, encoding="utf-8").read()))
    return out


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "status"
    {"apply": apply, "revert": revert, "status": status, "luts": luts, "preview": preview}[cmd]()

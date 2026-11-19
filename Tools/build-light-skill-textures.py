"""Light-skill FX textures (Astral Cataclysm light version): procedural stand-ins until ChatGPT paints them.

All textures are grey masks on flat black (the shader reads only the red channel), so a ChatGPT image on black can replace
any of them later: `python Tools/build-light-skill-textures.py import <name> <png>` turns it grey, resizes and writes the game copy.

  gen [names...]           write procedural sources (Assets/ArtLibrary/fx/light-skill/*-procedural.png) + game copies
  import <name> <png>      game copy from a painted source (name: beam-streaks | noise-clouds | meteor-core | burst-ring | portal-veil | portal-mote)
  preview                  docs/reference/light-skill-fx/textures-preview.png (each mask tiled 2x2 on dark and on snow)

Game copies: Assets/Resources/Sprites/fx/light-skill/<name>.png (beam-streaks, noise-clouds: tileable, wrap Repeat in code).
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "Assets/ArtLibrary/fx/light-skill"
GAME = ROOT / "Assets/Resources/Sprites/fx/light-skill"
REF = ROOT / "docs/reference/light-skill-fx"
GAME_SIZE = {"beam-streaks": (128, 256), "noise-clouds": (256, 256), "meteor-core": (128, 128), "burst-ring": (256, 256),
             "portal-veil": (128, 256), "portal-mote": (64, 64), "venom-bubble": (64, 64)}


def periodic_noise(w, h, cells_x, cells_y, rng):
    """Value noise that wraps on both axes (lattice indices taken modulo the cell count), smoothstep interpolation."""
    lattice = rng.random((cells_y, cells_x))
    xs = np.arange(w) * cells_x / w
    ys = np.arange(h) * cells_y / h
    x0 = np.floor(xs).astype(int); y0 = np.floor(ys).astype(int)
    fx = xs - x0; fy = ys - y0
    fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
    x1 = (x0 + 1) % cells_x; y1 = (y0 + 1) % cells_y
    a = lattice[np.ix_(y0, x0)]; b = lattice[np.ix_(y0, x1)]
    c = lattice[np.ix_(y1, x0)]; d = lattice[np.ix_(y1, x1)]
    top = a + (b - a) * fx[None, :]
    bot = c + (d - c) * fx[None, :]
    return top + (bot - top) * fy[:, None]


def fbm(w, h, base_x, base_y, octaves, rng, gain=0.5):
    """Tileable fBm: each octave doubles the (integer) cell count, so every octave still wraps."""
    out = np.zeros((h, w)); amp = 1.0; total = 0.0
    for o in range(octaves):
        out += periodic_noise(w, h, base_x * 2 ** o, base_y * 2 ** o, rng) * amp
        total += amp; amp *= gain
    return out / total


def norm(a):
    a = a - a.min()
    return a / max(a.max(), 1e-6)


def beam_streaks(w=256, h=512, seed=7):
    """Vertical light rays: thin bright columns of varying width / brightness, broken into long dashes along y. Wraps on both axes."""
    rng = np.random.default_rng(seed)
    img = np.zeros((h, w))
    x = np.arange(w)
    for _ in range(46):
        cx = rng.random() * w; width = rng.uniform(1.2, 9.0); bright = rng.uniform(0.25, 1.0)
        d = np.minimum(np.abs(x - cx), w - np.abs(x - cx))  # wrap across x
        col = np.exp(-(d / width) ** 2) * bright
        dashes = periodic_noise(1, h, 1, int(rng.integers(2, 6)), rng)[:, 0]  # long dashes along the ray, periodic in y
        dashes = np.clip((dashes - 0.3) / 0.5, 0, 1) ** 1.5
        img += col[None, :] * dashes[:, None]
    img += fbm(w, h, 8, 4, 3, rng) * 0.18  # faint haze between rays
    img = norm(img) ** 1.15
    return img


def noise_clouds(w=512, h=512, seed=11):
    """Swirling cloud fBm with a domain warp (both wrap), mid-grey average so dissolve thresholds spread evenly."""
    rng = np.random.default_rng(seed)
    warp_x = fbm(w, h, 4, 4, 3, rng); warp_y = fbm(w, h, 4, 4, 3, rng)
    base = fbm(w, h, 6, 6, 5, rng)
    ix = (np.arange(w)[None, :] + (warp_x - 0.5) * w * 0.18).astype(int) % w
    iy = (np.arange(h)[:, None] + (warp_y - 0.5) * h * 0.18).astype(int) % h
    img = norm(base[iy, ix])
    # flatten the histogram a little (dissolve uses the value as a threshold)
    order = np.argsort(img, axis=None)
    ranks = np.empty_like(order); ranks[order] = np.arange(order.size)
    eq = (ranks / order.size).reshape(img.shape)
    return np.clip(img * 0.4 + eq * 0.6, 0, 1)


def meteor_core(s=256, seed=3):
    """Meteor head: hot white disc with a soft halo and 4 faint flare spikes, on black."""
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:s, 0:s] / (s - 1) * 2 - 1
    r = np.hypot(x, y)
    core = np.clip(1 - r / 0.22, 0, 1) ** 0.6
    halo = np.exp(-(r / 0.42) ** 2) * 0.55
    spikes = (np.exp(-(np.abs(x) / 0.025) ** 2) + np.exp(-(np.abs(y) / 0.025) ** 2)) * np.clip(1 - r, 0, 1) ** 2 * 0.35
    flicker = fbm(s, s, 6, 6, 3, rng) * 0.12 * np.clip(1 - r / 0.6, 0, 1)
    return np.clip(core + halo + spikes + flicker, 0, 1)


def burst_ring(s=512, seed=5):
    """Impact ring: a bright band at 78 % radius with ragged noise and faint radial rays inside, on black."""
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:s, 0:s] / (s - 1) * 2 - 1
    r = np.hypot(x, y); a = (np.arctan2(y, x) / (2 * np.pi) + 0.5)
    ang = periodic_noise(512, 1, 24, 1, rng)[0]  # angular noise lookup (wraps around the circle)
    ragged = ang[(a * 511).astype(int) % 512]
    band = np.exp(-((r - 0.78 - (ragged - 0.5) * 0.06) / 0.07) ** 2)
    rays_lut = periodic_noise(512, 1, 64, 1, rng)[0] ** 3
    rays = rays_lut[(a * 511).astype(int) % 512] * np.clip(r / 0.78, 0, 1) ** 2 * (r < 0.8) * 0.45
    inner = np.exp(-(r / 0.35) ** 2) * 0.15
    return np.clip(band + rays + inner, 0, 1)


def portal_veil(w=256, h=512, seed=21):
    """Map portal light column (MapPortalLightFx): few broad, soft vertical bands with long smooth swells along y. Wraps on both axes.
    Calmer than beam-streaks (no thin rays, no dashes) so the scrolling column never reads as flicker."""
    rng = np.random.default_rng(seed)
    img = np.zeros((h, w))
    x = np.arange(w)
    for _ in range(9):
        cx = rng.random() * w; width = rng.uniform(14.0, 40.0); bright = rng.uniform(0.45, 1.0)
        d = np.minimum(np.abs(x - cx), w - np.abs(x - cx))
        col = np.exp(-(d / width) ** 2) * bright
        swell = 0.55 + 0.45 * periodic_noise(1, h, 1, int(rng.integers(2, 4)), rng)[:, 0]  # slow brightness swell along the band, periodic in y
        img += col[None, :] * swell[:, None]
    img += fbm(w, h, 4, 4, 3, rng) * 0.25
    return norm(img) * 0.85 + 0.15


def portal_mote(s=128):
    """Map portal mote: round soft glow with a small bright centre, no spikes (a rising particle must not sparkle)."""
    y, x = np.mgrid[0:s, 0:s] / (s - 1) * 2 - 1
    r = np.hypot(x, y)
    return np.clip(np.exp(-(r / 0.42) ** 2) * 0.75 + np.clip(1 - r / 0.25, 0, 1) ** 1.5 * 0.4, 0, 1) * (r < 1)


def venom_bubble(s=128):
    """Mire Hydra Queen poison bubble (MireHydraLightFx particles): a filled soft disc with a brighter rim and a small highlight spot top-left,
    so a rising particle reads as a bubble (not a glow mote) even small, on the bright stone arena."""
    y, x = np.mgrid[0:s, 0:s] / (s - 1) * 2 - 1
    r = np.hypot(x, y)
    body = np.clip(1 - r / 0.82, 0, 1) ** 0.35 * 0.55
    rim = np.exp(-((r - 0.74) / 0.09) ** 2) * 0.9
    spot = np.exp(-(np.hypot(x + 0.3, y + 0.32) / 0.14) ** 2)
    return np.clip(body + rim + spot, 0, 1) * (r < 0.9)


MAKERS = {"beam-streaks": beam_streaks, "noise-clouds": noise_clouds, "meteor-core": meteor_core, "burst-ring": burst_ring,
          "portal-veil": portal_veil, "portal-mote": portal_mote, "venom-bubble": venom_bubble}


def save_grey(a, path, size=None):
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "L")
    if size: im = im.resize(size, Image.LANCZOS)
    path.parent.mkdir(parents=True, exist_ok=True)
    im.convert("RGB").save(path)
    return path


def to_grey(png):
    """Painted source -> grey mask: max channel (keeps coloured glows bright), #00FF00 key -> black."""
    rgb = np.asarray(Image.open(png).convert("RGB")).astype(float) / 255
    green_key = (rgb[..., 1] > 0.75) & (rgb[..., 0] < 0.3) & (rgb[..., 2] < 0.3)
    g = rgb.max(axis=2); g[green_key] = 0
    return g


def gen(only=None):
    for name, make in MAKERS.items():
        if only and name not in only: continue
        a = make()
        print("src ", save_grey(a, SRC / (name + "-procedural.png")))
        print("game", save_grey(a, GAME / (name + ".png"), GAME_SIZE[name]))


def import_painted(name, png):
    print("game", save_grey(to_grey(png), GAME / (name + ".png"), GAME_SIZE[name]))


def preview():
    tiles = []
    for name in MAKERS:
        g = np.asarray(Image.open(GAME / (name + ".png")).convert("L")).astype(float) / 255
        g = np.tile(g, (2, 2)) if name in ("beam-streaks", "noise-clouds", "portal-veil") else g  # seams show up at the centre lines
        g = np.asarray(Image.fromarray((g * 255).astype(np.uint8)).resize((256, 256))).astype(float) / 255
        dark = np.stack([g * 0.75 + 0.05, g * 0.9 + 0.07, g + 0.12], -1)
        snow = np.stack([0.86 - g * 0.55, 0.9 - g * 0.3, 0.95 - g * 0.05], -1)
        tiles.append(np.concatenate([dark, snow], 0))
    out = np.concatenate(tiles, 1)
    REF.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)).save(REF / "textures-preview.png")
    print(REF / "textures-preview.png")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "gen"
    if cmd == "gen": gen(sys.argv[2:])
    elif cmd == "import": import_painted(sys.argv[2], sys.argv[3])
    elif cmd == "preview": preview()
    else: print(__doc__)

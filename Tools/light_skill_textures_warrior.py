"""Warrior RO3 Soft Fantasy V1 masks (2026-11-21, rules at the top of docs/sprite-rules.md, Warrior skills redone as shader FX).

Grey masks on black like every light-skill texture (LightBlob reads the red channel, colour comes from the material: WarriorLightLook),
written by `Tools/build-light-skill-textures.py gen war-sigil war-crack war-flame` (or `python Tools/light_skill_textures_warrior.py`):
  war-sigil   Warrior war seal lying on the ground: main band at 0.955 of the quad radius (same as holy-rune-ring, so one diameter formula),
              thin inner band, 8 small blade ticks pointing outward between them, an inner ring at 0.6 with 4 diamond studs, faint fill.
              Earth Break telegraph, Blade Cyclone boundary, Inferno Crash crater, Berserker Awakening seal.
  war-crack   ground cracks: 9 jagged branches out of a small impact disc, a few forks, brighter at the centre and thinner towards the rim.
              Earth Break slam, Inferno Crash crater, Charging Strike end.
  war-flame   one upright flame tongue (1:2 quad, stands on its foot): soft teardrop body, wavy tip, brighter low core.
              Flame Cleave licks, Inferno Crash crater flames, Berserker Awakening roar.
  war-greatsword  a big sword pointing DOWN (1:3 quad, stands on its tip): blade with a bright fuller, cross guard, grip, pommel.
              Earth Break's light sword dropping into the ground (RO3 analysis 5.4: a big light sword is one of the few pieces that earns a ground contact).
Every stroke is a soft (gaussian) profile so LightBlob's dark rim / light centre tone bands (ClericLightLook.Tone) show on it (rule 6).
"""
import numpy as np


def _grid(w, h=None):
    h = h or w
    y, x = np.mgrid[0:h, 0:w]
    return x / (w - 1) * 2 - 1, y / (h - 1) * 2 - 1  # image rows go down: y = +1 is the bottom of the picture


def _seg_dist(x, y, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    t = np.clip(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy), 0, 1)
    return np.hypot(x - (ax + t * dx), y - (ay + t * dy))


def war_sigil(s=512):
    x, y = _grid(s)
    r = np.hypot(x, y); a = np.arctan2(y, x)
    def band(c, w): return np.exp(-((r - c) / w) ** 2)
    rings = band(0.955, 0.018) + band(0.875, 0.009) * 0.75 + band(0.6, 0.009) * 0.6 + band(0.915, 0.05) * 0.1
    blades = np.zeros_like(r)
    for k in range(8):                                   # small blade ticks between the two outer bands, pointing out
        t = k * np.pi / 4 + np.pi / 8
        ca, sa = np.cos(t), np.sin(t)
        along = x * ca + y * sa; across = -x * sa + y * ca
        u = (along - 0.82) / 0.11                        # 0 at the hilt (0.82) .. 1 at the tip (0.93)
        half = 0.028 * np.clip(1 - u, 0, 1) ** 0.8       # narrows to a point
        inside = (u >= 0) & (u <= 1)
        blades = np.maximum(blades, np.exp(-(np.maximum(np.abs(across) - half, 0) / 0.006) ** 2) * inside * 0.85)
        guard = np.exp(-((along - 0.815) / 0.006) ** 2) * (np.abs(across) < 0.045) * 0.7
        blades = np.maximum(blades, guard)
    studs = np.zeros_like(r)
    for k in range(4):                                   # diamond studs on the inner ring
        t = k * np.pi / 2
        cx, cy = np.cos(t) * 0.6, np.sin(t) * 0.6
        d = np.abs(x - cx) / 0.045 + np.abs(y - cy) / 0.045
        studs = np.maximum(studs, np.clip((1 - d) / 0.25, 0, 1) * 0.85)
    ticks = np.exp(-(((a * 24 / np.pi) % 1 - 0.5) / 0.08) ** 2) * (np.abs(r - 0.6) < 0.035) * 0.35  # fine marks on the inner ring
    fill = np.clip((0.95 - r) / 0.2, 0, 1) * 0.06
    return np.clip(rings + blades + studs + ticks + fill, 0, 1) * (r < 1)


def war_crack(s=512, seed=71):
    rng = np.random.default_rng(seed)
    x, y = _grid(s)
    r = np.hypot(x, y)
    d = np.full(x.shape, 9.0); wmap = np.full(x.shape, 0.02)
    def crack(x0, y0, ang, length, steps, width):
        nonlocal d, wmap
        px, py = x0, y0
        pts = []
        for i in range(steps):
            ang += rng.uniform(-0.45, 0.45)
            seg = length / steps
            qx, qy = px + np.cos(ang) * seg, py + np.sin(ang) * seg
            dist = _seg_dist(x, y, px, py, qx, qy)
            w = width * (1 - 0.7 * i / steps)            # thinner towards the end
            closer = dist / w < d / wmap
            d = np.where(closer, dist, d); wmap = np.where(closer, w, wmap)
            px, py = qx, qy
            pts.append((px, py, ang))
        return pts
    for k in range(9):
        a0 = k * 2 * np.pi / 9 + rng.uniform(-0.2, 0.2)
        pts = crack(np.cos(a0) * 0.08, np.sin(a0) * 0.08, a0, rng.uniform(0.62, 0.86), 7, 0.026)
        if k % 2 == 0:                                   # a fork off every other branch, starting on the branch itself
            fx, fy, fa = pts[3]
            crack(fx, fy, fa + rng.choice([-0.7, 0.7]), rng.uniform(0.22, 0.34), 4, 0.016)
    lines = np.exp(-(d / wmap) ** 2)
    disc = np.exp(-(r / 0.11) ** 2) * 0.9
    fade = np.clip((0.98 - r) / 0.3, 0, 1)
    return np.clip((lines * (0.55 + 0.45 * np.clip(1 - r, 0, 1)) + disc) * fade, 0, 1) * (r < 1)


def war_flame(w=128, h=256):
    x, y = _grid(w, h)
    v = (1 - y) / 2                                      # 0 bottom .. 1 top
    wave = 0.08 * np.sin(v * 9.0) * v                    # the tip bends a little
    half = 0.78 * np.clip(v / 0.18, 0, 1) ** 0.5 * np.clip(1 - v, 0, 1) ** 0.9  # round foot, pointed tip
    d = np.abs(x - wave) - half
    body = np.clip(-d / 0.12, 0, 1) * 0.75
    rim = np.exp(-(d / 0.05) ** 2) * (v > 0.04) * 0.35
    core = np.exp(-((x - wave * 0.5) / 0.22) ** 2 - ((v - 0.28) / 0.2) ** 2) * 0.45
    foot = np.clip(v / 0.06, 0, 1)
    return np.clip((body + rim + core) * foot, 0, 1)


def war_greatsword(w=128, h=384):
    x, y = _grid(w, h)
    v = (1 - y) / 2                                      # 0 bottom (tip) .. 1 top (pommel)
    u = x * w / h * 1.0                                  # square units: u -1/3..1/3 across
    blade_half = 0.085 * np.clip(v / 0.14, 0, 1) ** 0.75  # pointed tip, straight edges up to the guard
    blade = np.clip((blade_half - np.abs(u)) / 0.012, 0, 1) * (v < 0.68)
    fuller = np.exp(-(u / 0.018) ** 2) * (v > 0.08) * (v < 0.64) * 0.5
    guard = np.clip((0.024 - np.abs(v - 0.69)) / 0.008, 0, 1) * np.clip((0.2 - np.abs(u)) / 0.012, 0, 1)
    grip = np.clip((0.03 - np.abs(u)) / 0.01, 0, 1) * (v > 0.7) * (v < 0.9)
    pommel = np.clip((0.045 - np.hypot(u, (v - 0.935) * 2.0)) / 0.012, 0, 1)  # v units are 2x the u units in pixels
    body = np.maximum.reduce([blade * 0.72, guard * 0.85, grip * 0.6, pommel * 0.85])
    edge = np.exp(-((np.abs(u) - blade_half) / 0.008) ** 2) * (v < 0.68) * (v > 0.01) * 0.35
    return np.clip(body + fuller + edge, 0, 1)


MAKERS = {"war-sigil": war_sigil, "war-crack": war_crack, "war-flame": war_flame, "war-greatsword": war_greatsword}
GAME_SIZE = {"war-sigil": (512, 512), "war-crack": (256, 256), "war-flame": (64, 128), "war-greatsword": (64, 192)}


if __name__ == "__main__":
    from pathlib import Path
    from PIL import Image
    root = Path(__file__).resolve().parent.parent
    for name, make in MAKERS.items():
        a = make()
        im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "L")
        src = root / "Assets/ArtLibrary/fx/light-skill" / (name + "-procedural.png")
        src.parent.mkdir(parents=True, exist_ok=True)
        im.convert("RGB").save(src)
        game = root / "Assets/Resources/Sprites/fx/light-skill" / (name + ".png")
        im.resize(GAME_SIZE[name], Image.LANCZOS).convert("RGB").save(game)
        print("game", game)

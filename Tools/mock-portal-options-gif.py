"""Renders animated mock-ups of map-exit marker options over a real map crop, for the user to pick before anything goes into the game.

Usage: python Tools/mock-portal-options-gif.py
Output: plans/reports/portal-redesign-261113/portal-options-animated.gif (2 x 2 panels, labelled A-D) + one GIF per option.
Background: Oakhaven ground around the south gate (tiles 13-14, row 21), hero = wizard idle frame, both at game scale (64 px per tile).
All effects are procedural (numpy), so the motion shown is exactly what code can reproduce in Unity.
"""
import math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
OUT = os.path.join(ROOT, "plans/reports/portal-redesign-261113")
T = 64                     # px per tile, as the ground art
FRAMES, FPS = 24, 12
W, H = 9 * T, 6 * T        # panel: 9 x 6 tiles
GATE = (4.5 * T, 4.6 * T)  # gate centre in the panel (south exit, road leads down)


def background():
    g = Image.open(os.path.join(ROOT, "Assets/Resources/Sprites/village/tiles/oakhaven-ground.png")).convert("RGBA")
    x0, y0 = int(14 * T - W / 2), int(21.6 * T - GATE[1])
    bg = g.crop((x0, y0, x0 + W, y0 + H))
    hero = Image.open(os.path.join(ROOT, "Assets/Resources/Sprites/classes/wizard/body/armed/idle.png")).convert("RGBA").crop((0, 0, 192, 192))
    hero = hero.resize((150, 150), Image.LANCZOS)
    bg.alpha_composite(hero, (int(GATE[0] - 190), int(GATE[1] - 200)))
    return bg


def glow(size, r, color, alpha):
    """Soft round light: RGBA array of size x size."""
    yy, xx = np.mgrid[0:size, 0:size] - (size - 1) / 2.0
    d = np.sqrt(xx ** 2 + yy ** 2) / r
    a = np.clip(1 - d, 0, 1) ** 2 * alpha
    out = np.zeros((size, size, 4), np.float32); out[..., :3] = color; out[..., 3] = a
    return out


def add(canvas, rgba, cx, cy):
    """Alpha-composite a float RGBA array (0-1) centred at cx, cy."""
    im = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA")
    canvas.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


def option_a(f):
    """A: Zenonia-style golden arrow bobbing over the road, pointing out of the map, soft halo under it."""
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    t = f / FRAMES
    bob = math.sin(t * 2 * math.pi) * 6
    halo = np.zeros((90, 160, 4), np.float32)
    yy, xx = np.mgrid[0:90, 0:160]
    d = np.sqrt(((xx - 80) / 80) ** 2 + ((yy - 45) / 45) ** 2)
    halo[..., :3] = (1, 0.88, 0.55); halo[..., 3] = np.clip(1 - d, 0, 1) ** 1.6 * (0.45 + 0.1 * math.sin(t * 2 * math.pi))
    add(layer, halo, GATE[0], GATE[1] + 10)
    d2 = ImageDraw.Draw(layer)
    cx, cy = GATE[0], GATE[1] - 30 + bob
    pts = [(cx - 26, cy - 18), (cx + 26, cy - 18), (cx + 26, cy - 2), (cx + 44, cy - 2), (cx, cy + 34), (cx - 44, cy - 2), (cx - 26, cy - 2)]
    d2.polygon([(x + 3, y + 4) for x, y in pts], fill=(60, 40, 10, 110))
    d2.polygon(pts, fill=(255, 205, 70, 255), outline=(110, 60, 10, 255))
    d2.polygon([(cx - 18, cy - 12), (cx + 6, cy - 12), (cx + 6, cy + 2), (cx - 18, cy + 2)], fill=(255, 240, 170, 255))
    return layer


def option_b(f):
    """B: column of soft light rising from the ground with motes drifting up (Ragnarok / MapleStory-like), gentle, no blinking."""
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    t = f / FRAMES
    col = np.zeros((220, 120, 4), np.float32)
    yy, xx = np.mgrid[0:220, 0:120]
    horiz = np.clip(1 - np.abs(xx - 60) / 60, 0, 1) ** 1.5
    vert = np.clip(yy / 220, 0, 1) ** 1.2
    col[..., :3] = (0.85, 0.95, 1.0); col[..., 3] = horiz * vert * 0.85
    add(layer, col, GATE[0], GATE[1] - 100)
    base = np.zeros((60, 150, 4), np.float32)
    yy, xx = np.mgrid[0:60, 0:150]
    d = np.sqrt(((xx - 75) / 75) ** 2 + ((yy - 30) / 30) ** 2)
    base[..., :3] = (0.8, 0.93, 1.0); base[..., 3] = np.minimum(np.clip(1 - d, 0, 1) * 1.6, 1)
    add(layer, base, GATE[0], GATE[1] + 8)
    rng = np.random.RandomState(3)
    for i in range(14):
        ph = (t + rng.rand()) % 1.0
        x = GATE[0] + (rng.rand() - 0.5) * 90 + math.sin((ph + i) * 6) * 6
        y = GATE[1] + 5 - ph * 190
        a = math.sin(ph * math.pi)
        add(layer, glow(18, 9, (1, 1, 0.92), a), x, y)
    return layer


def option_c(f):
    """C: bright mist / light at the end of the road: the exit edge fades into soft white haze that drifts slowly."""
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    t = f / FRAMES
    haze = np.zeros((H, W, 4), np.float32)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    road = np.clip(1 - np.abs(xx - GATE[0]) / 170, 0, 1) ** 1.2
    fade = np.clip((yy - (GATE[1] - 70)) / 120, 0, 1)
    wisp = 0.85 + 0.15 * np.sin(xx / 37 + t * 2 * math.pi) * np.sin(yy / 23 - t * 2 * math.pi)
    haze[..., :3] = (1, 1, 0.97); haze[..., 3] = np.minimum(road * fade * wisp * 1.3, 0.95)
    layer.alpha_composite(Image.fromarray((np.clip(haze, 0, 1) * 255).astype(np.uint8), "RGBA"))
    rng = np.random.RandomState(5)
    for i in range(10):
        ph = (t + rng.rand()) % 1.0
        x = GATE[0] + (rng.rand() - 0.5) * 220
        y = GATE[1] + 40 - ph * 110
        add(layer, glow(12, 6, (1, 1, 0.85), math.sin(ph * math.pi) * 0.9), x, y)
    return layer


def option_d(f):
    """D: name sign only: a small wooden signpost-style banner with the target map name and a down chevron, no ground effect."""
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    t = f / FRAMES
    d = ImageDraw.Draw(layer)
    try: font = ImageFont.truetype("arialbd.ttf", 20)
    except OSError: font = ImageFont.load_default()
    text = "Willowbrook Meadows"
    tw = d.textlength(text, font=font)
    cx, cy = GATE[0], GATE[1] - 40
    d.rounded_rectangle((cx - tw / 2 - 14, cy - 18, cx + tw / 2 + 14, cy + 16), 8, fill=(40, 28, 16, 200), outline=(220, 180, 90, 255), width=2)
    d.text((cx - tw / 2, cy - 12), text, font=font, fill=(255, 235, 180, 255))
    for k in range(3):
        a = int(255 * (0.35 + 0.65 * max(0, math.sin((t - k * 0.18) * 2 * math.pi))))
        y = cy + 30 + k * 14
        d.line([(cx - 14, y), (cx, y + 9), (cx + 14, y)], fill=(255, 210, 90, a), width=4)
    return layer


def label(img, ch):
    d = ImageDraw.Draw(img)
    try: font = ImageFont.truetype("arialbd.ttf", 44)
    except OSError: font = ImageFont.load_default()
    d.rectangle((0, 0, 64, 60), fill=(20, 20, 20, 220)); d.text((16, 4), ch, font=font, fill=(255, 255, 255, 255))
    return img


def main():
    bg = background()
    opts = [("A", option_a), ("B", option_b), ("C", option_c), ("D", option_d)]
    grid_frames = []
    per = {k: [] for k, _ in opts}
    for f in range(FRAMES):
        grid = Image.new("RGB", (W * 2 + 8, H * 2 + 8), (20, 20, 20))
        for i, (k, fn) in enumerate(opts):
            p = bg.copy(); p.alpha_composite(fn(f)); p = label(p, k)
            per[k].append(p.convert("RGB"))
            grid.paste(p.convert("RGB"), ((i % 2) * (W + 8), (i // 2) * (H + 8)))
        grid_frames.append(grid)
    os.makedirs(OUT, exist_ok=True)
    ms = int(1000 / FPS)
    grid_frames[0].save(os.path.join(OUT, "portal-options-animated.gif"), save_all=True, append_images=grid_frames[1:], duration=ms, loop=0)
    for k, fr in per.items():
        fr[0].save(os.path.join(OUT, "portal-option-%s.gif" % k), save_all=True, append_images=fr[1:], duration=ms, loop=0)
    grid_frames[FRAMES // 3].save(os.path.join(OUT, "portal-options-still.png"))
    print("wrote", OUT)


if __name__ == "__main__":
    main()

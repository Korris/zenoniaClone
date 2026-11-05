"""Backdrop + collision-row helper for painted 36x24 maps (Thatchwick Village, Willowbrook Meadows).

Usage (needs Pillow):
  python Tools/build-map-backdrop.py backdrop <src.png> <dst.png>
      Resize a 3:2 ChatGPT painting to 2304x1536 (Lanczos) for Assets/Resources/Sprites/village/tiles/<id>-ground.png.
  python Tools/build-map-backdrop.py grid <src.png> <out-dir>
      Numbered 36x24 grid: full.png plus q-nw/ne/sw/se.png (18x12 tiles each, 2x zoom) to classify tiles by eye.
  python Tools/build-map-backdrop.py check <image.png> <rows.txt> <out-prefix> --start tx,ty [--exits tx,ty;tx,ty]
      Validate 24 rows x 36 chars of T g d p w B, shade solid tiles (red T, blue w, green B), flood-fill walkable tiles from
      the start (and extra --exits) and print the unreachable ones plus walkable edge tiles. Writes <prefix>-full.png and
      <prefix>-nw/ne/sw/se.png. Lines starting with '#' or empty lines in rows.txt are ignored.
"""
import sys
from PIL import Image, ImageDraw, ImageFont

COLS, ROWS = 36, 24
VALID = set("TgdpwB")
SOLID = set("Tw")


def font(size):
    for name in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def cmd_backdrop(src, dst):
    im = Image.open(src).convert("RGB")
    if abs(im.width * 2 - im.height * 3) > 3:
        sys.exit("source is not 3:2: %dx%d" % im.size)
    im.resize((2304, 1536), Image.LANCZOS).save(dst)
    print("wrote", dst)


def quadrants(img, prefix, zoom=2):
    w, h = img.size
    tw, th = w / COLS, h / ROWS
    for name, (cx, cy) in {"nw": (0, 0), "ne": (18, 0), "sw": (0, 12), "se": (18, 12)}.items():
        box = (round(cx * tw), round(cy * th), round((cx + 18) * tw), round((cy + 12) * th))
        q = img.crop(box)
        q.resize((q.width * zoom, q.height * zoom), Image.LANCZOS).save("%s-%s.png" % (prefix, name))


def draw_grid(img, labels=True):
    """Grid lines every tile, column numbers on the top row of each 12-row block and row numbers on the left, every tile labelled small."""
    d = ImageDraw.Draw(img)
    tw, th = img.width / COLS, img.height / ROWS
    f = font(max(9, int(th * 0.28)))
    for c in range(COLS + 1):
        d.line([(c * tw, 0), (c * tw, img.height)], fill=(255, 255, 255, 140) if c % 6 else (255, 255, 0, 220), width=1)
    for r in range(ROWS + 1):
        d.line([(0, r * th), (img.width, r * th)], fill=(255, 255, 255, 140) if r % 6 else (255, 255, 0, 220), width=1)
    if labels:
        for r in range(ROWS):
            for c in range(COLS):
                d.text((c * tw + 2, r * th + 1), "%d,%d" % (c, r), fill=(255, 255, 255), font=f, stroke_width=1, stroke_fill=(0, 0, 0))


def cmd_grid(src, out_dir):
    import os
    os.makedirs(out_dir, exist_ok=True)
    img = Image.open(src).convert("RGB").resize((2304, 1536), Image.LANCZOS)
    draw_grid(img)
    img.resize((1536, 1024), Image.LANCZOS).save(os.path.join(out_dir, "full.png"))
    quadrants(img, os.path.join(out_dir, "q"))
    print("wrote grid images to", out_dir)


def load_rows(path):
    rows = [l.strip() for l in open(path, encoding="utf-8") if l.strip() and not l.lstrip().startswith("#")]
    errs = []
    if len(rows) != ROWS:
        errs.append("expected %d rows, got %d" % (ROWS, len(rows)))
    for i, r in enumerate(rows):
        if len(r) != COLS:
            errs.append("row %d has %d chars" % (i, len(r)))
        bad = set(r) - VALID
        if bad:
            errs.append("row %d has invalid chars %s" % (i, sorted(bad)))
    if errs:
        sys.exit("\n".join(errs))
    return rows


def flood(rows, starts):
    seen = set()
    stack = [s for s in starts if rows[s[1]][s[0]] not in SOLID]
    while stack:
        c, r = stack.pop()
        if (c, r) in seen:
            continue
        seen.add((c, r))
        for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nc, nr = c + dc, r + dr
            if 0 <= nc < COLS and 0 <= nr < ROWS and rows[nr][nc] not in SOLID and (nc, nr) not in seen:
                stack.append((nc, nr))
    return seen


def cmd_check(image, rows_path, prefix, start, exits):
    rows = load_rows(rows_path)
    img = Image.open(image).convert("RGBA").resize((2304, 1536), Image.LANCZOS)
    ov = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(ov)
    tw, th = img.width / COLS, img.height / ROWS
    for r in range(ROWS):
        for c in range(COLS):
            ch = rows[r][c]
            col = {"T": (255, 0, 0, 105), "w": (0, 80, 255, 105), "B": (0, 220, 0, 90)}.get(ch)
            if col:
                d.rectangle([c * tw, r * th, (c + 1) * tw, (r + 1) * th], fill=col)
    starts = [start] + exits
    seen = flood(rows, starts)
    for s in starts:
        if rows[s[1]][s[0]] in SOLID:
            print("ERROR: start/exit tile %s is solid" % (s,))
    lost = [(c, r) for r in range(ROWS) for c in range(COLS) if rows[r][c] not in SOLID and (c, r) not in seen]
    for c, r in lost:
        d.ellipse([c * tw + 10, r * th + 10, (c + 1) * tw - 10, (r + 1) * th - 10], fill=(255, 0, 255, 220))
    sx, sy = start
    d.rectangle([sx * tw, sy * th, (sx + 1) * tw, (sy + 1) * th], outline=(255, 255, 0, 255), width=4)
    out = Image.alpha_composite(img, ov)
    draw_grid(out, labels=False)
    out = out.convert("RGB")
    out.resize((1536, 1024), Image.LANCZOS).save(prefix + "-full.png")
    quadrants(out, prefix)
    print("unreachable walkable tiles (%d): %s" % (len(lost), lost))
    edge = [(c, r) for r in range(ROWS) for c in range(COLS) if rows[r][c] not in SOLID and (c in (0, COLS - 1) or r in (0, ROWS - 1))]
    print("walkable edge tiles:", edge)
    print("walkable tiles reachable:", len(seen))


if __name__ == "__main__":
    a = sys.argv[1:]
    if len(a) >= 3 and a[0] == "backdrop":
        cmd_backdrop(a[1], a[2])
    elif len(a) >= 3 and a[0] == "grid":
        cmd_grid(a[1], a[2])
    elif len(a) >= 6 and a[0] == "check" and a[4] == "--start":
        st = tuple(int(x) for x in a[5].split(","))
        ex = []
        if len(a) >= 8 and a[6] == "--exits":
            ex = [tuple(int(x) for x in p.split(",")) for p in a[7].split(";")]
        cmd_check(a[1], a[2], a[3], st, ex)
    else:
        sys.exit(__doc__)

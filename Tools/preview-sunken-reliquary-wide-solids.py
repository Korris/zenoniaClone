"""Blocked-tile picture of the 58x24 Sunken Reliquary: the ground chunks as the game lays them out, red = solid row tile, yellow = prop footprint,
magenta ring = boss tile (MapSpawnTable / WantedBoss), grid every tile with row / column numbers every 5.

Usage: python Tools/preview-sunken-reliquary-wide-solids.py [out.png]   (default docs/reference/maps-ro-soft-fantasy/sunken-reliquary-wide-arena-solids.png)
"""
import os, re, sys
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TILES = os.path.join(ROOT, "Assets", "Resources", "Sprites", "village", "tiles")
CS = os.path.join(ROOT, "Assets", "Scripts", "Maps", "SunkenReliquaryMap.cs")
DEFS = os.path.join(ROOT, "Assets", "Scripts", "Maps", "VillagePropDefs.cs")
OUT = os.path.join(ROOT, "docs", "reference", "maps-ro-soft-fantasy", "sunken-reliquary-wide-arena-solids.png")
T = 64


def ground():
    parts = [Image.open(os.path.join(TILES, "sunken-reliquary-ground-r0c%d.png" % c)).convert("RGB") for c in range(2)]
    im = Image.new("RGB", (sum(p.width for p in parts), parts[0].height))
    x = 0
    for p in parts:
        im.paste(p, (x, 0)); x += p.width
    return im


def main():
    out_path = sys.argv[1] if len(sys.argv) > 1 else OUT
    cs = open(CS, encoding="utf-8").read()
    body = cs[cs.index("string[] Rows"):cs.index("};", cs.index("string[] Rows"))]
    rows = re.findall(r'"([A-Za-z]+)"', body)
    defs = {n: (int(w), int(h)) for n, w, h in re.findall(r'\{ "(\w+)", new PropDef\([\d.]+f, [\d.]+f, (\d+), (\d+)\) \}', open(DEFS, encoding="utf-8").read())}
    props = set()
    for t, x, y in re.findall(r'new MapProp\("(\w+)", (\d+), (\d+)\)', cs):
        fw, fh = defs[t]
        for dx in range(fw):
            for dy in range(fh): props.add((int(x) + dx, int(y) - dy))
    im = ground()
    ov = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(ov)
    for y, r in enumerate(rows):
        for x, c in enumerate(r):
            box = (x * T, y * T, x * T + T - 1, y * T + T - 1)
            if c == "T": d.rectangle(box, fill=(255, 30, 30, 95))
            elif (x, y) in props: d.rectangle(box, fill=(255, 200, 0, 150))
            d.rectangle(box, outline=(0, 0, 0, 45))
            if y == 0 and x % 5 == 0: d.text((x * T + 4, 4), str(x), fill=(255, 255, 255, 255))
        if y % 5 == 0: d.text((4, y * T + 4), str(y), fill=(255, 255, 255, 255))
    for f, pat in (("MapSpawnTable.cs", r"SunkenReliquaryBossTile = new Vector2Int\((\d+), (\d+)\)"),
                   (os.path.join("Rpg", "WantedBoss.cs"), r'"mireHydraQueen".*"sunkenReliquary"(?:, \w+(?:\.\w+)?){6}, (\d+), (\d+)\)')):
        m = re.search(pat, open(os.path.join(ROOT, "Assets", "Scripts", f), encoding="utf-8").read())
        bx, by = int(m.group(1)), int(m.group(2))
        d.ellipse((bx * T + 6, by * T + 6, bx * T + T - 6, by * T + T - 6), outline=(255, 0, 255, 255), width=5)
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    Image.alpha_composite(im.convert("RGBA"), ov).convert("RGB").save(out_path)
    print(out_path)


if __name__ == "__main__":
    main()

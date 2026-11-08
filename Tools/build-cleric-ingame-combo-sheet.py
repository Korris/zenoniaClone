"""In-game check of the Cleric basic attack: 4 hits x 4 facings cut from `--record-combo cleric` frame dumps (one dump per facing).

Capture (from the built player, one run per facing; 60 fps fixed step, attack held from frame 30):
  Zenonia.exe --record-combo cleric --face down --shot-path D:/temp/cap-cleric-combo/down --tp 40,30 --zoom 30 --hairstyle 2 --dummies 5 --dummy-dist 40 -screen-width 3400 -screen-height 1400 (wide: the debug HUD sits left of the hero)
Hit k's frame = the middle of its active phase in the first combo (ComboConfigs.ClericHits: 70 / 60 / 130 ms, finisher 130 / 80 / 250 ms):
FRAMES below, checked by eye on the dump. Each tile is a square crop round the hero (screen centre, the camera follows him).

  python Tools/build-cleric-ingame-combo-sheet.py CAPTURE_DIR OUT.png [crop px]
"""
import os
import sys

from PIL import Image, ImageDraw

DIRS = ("down", "up", "left", "right")
NAMES = ("1 Light Thrust", "2 Aegis Ray", "3 Halo Lance", "4 Judgment Bloom")
START, FPS = 30, 60.0
TOTAL = (260, 260, 260, 460)
ACTIVE_MID = (170, 170, 170, 260)  # ms into the hit: after the active phase, the cast flash (4 frames at 26 fps) is over and the bolt flies
FRAMES = [START + round((sum(TOTAL[:k]) + ACTIVE_MID[k]) * FPS / 1000) for k in range(4)]


def main(cap, out, crop=None):
    tiles = []
    for d in DIRS:
        row = []
        for f in FRAMES:
            im = Image.open(os.path.join(cap, d, "frame_%05d.png" % f)).convert("RGB")
            c = crop or im.height * 2 // 3
            cx, cy = im.width // 2, im.height // 2
            row.append(im.crop((cx - c // 2, cy - c // 2, cx + c // 2, cy + c // 2)))
        tiles.append(row)
    c = tiles[0][0].width
    sheet = Image.new("RGB", (60 + c * 4, 24 + c * 4), (24, 26, 32))
    dr = ImageDraw.Draw(sheet)
    for k, n in enumerate(NAMES):
        dr.text((60 + k * c + 6, 6), f"{n}  (frame {FRAMES[k]})", fill=(255, 255, 255))
    for r, d in enumerate(DIRS):
        dr.text((4, 24 + r * c + c // 2), d, fill=(255, 255, 255))
        for k in range(4):
            sheet.paste(tiles[r][k], (60 + k * c, 24 + r * c))
    sheet.save(out)
    print("->", out, sheet.size, "frames", FRAMES)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else None)

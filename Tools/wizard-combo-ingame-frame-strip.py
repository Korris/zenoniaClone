"""Strip of consecutive in-game frames of one facing of the Wizard's basic-attack combo, from a --record-combo capture folder
(Tools/capture-wizard-basic-attack-ingame.py run): every `step` frames from the first attack frame over the whole 4-hit chain (1690 ms),
hero crop as in that tool, 2 rows. Shows the chain's smoothness, which the 4-pick sheet cannot.

  python Tools/wizard-combo-ingame-frame-strip.py CAPTURE_DIR/down OUT.png [step=2] [span_ms=1690]
span_ms: how long after the first attack / move frame to show (a --record-walk capture: 1120 = two 560 ms step cycles).
"""
import os
import sys

from PIL import Image, ImageDraw

FPS, ATTACK_FROM, CHAIN_MS = 60.0, 30, 1690  # 350 x 3 + 640 ms (ComboConfigs.WizardHits 2026-11-10)
HALF = (300, 330, 300, 260)  # crop around the window centre (the camera centres the hero): left, up, right, down


def main(folder, out, step=2, span_ms=CHAIN_MS):
    first = ATTACK_FROM + 1
    last = first + round(span_ms / 1000.0 * FPS)
    frames = [f for f in range(first, last + 1, step) if os.path.exists(f"{folder}/frame_{f:05d}.png")]
    W, H = Image.open(f"{folder}/frame_{frames[0]:05d}.png").size
    CROP = (W // 2 - HALF[0], H // 2 - HALF[1], W // 2 + HALF[2], H // 2 + HALF[3])
    cw, ch = (CROP[2] - CROP[0]) // 2, (CROP[3] - CROP[1]) // 2
    per_row = (len(frames) + 1) // 2
    sheet = Image.new("RGB", (cw * per_row, ch * 2), (20, 22, 30))
    dr = ImageDraw.Draw(sheet)
    for i, f in enumerate(frames):
        im = Image.open(f"{folder}/frame_{f:05d}.png").convert("RGB").crop(CROP).resize((cw, ch))
        x, y = (i % per_row) * cw, (i // per_row) * ch
        sheet.paste(im, (x, y))
        dr.text((x + 4, y + 4), f"f{f} {round((f - first) / FPS * 1000)} ms", fill=(255, 255, 0))
    sheet.save(out)
    print(out, sheet.size, len(frames), "frames")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 2, float(sys.argv[4]) if len(sys.argv) > 4 else CHAIN_MS)

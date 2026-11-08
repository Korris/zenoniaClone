"""In-game sheet of the Mechanic's 4-hit basic attack x 4 facings (docs/reference/mechanic-basic-attack-ingame.png), from the shared test build
(copied from Tools/capture-wizard-basic-attack-ingame.py, 2026-11-08, for the yellow FX v3). Runs the player once per facing with
--record-combo mechanic (60 fps fixed step, attack held from frame 30, dummies close by), crops the hero where each hit's shot has just left
(hit start + windup + AFTER_MS; ComboConfigs.MechanicHits 210/210/210/480 ms), plus a 5th column: hit 4's cannon explosion on the dummy.

  python Tools/capture-mechanic-basic-attack-ingame.py run [EXE] [OUT_DIR]     record the four facings (default D:/temp/player-all/Zenonia.exe, D:/temp/cap-mech-fx-yellow)
  python Tools/capture-mechanic-basic-attack-ingame.py candidates [OUT_DIR]    per facing, frames around each hit's pick (to choose PICK offsets)
  python Tools/capture-mechanic-basic-attack-ingame.py sheet [OUT_DIR] [OUT.png]
"""
import os
import subprocess
import sys

from PIL import Image, ImageDraw

EXE = "D:/temp/player-all/Zenonia.exe"
OUT = "D:/temp/cap-mech-fx-yellow"
FACINGS = ["down", "right", "left", "up"]
NAMES = ["1. bullet", "2. bullet", "3. Dual Shot (big bolt)", "4. cannon", "4. explosion"]
FPS = 60.0
ATTACK_FROM = 30  # DebugCapture.RecordCombo
HIT_START_MS = [0, 210, 420, 630, 630]
WINDUP_MS = [50, 50, 50, 140, 140]
AFTER = [10, 10, 100, 90, 300]  # ms after the windup: the shot out of the muzzle; col 5: the shell has landed (77 u at 240 u/s)
PICK = {}  # (facing, hit) -> frame offset, after looking at `candidates`
W, H = 3400, 1400
CROP = (W // 2 - 640, H // 2 - 450, W // 2 + 640, H // 2 + 520)  # hero centred by the camera


def frame_of(hit, facing):
    f = ATTACK_FROM + 1 + round((HIT_START_MS[hit] + WINDUP_MS[hit] + AFTER[hit]) / 1000.0 * FPS)
    return f + PICK.get((facing, hit), 0)


def run(exe, out):
    for d in FACINGS:
        folder = f"{out}/{d}"
        os.makedirs(folder, exist_ok=True)
        args = [exe, "-screen-width", str(W), "-screen-height", str(H), "-screen-fullscreen", "0", "--record-combo", "mechanic",
                "--face", d, "--tp", "40,30", "--zoom", "30", "--dummies", "7", "--dummy-dist", "40", "--shot-path", folder]
        print(" ".join(args))
        subprocess.run(args, timeout=600)


def crop(out, d, f):
    return Image.open(f"{out}/{d}/frame_{f:05d}.png").convert("RGB").crop(CROP)


def candidates(out):
    for d in FACINGS:
        rows = []
        for hit in range(5):
            base = frame_of(hit, d)
            rows.append([(f, crop(out, d, f)) for f in range(base - 4, base + 5, 2)])
        cw, ch = (CROP[2] - CROP[0]) // 3, (CROP[3] - CROP[1]) // 3
        sheet = Image.new("RGB", (cw * 5, ch * 5), (20, 22, 30))
        dr = ImageDraw.Draw(sheet)
        for r, row in enumerate(rows):
            for c, (f, im) in enumerate(row):
                sheet.paste(im.resize((cw, ch)), (c * cw, r * ch))
                dr.text((c * cw + 4, r * ch + 4), f"hit {r + 1} f{f}", fill=(255, 255, 0))
        sheet.save(f"{out}/candidates-{d}.png")
        print(f"{out}/candidates-{d}.png")


def sheet(out, dst):
    cw, ch = (CROP[2] - CROP[0]) // 2, (CROP[3] - CROP[1]) // 2
    lw, th = 90, 40
    img = Image.new("RGB", (lw + cw * 5, th + ch * 4 + 30), (16, 20, 34))
    dr = ImageDraw.Draw(img)
    for c, n in enumerate(NAMES):
        dr.text((lw + c * cw + 8, 12), n, fill=(255, 220, 140))
    for r, d in enumerate(FACINGS):
        dr.text((8, th + r * ch + ch // 2), d, fill=(255, 220, 140))
        for c in range(5):
            img.paste(crop(out, d, frame_of(c, d)).resize((cw, ch), Image.LANCZOS), (lw + c * cw, th + r * ch))
    dr.text((lw, th + ch * 4 + 8), "In game, T0, zoom 30, --record-combo mechanic --tp 40,30 (D:/temp/player-all, 2026-11-08), FX v3 yellow (Tools/build-mechanic-attack-fx-v3.py)",
            fill=(200, 200, 200))
    img.save(dst)
    print(dst, img.size)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else ""
    if cmd == "run":
        run(sys.argv[2] if len(sys.argv) > 2 else EXE, sys.argv[3] if len(sys.argv) > 3 else OUT)
    elif cmd == "candidates":
        candidates(sys.argv[2] if len(sys.argv) > 2 else OUT)
    elif cmd == "sheet":
        sheet(sys.argv[2] if len(sys.argv) > 2 else OUT, sys.argv[3] if len(sys.argv) > 3 else "docs/reference/mechanic-basic-attack-ingame.png")
    else:
        print(__doc__)

"""Mechanic V3 (Asteria, 26/11): copy approved ChatGPT sheets from src/mechanic-v3/ into src/ under the RO Fantasy pipeline names.

The design-phase idle was painted on #FF00FF while the RO Fantasy cutters key #00FF00 (build-ro-fantasy-hero-art-check.load_rgba,
ro_fantasy_body_cut.keyed). Magenta sources are keyed here (alpha from the distance to pure magenta, magenta spill on the soft edge pulled
down to the green channel), then flattened on pure #00FF00 with a hard alpha cut, so every pipeline tool reads them as usual.
Sources already on #00FF00 are copied unchanged.

  python Tools/mechanic_v3_prepare_sources.py            convert / copy every entry of COPIES (skip when the target is newer)
"""
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "Assets/ArtLibrary/ro-fantasy-heroes/src"
V3 = SRC / "mechanic-v3"
# design-phase file (src/mechanic-v3/) -> pipeline name (src/). The old V2 sources (-chatgpt-1) stay; the higher N wins.
COPIES = {
    "body-mechanic-idle-5dir-v3-chatgpt-5.png": "body-mechanic-idle-5dir-chatgpt-2.png",
    "hair-mechanic-style1-5dir-v3-chatgpt-1.png": "hair-mechanic-style1-5dir-chatgpt-2.png",
    "hair-mechanic-style2-5dir-v3-chatgpt-1.png": "hair-mechanic-style2-5dir-chatgpt-2.png",
    "hair-mechanic-style3-5dir-v3-chatgpt-1.png": "hair-mechanic-style3-5dir-chatgpt-2.png",
    # attack up-left: the approved sheet is chatgpt-3 with cell F5 replaced by a copy of F6 (verdict-attack8-chatgpt.txt); the raw
    # chatgpt-1..3 are rejected (hand swaps) and never copied (SKIP), so the pipeline's "newest N" can only pick this one
    "body-mechanic-attack-up-left-8f-v3-chatgpt-3-f5fix.png": "body-mechanic-attack-up-left-8f-chatgpt-3.png",
}
# rejected design-phase sheets that pattern_copies() would otherwise copy (attack up: chatgpt-2 wins over -1 by N, kept)
SKIP = {f"body-mechanic-attack-up-left-8f-v3-chatgpt-{n}.png" for n in (1, 2, 3)}


def is_magenta_bg(a):
    border = np.concatenate([a[0], a[-1], a[:, 0], a[:, -1]]).astype(int)
    m = (border[:, 0] > 200) & (border[:, 2] > 200) & (border[:, 1] < 60)
    return m.mean() > 0.6


def magenta_to_green(a):
    r, g, b = [a[..., i].astype(np.float32) for i in range(3)]
    alpha = np.clip((np.maximum.reduce([g, 255 - r, 255 - b]) - 40) / 80, 0, 1)
    soft = alpha < 1
    lim = g + 10  # despill: a pink fringe has r and b far above g
    r2 = np.where(soft, np.minimum(r, np.maximum(lim, 0.6 * r)), r)
    b2 = np.where(soft, np.minimum(b, np.maximum(lim, 0.6 * b)), b)
    out = np.dstack([r2, g, b2]).clip(0, 255).astype(np.uint8)
    out[alpha < 0.5] = (0, 255, 0)
    return out


def pattern_copies():
    """8-frame motion sheets: body-mechanic-<walk|attack>-<dir>-8f-v3-chatgpt-N.png -> body-mechanic-<walk|attack>-<dir>-8f-chatgpt-N.png
    (no V2 8-frame mechanic sources exist, so the V3 N is kept)."""
    import re
    out = {}
    for p in V3.glob("body-mechanic-*-8f-v3-chatgpt-*.png"):
        m = re.match(r"^(body-mechanic-(?:walk|attack)-[a-z-]+?-8f)-v3-(chatgpt-\d+\.png)$", p.name)
        if m and p.name not in SKIP:
            out[p.name] = f"{m[1]}-{m[2]}"
    return out


def main():
    for src_name, dst_name in {**COPIES, **pattern_copies()}.items():
        src, dst = V3 / src_name, SRC / dst_name
        if dst.exists() and dst.stat().st_mtime >= src.stat().st_mtime and "--force" not in sys.argv:
            print("up to date:", dst_name)
            continue
        a = np.asarray(Image.open(src).convert("RGB"))
        if is_magenta_bg(a):
            Image.fromarray(magenta_to_green(a)).save(dst)
            print("magenta -> green:", src_name, "->", dst_name)
        else:
            shutil.copyfile(src, dst)
            print("copied:", src_name, "->", dst_name)


if __name__ == "__main__":
    main()

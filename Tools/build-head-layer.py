"""Head as its own layer, any class (2026-11-07; Warrior prototype generalised): idle facing down only (row 0 of body/bare/idle.png, 4 breathing frames).
The game draws body/headless/idle.png + heads/<style>/down.png placed per frame by head-anchors.json (HeroHeadLayer.cs).

Head sprite: a full 192 x 192 cell in the body's frame space (pivot bottom centre), the head where frame 0 has it. Frame f draws it scaled
vertically by `sy` about the cell bottom, then moved by (x, y) frame px, y down: the breathing of that class's sheet (head_layer_masks.anchors).
Per-class cut settings: Tools/head_layer_classes.py. Styles 1-4 from ChatGPT: Tools/head_layer_styles.py.

Usage (project root; Pillow, numpy, scipy):
  python Tools/build-head-layer.py CLASS split OUT_DIR            mask preview, headless/head crops, 4x head crop on #00FF00 for ChatGPT
  python Tools/build-head-layer.py CLASS write                    headless sheet row 0, heads/0/down.png, head-anchors.json, raw style 0 crop
  python Tools/build-head-layer.py CLASS style SRC N OUT|write    ChatGPT edit of the 4x crop (one head, same framing) as style N
  python Tools/build-head-layer.py CLASS sheet SRC N0 OUT|write   ChatGPT image with several heads side by side, left to right = styles N0, N0+1, ... [nograft: keep ChatGPT's face]
  python Tools/build-head-layer.py CLASS check OUT_DIR            every style x frame composited the way the game draws (+ old frames; style 0 diff)
Idle + walk in every facing (Tools/head_layer_dirs.py, neck cuts in head_layer_dir_classes.py; idle-down stays the prototype's):
  python Tools/build-head-layer.py CLASS dirs-split OUT_DIR       anchors + mask preview of every row, nothing written
  python Tools/build-head-layer.py CLASS dirs-write               headless idle/walk rows, heads/0/up.png + right.png, anchors (other anims kept)
  python Tools/build-head-layer.py CLASS dirs-ref OUT_DIR         ChatGPT inputs: style 0 head right / up 4x on #00FF00 + the 5 facing-down styles
  python Tools/build-head-layer.py CLASS dirs-sheet SRC DIR N0 OUT|write   ChatGPT image in facing DIR (up / right): style 0 copy first, then styles N0.. [match-down: copy is only a size hint, fit to the facing-down heads]
  python Tools/build-head-layer.py CLASS anims-split OUT_DIR A,B / anims-write A,B   head layer for more anims (attacks): every row and frame
  python Tools/build-head-layer.py CLASS anims-normscale A,B     head at idle size in those anims (run after anims-write / dirs-write)
  python Tools/build-head-layer.py CLASS outfit-headless OUTFIT   body/<OUTFIT>/headless (armour outfit sheets) erased with the Lv1 head masks
  python Tools/build-head-layer.py CLASS dirs-scale DIR F         scale every style's DIR head by F about the chin (Warrior right: 0.92, after any dirs-sheet write)
  python Tools/build-head-layer.py CLASS dirs-check OUT_DIR [STYLES]   written files composited per row (default styles 0-4)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np  # noqa: E402
from PIL import Image  # noqa: E402

import head_layer_core as core  # noqa: E402
import head_layer_masks as masks  # noqa: E402
from head_layer_classes import CLASSES  # noqa: E402

UP = 4
BG = (60, 70, 90)


def zoomed(rgba, crop, z=6):
    x0, y0, x1, y1 = crop
    return core.on_bg(rgba, BG).convert("RGB").crop(crop).resize(((x1 - x0) * z, (y1 - y0) * z), Image.NEAREST)


def chatgpt_crop(cell, crop):
    x0, y0, x1, y1 = crop
    im = core.on_bg(cell[y0:y1, x0:x1]).convert("RGB")
    return im.resize((im.width * UP, im.height * UP), Image.NEAREST)


def setup(cls):
    cfg = CLASSES[cls]
    fr = core.frames(cls)
    head, keep = masks.head_mask(cls, cfg, fr[0])
    return cfg, fr, head, keep


def styles_of(cls, fr, head):
    p = core.paths(cls)
    out = {"0": masks.head_cell(fr[0], head)}
    if os.path.isdir(p["heads"]):
        for d in sorted(os.listdir(p["heads"]), key=lambda s: (len(s), s)):
            f = f"{p['heads']}/{d}/down.png"
            if d != "0" and os.path.isfile(f):
                out[d] = np.array(Image.open(f).convert("RGBA")).astype(np.float64)
    return out


def check(cls, out, extra):
    cfg, fr, head, keep = setup(cls)
    anc = masks.anchors(cfg, fr)
    hl = masks.headless_rows(cfg, fr, head, keep, anc)
    styles = styles_of(cls, fr, head)
    for i, p in enumerate(extra):
        styles[f"c{i}"] = np.array(Image.open(p).convert("RGBA")).astype(np.float64)
    x0, y0, x1, y1 = cfg["crop"]
    z, y1 = 4, min(y1 + 24, core.FRAME)
    w, h = (x1 - x0) * z, (y1 - y0) * z
    rows = []
    for name, hc in styles.items():
        cells = []
        for f in range(4):
            c = core.over(hl[f], core.place_head(hc, anc[f]))
            if name == "0":
                d = np.abs(core.pm(c) - core.pm(fr[f])).max(-1)
                print(f"style 0 frame {f}: max diff {d.max():.0f}, px > 24: {(d > 24).sum()}, px > 60: {(d > 60).sum()}")
            cells.append(zoomed(c, (x0, y0, x1, y1), z))
        rows.append(cells)
    rows.append([zoomed(fr[f], (x0, y0, x1, y1), z) for f in range(4)])
    sheet = Image.new("RGB", (w * 4, h * len(rows)))
    for r, cells in enumerate(rows):
        for f, im in enumerate(cells):
            sheet.paste(im, (f * w, r * h))
    os.makedirs(out, exist_ok=True)
    sheet.save(f"{out}/{cls}-check.png")
    print("styles", list(styles), "->", f"{out}/{cls}-check.png (rows: styles, last = old frames)")


def main():
    if len(sys.argv) < 3 or sys.argv[1] not in CLASSES:
        print(__doc__)
        return
    cls, cmd = sys.argv[1], sys.argv[2]
    p = core.paths(cls)
    if cmd == "split":
        out = sys.argv[3]
        os.makedirs(out, exist_ok=True)
        cfg, fr, head, keep = setup(cls)
        crop = (0, 0, core.FRAME, 140)
        vis = np.array(core.on_bg(fr[0], BG).convert("RGB")).astype(np.float64)
        vis[head & ~keep] = vis[head & ~keep] * 0.5 + np.array([0, 255, 255]) * 0.5
        vis[keep] = vis[keep] * 0.4 + np.array([255, 0, 255]) * 0.6
        Image.fromarray(vis.astype(np.uint8)).crop(crop).resize((crop[2] * 5, crop[3] * 5), Image.NEAREST).save(f"{out}/{cls}-mask.png")
        anc = masks.anchors(cfg, fr)
        hl = masks.headless_rows(cfg, fr, head, keep, anc)
        zoomed(hl[0], crop, 5).save(f"{out}/{cls}-headless0.png")
        zoomed(masks.head_cell(fr[0], head), crop, 5).save(f"{out}/{cls}-head0.png")
        chatgpt_crop(masks.head_cell(fr[0], head), cfg["crop"]).save(f"{out}/{cls}-head0-4x-green.png")
        print("head px", int(head.sum()), "keep px", int(keep.sum()), "anchors", anc)
    elif cmd == "write":
        cfg, fr, head, keep = setup(cls)
        anc = masks.anchors(cfg, fr)
        core.write_row0(p["headless"], masks.headless_rows(cfg, fr, head, keep, anc))
        os.makedirs(f"{p['heads']}/0", exist_ok=True)
        Image.fromarray(core.u8(masks.head_cell(fr[0], head))).save(f"{p['heads']}/0/down.png")
        os.makedirs(p["art"], exist_ok=True)
        chatgpt_crop(masks.head_cell(fr[0], head), cfg["crop"]).save(f"{p['art']}/style0-down-4x-green.png")
        core.write_anchors(p["anchors"], anc)
        print("wrote", p["headless"], "row 0,", f"{p['heads']}/0/down.png,", p["anchors"])
    elif cmd in ("style", "sheet"):
        import head_layer_styles as styles
        styles.run(cls, cmd, sys.argv[3], int(sys.argv[4]), sys.argv[5], graft="nograft" not in sys.argv[6:])
    elif cmd in ("dirs-split", "dirs-write"):
        import head_layer_dirs as hd
        refs, out = hd.build(cls)
        if cmd == "dirs-split":
            import head_layer_dirs_preview as pv
            pv.masks_sheet(cls, out, f"{sys.argv[3]}/{cls}-dirs-masks.png")
        else:
            hd.write(cls, refs, out)
            print("wrote headless idle/walk, heads/0/up+right, anchors of", cls)
    elif cmd == "dirs-ref":
        import head_layer_dir_styles as ds
        ds.ref(cls, sys.argv[3])
    elif cmd in ("anims-split", "anims-write"):  # attack anims: anims-split OUT_DIR A,B,.. / anims-write A,B,..
        import head_layer_dirs as hd
        names = (sys.argv[4] if cmd == "anims-split" else sys.argv[3]).split(",")
        refs, out = hd.build(cls, anims=names)
        if cmd == "anims-split":
            import head_layer_dirs_preview as pv
            pv.masks_sheet(cls, out, f"{sys.argv[3]}/{cls}-anims-masks.png")
        else:
            hd.write(cls, refs, out, heads=False)
            print("wrote headless", names, "and anchors of", cls)
    elif cmd == "dirs-sheet":
        import head_layer_dir_styles as ds
        ds.run(cls, sys.argv[3], sys.argv[4], int(sys.argv[5]), sys.argv[6], match_down="match-down" in sys.argv[7:])
    elif cmd == "anims-normscale":  # anims-normscale A,B: head drawn at idle size in those anims (chin kept)
        import head_layer_dirs as hd
        hd.normalize_scale(cls, sys.argv[3].split(","))
    elif cmd == "outfit-headless":  # outfit-headless OUTFIT: body/<OUTFIT>/headless from the Lv1 head masks
        import head_layer_dirs as hd
        hd.outfit_headless(cls, sys.argv[3])
    elif cmd == "dirs-scale":
        import head_layer_dir_styles as ds
        ds.scale_heads(cls, sys.argv[3], float(sys.argv[4]))
    elif cmd == "dirs-check":
        import head_layer_dirs_preview as pv
        styles = [int(x) for x in sys.argv[4].split(",")] if len(sys.argv) > 4 else list(range(5))
        pv.check_sheet(cls, styles, f"{sys.argv[3]}/{cls}-dirs-check.png")
    elif cmd == "check":
        check(cls, sys.argv[3], sys.argv[4:])
    else:
        print(__doc__)


if __name__ == "__main__":
    main()

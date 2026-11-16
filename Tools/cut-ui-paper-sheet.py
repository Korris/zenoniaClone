"""Cuts one Adventure Paper UI sheet (ChatGPT art on a flat #FF00FF or #00FF00 ground) into named sprites.

One tool for every sheet of plans/261116-1206-adventure-paper-ui-rebuild (phase 02); each sheet has a small config in
Tools/ui-paper-sheets/<sheet>.json:

  source     sheet png under Assets/ArtLibrary/ui/adventure-paper/<sheet>/ (path from the repo root)
  key        "#FF00FF" or "#00FF00": the flat ground colour of the sheet
  key_mode   "all"  every key-coloured pixel becomes transparent (holes inside pieces too)        [default]
             "edge" only the ground connected to the sheet edge; key-coloured areas inside a piece stay
  out        output folder under Assets/Resources/Sprites/ui/paper/
  close      closing radius that joins the parts of one piece (default 6); min_area: smallest blob kept (default 600)
  solid_alpha  alpha (0-255) above which a pixel counts as piece when blobs are found (default 40; raise it when the soft
             glow of two neighbours touches); grow: px of soft rim kept around each blob (default 3)
  rows       pieces as the sheet shows them, row by row, left to right. Each piece is a name, or an object:
             {"name", "width" | "height" (output px, aspect kept; both = exact size), "border": [left, bottom, right, top] (9-slice, output px),
              "box": [x0, y0, x1, y1] (source px: cut this box instead of a found blob), "skip": true (drawn but not wanted),
              "hole": name (also write the piece's largest enclosed window as a white plate of the same canvas: the
              backing and mask of a portrait drawn behind a hollow frame, tinted in code)}
  hard_edge  px (sheet-wide, or per piece): removes a soft glow painted around a piece, keeping only pixels within that
             distance of its opaque body (alpha above hard_alpha, default 235); hard_glow_yellow: true when
             a yellow glow on #00FF00 keys out nearly opaque (pixels at least as green as red do not count as body)
  box_hides_blobs  true: blobs inside a piece's "box" are not counted in the rows (a piece made of separate parts,
             such as four corner brackets or a pair of laurels)
  erase      [[x0, y0, x1, y1]] source boxes made transparent first (a thin strip between two pieces that touch)
  whole      [{"name", "source", "width"}]: pictures copied without keying (full-screen backgrounds)

  py Tools/cut-ui-paper-sheet.py Tools/ui-paper-sheets/<sheet>.json check OUT.png [scale]
        pieces on dark + light ground (9-slice pieces also stretched), plus OUT-boxes.png: the sheet with numbered blobs.
        Writes no game file. Look at both before `write`.
  py Tools/cut-ui-paper-sheet.py Tools/ui-paper-sheets/<sheet>.json write
        writes the sprites, the contact sheet docs/reference/ui-redesign-261116/art-contact/<sheet>-contact.png and the
        9-slice borders into Tools/ui-paper-sheets/paper-borders.json ("<group>/<name>": [l, b, r, t], one source for PaperArt).
        .meta files are left to Unity.
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
BORDERS = f"{ROOT}/Tools/ui-paper-sheets/paper-borders.json"
CONTACT_DIR = f"{ROOT}/docs/reference/ui-redesign-261116/art-contact"
DARK, LIGHT = (30, 34, 44, 255), (225, 225, 215, 255)


def keyed(path, key, mode):
    """Key colour -> transparent. Rim pixels get alpha from their distance to the key colour and lose the key spill."""
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    on = [i for i in range(3) if key[i] > 127]                      # channels the key colour is made of
    off = [i for i in range(3) if key[i] <= 127]
    excess = np.clip(a[..., on].min(-1) - a[..., off].max(-1), 0, None)
    k = excess / 255.0                                              # 1 on the pure key colour, 0 where it does not dominate
    alpha = np.clip((1 - k) * 1.25 - 0.08, 0, 1)
    if mode == "edge":                                              # keep key-coloured areas that the ground does not reach
        lab, _ = ndi.label(alpha < 0.999)
        edge = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
        alpha = np.where(np.isin(lab, edge[edge > 0]), alpha, 1.0)
    out = a.copy()
    for i in on:
        out[..., i] -= excess * 0.9 * (alpha < 0.999)
    out = np.dstack([out, alpha * 255])
    out[alpha < 0.06] = 0
    return np.clip(out, 0, 255).astype(np.uint8)


def spec(p):
    return {"name": p} if isinstance(p, str) else p


def find_rows(img, cfg):
    """Blobs of the sheet grouped into rows (a blob whose centre lies above the row's bottom joins it), each row left to right."""
    solid = img[..., 3] > cfg.get("solid_alpha", 40)
    solid[:6, :] = solid[-6:, :] = False
    solid[:, :6] = solid[:, -6:] = False
    lab, _ = ndi.label(ndi.binary_closing(solid, iterations=cfg.get("close", 6)))
    found = [(i + 1, s) for i, s in enumerate(ndi.find_objects(lab)) if (lab[s] == i + 1).sum() > cfg.get("min_area", 600)]
    found.sort(key=lambda f: f[1][0].start + f[1][0].stop)
    rows = []
    for f in found:
        centre = (f[1][0].start + f[1][0].stop) / 2
        if rows and centre < max(g[1][0].stop for g in rows[-1]): rows[-1].append(f)
        else: rows.append([f])
    for r in rows: r.sort(key=lambda f: f[1][1].start)
    return lab, rows


def shrink(piece, p):
    """Resize on premultiplied alpha so no ground colour bleeds into the rim."""
    if "width" in p and "height" in p: size = (p["width"], p["height"])   # exact box: a few % of stretch to match a set
    elif "width" in p: size = (p["width"], max(1, round(piece.height * p["width"] / piece.width)))
    elif "height" in p: size = (max(1, round(piece.width * p["height"] / piece.height)), p["height"])
    else: return piece
    a = np.asarray(piece).astype(np.float32)
    a[..., :3] *= a[..., 3:] / 255
    small = np.asarray(Image.fromarray(a.clip(0, 255).astype(np.uint8), "RGBa").resize(size, Image.LANCZOS)).astype(np.float32)
    al = small[..., 3:]
    small[..., :3] = np.where(al > 0, small[..., :3] * 255 / np.maximum(al, 1), 0)
    return Image.fromarray(small.clip(0, 255).astype(np.uint8), "RGBA")


def pieces(cfg, boxes_png=None):
    out = {}
    for w in cfg.get("whole", []):
        out[w["name"]] = (shrink(Image.open(f"{ROOT}/{w['source']}").convert("RGBA"), w), w)
    if "source" not in cfg: return out
    img = keyed(f"{ROOT}/{cfg['source']}", tuple(int(cfg["key"][i:i + 2], 16) for i in (1, 3, 5)), cfg.get("key_mode", "all"))
    for x0, y0, x1, y1 in cfg.get("erase", []): img[y0:y1, x0:x1] = 0     # separates two pieces whose glow touches
    free = img.copy()                                               # boxed pieces are cut by their box: keep their blobs out of the rows
    for r in cfg["rows"]:
        for q in r:
            if isinstance(q, dict) and "box" in q and cfg.get("box_hides_blobs"):
                x0, y0, x1, y1 = q["box"]; free[y0:y1, x0:x1] = 0
    lab, rows = find_rows(free, cfg)
    if boxes_png:
        dbg = Image.fromarray(img).convert("RGB"); d = ImageDraw.Draw(dbg)
        for ri, r in enumerate(rows):
            for ci, (_, (ys, xs)) in enumerate(r):
                d.rectangle((xs.start, ys.start, xs.stop, ys.stop), outline=(255, 255, 0), width=3)
                d.text((xs.start + 6, ys.start + 6), f"{ri + 1}.{ci + 1} {xs.stop - xs.start}x{ys.stop - ys.start}", fill=(255, 255, 255), stroke_width=3, stroke_fill=(0, 0, 0))
        dbg.save(boxes_png); print(boxes_png)
    want = [[spec(p) for p in r] for r in cfg["rows"]]
    blobs = [[p for p in r if "box" not in p] for r in want]
    if [len(r) for r in blobs if r] != [len(r) for r in rows]:
        sys.exit(f"sheet has rows of {[len(r) for r in rows]} pieces, config expects {[len(r) for r in blobs]} (see the boxes picture)")
    queue = iter([r for r in rows])
    for r in want:
        row = iter(next(queue)) if any("box" not in p for p in r) else iter(())
        for p in r:
            if "box" in p:
                x0, y0, x1, y1 = p["box"]; cut = img[y0:y1, x0:x1].copy()
            else:
                k, (ys, xs) = next(row)
                pad = cfg.get("grow", 3) + 1
                y0, y1, x0, x1 = max(0, ys.start - pad), ys.stop + pad, max(0, xs.start - pad), xs.stop + pad
                cut = img[y0:y1, x0:x1].copy()
                cut[..., 3] = np.where(ndi.binary_dilation(lab[y0:y1, x0:x1] == k, iterations=cfg.get("grow", 3)), cut[..., 3], 0)   # the soft rim belongs to the piece
            hard = p.get("hard_edge", cfg.get("hard_edge"))
            if hard:                                                 # drop a painted glow: keep only what lies within `hard` px of the opaque body
                core = cut[..., 3] > cfg.get("hard_alpha", 235)
                if cfg.get("hard_glow_yellow"):                      # a yellow glow on green ground keys out almost opaque: tell it by hue
                    c = cut[..., :3].astype(np.int16)
                    core &= ~((c[..., 1] - c[..., 0] > -8) & (c[..., 2] < np.minimum(c[..., 0], c[..., 1]) - 60) & (c[..., 1] > 150))
                core = ndi.binary_fill_holes(ndi.binary_opening(core, iterations=2))
                cut[..., 3] = np.where(ndi.binary_dilation(core, iterations=hard), cut[..., 3], 0)
            if p.get("skip"): continue
            out[p["name"]] = (shrink(Image.fromarray(cut), p), p)
            if "hole" in p:                                          # white plate filling the piece's window, 3 px under the frame
                inside = ndi.binary_fill_holes(cut[..., 3] > 128) & (cut[..., 3] < 128)
                lab2, n2 = ndi.label(inside)
                big = lab2 == 1 + int(np.argmax(ndi.sum(inside, lab2, range(1, n2 + 1))))
                plate = np.zeros_like(cut); plate[ndi.binary_dilation(big, iterations=3)] = 255
                soft = Image.fromarray(plate).resize((plate.shape[1] * 2, plate.shape[0] * 2), Image.LANCZOS).resize((plate.shape[1], plate.shape[0]), Image.LANCZOS)
                out[p["hole"]] = (shrink(soft, p), {"name": p["hole"]})
    return out


def nine_slice(im, border, size):
    """What Unity's sliced Image draws: corners kept, edges and middle stretched. border = left, bottom, right, top."""
    l, b, r, t = border; w, h = im.size; W, H = size
    out = Image.new("RGBA", size)
    xs = [(0, l, 0, l), (l, w - r, l, W - r), (w - r, w, W - r, W)]
    ys = [(0, t, 0, t), (t, h - b, t, H - b), (h - b, h, H - b, H)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if sx1 > sx0 and sy1 > sy0 and dx1 > dx0 and dy1 > dy0:
                out.paste(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.BILINEAR), (dx0, dy0))
    return out


def contact(ps, path, scale=1.0):
    """Every piece on dark and on light ground; 9-slice pieces again stretched wide and tall."""
    shots = []
    for name, (im, p) in ps.items():
        shots.append((f"{name} {im.width}x{im.height}", im))
        if "border" in p:
            shots.append((f"{name} 9-slice {p['border']}", nine_slice(im, p["border"], (round(im.width * 1.8), round(im.height * 1.4)))))
    shots = [(t, im.resize((max(1, round(im.width * scale)), max(1, round(im.height * scale))), Image.LANCZOS)) for t, im in shots]
    width, x, y, rowh, places = 1800, 12, 12, 0, []
    for t, im in shots:
        if x + im.width + 12 > width and x > 12: x, y, rowh = 12, y + rowh + 30, 0
        places.append((t, im, x, y)); x += max(im.width, 110) + 16; rowh = max(rowh, im.height)
    h = y + rowh + 30
    sheet = Image.new("RGBA", (width, h * 2), DARK); sheet.paste(LIGHT, (0, h, width, h * 2)); d = ImageDraw.Draw(sheet)
    for t, im, x, y in places:
        for oy, ink in ((0, (255, 255, 255)), (h, (0, 0, 0))):
            sheet.alpha_composite(im, (x, y + oy)); d.text((x, y + oy + im.height + 4), t[:max(18, im.width // 6)], fill=ink)
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    sheet.convert("RGB").save(path); print(path)


def write(cfg, sheet_name):
    out = f"{ROOT}/{cfg['out']}"; os.makedirs(out, exist_ok=True)
    ps = pieces(cfg)
    borders = json.load(open(BORDERS, encoding="utf-8")) if os.path.exists(BORDERS) else {}
    group = cfg["out"].rstrip("/").split("/")[-1]
    for name, (im, p) in ps.items():
        im.save(f"{out}/{name}.png"); print(f"{name:34s} {im.width}x{im.height}")
        if "border" in p: borders[f"{group}/{name}"] = p["border"]
    json.dump(dict(sorted(borders.items())), open(BORDERS, "w", encoding="utf-8", newline="\n"), indent=1)
    contact(ps, f"{CONTACT_DIR}/{sheet_name}-contact.png")
    print("wrote", len(ps), "pieces to", out)


if __name__ == "__main__":
    if len(sys.argv) < 3: sys.exit(__doc__)
    cfg = json.load(open(sys.argv[1], encoding="utf-8"))
    if sys.argv[2] == "check":
        contact(pieces(cfg, sys.argv[3][:-4] + "-boxes.png"), sys.argv[3], float(sys.argv[4]) if len(sys.argv) > 4 else 1.0)
    elif sys.argv[2] == "write": write(cfg, os.path.splitext(os.path.basename(sys.argv[1]))[0])
    else: sys.exit(__doc__)

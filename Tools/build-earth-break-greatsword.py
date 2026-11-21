"""Cuts the ChatGPT-painted giant sword of the Warrior's EARTH BREAK (21/11/2026, sprite-rules exception: a solid object summoned by a skill may be a
painted sprite, combined with shader / particle FX) into the game sprite + its rune emission mask.

    python Tools/build-earth-break-greatsword.py <source.png> [--preview out.png]

- Source: one upright, point-down sword on flat #00FF00 (or #FF00FF: read from the top-left pixel), in
  Assets/ArtLibrary/classes/warrior/skill-vfx/earth-break-sword/.
- Key: soft edge + despill (the key colour's share removed from edge pixels), small specks dropped (largest blob only).
- Output (Resources, loaded by WarriorLightLook.PaintedSword): Sprites/classes/warrior/fx/earth-break/greatsword.png (RGBA, cropped to the sword,
  the tip on the bottom row and centred horizontally, height OUT_H) and greatsword-runes.png (grey: the pale glowing runes on the fuller,
  softly dilated, same layout) for the shader's pulsing rune glow (PaintedFxObject _EmitTex). Writes .meta files once (mipmaps on, clamp).
- --preview: the sword over dark / snow / grass swatches with the runes tinted, to check the cut by eye.
- RO trailer style (21/11 evening, "Golden Knight Judgment"): the red parts of the painted art (blade, gem, grip) are regraded to a polished
  steel-ivory blade / pale steel-blue gem / dark leather grip, the gold (crossguard, fuller, runes) is kept; `--red` keeps the original colours.
"""
import os, sys, uuid
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "Sprites", "classes", "warrior", "fx", "earth-break")
OUT_H = 768
PAD = 6
FULLER_Y = (0.33, 0.70)        # rune strip of the cropped sword (source 2: runes from y ~0.36 to ~0.68)
FULLER_HALF_W = 0.085

META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: {srgb}
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: {alpha}
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def key_out(rgb):
    """Alpha from the key colour (green or magenta, read at the corner) with a soft edge, despilled colours."""
    R, G, B = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    green = G[2, 2] > 200 and R[2, 2] < 90 and B[2, 2] < 90
    k = (G - np.maximum(R, B)) if green else (np.minimum(R, B) - G)
    alpha = 1.0 - np.clip((k - 40.0) / 90.0, 0.0, 1.0)
    out = rgb.copy()
    if green:  # despill: green never above the larger of red / blue
        out[..., 1] = np.minimum(G, np.maximum(R, B))
    else:  # despill: remove the shared red + blue excess over green
        excess = np.clip(np.minimum(R, B) - G, 0, None)
        out[..., 0] = R - excess
        out[..., 2] = B - excess
    solid = (alpha > 0.5).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid)
    if n > 1:
        big = 1 + int(np.argmax(st[1:, 4]))
        keep = cv2.dilate((lab == big).astype(np.uint8), np.ones((5, 5), np.uint8))
        alpha *= keep
    return out, alpha


def steel_regrade(rgb):
    """Red / crimson pixels (hue within ~22 degrees of red, saturated) -> polished steel-ivory: lightness kept and lifted, cool grey-blue tint,
    soft blend at the hue border so the gold edges stay clean. Gold / orange (hue 25-60) and the dark outline are untouched."""
    hsv = cv2.cvtColor(np.clip(rgb, 0, 255).astype(np.uint8), cv2.COLOR_RGB2HSV_FULL).astype(np.float32)
    h, s, v = hsv[..., 0] * 360.0 / 256.0, hsv[..., 1] / 255.0, hsv[..., 2] / 255.0
    dh = np.minimum(np.abs(h), np.abs(360.0 - h))                      # distance to red
    w = np.clip((26.0 - dh) / 10.0, 0, 1) * np.clip((s - 0.3) / 0.25, 0, 1)
    lum = (0.3 * rgb[..., 0] + 0.59 * rgb[..., 1] + 0.11 * rgb[..., 2]) / 255.0
    lift = np.clip(0.42 + lum * 1.05, 0, 1)                           # dark crimson -> mid steel, bright red -> near ivory
    steel = np.stack([lift * 0.9, lift * 0.95, lift * 1.0], -1) * 255.0
    steel = steel * 0.9 + np.array([254, 253, 248], np.float32) * 0.1 * lift[..., None]
    out = rgb * (1 - w[..., None]) + steel * w[..., None]
    # orange gold (hue 18-55) -> champagne gold: hue pushed ~9 degrees toward yellow, saturation -25 %, a little lighter (palette #F3E1A8 family)
    g = np.clip((dh - 16.0) / 8.0, 0, 1) * np.clip((62.0 - h) / 8.0, 0, 1) * (h < 90)
    hsv2 = cv2.cvtColor(np.clip(out, 0, 255).astype(np.uint8), cv2.COLOR_RGB2HSV_FULL).astype(np.float32)
    hsv2[..., 0] = np.clip(hsv2[..., 0] + 9.0 * 256.0 / 360.0 * g, 0, 255)
    hsv2[..., 1] = hsv2[..., 1] * (1 - 0.25 * g)
    hsv2[..., 2] = np.clip(hsv2[..., 2] * (1 + 0.06 * g), 0, 255)
    champ = cv2.cvtColor(hsv2.astype(np.uint8), cv2.COLOR_HSV2RGB_FULL).astype(np.float32)
    return out * (1 - g[..., None]) + champ * g[..., None]


def rune_mask(rgb, alpha):
    """The carved runes on the gold fuller: pixels clearly brighter than their surroundings (white top-hat of the luminance) inside the fuller
    strip (centre column, between the crossguard and the fuller's point; FULLER_* as fractions of the cropped sword). Gold edge highlights
    outside the strip stay unlit, so only the runes pulse."""
    R, G, B = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    lum = (0.3 * R + 0.59 * G + 0.11 * B).astype(np.float32)
    top = lum - cv2.morphologyEx(lum, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (19, 19)))
    h, w = lum.shape
    ys, xs = np.nonzero(alpha > 0.5)
    cx = xs[ys >= ys.max() - 12].mean()
    strip = np.zeros_like(lum, bool)
    strip[int(FULLER_Y[0] * h):int(FULLER_Y[1] * h), int(cx - FULLER_HALF_W * w):int(cx + FULLER_HALF_W * w)] = True
    inner = cv2.erode((alpha > 0.9).astype(np.uint8), np.ones((15, 15), np.uint8)) > 0
    m = ((top > 18) & (lum > 170) & strip & inner).astype(np.float32)
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
    glow = cv2.GaussianBlur(m, (0, 0), 3.0)
    m = np.clip(m * 0.85 + glow * 1.6, 0, 1)
    return m * (alpha > 0.5)


def write_meta(path, srgb, alpha):
    meta = path + ".meta"
    if os.path.exists(meta): return
    with open(meta, "w", newline="\n") as f:
        f.write(META.format(guid=uuid.uuid4().hex, srgb=srgb, alpha=alpha))


def main():
    args = sys.argv[1:]
    preview = args[args.index("--preview") + 1] if "--preview" in args else None
    src = args[0]
    rgb = np.asarray(Image.open(src).convert("RGB")).astype(np.float32)
    col, alpha = key_out(rgb)
    if "--red" not in args:
        col = steel_regrade(col)
    ys, xs = np.nonzero(alpha > 0.05)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    # centre on the tip column (lowest opaque rows), so the point lands on the quad's foot centre
    tip = xs[ys >= y1 - 12].mean()
    half = int(np.ceil(max(tip - x0, x1 - tip))) + PAD
    cx = int(round(tip))
    W, H = 2 * half, (y1 - y0) + PAD
    rgba = np.zeros((H, W, 4), np.float32)
    sx0, sx1 = max(0, cx - half), min(rgb.shape[1], cx + half)
    dx0 = sx0 - (cx - half)
    rgba[PAD:, dx0:dx0 + (sx1 - sx0), :3] = col[y0:y1, sx0:sx1]
    rgba[PAD:, dx0:dx0 + (sx1 - sx0), 3] = alpha[y0:y1, sx0:sx1] * 255
    runes = np.zeros((H, W), np.float32)
    runes[PAD:, dx0:dx0 + (sx1 - sx0)] = rune_mask(col[y0:y1, sx0:sx1], alpha[y0:y1, sx0:sx1])
    scale = OUT_H / H
    size = (max(2, int(round(W * scale / 2)) * 2), OUT_H)
    img = Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), "RGBA").resize(size, Image.LANCZOS)
    rimg = Image.fromarray((runes * 255).astype(np.uint8), "L").resize(size, Image.LANCZOS)
    os.makedirs(OUT_DIR, exist_ok=True)
    p1, p2 = os.path.join(OUT_DIR, "greatsword.png"), os.path.join(OUT_DIR, "greatsword-runes.png")
    img.save(p1); rimg.save(p2)
    write_meta(p1, 1, 1); write_meta(p2, 0, 0)
    print("wrote", p1, img.size, "runes cover %.1f %%" % (100.0 * (np.asarray(rimg) > 128).sum() / max(1, (np.asarray(img)[..., 3] > 128).sum())))
    if preview:
        a = np.asarray(img).astype(np.float32) / 255
        r = np.asarray(rimg).astype(np.float32)[..., None] / 255
        tiles = []
        for bg in ((40, 30, 60), (235, 238, 245), (90, 140, 70)):
            base = np.ones_like(a[..., :3]) * np.array(bg, np.float32) / 255
            c = base * (1 - a[..., 3:]) + a[..., :3] * a[..., 3:]
            c = np.clip(c + r * np.array([1.0, 0.85, 0.45]) * 0.6, 0, 1)
            tiles.append(c)
        Image.fromarray((np.concatenate(tiles, 1) * 255).astype(np.uint8)).save(preview)
        print("preview", preview)


if __name__ == "__main__":
    main()

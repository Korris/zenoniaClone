"""Writes the synthetic layered hero (hero_layered_test_figure.py) as game data for `--layered-test` (HeroRenderer.Layered.cs).

Formats (the real art pipeline, step D, must write the same):
  Sprites/hero-layered/<set>/body/<anim>.png        rows = painted dirs (down, down-right, right, up-right, up), cols = frames, cell 320 x 256
  Sprites/hero-layered/<set>/body/<anim>-mask.png   same layout, R = hair, G = outfit (HeroRecolor.shader)
  Sprites/hero-layered/<set>/body/hand/<anim>.png   fist over the weapon handle (optional), same layout
  Sprites/hero-layered/<set>/hair/<style>.png       5 cells in a row (painted dirs), grey hair (shader _HairAll)
  Sprites/hero-layered/<set>/headgear/<id>.png      5 cells in a row, real colours
  Sprites/hero-layered/<set>/weapons/<name>.png     one upright weapon, grip point in the class json
  Data/hero-layered/<set>/body.json   cell, feet, bodyPx, anims, head anchors {anim, dir 0-4, f, x, y (skull top), w (skull width), angle}
  Data/hero-layered/<set>/class.json  hair styles, weapons, grips {part, anim, dir 0-7 (Dir8 order), f, x, y, angle, behind}, headgear list
Left dirs (up-left, left, down-left) draw the mirrored right rows for body / hand / hair / headgear; grips are listed for all 8 dirs.
"""
import json
import os
import uuid

import numpy as np
from PIL import Image

import hero_recolor_core as core
import hero_layered_test_figure as fig

SET = 'test'
ANIMS = [('idle', 1), ('walk', 4), ('attack1', 4)]
MIRROR = {5: 3, 6: 2, 7: 1}                       # Dir8 index -> painted row it mirrors
WEAPON_BEHIND = {0: False, 1: False, 2: False, 3: True, 4: True, 5: True, 6: True, 7: False}
OFFHAND_BEHIND = {0: False, 1: True, 2: True, 3: True, 4: True, 5: True, 6: False, 7: False}
SWORD_GRIP, SHIELD_GRIP = (20, 146), (32, 38)
META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 1
    sRGBTexture: {srgb}
  isReadable: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 2
    aniso: 1
    wrapU: 1
    wrapV: 1
  nPOTScale: 0
  spriteMode: 0
  alphaUsage: 1
  alphaIsTransparency: {alpha}
  textureType: 0
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 4096
    textureFormat: -1
    textureCompression: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def save_png(img, path, mask=False):
    """PNG + a Unity .meta (uncompressed, like the soft sheets; masks linear and not alpha-bled). Keeps an existing meta's guid."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    meta = path + '.meta'
    guid = uuid.uuid4().hex
    if os.path.exists(meta):
        for line in open(meta, encoding='utf-8'):
            if line.startswith('guid:'):
                guid = line.split()[1]
    open(meta, 'w', encoding='utf-8', newline='\n').write(META.format(guid=guid, srgb=0 if mask else 1, alpha=0 if mask else 1))


def angle_for(anim, dir8, f):
    """Sword angle (clockwise degrees, 0 = up) for a Dir8 index: the swing mirrors on the left side."""
    side = -1 if dir8 in (5, 6, 7, 0) else 1
    if anim == 'attack1':
        return fig.SWING_ANGLE[f] * side
    return 25 * side


def write(root):
    sprites = os.path.join(root, 'Assets/Resources/Sprites/hero-layered', SET)
    data = os.path.join(root, 'Assets/Resources/Data/hero-layered', SET)
    head_list, grips = [], []
    for anim, frames in ANIMS:
        body = Image.new('RGBA', (fig.CW * frames, fig.CH * 5), (0, 0, 0, 0))
        mask = Image.new('RGBA', body.size, (0, 0, 0, 255))
        hand = Image.new('RGBA', body.size, (0, 0, 0, 0))
        hands_of = {}
        for d in range(5):
            for f in range(frames):
                img, fist, head, hands = fig.body_frame(d, anim, f)
                sheet, m = core.to_game(core.key_green(img))
                body.paste(Image.fromarray(sheet), (f * fig.CW, d * fig.CH))
                mask.paste(Image.fromarray(m), (f * fig.CW, d * fig.CH))
                hand.paste(fist, (f * fig.CW, d * fig.CH))
                head_list.append(dict(anim=anim, dir=d, f=f, **head))
                hands_of[(d, f)] = hands
        save_png(body, os.path.join(sprites, 'body', anim + '.png'))
        save_png(mask, os.path.join(sprites, 'body', anim + '-mask.png'), mask=True)
        save_png(hand, os.path.join(sprites, 'body/hand', anim + '.png'))
        for dir8 in range(8):
            for f in range(frames):
                if dir8 in MIRROR:  # the mirrored body: the right hand is the mirror of the painted left hand
                    l, r = hands_of[(MIRROR[dir8], f)]['r'], hands_of[(MIRROR[dir8], f)]['l']
                    r, l = (fig.CW - r[0], r[1]), (fig.CW - l[0], l[1])
                else:
                    r, l = hands_of[(dir8, f)]['r'], hands_of[(dir8, f)]['l']
                grips.append(dict(part='weapon', anim=anim, dir=dir8, f=f, x=r[0], y=r[1], angle=angle_for(anim, dir8, f), behind=WEAPON_BEHIND[dir8]))
                grips.append(dict(part='offhand', anim=anim, dir=dir8, f=f, x=l[0], y=l[1] + 4, angle=0, behind=OFFHAND_BEHIND[dir8]))
    hair = Image.new('RGBA', (fig.HAIR_CELL[0] * 5, fig.HAIR_CELL[1]), (0, 0, 0, 0))
    hat = Image.new('RGBA', hair.size, (0, 0, 0, 0))
    for d in range(5):
        hair.paste(Image.fromarray(core.key_green(fig.hair_cell(d))), (d * fig.HAIR_CELL[0], 0))
        hat.paste(Image.fromarray(core.key_green(fig.hat_cell(d))), (d * fig.HAIR_CELL[0], 0))
    save_png(hair, os.path.join(sprites, 'hair/0.png'))
    save_png(hat, os.path.join(sprites, 'headgear/1.png'))
    save_png(fig.sword(), os.path.join(sprites, 'weapons/sword.png'))
    save_png(fig.shield(), os.path.join(sprites, 'weapons/shield.png'))
    base = 'Sprites/hero-layered/%s/' % SET
    body_json = dict(note='Synthetic layered test body (Tools/build-hero-recolor-mask.py test-sheets). Format: Tools/hero_layered_test_sheets.py.',
                     cellW=fig.CW, cellH=fig.CH, feetY=fig.FEET, bodyPx=200, sheets=base + 'body/',
                     anims=[dict(name=a, frames=n) for a, n in ANIMS], head=head_list)
    hw, hh = fig.HAIR_CELL
    class_json = dict(note='Synthetic layered test class: hair, headgear, weapons, grips (all 8 Dir8 dirs).', body='Data/hero-layered/%s/body' % SET,
                      hair=[dict(style=0, sheet=base + 'hair/0', cellW=hw, cellH=hh, ax=fig.HAIR_AX, ay=fig.HAIR_AY, refW=2 * fig.HEAD_RX)],
                      headgear=[dict(id=1, sheet=base + 'headgear/1', cellW=hw, cellH=hh, ax=80, ay=112, refW=2 * fig.HEAD_RX, drop=0.3)],
                      weapons=[dict(part='weapon', sheet=base + 'weapons/sword', gx=SWORD_GRIP[0], gy=SWORD_GRIP[1]),
                               dict(part='offhand', sheet=base + 'weapons/shield', gx=SHIELD_GRIP[0], gy=SHIELD_GRIP[1])],
                      grips=grips)
    os.makedirs(data, exist_ok=True)
    for name, obj in (('body.json', body_json), ('class.json', class_json)):
        with open(os.path.join(data, name), 'w', encoding='utf-8', newline='\n') as fh:
            json.dump(obj, fh, separators=(',', ':'))
    print('wrote %s and %s (%d head anchors, %d grips)' % (sprites, data, len(head_list), len(grips)))

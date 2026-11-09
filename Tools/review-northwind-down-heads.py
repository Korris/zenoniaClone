"""Magnified pre-build review of every down attack frame and hairstyle."""
from pathlib import Path
import json,math
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1]
RES=Path('C:/temp/zu-copy/Assets/Resources')
OUT=ROOT/'Build/Northwind-T9-Combo/prebuild-review';OUT.mkdir(parents=True,exist_ok=True)
anchors={(a['anim'],a['dir']):a['frames'] for a in json.loads((RES/'Data/classes/wizard/head-anchors.json').read_text())['anims']}
for style in range(6):
    head=Image.open(RES/f'Sprites/classes/wizard/heads/{style}/down.png').convert('RGBA')
    board=Image.new('RGB',(6*384,4*404),'#263443');draw=ImageDraw.Draw(board)
    for n in range(1,5):
        anim=f'attack{n}';count=6 if n==4 else 4
        sheet=Image.open(RES/f'Sprites/classes/wizard/body/headless/{anim}.png').convert('RGBA')
        for f in range(count):
            a=anchors[(anim,'down')][f];s=a.get('s',1);sy=a.get('sy',1);angle=math.radians(a.get('angle',0));c=math.cos(angle);sn=math.sin(angle)
            A=c/s;B=sn/s;D=-sn/(s*sy);E=c/(s*sy)
            C=96-A*(96+a['x'])-B*(192+a['y']);F=192-D*(96+a['x'])-E*(192+a['y'])
            transformed=head.transform((192,192),Image.Transform.AFFINE,(A,B,C,D,E,F),Image.Resampling.BICUBIC)
            body=sheet.crop((f*192,0,(f+1)*192,192));body.alpha_composite(transformed)
            hand=Image.open(RES/f'Sprites/classes/wizard/body/hand/{anim}.png').crop((f*192,0,(f+1)*192,192)).convert('RGBA');body.alpha_composite(hand)
            bg=Image.new('RGBA',(192,192),'#263443');bg.alpha_composite(body)
            board.paste(bg.convert('RGB').resize((384,384),Image.Resampling.NEAREST),(f*384,(n-1)*404+20))
            draw.text((f*384+6,(n-1)*404+3),f'hair{style} / {anim} / frame{f}',fill='white')
    board.save(OUT/f'hair{style}-all-down-attacks.png')
print('Review exported: 108 down attack frames across 6 hairstyles; 2x nearest-neighbor zoom')

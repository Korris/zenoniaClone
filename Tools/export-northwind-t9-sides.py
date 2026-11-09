"""Slice the generated 4x4 T9 source into 192px frames and animated reviews."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/ArtLibrary/classes/wizard/northwind-t9-sides'
image = Image.open(OUT / 'source-v2.png').convert('RGBA')
cells = []
for row in range(4):
    for col in range(4):
        source_row = 2 if row < 2 else 3
        source_col = 0 if row in (0,2) else col
        cell = image.crop((source_col*image.width//4, source_row*image.height//4,
                           (source_col+1)*image.width//4, (source_row+1)*image.height//4))
        a = np.array(cell)
        labels, count = ndimage.label(a[:,:,3] > 128)
        if not count:
            raise ValueError(f'Empty cell {row},{col}')
        sizes = np.bincount(labels.ravel()); sizes[0] = 0
        keep = ndimage.binary_dilation(labels == sizes.argmax(), iterations=2)
        a[:,:,3] = np.where(keep, a[:,:,3], 0)
        cell = Image.fromarray(a)
        cells.append(cell.crop(cell.getbbox()))
scale = 107 / float(np.median([c.height for c in cells]))
records = []
review = Image.new('RGB', (1536, 4*404), '#263443')
draw = ImageDraw.Draw(review)
for row, (direction, animation) in enumerate([
    ('left','idle'), ('left','attack'), ('right','idle'), ('right','attack')]):
    strip = Image.new('RGBA', (768,192))
    previews = []
    for frame in range(4):
        cell = cells[row*4+frame]
        cell = cell.resize((round(cell.width*scale), round(cell.height*scale)), Image.Resampling.LANCZOS)
        body = Image.new('RGBA',(192,192))
        x, y = 96-cell.width//2, 189-cell.height
        if animation == 'idle': y += [0, -1, -2, -1][frame]
        if x < 0 or y < 0: raise ValueError('Frame exceeds canvas')
        body.alpha_composite(cell,(x,y))
        dest = OUT / 'frames' / direction / animation
        dest.mkdir(parents=True, exist_ok=True)
        body.save(dest / f'{frame}.png')
        strip.alpha_composite(body,(frame*192,0))
        # Existing hairstyle art stays a separate layer; preview uses style 0.
        head = Image.open(ROOT / 'Assets/Resources/Sprites/classes/wizard/heads/0/right.png').convert('RGBA')
        if direction == 'left': head = head.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        # Preserve the canonical head pivot; centering a cropped hair silhouette
        # shifts the face away from the neck in side view.
        pixels = np.array(body)
        yy, xx = np.indices(pixels.shape[:2])
        collar = ((pixels[:,:,:3].max(axis=2) < 105) & (pixels[:,:,3]>180)
                  & (yy < y+23) & (yy >= y) & (abs(xx-96)<25))
        labels, count = ndimage.label(collar)
        choices=[]
        for label in range(1,count+1):
            cy,cx=np.where(labels==label)
            if len(cx)>=3:
                choices.append((len(cx)-abs(float(cx.mean())-96),float(cx.mean()),float(cy.mean())))
        _, neck_x, neck_y = max(choices) if choices else (0,96,y+10)
        neck_x += 3 if direction == 'left' else -3
        neck_y += 3  # overlap the open collar instead of resting on its upper rim
        head_scale=65/70
        head = head.resize((round(192*head_scale),round(192*head_scale)),Image.Resampling.LANCZOS)
        composite = body.copy()
        composite.alpha_composite(head,(round(neck_x-96*head_scale),round(neck_y-103*head_scale)))
        bg = Image.new('RGBA',(192,192),'#263443'); bg.alpha_composite(composite)
        previews.append(bg.convert('RGB').resize((384,384),Image.Resampling.NEAREST))
        review.paste(previews[-1],(frame*384,row*404+20))
        records.append(dict(direction=direction,animation=animation,frame=frame,head=dict(x=neck_x,chin=neck_y,scale=head_scale),sole=189))
    strip.save(OUT / f'{animation}-{direction}-headless.png')
    draw.text((8,row*404+4),f'{direction} {animation} / 4 frames / separate default head preview',fill='white')
    previews[0].save(OUT / f'{animation}-{direction}.gif',save_all=True,append_images=previews[1:],duration=([180]*4 if animation=='idle' else [110,80,110,130]),loop=0)
review.save(OUT / 'review-with-head.png')
(OUT / 'frames.json').write_text(json.dumps(dict(cellSize=192,scale=scale,frames=records),indent=2))
print(f'Exported {len(records)} frames to {OUT}')

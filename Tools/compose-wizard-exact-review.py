"""Reference/redraw comparison plus the actual player outfit/hair captures."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/ArtLibrary/classes/wizard/outfits/reference-20261109-exact'
OUT = ROOT / 'Build/wizard-current-reference'


def reference(t):
    rgb = np.asarray(Image.open(ART / f'ref/t{t}-design-4x.png').convert('RGB')).astype(float)
    d = np.maximum(np.maximum(rgb[..., 0], rgb[..., 2]), 255-rgb[..., 1])
    alpha = np.clip((d-20)/50, 0, 1)
    labels, _ = ndimage.label(alpha > .5, np.ones((3, 3)))
    counts = np.bincount(labels.ravel()); counts[0] = 0
    keep = ndimage.binary_dilation(labels == counts.argmax(), iterations=2)
    alpha *= keep
    rgb = np.clip((rgb-(1-alpha[..., None])*np.array([0,255,0]))/np.maximum(alpha[..., None], .02), 0, 255)
    im = Image.fromarray(np.dstack((rgb, alpha*255)).astype(np.uint8))
    return im.crop(im.getbbox())


def fit_height(im, height, max_width):
    k = min(height/im.height, max_width/im.width)
    return im.resize((round(im.width*k), round(im.height*k)), Image.Resampling.LANCZOS)


review = Image.new('RGBA', (3200, 1120), (36,43,55,255))
draw = ImageDraw.Draw(review)
game = Image.new('RGB', (1500,700), (36,43,55))
for t in range(10):
    x, y = t%5*640, t//5*560
    design = fit_height(reference(t), 490, 300)
    redraw = Image.open(ART / f'src/t{t}-body.png').convert('RGBA')
    redraw = fit_height(redraw.crop(redraw.getbbox()), 490, 300)
    review.alpha_composite(design, (x+(320-design.width)//2, y+540-design.height))
    review.alpha_composite(redraw, (x+320+(320-redraw.width)//2, y+540-redraw.height))
    draw.text((x+12,y+12), f'T{t} USER REFERENCE', fill='white')
    draw.text((x+332,y+12), f'T{t} REDRAW (EMPTY HANDS)', fill='white')
    shot = Image.open(OUT / f'captures/t{t}_1.png')
    game.paste(shot.crop((250,130,650,570)).resize((300,330),Image.Resampling.LANCZOS), (t%5*300,t//5*350+20))
    ImageDraw.Draw(game).text((t%5*300+8,t//5*350+4),f'T{t}',fill='white')
review.save(OUT / 'reference-vs-redraw-zoom.png')
game.save(OUT / 'all-tiers-ingame.png')
hair = Image.new('RGB', (1500,350), (36,43,55))
for style in range(1,6):
    shot = Image.open(OUT / f'hair-captures/hair{style}_1.png')
    hair.paste(shot.crop((300,270,600,600)).resize((300,330),Image.Resampling.LANCZOS), ((style-1)*300,20))
    ImageDraw.Draw(hair).text(((style-1)*300+8,4),f'HAIR {style}',fill='white')
hair.save(OUT / 'five-new-hairstyles-ingame.png')
print('Wrote enlarged reference/redraw board, ten outfit captures, five hairstyle captures')

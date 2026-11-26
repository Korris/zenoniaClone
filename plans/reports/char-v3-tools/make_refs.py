"""Build reference images for the 5-class V3 redesign:
- knight silhouette proportion sheet (flat grey figures + guide lines), no design details
- per-class crop of the Asteria art direction sheet, enlarged 2x"""
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage
R='docs/reference/character-v3/'
src=np.asarray(Image.open('Assets/ArtLibrary/ro-fantasy-heroes/src/body-warrior-idle-5dir-chatgpt-6.png').convert('RGB')).astype(int)
bg=np.array([4,248,3]); fg=np.abs(src-bg).sum(2)>90
fg=ndimage.binary_opening(fg,iterations=2); fg=ndimage.binary_fill_holes(fg)
out=np.zeros_like(src); out[:]=[0,255,0]; out[fg]=[90,90,100]
im=Image.fromarray(out.astype(np.uint8)); d=ImageDraw.Draw(im)
W=im.width
for y,c,t in [(86,(255,0,0),'TOP OF SKULL y=86'),(265,(0,0,255),'CHIN y~265 (head = 33% of height)'),(625,(255,0,0),'FEET y=625 (height 539 px = 74.4% of image)')]:
    d.line([(0,y),(W,y)],fill=c,width=3); d.text((8,y-14),t,fill=c)
im.save(R+'shared-v3-refs/knight-proportion-silhouette-5dir.png')
sheet=Image.open(R+'asteria-character-art-direction-v1-6-classes.png')
boxes={'wizard':(510,40,720,320),'mechanic':(715,40,915,320),'assassin':(905,40,1150,320),'cleric':(1150,40,1370,320),'archer':(1365,40,1580,320)}
for c,b in boxes.items():
    sheet.crop(b).resize(((b[2]-b[0])*2,(b[3]-b[1])*2),Image.LANCZOS).save(R+f'{c}-v3/00-asteria-sheet-crop-{c}.png')
print('ok')

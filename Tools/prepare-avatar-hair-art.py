from pathlib import Path
import json
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ArtLibrary/avatar-hair-parts'
for cls in ['wizard','mechanic','warrior','assassin','cleric']:
    heads=ROOT/f'Assets/Resources/Sprites/classes/{cls}/heads'
    styles=[];images={};boxes=[]
    for s in range(6):
        if not (heads/str(s)/'down.png').exists():break
        styles.append(s)
        for row,d in enumerate(['down','up','right']):
            im=Image.open(heads/str(s)/f'{d}.png').convert('RGBA');images[s,row]=im;boxes.append(im.getbbox())
    crop=(min(b[0] for b in boxes)-3,min(b[1] for b in boxes)-3,max(b[2] for b in boxes)+3,max(b[3] for b in boxes)+3)
    size=max(crop[2]-crop[0],crop[3]-crop[1]);cx=(crop[0]+crop[2])/2;cy=(crop[1]+crop[3])/2
    crop=(round(cx-size/2),round(cy-size/2),round(cx-size/2)+size,round(cy-size/2)+size)
    sheet=Image.new('RGBA',(1536,1024))
    for (s,row),im in images.items():sheet.alpha_composite(im.crop(crop).resize((240,240),Image.Resampling.NEAREST),(s*256+8,row*256+8))
    for col,row in enumerate(range(3)):
        sheet.alpha_composite(images[0,row].crop(crop).resize((240,240),Image.Resampling.NEAREST),(col*256+8,3*256+8))
    folder=OUT/cls;folder.mkdir(parents=True,exist_ok=True);sheet.save(folder/'source-contact.png')
    (folder/'layout.json').write_text(json.dumps(dict(crop=crop,styles=styles,cell=256,padding=8,artSize=240)))

from pathlib import Path
from PIL import Image,ImageDraw
root=Path('Assets/ArtLibrary/classes/wizard/northwind-default-attacks')
out=Path('Build/Northwind-T9-Combo/prebuild-default')
for n in range(1,5):
    im=Image.open(root/f'attack{n}-source.png').convert('RGBA')
    count=6 if n==4 else 4;cw=im.width//count;ch=im.height//4
    board=Image.new('RGB',(count*384,1000),'#263443')
    for r in range(4):
        for f in range(count):
            left=int(cw*.25);top=int(ch*.2);right=int(cw*.75);bottom=int(ch*.5)
            cell=im.crop((f*cw+left,r*ch+top,f*cw+right,r*ch+bottom))
            bg=Image.new('RGBA',cell.size,'#263443');bg.alpha_composite(cell)
            draw=ImageDraw.Draw(bg)
            for y in range(top,bottom,10):
                draw.line((0,y-top,cell.width,y-top),fill=(120,90,70,150));draw.text((2,y-top),str(y),fill='yellow')
            for x in range(left,right,20):
                draw.line((x-left,0,x-left,cell.height),fill=(120,90,70,150));draw.text((x-left,0),str(x),fill='yellow')
            board.paste(bg.convert('RGB').resize((384,230)),(f*384,r*250+20))
            ImageDraw.Draw(board).text((f*384+4,r*250+3),f'attack{n} row{r} frame{f}',fill='white')
    board.save(out/f'collars-attack{n}.png')

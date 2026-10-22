"""Generate clean, transparent HD landmark props for Oakhaven Village.
These are hand-authored layered sprite illustrations (not gameplay/background screenshots).
Output canvases match PROP_DEFINITIONS at 4 source pixels per world unit.
"""
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path('assets/hd/props/normalized')
S = 2  # supersample vector shapes then downsample for smoother painted edges
OUT.mkdir(parents=True, exist_ok=True)
INK = (70, 48, 35, 255)
WOOD = (126, 76, 42, 255)
WOOD_L = (178, 119, 64, 255)
WOOD_H = (222, 166, 96, 255)
STONE = (122, 128, 126, 255)
STONE_L = (177, 178, 164, 255)
STONE_D = (76, 82, 81, 255)
GREEN = (63, 125, 48, 255)
GREEN_L = (118, 173, 58, 255)
GOLD = (228, 176, 74, 255)


def canvas(size):
    return Image.new('RGBA', (size[0]*S, size[1]*S), (0,0,0,0))
def scaled(points): return [(int(x*S), int(y*S)) for x,y in points]
def poly(d, pts, fill, outline=INK, width=1):
    d.polygon(scaled(pts), fill=fill)
    if outline: d.line(scaled(pts+[pts[0]]), fill=outline, width=max(1,int(width*S)), joint='curve')
def line(d, pts, fill, width=1): d.line(scaled(pts), fill=fill, width=max(1,int(width*S)), joint='curve')
def ellipse(d, box, fill, outline=INK, width=1):
    box=tuple(int(v*S) for v in box); d.ellipse(box,fill=fill,outline=outline,width=max(1,int(width*S)) if outline else 1)
def rect(d, box, fill, outline=INK, width=1, radius=0):
    box=tuple(int(v*S) for v in box)
    if radius: d.rounded_rectangle(box, radius=int(radius*S), fill=fill, outline=outline, width=max(1,int(width*S)))
    else: d.rectangle(box,fill=fill,outline=outline,width=max(1,int(width*S)) if outline else 1)
def finish(im, name):
    im=im.resize((im.width//S, im.height//S), Image.Resampling.LANCZOS)
    im.save(OUT/name)

def well():
    """Stone fountain landmark for the central plaza, painted to fit the HD village props."""
    im=canvas((128,192)); d=ImageDraw.Draw(im)
    # Soft ground shadow and broad circular stone basin.
    ellipse(d,(8,151,120,184),(25,34,24,78),None)
    ellipse(d,(8,112,120,174),(61,66,65,255),INK,2)
    ellipse(d,(14,116,114,168),(177,177,161,255),INK,1)
    # Individual stone blocks around the rim give the ring a hand-built masonry feel.
    for i in range(12):
        import math
        a=(i/12)*math.tau
        cx=64+44*math.cos(a); cy=141+20*math.sin(a)
        ellipse(d,(cx-8,cy-6,cx+8,cy+6),STONE_L if i%3 else (196,194,174,255),INK,1)
        line(d,[(cx-4,cy-2),(cx+3,cy-1)],(218,215,195,255),1)
    # Water surface with layered blue shading and glints.
    ellipse(d,(24,120,104,160),(45,83,106,255),INK,1)
    ellipse(d,(29,123,99,156),(35,133,171,255),None)
    ellipse(d,(34,126,94,151),(52,157,188,255),None)
    line(d,[(39,138),(51,134),(65,137),(78,132),(89,135)],(194,238,239,255),2)
    line(d,[(45,146),(55,144),(62,146)],(130,211,225,255),1)
    # Central carved pedestal and a small water spout.
    poly(d,[(53,116),(75,116),(72,101),(56,101)],(142,145,137,255),INK,1)
    poly(d,[(49,103),(79,103),(75,96),(53,96)],(192,190,170,255),INK,1)
    rect(d,(59,79,69,98),(129,135,128,255),INK,1)
    ellipse(d,(55,75,73,84),(195,193,173,255),INK,1)
    poly(d,[(59,76),(63,62),(67,76)],(77,179,209,255),(42,91,111,255),1)
    line(d,[(63,66),(63,77)],(213,246,247,255),1)
    # Small engraved details and highlights on the stone rim.
    for x in (24,38,86,100):
        line(d,[(x,151),(x+6,153)],(101,107,103,255),1)
    finish(im,'well.png')

def lantern():
    im=canvas((64,128)); d=ImageDraw.Draw(im)
    ellipse(d,(9,111,55,123),(28,35,23,75),None)
    rect(d,(28,27,35,112),WOOD,INK,1); line(d,[(30,31),(30,108)],WOOD_H,1)
    line(d,[(18,33),(45,33)],WOOD,3); line(d,[(21,29),(42,29)],WOOD_H,1)
    poly(d,[(21,27),(25,17),(39,17),(43,27)],WOOD,INK,1)
    rect(d,(23,27,41,47),(250,195,97,255),INK,1)
    rect(d,(26,30,38,44),(255,230,143,255),None)
    line(d,[(24,27),(24,47)],INK,1); line(d,[(40,27),(40,47)],INK,1); line(d,[(23,37),(41,37)],WOOD,1)
    poly(d,[(22,48),(42,48),(39,53),(25,53)],WOOD,INK,1)
    finish(im,'lanternPost.png')

def market():
    im=canvas((192,192)); d=ImageDraw.Draw(im)
    ellipse(d,(13,166,179,187),(28,35,23,75),None)
    # legs and counter
    for x in (32,65,127,160):
        rect(d,(x,78,x+7,164),WOOD,INK,1); line(d,[(x+2,84),(x+2,158)],WOOD_H,1)
    poly(d,[(21,89),(169,89),(179,105),(12,105)],WOOD,INK,2)
    rect(d,(18,101,174,119),(146,92,49,255),INK,1)
    rect(d,(24,104,168,112),WOOD_H,None)
    # canopy top and striped awning
    poly(d,[(13,39),(178,39),(188,65),(3,65)],(111,63,42,255),INK,2)
    for i in range(8):
        x=10+i*22
        color=(202,68,48,255) if i%2==0 else (244,221,174,255)
        poly(d,[(x,52),(x+20,52),(x+25,77),(x+4,77)],color,INK,1)
    line(d,[(14,39),(177,39)],WOOD_H,2)
    # produce baskets and jars on counter
    for x,c in [(33,(196,65,43,255)),(60,(230,169,53,255)),(91,(90,147,58,255)),(119,(196,65,43,255)),(146,(84,137,171,255))]:
        ellipse(d,(x,82,x+22,98),WOOD,INK,1); ellipse(d,(x+2,80,x+20,94),c,INK,1)
        for j in range(3): ellipse(d,(x+4+j*5,83,x+9+j*5,88),GREEN_L if j%2==0 else GOLD,None)
    finish(im,'marketStall.png')

def crops():
    im=canvas((128,64)); d=ImageDraw.Draw(im)
    poly(d,[(3,47),(63,56),(124,44),(124,55),(63,64),(3,56)],(95,64,40,255),INK,1)
    for row in range(2):
        for col in range(5):
            x=12+col*23; y=20+row*17
            ellipse(d,(x-5,y+10,x+8,y+17),(50,83,34,255),None)
            line(d,[(x,y+13),(x,y-2)],(56,111,43,255),2)
            ellipse(d,(x-7,y-2,x,y+6),GREEN_L,INK,1)
            ellipse(d,(x,y-5,x+8,y+4),(83,151,48,255),INK,1)
            line(d,[(x,y),(x+4,y-2)],(171,205,83,255),1)
    finish(im,'cropPlot.png')

def scarecrow():
    im=canvas((64,128)); d=ImageDraw.Draw(im)
    ellipse(d,(12,111,52,122),(28,35,23,75),None)
    line(d,[(32,38),(32,115)],WOOD,4); line(d,[(32,41),(32,110)],WOOD_H,1)
    line(d,[(11,52),(53,52)],WOOD,4); line(d,[(14,50),(50,50)],WOOD_H,1)
    # straw head + hat
    ellipse(d,(22,23,42,43),(216,171,102,255),INK,1)
    line(d,[(26,32),(29,32)],INK,1); line(d,[(36,32),(39,32)],INK,1)
    poly(d,[(14,24),(32,12),(50,24)],WOOD,INK,1); poly(d,[(11,24),(53,24),(48,29),(16,29)],WOOD_H,INK,1)
    # tunic sleeves
    poly(d,[(22,43),(42,43),(46,67),(18,67)],(138,71,45,255),INK,1)
    poly(d,[(22,45),(28,45),(27,63),(21,63)],(215,177,93,255),None)
    line(d,[(32,45),(32,66)],WOOD_H,1)
    for x,y in [(18,51),(44,51),(20,61),(42,61)]: line(d,[(x,y),(x-3,y+5)],(219,190,117,255),1)
    finish(im,'scarecrow.png')

def woodpile():
    im=canvas((128,128)); d=ImageDraw.Draw(im)
    ellipse(d,(8,103,119,121),(28,35,23,75),None)
    # stacked cut logs
    for row in range(3):
        y=37+row*22
        for col in range(3):
            x=12+col*34+(row%2)*7
            poly(d,[(x,y+4),(x+22,y),(x+31,y+7),(x+9,y+13)],(136,83,43,255),INK,1)
            poly(d,[(x+9,y+13),(x+31,y+7),(x+31,y+14),(x+9,y+21)],(100,57,34,255),INK,1)
            ellipse(d,(x-1,y+3,x+10,y+15),(205,156,92,255),INK,1)
            ellipse(d,(x+2,y+6,x+7,y+12),(146,94,49,255),WOOD,1)
    # tie rope
    line(d,[(30,30),(34,94)],(215,181,111,255),2)
    finish(im,'woodpile.png')

def campfire():
    im=canvas((64,64)); d=ImageDraw.Draw(im)
    ellipse(d,(4,49,60,61),(28,35,23,75),None)
    for a,b in [((8,48),(55,57)),((10,57),(52,47))]: line(d,[a,b],WOOD,5); line(d,[a,b],WOOD_H,1)
    poly(d,[(22,49),(18,34),(27,39),(31,17),(38,37),(46,30),(43,49)],(227,99,35,255),INK,1)
    poly(d,[(27,48),(27,37),(32,27),(36,41),(41,36),(39,49)],(255,199,68,255),None)
    poly(d,[(31,48),(33,38),(36,46)],(255,239,149,255),None)
    finish(im,'campfire.png')

def barrel():
    im=canvas((64,64)); d=ImageDraw.Draw(im)
    ellipse(d,(9,51,55,61),(28,35,23,75),None)
    poly(d,[(16,12),(48,12),(51,47),(45,55),(19,55),(13,47)],WOOD,INK,1)
    ellipse(d,(16,8,48,19),WOOD_H,INK,1); ellipse(d,(20,10,44,17),(143,91,49,255),WOOD,1)
    for x in (18,45): line(d,[(x,17),(x+1,47)],WOOD_H,2)
    for y in (20,45):
        line(d,[(15,y),(49,y)],(76,83,83,255),4); line(d,[(15,y-1),(49,y-1)],(166,169,157,255),1)
    line(d,[(26,18),(27,44)],(206,148,85,255),1)
    finish(im,'barrel.png')

def crate():
    im=canvas((64,64)); d=ImageDraw.Draw(im)
    ellipse(d,(8,50,56,61),(28,35,23,75),None)
    poly(d,[(12,20),(43,13),(53,22),(22,30)],(188,130,71,255),INK,1)
    poly(d,[(22,30),(53,22),(53,47),(22,56)],WOOD,INK,1)
    poly(d,[(12,20),(22,30),(22,56),(12,46)],(106,66,39,255),INK,1)
    line(d,[(16,22),(25,29),(25,52)],WOOD_H,2)
    line(d,[(18,38),(49,30)],WOOD_H,3); line(d,[(19,47),(49,39)],(82,51,32,255),3)
    finish(im,'crate.png')

def bridge_arch():
    """A single 3x2-tile bridge overlay so the river crossing reads as one structure."""
    im=canvas((192,128)); d=ImageDraw.Draw(im)
    ellipse(d,(6,100,186,125),(20,35,28,75),None)
    # Stone abutments on each bank.
    for x in (3,166):
        rect(d,(x,43,x+22,104),STONE_D,INK,1)
        for yy in (49,64,79,94):
            line(d,[(x+2,yy),(x+20,yy)],STONE_L,1)
        for xx in (x+7,x+16):
            line(d,[(xx,45),(xx,60)],(205,203,183,255),1)
    # Wooden deck: slight perspective taper, planks laid across the walking direction.
    poly(d,[(14,39),(177,39),(184,91),(8,91)],(93,56,35,255),INK,2)
    poly(d,[(17,42),(174,42),(179,86),(13,86)],(157,99,52,255),INK,1)
    for x in range(20,174,12):
        line(d,[(x,43),(x+2,85)],(88,53,34,255),1)
        line(d,[(x+3,44),(x+5,84)],(206,151,82,255),1)
    # Guard rails and chunky posts on both long sides.
    line(d,[(11,38),(180,38)],(74,43,29,255),6)
    line(d,[(11,38),(180,38)],WOOD_H,2)
    line(d,[(8,91),(184,91)],(74,43,29,255),6)
    line(d,[(8,91),(184,91)],WOOD_H,2)
    for x in (15,48,81,114,147,177):
        rect(d,(x-4,29,x+4,47),(107,66,37,255),INK,1)
        rect(d,(x-3,30,x+3,34),WOOD_H,None)
        rect(d,(x-4,84,x+4,101),(107,66,37,255),INK,1)
        rect(d,(x-3,85,x+3,89),WOOD_H,None)
    # Rope/iron accents keep the silhouette legible at small game sizes.
    line(d,[(17,33),(47,33),(81,34),(114,33),(148,33),(177,33)],(204,166,104,255),2)
    line(d,[(16,97),(48,97),(81,98),(114,97),(148,97),(177,97)],(204,166,104,255),2)
    finish(im,'bridgeArch.png')

def rowboat():
    im=canvas((128,64)); d=ImageDraw.Draw(im)
    ellipse(d,(5,46,122,60),(28,35,23,75),None)
    poly(d,[(5,27),(20,38),(104,38),(122,27),(111,48),(96,57),(29,57),(14,48)],(97,54,34,255),INK,2)
    poly(d,[(12,29),(25,39),(100,39),(115,29),(104,45),(28,45)],(166,104,53,255),INK,1)
    for x in (30,46,62,78,94): line(d,[(x,40),(x,48)],(94,57,35,255),1)
    line(d,[(24,40),(103,40)],WOOD_H,2)
    # interior boards
    for y in (43,47): line(d,[(30,y),(98,y)],(210,155,83,255),1)
    finish(im,'rowboat.png')


def elder_house():
    """Compose the elder manor from existing painterly house art plus a broad entrance.

    Reuses the project's own blue-roof house sprite for all visible architecture so the landmark
    stays stylistically coherent with the existing cottages. The side wings sit behind the larger
    central facade; steps, banners and planters are added as restrained foreground details.
    """
    from PIL import Image, ImageDraw
    src = OUT / 'house3.png'
    if not src.exists():
        print('WARN elderHouse skipped: normalized house3.png is missing; run npm run hd:props first')
        return
    W,H,S = 448,336,2
    im=Image.new('RGBA',(W*S,H*S),(0,0,0,0))
    # Side wings use the same original art at a smaller scale and are partially occluded by the hall.
    wing=Image.open(src).convert('RGBA').resize((184*S,138*S),Image.Resampling.LANCZOS)
    im.alpha_composite(wing,(-5*S,83*S))
    im.alpha_composite(wing,(269*S,83*S))
    main=Image.open(src).convert('RGBA').resize((306*S,230*S),Image.Resampling.LANCZOS)
    im.alpha_composite(main,(71*S,0))
    d=ImageDraw.Draw(im)
    def pts(seq): return [(int(x*S),int(y*S)) for x,y in seq]
    def poly(seq,fill,outline=(66,48,36,255),width=1):
        pp=pts(seq); d.polygon(pp,fill=fill)
        if outline: d.line(pp+[pp[0]],fill=outline,width=max(1,width*S),joint='curve')
    def rect(box,fill,outline=(66,48,36,255),width=1):
        d.rectangle(tuple(int(v*S) for v in box),fill=fill,outline=outline,width=max(1,width*S) if outline else 1)
    def line(seq,fill,width=1): d.line(pts(seq),fill=fill,width=max(1,width*S),joint='curve')
    def ellipse(box,fill,outline=(66,48,36,255),width=1):
        d.ellipse(tuple(int(v*S) for v in box),fill=fill,outline=outline,width=max(1,width*S) if outline else 1)
    # Layered stone approach steps, sized wider than the cottage doorway.
    for box, fill in [((119,220,329,239),(177,177,163,255)),
                      ((135,237,313,254),(151,154,147,255)),
                      ((153,252,295,269),(190,188,170,255)),
                      ((171,267,277,282),(141,144,137,255))]:
        rect(box,fill,(76,79,75,255),1)
        line([(box[0]+6,box[1]+3),(box[2]-6,box[1]+3)],(222,219,201,255),1)
    # Blue-and-gold village heraldry on the main facade.
    for x in (123,302):
        line([(x+12,130),(x+12,181)],(85,58,39,255),2)
        poly([(x,135),(x+24,135),(x+23,167),(x+12,178),(x+1,167)],(29,62,108,255),(222,177,73,255),1)
        line([(x+12,143),(x+12,160)],(238,190,75,255),2)
        line([(x+6,150),(x+18,150)],(238,190,75,255),2)
    # Stone planters and restrained flowers; leave the center approach clear.
    for x in (42,362):
        rect((x,248,x+38,268),(128,132,122,255),(69,73,68,255),1)
        rect((x+2,245,x+36,251),(190,188,170,255),(69,73,68,255),1)
        for j in range(4):
            fx=x+4+j*8; fy=237-(j%2)*4
            col=[(80,140,49,255),(105,160,58,255),(204,113,149,255),(237,201,96,255)][j]
            ellipse((fx,fy,fx+9,fy+11),col,(50,91,39,255),1)
            ellipse((fx+3,fy-2,fx+6,fy+3),(244,231,190,255),None,0)
    im.resize((W,H),Image.Resampling.LANCZOS).save(OUT/'elderHouse.png')

for fn in (well, lantern, market, crops, scarecrow, woodpile, campfire, barrel, crate, rowboat, bridge_arch): fn()
elder_house()
print('Generated village landmark props in', OUT)

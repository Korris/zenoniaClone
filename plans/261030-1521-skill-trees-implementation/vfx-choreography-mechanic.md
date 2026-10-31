# Mechanic VFX choreography (ChatGPT "Tan Pham", 2026-10-30) — Earth-Break-quality spec

Source chat: https://chatgpt.com/c/6ae450c1-7638-83ec-a5ea-717aa58678ea
Notation as in vfx-choreography-assassin.md; perhit = fires when the flight passes each monster. Identity: hard mechanical sci-fi, steel, hot sparks, blue-white arcs, muzzle flashes, smoke, recoil.
KeepsSimArt skills (Homing Rockets, Plasma Barrage, Robot Assault, Orbital Cannon): the game still draws the rockets, shards, drone and explosions at their real positions; these phases only add to them and use no sprite.

## 1. Power Shot — 480 (piercing bolt, 7 tiles at 24 tiles/s)
- 0-75 @caster C[0:2] w1.6 a1 glow #FFB13B .75 | Embers 14 #FFB44A | Dust 5 | Ring .6 75 #FFA341 | Shake 70 .12 | Flash #FFD38A 35 | Zoom .012 80
- 0-290 @travel L[0:3] w1.3 a1 glow #58DFFF .65 | Stream Embers 18 #70E8FF + 10 #FFB14B
- perhit 100 ms @hit I[0:2] w1.4 a.90 glow #B7F5FF .50 | Embers 9 #FFCA65 | Dust 3 | Ring .8 90 #66D9FF | Shake 55 .05
- 290-390 @end I[2:3] w1.2 a.55 glow #69CFEF .30 | Embers 8 #8CEBFF | Ring .7 90 #6BD8F5
- 390-480 @end I[3] w1 a.12 glow #4C91AB .08 | Stream Embers 5 #A5B4BE
- Unique: loud orange muzzle flash launching a thin blue-white bolt, electric puncture sparks on every monster it crosses

## 2. Rocket Punch — 570 (piercing fist, 4 tiles at 14 tiles/s, width 2, knockback)
- 0-95 @caster C[0:2] w2.2 a1 glow #FF8A2A .75 | Embers 20 #FFB24D | Dust 8 | Ring .9 100 #FF8D38 | Shake 95 .18 | Flash #FFBB72 50 | Zoom .02 95
- 0-285 @travel L[0:3] w2 a1 glow #FF7424 .60 | Stream Embers 22 #FF9735 + 12 #B6C5D0
- perhit 120 ms @hit I[0:2] w2.4 a1 glow #FFC16B .65 | Embers 18 #FFBA55 | Debris 5 | Dust 10 | Ring 1.2 120 #FF9545 | Shake 80 .08
- 285-410 @end I[2:3] w2.2 a.70 glow #E98235 .38 | Embers 12 #FFA74D | Dust 8 | Ring 1 120 #E88D42
- 410-570 @end I[3] w1.8 a.18 glow #A65A30 .10 | Stream Embers 10 #89949E | Dust 4
- Unique: a steel fist rockets out on a short orange exhaust, blunt metallic impacts rather than an energy explosion

## 3. Homing Rockets — 330 launch accent (KeepsSimArt)
- 0-85 @caster Ring .6 100 #FF9C42 | Embers 15 #FFB650 | Dust 6 | Shake 75 .10 | Flash #FFBD76 35
- 85-175 @caster Ring 1 120 #F18B38 | Embers 18 #FFA343 + 14 #A3ADB5 | Dust 8 | Shake 85 .12
- 175-260 @caster Decal .3->.9 #6A5143 .16 | Embers 12 #8F9AA4 + 8 #FF9235
- 260-330 @caster Decal .9->0 #5B514C .16 | Stream Embers 7 #89939C
- Unique: three tightly staggered launch thumps with orange sparks and short grey exhaust

## 4. Plasma Barrage — 320 launch accent (KeepsSimArt)
- 0-75 @caster Ring .5 85 #62DFFF | Embers 12 #A5F4FF | Shake 65 .08 | Flash #B9F5FF 35
- 75-175 @caster Ring 1 110 #4FCFFF | Embers 22 #78E9FF + 18 #B1F6FF | Shake 90 .12
- 175-260 @caster Ring .8 100 #83EFFF | Embers 15 #66CFFF + 9 #E0FAFF
- 260-320 @caster Embers 7 #78C5DD + 4 #B4EFFF
- Unique: six rapid capacitor discharges, a sharp blue-white launch signature

## 5. Robot Assault — 580 deploy accent (KeepsSimArt)
- 0-120 @caster Ring .6 120 #55DFFF | Embers 8 #83E9FF
- 120-280 @caster Ring 1.1 160 #6BDFFF | Embers 12 #7BD8FF + 10 #FFB75B | Shake 90 .06
- 280-430 @caster Decal .4->1.2 #3C8EA8 .14 | Ring 1.4 170 #64CFFF | Stream Embers 10 #8BEFFF
- 430-580 @caster Decal 1.2->0 #3C8EA8 .14 | Stream Embers 6 #79B8D0
- Unique: a compact blue activation pulse and servo cue while the drone switches to assault mode

## 6. Target Lock — 760 (scan radius 7, marks at 0, no damage)
- 0-100 @caster C[0:1] w2 a.45 glow #FF9A35 .35 | Tele 1->7 #F7A544 | Ring 7 170 #F3A33B | Embers 12 #FFB95B
- 100-260 @caster L[0:3] w7 a.40 glow #F8A342 .30 | Tele 7 #D88A35 | Ring 7 210 #E7A34D | Stream Embers 12 #FFBA62
- 260-430 @caster I[0:2] w5 a.35 glow #FFB65C .28 | Decal 1->2 #A76C32 .12 | Ring 5 170 #F3B15B | Embers 10 #FFCB79
- 430-610 @caster I[2:3] w4 a.22 glow #C98A42 .15 | Tele 7->2 #B57C3C | Stream Embers 8 #DFA75E
- 610-760 @caster I[3] w2.5 a.08 glow #A16A35 .06 | Tele 2->0 #A16A35 | Embers 4 #D7A061
- Unique: a fast seven-tile amber radar sweep that retracts into a crisp targeting confirmation

## 7. EMP Burst — 820 (circle radius 3, hit 0, stun; lock 450)
- 0-130 @caster I[0:2] w6 a1 glow #B7F5FF .90 | Decal .3->3 #3B9EC0 .30 | Embers 35 #A9F3FF | Debris 5 | Dust 8 | Ring 3 180 #75E5FF | HitStop 65 | Shake 200 .45 | Flash #C8F7FF 110 | Zoom .055 200
- 130-310 @caster L[0:3] w6.2 a.80 glow #66DFFF .60 | Decal 3->3.2 #397E9C .22 | Ring 3.5 230 #8EEFFF | Stream Embers 32 #8BEFFF + 16 #E2FAFF | Shake 130 .12
- 310-450 @caster I[1:2] w6 a.50 glow #59BFD8 .35 | Decal 3.2->3 #326A85 .18 | Stream Embers 20 #9FEAFF + 12 #D0F7FF
- 450-630 @caster I[2:3] w5.5 a.25 glow #4C9DB7 .18 | Decal 3->2 #325B72 .12 | Stream Embers 12 #8AC9DE
- 630-820 @caster I[3] w4.5 a.08 glow #427C96 .06 | Decal 2->0 #325B72 .12 | Stream Embers 6 #8CB8C9
- Unique: an immediate blue-white pressure wave with a hard capacitor crack and collapsing static arcs

## 8. Orbital Cannon — 1750 ultimate (six strikes at 0, 250 ... 1250; KeepsSimArt; @target = middle of the strike area)
- 0-120 @target Tele 0->4 #FFAD42 | Decal 0->2 #A76C32 .20 | Ring 1.5 120 #FFB75A | Embers 12 #FFC56C | Shake 65 .04
- 120-250 @target Tele 2->4 #F2A43C | Decal 2->4 #9B6530 .18 | Stream Embers 12 #FFBC5D
- 250-370 @target Tele 4 #F6AD4C | Ring 2 130 #FFB34F | Embers 14 #FFC16B | Dust 3 | Shake 70 .05
- 370-500 @target Decal 4 #A36A34 .20 | Stream Embers 10 #FFB65A
- 500-620 @target Ring 2.3 140 #FFBB5E | Embers 16 #FFCC75 | Dust 4 | Shake 75 .06
- 620-750 @target Tele 4 #FFC15D | Decal 4 #B77734 .24 | Stream Embers 14 #FFC46C
- 750-870 @target Ring 2.6 150 #FFC16B | Embers 18 #FFD080 | Dust 4 | Shake 80 .07
- 870-1000 @target Tele 4 #FFD17C | Decal 4 #C3843E .26 | Stream Embers 18 #FFD38A
- 1000-1120 @target Ring 3 170 #FFD17C | Embers 22 #FFD890 | Dust 5 | Shake 85 .08
- 1120-1250 @target Tele 4 #FFE09A | Decal 4 #D08A3C .30 | Ring 3.5 130 #FFD58A | Stream Embers 28 #FFE1A1 | Shake 100 .08
- 1250-1450 @target Decal 4 #C87C32 .32 | Ring 4 220 #FFE09A + 4.8 300 #FFB65A | Embers 55 #FFD38B | Debris 18 | Dust 24 | HitStop 40 | Shake 380 .82 | Flash #FFE4AF 160 | Zoom .085 300
- 1450-1600 @target Decal 4->3 #9D6334 .22 | Ring 4.5 240 #E8A35D | Stream Embers 24 #FFBF73 | Dust 12 | Shake 110 .12
- 1600-1750 @target Tele 4->0 #A36A3B | Decal 3->0 #8D5930 .18 | Stream Embers 10 #C9A17D
- Unique: six precisely timed impacts over a four-tile zone, escalating targeting pulses, one overwhelming sixth strike
- Lead's change: ChatGPT's 1120-1250 "@sky" phase runs @target, because it has no sprite and the sky anchor would drag its telegraph and rings down the screen.

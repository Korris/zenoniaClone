# Warrior VFX choreography (ChatGPT "Tan Pham", 2026-10-30) — Earth-Break-quality spec

Source chat: https://chatgpt.com/c/6ae450c1-7638-83ec-a5ea-717aa58678ea
Notation as in vfx-choreography-wizard.md: times ms from cast start; widths and radii in tiles; Sprite C/L/I = cast/loop/impact rows of Resources/Sprites/skills/warrior/<id>/ (4 frames each);
Sprite w = width, a = alpha; Ring(radius, ms, colour); Shake(ms, 0-1); Zoom(amount, ms). Damage never waits for art: visual impacts start at the real hit (table times below).
Identity: heavy, physical, steel and fire, weight and dust. Earth Break keeps EarthBreakUltimate and has no entry.

## 1. Power Slash — 360 (hit 0)
- 0-90 @target face: I[0:2] w5 a1; glow #FF6725 .55; Embers 12 #FF7025; Debris 4; Dust 9; Ring(1.8,100,#E97B42); HitStop 35; Shake(100,.20); Flash #FFAA65 45; Zoom .025/100; sfx impact
- 90-230 @target face: L[0:3] w4.5 a.75; glow #FF4D19 .30; Embers 10 #FF6B26; Debris 3; Dust 6
- 230-360 @target face: I[2:3] w4 a.35; glow #A83417 .12; Embers 5 #D94C20; Dust 4
- Unique: instant heavy steel crescent, sharp orange edge, thick dusty follow-through

## 2. Whirlwind — 950 (hits 0, 250, 500; lock 750)
- 0-100 @caster: L[0:1] w5 a.8; glow #FF6725 .40; Decal 2.5 #8D3820; Embers 12 #FF8B31; Debris 3; Dust 10; Ring(2.0,100,#D96229); Shake(65,.06); sfx cast
- 100-250 @caster: L[1:2] w5 a.85; glow #FF541A .45; Decal 2.5 #963B1D; Embers 14 #FF7525; Debris 2; Dust 8
- 250-350 @caster: L[2:3] w5 a.9; glow #FF7A20 .50; Decal 2.5 #A3421B; Embers 15 #FF9A35; Debris 4; Dust 10; Ring(2.3,100,#E86A28); Shake(70,.07)
- 350-500 @caster: L[0:2] w5 a.9; glow #FF631C .50; Decal 2.5 #A8421B; Embers 18 #FF8126; Debris 3; Dust 12
- 500-650 @caster: I[0:2] w5.2 a1; glow #FFAB35 .65; Decal 2.5 #B4471B; Embers 25 #FF982B; Debris 9; Dust 18; Ring(2.7,160,#FF8A32); HitStop 35; Shake(150,.25); Flash #FFAB65 65; Zoom .035/130; sfx impact
- 650-750 @caster: I[2:3] w5 a.55; glow #E75B20 .25; Decal 2.5 #71301C; Embers 10 #FF792B; Dust 12
- 750-950 @caster: I[3] w4.5 a.2; glow #A63A1A .10; Decal 2 #5B281C; Embers 6 #D95520; Dust 8
- Unique: three heavy rotations sweeping dust outward, ending in a fiery circular impact

## 3. Flame Cleave — 700 (hit 0; lock 450)
- 0-100 @target face: I[0:1] w5.5 a1; glow #FF7B16 .70; Decal 3 #C54B16; Embers 22 #FF9B25; Debris 8; Dust 12; Ring(2.0,100,#FF7220); HitStop 40; Shake(130,.24); Flash #FF9C42 70; Zoom .035/130; sfx impact
- 100-280 @target face: L[0:3] w6 a.95; glow #FF8A13 .60; Decal 3 #A33D16; Embers 28 #FFAB2A; Debris 8; Dust 15; Shake(100,.08)
- 280-450 @target face: I[1:2] w6 a.85; glow #FF6315 .50; Decal 3 #B74716; Embers 24 #FF8122; Debris 5; Dust 12; Shake(100,.06)
- 450-700 @target face: I[2:3] w5.5 a.35; glow #E84A12 .20; Decal 3 #6F2814; Embers 14 #FF6A1C; Dust 8
- Unique: ground rupture then a low rolling wall of fire and scorched earth, never a flying crescent

## 4. Charging Strike — 650 (dash 0-250, hit 250)
- 0-100 @caster: C[0:2] w2 a.8; glow #F44720 .35; Embers 10 #FF6A2A; Debris 3; Dust 12; Shake(65,.05); sfx cast
- 100-250 @caster: L[0:3] w3 a.8; glow #FF5725 .50; Embers 20 #FF7131; Debris 6; Dust 18; Shake(110,.07)
- 250-390 @caster (hero is at the dash end): I[0:2] w3.5 a1; glow #FFAA4A .65; Decal 1.5 #9D4123; Embers 24 #FF8B39; Debris 15; Dust 24; Ring(2.0,170,#F18A42); HitStop 55; Shake(190,.38); Flash #FFB16A 90; Zoom .055/160; sfx impact
- 390-520 @caster: I[2:3] w3 a.55; glow #C64A23 .20; Decal 1.5 #71321D; Embers 10 #E86C2C; Debris 4; Dust 14; Shake(70,.05)
- 520-650 @caster: I[3] w2.5 a.2; glow #A64224 .08; Embers 4 #C85A2A; Dust 10
- Unique: shoulder-first rush with a red afterimage and a brutal collision at exactly 250 ms

## 5. Blade Cyclone — 2900 (hits 0, 500, 1000, 1500, 2000; zone follows the hero 2500; lock 450)
- 0-120 @caster: L[0:1] w5 a.75; glow #EF4C22 .35; Decal 2.5 #7C3020; Embers 12 #FF6D32; Debris 4; Dust 12; Ring(2.0,110,#C95230); Shake(70,.05); sfx cast
- 120-500 @caster: L[0:3] loop w5 a.78; glow #F65A23 .42; Decal 2.5 #8D3520; Embers 22 #FF762D; Debris 4; Dust 23
- 500-620 @caster: L[1:3] w5 a.82; glow #FF7029 .48; Decal 2.5 #923A20; Embers 14 #FF8533; Debris 4; Dust 12; Ring(2.2,110,#DB6232); Shake(70,.06)
- 620-1000 @caster: L[0:3] loop w5 a.75; glow #F85A24 .42; Decal 2.5 #85331E; Embers 16 #FF762E; Debris 3; Dust 14
- 1000-1120 @caster: L[0:2] w5 a.85; glow #FF782C .50; Decal 2.5 #963C20; Embers 16 #FF9135; Debris 5; Dust 13; Ring(2.3,110,#E66B32); Shake(75,.07)
- 1120-1500 @caster: L[0:3] loop w5 a.78; glow #F85B24 .45; Decal 2.5 #85331E; Embers 18 #FF7A30; Debris 3; Dust 16
- 1500-1620 @caster: L[1:3] w5 a.9; glow #FF812C .52; Decal 2.5 #A04020; Embers 18 #FF9638; Debris 6; Dust 15; Ring(2.4,120,#E87535); Shake(80,.08)
- 1620-2000 @caster: L[0:3] loop w5 a.8; glow #FA6326 .48; Decal 2.5 #8C361D; Embers 20 #FF8232; Debris 4; Dust 18
- 2000-2200 @caster: I[0:2] w5.2 a1; glow #FFAB42 .65; Decal 2.5 #B34720; Embers 32 #FFA03A; Debris 12; Dust 24; Ring(2.8,200,#FF9140); HitStop 40; Shake(190,.32); Flash #FFAA6A 80; Zoom .045/180; sfx impact
- 2200-2500 @caster: L[2:3] loop w5 a.5; glow #DC4A20 .25; Decal 2.5 #79301C; Embers 12 #E96C2C; Dust 16
- 2500-2900 @caster: I[2:3] w4.5 a.2; glow #A83A1D .08; Embers 8 #C95726; Dust 12
- Unique: mobile red steel tornado with five rhythmic beats and one finishing shockwave

## 6. Inferno Crash — 1150 (leap 0-400, hit 400; lock 700)
- 0-160 @caster: C[0:2] w2.2 a.9; glow #FF681B .50; Decal .8 #A63B18; Embers 18 #FF8C25; Debris 4; Dust 12; Shake(80,.07); sfx cast
- 160-300 @caster: L[0:2] w2.4 a.95; glow #FF8420 .65; Embers 22 #FFA02B; Dust 5
- 160-400 @target: Tele 3 #E75A1C
- 300-400 @caster: L[2:3] w2.6 a1; glow #FFB13B .85; Decal 1 #A94417; Embers 28 #FFB63C; Dust 8; Shake(90,.14)
- 400-570 @target: I[0:2] w6 a1; glow #FF9C22 .95; Decal 3 #D64A16; Embers 65 #FF9B28; Debris 32; Dust 28; Ring(3.0,190,#FF8B2B) + (3.8,260,#E65B20); HitStop 100; Shake(420,.78); Flash #FFB568 150; Zoom .085/280; sfx impact
- 570-700 @target: I[2:3] w6 a.85; glow #FF6418 .55; Decal 3 #A73B17; Embers 30 #FF8424; Debris 12; Dust 20; Ring(3.5,180,#E96C24); Shake(150,.18)
- 700-900 @target: I[3] w5.5 a.5; glow #D74B16 .30; Decal 3 #792A17; Embers 24 #FF7422; Debris 4; Dust 16; Shake(100,.08)
- 900-1150 @target: I[3] w5 a.2; glow #A83B15 .10; Decal 2.8 #54251A; Embers 12 #D95A1B; Dust 14
- Unique: airborne leap into a molten crater slam at 400 ms, double shockwave, flying rocks, fire pillars
- Lead's change: ChatGPT's "sky" descent phase runs @caster instead, because the hero is really in the air and the sky anchor would draw a second falling body.

## 7. Berserker Awakening — 12000 buff (no damage; lock 600)
- 0-180 @caster: C[0:2] w2.5 a.55; glow #FF4C20 .55; Decal 1.2 #9B261A; Embers 20 #FF762A; Debris 4; Dust 10; Ring(1.6,170,#D84A26); Shake(130,.18); Flash #FF784A 60; Zoom .025/130; sfx cast
- 180-420 @caster: C[2:3] w3 a.5; glow #FF6A1E .50; Decal 1.4 #A52E18; Embers 26 #FF9232; Debris 6; Dust 12; Ring(2.0,220,#EF5926); Shake(170,.24)
- 420-600 @caster: L[0:1] w3 a.35; glow #FF8A30 .35; Decal 1.5 #A8321B; Embers 18 #FF9D3D; Debris 3; Dust 8; Shake(100,.12)
- 600-10500 @caster: L[0:3] loop w3 a.25; glow #E9471B .22; Decal 1.2 #7A2418; Embers 60 #FF7830 stream (3 per 500 ms)
- 10500-11500 @caster: L[0:3] loop w3 a.2; glow #D8421A .16; Decal 1.2 #642019; Embers 4 #E96628 stream
- 11500-12000 @caster: I[0:3] w2.8 a.15 fading; glow #B83A18 .12; Decal 1.2 #4D1A16; Embers 8 #C95725; Dust 4
- Unique: translucent crimson flame mantle, intense activation, hero fully visible during the 12 s sustain

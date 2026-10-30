# Wizard VFX choreography (ChatGPT "Tan Pham", 2026-10-30) — Earth-Break-quality spec

Source chat: https://chatgpt.com/c/6ae450c1-7638-83ec-a5ea-717aa58678ea
Notation: times ms from cast start; radius in tiles; Sprite C/L/I = cast/loop/impact rows of Resources/Sprites/skills/wizard/<id>/;
ShockRing(radius, ms, colour); CameraShake(ms, 0-1); PunchZoom(amount, ms). Projectile impact phases start at collision (times assume max range).
HitStop pauses visuals only, never server damage scheduling. Scale particle counts down on low-end mobile, never timings.
Reference = EarthBreakUltimate.cs (charge ring + embers + telegraph -> sweep -> descending sword -> SLAM: hit-stop 110, shake 1.0, flash, 2 rings, 34 debris/22 dust/70 embers -> fissure ticks).

## 1. Arcane Bolt — 480
- 0-100 anticipation: C[0:3] additive #29BFFF; Embers 6 #59DFFF; sfx cast
- 100-310 travel: L[0:3] right 7 tiles; glow #39CFFF 0.65; Embers 8 #70EFFF (trail)
- 310-410 impact: I[0:3] r0.65; ShockRing(0.8,110,#4CCFFF); Embers 12 #B0F8FF; HitStop 20; Shake(65,0.07); sfx impact
- 410-480 linger: Embers 5 #70CFFF
- Unique: piercing blue comet + tiny star pop

## 2. Fireball — 580
- 0-110: C[0:3] additive #FF8B18; Embers 9 #FFB02A
- 110-340 travel: L[0:3] 7 tiles; glow #FF6B00 0.75; Embers 14 #FF8C18
- 340-470 impact: I[0:2] r1.5; ShockRing(1.5,150,#FF8500); Embers 26 #FF9A12; Debris 5; Dust 8; HitStop 35; Shake(120,0.17); Flash #FF9A36 70; PunchZoom 0.025/110
- 470-580 linger: I[3]; GroundDecal r1.0 #A83B12 (scorch); Embers 8 #FF6315
- Unique: expanding fireball + burning core

## 3. Arcane Lance — 520
- 0-130: C[0:3] additive #B342FF; GroundTelegraph line 8x1 #A537FF; Embers 10 #C875FF
- 130-300 travel: L[0:3] 8 tiles; glow #D065FF 0.9; Embers 16 #A94CFF
- 300-410 impact (per pierced target): I[0:2] r0.9; ShockRing(1.0,90,#C36AFF); Embers 20 #E7B0FF; HitStop 30; Shake(90,0.12); Flash #A944FF 45
- 410-520 linger: I[3]; glow #A84BFF 0.25; Embers 7 #D18CFF
- Unique: thin violet beam + successive piercing pops

## 4. Mana Shield — 520 cast / 8000 sustain
- 0-160: C[0:2]; GroundDecal r0.6->1.2 #36CFFF; glow #5ADFFF 0.35; Embers 8 #8CEFFF
- 160-350 formation: C[3] -> L[0]; ShockRing(1.3,180,#42DFFF); Embers 12 #A0F7FF; no shake
- 350-8000 sustain (follows hero): L[0:3] loop; glow #38BFFF 0.28; GroundDecal r1.2 #2C9FFF; Embers 2 per 500 ms #8ADFFF; loop hum
- on break: I[0:3] 240; ShockRing(1.5,170,#6CEAFF); Embers 18 #A5F4FF; Debris 8; Shake(90,0.10); Flash #6EDFFF 50; shatter sfx
- on expire: I[2:3] 180; Embers 6 #7CCFFF
- Unique: rotating bubble + crystalline shatter

## 5. Frost Nova — 850
- 0-200: C[0:3]; GroundTelegraph r0.8->3.0 #35E9FF; glow #6CEFFF 0.45; Embers 12 #B7F9FF
- 200-420 expansion: L[0:3]; GroundDecal r0.8->3.0 #5CDFFF; ShockRing(3.0,220,#76F2FF); Embers 20 #A5F5FF
- 420-630 impact: I[0:2] r3.0; ShockRing(3.2,180,#B8FFFF); Debris 14; Dust 18; Embers 25 #D2FFFF; HitStop 55; Shake(170,0.28); Flash #B9F6FF 90; PunchZoom 0.035/160
- 630-850 linger: I[3]; GroundDecal r3.0 #8ADBEF (frost); Dust 10; Embers 8 #B8F5FF
- Unique: radial ice-crystal eruption + freeze snap

## 6. Elemental Burst — 1150
- 0-280: C[0:3]; GroundTelegraph r2.5 #68DFFF; glow #FFFFFF 0.4; Embers 12 #FF791A + 12 #6DEFFF + 12 #B15AFF (converging)
- 280-540 core pulse: L[0:3]; glow #D6FAFF 0.9; ShockRing(1.2,160,#7ADFFF); Embers 15 #FF8A22 + 15 #77EFFF + 15 #C06AFF
- 540-850 impact: I[0:2] r2.5; ShockRing(2.6,180,#E0FFFF) + (3.0,260,#A15AFF); Embers 28 #FF851A + 28 #72EAFF + 24 #BC65FF; Debris 18; Dust 20; HitStop 85; Shake(300,0.52); PunchZoom 0.065/230; Flash #C7F5FF 130
- 850-1150 linger: I[3]; GroundDecal r2.5 #5484C8; Embers 10+10+10; Dust 8
- Unique: triple-colour orbital convergence + white-star detonation

## 7. Astral Cataclysm (ultimate) — 3100
- 0-450 ritual: C[0:2]; GroundTelegraph r4.0 #9238FF; glow #A951FF 0.45; Embers 24 #C17AFF
- 450-850 portal open: C[3]; telegraph #B34BFF; ShockRing(3.0,300,#A850FF); Embers 30 #E2A5FF; Shake(200,0.16)
- 850-1250 first descent: L[0:3] downward; glow #D68CFF 0.9; Embers 32 #D17AFF; telegraph r4.0 #D175FF
- 1250-1480 first impact: I[0:1]; ShockRing(3.0,200,#C55AFF); Debris 18; Dust 14; Embers 35 #C976FF; HitStop 45; Shake(220,0.45); Flash #B86AFF 80
- 1480-1950 meteor rain: 3 descending L instances, impacts at 1510/1690/1870 at offsets (-1.5,0.5),(1.3,-0.8),(0.6,1.5); each I[0:1] 140, ShockRing(1.8,120,#A84FFF), Debris 9, Dust 8, Embers 18 #CE79FF, HitStop 20, Shake(110,0.22)
- 1950-2250 final descent: L[2:3] scale 1.35; telegraph r4.0 #E18CFF; glow #F2B7FF 1.0; Embers 45 #DB9AFF; Shake(300,0.28)
- 2250-2550 FINAL SLAM: I[0:2] r4.0; ShockRing(4.0,240,#F0B0FF) + (5.0,360,#A43AFF); Debris 42; Dust 35; Embers 90 #C66BFF; HitStop 125; Shake(480,0.90); PunchZoom 0.105/340; Flash #E5B6FF 180; glow #F0C8FF 1.0
- 2550-3100 linger: I[2:3]; GroundDecal r4.0 #7130B2; ShockRing(4.5,380,#A65AFF); Embers 28 #B67AFF; Dust 18; Shake(180,0.12)
- Unique: sky portal + staggered meteor impacts + oversized final slam

Ice Storm: keep the existing IceStormUltimate implementation.

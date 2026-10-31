# Cleric VFX choreography (ChatGPT "Tan Pham", 2026-10-30) — Earth-Break-quality spec

Source chat: https://chatgpt.com/c/6ae450c1-7638-83ec-a5ea-717aa58678ea
Notation as in vfx-choreography-assassin.md; `along N` = offset along the aim in tiles. Identity: calm warm white-gold holy light, soft feathers, rising motes, gentle rings, almost no dust, no shake on heals.
Over-hero sprite alpha: burst at most .50, sustain at most .35.

## 1. Heal — 550 (heal 0; target = lowest HP ally)
- 0-100 @target I[0:1] w2.2 a.45 glow #FFF2B8 .48 | Decal .2->.8 #A8E8C2 .25 | Embers 12 #FFF1B2 | Ring .9 130 #BCEFCF
- 100-270 @target L[0:3] w2 a.32 glow #FFE8A5 .30 | Stream Embers 16 #FFF6D6 | Ring 1.1 180 #D7F5D8
- 270-420 @target I[1:3] w1.8 a.22 glow #E7F9D9 .18 | Stream Embers 9 #F9F4DC | Decal .8->.5 #BCE8C6 .16
- 420-550 @target I[3] w1.5 a.08 glow #FFF0BE .08 | Stream Embers 4 #F4F5D8
- Unique: immediate healing cross, delicate upward motes, soft mint halo

## 2. Spirit Blessing — 12000 buff (radius 4; the game sparkles other allies)
- 0-180 @caster C[0:3] w3.2 a.45 glow #FFF1AD .45 | Decal .5->2 #D8EEC4 .22 | Embers 18 #FFF3C5 | Ring 2.5 190 #F5E6AA
- 180-450 @caster I[0:2] w4 a.45 glow #F8E9B0 .38 | Decal 2->4 #D8F2C8 .18 | Ring 4 270 #E9F4C8 | Stream Embers 18 #FFF4D2
- 450-800 @caster I[2:3] w3 a.25 glow #FFF0C8 .20 | Decal 4->1.2 #D4EBC1 .12 | Stream Embers 10 #F5F3D8
- 800-11000 @caster L[0:3] loop w2.4 a.16 glow #F6E9B8 .12 | Decal 1.2 #CFE8BC .08 | Stream Embers 29 #FFF1D0 (2 per 700 ms)
- 11000-12000 @caster I[2:3] w2 a.12 glow #F2E4B3 .10 | Decal 1.2->0 #CFE8BC .08 | Stream Embers 4 #F4ECCC
- Unique: one gentle four-tile wave, then a barely visible golden aura

## 3. Holy Ray — 560 (line 7 x 1.5, hit 0; @target = middle of the line)
- 0-90 @target Line 7 #FFF0B8 | Decal 0->.7 #E5DDB2 .20 | Embers 10 #FFF5D7 | HitStop 25 | Shake 85 .12 | Flash #FFF3D7 45 | Zoom .015 90
- 0-115 @target along -2.5 I[0:2] w1.5 a.85 glow #FFF2C2 .55 | Embers 10 #FFF7DF | Ring .7 100 #F4E5A8
- 0-115 @target along 0 I[0:2] w1.6 a.85 glow #FFFFFF .60 | Embers 12 #FFF5CB | Ring .8 110 #F9E9B4
- 0-115 @target along 2.5 I[0:2] w1.5 a.85 glow #FFF2C2 .55 | Embers 10 #FFF7DF | Ring .7 100 #F4E5A8
- 115-270 @target Line 7 #EADFAE; along -2.5 L[0:2] w1.2 a.40 glow #FCE9AF .22; along 0 L[0:2] w1.3 a.45 glow #FFF0BC .25; along 2.5 L[0:2] w1.2 a.40 glow #FCE9AF .22 | Stream Embers 15 #FFF5D6
- 270-420 @target along -2.5 I[2:3] w1.1 a.22 glow #E9DBA9 .12; along 0 I[2:3] w1.2 a.25 glow #F2E6B5 .15; along 2.5 I[2:3] w1.1 a.22 glow #E9DBA9 .12 | Stream Embers 10 #F7EDD0
- 420-560 @target along 0 I[3] w1 a.08 glow #F4E7C0 .05 | Stream Embers 5 #F5EBCD
- Unique: three synchronized white-gold bursts along a seven-tile holy line, a piercing ray without a beam sprite

## 4. Divine Light — 780 (circle radius 2, 3 tiles ahead; damage + heal at 0)
- 0-130 @target I[0:2] w4 a.95 glow #FFF4C8 .75 | Decal .5->2 #E8E9BC .28 | Embers 26 #FFF6D8 | Ring 2 180 #FFF0BB | HitStop 40 | Shake 130 .24 | Flash #FFF5DC 90 | Zoom .035 140
- 130-330 @target L[0:3] w4.2 a.75 glow #FFE9AC .55 | Decal 2->2.2 #E6EDC5 .22 | Ring 2.4 230 #F4E9B9 | Stream Embers 24 #FFF5D5
- 330-550 @target I[2:3] w4 a.38 glow #F5E9BD .25 | Decal 2.2->2 #D9E7BD .16 | Stream Embers 18 #FFF8E4
- 550-780 @target I[3] w3.2 a.10 glow #E9DDAF .08 | Decal 2->0 #D9E7BD .16 | Stream Embers 8 #F6F0D8
- Unique: radiant starburst ahead, a firm holy impact inside a softer healing halo

## 5. Sanctuary — 8000 (zone radius 3 fixed on the ground; heal pulses every 1000 from 0 to 7000)
- 0-160 @target C[0:2] w5.5 a.30 glow #DDF5CA .32 | Decal 0->3 #BCE8C8 .22 | Ring 3 200 #E5F5D4 | Embers 16 #FFF5D8
- 160-450 @target C[2:3] w5.5 a.28 glow #FFF0C4 .22 | Decal 3 #B9E5C3 .18 | Ring 3.1 260 #E8F2D4 | Stream Embers 12 #F8F7E1
- 450-1000 @target L[0:3] loop w5.5 a.18 glow #D8F0CB .12 | Decal 3 #B8E7C5 .14 | Stream Embers 3 #FFF7E1
- each N000-N120, N = 1..7: @target L[0:1] w5.5 a.25 glow #F2EFCB .20 | Decal 3 #BCE8C8 .17 | Ring 2.8 170 #E4F5D6 | Embers 7 #FFF5D9
- each N120-(N+1)000, N = 1..6: @target L[0:3] loop w5.5 a.16 glow #D8F0CB .10 | Decal 3 #B8E7C5 .14 | Stream Embers 5 #FFF7E1
- 7120-7700 @target L[0:3] loop w5.5 a.14 glow #D8F0CB .08 | Decal 3 #B8E7C5 .12 | Stream Embers 2 #FFF7E1
- 7700-8000 @target I[2:3] w5.5 a.12 glow #DDEBCB .08 | Decal 3->0 #B8E7C5 .12 | Stream Embers 8 #F8F3DF
- Unique: a barely luminous sanctuary with eight synchronized healing rings, no camera movement

## 6. Guardian Spirit — 8000 (shield radius 4; the caster's own shield drives break / expire)
- 0-160 @caster C[0:2] w3.5 a.45 glow #FFF0BE .42 | Decal .4->1.5 #D4EBC8 .20 | Ring 2 170 #F5E9BC | Embers 14 #FFF4D8
- 160-420 @caster C[2:3] w4 a.48 glow #F9EDC7 .38 | Decal 1.5->2 #D4EBC8 .18 | Ring 3 220 #F2E9C4 | Stream Embers 16 #FFF7E2
- 420-800 @caster I[0:2] w3.5 a.30 glow #F3E8C4 .20 | Decal 2->1.2 #D2E6C5 .12 | Stream Embers 10 #FFF3D8
- 800-7500 @caster L[0:3] loop w2.8 a.18 glow #E9E8C9 .12 | Decal 1.2 #CDE5C6 .08 | Stream Embers 20 #FFF5DE (2 per 650 ms)
- 7500-8000 @caster I[2:3] w2.5 a.12 glow #E8E2C3 .08 | Decal 1.2->0 #CDE5C6 .08 | Stream Embers 5 #F7F0D8
- ON_BREAK 0-110 @caster I[0:1] w3.2 a.50 glow #FFF5D6 .48 | Ring 1.5 120 #FFF0C7 | Embers 14 #FFF8E6 | sfx break
- ON_BREAK 110-260 @caster I[1:3] w3.5 a.28 glow #F4E8C9 .18 | Ring 2 180 #E8E7CB | Stream Embers 16 #FFF7E6
- ON_BREAK 260-420 @caster I[3] w2.8 a.10 glow #E8E0BF .06 | Stream Embers 6 #F6F1DD
- ON_EXPIRE 0-140 @caster I[2:3] w2.8 a.18 glow #F3E9CC .12 | Ring 1.2 150 #E7E9D1 | Embers 7 #FFF4DE
- ON_EXPIRE 140-320 @caster I[3] w2.4 a.10 glow #E7E2C7 .06 | Stream Embers 8 #F8F2DF
- Unique: translucent protective wings that never hide the hero, soft feather shatter on break, peaceful dissolve on expiry

## 7. Purification — 760 (circle radius 3, hit 0; lock 450)
- 0-120 @caster I[0:2] w6 a.48 glow #FFF5D2 .55 | Decal .3->3 #BFECD8 .25 | Embers 24 #FFF7DF | Ring 3 180 #E7F9E3 | HitStop 35 | Shake 110 .20 | Flash #F5FFE9 75 | Zoom .025 120
- 120-300 @caster L[0:3] w6 a.42 glow #E7F6D6 .35 | Decal 3->3.2 #C5EED8 .20 | Ring 3.4 230 #F3F7DC | Stream Embers 24 #FFF7E5
- 300-450 @caster I[1:2] w5.5 a.28 glow #E6F0D7 .20 | Decal 3.2->3 #C9EBD6 .14 | Stream Embers 16 #F6F9E8
- 450-610 @caster I[2:3] w5 a.18 glow #F1E9CF .12 | Decal 3->2 #D0E8D6 .10 | Stream Embers 10 #FFF5E1
- 610-760 @caster I[3] w4.5 a.06 glow #E5E6CE .05 | Decal 2->0 #D0E8D6 .10 | Stream Embers 5 #F5F2DD
- Unique: an immediate white-mint radial wave with feathers and expanding rings, not an explosive blast

## 8. Divine Ascension — 1650 ultimate (heal radius 5 at 0, damage radius 5 at 100; lock 900)
- 0-100 @caster C[0:2] w7 a.50 glow #FFF7D8 .65 | Decal .5->5 #D5F2D5 .30 | Embers 36 #FFF8E7 | Ring 5 250 #EAF8D9 | Flash #FFF8E5 60 | Zoom .025 150
- 100-260 @caster I[0:2] w9 a.50 glow #FFF1BA .90 | Decal 5 #E8E5BC .30 | Embers 65 #FFF2C9 | Dust 4 | Ring 5 240 #FFF1C5 + 5.8 330 #D7F1D2 | HitStop 90 | Shake 430 .72 | Flash #FFF5D9 170 | Zoom .085 310
- 260-520 @caster L[0:2] w8 a.45 glow #FFF2C8 .55 | Decal 5 #D7EFD3 .22 | Ring 5.3 310 #F1EECF | Stream Embers 42 #FFF8E8
- 520-900 @caster L[0:3] w7 a.32 glow #F4EFCF .42 | Decal 5->4.5 #D5E9CF .18 | Stream Embers 48 #FFF5D8
- 900-1200 @caster I[1:2] w7 a.24 glow #F2E7C7 .28 | Decal 4.5->4 #D3E7D0 .14 | Ring 4.5 260 #E9EED5 | Stream Embers 30 #FFF8E6
- 1200-1450 @caster I[2:3] w6.5 a.16 glow #EDE7D0 .15 | Decal 4->2.5 #D5E7D1 .10 | Stream Embers 22 #F8F4E3
- 1450-1650 @caster I[3] w5.5 a.06 glow #EEE4C9 .06 | Decal 2.5->0 #D5E7D1 .10 | Stream Embers 10 #F6F0DC
- Unique: instant mass heal, a colossal white-gold impact 100 ms later, translucent wings and a rising pillar of light

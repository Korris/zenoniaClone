# Assassin VFX choreography (ChatGPT "Tan Pham", 2026-10-30) — Earth-Break-quality spec

Source chat: https://chatgpt.com/c/6ae450c1-7638-83ec-a5ea-717aa58678ea
Notation as in vfx-choreography-warrior.md. Compact: `from-to anchor row[f0:f1] w a glow #hex alpha | particles | Ring r ms #hex | HitStop | Shake ms v | Flash #hex ms | Zoom v ms | sfx`.
Stream = particles spread over the phase. Identity: fast, sharp, silent, thin violet-black shadow and cold steel, afterimages, little dust, no fire. Earth Break style finisher only on the ultimate.

## 1. Shadow Strike — 420 (hits 0, 120; lock 300)
- 0-65 @target face I[0:1] w3.6 a.85 glow #8C45D8 .40 | Embers 8 #A47BE6 | Dust 2 | Ring .7 65 #7946B5 | Shake 45 .04 | sfx impact
- 65-120 @target face C[0:2] w2.8 a.50 glow #6433A6 .22 | Stream Embers 5 #67409D
- 120-205 @target face I[2:3] w3.8 a1 glow #BD73FF .55 | Embers 12 #C69AFF | Dust 3 | Ring 1.2 100 #A65CE8 | HitStop 30 | Shake 95 .16 | Flash #A976E6 40 | Zoom .018 90
- 205-300 @caster L[0:2] w2.5 a.35 glow #6E3B9B .15 | Stream Embers 5 #74509C
- 300-420 @target face I[3] w3 a.12 glow #5A347C .06 | Embers 3 #8655B3
- Unique: two crossing violet dagger cuts, the second completes a sharp X

## 2. Shadow Step — 430 (blink 0, hit 80; lock 260)
- 0-80 @caster C[0:3] w2 a.65 glow #6734A0 .30 | Decal .4->.9 #321B4C .45 | Embers 8 #8855BD | Ring .8 90 #6A3BA3 | Dust 2 | sfx cast
- 0-80 @target L[0:1] w1.8 a.50 glow #8750BD .25 | Decal .3->.8 #28173F .40 | Stream Embers 7 #70439B
- 80-175 @target face I[0:2] w2.6 a1 glow #C185FF .65 | Embers 14 #D4ADFF | Ring 1 100 #A05BE3 | HitStop 45 | Shake 120 .24 | Flash #A46AE0 55 | Zoom .025 120 | sfx impact
- 175-260 @target I[2:3] w2.3 a.45 glow #67358E .18 | Stream Embers 8 #69438C | Dust 2
- 260-430 @target I[3] w1.8 a.10 glow #46265E .05 | Embers 3 #7A54A0
- Unique: black-violet vanish and reappear behind the enemy, one precise backstab at 80 ms

## 3. Blade Dance — 780 (hits 0, 150, 300, 450; lock 600)
- 0-90 @caster L[0:1] w4 a.72 glow #8B45D0 .35 | Decal 1->2 #35214F .22 | Embers 8 #A078DB | Dust 3 | Shake 55 .04
- 90-150 @caster L[1:2] w4 a.65 glow #7840B6 .28 | Stream Embers 6 #7954AC
- 150-240 @caster L[2:3] w4 a.82 glow #A35CE8 .42 | Embers 10 #BA8CF0 | Ring 1.7 100 #814CBF | Shake 60 .05
- 240-300 @caster L[0:1] w4 a.68 glow #8342B8 .30 | Stream Embers 7 #8656B8
- 300-390 @caster L[1:3] w4 a.88 glow #B46CF0 .48 | Embers 12 #C69AF2 | Ring 1.9 100 #9A5DDB | Shake 65 .07
- 390-450 @caster C[2:3] w3.5 a.60 glow #A35AE0 .35 | Stream Embers 8 #A16ACF
- 450-570 @caster I[0:2] w4.2 a1 glow #D19AFF .65 | Embers 20 #DBB7FF | Dust 5 | Ring 2.2 150 #B975F2 | HitStop 35 | Shake 145 .27 | Flash #B88AEF 65 | Zoom .035 140 | sfx impact
- 570-600 @caster I[2:3] w4 a.45 glow #8550B2 .20 | Stream Embers 6 #9B6AC5
- 600-780 @caster I[3] w3.5 a.12 glow #593477 .06 | Embers 4 #8C65AE
- Unique: four rhythmic cuts forming overlapping thin violet circles, a crossed-blade snap to finish

## 4. Phantom Rush — 850 (dashes 0-180, 180-360, 360-540; hits 180, 360, 540; lock 600)
- 0-180 @caster L[0:3] w2 a.70 glow #7542B2 .32 | Decal .3->.7 #342048 .25 | Stream Embers 12 #8A5CC2 | Dust 3
- 180-245 @caster I[0:1] w2.5 a.85 glow #A96BE8 .45 | Embers 9 #BB90F0 | Ring .8 80 #8E58C9 | Shake 55 .05
- 180-360 @caster L[0:3] w2 a.75 glow #8550C0 .38 | Stream Embers 14 #9B6DD0 | Dust 3
- 360-425 @caster I[1:2] w2.6 a.90 glow #B57AF0 .50 | Embers 11 #CAA0F5 | Ring .9 90 #A26ADD | Shake 65 .07
- 360-540 @caster L[0:3] w2.2 a.82 glow #9B5BD8 .45 | Stream Embers 16 #B07DE5 | Dust 4
- 540-660 @caster I[0:3] w3 a1 glow #D39BFF .70 | Embers 22 #DBB5FF | Dust 6 | Ring 1.4 140 #B87AEC | HitStop 40 | Shake 160 .32 | Flash #B78AEE 75 | Zoom .040 150 | sfx impact
- 660-850 @caster I[3] w2.8 a.18 glow #69418C .10 | Stream Embers 8 #8255A8
- Unique: three rapid dashes each leaving a black-violet afterimage, the third a steel finisher

## 5. Venom Daggers — 470 (hit 0, poison)
- 0-100 @target face I[0:2] w3.5 a.90 glow #8C43B8 .40 | Embers 10 #7ACB80 | Dust 2 | Ring 1 90 #7356A8 | HitStop 30 | Shake 90 .14 | Flash #9863B5 35 | Zoom .015 85 | sfx impact
- 100-230 @target face L[0:3] w3.5 a.65 glow #6E9C52 .28 | Decal .5->2 #395B38 .18 | Stream Embers 16 #6CBF76
- 230-350 @target face I[2:3] w3.2 a.35 glow #527F49 .16 | Embers 9 #78AF73 | Dust 2
- 350-470 @target face I[3] w3 a.10 glow #465B48 .05 | Stream Embers 5 #65956A
- Unique: narrow double slash with thin violet edges and cold green venom threads, no poison cloud
- Lead's change: widths cut from ChatGPT's 4.5 to 3.5 to match the real cone (radius 2.5 drawn centred 1.25 ahead).

## 6. Shadow Clone — 600 spawn accent (KeepsSimArt: the game draws the clone and its strikes)
- 0-120 @caster Decal .3->.8 #241733 .40 | Ring .7 110 #69408E | Embers 6 #8756B0 | Dust 2
- 120-280 @caster Decal .8->1.1 #38204D .32 | Ring 1.2 160 #8754B7 | Stream Embers 12 #8D5DB9
- 280-430 @caster Decal 1.1->.7 #352048 .22 | Ring 1.4 150 #6F419C | Embers 8 #9C71C5
- 430-600 @caster Decal .7->0 #241733 .22 | Stream Embers 6 #6C4A8D
- Unique: a silent silhouette splits from the hero with only a compact violet-black ground pulse

## 7. Death Mark — 850 pulse (mark at 0; 10000 ms on the monster; no sustain)
- 0-130 @target C[0:2] w1.8 a.45 glow #A15CDE .30 | Decal .2->.6 #45215E .25 | Ring .6 110 #9252CB | Embers 7 #B180DF | sfx cast
- 130-320 @target L[0:3] w1.8 a.50 glow #BC75F0 .35 | Ring .9 170 #A968E0 | Stream Embers 9 #AF7BD7
- 320-530 @target I[0:2] w2 a.45 glow #A65DD8 .30 | Decal .6->1 #4E2469 .18 | Embers 8 #C193E8
- 530-700 @target I[2:3] w1.6 a.25 glow #7B3FA8 .15 | Stream Embers 6 #9867BC
- 700-850 @target I[3] w1.2 a.08 glow #603282 .06 | Embers 3 #8656A8
- Unique: a small cold violet sigil locks onto one enemy, then disappears while the mark stays

## 8. Nightfall Execution — 1380 ultimate (hits every 120 ms from 0 to 840; lock 1000)
- 0-75 @caster L[0:1] w5.5 a.55 glow #7136A8 .30 | Decal 0->2 #21152E .35 | Embers 10 #8B5CC0 | Shake 50 .03 | sfx cast
- 75-120 @caster C[0:1] w5.5 a.35 glow #643197 .18 | Stream Embers 7 #74509C
- 120-195 @caster L[1:2] w5.7 a.62 glow #8744BE .35 | Embers 11 #A275D0 | Ring 1.5 100 #70409E | Shake 55 .04
- 195-240 @caster C[1:2] w5.7 a.35 glow #6C349C .20 | Stream Embers 8 #8456B0
- 240-315 @caster L[2:3] w5.8 a.68 glow #9852CE .40 | Embers 12 #B183DC | Ring 1.8 110 #824DB0 | Shake 60 .05
- 315-360 @caster C[2:3] w5.8 a.38 glow #7136A0 .22 | Stream Embers 8 #9260BB
- 360-435 @caster L[0:2] w6 a.72 glow #A15AD8 .43 | Embers 14 #B990E5 | Ring 2 110 #9255C4 | Shake 65 .06
- 435-480 @caster C[0:1] w6 a.40 glow #7B3BA8 .24 | Stream Embers 9 #9A68C5
- 480-555 @caster L[1:3] w6 a.76 glow #AE68E2 .46 | Embers 15 #C39BED | Ring 2.2 120 #9D60D0 | Shake 70 .07
- 555-600 @caster C[1:2] w6 a.42 glow #8440B3 .25 | Stream Embers 10 #A474D0
- 600-675 @caster L[0:3] w6 a.80 glow #B774EB .50 | Embers 16 #CCA5F2 | Ring 2.4 120 #AB6BDD | Shake 75 .08
- 675-720 @caster C[2:3] w6 a.45 glow #9047BD .28 | Stream Embers 12 #B180DC
- 720-795 @caster L[1:3] w6 a.88 glow #CA89F5 .55 | Embers 20 #DAB8FA | Ring 2.7 130 #BA7BEA | Shake 80 .08
- 795-840 @caster C[3] w5.5 a.48 glow #A05CCB .35 | Tele 1.5->3 #8C45C0 | Stream Embers 16 #C293E9
- 840-1020 @caster I[0:2] w6.2 a1 glow #DDA9FF .85 | Decal 1.5->3 #482160 .40 | Embers 48 #D6A5FF | Debris 8 | Dust 10 | Ring 3 190 #C185F5 + 3.6 260 #8E4AC8 | HitStop 60 | Shake 360 .75 | Flash #C7A1F0 140 | Zoom .085 290 | sfx impact
- 1020-1190 @caster I[2:3] w6 a.55 glow #9D55CF .35 | Decal 3->2 #351C4A .28 | Embers 22 #B984E3 | Dust 8 | Ring 3.2 170 #8145B0 | Shake 110 .10
- 1190-1380 @caster I[3] w5.5 a.12 glow #65358A .10 | Decal 2->0 #241631 .18 | Stream Embers 12 #8052A4
- Unique: seven razor-thin violet-black cuts accumulate, then an enormous crossed X detonates at 840 ms

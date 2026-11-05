# Frost Hollow 72x48 spawn clusters (tile rects; adjusted to the real rows in P6)

The 5 old zones stay in `FrostHollowMap.Zones` and are valid on the new ground-only rows (free walkable tiles per rect: 44/45, 18/20, 40/40, 40/40, 24/24; all reachable from start).
New clusters go into `MapSpawnTable.FrostHollowOuter` (plumbing done in P2, array empty): `ZonesFor("frostHollow")` = old 5 + outer.

| # | Area | Type | Lv | Count | tx,ty,w,h | Respawn ms |
|---|---|---|---|---|---|---|
| 1 | South of village gate | frostSlime | 1 | 6 | 2,27,10,5 | 7000 |
| 2 | South-west | iceGoblin | 2 | 6 | 3,37,12,6 | 8000 |
| 3 | South bank, east of river | spectralWolf | 2 | 5 | 26,28,10,6 | 8500 |
| 4 | East of old section | iceGoblin | 2 | 5 | 38,8,10,7 | 8000 |
| 5 | North shore of lake | spectralWolf | 2 | 6 | 40,19,12,5 | 9000 |
| 6 | Watchtower ledge | iceWraith | 7 | 7 | 50,2,12,7 | 9500 |
| 7 | North-east field | iceWraith | 7 | 6 | 58,10,10,8 | 9500 |
| 8 | South of lake | frostbiteYeti | 8 | 6 | 40,37,14,6 | 10000 |
| 9 | Den cave approach | crystalGolem | 9 | 5 | 54,24,9,9 | 10500 |
| A | South-east corner | frostbiteYeti | 8 | 5 | 58,38,10,6 | 10000 |

New 57 + old 18 = 75 (+ King). Ids per lead: iceWraith Lv7, frostbiteYeti Lv8, crystalGolem Lv9 (snowYeti renamed).

Constraints for P6: every rect at least 5 tiles (80 units, EntranceClearance) from start (4,21), village gate (2,21), Den portal (71,30-32) and arrival (69,31); rect has at least 2x count walkable tiles; clusters about 12 tiles apart.

Level gap: everything near the old section is Lv1-2, the far side Lv7-9. The user may want Lv3-5 tiers in the middle fields (4, 5, 7), reusing thornBoar / meadowMantis / brambleTreant or new sheets.


## Update 2026-11-05 (shipped table, MapSpawnTable.FrostHollowOuter): 12 clusters, 66 new + 18 old = 84
Level gradient: old valley Lv1-2 -> Lv3 snowFox (3, 4) -> Lv4 iceImp (5) -> Lv5 frostBear (6, C) -> Lv7 iceWraith (7) -> Lv8 yeti (8, A) -> Lv9 golem (9, B).
| # | Type | Lv | Count | tx,ty,w,h | Respawn |
|---|---|---|---|---|---|
| 1 | frostSlime | 1 | 6 | 2,27,10,5 | 7000 |
| 2 | iceGoblin | 2 | 6 | 3,37,12,6 | 8000 |
| 3 | snowFox | 3 | 6 | 26,28,10,6 | 7500 |
| 4 | snowFox | 3 | 5 | 38,8,10,7 | 7500 |
| 5 | iceImp | 4 | 6 | 40,19,12,5 | 9000 |
| 6 | frostBear | 5 | 5 | 48,8,9,5 | 9500 |
| 7 | iceWraith | 7 | 6 | 57,7,9,7 | 9500 |
| 8 | frostbiteYeti | 8 | 6 | 40,37,14,6 | 10000 |
| 9 | crystalGolem | 9 | 5 | 56,15,6,9 | 10500 |
| A | frostbiteYeti | 8 | 5 | 58,38,10,6 | 10000 |
| B | crystalGolem | 9 | 5 | 43,1,13,3 (watchtower plateau; stairs 49-50,5-6 below) | 10500 |
| C | frostBear | 5 | 5 | 25,38,10,6 | 9500 |

# World expansion design (ChatGPT, not approved yet)

Source: ChatGPT, account Tan Pham, 2026-10-30: https://chatgpt.com/c/6ae4a6ab-d4fc-83ec-ae3d-102a9f59b0ac
User direction: villages interleaved with hunting maps, not one long chain of hunting maps; learn from other games (Zenonia, Ragnarok Online, MapleStory, Toram, Dragon Nest), structure only, nothing copied.
Format: 36 x 24 tiles, 64 px per tile, backdrop 2304 x 1536 px, buildings and tall props as separate y-sorted sprites, collision rows T g d p w B.

## Rhythm
Town -> 2 hunting maps (sometimes an optional side area) -> boss / dungeon (first win unlocks the next town) -> next town. Travel stays two-way; unlocked towns become fast-travel points.

## Regions
| Town | Levels | Hunting | Optional | Boss / dungeon |
|---|---|---|---|---|
| Oakhaven (existing) | 1-20 | Bramblewood Trail, Frost Hollow (existing) | - | Frostfang Den |
| Sunspire Oasis (trade, player stalls) | 20-35 | Amber Dunes, Emberfen Marsh | Glasswind Caverns | Sunken Reliquary |
| Cinderwatch Hold (communal forge, party recruiting) | 35-48 | Ashen Wastes, Obsidian Rift | Sulfur Burrows | Emberheart Crucible |
| Moonveil Sanctuary (endgame social hub) | 48-60 | Silvermist Grove, Eclipse Highlands | Whispering Hollow | Astral Throne |

19 maps: 4 towns, 8 hunting, 4 boss / dungeon, 3 optional.

## Map table
| Map | Type | Level | Biome | Connects to |
|---|---|---|---|---|
| Oakhaven | Town, existing | 1-20 | Green woodland | Bramblewood, Frost Hollow |
| Bramblewood Trail | Hunting | 1-10 | Temperate forest | Oakhaven, Frost Hollow |
| Frost Hollow | Hunting, existing | 8-20 | Snowfield | Oakhaven, Bramblewood, Frostfang Den |
| Frostfang Den | Boss dungeon | 18-20 | Frozen cavern | Frost Hollow, Sunspire (unlock) |
| Sunspire Oasis | Town | 20-35 | Desert oasis | Frostfang Den (unlock), Amber Dunes |
| Amber Dunes | Hunting | 20-27 | Golden desert | Sunspire, Emberfen, Glasswind |
| Glasswind Caverns | Optional | 24-30 | Crystal caves | Amber Dunes |
| Emberfen Marsh | Hunting | 27-35 | Tropical swamp | Amber Dunes, Sunken Reliquary |
| Sunken Reliquary | Boss dungeon | 33-35 | Flooded ruins | Emberfen, Cinderwatch (unlock) |
| Cinderwatch Hold | Town | 35-48 | Volcanic fortress | Sunken Reliquary (unlock), Ashen Wastes |
| Ashen Wastes | Hunting | 35-42 | Volcanic badlands | Cinderwatch, Obsidian Rift, Sulfur Burrows |
| Sulfur Burrows | Optional | 39-45 | Magma caves | Ashen Wastes |
| Obsidian Rift | Hunting | 42-48 | Black volcanic canyon | Ashen Wastes, Emberheart Crucible |
| Emberheart Crucible | Boss dungeon | 46-48 | Volcanic temple | Obsidian Rift, Moonveil (unlock) |
| Moonveil Sanctuary | Town | 48-60 | Enchanted twilight forest | Emberheart Crucible (unlock), Silvermist Grove |
| Silvermist Grove | Hunting | 48-55 | Luminous forest | Moonveil, Eclipse Highlands, Whispering Hollow |
| Whispering Hollow | Optional | 52-58 | Haunted woodland | Silvermist Grove |
| Eclipse Highlands | Hunting | 55-60 | Astral highlands | Silvermist Grove, Astral Throne |
| Astral Throne | Final boss dungeon | 60 | Eclipse ruins | Eclipse Highlands |

## Open points
- The first draft's collision grids and portal positions (in the chat) belong to the old 7-map chain: redo them per map. ChatGPT itself says they need a flood-fill check.
- ChatGPT lists Oakhaven as both Lv 1-15 and Lv 1-20; the table above uses 1-20.
- Frost Hollow gains one new exit (Frostfang Den), so it is not fully unchanged.
- The game has 2 maps today, offline portals only, and each hunting map also needs new monster art and data. Online multi-zone is not built yet.

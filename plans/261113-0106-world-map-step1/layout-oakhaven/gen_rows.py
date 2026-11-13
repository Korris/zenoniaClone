COLS, ROWS = 36, 24
grid = [['T']*COLS for _ in range(ROWS)]

def rect(c0,r0,c1,r1,ch):
    for r in range(r0,r1+1):
        for c in range(c0,c1+1):
            if 0<=r<ROWS and 0<=c<COLS:
                grid[r][c] = ch

# --- base forest everywhere (default T) ---

# River (north part, above bridge) and (south part, below bridge)
rect(30,1,34,8,'w')
rect(30,13,34,23,'w')

# Bridge crossing + east bank road to the edge gate (rows 9-12)
rect(24,9,35,12,'B')

# Plaza + paths (big open walkable core), carve first, buildings painted after to override
rect(9,7,29,20,'d')

rect(12,0,24,6,'T')
# apron right below guild hall door so plaza connects to the door
rect(14,7,22,7,'p')

# Healer / apothecary cottage (NW) with shop counter
rect(1,1,10,6,'T')
# courtyard / shop apron in front (healerNina stands here)
rect(2,7,9,8,'d')

# Market stall (red/white awning) - solid block
rect(6,8,11,10,'T')

# Forge (NE) - solid block, open apron below it leading to the bridge
rect(23,1,31,8,'T')
rect(24,9,31,9,'d')

# Well (plaza centre) - small solid feature
rect(17,10,18,11,'T')

# South-west farm plot (fenced field) - fully solid/decorative, not walked on
rect(0,11,8,20,'T')

# Bottom-left cottage (left of the south path)
rect(9,13,15,19,'T')
# Bottom-right cottage (right of the south path)
rect(20,13,29,19,'T')

# South corridor from the plaza down through the gap between the two cottages
rect(16,11,19,19,'d')
rect(12,19,19,20,'d')
rect(12,20,16,20,'d')

# South gate approach + HUD-blocked bottom rows
rect(0,21,35,23,'T')       # rows 21-23 forced blocked (under HUD), then reopen the gate below
rect(12,21,16,21,'d')      # the gate row itself stays open
rect(13,21,14,21,'d')

# River bank strip beside the farm/cottage (keep solid forest, nothing to do - already T)

for r in grid:
    assert len(r) == COLS
    bad = set(r) - set('TgdpwB')
    assert not bad, bad

with open('plans/261113-0106-world-map-step1/layout-oakhaven/rows.txt','w',encoding='utf-8') as f:
    for r in grid:
        f.write(''.join(r) + '\n')
print('wrote rows.txt')

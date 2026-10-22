# Zenonia Web

A Zenonia-style chibi pixel-art action RPG that runs in the browser. Vanilla JavaScript (ES modules) + HTML5 Canvas. No framework, no build step, no backend. World art comes from the CC0 "Ninja Adventure" asset pack by pixel-boy (see `assets/ninja-adventure/CREDITS.txt`): NPCs with facesets, monsters, a boss, tilesets (field, water, cliffs, nature, houses, desert, dungeon) and FX. The hero is a custom 24px set (idle, walk, 3 combo attacks, dash, hurt, death + slash/dash/spark FX) built from an AI concept sheet by `tools/build-hero-sprites.py` and checked by `tools/validate_sprites.py` (see `assets/custom/hero/README.md`). Sprites are sliced at boot by `src/graphics/asset-atlas.js`.

The hero also has an **HD rendering path**: hand-painted, Zenonia-4-style sheets in `assets/hd/`, normalized by `tools/build-hd-hero.py` into `assets/hd/hero/normalized/` (see that folder's README). World/gameplay units don't change -- HD frames just carry more source pixels per world unit and get drawn with image smoothing on, crisp instead of chunky, alongside the still-pixel-art rest of the world. See `docs/system-architecture.md` → "HD Assets". Ground tiles (grass, dirt path, water, sand/stone/cave/lava floors, cliffs, walls, portal) have the same HD path, normalized by `tools/build-hd-tiles.py` into `assets/hd/tiles/normalized/`. Props (trees, rocks, houses, fences, torches, crystals...) have it too, normalized by `tools/build-hd-props.py` into `assets/hd/props/normalized/`.

## Features

- Chibi hero with 4-direction walk animation, sword swings and a 3-hit combo chain (finisher does bonus damage and knockback).
- Monsters with wander / chase / telegraph / attack AI, hit flash, knockback, floating damage numbers.
- EXP and levels (`floor(50 * level^1.5)` EXP per level), stat growth, full heal and +1 skill point per level.
- Zenonia-style skill tree (Tab): 15 nodes across 3 branches (Sword/Body/Spirit) x 3 tiers, 5 levels each, gated by hero level and prerequisite nodes. Actives (Slash Wave, Whirlwind, Heavy Blow, Blade Storm, Dash Strike, Healing Light, Battle Cry, Spirit Bolt) bind to a 3-slot hotbar (K/L/I); passives (Power Strike, Vitality, Iron Skin, Swift Feet, Second Wind, Focus, Crit Eye) boost stats like HP, defense, speed, crit chance and regen.
- 5 maps connected by level-gated portals: Oakhaven Village (safe starting hub, no monsters) <-> Verdant Meadow -> Dark Cave (Lv 3) -> Scorched Desert (Lv 7) -> Ruined Castle (Lv 12) with the Ancient Golem boss.
- Drops (HP/MP potions, gold, equipment), respawning spawn zones, death with EXP penalty and respawn, victory screen.
- Equipment & inventory (B): monsters have a chance to drop weapons/armor (bosses guaranteed rare+); 4 rarities (common/uncommon/rare/epic) scale item stats; 20-slot bag + 5 equipment slots (weapon/helmet/armor/gloves/boots) with a Zenonia-style panel for equipping, comparing and dropping items.
- 10 hand-painted Oakhaven villagers (elder, hunter, Mia, merchant, fisherman, carpenter, farmer, guard, child, healer) drawn at hero scale from HD idle sheets (`assets/hd/map/Oakhaven/` -> `tools/build-hd-npcs.py` -> `assets/hd/npcs/normalized/`), with HD dialog portraits; the 3 field NPCs (miner, nomad, knight) still use the CC0 pixel pack. Merchant/fisherman/carpenter/farmer/guard/child/healer are dialogue-only for now (no shop/crafting/fishing systems exist).
- Quest system: village NPCs with `!` (quest available) / `?` (ready to turn in) markers, dialog boxes with accept/decline, 9 kill/travel quests with level gates and prerequisites, quest log (Q) and on-screen tracker.
- Zenonia-style HUD: gold-framed bar with hero faceset, HP/MP/EXP, skill slots; minimap top-left; name tags; dialog boxes with NPC facesets.
- Save/load in `localStorage` (autosave on map change, level up, quest events, every 30 s; manual save from pause menu). Quest progress and inventory/equipment are saved too.

## Run

ES modules need an HTTP server (opening `index.html` via `file://` will not work).

```bash
npm start            # npx serve on http://localhost:5173
# or
python -m http.server 5173
```

Open `http://localhost:5173` in a browser.

## Controls

| Action | Keys |
|--------|------|
| Move | W A S D / Arrow keys |
| Attack (combo) | J or Space |
| Hotbar skills 1 / 2 / 3 | K / L / I (whatever active skill is bound to that slot) |
| Quick dash (no MP, brief invulnerability) | Double-tap a direction key |
| Talk to NPC | E (when "[E] Talk" shows) |
| Quest log | Q |
| Skill panel | Tab (or T) |
| Inventory / equipment | B |
| Pause | Esc |
| Menu select / confirm | Arrows + Enter, or mouse click |

## Tests

```bash
npm test             # node --test tests/*.test.js
```

Tests cover the pure logic modules: leveling, combo state machine, skill rules, quests, damage/loot/AI, equipment/inventory, tilemap collision, camera, save serialization, sprite animation, map invariants (reachability, props never cover NPCs/portals).

```bash
npm run check:browser     # headless Chrome smoke test: boot, combo chain, dash, death screenshots (needs the server running)
npm run sprites:build     # rebuild the hero set from assets/source/character-concept.png
npm run sprites:validate  # size / RGBA / grid / palette checks for assets/custom/hero
npm run hd:build          # normalize assets/hd/hero/*.png (HD, hand-painted) into assets/hd/hero/normalized/
npm run hd:tiles          # normalize assets/hd/tiles/*.png (HD ground tiles) into assets/hd/tiles/normalized/
npm run hd:props          # normalize assets/hd/props/*.png (HD props) into assets/hd/props/normalized/
npm run hd:npcs           # normalize assets/hd/map/Oakhaven/*.png (HD villager idle sheets) into assets/hd/npcs/normalized/
```

## Project layout

```
index.html, styles.css, package.json
assets/ninja-adventure/  CC0 art (tilesets, characters, monsters, boss, fx, items, ui) + CREDITS.txt
assets/custom/hero/      generated hero sprite sheets + FX strips + README
assets/source/           concept art the hero set is built from
assets/hd/hero/          raw + normalized HD (hand-painted) hero sheets + README
assets/hd/tiles/         raw + normalized HD ground tile sheets + README
assets/hd/props/         raw + normalized HD prop (trees/rocks/houses/fences...) sheets + README
assets/hd/map/Oakhaven/  raw 4x3 HD villager idle sheets (+ village_reference.png one level up)
assets/hd/npcs/          normalized HD villager sheets + dialog portraits + README
tools/                   sprite build/validate scripts, headless browser smoke test
src/
  main.js            boot
  core/              game loop, input, camera, constants, seeded RNG
  graphics/          asset loader + atlas (sprite sheet slicing), sprite registry, renderers
  entities/          player, monster, npc, projectile, drops, effects, floating text
  systems/           pure gameplay logic (combo, leveling, skills, quests, combat, AI, loot, damage)
  world/             tilemap, map builder, prop definitions, 4 map definitions, spawner, map manager
  game/              world state, per-frame update, player controller, NPC interaction, quest flow, session, state machine
  ui/                HUD, minimap, dialog box, quest log/tracker, skill panel, menus, banner
  save/              serializer (pure) + localStorage wrapper
tests/               node:test unit tests
docs/                project documentation
plans/               implementation plan and reports
```

See `docs/` for architecture, code standards and deployment notes.

# Two-weapon revision (ChatGPT "Tan Pham", 2026-10-30) — PROPOSAL, awaiting user decision

Chat: https://chatgpt.com/c/6ae450c1-7638-83ec-a5ea-717aa58678ea
Idea: each class tree = Branch A main weapon (4 skills) + Branch B second weapon (3 skills) + shared ultimate (needs >= 10 SP in each branch + Lv 50). No weapon swap, no extra button; magazine unchanged.

| Class | Main weapon (4) | Second weapon (3) | Shared ult |
|---|---|---|---|
| Warrior (greatsword + kite shield) | Power Slash, Whirlwind, Earth Break, Flame Cleave | **Shield Charge** (replaces Charging Strike), **Shield Wall** (replaces Blade Cyclone: -45% frontal dmg 5 s, knockback immune), **Shield Quake** (replaces Inferno Crash: shield slam shockwave, stagger) | Berserker Awakening (+ shield crest, damage reduction) |
| Wizard (staff + spellbook) | Arcane Bolt, Fireball, Ice Storm, Frost Nova | Arcane Lance, Mana Shield, Elemental Burst (cast from the floating book) | Astral Cataclysm |
| Mechanic (cannon + drone) | Power Shot, Homing Rockets, Plasma Barrage, Rocket Punch | Target Lock, Robot Assault, EMP Burst (drone-centred) | Orbital Cannon |
| Assassin (main + off-hand dagger) | Shadow Strike, Shadow Step, Phantom Rush, Death Mark | Blade Dance, Venom Daggers, Shadow Clone | Nightfall Execution |
| Cleric (holy staff + spirit) | Heal, Holy Ray, Divine Light, Purification | Spirit Blessing, Sanctuary, Guardian Spirit | Divine Ascension |

Only Warrior loses skills (3 replaced by shield skills -> new VFX art needed). Other classes keep all 8, just re-grouped.

Scaling proposal: effective ATK = A + W1 + 0.25*W2 (main-weapon skill), A + W2 + 0.25*W1 (second-weapon skill), A + 0.65*(W1+W2) (ultimate); damage = coefficient * effective ATK. Buff % skills scale by rank only.
Enhance curve proposal: +2.5%/lvl to +10, +4%/lvl +11..+15 => +15 = 1.45x weapon power.
  !! CONFLICT: the game's current enhance curve (user-approved earlier) gives +15 = +405% damage; ChatGPT's curve would be a big nerf. Keep current curve unless the user decides otherwise.
  !! Today only the main weapon is enhanceable; second weapons (shield, book, drone, spirit, off-hand) need their own enhance slot/item.
Synergy bonuses (no new input): Shield Charge -> Earth Break = more stagger; Shadow Step -> Blade Dance = +crit; Robot Assault -> Plasma Barrage = +Overcharge.

## Follow-up (honest A vs B review, same chat) — ChatGPT recommends HYBRID (~80% B)
- Keep the single tree, all 40 skills + VFX; no 2 branches, no Warrior shield-skill swap. Main weapon drives skill damage (+405% at +15 kept).
- Second weapon enhanceable +1..+15 for specialised stats (NOT the +405% damage curve):
  Warrior shield = DEF/HP/damage reduction; Wizard spellbook = MP, cast speed, MP cost; Mechanic drone = robot damage + robot fire rate; Assassin off-hand = crit rate/crit damage; Cleric spirit = healing + shield strength.
- 1-2 signature interactions per class with existing skills: Earth Break multi-hit -> small HP shield 3 s; Ice Storm -> longer slow (spellbook); Plasma Barrage -> robot extra volley; Shadow Step -> next hit +crit (off-hand); Heal -> spirit adds small shield.
- Reasons: no double +15 damage stacking (MMO/PvP balance), keep finished skills/art and spend time on combat feel, still creates stone demand for the second item.
- Warning: +405% with fail chance = pay-to-win risk for PvP -> add fail protection/pity, farmable stones, PvP power cap.
- Two branches can come later as a big update if data shows players want more depth.

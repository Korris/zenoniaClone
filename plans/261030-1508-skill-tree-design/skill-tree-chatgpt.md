# Skill tree design (ChatGPT "Tan Pham", 2026-10-30) — DRAFT, awaiting user review

Source chat: https://chatgpt.com/c/6ae450c1-7638-83ec-a5ea-717aa58678ea
Icon ref: Assets/ArtLibrary/62121b6b-e18b-489a-99e4-93e54a28993f.png (8 icons per class, R1/R2 x C1-C4)

Rules: Lv 1-60, 1 SP/level (60 SP), 8 active skills/class, max rank 10, tiers Basic (1-19) / Advanced (20-39) / Ultimate (40-60).
Values = rank 1. Magazine: 3 slots/page, swipe to change page, cooldowns keep running across pages.
Kept signature skills: Wizard Ice Storm, Warrior Earth Break, Assassin Shadow Step, Cleric Heal. Mechanic robot = passive buddy.

## Mechanic (orange / electric blue, ranged physical) — passive Overcharge (100 energy => next skill +20%)
| Icon | Skill | Lv / prereq | MP·CD | Effect |
|---|---|---|---|---|
| R1C1 | Power Shot | 1 | 8·3s | 150% line 7x1 piercing |
| R1C2 | Homing Rockets | 8, Power Shot 3 | 16·7s | 3x75% homing, r6 |
| R1C3 | Plasma Barrage | 15, Power Shot 5 | 22·9s | 6x55% cone 6/60° |
| R1C4 | Target Lock | 20, Rockets 3 | 18·15s | mark 5 foes 8s, +15% dmg taken |
| R2C1 | Rocket Punch | 26, Barrage 3 | 24·11s | 280% line 4x2, knockback 2 |
| R2C2 | Robot Assault | 32, Target Lock 3 | 28·18s | robot 8s, 65%/s r6 |
| R2C3 | EMP Burst | 40, Rocket Punch 3 | 35·20s | 350% r3, stun 1.5s |
| R2C4 | Orbital Cannon (ult) | 50, Assault 5 + EMP 5 | 80·45s | 6x140% r4, range 7 |

## Wizard (blue / cyan / violet, magic AoE)
| R1C1 | Arcane Bolt | 1 | 10·3s | 160% line 7x1 |
| R1C2 | Fireball | 8, Bolt 3 | 18·6s | 230% r1.5 + burn 15%/s 4s |
| R1C3 | **Ice Storm** | 15, Bolt 5 | 28·12s | 6 waves x70% over 3s, r3, slow 40% (icicles fall from sky) |
| R1C4 | Arcane Lance | 20, Fireball 3 | 24·9s | 320% piercing line 8x1 |
| R2C1 | Mana Shield | 26, Ice Storm 3 | 25·22s | absorb 250% ATK 8s |
| R2C2 | Frost Nova | 32, Ice Storm 5 | 30·16s | 280% r3 self, freeze 2s |
| R2C3 | Elemental Burst | 40, Lance 3 + Nova 3 | 40·20s | 400% r2.5, +25% vs burning/frozen |
| R2C4 | Astral Cataclysm (ult) | 50, Burst 5 + Shield 3 | 90·50s | 8x120% r4 meteors |

## Warrior (red / orange / cracked earth, melee bruiser)
| R1C1 | Power Slash | 1 | 8·3s | 170% arc r2.5 120° |
| R1C2 | Whirlwind | 8, Slash 3 | 18·8s | 3x100% r2.5 spin |
| R1C3 | **Earth Break** | 15, Slash 5 | 26·12s | 380% cone 5 long, cracks + stone spikes |
| R1C4 | Flame Cleave | 20, Whirlwind 3 | 22·10s | 300% arc r3 140° + burn |
| R2C1 | Charging Strike | 26, Earth Break 3 | 24·12s | 300% dash line 5x2 |
| R2C2 | Blade Cyclone | 32, Whirlwind 5 | 32·18s | 5x90% moving r2.5 |
| R2C3 | Inferno Crash | 40, Cleave 3 + Charge 3 | 40·22s | 520% leap r3 |
| R2C4 | Berserker Awakening (ult) | 50, Cyclone 5 + Crash 5 | 75·50s | +25% ATK +15% atk speed 12s |

## Assassin (purple / magenta / black, burst melee)
| R1C1 | Shadow Strike | 1 | 8·3s | 2x95% X slash r1.8 |
| R1C2 | **Shadow Step** | 8, Strike 3 | 18·8s | blink behind target (r6) + 260%, +30% from behind |
| R1C3 | Blade Dance | 15, Strike 5 | 22·10s | 4x85% r2 spin |
| R1C4 | Phantom Rush | 20, Step 3 | 24·12s | 3 dashes x120% |
| R2C1 | Venom Daggers | 26, Dance 3 | 20·18s | 10s poison 25%/s |
| R2C2 | Shadow Clone | 32, Step 5 | 30·20s | clone 8s, 50% dmg |
| R2C3 | Death Mark | 40, Rush 3 | 35·22s | next 3 hits +50% |
| R2C4 | Nightfall Execution (ult) | 50, Clone 5 + Mark 5 | 75·45s | 8x125% r3 |

## Cleric (mint / gold / white, support + holy dmg) — spirit buddy passive
| R1C1 | **Heal** | 1 | 12·5s | heal 220% ATK, self / lowest ally r6 |
| R1C2 | Spirit Blessing | 8, Heal 3 | 18·18s | +15% ATK party r4 12s |
| R1C3 | Holy Ray | 15, Heal 5 | 20·7s | 260% line 7x1.5 |
| R1C4 | Divine Light | 20, Blessing 3 | 24·12s | 300% dmg + 100% heal r2 |
| R2C1 | Sanctuary | 26, Heal 5 | 30·20s | zone r3 8s heal 35%/s |
| R2C2 | Guardian Spirit | 32, Blessing 5 | 32·22s | shield 200% ATK r4 8s |
| R2C3 | Purification | 40, Light 3 + Sanctuary 3 | 38·20s | 350% r3 + cleanse |
| R2C4 | Divine Ascension (ult) | 50, Guardian 5 + Purif 5 | 85·50s | heal 450% + 550% dmg r5 |

Scaling (all): damage +~10-15% of base per rank, MP +1..5 per rank, cooldown fixed.

## Notes / open
- Ice Storm unlocks at Lv 15 in this draft, Shadow Step Lv 8 — today they are available from Lv 1; decide.
- Shadow Step vs bosses: land at the nearest free rear/side spot outside the collision box.
- Next: ChatGPT VFX preview sheets per class -> user review -> final VFX.

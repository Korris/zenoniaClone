// Hero entity: stats, progression, resources and transient combat state.
// Position is center-x / feet-y in world px. Movement/combat logic lives in player-controller.
import { ComboController } from '../systems/combo-attack-system.js';
import { statsForLevel, applyExpGain, applyDeathPenalty } from '../systems/leveling-system.js';
import { computePassiveModifiers, HOTBAR_SLOTS } from '../systems/skill-system.js';
import { computeEquipmentModifiers } from '../systems/equipment-system.js';
import { EQUIPMENT_SLOTS } from '../data/item-definitions.js';
import { HERO_HITBOX, INVULNERABLE_AFTER_HIT_MS } from '../data/hero-stats-config.js';
import { entityBox } from '../systems/damage-formula.js';

export class Player {
  constructor() {
    this.x = 0;
    this.y = 0;
    this.dir = 'down';
    this.level = 1;
    this.exp = 0;
    this.skillPoints = 1;
    this.skills = {};
    this.hotbar = new Array(HOTBAR_SLOTS).fill(null);
    this.buffs = { atkMult: 1, timer: 0 };
    this.gold = 0;
    this.inventory = []; // bag of unequipped item instances, see data/item-definitions.js
    this.equipment = Object.fromEntries(EQUIPMENT_SLOTS.map((slot) => [slot, null]));
    this.hasUnseenItem = false; // drives the HUD's "NEW" blink until the bag is opened
    this.cooldowns = {};
    this.combo = new ComboController();
    this.invulnTimer = 0;
    this.animTimer = 0;
    this.moving = false;
    this.dead = false;
    this.dash = null; // { timer, dirX, dirY, speed, damageMult, hitIds:Set }
    this.knockback = { vx: 0, vy: 0, timer: 0 };
    this.hitFlashTimer = 0;
    this.deathTimer = 0; // drives the death animation
    this.idleTimer = 0; // drives the idle breathing animation
    this.recomputeStats();
    this.hp = this.maxHp;
    this.mp = this.maxMp;
  }

  get hitbox() {
    return HERO_HITBOX;
  }

  get box() {
    return entityBox(this, HERO_HITBOX);
  }

  /** Recalculate max stats from level + passives + equipment, clamping current HP/MP. */
  recomputeStats() {
    this.passiveMods = computePassiveModifiers(this);
    const equipMods = computeEquipmentModifiers(this);
    // Equipment crit folds into passiveMods so existing crit-roll call sites (player-damage.js,
    // combat-system.js) don't need to know about two separate sources.
    this.passiveMods.critChance += equipMods.critChance;
    const base = statsForLevel(this.level);
    this.maxHp = Math.round(base.maxHp * this.passiveMods.hpMult) + equipMods.hpAdd;
    this.maxMp = Math.round(base.maxMp * this.passiveMods.mpMult);
    this.atk = base.atk + equipMods.atkAdd;
    this.def = Math.round(base.def + this.passiveMods.defAdd) + equipMods.defAdd;
    this.speed = base.speed * this.passiveMods.speedMult * equipMods.speedMult;
    if (this.hp !== undefined) this.hp = Math.min(this.hp, this.maxHp);
    if (this.mp !== undefined) this.mp = Math.min(this.mp, this.maxMp);
  }

  /** Counts down a temporary buff (e.g. Battle Cry) and resets it on expiry. */
  tickBuffs(dt) {
    if (this.buffs.timer <= 0) return;
    this.buffs.timer -= dt;
    if (this.buffs.timer <= 0) {
      this.buffs.timer = 0;
      this.buffs.atkMult = 1;
    }
  }

  /** Returns actual damage taken (0 when invulnerable). */
  takeDamage(amount) {
    if (this.dead || this.invulnTimer > 0 || this.dash) return 0;
    const dealt = Math.min(this.hp, amount);
    this.hp -= dealt;
    this.invulnTimer = INVULNERABLE_AFTER_HIT_MS;
    this.hitFlashTimer = 120;
    if (this.hp <= 0) this.dead = true;
    return dealt;
  }

  heal(amount) {
    this.hp = Math.min(this.maxHp, this.hp + amount);
  }

  restoreMp(amount) {
    this.mp = Math.min(this.maxMp, this.mp + amount);
  }

  /** Adds EXP, resolves level-ups (full heal + skill points). Returns levels gained. */
  gainExp(amount) {
    const result = applyExpGain({ level: this.level, exp: this.exp }, amount);
    this.level = result.level;
    this.exp = result.exp;
    if (result.levelsGained > 0) {
      this.skillPoints += result.skillPointsGained;
      this.recomputeStats();
      this.hp = this.maxHp;
      this.mp = this.maxMp;
    }
    return result.levelsGained;
  }

  /** Respawn after death: apply EXP penalty, restore resources. */
  revive() {
    const penalized = applyDeathPenalty({ level: this.level, exp: this.exp });
    this.exp = penalized.exp;
    this.dead = false;
    this.deathTimer = 0;
    this.hp = this.maxHp;
    this.mp = this.maxMp;
    this.invulnTimer = 1000;
    this.dash = null;
    this.combo.reset();
    return penalized.lost;
  }

  toSaveData() {
    return {
      level: this.level, exp: this.exp, skillPoints: this.skillPoints, skills: { ...this.skills },
      hotbar: [...this.hotbar], gold: this.gold, hp: this.hp, mp: this.mp,
      inventory: [...this.inventory], equipment: { ...this.equipment },
    };
  }

  applySaveData(data) {
    this.level = data.level;
    this.exp = data.exp;
    this.skillPoints = data.skillPoints;
    this.skills = { ...data.skills };
    this.hotbar = Array.isArray(data.hotbar) ? [...data.hotbar] : new Array(HOTBAR_SLOTS).fill(null);
    this.gold = data.gold;
    this.inventory = Array.isArray(data.inventory) ? [...data.inventory] : [];
    this.equipment = data.equipment
      ? Object.fromEntries(EQUIPMENT_SLOTS.map((slot) => [slot, data.equipment[slot] || null]))
      : Object.fromEntries(EQUIPMENT_SLOTS.map((slot) => [slot, null]));
    this.recomputeStats();
    this.hp = Math.min(this.maxHp, data.hp ?? this.maxHp);
    this.mp = Math.min(this.maxMp, data.mp ?? this.maxMp);
  }
}

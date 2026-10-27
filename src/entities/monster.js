// Monster entity. Position is center-x / feet-y. AI state lives in `ai` (see monster-ai-system).
import { MONSTER_DEFINITIONS } from '../data/monster-definitions.js';
import { createAiState } from '../systems/monster-ai-system.js';
import { entityBox } from '../systems/damage-formula.js';

let nextMonsterId = 1;

export class Monster {
  constructor(type, x, y, zoneIndex = -1) {
    this.id = nextMonsterId++;
    this.type = type;
    this.def = MONSTER_DEFINITIONS[type];
    if (!this.def) throw new Error(`Unknown monster type: ${type}`);
    this.level = this.def.level ?? this.def.itemLevel ?? 1;
    this.x = x;
    this.y = y;
    this.zoneIndex = zoneIndex;
    this.maxHp = this.def.hp;
    this.hp = this.maxHp;
    this.ai = createAiState(x, y);
    this.dir = 'down'; // facing, drives which sheet column is drawn
    this.animTimer = Math.random() * 1000;
    this.hitFlashTimer = 0;
    this.knockback = { vx: 0, vy: 0, timer: 0 };
    this.dead = false;
    this.hitBySwing = new Set(); // swing ids that already damaged this monster
    this.isBoss = this.def.behavior === 'boss';
  }

  get box() {
    return entityBox(this, this.def.hitbox);
  }

  get isTelegraphing() {
    return this.ai.state === 'windup';
  }

  /** Apply damage and optional knockback impulse (px/s). Returns true if it died. */
  takeDamage(amount, knockbackX = 0, knockbackY = 0) {
    if (this.dead) return false;
    this.hp -= amount;
    this.hitFlashTimer = 100;
    if (!this.isBoss && (knockbackX || knockbackY)) {
      this.knockback = { vx: knockbackX, vy: knockbackY, timer: 120 };
    }
    // Getting hit wakes the monster up immediately
    if (this.ai.state === 'idle' || this.ai.state === 'wander' || this.ai.state === 'return') {
      this.ai.state = 'chase';
      this.ai.timer = 0;
    }
    if (this.hp <= 0) {
      this.hp = 0;
      this.dead = true;
    }
    return this.dead;
  }
}

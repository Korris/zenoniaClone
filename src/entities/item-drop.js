// Ground loot is picked up by pressing attack while standing over it.
export const DROP_KINDS = Object.freeze({
  hpPotion: { spriteKey: 'potionHp', label: 'HP', color: '#ff6b6b' },
  mpPotion: { spriteKey: 'potionMp', label: 'MP', color: '#6ba8ff' },
  upgradeStone: { spriteKey: 'upgradeStone', label: 'Stone', color: '#9fd8ff' },
  coin: { spriteKey: 'coin', label: 'Gold', color: '#ffd84a' },
});

export class ItemDrop {
  /**
   * @param {string} kind 'hpPotion'|'mpPotion'|'coin'|'equipment'
   * @param {number|object} value potion/gold amount, or an item instance when kind === 'equipment'
   */
  constructor(x, y, kind, value) {
    this.x = x;
    this.y = y;
    this.kind = kind;
    this.item = kind === 'equipment' ? value : null;
    this.value = kind === 'equipment' ? null : value;
    this.spriteKey = kind === 'equipment' ? null : DROP_KINDS[kind].spriteKey;
    this.lifeMs = 20000;
    this.age = 0;
    this.dead = false;
    this.pickupMsgCooldown = 0; // throttles the "Inventory full" floating text while standing on it
    // small scatter so multiple drops don't stack exactly
    this.vx = (Math.random() - 0.5) * 40;
    this.vy = (Math.random() - 0.5) * 40;
  }

  get box() {
    return { x: this.x - 4, y: this.y - 6, w: 8, h: 8 };
  }

  /** Vertical bob offset for rendering. */
  get bobOffset() {
    return Math.round(Math.sin(this.age / 200) * 1.5);
  }

  update(dt) {
    this.age += dt;
    this.lifeMs -= dt;
    if (this.pickupMsgCooldown > 0) this.pickupMsgCooldown -= dt;
    if (this.age < 200) {
      this.x += (this.vx * dt) / 1000;
      this.y += (this.vy * dt) / 1000;
    }
    if (this.lifeMs <= 0) this.dead = true;
  }
}

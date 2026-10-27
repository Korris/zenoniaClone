// A visual copy travels to the hero after the actual item has been collected once.
export class LootPickupEffect {
  constructor(drop, player) {
    this.drop = drop;
    this.player = player;
    this.x = drop.x;
    this.y = drop.y;
    this.lifeMs = 480;
    this.maxLifeMs = this.lifeMs;
    this.dead = false;
  }

  get progress() { return Math.min(1, Math.max(0, 1 - this.lifeMs / this.maxLifeMs)); }

  update(dt) {
    this.lifeMs -= dt;
    this.dead = this.lifeMs <= 0;
  }
}

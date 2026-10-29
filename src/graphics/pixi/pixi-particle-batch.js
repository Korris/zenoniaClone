// A fixed-capacity GPU particle batch (PixiJS v8 ParticleContainer + pre-made Particle objects). Each frame the
// caller does begin(), add() per live particle, end(); nothing is allocated. Particles beyond the capacity are dropped.

export class ParticleBatch {
  /**
   * @param {object} PIXI the pixi.js module
   * @param {import('pixi.js').Texture} texture shared by every particle in this batch
   * @param {number} capacity
   * @param {'normal'|'add'} blendMode
   */
  constructor(PIXI, texture, capacity, blendMode = 'normal') {
    this.container = new PIXI.ParticleContainer({
      texture,
      dynamicProperties: { position: true, vertex: true, rotation: true, color: true },
    });
    this.container.blendMode = blendMode;
    this.particles = Array.from({ length: capacity }, () => new PIXI.Particle({ texture, anchorX: 0.5, anchorY: 0.5 }));
    this.count = 0;
  }

  begin() {
    this.count = 0;
  }

  /** Adds one particle for this frame; returns false when the batch is full. Anchor (0..1) is where x/y sits on the sprite. */
  add(x, y, rotation, scaleX, scaleY, tint, alpha, anchorX = 0.5) {
    if (this.count >= this.particles.length) return false;
    const p = this.particles[this.count++];
    p.x = x; p.y = y; p.rotation = rotation; p.scaleX = scaleX; p.scaleY = scaleY;
    p.tint = tint; p.alpha = alpha; p.anchorX = anchorX;
    return true;
  }

  end() {
    const list = this.container.particleChildren;
    list.length = 0;
    for (let i = 0; i < this.count; i++) list.push(this.particles[i]);
    this.container.update();
  }
}

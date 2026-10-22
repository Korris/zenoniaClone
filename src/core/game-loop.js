// Fixed-timestep game loop: logic updates at 60Hz, rendering once per animation frame.
// Keeps physics/combat timing deterministic regardless of monitor refresh rate.
const STEP_MS = 1000 / 60;
const MAX_FRAME_MS = 250; // clamp after tab switch so we don't spiral

export class GameLoop {
  /**
   * @param {{update:(dt:number)=>void, render:()=>void}} handlers dt in ms
   */
  constructor(handlers) {
    this.handlers = handlers;
    this.running = false;
    this.lastTime = 0;
    this.accumulator = 0;
    this.tick = this.tick.bind(this);
  }

  start() {
    if (this.running) return;
    this.running = true;
    this.lastTime = performance.now();
    requestAnimationFrame(this.tick);
  }

  stop() {
    this.running = false;
  }

  tick(now) {
    if (!this.running) return;
    let frameTime = now - this.lastTime;
    if (frameTime > MAX_FRAME_MS) frameTime = MAX_FRAME_MS;
    this.lastTime = now;
    this.accumulator += frameTime;

    try {
      while (this.accumulator >= STEP_MS) {
        this.handlers.update(STEP_MS);
        this.accumulator -= STEP_MS;
      }
      this.handlers.render();
    } catch (err) {
      // Surface the error instead of silently freezing the rAF chain
      console.error('Game loop error:', err);
      this.running = false;
      return;
    }
    requestAnimationFrame(this.tick);
  }
}

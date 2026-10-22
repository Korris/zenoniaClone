// Tiny frame-cycling helper. Pure logic (no DOM) so it can be unit-tested.
export class SpriteAnimation {
  /**
   * @param {any[]} frames array of frame canvases (or any values)
   * @param {number} frameDuration ms per frame
   * @param {boolean} loop whether to wrap around at the end
   */
  constructor(frames, frameDuration = 150, loop = true) {
    this.frames = frames;
    this.frameDuration = frameDuration;
    this.loop = loop;
    this.elapsed = 0;
    this.index = 0;
    this.finished = false;
  }

  reset() {
    this.elapsed = 0;
    this.index = 0;
    this.finished = false;
  }

  /** Advance by dt milliseconds. */
  update(dt) {
    if (this.finished) return;
    this.elapsed += dt;
    while (this.elapsed >= this.frameDuration) {
      this.elapsed -= this.frameDuration;
      this.index++;
      if (this.index >= this.frames.length) {
        if (this.loop) {
          this.index = 0;
        } else {
          this.index = this.frames.length - 1;
          this.finished = true;
          return;
        }
      }
    }
  }

  get current() {
    return this.frames[this.index];
  }
}

// Canvas -> PixiJS Texture cache. The sprite registry already builds every frame as a canvas; each canvas is
// uploaded to the GPU once, on first use. HD sources (pixelsPerUnit > 1, painted backdrops) pick the same
// pre-shrunk half-size level the Canvas2D path uses (smooth-sprite-draw.js) and are sampled bilinearly, so the
// look matches without GPU mipmaps; pixel-art sources are sampled nearest-neighbour.
import { pickSpriteLevel } from '../smooth-sprite-draw.js';

const isSmooth = (source) => (source.pixelsPerUnit || 1) > 1 || (typeof HTMLImageElement !== 'undefined' && source instanceof HTMLImageElement);

export class TextureCache {
  constructor(PIXI) {
    this.PIXI = PIXI;
    this.map = new Map();
  }

  /** Texture for exactly this canvas/image (no level selection). */
  of(source) {
    let tex = this.map.get(source);
    if (!tex) {
      tex = this.PIXI.Texture.from(source, true);
      tex.source.scaleMode = isSmooth(source) ? 'linear' : 'nearest';
      this.map.set(source, tex);
    }
    return tex;
  }

  /**
   * Texture to draw `source` at `destW` x `destH` world units when one world unit is `scale` device pixels.
   * @returns {import('pixi.js').Texture}
   */
  forSize(source, destW, destH, scale) {
    if (!isSmooth(source)) return this.of(source);
    return this.of(pickSpriteLevel(source, Math.abs(destW) * scale, Math.abs(destH) * scale).level);
  }

  /** Frees every GPU texture (class switch / map change). Sources stay valid and re-upload on next use. */
  clear() {
    for (const tex of this.map.values()) tex.destroy(true);
    this.map.clear();
  }
}

// Weapon-trail ribbon as a textured mesh: a strip of quads along the blade tip's arc, UV u running tail -> tip so the
// gradient texture fades the old end out, v across the width for soft edges. Geometry buffers are preallocated and
// rewritten in place every frame. Outline points come from trailPolygon() (shared with the Canvas2D renderer).
import { trailPolygon } from '../combat-feel-renderer.js';
import { COMBAT_FEEL } from '../../data/combat-feel-config.js';

const SEGMENTS = COMBAT_FEEL.trail.segments;
const POINTS = SEGMENTS + 1; // per edge

export class TrailMesh {
  constructor(PIXI, texture, { additive = false } = {}) {
    this.positions = new Float32Array(POINTS * 4);
    const uvs = new Float32Array(POINTS * 4);
    const indices = new Uint32Array(SEGMENTS * 6);
    for (let i = 0; i < POINTS; i++) {
      uvs.set([i / SEGMENTS, 0, i / SEGMENTS, 1], i * 4);
      if (i === SEGMENTS) break;
      const a = i * 2;
      indices.set([a, a + 1, a + 2, a + 1, a + 3, a + 2], i * 6);
    }
    this.geometry = new PIXI.MeshGeometry({ positions: this.positions, uvs, indices });
    this.mesh = new PIXI.Mesh({ geometry: this.geometry, texture });
    if (additive) this.mesh.blendMode = 'add';
    this.mesh.visible = false;
  }

  /** @returns {boolean} false when nothing of the trail is visible (mesh hidden) */
  update(trail, widthScale, tint, alpha) {
    const px = trail.follow.x + trail.offsetX, py = trail.follow.y + trail.offsetY;
    const poly = trailPolygon(trail, px, py, widthScale, SEGMENTS);
    this.mesh.visible = Boolean(poly);
    if (!poly) return false;
    const n = POINTS;
    for (let i = 0; i < n; i++) { // outer point i, then the matching inner point (polygon stores the inner edge reversed)
      const inner = (n + (n - 1 - i)) * 2;
      this.positions[i * 4] = poly[i * 2]; this.positions[i * 4 + 1] = poly[i * 2 + 1];
      this.positions[i * 4 + 2] = poly[inner]; this.positions[i * 4 + 3] = poly[inner + 1];
    }
    const buffer = this.geometry.getBuffer('aPosition');
    buffer.data.set(this.positions);
    buffer.update();
    this.mesh.tint = tint;
    this.mesh.alpha = alpha;
    return true;
  }
}

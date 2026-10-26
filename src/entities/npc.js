// Friendly characters that stand on the map, talk, and hand out quests.
import { NPC_DEFINITIONS } from '../data/npc-definitions.js';

export class Npc {
  constructor(id, x, y) {
    this.id = id;
    this.def = NPC_DEFINITIONS[id];
    if (!this.def) throw new Error(`Unknown NPC: ${id}`);
    this.x = x;
    this.y = y;
    this.dir = this.def.dir || 'down';
    this.animTimer = Math.random() * 1000;
  }

  get name() {
    return this.def.name;
  }

  /** The NPC's resting pose (front-facing unless the definition says otherwise). */
  resetFacing() {
    this.dir = this.def.dir || 'down';
  }

  /**
   * Turn toward a hero who starts talking from the side; a hero above or below is answered facing
   * the front (Zenonia NPCs only ever turn left/right, never show their back).
   */
  faceTowards(px, py) {
    const dx = px - this.x;
    const dy = py - this.y;
    if (Math.abs(dx) > Math.abs(dy)) this.dir = dx > 0 ? 'right' : 'left';
    else this.dir = 'down';
  }
}

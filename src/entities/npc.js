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

  /** Face the hero while they are nearby so conversations feel natural. */
  faceTowards(px, py) {
    const dx = px - this.x;
    const dy = py - this.y;
    if (Math.abs(dx) > Math.abs(dy)) this.dir = dx > 0 ? 'right' : 'left';
    else this.dir = dy > 0 ? 'down' : 'up';
  }
}

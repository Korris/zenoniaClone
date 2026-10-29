// Shared setup for the Assassin mechanics tests: a level-20 assassin in a bare world, dummy foes,
// manual stepping of effects/pulses, and a Math.random override for deterministic damage rolls.
import { Player } from '../../src/entities/player.js';
import { Monster } from '../../src/entities/monster.js';
import { WorldState } from '../../src/game/world-state.js';

export function assassinWorld(skills = {}) {
  const player = new Player('assassin');
  Object.assign(player, { x: 100, y: 100, dir: 'right', level: 20 });
  player.skills = { ...skills };
  player.recomputeStats();
  player.hp = player.maxHp; player.mp = player.maxMp;
  player.passiveMods.critChance = 0; // deterministic damage unless a test wants crits
  const world = new WorldState(player, { shake() {} });
  world.hitRoll = () => 0.5;
  return world;
}
export const foe = (x, y, dir = 'down', hp = 5000) => { const m = new Monster('slime', x, y); m.hp = m.maxHp = hp; m.dir = dir; return m; };
export const step = (world, ms, dt = 20) => { for (let t = 0; t < ms; t += dt) { for (const e of world.effects) e.update(dt); for (const s of world.skillPulses) s.update(dt); } };
export const withRandom = (value, fn) => { const orig = Math.random; Math.random = () => value; try { return fn(); } finally { Math.random = orig; } };


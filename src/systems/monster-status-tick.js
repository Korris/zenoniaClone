// Per-frame upkeep of monster debuffs (see monster-status.js): countdowns, poison damage ticks and the
// Death Mark detonation. Called from world-update.js for every living monster.
import { damageMonster } from './combat-system.js';
import { POISON_TICK_MS } from './monster-status.js';

export const POISON_COLOR = '#8dff6a';
export const MARK_COLOR = '#d68cff';

function detonateMark(world, monster) {
  const { mark } = monster.status;
  delete monster.status.mark; // before damaging, so the blast is not banked into its own mark
  const damage = Math.max(1, Math.round(mark.base + mark.stored));
  world.addEffect({ x: monster.x, y: monster.y - 8, spriteKey: 'hitSpark', lifeMs: 420, scale: 3, tint: '#b57cff', spin: 6 });
  world.camera.shake(2.5, 160);
  world.addText(monster.x, monster.y - monster.def.hitbox.h - 24, 'DEATH MARK', MARK_COLOR, { lifeMs: 700 });
  damageMonster(world, monster, damage, 0, 0, MARK_COLOR, true);
}

export function tickMonsterStatus(world, monster, dt) {
  const s = monster.status;
  if (!s) return;
  for (const key of ['slow', 'blind']) {
    if (!s[key]) continue;
    s[key].timer -= dt;
    if (s[key].timer <= 0) delete s[key];
  }
  if (world.player?.dead) return; // no status damage (poison, Death Mark) once the hero has died
  if (s.poison) {
    s.poison.timer -= dt;
    s.poison.tick -= dt;
    while (s.poison && s.poison.tick <= 0 && !monster.dead) {
      s.poison.tick += POISON_TICK_MS;
      damageMonster(world, monster, s.poison.dmg, 0, 0, POISON_COLOR, false);
    }
    if (s.poison && s.poison.timer <= 0) delete s.poison;
  }
  if (s.mark && !monster.dead) {
    s.mark.timer -= dt;
    if (s.mark.timer <= 0) detonateMark(world, monster);
  }
}

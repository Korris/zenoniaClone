// What to draw for one monster this frame, as plain numbers + source canvases (no canvas/Pixi calls), so the
// PixiJS actor layer and tests share the frame-selection rules of monster-renderer.js (HD walk/attack sheets,
// boss strips, pixel pack, hit flash + rim, squash, launch lift). All rects are world units, top-left origin.
import { HD_MONSTER, HD_MONSTER_ATTACK } from '../asset-atlas-hd-monsters.js';
import { RIM_GROW, BOSS_FRAME_MS, HD_MONSTER_FACING, HD_MONSTER_UNITS, hitSquash, launchHeight, pickFrame, attackFrameIndex } from '../monster-renderer.js';

const rimRect = (r) => ({ x: r.x - (r.w * RIM_GROW) / 2, y: r.y - r.h * RIM_GROW, w: r.w * (1 + RIM_GROW), h: r.h * (1 + RIM_GROW) });

function hdSpec(monster, hd) {
  const scale = monster.def?.renderScale || 1;
  const facing = HD_MONSTER_FACING[monster.dir] || 'down';
  const attackIndex = attackFrameIndex(monster);
  const flashing = monster.hitFlashTimer > 0;
  const attacking = !flashing && attackIndex >= 0 && hd.attack?.[facing];
  const size = (attacking ? HD_MONSTER_ATTACK.frame / HD_MONSTER.pixelsPerUnit : HD_MONSTER_UNITS) * scale;
  const feetY = attacking ? (HD_MONSTER_ATTACK.feetY / HD_MONSTER.pixelsPerUnit) * scale : size;
  const frame = flashing ? hd.hitFlash[facing] : attacking ? hd.attack[facing][attackIndex] : pickFrame(hd.frames[facing], monster);
  const lift = launchHeight(monster);
  const { sx, sy } = hitSquash(monster);
  const w = size * sx, h = size * sy;
  const rect = { x: monster.x - size / 2 - (size * (sx - 1)) / 2, y: monster.y - feetY - lift + size * (1 - sy), w, h };
  return { frame, rect, baseW: size, baseH: size, rim: flashing && hd.hitRim?.[facing] ? { frame: hd.hitRim[facing], rect: rimRect(rect) } : null,
    flipX: false, shadow: { rx: 6 * scale, ry: 2.5 }, markerTop: monster.y - hd.bodyUnits * scale - lift, markers: true };
}

function bossSpec(monster, boss) {
  const moving = monster.ai.state === 'chase' || monster.ai.state === 'return';
  const strip = monster.hitFlashTimer > 0 ? boss.hit : moving ? boss.walk : boss.idle;
  const frame = strip[Math.floor(monster.animTimer / BOSS_FRAME_MS) % strip.length];
  const rect = { x: Math.round(monster.x - boss.width / 2), y: Math.round(monster.y - boss.height + 6), w: boss.width, h: boss.height };
  return { frame, rect, baseW: rect.w, baseH: rect.h, rim: null, flipX: monster.dir === 'right', shadow: { rx: 16, ry: 5 }, markerTop: rect.y, markers: false };
}

function pixelSpec(monster, sprite) {
  const frames = sprite.frames[monster.dir] || sprite.frames.down;
  const flashing = monster.hitFlashTimer > 0;
  const frame = flashing ? sprite.hitFlash[monster.dir] || sprite.hitFlash.down : pickFrame(frames, monster);
  const top = monster.y - sprite.height - launchHeight(monster);
  const { sx, sy } = hitSquash(monster);
  const w = sprite.width * sx, h = sprite.height * sy;
  const rect = { x: monster.x - sprite.width / 2 - (w - sprite.width) / 2, y: top + sprite.height - h, w, h };
  const rimFrame = flashing && sprite.hitRim?.[monster.dir];
  const rim = rimFrame ? { frame: rimFrame, rect: { x: monster.x - sprite.width / 2 - (w * (1 + RIM_GROW) - sprite.width) / 2,
    y: top + sprite.height - h * (1 + RIM_GROW), w: w * (1 + RIM_GROW), h: h * (1 + RIM_GROW) } } : null;
  return { frame, rect, baseW: sprite.width, baseH: sprite.height, rim, flipX: false, shadow: { rx: 5, ry: 2 }, markerTop: top, markers: true };
}

/** @returns {{frame, rect, baseW, baseH, rim, flipX, shadow, markerTop, markers}|null} null when no art exists for the type */
export function monsterSpec(monster, registry) {
  const hd = registry.monstersHd?.[monster.type];
  if (hd) return hdSpec(monster, hd);
  if (monster.isBoss && !monster.def.localBoss) return bossSpec(monster, registry.boss);
  const sprite = registry.monsters[monster.type] || registry.monsters[monster.def.sprite];
  return sprite ? pixelSpec(monster, sprite) : null;
}

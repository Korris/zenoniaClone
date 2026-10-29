// Draws the Assassin's Shadow Clone: the hero's own current frame, translucent, at the clone's position.
import { drawPlayer } from './player-renderer.js';

export function drawShadowClone(ctx, clone, registry) {
  const ghost = Object.create(clone.owner); // inherits every animation field live; only the position differs
  ghost.x = clone.x;
  ghost.y = clone.y;
  ctx.save();
  ctx.globalAlpha = 0.5 * clone.intensity;
  drawPlayer(ctx, ghost, registry);
  ctx.restore();
}

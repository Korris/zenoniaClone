// Entry point: loads the asset images, slices sprites, wires input, starts the fixed-step loop.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from './core/constants.js';
import { GameLoop } from './core/game-loop.js';
import { InputManager } from './core/input-manager.js';
import { loadAllImages } from './graphics/asset-loader.js';
import { buildSpriteRegistry } from './graphics/sprite-registry.js';
import { GameController } from './game/game-controller.js';

function drawLoading(ctx, loaded, total) {
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  ctx.fillStyle = '#0b0b14';
  ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  ctx.fillStyle = '#ffe680';
  ctx.font = 'bold 24px "Courier New", monospace';
  ctx.textAlign = 'center';
  ctx.fillText(`Loading assets ${loaded}/${total}`, CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2);
}

async function boot() {
  const canvas = document.getElementById('game-canvas');
  canvas.width = CANVAS_WIDTH;
  canvas.height = CANVAS_HEIGHT;
  const ctx = canvas.getContext('2d');
  ctx.imageSmoothingEnabled = false;
  drawLoading(ctx, 0, 1);

  const images = await loadAllImages((loaded, total) => drawLoading(ctx, loaded, total));
  const registry = buildSpriteRegistry(images);
  const input = new InputManager(canvas);
  const controller = new GameController(registry, input);

  const loop = new GameLoop({
    update: (dt) => controller.update(dt),
    render: () => controller.render(ctx),
  });
  loop.start();
  window.__zenonia = controller; // handy for debugging in devtools
}

boot().catch((err) => {
  console.error('Failed to start game:', err);
  document.body.innerHTML = `<pre style="color:#f66;padding:20px">Failed to start game:\n${err.stack || err}</pre>`;
});

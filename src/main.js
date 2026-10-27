// Entry point: loads the asset images, slices sprites, wires input, starts the fixed-step loop.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from './core/constants.js';
import { GameLoop } from './core/game-loop.js';
import { InputManager } from './core/input-manager.js';
import { loadAllImages } from './graphics/asset-loader.js';
import { buildSpriteRegistry } from './graphics/sprite-registry.js';
import { GameController } from './game/game-controller.js';
import { unlockCombatAudio } from './audio/combat-audio.js';
import { configureCanvasDisplay, setScreenTransform } from './core/canvas-display.js';

function drawLoading(ctx, loaded, total) {
  setScreenTransform(ctx);
  ctx.fillStyle = '#0b0b14';
  ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  ctx.fillStyle = '#ffe680';
  ctx.font = 'bold 24px "Courier New", monospace';
  ctx.textAlign = 'center';
  ctx.fillText(`Loading assets ${loaded}/${total}`, CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2);
}

async function boot() {
  const canvas = document.getElementById('game-canvas');
  const resize = () => configureCanvasDisplay(canvas, window.innerWidth, window.innerHeight, window.devicePixelRatio);
  resize();
  window.addEventListener('resize', resize);
  const ctx = canvas.getContext('2d');
  ctx.imageSmoothingEnabled = false;
  drawLoading(ctx, 0, 1);

  const images = await loadAllImages((loaded, total) => drawLoading(ctx, loaded, total));
  const registry = buildSpriteRegistry(images);
  const input = new InputManager(canvas);
  // AudioContext must be resumed directly from a real user gesture.
  window.addEventListener('keydown', unlockCombatAudio);
  canvas.addEventListener('pointerdown', unlockCombatAudio);
  const controller = new GameController(registry, input);

  const loop = new GameLoop({
    update: (dt) => controller.update(dt),
    render: () => {
      // Also catch browser zoom / moving between monitors with different pixel density.
      resize();
      controller.render(ctx);
    },
  });
  loop.start();
  window.__zenonia = controller; // handy for debugging in devtools
}

boot().catch((err) => {
  console.error('Failed to start game:', err);
  document.body.innerHTML = `<pre style="color:#f66;padding:20px">Failed to start game:\n${err.stack || err}</pre>`;
});

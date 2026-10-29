// Entry point: loads the asset images, slices sprites, wires input, starts the fixed-step loop.
import { GameLoop } from './core/game-loop.js';
import { InputManager } from './core/input-manager.js';
import { loadSharedImages } from './graphics/asset-loader.js';
import { buildSpriteRegistry } from './graphics/sprite-registry.js';
import { createClassAssetLoader } from './graphics/class-asset-loader.js';
import { GameController } from './game/game-controller.js';
import { unlockCombatAudio } from './audio/combat-audio.js';
import { configureCanvasDisplay } from './core/canvas-display.js';
import { FpsCounter } from './ui/fps-counter.js';
import { drawLoadingScreen } from './ui/loading-screen.js';

async function boot() {
  const canvas = document.getElementById('game-canvas');
  const resize = () => configureCanvasDisplay(canvas, window.innerWidth, window.innerHeight, window.devicePixelRatio);
  resize();
  window.addEventListener('resize', resize);
  // Opaque canvas: every frame is painted edge to edge, so the browser can skip alpha compositing.
  const ctx = canvas.getContext('2d', { alpha: false });
  ctx.imageSmoothingEnabled = false;
  drawLoadingScreen(ctx, 'Loading assets', 0, 1);

  const images = await loadSharedImages((loaded, total) => drawLoadingScreen(ctx, 'Loading assets', loaded, total));
  const registry = buildSpriteRegistry(images);
  const classAssets = createClassAssetLoader(registry, images); // the chosen class's art streams in after the hero pick
  const input = new InputManager(canvas);
  // AudioContext must be resumed directly from a real user gesture.
  window.addEventListener('keydown', unlockCombatAudio);
  canvas.addEventListener('pointerdown', unlockCombatAudio);
  const controller = new GameController(registry, input, classAssets);
  const fps = new FpsCounter();

  const loop = new GameLoop({
    update: (dt) => controller.update(dt),
    render: (alpha) => {
      // Also catch browser zoom / moving between monitors with different pixel density.
      resize();
      controller.render(ctx, alpha);
      fps.tick(performance.now());
      fps.render(ctx);
    },
  });
  loop.start();
  window.__zenonia = controller; // handy for debugging in devtools
}

boot().catch((err) => {
  console.error('Failed to start game:', err);
  document.body.innerHTML = `<pre style="color:#f66;padding:20px">Failed to start game:\n${err.stack || err}</pre>`;
});

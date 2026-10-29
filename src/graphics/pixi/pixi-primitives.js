// Small procedural textures shared by the PixiJS layers (white shapes, tinted per use): shadow ellipse, ring,
// 4-point star, spark capsule and the weapon-trail gradient strip. Created once from tiny 2D canvases.

function paint(width, height, draw) {
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  draw(canvas.getContext('2d'), width, height);
  return canvas;
}

export function createPrimitives(PIXI) {
  const tex = (canvas) => { const t = PIXI.Texture.from(canvas, true); t.source.scaleMode = 'linear'; return t; };
  return {
    white: PIXI.Texture.WHITE,
    ellipse: tex(paint(64, 32, (c, w, h) => { c.fillStyle = '#fff'; c.beginPath(); c.ellipse(w / 2, h / 2, w / 2, h / 2, 0, 0, Math.PI * 2); c.fill(); })),
    ring: tex(paint(128, 128, (c, w) => { c.strokeStyle = '#fff'; c.lineWidth = 5; c.beginPath(); c.arc(w / 2, w / 2, w / 2 - 4, 0, Math.PI * 2); c.stroke(); })),
    star: tex(paint(64, 64, (c, w) => {
      c.fillStyle = '#fff'; c.beginPath();
      for (let i = 0; i < 8; i++) { const a = (i * Math.PI) / 4, r = (i % 2 === 0 ? 1 : 0.24) * (w / 2 - 1); c.lineTo(w / 2 + Math.cos(a) * r, w / 2 + Math.sin(a) * r); }
      c.closePath(); c.fill();
    })),
    streak: tex(paint(32, 8, (c, w, h) => { // capsule along +x, brighter toward the head (right)
      const g = c.createLinearGradient(0, 0, w, 0); g.addColorStop(0, 'rgba(255,255,255,0)'); g.addColorStop(1, '#fff');
      c.fillStyle = g; c.beginPath(); c.roundRect(0, 0, w, h, h / 2); c.fill();
    })),
    trail: tex(paint(64, 16, (c, w, h) => { // alpha ramps up toward the tip (u=1), soft top/bottom edges (v)
      const g = c.createLinearGradient(0, 0, w, 0); g.addColorStop(0, 'rgba(255,255,255,0)'); g.addColorStop(0.35, 'rgba(255,255,255,0.55)'); g.addColorStop(1, '#fff');
      c.fillStyle = g; c.fillRect(0, 0, w, h);
      c.globalCompositeOperation = 'destination-in';
      const e = c.createLinearGradient(0, 0, 0, h); e.addColorStop(0, 'rgba(0,0,0,0.15)'); e.addColorStop(0.5, '#000'); e.addColorStop(1, 'rgba(0,0,0,0.15)');
      c.fillStyle = e; c.fillRect(0, 0, w, h);
    })),
  };
}

/** `#rrggbb` -> 0xrrggbb for Pixi tints. */
export const hexToInt = (css) => parseInt(css.slice(1, 7), 16);

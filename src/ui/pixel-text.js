// Text + bar drawing helpers for UI layers. UI is drawn in screen space (960x640),
// with a clean system sans-serif font shared across menus and dialogs.
export const UI_FONT = '"Segoe UI", Arial, sans-serif';

export function drawText(ctx, text, x, y, opts = {}) {
  const { size = 16, color = '#fff', align = 'left', bold = true, shadow = true, baseline = 'top' } = opts;
  ctx.font = `${bold ? '600' : '400'} ${size}px ${UI_FONT}`;
  ctx.textAlign = align;
  ctx.textBaseline = baseline;
  if (shadow) {
    ctx.fillStyle = 'rgba(0,0,0,0.45)';
    ctx.fillText(text, x, y + 1);
  }
  ctx.fillStyle = color;
  ctx.fillText(text, x, y);
}

/** Horizontal bar with border; `ratio` in [0,1]. */
export function drawBar(ctx, x, y, w, h, ratio, fillColor, backColor = '#2a2a3a') {
  ctx.fillStyle = '#0d0d14';
  ctx.fillRect(x - 2, y - 2, w + 4, h + 4);
  ctx.fillStyle = backColor;
  ctx.fillRect(x, y, w, h);
  ctx.fillStyle = fillColor;
  ctx.fillRect(x, y, Math.round(w * Math.max(0, Math.min(1, ratio))), h);
  // highlight line gives bars a bevelled pixel look
  ctx.fillStyle = 'rgba(255,255,255,0.25)';
  ctx.fillRect(x, y, Math.round(w * Math.max(0, Math.min(1, ratio))), 2);
}

/** Dark translucent panel with a light border. */
export function drawPanel(ctx, x, y, w, h, opts = {}) {
  const { fill = 'rgba(13,23,36,0.96)', border = '#35485e' } = opts;
  ctx.beginPath();
  ctx.roundRect(x + 0.5, y + 0.5, w - 1, h - 1, 10);
  ctx.fillStyle = fill;
  ctx.fill();
  ctx.strokeStyle = border;
  ctx.lineWidth = 1;
  ctx.stroke();
}

/** Full-screen dim overlay. */
export function drawOverlay(ctx, alpha = 0.6) {
  ctx.fillStyle = `rgba(0,0,0,${alpha})`;
  ctx.fillRect(0, 0, ctx.canvas.width, ctx.canvas.height);
}

/** Draws a sprite canvas scaled by an integer factor at screen coords. */
export function drawScaledSprite(ctx, sprite, x, y, scale = 3) {
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(sprite, x, y, sprite.width * scale, sprite.height * scale);
}

import { drawText } from './pixel-text.js';

export const BAG_FONT = '"Bahnschrift SemiCondensed", "Arial Narrow", "Segoe UI", sans-serif';
export function bagText(ctx, text, x, y, opts = {}) {
  drawText(ctx, text, x, y, { font: BAG_FONT, bold: false, ...opts });
}

/** Static blue lighting and a stone dais, behind the equipment preview only. */
export function bagPortraitBackdrop(ctx, x, y, w, h) {
  ctx.save();
  ctx.beginPath(); ctx.rect(x, y, w, h); ctx.clip();
  const glow = ctx.createRadialGradient(x + w / 2, y + h * 0.44, 8, x + w / 2, y + h * 0.44, w * 0.72);
  glow.addColorStop(0, '#98c9f14d'); glow.addColorStop(0.6, '#73a8d11c'); glow.addColorStop(1, '#619ad000');
  ctx.fillStyle = glow; ctx.fillRect(x, y, w, h);
  for (let i = 0; i < 9; i++) {
    const a = i * Math.PI * 2 / 9;
    ctx.beginPath(); ctx.moveTo(x + w / 2, y + h * 0.6);
    ctx.lineTo(x + w / 2 + Math.cos(a) * w, y + h * 0.6 + Math.sin(a) * h);
    ctx.lineTo(x + w / 2 + Math.cos(a + 0.18) * w, y + h * 0.6 + Math.sin(a + 0.18) * h); ctx.closePath();
    ctx.fillStyle = '#a3ceeb02'; ctx.fill();
  }
  ctx.beginPath(); ctx.ellipse(x + w / 2, y + h * 0.85, w * 0.46, h * 0.10, 0, 0, Math.PI * 2);
  ctx.fillStyle = '#0b1b2a66'; ctx.fill(); ctx.clip();
  for (let row = 0; row < 5; row++) for (let col = 0; col < 6; col++) {
    const px = x - 20 + col * 45 + (row % 2) * 22, py = y + h * 0.73 + row * 14;
    ctx.beginPath(); ctx.moveTo(px, py); ctx.lineTo(px + 22, py - 7); ctx.lineTo(px + 43, py); ctx.lineTo(px + 22, py + 7); ctx.closePath();
    ctx.fillStyle = (row + col) % 2 ? '#52718a18' : '#7892ab14'; ctx.fill(); ctx.strokeStyle = '#0c1f3240'; ctx.lineWidth = 1; ctx.stroke();
  }
  ctx.restore();
}

export function bagPanel(ctx, x, y, w, h, fill = null, border = '#536b80') {
  if (!fill) {
    fill = ctx.createLinearGradient(x, y, x + w, y + h);
    fill.addColorStop(0, '#253d57a8'); fill.addColorStop(0.5, '#182d4390'); fill.addColorStop(1, '#102235b8');
  }
  ctx.beginPath(); ctx.roundRect(x + 0.5, y + 0.5, w - 1, h - 1, 5);
  ctx.fillStyle = fill; ctx.fill(); ctx.lineWidth = 1; ctx.strokeStyle = border; ctx.stroke();
  ctx.beginPath(); ctx.roundRect(x + 2.5, y + 2.5, w - 5, h - 5, 4);
  ctx.strokeStyle = '#07111f90'; ctx.stroke();
}

export function bagButton(ctx, text, x, y, w, h, tone = 'steel', selected = false) {
  const colors = tone === 'gold' ? ['#f4d597', '#ad772d'] : tone === 'blue' ? ['#91dfff', '#155aa5'] : ['#607c9b', '#233b57'];
  const fill = ctx.createLinearGradient(0, y, 0, y + h);
  fill.addColorStop(0, colors[0]); fill.addColorStop(0.18, colors[1]); fill.addColorStop(0.8, tone === 'gold' ? '#9b672b' : '#18324f'); fill.addColorStop(1, tone === 'gold' ? '#d5aa5e' : '#456885');
  ctx.save();
  if (selected) { ctx.shadowColor = '#58caff'; ctx.shadowBlur = 9; }
  bagPanel(ctx, x, y, w, h, fill, selected ? '#a1edff' : '#9babb9');
  ctx.restore();
  ctx.strokeStyle = '#ffffff38'; ctx.beginPath(); ctx.moveTo(x + 4, y + 3); ctx.lineTo(x + w - 4, y + 3); ctx.stroke();
  bagText(ctx, text, x + w / 2, y + h / 2, { size: 16, bold: true, baseline: 'middle', align: 'center', color: '#f4f7ff' });
}

export function bagSlot(ctx, x, y, size, { selected = false, color = '#70899d', locked = false } = {}) {
  ctx.save();
  if (selected) { ctx.shadowColor = '#ffd576'; ctx.shadowBlur = 8; }
  const backing = ctx.createLinearGradient(x, y, x + size, y + size);
  backing.addColorStop(0, '#253e58'); backing.addColorStop(0.45, '#0d1d30'); backing.addColorStop(1, '#1e3951');
  bagPanel(ctx, x, y, size, size, backing, selected ? '#ffe7a5' : color);
  ctx.restore();
  ctx.strokeStyle = '#b4c8da55'; ctx.strokeRect(x + 3.5, y + 3.5, size - 7, size - 7);
  ctx.strokeStyle = selected ? '#fff2c4' : '#b6c8d499'; ctx.lineWidth = 1.25;
  ctx.beginPath(); ctx.moveTo(x + 3, y + 12); ctx.lineTo(x + 3, y + 4); ctx.lineTo(x + 13, y + 3);
  ctx.moveTo(x + size - 13, y + size - 3); ctx.lineTo(x + size - 3, y + size - 4); ctx.lineTo(x + size - 3, y + size - 12); ctx.stroke();
  if (locked) {
    ctx.strokeStyle = '#43596d'; ctx.lineWidth = 3;
    ctx.beginPath(); ctx.arc(x + size / 2, y + size * 0.43, size * 0.1, Math.PI, 0); ctx.stroke();
    ctx.fillStyle = '#43596d'; ctx.fillRect(x + size * 0.34, y + size * 0.43, size * 0.32, size * 0.28);
  }
}

export function bagWrappedText(ctx, text, x, y, width, { size = 12, color = '#b4d5e7', lineHeight = 19, maxLines = 4 } = {}) {
  ctx.font = `400 ${size}px ${BAG_FONT}`;
  const words = text.split(/\s+/); let line = '', row = 0;
  for (const word of words) {
    if (line && ctx.measureText(`${line} ${word}`).width > width) {
      bagText(ctx, line, x, y + row * lineHeight, { size, color, bold: false });
      if (++row >= maxLines) return;
      line = word;
    } else line += `${line ? ' ' : ''}${word}`;
  }
  if (line && row < maxLines) bagText(ctx, line, x, y + row * lineHeight, { size, color, bold: false });
}

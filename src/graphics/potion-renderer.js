// Compact, short-necked flask with a clean silhouette at small drop sizes.
export function drawPotion(ctx, kind, x, y, size = 40) {
  const hp = kind === 'hpPotion';
  ctx.save();
  ctx.translate(x, y); ctx.scale(size / 48, size / 48);
  ctx.lineJoin = 'round';
  const body = () => {
    ctx.beginPath(); ctx.moveTo(-6, -12); ctx.lineTo(-6, -7);
    ctx.bezierCurveTo(-15, -4, -16, 1, -16, 7);
    ctx.bezierCurveTo(-16, 17, -10, 20, 0, 20);
    ctx.bezierCurveTo(10, 20, 16, 17, 16, 7);
    ctx.bezierCurveTo(16, 1, 15, -4, 6, -7);
    ctx.lineTo(6, -12); ctx.closePath();
  };
  body();
  const glass = ctx.createLinearGradient(-20, -10, 20, 22);
  glass.addColorStop(0, '#ecfbff'); glass.addColorStop(0.3, '#7cabbc'); glass.addColorStop(0.6, '#183c55'); glass.addColorStop(1, '#c0efff');
  ctx.fillStyle = glass; ctx.fill(); ctx.strokeStyle = '#142737'; ctx.lineWidth = 2; ctx.stroke();
  ctx.save(); body(); ctx.clip();
  const liquid = ctx.createLinearGradient(0, 0, 0, 22);
  liquid.addColorStop(0, hp ? '#ff9ab4' : '#8aeaff');
  liquid.addColorStop(0.4, hp ? '#ed335e' : '#198eea');
  liquid.addColorStop(1, hp ? '#8b143d' : '#153b9b');
  ctx.fillStyle = liquid; ctx.fillRect(-20, 2, 40, 22);
  ctx.beginPath(); ctx.ellipse(0, 2, 14, 2, 0, 0, Math.PI * 2);
  ctx.fillStyle = hp ? '#ffc2cf' : '#b5f3ff'; ctx.fill();
  ctx.strokeStyle = '#ffffff9c'; ctx.lineWidth = 3;
  ctx.beginPath(); ctx.moveTo(-9, -2); ctx.bezierCurveTo(-13, 3, -13, 10, -9, 13); ctx.stroke();
  ctx.restore();
  const cap = ctx.createLinearGradient(0, -20, 0, -12);
  cap.addColorStop(0, '#f6e1af'); cap.addColorStop(1, '#8b6035');
  ctx.fillStyle = cap; ctx.fillRect(-7, -20, 14, 7);
  ctx.strokeStyle = '#413729'; ctx.lineWidth = 1; ctx.strokeRect(-7, -20, 14, 7);
  ctx.fillStyle = '#d6edf5'; ctx.fillRect(-8, -13, 16, 3);
  ctx.restore();
}

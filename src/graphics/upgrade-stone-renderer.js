// Code-drawn chibi Upgrade Stone: faceted cyan/violet gem, clean dark outline, one highlight, no haze.
export function drawUpgradeStone(ctx, x, y, size = 40) {
  ctx.save();
  ctx.translate(x, y); ctx.scale(size / 48, size / 48);
  ctx.lineJoin = 'round';
  const pts = [[0, -20], [14, -8], [11, 14], [0, 20], [-11, 14], [-14, -8]];
  const poly = (list) => { ctx.beginPath(); list.forEach(([px, py], i) => (i ? ctx.lineTo(px, py) : ctx.moveTo(px, py))); ctx.closePath(); };
  poly(pts);
  const body = ctx.createLinearGradient(-14, -20, 14, 20);
  body.addColorStop(0, '#bff4ff'); body.addColorStop(0.5, '#4fb6f2'); body.addColorStop(1, '#6a4fd8');
  ctx.fillStyle = body; ctx.fill();
  // Facets: left light, right dark, bottom deep.
  poly([[0, -20], [-14, -8], [-3, 0]]); ctx.fillStyle = '#d9fbff99'; ctx.fill();
  poly([[0, -20], [14, -8], [3, 0]]); ctx.fillStyle = '#2f7fd899'; ctx.fill();
  poly([[-3, 0], [3, 0], [11, 14], [0, 20], [-11, 14]]); ctx.fillStyle = '#4a37b455'; ctx.fill();
  poly(pts); ctx.strokeStyle = '#1b2547'; ctx.lineWidth = 2; ctx.stroke();
  ctx.strokeStyle = '#ffffffd0'; ctx.lineWidth = 2.5; ctx.lineCap = 'round';
  ctx.beginPath(); ctx.moveTo(-8, -6); ctx.lineTo(-6, 4); ctx.stroke();
  ctx.restore();
}

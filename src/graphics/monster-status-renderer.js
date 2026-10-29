// Small status pips above monsters: green = poisoned, blue = slowed, grey = blinded, purple diamond = Death Mark.
// Drawn in world space after the y-sorted actors (world-renderer.js). Procedural, no art.
const PIPS = [['poison', '#7dff5a'], ['slow', '#6fc4ff'], ['blind', '#b8b8c8']];

export function drawMonsterStatuses(ctx, monsters) {
  for (const m of monsters) {
    const s = m.status;
    if (!s || m.dead) continue;
    const top = m.y - m.def.hitbox.h - 6;
    let x = m.x - 3;
    for (const [key, color] of PIPS) {
      if (!s[key]) continue;
      ctx.fillStyle = color;
      ctx.fillRect(x, top, 2, 2);
      x += 3;
    }
    if (s.mark) {
      const pulse = 1 + 0.25 * Math.sin(s.mark.timer / 80);
      ctx.fillStyle = '#c96bff';
      ctx.beginPath();
      ctx.moveTo(m.x, top - 8 * pulse); ctx.lineTo(m.x + 3 * pulse, top - 4); ctx.lineTo(m.x, top); ctx.lineTo(m.x - 3 * pulse, top - 4);
      ctx.closePath(); ctx.fill();
    }
  }
}

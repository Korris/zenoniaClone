// Draws the Healing Circle on the ground: a pulsing gold/green ring plus rising motes. Procedural, so
// it needs no art. Drawn under the actors (world-renderer.js). Clean outlines, no smoky glow.
// The Assassin's Venom Cloud (kind 'venomZone') reuses it with a purple/green palette.
const PALETTES = {
  healingZone: { fill: '#c8ffb0', ring: '#fff3a0', inner: '#7dff9a' },
  venomZone: { fill: '#9a5cd6', ring: '#c9ff7a', inner: '#7dff5a' },
};
export function drawHealingZone(ctx, zone) {
  const palette = PALETTES[zone.kind] || PALETTES.healingZone;
  const t = zone.age / 1000, a = zone.intensity;
  if (a <= 0) return;
  const ry = zone.radius * 0.72;
  ctx.save();
  ctx.globalAlpha = 0.28 * a;
  ctx.fillStyle = palette.fill;
  ctx.beginPath();
  ctx.ellipse(zone.x, zone.y, zone.radius, ry, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.globalAlpha = 0.9 * a;
  ctx.lineWidth = 0.9;
  ctx.strokeStyle = palette.ring;
  ctx.beginPath();
  ctx.ellipse(zone.x, zone.y, zone.radius, ry, 0, 0, Math.PI * 2);
  ctx.stroke();
  const pulse = 0.55 + 0.1 * Math.sin(t * 6);
  ctx.strokeStyle = palette.inner;
  ctx.beginPath();
  ctx.ellipse(zone.x, zone.y, zone.radius * pulse, ry * pulse, 0, 0, Math.PI * 2);
  ctx.stroke();
  ctx.fillStyle = '#ffffff';
  for (let i = 0; i < 6; i++) {
    const angle = i * 1.047 + t * 0.8, rise = (t * 0.9 + i / 6) % 1;
    ctx.globalAlpha = a * (1 - rise);
    ctx.fillRect(zone.x + Math.cos(angle) * zone.radius * 0.7 - 0.5, zone.y + Math.sin(angle) * ry * 0.7 - rise * 9 - 0.5, 1, 1);
  }
  ctx.restore();
}

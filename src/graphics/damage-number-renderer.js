// Floating damage numbers (screen space, so the font stays sharp): pop-in overshoot, bigger outlined crits.
import { COMBAT_FEEL } from '../data/combat-feel-config.js';

const OUTGOING = ['#fff1bd', '#ffc15a', '#fa5335', '#bf192d'];
const CRIT = ['#fffbd6', '#ffec62', '#ffad25', '#e35716'];
const INCOMING = ['#e5faff', '#65d4ff', '#1685ef', '#1546ad'];

/** Scale at `ageMs`: starts at `from` and settles to 1 with a slight undershoot. */
export function damagePopScale(ageMs, from = COMBAT_FEEL.damageNumber.popFrom, popMs = COMBAT_FEEL.damageNumber.popMs) {
  if (ageMs >= popMs) return 1;
  const t = Math.max(0, ageMs) / popMs;
  return 1 + (from - 1) * (1 - t) * (1 - t) - 0.06 * Math.sin(t * Math.PI);
}

export function drawDamageNumber(ctx, t, sx, sy) {
  const cfg = COMBAT_FEEL.damageNumber;
  const age = t.maxLifeMs - t.lifeMs;
  const crit = t.critical && t.damageStyle !== 'incoming';
  const pop = damagePopScale(age, crit ? cfg.popFrom + 0.2 : cfg.popFrom);
  const size = crit ? cfg.critSize : cfg.size;
  ctx.save();
  ctx.globalAlpha = t.alpha;
  ctx.translate(sx, sy);
  ctx.scale(pop, pop);
  ctx.font = `italic 900 ${size}px "Arial Black", "Segoe UI", sans-serif`;
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.lineJoin = 'round';
  const gradient = ctx.createLinearGradient(0, -size / 2, 0, size / 2);
  const colors = t.damageStyle === 'incoming' ? INCOMING : crit ? CRIT : OUTGOING;
  colors.forEach((color, i) => gradient.addColorStop(i / 3, color));
  if (crit) { // thick red-brown rim so crits pop against any ground
    ctx.strokeStyle = cfg.critOutline;
    ctx.lineWidth = 11;
    ctx.strokeText(t.text, 0, 0);
  }
  // Drop shadow as an offset stroke: same look as shadowBlur 3 at a fraction of the GPU cost.
  ctx.lineWidth = 5;
  ctx.strokeStyle = '#000a';
  ctx.strokeText(t.text, 0, 3);
  ctx.strokeStyle = '#070f1e';
  ctx.strokeText(t.text, 0, 0);
  ctx.fillStyle = gradient;
  ctx.fillText(t.text, 0, 0);
  ctx.restore();
}

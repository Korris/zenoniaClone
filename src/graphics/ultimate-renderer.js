import { cropSprite } from './sprite-cell.js';
import { drawSmoothImage } from './smooth-sprite-draw.js';

export function buildUltimateFrames(image, kind) {
  if (!image) return null;
  if (kind === 'earthBreak') {
    const anchors = { charge: 320, sweep: 192, slam: 340, fissures: 192 };
    const result = {};
    for (const [stage, y] of Object.entries(anchors)) {
      result[stage] = [];
      for (let i = 1; i <= 6; i++) {
        const source = image[`earthBreak_${stage}_${i}`];
        if (!source) return null;
        const frame = cropSprite(source, 0, 0, source.width, source.height, false, 8);
        frame.effectAnchor = { x: 192, y, referenceWidth: 256 };
        result[stage].push(frame);
      }
    }
    return result;
  }
  const names = ['lock', 'launch', 'descend', 'blast', 'smoke'];
  const edges = [0, 320, 470, 700, 935, 1145];
  const scaleY = image.height / edges.at(-1);
  return Object.fromEntries(names.map((key, row) => {
    const band = cropSprite(image, 0, Math.round(edges[row] * scaleY), image.width, Math.round((edges[row + 1] - edges[row]) * scaleY));
    const pixels = band.getContext('2d').getImageData(0, 0, band.width, band.height).data;
    const profile = new Uint16Array(band.width);
    for (let y = 0; y < band.height; y++) for (let x = 0; x < band.width; x++) if (pixels[(y * band.width + x) * 4 + 3] > 180) profile[x]++;
    const cuts = [0], step = band.width / 6;
    for (let i = 1; i < 6; i++) {
      const expected = Math.round(i * step); let best = expected, score = Infinity;
      for (let x = Math.floor(expected - step * 0.18); x <= expected + step * 0.18; x++) {
        const value = profile[x - 1] + profile[x] + profile[x + 1];
        if (value < score || value === score && Math.abs(x - expected) < Math.abs(best - expected)) { best = x; score = value; }
      }
      cuts.push(best);
    }
    cuts.push(band.width);
    return [key, Array.from({ length: 6 }, (_, col) => cropSprite(band, cuts[col], 0, cuts[col + 1] - cuts[col], band.height, false, 8))];
  }));
}
function stamp(ctx, frames, t, x, y, size, anchor = 0.8, alpha = 1, rotation = 0) {
  if (!frames || t < 0 || t >= 1) return;
  ctx.save(); ctx.globalAlpha = alpha; ctx.translate(x, y); ctx.rotate(rotation);
  const frame = frames[Math.min(5, Math.floor(t * 6))], height = size * frame.height / frame.width;
  if (frame.effectAnchor) {
    const a = frame.effectAnchor, scale = size / a.referenceWidth;
    drawSmoothImage(ctx, frame, -a.x * scale, -a.y * scale, frame.width * scale, frame.height * scale);
  } else drawSmoothImage(ctx, frame, -size / 2, -height * anchor, size, height);
  ctx.restore();
}
const angleFor = dir => ({ right: 0, down: Math.PI / 2, left: Math.PI, up: -Math.PI / 2 }[dir]);
const EARTH_BREAK_VISUAL_SCALE = 0.7;

export function drawUltimate(ctx, e, frames, ground = false) {
  if (!frames) return;
  ctx.save();
  if (e.kind === 'earthBreak') {
    const visualRadius = e.radius * EARTH_BREAK_VISUAL_SCALE;
    if (ground) {
      stamp(ctx, frames.charge, e.age / 500, e.casterX, e.casterY, 48 * EARTH_BREAK_VISUAL_SCALE);
      stamp(ctx, frames.fissures, (e.age - 1450) / 1000, e.x, e.y, visualRadius * 2.4, 0.5);
    } else {
      stamp(ctx, frames.sweep, (e.age - 450) / 500, (e.x + e.casterX) / 2, (e.y + e.casterY) / 2 - 6, visualRadius * 1.8, 0.5, 1, angleFor(e.dir));
      // Linger on the descending sword; finish the impact frames quickly.
      const slamProgress = e.age < 1450
        ? (e.age - 850) / 1200
        : 0.5 + (e.age - 1450) / 900;
      stamp(ctx, frames.slam, slamProgress, e.x, e.y, visualRadius * 1.65, 0.9);
    }
  } else {
    if (ground) {
      stamp(ctx, frames.lock, e.age / 1400, e.x, e.y, 65, 0.62, Math.min(1, (1400 - e.age) / 300));
    } else for (const r of e.rockets) {
      const launch = (e.age - r.launchMs) / 600;
      if (launch >= 0 && launch < 1) {
        const x = e.casterX + (r.x - e.casterX) * launch;
        const y = e.casterY - 9 + (r.y - e.casterY - 52) * launch;
        const a = Math.atan2(r.y - e.casterY - 52, r.x - e.casterX);
        stamp(ctx, frames.launch, launch * 0.7, x, y, 30, 0.5, 1, a);
      }
      const descending = (e.age - r.impactMs + 520) / 520;
      if (descending >= 0 && descending < 1) stamp(ctx, frames.descend, descending * 0.8,
        r.x - (1 - descending) * 25, r.y - (1 - descending) * 85, 40, 0.85);
      stamp(ctx, frames.blast, (e.age - r.impactMs) / 500, r.x, r.y, e.radius * 2.6);
      const smoke = (e.age - r.impactMs - 280) / 570;
      stamp(ctx, frames.smoke, smoke, r.x, r.y - 4, e.radius * 2.6, 0.55, Math.max(0, 0.85 * (1 - smoke)));
    }
  }
  ctx.restore();
}

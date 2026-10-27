// Generated artwork replaces the pixel pack by registry key; gameplay keeps its existing IDs.
import { cropSprite } from './sprite-cell.js';
import { SKILL_ORDER } from '../data/skill-definitions.js';
import { FX_STRIPS } from './asset-atlas.js';

export const PAINTED_CLASS_IDS = ['warrior', 'wizard', 'mechanic'];
export const PAINTED_FX_ORDER = ['slash', 'arc', 'dashTrail', 'hitSpark', 'circular', 'flame', 'spark'];
// The generated atlas has uneven row spacing; use its transparent gutters, not height / 7.
const FX_ROW_EDGES = [0, 170, 380, 520, 690, 850, 995, 1168];

function cell(image, col, row, cols, rows, trim = false) {
  const w = image.width / cols, h = image.height / rows;
  const frame = cropSprite(image, Math.round(col * w), Math.round(row * h), Math.floor(w), Math.floor(h));
  if (!trim) return frame;
  const pixels = frame.getContext('2d').getImageData(0, 0, frame.width, frame.height).data;
  let left = frame.width, right = -1, top = frame.height, bottom = -1;
  for (let y = 0; y < frame.height; y++) for (let x = 0; x < frame.width; x++) {
    if (pixels[(y * frame.width + x) * 4 + 3] < 80) continue;
    left = Math.min(left, x); right = Math.max(right, x); top = Math.min(top, y); bottom = Math.max(bottom, y);
  }
  return right < left ? frame : cropSprite(frame, left, top, right - left + 1, bottom - top + 1);
}
function bands(counts, threshold, count) {
  const runs = []; let start = -1;
  for (let i = 0; i <= counts.length; i++) {
    if (i < counts.length && counts[i] > threshold) { if (start < 0) start = i; }
    else if (start >= 0) { runs.push({ start, end: i }); start = -1; }
  }
  return runs.sort((a, b) => (b.end - b.start) - (a.end - a.start)).slice(0, count).sort((a, b) => a.start - b.start);
}
function gutterEdges(profile, count) {
  const edges = [0], step = profile.length / count;
  for (let i = 1; i < count; i++) {
    const expected = Math.round(i * step), radius = Math.floor(step * 0.2);
    let best = expected, score = Infinity;
    for (let x = Math.max(1, expected - radius); x < Math.min(profile.length - 1, expected + radius); x++) {
      const value = profile[x - 1] + profile[x] + profile[x + 1];
      if (value < score || value === score && Math.abs(x - expected) < Math.abs(best - expected)) { best = x; score = value; }
    }
    edges.push(best);
  }
  edges.push(profile.length);
  return edges;
}
function iconCells(image) {
  const sheet = cropSprite(image, 0, 0, image.width, image.height);
  const data = sheet.getContext('2d').getImageData(0, 0, sheet.width, sheet.height).data;
  const rowInk = new Uint32Array(sheet.height);
  for (let y = 0; y < sheet.height; y++) for (let x = 0; x < sheet.width; x++) if (data[(y * sheet.width + x) * 4 + 3] > 80) rowInk[y]++;
  const rows = bands(rowInk, sheet.width * 0.2, 3);
  if (rows.length !== 3) return Array.from({ length: 15 }, (_, i) => cell(image, i % 5, Math.floor(i / 5), 5, 3, true));
  return rows.flatMap((band, r) => {
    // Use the dense band only to locate its row; include thin flame tips, stars and speed lines.
    const row = { start: r ? Math.floor((rows[r - 1].end + band.start) / 2) : 0,
      end: r + 1 < rows.length ? Math.floor((band.end + rows[r + 1].start) / 2) : sheet.height };
    const colInk = new Uint32Array(sheet.width);
    for (let x = 0; x < sheet.width; x++) for (let y = row.start; y < row.end; y++) if (data[(y * sheet.width + x) * 4 + 3] > 80) colInk[x]++;
    const edges = gutterEdges(colInk, 5);
    return Array.from({ length: 5 }, (_, c) => {
      const left = edges[c], right = edges[c + 1];
      const frame = cropSprite(sheet, left, row.start, right - left, row.end - row.start);
      return cell(frame, 0, 0, 1, 1, true);
    });
  });
}
export function buildPaintedSkillIcons(images) {
  const icons = {};
  PAINTED_CLASS_IDS.forEach((classId, group) => {
    const image = images[`paintedSkills${classId}`];
    if (!image) return;
    const frames = iconCells(image);
    SKILL_ORDER.slice(group * 15, group * 15 + 15).forEach((id, i) => {
      icons[id] = frames[i];
    });
  });
  if (images.ultimateIcons) ['bladeStorm', 'carpetBomb'].forEach((id, col) => {
    icons[id] = cell(images.ultimateIcons, col, 0, 2, 1, true);
  });
  return icons;
}
export function buildPaintedCombatEffects(images) {
  const effects = {};
  if (images.paintedCombatFx) PAINTED_FX_ORDER.forEach((name, row) => {
    const { w, h } = FX_STRIPS[name];
    effects[name] = Array.from({ length: 4 }, (_, col) => {
      const image = images.paintedCombatFx, rowScale = image.height / 1168;
      const raw = cropSprite(image, Math.round(col * image.width / 4), Math.round(FX_ROW_EDGES[row] * rowScale),
        Math.floor(image.width / 4), Math.round((FX_ROW_EDGES[row + 1] - FX_ROW_EDGES[row]) * rowScale));
      const canvas = document.createElement('canvas'); canvas.width = w * 8; canvas.height = h * 8;
      const ctx = canvas.getContext('2d'); ctx.imageSmoothingEnabled = true; ctx.imageSmoothingQuality = 'high';
      const scale = Math.min(canvas.width / raw.width, canvas.height / raw.height);
      ctx.drawImage(raw, (canvas.width - raw.width * scale) / 2, (canvas.height - raw.height * scale) / 2, raw.width * scale, raw.height * scale);
      canvas.pixelsPerUnit = 8;
      return canvas;
    });
  });
  if (images.paintedUtility) {
    effects.markers = {};
    ['available', 'turnIn', 'talk', 'particle'].forEach((name, col) => {
      const sprite = cell(images.paintedUtility, col, 0, 4, 1, true);
      // Fit the existing ten-unit marker height; particles use an explicit world size.
      sprite.pixelsPerUnit = sprite.height / 10;
      if (name === 'particle') effects.hitParticle = sprite;
      else effects.markers[name] = sprite;
    });
  }
  if (images.warriorSlashFx) ['slash', 'arc'].forEach((name, row) => {
    const { w, h } = FX_STRIPS[name];
    effects[name] = Array.from({ length: 4 }, (_, col) => {
      const raw = cell(images.warriorSlashFx, col, row, 4, 2);
      const frame = document.createElement('canvas'); frame.width = w * 8; frame.height = h * 8;
      const ctx = frame.getContext('2d'); ctx.imageSmoothingEnabled = true; ctx.imageSmoothingQuality = 'high';
      ctx.drawImage(raw, 0, 0, frame.width, frame.height); frame.pixelsPerUnit = 8;
      return frame;
    });
  });
  return effects;
}

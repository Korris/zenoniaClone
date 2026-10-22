// Zenonia-style HUD: gold-framed bottom bar with portrait, LV, HP/MP/EXP bars and skill slots;
// minimap top-left; quest tracker + combo counter top-right; boss bar top-center.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { drawText, drawBar, drawPanel, drawScaledSprite } from './pixel-text.js';
import { expToNext } from '../systems/leveling-system.js';
import { SKILL_DEFINITIONS, HOTBAR_SLOT_KEYS, getSkillValue } from '../data/skill-definitions.js';
import { getSkillLevel } from '../systems/skill-system.js';
import { drawMinimap } from './minimap-renderer.js';
import { drawQuestTracker } from './quest-tracker-hud.js';

const BAR_H = 104;
const BAR_Y = CANVAS_HEIGHT - BAR_H;
const GOLD = '#d9b45a';
const GOLD_DARK = '#8a6a20';
const SLOT_SIZE = 56;

/** Ornate frame: dark fill, double gold border, corner studs. */
function drawGoldFrame(ctx, x, y, w, h) {
  ctx.fillStyle = 'rgba(14,12,30,0.92)';
  ctx.fillRect(x, y, w, h);
  ctx.lineWidth = 3;
  ctx.strokeStyle = GOLD_DARK;
  ctx.strokeRect(x + 1.5, y + 1.5, w - 3, h - 3);
  ctx.lineWidth = 2;
  ctx.strokeStyle = GOLD;
  ctx.strokeRect(x + 5, y + 5, w - 10, h - 10);
  ctx.fillStyle = GOLD;
  for (const [cx, cy] of [[x + 6, y + 6], [x + w - 6, y + 6], [x + 6, y + h - 6], [x + w - 6, y + h - 6]]) {
    ctx.beginPath();
    ctx.arc(cx, cy, 4, 0, Math.PI * 2);
    ctx.fill();
  }
}

function drawPortrait(ctx, player, registry) {
  const cx = 70;
  const cy = BAR_Y + BAR_H / 2;
  ctx.fillStyle = GOLD_DARK;
  ctx.beginPath(); ctx.arc(cx, cy, 44, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = GOLD;
  ctx.beginPath(); ctx.arc(cx, cy, 40, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = '#2a2448';
  ctx.beginPath(); ctx.arc(cx, cy, 36, 0, Math.PI * 2); ctx.fill();
  ctx.save();
  ctx.beginPath(); ctx.arc(cx, cy, 36, 0, Math.PI * 2); ctx.clip();
  const face = registry.faces.hero;
  ctx.imageSmoothingEnabled = Boolean(registry.faces.heroIsHd); // crisp upscale for the HD faceset only
  ctx.drawImage(face, cx - face.width, cy - face.height, face.width * 2, face.height * 2);
  ctx.restore();
  drawPanel(ctx, cx - 34, cy + 24, 68, 22, { fill: '#1b1630', border: GOLD });
  drawText(ctx, `LV.${String(player.level).padStart(2, '0')}`, cx, cy + 27, { size: 14, align: 'center', color: '#ffe680' });
}

function drawStatBars(ctx, player) {
  const x = 130;
  const w = 300;
  const rows = [
    ['HP', BAR_Y + 18, 14, player.hp / player.maxHp, '#e03c3c', `${Math.ceil(player.hp)}/${player.maxHp}`],
    ['MP', BAR_Y + 44, 12, player.mp / player.maxMp, '#3b7dff', `${Math.floor(player.mp)}/${player.maxMp}`],
    ['EXP', BAR_Y + 68, 8, player.exp / expToNext(player.level), '#f2c14e', `${player.exp}/${expToNext(player.level)}`],
  ];
  for (const [label, y, h, ratio, color, text] of rows) {
    drawText(ctx, label, x, y - 2, { size: 13, color: '#ffe680' });
    drawBar(ctx, x + 40, y, w, h, ratio, color);
    drawText(ctx, text, x + 40 + w / 2, y - 1 + (h - 10) / 2, { size: 10, align: 'center' });
  }
  drawText(ctx, `${player.gold} G`, x + 40 + w, BAR_Y + 84, { size: 13, align: 'right', color: '#ffd84a' });
}

/** Draws the 3 hotbar slots from player.hotbar; empty slots just show the hotkey, dimmed. */
function drawSkillSlots(ctx, player, icons) {
  let x = 500;
  const y = BAR_Y + 20;
  player.hotbar.forEach((skillId, slot) => {
    const def = skillId ? SKILL_DEFINITIONS[skillId] : null;
    const level = def ? getSkillLevel(player, skillId) : 0;
    drawPanel(ctx, x, y, SLOT_SIZE, SLOT_SIZE, { fill: '#1b1630', border: def ? GOLD : '#555' });
    if (def && icons[skillId]) {
      ctx.globalAlpha = level > 0 ? 1 : 0.3;
      drawScaledSprite(ctx, icons[skillId], x + 4, y + 4, 2);
      ctx.globalAlpha = 1;
    }
    const cd = def ? player.cooldowns[skillId] || 0 : 0;
    if (def && cd > 0) {
      const total = getSkillValue(skillId, 'cooldownMs', level);
      ctx.fillStyle = 'rgba(0,0,0,0.65)';
      ctx.fillRect(x, y, SLOT_SIZE, SLOT_SIZE * (cd / total));
      drawText(ctx, (cd / 1000).toFixed(1), x + SLOT_SIZE / 2, y + 20, { size: 13, align: 'center' });
    }
    drawPanel(ctx, x + SLOT_SIZE - 20, y - 6, 22, 16, { fill: '#1b1630', border: GOLD });
    drawText(ctx, HOTBAR_SLOT_KEYS[slot], x + SLOT_SIZE - 9, y - 5, { size: 11, align: 'center', color: '#ffe680' });
    if (def) {
      const cost = getSkillValue(skillId, 'mpCost', level);
      drawText(ctx, `${cost}MP`, x + SLOT_SIZE / 2, y + SLOT_SIZE + 2, { size: 10, align: 'center', color: player.mp >= cost ? '#8fc3ff' : '#ff7070' });
    }
    x += SLOT_SIZE + 14;
  });
  const hint = player.skillPoints > 0 ? `TAB Skills (+${player.skillPoints})` : 'TAB Skills';
  const blink = player.skillPoints > 0 && Math.floor(performance.now() / 400) % 2 === 0;
  drawPanel(ctx, x + 6, y + 8, 130, 40, { fill: '#1b1630', border: blink ? '#ffe680' : GOLD });
  drawText(ctx, hint, x + 71, y + 14, { size: 11, align: 'center', color: blink ? '#ffe680' : '#cfcfe0' });
  drawText(ctx, 'Q Quests   B Bag', x + 71, y + 28, { size: 11, align: 'center', color: '#cfcfe0' });
  if (player.hasUnseenItem && Math.floor(performance.now() / 300) % 2 === 0) {
    drawText(ctx, 'NEW', x + 130, y + 2, { size: 10, align: 'center', color: '#ff6b6b' });
  }
}

function drawComboCounter(ctx, player) {
  const count = player.combo.displayCount;
  if (count <= 0) return;
  const label = count >= 3 ? `${count} HIT FINISH!` : `${count} HIT COMBO`;
  drawText(ctx, label, CANVAS_WIDTH / 2, 70, { size: 20 + count * 2, align: 'center', color: count >= 3 ? '#ffb13b' : '#ffe680' });
}

function drawBossBar(ctx, boss) {
  const w = 420;
  const x = (CANVAS_WIDTH - w) / 2;
  drawText(ctx, boss.def.name, CANVAS_WIDTH / 2, 14, { size: 16, align: 'center', color: '#ff7a7a' });
  drawBar(ctx, x, 36, w, 12, boss.hp / boss.maxHp, '#c43a3a', '#3a1a1a');
}

/**
 * @param {CanvasRenderingContext2D} ctx screen-space context
 * @param {import('../game/world-state.js').WorldState} world
 * @param {object} registry sprite registry
 * @param {{questState:object, npcMarkers:object}} view
 */
export function drawHud(ctx, world, registry, view) {
  drawGoldFrame(ctx, 0, BAR_Y, CANVAS_WIDTH, BAR_H);
  drawPortrait(ctx, world.player, registry);
  drawStatBars(ctx, world.player);
  drawSkillSlots(ctx, world.player, registry.icons);
  drawMinimap(ctx, world, view.npcMarkers);
  drawQuestTracker(ctx, view.questState, 20);
  drawComboCounter(ctx, world.player);
  const boss = world.livingBoss;
  if (boss) drawBossBar(ctx, boss);
}

export const HUD_BAR_HEIGHT = BAR_H;

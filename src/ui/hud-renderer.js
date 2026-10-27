// Floating HUD: status top-left, circular minimap top-right, combat skills bottom-right.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { expToNext } from '../systems/leveling-system.js';
import { SKILL_DEFINITIONS, HOTBAR_SLOT_KEYS, getSkillValue } from '../data/skill-definitions.js';
import { getSkillLevel } from '../systems/skill-system.js';
import { drawMinimap } from './minimap-renderer.js';
import { drawQuestTracker } from './quest-tracker-hud.js';

const BAR_H = 0;
const GOLD = '#c3d8e3';
const GOLD_DARK = '#526b7c';

function drawPortrait(ctx, player, registry) {
  disc(ctx, 58, 58, 44);
  ctx.save();
  ctx.beginPath(); ctx.arc(58, 58, 36, 0, Math.PI * 2); ctx.clip();
  const face = registry.faces.heroByClass?.[player.classId] || registry.faces.hero;
  ctx.imageSmoothingEnabled = Boolean(registry.faces.heroIsHd);
  ctx.drawImage(face, 22, 22, 72, 72);
  ctx.restore();
}

function label(ctx, value, x, y, size = 12, color = '#ecf5ff', align = 'left') {
  ctx.font = `600 ${size}px "Segoe UI", Arial, sans-serif`;
  ctx.textAlign = align; ctx.textBaseline = 'middle';
  ctx.fillStyle = '#07111e'; ctx.fillText(value, x + 1, y + 1);
  ctx.fillStyle = color; ctx.fillText(value, x, y);
}
function disc(ctx, x, y, r, accent = '#9abaca') {
  const metal = ctx.createLinearGradient(0, y-r, 0, y+r);
  metal.addColorStop(0, '#eef5f7'); metal.addColorStop(0.4, '#728b9b');
  metal.addColorStop(0.65, '#293944'); metal.addColorStop(1, '#bdd0d8');
  ctx.beginPath(); ctx.arc(x, y, r, 0, Math.PI*2); ctx.fillStyle = metal; ctx.fill();
  ctx.beginPath(); ctx.arc(x, y, r-3, 0, Math.PI*2); ctx.fillStyle = '#10202fee'; ctx.fill();
  ctx.beginPath(); ctx.arc(x, y, r-7, 0, Math.PI*2); ctx.strokeStyle = accent; ctx.lineWidth = 1; ctx.stroke();
}
function drawStatBars(ctx, player) {
  const shade = ctx.createLinearGradient(90, 0, 290, 0);
  shade.addColorStop(0, '#081522ee'); shade.addColorStop(0.85, '#0d1b2cbb'); shade.addColorStop(1, '#0d1b2c00');
  ctx.fillStyle = shade; ctx.fillRect(90, 15, 200, 89);
  label(ctx, `LV. ${player.level}`, 112, 26, 15);
  const next = expToNext(player.level);
  const rows = [
    ['HP', 42, 17, player.hp/player.maxHp, '#fa7c87', '#b72b43', `${Math.ceil(player.hp)} / ${player.maxHp}`],
    ['MP', 66, 12, player.mp/player.maxMp, '#7cddff', '#2869bd', `${Math.floor(player.mp)} / ${player.maxMp}`],
    ['EXP', 85, 6, player.exp/next, '#ffe7a5', '#b88a38', ''],
  ];
  for (const [name, y, h, ratio, light, dark, value] of rows) {
    const x=112, w=158, filled=w*Math.max(0, Math.min(1, ratio));
    ctx.fillStyle='#050c16'; ctx.fillRect(x-2, y-2, w+4, h+4);
    ctx.fillStyle='#253440'; ctx.fillRect(x, y, w, h);
    const gradient=ctx.createLinearGradient(0,y,0,y+h);
    gradient.addColorStop(0,light); gradient.addColorStop(1,dark);
    ctx.fillStyle=gradient; ctx.fillRect(x,y,filled,h);
    ctx.fillStyle='#ffffff66'; ctx.fillRect(x,y,filled,1);
    if(value) { label(ctx,name,x+6,y+h/2,10); label(ctx,value,x+w-6,y+h/2,10,'#fff','right'); }
  }
}
function drawSkillSlots(ctx, player, icons) {
  const ax=CANVAS_WIDTH-76, ay=CANVAS_HEIGHT-80;
  disc(ctx,ax,ay,60,'#e7ca8c');
  ctx.save(); ctx.translate(ax,ay-6); ctx.rotate(Math.PI/4);
  ctx.fillStyle='#e2f5ff'; ctx.beginPath(); ctx.moveTo(0,-30); ctx.lineTo(7,-17); ctx.lineTo(5,14); ctx.lineTo(-5,14); ctx.lineTo(-7,-17); ctx.closePath(); ctx.fill();
  ctx.fillStyle='#ebce96'; ctx.fillRect(-16,14,32,5); ctx.fillRect(-4,19,8,15); ctx.restore();
  label(ctx,'J / SPACE',ax,ay+39,10,'#ecdbb8','center');
  const positions=[[ax-120,ay+8],[ax-99,ay-78],[ax-13,ay-120]];
  player.hotbar.forEach((id,slot)=>{
    const [x,y]=positions[slot];
    const def=id ? SKILL_DEFINITIONS[id] : null;
    const level=def ? getSkillLevel(player,id) : 0;
    const cost=def ? getSkillValue(id,'mpCost',level) : 0;
    const cd=def ? player.cooldowns[id] || 0 : 0;
    const ready=level>0 && player.mp>=cost && cd<=0;
    disc(ctx,x,y,36,ready ? '#9be4ff' : '#657c8b');
    ctx.save(); ctx.beginPath(); ctx.arc(x,y,28,0,Math.PI*2); ctx.clip();
    if(def && icons[id]) {
      ctx.globalAlpha=ready ? 1 : 0.4; ctx.imageSmoothingEnabled=true;
      ctx.drawImage(icons[id],x-27,y-27,54,54); ctx.globalAlpha=1;
    } else label(ctx,'+',x,y-2,26,'#6e8796','center');
    if(cd>0) {
      ctx.fillStyle='#030c19bb'; ctx.beginPath(); ctx.moveTo(x,y);
      ctx.arc(x,y,29,-Math.PI/2,-Math.PI/2+Math.PI*2*Math.min(1,cd/getSkillValue(id,'cooldownMs',level)));
      ctx.closePath(); ctx.fill(); label(ctx,(cd/1000).toFixed(1),x,y,17,'#fff','center');
    }
    ctx.restore();
    ctx.beginPath(); ctx.arc(x+25,y+24,12,0,Math.PI*2); ctx.fillStyle='#172a3a'; ctx.fill();
    ctx.strokeStyle='#a5bdc9'; ctx.lineWidth=1; ctx.stroke();
    label(ctx,HOTBAR_SLOT_KEYS[slot],x+25,y+24,11,'#fff','center');
    if(def) label(ctx,`${cost} MP`,x,y+46,10,player.mp>=cost ? '#ace0fa' : '#ff8997','center');
  });
  label(ctx,player.skillPoints>0 ? `TAB  Skills  +${player.skillPoints}` : 'TAB  Skills',CANVAS_WIDTH-22,CANVAS_HEIGHT-268,12,'#f4db9b','right');
  label(ctx,`[1] HP x${player.potions?.hpPotion ?? 0}    [2] MP x${player.potions?.mpPotion ?? 0}`,20,CANVAS_HEIGHT-44,12,'#bde3ff');
  label(ctx,player.hasUnseenItem ? 'B  Bag + NEW     Q  Quests' : 'B  Bag     Q  Quests',20,CANVAS_HEIGHT-22,12);
}

function drawTargetBar(ctx, monster) {
  const w = 240, x = (CANVAS_WIDTH - w) / 2, y = 14;
  const shade = ctx.createLinearGradient(x, 0, x + w, 0);
  shade.addColorStop(0, '#09111bd9'); shade.addColorStop(0.8, '#101822eb'); shade.addColorStop(1, '#10182299');
  ctx.fillStyle = shade; ctx.fillRect(x, y, w, 62);
  ctx.fillStyle = '#a4b3be'; ctx.fillRect(x, y, w, 1);
  ctx.fillStyle = '#566572'; ctx.fillRect(x, y + 61, w, 1);
  ctx.font = '600 16px "Segoe UI", Arial, sans-serif';
  const nameWidth = ctx.measureText(monster.def.name).width;
  const markerX = x + (w - nameWidth - 17) / 2;
  label(ctx, monster.def.name, markerX + 17, y + 18, 16);
  ctx.fillStyle = monster.isBoss ? '#b56cf1' : '#f4af54';
  ctx.fillRect(markerX, y + 12, 12, 12);
  ctx.strokeStyle = '#e2e9f2'; ctx.lineWidth = 1; ctx.strokeRect(markerX, y + 12, 12, 12);
  const levelText = `LV. ${monster.level}`;
  ctx.font = '600 12px "Segoe UI", Arial, sans-serif';
  const levelWidth = ctx.measureText(levelText).width;
  const padding = 14, gap = 8;
  const bx = x + padding + levelWidth + gap + 2, by = y + 37, bh = 12;
  const bw = x + w - padding - 2 - bx;
  label(ctx, levelText, x + padding, by + bh / 2, 12, '#e4eaf1');
  ctx.fillStyle = '#03070d'; ctx.fillRect(bx - 2, by - 2, bw + 4, bh + 4);
  ctx.fillStyle = '#42151c'; ctx.fillRect(bx, by, bw, bh);
  const red = ctx.createLinearGradient(0, by, 0, by + bh);
  red.addColorStop(0, '#ff747d'); red.addColorStop(0.45, '#e8384b'); red.addColorStop(1, '#9f152e');
  ctx.fillStyle = red; ctx.fillRect(bx, by, bw * Math.max(0, Math.min(1, monster.hp / monster.maxHp)), bh);
  label(ctx, `${Math.ceil(monster.hp)} / ${monster.maxHp}`, bx + bw / 2, by + bh / 2, 9, '#fff', 'center');
}

/**
 * @param {CanvasRenderingContext2D} ctx screen-space context
 * @param {import('../game/world-state.js').WorldState} world
 * @param {object} registry sprite registry
 * @param {{questState:object, npcMarkers:object}} view
 */
export function drawHud(ctx, world, registry, view) {
  ctx.save();
  drawStatBars(ctx, world.player);
  drawPortrait(ctx, world.player, registry);
  drawSkillSlots(ctx, world.player, registry.icons);
  drawMinimap(ctx, world, view.npcMarkers);
  drawQuestTracker(ctx, view.questState, 180);
  const target = world.combatTarget;
  const visibleTarget = target && !target.dead && world.monsters.includes(target) ? target : world.livingBoss;
  if (visibleTarget) drawTargetBar(ctx, visibleTarget);
  ctx.restore();
}

export const HUD_BAR_HEIGHT = BAR_H;

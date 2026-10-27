import { drawText, drawPanel, drawOverlay } from './pixel-text.js';
import { getClassDefinition } from '../data/class-definitions.js';

export const MENU_TABS = ['Inventory', 'Skills', 'Equipment'];
export function menuHit(x, y) {
  if (x >= 856 && x <= 890 && y >= 54 && y <= 88) return 'close';
  if (y < 100 || y > 136) return null;
  const i = MENU_TABS.findIndex((_, i) => x >= 74 + i * 164 && x <= 230 + i * 164);
  return i < 0 ? null : MENU_TABS[i];
}
export function drawCharacterMenu(ctx, player, active) {
  drawOverlay(ctx, 0.74);
  const fill = ctx.createLinearGradient(0, 36, 0, 604);
  fill.addColorStop(0, '#19283b'); fill.addColorStop(1, '#0b1421');
  drawPanel(ctx, 50, 36, 860, 568, { fill, border: '#4b627b' });
  drawText(ctx, '✧', 75, 50, { size: 30, color: '#d7c49c', shadow: false });
  drawText(ctx, 'CHARACTER', 112, 55, { size: 22, shadow: false });
  drawText(ctx, `${getClassDefinition(player.classId).name}  /  Level ${player.level}`, 278, 63, { size: 12, color: '#94a9bf', bold: false, shadow: false });
  drawText(ctx, `${player.gold.toLocaleString()} G`, 830, 61, { size: 14, align: 'right', color: '#ecd091', shadow: false });
  drawPanel(ctx, 856, 54, 34, 34, { fill: '#26364a', border: '#43566e' });
  drawText(ctx, '×', 873, 57, { size: 23, align: 'center', shadow: false });
  MENU_TABS.forEach((tab, i) => {
    const x = 74 + i * 164, selected = active === tab;
    if (selected) {
      const glow = ctx.createLinearGradient(0, 100, 0, 136);
      glow.addColorStop(0, '#24445a11'); glow.addColorStop(1, '#326a8855');
      ctx.fillStyle = glow; ctx.fillRect(x, 100, 156, 36);
      ctx.save(); ctx.shadowColor = '#61dfff'; ctx.shadowBlur = 8;
      ctx.fillStyle = '#8be6ff'; ctx.fillRect(x, 134, 156, 2); ctx.restore();
    }
    drawText(ctx, tab, x + 78, 109, { size: 14, align: 'center', color: selected ? '#c7f1ff' : '#96aabe', shadow: false });
  });
  drawText(ctx, active === 'Skills' ? `${player.skillPoints} points available` : `${player.inventory.length} / 20 bag slots`, 884, 111, { size: 12, align: 'right', color: '#91b7ca', shadow: false });
}

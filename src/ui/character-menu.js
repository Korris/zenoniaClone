import { drawOverlay } from './pixel-text.js';
import { bagText as drawText, bagPanel, bagButton } from './bag-widgets.js';

export const BAG = Object.freeze({ x: 304, y: 34, width: 640, height: 568, contentY: 154, bottom: 602 });
export const MENU_TABS = ['Equipment', 'Inventory', 'Skills'];
export function tabBounds(index) {
  return { x: BAG.x + 16 + index * 202, y: BAG.y + 62, width: 198, height: 46 };
}
export function menuHit(x, y) {
  if (x >= BAG.x + BAG.width - 50 && x <= BAG.x + BAG.width - 16 && y >= BAG.y + 14 && y <= BAG.y + 48) return 'close';
  const i = MENU_TABS.findIndex((_, i) => {
    const b = tabBounds(i);
    return x >= b.x && x <= b.x + b.width && y >= b.y && y <= b.y + b.height;
  });
  return i < 0 ? null : MENU_TABS[i];
}

function symbol(ctx, kind, x, y) {
  ctx.save(); ctx.translate(x, y); ctx.scale(0.8, 0.8);
  ctx.fillStyle = '#eee5ce'; ctx.strokeStyle = '#25394c'; ctx.lineWidth = 1.5;
  if (kind === 'Equipment') {
    for (const angle of [-0.7, 0.7]) {
      ctx.save(); ctx.rotate(angle);
      ctx.beginPath(); ctx.moveTo(0, -14); ctx.lineTo(4, -9); ctx.lineTo(3, 6); ctx.lineTo(-3, 6); ctx.lineTo(-4, -9); ctx.closePath(); ctx.fill(); ctx.stroke();
      ctx.fillRect(-7, 6, 14, 3); ctx.fillRect(-2, 9, 4, 7); ctx.restore();
    }
  } else if (kind === 'Skills') {
    for (const side of [-1, 1]) {
      ctx.beginPath(); ctx.moveTo(0, -10); ctx.quadraticCurveTo(side * 7, -15, side * 14, -10); ctx.lineTo(side * 14, 12); ctx.quadraticCurveTo(side * 7, 8, 0, 13); ctx.closePath(); ctx.fill(); ctx.stroke();
      ctx.strokeStyle = '#9c8969'; ctx.lineWidth = 0.8;
      for (let row = 0; row < 3; row++) { ctx.beginPath(); ctx.moveTo(side * 4, -4 + row * 4); ctx.lineTo(side * 10, -5 + row * 4); ctx.stroke(); }
      ctx.strokeStyle = '#25394c'; ctx.lineWidth = 1.5;
    }
  } else {
    ctx.beginPath(); ctx.moveTo(-7, -10); ctx.lineTo(7, -10); ctx.bezierCurveTo(5, -4, 14, 0, 12, 12); ctx.quadraticCurveTo(0, 17, -12, 12); ctx.bezierCurveTo(-14, 0, -5, -4, -7, -10); ctx.fill(); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(-8, -9); ctx.lineTo(8, -9); ctx.stroke();
    ctx.fillStyle = '#23394c'; ctx.beginPath(); ctx.moveTo(-5, -11); ctx.lineTo(0, -7); ctx.lineTo(5, -11); ctx.lineTo(3, -4); ctx.lineTo(-3, -4); ctx.closePath(); ctx.fill();
    ctx.strokeStyle = '#b6ad99'; ctx.lineWidth = 0.8;
    ctx.beginPath(); ctx.moveTo(-6, 1); ctx.quadraticCurveTo(-9, 7, -7, 10); ctx.moveTo(7, 1); ctx.quadraticCurveTo(9, 8, 6, 12); ctx.stroke();
  }
  ctx.restore();
}

export function drawCharacterMenu(ctx, player, active, registry) {
  drawOverlay(ctx, 0.14);
  const skin = registry?.ui?.bagFrame;
  if (skin) {
    ctx.save(); ctx.imageSmoothingEnabled = true; ctx.imageSmoothingQuality = 'high';
    // Keep the painted title rail behind the title; stretch the body independently.
    ctx.drawImage(skin, 0, 0, skin.width, 180, BAG.x, BAG.y, BAG.width, 60);
    ctx.drawImage(skin, 0, 180, skin.width, skin.height - 180, BAG.x, BAG.y + 60, BAG.width, BAG.height - 60);
    ctx.restore();
  } else bagPanel(ctx, BAG.x, BAG.y, BAG.width, BAG.height, '#102139fa', '#a6b8c4');
  // The painted skin includes the leather pack and close-button backing.
  if (!skin) {
  ctx.save(); ctx.translate(BAG.x + 43, BAG.y + 27); ctx.rotate(0.1);
  bagPanel(ctx, -20, -20, 38, 40, '#916336', '#edc58f');
  bagPanel(ctx, -17, -20, 32, 16, '#bd8c55', '#e2bd88');
  ctx.fillStyle = '#4b321f'; ctx.fillRect(-12, -5, 5, 18); ctx.fillRect(6, -5, 5, 18);
  ctx.fillStyle = '#e3bc72'; ctx.fillRect(-12, 1, 5, 5); ctx.fillRect(6, 1, 5, 5); ctx.restore();
  }
  drawText(ctx, 'Bag', BAG.x + 98, BAG.y + 18, { size: 24 });
  const closeX = BAG.x + BAG.width - 50;
  if (!skin) bagPanel(ctx, closeX + 2, BAG.y + 16, 30, 30, '#8e2427', '#e3b4a2');
  ctx.save(); ctx.strokeStyle = '#fff5e9'; ctx.lineWidth = 3; ctx.lineCap = 'round';
  ctx.beginPath(); ctx.moveTo(closeX + 8, BAG.y + 23); ctx.lineTo(closeX + 23, BAG.y + 38);
  ctx.moveTo(closeX + 23, BAG.y + 23); ctx.lineTo(closeX + 8, BAG.y + 38); ctx.stroke(); ctx.restore();
  MENU_TABS.forEach((tab, i) => {
    const b = tabBounds(i), selected = active === tab;
    bagButton(ctx, '', b.x, b.y, b.width, b.height, selected ? 'blue' : 'steel', selected);
    symbol(ctx, tab, b.x + 39, b.y + 22);
    drawText(ctx, tab === 'Skills' ? 'Skill' : tab, b.x + 62, b.y + 14, { size: 16, color: '#f2f5ff' });
    if (selected) {
      ctx.beginPath(); ctx.moveTo(b.x + b.width / 2 - 9, b.y + b.height - 1); ctx.lineTo(b.x + b.width / 2, b.y + b.height + 8); ctx.lineTo(b.x + b.width / 2 + 9, b.y + b.height - 1);
      ctx.fillStyle = '#519de7'; ctx.fill(); ctx.strokeStyle = '#a8eeff'; ctx.lineWidth = 2; ctx.stroke();
    }
  });
}

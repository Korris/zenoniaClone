// Supply cells (gold, potions, materials) shown before the bag slots in the Inventory view.
import { bagText as drawText, bagSlot, bagWrappedText, bagButton } from './bag-widgets.js';
import { drawPotion } from '../graphics/potion-renderer.js';
import { BAG_ACTIONS } from './inventory-layout.js';

export const SUPPLY_NAMES = { coin: 'Gold', hpPotion: 'HP Potion', mpPotion: 'MP Potion', upgradeStone: 'Upgrade Stone' };
const SUPPLY_BLURBS = {
  coin: 'Gold used for trading.',
  hpPotion: 'Restores 30% HP.',
  mpPotion: 'Restores 30% MP.',
  upgradeStone: 'Used to enhance weapons. Success drops at higher +.',
};

export function supplyCount(player, kind) {
  if (kind === 'coin') return player.gold;
  if (kind === 'upgradeStone') return player.materials?.upgradeStone || 0;
  return player.potions?.[kind] || 0;
}

export function drawSupplyIcon(ctx, player, kind, x, y) {
  if (!kind) return;
  const count = supplyCount(player, kind);
  ctx.save(); if (count === 0) ctx.globalAlpha = 0.4;
  if (kind === 'coin') {
    const gold = ctx.createLinearGradient(x - 20, y, x + 20, y);
    gold.addColorStop(0, '#b67e22'); gold.addColorStop(0.5, '#fff0a4'); gold.addColorStop(1, '#edaa2e');
    ctx.beginPath(); ctx.ellipse(x, y - 4, 21, 24, 0.2, 0, Math.PI * 2); ctx.fillStyle = gold; ctx.fill();
    ctx.strokeStyle = '#ffe7a0'; ctx.lineWidth = 3; ctx.stroke();
    drawText(ctx, 'G', x, y - 17, { size: 24, align: 'center', color: '#a8771d' });
  } else drawPotion(ctx, kind, x, y - 5, 48);
  ctx.restore();
  drawText(ctx, `${count}`, x + 31, y + 25, { size: 14, align: 'right' });
}

export function drawSupplyDetails(ctx, player, kind) {
  bagSlot(ctx, 615, 169, 76);
  drawSupplyIcon(ctx, player, kind, 653, 207);
  drawText(ctx, SUPPLY_NAMES[kind], 704, 175, { size: 19, color: '#f2deae' });
  drawText(ctx, `Quantity: ${supplyCount(player, kind)}`, 616, 285, { size: 15 });
  bagWrappedText(ctx, SUPPLY_BLURBS[kind] || '', 616, 340, 288, { size: 14 });
  if (kind === 'hpPotion' || kind === 'mpPotion') bagButton(ctx, 'Use', BAG_ACTIONS.primary.x, BAG_ACTIONS.primary.y, BAG_ACTIONS.primary.w, BAG_ACTIONS.primary.h, 'gold');
}

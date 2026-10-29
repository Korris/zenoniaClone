// Zenonia-style skill tree: 3 branches x 3 tiers of nodes with prerequisite connectors.
// Keyboard: Left/Right branch, Up/Down node, Enter upgrade, K/L/I bind to hotbar slot, Tab/Esc close.
// Mouse: click selects a node, clicking the already-selected node upgrades it.
import { drawCharacterMenu } from './character-menu.js';
import { bagText as drawText, bagPanel, bagButton, bagSlot, bagWrappedText } from './bag-widgets.js';
import { SKILL_DEFINITIONS, HOTBAR_SLOT_KEYS, SKILL_HOTKEY_ACTIONS } from '../data/skill-definitions.js';
import { getClassDefinition } from '../data/class-definitions.js';
import { canUpgradeSkill, upgradeSkill, getSkillLevel, bindSkillToSlot } from '../systems/skill-system.js';
import {
  PANEL_X, NODE_SIZE,
  skillsForBranch, computeSkillTreeLayout, describeNodeState, describeNextLevel, describeRequirement,
} from './skill-tree-layout.js';

const COLUMN_TITLES = { sword: 'SWORD', body: 'BODY', spirit: 'SPIRIT', fire: 'FIRE', frost: 'FROST', arcane: 'ARCANE', gunnery: 'GUNNERY', explosives: 'EXPLOSIVES', engineering: 'ENGINEERING', holy: 'HOLY', restoration: 'RESTORATION', blessing: 'BLESSING', shadow: 'SHADOW', venom: 'VENOM', agility: 'AGILITY' };
const STATE_BORDER = { locked: '#555566', learned: '#cfcfe0', available: '#ffe680', maxed: '#8fff8f' };

export class SkillTreePanel {
  constructor() {
    this.branchIndex = 0;
    this.nodeIndex = 0;
    this.message = '';
    this.messageTimer = 0;
    this.classId = null;
    this.branches = [];
    this.layout = { positions: {}, navIndex: {} };
  }

  /** Rebuild the tree for the hero's class (the panel is shared across New Games). */
  syncClass(player) {
    if (player.classId === this.classId) return;
    this.classId = player.classId;
    this.branches = getClassDefinition(player.classId).branches;
    this.layout = computeSkillTreeLayout(this.branches);
    this.branchIndex = 0;
    this.nodeIndex = 0;
  }

  get selectedSkillId() {
    return skillsForBranch(this.branches[this.branchIndex])[this.nodeIndex];
  }

  /** @returns {{close:boolean, upgraded:string|null}} */
  handleInput(input, player) {
    this.syncClass(player);
    const result = { close: false, upgraded: null };
    const count = this.branches.length;
    if (input.wasPressed('skillPanel') || input.wasPressed('pause')) result.close = true;
    if (input.wasPressed('moveLeft')) this.branchIndex = (this.branchIndex + count - 1) % count;
    if (input.wasPressed('moveRight')) this.branchIndex = (this.branchIndex + 1) % count;
    if (input.wasPressed('moveUp')) this.nodeIndex = (this.nodeIndex + 4) % 5;
    if (input.wasPressed('moveDown')) this.nodeIndex = (this.nodeIndex + 1) % 5;

    for (const click of input.takeClicks()) {
      if (click.x >= 716 && click.x <= 914 && click.y >= 538 && click.y <= 576) { this.tryUpgrade(player, result); continue; }
      const bind = click.y >= 526 && click.y <= 560 ? [0, 1, 2].find(i => click.x >= 334 + i * 114 && click.x <= 434 + i * 114) : undefined;
      if (bind !== undefined) { const outcome = bindSkillToSlot(player, this.selectedSkillId, bind); this.flash(outcome.ok ? `Bound to [${HOTBAR_SLOT_KEYS[bind]}]` : outcome.reason); continue; }
      const hit = this.nodeAt(click.x, click.y);
      if (!hit) continue;
      if (hit === this.selectedSkillId) this.tryUpgrade(player, result);
      else this.select(hit);
    }
    if (input.wasPressed('confirm')) this.tryUpgrade(player, result);

    for (const [action, slot] of Object.entries(SKILL_HOTKEY_ACTIONS)) {
      if (!input.wasPressed(action)) continue;
      const outcome = bindSkillToSlot(player, this.selectedSkillId, slot);
      this.flash(outcome.ok ? `Bound to [${HOTBAR_SLOT_KEYS[slot]}]` : outcome.reason);
    }

    this.messageTimer -= 16;
    return result;
  }

  nodeAt(x, y) {
    const half = NODE_SIZE / 2;
    for (const [id, pos] of Object.entries(this.layout.positions)) {
      if (Math.abs(x - pos.x) <= half && Math.abs(y - pos.y) <= half) return id;
    }
    return null;
  }

  select(skillId) {
    const nav = this.layout.navIndex[skillId];
    this.branchIndex = nav.branchIndex;
    this.nodeIndex = nav.nodeIndex;
  }

  tryUpgrade(player, result) {
    const skillId = this.selectedSkillId;
    const outcome = upgradeSkill(player, skillId);
    if (outcome.ok) {
      player.recomputeStats();
      result.upgraded = skillId;
      this.flash(`${SKILL_DEFINITIONS[skillId].name} -> Lv ${getSkillLevel(player, skillId)}`);
    } else {
      this.flash(outcome.reason);
    }
  }

  flash(text) {
    this.message = text;
    this.messageTimer = 1500;
  }

  render(ctx, player, registry) {
    this.syncClass(player);
    const icons = registry.icons || registry;
    drawCharacterMenu(ctx, player, 'Skills', registry);
    bagPanel(ctx, 320, 154, 376, 426); bagPanel(ctx, 702, 154, 226, 426);
    this.branches.forEach((branch, i) => {
      drawText(ctx, COLUMN_TITLES[branch], PANEL_X + 16 + (i + 0.5) * (376 / this.branches.length), 169, { size: 12, align: 'center', color: '#b7dcf3' });
    });
    this.drawConnectors(ctx, player);
    for (const skillId of Object.keys(this.layout.positions)) this.drawNode(ctx, player, icons, skillId);
    drawText(ctx, `Skill points: ${player.skillPoints}`, 334, 492, { size: 14, color: '#f3d99c' });
    for (let i = 0; i < 3; i++) bagButton(ctx, `Bind [${HOTBAR_SLOT_KEYS[i]}]`, 334 + i * 114, 526, 100, 34);
    this.drawInfoBox(ctx, player, icons);
  }

  drawConnectors(ctx, player) {
    const { positions } = this.layout;
    ctx.lineWidth = 2;
    for (const [skillId, def] of Object.entries(SKILL_DEFINITIONS)) {
      for (const req of def.requires || []) {
        const from = positions[skillId];
        const to = positions[req.skill];
        if (!from || !to) continue;
        ctx.strokeStyle = getSkillLevel(player, req.skill) >= req.level ? '#6fc8e3' : '#4a4a5c';
        ctx.beginPath();
        ctx.moveTo(from.x, from.y);
        ctx.lineTo(to.x, to.y);
        ctx.stroke();
      }
    }
  }

  drawNode(ctx, player, icons, skillId) {
    const { x, y } = this.layout.positions[skillId];
    const state = describeNodeState(player, skillId);
    const selected = skillId === this.selectedSkillId;
    const half = NODE_SIZE / 2;
    const blink = state.available && player.skillPoints > 0 && Math.floor(performance.now() / 350) % 2 === 0;
    const borderColor = selected ? '#ffffff' : blink ? '#fff6c8' : state.maxed ? STATE_BORDER.maxed : state.available ? STATE_BORDER.available : state.learned ? STATE_BORDER.learned : STATE_BORDER.locked;

    bagSlot(ctx, x - half, y - half, NODE_SIZE, { selected, color: borderColor });

    const icon = icons[skillId];
    ctx.globalAlpha = state.locked ? 0.4 : 1;
    if (icon) { ctx.imageSmoothingEnabled = true; ctx.drawImage(icon, x - 16, y - 19, 32, 32); }
    ctx.globalAlpha = 1;

    const levelText = state.maxed ? 'MAX' : `${state.level}/${SKILL_DEFINITIONS[skillId].maxLevel}`;
    drawText(ctx, levelText, x, y + half - 14, { size: 9, align: 'center', color: state.maxed ? '#ffe680' : '#cfcfe0' });
  }

  drawInfoBox(ctx, player, icons) {
    const id = this.selectedSkillId, def = SKILL_DEFINITIONS[id], level = getSkillLevel(player, id);
    bagSlot(ctx, 716, 169, 54, { color: '#8bbedb' });
    if (icons[id]) { ctx.imageSmoothingEnabled = true; ctx.drawImage(icons[id], 721, 174, 44, 44); }
    bagWrappedText(ctx, def.name, 780, 172, 134, { size: 16, color: '#eef7ff', maxLines: 2 });
    drawText(ctx, `Lv. ${level} / ${def.maxLevel}`, 780, 213, { size: 12, color: '#f0d694' });
    bagWrappedText(ctx, def.description, 716, 251, 198, { size: 12, lineHeight: 19, maxLines: 4 });
    bagWrappedText(ctx, describeNextLevel(id, level), 716, 345, 198, { size: 12, lineHeight: 20, maxLines: 3, color: '#98dbff' });
    bagWrappedText(ctx, describeRequirement(player, id), 716, 419, 198, { size: 11, lineHeight: 18, maxLines: 3, color: '#d7c49e' });
    const slot = player.hotbar.indexOf(id);
    drawText(ctx, def.type === 'passive' ? 'Passive skill' : slot >= 0 ? `Bound: [${HOTBAR_SLOT_KEYS[slot]}]` : 'Active skill', 716, 490, { size: 12, color: '#b9d4e4' });
    bagButton(ctx, 'Level Up', 716, 538, 198, 38, 'gold');
    if (this.messageTimer > 0) bagWrappedText(ctx, this.message, 324, 581, 596, { size: 10, lineHeight: 13, maxLines: 1, color: '#f4dc9c' });
  }
}

// Zenonia-style skill tree: 3 branches x 3 tiers of nodes with prerequisite connectors.
// Keyboard: Left/Right branch, Up/Down node, Enter upgrade, K/L/I bind to hotbar slot, Tab/Esc close.
// Mouse: click selects a node, clicking the already-selected node upgrades it.
import { drawText, drawPanel, drawOverlay, drawScaledSprite } from './pixel-text.js';
import { SKILL_DEFINITIONS, HOTBAR_SLOT_KEYS, SKILL_HOTKEY_ACTIONS } from '../data/skill-definitions.js';
import { getClassDefinition } from '../data/class-definitions.js';
import { canUpgradeSkill, upgradeSkill, getSkillLevel, bindSkillToSlot } from '../systems/skill-system.js';
import {
  PANEL_X, PANEL_Y, PANEL_W, PANEL_H, NODE_SIZE, TREE_TOP, INFO_BOX_Y, INFO_BOX_H,
  skillsForBranch, computeSkillTreeLayout, describeNodeState, describeNextLevel, describeRequirement,
} from './skill-tree-layout.js';

const COLUMN_TITLES = { sword: 'SWORD', body: 'BODY', spirit: 'SPIRIT', fire: 'FIRE', frost: 'FROST', arcane: 'ARCANE' };
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

  render(ctx, player, icons) {
    this.syncClass(player);
    drawOverlay(ctx, 0.65);
    drawPanel(ctx, PANEL_X, PANEL_Y, PANEL_W, PANEL_H);
    drawText(ctx, 'SKILL TREE', PANEL_X + 16, PANEL_Y + 12, { size: 16, color: '#ffe680' });
    drawText(ctx, `Skill Points: ${player.skillPoints}`, PANEL_X + PANEL_W - 16, PANEL_Y + 14, { size: 14, align: 'right', color: player.skillPoints > 0 ? '#8fff8f' : '#cfcfe0' });
    this.branches.forEach((branch, i) => {
      drawText(ctx, COLUMN_TITLES[branch], PANEL_X + (i + 0.5) * (PANEL_W / this.branches.length), PANEL_Y + 32, { size: 14, align: 'center', color: '#8fc3ff' });
    });
    this.drawConnectors(ctx, player);
    for (const skillId of Object.keys(this.layout.positions)) this.drawNode(ctx, player, icons, skillId);
    this.drawInfoBox(ctx, player);
  }

  drawConnectors(ctx, player) {
    const { positions } = this.layout;
    ctx.lineWidth = 2;
    for (const [skillId, def] of Object.entries(SKILL_DEFINITIONS)) {
      for (const req of def.requires || []) {
        const from = positions[skillId];
        const to = positions[req.skill];
        if (!from || !to) continue;
        ctx.strokeStyle = getSkillLevel(player, req.skill) >= req.level ? '#d9b45a' : '#4a4a5c';
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

    ctx.fillStyle = state.locked ? 'rgba(10,10,18,0.9)' : 'rgba(30,26,54,0.92)';
    ctx.fillRect(x - half, y - half, NODE_SIZE, NODE_SIZE);
    ctx.lineWidth = selected ? 3 : 2;
    ctx.strokeStyle = borderColor;
    ctx.strokeRect(x - half + 1, y - half + 1, NODE_SIZE - 2, NODE_SIZE - 2);

    const icon = icons[skillId];
    ctx.globalAlpha = state.locked ? 0.4 : 1;
    if (icon) drawScaledSprite(ctx, icon, x - icon.width, y - icon.height - 4, 2);
    ctx.globalAlpha = 1;

    const levelText = state.maxed ? 'MAX' : `${state.level}/${SKILL_DEFINITIONS[skillId].maxLevel}`;
    drawText(ctx, levelText, x, y + half - 14, { size: 11, align: 'center', color: state.maxed ? '#ffe680' : '#cfcfe0' });
  }

  drawInfoBox(ctx, player) {
    const skillId = this.selectedSkillId;
    const def = SKILL_DEFINITIONS[skillId];
    const level = getSkillLevel(player, skillId);
    drawPanel(ctx, PANEL_X + 16, INFO_BOX_Y, PANEL_W - 32, INFO_BOX_H, { fill: 'rgba(8,8,18,0.9)' });
    const typeLabel = def.type === 'active' ? 'Active' : 'Passive';
    drawText(ctx, `${def.name}  [${typeLabel}]`, PANEL_X + 28, INFO_BOX_Y + 8, { size: 15, color: '#ffffff' });
    drawText(ctx, def.description, PANEL_X + 28, INFO_BOX_Y + 28, { size: 12, color: '#b8b8cc', bold: false });
    drawText(ctx, describeNextLevel(skillId, level), PANEL_X + 28, INFO_BOX_Y + 46, { size: 12, color: '#8fc3ff', bold: false });
    drawText(ctx, describeRequirement(player, skillId), PANEL_X + PANEL_W - 28, INFO_BOX_Y + 8, { size: 12, align: 'right', color: '#ff9a7a', bold: false });
    const slot = def.type === 'active' ? player.hotbar.indexOf(skillId) : -1;
    const boundText = def.type !== 'active' ? '' : slot >= 0 ? `Bound: [${HOTBAR_SLOT_KEYS[slot]}]` : 'Unbound - press K/L/I';
    drawText(ctx, boundText, PANEL_X + PANEL_W - 28, INFO_BOX_Y + 28, { size: 12, align: 'right', color: '#ffe680', bold: false });
    const footer = this.messageTimer > 0 ? this.message : 'Arrows select   Enter upgrade   K/L/I bind   Tab close';
    drawText(ctx, footer, PANEL_X + PANEL_W / 2, INFO_BOX_Y + INFO_BOX_H + 14, { size: 12, align: 'center', color: this.messageTimer > 0 ? '#ffe680' : '#9a9ab0' });
  }
}

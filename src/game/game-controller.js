// Top-level state machine: title -> playing <-> (paused | skills | questLog | dialog) -> dead / win.
// Routes input to the active screen and composes the frame.
import { GAME_STATES, CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { GameSession } from './game-session.js';
import { updateWorld } from './world-update.js';
import { renderWorld } from '../graphics/world-renderer.js';
import { drawHud } from '../ui/hud-renderer.js';
import { SkillTreePanel } from '../ui/skill-tree-panel.js';
import { InventoryPanel } from '../ui/inventory-panel.js';
import { QuestLogPanel } from '../ui/quest-log-panel.js';
import { DialogBox } from '../ui/dialog-box.js';
import { MessageBanner } from '../ui/message-banner.js';
import { createTitleMenu, createPauseMenu, createDeathMenu, createVictoryMenu } from '../ui/menu-screens.js';
import { hasSave } from '../save/local-storage-save.js';
import { findNearbyNpc, computeNpcMarkers, openNpcDialog, applyDialogOutcome } from './npc-interaction.js';
import { onMonsterKilledForQuests, onMapReachedForQuests, onDialogOutcomeForQuests } from './quest-flow.js';

const S = GAME_STATES;

export class GameController {
  constructor(registry, input) {
    this.registry = registry;
    this.input = input;
    this.banner = new MessageBanner();
    this.session = new GameSession(this.banner);
    this.skillPanel = new SkillTreePanel();
    this.inventoryPanel = new InventoryPanel();
    this.questLog = new QuestLogPanel();
    this.dialog = new DialogBox();
    this.state = S.TITLE;
    this.menu = createTitleMenu(hasSave());
    this.nearbyNpc = null;
    this.npcMarkers = {};
  }

  update(dt) {
    this.banner.update(dt);
    const handlers = {
      [S.TITLE]: () => this.updateTitle(),
      [S.PLAYING]: () => this.updatePlaying(dt),
      [S.PAUSED]: () => this.updatePaused(),
      [S.SKILLS]: () => this.updateSkills(),
      [S.INVENTORY]: () => this.updateInventory(),
      [S.QUEST_LOG]: () => { if (this.questLog.handleInput(this.input).close) this.state = S.PLAYING; },
      [S.DIALOG]: () => this.updateDialog(dt),
      [S.DEAD]: () => this.updateDead(),
      [S.WIN]: () => this.updateWin(),
    };
    handlers[this.state]?.();
    this.input.endFrame();
  }

  updateTitle() {
    const choice = this.menu.handleInput(this.input);
    if (choice === 'New Game') this.session.startNewGame();
    else if (choice === 'Continue' && !this.session.continueFromSave()) this.session.startNewGame();
    if (choice) this.state = S.PLAYING;
  }

  updatePlaying(dt) {
    const { input, session } = this;
    if (input.wasPressed('pause')) { this.menu = createPauseMenu(); this.state = S.PAUSED; return; }
    if (input.wasPressed('skillPanel')) { this.state = S.SKILLS; return; }
    if (input.wasPressed('questLog')) { this.state = S.QUEST_LOG; return; }
    if (input.wasPressed('inventory')) { session.player.hasUnseenItem = false; this.state = S.INVENTORY; return; }
    const previousNearby = this.nearbyNpc;
    this.nearbyNpc = findNearbyNpc(session.world);
    if (previousNearby && previousNearby !== this.nearbyNpc) previousNearby.dir = previousNearby.def.dir; // resume the idle pose once the hero walks off
    if (this.nearbyNpc) this.nearbyNpc.faceTowards(session.player.x, session.player.y); // turn to the approaching hero
    if (this.nearbyNpc && input.wasPressed('interact')) {
      const { faces } = this.registry;
      openNpcDialog(this.dialog, this.nearbyNpc, session.player, session.quests, faces[this.nearbyNpc.id] || faces[this.nearbyNpc.def.sprite] || null);
      this.state = S.DIALOG;
      return;
    }
    const events = updateWorld(session.world, input, dt, this.banner);
    session.tickAutosave(dt);
    for (const event of events) this.handleEvent(event);
    this.npcMarkers = computeNpcMarkers(session.world, session.quests);
    if (session.player.dead && this.state === S.PLAYING) this.handleEvent({ type: 'playerDied' });
  }

  handleEvent(event) {
    const { session } = this;
    if (event.type === 'portal') {
      const result = session.usePortal(event.portal);
      if (result.travelled) onMapReachedForQuests(session, result.reached);
    } else if (event.type === 'monsterKilled') onMonsterKilledForQuests(session, event.monsterType);
    else if (event.type === 'levelUp') {
      this.banner.show('LEVEL UP!', `Hero is now level ${session.player.level}. +${event.levels} skill point`, 1800, '#8fff8f');
      session.save(false);
    } else if (event.type === 'bossDefeated') {
      session.save(false);
      this.menu = createVictoryMenu(session.player);
      this.state = S.WIN;
    } else if (event.type === 'playerDied') {
      this.deathDelay = 900;
      this.state = S.DEAD;
      this.menu = null;
    }
  }

  updateDialog(dt) {
    this.dialog.update(dt);
    const result = this.dialog.handleInput(this.input);
    if (!result.closed) return;
    const { session } = this;
    const outcome = applyDialogOutcome(this.dialog.payload, result.choice, session.player, session.quests);
    onDialogOutcomeForQuests(session, outcome);
    this.npcMarkers = computeNpcMarkers(session.world, session.quests);
    this.state = S.PLAYING;
  }

  updatePaused() {
    if (this.input.wasPressed('pause')) { this.state = S.PLAYING; return; }
    const choice = this.menu.handleInput(this.input);
    if (choice === 'Resume') this.state = S.PLAYING;
    else if (choice === 'Save Game') { this.session.save(true); this.state = S.PLAYING; }
    else if (choice === 'Quit to Title') this.returnToTitle();
  }

  updateSkills() {
    const result = this.skillPanel.handleInput(this.input, this.session.player);
    if (result.upgraded) this.session.save(false);
    if (result.close) this.state = S.PLAYING;
  }

  updateInventory() {
    const result = this.inventoryPanel.handleInput(this.input, this.session.player);
    if (result.close) { this.session.save(false); this.state = S.PLAYING; }
  }

  updateDead() {
    this.session.player.deathTimer += 16; // world is frozen, so drive the death animation here
    if (this.menu === null) {
      this.deathDelay -= 16; // short delay so the death animation plays before the menu
      if (this.deathDelay > 0) return;
      this.menu = createDeathMenu(Math.floor(this.session.player.exp * 0.1));
      return;
    }
    if (this.menu.handleInput(this.input) === 'Respawn') {
      this.session.respawn();
      this.session.save(false);
      this.state = S.PLAYING;
    }
  }

  updateWin() {
    const choice = this.menu.handleInput(this.input);
    if (choice === 'Keep Playing') this.state = S.PLAYING;
    else if (choice === 'Return to Title') this.returnToTitle();
  }

  returnToTitle() {
    this.session.save(false);
    this.menu = createTitleMenu(hasSave());
    this.state = S.TITLE;
  }

  render(ctx) {
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.fillStyle = '#000';
    ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
    if (this.state === S.TITLE) { this.menu.render(ctx, this.registry, { dim: false, showHero: true }); return; }

    const { session } = this;
    const view = { npcMarkers: this.npcMarkers, nearbyNpc: this.state === S.PLAYING ? this.nearbyNpc : null, questState: session.quests };
    renderWorld(ctx, session.world, this.registry, view);
    drawHud(ctx, session.world, this.registry, view);
    this.banner.render(ctx);

    if (this.state === S.SKILLS) this.skillPanel.render(ctx, session.player, this.registry.icons);
    else if (this.state === S.INVENTORY) this.inventoryPanel.render(ctx, session.player, this.registry);
    else if (this.state === S.QUEST_LOG) this.questLog.render(ctx, session.quests);
    else if (this.state === S.DIALOG) this.dialog.render(ctx);
    else if (this.state === S.DEAD && this.menu) this.menu.render(ctx, this.registry, { dimAlpha: 0.75 });
    else if (this.state === S.PAUSED || this.state === S.WIN) this.menu.render(ctx, this.registry);
  }
}

// Top-level state machine: title -> playing <-> (paused | skills | questLog | dialog) -> dead / win.
// Routes input to the active screen and composes the frame.
import { GAME_STATES, CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { setScreenTransform } from '../core/canvas-display.js';
import { GameSession } from './game-session.js';
import { updateWorld } from './world-update.js';
import { renderWorld } from '../graphics/world-renderer.js';
import { drawHud } from '../ui/hud-renderer.js';
import { SkillTreePanel } from '../ui/skill-tree-panel.js';
import { InventoryPanel } from '../ui/inventory-panel.js';
import { QuestLogPanel } from '../ui/quest-log-panel.js';
import { DialogBox } from '../ui/dialog-box.js';
import { MessageBanner } from '../ui/message-banner.js';
import { createTitleMenu, createPauseMenu, createVictoryMenu } from '../ui/menu-screens.js';
import { ClassSelectScreen } from '../ui/class-select-screen.js';
import { hasSave, readSave } from '../save/local-storage-save.js';
import { drawLoadingScreen } from '../ui/loading-screen.js';
import { usePotion } from '../systems/loot-system.js';
import { menuHit } from '../ui/character-menu.js';
import { findNearbyNpc, computeNpcMarkers, openNpcDialog, applyDialogOutcome } from './npc-interaction.js';
import { captureInterpolationSnapshot, renderInterpolated } from './render-interpolation.js';
import { onMonsterKilledForQuests, onMapReachedForQuests, onDialogOutcomeForQuests } from './quest-flow.js';

const S = GAME_STATES;
const DEATH_ANIMATION_MS = 1400;

export class GameController {
  /** @param {{ensure:(classId:string, onProgress?:Function)=>Promise<unknown>}|null} classAssets lazy per-class art loader (null => already in the registry) */
  constructor(registry, input, classAssets = null) {
    this.registry = registry;
    this.classAssets = classAssets;
    this.loading = { label: '', loaded: 0, total: 0 };
    this.input = input;
    this.banner = new MessageBanner();
    this.session = new GameSession(this.banner);
    this.skillPanel = new SkillTreePanel();
    this.inventoryPanel = new InventoryPanel();
    this.questLog = new QuestLogPanel();
    this.dialog = new DialogBox();
    this.state = S.TITLE;
    this.worldBackend = null; // optional world renderer backend (PixiJS): { renderWorld(ctx, world, registry, view) }
    this.menu = createTitleMenu(hasSave());
    this.nearbyNpc = null;
    this.npcMarkers = {};
  }

  update(dt) {
    captureInterpolationSnapshot(this.session.world);
    this.banner.update(dt);
    const handlers = {
      [S.TITLE]: () => this.updateTitle(),
      [S.CLASS_SELECT]: () => this.updateClassSelect(),
      [S.LOADING]: () => {}, // the pending class-asset promise advances the state
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
    if (choice === 'New Game') {
      this.openClassSelect();
    } else if (choice === 'Continue') {
      const saved = readSave();
      // An unusable save goes through hero selection like a new game.
      if (!saved) this.openClassSelect();
      else this.loadClassThen(saved.player.classId, () => {
        if (this.session.continueFromSave()) this.state = S.PLAYING;
        else this.openClassSelect();
      });
    }
  }

  openClassSelect() {
    this.classSelect = new ClassSelectScreen();
    this.state = S.CLASS_SELECT;
  }

  updateClassSelect() {
    const { classId, back } = this.classSelect.handleInput(this.input);
    if (back) this.state = S.TITLE;
    if (!classId) return;
    this.loadClassThen(classId, () => {
      this.session.startNewGame(classId);
      this.state = S.PLAYING;
    });
  }

  /** Shows the loading screen while the class's art streams in, then runs `onReady` (also when the art failed: the loader falls back). */
  loadClassThen(classId, onReady) {
    if (!this.classAssets) { onReady(); return; }
    this.state = S.LOADING;
    this.loading = { label: `Preparing ${classId}`, loaded: 0, total: 0 };
    const finish = () => { if (this.state === S.LOADING) onReady(); };
    this.classAssets.ensure(classId, (loaded, total) => { this.loading.loaded = loaded; this.loading.total = total; })
      .then(finish, (err) => { console.error('Class asset load failed:', err); finish(); });
  }

  updatePlaying(dt) {
    const { input, session } = this;
    for (const kind of ['hpPotion', 'mpPotion']) {
      if (input.wasPressed(kind) && usePotion(session.player, kind)) {
        session.world.addText(session.player.x, session.player.y - 30, kind === 'hpPotion' ? 'HP restored' : 'MP restored', kind === 'hpPotion' ? '#ff8c9c' : '#80ceff');
      }
    }
    if (input.wasPressed('pause')) { this.menu = createPauseMenu(); this.state = S.PAUSED; return; }
    if (input.wasPressed('skillPanel')) { this.state = S.SKILLS; return; }
    if (input.wasPressed('questLog')) { this.state = S.QUEST_LOG; return; }
    if (input.wasPressed('inventory')) { session.player.hasUnseenItem = false; this.inventoryPanel.mode = 'Equipment'; this.inventoryPanel.selection = { area: 'equipment', index: 0 }; this.state = S.INVENTORY; return; }
    const previousNearby = this.nearbyNpc;
    this.nearbyNpc = findNearbyNpc(session.world);
    if (previousNearby && previousNearby !== this.nearbyNpc) previousNearby.resetFacing(); // resume the idle pose once the hero walks off
    // NPCs no longer track the hero while nearby; openNpcDialog() turns them only when talked to.
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
      this.deathDelay = DEATH_ANIMATION_MS;
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
    const menuInput = this.characterMenuInput();
    if (!menuInput) return;
    const result = this.skillPanel.handleInput(menuInput, this.session.player);
    if (result.upgraded) this.session.save(false);
    if (result.close) this.state = S.PLAYING;
  }

  updateInventory() {
    const menuInput = this.characterMenuInput();
    if (!menuInput) return;
    const result = this.inventoryPanel.handleInput(menuInput, this.session.player);
    if (result.close) { this.session.save(false); this.state = S.PLAYING; }
  }

  characterMenuInput() {
    let tab = null;
    if (this.state === S.SKILLS && this.input.wasPressed('inventory')) tab = 'Inventory';
    if (this.state === S.INVENTORY && this.input.wasPressed('skillPanel')) tab = 'Skills';
    const clicks = [];
    for (const click of this.input.takeClicks()) {
      const hit = menuHit(click.x, click.y);
      if (hit) tab = hit;
      else clicks.push(click);
    }
    if (tab) {
      this.session.save(false);
      if (tab === 'close') this.state = S.PLAYING;
      else if (tab === 'Skills') this.state = S.SKILLS;
      else {
        this.state = S.INVENTORY;
        this.session.player.hasUnseenItem = false;
        this.inventoryPanel.mode = tab;
        this.inventoryPanel.page = 0;
        this.inventoryPanel.selection = { area: tab === 'Equipment' ? 'equipment' : 'supply', index: 0 };
        this.inventoryPanel.lastClick = null;
      }
      return null;
    }
    const input = Object.create(this.input);
    input.takeClicks = () => clicks;
    return input;
  }

  /** Death animation plays for DEATH_ANIMATION_MS, then the hero wakes up in Oakhaven Village (no menu). */
  updateDead() {
    this.session.player.deathTimer += 16; // world is frozen, so drive the death animation here
    this.deathDelay -= 16;
    if (this.deathDelay > 0) return;
    const lost = this.session.respawn();
    this.session.save(false);
    this.state = S.PLAYING;
    this.banner.show('You fainted...', lost > 0 ? `Returned to Oakhaven Village (-${lost} EXP)` : 'Returned to Oakhaven Village', 3000, '#ff9a7a');
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

  /** @param {number} [alpha] 0..1 progress into the next logic step (see render-interpolation.js) */
  render(ctx, alpha = 0) {
    setScreenTransform(ctx);
    // With a WebGL world backend (?renderer=pixi) the world shows through this canvas, so it is cleared to transparent
    // while a world is on screen; title / class select / loading stay opaque black and hide the WebGL canvas.
    const worldOnScreen = ![S.TITLE, S.CLASS_SELECT, S.LOADING].includes(this.state);
    if (this.worldBackend && worldOnScreen) ctx.clearRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
    else {
      ctx.fillStyle = '#000';
      ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
    }
    if (this.state === S.TITLE) { this.menu.render(ctx, this.registry, { dim: false, showHero: true }); return; }
    if (this.state === S.CLASS_SELECT) { this.classSelect.render(ctx, this.registry, performance.now()); return; }
    if (this.state === S.LOADING) { drawLoadingScreen(ctx, this.loading.label, this.loading.loaded, this.loading.total); return; }

    const { session } = this;
    const view = { npcMarkers: this.npcMarkers, nearbyNpc: this.state === S.PLAYING ? this.nearbyNpc : null, questState: session.quests };
    renderInterpolated(session.world, alpha, () => {
      (this.worldBackend ? this.worldBackend.renderWorld : renderWorld)(ctx, session.world, this.registry, view);
      drawHud(ctx, session.world, this.registry, view);
    });
    this.banner.render(ctx);

    if (this.state === S.SKILLS) this.skillPanel.render(ctx, session.player, this.registry);
    else if (this.state === S.INVENTORY) this.inventoryPanel.render(ctx, session.player, this.registry);
    else if (this.state === S.QUEST_LOG) this.questLog.render(ctx, session.quests);
    else if (this.state === S.DIALOG) this.dialog.render(ctx);
    else if (this.state === S.PAUSED || this.state === S.WIN) this.menu.render(ctx, this.registry);
  }
}

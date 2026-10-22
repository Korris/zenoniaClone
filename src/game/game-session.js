// A play session: owns the hero/world/quest state and performs high-level actions (new game,
// load, save, map travel, respawn). The state machine in game-controller.js decides WHEN.
import { Player } from '../entities/player.js';
import { Camera } from '../core/camera.js';
import { WorldState } from './world-state.js';
import { loadMap, placePlayerAtWorld } from '../world/map-manager.js';
import { FIRST_MAP_ID } from '../world/map-registry.js';
import { serializeSave } from '../save/save-serializer.js';
import { writeSave, readSave } from '../save/local-storage-save.js';
import { createQuestState, sanitizeQuestState, recordMapEnter } from '../systems/quest-system.js';

const AUTOSAVE_INTERVAL_MS = 30000;

export class GameSession {
  constructor(banner) {
    this.banner = banner;
    this.player = new Player();
    this.camera = new Camera();
    this.world = new WorldState(this.player, this.camera);
    this.quests = createQuestState();
    this.autosaveTimer = AUTOSAVE_INTERVAL_MS;
    this.portalMessageCooldown = 0;
  }

  startNewGame() {
    this.player = new Player();
    this.world = new WorldState(this.player, this.camera);
    this.quests = createQuestState();
    this.enterMap(FIRST_MAP_ID);
  }

  /** @returns {boolean} true when a valid save was loaded */
  continueFromSave() {
    const save = readSave();
    if (!save) return false;
    this.player = new Player();
    this.player.applySaveData(save.player);
    this.world = new WorldState(this.player, this.camera);
    this.world.bossDefeated = save.bossDefeated;
    this.quests = sanitizeQuestState(save.quests);
    this.enterMap(save.mapId);
    if (save.x !== null && save.y !== null) placePlayerAtWorld(this.world, save.x, save.y);
    return true;
  }

  /** Load a map, show its banner, and tick "reach" quests. Returns quest ids that completed. */
  enterMap(mapId, tx = null, ty = null) {
    const mapDef = loadMap(this.world, mapId, tx, ty);
    this.banner.show(mapDef.name, mapDef.boss ? 'A powerful presence lurks here...' : '', 2400);
    return recordMapEnter(this.quests, mapDef.id);
  }

  save(showMessage = true) {
    if (this.player.dead) return false; // death is resolved by respawn() before the next save
    const ok = writeSave(serializeSave({
      player: this.player, mapId: this.world.mapId, x: this.player.x, y: this.player.y,
      bossDefeated: this.world.bossDefeated, quests: this.quests,
    }));
    if (showMessage) this.world.addText(this.player.x, this.player.y - 28, ok ? 'Game saved' : 'Save failed', ok ? '#8fff8f' : '#ff7070', { lifeMs: 900 });
    return ok;
  }

  /** Handle stepping on a portal: travel if level allows, otherwise warn (throttled). */
  usePortal(portal) {
    if (this.player.level >= portal.minLevel) {
      const reached = this.enterMap(portal.targetMap, portal.targetTx, portal.targetTy);
      this.save(false);
      return { travelled: true, reached };
    }
    if (this.portalMessageCooldown <= 0) {
      this.banner.show('Portal sealed', `Requires hero level ${portal.minLevel}`, 1500, '#ff9a7a');
      this.portalMessageCooldown = 1600;
    }
    return { travelled: false, reached: [] };
  }

  /** Revive at the current map's start; returns EXP lost. */
  respawn() {
    const lost = this.player.revive();
    const { playerStart } = this.world.mapDef;
    this.enterMap(this.world.mapId, playerStart.tx, playerStart.ty);
    return lost;
  }

  tickAutosave(dt) {
    this.portalMessageCooldown -= dt;
    this.autosaveTimer -= dt;
    if (this.autosaveTimer <= 0) {
      this.autosaveTimer = AUTOSAVE_INTERVAL_MS;
      this.save(false);
    }
  }
}

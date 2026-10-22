// Three-hit combo state machine. States: idle -> windup -> active -> recover -> (next hit | idle).
// A press during a swing is buffered (one press max). After a swing ends, the player has
// `chainWindowMs` to press again and continue the chain; otherwise the combo resets.
// Pure logic (no DOM, no rendering) -> unit-testable.
import { COMBO_CONFIG } from '../data/hero-stats-config.js';

export class ComboController {
  constructor(config = COMBO_CONFIG) {
    this.config = config;
    this.reset();
  }

  reset() {
    this.state = 'idle';
    this.step = 0; // 1..3 while swinging, 0 when idle
    this.lastStep = 0; // last completed hit, used to continue the chain
    this.timer = 0;
    this.chainTimer = 0;
    this.queued = false;
  }

  get isAttacking() {
    return this.state !== 'idle';
  }

  /** Current hit config (null when idle). */
  get currentHit() {
    return this.step > 0 ? this.config.hits[this.step - 1] : null;
  }

  /** 0..1 progress through the current swing (windup + active + recover), for animation frames. */
  get phaseProgress() {
    const hit = this.currentHit;
    if (!hit) return 0;
    const total = hit.windupMs + hit.activeMs + hit.recoverMs;
    const elapsed = this.state === 'windup' ? this.timer : this.state === 'active' ? hit.windupMs + this.timer : hit.windupMs + hit.activeMs + this.timer;
    return Math.min(1, elapsed / total);
  }

  /** Combo counter for HUD: how many hits landed in the current chain. */
  get displayCount() {
    return this.state === 'idle' ? this.lastStep : Math.max(this.step - 1, this.lastStep);
  }

  /**
   * Player pressed attack.
   * @returns {boolean} true if a new swing started immediately
   */
  pressAttack() {
    if (this.state === 'idle') {
      const canChain = this.chainTimer > 0 && this.lastStep > 0 && this.lastStep < this.config.hits.length;
      this.startHit(canChain ? this.lastStep + 1 : 1);
      return true;
    }
    if (this.step < this.config.hits.length) this.queued = true;
    return false;
  }

  startHit(step) {
    this.step = step;
    this.state = 'windup';
    this.timer = 0;
    this.queued = false;
    this.chainTimer = 0;
  }

  /**
   * Advance time. Returns an array of events: {type:'hit', step} when a swing becomes active,
   * {type:'end', step} when the swing finishes.
   */
  update(dt) {
    const events = [];
    if (this.state === 'idle') {
      if (this.chainTimer > 0) {
        this.chainTimer -= dt;
        if (this.chainTimer <= 0) this.lastStep = 0;
      }
      return events;
    }

    const hit = this.currentHit;
    this.timer += dt;
    if (this.state === 'windup' && this.timer >= hit.windupMs) {
      this.state = 'active';
      this.timer -= hit.windupMs;
      events.push({ type: 'hit', step: this.step });
    }
    if (this.state === 'active' && this.timer >= hit.activeMs) {
      this.state = 'recover';
      this.timer -= hit.activeMs;
    }
    if (this.state === 'recover' && this.timer >= hit.recoverMs) {
      events.push({ type: 'end', step: this.step });
      this.finishHit();
    }
    return events;
  }

  finishHit() {
    const finished = this.step;
    const isLast = finished >= this.config.hits.length;
    if (this.queued && !isLast) {
      this.lastStep = finished;
      this.startHit(finished + 1);
      return;
    }
    this.state = 'idle';
    this.step = 0;
    this.queued = false;
    this.lastStep = isLast ? 0 : finished; // finisher resets chain completely
    this.chainTimer = isLast ? 0 : this.config.chainWindowMs;
  }

  /** Damage multiplier for a hit step, finisher scales with hero level. */
  multiplierFor(step, heroLevel) {
    const hit = this.config.hits[step - 1];
    if (!hit) return 1;
    if (step === this.config.hits.length) return hit.mult + this.config.finisherMultPerLevel * (heroLevel - 1);
    return hit.mult;
  }
}

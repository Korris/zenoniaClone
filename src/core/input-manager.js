// Keyboard + mouse input mapped to named actions. `isDown` = held, `wasPressed` = edge
// triggered once per frame (cleared in endFrame). Mouse clicks are queued in canvas
// logical coordinates (960x640) so UI code never deals with CSS scaling.
export const ACTION_KEYS = Object.freeze({
  moveUp: ['KeyW', 'ArrowUp'],
  moveDown: ['KeyS', 'ArrowDown'],
  moveLeft: ['KeyA', 'ArrowLeft'],
  moveRight: ['KeyD', 'ArrowRight'],
  attack: ['KeyJ', 'Space'],
  skill1: ['KeyK'],
  skill2: ['KeyL'],
  skill3: ['KeyI'],
  skillPanel: ['Tab', 'KeyT'],
  pause: ['Escape'],
  confirm: ['Enter', 'KeyJ', 'Space'],
  interact: ['KeyE', 'Enter'],
  questLog: ['KeyQ'],
  inventory: ['KeyB'],
  dropItem: ['KeyX'],
});

const PREVENT_DEFAULT_CODES = new Set(['Tab', 'Space', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight']);

export class InputManager {
  constructor(canvas) {
    this.canvas = canvas;
    this.heldCodes = new Set();
    this.pressedCodes = new Set();
    this.clicks = [];
    this.keyToActions = this.buildKeyLookup();
    window.addEventListener('keydown', (e) => this.onKeyDown(e));
    window.addEventListener('keyup', (e) => this.onKeyUp(e));
    window.addEventListener('blur', () => this.heldCodes.clear());
    canvas.addEventListener('mousedown', (e) => this.onMouseDown(e));
  }

  buildKeyLookup() {
    const lookup = {};
    for (const [action, codes] of Object.entries(ACTION_KEYS)) {
      for (const code of codes) (lookup[code] ||= []).push(action);
    }
    return lookup;
  }

  onKeyDown(e) {
    if (PREVENT_DEFAULT_CODES.has(e.code)) e.preventDefault();
    if (e.repeat) return;
    this.heldCodes.add(e.code);
    this.pressedCodes.add(e.code);
  }

  onKeyUp(e) {
    this.heldCodes.delete(e.code);
  }

  onMouseDown(e) {
    const rect = this.canvas.getBoundingClientRect();
    const scaleX = this.canvas.width / rect.width;
    const scaleY = this.canvas.height / rect.height;
    this.clicks.push({ x: (e.clientX - rect.left) * scaleX, y: (e.clientY - rect.top) * scaleY });
  }

  /** True while any key bound to the action is held. */
  isDown(action) {
    return ACTION_KEYS[action].some((code) => this.heldCodes.has(code));
  }

  /** True only on the frame a bound key went down. */
  wasPressed(action) {
    return ACTION_KEYS[action].some((code) => this.pressedCodes.has(code));
  }

  /** Movement vector from held keys, normalized so diagonals aren't faster. */
  getMoveVector() {
    let x = (this.isDown('moveRight') ? 1 : 0) - (this.isDown('moveLeft') ? 1 : 0);
    let y = (this.isDown('moveDown') ? 1 : 0) - (this.isDown('moveUp') ? 1 : 0);
    if (x !== 0 && y !== 0) {
      const inv = Math.SQRT1_2;
      x *= inv;
      y *= inv;
    }
    return { x, y };
  }

  /** Drains queued clicks (logical canvas coordinates). */
  takeClicks() {
    const clicks = this.clicks;
    this.clicks = [];
    return clicks;
  }

  /** Call at end of each update to clear edge-triggered presses. */
  endFrame() {
    this.pressedCodes.clear();
    this.clicks.length = 0; // unconsumed clicks must not replay when a menu opens later
  }
}

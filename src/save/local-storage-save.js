// localStorage wrapper. All failures (quota, private mode, corrupt JSON) are swallowed and
// reported as null/false so the game never crashes because of saving.
import { SAVE_KEY } from '../core/constants.js';
import { validateSave } from './save-serializer.js';

export function writeSave(saveObject) {
  try {
    localStorage.setItem(SAVE_KEY, JSON.stringify(saveObject));
    return true;
  } catch (err) {
    console.warn('Save failed:', err);
    return false;
  }
}

/** Returns a validated save object or null when none/invalid. */
export function readSave() {
  try {
    const raw = localStorage.getItem(SAVE_KEY);
    if (!raw) return null;
    return validateSave(JSON.parse(raw));
  } catch (err) {
    console.warn('Load failed:', err);
    return null;
  }
}

export function clearSave() {
  try {
    localStorage.removeItem(SAVE_KEY);
  } catch (err) {
    console.warn('Clear save failed:', err);
  }
}

export function hasSave() {
  return readSave() !== null;
}

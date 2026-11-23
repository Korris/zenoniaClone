import { ApiError } from '../errors.js';
import cardTable from './card-ids.json' with { type: 'json' };
import { CARD_MAX_STACK } from './rpg-constants.js';
import { stateSchema } from './item-validation.js';

// Card Wager rules (mirror Assets/Scripts/Rpg/Cards/WagerRules.cs on the zone side). The API checks them again: it never trusts the zone on rarity / boss / spare copies.
export const WAGER_MIN_LEVEL = 30;
export const WAGER_MIN_ACCOUNT_AGE_DAYS = 7;
export const WAGER_MAX_PER_DAY = 3; // wager matches per account per UTC day
export const WAGER_MAX_PER_PAIR_DAY = 2; // between the same two accounts per UTC day
const WAGERABLE = new Set(['N', 'R']);

const refuse = (code, message) => new ApiError(422, code, message);
const info = new Map(cardTable.map((c, i) => [c.id, { ...c, index: i }]));

/** Copies that may be wagered: none when the card kind is locked (Lock Card is a flag per card id), else owned - copies in the deck (CardCollection.Spare). */
export function spareCopies(state, cardId) {
  const e = state.cards.find((c) => c.id === cardId);
  if (!e) return 0;
  if (e.locked > 0) return 0;
  return Math.max(0, e.n - state.cardDeck.filter((d) => d === cardId).length);
}

/** Rarity ('N' / 'R') of a wagerable card. Throws 422 for an unknown card, a boss, SR or SSR. */
export function wagerRarity(cardId) {
  const c = info.get(cardId);
  if (!c) throw refuse('wager_unknown_card', 'Unknown card');
  if (c.isBoss) throw refuse('wager_boss_card', 'A boss card cannot be wagered');
  if (!WAGERABLE.has(c.rarity)) throw refuse('wager_rarity', 'Only N and R cards can be wagered');
  return c.rarity;
}

/** One side's own checks before the card leaves it. */
export function checkWagerSide(state, cardId) {
  wagerRarity(cardId);
  if (state.level < WAGER_MIN_LEVEL) throw refuse('wager_level', `Card Wager needs level ${WAGER_MIN_LEVEL}`);
  if (spareCopies(state, cardId) < 1) throw refuse('wager_no_spare', 'No spare copy of that card (locked, in the deck or not owned)');
}

/** Whether one more copy of `cardId` fits the stack. */
export const canReceive = (state, cardId) => (state.cards.find((c) => c.id === cardId)?.n ?? 0) < CARD_MAX_STACK;

/** Takes one copy out of the state (the Album keeps the card). Works on a clone; the result must pass the whole-state validation. */
export function takeWagerCard(state, cardId) {
  checkWagerSide(state, cardId);
  const next = structuredClone(state);
  const i = next.cards.findIndex((c) => c.id === cardId);
  if (next.cards[i].n === 1) next.cards.splice(i, 1);
  else next.cards[i].n -= 1;
  return validate(next);
}

/** Adds one copy of each id in `cardIds` (a win gives two, a refund one). Clone; the Album learns the card. Reports copies lost to the 999 cap. */
export function giveWagerCards(state, cardIds) {
  const next = structuredClone(state);
  let overflow = 0;
  for (const id of cardIds) {
    const e = next.cards.find((c) => c.id === id);
    if (!e) next.cards.push({ id, n: 1, locked: 0 });
    else if (e.n >= CARD_MAX_STACK) overflow += 1;
    else e.n += 1;
    const idx = info.get(id).index;
    if (!next.album.includes(idx)) next.album.push(idx);
  }
  return { state: validate(next), overflow };
}

function validate(state) {
  const r = stateSchema.safeParse(state);
  if (!r.success) throw refuse('wager_invalid', r.error.issues[0]?.message ?? 'The wager would break a character');
  return r.data;
}

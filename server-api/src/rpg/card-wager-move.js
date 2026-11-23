import { ApiError } from '../errors.js';
import cardTable from './card-ids.json' with { type: 'json' };
import { CARD_DECK_MAX, CARD_MAX_STACK } from './rpg-constants.js';
import { stateSchema } from './item-validation.js';

// Card Wager v2 (user rule 23/11): EVERY casual PvP duel stakes both whole 8-card decks; the winner takes any 1 card of the loser's deck.
// Mirror of Assets/Scripts/Rpg/Cards/WagerRules.cs on the zone side. The API checks it again: it never trusts the zone on level, deck shape or copies owned.
export const WAGER_MIN_LEVEL = 30;
export const WAGER_MIN_ACCOUNT_AGE_DAYS = 7;
export const WAGER_MAX_PER_DAY = 10; // PvP card duels per account per UTC day
export const WAGER_MAX_PER_PAIR_DAY = 2; // between the same two accounts per UTC day
const MAX_BOSS = 1;
const MAX_SR_PLUS = 2; // SR / SSR cards that are not bosses

const refuse = (code, message) => new ApiError(422, code, message);
const info = new Map(cardTable.map((c, i) => [c.id, { ...c, index: i }]));
const RANK = { N: 0, R: 1, SR: 2, SSR: 3 };

/** The deck of a side: exactly 8 known cards, within the deck rules, every copy owned. Throws 422 otherwise. */
export function checkDeckSide(state) {
  if (state.level < WAGER_MIN_LEVEL) throw refuse('wager_level', `Card duel needs level ${WAGER_MIN_LEVEL}`);
  const deck = state.cardDeck;
  if (deck.length !== CARD_DECK_MAX) throw refuse('wager_deck', `The deck needs ${CARD_DECK_MAX} cards`);
  const used = new Map();
  let boss = 0;
  let srPlus = 0;
  for (const id of deck) {
    const c = info.get(id);
    if (!c) throw refuse('wager_unknown_card', 'Unknown card in the deck');
    used.set(id, (used.get(id) ?? 0) + 1);
    if (c.isBoss) boss += 1;
    else if (RANK[c.rarity] >= RANK.SR) srPlus += 1;
  }
  if (boss > MAX_BOSS || srPlus > MAX_SR_PLUS) throw refuse('wager_deck', 'The deck breaks the Boss / SR rules');
  for (const [id, n] of used) {
    if ((state.cards.find((e) => e.id === id)?.n ?? 0) < n) throw refuse('wager_deck', 'The deck uses a card that is not owned');
  }
}

/**
 * Takes the whole deck out of the collection (the Album keeps the cards; `state.cardDeck` stays as it is so the return can rebuild it).
 * Returns the new state plus `lockedIds`: deck kinds that were Locked, restored when the copies come back.
 */
export function takeDeck(state) {
  checkDeckSide(state);
  const next = structuredClone(state);
  const lockedIds = [];
  for (const id of state.cardDeck) {
    const i = next.cards.findIndex((c) => c.id === id);
    if (next.cards[i].locked > 0 && !lockedIds.includes(id)) lockedIds.push(id);
    if (next.cards[i].n === 1) next.cards.splice(i, 1);
    else next.cards[i].n -= 1;
  }
  return { state: validate(next), lockedIds };
}

/**
 * Adds one copy of each id in `cardIds`; a kind that comes back after its last copy left gets its Lock flag again when it is in `lockedIds`.
 * The deck is then trimmed to the copies owned (the loser's deck loses the card that was taken). Clone; the Album learns new cards. Reports copies lost to the 999 cap.
 */
export function giveWagerCards(state, cardIds, lockedIds = []) {
  const next = structuredClone(state);
  let overflow = 0;
  for (const id of cardIds) {
    const e = next.cards.find((c) => c.id === id);
    if (!e) next.cards.push({ id, n: 1, locked: lockedIds.includes(id) ? 1 : 0 });
    else if (e.n >= CARD_MAX_STACK) overflow += 1;
    else e.n += 1;
    const idx = info.get(id).index;
    if (!next.album.includes(idx)) next.album.push(idx);
  }
  const left = new Map(next.cards.map((c) => [c.id, c.n]));
  next.cardDeck = next.cardDeck.filter((id) => {
    const n = left.get(id) ?? 0;
    if (n < 1) return false;
    left.set(id, n - 1);
    return true;
  });
  return { state: validate(next), overflow };
}

function validate(state) {
  const r = stateSchema.safeParse(state);
  if (!r.success) throw refuse('wager_invalid', r.error.issues[0]?.message ?? 'The wager would break a character');
  return r.data;
}

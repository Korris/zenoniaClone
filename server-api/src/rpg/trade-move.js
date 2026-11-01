import { ApiError } from '../errors.js';
import { MAX_INT } from './rpg-constants.js';
import { stateSchema } from './item-validation.js';

const refuse = (code, message) => new ApiError(422, code, message);
const STARTER_PREFIX = 'starter-'; // TradeBook.StarterUidPrefix: every character has gear under these uids

/** Takes `uids` out of the giver's bag (never from what it wears). Returns the taken items. */
function takeItems(state, uids) {
  if (new Set(uids).size !== uids.length) throw refuse('trade_duplicate_item', 'The same item is offered twice');
  return uids.map((uid) => {
    if (uid.startsWith(STARTER_PREFIX)) throw refuse('trade_starter', 'Starter gear cannot be traded');
    const i = state.inventory.findIndex((it) => it.uid === uid);
    if (i < 0) throw refuse('trade_item_missing', `Item ${uid} is not in the bag`);
    return state.inventory.splice(i, 1)[0];
  });
}

/**
 * Both sides after the trade, from each side's pre-trade state. Pure, works on clones. The result must pass the whole-state
 * validation (bag size, duplicate uids), so a trade that would break a save can never be written. Throws 422 otherwise.
 */
export function moveTradeGoods(a, b) {
  const nextA = structuredClone(a.state);
  const nextB = structuredClone(b.state);
  if (a.giveUids.length + b.giveUids.length === 0 && a.giveGold + b.giveGold === 0) throw refuse('trade_empty', 'Nothing to trade');
  if (a.giveGold > nextA.gold || b.giveGold > nextB.gold) throw refuse('insufficient_funds', 'Not enough gold');
  const aGave = takeItems(nextA, a.giveUids);
  const bGave = takeItems(nextB, b.giveUids);
  nextA.inventory.push(...bGave);
  nextB.inventory.push(...aGave);
  nextA.gold += b.giveGold - a.giveGold;
  nextB.gold += a.giveGold - b.giveGold;
  if (nextA.gold > MAX_INT || nextB.gold > MAX_INT) throw refuse('trade_gold_overflow', 'Too much gold');
  const check = (state) => {
    const r = stateSchema.safeParse(state);
    if (!r.success) throw refuse('trade_invalid', r.error.issues[0]?.message ?? 'The trade would break a character');
    return r.data;
  };
  return { a: check(nextA), b: check(nextB), aGave, bGave };
}

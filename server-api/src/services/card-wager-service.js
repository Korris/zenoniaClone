import { ApiError, conflict, forbidden, notFound } from '../errors.js';
import {
  canReceive, checkWagerSide, giveWagerCards, takeWagerCard, wagerRarity,
  WAGER_MAX_PER_DAY, WAGER_MAX_PER_PAIR_DAY, WAGER_MIN_ACCOUNT_AGE_DAYS,
} from '../rpg/card-wager-move.js';
import { toObjectId } from './character-service.js';

const DAY_MS = 86_400_000;
const refuse = (code, message) => new ApiError(422, code, message);
const rev409 = (currentRev) => conflict('rev_conflict', 'Character changed since the given rev', { currentRev });
const other = (side) => (side === 'a' ? 'b' : 'a');

/**
 * Card Wager escrow. A wagered card leaves BOTH characters (one MongoDB transaction, like commitTrade) before the duel starts and sits
 * in `card_wagers{_id=matchId}` as status 'held'. Afterwards exactly one of: settle (winner gets both, draw gives each its own back; idempotent
 * per matchId) or refund (expired / zone restarted, each side gets its own back). Every move is a `ledger` entry (type 'card_wager').
 * Cards only ever move between a character and an escrow row, so the total over characters + held rows never changes.
 */
export function createCardWagerService({ db, cfg, now, characters }) {
  const col = db.collection('characters');
  const wagers = db.collection('card_wagers');
  const ledger = db.collection('ledger');
  const users = db.collection('users');
  const utcDayStart = () => new Date(Math.floor(now() / DAY_MS) * DAY_MS);

  const ledgerRow = (w, action, side, extra = {}) => ({
    type: 'card_wager', action, matchId: w._id, at: new Date(now()),
    characterId: w[side].characterId, userId: w[side].userId,
    otherCharacterId: w[other(side)].characterId, otherUserId: w[other(side)].userId,
    cardId: w[side].cardId, otherCardId: w[other(side)].cardId, ...extra,
  });

  /** Writes a new state + rev bump. `liveSession` = the lock the caller holds (online character), null = the character has no live session. */
  async function writeState(doc, state, expectRev, liveSession, session) {
    const filter = { _id: doc._id, rev: expectRev, ...(liveSession ? { 'session.id': liveSession } : {}) };
    const set = { state: { ...state, savedAt: now() }, ...(liveSession ? { 'session.at': new Date(now()) } : {}) };
    const r = await col.updateOne(filter, { $set: set, $inc: { rev: 1 } }, { session });
    if (r.modifiedCount !== 1) throw rev409(doc.rev);
    return expectRev + 1;
  }

  /** Per-account daily limits (UTC day), counted from the escrow rows themselves. Wagers that were refunded never ran and do not count. */
  async function checkLimits(w, session) {
    const since = { createdAt: { $gte: utcDayStart() }, status: { $in: ['held', 'settled'] } };
    for (const side of ['a', 'b']) {
      const userId = w[side].userId;
      const n = await wagers.countDocuments({ ...since, $or: [{ 'a.userId': userId }, { 'b.userId': userId }] }, { session });
      if (n >= WAGER_MAX_PER_DAY) throw refuse('wager_daily_limit', `At most ${WAGER_MAX_PER_DAY} wager matches per day`);
    }
    const pair = await wagers.countDocuments({
      ...since, $or: [{ 'a.userId': w.a.userId, 'b.userId': w.b.userId }, { 'a.userId': w.b.userId, 'b.userId': w.a.userId }],
    }, { session });
    if (pair >= WAGER_MAX_PER_PAIR_DAY) throw refuse('wager_pair_limit', `At most ${WAGER_MAX_PER_PAIR_DAY} wager matches per day against the same player`);
  }

  async function checkAccountAge(userId, session) {
    const u = await users.findOne({ _id: toObjectId(userId) }, { projection: { createdAt: 1 }, session });
    if (!u?.createdAt || now() - u.createdAt.getTime() < WAGER_MIN_ACCOUNT_AGE_DAYS * DAY_MS) {
      throw refuse('wager_account_age', `The account must be at least ${WAGER_MIN_ACCOUNT_AGE_DAYS} days old`);
    }
  }

  /** The credited characters as stored now (a replay: the zone adopts the rev + state when its own rev is older). */
  async function creditedNow(w, session) {
    const out = [];
    for (const c of w.credited ?? []) {
      const doc = await col.findOne({ _id: toObjectId(c.characterId) }, { session });
      if (doc) out.push({ side: c.side, characterId: c.characterId, cards: c.cards, rev: doc.rev, state: doc.state });
    }
    return out;
  }

  return {
    /**
     * Takes the wagered card of both sides out of their states. a / b = { characterId, sessionId, rev, state, cardId }.
     * -> { replayed, status, a:{rev,state}, b:{rev,state} }. Idempotent per matchId. `guard(oldState, newState)` runs like in a save.
     */
    async escrow(matchId, a, b, guard) {
      if (!cfg.CARD_WAGER_ENABLED) throw forbidden('wager_disabled', 'Card Wager is switched off');
      if (a.characterId === b.characterId) throw refuse('wager_self', 'A character cannot wager against itself');
      return db.client.withSession((session) => session.withTransaction(async () => {
        const prior = await wagers.findOne({ _id: matchId }, { session });
        const load = async (side) => {
          const doc = await col.findOne({ _id: toObjectId(side.characterId) }, { session });
          if (!doc) throw notFound('Character');
          if (!prior && (!doc.session || doc.session.id !== side.sessionId)) throw conflict('session_mismatch', 'This session does not own the character');
          return doc;
        };
        const docA = await load(a);
        const docB = await load(b);
        if (prior) {
          if (prior.a.characterId !== a.characterId || prior.b.characterId !== b.characterId) throw conflict('wager_match_conflict', 'matchId belongs to another match');
          return { replayed: true, status: prior.status, a: { rev: docA.rev, state: docA.state }, b: { rev: docB.rev, state: docB.state } };
        }
        if (String(docA.userId) === String(docB.userId)) throw refuse('wager_self', 'Both characters belong to one account');
        for (const [side, doc] of [[a, docA], [b, docB]]) {
          if (doc.rev !== side.rev) throw rev409(doc.rev);
          if (side.state.classId !== doc.classId) throw refuse('class_mismatch', 'classId cannot change');
          guard?.(doc.state, side.state);
          checkWagerSide(side.state, side.cardId);
          await checkAccountAge(doc.userId, session);
        }
        if (wagerRarity(a.cardId) !== wagerRarity(b.cardId)) throw refuse('wager_rarity_mismatch', 'Both cards must have the same rarity');
        const nextA = takeWagerCard(a.state, a.cardId);
        const nextB = takeWagerCard(b.state, b.cardId);
        if (!canReceive(nextA, b.cardId) || !canReceive(nextB, a.cardId)) throw refuse('wager_stack_full', 'A side could not hold the other card (999 copies)');
        const at = now();
        const w = {
          _id: matchId, status: 'held', zoneId: cfg.ZONE_ID, createdAt: new Date(at), expiresAt: new Date(at + cfg.CARD_WAGER_HOLD_SEC * 1000),
          a: { characterId: a.characterId, userId: docA.userId, cardId: a.cardId },
          b: { characterId: b.characterId, userId: docB.userId, cardId: b.cardId },
        };
        await checkLimits(w, session);
        const revA = await writeState(docA, nextA, a.rev, a.sessionId, session);
        const revB = await writeState(docB, nextB, b.rev, b.sessionId, session);
        await wagers.insertOne(w, { session });
        await ledger.insertMany([ledgerRow(w, 'escrow', 'a', { rev: revA }), ledgerRow(w, 'escrow', 'b', { rev: revB })], { session });
        return { replayed: false, status: 'held', a: { rev: revA, state: nextA }, b: { rev: revB, state: nextB } };
      }));
    },

    /**
     * Ends a held wager. result 'a' | 'b' = that side wins both cards, 'draw' = each side gets its own back (also how the zone cancels a match that never started).
     * `sides.a / sides.b` = { sessionId, rev, state } of a receiver that is ONLINE on the zone (its current state); a receiver left out must have no live session
     * (the API then writes the character document itself), otherwise 409 receiver_online and the zone retries. Still works with the switch off: held cards must come back.
     * -> { replayed, status:'settled', result, credited:[{side, characterId, cards, rev, state}] }.
     */
    async settle(matchId, result, sides, guard) {
      return db.client.withSession((session) => session.withTransaction(async () => {
        const w = await wagers.findOne({ _id: matchId }, { session });
        if (!w) throw notFound('Card wager');
        if (w.status === 'settled') {
          if (w.result !== result) throw conflict('wager_already_settled', `Already settled as ${w.result}`);
          return { replayed: true, status: 'settled', result, credited: await creditedNow(w, session) };
        }
        if (w.status !== 'held') throw conflict('wager_refunded', 'The wager was refunded');
        const plan = result === 'draw'
          ? [{ side: 'a', cards: [w.a.cardId] }, { side: 'b', cards: [w.b.cardId] }]
          : [{ side: result, cards: [w.a.cardId, w.b.cardId] }];
        const credited = [];
        const rows = [];
        for (const p of plan) {
          const ref = w[p.side];
          const doc = await col.findOne({ _id: toObjectId(ref.characterId) }, { session });
          if (!doc) throw notFound('Character');
          const sup = sides?.[p.side];
          let base = doc.state;
          if (sup) {
            if (!doc.session || doc.session.id !== sup.sessionId) throw conflict('session_mismatch', 'This session does not own the character');
            if (doc.rev !== sup.rev) throw rev409(doc.rev);
            if (sup.state.classId !== doc.classId) throw refuse('class_mismatch', 'classId cannot change');
            guard?.(doc.state, sup.state);
            base = sup.state;
          } else if (characters.live(doc)) {
            throw conflict('receiver_online', 'The receiver is online: send its current state');
          }
          const given = giveWagerCards(base, p.cards);
          const rev = await writeState(doc, given.state, doc.rev, sup ? sup.sessionId : null, session);
          credited.push({ side: p.side, characterId: ref.characterId, cards: p.cards, rev, state: given.state });
          rows.push(ledgerRow(w, 'settle', p.side, { result, gave: p.cards, rev, ...(given.overflow ? { overflow: given.overflow } : {}) }));
        }
        const r = await wagers.updateOne({ _id: matchId, status: 'held' }, {
          $set: { status: 'settled', result, settledAt: new Date(now()), credited: credited.map(({ side, characterId, cards }) => ({ side, characterId, cards })) },
        }, { session });
        if (r.modifiedCount !== 1) throw conflict('wager_conflict', 'The wager changed meanwhile');
        await ledger.insertMany(rows, { session });
        return { replayed: false, status: 'settled', result, credited };
      }));
    },

    /** Gives both sides of one held wager their own card back. Neither side may have a live session, else it stays held for the next sweep. */
    async refundOne(matchId, reason) {
      return db.client.withSession((session) => session.withTransaction(async () => {
        const w = await wagers.findOne({ _id: matchId, status: 'held' }, { session });
        if (!w) return false;
        const docs = await Promise.all(['a', 'b'].map((s) => col.findOne({ _id: toObjectId(w[s].characterId) }, { session })));
        if (docs.some((d) => !d || characters.live(d))) return false;
        const rows = [];
        for (const [i, s] of ['a', 'b'].entries()) {
          const given = giveWagerCards(docs[i].state, [w[s].cardId]);
          const rev = await writeState(docs[i], given.state, docs[i].rev, null, session);
          rows.push(ledgerRow(w, 'refund', s, { reason, rev, ...(given.overflow ? { overflow: given.overflow } : {}) }));
        }
        await wagers.updateOne({ _id: matchId, status: 'held' }, { $set: { status: 'refunded', refundedAt: new Date(now()), reason } }, { session });
        await ledger.insertMany(rows, { session });
        return true;
      }));
    },

    /** Refunds every wager of this character whose hold ran out. Call at redeem BEFORE the session lock, so the zone loads the card back. Returns how many. */
    async refundExpiredFor(characterId) {
      const held = await wagers.find({
        status: 'held', expiresAt: { $lte: new Date(now()) }, $or: [{ 'a.characterId': characterId }, { 'b.characterId': characterId }],
      }, { projection: { _id: 1 } }).toArray();
      let n = 0;
      for (const w of held) if (await this.refundOne(w._id, 'expired')) n++;
      return n;
    },

    /** Refunds every held wager of a zone that just restarted (its duels are gone). Call after the zone's session locks were released. */
    async refundZone(zoneId) {
      const held = await wagers.find({ status: 'held', zoneId }, { projection: { _id: 1 } }).toArray();
      let n = 0;
      for (const w of held) if (await this.refundOne(w._id, 'zone_reset')) n++;
      return n;
    },
  };
}

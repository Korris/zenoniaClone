import { ApiError, conflict, forbidden, notFound } from '../errors.js';
import {
  giveWagerCards, takeDeck,
  WAGER_MAX_PER_DAY, WAGER_MAX_PER_PAIR_DAY, WAGER_MIN_ACCOUNT_AGE_DAYS,
} from '../rpg/card-wager-move.js';
import { toObjectId } from './character-service.js';

const DAY_MS = 86_400_000;
const refuse = (code, message) => new ApiError(422, code, message);
// `side` ('a' | 'b') tells the zone WHICH of the two characters of the call is out of step (it holds both, and must not drop the wrong one)
const rev409 = (currentRev, side) => conflict('rev_conflict', 'Character changed since the given rev', { currentRev, ...(side ? { side } : {}) });
const session409 = (side) => conflict('session_mismatch', 'This session does not own the character', side ? { side } : undefined);
const other = (side) => (side === 'a' ? 'b' : 'a');

/**
 * Card Wager v2 escrow. The WHOLE 8-card deck of BOTH characters leaves them (one MongoDB transaction, like commitTrade) before the duel starts and sits
 * in `card_wagers{_id=matchId}` as status 'held'. Afterwards exactly one of: settle (the winner gets its own 8 back plus ONE card it picks from the loser's deck,
 * the loser gets 7 back; a draw returns everything; idempotent per matchId) or refund (expired / zone restarted: each side gets its own 8 back).
 * Every move is a `ledger` entry (type 'card_wager'). Cards only ever move between a character and an escrow row (and from the loser's row to the winner),
 * so the total over characters + held rows never changes.
 */
export function createCardWagerService({ db, cfg, now, characters }) {
  const col = db.collection('characters');
  const wagers = db.collection('card_wagers');
  const ledger = db.collection('ledger');
  const users = db.collection('users');
  const counters = db.collection('card_wager_counters');
  const dayOf = (ms) => Math.floor(ms / DAY_MS);

  const ledgerRow = (w, action, side, extra = {}) => ({
    type: 'card_wager', action, matchId: w._id, at: new Date(now()),
    characterId: w[side].characterId, userId: w[side].userId,
    otherCharacterId: w[other(side)].characterId, otherUserId: w[other(side)].userId,
    deck: w[side].deck, ...extra,
  });

  /** Writes a new state + rev bump. `liveSession` = the lock the caller holds (online character), null = the character has no live session. */
  async function writeState(doc, state, expectRev, liveSession, session) {
    const filter = { _id: doc._id, rev: expectRev, ...(liveSession ? { 'session.id': liveSession } : {}) };
    const set = { state: { ...state, savedAt: now() }, ...(liveSession ? { 'session.at': new Date(now()) } : {}) };
    const r = await col.updateOne(filter, { $set: set, $inc: { rev: 1 } }, { session });
    if (r.modifiedCount !== 1) throw rev409(doc.rev);
    return expectRev + 1;
  }

  // Per-account / per-pair daily limits (UTC day). One counter document per key and day, bumped with $inc INSIDE the escrow transaction: two escrows of the same account that run
  // at the same time write the same document, so MongoDB makes one of them retry and it sees the other's count (a plain countDocuments let both pass: write skew).
  const userKey = (userId, day) => `u:${userId}:${day}`;
  const pairKey = (u1, u2, day) => `p:${[String(u1), String(u2)].sort().join(':')}:${day}`;

  async function bump(key, max, code, message, session) {
    let r;
    try {
      r = await counters.findOneAndUpdate({ _id: key }, { $inc: { n: 1 }, $setOnInsert: { at: new Date(now()) } }, { upsert: true, returnDocument: 'after', session });
    } catch (e) {
      if (e?.code === 11000) e.addErrorLabel?.('TransientTransactionError'); // two first-of-the-day upserts met: the loser runs again and finds the document
      throw e;
    }
    if (r.n > max) throw refuse(code, message); // throwing aborts the transaction, so the increments of this attempt are rolled back
  }

  async function checkLimits(w, session) {
    const day = dayOf(now());
    for (const side of ['a', 'b']) await bump(userKey(w[side].userId, day), WAGER_MAX_PER_DAY, 'wager_daily_limit', `At most ${WAGER_MAX_PER_DAY} card duels per day`, session);
    await bump(pairKey(w.a.userId, w.b.userId, day), WAGER_MAX_PER_PAIR_DAY, 'wager_pair_limit', `At most ${WAGER_MAX_PER_PAIR_DAY} card duels per day against the same player`, session);
  }

  /** A refunded match never ran: it gives its slots back (the day it was made on). */
  async function releaseLimits(w, session) {
    const day = dayOf(w.createdAt.getTime());
    for (const side of ['a', 'b']) await counters.updateOne({ _id: userKey(w[side].userId, day) }, { $inc: { n: -1 } }, { session });
    await counters.updateOne({ _id: pairKey(w.a.userId, w.b.userId, day) }, { $inc: { n: -1 } }, { session });
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

  /** Cards each side gets at the end: own deck back, the winner plus the picked card, the loser minus it. Throws 422 on a pick that is not allowed. */
  function planSettle(w, result, pick) {
    if (result === 'draw') {
      if (pick) throw refuse('wager_pick_invalid', 'A draw takes no card');
      return { a: [...w.a.deck], b: [...w.b.deck] };
    }
    const loser = other(result);
    if (!pick) throw refuse('wager_pick_required', 'The winner must pick a card of the loser deck');
    const i = w[loser].deck.indexOf(pick);
    if (i < 0) throw refuse('wager_pick_invalid', 'That card is not in the loser deck');
    const kept = w[loser].deck.filter((_, k) => k !== i);
    return { [result]: [...w[result].deck, pick], [loser]: kept };
  }

  return {
    /**
     * Takes the whole deck of both sides out of their states. a / b = { characterId, sessionId, rev, state } (state.cardDeck = the staked deck).
     * -> { replayed, status, a:{rev,state}, b:{rev,state} }. Idempotent per matchId. `guard(oldState, newState)` runs like in a save.
     */
    async escrow(matchId, a, b, guard) {
      if (a.characterId === b.characterId) throw refuse('wager_self', 'A character cannot duel itself');
      return db.client.withSession((session) => session.withTransaction(async () => {
        const prior = await wagers.findOne({ _id: matchId }, { session });
        const load = async (side) => {
          const doc = await col.findOne({ _id: toObjectId(side.characterId) }, { session });
          if (!doc) throw notFound('Character');
          if (!prior && (!doc.session || doc.session.id !== side.sessionId)) throw session409(side === a ? 'a' : 'b');
          return doc;
        };
        const docA = await load(a);
        const docB = await load(b);
        if (prior) {
          if (prior.a.characterId !== a.characterId || prior.b.characterId !== b.characterId) throw conflict('wager_match_conflict', 'matchId belongs to another match');
          return { replayed: true, status: prior.status, a: { rev: docA.rev, state: docA.state }, b: { rev: docB.rev, state: docB.state } };
        }
        // The switch only stops NEW matches: a replay (above) of a match that already exists is answered even when it was turned off in between.
        if (!cfg.CARD_WAGER_ENABLED) throw forbidden('wager_disabled', 'Card Wager is switched off');
        if (String(docA.userId) === String(docB.userId)) throw refuse('wager_self', 'Both characters belong to one account');
        for (const [side, doc] of [[a, docA], [b, docB]]) {
          if (doc.rev !== side.rev) throw rev409(doc.rev, side === a ? 'a' : 'b');
          if (side.state.classId !== doc.classId) throw refuse('class_mismatch', 'classId cannot change');
          guard?.(doc.state, side.state);
          await checkAccountAge(doc.userId, session);
        }
        const takenA = takeDeck(a.state);
        const takenB = takeDeck(b.state);
        const at = now();
        const w = {
          _id: matchId, status: 'held', zoneId: cfg.ZONE_ID, createdAt: new Date(at), expiresAt: new Date(at + cfg.CARD_WAGER_HOLD_SEC * 1000),
          a: { characterId: a.characterId, userId: docA.userId, deck: [...a.state.cardDeck], lockedIds: takenA.lockedIds },
          b: { characterId: b.characterId, userId: docB.userId, deck: [...b.state.cardDeck], lockedIds: takenB.lockedIds },
        };
        await checkLimits(w, session);
        const revA = await writeState(docA, takenA.state, a.rev, a.sessionId, session);
        const revB = await writeState(docB, takenB.state, b.rev, b.sessionId, session);
        await wagers.insertOne(w, { session });
        await ledger.insertMany([ledgerRow(w, 'escrow', 'a', { rev: revA }), ledgerRow(w, 'escrow', 'b', { rev: revB })], { session });
        return { replayed: false, status: 'held', a: { rev: revA, state: takenA.state }, b: { rev: revB, state: takenB.state } };
      }));
    },

    /**
     * Ends a held match. result 'a' | 'b' = that side wins and takes `pick` (a card id of the loser's staked deck); 'draw' = everybody gets the own deck back (also how the zone
     * cancels a match that never started). `sides.a / sides.b` = { sessionId, rev, state } of a receiver that is ONLINE on the zone (its current state); a receiver left out must have
     * no live session (the API then writes the character document itself), otherwise 409 receiver_online and the zone retries. Still works with the switch off: held cards must come back.
     * -> { replayed, status:'settled', result, pick, credited:[{side, characterId, cards, rev, state}] } (both sides).
     */
    async settle(matchId, result, pick, sides, guard) {
      return db.client.withSession((session) => session.withTransaction(async () => {
        const w = await wagers.findOne({ _id: matchId }, { session });
        if (!w) throw notFound('Card wager');
        if (w.status === 'settled') {
          if (w.result !== result || (w.pick ?? null) !== (pick ?? null)) throw conflict('wager_already_settled', `Already settled as ${w.result}${w.pick ? ` taking ${w.pick}` : ''}`);
          return { replayed: true, status: 'settled', result, pick: w.pick ?? null, credited: await creditedNow(w, session) };
        }
        if (w.status !== 'held') throw conflict('wager_refunded', 'The match was refunded');
        const plan = planSettle(w, result, pick);
        const credited = [];
        const rows = [];
        for (const s of ['a', 'b']) {
          const ref = w[s];
          const doc = await col.findOne({ _id: toObjectId(ref.characterId) }, { session });
          if (!doc) { // the character is gone (removed before deletion was blocked while held): nobody to give to, the other side must still get its cards
            rows.push(ledgerRow(w, 'settle_missing', s, { result, ...(pick ? { pick } : {}), lost: plan[s] }));
            continue;
          }
          const sup = sides?.[s];
          let base = doc.state;
          if (sup) {
            if (!doc.session || doc.session.id !== sup.sessionId) throw session409(s);
            if (doc.rev !== sup.rev) throw rev409(doc.rev, s);
            if (sup.state.classId !== doc.classId) throw refuse('class_mismatch', 'classId cannot change');
            guard?.(doc.state, sup.state);
            base = sup.state;
          } else if (characters.live(doc)) {
            throw conflict('receiver_online', 'The receiver is online: send its current state');
          }
          const given = giveWagerCards(base, plan[s], ref.lockedIds ?? []);
          const rev = await writeState(doc, given.state, doc.rev, sup ? sup.sessionId : null, session);
          credited.push({ side: s, characterId: ref.characterId, cards: plan[s], rev, state: given.state });
          rows.push(ledgerRow(w, 'settle', s, { result, ...(pick ? { pick } : {}), gave: plan[s], rev, ...(given.overflow ? { overflow: given.overflow } : {}) }));
        }
        const r = await wagers.updateOne({ _id: matchId, status: 'held' }, {
          $set: { status: 'settled', result, pick: pick ?? null, settledAt: new Date(now()), credited: credited.map(({ side, characterId, cards }) => ({ side, characterId, cards })) },
        }, { session });
        if (r.modifiedCount !== 1) throw conflict('wager_conflict', 'The match changed meanwhile');
        await ledger.insertMany(rows, { session });
        return { replayed: false, status: 'settled', result, pick: pick ?? null, credited };
      }));
    },

    /** Gives both sides of one held match their own deck back. Neither side may have a live session, else it stays held for the next sweep. */
    async refundOne(matchId, reason) {
      return db.client.withSession((session) => session.withTransaction(async () => {
        const w = await wagers.findOne({ _id: matchId, status: 'held' }, { session });
        if (!w) return false;
        const docs = await Promise.all(['a', 'b'].map((s) => col.findOne({ _id: toObjectId(w[s].characterId) }, { session })));
        if (docs.some((d) => d && characters.live(d))) return false;
        const rows = [];
        for (const [i, s] of ['a', 'b'].entries()) {
          if (!docs[i]) { rows.push(ledgerRow(w, 'refund_missing', s, { reason })); continue; } // that character is gone: only the side that still exists gets its deck
          const given = giveWagerCards(docs[i].state, w[s].deck, w[s].lockedIds ?? []);
          const rev = await writeState(docs[i], given.state, docs[i].rev, null, session);
          rows.push(ledgerRow(w, 'refund', s, { reason, rev, ...(given.overflow ? { overflow: given.overflow } : {}) }));
        }
        await wagers.updateOne({ _id: matchId, status: 'held' }, { $set: { status: 'refunded', refundedAt: new Date(now()), reason } }, { session });
        await releaseLimits(w, session);
        await ledger.insertMany(rows, { session });
        return true;
      }));
    },

    /** Refunds every match of this character whose hold ran out. Call at redeem BEFORE the session lock, so the zone loads the cards back. Returns how many. */
    async refundExpiredFor(characterId) {
      const held = await wagers.find({
        status: 'held', expiresAt: { $lte: new Date(now()) }, $or: [{ 'a.characterId': characterId }, { 'b.characterId': characterId }],
      }, { projection: { _id: 1 } }).toArray();
      let n = 0;
      for (const w of held) if (await this.refundOne(w._id, 'expired')) n++;
      return n;
    },

    /**
     * The periodic sweep: refunds every held match whose hold ran out (a zone that never settled it and never came back, a settle that failed for good). A side with a live session
     * keeps it held until the next sweep. One failing match never stops the others. Returns { refunded, failed }.
     */
    async sweepExpired(onError) {
      const due = await wagers.find({ status: 'held', expiresAt: { $lte: new Date(now()) } }, { projection: { _id: 1 } }).limit(200).toArray();
      let refunded = 0;
      let failed = 0;
      for (const w of due) {
        try { if (await this.refundOne(w._id, 'expired')) refunded++; } catch (e) { failed++; onError?.(e, w._id); }
      }
      return { refunded, failed };
    },

    /** Is a match of this character still held? Used to refuse deleting it (the other side's cards are in that match). */
    async hasHeld(characterId) {
      return !!(await wagers.findOne({ status: 'held', $or: [{ 'a.characterId': characterId }, { 'b.characterId': characterId }] }, { projection: { _id: 1 } }));
    },

    /** Refunds every held match of a zone that just restarted (its duels are gone). Call after the zone's session locks were released. */
    async refundZone(zoneId) {
      const held = await wagers.find({ status: 'held', zoneId }, { projection: { _id: 1 } }).toArray();
      let n = 0;
      for (const w of held) if (await this.refundOne(w._id, 'zone_reset')) n++;
      return n;
    },
  };
}

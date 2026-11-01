import { ObjectId } from 'mongodb';
import crypto from 'node:crypto';
import { ApiError, conflict, notFound } from '../errors.js';
import { CLASS_IDS } from '../rpg/rpg-constants.js';
import { newCharacterState } from '../rpg/item-validation.js';
import { moveTradeGoods } from '../rpg/trade-move.js';

const RECENT_OPS = 30; // idempotency window per character

export function toObjectId(id) {
  if (typeof id !== 'string' || !ObjectId.isValid(id) || id.length !== 24) throw notFound('Character');
  return new ObjectId(id);
}

/** Character persistence + session lock. All multi-field changes are single-document atomic updates (no transactions needed). */
export function createCharacterService({ db, now, sessionLockSec }) {
  const col = db.collection('characters');
  const live = (doc) => !!doc.session && doc.session.at.getTime() > now() - sessionLockSec * 1000;

  const summary = (doc) => ({
    id: String(doc._id), name: doc.name, classId: doc.classId, level: doc.state.level, mapId: doc.state.mapId,
    rev: doc.rev, online: live(doc), createdAt: doc.createdAt,
  });
  const full = (doc) => ({ ...summary(doc), state: doc.state });

  async function getOwned(userId, id) {
    const doc = await col.findOne({ _id: toObjectId(id), userId });
    if (!doc) throw notFound('Character'); // same answer for "not yours" and "does not exist"
    return doc;
  }

  /** Loads a character and asserts `sessionId` holds its lock (a stale/foreign session may never write). */
  async function getForSession(id, sessionId) {
    const doc = await col.findOne({ _id: toObjectId(id) });
    if (!doc) throw notFound('Character');
    if (!doc.session || doc.session.id !== sessionId) throw conflict('session_mismatch', 'This session does not own the character');
    return doc;
  }

  const rev409 = (currentRev) => conflict('rev_conflict', 'Character changed since the given rev', { currentRev });

  return {
    live, summary, full, getOwned, getForSession,

    async list(userId) {
      const docs = await col.find({ userId }).sort({ createdAt: 1 }).toArray();
      return docs.map(summary);
    },

    async create(userId, name, classId) {
      if (!CLASS_IDS.includes(classId)) throw new ApiError(400, 'validation_error', 'Unknown class');
      const doc = {
        userId, name, nameLower: name.toLowerCase(), classId, rev: 0, state: newCharacterState(classId),
        session: null, recentOps: [], createdAt: new Date(now()),
      };
      try {
        const r = await col.insertOne(doc);
        return full({ ...doc, _id: r.insertedId });
      } catch (e) {
        if (e.code !== 11000) throw e;
        if (e.keyPattern?.nameLower || /nameLower/.test(e.message)) throw conflict('name_taken', 'Character name is already taken');
        throw conflict('class_taken', 'You already have a character of this class');
      }
    },

    async remove(userId, id) {
      const doc = await getOwned(userId, id);
      if (live(doc)) throw conflict('character_online', 'Character is online');
      await col.deleteOne({ _id: doc._id, rev: doc.rev });
    },

    /** Takes the single-session lock (free, or held by a session whose heartbeat is older than the lock window). */
    async lockSession(characterId, zoneId) {
      const sessionId = crypto.randomBytes(16).toString('hex');
      const doc = await col.findOneAndUpdate(
        { _id: toObjectId(characterId), $or: [{ session: null }, { 'session.at': { $lte: new Date(now() - sessionLockSec * 1000) } }] },
        { $set: { session: { id: sessionId, zone: zoneId, at: new Date(now()) } } },
        { returnDocument: 'after' },
      );
      if (!doc) {
        if (!(await col.findOne({ _id: toObjectId(characterId) }, { projection: { _id: 1 } }))) throw notFound('Character');
        throw conflict('already_online', 'Character is already online');
      }
      return { sessionId, doc };
    },

    /** Releases the lock if `sessionId` still holds it (idempotent). */
    async endSession(characterId, sessionId) {
      const r = await col.updateOne({ _id: toObjectId(characterId), 'session.id': sessionId }, { $set: { session: null } });
      return r.modifiedCount === 1;
    },

    /**
     * Replaces the persisted state (periodic / event save). Needs the lock + the current rev; refreshes the lock heartbeat.
     * `guard(oldState, newState)` may throw to reject tampering.
     */
    async save(characterId, sessionId, rev, state, guard) {
      const doc = await getForSession(characterId, sessionId);
      if (doc.rev !== rev) throw rev409(doc.rev);
      if (state.classId !== doc.classId) throw new ApiError(422, 'class_mismatch', 'classId cannot change');
      guard?.(doc.state, state);
      const saved = { ...state, savedAt: now() };
      const r = await col.findOneAndUpdate(
        { _id: doc._id, rev, 'session.id': sessionId },
        { $set: { state: saved, 'session.at': new Date(now()) }, $inc: { rev: 1 } },
        { returnDocument: 'after' },
      );
      if (!r) throw rev409((await col.findOne({ _id: doc._id }))?.rev);
      return { rev: r.rev, savedAt: saved.savedAt };
    },

    /**
     * The two characters of one trade, both or neither, in one MongoDB transaction (needs a replica set, see README):
     * each side sends its current state (what its zone holds) and what it gives; the goods move HERE, so nothing the zone
     * says can create or drop them. Both revs must be current and both sessions must hold their locks. Idempotent per tradeId:
     * a retry after a lost answer finds the ledger entry and is told the trade happened, it can never apply twice.
     * `guard(oldState, newState)` runs on each pre-trade state, like a save.
     */
    async commitTrade(tradeId, a, b, guard) {
      const ledger = db.collection('ledger');
      const load = async (side, session) => {
        const doc = await col.findOne({ _id: toObjectId(side.characterId) }, { session });
        if (!doc) throw notFound('Character');
        if (!doc.session || doc.session.id !== side.sessionId) throw conflict('session_mismatch', 'This session does not own the character');
        return doc;
      };
      if (a.characterId === b.characterId) throw new ApiError(422, 'trade_self', 'A character cannot trade with itself');
      return db.client.withSession((session) => session.withTransaction(async () => {
        const docA = await load(a, session);
        const docB = await load(b, session);
        const prior = await ledger.findOne({ tradeId }, { session });
        if (prior) return { replayed: true, a: { rev: docA.rev }, b: { rev: docB.rev } };
        for (const [side, doc] of [[a, docA], [b, docB]]) {
          if (doc.rev !== side.rev) throw rev409(doc.rev);
          if (side.state.classId !== doc.classId) throw new ApiError(422, 'class_mismatch', 'classId cannot change');
          guard?.(doc.state, side.state);
        }
        const moved = moveTradeGoods(a, b);
        const at = now();
        const write = async (doc, side, state) => {
          const r = await col.updateOne(
            { _id: doc._id, rev: side.rev, 'session.id': side.sessionId },
            { $set: { state: { ...state, savedAt: at }, 'session.at': new Date(at) }, $inc: { rev: 1 } },
            { session },
          );
          if (r.modifiedCount !== 1) throw rev409(doc.rev);
        };
        await write(docA, a, moved.a);
        await write(docB, b, moved.b);
        await ledger.insertOne({
          tradeId, type: 'trade', at: new Date(at), characterId: String(docA._id), otherCharacterId: String(docB._id),
          userId: docA.userId, otherUserId: docB.userId,
          after: { a: { rev: a.rev + 1, gold: moved.a.gold, gave: moved.aGave, goldGiven: a.giveGold }, b: { rev: b.rev + 1, gold: moved.b.gold, gave: moved.bGave, goldGiven: b.giveGold } },
        }, { session });
        return { replayed: false, a: { rev: a.rev + 1 }, b: { rev: b.rev + 1 } };
      }));
    },

    /** A zone that restarted holds nobody any more: frees the locks it had, so its players need not wait out the 90 s heartbeat. Returns how many. */
    async releaseZoneSessions(zoneId) {
      const r = await col.updateMany({ 'session.zone': zoneId }, { $set: { session: null } });
      return r.modifiedCount;
    },

    /**
     * Idempotent atomic state transition used by inventory ops and enhance.
     * `compute(state)` returns { state, result, changed } (changed=false => nothing written, no rev bump).
     * A reqId seen before returns the stored result without re-applying (replay-safe).
     */
    async mutate(characterId, sessionId, rev, reqId, type, compute) {
      const doc = await getForSession(characterId, sessionId);
      const prior = doc.recentOps?.find((o) => o.reqId === reqId);
      if (prior) return { ...prior.result, rev: doc.rev, state: doc.state, replayed: true };
      if (doc.rev !== rev) throw rev409(doc.rev);
      const out = compute(doc.state);
      if (!out.changed) return { ...out.result, rev: doc.rev, state: doc.state, replayed: false };
      const r = await col.findOneAndUpdate(
        { _id: doc._id, rev, 'session.id': sessionId },
        {
          $set: { state: out.state, 'session.at': new Date(now()) },
          $inc: { rev: 1 },
          $push: { recentOps: { $each: [{ reqId, type, result: out.result }], $slice: -RECENT_OPS } },
        },
        { returnDocument: 'after' },
      );
      if (!r) throw rev409((await col.findOne({ _id: doc._id }))?.rev);
      await db.collection('ledger').insertOne({
        characterId: String(doc._id), userId: doc.userId, reqId, type, at: new Date(now()), before: out.before ?? null, after: out.after ?? null, result: out.result,
      });
      return { ...out.result, rev: r.rev, state: r.state, replayed: false };
    },
  };
}

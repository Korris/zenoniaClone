import { ApiError, conflict } from '../errors.js';
import { accountStateSchema, findAccountTamper, migrateAccountState, newAccountState } from '../rpg/account-validation.js';

const RECENT_OPS = 30; // idempotency window per account, same policy as character-service

/**
 * Account-level persistence (gacha pet pulls, pity, daily/weekly rewards): one document per userId,
 * separate from the character it is read/written through (a character only carries which pet it has out, petId).
 */
export function createAccountService({ db, now, characters }) {
  const col = db.collection('accounts');
  const rev409 = (currentRev) => conflict('rev_conflict', 'Account changed since the given rev', { currentRev });

  /** A stored doc, normalized to the current V2 shape (migrateAccountState handles old version 0/1 documents). */
  const normalize = (doc) => (doc ? { ...doc, state: accountStateSchema.parse(migrateAccountState(doc.state)) } : doc);

  /** Loads the account document, creating the default one on first touch (race-safe: a duplicate insert just loses the race). */
  async function getOrCreate(userId) {
    const doc = await col.findOne({ _id: userId });
    if (doc) return normalize(doc);
    const fresh = { _id: userId, rev: 0, state: newAccountState(), recentOps: [], createdAt: new Date(now()) };
    try {
      await col.insertOne(fresh);
      return fresh;
    } catch (e) {
      if (e.code !== 11000) throw e;
      return normalize(await col.findOne({ _id: userId }));
    }
  }

  /** Loads the account of the character's owner; the session lock is enforced via character-service so only the zone holding it may read/write. */
  async function getForCharacterSession(characterId, sessionId) {
    const char = await characters.getForSession(characterId, sessionId);
    const account = await getOrCreate(char.userId);
    return { char, account };
  }

  return {
    getOrCreate, getForCharacterSession,

    /**
     * Idempotent, rev-checked, anti-tamper write of the whole account state.
     * reqId seen before -> { rev: stored rev, replayed: true } without touching anything.
     */
    async write(characterId, sessionId, rev, reqId, state) {
      const { char, account } = await getForCharacterSession(characterId, sessionId);
      const prior = account.recentOps?.find((o) => o.reqId === reqId);
      if (prior) return { rev: prior.rev, replayed: true };
      if (account.rev !== rev) throw rev409(account.rev);
      const reason = findAccountTamper(account.state, state);
      if (reason) throw new ApiError(422, 'account_tamper', reason);
      const before = pickLedgerFields(account.state);
      const r = await col.findOneAndUpdate(
        { _id: account._id, rev },
        {
          $set: { state },
          $inc: { rev: 1 },
          $push: { recentOps: { $each: [{ reqId, rev: rev + 1 }], $slice: -RECENT_OPS } },
        },
        { returnDocument: 'after' },
      );
      if (!r) throw rev409((await col.findOne({ _id: account._id }))?.rev);
      await db.collection('ledger').insertOne({
        type: 'account', userId: char.userId, characterId: String(char._id), reqId, at: new Date(now()),
        before, after: pickLedgerFields(state),
      });
      return { rev: r.rev, replayed: false };
    },
  };
}

/** The currency/pity fields worth auditing on every account write. */
function pickLedgerFields(state) {
  const { tickets, crystals, urShards, sharedShards, pitySSR, pityUR, totalPulls } = state;
  return { tickets, crystals, urShards, sharedShards, pitySSR, pityUR, totalPulls };
}

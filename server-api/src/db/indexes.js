/**
 * Collections:
 *  users{usernameLower uniq, emailLower uniq, passHash, failedLogins, lockUntil}
 *  refresh_tokens{_id=sha256(token), userId, family, expiresAt(TTL), revoked}
 *  password_resets{_id=sha256(token), userId, createdAt, expiresAt(TTL)}
 *  characters{userId, classId, nameLower uniq, rev, state(SaveData shape), session, recentOps}; an account may hold several heroes of one
 *    class (2026-11-12: the old unique (userId, classId) index is dropped at boot), the count is capped in character-service (MAX_CHARACTERS)
 *    state.wantedDay: WantedBoss.DayKey of the last daily Wanted Boss claim (0 = never); kept from going backwards by rpg/wanted-day.js, no index
 *  tickets{_id=sha256(ticket), characterId, userId, expiresAt(TTL)}
 *  ledger{characterId, reqId, type, ...} audit trail of economy ops; a trade entry carries a unique tradeId, which is what makes a commit idempotent
 */
export async function ensureIndexes(db) {
  await db.collection('users').createIndex({ usernameLower: 1 }, { unique: true });
  await db.collection('users').createIndex({ emailLower: 1 }, { unique: true });
  await db.collection('refresh_tokens').createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
  await db.collection('refresh_tokens').createIndex({ userId: 1 });
  await db.collection('password_resets').createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
  await db.collection('password_resets').createIndex({ userId: 1 });
  await db.collection('characters').createIndex({ nameLower: 1 }, { unique: true });
  // One hero per class is no longer a rule: drop the unique index an older deployment made (absent on a fresh database).
  await db.collection('characters').dropIndex('userId_1_classId_1').catch((e) => { if (e.codeName !== 'IndexNotFound' && e.codeName !== 'NamespaceNotFound') throw e; });
  await db.collection('characters').createIndex({ userId: 1 });
  await db.collection('tickets').createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
  await db.collection('ledger').createIndex({ characterId: 1, at: -1 });
  await db.collection('ledger').createIndex({ tradeId: 1 }, { unique: true, partialFilterExpression: { tradeId: { $type: 'string' } } });
}

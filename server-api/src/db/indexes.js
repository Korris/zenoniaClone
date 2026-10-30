/**
 * Collections:
 *  users{usernameLower uniq, emailLower uniq, passHash, failedLogins, lockUntil}
 *  refresh_tokens{_id=sha256(token), userId, family, expiresAt(TTL), revoked}
 *  password_resets{_id=sha256(token), userId, expiresAt(TTL)}
 *  characters{userId, classId, nameLower uniq, (userId,classId) uniq, rev, state(SaveData shape), session, recentOps}
 *  tickets{_id=sha256(ticket), characterId, userId, expiresAt(TTL)}
 *  ledger{characterId, reqId, type, ...} audit trail of economy ops
 */
export async function ensureIndexes(db) {
  await db.collection('users').createIndex({ usernameLower: 1 }, { unique: true });
  await db.collection('users').createIndex({ emailLower: 1 }, { unique: true });
  await db.collection('refresh_tokens').createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
  await db.collection('refresh_tokens').createIndex({ userId: 1 });
  await db.collection('password_resets').createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
  await db.collection('characters').createIndex({ nameLower: 1 }, { unique: true });
  await db.collection('characters').createIndex({ userId: 1, classId: 1 }, { unique: true });
  await db.collection('tickets').createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
  await db.collection('ledger').createIndex({ characterId: 1, at: -1 });
}

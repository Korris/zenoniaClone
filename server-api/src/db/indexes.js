/**
 * Collections:
 *  users{usernameLower uniq, emailLower uniq, passHash, failedLogins, lockUntil}
 *  refresh_tokens{_id=sha256(token), userId, family, expiresAt(TTL), revoked}
 *  password_resets{_id=sha256(token), userId, createdAt, expiresAt(TTL)}
 *  characters{userId, slot (uniq per user), classId, nameLower uniq, rev, state(SaveData shape), session, recentOps}; slot = place on the character select screen
 *    (0..MAX_CHARACTERS-1, older heroes get one on the next list); an account may hold several heroes of one
 *    class (2026-11-12: the old unique (userId, classId) index is dropped at boot), the count is capped in character-service (MAX_CHARACTERS)
 *    state.cards / gems / album / albumClaimed / cardDeck / cardDaily / cardNpcFirstWins / cardRank: monster card collection (save v6, rpg/item-validation.js); item.sockets: socketed Soul Gems; no index
 *    state.wantedDay: WantedBoss.DayKey of the last daily Wanted Boss claim (0 = never); kept from going backwards by rpg/wanted-day.js, no index
 *  tickets{_id=sha256(ticket), characterId, userId, expiresAt(TTL)}
 *  accounts{_id=userId, rev, state(AccountState shape: gacha pets/pity/daily-weekly rewards), recentOps, createdAt}
 *    one per user, not per character; _id is already the only lookup key, no extra index needed
 *  ledger{characterId, reqId, type, ...} audit trail of economy ops; a trade entry carries a unique tradeId, which is what makes a commit idempotent
 *    (an 'account' entry additionally carries userId and before/after currency snapshots, see services/account-service.js)
 *  card_wagers{_id=matchId, status held|settled|refunded, zoneId, createdAt, expiresAt, a/b{characterId,userId,deck[8],lockedIds}, result, pick, settledAt, refundedAt, credited}
 *    escrow of a Card Wager (services/card-wager-service.js); every move also writes a ledger row {type:'card_wager', action: escrow|settle|refund, matchId}
 *  card_wager_counters{_id='u:<userId>:<utcDay>' | 'p:<userA>:<userB>:<utcDay>', n, at(TTL 3 d)}
 *    the daily limits of Card Wager: $inc inside the escrow transaction (two escrows of one account write the same document, so they cannot both pass); a refund gives the slot back
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
  await db.collection('characters').createIndex({ userId: 1, slot: 1 }, { unique: true, partialFilterExpression: { slot: { $type: 'number' } } });
  await db.collection('tickets').createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });
  await db.collection('ledger').createIndex({ characterId: 1, at: -1 });
  await db.collection('ledger').createIndex({ tradeId: 1 }, { unique: true, partialFilterExpression: { tradeId: { $type: 'string' } } });
  await db.collection('card_wagers').createIndex({ status: 1, expiresAt: 1 });
  await db.collection('card_wagers').createIndex({ status: 1, zoneId: 1 });
  await db.collection('card_wagers').createIndex({ 'a.characterId': 1 });
  await db.collection('card_wagers').createIndex({ 'b.characterId': 1 });
  await db.collection('card_wagers').createIndex({ 'a.userId': 1, createdAt: -1 }); // daily limits count per account
  await db.collection('card_wagers').createIndex({ 'b.userId': 1, createdAt: -1 });
  await db.collection('card_wager_counters').createIndex({ at: 1 }, { expireAfterSeconds: 3 * 86400 }); // a day counter is only read on its own day
}

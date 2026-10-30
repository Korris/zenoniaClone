import { unauthorized } from '../errors.js';
import { randomToken, sha256 } from './refresh-token-service.js';

/**
 * Zone tickets: random 256-bit token, stored hashed, valid for ttlSec and consumable exactly once
 * (atomic findOneAndDelete, so two zone servers racing on the same ticket cannot both win).
 */
export function createTicketService({ db, ttlSec, now }) {
  const col = db.collection('tickets');
  return {
    async create(characterId, userId, zoneId) {
      const ticket = randomToken(32);
      await col.insertOne({ _id: sha256(ticket), characterId, userId, zoneId, expiresAt: new Date(now() + ttlSec * 1000) });
      return ticket;
    },

    /** Consumes the ticket; returns { characterId, userId, zoneId } or throws 401 invalid_ticket (unknown, expired or already used). */
    async redeem(ticket) {
      const doc = await col.findOneAndDelete({ _id: sha256(ticket), expiresAt: { $gt: new Date(now()) } });
      if (!doc) throw unauthorized('invalid_ticket', 'Ticket is invalid, expired or already used');
      return { characterId: doc.characterId, userId: doc.userId, zoneId: doc.zoneId };
    },

    /** Drops tickets previously issued for a character (a new /enter supersedes older unused ones). */
    async revokeForCharacter(characterId) {
      await col.deleteMany({ characterId });
    },
  };
}

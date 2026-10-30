import crypto from 'node:crypto';
import { unauthorized } from '../errors.js';

export const sha256 = (s) => crypto.createHash('sha256').update(s).digest('hex');
export const randomToken = (bytes = 32) => crypto.randomBytes(bytes).toString('base64url');

/**
 * Opaque, rotating refresh tokens stored hashed. Each use revokes the old token and issues a new one in the same family.
 * Presenting an already-revoked token means theft or a race: the whole family is revoked (the legit device must log in again).
 */
export function createRefreshTokenService({ db, ttlSec, now }) {
  const col = db.collection('refresh_tokens');
  return {
    async issue(userId, family = randomToken(12)) {
      const token = randomToken(48);
      await col.insertOne({ _id: sha256(token), userId, family, revoked: false, expiresAt: new Date(now() + ttlSec * 1000) });
      return token;
    },

    /** Rotates; returns { userId, token }. Throws 401 when unknown, expired or reused. */
    async rotate(token) {
      const doc = await col.findOneAndUpdate(
        { _id: sha256(token), revoked: false, expiresAt: { $gt: new Date(now()) } },
        { $set: { revoked: true } },
      );
      if (!doc) {
        const old = await col.findOne({ _id: sha256(token) });
        if (old?.revoked) await col.updateMany({ family: old.family }, { $set: { revoked: true } }); // reuse detected
        throw unauthorized('invalid_refresh_token', 'Refresh token is invalid or expired');
      }
      return { userId: doc.userId, token: await this.issue(doc.userId, doc.family) };
    },

    /** Idempotent logout of one device. */
    async revoke(token) {
      await col.updateOne({ _id: sha256(token) }, { $set: { revoked: true } });
    },

    async revokeAllForUser(userId) {
      await col.updateMany({ userId }, { $set: { revoked: true } });
    },
  };
}

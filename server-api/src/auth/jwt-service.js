import jwt from 'jsonwebtoken';
import { unauthorized } from '../errors.js';

/** Short-lived HS256 access tokens. Refresh tokens are opaque (see refresh-token-service.js). */
export function createJwtService({ secret, accessTtlSec }) {
  return {
    sign(user) {
      return jwt.sign({ sub: String(user._id), username: user.username, typ: 'access' }, secret, { algorithm: 'HS256', expiresIn: accessTtlSec });
    },
    /** Returns { userId, username } or throws 401 (token_expired / invalid_token). */
    verify(token) {
      try {
        const p = jwt.verify(token, secret, { algorithms: ['HS256'] });
        if (p.typ !== 'access' || typeof p.sub !== 'string') throw new Error('wrong token type');
        return { userId: p.sub, username: p.username };
      } catch (e) {
        if (e.name === 'TokenExpiredError') throw unauthorized('token_expired', 'Access token expired');
        throw unauthorized('invalid_token', 'Invalid access token');
      }
    },
    accessTtlSec,
  };
}

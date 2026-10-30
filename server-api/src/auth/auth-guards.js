import crypto from 'node:crypto';
import { forbidden, unauthorized } from '../errors.js';

/** preHandler factories: user JWT for public routes, shared service key for zone-server routes. */
export function createGuards({ jwt, serviceKey }) {
  const keyHash = crypto.createHash('sha256').update(serviceKey).digest();
  return {
    async requireUser(req) {
      const h = req.headers.authorization;
      if (typeof h !== 'string' || !h.startsWith('Bearer ')) throw unauthorized();
      req.user = jwt.verify(h.slice(7));
    },
    async requireService(req) {
      const k = req.headers['x-service-key'];
      if (typeof k !== 'string') throw unauthorized('missing_service_key', 'Service key required');
      const given = crypto.createHash('sha256').update(k).digest(); // equal-length digests => timingSafeEqual is safe
      if (!crypto.timingSafeEqual(given, keyHash)) throw forbidden('bad_service_key', 'Invalid service key');
    },
  };
}

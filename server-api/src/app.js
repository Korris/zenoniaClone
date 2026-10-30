import crypto from 'node:crypto';
import Fastify from 'fastify';
import { RateLimiter } from './auth/rate-limiter.js';
import { createGuards } from './auth/auth-guards.js';
import { createJwtService } from './auth/jwt-service.js';
import { createPasswordService } from './auth/password-service.js';
import { createRefreshTokenService } from './auth/refresh-token-service.js';
import { createTicketService } from './auth/ticket-service.js';
import { errorHandler } from './errors.js';
import { authRoutes } from './routes/auth-routes.js';
import { characterRoutes } from './routes/character-routes.js';
import { internalRoutes } from './routes/internal-routes.js';
import { createCharacterService } from './services/character-service.js';

/** Uniform [0,1) with 53 bits from the OS CSPRNG (enhance rolls must not be predictable). */
export const secureRandom = () => crypto.randomInt(0, 2 ** 48) / 2 ** 48;

/**
 * Builds the Fastify app. All side-effecting dependencies are injectable so tests are deterministic:
 *  db (required), cfg (required), now () => ms, rand () => [0,1), mailer { sendPasswordReset }.
 */
export function buildApp({ db, cfg, now = Date.now, rand = secureRandom, mailer, logger = false }) {
  const app = Fastify({ logger, trustProxy: cfg.TRUST_PROXY, bodyLimit: 256 * 1024 });
  app.setErrorHandler(errorHandler);
  app.setNotFoundHandler((req, reply) => reply.status(404).send({ error: { code: 'not_found', message: `Route ${req.method} ${req.url} not found` } }));

  const jwt = createJwtService({ secret: cfg.JWT_SECRET, accessTtlSec: cfg.ACCESS_TTL_SEC });
  const guards = createGuards({ jwt, serviceKey: cfg.SERVICE_KEY });
  const limiters = {
    login: new RateLimiter({ max: cfg.LOGIN_IP_MAX, windowMs: cfg.LOGIN_IP_WINDOW_SEC * 1000, now }),
    register: new RateLimiter({ max: cfg.REGISTER_IP_MAX, windowMs: cfg.REGISTER_IP_WINDOW_SEC * 1000, now }),
  };
  const deps = {
    db, cfg, now, rand, jwt, guards, limiters,
    passwords: createPasswordService(cfg.BCRYPT_COST),
    refreshTokens: createRefreshTokenService({ db, ttlSec: cfg.REFRESH_TTL_SEC, now }),
    tickets: createTicketService({ db, ttlSec: cfg.TICKET_TTL_SEC, now }),
    characters: createCharacterService({ db, now, sessionLockSec: cfg.SESSION_LOCK_SEC }),
    // Password reset stub: no email is sent; the link is only logged (the reset token is never returned over HTTP).
    mailer: mailer ?? { sendPasswordReset: async ({ email, link }) => app.log.info({ email, link }, 'password reset requested (email stub)') },
  };

  app.get('/health', async () => ({ ok: true }));
  authRoutes(app, deps);
  characterRoutes(app, deps);
  internalRoutes(app, deps);
  return app;
}


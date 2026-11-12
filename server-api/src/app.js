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
import { resetPageRoutes } from './routes/reset-page.js';
import { createMailer } from './mail/mailer.js';
import { createCharacterService } from './services/character-service.js';

/** Uniform [0,1) with 48 bits from the OS CSPRNG (enhance rolls must not be predictable). randomInt's range stops just short of 2^48, so the bytes are read directly. */
export const secureRandom = () => crypto.randomBytes(6).readUIntBE(0, 6) / 2 ** 48;

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
    characters: createCharacterService({ db, now, sessionLockSec: cfg.SESSION_LOCK_SEC, maxCharacters: cfg.MAX_CHARACTERS }),
    // Real SMTP when SMTP_* is complete. The reset token is never returned over HTTP.
    mailer: mailer ?? createMailer({ cfg, log: app.log }),
  };

  app.get('/health', async () => ({ ok: true }));
  resetPageRoutes(app);
  authRoutes(app, deps);
  characterRoutes(app, deps);
  internalRoutes(app, deps);
  return app;
}


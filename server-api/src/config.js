import { z } from 'zod';

const num = (def) => z.coerce.number().int().positive().default(def);

const schema = z.object({
  PORT: z.coerce.number().int().min(0).max(65535).default(3000),
  HOST: z.string().default('0.0.0.0'),
  MONGO_URI: z.string().min(1),
  MONGO_DB: z.string().default('zenonia'),
  JWT_SECRET: z.string().min(32, 'JWT_SECRET must be at least 32 characters'),
  SERVICE_KEY: z.string().min(16, 'SERVICE_KEY must be at least 16 characters'),
  ZONE_ID: z.string().default('frost-hollow'),
  ZONE_HOST: z.string().default('127.0.0.1'),
  ZONE_PORT: z.coerce.number().int().min(1).max(65535).default(9050),
  TRUST_PROXY: z.enum(['true', 'false']).default('false').transform((v) => v === 'true'),
  PASSWORD_RESET_URL: z.string().default('http://localhost:3000/reset'),
  ACCESS_TTL_SEC: num(900),
  REFRESH_TTL_SEC: num(30 * 24 * 3600),
  TICKET_TTL_SEC: num(60),
  SESSION_LOCK_SEC: num(90),
  RESET_TTL_SEC: num(3600),
  BCRYPT_COST: z.coerce.number().int().min(4).max(15).default(11),
  LOGIN_MAX_FAILURES: num(5),
  LOGIN_LOCKOUT_SEC: num(900),
  LOGIN_IP_MAX: num(30),
  LOGIN_IP_WINDOW_SEC: num(60),
  REGISTER_IP_MAX: num(5),
  REGISTER_IP_WINDOW_SEC: num(3600),
});

/** Validates env-like input; throws one readable error listing every bad variable (fail fast at boot). */
export function loadConfig(env = process.env) {
  const parsed = schema.safeParse(env);
  if (!parsed.success) {
    const msg = parsed.error.issues.map((i) => `${i.path.join('.')}: ${i.message}`).join('; ');
    throw new Error(`Invalid configuration: ${msg}`);
  }
  return Object.freeze(parsed.data);
}

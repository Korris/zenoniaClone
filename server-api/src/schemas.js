import { z } from 'zod';
import { CLASS_IDS } from './rpg/rpg-constants.js';
import { opsSchema } from './rpg/inventory-ops.js';
import { stateSchema } from './rpg/item-validation.js';

// bcrypt only hashes the first 72 BYTES, so cap by bytes (not chars) to avoid silently truncated passwords.
const password = z
  .string()
  .min(8, 'at least 8 characters')
  .refine((p) => Buffer.byteLength(p, 'utf8') <= 72, 'at most 72 bytes');

export const registerSchema = z.object({
  username: z.string().trim().min(3).max(20).regex(/^[A-Za-z0-9_]+$/, 'letters, digits and _ only'),
  email: z.string().trim().toLowerCase().email().max(254),
  password,
});

export const loginSchema = z.object({
  login: z.string().trim().min(1).max(254), // username or email
  password: z.string().min(1).max(200),
});

export const refreshSchema = z.object({ refreshToken: z.string().min(20).max(200) });
export const resetRequestSchema = z.object({ email: z.string().trim().toLowerCase().email().max(254) });
export const resetConfirmSchema = z.object({ token: z.string().min(20).max(200), password });

export const createCharacterSchema = z.object({
  name: z.string().trim().min(3).max(16).regex(/^[A-Za-z0-9_]+$/, 'letters, digits and _ only'),
  classId: z.enum(CLASS_IDS),
});

const sessionId = z.string().regex(/^[a-f0-9]{32}$/);
const reqId = z.string().min(8).max(64).regex(/^[A-Za-z0-9._-]+$/);
const rev = z.number().int().min(0);

export const redeemSchema = z.object({ ticket: z.string().min(20).max(200) });
export const saveSchema = z.object({ sessionId, rev, state: stateSchema });
export const opsRequestSchema = z.object({ sessionId, rev, reqId, ops: opsSchema });
export const enhanceSchema = z.object({ sessionId, rev, reqId, itemUid: z.string().min(1).max(64) });
export const endSessionSchema = z.object({ characterId: z.string().length(24) });

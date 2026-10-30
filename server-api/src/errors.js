import { ZodError } from 'zod';

/** Error with an HTTP status and a stable machine code. Rendered as { error: { code, message, details? } }. */
export class ApiError extends Error {
  constructor(status, code, message, details) {
    super(message);
    this.status = status;
    this.code = code;
    this.details = details;
  }
}

export const badRequest = (code, msg, details) => new ApiError(400, code, msg, details);
export const unauthorized = (code = 'unauthorized', msg = 'Authentication required') => new ApiError(401, code, msg);
export const forbidden = (code = 'forbidden', msg = 'Forbidden') => new ApiError(403, code, msg);
export const notFound = (what = 'Resource') => new ApiError(404, 'not_found', `${what} not found`);
export const conflict = (code, msg, details) => new ApiError(409, code, msg, details);
export const tooMany = (retryAfterSec) =>
  new ApiError(429, 'rate_limited', 'Too many requests, try again later', { retryAfterSec });

function zodToApiError(err) {
  const details = err.issues.map((i) => ({ path: i.path.join('.'), message: i.message }));
  return new ApiError(400, 'validation_error', 'Invalid request', details);
}

/** Parses `data` with a zod schema; throws a 400 validation_error with per-field details. */
export function parse(schema, data) {
  const r = schema.safeParse(data ?? {});
  if (!r.success) throw zodToApiError(r.error);
  return r.data;
}

/** Fastify error handler: one consistent body for every failure; never leaks internals on 5xx. */
export function errorHandler(err, req, reply) {
  if (err instanceof ZodError) err = zodToApiError(err);
  if (err instanceof ApiError) {
    if (err.status === 429 && err.details?.retryAfterSec) reply.header('retry-after', String(err.details.retryAfterSec));
    return reply.status(err.status).send({ error: { code: err.code, message: err.message, ...(err.details ? { details: err.details } : {}) } });
  }
  if (err.statusCode && err.statusCode < 500) {
    const code = err.statusCode === 413 ? 'payload_too_large' : 'bad_request';
    return reply.status(err.statusCode).send({ error: { code, message: err.message } });
  }
  req.log.error({ err }, 'unhandled error');
  return reply.status(500).send({ error: { code: 'internal_error', message: 'Internal server error' } });
}

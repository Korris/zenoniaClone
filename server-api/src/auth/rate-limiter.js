/**
 * In-memory fixed-window limiter keyed by string (per process; put a shared store behind it when the API is scaled to several
 * instances). `now` is injectable so tests control time.
 */
export class RateLimiter {
  constructor({ max, windowMs, now = Date.now }) {
    this.max = max;
    this.windowMs = windowMs;
    this.now = now;
    this.hits = new Map(); // key -> { count, resetAt }
  }

  /** Registers one hit. Returns { allowed, retryAfterSec }. */
  hit(key) {
    const t = this.now();
    this.#sweep(t);
    let e = this.hits.get(key);
    if (!e || e.resetAt <= t) {
      e = { count: 0, resetAt: t + this.windowMs };
      this.hits.set(key, e);
    }
    e.count += 1;
    return { allowed: e.count <= this.max, retryAfterSec: Math.max(1, Math.ceil((e.resetAt - t) / 1000)) };
  }

  /** Forgets a key (e.g. after a successful login). */
  reset(key) {
    this.hits.delete(key);
  }

  #sweep(t) {
    if (this.hits.size < 5000) return; // bounded memory: only sweep when large
    for (const [k, e] of this.hits) if (e.resetAt <= t) this.hits.delete(k);
  }
}

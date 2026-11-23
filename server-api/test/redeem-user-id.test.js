import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { makeCtx } from './helpers.js';

let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

// The zone needs the account behind a character (Ranked: two characters of one account never meet).
const redeem = async (token, charId) => {
  const e = await c.call('POST', `/characters/${charId}/enter`, { token });
  return c.svc('POST', '/internal/tickets/redeem', { ticket: e.body.ticket });
};

describe('internal redeem returns the account id', () => {
  it('gives the same userId for two characters of one account and another for a stranger', async () => {
    const u = await c.register('rkowner');
    const a = await c.createChar(u.accessToken, 'rkaltone');
    const b = await c.createChar(u.accessToken, 'rkalttwo');
    const other = await c.register('rkotheracct');
    const o = await c.createChar(other.accessToken, 'rkother');
    const ra = await redeem(u.accessToken, a.id), rb = await redeem(u.accessToken, b.id), ro = await redeem(other.accessToken, o.id);
    expect(ra.status).toBe(200);
    expect(typeof ra.body.userId).toBe('string');
    expect(ra.body.userId.length).toBeGreaterThan(0);
    expect(rb.body.userId).toBe(ra.body.userId);
    expect(ro.body.userId).not.toBe(ra.body.userId);
  });
});

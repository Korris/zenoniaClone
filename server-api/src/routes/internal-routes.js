import { ApiError, parse } from '../errors.js';
import { applyOps } from '../rpg/inventory-ops.js';
import { attemptEnhance, findEnhanceTamper } from '../rpg/enhance-logic.js';
import { endSessionSchema, enhanceSchema, opsRequestSchema, redeemSchema, saveSchema } from '../schemas.js';

/** Zone-server routes, guarded by the shared service key (x-service-key). Every write needs the session lock + the current rev. */
export function internalRoutes(app, { cfg, characters, tickets, guards, rand }) {
  const opts = { preHandler: guards.requireService };

  // Consumes the ticket, takes the one-session lock and returns the character to load.
  app.post('/internal/tickets/redeem', opts, async (req) => {
    const { ticket } = parse(redeemSchema, req.body);
    const { characterId } = await tickets.redeem(ticket);
    const { sessionId, doc } = await characters.lockSession(characterId, cfg.ZONE_ID);
    return { sessionId, character: characters.full(doc) };
  });

  // Periodic / event snapshot (level-up, rare drop, zone change, logout). Returns the new rev.
  app.post('/internal/characters/:id/save', opts, async (req) => {
    const { sessionId, rev, state } = parse(saveSchema, req.body);
    return characters.save(req.params.id, sessionId, rev, state, (oldState, newState) => {
      const uid = findEnhanceTamper(oldState, newState);
      if (uid) throw new ApiError(422, 'enhance_tamper', `Item ${uid} carries an enhance level the API never granted`);
    });
  });

  // Atomic batch of inventory / equipment / currency changes (e.g. rare drop pickup, potion purchase).
  app.post('/internal/characters/:id/ops', opts, async (req) => {
    const { sessionId, rev, reqId, ops } = parse(opsRequestSchema, req.body);
    return characters.mutate(req.params.id, sessionId, rev, reqId, 'ops', (state) => {
      const next = applyOps(state, ops);
      return { changed: true, state: next, result: { ok: true }, before: { ops }, after: null };
    });
  });

  // One enhance attempt, rolled here with the C# numbers; costs 1 Upgrade Stone (also on failure). Idempotent per reqId.
  app.post('/internal/characters/:id/enhance', opts, async (req) => {
    const { sessionId, rev, reqId, itemUid } = parse(enhanceSchema, req.body);
    return characters.mutate(req.params.id, sessionId, rev, reqId, 'enhance', (state) => {
      const r = attemptEnhance(state, itemUid, rand);
      const result = { ok: r.ok, success: r.success, level: r.level, reason: r.reason, itemUid, upgradeStone: r.state.upgradeStone };
      return { changed: r.ok, state: r.state, result, before: { upgradeStone: state.upgradeStone }, after: { level: r.level, upgradeStone: r.state.upgradeStone } };
    });
  });

  // Releases the lock (call after the final save). Idempotent.
  app.post('/internal/sessions/:sessionId/end', opts, async (req) => {
    const { characterId } = parse(endSessionSchema, req.body);
    return { released: await characters.endSession(characterId, req.params.sessionId) };
  });

  // Zone registry stub (phase 4 fills it).
  app.get('/internal/zones', opts, async () => ({ zones: [{ id: cfg.ZONE_ID, host: cfg.ZONE_HOST, port: cfg.ZONE_PORT }] }));
}

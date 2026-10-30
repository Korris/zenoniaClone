import { conflict, parse } from '../errors.js';
import { createCharacterSchema } from '../schemas.js';

/** Public (user JWT) character routes: list / create / load / delete / enter (zone ticket). */
export function characterRoutes(app, { cfg, characters, tickets, guards }) {
  const opts = { preHandler: guards.requireUser };

  app.get('/characters', opts, async (req) => ({ characters: await characters.list(req.user.userId) }));

  app.post('/characters', opts, async (req, reply) => {
    const { name, classId } = parse(createCharacterSchema, req.body);
    return reply.status(201).send({ character: await characters.create(req.user.userId, name, classId) });
  });

  app.get('/characters/:id', opts, async (req) => ({ character: characters.full(await characters.getOwned(req.user.userId, req.params.id)) }));

  app.delete('/characters/:id', opts, async (req) => {
    await characters.remove(req.user.userId, req.params.id);
    return { ok: true };
  });

  // Issues the single-use 60 s ticket the client puts in its zone Hello. The lock itself is taken when the zone redeems it.
  app.post('/characters/:id/enter', opts, async (req) => {
    const doc = await characters.getOwned(req.user.userId, req.params.id);
    if (characters.live(doc)) throw conflict('already_online', 'Character is already online');
    const id = String(doc._id);
    await tickets.revokeForCharacter(id); // a new enter supersedes older unused tickets
    const ticket = await tickets.create(id, req.user.userId, cfg.ZONE_ID);
    return { ticket, zoneId: cfg.ZONE_ID, zoneHost: cfg.ZONE_HOST, zonePort: cfg.ZONE_PORT, expiresIn: cfg.TICKET_TTL_SEC };
  });
}

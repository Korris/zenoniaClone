import { ObjectId } from 'mongodb';
import { conflict, parse, tooMany, unauthorized } from '../errors.js';
import { randomToken, sha256 } from '../auth/refresh-token-service.js';
import { loginSchema, refreshSchema, registerSchema, resetConfirmSchema, resetRequestSchema } from '../schemas.js';

/** /auth/*: register, login (lockout + IP limit), refresh (rotating), logout, password reset (stub: link is logged). */
export function authRoutes(app, { db, cfg, now, jwt, passwords, refreshTokens, limiters, mailer }) {
  const users = db.collection('users');
  const resets = db.collection('password_resets');

  const publicUser = (u) => ({ id: String(u._id), username: u.username, email: u.email });
  async function session(user) {
    return {
      user: publicUser(user),
      accessToken: jwt.sign(user),
      refreshToken: await refreshTokens.issue(String(user._id)),
      expiresIn: jwt.accessTtlSec,
    };
  }
  function limit(limiter, key) {
    const r = limiter.hit(key);
    if (!r.allowed) throw tooMany(r.retryAfterSec);
  }

  app.post('/auth/register', async (req, reply) => {
    limit(limiters.register, req.ip);
    const { username, email, password } = parse(registerSchema, req.body);
    const user = {
      username, usernameLower: username.toLowerCase(), email, emailLower: email, passHash: await passwords.hash(password),
      failedLogins: 0, lockUntil: null, createdAt: new Date(now()),
    };
    try {
      user._id = (await users.insertOne(user)).insertedId;
    } catch (e) {
      if (e.code !== 11000) throw e;
      throw conflict(/emailLower/.test(e.message) ? 'email_taken' : 'username_taken', 'Username or email is already registered');
    }
    return reply.status(201).send(await session(user));
  });

  app.post('/auth/login', async (req) => {
    limit(limiters.login, req.ip);
    const { login, password } = parse(loginSchema, req.body);
    const key = login.toLowerCase();
    const user = await users.findOne({ $or: [{ usernameLower: key }, { emailLower: key }] });
    if (!user) {
      await passwords.verifyDummy(password); // same timing as a real check
      throw unauthorized('invalid_credentials', 'Wrong username or password');
    }
    if (user.lockUntil && user.lockUntil.getTime() > now()) {
      throw tooMany(Math.ceil((user.lockUntil.getTime() - now()) / 1000));
    }
    if (!(await passwords.verify(password, user.passHash))) {
      const upd = await users.findOneAndUpdate({ _id: user._id }, { $inc: { failedLogins: 1 } }, { returnDocument: 'after' });
      if (upd.failedLogins >= cfg.LOGIN_MAX_FAILURES) {
        await users.updateOne({ _id: user._id }, { $set: { failedLogins: 0, lockUntil: new Date(now() + cfg.LOGIN_LOCKOUT_SEC * 1000) } });
      }
      throw unauthorized('invalid_credentials', 'Wrong username or password');
    }
    if (user.failedLogins || user.lockUntil) await users.updateOne({ _id: user._id }, { $set: { failedLogins: 0, lockUntil: null } });
    return session(user);
  });

  app.post('/auth/refresh', async (req) => {
    limit(limiters.login, req.ip);
    const { refreshToken } = parse(refreshSchema, req.body);
    const { userId, token } = await refreshTokens.rotate(refreshToken);
    const user = ObjectId.isValid(userId) ? await users.findOne({ _id: new ObjectId(userId) }) : null;
    if (!user) throw unauthorized('invalid_refresh_token', 'Refresh token is invalid or expired');
    return { user: publicUser(user), accessToken: jwt.sign(user), refreshToken: token, expiresIn: jwt.accessTtlSec };
  });

  app.post('/auth/logout', async (req) => {
    const { refreshToken } = parse(refreshSchema, req.body);
    await refreshTokens.revoke(refreshToken);
    return { ok: true };
  });

  // Always answers 202 so the endpoint cannot be used to discover registered emails.
  app.post('/auth/password-reset/request', async (req, reply) => {
    limit(limiters.login, req.ip);
    const { email } = parse(resetRequestSchema, req.body);
    const user = await users.findOne({ emailLower: email });
    if (user) {
      const token = randomToken(32);
      await resets.insertOne({ _id: sha256(token), userId: String(user._id), expiresAt: new Date(now() + cfg.RESET_TTL_SEC * 1000) });
      await mailer.sendPasswordReset({ email, username: user.username, link: `${cfg.PASSWORD_RESET_URL}?token=${token}`, token });
    }
    return reply.status(202).send({ ok: true });
  });

  app.post('/auth/password-reset/confirm', async (req) => {
    limit(limiters.login, req.ip);
    const { token, password } = parse(resetConfirmSchema, req.body);
    const doc = await resets.findOneAndDelete({ _id: sha256(token), expiresAt: { $gt: new Date(now()) } });
    if (!doc) throw unauthorized('invalid_reset_token', 'Reset token is invalid or expired');
    await users.updateOne(
      { _id: new ObjectId(doc.userId) },
      { $set: { passHash: await passwords.hash(password), failedLogins: 0, lockUntil: null } },
    );
    await refreshTokens.revokeAllForUser(doc.userId); // every device must log in again
    return { ok: true };
  });
}

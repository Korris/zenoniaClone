import nodemailer from 'nodemailer';

/** The SMTP_* variables still empty. SMTP is on only when none is missing. */
export function smtpMissing(cfg) {
  return ['SMTP_HOST', 'SMTP_USER', 'SMTP_PASS', 'SMTP_FROM'].filter((k) => !cfg[k]);
}

export function resetMailText({ username, link, ttlSec }) {
  const minutes = Math.round(ttlSec / 60);
  return [
    `Hello ${username},`,
    '',
    'Someone asked to reset the password of your Pocket Valor account. To choose a new one, open this link:',
    '',
    link,
    '',
    `The link works once and expires in ${minutes} minutes. If this was not you, ignore this mail: your password stays as it is.`,
  ].join('\n');
}

/**
 * The mailer buildApp uses unless one is injected.
 * SMTP_* complete: real mail. Otherwise: the link goes to the log outside production; in production the log says no mail went out and
 * never holds the link, the token or the address, and startup says so loudly.
 * Port 587 with SMTP_SECURE=false is STARTTLS (what Gmail wants), and requireTLS makes it refuse to go on without encryption.
 */
export function createMailer({ cfg, log, createTransport = nodemailer.createTransport }) {
  const production = cfg.NODE_ENV === 'production';
  const missing = smtpMissing(cfg);

  if (missing.length > 0) {
    if (production) {
      log.warn({ missing }, 'SMTP is not configured: password reset mails will NOT be sent. Fill the missing SMTP_* variables');
      return { enabled: false, sendPasswordReset: async () => log.warn({ missing }, 'password reset requested but no mail was sent: SMTP is not configured') };
    }
    return { enabled: false, sendPasswordReset: async ({ email, link }) => log.info({ email, link }, 'password reset requested (no SMTP: dev link, not mailed)') };
  }

  if (production && !cfg.PASSWORD_RESET_URL.startsWith('https://')) log.warn('PASSWORD_RESET_URL is not https: reset tokens would cross the internet in the clear');
  const transport = createTransport({
    host: cfg.SMTP_HOST,
    port: cfg.SMTP_PORT,
    secure: cfg.SMTP_SECURE,
    requireTLS: cfg.SMTP_REQUIRE_TLS && !cfg.SMTP_SECURE,
    auth: { user: cfg.SMTP_USER, pass: cfg.SMTP_PASS },
    connectionTimeout: 10000,
    greetingTimeout: 10000,
    socketTimeout: 20000,
  });
  log.info({ host: cfg.SMTP_HOST, port: cfg.SMTP_PORT, secure: cfg.SMTP_SECURE }, 'SMTP on: password reset links are mailed');
  return {
    enabled: true,
    sendPasswordReset: async ({ email, username, link }) => {
      await transport.sendMail({
        from: cfg.SMTP_FROM,
        to: email,
        subject: 'Reset your Pocket Valor password',
        text: resetMailText({ username, link, ttlSec: cfg.RESET_TTL_SEC }),
      });
    },
  };
}

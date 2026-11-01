import crypto from 'node:crypto';

// The link in the mail is /reset#token=...: a fragment is never sent to any server, so it cannot land in an access log or a Referer header.
// The script reads it, then removes it from the address bar and the browser history before anything else happens.
const STYLE = `body{font:16px system-ui,sans-serif;background:#14161c;color:#e8e8ea;margin:0;display:flex;justify-content:center;padding:48px 16px}
main{width:100%;max-width:360px}h1{font-size:22px;margin:0 0 20px}label{display:block;margin:14px 0 4px}
input{width:100%;box-sizing:border-box;padding:10px;font:inherit;background:#22252e;color:inherit;border:1px solid #454a58;border-radius:4px}
button{margin-top:20px;width:100%;padding:11px;font:inherit;font-weight:600;background:#e0a33a;color:#14161c;border:0;border-radius:4px;cursor:pointer}
button:disabled{opacity:.5;cursor:default}p{min-height:1.4em;margin:16px 0 0}`;

const SCRIPT = `(function () {
  var match = /^#token=([A-Za-z0-9_-]+)$/.exec(location.hash);
  var token = match ? match[1] : null;
  history.replaceState(null, '', location.pathname);
  var form = document.getElementById('f'), note = document.getElementById('note'), btn = document.getElementById('go');
  function say(text) { note.textContent = text; }
  if (!token) { form.hidden = true; say('This link is not complete. Ask for a new password reset mail from the game.'); return; }
  form.addEventListener('submit', function (e) {
    e.preventDefault();
    var a = document.getElementById('p1').value, b = document.getElementById('p2').value;
    if (a !== b) { say('The two passwords are different.'); return; }
    btn.disabled = true; say('Saving...');
    fetch('/auth/password-reset/confirm', { method: 'POST', headers: { 'content-type': 'application/json' }, referrerPolicy: 'no-referrer', body: JSON.stringify({ token: token, password: a }) })
      .then(function (r) {
        if (r.ok) { token = null; form.hidden = true; say('Password changed. Go back to the game and log in.'); return; }
        btn.disabled = false;
        if (r.status === 400) say('The password must be 8 to 72 characters long.');
        else if (r.status === 401) { form.hidden = true; say('This link has expired or was already used. Ask for a new one from the game.'); }
        else if (r.status === 429) say('Too many tries. Wait a minute and try again.');
        else say('The server could not do that. Try again later.');
      })
      .catch(function () { btn.disabled = false; say('The server cannot be reached. Try again.'); });
  });
})();`;

const hash = (s) => `'sha256-${crypto.createHash('sha256').update(s).digest('base64')}'`;

const HTML = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="referrer" content="no-referrer">
<meta name="robots" content="noindex">
<title>Pocket Valor password reset</title>
<style>${STYLE}</style>
</head>
<body>
<main>
<h1>Pocket Valor</h1>
<p>Choose a new password.</p>
<form id="f" autocomplete="off">
<label for="p1">New password</label><input id="p1" type="password" minlength="8" autocomplete="new-password" required>
<label for="p2">Repeat it</label><input id="p2" type="password" minlength="8" autocomplete="new-password" required>
<button id="go" type="submit">Change password</button>
</form>
<p id="note" role="status"></p>
</main>
<script>${SCRIPT}</script>
</body>
</html>
`;

const CSP = [
  "default-src 'none'", `script-src ${hash(SCRIPT)}`, `style-src ${hash(STYLE)}`, "connect-src 'self'",
  "form-action 'none'", "base-uri 'none'", "frame-ancestors 'none'",
].join('; ');

/** GET /reset: where the link in the mail lands. A static page: it holds no secret, the browser posts to /auth/password-reset/confirm. */
export function resetPageRoutes(app) {
  app.get('/reset', async (req, reply) => reply
    .header('content-type', 'text/html; charset=utf-8')
    .header('content-security-policy', CSP)
    .header('referrer-policy', 'no-referrer')
    .header('cache-control', 'no-store')
    .header('x-content-type-options', 'nosniff')
    .send(HTML));
}

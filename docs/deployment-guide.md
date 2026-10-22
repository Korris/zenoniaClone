# Deployment Guide

## Local Development

### Prerequisites
- **Node.js 22+** (for npm & test runner).
- Modern browser (Chrome, Firefox, Safari, Edge) supporting ES modules.

### Run Locally

#### Option 1: npx serve (Recommended)
```bash
npm start
```
Opens http://localhost:5173 automatically (or http://localhost:3000 if port 5173 busy).

#### Option 2: Python built-in server
```bash
python -m http.server 5173
```
Then navigate to http://localhost:5173.

#### Option 3: Any static file server
- VS Code Live Server extension
- Node http-server (`npx http-server`)
- Ruby: `ruby -run -ehttpd . -p5173`

**Why HTTP?** ES modules require the `file://` protocol to work, but most browsers block cross-origin imports over `file://`. Use HTTP.

## Run Tests

```bash
npm test
```

Runs `node --test tests/*.test.js` — 8 test files covering combo, damage, leveling, save, skill, tilemap, quest, sprite-animation, and camera systems.

**Expected behavior:** All 63 tests pass. If a test fails, output shows assertion error and line number.

### Dev Tools

No sprite validation needed; sprites come from external asset pack PNG files.

### Writing Tests
1. Create `tests/my-feature.test.js`:
   ```js
   import test from 'node:test';
   import assert from 'node:assert';
   import { applyExpGain } from '../src/systems/leveling-system.js';

   test('applyExpGain gains a level when EXP >= threshold', () => {
     const result = applyExpGain({ level: 1, exp: 0 }, 60);
     assert.equal(result.level, 2);
     assert.equal(result.levelsGained, 1);
   });
   ```

2. Run `npm test` — new test auto-discovered.

## Build (None Required)

**This project requires no build step.** Deploy `src/`, `assets/` (ninja-adventure + custom/hero), and `index.html` as-is.

### Why?
- Vanilla JS + Canvas (no compilation).
- ES modules run natively in modern browsers.
- No minification or bundling.
- **Important:** `assets/` folders must be served; sprites load from `assets/ninja-adventure/` + `assets/custom/hero/` at runtime.
- **Note:** Python 3 + Pillow/numpy only needed locally for `npm run sprites:build` (regenerate hero sprites). Not required for deployment.

## Deploy to Production

### Static Hosting Options

#### GitHub Pages
1. Push repo to GitHub.
2. Enable Pages in repo settings (source: main branch, root folder).
3. Deployed to `https://<username>.github.io/<repo>/`.

#### Netlify
1. Connect GitHub repo in Netlify dashboard.
2. Build command: (leave blank, no build needed).
3. Publish directory: `.` (root).
4. Deploy on push.

#### Vercel
1. Connect GitHub repo.
2. Framework: `Other` (or none).
3. Build command: (blank).
4. Output directory: (blank).
5. Deploy.

#### AWS S3 + CloudFront
1. Create S3 bucket; enable static website hosting.
2. Upload all files.
3. (Optional) Add CloudFront distribution for caching & CDN.

#### Simple SSH/SCP
```bash
scp -r . user@server:/var/www/zenonia/
```
Serve with nginx/Apache (configure for ES modules MIME types).

### MIME Type Configuration (Important!)

Ensure `.js` files served with `Content-Type: text/javascript` or `application/javascript`.

**nginx:**
```nginx
location ~ \.js$ {
  add_header Content-Type text/javascript;
}
```

**Apache:** (in `.htaccess` or vhost)
```apache
AddType text/javascript .js
```

### Environment Variables (None)

No `.env` file needed. All config is in `src/data/*.js` (hardcoded). Player saves stored in browser `localStorage`.

## Directory Structure for Deployment

```
.
├── index.html          (Entry point, served as root)
├── src/                (Game code)
│   ├── main.js
│   ├── core/, graphics/, systems/, entities/, data/, game/, world/, ui/, save/
├── assets/ninja-adventure/  (REQUIRED: PNG sprites, tilesets, FX; CC0 by pixel-boy)
│   ├── tilesets/, characters/, monsters/, boss/, fx/, items/, ui/, CREDITS.txt
├── tests/              (Run locally; not deployed)
├── docs/               (Documentation; optional to deploy)
├── package.json        (Not read at runtime; dev-only)
└── README.md           (Optional)
```

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| "Loading assets" spinner never ends | `assets/` folder not found or images 404 | Ensure `assets/ninja-adventure/` is deployed and served. Check network tab in DevTools. |
| "Failed to load image" error | Missing PNG files in assets/ninja-adventure | Deploy entire `assets/ninja-adventure/` folder with all subfolders (tilesets, characters, monsters, boss, fx, items, ui). |
| "Cross-Origin Request Blocked" on startup | Using `file://` protocol | Use HTTP server (`npm start`). ES modules require http://. |
| Sprites appear blank | Image files loaded but not sliced | Already handled; check browser console for asset load errors. |
| Save not persisting | localStorage disabled | User must enable browser storage; no server fallback. |
| "Cannot find module" import error | Wrong relative path or missing file | Check file exists; verify `import` path matches case-sensitive filename. |

## Size & Performance

- **Code:** ~3500 lines across 58 files.
- **Canvas:** 960×640 pixels (logical), 3× upscaled to 2880×1920 on display.
- **No network calls:** Everything client-side.
- **Save size:** ~1 KB JSON per game (version 1).

## Version Management

Save format versioned in `src/save/save-serializer.js`:
- **v1** (current): `{ version: 1, player: {...}, mapId, x, y, bossDefeated }`

If save format changes, increment version and add migration in `validateSave()`.

## Release Checklist

- [ ] All tests pass: `npm test` (63 tests).
- [ ] Assets folder deployed: `assets/ninja-adventure/` with all images.
- [ ] No console errors in browser DevTools; loading screen shows and completes.
- [ ] Play from start → boss win → save/load works.
- [ ] Combo timing feels responsive (chain window: 400ms).
- [ ] No obvious lag or visual glitches.
- [ ] Commit with conventional commit message (e.g., `fix: asset loading`, `feat: new NPC`).
- [ ] Tag release: `git tag -a v0.3.0 -m "Release v0.3: Asset pack integration"`
- [ ] Push & deploy.

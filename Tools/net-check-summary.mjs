// Budget check for Tools/net-local-test.ps1 output: node net-check-summary.mjs <dir> [--clients N] [--strict-up]
// Reads server.json + c<i>.json (written by `--net-stats`), prints a table of numbers and PASS / FAIL per rule, exits 1 when a rule fails.
import fs from 'node:fs';
import path from 'node:path';

const dir = process.argv[2];
const args = process.argv.slice(3);
const argVal = (k, d) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : d; };
const expected = Number(argVal('--clients', 0));
const upBudget = args.includes('--strict-up') ? 1.0 : 1.5; // plan: <= 1 KB/s; a bot that presses attack every frame cannot dedupe input frames

if (!dir) { console.error('usage: node net-check-summary.mjs <dir> [--clients N]'); process.exit(2); }
const read = (f) => { try { return JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8')); } catch { return null; } };

const server = read('server.json');
const clients = [];
for (let i = 1; i <= Math.max(expected, 200); i++) { const c = read(`c${i}.json`); if (c) clients.push({ i, ...c }); else if (i > expected) break; }

const rows = [];
const check = (name, ok, detail) => rows.push({ name, ok, detail });
const num = (v) => (typeof v === 'number' ? v : 0);
const fmt = (v, d = 2) => (typeof v === 'number' ? v.toFixed(d) : String(v));

if (!server) check('server stats written', false, 'server.json missing');
else {
  check('server stats written', true, `players peak ${server.playersPeak}`);
  check('server tick p99 < 20 ms', num(server.tickMs?.p99) < 20, `avg ${fmt(server.tickMs?.avg, 3)} p50 ${fmt(server.tickMs?.p50, 3)} p99 ${fmt(server.tickMs?.p99, 3)} max ${fmt(server.tickMs?.max, 3)} ms (${server.tickMs?.n} ticks)`);
  check('snapshot <= 1000 B, none over cap', num(server.snapshotMaxBytes) <= 1000 && num(server.snapshotsOverCap) === 0, `max ${server.snapshotMaxBytes} B, avg ${fmt(num(server.snapshotBytes) / Math.max(1, num(server.snapshotsSent)), 1)} B, ${server.snapshotsSent} sent`);
  check('server exceptions = 0', num(server.exceptions) === 0, `${num(server.exceptions)} ${(server.exceptionTexts || []).join(' | ')}`);
  check('nobody kicked / malformed', num(server.kicked) === 0, `kicked ${num(server.kicked)}`);
  check('client down <= 20 KB/s (server view)', num(server.clientDownKBps?.max) <= 20, `avg ${fmt(server.clientDownKBps?.avg)} max ${fmt(server.clientDownKBps?.max)} KB/s over ${server.clientDownKBps?.n} clients`);
  check(`client up <= ${upBudget} KB/s (server view)`, num(server.clientUpKBps?.max) <= upBudget, `avg ${fmt(server.clientUpKBps?.avg)} max ${fmt(server.clientUpKBps?.max)} KB/s`);
  check('boss killed at least once', num(server.bossDeaths) >= 1, `bossDeaths ${num(server.bossDeaths)}, zone kills ${num(server.zoneKills)}`);
  check('events batched', num(server.eventsSent) > 0, `${num(server.eventsSent)} events, ${fmt(num(server.eventBytes) / Math.max(1, num(server.eventBatchesSent)), 1)} B per batch`);
}

check(`all ${expected} clients wrote stats`, clients.length >= expected, `${clients.length} / ${expected}`);
const joined = clients.filter((c) => c.state === 'Joined');
check('every client still joined at the end', joined.length === clients.length && clients.length > 0, `${joined.length} / ${clients.length}`);
check('client exceptions = 0', clients.every((c) => num(c.exceptions) === 0), clients.filter((c) => num(c.exceptions) > 0).map((c) => `c${c.i}: ${(c.exceptionTexts || [])[0]}`).join(' | ') || '0');
check('no malformed packets', clients.every((c) => num(c.malformedPackets) === 0), `${clients.reduce((a, c) => a + num(c.malformedPackets), 0)}`);
if (clients.length) {
  const snapRate = clients.map((c) => num(c.snapshotsReceived) / Math.max(1, num(c.uptimeSeconds) - num(c.joinedAtSeconds)));
  check('snapshots ~15 Hz at every client', snapRate.every((r) => r > 11), `min ${fmt(Math.min(...snapRate), 1)} max ${fmt(Math.max(...snapRate), 1)} per s`);
  const down = clients.map((c) => num(c.downKBps)), up = clients.map((c) => num(c.upKBps));
  check('client down <= 20 KB/s (client view)', Math.max(...down) <= 20, `avg ${fmt(down.reduce((a, b) => a + b, 0) / down.length)} max ${fmt(Math.max(...down))} KB/s`);
  check(`client up <= ${upBudget} KB/s (client view)`, Math.max(...up) <= upBudget, `avg ${fmt(up.reduce((a, b) => a + b, 0) / up.length)} max ${fmt(Math.max(...up))} KB/s`);
  check('every client moved and was hit-sync active', clients.every((c) => num(c.localDistance) > 50 && num(c.monsterHitsSeen) > 0), clients.filter((c) => !(num(c.localDistance) > 50 && num(c.monsterHitsSeen) > 0)).map((c) => `c${c.i}`).join(',') || 'all');
  check('EXP split reached the fighters', clients.filter((c) => num(c.expGained) > 0).length >= Math.ceil(clients.length * 0.75), `${clients.filter((c) => num(c.expGained) > 0).length} / ${clients.length} gained EXP, total ${clients.reduce((a, c) => a + num(c.expGained), 0)}`);
  check('players see each other', clients.every((c) => num(c.heroesSeenDistinct) >= Math.min(clients.length, 3)), `min distinct heroes seen ${Math.min(...clients.map((c) => num(c.heroesSeenDistinct)))}`);
  // boss death: every client got the zone-wide event within 200 ms of the server emitting it
  const t0 = (server?.bossDiedUtcMs || []).map(Number)[0];
  if (t0) {
    const lag = clients.map((c) => ((c.bossDiedRecvUtcMs || []).map(Number)[0] ?? NaN) - t0);
    const seen = lag.filter((v) => !Number.isNaN(v));
    check('boss death seen by every client (+-200 ms)', seen.length === clients.length && seen.every((v) => Math.abs(v) <= 200), `${seen.length} / ${clients.length} saw it, lag min ${fmt(Math.min(...seen), 0)} max ${fmt(Math.max(...seen), 0)} ms`);
  }
  console.log(`\nper client: id class-level exp gold items | down/up KB/s | rtt | hits seen | boss seen`);
  for (const c of clients) console.log(`  c${c.i}: lv ${num(c.finalLevel)} exp ${num(c.finalExp)} gold ${num(c.finalGold)} items ${num(c.finalInventory)} | ${fmt(num(c.downKBps))}/${fmt(num(c.upKBps))} | ${fmt(num(c.rttMs), 0)} ms | ${num(c.monsterHitsSeen)} | ${num(c.bossDiedSeen)}`);
}

console.log('\n' + rows.map((r) => `${r.ok ? 'PASS' : 'FAIL'}  ${r.name}  --  ${r.detail}`).join('\n'));
const failed = rows.filter((r) => !r.ok).length;
console.log(`\n${failed === 0 ? 'ALL CHECKS PASSED' : failed + ' CHECK(S) FAILED'}`);
fs.writeFileSync(path.join(dir, 'summary.json'), JSON.stringify({ rows, server, clients }, null, 2));
process.exit(failed === 0 ? 0 : 1);

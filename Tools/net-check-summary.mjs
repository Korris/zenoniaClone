// Budget check for Tools/net-local-test.ps1 output: node net-check-summary.mjs <dir> [--clients N] [--lag]  (--lag: also enforce the prediction budgets)
// Reads server.json + c<i>.json (written by `--net-stats`), prints a table of numbers and PASS / FAIL per rule, exits 1 when a rule fails.
import fs from 'node:fs';
import path from 'node:path';

const dir = process.argv[2];
const args = process.argv.slice(3);
const argVal = (k, d) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : d; };
const expected = Number(argVal('--clients', 0));
const tolerance = Number(argVal('--tolerance-ms', 200));
const metrics = {}; // machine readable numbers for Tools/net-lag-matrix.ps1 (written to summary.json)
const upBudget = 1.0; // plan: <= 1 KB/s up (input frames are 12 B per packet since protocol v2)

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
  // The server-side figure counts a datagram that carries merged packets (input + acks) once per packet, so it over-reports upstream by ~1.2-1.5x: informational only.
  check(`client up <= ${upBudget * 1.5} KB/s (server view, over-counts merged datagrams)`, num(server.clientUpKBps?.max) <= upBudget * 1.5, `avg ${fmt(server.clientUpKBps?.avg)} max ${fmt(server.clientUpKBps?.max)} KB/s`);
  check('boss killed at least once', num(server.bossDeaths) >= 1, `bossDeaths ${num(server.bossDeaths)}, zone kills ${num(server.zoneKills)}`);
  if (num(server.skillCastsServer) > 0) // skills bot run: casts happened on the server and every client replayed other players' casts
    check('skills cast and replayed', clients.every((c) => num(c.skillCastsSeen) > 0), `${num(server.skillCastsServer)} casts on the server, seen by clients: ${clients.map((c) => num(c.skillCastsSeen)).join(',')}`);
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
  metrics.downKBpsAvg = down.reduce((a, b) => a + b, 0) / down.length; metrics.upKBpsAvg = up.reduce((a, b) => a + b, 0) / up.length;
  metrics.rttMsAvg = clients.reduce((a, c) => a + num(c.rttMs), 0) / clients.length;
  metrics.snapshotAvgBytes = clients.reduce((a, c) => a + num(c.snapshotBytesTotal), 0) / Math.max(1, clients.reduce((a, c) => a + num(c.snapshotsReceived), 0));
  check('client down <= 20 KB/s (client view)', Math.max(...down) <= 20, `avg ${fmt(down.reduce((a, b) => a + b, 0) / down.length)} max ${fmt(Math.max(...down))} KB/s`);
  check(`client up <= ${upBudget} KB/s (client view)`, Math.max(...up) <= upBudget, `avg ${fmt(up.reduce((a, b) => a + b, 0) / up.length)} max ${fmt(Math.max(...up))} KB/s`);
  check('every client moved and was hit-sync active', clients.every((c) => num(c.localDistance) > 50 && (num(c.monsterHitsSeen) > 0 || num(c.inputToMotionMs?.n) > 0)), clients.filter((c) => !(num(c.localDistance) > 50 && (num(c.monsterHitsSeen) > 0 || num(c.inputToMotionMs?.n) > 0))).map((c) => `c${c.i}`).join(',') || 'all');
  check('EXP split reached the fighters', clients.filter((c) => num(c.expGained) > 0).length >= Math.ceil(clients.length * 0.5), `${clients.filter((c) => num(c.expGained) > 0).length} / ${clients.length} gained EXP, total ${clients.reduce((a, c) => a + num(c.expGained), 0)}`);
  check('players see each other', clients.every((c) => num(c.heroesSeenDistinct) >= Math.min(clients.length, 3)), `min distinct heroes seen ${Math.min(...clients.map((c) => num(c.heroesSeenDistinct)))}`);
  // boss death: every client got the zone-wide event within 200 ms of the server emitting it
  const t0 = (server?.bossDiedUtcMs || []).map(Number)[0];
  if (t0) {
    const lag = clients.map((c) => ((c.bossDiedRecvUtcMs || []).map(Number)[0] ?? NaN) - t0);
    const seen = lag.filter((v) => !Number.isNaN(v));
    check(`boss death seen by every client (+-${tolerance} ms)`, seen.length === clients.length && seen.every((v) => Math.abs(v) <= tolerance), `${seen.length} / ${clients.length} saw it, lag min ${fmt(Math.min(...seen), 0)} max ${fmt(Math.max(...seen), 0)} ms`);
  }
  // prediction: how far the server moved the predicted hero (units per reconcile, zeros included) and how often
  const itm = clients.filter((c) => c.inputToMotionMs && num(c.inputToMotionMs.n) > 0);
  if (itm.length) { const all = itm.reduce((a, c) => a + num(c.inputToMotionMs.n), 0); metrics.inputToMotionMsAvg = itm.reduce((a, c) => a + num(c.inputToMotionMs.avg) * num(c.inputToMotionMs.n), 0) / all; metrics.inputToMotionMsP99 = Math.max(...itm.map((c) => num(c.inputToMotionMs.p99))); metrics.inputToMotionSamples = all; }
  if (itm.length) console.log(`input-to-motion (key down -> hero moved 0.5 u): avg ${fmt(itm.reduce((a, c) => a + num(c.inputToMotionMs.avg), 0) / itm.length, 0)} ms, p50 ${fmt(itm.reduce((a, c) => a + num(c.inputToMotionMs.p50), 0) / itm.length, 0)}, worst p99 ${fmt(Math.max(...itm.map((c) => num(c.inputToMotionMs.p99))), 0)} ms (${itm.reduce((a, c) => a + num(c.inputToMotionMs.n), 0)} samples)`);
  const predicted = clients.filter((c) => c.correctionUnits && num(c.correctionUnits.n) > 0);
  if (predicted.length) {
    const n = predicted.reduce((a, c) => a + num(c.correctionUnits.n), 0);
    const mean = predicted.reduce((a, c) => a + num(c.correctionUnits.avg) * num(c.correctionUnits.n), 0) / n;
    const p99 = Math.max(...predicted.map((c) => num(c.correctionUnits.p99))), max = Math.max(...predicted.map((c) => num(c.correctionUnits.max)));
    const corrected = predicted.reduce((a, c) => a + num(c.reconcilesCorrected), 0);
    metrics.correctionMean = mean; metrics.correctionP99 = p99; metrics.correctionMax = max; metrics.reconciles = n; metrics.reconcilesCorrected = corrected;
    check('prediction: mean correction < 1.5 units', mean < 1.5, `mean ${fmt(mean, 3)} p99(worst client) ${fmt(p99)} max ${fmt(max)} units; ${corrected} of ${n} reconciles corrected`);
    // rubber band = a correction that is not a server-side death / respawn / blink (those legitimately move the hero)
    const rub = clients.filter((c) => c.correctionRubber && num(c.correctionRubber.n) > 0), tele = clients.filter((c) => c.correctionTeleport && num(c.correctionTeleport.n) > 0);
    const rubMax = rub.length ? Math.max(...rub.map((c) => num(c.correctionRubber.max))) : 0, rubP99 = rub.length ? Math.max(...rub.map((c) => num(c.correctionRubber.p99))) : 0;
    metrics.rubberCount = rub.reduce((a, c) => a + num(c.correctionRubber.n), 0); metrics.rubberMax = rubMax; metrics.rubberP99 = rubP99; metrics.teleportCount = tele.reduce((a, c) => a + num(c.correctionTeleport.n), 0);
    if (args.includes('--lag')) check('prediction: no rubber band > 8 units', rubMax <= 8, `${metrics.rubberCount} rubber-band corrections (max ${fmt(rubMax)}, worst p99 ${fmt(rubP99)}), ${metrics.teleportCount} death / respawn / blink teleports`);
    const delay = clients.map((c) => num(c.interpDelayMs?.avg)).filter((v) => v > 0), jit = clients.map((c) => num(c.arrivalJitterMs?.avg));
    if (delay.length) { metrics.interpDelayMsAvg = delay.reduce((a, b) => a + b, 0) / delay.length; metrics.arrivalJitterMsAvg = jit.reduce((a, b) => a + b, 0) / jit.length; }
    if (delay.length) console.log(`interpolation delay avg ${fmt(delay.reduce((a, b) => a + b, 0) / delay.length, 0)} ms (max ${fmt(Math.max(...delay), 0)}), arrival jitter avg ${fmt(jit.reduce((a, b) => a + b, 0) / jit.length, 1)} ms`);
  }
  console.log(`\nper client: id class-level exp gold items | down/up KB/s | rtt | hits seen | boss seen`);
  for (const c of clients) console.log(`  c${c.i}: lv ${num(c.finalLevel)} exp ${num(c.finalExp)} gold ${num(c.finalGold)} items ${num(c.finalInventory)} | ${fmt(num(c.downKBps))}/${fmt(num(c.upKBps))} | ${fmt(num(c.rttMs), 0)} ms | ${num(c.monsterHitsSeen)} | ${num(c.bossDiedSeen)}`);
}

console.log('\n' + rows.map((r) => `${r.ok ? 'PASS' : 'FAIL'}  ${r.name}  --  ${r.detail}`).join('\n'));
const failed = rows.filter((r) => !r.ok).length;
console.log(`\n${failed === 0 ? 'ALL CHECKS PASSED' : failed + ' CHECK(S) FAILED'}`);
if (server) { metrics.tickP99Ms = num(server.tickMs?.p99); metrics.tickAvgMs = num(server.tickMs?.avg); metrics.inputFramesDropped = num(server.inputFramesDropped); metrics.inputStepsStarved = num(server.inputStepsStarved); metrics.inputCatchUps = num(server.inputCatchUps); metrics.snapshotsFull = num(server.snapshotsFull); metrics.snapshotsSent = num(server.snapshotsSent); }
metrics.failed = failed; metrics.clients = clients.length;
fs.writeFileSync(path.join(dir, 'summary.json'), JSON.stringify({ metrics, rows, server, clients }, null, 2));
process.exit(failed === 0 ? 0 : 1);

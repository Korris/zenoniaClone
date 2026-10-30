# Phase 08 - Deploy/ops: Linux server build, Docker, VPS, zone scaling, monitoring, anti-cheat basics

## Context links
- `Assets/Editor/PlayerBuildScript.cs`, `Tools/build-from-copy.ps1` (P1), `server-api/` (P3-P6), P4 zone registry, P2 bot swarm

## Overview
Priority P2, pending, 18h. Linux dedicated server build, Docker images (api, zone-process, mongo replica set, caddy), VPS deploy, zone placement across
processes/hosts, metrics/logs/alerts, backups, anti-cheat basics.

## Key insights
- Needs Unity Hub module "Linux Dedicated Server Build Support" (not installed) - user action.
- Dedicated Server subtarget strips rendering; confirm map collision does not depend on textures (tile data) and headless skips sprite loads (P1 step 8).
- Zone placement static config first (`zones.yml`: process -> zones -> port); auto-balancing later (YAGNI). Hot map -> separate process; channels if > maxPlayers.

## Requirements
- `PlayerBuildScript.BuildLinuxServer` (`StandaloneBuildSubtarget.Server`, `-buildOut`).
- Compose: `api`, `zone-a` (frost-hollow), `zone-b` (oakhaven + arena), `mongo` (single-node replica set, auth, volume), `caddy` (TLS for API). UDP ports published per zone container.
- Metrics: `/metrics` Prometheus text on API and zones (CCU per zone, tick p50/p99, bytes in/out, saves/s, API latency, errors); JSON logs; uptime alert (Telegram/Discord webhook).
- Backups: nightly `mongodump`, 14-day retention, restore drill documented.
- Anti-cheat basics: server authority (already); input/packet rate caps; impossible-action counters (presses above cap, portal outside rect, InvAction unknown uid) -> log + auto-kick threshold; economy anomaly query (gold/hour outliers); client version check; GM ban endpoint.

## Architecture
VPS 4 vCPU / 8 GB start (size from P2 load: CCU per zone process x RAM). Firewall: 22 (key only), 443/tcp, zone UDP ports. Mongo internal only.

## Related code files
Create: `deploy/docker-compose.yml`, `deploy/Dockerfile.api`, `deploy/Dockerfile.zone`, `deploy/Caddyfile`, `deploy/zones.yml`, `deploy/.env.example`, `deploy/backup-mongo.sh`, `deploy/README.md`,
`Tools/build-linux-server-from-copy.ps1`, `Scripts/Net/Server/ZoneMetrics.cs`, `Scripts/Net/Server/CheatHeuristics.cs`, `server-api/src/routes/gm-routes.js`
Modify: `Assets/Editor/PlayerBuildScript.cs`, `NetLaunchArgs.cs` (`--log-json`, `--zones-config`), `ZoneServer.cs` (metrics, heuristics hooks)

## Implementation steps
1. Install module; Linux server build from copy; run in Docker locally; bot swarm against it.
2. Dockerfiles + compose (replica set init); local soak 1 h with 200 bots.
3. VPS provision, firewall, TLS, deploy by image tag (git sha).
4. Metrics, logs, alerts, backups + restore drill.
5. Cheat heuristics + GM ban/kick.
6. Android on 4G (3 phones) to prod; record RTT and reconnects.

## Todo
- [ ] Linux build
- [ ] Docker + compose
- [ ] VPS + TLS + firewall
- [ ] Metrics/logs/alerts/backups
- [ ] Anti-cheat basics + GM tools
- [ ] Mobile network test

## Success criteria
- 1 h soak 200 bots on VPS: tick p99 < 33 ms, zero crashes, RAM per zone process logged and stable.
- Restore drill restores yesterday's dump in < 15 min.
- 3 phones on 4G complete a boss fight; reconnect after network switch (wifi->4G) within 10 s.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Carrier NAT/UDP drops | M x H | 1 s keepalive, reconnect with session resume (ticket refresh), server public IP |
| Linux build differences (culture, paths) | M x M | InvariantCulture, run harness on Linux image |
| Single VPS SPOF | M x M | restart policies, backups; second host later |
| Cost growth | L x M | scale by measured CCU/process |

## Security
Secrets only in VPS `.env`; Mongo private + auth; TLS; SSH key-only + fail2ban; containers non-root; service key rotation; rate limits from P3.

## Rollback
Images tagged by sha; `docker compose up -d` previous tag; mongodump restore.

## Next steps
Second region / autoscaling only when CCU demands.

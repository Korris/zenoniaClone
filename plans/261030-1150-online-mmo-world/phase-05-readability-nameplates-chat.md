# Phase 05 - Own-character readability, nameplates, chat

## Context links
- `HeroRenderer.cs`, `GameConfig.OrderBase`, `FeelConfig.cs`, `GameSfx.cs`, FX pools (`SlashEffect`, `SwordTrail`, `ImpactFx`, `BurstFx`, `ComboProjectile`, `UltimateFx`); P1 `HeroFx`, `LocalPlayer`; P3 API

## Overview
Priority P2, pending, 18h. Part A (after P1): ring under own hero, coloured nameplates (self / party / other / guild later), own hero sorted above other heroes,
other players' FX dimmed (alpha, no glow/shake/flash/hit-stop), SFX voice limiting, remote hero LOD cap. Part B (after P3/P4): chat - map, global, whisper, party.

## Key insights
- All FX enter via few static spawners -> one `local` flag through `HeroFx` instead of per-renderer edits.
- Map chat is zone-local (zone server relays); global/whisper/party cross processes -> API "world bus" WebSocket that zone servers connect to (service key). Redis only if API scales out (YAGNI).
- 50+ heroes on Android: full HeroRenderer for nearest 30, others simplified (body only, no gear overlays/companions).

## Requirements
- `FeelConfig.RemoteFxAlpha = 0.45`, remote glow off, remote shake/flash/hit-stop off; remote SFX volume 0.5, max 3 voices per clip group, global cap 16.
- Nameplate: name (+ level), HP bar for party; colours self gold, party green, other white.
- Chat: C2S `Chat{channel(Map,Global,Whisper,Party), target, text}` op 5; S2C `ChatMsg{channel, fromName, text, time}` op 21 (ch2). 120 chars, 1 msg/s, global 1 msg/10 s, strip rich-text tags, mute list, server word filter hook.

## Related code files
Create: `Scripts/OwnHeroMarker.cs`, `Scripts/HeroNameplate.cs`, `Scripts/RemoteHeroLod.cs`, `Scripts/Net/Client/ChatGui.cs`, `Scripts/Net/Server/ChatRelay.cs`, `Scripts/Net/Server/WorldBusClient.cs`, `server-api/src/bus/world-bus.js`
Modify: `HeroFx.cs`, `HeroRenderer.cs` (order band, LOD), `SlashEffect.cs`, `SwordTrail.cs`, `ImpactFx.cs`, `BurstFx.cs`, `ComboProjectile.cs`, `UltimateFx.cs`, `GameSfx.cs`, `FeelConfig.cs`, `Net/Protocol/NetOpcodes.cs`
Ownership: P5 owns FX/renderer files above; P2 must not edit them.

## Implementation steps
1. local flag plumbing; remote FX rules; ring + sorting; nameplates; SFX limiter; LOD cap.
2. 50-bot record-net video review (Android + PC FPS).
3. World bus (API WS, auth by service key, presence map name->zone).
4. ChatRelay (map local, others via bus), rate limits, filter; ChatGui (touch-friendly, toggles).

## Todo
- [ ] Readability rules + ring + nameplates
- [ ] SFX limiter + LOD
- [ ] Video/FPS review
- [ ] World bus
- [ ] Chat relay + UI

## Success criteria
- Own hero identifiable in every frame of a 50-hero video (user review); Android >= 50 FPS with 50 heroes visible.
- Whisper across two processes < 300 ms; spam beyond limits dropped with toast.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Offline look changes | L x M | local = true offline |
| Bus single point of failure | M x M | zone degrades to map chat only; reconnect backoff |
| Chat abuse | M x M | limits, mute, report log in Mongo |

## Security
Server-side sanitization and rate limits; whisper target resolved server side (no client-provided ids trusted).

## Next steps
P6 party/guild channels on the same bus.

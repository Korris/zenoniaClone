# Phase 07 (DEFERRED, not approved yet): protocol + zone look

Do not start without user approval. Depends on phase 06.

## Context
`Assets/Scripts/Net/Protocol/NetEventTypes.cs` (`NetAppearance.Read/Write`, `HairStyle` last byte), `Net/NetEventBus.cs:167`, `HeroController.Net.cs` (`NetHairStyle`, `SyncHairStyleOnline`, `ProxyLook`), `Net/Protocol/NetOpcodes.cs` (`InvOp.HairStyle = 13`), `Net/Server/ZonePersistence.cs`, `ZoneApiHttp.cs` (redeem).

## Requirements
- Zone loads `look` from redeem -> sets `NetHairStyle` + new `NetHairDye` before the first Appearance event, so joiners and others see it without the client sending anything.
- `NetAppearance` + `HairDye` byte (sbyte semantics, 255 = -1). This changes the wire: bump the protocol version checked at Hello so mismatched client/zone builds are refused with a clear message, not misparsed.
- `InvOp.HairStyle` keeps working (H key); add dye in the same op (Slot = style, extra byte = dye) or keep dye fixed in game (decision: question 4 in plan.md). In-game change persisted via the normal character save if `look` is moved into save path, else not persisted (state it).
- `ProxyLook` applies `HairDye` via `HeroRenderer.HairDye`.
- Tests: ZenoniaNet.Tests round trip of `NetAppearance` with dye; version mismatch refused.

## Risks
Client/zone version skew (High x Med): ship zone + client together; version bump makes skew visible. Skill table untouched, so no server skill rebuild needed beyond the normal zone build.

# Phase 06 - Party + EXP share, then guild, trade, vendor shop

## Context links
- P1 EXP split / loot owner; P3 API ledger; P5 world bus/chat

## Overview
Priority P2, pending, 24h. Order: party (MVP) -> trade -> vendor shop -> guild (basic). All server-authoritative, ledgered in Mongo.

## Key insights
- Party is cross-zone state -> lives in API, pushed to zones via world bus; zone uses it for EXP share and loot priority.
- Trade/vendor touch two characters' inventories atomically -> do in API (Mongo transaction, replica set required), zone just forwards and refreshes state. Zone must save both characters first (flush) and lock their inventories during trade.

## Requirements
- Party: max 5, invite/accept/leave/kick, leader; EXP share among members within 400 units on same zone (even split + 10 %/member bonus, RO-like, tune later); loot priority extends to party for +2 s.
- Trade: request, both add items/gold, both confirm twice, API transaction, ledger; cancel on move > 100 units or zone change.
- Vendor shop (player shop): stall with up to 8 items + prices, buyer purchase via API transaction; stall despawns on logout.
- Guild (basic): create (gold cost), invite, ranks leader/member, guild chat channel, emblem later.

## Related code files
Create: `server-api/src/routes/party-routes.js`, `trade-routes.js`, `vendor-routes.js`, `guild-routes.js`, `src/economy/inventory-transaction.js`; `Scripts/Net/Server/PartyCache.cs`, `TradeSession.cs`, `VendorStall.cs`; `Scripts/Net/Client/PartyGui.cs`, `TradeWindow.cs`, `VendorWindow.cs`, `GuildGui.cs`
Modify: `PlayerRpg.cs` (EXP share hook), `Scripts/World/LootOwnership.cs`, `Net/Protocol/*` (Party/Trade/Vendor ops), `HeroNameplate.cs` (party/guild colour)

## Implementation steps
1. Party API + bus push + PartyGui; EXP share + loot priority.
2. Inventory transaction module (Mongo transactions) + ledger; tests incl. concurrency.
3. Trade flow + UI.
4. Vendor stall + UI.
5. Guild basic + guild chat.

## Todo
- [ ] Party + EXP share
- [ ] Inventory transactions
- [ ] Trade
- [ ] Vendor
- [ ] Guild basic

## Success criteria
- Concurrency test: 1000 random parallel trades -> item/gold totals conserved, 0 dupes.
- Party of 3 in range gets shared EXP per formula (unit tests).

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Dupes via trade + save race | M x H | flush saves, lock inventory, transaction, version bump; zone reloads state after trade |
| Mongo transactions need replica set | H x M | single-node replica set in Docker (P8) |
| Scope creep | H x M | ship party first; guild minimal |

## Security
All economy ops server-side with ledger; trade confirm tokens; rate limits; audit queries for GMs.

## Next steps
Guild wars / PvP ties into P7.

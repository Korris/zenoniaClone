# Phase 04: Client, tracker, chat line, NPC board

## Context
- `Net/Client/ClientRpgMirror.cs` `OnRpgState`: restores the full state from `SaveJson` and calls `QuestTracker.Observe(state.Quests, questsKnown)` (the first push of a connection is "arrival", not news). `wantedDay` arrives the same way, so there is **no wire change**.
- `QuestTracker.cs` lines 59-70: `ChatHub.System(...)` and `NotificationToast.Show(title, text, icon)`. Copy this pattern.
- Chat is client-local (`ChatHub` has only `LocalChatTransport`; there is no network chat), so the server announces nothing in chat. The client writes its own system line.
- `TownNpc.Talk()`: quest giver first, then potion seller, then `NpcDialogueBox.Open(name, portrait, lines, null)`. Villagers exist online too.
- `NpcDefs`: `guardRook` (Oakhaven, at the south gate toward the boss maps) and `bountyLeila` (Sunspire, a bounty board line that says "(Bounties: coming soon)").

## Overview
Priority P2. Online only (`!NetMode.IsOffline`). Offline the NPCs keep their usual lines. No new art: the dialogue box, the NPC faces, the chat and `NotificationToast` (with the existing coin/stone icon from `LootSprites`) all exist already.

## Requirements
- **On arrival** (the first RpgState of a connection), one system line:
  - not claimed: `Wanted today: Glacier Warden in Frostfang Den. Reward: +600 G, +5 Upgrade Stones, +3/+3 Medium Potions. Resets 05:00.`;
  - claimed: `Wanted reward claimed today. Next wanted boss in 6h 12m.`
- **On claim** (`WantedDay` rises during a session): `ChatHub.System("Wanted boss defeated! " + RewardText)` and `NotificationToast.Show("Wanted Boss!", RewardText, coin icon)`.
- **Board**: talking to `guardRook` or `bountyLeila` online opens `NpcDialogueBox` with dynamic pages: today's boss, map name (`MapRegistry.Get(def.MapId).Name`), boss level, reward, claimed or not, and time to reset. Page 2 keeps the NPC's normal first line.
- Today's boss and the reset countdown on the client come from `WantedBoss.Today(device UTC)`. This is display only; the zone decides. "Claimed" is `state.WantedDay >= WantedBoss.DayKey(deviceUtc)`.
- Optional, not in v1: a "Daily: Wanted Boss" row in `QuestLogWindow`, a minimap marker, a quest-style "!" over Guard Rook, and a bounty-board prop art.

## Architecture
`ClientRpgMirror.OnRpgState` -> `WantedBossTracker.Observe(state.WantedDay, known)` keeps the last seen day and announces when it rises. `TownNpc.Talk` -> `WantedBossBoard.TryOpen(npcId, name, portrait, defaultLines)` returns true when it handled the NPC.

## Related code files
- Create: `Assets/Scripts/WantedBossTracker.cs` (~50 lines: arrival line, claim announcement, `TimeToReset` text).
- Create: `Assets/Scripts/WantedBossBoard.cs` (~50 lines: board NPC ids and page text).
- Modify: `Assets/Scripts/Net/Client/ClientRpgMirror.cs` (1 line next to `QuestTracker.Observe`).
- Modify: `Assets/Scripts/TownNpc.cs` (1 line before the `NpcDialogueBox.Open` fallback; the file is 105 lines).
- Reuse `PlayerState.RewardText` from phase 01 (DRY for the text).

## Implementation steps
1. `WantedBossTracker.Observe(int day, bool known)`:
   - if `!known`, print the arrival line;
   - else if `day > last`, announce the claim;
   - store `last`, and reset it on disconnect (`ResetState`, like the other static helpers with Domain Reload off).
2. `WantedBossBoard.TryOpen`: return false when offline or the id is not a board NPC; otherwise build the pages and call `NpcDialogueBox.Open`.
3. Hook both into the mirror and `TownNpc.Talk`.
4. Build from copy (`Tools/build-from-copy.ps1`). Run the client against a dev host (phase 05) and **look at** the dialogue box and chat line in a screenshot. Long text must not overflow the box; split it into pages.

## Todo
- [ ] WantedBossTracker.cs
- [ ] WantedBossBoard.cs
- [ ] mirror + TownNpc hooks
- [ ] build from copy, 0 errors
- [ ] screenshot of board + chat line + toast viewed

## Success criteria
- Online: arrival line once per connection (not on every RpgState push); claim toast + chat line once per claim; the board shows the right boss, map and claimed state; offline shows the NPCs' normal chatter.
- Screenshots in `docs/reference/wanted-boss-board-ingame.png` and `wanted-boss-claim-chat-ingame.png`.

## Risks
| Risk | L x I | Mitigation |
|---|---|---|
| Device clock off near the reset: the board shows the wrong boss or claimed state for a few minutes | L x L | Display only; the claim toast comes from the server-driven state. Accepted for v1. |
| Arrival line repeats on a zone change (new connection?) | M x L | Zone changes keep the connection (`ZoneChanged`). If `questsKnown` resets, apply the same gate. Check in e2e. |
| Text too long for `NpcDialogueBox` or mobile width | M x L | 2 short pages; check in the screenshot (web + mobile HUD `--hud web/mobile`). |
| New client against an old zone shows "not claimed" forever | L x L | Ship the zone and client together (plan.md). |

## Security
The client only reads. It never sends anything for this feature.

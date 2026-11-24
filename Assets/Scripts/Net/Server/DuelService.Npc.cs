using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The duel with Card Master Orin, the ONLY NPC duel (card tutorial quest). PlayNpc A = 0: the scripted tutorial duel with the borrowed deck, only while quest first-card-duel is Active; only a WIN moves the quest to
    /// ReadyToTurnIn (a loss, draw or give-up leaves it Active: replay for free, nothing is lost or duplicated). A = 1: a practice duel, only once the quest is Done; no reward, no stakes, nothing saved. No turn clock, but 10 minutes without a player action drops the duel. A player who dies, leaves the safe map, idles out or the zone drops the duel
    /// WITHOUT moving the quest (the deck is borrowed, nothing to give back): they play it again. The reward itself is paid at the quest turn-in, never here.
    /// </summary>
    public sealed partial class DuelService
    {
        bool NpcRunning(Entry e) { return e.Npc != null && !e.Npc.Finished; }

        void PlayNpc(Entry e, byte mode, long now)
        {
            NetPlayer p = e.Player;
            if (mode > 1) { Refuse(p, DuelFail.BadMove); return; }
            if (Running(p.PlayerId)) { Refuse(p, DuelFail.InDuel); return; }
            if (lobby.IsBusy(p.PlayerId) || queue.Contains(p.PlayerId)) { Refuse(p, DuelFail.Busy); return; }
            DuelFail why = StartRefusal(p);
            if (why != DuelFail.None) { Refuse(p, why); return; }
            if (ServerInventoryActions.DistanceToNpc(p, ServerInventoryActions.HeroMap(p), CardDuelQuest.GiverId) > QuestCatalog.ReachUnits) { Refuse(p, DuelFail.TooFar); return; }
            QuestState quest = p.Rpg.State.Quests.StateOf(CardDuelQuest.First);
            if (mode == 0 ? quest != QuestState.Active : quest != QuestState.Done) { Refuse(p, DuelFail.NoQuest); return; }
            e.Npc = mode == 0 ? DuelNpcSession.Tutorial(now, NewSeed()) : DuelNpcSession.Practice(NewSeed(), DuelNpcSession.PracticeDeck(p.Rpg.State.Cards), now);
            stats.Add(mode == 0 ? "duelTutorialStarted" : "duelPracticeStarted", 1);
            SendView(p, e.Npc.View(now));
        }

        void PlaceNpc(Entry e, DuelRequestMsg m, long now)
        {
            DuelPlay r = e.Npc.PlayerPlace(m.A, m.B, now, m.Turn, m.Card);
            if (!r.Ok) { Refuse(e.Player, PlayFailure(r)); SendView(e.Player, e.Npc.View(now)); return; }
            if (e.Npc.Finished) FinishNpc(e, now); else SendView(e.Player, e.Npc.View(now));
        }

        void ForfeitNpc(Entry e, long now)
        {
            e.Npc.Forfeit(); // giving up counts like a loss: the quest stays Active
            FinishNpc(e, now);
        }

        void SkipScriptNpc(Entry e, long now)
        {
            if (e.Npc.SkipScript(now)) SendView(e.Player, e.Npc.View(now)); else Refuse(e.Player, DuelFail.BadMove);
        }

        /// <summary>The duel is over by play or by the player giving up: the final view goes out and a tutorial WIN moves the quest, exactly once (TakeResult).</summary>
        void FinishNpc(Entry e, long now)
        {
            NetPlayer p = e.Player;
            DuelNpcSession s = e.Npc;
            // the quest moves and its state goes out BEFORE the final view: the result banner reads the quest state (hand-in or cut short)
            if (s.Mode == DuelNpcMode.Tutorial && p.Hero != null)
            {
                DuelResult? result = s.TakeResult(); // once: a loss / draw / give-up leaves the quest Active (play again for free), only a win moves it
                if (result == DuelResult.Win)
                {
                    Zone.Enter(p.Hero.HomeZone);
                    p.Rpg.QuestCardDuel();
                    PushState(p);
                    if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
                    stats.Add("duelTutorialWon", 1);
                }
                Debug.Log("[ZenoniaProto] duel: " + p.Name + " finished the tutorial duel with Orin: " + result);
            }
            SendView(p, s.View(now));
        }

        /// <summary>Per tick: Orin's reply when due, and the end of a duel for a player who can no longer duel (dead / hostile map / 10 min idle): shown as a loss, the quest does NOT move.</summary>
        void TickNpc(long now)
        {
            foreach (Entry e in entries.Values)
            {
                if (!NpcRunning(e)) continue;
                if (!CanKeepDueling(e.Player.Hero) || e.Npc.Idle(now)) { e.Npc.Forfeit(); SendView(e.Player, e.Npc.View(now)); e.Npc = null; stats.Add("duelNpcAborted", 1); continue; }
                if (!e.Npc.Tick(now)) continue;
                if (e.Npc.Finished) FinishNpc(e, now); else SendView(e.Player, e.Npc.View(now));
            }
        }
    }
}

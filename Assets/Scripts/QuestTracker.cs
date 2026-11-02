using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The player's side of the quests. Requests go through the same InvOp rules everywhere: online to the zone server, which owns the state and saves it with the character; offline straight to the hero's PlayerRpg.
    /// Whatever then changes in the QuestLog is told to the player here, by comparing it with what was seen last, so online and offline say the same things. Client only: a zone server has no player to tell.
    /// </summary>
    public static class QuestTracker
    {
        struct Seen { public QuestState State; public int Progress; }
        static readonly Dictionary<string, Seen> LastSeen = new Dictionary<string, Seen>();

        /// <summary>Goes up whenever a quest was seen to change: the quest list redraws on it.</summary>
        public static int Stamp { get; private set; }

        /// <summary>This hero's quests. Null without a hero.</summary>
        public static QuestLog Log { get { HeroController h = GameSession.Hero; return h != null && h.Rpg != null ? h.Rpg.Quests : null; } }

        public static void Accept(QuestDef q) { Send(InvOp.QuestAccept, q); }
        public static void TurnIn(QuestDef q) { Send(InvOp.QuestTurnIn, q); }

        static void Send(InvOp op, QuestDef q)
        {
            if (NetMode.IsClient) { NetClient.SendInvAction(op, q.Id, 0); return; } // the answer's RpgState brings the change
            PlayerRpg rpg = LocalPlayer.Rpg;
            if (rpg != null) rpg.ApplyInvAction(op, q.Id, 0);
        }

        /// <summary>
        /// The state of the quests as they are now. `announce` is false when this is a character just arrived (a loaded save, the first RpgState of a connection):
        /// that only sets what the next comparison starts from, a quest finished last week is not news.
        /// </summary>
        public static void Observe(QuestLog log, bool announce)
        {
            foreach (QuestDef q in QuestCatalog.All)
            {
                var now = new Seen { State = log.StateOf(q), Progress = log.ProgressOf(q) };
                Seen before;
                if (!LastSeen.TryGetValue(q.Id, out before)) before = new Seen { State = QuestState.Available };
                LastSeen[q.Id] = now;
                if (now.State == before.State && now.Progress == before.Progress) continue;
                Stamp++;
                if (announce) Tell(q, now);
            }
        }

        static void Tell(QuestDef q, Seen now)
        {
            Sprite icon = UiArt.Win("quest-icon");
            switch (now.State)
            {
                case QuestState.Active:
                    if (now.Progress == 0)
                    {
                        ChatHub.System("Quest accepted: " + q.Title);
                        NotificationToast.Show("Quest Accepted", q.Title, icon);
                    }
                    else NotificationToast.Show(q.Title, "Defeat " + q.MonsterName + "  " + now.Progress + " / " + q.Count, icon);
                    break;
                case QuestState.ReadyToTurnIn:
                    ChatHub.System("Objective complete: return to " + q.GiverName);
                    NotificationToast.Show("Objective Complete", "Return to " + q.GiverName, icon);
                    break;
                case QuestState.Done:
                    ChatHub.System("Quest complete: " + q.Title + " (+" + q.RewardGold + " G, +" + q.RewardStones + " stones, +" + q.RewardHpPotions + " HP potions)");
                    NotificationToast.Show("Quest Complete!", q.Title + "  +" + q.RewardGold + " G", icon);
                    break;
            }
        }
    }
}

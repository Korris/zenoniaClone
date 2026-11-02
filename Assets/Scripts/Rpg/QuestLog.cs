using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>Static description of one kill quest (text, who gives it, monster type to hunt, rewards).</summary>
    public sealed class QuestDef
    {
        public string Id, Title, Description, MonsterType, MonsterName, GiverNpcId, GiverName;
        public int Count, RewardGold, RewardStones, RewardHpPotions;
        public string[] OfferLines, ActiveLines, ReadyLines, DoneLines;
    }

    public enum QuestState { Available = 0, Active = 1, ReadyToTurnIn = 2, Done = 3 }

    /// <summary>What a quest giver shows above its head and on the minimap: "?" = a quest to take, "!" = a quest to hand in.</summary>
    public enum QuestMarker { None, Available, Ready }

    /// <summary>Every quest in the game. The zone server checks a request against this and the distance to the giver.</summary>
    public static class QuestCatalog
    {
        /// <summary>World units a player may stand from the giver: the talk range plus some slack for movement the server saw last (same as PotionShops).</summary>
        public const float ReachUnits = 44f;

        public static readonly QuestDef[] All =
        {
            new QuestDef
            {
                Id = "crystal-survey", Title = "Ancient Crystal Survey", GiverNpcId = "elderRowan", GiverName = "Elder Thomas",
                Description = "The village elder is worried about the strange monsters near the crystal mines. Thin out the slimes and report back.",
                MonsterType = "frostSlime", MonsterName = "Frost Slime", Count = 5, RewardGold = 150, RewardStones = 3, RewardHpPotions = 2,
                OfferLines = new[]
                {
                    "These crystal mines have always been peaceful,\nbut lately, strange monsters have started to appear...",
                    "The Frost Slimes are the worst of it. Could you help us investigate and thin them out?",
                },
                ActiveLines = new[] { "Any luck with the Frost Slimes? They are all over the hollow.\nCome back when you have dealt with five of them." },
                ReadyLines = new[] { "You did it! The mines are quiet again.", "Please take this as thanks. Use the stones well, traveller." },
                DoneLines = new[] { "Thank you again. The hollow is safer thanks to you." },
            },
        };

        public static QuestDef Get(string id)
        {
            foreach (QuestDef q in All) if (q.Id == id) return q;
            return null;
        }

        /// <summary>Null when this player may talk the quest over with its giver. Never the client's word: the distance comes from where the server has the hero.</summary>
        public static string Refusal(string questId, float distanceToGiver)
        {
            QuestDef q = Get(questId);
            if (q == null) return "No such quest";
            return distanceToGiver > ReachUnits ? "Too far from " + q.GiverName : null;
        }
    }

    /// <summary>One quest in a save: only quests that were accepted have one, so a save without any simply has everything still available.</summary>
    [Serializable]
    public class QuestEntry
    {
        public string id;
        public int state, progress;
    }

    /// <summary>
    /// One character's quests: Available -> Active -> ReadyToTurnIn -> Done, once each. Part of PlayerState, so it is saved with the character and reaches the
    /// client in every RpgState. Only the side that owns the state changes it (the zone server online, the game itself offline). No Unity types.
    /// </summary>
    public sealed class QuestLog
    {
        readonly List<QuestEntry> entries = new List<QuestEntry>();

        QuestEntry Find(string id)
        {
            for (int i = 0; i < entries.Count; i++) if (entries[i].id == id) return entries[i];
            return null;
        }

        public QuestState StateOf(QuestDef q)
        {
            QuestEntry e = Find(q.Id);
            return e != null ? (QuestState)e.state : QuestState.Available;
        }

        public int ProgressOf(QuestDef q)
        {
            QuestEntry e = Find(q.Id);
            return e != null ? e.progress : 0;
        }

        /// <summary>False unless the quest was still available.</summary>
        public bool Accept(QuestDef q)
        {
            if (q == null || Find(q.Id) != null) return false;
            entries.Add(new QuestEntry { id = q.Id, state = (int)QuestState.Active });
            return true;
        }

        /// <summary>One monster of this type died with this character's help. True when any quest moved.</summary>
        public bool CountKill(string monsterType)
        {
            bool moved = false;
            for (int i = 0; i < entries.Count; i++)
            {
                QuestEntry e = entries[i];
                if (e.state != (int)QuestState.Active) continue;
                QuestDef q = QuestCatalog.Get(e.id);
                if (q == null || q.MonsterType != monsterType) continue;
                e.progress = Math.Min(q.Count, e.progress + 1);
                if (e.progress >= q.Count) e.state = (int)QuestState.ReadyToTurnIn;
                moved = true;
            }
            return moved;
        }

        /// <summary>False unless the objective is complete. Closes the quest for good: the caller pays the reward exactly when this says true.</summary>
        public bool Close(QuestDef q)
        {
            QuestEntry e = q != null ? Find(q.Id) : null;
            if (e == null || e.state != (int)QuestState.ReadyToTurnIn) return false;
            e.state = (int)QuestState.Done;
            return true;
        }

        /// <summary>What this villager shows. Something to hand in matters more than something to take. Allocates nothing: the minimap asks ten times a second.</summary>
        public QuestMarker MarkerFor(string npcId)
        {
            QuestMarker marker = QuestMarker.None;
            foreach (QuestDef q in QuestCatalog.All)
            {
                if (q.GiverNpcId != npcId) continue;
                QuestState s = StateOf(q);
                if (s == QuestState.ReadyToTurnIn) return QuestMarker.Ready;
                if (s == QuestState.Available) marker = QuestMarker.Available;
            }
            return marker;
        }

        /// <summary>The quest a talk with this villager is about: one to hand in, else one to take, else one under way, else a finished one. Null = he gives none.</summary>
        public QuestDef TalkAbout(string npcId)
        {
            QuestDef best = null;
            int bestRank = -1;
            foreach (QuestDef q in QuestCatalog.All)
            {
                if (q.GiverNpcId != npcId) continue;
                QuestState s = StateOf(q);
                int rank = s == QuestState.ReadyToTurnIn ? 3 : s == QuestState.Available ? 2 : s == QuestState.Active ? 1 : 0;
                if (rank > bestRank) { best = q; bestRank = rank; }
            }
            return best;
        }

        /// <summary>A copy for the save, so a save never shares entries with the live log.</summary>
        public List<QuestEntry> ToList()
        {
            var list = new List<QuestEntry>(entries.Count);
            foreach (QuestEntry e in entries) list.Add(new QuestEntry { id = e.id, state = e.state, progress = e.progress });
            return list;
        }

        public void Load(List<QuestEntry> saved)
        {
            entries.Clear();
            entries.AddRange(Sanitize(saved));
        }

        /// <summary>
        /// Never trust raw JSON: unknown quests and repeats are dropped, a state that is not one of the four is dropped, an entry for a quest not taken is dropped, the count is kept in range
        /// and the state follows it, so a hand-edited save cannot make the API refuse the character or pay a reward early.
        /// </summary>
        public static List<QuestEntry> Sanitize(List<QuestEntry> raw)
        {
            var clean = new List<QuestEntry>();
            if (raw == null) return clean;
            foreach (QuestEntry e in raw)
            {
                QuestDef q = e != null ? QuestCatalog.Get(e.id) : null;
                if (q == null || e.state <= (int)QuestState.Available || e.state > (int)QuestState.Done) continue;
                bool seen = false;
                foreach (QuestEntry c in clean) if (c.id == e.id) seen = true;
                if (seen) continue;
                int progress = Math.Max(0, Math.Min(q.Count, e.progress));
                int state = e.state;
                if (state == (int)QuestState.Active && progress >= q.Count) state = (int)QuestState.ReadyToTurnIn;
                if (state == (int)QuestState.ReadyToTurnIn && progress < q.Count) state = (int)QuestState.Active;
                clean.Add(new QuestEntry { id = e.id, state = state, progress = progress });
            }
            return clean;
        }
    }
}

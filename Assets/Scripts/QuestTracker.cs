using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Static description of one kill quest (text, monster type to hunt, rewards).</summary>
    public sealed class QuestDef
    {
        public string Id, Title, Description, MonsterType, MonsterName, GiverName;
        public int Count, RewardGold, RewardStones, RewardHpPotions;
        public string[] OfferLines, ActiveLines, ReadyLines, DoneLines;

        public static readonly QuestDef CrystalSurvey = new QuestDef
        {
            Id = "crystal-survey", Title = "Ancient Crystal Survey", GiverName = "Elder Thomas",
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
        };
    }

    public enum QuestState { Available, Active, ReadyToTurnIn, Done }

    /// <summary>
    /// Local (offline / host) progress of the one quest: counts kills of the local hero (KillRewards.LocalKill), moves Available -> Active ->
    /// ReadyToTurnIn -> Done and pays the reward through the hero's PlayerRpg. State is in memory only for now (not in the save file).
    /// </summary>
    public static class QuestTracker
    {
        public static readonly QuestDef Def = QuestDef.CrystalSurvey;
        public static QuestState State { get; private set; }
        public static int Progress { get; private set; }

        public static void Init()
        {
            State = QuestState.Available; Progress = 0;
            KillRewards.LocalKill -= OnKill; // static event: survives play sessions when Domain Reload is off
            KillRewards.LocalKill += OnKill;
        }

        public static void Accept()
        {
            if (State != QuestState.Available) return;
            State = QuestState.Active; Progress = 0;
            ChatHub.System("Quest accepted: " + Def.Title);
            NotificationToast.Show("Quest Accepted", Def.Title, UiArt.Win("quest-icon"));
        }

        static void OnKill(Monster m)
        {
            if (State != QuestState.Active || m == null || m.Type != Def.MonsterType) return;
            Progress = Mathf.Min(Def.Count, Progress + 1);
            if (Progress >= Def.Count)
            {
                State = QuestState.ReadyToTurnIn;
                ChatHub.System("Objective complete: return to " + Def.GiverName);
                NotificationToast.Show("Objective Complete", "Return to " + Def.GiverName, UiArt.Win("quest-icon"));
            }
            else NotificationToast.Show(Def.Title, "Defeat " + Def.MonsterName + "  " + Progress + " / " + Def.Count, UiArt.Win("quest-icon"));
        }

        /// <summary>Pays the reward (gold + upgrade stones + HP potions) and closes the quest.</summary>
        public static void TurnIn()
        {
            PlayerRpg rpg = LocalPlayer.Rpg;
            if (State != QuestState.ReadyToTurnIn || rpg == null) return;
            State = QuestState.Done;
            rpg.Pickup(new LootEntry { Kind = LootKind.Coin, Value = Def.RewardGold });
            rpg.Pickup(new LootEntry { Kind = LootKind.UpgradeStone, Value = Def.RewardStones });
            for (int i = 0; i < Def.RewardHpPotions; i++) rpg.Pickup(new LootEntry { Kind = LootKind.HpPotion, Value = 1 });
            ChatHub.System("Quest complete: " + Def.Title + " (+" + Def.RewardGold + " G, +" + Def.RewardStones + " stones, +" + Def.RewardHpPotions + " HP potions)");
            NotificationToast.Show("Quest Complete!", Def.Title + "  +" + Def.RewardGold + " G", UiArt.Win("quest-icon"));
        }
    }
}

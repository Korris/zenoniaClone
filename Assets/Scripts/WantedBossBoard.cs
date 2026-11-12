using System;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>What the Wanted Boss board shows. The one binding point for the board's look: a window, a poster or the NPC dialogue below read these fields and nothing else.</summary>
    public struct WantedBoardData
    {
        public string BossName, MapName, Countdown, RewardText;
        public int BossLevel, Gold, Stones, HpPotions, MpPotions;
        public string PotionSize;   // "Small" | "Medium" | "Large"
        public bool Claimed;        // this hero was paid today
        public string BossType;     // sheet / art key (Resources/Sprites/monsters/<BossType>.png)
    }

    /// <summary>
    /// The Wanted Boss "board": Guard Rook (Oakhaven) and Leila (Sunspire) tell today's boss, where it is, what it pays and whether this hero was paid, in the usual NPC dialogue box.
    /// Online only: offline the two keep their normal chatter. Reads the mirrored state (PlayerState.WantedDay); never sends anything. Look is functional only: Current() is what an art pass binds to.
    /// </summary>
    public static class WantedBossBoard
    {
        public static bool IsBoardNpc(string npcId) { return npcId == "guardRook" || npcId == "bountyLeila"; }

        /// <summary>Today's board for the local hero, or null without an online hero.</summary>
        public static WantedBoardData? Current()
        {
            HeroController h = GameSession.Hero;
            if (NetMode.IsOffline || h == null || h.Rpg == null) return null;
            WantedBossDef d = WantedBoss.Today(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return new WantedBoardData
            {
                BossType = d.BossType, BossName = d.BossName, BossLevel = d.BossLevel, MapName = WantedBossTracker.MapName(d), Countdown = WantedBossTracker.TimeToReset(),
                Gold = d.Gold, Stones = d.Stones, HpPotions = d.HpPotions, MpPotions = d.MpPotions, PotionSize = PotionCatalog.Name(true, d.PotionSize).Split(' ')[0],
                RewardText = WantedBoss.RewardText(d), Claimed = WantedBossTracker.ClaimedToday(h.Rpg.WantedDay),
            };
        }

        /// <summary>Opens the board for a board NPC when online and the hero's state is known; false = the caller shows the normal lines.</summary>
        public static bool TryOpen(string npcId, string name, Sprite portrait, string[] defaultLines)
        {
            if (!IsBoardNpc(npcId)) return false;
            WantedBoardData? c = Current();
            if (c == null) return false;
            WantedBoardData d = c.Value;
            string[] pages =
            {
                "WANTED today: " + d.BossName + " (Lv " + d.BossLevel + ") in " + d.MapName + ".",
                "Bounty: " + d.RewardText + ".\n" + (d.Claimed ? "You already claimed it. Next wanted boss in " + d.Countdown + "." : "Not claimed yet. Resets in " + d.Countdown + "."),
                defaultLines.Length > 0 ? defaultLines[0] : "",
            };
            NpcDialogueBox.Open(name, portrait, pages, null);
            return true;
        }
    }
}

using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the Card Duel (docs/reference/card-gem-ui/06a, 06b), offline, through the real request path (DuelClient -> LocalDuelHost -> DuelNpcRun):
    ///   --duel-seed            --gem-seed cards (100,000 gold) and a valid 8-card deck (1 boss, 2 SR) saved through DeckRules
    ///   --duel-open deck|battle   opens the Card window on the Card Duel page, or also presses Play on NPC --duel-npc N (0-2, default 1)
    ///   --duel-bot             plays the human side automatically (Hard AI picks, one move every 0.6 s) so a whole duel can be captured
    /// Start in a safe map: --map oakhaven-village (a duel is refused on a hostile map, like online).
    /// </summary>
    public partial class DebugCapture
    {
        static void DebugDuel()
        {
            string[] args = Environment.GetCommandLineArgs();
            PlayerState p = RpgSession.Player;
            if (p != null && Array.IndexOf(args, "--duel-seed") >= 0)
            {
                SeedGems(p);
                DeckRules.Save(p, new[] { "frostfangKing", "duneScarab", "sandRaider", "crystalGolem", "iceGoblin", "frostSlime", "thornBoar", "meadowSlime" });
            }
            string pvp = Arg(args, "--duel-pvp");
            if (pvp != null) { DebugDuelPvp(pvp); return; } // PvP / wager screens without a second client (DebugCapture.DuelPvp.cs)
            string open = Arg(args, "--duel-open");
            if (open == null) return;
            CardWindow.Open(CardWindow.Page.Duel);
            Canvas.ForceUpdateCanvases();
            if (Array.IndexOf(args, "--duel-bot") >= 0) new GameObject("DuelCaptureBot").AddComponent<DuelCaptureBot>();
            if (open == "battle") { int npc; if (!int.TryParse(Arg(args, "--duel-npc"), out npc)) npc = 1; DuelClient.PlayNpc(npc); }
        }

        /// <summary>Plays the human side: when it is the player's turn and the last answer is in, the Hard AI's pick goes out through DuelClient.Place.</summary>
        sealed class DuelCaptureBot : MonoBehaviour
        {
            float nextAt;
            readonly DuelRng rng = new DuelRng(99);

            void Update()
            {
                DuelView v = DuelClient.View;
                if (v == null || v.Finished || !v.YourTurn || Time.unscaledTime < nextAt) return;
                nextAt = Time.unscaledTime + 1.6f;
                DuelBoard b = v.Board();
                int bestHand = 0, bestCell = -1, bestFlips = -1;
                for (int h = 0; h < v.Hand.Count; h++)
                    for (int c = 0; c < DuelBoard.Cells; c++)
                    {
                        if (!b.IsEmpty(c)) continue;
                        int flips = DuelNpcAi.PopCount(b.PreviewFlips(DuelView.Me, v.Hand[h], c));
                        if (flips > bestFlips || (flips == bestFlips && rng.Next(3) == 0)) { bestFlips = flips; bestHand = h; bestCell = c; }
                    }
                if (bestCell >= 0 && DuelClient.Place(bestHand, bestCell)) nextAt = Time.unscaledTime + 1.6f;
            }
        }
    }
}

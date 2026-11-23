using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the Card Duel (docs/reference/card-gem-ui/06a, 06b, phase-06b-shots). Card Duel is online only, so these feed the UI the same messages the server would send (see DebugCapture.DuelPvp.cs):
    ///   --duel-seed            --gem-seed cards (100,000 gold) and a valid 8-card deck (1 boss, 2 SR) saved through DeckRules
    ///   --duel-open deck       opens the Card window on the Card Duel page (offline: the page shows it is disabled and why)
    ///   --duel-online          pretend to be online for the deck page (the Challenge Player button and the stakes warning show), a capture only
    ///   --duel-pvp <mode>      PvP / pick screens (DebugCapture.DuelPvp.cs)
    /// Start in a safe map: --map oakhaven-village.
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
            if (Array.IndexOf(args, "--duel-online") >= 0) DuelClient.DebugPretendOnline = true;
            string pvp = Arg(args, "--duel-pvp");
            if (pvp != null) { DebugDuelPvp(pvp); return; } // PvP / pick screens without a second client (DebugCapture.DuelPvp.cs)
            string open = Arg(args, "--duel-open");
            if (open == null) return;
            CardWindow.Open(CardWindow.Page.Duel);
            Canvas.ForceUpdateCanvases();
        }
    }
}

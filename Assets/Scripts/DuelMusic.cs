using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The music of the Card Duel result: one jingle for the winner, one for the loser, played when the result banner (or the winner's pick screen) first shows. It goes through GameMusic.PlayJingle, so the map music
    /// ducks while it plays and comes back after, and the Settings music switch mutes it. Every outcome has a main track and an alternate in the table below: to swap one, change `UseAlternates` or the file names
    /// (tracks live in Resources/Audio/music/card-duel/). A draw has no track of its own: it plays the softer defeat jingle (my call: nobody lost anything, so not the "try again tomorrow" one).
    /// </summary>
    public static class DuelMusic
    {
        /// <summary>true = every outcome plays Alts[AltIndex] instead of the main track (swap tracks here, or just change Main).</summary>
        public static bool UseAlternates = false;
        public static int AltIndex = 0;

        struct Pair { public string Main; public string[] Alts; }

        const string Folder = "card-duel/";
        static readonly Pair Win = new Pair { Main = Folder + "duel-victory-cozy-win-sparkle", Alts = new[] { Folder + "duel-victory-tiny-trophy-smile", Folder + "duel-victory-spark", Folder + "duel-victory-heros-prize" } }; // user pick 23/11: cozy-win-sparkle (spark was "too heroic")
        static readonly Pair Lose = new Pair { Main = Folder + "duel-defeat-try-again-tomorrow", Alts = new[] { Folder + "duel-defeat-try-again-gently" } };
        static readonly Pair Draw = new Pair { Main = Folder + "duel-defeat-try-again-gently", Alts = new[] { Folder + "duel-defeat-try-again-tomorrow" } };

        /// <summary>The track for an outcome (null while the duel still plays).</summary>
        public static string TrackFor(DuelResult result)
        {
            Pair p;
            switch (result)
            {
                case DuelResult.Win: p = Win; break;
                case DuelResult.Lose: p = Lose; break;
                case DuelResult.Draw: p = Draw; break;
                default: return null;
            }
            return UseAlternates ? p.Alts[Mathf.Clamp(AltIndex, 0, p.Alts.Length - 1)] : p.Main;
        }

        /// <summary>Plays the result jingle once (the caller guards "once per duel").</summary>
        public static void OnResult(DuelResult result)
        {
            string track = TrackFor(result);
            if (track != null) GameMusic.PlayJingle(track);
        }
    }
}

using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetWindow, Capture tab (docs/pet-system-design.md 6): the balls the account holds and how to get more, what a ball needs (a monster you hit in the last 10 s at
    /// half HP or less, 4 tiles; or a fallen boss in its window), the odds by rarity, today's limits and the rare-ball progress from Wanted Boss claims. The throw itself is
    /// CaptureMarker's buttons over the Talk prompt, in the world.
    /// </summary>
    public sealed partial class PetWindow
    {
        Text captureText;

        void BuildCapture()
        {
            captureText = UiKit.Label(captureTab, "Text", 14f, 8f, W - 90f, BodyH - 16f, "", 12, UiWinKit.Cream, TextAnchor.UpperLeft);
        }

        void FillCapture(PetDef d, AccountState a, PlayerState p)
        {
            var s = new StringBuilder();
            int balls = a != null ? a.balls : 0, rare = a != null ? a.rareBalls : 0, bought = a != null ? a.ballsBoughtToday : 0;
            s.Append("<b>Pet Balls ").Append(balls).Append("</b>   ").Append(PetRules.BallPrice).Append(" G each at Dale or Nara, ").Append(PetRules.BallsPerDay - bought).Append(" of ").Append(PetRules.BallsPerDay)
             .Append(" left to buy today; ").Append(Pct(PetRules.BossBallChance)).Append(" from every map boss.\n");
            s.Append("<b>Rare Balls ").Append(rare).Append("</b>   one per ").Append(PetRules.WantedClaimsForRareBall).Append(" Wanted Boss claims (").Append(a != null ? a.wantedForRareBall : 0).Append(" / ")
             .Append(PetRules.WantedClaimsForRareBall).Append("); a rare ball always catches a valid target.\n\n");
            s.Append("<b>How</b>   Fight a monster to half HP or less, stand within 4 tiles, tap the ball button over the Talk prompt. Only the monster you hit in the last 10 s counts. ")
             .Append("Under a quarter HP the odds rise. A miss costs the ball; a refusal costs nothing. Caught: it vanishes with no loot or EXP and joins your collection (a species you own gives ")
             .Append(PetRules.DuplicateShards).Append(" shards).\n");
            s.Append("<b>Bosses</b>   never caught alive. When a map boss falls, everyone with a real share may throw one ball within ").Append(PetRules.BossCorpseMs / 1000).Append(" s (SSR ")
             .Append(Pct(PetRules.BossCaptureChance(PetRarity.SSR, false))).Append(", UR ").Append(Pct(PetRules.BossCaptureChance(PetRarity.UR, false))).Append("); a Wanted Boss ").Append(PetRules.WantedCorpseMs / 1000)
             .Append(" s (SSR ").Append(Pct(PetRules.BossCaptureChance(PetRarity.SSR, true))).Append(", UR ").Append(Pct(PetRules.BossCaptureChance(PetRarity.UR, true))).Append("). One boss pet a day")
             .Append(a != null && a.dayBossCaught ? " <color=#ffd27a>(done today)</color>" : "").Append(".\n\n");
            s.Append("<b>Odds with a normal ball</b> (half HP / under a quarter)\n");
            for (PetRarity r = PetRarity.N; r <= PetRarity.SSR; r++)
                s.Append("<color=").Append(PetArt.RarityHex(r)).Append(">").Append(r).Append("</color> ").Append(Pct(PetRules.LiveCaptureChance(r, 0.5))).Append(" / ").Append(Pct(PetRules.LiveCaptureChance(r, 0.25))).Append("     ");
            s.Append("<color=").Append(PetArt.RarityHex(PetRarity.UR)).Append(">UR</color> only from a fallen boss\n");
            int level = a != null ? a.CollectionLevel : 1;
            s.Append("Collection level ").Append(level).Append(": a ball may catch up to ").Append(level >= 4 ? "UR" : level == 3 ? "SSR" : level == 2 ? "SR" : "R").Append(" pets (level 2 SR, 3 SSR, 4+ UR).\n\n");
            s.Append("<b>Selected: ").Append(d.Name).Append("</b>   ");
            if (d.MonsterType == null) s.Append("not a monster: never caught.");
            else if (d.Rarity == PetRarity.UR) s.Append("UR: a ball on the fallen ").Append(d.Name).Append(" only.");
            else s.Append("a ball on a ").Append(d.Name).Append(" (").Append(d.MonsterType).Append(") at half HP: ").Append(Pct(PetRules.LiveCaptureChance(d.Rarity, 0.5))).Append(", under a quarter: ")
                  .Append(Pct(PetRules.LiveCaptureChance(d.Rarity, 0.25))).Append(". Needs collection level ").Append(PetRules.CollectionLevelFor(d.Rarity)).Append(".");
            captureText.text = s.ToString();
        }
    }
}

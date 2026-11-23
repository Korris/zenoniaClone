using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// Ranked row of the result banner (phase 07): the tier badge (ChatGPT art) and two lines, the LP the server gave or took ("+25 LP") over the new tier and LP; a match that did not count says so instead. Box model:
    /// a horizontal row, padding 4, gap 10, children centred; the text column is a vertical group. Shown only for a finished Ranked duel.
    /// </summary>
    public sealed partial class CardDuelBattleView
    {
        const float RankRowY = 232f, RankRowH = 52f, RankBadgeW = 44f, RankBadgeH = 40f, RankTextW = 200f;
        RectTransform rankRow;
        Image rankBadge;
        Text rankLine1, rankLine2;

        void BuildRankRow()
        {
            float w = MidW - 40f;
            rankRow = UiKit.Rect(bannerRoot, "RankRow", 40f, RankRowY, w - 80f, RankRowH);
            var row = rankRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(4, 4, 4, 4); row.spacing = 10f; row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false; row.childControlHeight = false; row.childForceExpandWidth = false; row.childForceExpandHeight = false;
            rankBadge = GemUiArt.Img(rankRow, "Badge", 0f, 0f, RankBadgeW, RankBadgeH, null);
            RectTransform col = UiKit.Rect(rankRow, "Text", 0f, 0f, RankTextW, RankRowH - 8f);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.MiddleLeft; v.childControlWidth = true; v.childControlHeight = false; v.childForceExpandWidth = true; v.childForceExpandHeight = false; v.spacing = 0f;
            rankLine1 = U.Txt(col, "Line1", 0f, 0f, RankTextW, 26f, "", 20, U.Ink);
            rankLine2 = U.Txt(col, "Line2", 0f, 0f, RankTextW, 18f, "", U.TextSmall, U.InkSoft);
            rankRow.gameObject.SetActive(false);
        }

        void DrawRankRow(DuelView v)
        {
            rankRow.gameObject.SetActive(v.Ranked);
            if (!v.Ranked) return;
            PlayerState p = PetActions.Me;
            CardRankData r = p != null ? p.Cards.Rank : new CardRankData();
            RankTier tier = RankedRules.Tier(r.lp);
            GemUiArt.SetPiece(rankBadge, "cards/ranked/rank-" + RankedRules.TierName(tier).ToLowerInvariant());
            string lp = r.lp.ToString(CultureInfo.InvariantCulture);
            if (v.RankNotCounted) { rankLine1.text = "No LP change"; rankLine1.color = U.InkSoft; rankLine2.text = "Same opponent too often, or it ended too early"; return; }
            int d = v.LpChange;
            rankLine1.text = (d > 0 ? "+" : d < 0 ? "-" : "+") + Mathf.Abs(d).ToString(CultureInfo.InvariantCulture) + " LP";
            rankLine1.color = d > 0 ? U.Good : d < 0 ? U.Bad : U.Ink;
            rankLine2.text = RankedRules.TierName(tier) + "  " + lp + " LP";
        }
    }
}

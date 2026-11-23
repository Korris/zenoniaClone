using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The Ranked box of the Card Duel page (phase 07), drawn over the deck + info columns like the player list: the tier badge (ChatGPT art cards/ranked/rank-*), tier name, LP and the bar to the next tier,
    /// this season's record, the season countdown, the searching state (seconds waited, Cancel) and the Find Match button. Box model: panel padding 24, blocks 12 apart, boxes 10 apart. It only draws what the
    /// server said (PlayerState rank via DuelClient.DisplayRank, DuelClient.Ranked) and sends QueueRanked / LeaveQueue; the server decides every rule.
    /// </summary>
    public sealed class CardDuelRankedPanel
    {
        public const float W = 641f, H = 544f, Pad = 24f, Gap = 12f;
        const float BadgeBox = 150f, BarW = 400f, BarH = 24f, StatH = 66f, StatGap = 10f;

        public readonly RectTransform Root;
        readonly System.Func<bool> deckReady;
        readonly System.Action onBack;
        readonly Image badge, fill;
        readonly Text tierName, lpText, toNext, season, status, subStatus, prevBest, rules;
        readonly Text[] statValue = new Text[4];
        readonly U.PieceButton find, back;
        int shownVersion = -1, shownSecs = -1, shownMinutes = -1;

        public CardDuelRankedPanel(Transform parent, float x, float y, System.Func<bool> deckIsReady, System.Action onBackTap)
        {
            deckReady = deckIsReady; onBack = onBackTap;
            Root = UiKit.Rect(parent, "RankedPanel", x, y, W, H);
            U.Panel(Root, "Box", 0f, 0f, W, H, true).raycastTarget = true; // taps stop here
            U.Txt(Root, "Title", Pad, 12f, 300f, 36f, "Ranked", 26, U.Ink);
            season = U.Txt(Root, "Season", W - Pad - 300f, 12f, 300f, 36f, "", U.TextNormal + 2, U.InkSoft, TextAnchor.MiddleRight);

            // tier block: badge on the left, name / LP / bar on the right
            float blockY = 58f;
            badge = GemUiArt.Img(Root, "Badge", Pad, blockY, BadgeBox, BadgeBox - 20f, null);
            float tx = Pad + BadgeBox + Gap;
            tierName = U.Txt(Root, "Tier", tx, blockY + 4f, W - tx - Pad, 40f, "", 32, U.Ink);
            lpText = U.Txt(Root, "Lp", tx, blockY + 46f, W - tx - Pad, 30f, "", 22, U.Ink);
            GemUiArt.Img(Root, "Track", tx, blockY + 84f, BarW, BarH, GemUiArt.Frame("progress-track"));
            fill = GemUiArt.Img(Root, "Fill", tx, blockY + 84f, 48f, BarH, GemUiArt.Frame("progress-fill"));
            toNext = U.Txt(Root, "ToNext", tx, blockY + 112f, W - tx - Pad, 24f, "", U.TextSmall, U.InkSoft);

            // record: 4 equal boxes, label over value
            string[] labels = { "Wins", "Losses", "Draws", "Best LP" };
            float statY = blockY + BadgeBox - 6f, statW = (W - 2f * Pad - 3f * StatGap) / 4f;
            for (int i = 0; i < 4; i++)
            {
                float sx = Pad + i * (statW + StatGap);
                RectTransform box = UiKit.Rect(Root, "Stat" + i, sx, statY, statW, StatH);
                U.Panel(box, "Bg", 0f, 0f, statW, StatH);
                U.Txt(box, "Label", 0f, 6f, statW, 22f, labels[i], U.TextSmall, U.InkSoft, TextAnchor.MiddleCenter);
                statValue[i] = U.Txt(box, "Value", 0f, 28f, statW, 32f, "0", 24, U.Ink, TextAnchor.MiddleCenter);
            }
            prevBest = U.Txt(Root, "PrevBest", Pad, statY + StatH + 6f, W - 2f * Pad, 22f, "", U.TextSmall, U.InkSoft, TextAnchor.MiddleLeft);

            // status: what the button will do / the searching state
            float stY = statY + StatH + 34f;
            status = U.Txt(Root, "Status", Pad, stY, W - 2f * Pad, 36f, "", 22, U.Ink, TextAnchor.MiddleCenter);
            subStatus = U.Txt(Root, "SubStatus", Pad, stY + 36f, W - 2f * Pad, 26f, "", U.TextNormal, U.InkSoft, TextAnchor.MiddleCenter);
            rules = U.Txt(Root, "Rules", Pad, H - 76f - 12f - 52f, W - 2f * Pad, 52f,
                "Ranked takes no cards and pays no Gold. Win +20 to 30 LP, lose -15 to 25 LP, a draw changes nothing. Leaving a match counts as a loss.", U.TextSmall, U.InkSoft, TextAnchor.UpperCenter);

            float bw = 200f, bgap = 20f, bx = (W - 2f * bw - bgap) * 0.5f;
            back = new U.PieceButton(Root, "Back", bx, H - Pad - U.ButtonH, bw, U.ButtonH, GemUiArt.Frame("btn-blue"), "Back", () => onBack());
            find = new U.PieceButton(Root, "Find", bx + bw + bgap, H - Pad - U.ButtonH, bw, U.ButtonH, GemUiArt.Frame("btn-teal"), "Find Match", OnFind);
            Root.gameObject.SetActive(false);
        }

        public bool Visible { get { return Root.gameObject.activeSelf; } }

        public void Show(bool on) { Root.gameObject.SetActive(on); if (on) { shownVersion = -1; Refresh(); } }

        void OnFind()
        {
            if (DuelClient.RankedQueued) DuelClient.LeaveQueue();
            else if (!DuelClient.Running && !DuelClient.InLobby) DuelClient.QueueRanked();
        }

        /// <summary>Per frame while visible: redraw on a new server message or a changed saved rank, tick the waiting seconds and the season countdown.</summary>
        public void Tick()
        {
            if (!Visible) return;
            int secs = Mathf.FloorToInt(DuelClient.RankedWaitSeconds), mins = DuelClient.RankedMinutesLeft;
            if (shownVersion != DuelClient.RankedVersion || secs != shownSecs || mins != shownMinutes) Refresh();
        }

        static string Clock(int totalSecs) { return (totalSecs / 60).ToString(CultureInfo.InvariantCulture) + ":" + (totalSecs % 60).ToString("00", CultureInfo.InvariantCulture); }

        static string Left(int minutes)
        {
            if (minutes >= 1440) return (minutes / 1440) + " d " + (minutes % 1440) / 60 + " h";
            if (minutes >= 60) return (minutes / 60) + " h " + (minutes % 60) + " min";
            return minutes + " min";
        }

        public void Refresh()
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            CardRankData r = DuelClient.DisplayRank(p);
            RankTier tier = RankedRules.Tier(r.lp);
            GemUiArt.SetPiece(badge, "cards/ranked/rank-" + RankedRules.TierName(tier).ToLowerInvariant());
            tierName.text = RankedRules.TierName(tier);
            lpText.text = r.lp.ToString(CultureInfo.InvariantCulture) + " LP";
            int next = RankedRules.NextTierLp(tier);
            toNext.text = next < 0 ? "Top tier reached" : (next - r.lp).ToString(CultureInfo.InvariantCulture) + " LP to " + RankedRules.TierName((RankTier)((int)tier + 1));
            float prog = RankedRules.TierProgress(r.lp);
            fill.rectTransform.sizeDelta = new Vector2(Mathf.Max(48f, BarW * prog), BarH);
            fill.gameObject.SetActive(prog > 0f || r.lp > 0);
            statValue[0].text = r.wins.ToString(CultureInfo.InvariantCulture); statValue[1].text = r.losses.ToString(CultureInfo.InvariantCulture);
            statValue[2].text = r.draws.ToString(CultureInfo.InvariantCulture); statValue[3].text = r.best.ToString(CultureInfo.InvariantCulture);
            prevBest.text = r.prevBest > 0 ? "Last season's best: " + r.prevBest + " LP (" + RankedRules.TierName(RankedRules.Tier(r.prevBest)) + ")" : "";
            shownMinutes = DuelClient.RankedMinutesLeft;
            season.text = DuelClient.RankedSeason > 0 ? "Season " + DuelClient.RankedSeason + "  -  ends in " + Left(shownMinutes) : "";

            bool queued = DuelClient.RankedQueued;
            shownSecs = Mathf.FloorToInt(DuelClient.RankedWaitSeconds);
            string problem = DuelClient.RankedProblem(p);
            bool ready = deckReady();
            if (queued)
            {
                status.text = "Searching for an opponent...  " + Clock(shownSecs);
                subStatus.text = shownSecs >= 30 ? "Widening the search to find a match" : "Looking for a player of a similar level";
                find.Set("Cancel", true);
            }
            else
            {
                status.text = problem.Length > 0 ? problem : !ready ? "You need a full, valid 8-card deck" : "Ready for a ranked match";
                subStatus.text = "";
                find.Set("Find Match", problem.Length == 0 && ready && !DuelClient.Running && !DuelClient.InLobby);
            }
            status.color = !queued && (problem.Length > 0 || !ready) ? U.Bad : U.Ink;
            shownVersion = DuelClient.RankedVersion;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// Battle screen of the Card Duel (mockup 06b), construction. Everything is a piece of the ChatGPT card kit (cards/board/*, cards/card/*): the board art with its 16 cells, score plates, turn plate, timer ring,
    /// log frame, hand tray, NPC portrait, result banner. Only text and the position of pieces are made here; geometry constants are fractions of the art (the board's cells measured on board-4x4.png).
    /// Layout (window units, region 1096 x 546; the winner's pick screen is CardDuelPickPanel): left column 300 (score, turn, timer, log), board + hand 570 in the middle, opponent + selected card 206 on the right.
    /// </summary>
    public sealed partial class CardDuelBattleView
    {
        public const float RegionW = 1096f, RegionH = 546f;
        const float LeftW = 300f, MidX = 310f, MidW = 570f, RightX = 890f, RightW = 206f;
        // board-4x4.png is 560 x 516: cell column / row origins and sizes in art pixels
        const float ArtW = 560f, ArtH = 516f, CellX0 = 66f, CellPitchX = 117f, CellW = 104f, CellY0 = 41f, CellPitchY = 112f, CellH = 108f;
        const float BoardH = 420f, HandY = 424f, HandH = 122f, HandCardW = 82f, HandCardH = 100f;
        public const int EdgePx = 20; // duel card numbers: >= 16 px at 1280x720, bold white with a dark rim

        readonly Action requestClose;
        RectTransform root, boardRoot, bannerRoot;
        Text mineScore, theirScore, mineLabel, theirLabel, turnText, stateText, timerText, logHeader, handHeader, oppName, oppLeft, myLeft, hint, bannerTitle, bannerSub;
        Image statePill, timerFill, oppPortrait, bannerArt;
        Image[] ownerOverlay = new Image[16], validMark = new Image[16], lastMark = new Image[16], cardShadow = new Image[16], cardFlash = new Image[16];
        RectTransform[] slotRt = new RectTransform[16];
        Vector3[] cellHome = new Vector3[16];
        RectTransform fxLayer;
        GemCardView[] cell = new GemCardView[16], hand = new GemCardView[5];
        Text[] logTurn = new Text[LogRows], logText = new Text[LogRows];
        Image[] logTag = new Image[LogRows];
        Text wagerLine;
        RectTransform bannerSubText;
        GemCardView bannerCard;
        CardDuelPickPanel pick;
        const float BannerCardW = 62f, BannerCardH = 82f;
        Text[] statNum = new Text[4];
        GemCardView big;
        U.PieceButton bannerButton, forfeit;

        const int LogRows = 5;
        public CardDuelBattleView(Action onRequestClose) { requestClose = onRequestClose; }

        public RectTransform Root { get { return root; } }

        public void Build(RectTransform window, float x, float y)
        {
            root = UiKit.Rect(window, "DuelBattle", x, y, RegionW, RegionH);
            BuildLeft();
            BuildBoard();
            BuildHand();
            BuildRight();
            BuildBanner();
            pick = new CardDuelPickPanel(root, MidX, 0f); // the winner's pick screen: over the board while the winner chooses a card of the loser's deck
            root.gameObject.SetActive(false);
        }

        static string B(string name) { return "cards/board/" + name; }

        void BuildLeft()
        {
            const float PlateH = 54f, PlateW = 132f;
            GemUiArt.Img(root, "MinePlate", 0f, 0f, PlateW, PlateH, B("score-plate-mine"));
            mineLabel = U.Txt(root, "MineLabel", 64f, 3f, PlateW - 70f, 20f, "You", 14, U.Ink, TextAnchor.MiddleLeft);
            mineScore = U.Txt(root, "MineScore", 64f, 18f, PlateW - 70f, 34f, "0", 28, U.Ink, TextAnchor.MiddleLeft);
            GemUiArt.Img(root, "Vs", PlateW + 2f, 11f, 32f, 32f, B("icon-vs"));
            float ex = LeftW - PlateW;
            GemUiArt.Img(root, "TheirPlate", ex, 0f, PlateW, PlateH, B("score-plate-enemy"));
            theirLabel = U.Txt(root, "TheirLabel", ex + 64f, 3f, PlateW - 70f, 20f, "", 14, U.Ink, TextAnchor.MiddleLeft);
            theirScore = U.Txt(root, "TheirScore", ex + 64f, 18f, PlateW - 70f, 34f, "0", 28, U.Ink, TextAnchor.MiddleLeft);

            GemUiArt.Img(root, "TurnPlate", 57f, 55f, 185f, 52f, B("turn-plate")); // 285 x 80 art at its own ratio, centred in the column; the text sits inside the pill, clear of the laurels
            turnText = U.Txt(root, "Turn", 97f, 55f, 105f, 52f, "Turn 1 / 16", 16, U.Ink, TextAnchor.MiddleCenter);

            statePill = GemUiArt.Img(root, "StatePill", 0f, 110f, 236f, 44f, GemUiArt.Frame("btn-teal"));
            stateText = U.Txt(root, "State", 10f, 110f, 216f, 40f, "", 20, U.OnButton, TextAnchor.MiddleCenter);
            var rim = stateText.gameObject.AddComponent<Outline>(); rim.effectColor = new Color(0.45f, 0.28f, 0.1f, 0.85f); rim.effectDistance = new Vector2(1.2f, -1.2f);
            GemUiArt.Img(root, "TimerTrack", 242f, 106f, 52f, 52f, B("timer-ring-track"));
            timerFill = GemUiArt.Img(root, "TimerFill", 242f, 106f, 52f, 52f, B("timer-ring-fill"));
            timerFill.type = Image.Type.Filled; timerFill.fillMethod = Image.FillMethod.Radial360; timerFill.fillOrigin = (int)Image.Origin360.Top; timerFill.fillClockwise = false;
            timerText = U.Txt(root, "Seconds", 242f, 106f, 52f, 52f, "", 17, U.Ink, TextAnchor.MiddleCenter);

            const float LogY = 164f, LogH = 380f;
            GemUiArt.Img(root, "LogFrame", 0f, LogY, LeftW, LogH, B("log-frame"));
            logHeader = U.Txt(root, "LogHeader", 70f, LogY + 18f, LeftW - 110f, 30f, "Battle Log", U.TextTitle, U.Ink, TextAnchor.MiddleLeft);
            BuildLogRows(LogY);
        }

        // Log row = a flex row (like CSS: display flex, align-items center, padding 4 8, gap 8): the Turn tag box and the text box are children of one HorizontalLayoutGroup, so both are centred
        // on the row's middle whatever the text's height; the tag has a fixed box, the text takes the rest of the width.
        const float LogRowH = 60f, LogRowGap = 0f, LogPadX = 6f, LogPadY = 4f, LogGap = 8f, LogTagW = 58f, LogTagH = 26f;

        void BuildLogRows(float logY)
        {
            float rowW = LeftW - 36f;
            for (int i = 0; i < LogRows; i++)
            {
                RectTransform row = UiKit.Rect(root, "LogRow" + i, 18f, logY + 62f + i * (LogRowH + LogRowGap), rowW, LogRowH);
                Image bg = GemUiArt.Img(row, "Bg", 0f, 0f, rowW, LogRowH, B("log-line"));
                bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var flex = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                flex.padding = new RectOffset((int)LogPadX, (int)LogPadX, (int)LogPadY, (int)LogPadY);
                flex.spacing = LogGap; flex.childAlignment = TextAnchor.MiddleLeft;
                flex.childControlWidth = true; flex.childControlHeight = true; flex.childForceExpandWidth = false; flex.childForceExpandHeight = false;
                RectTransform tagBox = UiKit.Rect(row, "Tag", 0f, 0f, LogTagW, LogTagH);
                logTag[i] = GemUiArt.Img(tagBox, "Pill", 0f, 0f, LogTagW, LogTagH, GemUiArt.Frame("filter-pill"));
                var tagSize = tagBox.gameObject.AddComponent<LayoutElement>(); tagSize.minWidth = tagSize.preferredWidth = LogTagW; tagSize.minHeight = tagSize.preferredHeight = LogTagH;
                logTurn[i] = U.Txt(tagBox, "Turn", 0f, 0f, LogTagW, LogTagH, "", U.TextSmall, U.Ink, TextAnchor.MiddleCenter);
                logTurn[i].alignByGeometry = true; logTurn[i].resizeTextForBestFit = false;
                logText[i] = U.Txt(row, "Text", 0f, 0f, 100f, LogRowH - 2f * LogPadY, "", U.TextSmall, U.Ink, TextAnchor.MiddleLeft);
                logText[i].resizeTextForBestFit = false; logText[i].verticalOverflow = VerticalWrapMode.Overflow; logText[i].alignByGeometry = true;
                // the text box is as tall as the row's inner box and its lines are centred in it (a flex item stretched to the row, text centred), so one, two or three lines sit on the same middle as the tag
                var grow = logText[i].gameObject.AddComponent<LayoutElement>(); grow.flexibleWidth = 1f; grow.minHeight = grow.preferredHeight = LogRowH - 2f * LogPadY;
            }
        }

        void BuildBoard()
        {
            float s = CardDuelBoardLayout.Scale(BoardH), bw = CardDuelBoardLayout.ArtW * s, bx = MidX + (MidW - bw) * 0.5f;
            boardRoot = UiKit.Rect(root, "Board", bx, 0f, bw, BoardH);
            GemUiArt.Img(boardRoot, "Art", 0f, 0f, bw, BoardH, B("board-4x4"));
            for (int c = 0; c < 16; c++)
            {
                int col = c % 4, row = c / 4, cellIndex = c;
                Rect cr = CardDuelBoardLayout.Scaled(CardDuelBoardLayout.Cell(col, row), s);
                Rect card = CardDuelBoardLayout.Scaled(CardDuelBoardLayout.CardIn(CardDuelBoardLayout.Cell(col, row)), s);
                RectTransform slot = UiKit.Rect(boardRoot, "Cell" + c, cr.x, cr.y, cr.width, cr.height);
                Image hit = slot.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                UiWinKit.MakeButton(hit, () => OnCell(cellIndex));
                validMark[c] = GemUiArt.Img(slot, "Valid", 0f, 0f, cr.width, cr.height, null);
                slotRt[c] = slot;
                cardShadow[c] = UiKit.Rect(slot, "Shadow", card.x - cr.x, card.y - cr.y, card.width, card.height).gameObject.AddComponent<Image>(); // under the card: the soft shadow of a card that is lifted
                cardShadow[c].color = Color.clear; cardShadow[c].raycastTarget = false;
                cell[c] = new GemCardView(slot, "Card", card.x - cr.x, card.y - cr.y, card.width, card.height, null, false, false, EdgePx);
                ownerOverlay[c] = GemUiArt.Img(cell[c].Root, "Owner", -3f, -3f, card.width + 6f, card.height + 6f, null);
                lastMark[c] = GemUiArt.Img(cell[c].Root, "Last", -5f, -5f, card.width + 10f, card.height + 10f, null);
                cardFlash[c] = UiKit.Rect(cell[c].Root, "Flash", 0f, 0f, card.width, card.height).gameObject.AddComponent<Image>(); // the light on the edge-on card at 90 degrees
                cardFlash[c].color = Color.clear; cardFlash[c].raycastTarget = false;
                CenterPivot(cell[c].Root); cellHome[c] = cell[c].Root.localPosition;
                cell[c].NumbersOnTop();
                cell[c].Root.gameObject.SetActive(false);
            }
            BuildFxLayer(bw);
            // labels: one box per tab (the size of the tab drawn in the art), text centred in it both ways
            for (int i = 0; i < 4; i++)
            {
                Rect ct = CardDuelBoardLayout.Scaled(CardDuelBoardLayout.ColumnTab(i), s), rt = CardDuelBoardLayout.Scaled(CardDuelBoardLayout.RowTab(i), s);
                Label(boardRoot, "ColLabel" + i, ct, (i + 1).ToString());
                Label(boardRoot, "RowLabel" + i, rt, ((char)('A' + i)).ToString());
            }
        }

        /// <summary>A label box: its rect is the tab's rect, the text is centred horizontally and vertically (alignByGeometry centres the glyphs themselves, not the font's line box).</summary>
        static void Label(Transform parent, string name, Rect box, string text)
        {
            Text t = U.Txt(parent, name, box.x, box.y, box.width, box.height, text, 15, U.Ink, TextAnchor.MiddleCenter);
            t.alignByGeometry = true;
            t.resizeTextForBestFit = false;
        }

        void BuildHand()
        {
            GemUiArt.Img(root, "HandTray", MidX, HandY, MidW, HandH, B("hand-tray"));
            handHeader = U.Txt(root, "HandHeader", MidX + 44f, HandY + 2f, 200f, 22f, "My Hand", U.TextNormal, U.Ink, TextAnchor.MiddleLeft);
            float gap = 12f, total = 5 * HandCardW + 4 * gap, x0 = MidX + (MidW - total) * 0.5f;
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                hand[i] = new GemCardView(root, "Hand" + i, x0 + i * (HandCardW + gap), HandY + 20f, HandCardW, HandCardH, () => OnHand(index), false, false, EdgePx);
            }
        }

        void BuildRight()
        {
            oppPortrait = GemUiArt.Img(root, "OppFace", RightX + 11f, 7f, 60f, 62f, null);
            GemUiArt.Img(root, "OppRing", RightX + 4f, 0f, 74f, 76f, B("avatar-ring-enemy"));
            oppName = U.Txt(root, "OppName", RightX + 82f, 4f, RightW - 82f, 40f, "", U.TextNormal + 1, U.Ink, TextAnchor.MiddleLeft);
            oppLeft = U.Txt(root, "OppLeft", RightX + 82f, 44f, RightW - 82f, 28f, "", U.TextSmall + 1, U.InkSoft, TextAnchor.MiddleLeft);
            U.Panel(root, "SelBox", RightX, 84f, RightW, 330f, true);
            U.Txt(root, "SelHeader", RightX + 12f, 90f, RightW - 24f, 28f, "Selected Card", U.TextNormal, U.Ink, TextAnchor.MiddleLeft);
            big = new GemCardView(root, "Big", RightX + 10f, 124f, 96f, 128f, null, false, false);
            string[] arrows = { "arrow-up", "arrow-right", "arrow-down", "arrow-left" };
            string[] names = { "Up", "Right", "Down", "Left" };
            for (int i = 0; i < 4; i++)
            {
                float y = 126f + i * 32f;
                GemUiArt.Img(root, "StatIcon" + i, RightX + 116f, y + 3f, 22f, 22f, GemUiArt.Icon(arrows[i]));
                U.Txt(root, "StatName" + i, RightX + 140f, y, 36f, 28f, names[i], U.TextSmall, U.InkSoft, TextAnchor.MiddleLeft);
                statNum[i] = U.Txt(root, "StatNum" + i, RightX + 170f, y, 28f, 28f, "", 18, U.Ink, TextAnchor.MiddleCenter);
            }
            hint = U.Txt(root, "Hint", RightX + 12f, 262f, RightW - 24f, 50f, "", U.TextSmall, U.InkSoft, TextAnchor.UpperLeft);
            wagerLine = U.Txt(root, "Wager", RightX + 12f, 306f, RightW - 24f, 34f, "", U.TextSmall, U.Ink, TextAnchor.UpperLeft);
            myLeft = U.Txt(root, "MyLeft", RightX + 12f, 340f, RightW - 24f, 26f, "", U.TextSmall + 1, U.Ink, TextAnchor.MiddleLeft);
            forfeit = new U.PieceButton(root, "Forfeit", RightX + 12f, 366f, RightW - 24f, 40f, GemUiArt.Frame("btn-red"), "Forfeit", () => { if (requestClose != null) requestClose(); });
        }

        void BuildBanner()
        {
            bannerRoot = UiKit.Rect(root, "Banner", MidX + 20f, 60f, MidW - 40f, 350f);
            Image veil = bannerRoot.gameObject.AddComponent<Image>(); veil.color = new Color(0.16f, 0.22f, 0.3f, 0.0f); veil.raycastTarget = true; // taps stop here (not art)
            bannerArt = GemUiArt.Img(bannerRoot, "Art", (MidW - 40f - 400f) * 0.5f, 0f, 400f, 180f, B("banner-win"));
            bannerTitle = U.Txt(bannerRoot, "Title", (MidW - 40f - 220f) * 0.5f, 62f, 220f, 54f, "Victory!", 30, Color.white, TextAnchor.MiddleCenter);
            var rim = bannerTitle.gameObject.AddComponent<Outline>(); rim.effectColor = new Color(0.4f, 0.2f, 0.05f, 0.9f); rim.effectDistance = new Vector2(1.5f, -1.5f);
            U.Panel(bannerRoot, "SubBox", 24f, 172f, MidW - 88f, 176f, true); // plain panel under the result text and the button: the board behind is busy
            bannerCard = new GemCardView(bannerRoot, "TakenCard", 34f, 178f, BannerCardW, BannerCardH, null, false, true, 14);
            bannerSub = U.Txt(bannerRoot, "Sub", 34f, 176f, MidW - 108f, 86f, "", 16, U.Ink, TextAnchor.MiddleCenter);
            bannerSubText = bannerSub.rectTransform;
            bannerButton = new U.PieceButton(bannerRoot, "Continue", (MidW - 40f - 190f) * 0.5f, 292f, 190f, U.ButtonH, GemUiArt.Frame("btn-teal"), "Continue", () => DuelClient.Reset());
            BuildRankRow();
            bannerRoot.gameObject.SetActive(false);
        }
    }
}

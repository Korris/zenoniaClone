using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The duel with Card Master Orin on the battle screen (tutorial and practice), everything the shared board adds for it. No turn clock and no stakes (DuelView.Npc): the timer ring is hidden, the state pill
    /// shares its row with a Hint button. While the scripted tutorial runs (View.ScriptStep > 0) a lesson panel takes the Battle Log's place (title and text from DuelTutorialScript, one source), a gold ring marks
    /// the card to pick and the cell to tap, and any other card / cell is refused here (the server refuses too). Free play: Hint asks DuelNpcAi (the same brain as Orin, careful mode) for one move and rings it.
    /// The result adds one line (return the deck to Orin / practice only) and a "Talk to Orin" button after the tutorial. Only pieces of the card kit; the board and hand are the shared ones.
    /// </summary>
    public sealed partial class CardDuelBattleView
    {
        const float LessonY = 164f, LessonW = 300f, LessonH = 380f, LessonPad = 16f, StatePillW = 236f, StatePillNpcW = 170f;
        RectTransform lessonRoot;
        Text lessonTag, lessonTitle, lessonBody, lessonRecap;
        Image cellRing, handRing;
        U.PieceButton hintButton, talkOrin;
        int markHand = -1, markCell = -1, hintVersion = -1, hintHand, hintCell, hintFlips;
        bool lessonLock; // the player must follow the lesson: only the marked card on the marked cell

        void BuildTutorial()
        {
            float inner = LessonW - 2f * LessonPad;
            lessonRoot = UiKit.Rect(root, "Lesson", 0f, LessonY, LessonW, LessonH);
            U.Panel(lessonRoot, "Box", 0f, 0f, LessonW, LessonH, true).raycastTarget = true; // taps stop here, the log under it is hidden
            GemUiArt.Img(lessonRoot, "Tag", LessonPad, LessonPad, 150f, 34f, GemUiArt.Frame("btn-gold"));
            lessonTag = U.Txt(lessonRoot, "TagText", LessonPad, LessonPad, 150f, 34f, "", U.TextNormal, U.OnButton, TextAnchor.MiddleCenter);
            var rim = lessonTag.gameObject.AddComponent<Outline>(); rim.effectColor = new Color(0.45f, 0.28f, 0.1f, 0.85f); rim.effectDistance = new Vector2(1.2f, -1.2f);
            lessonTitle = U.Txt(lessonRoot, "Title", LessonPad, 58f, inner, 34f, "", U.TextTitle, U.Ink);
            lessonBody = U.Txt(lessonRoot, "Body", LessonPad, 98f, inner, 130f, "", U.TextNormal, U.Ink, TextAnchor.UpperLeft);
            lessonRecap = U.Txt(lessonRoot, "Recap", LessonPad, 232f, inner, 84f, "", U.TextSmall, U.InkSoft, TextAnchor.UpperLeft);
            new U.PieceButton(lessonRoot, "Skip", LessonPad, 322f, inner, 44f, GemUiArt.Frame("btn-blue"), "Skip tutorial", DuelClient.SkipScript);
            lessonRoot.gameObject.SetActive(false);

            hintButton = new U.PieceButton(root, "Hint", StatePillNpcW + 8f, 110f, LeftW - StatePillNpcW - 8f, 44f, GemUiArt.Frame("btn-gold"), "Hint", OnHint);
            hintButton.Root.gameObject.SetActive(false);
            cellRing = GemUiArt.Img(boardRoot, "LessonCell", 0f, 0f, 10f, 10f, "cards/card/select-frame-gold"); // the rings are the kit's gold selection frame, over the cards
            handRing = GemUiArt.Img(root, "LessonHand", 0f, 0f, 10f, 10f, "cards/card/select-frame-gold");
            cellRing.preserveAspect = handRing.preserveAspect = false;
            cellRing.gameObject.SetActive(false); handRing.gameObject.SetActive(false);
            talkOrin = new U.PieceButton(bannerRoot, "TalkOrin", 275f, 292f, 190f, U.ButtonH, GemUiArt.Frame("btn-gold"), "Talk to Orin", TalkToOrin);
            talkOrin.Root.gameObject.SetActive(false);
        }

        /// <summary>The scripted step the screen is on (null: not a tutorial, skipped, over, or the match is finished). Moves shown = the script position.</summary>
        DuelScriptStep LessonStep(DuelView v)
        {
            if (!v.Npc || v.Finished || v.ScriptStep <= 0 || presented < 0 || presented >= DuelTutorialScript.Steps.Length) return null;
            return DuelTutorialScript.Steps[presented];
        }

        /// <summary>Which card / cell to ring: the lesson's, else the Hint just asked for (valid for this view only).</summary>
        void UpdateLesson(DuelView v)
        {
            markHand = markCell = -1; lessonLock = false;
            if (!v.Npc || v.Finished || !v.YourTurn || Animating(v)) return;
            DuelScriptStep s = LessonStep(v);
            if (s != null && s.Side == DuelView.Me)
            {
                markHand = v.Hand.IndexOf(CardTable.Index(s.CardId)); markCell = s.Cell; lessonLock = markHand >= 0;
                if (lessonLock) selected = markHand; // the lesson's card is the selected one (no other card can be picked)
            }
            else if (s == null && hintVersion == DuelClient.Version) { markHand = hintHand; markCell = hintCell; }
        }

        bool LessonBlocks(int hand, int cellIndex)
        {
            if (!lessonLock || (hand < 0 ? cellIndex == markCell : hand == markHand)) return false;
            DuelClient.Notify(DuelClient.FailText(DuelFail.ScriptMove, 0), false);
            return true;
        }

        void OnHint()
        {
            DuelView v = DuelClient.View;
            int h, c;
            if (v == null || !v.Npc || !v.YourTurn || v.Finished || Animating(v) || !DuelNpcAi.Best(v.Board(), v.Hand, DuelView.Me, null, true, out h, out c)) return;
            hintVersion = DuelClient.Version; hintHand = h; hintCell = c; selected = h;
            hintFlips = DuelBoard.PopCount(v.Board().PreviewFlips(DuelView.Me, v.Hand[h], c));
            Redraw(v);
        }

        /// <summary>Capture only (--duel-npc hint): taps Hint.</summary>
        public void PressHint() { OnHint(); }

        /// <summary>The Hint's words for the right column (null when no Hint is showing).</summary>
        string HintLine(DuelView v)
        {
            if (!v.Npc || hintVersion != DuelClient.Version || !v.YourTurn || hintHand >= v.Hand.Count) return null;
            return "Hint: " + CardName(v.Hand[hintHand]) + " at " + DuelBoard.Coord(hintCell) + (hintFlips > 0 ? ", flips " + hintFlips : ", no flips yet");
        }

        void DrawLesson(DuelView v, DuelScriptStep s)
        {
            bool mine = s.Side == DuelView.Me;
            DuelScriptStep before = presented > 0 ? DuelTutorialScript.Steps[presented - 1] : null;
            lessonTag.text = mine ? "Lesson " + DuelTutorialScript.LessonAt(presented) + " / " + DuelTutorialScript.Lessons : "Orin's move";
            lessonTitle.text = s.Title;
            lessonBody.text = s.Body;
            lessonRecap.text = mine && before != null && before.Side == DuelView.Them ? "Orin's last move: " + before.Body : "";
        }

        /// <summary>After a redraw: the Orin-only parts of the screen (no clock, Orin's portrait, lesson panel, Hint, rings).</summary>
        void DrawNpcPanels(DuelView v)
        {
            bool npc = v.Npc;
            timerTrack.gameObject.SetActive(!npc); timerFill.gameObject.SetActive(!npc); timerText.gameObject.SetActive(!npc);
            float pillW = npc ? StatePillNpcW : StatePillW; // the freed timer slot makes room for the Hint button
            statePill.rectTransform.sizeDelta = new Vector2(pillW, 44f);
            stateText.rectTransform.sizeDelta = new Vector2(pillW - 20f, 40f);
            if (npc && CardMasterTalk.Face() != null) { oppPortrait.sprite = CardMasterTalk.Face(); oppPortrait.type = Image.Type.Simple; oppPortrait.color = Color.white; oppPortrait.preserveAspect = true; }
            DuelScriptStep step = LessonStep(v);
            lessonRoot.gameObject.SetActive(step != null);
            if (step != null) DrawLesson(v, step);
            bool free = npc && !v.Finished && step == null;
            hintButton.Root.gameObject.SetActive(free);
            if (free) hintButton.Set("Hint", v.YourTurn && !Animating(v));
            Ring(cellRing, markCell >= 0 ? slotRt[markCell].anchoredPosition : Vector2.zero, markCell >= 0 ? slotRt[markCell].sizeDelta : Vector2.zero, markCell >= 0, 4f);
            float hx = markHand >= 0 ? HandX(markHand) : 0f;
            Ring(handRing, new Vector2(hx, -(HandY + 20f)), new Vector2(HandCardW, HandCardH), markHand >= 0, 4f);
        }

        static void Ring(Image ring, Vector2 topLeft, Vector2 size, bool on, float grow)
        {
            ring.gameObject.SetActive(on);
            if (!on) return;
            ring.rectTransform.anchoredPosition = topLeft + new Vector2(-grow, grow);
            ring.rectTransform.sizeDelta = size + new Vector2(2f * grow, 2f * grow);
        }

        /// <summary>Per frame in a duel with Orin: the rings breathe (alpha only, no allocation).</summary>
        void TickTutorial(DuelView v)
        {
            if (markCell < 0 && markHand < 0) return;
            float a = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 5f);
            cellRing.color = new Color(1f, 1f, 1f, a); handRing.color = new Color(1f, 1f, 1f, a);
        }

        // ---- the result ----

        /// <summary>What the quest says now (the quest state reaches the client before the final view): hand the deck back, cut short (play again), or practice.</summary>
        static CardDuelQuest.EndNote NpcEnd()
        {
            QuestLog log = QuestTracker.Log;
            return log == null ? CardDuelQuest.EndNote.Practice : CardDuelQuest.NoteFor(log.StateOf(CardDuelQuest.First));
        }

        /// <summary>A tutorial that ended without the quest moving (death, hostile map, idle): not "You left the duel".</summary>
        static bool NpcCutShort(DuelView v) { return v.Npc && v.EndReason == 1 && NpcEnd() == CardDuelQuest.EndNote.CutShort; }

        /// <summary>The quest line of the result of a duel with Orin. Empty for any other duel.</summary>
        static string NpcBannerLine(DuelView v)
        {
            if (!v.Npc) return "";
            switch (NpcEnd())
            {
                case CardDuelQuest.EndNote.HandIn: return "\nReturn the borrowed deck to Orin for your reward.";
                case CardDuelQuest.EndNote.CutShort: return "\nTalk to Orin to play it again.";
                default: return "\nPractice only, nothing won or lost.";
            }
        }

        /// <summary>Tutorial result: Continue (back to the deck page) and Talk to Orin side by side; every other result keeps the single centred Continue.</summary>
        void LayoutBannerButtons(DuelView v)
        {
            bool two = v.Npc && NpcEnd() == CardDuelQuest.EndNote.HandIn;
            talkOrin.Root.gameObject.SetActive(two);
            bannerButton.Root.anchoredPosition = new Vector2(two ? 65f : (MidW - 40f - 190f) * 0.5f, bannerButton.Root.anchoredPosition.y);
        }

        /// <summary>Closes the Card window and starts Orin's conversation (the quest is ready to hand in).</summary>
        public static void TalkToOrin()
        {
            CardWindow.RequestClose(); // the duel is over: this resets it and closes the window
            CardMasterTalk.Open();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The capture lesson of the duel with Orin (BIBLE Q04): the FIRST capture of a duel with him is explained. Before the cards turn, the two numbers that were compared ("7 > 3": the placed card's edge facing the
    /// card it takes, and that card's edge) are shown side by side between the two cards for 0.35 s (CompareMs); once the cards have landed "CAPTURE!" pops over the placed card for 0.7 s. Text only (bold, dark rim,
    /// the number style of the duel cards) drawn in the board's effect layer; no popup, nothing blocks the board. Every later capture plays the plain flip animation of CardDuelBattleView.Flip.cs.
    /// </summary>
    public sealed partial class CardDuelBattleView
    {
        public const float CompareMs = 350f, CapturedMs = 700f;
        Text compareText, capturedText;
        bool compareDone;           // this duel has already explained a capture
        bool comparing;             // the numbers are on screen for the move being played
        float compareFrom, compareTo; // ms since the move began
        float capturedUntil = -1f;  // unscaled time the CAPTURE! word hides

        void BuildCapture()
        {
            compareText = CaptureLabel("Compare", 40, new Color(1f, 0.93f, 0.55f));
            capturedText = CaptureLabel("Captured", 30, Color.white);
        }

        Text CaptureLabel(string name, int size, Color colour)
        {
            Text t = U.Txt(fxLayer, name, -90f, -20f, 180f, 40f, "", size, colour, TextAnchor.MiddleCenter);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0f, 1f);
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            t.resizeTextForBestFit = false; t.fontSize = size; t.fontStyle = FontStyle.Bold; t.alignByGeometry = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            var rim = t.gameObject.AddComponent<Outline>(); rim.effectColor = new Color(0.16f, 0.09f, 0.03f, 1f); rim.effectDistance = new Vector2(2f, -2f);
            t.gameObject.SetActive(false);
            return t;
        }

        /// <summary>Called by BeginMove: the extra wait (ms) before the cards turn when this move is the first capture of a duel with Orin (and the numbers start showing), else 0. `victim` = the first cell that turns, `dir` = 0 up, 1 right, 2 down, 3 left.</summary>
        float CaptureLead(DuelView v, DuelMove m, int victim, int dir)
        {
            if (!v.Npc || compareDone || m.FlipMask == 0 || victim < 0 || compareText == null) return 0f;
            CardDef a = CardTable.ByIndex(m.Card), d = CardTable.ByIndex(dispCard[victim]);
            if (a == null || d == null) return 0f;
            int mine = dir == 0 ? a.Top : dir == 1 ? a.Right : dir == 2 ? a.Bottom : a.Left;
            int theirs = dir == 0 ? d.Bottom : dir == 1 ? d.Left : dir == 2 ? d.Top : d.Right;
            compareDone = true; comparing = true;
            compareFrom = PlaceMs + PlaceGapMs; compareTo = compareFrom + CompareMs;
            compareText.text = mine + " > " + theirs;
            compareText.rectTransform.anchoredPosition = (CellCentre(m.Cell) + CellCentre(victim)) * 0.5f;
            capturedText.rectTransform.anchoredPosition = CellCentre(m.Cell) + new Vector2(0f, 8f);
            return CompareMs;
        }

        /// <summary>Per frame while a move plays (t = ms since it began): the numbers are visible only in their window.</summary>
        void TickCapture(float t)
        {
            if (compareText == null) return;
            bool show = comparing && t >= compareFrom && t < compareTo;
            if (compareText.gameObject.activeSelf != show) compareText.gameObject.SetActive(show);
            if (comparing && t >= compareTo) comparing = false;
        }

        /// <summary>The cards of the explained capture have landed: "CAPTURE!" over the placed card.</summary>
        void CaptureLanded(bool explained)
        {
            if (!explained || capturedText == null) return;
            capturedText.text = "CAPTURE!";
            capturedText.gameObject.SetActive(true);
            capturedUntil = Time.unscaledTime + CapturedMs / 1000f;
        }

        void TickCaptured()
        {
            if (capturedText != null && capturedUntil > 0f && Time.unscaledTime >= capturedUntil) { capturedUntil = -1f; capturedText.gameObject.SetActive(false); }
        }

        void ResetCapture()
        {
            compareDone = comparing = false; capturedUntil = -1f;
            if (compareText != null) { compareText.gameObject.SetActive(false); capturedText.gameObject.SetActive(false); }
        }
    }
}

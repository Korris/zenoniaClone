using System;
using UnityEngine;
using UnityEngine.UI;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The "Sure?" box of the Card window (Sell, Convert SR+, Socket): the ChatGPT detail panel with the question, a main button (the action, teal) and Cancel (blue), over a veil that
    /// stops taps reaching the page behind. Built once by CardWindow; pages call Ask. Closing the window cancels it.
    /// </summary>
    public sealed class CardConfirmBox
    {
        const float W = 480f, H = 230f, PadX = 28f;
        static CardConfirmBox instance;

        readonly RectTransform veil;
        readonly Text message;
        readonly U.PieceButton yes, no;
        Action onYes, onNo;

        public static bool IsOpen { get { return instance != null && instance.veil.gameObject.activeSelf; } }

        public CardConfirmBox(RectTransform parent, float parentW, float parentH)
        {
            instance = this;
            veil = UiKit.Fill(parent, "ConfirmVeil");
            UiKit.Panel(veil, new Color(0.16f, 0.22f, 0.3f, 0.35f), true); // taps stop here (a veil, not art)
            RectTransform box = UiKit.Rect(veil, "Box", (parentW - W) * 0.5f, (parentH - H) * 0.5f, W, H);
            U.Panel(box, "Frame", 0f, 0f, W, H, true);
            message = U.Txt(box, "Message", PadX, 22f, W - PadX * 2f, H - 108f, "", U.TextNormal + 2, U.Ink, TextAnchor.MiddleCenter);
            float bw = 190f, gap = 20f, x = (W - bw * 2f - gap) * 0.5f;
            yes = new U.PieceButton(box, "Yes", x, H - 70f, bw, U.ButtonH, GemUiArt.Frame("btn-teal"), "Yes", () => { Action go = onYes; Close(); if (go != null) go(); });
            no = new U.PieceButton(box, "Cancel", x + bw + gap, H - 70f, bw, U.ButtonH, GemUiArt.Frame("btn-blue"), "Cancel", () => { Action alt = onNo; Close(); if (alt != null) alt(); });
            veil.gameObject.SetActive(false);
        }

        /// <summary>`cancelLabel` / `onCancel`: the second button's text and what it does besides closing the box (Orin's "Duel / Later", "Practice / Open Cards"). Esc only closes the box.</summary>
        public static void Ask(string text, string yesLabel, Action go, string cancelLabel = "Cancel", Action onCancel = null)
        {
            if (instance == null || go == null) return; // without the box there is no safe way to confirm: nothing is sent
            instance.message.text = text;
            instance.yes.Set(yesLabel, true);
            instance.no.Set(cancelLabel, true);
            instance.onYes = go; instance.onNo = onCancel;
            instance.veil.SetAsLastSibling();
            instance.veil.gameObject.SetActive(true);
        }

        public static void Close()
        {
            if (instance == null) return;
            instance.onYes = null; instance.onNo = null;
            instance.veil.gameObject.SetActive(false);
        }

        public static void Forget(CardConfirmBox box) { if (instance == box) instance = null; }
    }
}

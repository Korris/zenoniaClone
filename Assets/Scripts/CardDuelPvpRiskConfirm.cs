using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The first-PvP-duel question (flag CardCollection.HelpPvpRisk, stored with the character): before a character challenges someone or accepts an invite that has cards at stake, a box says "winner takes 1 card
    /// from the loser's deck" and shows the 8 cards of the current deck that could be taken. "I understand" stores the flag (InvOp.CardHelp) and runs the action; Cancel / Esc drop it. Never asked for Ranked (no
    /// cards at stake, it does not call this) or once the flag is set. Drawn over the world with the card kit (panel, card frames, teal / blue buttons); built the first time it is needed.
    /// </summary>
    public sealed class CardDuelPvpRiskConfirm : MonoBehaviour
    {
        const float W = 480f, H = 438f, Pad = 24f, CardW = 76f, CardH = 101f, GapX = 20f, GapY = 12f, GridY = 112f;
        static CardDuelPvpRiskConfirm instance;
        static PlayerState acknowledgedFor; // the character that just said "I understand": the server's answer takes a moment, do not ask twice in a row (another character is still asked)

        RectTransform overlay, box;
        readonly GemCardView[] slot = new GemCardView[DeckRules.Size];
        Action onYes;
        float fitW, fitH;

        public static bool IsOpen { get { return instance != null && instance.overlay.gameObject.activeSelf; } }

        static CardDuelPvpRiskConfirm() { UiWindows.RegisterModal(() => IsOpen, Close); }

        /// <summary>The character has not yet said it understands the risk of a duel with cards at stake.</summary>
        public static bool Needed(PlayerState p) { return p != null && acknowledgedFor != p && (p.Cards.Help & CardCollection.HelpPvpRisk) == 0; }

        /// <summary>Runs `go` now when the question was answered before, else shows the question and runs it after "I understand".</summary>
        public static void Ask(Action go)
        {
            if (go == null) return;
            PlayerState p = PetActions.Me;
            if (!Needed(p) || UiScreens.Root == null) { go(); return; }
            if (instance == null)
            {
                var obj = new GameObject("CardDuelPvpRiskConfirm");
                obj.transform.SetParent(UiScreens.Root, false);
                instance = obj.AddComponent<CardDuelPvpRiskConfirm>();
                instance.Build(UiKit.Canvas(UiScreens.Root).transform);
            }
            instance.Show(p, go);
        }

        public static void Close() { if (instance != null) { instance.onYes = null; instance.overlay.gameObject.SetActive(false); } }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(Transform canvas)
        {
            overlay = UiKit.Fill(canvas, "PvpRiskOverlay");
            UiKit.Panel(overlay, new Color(0.16f, 0.22f, 0.3f, 0.35f), true); // taps stop here (a veil, not art)
            box = UiWinKit.Anchored(overlay, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, W, H);
            U.Panel(box, "Frame", 0f, 0f, W, H, true).raycastTarget = true;
            U.Txt(box, "Title", Pad, 16f, W - 2f * Pad, 34f, "Cards at stake", 24, U.Bad, TextAnchor.MiddleCenter);
            U.Txt(box, "Text", Pad, 54f, W - 2f * Pad, 52f, "The winner takes 1 card from the loser's deck. If you lose, the winner may take any of these 8 cards:", U.TextNormal, U.Ink, TextAnchor.UpperCenter);
            float x0 = (W - (4 * CardW + 3 * GapX)) * 0.5f;
            for (int i = 0; i < slot.Length; i++)
                slot[i] = new GemCardView(box, "Card" + i, x0 + (i % 4) * (CardW + GapX), GridY + (i / 4) * (CardH + GapY), CardW, CardH, null, false, false, 18); // no name bar: 76 px is too narrow for names like Meadow Slime, the art and the numbers say which card it is
            U.Txt(box, "Note", Pad, GridY + 2f * CardH + GapY + 8f, W - 2f * Pad, 24f, "A draw takes nothing. Ranked duels take no cards.", U.TextSmall, U.InkSoft, TextAnchor.MiddleCenter);
            float bw = 190f, gap = 20f, x = (W - bw * 2f - gap) * 0.5f;
            new U.PieceButton(box, "Yes", x, H - 70f, bw, U.ButtonH, GemUiArt.Frame("btn-teal"), "I understand", Confirm);
            new U.PieceButton(box, "Cancel", x + bw + gap, H - 70f, bw, U.ButtonH, GemUiArt.Frame("btn-blue"), "Cancel", Close);
            NameTags.Occlude(box); // IMGUI name tags would draw over the box
            overlay.gameObject.SetActive(false);
        }

        void Show(PlayerState p, Action go)
        {
            onYes = go;
            for (int i = 0; i < slot.Length; i++)
            {
                CardDef c = i < p.Cards.Deck.Count ? CardTable.Get(p.Cards.Deck[i]) : null;
                slot[i].Set(c, 0, false);
                slot[i].Root.gameObject.SetActive(c != null);
            }
            overlay.SetAsLastSibling();
            overlay.gameObject.SetActive(true);
            fitW = 0f; // refit on the next frame
        }

        void Confirm()
        {
            Action go = onYes;
            acknowledgedFor = PetActions.Me;
            PetActions.Send(InvOp.CardHelp, (byte)CardCollection.HelpPvpRisk); // stored with the character (offline: saved at once)
            Close();
            if (go != null) go();
        }

        void Update()
        {
            if (!overlay.gameObject.activeSelf || (overlay.rect.width == fitW && overlay.rect.height == fitH)) return;
            fitW = overlay.rect.width; fitH = overlay.rect.height;
            box.localScale = Vector3.one * Mathf.Max(0.1f, Mathf.Min(1f, fitW * 0.96f / W, (fitH - 8f) / H));
        }
    }
}

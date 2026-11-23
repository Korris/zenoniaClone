using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The player-vs-player challenge boxes of the Card Duel (phase 06), drawn over the world (the Card window need not be open): the invite you RECEIVED (who, a plain "Card Wager" or "Friendly Duel" tag so the
    /// stakes are clear BEFORE Accept, the seconds left, Accept / Decline), the invite you SENT (Waiting, Cancel), and the wager panel (CardWagerPanel) once a wager is accepted. It only draws what the server last
    /// said (DuelClient.Lobby) and sends requests; the server decides every step. Created the first time a lobby message arrives.
    /// </summary>
    public sealed class CardDuelLobbyWindow : MonoBehaviour
    {
        const float InviteW = 480f, InviteH = 268f, WaitW = 480f, WaitH = 188f, Pad = 24f;
        static CardDuelLobbyWindow instance;

        RectTransform overlay, invite, wait;
        CardWagerPanel panel;
        Text inviteTitle, inviteLine, inviteRule, inviteWarn, inviteSecs, waitLine, waitSecs;
        Image invitePill, waitPill;
        Text invitePillText, waitPillText;
        U.PieceButton accept;
        int shownVersion = -1;
        float fitW, fitH;

        public static bool WagerPanelOpen { get { return instance != null && instance.panel != null && instance.panel.Visible; } }

        static CardDuelLobbyWindow() { UiWindows.RegisterModal(() => WagerPanelOpen, DuelClient.LeaveLobby); }

        /// <summary>A lobby message arrived: make sure the window exists and redraw.</summary>
        public static void Sync()
        {
            if (instance == null)
            {
                if (!DuelClient.InLobby || UiScreens.Root == null) return;
                var go = new GameObject("CardDuelLobbyWindow");
                go.transform.SetParent(UiScreens.Root, false);
                instance = go.AddComponent<CardDuelLobbyWindow>();
                instance.Build(UiKit.Canvas(UiScreens.Root).transform);
            }
            instance.Apply();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(Transform canvas)
        {
            overlay = UiKit.Fill(canvas, "DuelLobbyOverlay");
            invite = UiWinKit.Anchored(overlay, "Invite", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 70f, InviteW, InviteH);
            U.Panel(invite, "Frame", 0f, 0f, InviteW, InviteH, true).raycastTarget = true; // swallows taps so they never reach the world
            inviteTitle = U.Txt(invite, "Title", Pad, 14f, InviteW - 2f * Pad - 80f, 32f, "Card Duel Challenge", 24, U.Ink);
            ClockIcon(invite, InviteW - Pad - 76f, 18f);
            inviteSecs = U.Txt(invite, "Secs", InviteW - Pad - 50f, 14f, 50f, 32f, "", U.TextNormal + 2, U.Ink, TextAnchor.MiddleRight);
            inviteLine = U.Txt(invite, "Line", Pad, 52f, InviteW - 2f * Pad, 30f, "", 20, U.Ink);
            invitePill = GemUiArt.Img(invite, "Tag", Pad, 92f, 190f, 38f, GemUiArt.Frame("btn-gold"));
            invitePillText = TagText(invite, Pad, 92f, 190f, 38f);
            inviteRule = U.Txt(invite, "Rule", Pad, 136f, InviteW - 2f * Pad, 56f, "", U.TextNormal, U.InkSoft, TextAnchor.UpperLeft);
            inviteWarn = U.Txt(invite, "Warn", Pad, 134f, InviteW - 2f * Pad, 24f, "", U.TextSmall, U.Bad, TextAnchor.UpperLeft);
            float bw = 196f, gap = 20f, x = (InviteW - 2f * bw - gap) * 0.5f;
            accept = new U.PieceButton(invite, "Accept", x, InviteH - 66f, bw, U.ButtonH, GemUiArt.Frame("btn-teal"), "Accept", DuelClient.Accept);
            new U.PieceButton(invite, "Decline", x + bw + gap, InviteH - 66f, bw, U.ButtonH, GemUiArt.Frame("btn-blue"), "Decline", DuelClient.Decline);

            wait = UiWinKit.Anchored(overlay, "Wait", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 70f, WaitW, WaitH);
            U.Panel(wait, "Frame", 0f, 0f, WaitW, WaitH, true).raycastTarget = true;
            U.Txt(wait, "Title", Pad, 12f, WaitW - 2f * Pad - 80f, 30f, "Challenge Sent", 22, U.Ink);
            ClockIcon(wait, WaitW - Pad - 76f, 14f);
            waitSecs = U.Txt(wait, "Secs", WaitW - Pad - 50f, 12f, 50f, 30f, "", U.TextNormal + 2, U.Ink, TextAnchor.MiddleRight);
            waitLine = U.Txt(wait, "Line", Pad, 48f, WaitW - 2f * Pad, 30f, "", 18, U.Ink);
            waitPill = GemUiArt.Img(wait, "Tag", Pad, 84f, 190f, 34f, GemUiArt.Frame("btn-gold"));
            waitPillText = TagText(wait, Pad, 84f, 190f, 34f);
            new U.PieceButton(wait, "Cancel", (WaitW - 196f) * 0.5f, WaitH - 62f, 196f, 46f, GemUiArt.Frame("btn-red"), "Cancel", DuelClient.CancelInvite);

            NameTags.Occlude(invite); NameTags.Occlude(wait); // IMGUI name tags would draw over these boxes
            panel = new CardWagerPanel(overlay);
            invite.gameObject.SetActive(false); wait.gameObject.SetActive(false);
        }

        static void ClockIcon(RectTransform parent, float x, float y)
        {
            TradeArt.Plain(UiKit.Rect(parent, "Clock", x, y, 24f, 24f), "clock");
        }

        static Text TagText(RectTransform parent, float x, float y, float w, float h)
        {
            Text t = U.Txt(parent, "TagText", x, y, w, h, "", 17, U.OnButton, TextAnchor.MiddleCenter);
            var rim = t.gameObject.AddComponent<Outline>(); rim.effectColor = new Color(0.45f, 0.28f, 0.1f, 0.85f); rim.effectDistance = new Vector2(1.2f, -1.2f);
            return t;
        }

        static void SetTag(Image skin, Text text, bool wager)
        {
            GemUiArt.SetPiece(skin, GemUiArt.Frame(wager ? "btn-gold" : "btn-blue"));
            text.text = wager ? "Card Wager" : "Friendly Duel";
        }

        void Apply()
        {
            DuelLobbyMsg m = DuelClient.Lobby;
            bool incoming = m.Phase == 1, outgoing = m.Phase == 2, pick = m.Phase == 3 || m.Phase == 4;
            string other = string.IsNullOrEmpty(m.OtherName) ? "A player" : m.OtherName;
            if (incoming)
            {
                inviteLine.text = other + " challenges you to a Card Duel!";
                SetTag(invitePill, invitePillText, m.Wager);
                string need = m.Wager ? CardWagerPanel.EntryProblem() : "";
                inviteRule.text = m.Wager
                    ? "Each of you puts in 1 spare N or R card of the same rarity. The winner takes both cards. Nothing moves until you both confirm."
                    : "No cards at stake. Win, lose or draw you still earn Gold (5 paid matches a day).";
                inviteWarn.text = need;
                inviteRule.gameObject.SetActive(need.Length == 0);
                accept.Set("Accept", need.Length == 0);
            }
            if (outgoing)
            {
                waitLine.text = "Waiting for " + other + " to answer...";
                SetTag(waitPill, waitPillText, m.Wager);
            }
            invite.gameObject.SetActive(incoming);
            wait.gameObject.SetActive(outgoing);
            panel.Show(pick);
            if (incoming || outgoing || pick) overlay.SetAsLastSibling();
            shownVersion = DuelClient.LobbyVersion;
            if (pick) panel.Refresh();
        }

        void Update()
        {
            if (shownVersion != DuelClient.LobbyVersion) Apply();
            if (overlay.rect.width != fitW || overlay.rect.height != fitH)
            {
                fitW = overlay.rect.width; fitH = overlay.rect.height;
                Fit(invite, InviteW, InviteH); Fit(wait, WaitW, WaitH); panel.Fit(fitW, fitH);
            }
            string secs = Mathf.CeilToInt(DuelClient.LobbySecondsLeft).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (invite.gameObject.activeSelf && inviteSecs.text != secs) inviteSecs.text = secs;
            if (wait.gameObject.activeSelf && waitSecs.text != secs) waitSecs.text = secs;
            if (panel.Visible) panel.Tick();
        }

        void Fit(RectTransform box, float w, float h)
        {
            float scale = Mathf.Min(1f, fitW * 0.96f / w, (fitH - 8f) / h);
            box.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }
    }
}

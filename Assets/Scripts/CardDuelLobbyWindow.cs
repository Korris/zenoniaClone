using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The player-vs-player challenge boxes of the Card Duel (phase 06 v2), drawn over the world (the Card window need not be open): the invite you RECEIVED (who, the stakes in plain words BEFORE Accept:
    /// "Winner takes 1 card from the loser's deck", the seconds left, Accept / Decline) and the invite you SENT (Waiting, Cancel). It only draws what the server last said (DuelClient.Lobby) and sends requests;
    /// the server decides every step. There is no card picking before the duel any more: the winner picks AFTER the match (CardDuelPickPanel). Created the first time a lobby message arrives.
    /// </summary>
    public sealed class CardDuelLobbyWindow : MonoBehaviour
    {
        const float InviteW = 480f, InviteH = 316f, WaitW = 480f, WaitH = 188f, Pad = 24f;
        static CardDuelLobbyWindow instance;

        RectTransform overlay, invite, wait;
        Text inviteTitle, inviteLine, inviteRule, inviteWarn, inviteSecs, waitLine, waitSecs;
        Image invitePill, waitPill;
        Text invitePillText, waitPillText;
        U.PieceButton accept;
        int shownVersion = -1;
        float fitW, fitH;

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
            inviteRule = U.Txt(invite, "Rule", Pad, 138f, InviteW - 2f * Pad, 30f, "", U.TextNormal + 2, U.Ink, TextAnchor.UpperLeft);
            inviteWarn = U.Txt(invite, "Warn", Pad, 172f, InviteW - 2f * Pad, 56f, "", U.TextNormal, U.Bad, TextAnchor.UpperLeft);
            float bw = 196f, gap = 20f, x = (InviteW - 2f * bw - gap) * 0.5f;
            accept = new U.PieceButton(invite, "Accept", x, InviteH - 68f, bw, U.ButtonH, GemUiArt.Frame("btn-teal"), "Accept", DuelClient.Accept);
            new U.PieceButton(invite, "Decline", x + bw + gap, InviteH - 68f, bw, U.ButtonH, GemUiArt.Frame("btn-blue"), "Decline", DuelClient.Decline);

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

        static void SetTag(Image skin, Text text, bool stakes)
        {
            GemUiArt.SetPiece(skin, GemUiArt.Frame(stakes ? "btn-gold" : "btn-blue"));
            text.text = stakes ? "Cards at stake" : "No cards at stake";
        }

        void Apply()
        {
            DuelLobbyMsg m = DuelClient.Lobby;
            bool incoming = m.Phase == 1, outgoing = m.Phase == 2;
            string other = string.IsNullOrEmpty(m.OtherName) ? "A player" : m.OtherName;
            if (incoming)
            {
                inviteLine.text = other + " challenges you to a Card Duel!";
                SetTag(invitePill, invitePillText, m.Stakes);
                string need = m.Stakes ? DuelClient.EntryProblem() : "";
                inviteRule.text = m.Stakes ? "Winner takes 1 card from the loser's deck." : "Nothing is at stake in this duel.";
                inviteWarn.text = need.Length > 0 ? need : m.Stakes ? "Your whole 8-card deck is at risk: the winner may take any of its cards, even Locked, SR, SSR or Boss ones. A draw takes nothing." : "";
                accept.Set("Accept", need.Length == 0);
            }
            if (outgoing)
            {
                waitLine.text = "Waiting for " + other + " to answer...";
                SetTag(waitPill, waitPillText, m.Stakes);
            }
            invite.gameObject.SetActive(incoming);
            wait.gameObject.SetActive(outgoing);
            if (incoming || outgoing) overlay.SetAsLastSibling();
            shownVersion = DuelClient.LobbyVersion;
        }

        void Update()
        {
            if (shownVersion != DuelClient.LobbyVersion) Apply();
            if (overlay.rect.width != fitW || overlay.rect.height != fitH)
            {
                fitW = overlay.rect.width; fitH = overlay.rect.height;
                Fit(invite, InviteW, InviteH); Fit(wait, WaitW, WaitH);
            }
            string secs = Mathf.CeilToInt(DuelClient.LobbySecondsLeft).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (invite.gameObject.activeSelf && inviteSecs.text != secs) inviteSecs.text = secs;
            if (wait.gameObject.activeSelf && waitSecs.text != secs) waitSecs.text = secs;
        }

        void Fit(RectTransform box, float w, float h)
        {
            float scale = Mathf.Min(1f, fitW * 0.96f / w, (fitH - 8f) / h);
            box.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }
    }
}

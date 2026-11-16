using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Typing bar of the compact chat ("Typing on mobile" of hud-compact-parts-board-states-v2.png): one slim dark capsule on the bottom
    /// edge of the chat block (PaperChatFeedView.Block), under the lines: the channel pill (a tap, or Tab, steps World / Map / Party /
    /// Whisper), the single-line field, a small send button. ChatWindow opens it from the tiny chat icon, a tap on the lines or Enter,
    /// and closes it on send, Esc or a tap outside. On a phone the block is lifted above the on-screen keyboard while it is up.
    /// </summary>
    public sealed class PaperChatBarView : ChatPanel
    {
        static readonly Color Back = new Color(0.05f, 0.06f, 0.09f, 0.78f), PlaceholderInk = new Color(1f, 0.97f, 0.88f, 0.5f);

        ChatChannel send = ChatChannel.World;
        Text pillLabel;
        Image pill;
        RectTransform lifted;
        float shownLift;

        /// <param name="block">the chat block; the bar lies on its bottom edge and the whole block is what the keyboard lifts</param>
        /// <param name="channelChanged">the pill was tapped (the window puts the caret back in the field)</param>
        public static PaperChatBarView Build(RectTransform block, bool web, Action channelChanged, Action sendLine)
        {
            var v = new PaperChatBarView { lifted = block };
            float h = web ? 24f : HudAnchors.ChatBarHeight, pad = 3f, pillW = web ? 58f : 66f, inner = h - pad * 2f;
            v.root = UiKit.Fill(block, "ChatBar");
            v.root.anchorMax = new Vector2(1f, 0f);
            float bottom = web ? PaperChatFeedView.WebBarBottom : HudAnchors.ChatBarBottom;
            v.root.offsetMin = new Vector2(0f, bottom);
            v.root.offsetMax = new Vector2(0f, bottom + h);
            PaperArt.Draw(v.root, PaperPiece.CChatBar, Back, PaperHudCompact.Rim, h * 0.5f, true);

            RectTransform p = UiWinKit.Anchored(v.root, "ChannelPill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), pad, 0f, pillW, inner);
            v.pill = PaperArt.Draw(p, PaperPiece.CChatPill, Color.white, Color.clear, inner * 0.5f);
            v.pillLabel = PaperTheme.Caption(p, "", web ? 11 : 12, PaperTheme.Ink, PaperInk.Plain);
            if (v.pill.sprite != PaperArt.Round()) v.pillLabel.rectTransform.offsetMax = new Vector2(-inner * 0.6f, 0f); // the cut pill has its arrow on the right
            UiWinKit.MakeButton(v.pill, () => { v.NextChannel(); channelChanged(); });

            RectTransform s = UiWinKit.Anchored(v.root, "ChatSend", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), -pad, 0f, inner + 6f, inner);
            Image sendFace = PaperArt.Draw(s, PaperPiece.CChatSend, PaperTheme.BtnGold, Color.clear, inner * 0.5f);
            if (sendFace.sprite == PaperArt.Round()) PaperTheme.Caption(s, ">", 13, PaperTheme.Ink, PaperInk.Plain);
            UiWinKit.MakeButton(sendFace, sendLine);

            v.field = UiKit.Fill(v.root, "Input");
            v.field.offsetMin = new Vector2(pad + pillW + 4f, 0f);
            v.field.offsetMax = new Vector2(-(pad + inner + 10f), 0f);
            Image hit = UiKit.Panel(v.field, Color.clear, true);
            int font = web ? 12 : 13;
            v.placeholder = PaperTheme.Body(v.field, "Placeholder", 0f, 0f, 10f, h, "Type a message...", font, PlaceholderInk);
            Text typed = PaperTheme.Body(v.field, "Typed", 0f, 0f, 10f, h, "", font, PaperTheme.TextLight);
            foreach (Text t in new[] { v.placeholder, typed })
            {
                RectTransform rt = t.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(4f, 1f); rt.offsetMax = new Vector2(-4f, -1f);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.font = PaperChatStyle.Font;
            }
            typed.supportRichText = false; // what the player types is shown as typed
            v.field.gameObject.AddComponent<RectMask2D>(); // a long line scrolls inside the field
            v.input = v.field.gameObject.AddComponent<InputField>();
            v.input.targetGraphic = hit; v.input.textComponent = typed; v.input.placeholder = v.placeholder;
            v.input.transition = Selectable.Transition.None;
            v.input.lineType = InputField.LineType.SingleLine; v.input.characterLimit = ChatHub.MaxLength;
            v.input.customCaretColor = true; v.input.caretColor = PaperTheme.TextLight;
            v.input.selectionColor = new Color(PaperTheme.Gold.r, PaperTheme.Gold.g, PaperTheme.Gold.b, 0.5f);
            v.input.onValidateInput = (text, pos, c) => c == '\t' ? '\0' : c; // Tab switches the channel, it is never typed
            v.ShowChannel();
            return v;
        }

        public override ChatChannel SendChannel { get { return send; } }

        public override void NextChannel()
        {
            send = (ChatChannel)(((int)send + 1) % ChatChannelFilter.Channels);
            ShowChannel();
        }

        /// <summary>The pill names the channel; without its art the flat pill takes the channel's colour.</summary>
        void ShowChannel()
        {
            pillLabel.text = send.ToString();
            if (pill.sprite == PaperArt.Round()) pill.color = PaperTheme.Hex("#" + ChatTextFormat.Feed.Tag(send));
        }

        /// <summary>The bar shows no lines of its own (the feed above it does); each refresh it only follows the phone keyboard.</summary>
        public override void Refresh(IList<ChatMessage> history) { FollowKeyboard(); }

        /// <summary>
        /// Phone: while the on-screen keyboard is up the chat block (lines + bar) moves above it. Devices that do not report the
        /// keyboard's height (some Android versions) get the middle of the screen, which every keyboard leaves free.
        /// </summary>
        void FollowKeyboard()
        {
            float lift = 0f;
            if (TouchScreenKeyboard.isSupported && TouchScreenKeyboard.visible)
            {
                Canvas c = lifted.GetComponentInParent<Canvas>();
                float scale = c != null && c.rootCanvas.scaleFactor > 0f ? c.rootCanvas.scaleFactor : 1f;
                float area = TouchScreenKeyboard.area.height;
                lift = (area > 0f ? area : Screen.height * 0.5f) / scale;
            }
            if (Mathf.Approximately(lift, shownLift)) return;
            shownLift = lift;
            lifted.anchoredPosition = new Vector2(lifted.anchoredPosition.x, lift);
        }
    }
}

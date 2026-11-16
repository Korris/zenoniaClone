using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The chat history of the PC layout: a wooden notice board (parchment in a wooden frame) with the red "Chat" ribbon, a red X, the
    /// channel ribbons hanging off its left edge (All / World / Map / Party / Whisper = which lines are listed), filter chips while All
    /// is shown, the scrolling lines (the 40 of ChatHub), and its own input row: send-channel dropdown, paper field, gold Send.
    /// The compact HUD shows the chat as plain lines (PaperChatFeedView); this board opens over them, bottom-left, only when the player
    /// clicks the small icon beside "[Enter] to chat", and closes with its X. There is no board on mobile.
    /// No sender avatars (a chat line does not carry the class) and no "is typing" line. Parts: .Ribbons.cs, .Lines.cs, .SendMenu.cs.
    /// </summary>
    public sealed partial class PaperChatPanelView : ChatPanel
    {
        const float Width = 340f, Height = 232f; // no wider than the old web panel
        const float BoardTop = 10f, Rim = 18f, Gap = 4f; // the title ribbon overhangs the board by BoardTop; Rim = wood + a little paper
        const float RibbonW = 50f, ChipH = 22f, InputH = 28f, InnerW = Width - RibbonW - Rim * 2f, InnerH = Height - BoardTop - Rim * 2f;
        const int FontSize = 13;

        readonly ChatChannelFilter filter = new ChatChannelFilter();
        ChatChannel send = ChatChannel.World;
        Action changed;
        RectTransform inner;

        public static PaperChatPanelView Build(Transform layer, Action changed, Action close, Action sendLine)
        {
            var v = new PaperChatPanelView { changed = changed };
            v.root = UiWinKit.Anchored(layer, "PaperChatPanel", Vector2.zero, Vector2.zero, 6f, 8f, Width, Height);

            v.BuildRibbons(BoardTop + 30f); // before the board: their right ends tuck under its frame
            RectTransform board = PaperKit.Frame(v.root, "Board", RibbonW, BoardTop, Width - RibbonW, Height - BoardTop, PaperFrame.Paper);
            v.inner = UiKit.Fill(board, "Inner", Rim);
            const float TitleH = 26f, CloseSize = 26f;
            Sprite quill = PaperArt.Get(PaperPiece.ChatQuill); // the feather behind the title: only once it is cut
            if (quill != null) UiKit.Icon(v.root, "Quill", RibbonW + 2f, -4f, TitleH * 1.4f).sprite = quill;
            PaperKit.Ribbon(v.root, "Title", RibbonW + 16f, 0f, TitleH * 3.5f, TitleH, "Chat");
            PaperKit.CloseButton(v.root, Width - CloseSize + 2f, 0f, CloseSize, close);

            v.BuildChips();
            v.BuildLines();
            v.BuildInputRow(sendLine);
            v.BuildSendMenu(); // last: its list opens over the board
            v.ShowState();
            Current = v;
            return v;
        }

        /// <summary>Input row at the bottom of the paper: [send channel v] [field] [Send].</summary>
        void BuildInputRow(Action sendLine)
        {
            const float Y = InnerH - InputH, DropW = 70f, SendW = 56f;
            BuildSendButton(0f, Y, DropW);
            const float FieldW = InnerW - DropW - SendW - Gap * 2f;
            field = UiKit.Rect(inner, "Input", DropW + Gap, Y, FieldW, InputH);
            Image fieldBg = PaperArt.Draw(field, PaperPiece.ChatInput, PaperTheme.Cream, PaperTheme.InkSoft, 7f, true);
            placeholder = PaperTheme.Body(field, "Placeholder", 0f, 0f, FieldW, InputH, "Press Enter to chat", FontSize - 1, PaperTheme.InkSoft);
            placeholder.fontStyle = FontStyle.Italic;
            Text typed = PaperTheme.Body(field, "Typed", 0f, 0f, FieldW, InputH, "", FontSize - 1, PaperTheme.Ink);
            foreach (Text t in new[] { placeholder, typed })
            {
                RectTransform rt = t.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(9f, 2f); rt.offsetMax = new Vector2(-9f, -1f);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.font = PaperChatStyle.Font;
            }
            typed.supportRichText = false; // what the player types is shown as typed
            input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = fieldBg; input.textComponent = typed; input.placeholder = placeholder;
            input.transition = Selectable.Transition.None;
            input.lineType = InputField.LineType.SingleLine; input.characterLimit = ChatHub.MaxLength;
            input.customCaretColor = true; input.caretColor = PaperTheme.Ink;
            input.selectionColor = new Color(PaperTheme.Gold.r, PaperTheme.Gold.g, PaperTheme.Gold.b, 0.6f);
            input.onValidateInput = (text, pos, c) => c == '\t' ? '\0' : c; // Tab switches the channel, it is never typed
            new PaperButton(inner, "Send", InnerW - SendW, Y, SendW, InputH, "Send", PaperTone.Gold, sendLine);
        }

        public override ChatChannel SendChannel { get { return send; } }

        public override void NextChannel() { SetSend((ChatChannel)(((int)send + 1) % ChatChannelFilter.Channels)); }

        public override void Refresh(IList<ChatMessage> history) { RefreshLines(history); }

        /// <summary>
        /// The channel typed lines go to. The line just sent must be visible: a one-channel view follows the send channel,
        /// the All view turns that channel's chip back on.
        /// </summary>
        void SetSend(ChatChannel c)
        {
            send = c;
            if (filter.View != ChatChannelFilter.All) filter.View = (int)c;
            else filter.Enable(c);
            ShowState();
            changed();
        }

        /// <summary>A ribbon was picked: list that channel (and type on it), or everything.</summary>
        void SelectView(int view)
        {
            filter.View = view;
            if (view != ChatChannelFilter.All) send = (ChatChannel)view;
            jumpToNewest = true;
            ShowState();
            changed();
        }

        /// <summary>Ribbons, chips, the lines area and the send button follow the filter / send channel.</summary>
        void ShowState()
        {
            ShowRibbons();
            ShowChips();
            LayoutLines(filter.View == ChatChannelFilter.All);
            ShowSendChannel();
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The open chat panel: gold frame, World / Map / Party / Whisper tabs, close X, the last lines, the input field (+ Send button on mobile).
    /// Mobile: below the status plate on the left, ~38 % of a 16:9 screen wide. Web: bottom-left, semi-transparent, "Press Enter to chat".
    /// </summary>
    public sealed class ChatPanelView
    {
        public static readonly ChatChannel[] Tabs = { ChatChannel.World, ChatChannel.Map, ChatChannel.Party, ChatChannel.Whisper };

        public RectTransform root, field;
        public Text lines, placeholder;
        public InputField input;
        public int maxLines;
        readonly Image[] tabImages = new Image[4];
        readonly Text[] tabLabels = new Text[4];

        public static ChatPanelView Build(Transform layer, bool web, Action<int> selectTab, Action close, Action send)
        {
            var v = new ChatPanelView();
            float w = web ? 336f : 440f, h = web ? 166f : 276f, pad = web ? 12f : 16f, tabH = web ? 26f : 32f, inputH = web ? 30f : 38f;
            int font = web ? 14 : 16;
            v.maxLines = web ? 4 : 8;
            v.root = web ? UiWinKit.Anchored(layer, "ChatPanel", Vector2.zero, Vector2.zero, 6f, 8f, w, h)
                         : UiWinKit.Anchored(layer, "ChatPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), 6f, -RpgHud.BlockHeight - 10f, w, h);
            UiWinKit.Sliced(v.root, "panel", 0.34f, true).color = new Color(1f, 1f, 1f, web ? 0.86f : 0.97f);

            float closeSize = tabH - 4f, tabW = (w - pad * 2f - closeSize - 8f) / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                RectTransform t = UiKit.Rect(v.root, "Tab" + Tabs[i], pad + i * tabW, pad - 2f, tabW - 2f, tabH);
                v.tabImages[i] = UiWinKit.Plain(t, "tab-inactive"); v.tabImages[i].preserveAspect = false;
                UiWinKit.MakeButton(v.tabImages[i], () => selectTab(index));
                v.tabLabels[i] = UiKit.Label(UiKit.Fill(t, "Label"), Tabs[i].ToString(), font - 1, UiWinKit.Dim, TextAnchor.MiddleCenter, true);
            }
            UiWinKit.CloseButton(v.root, w - pad - closeSize + 2f, pad, closeSize, close);

            float linesTop = pad + tabH + 6f, inputTop = h - pad - inputH + 2f;
            RectTransform clip = UiKit.Rect(v.root, "Lines", pad + 4f, linesTop, w - pad * 2f - 8f, inputTop - linesTop - 4f);
            clip.gameObject.AddComponent<RectMask2D>();
            v.lines = UiKit.Label(UiKit.Fill(clip, "Text"), "", font, Color.white, TextAnchor.LowerLeft);
            v.lines.lineSpacing = web ? 1.05f : 1.15f;

            float sendW = web ? 0f : 56f;
            v.field = UiKit.Rect(v.root, "Input", pad, inputTop, w - pad * 2f - (web ? 0f : sendW + 8f), inputH);
            Image fieldBg = UiWinKit.Sliced(v.field, "chat-input", inputH / 96f, true);
            v.placeholder = UiKit.Label(UiKit.Fill(v.field, "Placeholder", 0f), web ? "Press Enter to chat" : "Type a message...", font - 1, new Color(0.6f, 0.65f, 0.75f), TextAnchor.MiddleLeft);
            v.placeholder.fontStyle = web ? FontStyle.Italic : FontStyle.Normal;
            Text typed = UiKit.Label(UiKit.Fill(v.field, "Typed", 0f), "", font - 1, Color.white, TextAnchor.MiddleLeft);
            v.placeholder.rectTransform.offsetMin = typed.rectTransform.offsetMin = new Vector2(12f, 2f);
            v.placeholder.rectTransform.offsetMax = typed.rectTransform.offsetMax = new Vector2(-12f, -2f);
            v.placeholder.horizontalOverflow = typed.horizontalOverflow = HorizontalWrapMode.Overflow;
            v.input = v.field.gameObject.AddComponent<InputField>();
            v.input.targetGraphic = fieldBg; v.input.textComponent = typed; v.input.placeholder = v.placeholder;
            v.input.lineType = InputField.LineType.SingleLine; v.input.characterLimit = ChatHub.MaxLength;
            v.lines.font = v.placeholder.font = typed.font = ChatArt.TextFont();
            v.input.onValidateInput = (text, pos, c) => c == '\t' ? '\0' : c; // Tab switches the channel, it is never typed

            if (!web)
            {
                RectTransform sendRt = UiKit.Rect(v.root, "Send", w - pad - sendW, inputTop, sendW, inputH);
                UiWinKit.MakeButton(UiWinKit.Plain(sendRt, "send"), send).GetComponent<Image>().preserveAspect = false;
            }
            return v;
        }

        public void ShowTab(int selected)
        {
            for (int k = 0; k < Tabs.Length; k++)
            {
                tabImages[k].sprite = UiArt.Win(k == selected ? "tab-active" : "tab-inactive");
                tabLabels[k].color = k == selected ? UiWinKit.Cream : UiWinKit.Dim;
            }
        }
    }
}

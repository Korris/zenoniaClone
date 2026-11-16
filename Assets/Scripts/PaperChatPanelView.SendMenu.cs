using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Send-channel dropdown left of the input field: shows the channel typed lines go to, in its colour; a tap opens the list of the
    /// four channels above it (Tab while typing steps through them too).
    /// </summary>
    public sealed partial class PaperChatPanelView
    {
        Text sendLabel;
        RectTransform sendMenu, sendButton;

        void BuildSendButton(float x, float y, float w)
        {
            sendButton = UiKit.Rect(inner, "SendChannel", x, y, w, InputH);
            Image face = PaperArt.Draw(sendButton, PaperPiece.ChatDropdown, PaperTheme.Cream, PaperTheme.InkSoft, 7f);
            UiWinKit.MakeButton(face, () => sendMenu.gameObject.SetActive(!sendMenu.gameObject.activeSelf));
            bool art = face.sprite != PaperArt.Round();
            sendLabel = PaperTheme.Caption(sendButton, "", 11, PaperTheme.Ink, PaperInk.Plain);
            sendLabel.rectTransform.offsetMax = new Vector2(-InputH * 0.6f, 0f); // the art's arrow sits on the right
            if (!art) PaperTheme.Body(sendButton, "Arrow", w - InputH * 0.7f, 0f, InputH * 0.6f, InputH, "v", PaperTheme.BarSize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleCenter, true);
        }

        /// <summary>The list of channels, opening upwards from the dropdown (over the lines).</summary>
        void BuildSendMenu()
        {
            float rowH = InputH - 4f, w = sendButton.sizeDelta.x + 8f, h = rowH * ChatChannelFilter.Channels + 8f;
            sendMenu = UiKit.Rect(inner, "SendMenu", 0f, InnerH - InputH - 2f - h, w, h);
            PaperArt.Plate(sendMenu, PaperTheme.Cream, PaperTheme.InkSoft, 7f, true);
            for (int c = 0; c < ChatChannelFilter.Channels; c++)
            {
                ChatChannel channel = (ChatChannel)c;
                RectTransform row = UiKit.Rect(sendMenu, "Pick" + channel, 4f, 4f + c * rowH, w - 8f, rowH);
                Image hit = UiKit.Panel(row, Color.clear, true);
                UiWinKit.MakeButton(hit, () => SetSend(channel));
                PaperTheme.Caption(row, channel.ToString(), 11, InkOf(channel), PaperInk.Plain);
            }
            sendMenu.gameObject.SetActive(false);
        }

        void ShowSendChannel()
        {
            sendLabel.text = send.ToString();
            sendLabel.color = InkOf(send);
            if (sendMenu != null) sendMenu.gameObject.SetActive(false); // any pick (ribbon, chip row, Tab, the list itself) closes the list
        }

        /// <summary>The channel's tag colour on paper (ChatTextFormat.Paper), as a Color.</summary>
        static Color InkOf(ChatChannel c) { return PaperTheme.Hex("#" + ChatTextFormat.Paper.Tag(c)); }
    }
}

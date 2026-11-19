using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The open chat of the RO Soft HUD (user 19/11: ChatGPT-painted in the 02-hud.png style, sheet E ro-hud-e-chat-toast): the ivory
    /// 9-slice panel, the channel tabs as pills (navy = open tab, ivory = others), the round close X, one soft strip per message line
    /// (newest at the bottom), the white input box and the coral Send button with its paper plane. Laid out like CSS boxes
    /// (docs/ui-layout-rules.md): panel padding Pad, gap Gap between rows, tab pills padding 3 / 12 with the label centred both ways,
    /// strip padding 2 / 12, input padding 12; no text touches a border.
    /// Placement: mobile = centred under the menu row (like its one-line bar, RoHudChatBar); PC (RoHud.Pc) = bottom-left, open from the
    /// start, "Press Enter to chat". Same logic as the old panel (ChatWindow owns input, send and history).
    /// </summary>
    public sealed class RoHudChatPanel : ChatPanel
    {
        const float Pad = 16f, Gap = 8f, TabH = 32f, CloseSize = 32f, LineH = 30f, LineGap = 5f, InputH = 42f, SendSize = 48f;
        const int LinePadX = 14, LinePadY = 3;
        static readonly ChatChannel[] Tabs = ChatPanelView.Tabs;

        ChatChannel tab = ChatChannel.World;
        Action changed;
        readonly Image[] tabArt = new Image[4];
        readonly Text[] tabLabels = new Text[4];
        Text[] lineTexts;
        GameObject[] lineRows;

        public static RoHudChatPanel Build(Transform layer, bool pc, Action changed, Action close, Action send)
        {
            var v = new RoHudChatPanel { changed = changed };
            // Height from the line count, so no empty strip is left above the lines (acceptance round 2).
            const int Lines = 4;
            float w = pc ? 420f : 460f;
            float h = Pad + TabH + Gap + Lines * (LineH + LineGap) - LineGap + Gap - (SendSize - InputH) * 0.5f + SendSize + Pad;
            v.root = pc ? UiWinKit.Anchored(layer, "RoChatPanel", Vector2.zero, Vector2.zero, RoHud.Margin, RoHud.Margin, w, h)
                        : UiWinKit.Anchored(layer, "RoChatPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 0f, -96f, w, h); // under the menu row
            RoHud.Art(v.root, "hud-chat-panel", true).color = new Color(1f, 1f, 1f, pc ? 0.92f : 0.97f); // raycast: a click on the panel never attacks

            // Tab row: equal pills, gap Gap, the close X at the right end.
            RectTransform row = UiKit.Rect(v.root, "Tabs", Pad, Pad, w - Pad * 2f - CloseSize - Gap, TabH);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = Gap; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = hl.childForceExpandHeight = true;
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                RectTransform t = UiKit.Rect(row, "Tab" + Tabs[i], 0f, 0f, 10f, TabH);
                v.tabArt[i] = RoHud.Art(t, "hud-chat-tab", true);
                v.tabArt[i].pixelsPerUnitMultiplier = 48f / TabH; // the art's height = the pill's: round ends (ui-layout-rules sổ lỗi)
                UiWinKit.MakeButton(v.tabArt[i], () => v.SelectTab(index));
                RectTransform lab = UiKit.Fill(t, "Label");
                lab.offsetMin = new Vector2(RoHud.PillPadX, RoHud.PillPadY); lab.offsetMax = new Vector2(-RoHud.PillPadX, -RoHud.PillPadY);
                v.tabLabels[i] = RoHud.Label(lab, Tabs[i].ToString(), 14, RoHud.Ink, TextAnchor.MiddleCenter);
                v.tabLabels[i].resizeTextForBestFit = true; v.tabLabels[i].resizeTextMinSize = 10; v.tabLabels[i].resizeTextMaxSize = 14;
                v.tabLabels[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            RectTransform x = UiKit.Rect(v.root, "Close", w - Pad - CloseSize, Pad, CloseSize, CloseSize);
            UiWinKit.MakeButton(RoHud.Art(x, "hud-chat-close", true), () => close());

            // Input row at the bottom: the box (padding 12) + Send.
            float inputTop = h - Pad - SendSize + (SendSize - InputH) * 0.5f;
            v.field = UiKit.Rect(v.root, "Input", Pad, inputTop, w - Pad * 2f - SendSize - Gap - 4f, InputH);
            Image box = RoHud.Art(v.field, "hud-chat-input", true);
            box.pixelsPerUnitMultiplier = 56f / InputH;
            v.placeholder = RoHud.Label(Inset(v.field, "Placeholder", 12f, 2f), pc ? "Press Enter to chat" : "Type a message...", 14, RoSoftTheme.InkSoft, TextAnchor.MiddleLeft, false);
            v.placeholder.fontStyle = pc ? FontStyle.Italic : FontStyle.Normal;
            Text typed = RoHud.Label(Inset(v.field, "Typed", 12f, 2f), "", 14, RoHud.Ink, TextAnchor.MiddleLeft, false);
            typed.supportRichText = false;
            v.input = v.field.gameObject.AddComponent<InputField>();
            v.input.targetGraphic = box; v.input.textComponent = typed; v.input.placeholder = v.placeholder;
            v.input.lineType = InputField.LineType.SingleLine; v.input.characterLimit = ChatHub.MaxLength;
            v.input.caretColor = RoHud.Ink;
            v.input.onValidateInput = (text, pos, c) => c == '\t' ? '\0' : c; // Tab switches the channel, it is never typed
            RectTransform sendRt = UiKit.Rect(v.root, "Send", w - Pad - SendSize, h - Pad - SendSize, SendSize, SendSize);
            Image sendArt = RoHud.Art(sendRt, "hud-chat-send", true);
            UiWinKit.MakeButton(sendArt, () => send());
            Sprite up = RoHud.Piece("hud-chat-send"), down = RoHud.Piece("hud-chat-send-pressed");
            if (down != null) sendArt.gameObject.GetComponent<Button>().transition = Selectable.Transition.SpriteSwap;
            if (down != null) sendArt.gameObject.GetComponent<Button>().spriteState = new SpriteState { pressedSprite = down, highlightedSprite = up, selectedSprite = up };
            RoHud.KeyCap(sendRt, RoHud.TopLeft, 4f, -4f, InputAdapter.KeyLabel(GameKey.Chat));

            // Message strips between the tabs and the input, newest at the bottom.
            float linesTop = Pad + TabH + Gap, linesBottom = inputTop - Gap;
            int n = Mathf.Max(1, Mathf.FloorToInt((linesBottom - linesTop + LineGap) / (LineH + LineGap) + 0.01f));
            v.lineTexts = new Text[n]; v.lineRows = new GameObject[n];
            for (int i = 0; i < n; i++)
            {
                RectTransform strip = UiKit.Rect(v.root, "Line" + i, Pad, linesBottom - (n - i) * (LineH + LineGap) + LineGap, w - Pad * 2f, LineH);
                Image art = RoHud.Art(strip, "hud-chat-line");
                art.pixelsPerUnitMultiplier = 36f / LineH;
                art.color = new Color(1f, 1f, 1f, 0.75f);
                Text t = RoHud.Label(Inset(strip, "Text", LinePadX, LinePadY), "", 16, RoHud.Ink, TextAnchor.MiddleLeft, false);
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = 16; // a long line shrinks a little, never spills
                v.lineTexts[i] = t; v.lineRows[i] = strip.gameObject;
                strip.gameObject.SetActive(false);
            }
            v.ShowTab(0);
            NameTags.Occlude(v.root); // the IMGUI name tags stay off the open panel
            return v;
        }

        /// <summary>A child filling `parent` minus padding px (x) / py (y) on each side.</summary>
        static RectTransform Inset(RectTransform parent, string name, float px, float py)
        {
            RectTransform rt = UiKit.Fill(parent, name);
            rt.offsetMin = new Vector2(px, py); rt.offsetMax = new Vector2(-px, -py);
            return rt;
        }

        public override ChatChannel SendChannel { get { return tab; } }

        public override void NextChannel() { SelectTab((Array.IndexOf(Tabs, tab) + 1) % Tabs.Length); }

        public override void Refresh(IList<ChatMessage> history)
        {
            List<ChatMessage> picked = ChatTextFormat.Recent(history, tab, lineTexts.Length);
            int first = lineTexts.Length - picked.Count; // bottom-aligned: empty strips stay hidden at the top
            for (int i = 0; i < lineTexts.Length; i++)
            {
                bool on = i >= first;
                if (lineRows[i].activeSelf != on) lineRows[i].SetActive(on);
                lineTexts[i].text = on ? ChatTextFormat.Line(picked[i - first], ChatTextFormat.RoSoft) : "";
            }
        }

        void SelectTab(int i)
        {
            tab = Tabs[i];
            ShowTab(i);
            changed();
        }

        void ShowTab(int selected)
        {
            Sprite on = RoHud.Piece("hud-chat-tab-on"), off = RoHud.Piece("hud-chat-tab");
            for (int k = 0; k < Tabs.Length; k++)
            {
                tabArt[k].sprite = k == selected ? on : off;
                tabLabels[k].color = k == selected ? Color.white : RoHud.Ink;
            }
        }
    }
}

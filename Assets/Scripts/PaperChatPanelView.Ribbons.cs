using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Channel ribbons on the left edge of the notice board (which lines are listed; the picked one sticks out) and the filter chips
    /// on top of the paper (only while "All" is listed: All = every channel back on, one chip per channel = show / hide its lines).
    /// </summary>
    public sealed partial class PaperChatPanelView
    {
        const float RibbonTuck = 12f, RibbonOut = 6f; // hidden under the board's frame / how far the picked ribbon sticks out

        readonly RectTransform[] ribbons = new RectTransform[PaperChatStyle.Ribbons];
        readonly PaperTab[] chips = new PaperTab[PaperChatStyle.Ribbons]; // [0] = All, then the channels
        RectTransform chipRow;

        void BuildRibbons(float top)
        {
            const float h = 28f, pitch = h + 4f, w = RibbonW - RibbonOut + RibbonTuck;
            for (int i = 0; i < ribbons.Length; i++)
            {
                int view = i - 1;
                RectTransform rt = ribbons[i] = UiKit.Rect(root, "Ribbon" + PaperChatStyle.Name(view), RibbonOut, top + i * pitch, w, h);
                Image img = PaperArt.Draw(rt, PaperPiece.ChatRibbon, PaperChatStyle.Fill(view), PaperChatStyle.Edge(view), 7f);
                if (img.sprite != PaperArt.Round()) img.color = PaperChatStyle.Fill(view); // the cut ribbon is white: tinted per channel
                UiWinKit.MakeButton(img, () => SelectView(view));
                bool gold = view == ChatChannelFilter.All; // dark ink on the gold ribbon, light outlined text on the others
                Text label = PaperTheme.Caption(rt, PaperChatStyle.Name(view), 11, gold ? PaperTheme.Ink : PaperTheme.TextLight, gold ? PaperInk.Plain : PaperInk.Outlined);
                label.rectTransform.offsetMax = new Vector2(-RibbonTuck, 0f);
                Sprite icon = PaperArt.Get(PaperChatStyle.IconPiece(view));
                if (icon == null) continue;
                UiKit.Icon(rt, "Icon", 3f, (h - 16f) * 0.5f, 16f).sprite = icon;
                label.rectTransform.offsetMin = new Vector2(18f, 0f);
            }
        }

        void ShowRibbons()
        {
            for (int i = 0; i < ribbons.Length; i++)
                ribbons[i].anchoredPosition = new Vector2(i - 1 == filter.View ? 0f : RibbonOut, ribbons[i].anchoredPosition.y);
        }

        void BuildChips()
        {
            chipRow = UiKit.Rect(inner, "Chips", 0f, 0f, InnerW, ChipH);
            float[] weights = { 5f, 7f, 6f, 7f, 9f }; // room each caption needs: All, World, Map, Party, Whisper
            float total = 0f, gap = 3f, x = 0f;
            foreach (float k in weights) total += k;
            float unit = (InnerW - gap * (weights.Length - 1)) / total;
            for (int i = 0; i < chips.Length; i++)
            {
                int view = i - 1;
                float w = weights[i] * unit;
                chips[i] = new PaperTab(chipRow, "Chip" + PaperChatStyle.Name(view), x, 0f, w, ChipH, PaperChatStyle.Name(view), PaperTabStyle.Chip, () => ChipTapped(view));
                chips[i].Label.fontSize = 11;
                x += w + gap;
            }
        }

        void ChipTapped(int view)
        {
            if (view == ChatChannelFilter.All) filter.EnableAll();
            else filter.Toggle((ChatChannel)view);
            ShowChips();
            changed();
        }

        void ShowChips()
        {
            bool all = filter.View == ChatChannelFilter.All;
            if (chipRow.gameObject.activeSelf != all) chipRow.gameObject.SetActive(all);
            chips[0].SetActive(filter.AllOn);
            for (int c = 0; c < ChatChannelFilter.Channels; c++) chips[c + 1].SetActive(filter.IsOn((ChatChannel)c));
        }
    }
}

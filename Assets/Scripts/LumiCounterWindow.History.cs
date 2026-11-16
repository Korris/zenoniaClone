using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// LumiCounterWindow: the Summon History board (bottom right). The account keeps its last 200 pulls; the board shows them newest first,
    /// five a page: when, the rarity label, the pet, NEW or the shards a duplicate / the shard result gave, and the two guarantee counters
    /// right after that pull (what the old History tab listed). Newer / Older turn the pages.
    /// </summary>
    public sealed partial class LumiCounterWindow
    {
        const int HistoryRows = 5;
        const float HistoryX = 704f, HistoryY = 326f, HistoryW = 426f, HistoryH = 264f, HistoryRow = 33f;

        sealed class HistoryLine { public Text When, Name, Gain, Pity; public Image Rarity, Icon; }

        readonly HistoryLine[] historyLines = new HistoryLine[HistoryRows];
        Text historyTotal, historyPageText;
        PaperButton historyNewer, historyOlder;
        int historyPage;

        void BuildHistory()
        {
            RectTransform panel = UiKit.Rect(counter, "History", HistoryX, HistoryY, HistoryW, HistoryH);
            PaperArt.Draw(panel, PaperPiece.GachaPanel, PaperTheme.Paper, PaperTheme.Wood, 10f, true);
            TitleTag(panel, "Summon History", 22f, 176f);
            historyTotal = PaperTheme.Body(panel, "Total", 204f, 16f, HistoryW - 204f - 22f, 18f, "", PaperTheme.BarSize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleRight, true);
            for (int i = 0; i < HistoryRows; i++)
            {
                float y = 38f + i * HistoryRow;
                var l = new HistoryLine();
                l.When = PaperTheme.Body(panel, "When" + i, 20f, y, 76f, HistoryRow, "", PaperTheme.BarSize, PaperTheme.InkSoft);
                l.Rarity = UiKit.Icon(panel, "Rarity" + i, 96f, y + 3f, 40f);
                l.Icon = UiKit.Icon(panel, "Icon" + i, 140f, y + 4f, 25f);
                l.Name = PaperTheme.Body(panel, "Name" + i, 170f, y, 110f, HistoryRow, "", PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
                l.Gain = PaperTheme.Body(panel, "Gain" + i, 270f, y, 76f, HistoryRow, "", PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleRight, true);
                l.Pity = PaperTheme.Body(panel, "Pity" + i, 348f, y, 58f, HistoryRow, "", PaperTheme.BarSize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleRight);
                l.Name.horizontalOverflow = HorizontalWrapMode.Overflow;
                historyLines[i] = l;
            }
            float by = 38f + HistoryRows * HistoryRow + 6f;
            historyNewer = new PaperButton(panel, "Newer", 66f, by, 96f, 30f, "◀ Newer", PaperTone.Gold, () => historyPage = Math.Max(0, historyPage - 1));
            historyPageText = PaperTheme.Title(panel, "Page", 162f, by, 102f, 30f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            historyOlder = new PaperButton(panel, "Older", 264f, by, 96f, 30f, "Older ▶", PaperTone.Gold, () => historyPage++);
        }

        void RefreshHistory(AccountState a)
        {
            int total = a.history.Count, pages = Math.Max(1, (total + HistoryRows - 1) / HistoryRows);
            historyPage = Mathf.Clamp(historyPage, 0, pages - 1);
            historyTotal.text = "Total " + Amount(a.totalPulls) + " pulls  (" + PetRules.HistoryMax + " Max)";
            historyPageText.text = (historyPage + 1) + " / " + pages;
            historyNewer.SetEnabled(historyPage > 0);
            historyOlder.SetEnabled(historyPage < pages - 1);
            for (int i = 0; i < HistoryRows; i++)
            {
                HistoryLine l = historyLines[i];
                int idx = total - 1 - historyPage * HistoryRows - i; // newest first
                bool has = idx >= 0;
                l.Rarity.enabled = l.Icon.enabled = false;
                l.When.text = has ? PetGachaText.When(a.history[idx].at) : "";
                l.Name.text = has ? "" : i == 0 && total == 0 ? "No pulls yet." : "";
                l.Gain.text = l.Pity.text = "";
                if (!has) continue;
                PullRecord r = a.history[idx];
                PetDef d = PetConfig.Get(r.id);
                ShowRarity(l.Rarity, d);
                l.Icon.sprite = d != null ? PetArt.Icon(d) : PaperArt.Get(PaperPiece.IconShard);
                l.Icon.color = d != null ? PetArt.TintOf(d) : Color.white;
                l.Icon.enabled = l.Icon.sprite != null;
                l.Name.text = d != null ? d.Name : "Shared Shards";
                l.Gain.text = d != null && !r.dup ? "<color=#188231>NEW</color>" : "+" + r.shards + " shards";
                l.Pity.text = "pity " + r.pitySSR + "/" + r.pityUR;
            }
            RefreshFlags();
        }
    }
}

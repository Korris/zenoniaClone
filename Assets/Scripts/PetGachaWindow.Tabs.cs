using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>PetGachaWindow: the published rate table (text: PetGachaText) and the pull history (last 200, newest first, 16 a page).</summary>
    public sealed partial class PetGachaWindow
    {
        const int HistoryRows = 16;
        readonly Text[] historyLines = new Text[HistoryRows];
        Text historyPageText;
        int historyPage;

        void BuildRates()
        {
            UiWinKit.Sliced(ratesTab, "section", 0.3f);
            UiKit.Label(ratesTab, "Table", 14f, 8f, W - 90f, H - BodyY - 40f, PetGachaText.RateTable(), 13, UiWinKit.Cream, TextAnchor.UpperLeft);
        }

        void BuildHistory()
        {
            UiWinKit.Sliced(historyTab, "section", 0.3f);
            for (int i = 0; i < HistoryRows; i++) historyLines[i] = UiKit.Label(historyTab, "Line" + i, 14f, 8f + i * 19f, W - 90f, 19f, "", 12, UiWinKit.Cream);
            UiWinKit.ArtButton(historyTab, "Newer", "btn-navy", 14f, H - BodyY - 60f, 90f, 30f, "Newer", 13, Color.white, () => { historyPage = Math.Max(0, historyPage - 1); shownKey = -1; });
            UiWinKit.ArtButton(historyTab, "Older", "btn-navy", 112f, H - BodyY - 60f, 90f, 30f, "Older", 13, Color.white, () => { historyPage++; shownKey = -1; });
            historyPageText = UiKit.Label(historyTab, "Page", 214f, H - BodyY - 60f, 300f, 30f, "", 12, UiWinKit.Dim);
        }

        void FillHistory(AccountState a)
        {
            int total = a.history.Count, pages = Math.Max(1, (total + HistoryRows - 1) / HistoryRows);
            historyPage = Math.Min(historyPage, pages - 1);
            historyPageText.text = "Page " + (historyPage + 1) + " / " + pages + "   (last " + PetRules.HistoryMax + " pulls are kept)";
            for (int i = 0; i < HistoryRows; i++)
            {
                int idx = total - 1 - historyPage * HistoryRows - i; // newest first
                if (idx < 0) { historyLines[i].text = i == 0 && total == 0 ? "No pulls yet." : ""; continue; }
                PullRecord r = a.history[idx];
                PetDef d = PetConfig.Get(r.id);
                string when = PetGachaText.When(r.at);
                string what = d == null ? "<color=#bfe6ff>+" + r.shards + " shared shards</color>" : PetArt.Label(d) + " " + d.Name + "   " + (r.dup ? "+" + r.shards + " shards" : "<color=#9fff9f>new</color>");
                historyLines[i].text = when + "   " + what + "   <color=#8a96a4>pity " + r.pitySSR + "/" + r.pityUR + "</color>";
            }
        }
    }
}

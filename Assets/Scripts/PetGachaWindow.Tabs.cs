using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>PetGachaWindow: the published rate table and the pull history (last 200, newest first, 16 a page).</summary>
    public sealed partial class PetGachaWindow
    {
        const int HistoryRows = 16;
        readonly Text[] historyLines = new Text[HistoryRows];
        Text historyPageText;
        int historyPage;

        void BuildRates()
        {
            UiWinKit.Sliced(ratesTab, "section", 0.3f);
            UiKit.Label(ratesTab, "Table", 14f, 8f, W - 90f, H - BodyY - 40f, RateTable(), 13, UiWinKit.Cream, TextAnchor.UpperLeft);
        }

        /// <summary>Every number of the gacha, from the same constants the server rolls with (PetRules), so the table can never drift from the rules.</summary>
        static string RateTable()
        {
            var b = new StringBuilder();
            b.Append("<b>Rates per pull</b> (pets of one rarity share it evenly; 1 Gacha Coin a pull, ").Append(PetRules.CrystalsPerPull).Append(" Pet Crystals buy a coin)\n");
            for (int r = PetGacha.Rates.Length - 1; r >= 0; r--)
            {
                var rarity = (PetRarity)r;
                b.Append("<color=").Append(PetArt.RarityHex(rarity)).Append(">").Append(rarity).Append("  ").Append(Pct(PetGacha.Rates[r])).Append("</color>   ");
                bool first = true;
                foreach (PetDef d in PetConfig.All)
                {
                    if (d.Legacy || d.Rarity != rarity) continue;
                    b.Append(first ? "" : ", ").Append(d.Name).Append(" ").Append(Pct(PetGacha.PetRate(d)));
                    first = false;
                }
                b.Append('\n');
            }
            b.Append("<color=#bfe6ff>Shards  ").Append(Pct(PetRules.GachaShardRate)).Append("</color>   ").Append(PetRules.SharedShardsPerPull).Append(" shared shards (Growth tab turns them into any owned pet's shards)\n");
            b.Append("\n<b>Guarantees</b> (count per account, never expire)\n");
            double ur = PetGacha.Rates[4] / (PetGacha.Rates[3] + PetGacha.Rates[4]);
            b.Append(PetRules.SsrPity).Append("th pull without SSR or better: SSR+ (SSR ").Append(Pct(1 - ur)).Append(", UR ").Append(Pct(ur)).Append("); an SSR+ resets this counter.\n");
            b.Append(PetRules.UrPity).Append("th pull without UR: UR; a UR resets both counters.  x10 = ten single pulls, same rates.\n");
            b.Append("\n<b>Duplicates and stars</b>\n");
            b.Append("A pet you own: +").Append(PetRules.DuplicateShards).Append(" shards of it. Stars: ").Append(PetRules.StarShards[0]).Append(" shards + ").Append(PetRules.StarGold[0]).Append(" G, then ")
             .Append(PetRules.StarShards[1]).Append(" + ").Append(PetRules.StarGold[1]).Append(", ").Append(PetRules.StarShards[2]).Append(" + ").Append(PetRules.StarGold[2]).Append(", ").Append(PetRules.StarShards[3]).Append(" + ").Append(PetRules.StarGold[3])
             .Append(" G. Each star: passive x1.1 more (5 stars = x1.4).\n");
            b.Append("Every pull that is not UR: +1 UR shard; ").Append(PetRules.UrExchangeCost).Append(" UR shards = Frostfang King.\n");
            b.Append("Legacy pets (not in the gacha): Spirit at level 5, Imp for 3000 G at a merchant.");
            return b.ToString();
        }

        static string Pct(double v) { return (v * 100.0).ToString(v * 100.0 % 1.0 == 0 ? "0" : "0.###", CultureInfo.InvariantCulture) + "%"; }

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
                string when = DateTimeOffset.FromUnixTimeMilliseconds(r.at).ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);
                string what = d == null ? "<color=#bfe6ff>+" + r.shards + " shared shards</color>" : PetArt.Label(d) + " " + d.Name + "   " + (r.dup ? "+" + r.shards + " shards" : "<color=#9fff9f>new</color>");
                historyLines[i].text = when + "   " + what + "   <color=#8a96a4>pity " + r.pitySSR + "/" + r.pityUR + "</color>";
            }
        }
    }
}

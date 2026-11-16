using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// LumiCounterWindow: the Rates board (top right) and the two flags beside it. One row per rarity, UR first: its painted label, the
    /// rate of the rarity, then every pet of it with its own rate; a last row for the shard result. All numbers are read from PetGacha /
    /// PetRules (the constants the server rolls with), nothing is typed here. The rules never change while the game runs, so the board is
    /// filled once. Flags: Rates opens the rules paper (guarantees, duplicates, stars: the rest of the old Rates tab), History turns the
    /// history back to its newest page; the gold flag is the one tapped last.
    /// </summary>
    public sealed partial class LumiCounterWindow
    {
        const float RatesX = 704f, RatesY = 62f, RatesW = 338f, RatesH = 258f, RateRow = 35f;
        Image flagRates, flagHistory;
        Text flagRatesText, flagHistoryText;
        bool historyFlag;

        void BuildRates()
        {
            RectTransform panel = UiKit.Rect(counter, "Rates", RatesX, RatesY, RatesW, RatesH);
            PaperArt.Draw(panel, PaperPiece.GachaPanel, PaperTheme.Paper, PaperTheme.Wood, 10f, true);
            TitleTag(panel, "Rates", (RatesW - 130f) * 0.5f, 130f);
            float y = 34f;
            for (int r = PetGacha.Rates.Length - 1; r >= 0; r--, y += RateRow)
            {
                var rarity = (PetRarity)r;
                int n = PetGacha.CountOf(rarity);
                ShowRarity(UiKit.Icon(panel, "Label" + rarity, 20f, y + 2f, 44f), PetGacha.NthOf(rarity, 0));
                RateText(panel, y, PetGachaText.Pct(PetGacha.Rates[r]));
                string names = "";
                for (int i = 0; i < n; i++)
                {
                    PetDef d = PetGacha.NthOf(rarity, i);
                    if (i < 2) PetIcon(panel, 128f + i * 25f, y + 5f, 24f, d);
                    names += (i > 0 ? "\n" : "") + d.Name + (n > 1 ? "   <color=#7A5A45>" + PetGachaText.Pct(PetGacha.PetRate(d)) + "</color>" : "");
                }
                RowText(panel, y, names);
            }
            PaperKit.RibbonLabel(panel, "LabelShards", 16f, y + 6f, 52f, 22f, "Shards", PaperLabel.Purple);
            RateText(panel, y, PetGachaText.Pct(PetRules.GachaShardRate));
            Image shard = UiKit.Icon(panel, "Shard", 140f, y + 5f, 24f);
            shard.sprite = PaperArt.Get(PaperPiece.IconShard);
            shard.enabled = shard.sprite != null;
            RowText(panel, y, PetRules.SharedShardsPerPull + " Shared Shards");

            flagRates = Flag("FlagRates", 92f, "Rates", out flagRatesText, () => { historyFlag = false; OpenDialog(Dialog.Rules, 0); });
            flagHistory = Flag("FlagHistory", 176f, "History", out flagHistoryText, () => { historyFlag = true; historyPage = 0; });
        }

        static void RateText(Transform panel, float y, string pct)
        {
            PaperTheme.Title(panel, "Rate", 64f, y, 62f, RateRow, pct, PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
        }

        static void RowText(Transform panel, float y, string text)
        {
            Text t = PaperTheme.Body(panel, "Pets", 182f, y, RatesW - 182f - 16f, RateRow, text, PaperTheme.BarSize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            t.lineSpacing = 0.85f;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        /// <summary>A pet's picture in its own tint (six pets share a tinted stand-in until theirs is drawn, PetArt).</summary>
        static Image PetIcon(Transform parent, float x, float y, float size, PetDef d)
        {
            Image img = UiKit.Icon(parent, "Pet", x, y, size);
            img.sprite = PetArt.Icon(d);
            img.color = PetArt.TintOf(d);
            img.enabled = img.sprite != null;
            return img;
        }

        /// <summary>The cream name tag on a board's top rim.</summary>
        static void TitleTag(RectTransform panel, string title, float x, float w)
        {
            RectTransform tag = UiKit.Rect(panel, "Title", x, -8f, w, 34f);
            PaperArt.Draw(tag, PaperPiece.GachaTitleTag, PaperTheme.Cream, PaperTheme.BtnGoldEdge, 8f);
            PaperTheme.Caption(tag, title, PaperTheme.SectionSize, PaperTheme.Ink, PaperInk.Plain);
        }

        Image Flag(string name, float y, string text, out Text label, System.Action onClick)
        {
            RectTransform rt = UiKit.Rect(counter, name, RatesX + RatesW - 2f, y, 73f, 80f);
            Image img = PaperArt.Draw(rt, null, Color.white, Color.clear);
            img.preserveAspect = true;
            label = PaperTheme.Title(rt, "Text", 17f, 21f, 46f, 30f, text, PaperTheme.BarSize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter); // on the cloth, right of the pole
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiWinKit.MakeButton(img, onClick);
            return img;
        }

        void RefreshFlags()
        {
            PaperArt.Redraw(flagRates, historyFlag ? PaperPiece.GachaFlagWood : PaperPiece.GachaFlagGold, historyFlag ? PaperTheme.Wood : PaperTheme.BtnGold, PaperTheme.WoodDark);
            PaperArt.Redraw(flagHistory, historyFlag ? PaperPiece.GachaFlagGold : PaperPiece.GachaFlagWood, historyFlag ? PaperTheme.BtnGold : PaperTheme.Wood, PaperTheme.WoodDark);
            flagRatesText.color = historyFlag ? PaperTheme.TextLight : PaperTheme.Ink; // dark on the gold cloth, light on the brown one
            flagHistoryText.color = historyFlag ? PaperTheme.Ink : PaperTheme.TextLight;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// RoGachaWindow, right column: tabs Drop rates / Pull history over one table box, then "Pets in this Gacha" (every gacha pet with its
    /// rarity; a tap previews it on the pedestal). Drop rates = PetGacha.Rates per rarity plus the shared-shard result (PetRules.GachaShardRate),
    /// so the six rows add up to 100 %. Pull history = the account's last pulls, newest first, 5 a page.
    /// </summary>
    public sealed partial class RoGachaWindow
    {
        enum Tab { Rates, History }
        const int HistoryRows = 5;
        static readonly string[] RarityNames = { "Common", "Rare", "Super Rare", "Ultra Rare", "Legendary" };

        Tab tab;
        int historyPage;
        InventoryRoBoxes.PillBox tabRates, tabHistory;
        GameObject ratesBox, historyBox;
        readonly HistoryLine[] historyLines = new HistoryLine[HistoryRows];
        Text historyEmpty, historyPageText;

        sealed class HistoryLine
        {
            public GameObject Root;
            public Text When, Name, What;
            public Image Icon;
            public InventoryRoBoxes.PillBox Pill;
        }

        void BuildRight()
        {
            Image panelImg = W.Img(window, "RightPanel", X(1116f), Y(112f), D(514f), D(720f), "bag-section");
            panelImg.color = new Color(1f, 1f, 1f, 0.75f);
            RectTransform panel = panelImg.rectTransform;
            float px = X(1116f), py = Y(112f);
            Func<float, float> lx = mx => X(mx) - px, ly = my => Y(my) - py;

            tabRates = TabPill(panel, "TabRates", lx(1134f), ly(140f), D(230f), "Drop rates", Tab.Rates);
            tabHistory = TabPill(panel, "TabHistory", lx(1378f), ly(140f), D(232f), "Pull history", Tab.History);

            RectTransform box = W.Img(panel, "TableBox", lx(1136f), ly(218f), D(476f), D(266f), "slot").rectTransform;
            box.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.6f);
            float rowH = (D(266f) - 12f) / 6f;
            ratesBox = UiKit.Rect(box, "Rates", 0f, 0f, D(476f), D(266f)).gameObject;
            for (int r = PetGacha.Rates.Length; r >= 0; r--) // N first ... UR, then the shard result
            {
                int i = r == PetGacha.Rates.Length ? 5 : r;
                float y = 6f + i * rowH;
                Image cap = W.Img(ratesBox.transform, "Row" + i, 6f, y + 2f, D(476f) - 12f, rowH - 4f, "bag-pill-white");
                cap.pixelsPerUnitMultiplier = cap.sprite != null ? cap.sprite.rect.height / (2f * (rowH - 4f)) : 1f;
                cap.color = new Color(0.9f, 0.93f, 1f, 0.55f);
                if (i < 5)
                {
                    var rarity = (PetRarity)i;
                    RarityPill(ratesBox.transform, "Pill" + i, 14f, y + (rowH - 22f) * 0.5f, 22f, rarity, 14, 48f);
                    W.Txt(ratesBox.transform, "Name" + i, 82f, y, 160f, rowH, RarityNames[i], 15, W.Ink);
                    W.Txt(ratesBox.transform, "Pct" + i, 200f, y, D(476f) - 214f, rowH, Pct(PetGacha.Rates[i]), 16, W.Ink, TextAnchor.MiddleRight);
                }
                else
                {
                    Image ic = W.Img(ratesBox.transform, "ShardIcon", 26f, y + (rowH - 24f) * 0.5f, 24f, 24f, "gacha-ico-shard");
                    ic.color = RoSoftTheme.Hex("#4F94D6");
                    W.Txt(ratesBox.transform, "Name5", 82f, y, 200f, rowH, PetRules.SharedShardsPerPull + " shared shards", 15, W.Ink);
                    W.Txt(ratesBox.transform, "Pct5", 200f, y, D(476f) - 214f, rowH, Pct(PetRules.GachaShardRate), 16, W.Ink, TextAnchor.MiddleRight);
                }
            }

            historyBox = UiKit.Rect(box, "History", 0f, 0f, D(476f), D(266f)).gameObject;
            for (int i = 0; i < HistoryRows; i++)
            {
                float y = 6f + i * rowH;
                var l = new HistoryLine { Root = UiKit.Rect(historyBox.transform, "Row" + i, 0f, y, D(476f), rowH).gameObject };
                if (i > 0) W.Img(l.Root.transform, "Line", 10f, 0f, D(476f) - 20f, 1f, null).color = new Color(0.8f, 0.85f, 0.92f, 0.8f);
                l.When = W.Txt(l.Root.transform, "When", 10f, 0f, 70f, rowH, "", 11, W.InkSoft, TextAnchor.MiddleLeft, false);
                l.Pill = RarityPill(l.Root.transform, "Pill", 82f, (rowH - 20f) * 0.5f, 20f, PetRarity.N, 12, 40f);
                l.Icon = W.Img(l.Root.transform, "Shard", 92f, (rowH - 20f) * 0.5f, 20f, 20f, "gacha-ico-shard");
                l.Icon.color = RoSoftTheme.Hex("#4F94D6");
                l.Name = W.Txt(l.Root.transform, "Name", 132f, 0f, 120f, rowH, "", 13, W.Ink);
                l.What = W.Txt(l.Root.transform, "What", 200f, 0f, D(476f) - 210f, rowH, "", 12, W.Ink, TextAnchor.MiddleRight);
                historyLines[i] = l;
            }
            historyEmpty = W.Txt(historyBox.transform, "Empty", 0f, 0f, D(476f), D(266f) - rowH, "No pulls yet.", 15, W.InkSoft, TextAnchor.MiddleCenter);
            float pagerY = 6f + HistoryRows * rowH;
            W.Clickable(W.Img(historyBox.transform, "Newer", 14f, pagerY + (rowH - 26f) * 0.5f, 22f, 26f, "gacha-arrow-left", true), () => { historyPage = Math.Max(0, historyPage - 1); Refresh(); });
            W.Clickable(W.Img(historyBox.transform, "Older", D(476f) - 36f, pagerY + (rowH - 26f) * 0.5f, 22f, 26f, "gacha-arrow-right", true), () => { historyPage++; Refresh(); });
            historyPageText = W.Txt(historyBox.transform, "Page", 40f, pagerY, D(476f) - 80f, rowH, "", 12, W.InkSoft, TextAnchor.MiddleCenter, false);

            Image paw = W.Img(panel, "PetsIcon", lx(1142f), ly(520f), D(30f), D(30f), "gacha-ico-paw");
            paw.color = RoSoftTheme.Hex("#4F94D6");
            W.Txt(panel, "PetsTitle", lx(1180f), ly(516f), 260f, D(38f), "Pets in this Gacha", 17, W.Ink);
            var pets = Pets();
            for (int i = 0; i < pets.Count && i < 10; i++)
            {
                PetDef d = pets[i];
                int inRow = i / 5 < pets.Count / 5 ? 5 : pets.Count % 5;
                float rowX = (5 - inRow) * 97f * 0.5f; // a short last row is centred
                var cell = new W.Cell(panel, "Pet" + i, lx(1135f + rowX + (i % 5) * 97f), ly(570f + (i / 5) * 124f), D(94f), () => PreviewPet(d));
                PetCard(cell, d.Rarity, false);
                cell.SetIcon(PetArt.Icon(d), Color.white, 0.9f);
                RarityPill(cell.Root, "Rarity", -2f, -3f, 15f, d.Rarity, 10, 22f);
            }
        }

        InventoryRoBoxes.PillBox TabPill(Transform parent, string name, float x, float y, float w, string text, Tab t)
        {
            float h = D(56f);
            Image face = W.Img(parent, name, x, y, w, h, "bag-pill-white", true);
            face.pixelsPerUnitMultiplier = face.sprite != null ? face.sprite.rect.height / (2f * h) : 1f;
            Text label = W.Txt(face.transform, "Label", 0f, -1f, w, h, text, 17, W.Ink, TextAnchor.MiddleCenter);
            W.Clickable(face, () => { tab = t; historyPage = 0; Refresh(); });
            return new InventoryRoBoxes.PillBox { Root = face.rectTransform, Skin = face, Label = label };
        }

        static void SetTab(InventoryRoBoxes.PillBox p, bool on)
        {
            W.SetArt(p.Skin, on ? "btn-sky" : "bag-pill-white");
            if (p.Skin.sprite != null) p.Skin.pixelsPerUnitMultiplier = p.Skin.sprite.rect.height / (2f * p.Root.sizeDelta.y);
            p.Label.color = on ? Color.white : W.Ink;
            Shadow s = p.Label.GetComponent<Shadow>() ?? p.Label.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0.1f, 0.25f, 0.45f, 0.35f);
            s.enabled = on;
        }

        static string Pct(double v) { return (v * 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "%"; }

        void RefreshRight(AccountState a)
        {
            SetTab(tabRates, tab == Tab.Rates);
            SetTab(tabHistory, tab == Tab.History);
            ratesBox.SetActive(tab == Tab.Rates);
            historyBox.SetActive(tab == Tab.History);
            if (tab != Tab.History) return;
            int total = a.history.Count, pages = Math.Max(1, (total + HistoryRows - 1) / HistoryRows);
            historyPage = Math.Min(historyPage, pages - 1);
            historyEmpty.gameObject.SetActive(total == 0);
            historyPageText.text = total == 0 ? "" : "Page " + (historyPage + 1) + " / " + pages + "  ·  " + a.totalPulls + " pulls";
            for (int i = 0; i < HistoryRows; i++)
            {
                HistoryLine l = historyLines[i];
                int idx = total - 1 - historyPage * HistoryRows - i; // newest first
                l.Root.SetActive(idx >= 0);
                if (idx < 0) continue;
                PullRecord r = a.history[idx];
                PetDef d = PetConfig.Get(r.id);
                l.When.text = PetGachaText.When(r.at);
                l.Pill.Root.gameObject.SetActive(d != null);
                l.Icon.enabled = d == null;
                if (d != null) SetRarity(l.Pill, d.Rarity);
                l.Name.text = d != null ? d.Name : "Shared shards";
                l.What.text = d == null ? "+" + r.shards : r.dup ? "+" + r.shards + " shards" : "<color=#2E9E57>NEW</color>";
            }
        }
    }
}

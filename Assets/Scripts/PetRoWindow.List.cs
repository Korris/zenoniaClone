using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using PW = ZenoniaProto.PetRoWidgets;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// PetRoWindow, pet list: the species the account owns (n / all species), filter (All / In team / Not in team), rarity (All, N .. UR, Legacy),
    /// sort by rarity (arrow flips the order), 5 x 2 cards a page with arrows and page dots. A card selects its pet for the right-hand card.
    /// The two dropdowns share one small popup list (BuildPopups).
    /// </summary>
    public sealed partial class PetRoWindow
    {
        const int ListCols = 5, ListRows = 2, ListPerPage = ListCols * ListRows;
        const float CardSize = 86f, CardStep = 98.5f, CardX0 = 34f, CardY0 = 42f, CardRowStep = 96f;
        static readonly string[] OwnFilters = { "All", "In team", "Not in team" };
        static readonly string[] RarityFilters = { "Rarity", "N", "R", "SR", "SSR", "UR", "Legacy" };

        readonly PW.Card[] cards = new PW.Card[ListPerPage];
        readonly List<int> listed = new List<int>();
        readonly Image[] dots = new Image[4];
        Text listTitle, ownFilterText, rarityFilterText;
        Image sortArrow;
        GameObject prevArrow, nextArrow;
        int ownFilter, rarityFilter, listPageIndex;
        bool sortAscending;

        // popup list under a dropdown
        RectTransform popup;
        readonly List<GameObject> popupRows = new List<GameObject>();

        void BuildList(RectTransform box)
        {
            W.Img(box, "TitleIcon", 14f, 10f, 22f, 22f, "pet-el-ice");
            listTitle = W.Txt(box, "Title", 42f, 4f, 220f, 34f, "", 18, W.Ink);
            ownFilterText = Dropdown(box, "OwnFilter", 286f, 104f, () => OpenDropdown(box, 286f, 104f, OwnFilters, i => { ownFilter = i; listPageIndex = 0; Refresh(); }));
            rarityFilterText = Dropdown(box, "RarityFilter", 398f, 104f, () => OpenDropdown(box, 398f, 104f, RarityFilters, i => { rarityFilter = i; listPageIndex = 0; Refresh(); }));
            Image sort = W.Img(box, "Sort", 508f, 6f, 30f, 30f, "bag-pill-white", true);
            sort.pixelsPerUnitMultiplier = 104f / 60f;
            sort.color = SoftBox;
            sortArrow = W.Img(sort.transform, "Arrow", 7f, 7f, 16f, 16f, "hud-chevron-up");
            sortArrow.color = W.Ink;
            Centre(sortArrow.rectTransform);
            W.Clickable(sort, () => { sortAscending = !sortAscending; Refresh(); });
            for (int i = 0; i < cards.Length; i++)
            {
                int c = i;
                cards[i] = new PW.Card(box, "Card" + i, CardX0 + (i % ListCols) * CardStep, CardY0 + (i / ListCols) * CardRowStep, CardSize, () => OnCard(c));
            }
            prevArrow = W.Clickable(W.Img(box, "Prev", 6f, 112f, 22f, 32f, "pet-chevron-left", true), () => TurnList(-1)).gameObject;
            nextArrow = W.Clickable(W.Img(box, "Next", CentreW - 28f, 112f, 22f, 32f, "pet-chevron-right", true), () => TurnList(1)).gameObject;
            for (int i = 0; i < dots.Length; i++) dots[i] = W.Img(box, "Dot" + i, 0f, box.sizeDelta.y - 16f, 9f, 9f, "pet-dot");
        }

        /// <summary>A white dropdown box: label left, the kit's arrow on the right.</summary>
        Text Dropdown(RectTransform box, string name, float x, float w, UnityEngine.Events.UnityAction onClick)
        {
            Image d = W.Img(box, name, x, 6f, w, 30f, "bag-pill-white", true);
            d.pixelsPerUnitMultiplier = 104f / 56f;
            d.color = SoftBox;
            Text t = W.Txt(d.transform, "Label", 12f, 0f, w - 36f, 29f, "", 14, W.Ink);
            Image arrow = W.Img(d.transform, "Arrow", w - 26f, 8f, 14f, 14f, "hud-chevron-up");
            arrow.color = W.Ink;
            Centre(arrow.rectTransform);
            arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            W.Clickable(d, onClick);
            return t;
        }

        /// <summary>Pivot to the middle (same place on screen) so a 180 degree turn flips the arrow in place.</summary>
        static void Centre(RectTransform rt)
        {
            Vector2 size = rt.sizeDelta;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition += new Vector2(size.x * 0.5f, -size.y * 0.5f);
        }

        void BuildPopups()
        {
            popup = W.Img(window, "Dropdown", 0f, 0f, 104f, 10f, "bag-pill-white", true).rectTransform;
            popup.GetComponent<Image>().pixelsPerUnitMultiplier = 104f / 24f;
            popup.gameObject.SetActive(false);
        }

        void OpenDropdown(RectTransform box, float x, float w, string[] options, System.Action<int> pick)
        {
            if (popup.gameObject.activeSelf) { CloseDropdown(); return; }
            foreach (GameObject g in popupRows) Destroy(g);
            popupRows.Clear();
            const float rowH = 28f;
            popup.SetAsLastSibling();
            popup.anchoredPosition = new Vector2(CentreX + x, -(PanelY + 300f + 38f));
            popup.sizeDelta = new Vector2(w, options.Length * rowH + 8f);
            for (int i = 0; i < options.Length; i++)
            {
                int k = i;
                Image row = W.Img(popup, "Opt" + i, 4f, 4f + i * rowH, w - 8f, rowH, "bag-pill-mint", true);
                row.color = new Color(1f, 1f, 1f, 0f); // invisible until hovered: the text is the option
                W.Txt(row.transform, "Text", 10f, 0f, w - 28f, rowH, i == 0 && options == RarityFilters ? "All rarities" : options[i], 14, W.Ink);
                Button b = W.Clickable(row, () => { CloseDropdown(); pick(k); });
                ColorBlock cb = b.colors; cb.highlightedColor = new Color(1f, 1f, 1f, 0.6f); b.colors = cb;
                popupRows.Add(row.gameObject);
            }
            popup.gameObject.SetActive(true);
        }

        void CloseDropdown() { if (popup != null) popup.gameObject.SetActive(false); }

        void RefreshList(PlayerState p, AccountState a)
        {
            listed.Clear();
            for (int i = 0; i < PetConfig.All.Length; i++)
            {
                PetDef d = PetConfig.All[i];
                if (a == null || !a.OwnsPet(d.Id)) continue;
                bool inTeam = PetTeam.Has(p.Team, d.Id);
                if (ownFilter == 1 && !inTeam || ownFilter == 2 && inTeam) continue;
                if (rarityFilter > 0 && PetConfig.RarityLabel(d) != RarityFilters[rarityFilter]) continue;
                listed.Add(i);
            }
            listed.Sort((x, y) =>
            {
                int rx = RankOf(PetConfig.All[x]), ry = RankOf(PetConfig.All[y]);
                int c = sortAscending ? rx.CompareTo(ry) : ry.CompareTo(rx);
                if (c == 0) c = a.LevelOf(PetConfig.All[y].Id).CompareTo(a.LevelOf(PetConfig.All[x].Id));
                return c != 0 ? c : x.CompareTo(y);
            });
            listTitle.text = "Pet List (" + (a != null ? a.Species : 0) + "/" + PetConfig.All.Length + ")";
            ownFilterText.text = OwnFilters[ownFilter];
            rarityFilterText.text = RarityFilters[rarityFilter];
            sortArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, sortAscending ? 0f : 180f); // up = rarest last
            int pagesN = Mathf.Max(1, (listed.Count + ListPerPage - 1) / ListPerPage);
            listPageIndex = Mathf.Clamp(listPageIndex, 0, pagesN - 1);
            for (int i = 0; i < cards.Length; i++)
            {
                int at = listPageIndex * ListPerPage + i;
                PetDef d = at < listed.Count ? PetConfig.All[listed[at]] : null;
                cards[i].Set(d, d != null ? a.Pet(d.Id) : null, d != null && listed[at] == selected);
            }
            prevArrow.SetActive(pagesN > 1);
            nextArrow.SetActive(pagesN > 1);
            float dotsW = pagesN * 9f + (pagesN - 1) * 8f, x0 = CentreW * 0.5f - dotsW * 0.5f;
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i].gameObject.SetActive(i < pagesN);
                dots[i].rectTransform.anchoredPosition = new Vector2(x0 + i * 17f, dots[i].rectTransform.anchoredPosition.y);
                dots[i].color = i == listPageIndex ? RoSoftTheme.SkyDark : RoSoftTheme.Hex("#C9D6E2");
            }
        }

        /// <summary>Sort key: rarity N 0 .. UR 4, Legacy below N.</summary>
        static int RankOf(PetDef d) { return d.Legacy ? -1 : (int)d.Rarity; }

        void OnCard(int card)
        {
            int at = listPageIndex * ListPerPage + card;
            if (at < listed.Count) Select(listed[at]);
        }

        void TurnList(int by)
        {
            int pagesN = Mathf.Max(1, (listed.Count + ListPerPage - 1) / ListPerPage);
            listPageIndex = (listPageIndex + by + pagesN) % pagesN;
            Refresh();
        }
    }
}

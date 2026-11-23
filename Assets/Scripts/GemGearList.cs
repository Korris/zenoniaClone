using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// Left column of the socketing / removal screens: Equip / Bag tabs and the items that have sockets (weapon 2, armor 2, necklace 1), six per page. Each row is the
    /// menu-button art with the item icon, its name and one dot per socket (empty / filled piece). Picking a row calls onPick; the row stays selected across redraws.
    /// </summary>
    public sealed class GemGearList
    {
        const int PerPage = 5;
        const float TabH = U.ChipH + 4f, RowPitch = U.RowH + 6f, DotSize = 22f, DotGap = 4f;

        sealed class Row
        {
            public RectTransform Root; public Image Skin, Icon; public Text Name; public Image[] Dots = new Image[2]; public Item Item;
        }

        readonly RectTransform root;
        readonly Row[] rows = new Row[PerPage];
        readonly U.Chip equipTab, bagTab;
        readonly Text emptyText, pageText;
        readonly Action<Item> onPick;
        bool worn = true;
        int page;
        string selectedUid;
        List<Item> shown = new List<Item>();

        public Item Selected { get; private set; }
        public bool ShowWorn { get { return worn; } }

        public GemGearList(Transform parent, float x, float y, float w, Action<Item> onPick)
        {
            this.onPick = onPick;
            root = UiKit.Rect(parent, "GearList", x, y, w, TabH + 8f + PerPage * RowPitch + 36f);
            float tabW = (w - 8f) * 0.5f;
            equipTab = new U.Chip(root, "TabEquip", 0f, 0f, tabW, GemUiArt.Icon("menu-socket"), "Equip", () => SetTab(true));
            bagTab = new U.Chip(root, "TabBag", tabW + 8f, 0f, tabW, GemUiArt.Icon("filter-all"), "Bag", () => SetTab(false));
            for (int i = 0; i < PerPage; i++) rows[i] = BuildRow(i, w);
            emptyText = U.Txt(root, "Empty", 8f, TabH + 24f, w - 16f, 60f, "Nothing here has sockets.\nWeapons, armor and necklaces do.", U.TextSmall, U.InkSoft, TextAnchor.UpperCenter);
            float navY = TabH + 8f + PerPage * RowPitch + 4f;
            pageText = U.Txt(root, "Page", w * 0.5f - 40f, navY, 80f, 28f, "1 / 1", U.TextNormal, U.Ink, TextAnchor.MiddleCenter);
            Arrow(w * 0.5f - 90f, navY, "btn-page-left", -1);
            Arrow(w * 0.5f + 52f, navY, "btn-page-right", 1);
        }

        Row BuildRow(int i, float w)
        {
            var r = new Row();
            r.Root = UiKit.Rect(root, "Row" + i, 0f, TabH + 8f + i * RowPitch, w, U.RowH);
            r.Skin = r.Root.gameObject.AddComponent<Image>();
            Row captured = r;
            UiWinKit.MakeButton(r.Skin, () => { if (captured.Item != null) Pick(captured.Item); });
            r.Icon = UiKit.Rect(r.Root, "Icon", U.RowPadL + 4f, (U.RowH - U.RowIcon) * 0.5f, U.RowIcon, U.RowIcon).gameObject.AddComponent<Image>();
            r.Icon.preserveAspect = true; r.Icon.raycastTarget = false;
            float tx = U.RowPadL + 4f + U.RowIcon + U.RowGap;
            r.Name = U.Txt(r.Root, "Name", tx, 4f, w - tx - 10f, 28f, "", U.TextNormal, U.Ink);
            for (int d = 0; d < r.Dots.Length; d++)
            {
                r.Dots[d] = UiKit.Rect(r.Root, "Dot" + d, tx + d * (DotSize + DotGap), 33f, DotSize, DotSize).gameObject.AddComponent<Image>();
                r.Dots[d].raycastTarget = false;
            }
            return r;
        }

        void Arrow(float x, float y, string piece, int dir)
        {
            Image a = GemUiArt.Img(root, "Arrow" + dir, x, y, 38f, 28f, GemUiArt.Frame(piece), true);
            UiWinKit.MakeButton(a, () => { page += dir; Refresh(PetActions.Me); });
        }

        void SetTab(bool wornTab) { worn = wornTab; page = 0; Refresh(PetActions.Me); }

        void Pick(Item item) { selectedUid = item.uid; Selected = item; onPick(item); Refresh(PetActions.Me); }

        /// <summary>Redraws from the player's items. Keeps the selection when the item is still on this tab, else selects the first row.</summary>
        public void Refresh(PlayerState p)
        {
            equipTab.Set(worn); bagTab.Set(!worn);
            shown = GemUiModel.Gear(p, worn);
            int pages = Mathf.Max(1, Mathf.CeilToInt(shown.Count / (float)PerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            pageText.text = (page + 1) + " / " + pages;
            Item keep = shown.Find(i => i.uid == selectedUid);
            if (keep == null) { keep = shown.Count > 0 ? shown[Mathf.Min(page * PerPage, shown.Count - 1)] : null; selectedUid = keep != null ? keep.uid : null; }
            bool changed = keep != Selected;
            Selected = keep;
            emptyText.gameObject.SetActive(shown.Count == 0);
            for (int i = 0; i < PerPage; i++)
            {
                Row r = rows[i];
                int index = page * PerPage + i;
                r.Item = index < shown.Count ? shown[index] : null;
                r.Root.gameObject.SetActive(r.Item != null);
                if (r.Item == null) continue;
                bool on = r.Item == Selected;
                GemUiArt.SetPiece(r.Skin, GemUiArt.Frame(on ? "menu-btn-selected" : "menu-btn"));
                Sprite icon = LootSprites.Icon(r.Item);
                r.Icon.sprite = icon; r.Icon.color = icon != null ? Color.white : Color.clear;
                r.Name.text = EnhanceStats.DisplayName(r.Item);
                int slots = GemSockets.SlotCount(r.Item.slot), filled = GemUiModel.Filled(r.Item);
                for (int d = 0; d < r.Dots.Length; d++)
                {
                    r.Dots[d].gameObject.SetActive(d < slots);
                    if (d < slots) GemUiArt.SetPiece(r.Dots[d], GemUiArt.Socket(d < filled ? "socket-filled" : "socket-empty"));
                }
            }
            if (changed && Selected != null) onPick(Selected);
        }
    }
}

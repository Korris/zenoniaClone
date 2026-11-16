using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Test screen showing every widget of the Adventure Paper kit on one PaperLayout (capture flag --kit-sheet, DebugCapture.UiRedesign.cs).
    /// It is the check of the kit itself: 9-slice corners, Vietnamese text, the fonts, and how a 16:9 screen sits on 4:3 / 20:9 / 21:9.
    /// Captions are Vietnamese on purpose (accents must render). Not reachable in normal play.
    /// </summary>
    public static class PaperKitSheet
    {
        static readonly string[] RarityNames = { "Thường", "Tốt", "Hiếm", "Anh Hùng", "Huyền Thoại" };
        static readonly string[] SampleSlots = { Slots.Weapon, Slots.Armor, Slots.Boots, Slots.Helmet, Slots.Offhand };
        static PaperLayout layout;

        public static bool IsOpen { get { return layout != null; } }

        public static void Close() { if (layout != null) Object.Destroy(layout.gameObject); layout = null; }

        public static void Open(Transform canvas)
        {
            Close();
            layout = PaperLayout.Create(canvas, "PaperKitSheet", PaperTheme.Navy); // no scene picture: the flat navy shows the cover area on wide screens
            RectTransform c = layout.Content;
            PaperKit.TitleBoard(c, 8f, 2f, 228f, 70f, "KIT SHEET", "Adventure Paper");
            new PaperChip(c, "Gold", 400f, 24f, 130f, 28f, LootSprites.Coin(), "3,000 G", () => { });
            new PaperChip(c, "Potion", 546f, 24f, 86f, 28f, LootSprites.Potion(true), "x20");
            new PaperChip(c, "Stone", 648f, 24f, 92f, 28f, LootSprites.Stone(), "x100");
            new PaperButton(c, "Skill", 880f, 16f, 130f, 42f, "Kỹ Năng", PaperTone.Blue, () => { });
            PaperKit.CloseButton(c, PaperLayout.DesignWidth - 56f, 14f, 44f, Close);
            BuildFrames(Panel(c, "Frames", 8f, 74f, 380f, 560f, PaperFrame.Paper));
            BuildItems(Panel(c, "Items", 392f, 74f, 452f, 560f, PaperFrame.Paper));
            BuildBars(Panel(c, "Bars", 848f, 74f, 282f, 268f, PaperFrame.Pinned));
            BuildDark(Panel(c, "Dark", 848f, 350f, 282f, 284f, PaperFrame.Wood));
        }

        /// <summary>A frame and the content area inside its rim.</summary>
        static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, PaperFrame kind)
        {
            return PaperKit.Inner(PaperKit.Frame(parent, name, x, y, w, h, kind), kind);
        }

        static void Note(Transform p, float x, float y, float w, string text, Color color)
        {
            PaperTheme.Body(p, "Note", x, y, w, 16f, text, PaperTheme.BarSize, color, PaperInk.Plain, TextAnchor.MiddleCenter);
        }

        static void BuildFrames(RectTransform p)
        {
            PaperKit.Heading(p, "H1", -6f, 2f, 330f, "Khung cửa sổ & panel");
            Note(PaperKit.Frame(p, "Pinned", 0f, 26f, 146f, 82f, PaperFrame.Pinned), 0f, 33f, 146f, "Giấy ghim", PaperTheme.Ink);
            Note(PaperKit.Frame(p, "Slate", 154f, 26f, 84f, 82f, PaperFrame.Slate), 0f, 33f, 84f, "Bảng tối", PaperTheme.TextLight);
            Note(PaperKit.Frame(p, "Card", 246f, 26f, 86f, 82f, PaperFrame.Card), 0f, 33f, 86f, "Thẻ giấy", PaperTheme.Ink);
            Note(PaperKit.Frame(p, "Wood", 0f, 112f, 332f, 44f, PaperFrame.Wood), 0f, 14f, 332f, "Bảng gỗ", PaperTheme.TextLight);

            PaperKit.Heading(p, "H2", -6f, 160f, 330f, "Tab");
            string[] tabs = { "Trang Bị", "Kỹ Năng", "Pet" };
            for (int i = 0; i < tabs.Length; i++) new PaperTab(p, "Tab" + i, i * 112f, 184f, 108f, 36f, tabs[i], PaperTabStyle.Leather, () => { }).SetActive(i == 0);
            new PaperTab(p, "Plank0", 0f, 224f, 164f, 36f, "Cơ Bản", PaperTabStyle.Plank, () => { }).SetActive(true);
            new PaperTab(p, "Plank1", 168f, 224f, 164f, 36f, "Nâng Cao", PaperTabStyle.Plank, () => { });
            string[] chips = { "Tất cả", "Vũ khí", "Giáp", "Giày" };
            for (int i = 0; i < chips.Length; i++) new PaperTab(p, "Chip" + i, i * 84f, 264f, 80f, 26f, chips[i], PaperTabStyle.Chip, () => { }).SetActive(i == 0);

            PaperKit.Heading(p, "H3", -6f, 294f, 330f, "Nút");
            new PaperButton(p, "Gold", 0f, 318f, 163f, 42f, "Xác Nhận", PaperTone.Gold, () => { });
            new PaperButton(p, "Blue", 169f, 318f, 163f, 42f, "Chi Tiết", PaperTone.Blue, () => { });
            new PaperButton(p, "Red", 0f, 364f, 106f, 36f, "Hủy / Bán", PaperTone.Red, () => { });
            new PaperButton(p, "Purple", 113f, 364f, 106f, 36f, "Quay x10", PaperTone.Purple, () => { });
            new PaperButton(p, "Off", 226f, 364f, 106f, 36f, "Đã tắt", PaperTone.Gold, () => { }).SetEnabled(false); // a gold button after SetEnabled(false)

            PaperKit.Heading(p, "H4", -6f, 404f, 330f, "Chữ (Baloo 2 / Nunito)");
            PaperTheme.Title(p, "T22", 0f, 426f, 332f, 28f, "Thùng Đồ Huyền Thoại 22", PaperTheme.TitleSize, PaperTheme.Ink);
            PaperTheme.Title(p, "T15", 0f, 454f, 332f, 20f, "Mục: Kỹ Năng Đặc Biệt 15", PaperTheme.SectionSize, PaperTheme.Ink);
            PaperTheme.Body(p, "B13", 0f, 474f, 332f, 18f, "Nội dung 13: ắ ằ ẳ ẵ ặ ấ ề ể ỗ ợ ữ ỵ Đ Ư Ơ", PaperTheme.BodySize, PaperTheme.Ink);
            PaperTheme.Body(p, "B13b", 0f, 492f, 332f, 18f, "Đậm 13: Găng tay thép của thợ rèn hoàng gia", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
        }

        static void BuildItems(RectTransform p)
        {
            PaperKit.Heading(p, "H1", -6f, 2f, 400f, "Ô vật phẩm");
            var items = new Item[PaperTheme.RarityCount];
            for (int t = 0; t < PaperTheme.RarityCount; t++)
            {
                // "legendary" is not an item rarity yet: the sample item is epic, only the frame shows the fifth tier
                items[t] = ItemFactory.Generate(RpgRandom.Default, 60, SampleSlots[t], PaperTheme.RarityId(Mathf.Min(t, ItemDefs.RarityOrder.Length - 1)), "warrior");
                new PaperSlot(p, "Slot" + t, 4f + t * 67f, 28f, 56f).Set(t, LootSprites.Icon(items[t]));
                Note(p, -2f + t * 67f, 85f, 68f, RarityNames[t], PaperTheme.Ink);
            }
            new PaperSlot(p, "Empty", 4f + 5 * 67f, 28f, 56f);
            Note(p, -2f + 5 * 67f, 85f, 68f, "Trống", PaperTheme.Ink);
            new PaperSlot(p, "Selected", 4f, 104f, 56f).Set(2, LootSprites.Icon(items[2]), true);
            Note(p, -2f, 161f, 68f, "Đang chọn", PaperTheme.Ink);
            new PaperSlot(p, "Locked", 71f, 104f, 56f).Set(3, LootSprites.Icon(items[3]), false, true);
            Note(p, 65f, 161f, 68f, "Bị khóa", PaperTheme.Ink);
            new PaperStars(p, "Stars", 144f, 122f, 5, 18f).Set(3);
            PaperKit.PageArrow(p, "Prev", 250f, 112f, 40f, true, () => { });
            new PaperDots(p, "Dots", 300f, 126f, 3, 12f).Set(1);
            PaperKit.PageArrow(p, "Next", 360f, 112f, 40f, false, () => { });

            PaperKit.Heading(p, "H2", -6f, 180f, 400f, "Thẻ vật phẩm");
            int[] tiers = { 0, 2, 3, 4 };
            string[] names = { "Kiếm Sắt", "Khiên Hoàng Gia", "Dây Chuyền Thạch Anh", "Vương Miện" };
            for (int i = 0; i < tiers.Length; i++)
                new PaperCard(p, "Card" + i, 6f + i * 102f, 205f).Set(tiers[i], LootSprites.Icon(items[tiers[i]]), names[i], i == 3 ? "Cần Lv. 70" : "Lv. 60", i + 2, i == 1, i == 3);

            PaperKit.Heading(p, "H3", -6f, 322f, 400f, "Badge & nhãn");
            new PaperBadge(p, 16f, 360f, 24f).Set(80);
            new PaperBadge(p, 48f, 360f, 24f).Set(120);
            for (int m = 0; m < 5; m++) PaperKit.Mark(p, (PaperMark)m, 84f + m * 32f, 360f, m == (int)PaperMark.Dot ? 12f : 24f);
            string[] labels = { "New", "Hot", "Lv70", "Đã gán", "Max" };
            for (int i = 0; i < labels.Length; i++) PaperKit.RibbonLabel(p, "Label" + i, 248f + (i % 3) * 54f, i < 3 ? 334f : 366f, i == 4 ? 84f : 50f, i == 1 ? 34f : i == 4 ? 30f : 24f, labels[i], (PaperLabel)i);
            PaperKit.Ribbon(p, "Ribbon", 0f, 396f, 150f, 43f, "Lv. 60");
            PaperKit.SectionStrip(p, "Strip", 160f, 408f, 244f, 30f, "So sánh với đồ đang mặc");
            RectTransform shelf = PaperKit.Frame(p, "SlotsOnWood", 0f, 452f, 404f, 60f, PaperFrame.Wood);
            for (int t = 0; t < 8; t++) new PaperSlot(shelf, "S" + t, 12f + t * 48f, 8f, 44f).Set(t < 5 ? t : -1, t < 5 ? LootSprites.Icon(items[t]) : null);
        }

        static void BuildBars(RectTransform p)
        {
            PaperKit.Heading(p, "H1", -6f, 0f, 240f, "Thanh & tài nguyên");
            var hp = new PaperBar(p, "Hp", 0f, 24f, 242f, 30f, PaperTheme.BarHp, "HP"); hp.Set(512f / 818f); hp.SetText("512 / 818");
            var mp = new PaperBar(p, "Mp", 0f, 54f, 242f, 30f, PaperTheme.BarMp, "MP"); mp.Set(210f / 335f); mp.SetText("210 / 335");
            var exp = new PaperBar(p, "Exp", 0f, 84f, 242f, 30f, PaperTheme.BarExp, "EXP"); exp.Set(0.45f); exp.SetText("35,200 / 78,000");
            var small = new PaperBar(p, "Small", 0f, 116f, 242f, 26f, PaperTheme.BarExp); small.Set(0.623f); small.SetText("EXP 62.3%");
            new PaperBar(p, "Empty", 0f, 146f, 118f, 18f, PaperTheme.BarHp).Set(0f);
            new PaperBar(p, "Tiny", 124f, 146f, 118f, 18f, PaperTheme.Purple).Set(0.04f);
            new PaperChip(p, "Level", 0f, 174f, 62f, 26f, null, "Lv. 60");
            new PaperChip(p, "Potion", 68f, 174f, 72f, 26f, LootSprites.Potion(false), "50/50");
            new PaperChip(p, "Gold", 146f, 174f, 96f, 26f, LootSprites.Coin(), "3,000", () => { });
        }

        static void BuildDark(RectTransform p)
        {
            PaperTheme.Title(p, "Title", 2f, 0f, 246f, 24f, "Trên nền gỗ / nền game", PaperTheme.SectionSize, PaperTheme.TitleGold, PaperInk.Outlined);
            PaperTheme.Body(p, "Outlined", 2f, 24f, 246f, 18f, "Chữ sáng viền nâu: Đã học Lv. 6/10", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleLeft, true);
            PaperTheme.Body(p, "World", 2f, 42f, 246f, 18f, "Chữ có bóng: [Thế giới] Rook: Chào!", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.World);
            RectTransform slate = PaperKit.Inner(PaperKit.Frame(p, "Slate", 0f, 64f, 250f, 104f, PaperFrame.Slate), PaperFrame.Slate);
            for (int t = 0; t < 4; t++) new PaperSlot(slate, "S" + t, 3f + t * 56f, 2f, 48f).Set(t + 1, null, t == 1, t == 3);
            var hp = new PaperBar(slate, "Hp", 0f, 52f, 222f, 24f, PaperTheme.BarHp); hp.Set(0.3f); hp.SetText("30%");
            new PaperButton(p, "Equip", 0f, 174f, 122f, 38f, "Trang Bị", PaperTone.Blue, () => { });
            new PaperButton(p, "Discard", 128f, 174f, 122f, 38f, "Vứt Bỏ", PaperTone.Red, () => { });
            new PaperButton(p, "Grey", 0f, 218f, 250f, 34f, "Không thể dùng", PaperTone.Grey, () => { });
        }
    }
}

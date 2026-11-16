using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// ArmoryWindow: the pinned paper on the right, its pieces. One paper, four faces (ArmoryWindow.CompareShow.cs fills them):
    ///   a bag item    name, type, rarity, level needed; "Compare with Equipped" table; Equip / Enhance / Discard (+ the discard question)
    ///   a worn item   the same head with the "Equipped" label; its stats; the enhance preview; Enhance / Unequip
    ///   a supply      a potion stack or the upgrade stones: how many, what it does; Use for potions
    ///   nothing       a one-line hint
    /// The action row hangs on the bottom edge, so the paper can be taller in the grid view without moving anything else.
    /// </summary>
    public sealed partial class ArmoryWindow
    {
        const float CompareX = 732f, CompareW = 398f, PaperW = CompareW - 40f; // the pinned frame's rim is 20 units
        const int StatRows = 5;

        RectTransform compareFrame, actionRow, rarityPill;
        GameObject head, bagFace, wornFace, supplyFace, enhanceBlock;
        PaperSlot detailSlot;
        Text nameText, typeText, rarityText, levelNeedText, hintText, noteText, wornStatsText, wornNoteText, plusText, gainText, stonesText, ownedText, blurbText;
        GameObject equippedLabel;
        readonly Text[] rowLabel = new Text[StatRows], rowWorn = new Text[StatRows], rowItem = new Text[StatRows], rowChange = new Text[StatRows];
        PaperBar chanceBar;
        PaperButton mainButton, enhanceButton, discardButton, downgradeButton;

        void BuildCompare()
        {
            compareFrame = PaperKit.Frame(content, "Compare", CompareX, 54f, CompareW, 327f, PaperFrame.Pinned);
            RectTransform paper = PaperKit.Inner(compareFrame, PaperFrame.Pinned);
            hintText = PaperTheme.Body(paper, "Hint", 0f, 100f, PaperW, 40f, "", PaperTheme.BodySize + 1, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.MiddleCenter, true);

            head = UiKit.Rect(paper, "Head", 0f, 0f, PaperW, 96f).gameObject;
            detailSlot = new PaperSlot(head.transform, "Icon", 0f, 4f, 84f);
            nameText = PaperTheme.Title(head.transform, "Name", 96f, 0f, PaperW - 96f, 30f, "", 20, PaperTheme.Ink);
            nameText.resizeTextForBestFit = true; nameText.resizeTextMinSize = PaperTheme.BodySize; nameText.resizeTextMaxSize = 20; // a long name shrinks instead of wrapping
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap; nameText.verticalOverflow = VerticalWrapMode.Truncate;
            typeText = PaperTheme.Body(head.transform, "Type", 96f, 30f, PaperW - 96f, 18f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);
            rarityPill = UiKit.Rect(head.transform, "Rarity", 96f, 51f, 100f, 21f);
            PaperArt.Plate(rarityPill, Color.white, Color.clear, 10f);
            rarityText = PaperTheme.Caption(rarityPill, "", PaperTheme.BodySize, PaperTheme.TextLight);
            levelNeedText = PaperTheme.Body(head.transform, "LevelNeed", PaperW - 120f, 51f, 120f, 21f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleRight, true);
            equippedLabel = PaperKit.RibbonLabel(head.transform, "Equipped", 96f, 75f, 78f, 20f, "Equipped", PaperLabel.Green).transform.parent.gameObject;
            ownedText = PaperTheme.Title(head.transform, "Owned", 96f, 52f, PaperW - 96f, 24f, "", PaperTheme.SectionSize, PaperTheme.Ink);

            bagFace = UiKit.Rect(paper, "BagItem", 0f, 98f, PaperW, 150f).gameObject;
            PaperKit.SectionStrip(bagFace.transform, "Strip", 0f, 0f, 186f, 22f, "Compare with Equipped");
            string[] columns = { "Stat", "Equipped", "This Item", "Change" };
            for (int c = 0; c < columns.Length; c++) Column(bagFace.transform, "Head" + c, c, 24f, columns[c], PaperTheme.InkSoft);
            for (int i = 0; i < StatRows; i++)
            {
                float y = 41f + i * 17f;
                rowLabel[i] = Column(bagFace.transform, "Stat" + i, 0, y, "", PaperTheme.Ink);
                rowWorn[i] = Column(bagFace.transform, "Worn" + i, 1, y, "", PaperTheme.Ink);
                rowItem[i] = Column(bagFace.transform, "Item" + i, 2, y, "", PaperTheme.Ink);
                rowChange[i] = Column(bagFace.transform, "Change" + i, 3, y, "", PaperTheme.Ink);
            }
            noteText = PaperTheme.Body(bagFace.transform, "Note", 4f, 0f, PaperW - 8f, 32f, "", PaperTheme.BarSize + 1, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.UpperLeft);

            wornFace = UiKit.Rect(paper, "WornItem", 0f, 98f, PaperW, 150f).gameObject;
            PaperArt.Plate(UiKit.Rect(wornFace.transform, "StatsPlate", 0f, 2f, PaperW, 24f), PaperTheme.Cream, PaperTheme.PaperEdge, 6f);
            wornStatsText = PaperTheme.Body(wornFace.transform, "Stats", 8f, 2f, PaperW - 16f, 24f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            wornNoteText = PaperTheme.Body(wornFace.transform, "Note", 4f, 34f, PaperW - 8f, 36f, "", PaperTheme.BodySize, PaperTheme.InkSoft, PaperInk.Plain, TextAnchor.UpperLeft);
            enhanceBlock = UiKit.Rect(wornFace.transform, "Enhance", 0f, 30f, PaperW, 118f).gameObject;
            PaperKit.SectionStrip(enhanceBlock.transform, "Strip", 0f, 0f, 96f, 22f, "Enhance");
            plusText = PaperTheme.Title(enhanceBlock.transform, "Plus", 0f, 22f, PaperW, 24f, "", 17, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter);
            gainText = PaperTheme.Body(enhanceBlock.transform, "Gain", 0f, 45f, PaperW, 18f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            chanceBar = new PaperBar(enhanceBlock.transform, "Chance", 40f, 64f, PaperW - 80f, 28f, PaperTheme.BarExp);
            Image stone = UiKit.Icon(enhanceBlock.transform, "Stone", 84f, 95f, 20f);
            stone.sprite = LootSprites.Stone();
            stonesText = PaperTheme.Body(enhanceBlock.transform, "Stones", 108f, 95f, PaperW - 116f, 20f, "", PaperTheme.BodySize, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleLeft, true);

            supplyFace = UiKit.Rect(paper, "Supply", 0f, 98f, PaperW, 150f).gameObject;
            blurbText = PaperTheme.Body(supplyFace.transform, "Blurb", 4f, 8f, PaperW - 8f, 60f, "", PaperTheme.BodySize + 1, PaperTheme.Ink, PaperInk.Plain, TextAnchor.UpperLeft);

            actionRow = UiWinKit.Anchored(compareFrame, "Actions", Vector2.zero, Vector2.zero, 20f, 22f, PaperW, 38f);
            mainButton = new PaperButton(actionRow, "Main", 0f, 0f, 120f, 38f, "Equip", PaperTone.Blue, OnMainButton);
            enhanceButton = new PaperButton(actionRow, "Enhance", 0f, 0f, 120f, 38f, "Enhance", PaperTone.Gold, EnhanceSelected);
            discardButton = new PaperButton(actionRow, "Discard", 0f, 0f, 120f, 38f, "Discard", PaperTone.Red, DiscardSelected);
            // TEMPORARY test tool of the old window (InventoryPanel.DevDowngrade.cs): "-1" lowers the + of the shown weapon. Test builds, offline only.
            if (BagActions.DowngradeAvailable) downgradeButton = new PaperButton(compareFrame, "DevDowngrade", CompareW - 70f, 14f, 40f, 24f, "-1", PaperTone.Purple, DowngradeSelected);
        }

        // Columns of the compare table: Stat | Equipped | This Item | Change.
        static readonly float[] ColumnX = { 6f, 92f, 178f, 268f };
        static readonly float[] ColumnW = { 84f, 80f, 84f, 84f };

        static Text Column(Transform parent, string name, int column, float y, string text, Color color)
        {
            return PaperTheme.Body(parent, name, ColumnX[column], y, ColumnW[column], 17f, text, PaperTheme.BodySize, color, PaperInk.Plain, column == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, true);
        }

        /// <summary>Lays the visible action buttons out side by side over the row (null = that button is hidden). A worn item leads with Enhance, like the mockup.</summary>
        void LayoutActions(string main, bool enhance, string discard, bool enhanceFirst = false)
        {
            int n = (main != null ? 1 : 0) + (enhance ? 1 : 0) + (discard != null ? 1 : 0);
            float w = n > 0 ? (PaperW - (n - 1) * 8f) / n : 0f, x = 0f;
            if (enhanceFirst) x = Place(enhanceButton, null, enhance, x, w);
            x = Place(mainButton, main, main != null, x, w);
            if (!enhanceFirst) x = Place(enhanceButton, null, enhance, x, w);
            Place(discardButton, discard, discard != null, x, w);
        }

        static float Place(PaperButton b, string caption, bool shown, float x, float w)
        {
            b.Root.gameObject.SetActive(shown);
            if (!shown) return x;
            if (caption != null) b.Label.text = caption;
            b.Root.anchoredPosition = new Vector2(x, 0f);
            b.Root.sizeDelta = new Vector2(w, 38f);
            return x + w + 8f;
        }
    }
}

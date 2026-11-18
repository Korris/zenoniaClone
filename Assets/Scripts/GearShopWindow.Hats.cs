using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// GearShopWindow: the "Hats" tab after the tier tabs. Every village blacksmith sells the four common shop hats (HeadgearCatalog, source shop)
    /// for gold; cosmetic only, so the row says "Cosmetic" where gear shows its stats. Online the row's entry (128 + hat id) goes in InvOp.GearBuy.
    /// </summary>
    public sealed partial class GearShopWindow
    {
        Button hatTab;
        Text hatTabLabel;

        void BuildHatTab(float x)
        {
            hatTab = UiWinKit.ArtButton(root, "TabHats", "btn-navy", x, 102f, TabW, 30f, "Hats", 14, Color.white, ShowHats);
            hatTabLabel = hatTab.GetComponentInChildren<Text>();
        }

        /// <summary>Shown tab = mint pill (RO Soft) or full white; the others white pill / dimmed.</summary>
        static void StyleTab(Button tab, Text label, bool on)
        {
            if (UiStyle.Ro) // RO Soft Fantasy pill tabs: mint = shown tab, white = the others
            {
                var face = (Image)tab.targetGraphic;
                face.sprite = RoSoftSprites.Get(on ? RoSoftSprites.Shape.TabOn : RoSoftSprites.Shape.TabOff);
                face.type = Image.Type.Sliced; face.pixelsPerUnitMultiplier = 1f; face.color = Color.white;
                label.color = on ? Color.white : RoSoftTheme.Ink;
            }
            else tab.targetGraphic.color = on ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }

        /// <summary>The shop hats, one row each (fewer than MaxRows).</summary>
        void ShowHats()
        {
            for (int t = 0; t < MaxTiersShown; t++) StyleTab(tabs[t], tabLabels[t], false);
            StyleTab(hatTab, hatTabLabel, true);
            int n = 0;
            foreach (HeadgearDef hat in HeadgearCatalog.BySource(HeadgearSource.Shop))
            {
                if (n >= MaxRows) break;
                Row row = rows[n++];
                row.HatId = hat.Id; row.Entry = HeadgearCatalog.ShopEntry(hat.Id);
                row.Icon.sprite = HeadgearArt.Icon(hat.Id);
                row.Icon.enabled = row.Icon.sprite != null;
                row.Name.text = hat.Name;
                row.Stats.text = "Cosmetic hat";
                row.ReqLv.text = "No req.";
                row.Price.text = hat.Price + " G";
                row.Root.gameObject.SetActive(true);
            }
            for (int i = n; i < MaxRows; i++) rows[i].Root.gameObject.SetActive(false);
            for (int i = 0; i < n; i++) rows[i].Root.anchoredPosition = new Vector2(30f, -(RowsY + i * RowH));
            root.sizeDelta = new Vector2(W, RowsY + n * RowH + BottomPad);
            shownGold = -1;
        }

        /// <summary>Capture runs (--open-shop NPC --shop-tab hats): the hat rows.</summary>
        public static void TestShowHats() { if (instance != null && instance.open) instance.ShowHats(); }
    }
}

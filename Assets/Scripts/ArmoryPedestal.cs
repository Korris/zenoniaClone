using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// One gear pedestal of the Armory Carousel (bag-final-mockup.png): the worn item floating over a stone drum, with a paper name tag in
    /// front ("Weapon", "Armor" ...). Selected = the lit drum and a gold tag. Until sheet A1 is cut the drum and the tag are flat plates
    /// (PaperPiece.BagPedestal / BagPedestalSelected / BagPedestalLabel; the cut drum is 256 x 200 px). The whole pedestal is one tap target.
    /// </summary>
    public sealed class ArmoryPedestal
    {
        public const float W = 96f, H = 102f, IconSize = 56f;
        /// <summary>Centre of the floating item, from the pedestal's top-left: Place puts this point on the carousel.</summary>
        public const float IconCx = W * 0.5f, IconCy = IconSize * 0.5f;

        public readonly RectTransform Root;
        public readonly Image Icon;
        readonly Image drum, tag;
        readonly Text label, corner;
        readonly Outline labelOutline;
        int lit = -1;

        public ArmoryPedestal(Transform parent, string name, float cx, float cy, string caption, Action onClick)
        {
            Root = UiKit.Rect(parent, name, cx - IconCx, cy - IconCy, W, H);
            Image hit = UiKit.Panel(Root, Color.clear, true); // invisible: the drum, the item and the tag all answer the tap
            drum = PaperArt.Draw(UiKit.Rect(Root, "Drum", 5f, 28f, W - 10f, 67f), null, Color.white, Color.clear);
            Icon = UiKit.Icon(Root, "Icon", (W - IconSize) * 0.5f, 0f, IconSize);
            corner = PaperTheme.Body(Root, "Corner", W - 44f, 0f, 40f, 16f, "", PaperTheme.BodySize, PaperTheme.TitleGold, PaperInk.World, TextAnchor.MiddleRight, true);
            tag = PaperArt.Draw(UiKit.Rect(Root, "Tag", 8f, 76f, W - 16f, 24f), null, Color.white, Color.clear);
            label = PaperTheme.Caption(tag.transform, caption, PaperTheme.BarSize + 1, PaperTheme.Ink);
            labelOutline = label.GetComponent<Outline>();
            labelOutline.enabled = false; // dark text on the paper tag
            UiWinKit.MakeButton(hit, onClick);
            Light(false);
        }

        /// <summary>Hides the name tag (the small carousel above the grid view: too small to read).</summary>
        public void ShowTag(bool on) { if (tag.gameObject.activeSelf != on) tag.gameObject.SetActive(on); }

        /// <summary>The item on the drum. `corner` = small text at its top-right ("+7", "3*"); a faint tint shows what belongs on an empty pedestal.</summary>
        public void Set(Sprite icon, Color tint, string cornerText, bool selected)
        {
            if (Icon.sprite != icon) { Icon.sprite = icon; Icon.enabled = icon != null; }
            Icon.color = tint;
            corner.text = cornerText ?? "";
            Light(selected);
        }

        /// <summary>The lit look without changing what is shown: the selected pedestal, or the one a dragged card is over.</summary>
        public void Light(bool on)
        {
            if (lit == (on ? 1 : 0)) return;
            lit = on ? 1 : 0;
            PaperArt.Redraw(drum, on ? PaperPiece.BagPedestalSelected : PaperPiece.BagPedestal, on ? PaperTheme.Gold : PaperTheme.Steel, on ? PaperTheme.Glow : PaperTheme.Slate, 16f);
            PaperArt.Redraw(tag, on ? PaperPiece.BagPedestalLabelSelected : PaperPiece.BagPedestalLabel, on ? PaperTheme.BtnGold : PaperTheme.Paper, on ? PaperTheme.BtnGoldEdge : PaperTheme.Wood, 6f);
        }
    }
}

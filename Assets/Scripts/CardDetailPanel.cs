using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The card detail box of My Cards and the Monster Album (mockups 01 / 02): name, big card, rarity + type, Element, Source, Owned, the four Duel Stats, the Card Effect
    /// (the option of its Power / Guard gem), the description. Numbers come from CardTable, never from the mockup. The page puts its action buttons under it (from ButtonsY).
    /// A card the character never owned (Album) shows silhouette and "?" instead of the facts.
    /// </summary>
    public sealed class CardDetailPanel
    {
        public const float Height = 544f, ButtonsY = 408f;
        readonly RectTransform root;
        readonly GemCardView big;
        readonly Image badge, elementIcon;
        readonly Text title, type, element, source, owned, effect, description;
        readonly Text[] stat = new Text[4];

        public CardDetailPanel(Transform parent, float x, float y, float w)
        {
            root = UiKit.Rect(parent, "CardDetail", x, y, w, Height);
            U.Panel(root, "Box", 0f, 0f, w, Height, true);
            title = U.Txt(root, "Title", 16f, 8f, w - 32f, 30f, "", U.TextTitle, U.Ink, TextAnchor.MiddleCenter);
            big = new GemCardView(root, "Big", 14f, 46f, 112f, 149f, null, false);
            float tx = 138f, tw = w - tx - 12f;
            badge = GemUiArt.Img(root, "Badge", tx, 48f, 48f, 24f, null);
            type = U.Txt(root, "Type", tx + 54f, 46f, tw - 54f, 28f, "", U.TextNormal, U.Ink);
            elementIcon = GemUiArt.Img(root, "ElementIcon", tx, 82f, 22f, 22f, null);
            element = U.Txt(root, "Element", tx + 28f, 80f, tw - 28f, 26f, "", U.TextNormal, U.Ink);
            source = U.Txt(root, "Source", tx, 110f, tw, 46f, "", U.TextSmall + 1, U.Ink, TextAnchor.UpperLeft);
            owned = U.Txt(root, "Owned", tx, 158f, tw, 40f, "", U.TextSmall + 1, U.Ink, TextAnchor.UpperLeft);
            U.Heading(root, "StatsHeading", 16f, 200f, w - 32f, "Duel Stats");
            string[] arrows = { "arrow-up", "arrow-right", "arrow-down", "arrow-left" };
            float gap = 6f, bw = (w - 28f - gap * 3f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                float bx = 14f + i * (bw + gap);
                GemUiArt.Img(root, "Stat" + i, bx, 230f, bw, 44f, GemUiArt.Frame("panel-inner"));
                GemUiArt.Img(root, "StatIcon" + i, bx + 8f, 241f, 22f, 22f, GemUiArt.Icon(arrows[i]));
                stat[i] = U.Txt(root, "StatNum" + i, bx + 32f, 230f, bw - 36f, 44f, "", 22, U.Ink, TextAnchor.MiddleCenter);
            }
            U.Heading(root, "EffectHeading", 16f, 282f, w - 32f, "Card Effect");
            U.Panel(root, "EffectBox", 14f, 312f, w - 28f, 52f);
            effect = U.Txt(root, "Effect", 24f, 314f, w - 48f, 48f, "", U.TextSmall + 1, U.Ink, TextAnchor.MiddleLeft);
            description = U.Txt(root, "Description", 16f, 368f, w - 32f, 38f, "", U.TextSmall, U.InkSoft, TextAnchor.UpperLeft);
        }

        public RectTransform Root { get { return root; } }

        public void Set(CardDef c, PlayerState p, bool known)
        {
            if (c == null)
            {
                title.text = "No card selected";
                big.Root.gameObject.SetActive(false);
                badge.color = Color.clear; elementIcon.color = Color.clear;
                type.text = element.text = source.text = owned.text = effect.text = description.text = "";
                for (int i = 0; i < 4; i++) stat[i].text = "";
                return;
            }
            GemUiArt.SetPiece(elementIcon, known ? GemUiArt.ElementIcon(c) : null);
            source.text = "Source: " + GemUiModel.RegionName(c.Region);
            if (!known)
            {
                title.text = "???";
                big.SetUnknown(c, false);
                GemUiArt.SetPiece(badge, null);
                type.text = "Not collected"; element.text = "???"; owned.text = "Owned: x0";
                for (int i = 0; i < 4; i++) stat[i].text = "?";
                effect.text = "Collect this card to see its effect."; description.text = "Defeat this monster to find its card.";
                return;
            }
            title.text = c.Name;
            big.Set(c, 0, false);
            GemUiArt.SetPiece(badge, GemUiArt.RarityBadge(c));
            type.text = c.IsBoss ? "Boss" : "Normal";
            element.text = c.Element;
            owned.text = "Owned: x" + p.Cards.Count(c.Id) + (p.Cards.Locked(c.Id) > 0 ? "\nLocked: " + p.Cards.Locked(c.Id) : "") + (p.Cards.InDeck(c.Id) > 0 ? "\nIn deck: " + p.Cards.InDeck(c.Id) : "");
            stat[0].text = c.Top.ToString(); stat[1].text = c.Right.ToString(); stat[2].text = c.Bottom.ToString(); stat[3].text = c.Left.ToString();
            effect.text = "Power Gem: " + GemSockets.OptionText(c.Option(GemKind.Power)) + "\nGuard Gem: " + GemSockets.OptionText(c.Option(GemKind.Guard));
            description.text = c.Description;
        }
    }
}

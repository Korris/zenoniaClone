using System;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>One page of the Soul Gem screens (convert / socket / remove), drawn into a page rect of PageW x PageH. The host (GemWindow now, CardWindow's left menu in phase 03) builds it once and calls Refresh when GemUiModel.StateKey changes.</summary>
    public abstract class GemPage
    {
        public const float PageW = 951f, PageH = 544f;
        public RectTransform Root { get; protected set; }
        protected static PlayerState Me { get { return PetActions.Me; } }
        public abstract void Build(RectTransform parent);
        public abstract void Refresh();
        public virtual void OnShow() { Refresh(); }
    }

    /// <summary>Gem info box (Gem Info / Selected Gem): the gem picture and name, the Rarity / Type / Element / Source rows, then the "Gem Option" box with the stat line and an optional red lock note under it.</summary>
    public sealed class GemInfoPanel
    {
        const float RowPitch = 28f, RowH = 24f, LabelW = 70f, Pad = 14f;
        readonly RectTransform root;
        readonly GemTileView tile;
        readonly Text name, rarity, type, element, source, option, locked;
        readonly Image elementIcon, optionIcon;

        public GemInfoPanel(Transform parent, float x, float y, float w, string heading)
        {
            root = UiKit.Rect(parent, "GemInfo", x, y, w, 430f);
            U.Panel(root, "InfoBox", 0f, 0f, w, 262f, true);
            U.Heading(root, "Heading", 16f, 8f, w - 32f, heading);
            tile = new GemTileView(root, "Gem", Pad, 44f, 84f, null, false, false);
            name = U.Txt(root, "Name", Pad + 92f, 46f, w - Pad * 2f - 92f, 80f, "", U.TextNormal + 1, U.Ink, TextAnchor.MiddleLeft);
            rarity = Row("Rarity", 0, w); type = Row("Type", 1, w); element = Row("Element", 2, w); source = Row("Source", 3, w);
            elementIcon = UiKit.Rect(root, "ElementIcon", Pad + LabelW + 8f, 138f + 2 * RowPitch + 2f, 20f, 20f).gameObject.AddComponent<Image>();
            elementIcon.raycastTarget = false;
            U.Panel(root, "OptionBox", 0f, 270f, w, 82f, true);
            U.Heading(root, "OptionHeading", 16f, 276f, w - 32f, "Gem Option");
            optionIcon = UiKit.Rect(root, "OptionIcon", 24f, 306f, 34f, 34f).gameObject.AddComponent<Image>();
            optionIcon.raycastTarget = false; optionIcon.preserveAspect = true;
            option = U.Txt(root, "Option", 68f, 304f, w - 84f, 38f, "", 24, U.Ink);
            locked = U.Txt(root, "Locked", 6f, 358f, w - 12f, 64f, "", U.TextSmall, U.InkSoft, TextAnchor.UpperLeft);
        }

        Text Row(string label, int i, float w)
        {
            float y = 138f + i * RowPitch;
            GemUiArt.Img(root, "Label" + label, Pad, y, LabelW, RowH, GemUiArt.Frame("filter-pill"));
            U.Txt(root, "L" + label, Pad + 2f, y, LabelW - 4f, RowH, label, U.TextSmall, U.InkSoft, TextAnchor.MiddleCenter);
            return U.Txt(root, "V" + label, Pad + LabelW + 8f, y, w - Pad * 2f - LabelW - 8f, RowH, "", U.TextSmall + 1, U.Ink);
        }

        /// <summary>`lockedNote` goes under the option box in red when `tradeLocked`, else in soft ink.</summary>
        public void Set(CardDef c, GemKind kind, bool tradeLocked, string lockedNote)
        {
            root.gameObject.SetActive(c != null);
            if (c == null) return;
            tile.Set(c, kind, 0, false, false);
            name.text = U.Name(kind, c);
            rarity.text = c.IsBoss ? c.Rarity + " (Boss)" : c.Rarity.ToString();
            type.text = kind == GemKind.Power ? "Power Gem" : "Guard Gem";
            element.text = (GemUiArt.ElementIcon(c) != null ? "      " : "") + c.Element;
            GemUiArt.SetPiece(elementIcon, GemUiArt.ElementIcon(c));
            source.text = c.Name;
            GemOption o = c.Option(kind);
            option.text = GemSockets.OptionText(o);
            GemUiArt.SetPiece(optionIcon, GemUiArt.FilterIcon(StatFilter(o.Stat)));
            locked.text = lockedNote ?? "";
            locked.color = tradeLocked ? U.Bad : U.InkSoft;
        }

        static GemFilter StatFilter(GemStat s) { return s == GemStat.Atk ? GemFilter.Atk : s == GemStat.Def ? GemFilter.Def : s == GemStat.MaxHp ? GemFilter.Hp : GemFilter.Utility; }
    }

    /// <summary>Middle stage of the socket / removal pages: the selected item's name, its big icon and one tile per socket (the gem in it, or the empty socket piece). Tapping a tile selects that socket.</summary>
    public sealed class GemItemStage
    {
        const float Tile = 76f;
        readonly RectTransform root;
        readonly Text title, hint;
        readonly Image icon;
        readonly GemTileView[] gems = new GemTileView[2];
        readonly Image[] empty = new Image[2];
        readonly Image[] pick = new Image[2];
        readonly Action<int> onPick;
        public int Selected { get; private set; }

        public GemItemStage(Transform parent, float x, float y, float w, Action<int> onPick)
        {
            this.onPick = onPick;
            root = UiKit.Rect(parent, "Stage", x, y, w, 296f);
            U.Panel(root, "StageBox", 0f, 0f, w, 296f, true);
            title = U.Txt(root, "Title", 16f, 8f, w - 32f, 30f, "", U.TextTitle, U.Ink, TextAnchor.MiddleCenter);
            icon = UiKit.Rect(root, "Icon", w * 0.5f - 62f, 44f, 124f, 124f).gameObject.AddComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            hint = U.Txt(root, "Hint", 16f, 172f, w - 32f, 20f, "", U.TextSmall, U.InkSoft, TextAnchor.MiddleCenter);
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                float tx = w * 0.5f + (i == 0 ? -Tile - 14f : 14f);
                empty[i] = GemUiArt.Img(root, "Empty" + i, tx + 10f, 200f + 10f, Tile - 20f, Tile - 20f, GemUiArt.Socket("socket-empty"), true);
                UiWinKit.MakeButton(empty[i], () => Pick(index));
                gems[i] = new GemTileView(root, "Gem" + i, tx, 200f, Tile, () => Pick(index), false, false);
                pick[i] = GemUiArt.Img(root, "Pick" + i, tx - 4f, 196f, Tile + 8f, Tile + 8f, null);
            }
        }

        void Pick(int i) { Selected = i; onPick(i); Draw(lastItem); }

        Item lastItem;

        /// <summary>Draws `item` (null = nothing selected). `selectable`: tapping an occupied (true) or any socket moves the selection ring.</summary>
        public void Draw(Item item)
        {
            lastItem = item;
            root.gameObject.SetActive(item != null);
            if (item == null) return;
            title.text = EnhanceStats.DisplayName(item);
            Sprite s = LootSprites.Icon(item);
            icon.sprite = s; icon.color = s != null ? Color.white : Color.clear;
            int slots = GemSockets.SlotCount(item.slot);
            hint.text = slots + (slots == 1 ? " socket" : " sockets") + ", " + GemUiModel.Filled(item) + " used";
            for (int i = 0; i < 2; i++)
            {
                bool exists = i < slots;
                string gem = exists && item.sockets != null && i < item.sockets.Count ? item.sockets[i] : null;
                string id; GemKind kind;
                bool filled = gem != null && GemSockets.TryParse(gem, out id, out kind);
                empty[i].gameObject.SetActive(exists && !filled);
                gems[i].Root.gameObject.SetActive(filled);
                if (filled) { GemSockets.TryParse(gem, out id, out kind); gems[i].Set(CardTable.Get(id), kind, 0, false, true); }
                pick[i].gameObject.SetActive(exists);
                GemUiArt.SetPiece(pick[i], exists && Selected == i ? "cards/card/select-frame-gold" : null);
            }
        }

        public void Select(int socket) { Selected = Mathf.Max(0, socket); }
    }
}

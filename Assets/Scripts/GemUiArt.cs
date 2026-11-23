using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Art of the Soul Gem screens (convert / socket / remove). EVERY picture is a ChatGPT piece from the card kit (phase 02,
    /// Resources/Sprites/ui/paper/cards/{frame,card,icons,gems}/ and the monster portraits Resources/Sprites/monsters/card-portraits/&lt;id&gt;.png):
    /// nothing is drawn in code. A piece that is not delivered yet shows nothing (the layout and texts stay), and PaperArt logs it once.
    /// </summary>
    public static class GemUiArt
    {
        public const string PortraitFolder = "Sprites/monsters/card-portraits/";
        static readonly Dictionary<string, Sprite> Portraits = new Dictionary<string, Sprite>();

        public static string Frame(string name) { return "cards/frame/" + name; }
        public static string Icon(string name) { return "cards/icons/" + name; }
        public static string Socket(string name) { return "cards/gems/" + name; }

        /// <summary>"cards/gems/gem-power-sr": the gem shell of a kind and rarity.</summary>
        public static string GemShell(GemKind kind, CardRarity r) { return "cards/gems/gem-" + (kind == GemKind.Power ? "power" : "guard") + "-" + r.ToString().ToLowerInvariant(); }

        public static string CardFrame(CardDef c) { return c.IsBoss ? "cards/card/card-frame-boss" : "cards/card/card-frame-" + c.Rarity.ToString().ToLowerInvariant(); }
        public static string RarityBadge(CardDef c) { return "cards/card/badge-" + c.Rarity.ToString().ToLowerInvariant(); }

        public static string FilterIcon(GemFilter f)
        {
            switch (f) { case GemFilter.Atk: return Icon("filter-atk"); case GemFilter.Def: return Icon("filter-def"); case GemFilter.Hp: return Icon("filter-hp"); case GemFilter.Utility: return Icon("filter-utility"); default: return Icon("filter-all"); }
        }

        /// <summary>Element icon of a card: Shadow uses the "dark" piece; Poison has no piece in the kit yet (asked from ChatGPT: it shows nothing meanwhile).</summary>
        public static string ElementIcon(CardDef c)
        {
            string e = (c.Element ?? "").ToLowerInvariant();
            if (e == "shadow") e = "dark";
            return e == "poison" ? null : Icon("element-" + e);
        }

        /// <summary>The ChatGPT portrait of a card's monster, cached; null until it is delivered.</summary>
        public static Sprite Portrait(CardDef c)
        {
            if (c == null) return null;
            Sprite s;
            if (Portraits.TryGetValue(c.Id, out s) && s != null) return s;
            var tex = Resources.Load<Texture2D>(PortraitFolder + c.Id);
            s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect) : null;
            Portraits[c.Id] = s;
            return s;
        }

        /// <summary>Puts a piece on an image (9-sliced by its borders, else whole with its aspect). A missing piece clears the image.</summary>
        public static void SetPiece(Image img, string piece)
        {
            Sprite s = piece != null ? PaperArt.Get(piece) : null;
            if (s == null) { img.sprite = null; img.color = Color.clear; return; }
            img.color = Color.white;
            PaperArt.Apply(img, s);
            if (s.border == Vector4.zero) img.preserveAspect = true;
        }

        /// <summary>Piece image at a top-left based position (UiKit coordinates).</summary>
        public static Image Img(Transform parent, string name, float x, float y, float w, float h, string piece, bool raycast = false)
        {
            Image img = UiKit.Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            img.raycastTarget = raycast;
            SetPiece(img, piece);
            return img;
        }

        /// <summary>Which gems a filter chip keeps, by the stat of the gem option.</summary>
        public static bool Matches(GemFilter f, GemStat stat)
        {
            switch (f)
            {
                case GemFilter.Atk: return stat == GemStat.Atk;
                case GemFilter.Def: return stat == GemStat.Def;
                case GemFilter.Hp: return stat == GemStat.MaxHp;
                case GemFilter.Utility: return stat == GemStat.MaxMp || stat == GemStat.Crit;
                default: return true;
            }
        }

        /// <summary>Same filter on a card (its category), for the convert grid.</summary>
        public static bool Matches(GemFilter f, CardCategory c)
        {
            switch (f)
            {
                case GemFilter.Atk: return c == CardCategory.Atk;
                case GemFilter.Def: return c == CardCategory.Def;
                case GemFilter.Hp: return c == CardCategory.Hp;
                case GemFilter.Utility: return c == CardCategory.Utility;
                default: return true;
            }
        }
    }

    public enum GemFilter { All, Atk, Def, Hp, Utility }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The trade UI's own art (Resources/Sprites/ui/trade/trade-*.png), the one place that passes each piece's border, as PartyArt does for the party pieces. Pieces with no entry are drawn
    /// unsliced (coin, lock, check, clock, warning). The rest take their scale from the height they are drawn at, or the fixed one given here.
    /// </summary>
    public static class TradeArt
    {
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "offer-frame", new Vector4(54f, 54f, 54f, 54f) }, { "offer-frame-locked", new Vector4(54f, 54f, 54f, 54f) }, { "gold-field", new Vector4(30f, 0f, 30f, 0f) },
            { "title-plaque", new Vector4(42f, 0f, 42f, 0f) }, { "btn-disabled", new Vector4(32f, 0f, 32f, 0f) }, { "btn-confirm", new Vector4(32f, 0f, 32f, 0f) }, { "slot-locked", new Vector4(45f, 45f, 45f, 45f) },
        };

        public const float OfferFrameScale = 0.42f, SlotNativeSize = 160f;

        public static Sprite Get(string name)
        {
            Vector4 border;
            Borders.TryGetValue(name, out border);
            return UiArt.Get("trade/trade-" + name, border);
        }

        public static Image Sliced(RectTransform rt, string piece, float scale, bool raycast = false)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Get(piece);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / scale;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Sliced piece whose corners scale with the height it is drawn at (plaque, gold field, buttons).</summary>
        public static Image ByHeight(RectTransform rt, string piece, float h, bool raycast = false)
        {
            Sprite s = Get(piece);
            return Sliced(rt, piece, h / (s != null ? s.rect.height : 100f), raycast);
        }

        public static Image Plain(RectTransform rt, string piece)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Get(piece);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Swaps the look of an art button and keeps its corners true to its height.</summary>
        public static void SetButton(Image img, Sprite sprite, float h)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            if (sprite != null) img.pixelsPerUnitMultiplier = sprite.rect.height / h;
        }
    }
}

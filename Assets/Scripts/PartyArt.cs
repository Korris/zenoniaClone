using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The party UI's own art (Resources/Sprites/ui/party/party-*.png). UiArt.Win only knows win-* pieces, and UiArt.Get caches by name, so this table is the one place that passes each
    /// piece's border. `scale` works as in UiWinKit: canvas units per native pixel, so corners keep their look. Buttons take their scale from their height.
    /// </summary>
    public static class PartyArt
    {
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "invite-frame", new Vector4(66f, 62f, 66f, 62f) }, { "panel-frame", new Vector4(64f, 62f, 64f, 62f) }, { "row-frame", new Vector4(16f, 16f, 16f, 16f) },
            { "hp-frame", new Vector4(24f, 0f, 24f, 0f) }, { "hp-fill", new Vector4(20f, 0f, 20f, 0f) },
            { "btn-accept", new Vector4(30f, 0f, 30f, 0f) }, { "btn-decline", new Vector4(30f, 0f, 30f, 0f) }, { "btn-cancel", new Vector4(30f, 0f, 30f, 0f) },
            { "btn-leave", new Vector4(26f, 0f, 26f, 0f) }, { "btn-invite", new Vector4(54f, 0f, 54f, 0f) },
        };

        public const float InviteFrameScale = 0.5f, PanelFrameScale = 0.3f, RowFrameScale = 0.32f;
        // the opening of party-hp-frame, as parts of the bar's height
        const float HpOpeningSide = 0.115f, HpOpeningBottom = 0.154f;

        /// <summary>Piece party-{name} with its border when it has one (kick, crown and class icons have none).</summary>
        public static Sprite Get(string name)
        {
            Vector4 border;
            Borders.TryGetValue(name, out border);
            return UiArt.Get("party/party-" + name, border);
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

        /// <summary>Unsliced piece keeping its aspect.</summary>
        public static Image Plain(RectTransform rt, string piece, bool raycast = false)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Get(piece);
            img.preserveAspect = true;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Art button with a centred bold caption; corners scale with the button height, as UiWinKit.ArtButton.</summary>
        public static Button ArtButton(Transform parent, string name, string piece, float x, float y, float w, float h, string text, int fontSize, Color textColor, Action onClick)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            Sprite s = Get(piece);
            Image img = Sliced(rt, piece, h / (s != null ? s.rect.height : 100f));
            UiKit.Label(UiKit.Fill(rt, "Label"), text, fontSize, textColor, TextAnchor.MiddleCenter, true);
            return UiWinKit.MakeButton(img, onClick);
        }

        /// <summary>HP bar in the gold frame. Sliced, because Bar.Set moves anchorMax and a Simple fill would squash its round ends.</summary>
        public static UiKit.Bar HpBar(Transform parent, string name, float x, float y, float w, float h, int fontSize)
        {
            var bar = new UiKit.Bar(parent, name, x, y, w, h, Color.white, fontSize, Get("hp-frame"));
            bar.FillRt.offsetMin = new Vector2(h * HpOpeningSide, h * HpOpeningBottom);
            bar.FillRt.offsetMax = new Vector2(-h * HpOpeningSide, -h * HpOpeningSide);
            SetHpFill(bar, h, false);
            return bar;
        }

        /// <summary>Red plate normally. A tint would only muddy the red, so a member on another map gets a plain slate fill instead.</summary>
        public static void SetHpFill(UiKit.Bar bar, float h, bool away)
        {
            Image fill = bar.FillImage;
            fill.sprite = away ? null : Get("hp-fill");
            fill.color = away ? new Color(0.42f, 0.48f, 0.58f) : Color.white;
            fill.type = away ? Image.Type.Simple : Image.Type.Sliced;
            if (!away && fill.sprite != null) fill.pixelsPerUnitMultiplier = fill.sprite.rect.height / (h * (1f - HpOpeningSide - HpOpeningBottom));
        }

        /// <summary>Round class icon by hero class index (null for one this build does not know).</summary>
        public static Sprite ClassIcon(int heroClass)
        {
            return heroClass >= 0 && heroClass < HeroClassDef.All.Length ? Get("class-" + RpgSession.ClassId(HeroClassDef.All[heroClass].Id)) : null;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The RO Soft Fantasy frame and button art, cut from the ChatGPT kit sheets (Assets/ArtLibrary/ui/ro-soft-kit/src/) by
    /// Tools/cut-ui-paper-sheet.py with Tools/ui-ro-soft-sheets/*.json into Resources/Sprites/ui/ro-soft/. Each Shape names the piece
    /// that draws it; 9-slice borders come from RoSoftSprites.Borders.cs (generated). The pieces are cut at twice their on-screen size
    /// and made at 200 pixels per unit, so a sliced Image at pixelsPerUnitMultiplier 1 draws their corners at half size: sharp on
    /// high-dpi screens. Colours live in the art: the windows' own Image.color tints (dimmed buttons, inactive tabs, rarity rings)
    /// still multiply on top. A missing piece logs once and returns null (no painted fallback).
    /// </summary>
    public static partial class RoSoftSprites
    {
        public enum Shape { Card, Section, Row, Cell, Primary, Secondary, Danger, Gold, TabOn, TabOff, Pill, SectionLocked, Disabled, Night, NightCell, Glass, ToggleOn, ToggleOff }

        const string Folder = "Sprites/ui/ro-soft/";
        const float PixelsPerUnit = 200f; // pieces are cut at 2x: 2 art pixels per canvas unit (canvas reference = 100)

        /// <summary>The kit piece of each shape. Pieces still missing from the kit borrow the nearest one (see the art report).</summary>
        static readonly Dictionary<Shape, string> Piece = new Dictionary<Shape, string>
        {
            { Shape.Card, "panel" },
            { Shape.Section, "panel" },
            { Shape.Row, "btn-ivory" },
            { Shape.Cell, "slot" },
            { Shape.Primary, "btn-mint" },
            { Shape.Secondary, "btn-sky" },
            { Shape.Danger, "btn-coral" },
            { Shape.Gold, "btn-gold" },
            { Shape.TabOn, "tab-on" },
            { Shape.TabOff, "tab-off" },
            { Shape.Pill, "tab-off" },
            { Shape.SectionLocked, "slot-selected" },
            { Shape.Disabled, "btn-grey" },
            { Shape.Night, "hud-plate" },
            { Shape.NightCell, "hud-plate" },
            { Shape.Glass, "panel-glass" },
            { Shape.ToggleOn, "toggle-on" },
            { Shape.ToggleOff, "toggle-off" },
        };

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<Sprite, Shape> ShapeBySprite = new Dictionary<Sprite, Shape>();
        static readonly HashSet<string> Missing = new HashSet<string>();

        public static void ResetState() { Cache.Clear(); ShapeBySprite.Clear(); Missing.Clear(); }

        public static bool IsButton(Shape s) { return s == Shape.Primary || s == Shape.Secondary || s == Shape.Danger || s == Shape.Gold || s == Shape.TabOn; }

        public static Sprite Get(Shape shape)
        {
            Sprite s = Load(Piece[shape]);
            if (s != null && !ShapeBySprite.ContainsKey(s)) ShapeBySprite[s] = shape; // first shape wins for shared pieces (none of them is a button)
            return s;
        }

        /// <summary>The shape a kit sprite was handed out as (false for any other sprite).</summary>
        public static bool ShapeOf(Sprite sprite, out Shape shape)
        {
            if (sprite != null && ShapeBySprite.TryGetValue(sprite, out shape)) return true;
            shape = Shape.Card;
            return false;
        }

        /// <summary>Round close button (white disc, ink X), drawn unsliced.</summary>
        public static Sprite Close() { return Load("btn-close"); }

        /// <summary>Any kit piece by file name (slot-empty, btn-plus, window-header ...), 9-sliced when it has a border; null when missing.</summary>
        public static Sprite Named(string piece) { return Load(piece); }

        /// <summary>A kit piece by file name, made known to RoSoftSkin as `shape` (button shapes get white labels). For window-specific art such as the bag's bag-tab-on.</summary>
        public static Sprite NamedAs(string piece, Shape shape)
        {
            Sprite s = Load(piece);
            if (s != null) ShapeBySprite[s] = shape;
            return s;
        }

        static Sprite Load(string piece)
        {
            Sprite s;
            if (Cache.TryGetValue(piece, out s) && s != null) return s;
            if (Missing.Contains(piece)) return null;
            var tex = Resources.Load<Texture2D>(Folder + piece);
            if (tex == null)
            {
                Missing.Add(piece);
                Debug.LogWarning("[ZenoniaProto] RO Soft UI piece missing: " + Folder + piece);
                return null;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector4 border;
            Borders.TryGetValue("ro-soft/" + piece, out border);
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            s.name = "ro-soft/" + piece;
            Cache[piece] = s;
            return s;
        }
    }
}

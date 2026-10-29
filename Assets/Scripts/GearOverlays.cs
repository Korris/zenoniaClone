using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Overlay art (staff / sword / shield / gun / robot) as runtime sprites whose pivot is the grip pixel; cached by path.</summary>
    public static class GearOverlays
    {
        public const float Ppu = 100f;                       // overlay sprites are 100 px per unit, scaled per anchor
        public const float FrameUnit = GameConfig.FrameUnits / GameConfig.FramePx; // world units per 192px-frame pixel
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static void ResetState() { Cache.Clear(); }

        /// <summary>Sprite for Resources/Sprites/&lt;folder&gt;/&lt;name&gt;-base|enhanced, pivot at the manifest grip. Null when the PNG is missing.</summary>
        public static Sprite Get(string folder, OverlayEntry entry, bool enhanced)
        {
            string path = "Sprites/" + folder + "/" + entry.name + (enhanced ? "-enhanced" : "-base");
            return Pivoted(path, enhanced ? entry.enhancedGrip : entry.baseGrip);
        }

        /// <summary>Sprite from an explicit resource path with the pivot at (gripX, gripY) pixels from the top-left.</summary>
        public static Sprite Pivoted(string path, GripInfo grip)
        {
            Sprite s;
            if (Cache.TryGetValue(path, out s) && s != null) return s;
            Texture2D tex = SpriteSheet.Load(path);
            s = null;
            if (tex != null)
            {
                // PNG y runs down, Unity texture y runs up.
                var pivot = new Vector2(grip.gripX / (float)tex.width, (tex.height - grip.gripY) / (float)tex.height);
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, Ppu, 0, SpriteMeshType.FullRect);
            }
            Cache[path] = s;
            return s;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// How high a layered hero's head reaches, in world units above its feet: the highest opaque pixel of its hair, hair accessory and hat
    /// (HeroLayeredView), so the RO HUD portrait can fit a tall wizard hat, bunny ears or big hair inside its circle (RoHudPortraits).
    /// The cells are runtime sprites cut from whole sheets (full-rect meshes), so each cell's top opaque row is found once from the
    /// sheet's pixels and kept per sprite; a frame only adds a dictionary look-up per layer.
    /// </summary>
    public static class RoHudPortraitHeadTop
    {
        const byte SolidAlpha = 48; // a soft anti-aliased fringe does not count as the head's top
        static readonly Dictionary<Sprite, float> topOfSprite = new Dictionary<Sprite, float>(); // sprite-local units above its pivot, or NaN (empty)

        /// <summary>The highest opaque point of `view`'s head layers above `feetY` (world), or NegativeInfinity when none is drawn.</summary>
        public static float Above(HeroLayeredView view, float feetY)
        {
            float top = float.NegativeInfinity;
            if (view == null) return top;
            Measure(view.Hair, feetY, ref top);
            Measure(view.HairAccessory, feetY, ref top);
            Measure(view.Headgear, feetY, ref top);
            return top;
        }

        static void Measure(SpriteRenderer r, float feetY, ref float top)
        {
            if (r == null || !r.enabled || r.sprite == null || !r.gameObject.activeInHierarchy) return;
            float local = SpriteTop(r.sprite);
            if (float.IsNaN(local)) return;
            float y = r.transform.TransformPoint(new Vector3(0f, local, 0f)).y - feetY; // hats tilt only a few degrees: the centre column is enough
            if (y > top) top = y;
        }

        /// <summary>Top opaque row of the sprite's cell, in sprite-local units above the pivot (NaN: fully transparent).</summary>
        static float SpriteTop(Sprite s)
        {
            float v;
            if (topOfSprite.TryGetValue(s, out v)) return v;
            v = float.NaN;
            try
            {
                Texture2D tex = s.texture;
                Rect rc = s.textureRect;
                Color32[] px = SpriteSheet.ReadPixels(tex);
                int x0 = Mathf.FloorToInt(rc.x), y0 = Mathf.FloorToInt(rc.y), w = Mathf.RoundToInt(rc.width), h = Mathf.RoundToInt(rc.height);
                for (int row = h - 1; row >= 0 && float.IsNaN(v); row--)
                {
                    int i = (y0 + row) * tex.width + x0;
                    for (int c = 0; c < w; c++) if (px[i + c].a >= SolidAlpha) { v = (row + 1 - s.pivot.y) / s.pixelsPerUnit; break; }
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[ZenoniaProto] portrait head top: " + e.Message); }
            topOfSprite[s] = v;
            return v;
        }
    }
}

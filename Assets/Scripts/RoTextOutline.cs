using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The RO Soft look's thin dark outline for text with no backing (monster foot labels, player names over heads; docs/ui-layout-rules.md:
    /// "chữ thế giới không nền phải có viền tối 1-2 px"). Neither TextMesh nor IMGUI has an outline, so the text is drawn 8 times in dark
    /// navy, offset by Width around the glyphs, under the coloured copy. One table of offsets and one colour for every user, so the
    /// monster labels and the player names read as the same family.
    /// </summary>
    public static class RoTextOutline
    {
        /// <summary>Outline width in world units (view 216 units tall: ~1.5 px at 1080p at the default zoom).</summary>
        public const float Width = 0.3f;
        /// <summary>Dark navy ink, softer than pure black on the pastel maps.</summary>
        public static readonly Color Color = new Color(0.08f, 0.1f, 0.16f, 0.9f);
        /// <summary>The 8 unit offsets (4 axes + 4 diagonals) of the outline copies.</summary>
        public static readonly Vector2[] Dirs =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(0.71f, 0.71f), new Vector2(-0.71f, 0.71f), new Vector2(0.71f, -0.71f), new Vector2(-0.71f, -0.71f),
        };

        /// <summary>IMGUI label with the outline: 8 dark copies `px` pixels around `r`, then the text in `fill`. Leaves `style`'s colour at `fill`.</summary>
        public static void GuiLabel(Rect r, GUIContent text, GUIStyle style, Color fill, float px)
        {
            style.normal.textColor = Color;
            for (int i = 0; i < Dirs.Length; i++)
                GUI.Label(new Rect(r.x + Dirs[i].x * px, r.y - Dirs[i].y * px, r.width, r.height), text, style);
            style.normal.textColor = fill;
            GUI.Label(r, text, style);
        }
    }
}

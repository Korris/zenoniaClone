using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Art of the Adventure Paper kit: pieces cut from the ChatGPT sheets live in Resources/Sprites/ui/paper/{group}/{piece}.png
    /// (group = kit, hud, boss, bag ...). Sprites are built once from the textures with the 9-slice border of PaperArt.Borders.cs.
    /// Until a piece is delivered every widget draws a flat rounded plate in its theme colour instead (Plate), so screens can be built
    /// and captured before their art exists; a missing piece is logged once.
    /// </summary>
    public static partial class PaperArt
    {
        /// <summary>Native art pixels per canvas unit (the sheets are drawn about 2.5x the canvas size): keeps 9-slice corners at their drawn size.</summary>
        public const float PixelsPerUnit = 2.5f;
        const string Folder = "Sprites/ui/paper/";

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly HashSet<string> Missing = new HashSet<string>();
        static Sprite round;

        /// <summary>Sprite of a piece ("kit/btn-gold"), or null when the art is not there yet (logged once per piece).</summary>
        public static Sprite Get(string piece)
        {
            Sprite s;
            if (Cache.TryGetValue(piece, out s) && s != null) return s;
            if (Missing.Contains(piece)) return null;
            var tex = Resources.Load<Texture2D>(Folder + piece);
            if (tex == null)
            {
                Missing.Add(piece);
                Debug.LogWarning("[ZenoniaProto] paper UI piece not delivered yet, flat fallback: " + Folder + piece);
                return null;
            }
            Vector4 border;
            if (!Borders.TryGetValue(piece, out border) && piece.StartsWith("cards/")) Borders.TryGetValue(piece.Substring(6), out border); // the card kit (Sprites/ui/paper/cards/) is cut with borders keyed without the "cards/" folder
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            Cache[piece] = s;
            return s;
        }

        public static bool Has(string piece) { return Get(piece) != null; }

        /// <summary>
        /// Draws `piece` on `rt`: the art when delivered (9-sliced when it has a border), otherwise a rounded plate of `fill` with an `edge` rim
        /// (edge alpha 0 = no rim). piece null = always the plate. Returns the image on `rt`; change what it shows later with Redraw.
        /// </summary>
        public static Image Draw(RectTransform rt, string piece, Color fill, Color edge, float radius = 8f, bool raycast = false)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = raycast;
            Redraw(img, piece, fill, edge, radius);
            return img;
        }

        /// <summary>Flat rounded plate (veils, wells, markers that have no art of their own).</summary>
        public static Image Plate(RectTransform rt, Color fill, Color edge, float radius = 8f, bool raycast = false)
        {
            return Draw(rt, null, fill, edge, radius, raycast);
        }

        /// <summary>
        /// Changes what an image made by Draw shows (button disabled, tab active, slot rarity ...): the art of `piece` when it is delivered,
        /// else the plate in the new colours. True = art. Each state falls back on its own, so a half-delivered set still reads right.
        /// </summary>
        public static bool Redraw(Image img, string piece, Color fill, Color edge, float radius = 8f)
        {
            Sprite s = piece != null ? Get(piece) : null;
            Transform face = img.transform.Find(FaceName);
            if (s != null)
            {
                if (face != null) face.gameObject.SetActive(false);
                Apply(img, s);
                return true;
            }
            bool rim = edge.a > 0f; // the rim is this image; the face is a child 2 units smaller (3 below, like the board's buttons)
            SetRound(img, rim ? edge : fill, radius);
            if (!rim) { if (face != null) face.gameObject.SetActive(false); return false; }
            if (face == null)
            {
                RectTransform f = UiKit.Fill(img.transform, FaceName);
                f.offsetMin = new Vector2(2f, 3f); f.offsetMax = new Vector2(-2f, -2f);
                f.SetAsFirstSibling(); // under the captions / icons already on the widget
                f.gameObject.AddComponent<Image>().raycastTarget = false;
                face = f;
            }
            face.gameObject.SetActive(true);
            SetRound(face.GetComponent<Image>(), fill, Mathf.Max(1f, radius - 2f));
            return false;
        }

        /// <summary>Puts a delivered sprite on an image with the right slicing for its size.</summary>
        public static void Apply(Image img, Sprite s)
        {
            img.sprite = s;
            img.color = Color.white;
            if (s.border == Vector4.zero) { img.type = Image.Type.Simple; return; }
            img.type = Image.Type.Sliced;
            // Horizontal-only pieces (buttons, bars, strips) scale with the widget height; full frames keep their corners at the drawn size.
            float h = img.rectTransform.rect.height;
            bool horizontalOnly = s.border.y == 0f && s.border.w == 0f;
            img.pixelsPerUnitMultiplier = horizontalOnly && h > 0f ? s.rect.height / h : PixelsPerUnit;
        }

        static void SetRound(Image img, Color color, float radius)
        {
            img.sprite = Round();
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = RoundRadius / Mathf.Max(1f, radius);
            img.color = color;
        }

        const string FaceName = "Face";
        const int RoundSize = 64;
        const float RoundRadius = 24f;

        /// <summary>White anti-aliased rounded square (64 px, radius 24), 9-sliced: the only procedural shape of the fallbacks.</summary>
        public static Sprite Round()
        {
            if (round != null) return round;
            var px = new Color32[RoundSize * RoundSize];
            for (int y = 0; y < RoundSize; y++)
                for (int x = 0; x < RoundSize; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, RoundRadius, RoundSize - RoundRadius), cy = Mathf.Clamp(y + 0.5f, RoundRadius, RoundSize - RoundRadius);
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    px[y * RoundSize + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(RoundRadius - d + 0.5f)));
                }
            var tex = new Texture2D(RoundSize, RoundSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float b = RoundRadius + 2f;
            round = Sprite.Create(tex, new Rect(0, 0, RoundSize, RoundSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            return round;
        }

        /// <summary>Full-screen scene of a "place" screen (Resources/Sprites/ui/paper/{piece}): loaded on open, not cached; release with Unload.</summary>
        public static Texture2D LoadBackground(string piece) { return Resources.Load<Texture2D>(Folder + piece); }

        public static void Unload(Texture2D background) { if (background != null) Resources.UnloadAsset(background); }
    }
}

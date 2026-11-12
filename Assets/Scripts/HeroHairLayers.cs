using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Layered head for HeroHeadLayer: one shared bald face per facing plus hair layers per hairstyle, all 192 cells in the same frame space as
    /// the old full heads, so the head anchors are unchanged. Art (Sprites/classes/&lt;class&gt;/heads/):
    ///   face/&lt;dir&gt;.png            bald head (face, ears, scalp), drawn by the head renderer itself
    ///   hair/&lt;style&gt;/&lt;dir&gt;.png    hair in front of the face (gray, dyed by HairDye)
    ///   hair-back/&lt;style&gt;/&lt;dir&gt;.png optional hair behind the face (long hair, ponytail)
    ///   hair-under/&lt;style&gt;/&lt;dir&gt;.png optional hair behind the BODY (long locks below the chin that fall behind the collar and robe)
    /// Colours: Data/classes/&lt;class&gt;/hair-dyes.json ("styles": default dye per style, "presets": the dye cycle). Missing file = raw gray.
    /// The hair renderers are children of the head renderer, so they share its move, scale, rotation and mirror.
    /// </summary>
    public sealed class HeroHairLayers
    {
        [Serializable] class DyeFile { public string[] styles; public string[] presets; }

        const float FrontZ = -0.0004f, BackZ = 0.0004f; // in front of / behind the face, both still in front of the body

        readonly string root; // Sprites/classes/<class>/heads/
        readonly SpriteRenderer front, back, under;
        readonly Color32?[] styleDyes = new Color32?[0];
        readonly Color32[] presets = new Color32[0];

        /// <summary>Dye cycle index: -1 = the hairstyle's own colour, 0..N-1 = Data presets.</summary>
        public int Dye { get; set; } = -1;
        public int PresetCount { get { return presets.Length; } }

        /// <summary>Any colour (a colour picker, a saved look); wins over the Dye preset. null = none.</summary>
        public Color32? CustomDye { get; set; }

        HeroHairLayers(string root, string classRoot, SpriteRenderer head)
        {
            this.root = root;
            front = ProceduralSprites.Child(head.transform, "HairFront", null);
            front.transform.localPosition = new Vector3(0f, 0f, FrontZ);
            back = ProceduralSprites.Child(head.transform, "HairBack", null);
            back.transform.localPosition = new Vector3(0f, 0f, BackZ);
            under = ProceduralSprites.Child(head.transform, "HairUnder", null);
            under.transform.localPosition = new Vector3(0f, 0f, BackZ);
            var asset = Resources.Load<TextAsset>("Data/" + classRoot + "/hair-dyes");
            if (asset == null) return;
            try
            {
                DyeFile f = JsonUtility.FromJson<DyeFile>(asset.text);
                if (f != null && f.styles != null) styleDyes = Array.ConvertAll(f.styles, ParseColor);
                if (f != null && f.presets != null) presets = Array.ConvertAll(Array.FindAll(f.presets, p => ParseColor(p).HasValue), p => ParseColor(p).Value);
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] hair-dyes.json of " + classRoot + ": " + e.Message); }
        }

        /// <summary>Layered head of a class, or null when it has no heads/face/down + heads/hair/0/down art (the class keeps one full head per style).</summary>
        public static HeroHairLayers TryCreate(string classRoot, SpriteRenderer head)
        {
            string root = "Sprites/" + classRoot + "/heads/";
            if (SpriteSheet.TryLoad(root + "face/down") == null || SpriteSheet.TryLoad(root + "hair/0/down") == null) return null;
            return new HeroHairLayers(root, classRoot, head);
        }

        /// <summary>Number of hairstyles with a facing-down front hair layer (hair/0 .. hair/N-1), at most max.</summary>
        public int CountStyles(int max)
        {
            int n = 0;
            while (n < max && Cell("hair/" + n + "/down") != null) n++;
            return n;
        }

        /// <summary>Bald face for a facing (null = not painted, the caller may mirror the right one).</summary>
        public Sprite Face(string dir) { return Cell("face/" + dir); }

        /// <summary>Shows the hair of a style for the facing the face was drawn with (same dir name, same mirror).</summary>
        public void Show(int style, string dir, bool flipX, Color tint, int order)
        {
            Color32? dye = CustomDye.HasValue ? CustomDye : Dye >= 0 && Dye < presets.Length ? (Color32?)presets[Dye] : style < styleDyes.Length ? styleDyes[style] : null;
            Set(front, HairDye.Apply(Cell("hair/" + style + "/" + dir), dye), flipX, tint, order);
            Set(back, HairDye.Apply(Cell("hair-back/" + style + "/" + dir), dye), flipX, tint, order);
            Set(under, HairDye.Apply(Cell("hair-under/" + style + "/" + dir), dye), flipX, tint, order - 1); // one step below the body
        }

        public void Hide() { front.sprite = null; back.sprite = null; under.sprite = null; }

        static void Set(SpriteRenderer sr, Sprite sp, bool flipX, Color tint, int order)
        {
            sr.sprite = sp;
            sr.flipX = flipX;
            sr.color = tint;
            sr.sortingOrder = order;
        }

        readonly Dictionary<string, Sprite> cells = new Dictionary<string, Sprite>();

        Sprite Cell(string rel)
        {
            Sprite sp;
            if (!cells.TryGetValue(rel, out sp)) cells[rel] = sp = LoadCell(root + rel);
            return sp;
        }

        /// <summary>A whole 192 head cell with the body frame's bottom-centre pivot, or null when the PNG is missing.</summary>
        public static Sprite LoadCell(string path)
        {
            if (SpriteSheet.TryLoad(path) == null) return null;
            Sprite[][] g = SpriteSheet.Grid(path, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, new Vector2(0.5f, 0f));
            return g != null && g.Length > 0 && g[0].Length > 0 ? g[0][0] : null;
        }

        static Color32? ParseColor(string hex)
        {
            Color c;
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out c) ? (Color32?)(Color32)c : null;
        }
    }
}

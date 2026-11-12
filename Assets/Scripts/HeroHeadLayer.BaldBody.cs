using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Hair on the painted bald head (layered-hair classes with Data/classes/&lt;class&gt;/hair-anchors.json; Tools/build-wizard-hair-on-bald-body.py):
    /// the body keeps the bald head painted in every frame (its own face, mouth, tilt), nothing is cut off and no shared face is pasted on.
    /// Only the hair layers of HeroHairLayers move, onto the painted skull, by the frame's hair anchor (same fields as a head anchor).
    ///   body/bald/&lt;anim&gt;.png       the body with its bald head, same grid as body/bare
    ///   body/over-hair/&lt;anim&gt;.png  optional: sleeves and fists raised beside the head, drawn again above the hair
    /// Frames without a hair anchor or bald sheet (armour outfits, other classes) keep the headless body + head path of Apply.
    /// </summary>
    public sealed partial class HeroHeadLayer
    {
        const float OverHairZ = -0.002f; // in front of the hair (head -0.001, front hair -0.0004 more), same sorting order as the body

        readonly Dictionary<string, HeadFrame[]> hairAnchors = new Dictionary<string, HeadFrame[]>();
        readonly Dictionary<string, Sprite[][]> bodySheets = new Dictionary<string, Sprite[][]>(); // "bald/<anim>", "over-hair/<anim>"
        SpriteRenderer overHair;

        /// <summary>Custom hair colour (any colour, e.g. from a colour picker); null = the dye preset / the style's own colour. Layered hair only.</summary>
        public Color32? HairColor
        {
            get { return hair != null ? hair.CustomDye : null; }
            set { if (hair != null) hair.CustomDye = value; }
        }

        void LoadBaldBody(Transform parent)
        {
            if (hair == null) return;
            var asset = Resources.Load<TextAsset>("Data/" + root + "/hair-anchors");
            if (asset == null) return;
            try
            {
                HeadFile file = JsonUtility.FromJson<HeadFile>(asset.text);
                if (file != null && file.anims != null)
                    foreach (HeadAnim a in file.anims)
                        if (a != null && a.frames != null) hairAnchors[a.anim + "/" + a.dir] = a.frames;
            }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] hair-anchors.json of " + root + ": " + e.Message); }
            overHair = ProceduralSprites.Child(parent, "BodyOverHair", null);
            overHair.transform.localPosition = new Vector3(0f, 0f, OverHairZ);
        }

        Sprite[] BodySheetRow(string layer, string anim, int row)
        {
            string key = layer + "/" + anim;
            Sprite[][] grid;
            if (!bodySheets.TryGetValue(key, out grid))
            {
                string path = "Sprites/" + root + "/body/" + key;
                grid = SpriteSheet.TryLoad(path) != null ? SpriteSheet.Grid(path, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, new Vector2(0.5f, 0f)) : null;
                bodySheets[key] = grid;
            }
            return SpriteSheet.Row(grid, row);
        }

        /// <summary>The bald body sprite of a frame with its hair placed on the painted skull; null = the frame has no hair anchor or bald sheet.</summary>
        Sprite ApplyBaldBody(string anim, string dirName, int row, int frame, bool mirror, int bodyOrder, Color tint)
        {
            HeadFrame[] frames;
            if (!hairAnchors.TryGetValue(anim + "/" + dirName, out frames) || frame >= frames.Length || frames[frame] == null) return null;
            Sprite[] bodyRow = BodySheetRow("bald", anim, row);
            if (bodyRow == null || frame >= bodyRow.Length || bodyRow[frame] == null) return null;
            HeadFrame a = frames[frame];
            bool flipHair = dirName == "left" && hair.Face("left") == null; // left hair = the right hair mirrored (anchors are registered against it)
            bool flipX = mirror != flipHair;
            sr.sprite = null; // no pasted face: the body's own painted head shows
            Transform t = sr.transform;
            t.localPosition = new Vector3((mirror ? -a.x : a.x) * GearOverlays.FrameUnit, -a.y * GearOverlays.FrameUnit, NearerZ);
            t.localRotation = Quaternion.Euler(0f, 0f, mirror ? a.angle : -a.angle);
            float sc = a.s > 0f ? a.s : 1f;
            t.localScale = new Vector3(sc, sc * (a.sy > 0f ? a.sy : 1f), 1f);
            hair.Show(style, flipHair ? "right" : dirName, flipX, tint, bodyOrder);
            Sprite[] overRow = BodySheetRow("over-hair", anim, row);
            overHair.sprite = overRow != null && frame < overRow.Length ? overRow[frame] : null;
            overHair.flipX = mirror;
            overHair.color = tint;
            overHair.sortingOrder = bodyOrder;
            return bodyRow[frame];
        }
    }
}

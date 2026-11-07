#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Head as its own layer (idle in every facing and walk; every class with head art): the body draws a headless sheet and the head of the chosen hairstyle goes on top,
    /// scaled (sy) and moved per frame by Data/classes/&lt;class&gt;/head-anchors.json (frame px, y down: the breathing).
    /// Art: Sprites/classes/&lt;class&gt;/body/headless/&lt;anim&gt;.png (same grid as body/bare) and Sprites/classes/&lt;class&gt;/heads/&lt;style&gt;/&lt;dir&gt;.png,
    /// a full 192 cell in the body's frame space with the head where frame 0 has it (Tools/build-head-layer.py).
    /// A frame uses the layer only when it has an anchor, a headless sheet row and a head for the style; everything else keeps the whole body.
    /// </summary>
    public sealed class HeroHeadLayer
    {
        [Serializable] class HeadFrame { public float x; public float y; public float s; public float sy; public float angle; } // s: uniform scale, sy: extra vertical scale (breathing), both about the cell bottom, 0 = 1
        [Serializable] class HeadAnim { public string anim; public string dir; public HeadFrame[] frames; }
        [Serializable] class HeadFile { public HeadAnim[] anims; }

        const int MaxStyles = 10;
        const float NearerZ = -0.001f; // same sorting order as the body, drawn just in front of it (the front gear and the fist stay above)

        readonly SpriteRenderer sr;
        readonly string root; // e.g. classes/warrior
        readonly Dictionary<string, HeadFrame[]> anchors = new Dictionary<string, HeadFrame[]>();
        readonly Dictionary<string, Sprite[][]> headless = new Dictionary<string, Sprite[][]>();
        readonly Dictionary<string, Sprite> heads = new Dictionary<string, Sprite>(); // "style/dir" -> sprite, null = none painted
        int style;

        /// <summary>Number of hairstyles with a facing-down head (heads/0 .. heads/N-1).</summary>
        public int StyleCount { get; private set; }

        /// <summary>Chosen hairstyle; out of range wraps. Local look only for now (not saved, not networked).</summary>
        public int Style
        {
            get { return style; }
            set { style = StyleCount > 0 ? ((value % StyleCount) + StyleCount) % StyleCount : 0; }
        }

        HeroHeadLayer(Transform parent, string classRoot, HeadFile file)
        {
            root = classRoot;
            foreach (HeadAnim a in file.anims) if (a != null && a.frames != null) anchors[a.anim + "/" + a.dir] = a.frames;
            while (StyleCount < MaxStyles && HeadSprite(StyleCount, "down") != null) StyleCount++;
            sr = ProceduralSprites.Child(parent, "Head", null);
            sr.transform.localPosition = new Vector3(0f, 0f, NearerZ);
        }

        /// <summary>Head layer of a class, or null when it has no head-anchors.json or no heads/0 art (silently: the class draws its whole body).</summary>
        public static HeroHeadLayer Create(HeroClassDef cls, Transform parent)
        {
            if (cls == null || string.IsNullOrEmpty(cls.BareFolder) || !cls.BareFolder.EndsWith("/body/bare")) return null;
            string classRoot = cls.BareFolder.Substring(0, cls.BareFolder.Length - "/body/bare".Length);
            var asset = Resources.Load<TextAsset>("Data/" + classRoot + "/head-anchors");
            if (asset == null) return null;
            HeadFile file;
            try { file = JsonUtility.FromJson<HeadFile>(asset.text); }
            catch (Exception e) { Debug.LogError("[ZenoniaProto] head-anchors.json of " + classRoot + ": " + e.Message); return null; }
            if (file == null || file.anims == null) return null;
            var layer = new HeroHeadLayer(parent, classRoot, file);
            return layer.StyleCount > 0 ? layer : null;
        }

        Sprite HeadSprite(int s, string dir)
        {
            string key = s + "/" + dir;
            Sprite sp;
            if (heads.TryGetValue(key, out sp)) return sp;
            string path = "Sprites/" + root + "/heads/" + key;
            sp = null;
            if (SpriteSheet.TryLoad(path) != null)
            {
                Sprite[][] g = SpriteSheet.Grid(path, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, new Vector2(0.5f, 0f));
                sp = g != null && g.Length > 0 && g[0].Length > 0 ? g[0][0] : null;
            }
            heads[key] = sp;
            return sp;
        }

        Sprite[] HeadlessRow(string anim, int row)
        {
            Sprite[][] grid;
            if (!headless.TryGetValue(anim, out grid))
            {
                string path = "Sprites/" + root + "/body/headless/" + anim;
                grid = SpriteSheet.TryLoad(path) != null ? SpriteSheet.Grid(path, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, new Vector2(0.5f, 0f)) : null;
                headless[anim] = grid;
            }
            return SpriteSheet.Row(grid, row);
        }

        /// <summary>
        /// For a frame drawn with the Lv1 bare body: the headless body sprite to draw instead, and the head placed on top; null (head hidden) when the frame keeps the whole body.
        /// </summary>
        public Sprite Apply(string anim, string dirName, int row, int frame, bool mirror, int bodyOrder, Color tint)
        {
            HeadFrame[] frames;
            Sprite head = HeadSprite(style, dirName);
            bool flipHead = false;
            if (head == null && dirName == "left") { head = HeadSprite(style, "right"); flipHead = head != null; } // left: the right head mirrored (anchors are registered against it)
            Sprite[] bodyRow = head != null && anchors.TryGetValue(anim + "/" + dirName, out frames) && frame < frames.Length && frames[frame] != null ? HeadlessRow(anim, row) : null;
            if (bodyRow == null || frame >= bodyRow.Length || bodyRow[frame] == null) { Hide(); return null; }
            HeadFrame a = anchors[anim + "/" + dirName][frame];
            sr.sprite = head;
            sr.flipX = mirror != flipHead;
            sr.color = tint;
            sr.sortingOrder = bodyOrder;
            Transform t = sr.transform;
            t.localPosition = new Vector3((mirror ? -a.x : a.x) * GearOverlays.FrameUnit, -a.y * GearOverlays.FrameUnit, NearerZ);
            t.localRotation = Quaternion.Euler(0f, 0f, mirror ? a.angle : -a.angle); // clockwise in frame space, about the cell's bottom centre
            float sc = a.s > 0f ? a.s : 1f;
            t.localScale = new Vector3(sc, sc * (a.sy > 0f ? a.sy : 1f), 1f); // the sheet breathes by a vertical scale about the sole for most classes
            return bodyRow[frame];
        }

        public void Hide() { sr.sprite = null; }

        /// <summary>H in the editor and development builds: next hairstyle for the local hero. Temporary test key, read like DevBuffKey.</summary>
        public static bool CycleKeyPressed()
        {
            if (ReleaseBuild.IsRelease) return false; // test key like E / Q: on in every test build, off in a release
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.H);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb[Key.H].wasPressedThisFrame;
#else
            return false;
#endif
        }
    }
}

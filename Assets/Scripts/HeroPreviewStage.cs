using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Live full-body heroes for the character screens: the real HeroRenderer in starter gear, drawn like in the world (the layered hero with the
    /// character's hair / outfit colours when the class has layered art, else the paper doll), filmed by its own orthographic camera far outside
    /// every map (the hero select preview does the same, at another spot) into one RenderTexture that a RawImage shows. N slots side by side share
    /// the texture; slot i is the i-th column. Each slot stands idle (the layered body breathes in code) in one of the 8 view directions.
    /// The texture follows the main RawImage's size on screen, one texel per screen pixel, point sampled, so the sprites are filtered once like in
    /// the world (a fixed 16 px/unit texture stretched 1.5-2x by the canvas blurred the hero). Smaller views of the same texture (slot cards) read
    /// its mipmaps. The renderer needs no HeroController. Camera and heroes are on only while the owner screen is up (Active).
    /// </summary>
    public sealed class HeroPreviewStage : MonoBehaviour
    {
        /// <summary>World units of a slot's column (default), and of the view's height: a 192 px frame is 24 units, the feet stand 2 units above the bottom, a staff or a raised scale has the rest.</summary>
        public const float SlotW = 24f, ViewH = 30f, FeetFromBottom = 2f;
        const float StartPxPerUnit = 16f, IdleFps = 6f;
        const int MinTexH = 64, MaxTexSide = 4096;
        static readonly Vector3 Origin = new Vector3(-30000f, -26000f, 0f); // the hero select preview sits at (-30000, -30000)

        sealed class Entry
        {
            public HeroRenderer View;
            public HeroClassDef Def;
            public CharacterAppearanceData Look;
            public int Dir8;
        }

        readonly Dictionary<int, Entry> views = new Dictionary<int, Entry>(); // slot * 16 + class -> renderer, built on first use
        readonly Dictionary<int, Entry> shown = new Dictionary<int, Entry>();  // slot -> what the slot shows now
        readonly Vector3[] corners = new Vector3[4];
        RenderTexture texture;
        RawImage target;
        Camera cam;
        int slotCount;
        float slotW = SlotW;

        /// <summary>Aspect (width / height) of the whole texture, for sizing the RawImage that shows it.</summary>
        public float Aspect { get { return slotCount * slotW / ViewH; } }

        /// <summary>view: the biggest RawImage of the texture (its screen size sets the texture size); columnW: world units of a slot's column.</summary>
        public static HeroPreviewStage Create(Transform parent, RawImage view, int slots, float columnW = SlotW)
        {
            var go = new GameObject("HeroPreviewStage");
            go.transform.SetParent(parent, false);
            var stage = go.AddComponent<HeroPreviewStage>();
            stage.Init(view, slots, columnW);
            return stage;
        }

        void Init(RawImage view, int slots, float columnW)
        {
            slotCount = slots;
            slotW = columnW;
            target = view;
            int h = Mathf.RoundToInt(ViewH * StartPxPerUnit);
            texture = new RenderTexture(TexWidth(h), h, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, useMipMap = true, autoGenerateMips = true };
            target.texture = texture;
            target.raycastTarget = false;
            var camGo = new GameObject("HeroPreviewCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = Origin + new Vector3(slots * slotW * 0.5f, ViewH * 0.5f - FeetFromBottom, -10f);
            cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = ViewH * 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // the pedestal and banner behind show through
            cam.targetTexture = texture;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 30f;
            cam.enabled = false;
        }

        int TexWidth(int h) { return Mathf.Clamp(Mathf.RoundToInt(h * slotCount * slotW / ViewH), 1, MaxTexSide); }

        /// <summary>Resizes the texture (the same object, so every RawImage keeps it) to the main view's height in screen pixels (overlay canvas: world = screen px).</summary>
        void MatchScreenSize()
        {
            if (target == null || !target.isActiveAndEnabled) return;
            int widest = Mathf.FloorToInt(MaxTexSide * ViewH / (slotCount * slotW)); // keep the aspect when the width would pass the limit (many slots, 4K)
            int h = Mathf.Clamp(Mathf.RoundToInt(ViewScreenHeight / Mathf.Max(0.01f, target.uvRect.height)), MinTexH, Mathf.Min(MaxTexSide, widest));
            if (h == texture.height) return;
            texture.Release();
            texture.height = h;
            texture.width = TexWidth(h);
            texture.Create();
        }

        /// <summary>Camera on/off (also hides every hero): the screen is not up, nothing is drawn.</summary>
        public void SetActive(bool on)
        {
            cam.enabled = on;
            if (!on) Clear();
        }

        /// <summary>Empties every slot (a page change or a screen that hides).</summary>
        public void Clear()
        {
            foreach (Entry e in shown.Values) e.View.gameObject.SetActive(false);
            shown.Clear();
        }

        /// <summary>
        /// Puts a hero of a class in a slot with a look (null or not stored = the colours picked from `seed`, as the world would show that hero);
        /// scale grows it about its feet (the picked one is bigger).
        /// </summary>
        public void Set(int slot, HeroClass id, CharacterAppearanceData look, string seed, float scale)
        {
            int key = slot * 16 + (int)id;
            Entry e;
            if (!views.TryGetValue(key, out e))
            {
                HeroClassDef def = HeroClassDef.Get(id);
                var go = new GameObject("Preview_" + slot + "_" + def.Name);
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<HeroRenderer>();
                view.Init(def, HeldGearData.Load(def));
                PlayerState starter = PlayerState.NewGame(RpgSession.ClassId(def.Id));
                view.SetEquipmentGear(starter.GetEquipped(Slots.Weapon), starter.GetEquipped(Slots.Offhand));
                view.UseLayered(HeroRenderer.LayeredSetFor(def)); // the world's default style; false (paper doll) for a class without layered art
                e = views[key] = new Entry { View = view, Def = def };
            }
            Entry before;
            if (shown.TryGetValue(slot, out before) && before != e) before.View.gameObject.SetActive(false);
            shown[slot] = e;
            e.View.gameObject.SetActive(true);
            e.View.transform.position = Origin + new Vector3((slot + 0.5f) * slotW, 0f, 0f);
            e.View.transform.localScale = new Vector3(scale, scale, 1f);
            if (e.Look == null || look == null || !e.Look.SameLook(look))
            {
                e.Look = look != null ? look.Clone() : null;
                // local = false: the preview must not replace the playing hero's look (HeroRenderer.LocalLook, bag portrait); the crowd size variation it brings is a few percent.
                // classicHair: a class still on the paper doll shows the chosen palette hair colour too.
                e.View.SetAppearance(look, seed ?? "", false, true);
                if (look != null && look.stored) e.View.HairStyle = look.hairStyle;
            }
        }

        /// <summary>Turns a slot's hero (Dir8 order: 0 down, 2 right, 4 up, 6 left).</summary>
        public void SetPose(int slot, int dir8)
        {
            Entry e;
            if (shown.TryGetValue(slot, out e)) e.Dir8 = ((dir8 % 8) + 8) % 8;
        }

        /// <summary>The renderer a slot shows (capture scripts), or null.</summary>
        public HeroRenderer ViewIn(int slot)
        {
            Entry e;
            return shown.TryGetValue(slot, out e) ? e.View : null;
        }

        /// <summary>The texture's size in pixels (capture scripts).</summary>
        public Vector2Int TextureSize { get { return new Vector2Int(texture.width, texture.height); } }

        /// <summary>Height of the main view on screen in pixels (overlay canvas: world corners are screen pixels).</summary>
        public float ViewScreenHeight
        {
            get
            {
                if (target == null) return 0f;
                target.rectTransform.GetWorldCorners(corners);
                return corners[1].y - corners[0].y;
            }
        }

        /// <summary>Hairstyles the slot's class has layered art for (1 for the paper doll path: its old head styles are not offered).</summary>
        public static int HairStyleCount(HeroClass id)
        {
            string json = HeroRenderer.LayeredSetFor(HeroClassDef.Get(id));
            HeroLayeredSet set = json != null ? HeroLayeredSet.Load(json) : null;
            return set != null ? Mathf.Max(1, set.HairStyleCount) : 1;
        }

        /// <summary>True when the class's layered body keeps its painted outfit (RO Fantasy bodies): the creation screen offers no outfit colour.</summary>
        public static bool FixedOutfit(HeroClass id)
        {
            string json = HeroRenderer.LayeredSetFor(HeroClassDef.Get(id));
            HeroLayeredSet set = json != null ? HeroLayeredSet.Load(json) : null;
            return set != null && set.FixedOutfit;
        }

        void LateUpdate()
        {
            if (!cam.enabled) return;
            MatchScreenSize();
            foreach (Entry e in shown.Values) ShowIdle(e);
        }

        /// <summary>One frame of the slot's idle (a layered body breathes in code, HeroRenderer.Layered; its weapon, fist, hair and hat follow).</summary>
        static void ShowIdle(Entry e)
        {
            HeroRenderer v = e.View;
            v.Dir8 = e.Dir8;
            int dir4 = Dir8Util.ToCardinal(e.Dir8);
            int n = Mathf.Max(1, v.FrameCount("idle", dir4));
            v.Show("idle", dir4, (int)(Time.unscaledTime * IdleFps) % n, 0, 1f);
        }

        void OnDestroy() { if (texture != null) texture.Release(); }
    }
}

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
    /// the texture; slot i is the i-th column. Each slot has a pose: one of the 8 view directions and a motion (idle, walk loop, attack 1-2-3 loop).
    /// The renderer needs no HeroController. Camera and heroes are on only while the owner screen is up (Active).
    /// </summary>
    public sealed class HeroPreviewStage : MonoBehaviour
    {
        public enum Motion { Idle, Walk, Attack }

        /// <summary>World units of one slot's column, and of the view's height: a 192 px frame is 24 units, the feet stand 2 units above the bottom, a staff or a raised scale has the rest.</summary>
        public const float SlotW = 24f, ViewH = 30f, FeetFromBottom = 2f;
        const float PxPerUnit = 16f, IdleFps = 6f, WalkCycleSeconds = 0.6f, AttackFps = 11f, AttackRest = 0.5f;
        static readonly string[] DefaultCombo = { "attack1", "attack2", "attack3" }; // HeroSelectPreview's fallback for a class without ComboAnims
        static readonly Vector3 Origin = new Vector3(-30000f, -26000f, 0f); // the hero select preview sits at (-30000, -30000)

        sealed class Entry
        {
            public HeroRenderer View;
            public HeroClassDef Def;
            public CharacterAppearanceData Look;
            public int Dir8;
            public Motion Motion;
            public float Since; // unscaled time the motion started (the attack loop begins on its first frame)
        }

        readonly Dictionary<int, Entry> views = new Dictionary<int, Entry>(); // slot * 16 + class -> renderer, built on first use
        readonly Dictionary<int, Entry> shown = new Dictionary<int, Entry>();  // slot -> what the slot shows now
        RenderTexture texture;
        Camera cam;
        int slotCount;

        /// <summary>Aspect (width / height) of the whole texture, for sizing the RawImage that shows it.</summary>
        public float Aspect { get { return slotCount * SlotW / ViewH; } }

        public static HeroPreviewStage Create(Transform parent, RawImage target, int slots)
        {
            var go = new GameObject("HeroPreviewStage");
            go.transform.SetParent(parent, false);
            var stage = go.AddComponent<HeroPreviewStage>();
            stage.Init(target, slots);
            return stage;
        }

        void Init(RawImage target, int slots)
        {
            slotCount = slots;
            texture = new RenderTexture(Mathf.RoundToInt(slots * SlotW * PxPerUnit), Mathf.RoundToInt(ViewH * PxPerUnit), 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
            target.texture = texture;
            target.raycastTarget = false;
            var camGo = new GameObject("HeroPreviewCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = Origin + new Vector3(slots * SlotW * 0.5f, ViewH * 0.5f - FeetFromBottom, -10f);
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
            e.View.transform.position = Origin + new Vector3((slot + 0.5f) * SlotW, 0f, 0f);
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

        /// <summary>Turns a slot's hero (Dir8 order: 0 down, 2 right, 4 up, 6 left) and sets its motion; a new motion starts from its first frame.</summary>
        public void SetPose(int slot, int dir8, Motion motion)
        {
            Entry e;
            if (!shown.TryGetValue(slot, out e)) return;
            e.Dir8 = ((dir8 % 8) + 8) % 8;
            if (e.Motion != motion) { e.Motion = motion; e.Since = Time.unscaledTime; }
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

        void Update()
        {
            if (!cam.enabled) return;
            foreach (Entry e in shown.Values) ShowPose(e);
        }

        /// <summary>One frame of the slot's pose. Attack: the class's combo steps one after the other, then a short rest in idle.</summary>
        void ShowPose(Entry e)
        {
            HeroRenderer v = e.View;
            v.Dir8 = e.Dir8;
            int dir4 = Dir8Util.ToCardinal(e.Dir8);
            float t = Time.unscaledTime - e.Since;
            string anim = "idle";
            int frame;
            if (e.Motion == Motion.Walk && v.HasAnim("walk", dir4))
            {
                anim = "walk";
                int n = v.FrameCount(anim, dir4);
                frame = (int)(t / (WalkCycleSeconds / n)) % n;
            }
            else if (e.Motion == Motion.Attack && AttackFrame(v, e.Def.ComboAnims, dir4, t, out anim, out frame)) { }
            else
            {
                anim = "idle";
                int n = Mathf.Max(1, v.FrameCount(anim, dir4));
                frame = (int)(Time.unscaledTime * IdleFps) % n;
            }
            v.Show(anim, dir4, frame, 0, 1f);
        }

        static bool AttackFrame(HeroRenderer v, string[] combo, int dir4, float t, out string anim, out int frame)
        {
            anim = null; frame = 0;
            if (combo == null || combo.Length == 0) combo = DefaultCombo;
            int total = 0;
            foreach (string a in combo) total += Mathf.Max(1, v.FrameCount(a, dir4));
            float loop = total / AttackFps + AttackRest;
            int f = (int)((t % loop) * AttackFps);
            foreach (string a in combo)
            {
                int n = Mathf.Max(1, v.FrameCount(a, dir4));
                if (f < n) { anim = a; frame = f; return true; }
                f -= n;
            }
            return false; // the rest after the last step: idle
        }

        void OnDestroy() { if (texture != null) texture.Release(); }
    }
}

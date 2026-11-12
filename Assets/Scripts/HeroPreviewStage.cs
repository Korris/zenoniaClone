using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Live full-body heroes for the character screens: the real paper-doll HeroRenderer (starter gear, chosen hairstyle and colour), idle facing down, filmed by its own orthographic camera
    /// far outside every map (the hero select preview does the same, at another spot) into one RenderTexture that a RawImage shows. N slots side by side share the texture; slot i is the
    /// i-th column. The renderer needs no HeroController. Camera and heroes are on only while the owner screen is up (Active).
    /// </summary>
    public sealed class HeroPreviewStage : MonoBehaviour
    {
        /// <summary>World units of one slot's column, and of the view's height: a 192 px frame is 24 units, the feet stand 2 units above the bottom, a staff or a raised scale has the rest.</summary>
        public const float SlotW = 24f, ViewH = 30f, FeetFromBottom = 2f;
        const float PxPerUnit = 16f, IdleFps = 6f;
        static readonly Vector3 Origin = new Vector3(-30000f, -26000f, 0f); // the hero select preview sits at (-30000, -30000)

        sealed class Entry { public HeroRenderer View; public int Style = int.MinValue, Dye = int.MinValue; }

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

        /// <summary>Puts a hero of a class in a slot with a look; scale grows it about its feet (the picked one is bigger).</summary>
        public void Set(int slot, HeroClass id, int style, int dye, float scale)
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
                e = views[key] = new Entry { View = view };
            }
            Entry before;
            if (shown.TryGetValue(slot, out before) && before != e) before.View.gameObject.SetActive(false);
            shown[slot] = e;
            e.View.gameObject.SetActive(true);
            e.View.transform.position = Origin + new Vector3((slot + 0.5f) * SlotW, 0f, 0f);
            e.View.transform.localScale = new Vector3(scale, scale, 1f);
            if (e.Style != style) { e.View.HairStyle = style; e.Style = style; }
            if (e.Dye != dye) { e.View.HairDye = dye; e.Dye = dye; }
        }

        void Update()
        {
            if (!cam.enabled) return;
            foreach (Entry e in shown.Values)
            {
                int count = e.View.FrameCount("idle", 0);
                if (count > 0) e.View.Show("idle", 0, (int)(Time.unscaledTime * IdleFps) % count, 0, 1f);
            }
        }

        void OnDestroy() { if (texture != null) texture.Release(); }
    }
}

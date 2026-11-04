using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The hero in the bag window: a live paper-doll HeroRenderer breathing in its idle row facing down (PortraitDir), wearing the real weapon and offhand with their enhance effect, filmed far off the map by its own camera.
    /// The texture clears to transparent and is shown premultiplied (UiPremultiplied), so the additive enhance layers still light up without a backdrop.
    /// </summary>
    public sealed class HeroPortraitView : MonoBehaviour
    {
        const int TextureW = 672, TextureH = 528;      // twice the 336 x 264 box: the art is 8 px per world unit, so it never gets softer than the game
        const float CameraHalfHeight = 16f, FeetAboveBottom = 7f, IdleFps = 6f; // the Warrior's sword tip hangs below his feet
        const float WarriorShiftX = 0f; // the box is wide enough for his body to stand in the middle with the sword beside him
        // Facing down (the user wants the hero facing the player here, 2026-11-03). The worn weapon shows only where the idle-down row is a paper doll:
        // Warrior and Assassin; the Wizard, Mechanic and Cleric rows are painted art with a fixed weapon (anchor fallback) until they get a doll too.
        const int PortraitDir = 0; // HeroRenderer.DirNames: down
        static readonly Vector3 Stage = new Vector3(-34000f, -30000f, 0f); // far outside every map, apart from HeroSelectPreview's stage

        readonly HeroRenderer[] views = new HeroRenderer[HeroClassDef.All.Length];
        RenderTexture texture;
        Camera cam;
        HeroRenderer view;
        int frame;
        float clock;

        public void Init(RawImage image)
        {
            texture = new RenderTexture(TextureW, TextureH, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
            image.texture = texture;
            image.raycastTarget = false;
            Shader premultiplied = Resources.Load<Shader>("Shaders/UiPremultiplied");
            if (premultiplied != null) image.material = new Material(premultiplied) { name = "Ui_HeroPortrait" };
            var go = new GameObject("HeroPortraitCamera");
            go.transform.SetParent(transform, false);
            go.transform.position = Stage + new Vector3(0f, CameraHalfHeight - FeetAboveBottom, -10f);
            cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraHalfHeight;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.targetTexture = texture;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 30f;
            cam.enabled = false;
        }

        /// <summary>Dresses the hero of this class (built on first use) and starts filming.</summary>
        public void Show(HeroClassDef def, Item weapon, Item offhand)
        {
            int i = (int)def.Id;
            if (views[i] == null)
            {
                var go = new GameObject("Portrait_" + def.Name);
                go.transform.SetParent(transform, false);
                go.transform.position = Stage;
                views[i] = go.AddComponent<HeroRenderer>();
                views[i].Init(def, HeldGearData.Load(def));
            }
            if (view != null && view != views[i]) view.gameObject.SetActive(false);
            view = views[i];
            view.gameObject.SetActive(true);
            view.SetEquipmentGear(weapon, offhand);
            cam.transform.position = Stage + new Vector3(def.Id == HeroClass.Warrior ? WarriorShiftX : 0f, CameraHalfHeight - FeetAboveBottom, -10f);
            view.Show("idle", PortraitDir, frame, 0, 1f);
            cam.enabled = true;
        }

        public void Stop()
        {
            cam.enabled = false;
            if (view != null) view.gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (view == null || !cam.enabled) return;
            clock += dt;
            float each = 1f / IdleFps;
            if (clock < each) return;
            clock -= each;
            int count = view.FrameCount("idle", PortraitDir);
            frame = count > 0 ? (frame + 1) % count : 0;
            view.Show("idle", PortraitDir, frame, 0, 1f);
        }

        void OnDestroy() { if (texture != null) texture.Release(); }
    }
}

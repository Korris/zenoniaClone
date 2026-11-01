using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Live preview on the hero select: the real paper-doll HeroRenderer in starter gear on a painted ground, filmed far off the map by its own camera, so the player sees the hero as the
    /// game draws it (the painted heroes above are fancier than the sprites). Loops idle, walk and the combo. The floating companion is not shown here.
    /// </summary>
    public sealed class HeroSelectPreview : MonoBehaviour
    {
        const int TextureW = 436, TextureH = 274;
        const float CameraHalfHeight = 15f, FeetBelowCentre = 10f;
        const float IdleFps = 6f, WalkFps = 10f, AttackFps = 13f;
        const int GroundOrder = -100;
        static readonly Vector3 Stage = new Vector3(-30000f, -30000f, 0f); // far outside every map; the main camera never sees it

        struct Step { public string Anim; public int Dir; public int Loops; public float Fps; }

        readonly HeroRenderer[] views = new HeroRenderer[HeroClassDef.All.Length];
        readonly System.Collections.Generic.List<Step> steps = new System.Collections.Generic.List<Step>();
        RawImage target;
        RenderTexture texture;
        Camera cam;
        HeroRenderer view;
        int step, loop, frame;
        float clock;

        public void Init(RawImage image)
        {
            target = image;
            texture = new RenderTexture(TextureW, TextureH, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
            target.texture = texture;
            var go = new GameObject("HeroSelectPreviewCamera");
            go.transform.SetParent(transform, false);
            go.transform.position = Stage + new Vector3(0f, FeetBelowCentre, -10f);
            cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraHalfHeight;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.17f, 0.25f, 0.2f, 1f);
            cam.targetTexture = texture;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 30f;
            cam.enabled = false;

            Sprite groundArt = UiArt.Get("hero-select/preview-ground");
            if (groundArt == null) return;
            var ground = new GameObject("PreviewGround").AddComponent<SpriteRenderer>();
            ground.transform.SetParent(transform, false);
            ground.sprite = groundArt;
            ground.sortingOrder = GroundOrder;
            ground.transform.position = Stage + new Vector3(0f, FeetBelowCentre, 1f);
            float cover = Mathf.Max(CameraHalfHeight * 2f / groundArt.bounds.size.y, CameraHalfHeight * 2f * TextureW / TextureH / groundArt.bounds.size.x);
            ground.transform.localScale = new Vector3(cover, cover, 1f);
        }

        /// <summary>Switches to a class (built on first use, so the classes nobody looks at cost nothing).</summary>
        public void Show(HeroClassDef def)
        {
            if (view != null) view.gameObject.SetActive(false);
            steps.Clear();
            cam.enabled = true;
            int i = (int)def.Id;
            if (views[i] == null)
            {
                var go = new GameObject("Preview_" + def.Name);
                go.transform.SetParent(transform, false);
                go.transform.position = Stage;
                views[i] = go.AddComponent<HeroRenderer>();
                views[i].Init(def, HeldGearData.Load(def));
                PlayerState starter = PlayerState.NewGame(RpgSession.ClassId(def.Id));
                views[i].SetEquipmentGear(starter.GetEquipped(Slots.Weapon), starter.GetEquipped(Slots.Offhand));
            }
            view = views[i];
            view.gameObject.SetActive(true);
            BuildSteps(def);
            Replay();
        }

        /// <summary>Starts the loop over (the play button).</summary>
        public void Replay()
        {
            if (view == null || steps.Count == 0) return;
            step = 0; loop = 0; frame = 0; clock = 0f;
            view.Show(steps[0].Anim, steps[0].Dir, 0, 0, 1f);
        }

        public void Stop() { cam.enabled = false; }

        void BuildSteps(HeroClassDef def)
        {
            steps.Add(new Step { Anim = "idle", Dir = 0, Loops = 3, Fps = IdleFps });
            steps.Add(new Step { Anim = "walk", Dir = 3, Loops = 2, Fps = WalkFps });
            string[] combo = def.ComboAnims ?? new[] { "attack1", "attack2", "attack3" };
            foreach (string anim in combo) steps.Add(new Step { Anim = anim, Dir = 3, Loops = 1, Fps = AttackFps });
        }

        public void Tick(float dt)
        {
            if (view == null || steps.Count == 0) return;
            clock += dt;
            Step s = steps[step];
            float each = 1f / s.Fps;
            if (clock < each) return;
            clock -= each;
            frame++;
            int count = view.FrameCount(s.Anim, s.Dir);
            if (frame >= count)
            {
                frame = 0;
                if (++loop >= s.Loops)
                {
                    loop = 0;
                    step = (step + 1) % steps.Count;
                    s = steps[step];
                }
            }
            view.Show(s.Anim, s.Dir, frame, 0, 1f);
        }

        void OnDestroy() { if (texture != null) texture.Release(); }
    }
}

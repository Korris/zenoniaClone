using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Preview on the hero select: the live HeroRenderer in starter gear (the layered V3 body when the class has one, as in the world) idling, walking and attacking on a painted ground,
    /// filmed far off the map by its own camera. The old recorded clips (Resources/Video/hero-select/<class>, Tools/record-hero-previews.ps1) show the pre-V3 art, so they are only
    /// played when UseClips is on.
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

        VideoPlayer video;

        /// <summary>Plays the recorded clips instead of the live hero (off: they still show the pre-V3 heroes; re-record them before turning this on).</summary>
        public static bool UseClips = false;

        public void Init(RawImage image)
        {
            target = image;
            texture = new RenderTexture(TextureW, TextureH, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
            target.texture = texture;
            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = true;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = texture;
            video.audioOutputMode = VideoAudioOutputMode.None;
            video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
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

            Sprite groundArt = UiArt.Get("hero-select-ro/preview-ground") ?? UiArt.Get("hero-select/preview-ground"); // 12 % brighter copy (ChatGPT review), Tools/build-hero-select-ro-art.py ground
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
        public void Show(HeroClassDef def, CharacterAppearanceData look = null)
        {
            if (view != null) view.gameObject.SetActive(false);
            view = null;
            steps.Clear();
            VideoClip clip = UseClips ? Resources.Load<VideoClip>("Video/hero-select/" + def.Name.ToLowerInvariant()) : null;
            if (clip != null)
            {
                cam.enabled = false;
                video.clip = clip;
                video.Play();
                return;
            }
            video.Stop();
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
                views[i].UseLayered(HeroRenderer.LayeredSetFor(def)); // the world's default V3 look; false (paper doll) for a class without layered art
            }
            view = views[i];
            view.SetAppearance(look, "", false, true); // the picker's colours for the class, without touching the playing hero's look (as HeroPreviewStage)
            if (look != null && look.stored) view.HairStyle = look.hairStyle;
            view.gameObject.SetActive(true);
            BuildSteps(def);
            Replay();
        }

        /// <summary>Starts the loop over (the play button).</summary>
        public void Replay()
        {
            if (video.clip != null && video.isPlaying) { video.time = 0; return; }
            if (view == null || steps.Count == 0) return;
            step = 0; loop = 0; frame = 0; clock = 0f;
            view.Show(steps[0].Anim, steps[0].Dir, 0, 0, 1f);
        }

        public void Stop() { cam.enabled = false; video.Stop(); }

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
            int count = view.FrameCount(s.Anim, s.Dir);
            float each = s.Anim == "walk" ? 4f / (s.Fps * Mathf.Max(1, count)) : 1f / s.Fps; // walk: the same cycle length whatever the sheet's frame count (4 or 8 columns)
            if (clock < each) return;
            clock -= each;
            frame++;
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

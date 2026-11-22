using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The giant sword of the Warrior's EARTH BREAK as a real 3D model (22/11/2026, user: "sửa Earth Break đi, làm model kiếm 3D"; replaces
    /// WarriorPaintedSword, the flat painted sprite that lay on the ground and bent when the 2.5D / 3D camera turned). Mesh: EarthBreakGreatswordMesh
    /// (blade, fuller, crossguard, grip, pommel, gem) wearing the ChatGPT-painted front view (steel-ivory blade + gold, RO trailer Warrior palette) as
    /// its hand-painted texture; shader Shaders/GreatswordModel (halo, painted body, pulsing gold runes, gem glow, contact flash, golden dissolve).
    /// Lives under a ClericLightFx host (its clock, anchor = impact centre, crowd LOD; ends / clears with it). Timeline (ms of the host):
    ///  - t0 (~100 ms since 22/11 evening, user: "rơi quá nhanh làm không thể thấy cây kiếm đẹp"): summoned high above the centre out of an ivory
    ///    flash (fade 120 ms, shader flash 150 ms), hovers ~440 ms drifting slowly down with a gentle bob while it turns from 160 to 28 deg and
    ///    settles (ease-out-back), the runes gathering light;
    ///  - a short lift (anticipation), then the last `plungeMs` (~110) a fast plunge, the point hits the ground EXACTLY at `contactMs`
    ///    (EarthBreakUltimate's slam, 650), sinks a few units with a short recoil, soft ivory flash 50 ms (kept low so the shape stays readable), runes flare;
    ///  - stands stuck in the ground through the hits, burns away into golden light over the last `dissolveMs` before t1.
    /// View: flat 2D maps = the front view turned 28 deg on its long axis (bevels, guard depth and lighting show); 2.5D / 3D maps = the same, stood
    /// on the camera-facing plane like the heroes (View25D.Tilt), so any camera yaw / pitch sees it upright, at the old on-screen size
    /// (ClericLightFx.PictureRise). On a 3D map (Map3D) it is lit by the scene sun with the Map3D painterly ramp; elsewhere by a fixed key light from
    /// the upper left of the screen. The point sinks into the terrain (depth-tested), drawn in the FX band (sortingOrder) like the old sprite.
    /// </summary>
    public sealed class EarthBreakGreatsword3D : MonoBehaviour
    {
        const string Folder = "Sprites/classes/warrior/fx/earth-break/";
        const float RestTwist = 28f, StartTwist = 160f, HoverLow = 0.72f, LiftUp = 4f; // hover ends at 72 % of dropFrom, +4 units wind-up before the plunge

        static Material material;
        static bool tried;
        static MaterialPropertyBlock block;
        static int fadeId, dissolveId, flashId, boostId, sceneId, keyId, haloId;

        ClericLightFx host;
        MeshRenderer r;
        float t0, contact, t1, dissolveMs, height, sink, dropFrom, plunge;

        /// <summary>True when the shader and both textures are present (else Earth Break keeps the procedural mask sword).</summary>
        public static bool Ready { get { return Material != null; } }

        /// <summary>Loads the shader, art and mesh ahead of the first cast (WarriorLightLook.Load at class spawn) so the cast never hitches.</summary>
        public static void Preload() { if (Ready) EarthBreakGreatswordMesh.Get(); }

        static Material Material
        {
            get
            {
                if (tried) return material;
                tried = true;
                Shader sh = Resources.Load<Shader>("Shaders/GreatswordModel");
                Texture2D tex = Resources.Load<Texture2D>(Folder + "greatsword"), runes = Resources.Load<Texture2D>(Folder + "greatsword-runes");
                if (sh == null || !sh.isSupported || tex == null || runes == null)
                {
                    Debug.LogWarning("[ZenoniaProto] Earth Break 3D sword: shader or art missing, using the mask sword");
                    return null;
                }
                tex.wrapMode = TextureWrapMode.Clamp; runes.wrapMode = TextureWrapMode.Clamp;
                material = new Material(sh) { name = "EarthBreakGreatsword3D" };
                material.SetTexture("_MainTex", tex);
                material.SetTexture("_EmitTex", runes);
                block = new MaterialPropertyBlock();
                fadeId = Shader.PropertyToID("_Fade"); dissolveId = Shader.PropertyToID("_Dissolve"); flashId = Shader.PropertyToID("_Flash");
                boostId = Shader.PropertyToID("_RuneBoost"); sceneId = Shader.PropertyToID("_SceneLit"); keyId = Shader.PropertyToID("_KeyDir");
                haloId = Shader.PropertyToID("_HaloAlpha");
                return material;
            }
        }

        /// <summary>
        /// Lays the sword on `fx` (anchored on the impact centre): visible from `t0` to `t1`, starts `dropFrom` units up, point in the ground at
        /// `contactMs` after a `plungeMs` final plunge, burns away over the last `dissolveMs`. `height` = sword height in units on the flat view;
        /// the point sinks `sink` units.
        /// Gold motes rise off the blade while it stands. Returns false when the shader or art is missing.
        /// </summary>
        public static bool Lay(ClericLightFx fx, float t0, float t1, float height, float sink, float dropFrom, float contactMs, float dissolveMs, ClericLightLook.Pal glow,
            float plungeMs = 110f)
        {
            if (!Ready || fx == null) return false;
            var go = new GameObject("EarthBreakSword3D");
            go.transform.SetParent(fx.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = EarthBreakGreatswordMesh.Get();
            var s = go.AddComponent<EarthBreakGreatsword3D>();
            s.r = go.AddComponent<MeshRenderer>();
            s.r.sharedMaterial = Material;
            s.r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s.r.receiveShadows = false;
            s.r.enabled = false;
            s.host = fx; s.t0 = t0; s.t1 = t1; s.contact = contactMs; s.dissolveMs = Mathf.Max(1f, dissolveMs);
            s.height = height; s.sink = sink; s.dropFrom = dropFrom;
            s.plunge = Mathf.Clamp(plungeMs, 40f, Mathf.Max(40f, contactMs - t0 - 60f));

            float w = height * 0.42f; // golden motes rising off the blade's edges while it stands in the ground (no flames, RO trailer style)
            for (float t = contactMs + 60f; t < t1 - 60f; t += 140f)
                fx.At(t, f =>
                {
                    int n = f.Own ? 2 : 1;
                    for (int i = 0; i < n; i++)
                    {
                        float y = Random.Range(0.05f, 0.62f) * height, x = Random.Range(-0.24f, 0.24f) * w * (1f - y / height * 0.4f);
                        f.Emit(i == 0, f.Anchor + new Vector2(x, y - sink), new Vector2(Random.Range(-3f, 3f), Random.Range(18f, 32f)), Random.Range(1.2f, 2.2f),
                            i == 0 ? glow.Light : glow.Core, Random.Range(0.45f, 0.7f));
                    }
                });
            return true;
        }

        /// <summary>Lift (units above the ground) of the point at hover / plunge time `age` (&lt; contact): see the class summary.</summary>
        public static float LiftAt(float age, float t0, float contact, float plunge, float dropFrom)
        {
            float plungeAt = contact - plunge, low = dropFrom * HoverLow;
            if (age < plungeAt)
            {
                float u = Mathf.Clamp01((age - t0) / Mathf.Max(1f, plungeAt - t0));
                float ease = 1f - (1f - u) * (1f - u);                                   // slow drift down, easing out
                float bob = Mathf.Sin(u * Mathf.PI * 2f) * 1.6f * (1f - u);             // gentle float
                float wind = LiftUp * Mathf.SmoothStep(0f, 1f, (u - 0.8f) / 0.2f);      // rises a little just before the plunge
                return Mathf.Lerp(dropFrom, low, ease) + bob + wind;
            }
            float k = Mathf.Clamp01((age - plungeAt) / plunge);
            return (low + LiftUp) * (1f - k * k);                                        // accelerating plunge, 0 at contact
        }

        void LateUpdate()
        {
            if (host == null) return;
            float age = host.AgeMs;
            bool on = age >= t0 && age < t1 && FeelConfig.FxOn;
            r.enabled = on;
            if (!on) return;

            float lift, twist, hover = 0f;
            if (age < contact)
            {
                lift = LiftAt(age, t0, contact, plunge, dropFrom);
                float u = Mathf.Clamp01((age - t0) / Mathf.Max(1f, contact - plunge - t0)), v = u - 1f;
                float back = 1f + 2.2f * v * v * v + 1.2f * v * v;                     // ease-out-back: turns in, overshoots a little, settles
                twist = Mathf.LerpUnclamped(StartTwist, RestTwist, back);
                hover = u;
            }
            else
            {
                float k = (age - contact) / 110f;
                lift = k < 1f ? -3.5f * Mathf.Sin(Mathf.PI * k) * (1f - 0.5f * k) : 0f; // bites in, small recoil
                twist = RestTwist;
            }

            bool deep = ClericLightFx.Deep;
            Quaternion q = deep ? View25D.Tilt : Quaternion.identity;
            float rise = deep ? ClericLightFx.PictureRise : 1f;
            Vector2 a = host.Anchor;
            transform.position = new Vector3(a.x, a.y, 0f) + q * new Vector3(0f, (lift - sink) * rise, 0f);
            transform.rotation = q * Quaternion.Euler(0f, twist, 0f);
            transform.localScale = Vector3.one * (height * rise);
            r.sortingOrder = FeelConfig.FxOrder - 18; // the old sprite's slot in the FX band

            Camera cam = Camera.main;
            Vector3 key = cam != null ? cam.transform.TransformDirection(new Vector3(-0.55f, 0.62f, -0.56f)).normalized : new Vector3(-0.55f, 0.62f, -0.56f);
            // summon flash (born out of light) and a soft contact flash (0.45: the blade must stay readable through the slam)
            float flash = age < t0 + 150f ? 1f - (age - t0) / 150f : age >= contact && age < contact + 50f ? 0.45f * (1f - (age - contact) / 50f) : 0f;
            r.GetPropertyBlock(block);
            block.SetFloat(fadeId, Mathf.Clamp01((age - t0) / 120f));
            block.SetFloat(dissolveId, Mathf.Clamp01((age - (t1 - dissolveMs)) / dissolveMs));
            block.SetFloat(flashId, flash);
            block.SetFloat(boostId, age >= contact ? Mathf.Max(0f, 1f - (age - contact) / 450f) * 0.9f : 0.3f + 0.45f * hover); // runes gather light while it hovers
            block.SetFloat(sceneId, Map3D.Active ? 1f : 0f);
            block.SetVector(keyId, key);
            float halo = age < contact ? 1.35f : 1f; // a brighter rim while it hovers so it reads on snow
            block.SetFloat(haloId, halo * (host.Lod == FxTier.Own ? 1f : host.Lod == FxTier.Near ? 0.5f : 0.15f)); // the halo is soft glow: crowd LOD
            r.SetPropertyBlock(block);
        }
    }
}

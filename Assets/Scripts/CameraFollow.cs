using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Orthographic follow camera: smooth-damped, clamped to the map, with trauma-based screen shake (camera offset only).</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        const float SmoothTime = 0.09f;
        static float trauma, punch, punchDecay = PunchDecayPerSecond;
        const float PunchZoom = 0.09f, PunchDecayPerSecond = 2.2f; // ultimate slam: orthographic size shrinks up to 9% then eases back
        float baseSize;

        public Transform Target;
        public float TargetYOffset = 10f; // aim at the torso, not the feet
        Camera cam;
        Vector2 velocity;
        Vector2 pos;

        /// <summary>Adds shake trauma (0..1, clamped). Offset grows with trauma squared and decays linearly. No-op when effects are off.</summary>
        public static void AddTrauma(float amount)
        {
            if (!NetMode.RendersFx) return; // headless server: no cosmetics
            if (!FeelConfig.FxOn) return;
            trauma = Mathf.Min(1f, trauma + amount);
        }

        /// <summary>Camera punch-in (0..1): a quick zoom pulse that eases back, for ultimate impacts. No-op when effects are off.</summary>
        public static void Punch(float amount)
        {
            if (!NetMode.RendersFx) return; // headless server: no cosmetics
            if (!FeelConfig.FxOn) return;
            punchDecay = PunchDecayPerSecond;
            punch = Mathf.Min(1f, punch + amount);
        }

        /// <summary>Punch-in by a wanted zoom fraction (0.05 = view shrinks 5 %) that eases back over `ms` (skill choreography). Capped at the ultimate punch (9 %).</summary>
        public static void ZoomPulse(float zoomFraction, float ms)
        {
            if (!NetMode.RendersFx || !FeelConfig.FxOn || zoomFraction <= 0f) return;
            punch = Mathf.Max(punch, Mathf.Min(1f, Mathf.Sqrt(zoomFraction / PunchZoom)));
            punchDecay = punch / Mathf.Max(0.05f, ms / 1000f);
        }

        // Skills call these instead of AddTrauma / Punch / ZoomPulse: no screen shake or zoom jolt while casting (user decision 2026-11-03,
        // skills are spammed now). Combo hits, getting hurt and boss deaths still shake. Flip FeelConfig.SkillCameraShake to bring it back.
        public static void SkillShake(float amount) { if (FeelConfig.SkillCameraShake) AddTrauma(amount); }
        public static void SkillPunch(float amount) { if (FeelConfig.SkillCameraShake) Punch(amount); }
        public static void SkillZoomPulse(float zoomFraction, float ms) { if (FeelConfig.SkillCameraShake) ZoomPulse(zoomFraction, ms); }

        public static void ResetState() { trauma = 0f; punch = 0f; punchDecay = PunchDecayPerSecond; }

        public void Configure()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = baseSize = FitToMap(DevStartConfig.CameraViewHeight() * 0.5f * CameraWheelZoom.Current, cam.aspect); // dev-start.json zoom; PC wheel zoom (1 elsewhere)
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.09f, 0.13f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            if (View25D.Active) View25DCamera.Configure(cam); // 2.5D map (View25D.Maps): tilted perspective view
            else if (!cam.orthographic) View25DCamera.Restore(cam);
            if (Target != null) { pos = Clamp(TargetPos()); velocity = Vector2.zero; Apply(pos); }
        }

        const float BossFrameNear = 140f, BossFrameFar = 260f; // hero-to-boss distance: full framing bias inside Near, none beyond Far
        const float BossFrameShare = 0.5f;                     // aim halfway between the hero and the middle of the boss's painted body

        Vector2 TargetPos()
        {
            // 2.5D profile with a look-at height (RO3 Feel): screen centre on the hero's chest instead of a ground point north of the feet
            float yOffset = cam != null && !cam.orthographic && View25D.LookAtGroundOffset > 0f ? View25D.LookAtGroundOffset : TargetYOffset;
            var aim = new Vector2(Target.position.x, Target.position.y + yOffset);
            return aim + BossFraming(aim);
        }

        /// <summary>Near a live Wanted Boss the camera leans toward the middle of its painted body (5x a hero), so the whole boss and the ring of
        /// heroes around it fit on screen instead of the boss's head running off the top. Fades with distance; the hero always stays inside the
        /// middle 60 % of the view.</summary>
        Vector2 BossFraming(Vector2 aim)
        {
            Monster boss = Zone.Main != null ? Zone.Main.Wanted : null;
            if (boss == null || boss.IsDying || cam == null) return Vector2.zero;
            Vector2 mid = boss.Feet + new Vector2(0f, boss.DrawnHeight * 0.5f);
            float d = Vector2.Distance(boss.Feet, (Vector2)Target.position);
            float w = 1f - Mathf.Clamp01((d - BossFrameNear) / (BossFrameFar - BossFrameNear));
            if (w <= 0f) return Vector2.zero;
            Vector2 shift = (mid - aim) * (BossFrameShare * w);
            float halfH = cam.orthographicSize * 0.6f, halfW = halfH * cam.aspect;
            return new Vector2(Mathf.Clamp(shift.x, -halfW, halfW), Mathf.Clamp(shift.y, -halfH, halfH));
        }

        /// <summary>Largest orthographic half-height that keeps the view inside the current map (the wide default view on a small map such as
        /// Thornheart Chamber 24x16, or a narrow window): no empty void around the map.</summary>
        public static float FitToMap(float halfHeight, float aspect)
        {
            float fit = Mathf.Min(GameConfig.MapH * 0.5f, GameConfig.MapW * 0.5f / Mathf.Max(0.01f, aspect));
            return Mathf.Min(halfHeight, fit);
        }

        Vector2 Clamp(Vector2 p)
        {
            if (!cam.orthographic) return View25DCamera.Clamp(cam, p);
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            p.x = GameConfig.MapW <= halfW * 2f ? GameConfig.MapW * 0.5f : Mathf.Clamp(p.x, halfW, GameConfig.MapW - halfW);
            p.y = GameConfig.MapH <= halfH * 2f ? GameConfig.MapH * 0.5f : Mathf.Clamp(p.y, halfH, GameConfig.MapH - halfH);
            return p;
        }

        void Apply(Vector2 p)
        {
            if (!cam.orthographic) { View25DCamera.Place(cam, p); return; }
            transform.position = new Vector3(p.x, p.y, -10f);
        }

        void LateUpdate()
        {
            if (Target == null || cam == null) return;
            float dt = Time.deltaTime;
            if (punch > 0f)
            {
                punch = Mathf.Max(0f, punch - punchDecay * dt);
                cam.orthographicSize = baseSize * (1f - PunchZoom * punch * punch);
            }
            else cam.orthographicSize = baseSize = FitToMap(cam.orthographicSize, cam.aspect); // adopt any external zoom change while idle, never wider than the map
            Vector2 goal = Clamp(TargetPos());
            pos = Vector2.SmoothDamp(pos, goal, ref velocity, SmoothTime, Mathf.Infinity, dt);
            Vector2 shown = pos;
            if (trauma > 0f)
            {
                trauma = Mathf.Max(0f, trauma - FeelConfig.TraumaDecayPerSecond * dt);
                float t = Time.unscaledTime * FeelConfig.ShakeFrequency;
                var noise = new Vector2(Mathf.PerlinNoise(t, 0.37f) * 2f - 1f, Mathf.PerlinNoise(7.91f, t) * 2f - 1f);
                shown += noise * (trauma * trauma * FeelConfig.MaxShakeUnits);
            }
            Apply(shown);
        }
    }
}

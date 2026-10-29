using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Orthographic follow camera: smooth-damped, clamped to the map, with trauma-based screen shake (camera offset only).</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        const float SmoothTime = 0.09f;
        static float trauma, punch;
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
            if (!FeelConfig.FxOn) return;
            trauma = Mathf.Min(1f, trauma + amount);
        }

        /// <summary>Camera punch-in (0..1): a quick zoom pulse that eases back, for ultimate impacts. No-op when effects are off.</summary>
        public static void Punch(float amount)
        {
            if (!FeelConfig.FxOn) return;
            punch = Mathf.Min(1f, punch + amount);
        }

        public static void ResetState() { trauma = 0f; punch = 0f; }

        public void Configure()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = baseSize = GameConfig.ViewHeightUnits * 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.09f, 0.13f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            if (Target != null) { pos = Clamp(TargetPos()); velocity = Vector2.zero; Apply(pos); }
        }

        Vector2 TargetPos() { return new Vector2(Target.position.x, Target.position.y + TargetYOffset); }

        Vector2 Clamp(Vector2 p)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            p.x = GameConfig.MapW <= halfW * 2f ? GameConfig.MapW * 0.5f : Mathf.Clamp(p.x, halfW, GameConfig.MapW - halfW);
            p.y = GameConfig.MapH <= halfH * 2f ? GameConfig.MapH * 0.5f : Mathf.Clamp(p.y, halfH, GameConfig.MapH - halfH);
            return p;
        }

        void Apply(Vector2 p)
        {
            transform.position = new Vector3(p.x, p.y, -10f);
        }

        void LateUpdate()
        {
            if (Target == null || cam == null) return;
            float dt = Time.deltaTime;
            if (punch > 0f)
            {
                punch = Mathf.Max(0f, punch - PunchDecayPerSecond * dt);
                cam.orthographicSize = baseSize * (1f - PunchZoom * punch * punch);
            }
            else baseSize = cam.orthographicSize; // adopt any external zoom change while idle
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

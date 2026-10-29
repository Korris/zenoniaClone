using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Orthographic follow camera: smooth-damped, clamped to the map, with a decaying screen shake.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        const float SmoothTime = 0.09f;
        static float shakeStrength, shakeLeft;

        public Transform Target;
        public float TargetYOffset = 10f; // aim at the torso, not the feet
        Camera cam;
        Vector2 velocity;
        Vector2 pos;

        public static void Shake(float strength, float ms)
        {
            shakeStrength = Mathf.Max(shakeStrength, strength);
            shakeLeft = Mathf.Max(shakeLeft, ms / 1000f);
        }

        public static void ResetState() { shakeStrength = 0f; shakeLeft = 0f; }

        public void Configure()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = GameConfig.ViewHeightUnits * 0.5f;
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
            Vector2 goal = Clamp(TargetPos());
            pos = Vector2.SmoothDamp(pos, goal, ref velocity, SmoothTime, Mathf.Infinity, dt);
            Vector2 shown = pos;
            if (shakeLeft > 0f)
            {
                shakeLeft -= dt;
                shown += Random.insideUnitCircle * shakeStrength;
                if (shakeLeft <= 0f) shakeStrength = 0f;
            }
            Apply(shown);
        }
    }
}

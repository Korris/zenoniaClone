using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ground marker of a click-to-move goal (ClickMove): two thin pale rings on the goal point that pop in from 1.6x and pulse gently, gone once the
    /// hero is there (or the walk was cancelled). Same feathered ring mesh as the lock ring (TargetRingMesh), drawn on the walking plane: a real circle
    /// under the 2.5D / 3D camera, the flattened ellipse of the other ground marks on the 2D map. Created on the first click, lives with the session.
    /// </summary>
    public sealed class ClickMoveMarker : MonoBehaviour
    {
        const float Radius = 6f, PopSeconds = 0.22f, ReferenceScreenHeight = 860f, LinePx = 2f;
        static readonly Color Tint = new Color(0.86f, 0.96f, 1f, 0.95f);
        static ClickMoveMarker instance;

        TargetRingMesh outer, inner;
        float shownAt = -10f;

        /// <summary>A new goal was clicked: replay the pop-in.</summary>
        public static void Show()
        {
            if (instance == null) instance = new GameObject("ClickMoveMarker").AddComponent<ClickMoveMarker>();
            instance.shownAt = Time.unscaledTime;
        }

        void Awake()
        {
            outer = TargetRingMesh.Create(transform);
            inner = TargetRingMesh.Create(transform);
        }

        void OnDestroy() { if (instance == this) instance = null; }

        void LateUpdate()
        {
            Vector2 at;
            bool on = ClickMove.GroundGoal(out at);
            outer.Visible = on;
            inner.Visible = on;
            if (!on) return;
            Camera cam = Camera.main;
            float ortho = cam != null && cam.orthographic ? cam.orthographicSize : 90f * CameraWheelZoom.Current;
            float px = 2f * ortho / ReferenceScreenHeight;
            float k = Mathf.Clamp01((Time.unscaledTime - shownAt) / PopSeconds);
            float pop = Mathf.Lerp(1.6f, 1f, 1f - (1f - k) * (1f - k));
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 5f);
            float flat = View25D.Active ? 1f : 0.5f;
            var c = new Color(Tint.r, Tint.g, Tint.b, Tint.a * Mathf.Lerp(0.4f, 1f, k));
            int order = GameConfig.OrderBase(at.y);
            float r = Radius * pop;
            outer.Draw(new Vector3(at.x, at.y, -0.1f), r, r * flat, LinePx * px, px, c, order);
            float ri = r * 0.45f * pulse;
            inner.Draw(new Vector3(at.x, at.y, -0.1f), ri, ri * flat, LinePx * px, px, c, order);
        }
    }
}

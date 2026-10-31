using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Fixed joystick: the base stays in its corner; a touch on it moves the knob, which follows the drag inside the base radius, and the direction
    /// drives Up / Down / Left / Right (8-way, 22 % dead zone). The finger may leave the zone while it holds. Released, the knob recentres.
    /// Feeds the same keys as the keyboard, so gameplay code is unchanged.
    /// </summary>
    public sealed class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const float DeadZone = 0.22f, IdleAlpha = 0.92f;
        RectTransform stick, knob;
        CanvasGroup group;
        float radius;
        int pointer = int.MinValue;
        System.Action<bool, bool, bool, bool> output;

        /// <summary>This component sits on the invisible raycast area around the base; stick = base (child of that area); knob = child of stick.</summary>
        public void Init(RectTransform baseRt, RectTransform knobRt, float r, System.Action<bool, bool, bool, bool> setKeys)
        {
            stick = baseRt; knob = knobRt; radius = r; output = setKeys;
            group = stick.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = IdleAlpha;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId;
            group.alpha = 1f;
            Steer(e);
        }

        public void OnDrag(PointerEventData e) { if (e.pointerId == pointer) Steer(e); }

        public void OnPointerUp(PointerEventData e) { if (e.pointerId == pointer) Release(); }

        void OnDisable() { Release(); }

        void Steer(PointerEventData e)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(stick, e.position, e.pressEventCamera, out local)) return;
            Vector2 v = Vector2.ClampMagnitude(local, radius);
            knob.anchoredPosition = v;
            v /= radius;
            bool up = false, down = false, left = false, right = false;
            if (v.magnitude >= DeadZone)
            {
                // 8 sectors of 45 degrees: each axis is on when the direction is within 67.5 degrees of it.
                float a = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                right = Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 67.5f;
                up = Mathf.Abs(Mathf.DeltaAngle(a, 90f)) < 67.5f;
                left = Mathf.Abs(Mathf.DeltaAngle(a, 180f)) < 67.5f;
                down = Mathf.Abs(Mathf.DeltaAngle(a, -90f)) < 67.5f;
            }
            output(up, right, down, left);
        }

        void Release()
        {
            pointer = int.MinValue;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (group != null) group.alpha = IdleAlpha;
            if (output != null) output(false, false, false, false);
        }
    }
}

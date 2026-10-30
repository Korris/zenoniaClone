using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Floating joystick (Zenonia 5 style): touching anywhere in the left touch zone moves the base under the finger, the knob follows the
    /// drag inside the base radius and the direction drives Up / Down / Left / Right (8-way, 22 % dead zone). Released, the knob recentres
    /// and the base fades back to its rest spot in the corner. Feeds the same keys as the keyboard, so gameplay code is unchanged.
    /// </summary>
    public sealed class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const float DeadZone = 0.22f, IdleAlpha = 0.92f;
        RectTransform zone, stick, knob;
        CanvasGroup group;
        Vector2 rest;
        float radius;
        int pointer = int.MinValue;
        System.Action<bool, bool, bool, bool> output;

        /// <summary>zone = invisible raycast area; stick = base (child of zone) at its rest position; knob = child of stick.</summary>
        public void Init(RectTransform touchZone, RectTransform baseRt, RectTransform knobRt, float r, System.Action<bool, bool, bool, bool> setKeys)
        {
            zone = touchZone; stick = baseRt; knob = knobRt; radius = r; output = setKeys;
            rest = stick.anchoredPosition;
            group = stick.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = IdleAlpha;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out local)) return;
            pointer = e.pointerId;
            // The base jumps under the finger (kept fully on screen), anchored like the rest spot (bottom-left of the zone).
            Vector2 p = local - zone.rect.min;
            stick.anchoredPosition = new Vector2(Mathf.Max(p.x, radius), Mathf.Max(p.y, radius));
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
            if (stick != null) stick.anchoredPosition = rest;
            if (group != null) group.alpha = IdleAlpha;
            if (output != null) output(false, false, false, false);
        }
    }
}

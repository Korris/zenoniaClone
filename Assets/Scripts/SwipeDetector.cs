using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZenoniaProto
{
    /// <summary>
    /// Touch surface that tells a tap from a vertical swipe. Tracks ONE pointer id (multi-touch safe: other fingers on the joystick / attack
    /// are separate surfaces). A swipe fires as soon as the drag travels >= MinSwipe canvas units and is mostly vertical (onSwipe(+1) = up);
    /// a release without a swipe is a tap. Used by the skill-magazine slots and the thin strip around their arc.
    /// </summary>
    public sealed class SwipeDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float MinSwipe = 40f;
        public Action<int> onSwipe;   // +1 up, -1 down
        public Action onTap;          // released without a swipe
        public Action onPress, onRelease; // visual press feedback hooks (optional)
        public Func<bool> onHold; // optional repeat after 300 ms; true consumes the release tap
        float heldSince;
        bool holdUsed;
        int pointer = int.MinValue;
        Vector2 start;
        bool swiped;

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId; start = e.position; swiped = false;
            heldSince = Time.unscaledTime; holdUsed = false;
            if (onPress != null) onPress();
        }

        public void OnBeginDrag(PointerEventData e) { } // required so the EventSystem delivers OnDrag

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointer || swiped) return;
            Canvas c = GetComponentInParent<Canvas>();
            float scale = c != null && c.rootCanvas.scaleFactor > 0f ? c.rootCanvas.scaleFactor : 1f;
            Vector2 d = (e.position - start) / scale;
            if (Mathf.Abs(d.y) < MinSwipe || Mathf.Abs(d.y) < Mathf.Abs(d.x) * 1.5f) return; // too short, or sideways
            swiped = true;
            if (onRelease != null) onRelease(); // stop a farm hold immediately when it becomes a page swipe
            if (onSwipe != null) onSwipe(d.y > 0f ? 1 : -1);
        }

        public void OnEndDrag(PointerEventData e) { }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointer) return;
            bool tap = !swiped && !holdUsed;
            pointer = int.MinValue; swiped = false;
            if (onRelease != null) onRelease();
            if (tap && onTap != null) onTap();
        }

        void OnDisable()
        {
            if (pointer == int.MinValue) return;
            pointer = int.MinValue; swiped = false;
            if (onRelease != null) onRelease();
        }

        void Update()
        {
            if (Application.isFocused && pointer != int.MinValue && !swiped && onHold != null && Time.unscaledTime - heldSince >= 0.3f)
                holdUsed |= onHold();
        }

        void OnApplicationFocus(bool focused) { if (!focused) OnDisable(); }
    }
}

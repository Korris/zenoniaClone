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
        /// <summary>Optional drag-aim (Ground skills, 26/11): after AimHoldSeconds held still, onAimStart(screen) true = aiming; then drags go to onAimDrag
        /// (no page swipe), the release to onAimEnd (no tap), a lost pointer to onAimCancel.</summary>
        public Func<Vector2, bool> onAimStart;
        public Action<Vector2> onAimDrag, onAimEnd;
        public Action onAimCancel;
        public const float AimHoldSeconds = 0.15f;
        bool aiming;
        float heldSince;
        bool holdUsed;
        int pointer = int.MinValue;
        Vector2 start;
        bool swiped;

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId; start = e.position; swiped = false; aiming = false;
            heldSince = Time.unscaledTime; holdUsed = false;
            if (onPress != null) onPress();
        }

        public void OnBeginDrag(PointerEventData e) { } // required so the EventSystem delivers OnDrag

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointer || swiped) return;
            if (aiming) { if (onAimDrag != null) onAimDrag(e.position); return; }
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
            bool tap = !swiped && !holdUsed && !aiming;
            pointer = int.MinValue; swiped = false;
            if (onRelease != null) onRelease();
            if (aiming) { aiming = false; if (onAimEnd != null) onAimEnd(e.position); }
            if (tap && onTap != null) onTap();
        }

        void OnDisable()
        {
            if (pointer == int.MinValue) return;
            pointer = int.MinValue; swiped = false;
            if (onRelease != null) onRelease();
            if (aiming) { aiming = false; if (onAimCancel != null) onAimCancel(); }
        }

        void Update()
        {
            if (pointer != int.MinValue && !swiped && !aiming && onAimStart != null && Time.unscaledTime - heldSince >= AimHoldSeconds)
            {
                aiming = onAimStart(start);
                if (aiming) { holdUsed = true; if (onRelease != null) onRelease(); }
            }
            if (Application.isFocused && pointer != int.MinValue && !swiped && !aiming && onHold != null && Time.unscaledTime - heldSince >= 0.3f)
                holdUsed |= onHold();
        }

        void OnApplicationFocus(bool focused) { if (!focused) OnDisable(); }
    }
}

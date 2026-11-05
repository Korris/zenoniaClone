using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Fixed joystick: the base stays in its corner; a touch on it moves the knob, which follows the drag inside the base radius, and the direction
    /// drives Up / Down / Left / Right (8-way). The finger may leave the zone while it holds. Released, the knob recentres.
    /// Smoothness: the drag is read from the first pixel (no EventSystem drag threshold), the dead zone and the 8 sector edges both have hysteresis,
    /// so a thumb resting near the centre or sliding along a sector edge does not flicker the direction on and off.
    /// Feeds the same keys as the keyboard, so gameplay code is unchanged.
    /// </summary>
    public sealed class TouchJoystick : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
    {
        const float DeadZoneIn = 0.20f, DeadZoneOut = 0.12f; // start moving past 20 % of the radius, stop only back inside 12 %
        const float SectorDeg = 45f, SectorHysteresisDeg = 7f; // a new sector wins once the angle is 7 degrees past the shared edge
        const float IdleAlpha = 0.92f;
        RectTransform stick, knob;
        CanvasGroup group;
        float radius;
        int pointer = int.MinValue;
        int sector = -1; // 0 = right, counter-clockwise by 45 degrees; -1 = centred
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

        /// <summary>Drag from the first pixel: the default threshold swallows the first ~10 px of every thumb move.</summary>
        public void OnInitializePotentialDrag(PointerEventData e) { if (e.pointerId == pointer) e.useDragThreshold = false; }

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
            sector = PickSector(v, sector);
            Emit(sector);
        }

        /// <summary>8-way sector of `v` with hysteresis against `current`: inside the dead band the old answer stands, and the old sector keeps the direction until the angle is clearly in a neighbour.</summary>
        internal static int PickSector(Vector2 v, int current)
        {
            float mag = v.magnitude;
            if (mag < (current < 0 ? DeadZoneIn : DeadZoneOut)) return -1;
            float a = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (current >= 0 && Mathf.Abs(Mathf.DeltaAngle(a, current * SectorDeg)) < SectorDeg * 0.5f + SectorHysteresisDeg) return current;
            return ((int)Mathf.Round(a / SectorDeg) + 8) % 8;
        }

        void Emit(int s)
        {
            // Sector s covers the directions within 67.5 degrees of its centre, so each axis is on for its own sector and the two diagonals beside it.
            bool right = s == 0 || s == 1 || s == 7;
            bool up = s == 1 || s == 2 || s == 3;
            bool left = s == 3 || s == 4 || s == 5;
            bool down = s == 5 || s == 6 || s == 7;
            output(up, right, down, left);
        }

        void Release()
        {
            pointer = int.MinValue;
            sector = -1;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (group != null) group.alpha = IdleAlpha;
            if (output != null) output(false, false, false, false);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZenoniaProto
{
    /// <summary>
    /// Press surface of the Adventure Paper HUD controls. Two uses, like the old TouchButton / TapButton pair:
    ///   Hold(key)   attack, potions: the GameKey is held while the finger (or mouse) is down and pressed once on the way down
    ///   Tap(action) entry tags, More, fan blades, the page plaque: one action when the pointer is released over the control
    /// Tracks ONE pointer id, so another finger on the joystick never releases it. `onPressed` swaps the art to its pressed piece
    /// (true on down, false on release); without art the control squashes a little, as the old buttons did.
    /// </summary>
    public sealed class PaperHudPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        static readonly Vector3 Squash = new Vector3(0.94f, 0.94f, 1f);
        GameKey key;
        bool holdsKey;
        Action onTap;
        Action<bool> onPressed;
        int pointer = int.MinValue;

        public static PaperHudPress Hold(GameObject go, GameKey key, Action<bool> onPressed = null)
        {
            var p = go.AddComponent<PaperHudPress>();
            p.key = key; p.holdsKey = true; p.onPressed = onPressed;
            return p;
        }

        public static PaperHudPress Tap(GameObject go, Action onTap, Action<bool> onPressed = null)
        {
            var p = go.AddComponent<PaperHudPress>();
            p.onTap = onTap; p.onPressed = onPressed;
            return p;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue) return;
            pointer = e.pointerId;
            if (holdsKey) { TouchControls.SetHeld(key, true); TouchControls.Press(key); }
            transform.localScale = Squash;
            if (onPressed != null) onPressed(true);
        }

        public void OnPointerUp(PointerEventData e) { if (e.pointerId == pointer) Release(); }

        public void OnPointerClick(PointerEventData e) { if (onTap != null) onTap(); }

        void Release()
        {
            pointer = int.MinValue;
            if (holdsKey) TouchControls.SetHeld(key, false);
            transform.localScale = Vector3.one;
            if (onPressed != null) onPressed(false);
        }

        void OnDisable() { if (pointer != int.MinValue) Release(); }
    }
}

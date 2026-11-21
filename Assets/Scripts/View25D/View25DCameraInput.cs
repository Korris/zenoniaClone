using UnityEngine;
using UnityEngine.EventSystems;

namespace ZenoniaProto
{
    /// <summary>
    /// Player input for the 2.5D camera (View25DCameraControl, prototype 22/11/2026), on the gameplay camera; idle off the RO3 2.5D maps.
    /// PC: hold the RIGHT or MIDDLE mouse button and drag: up / down = tilt (pitch 45..70 deg), left / right = rotate (yaw ±28 deg, when Settings "Rotate camera" is on);
    /// wheel = zoom (CameraWheelZoom); PageUp / PageDown = tilt; comma / period = rotate; Home or a double-click of the middle button = reset (58 deg, yaw 0).
    /// Touch: two fingers on the world (not on the joystick / buttons): drag up / down = tilt, left / right = rotate, pinch = zoom.
    /// Mouse up tilts the view toward the horizon and mouse right turns it right, like a third-person camera (touch: the ground follows the fingers sideways);
    /// Settings "Invert camera drag" flips both axes.
    /// Ignored while a window owns the pointer / keyboard (chat typing, modal windows, inventory), same gate as the wheel zoom.
    /// </summary>
    public sealed class View25DCameraInput : MonoBehaviour
    {
        const float YawPerPx = 0.22f, PitchPerPx = 0.16f;     // degrees per screen pixel of drag at 720p (scaled by the screen height)
        const float KeyYawPerSec = 40f, KeyPitchPerSec = 30f;
        const float TouchYawPerPx = 0.18f, TouchPitchPerPx = 0.14f;
        const float DoubleClickSec = 0.3f;

        int dragButton = -1;
        Vector3 lastMouse;
        float lastMiddleDown = -1f;
        bool twoFinger;
        Vector2 lastMid;
        float lastSpan;

        void Update()
        {
            if (!View25DCameraControl.Applies) { dragButton = -1; twoFinger = false; return; }
            float dt = Time.unscaledDeltaTime;
            bool blocked = CameraWheelZoom.WindowOwnsPointer();
            bool turning = false;
            float s = 720f / Mathf.Max(1f, Screen.height), inv = View25DCameraControl.Invert ? -1f : 1f;

            turning |= Mouse(blocked, s, inv);
            turning |= Touches(blocked, s, inv);
            if (!KeysBlocked())
            {
                float k = 0f, p = 0f;
                if (Input.GetKey(KeyCode.Comma)) k += 1f;
                if (Input.GetKey(KeyCode.Period)) k -= 1f;
                if (Input.GetKey(KeyCode.PageUp)) p += 1f;
                if (Input.GetKey(KeyCode.PageDown)) p -= 1f;
                if (k != 0f) { View25DCameraControl.AddYaw(k * KeyYawPerSec * dt); turning = true; }
                if (p != 0f) View25DCameraControl.AddPitch(p * KeyPitchPerSec * dt);
                if (Input.GetKeyDown(KeyCode.Home)) View25DCameraControl.Reset();
            }
            View25DCameraControl.Tick(dt, turning);
        }

        /// <summary>The keyboard belongs to a window (chat typing, modal windows, inventory, hero change); the mouse being over the HUD does not count.</summary>
        static bool KeysBlocked() { return GameSession.Hero == null || UiWindows.BlocksGameplay || InventoryPanel.IsOpen || HeroChangeScreen.IsOpen || AccountScreenGui.MenuOpen; }

        /// <summary>Right / middle drag; returns true while the yaw is being turned.</summary>
        bool Mouse(bool blocked, float s, float inv)
        {
            if (dragButton >= 0 && !Input.GetMouseButton(dragButton)) dragButton = -1;
            if (dragButton < 0 && !blocked)
            {
                for (int b = 1; b <= 2; b++)
                {
                    if (!Input.GetMouseButtonDown(b)) continue;
                    if (b == 2)
                    {
                        if (lastMiddleDown >= 0f && Time.unscaledTime - lastMiddleDown < DoubleClickSec) { View25DCameraControl.Reset(); lastMiddleDown = -1f; break; }
                        lastMiddleDown = Time.unscaledTime;
                    }
                    dragButton = b;
                    lastMouse = Input.mousePosition;
                    break;
                }
            }
            if (dragButton < 0) return false;
            Vector3 m = Input.mousePosition, d = m - lastMouse;
            lastMouse = m;
            View25DCameraControl.AddYaw(-d.x * s * YawPerPx * inv);   // mouse right: yaw down = the view heading turns clockwise (right)
            View25DCameraControl.AddPitch(-d.y * s * PitchPerPx * inv); // mouse up: lower pitch, toward the horizon
            return true;
        }

        /// <summary>Two fingers on the world: mid-point drag = tilt / rotate, span change = pinch zoom.</summary>
        bool Touches(bool blocked, float s, float inv)
        {
            if (Input.touchCount != 2 || blocked) { twoFinger = false; return false; }
            Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
            Vector2 mid = (a.position + b.position) * 0.5f;
            float span = Vector2.Distance(a.position, b.position);
            if (!twoFinger)
            {
                EventSystem es = EventSystem.current;
                if (es != null && (es.IsPointerOverGameObject(a.fingerId) || es.IsPointerOverGameObject(b.fingerId))) return false; // a finger on the joystick / a button
                twoFinger = true;
                lastMid = mid;
                lastSpan = span;
                return false;
            }
            Vector2 d = mid - lastMid;
            lastMid = mid;
            View25DCameraControl.AddYaw(d.x * s * TouchYawPerPx * inv); // touch "grabs" the world: fingers right = the ground turns with them
            View25DCameraControl.AddPitch(-d.y * s * TouchPitchPerPx * inv);
            if (lastSpan > 20f && span > 20f) CameraWheelZoom.ZoomByFactor(lastSpan / span); // fingers apart = smaller view = closer
            lastSpan = span;
            return Mathf.Abs(d.x) > 0.01f;
        }
    }
}

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Names above other players only (online client and host), IMGUI over the world: no name on the local hero or on party members, so a name means a stranger.
    /// Crowd rule: names are placed nearest-to-your-hero first, a name that would overlap one already placed is skipped, and at most MaxNames show,
    /// so a pack of players never turns into a wall of text. A name never covers the ring under your own hero, and a name shown last frame
    /// keeps a head start (ShownBonus) so two players running past each other do not make the names flicker.
    /// Holding Alt shows every name (still nearest first, overlaps allowed).
    /// </summary>
    public class NameTags : MonoBehaviour
    {
        const int MaxNames = 8;
        const float ShownBonus = 40f; // world units: a name on screen last frame sorts as if this much nearer
        GUIStyle style;
        readonly List<HeroController> order = new List<HeroController>();
        readonly List<Rect> placed = new List<Rect>();
        readonly HashSet<HeroController> shown = new HashSet<HeroController>(), shownNow = new HashSet<HeroController>();
        readonly Dictionary<HeroController, float> sortKey = new Dictionary<HeroController, float>();

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || NetMode.IsOffline || TradeWindow.IsOpen) return; // IMGUI draws over the UGUI canvas: the tags would sit on the trade window's items
            Camera cam = Camera.main;
            Zone zone = Zone.Main;
            if (cam == null || zone == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            style.fontSize = Mathf.Max(10, Mathf.RoundToInt(Screen.height / 640f * 13f));

            HeroController me = LocalPlayer.Hero;
            Vector2 from = me != null ? me.Position : (Vector2)cam.transform.position;
            order.Clear();
            for (int i = 0; i < zone.Heroes.All.Count; i++)
            {
                HeroController h = zone.Heroes.All[i];
                if (h == null || h.IsDead || string.IsNullOrEmpty(h.PlayerName) || h == me || NetClient.Party.Contains(h.PlayerId)) continue;
                order.Add(h);
                sortKey[h] = Vector2.Distance(h.Position, from) - (shown.Contains(h) ? ShownBonus : 0f);
            }
            order.Sort((a, b) => sortKey[a].CompareTo(sortKey[b]));
            sortKey.Clear();

            bool all = ShowAllHeld();
            placed.Clear();
            shownNow.Clear();
            Rect ring = me != null ? HeroRingRect(cam, me.Position) : Rect.zero;
            for (int i = 0; i < order.Count; i++)
            {
                HeroController h = order[i];
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(h.Position.x, h.Position.y + 30f, 0f));
                if (sp.z < 0f || sp.x < -80f || sp.x > Screen.width + 80f) continue;
                Vector2 size = style.CalcSize(new GUIContent(h.PlayerName));
                var rect = new Rect(0f, 0f, size.x + 4f, size.y);
                rect.center = new Vector2(sp.x, Screen.height - sp.y);
                if (!all)
                {
                    if (placed.Count >= MaxNames) break;
                    if (Overlaps(rect) || rect.Overlaps(ring)) continue;
                }
                placed.Add(rect);
                shownNow.Add(h);
                style.normal.textColor = Color.black;
                var shadow = rect; shadow.x += 1f; shadow.y += 1f;
                GUI.Label(shadow, h.PlayerName, style);
                style.normal.textColor = Color.white;
                GUI.Label(rect, h.PlayerName, style);
            }
            shown.Clear();
            shown.UnionWith(shownNow);
        }

        /// <summary>Screen rect (GUI space) of the gold ring under your own hero (LocalHeroMarker: 22 x 11 units around the feet).</summary>
        static Rect HeroRingRect(Camera cam, Vector2 feet)
        {
            Vector3 a = cam.WorldToScreenPoint(new Vector3(feet.x - 11f, feet.y - 5.5f, 0f));
            Vector3 b = cam.WorldToScreenPoint(new Vector3(feet.x + 11f, feet.y + 5.5f, 0f));
            return Rect.MinMaxRect(a.x, Screen.height - b.y, b.x, Screen.height - a.y);
        }

        bool Overlaps(Rect r)
        {
            for (int i = 0; i < placed.Count; i++) if (placed[i].Overlaps(r)) return true;
            return false;
        }

        /// <summary>PC: either Alt key held.</summary>
        static bool ShowAllHeld()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb[Key.LeftAlt].isPressed || kb[Key.RightAlt].isPressed);
#else
            return false;
#endif
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Top-right of the RO Soft HUD (02-hud.png) above and on the round minimap: the navy map-name pill (pin + the map's name) and the
    /// teal connection pill (signal icon + the round trip "42ms" online, "Offline" offline; the game has no channels, so no "Ch.1"),
    /// and the blue "+" on the ring's lower right, which steps the minimap zoom. The minimap itself (ground, dots, ring) is
    /// Minimap.RoRound.cs. Pills are RoHud.Pill boxes (padding, icon gap, width from the text).
    /// </summary>
    public sealed class RoHudMapPanel
    {
        const float PlusSize = 30f;
        readonly Text mapName, signal;
        readonly RectTransform signalRt, mapRt;

        /// <summary>Width of both pills with their gap (the menu row keeps left of them).</summary>
        public float PillsWidth { get { return signalRt.rect.width + 4f + mapRt.rect.width; } }
        object shownMap;
        int shownRtt = int.MinValue;
        float wait;

        public RoHudMapPanel(RectTransform safe)
        {
            RectTransform signalPill, mapPill;
            signal = RoHud.Pill(safe, "SignalPill", RoHud.TopRight, RoHud.TopRight, -RoHud.Margin, -RoHud.PillTop, RoHud.PillH, "hud-channel-pill", "hud-icon-signal", 14f, "Offline", 12, Color.white, out signalPill);
            mapName = RoHud.Pill(safe, "MapPill", RoHud.TopRight, RoHud.TopRight, -RoHud.Margin, -RoHud.PillTop, RoHud.PillH, "hud-map-pill", "hud-icon-pin-white", 14f, "", 12, Color.white, out mapPill, 70f);
            // the map pill sits left of the signal pill: re-anchored every layout pass by a tiny follower
            mapPill.gameObject.AddComponent<LeftOf>().target = signalPill;
            signalRt = signalPill; mapRt = mapPill;

            float c = RoHud.MinimapRight + RoHud.MinimapSize * 0.5f, off = RoHud.MinimapSize * 0.5f * 0.707f; // the ring's lower-right point (-45 deg)
            RectTransform plus = RoHud.Box(safe, "MinimapPlus", RoHud.TopRight, -(c - off), -(RoHud.MinimapTop + RoHud.MinimapSize * 0.5f + off), PlusSize, PlusSize);
            RoHud.Art(plus, "hud-btn-plus", true);
            PaperHudPress.Tap(plus.gameObject, Minimap.CycleZoom);
            // PC: the big map's key on the ring's lower-left (a tap on the minimap opens it too, WorldMapWindow)
            RoHud.KeyCap(safe, RoHud.TopRight, -(c + off), -(RoHud.MinimapTop + RoHud.MinimapSize * 0.5f + off), InputAdapter.KeyLabel(GameKey.WorldMap));
        }

        /// <summary>Keeps a top-right pill just left of another one whose width follows its text.</summary>
        sealed class LeftOf : MonoBehaviour
        {
            public RectTransform target;
            void LateUpdate()
            {
                var rt = (RectTransform)transform;
                float x = target.anchoredPosition.x - target.rect.width - 4f;
                if (Mathf.Abs(rt.anchoredPosition.x - x) > 0.1f) rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
            }
        }

        public void Tick(float dt)
        {
            IGameMap map = MapRegistry.Current;
            if (map != shownMap) { shownMap = map; mapName.text = map != null ? map.Name : ""; }
            wait -= dt;
            if (wait > 0f) return;
            wait = 1f; // the ping is measured once a second
            int rtt = NetMode.IsOffline ? -2 : NetClient.RttMs;
            if (rtt == shownRtt) return;
            shownRtt = rtt;
            signal.text = rtt == -2 ? "Offline" : rtt < 0 ? "..." : rtt + "ms";
        }
    }
}

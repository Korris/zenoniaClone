using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Minimap: the round look of the RO Soft HUD (02-hud.png). While RoHud.Active the square panel becomes a circle under the map /
    /// connection pills: the same baked ground and dots, inside the ChatGPT ring's round mask, centred on the hero and zoomed (Zoom; the
    /// HUD's "+" button cycles it). The dots keep their map-space places, so only the ground's rect moves. Switching the look back
    /// restores the square panel.
    /// </summary>
    public sealed partial class Minimap
    {
        static readonly float[] Zooms = { 1.6f, 2.4f, 1f };
        static int zoomIndex;
        static readonly Color RoundBack = new Color(0.86f, 0.9f, 0.8f, 1f); // past the map's edge: a soft meadow plate, not a hole

        RectTransform roundMask;
        Image squareFrame;
        GameObject ringGo;
        bool round;

        /// <summary>Zoom of the round minimap (map units per minimap unit).</summary>
        public static float Zoom { get { return Zooms[zoomIndex]; } }

        /// <summary>The "+" button of the RO HUD: next zoom step (wraps).</summary>
        public static void CycleZoom() { zoomIndex = (zoomIndex + 1) % Zooms.Length; }

        void TickRoRound(HeroController hero)
        {
            bool want = RoHud.Active;
            if (want != round) SetRound(want);
            if (!round) return;
            RectTransform g = ground.rectTransform;
            float z = Zoom;
            if (g.localScale.x != z) g.localScale = new Vector3(z, z, 1f);
            // The hero at the centre of the circle, except near the map's edges: the view stops there so the map always fills the circle
            // (user 20/11: past the edge the circle showed a big empty plate) and the hero's dot moves off centre instead.
            Vector2 p = -MapPoint(hero.Position) * z;
            g.anchoredPosition = new Vector2(KeepInside(p.x, InnerW * z), KeepInside(p.y, InnerH * z));
            KeepDotSize(g, z);
        }

        // Radius of the visible disc: hud-minimap-mask's disc is 233 of its 291 px, drawn over the whole MinimapSize box (1 px under the ring).
        static readonly float DiscRadius = RoHud.MinimapSize * 0.5f * 233f / 291f + 1f;

        /// <summary>Ground offset (its left / bottom edge from the circle's centre) clamped so the ground of length `len` covers the disc.</summary>
        static float KeepInside(float offset, float len)
        {
            if (len < DiscRadius * 2f) return -len * 0.5f; // smaller than the circle (cannot happen at zoom >= 1): centred on the plate
            return Mathf.Clamp(offset, DiscRadius - len, -DiscRadius);
        }

        readonly System.Collections.Generic.List<Image> dotImages = new System.Collections.Generic.List<Image>();
        float dotScanLeft;

        /// <summary>The zoom scales the ground and its dots alike: dots are scaled back so they keep their normal size (pools grow, so rescanned twice a second).</summary>
        void KeepDotSize(RectTransform g, float z)
        {
            dotScanLeft -= Time.unscaledDeltaTime;
            if (dotScanLeft <= 0f) { dotScanLeft = 0.5f; g.GetComponentsInChildren(true, dotImages); }
            Vector3 s = new Vector3(1f / z, 1f / z, 1f);
            for (int i = 0; i < dotImages.Count; i++)
            {
                Image d = dotImages[i];
                if (d != null && d.transform.localScale != s) d.transform.localScale = s;
            }
        }

        void SetRound(bool on)
        {
            round = on;
            RectTransform g = ground.rectTransform;
            if (squareFrame == null) squareFrame = root.GetComponent<Image>();
            if (on)
            {
                if (roundMask == null)
                {
                    // The mask disc fills the box exactly like the ring art (same sprite size, both preserveAspect): its edge sits under the ring's
                    // band. An inset of 9 left a 6 px band between the disc and the ring where the world showed through (user 20/11).
                    roundMask = UiKit.Fill(root, "RoundMask");
                    Image m = RoHud.Art(roundMask, "hud-minimap-mask");
                    m.color = RoundBack;
                    roundMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                    RectTransform ring = UiKit.Fill(root, "Ring");
                    RoHud.Art(ring, "hud-minimap-ring");
                    ringGo = ring.gameObject;
                    NameTags.Occlude(roundMask); // IMGUI name tags never draw over the round minimap
                }
                roundMask.gameObject.SetActive(true);
                ringGo.SetActive(true);
                g.SetParent(roundMask, false);
                g.anchorMin = g.anchorMax = new Vector2(0.5f, 0.5f);
                g.pivot = Vector2.zero;
                g.sizeDelta = new Vector2(InnerW, InnerH);
                root.sizeDelta = new Vector2(RoHud.MinimapSize, RoHud.MinimapSize);
                root.anchoredPosition = new Vector2(-(RoHud.MinimapRight + RoHud.MinimapSize * 0.5f), -(RoHud.MinimapTop + RoHud.MinimapSize * 0.5f));
                if (squareFrame != null) squareFrame.enabled = false;
                ringGo.transform.SetAsLastSibling();
            }
            else
            {
                g.SetParent(root, false);
                g.SetSiblingIndex(0);
                g.anchorMin = Vector2.zero; g.anchorMax = Vector2.one; g.pivot = new Vector2(0.5f, 0.5f);
                g.offsetMin = new Vector2(FrameInset, FrameInset); g.offsetMax = new Vector2(-FrameInset, -FrameInset);
                g.localScale = Vector3.one;
                foreach (Image d in dotImages) if (d != null) d.transform.localScale = Vector3.one;
                root.sizeDelta = new Vector2(OuterW, OuterH);
                root.anchoredPosition = new Vector2(-(RightMargin + OuterW * 0.5f), -(TopOffset + OuterH * 0.5f));
                if (squareFrame != null) squareFrame.enabled = true;
                if (roundMask != null) { roundMask.gameObject.SetActive(false); ringGo.SetActive(false); }
            }
        }
    }
}

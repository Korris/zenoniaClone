using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Self-contained poison badge for a HUD anchor (2026-11-19): the ChatGPT drop icon (StatusIcons.Poison) with the seconds left at its
    /// lower right, shown while hero.PoisonMsLeft &gt; 0 (replicated: NetEventType.HeroStatus). Attach it to any RectTransform anchor, e.g. the
    /// RO Soft HUD's party row anchor (RoHudParty.StatusIconsOf), and call Show(hero) every frame (null hides it). The local player's own
    /// poison shows in the buff row under the HP bar (HudBuffRow), which RoHudStatus.StatusIcons already hosts.
    /// </summary>
    public sealed class PoisonStatusIcon
    {
        readonly GameObject root;
        readonly Text seconds;
        int shownSeconds = -1;

        /// <summary>`size` = icon height in canvas units; the badge sits at the anchor's top-left.</summary>
        public PoisonStatusIcon(RectTransform anchor, float size)
        {
            RectTransform rt = UiKit.Rect(anchor, "Poison", 0f, 0f, size, size);
            root = rt.gameObject;
            RoHud.Art(rt, "slot"); // the same ivory cell as the buff row under the HP bar
            Image icon = UiKit.Fill(rt, "Icon", 2f).gameObject.AddComponent<Image>();
            icon.sprite = StatusIcons.Poison;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            seconds = HudBuffRow.RoBadge(rt); // "6s" in a navy badge on the lower-right corner, not over the drop
            root.SetActive(false);
        }

        public void Show(HeroController hero)
        {
            float ms = hero != null && StatusIcons.Poison != null ? hero.PoisonMsLeft : 0f;
            bool on = ms > 0f;
            if (root.activeSelf != on) root.SetActive(on);
            if (!on) { shownSeconds = -1; return; }
            int s = Mathf.CeilToInt(ms / 1000f);
            if (s != shownSeconds) { shownSeconds = s; seconds.text = s + "s"; }
        }
    }
}

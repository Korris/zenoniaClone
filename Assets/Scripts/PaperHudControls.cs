using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The controls of the compact HUD, built by TouchControls the first time UiScreens.UseNew(UiScreen.Hud) is on and shown instead
    /// of its old layer: the menu icon row under the minimap (HudEntryButtons) and, by HudLayout, either the touch pad with the skill
    /// arc (PaperTouchPad) or the PC hotbar with the EXP line (PaperHotbar). Everything sits in its own safe-area root, just above the
    /// old touch layer: under the status block, the toasts and every window.
    /// </summary>
    public sealed class PaperHudControls
    {
        readonly RectTransform root;
        readonly PaperTouchPad pad;     // mobile layout
        readonly PaperHotbar hotbar;    // PC layout
        public readonly HudEntryButtons Entry;

        public PaperHudControls(RectTransform oldLayer)
        {
            PaperLayout layout = PaperLayout.CreateSafeArea(oldLayer.parent, "PaperHudControls");
            root = layout.Root;
            root.SetSiblingIndex(oldLayer.GetSiblingIndex() + 1);
            if (HudLayout.IsWeb) hotbar = new PaperHotbar(layout.Safe);
            else pad = new PaperTouchPad(layout.Safe);
            Entry = new HudEntryButtons(layout.Safe); // last: the "..." strip and its veil lie over the other controls
            root.gameObject.SetActive(false);
        }

        public bool Visible { get { return root.gameObject.activeSelf; } }

        public void SetVisible(bool on) { root.gameObject.SetActive(on); }

        /// <summary>Next / previous page of the skill magazine (Tab, a swipe, capture scripts).</summary>
        public void CyclePage(int dir) { if (pad != null) pad.CyclePage(dir); else hotbar.CyclePage(dir); }

        public PaperPotionButton Potion(bool hp) { return pad != null ? pad.Potion(hp) : hotbar.Potion(hp); }

        /// <summary>The control named `name` anywhere in the layer (capture scripts tap it through the EventSystem); null when there is none.</summary>
        public Transform Find(string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        /// <summary>Per frame while a hero plays. Hidden (a window is open): only the badges are kept up to date.</summary>
        public void Tick(HeroController hero, float dt)
        {
            bool visible = Visible;
            Entry.Tick(hero.Rpg, visible, dt);
            if (!visible) return;
            if (pad != null) pad.Tick(hero, dt); else hotbar.Tick(hero, dt);
        }
    }
}

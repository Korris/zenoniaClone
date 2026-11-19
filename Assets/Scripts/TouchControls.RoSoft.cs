using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// TouchControls: the RO Soft Fantasy HUD (RoHud.Active, 02-hud.png). Read every frame like the paper look: the first time it is on,
    /// RoHudControls is built next to the old layer; from then on whichever look is on is the one shown. Input rules are untouched.
    /// </summary>
    public partial class TouchControls
    {
        RoHudControls ro;

        /// <summary>The RO controls, once built (capture scripts); null before.</summary>
        internal static RoHudControls Ro { get { return instance != null ? instance.ro : null; } }

        bool UseRo()
        {
            if (!RoHud.Active) return false;
            if (ro == null) ro = new RoHudControls(layer);
            return true;
        }

        void UpdateRo(HeroController hero, bool visible, bool roOn)
        {
            if (ro == null) return;
            if (ro.Visible != visible)
            {
                ro.SetVisible(visible);
                if (!visible) ReleaseHeld();
            }
            if (hero != null && roOn) ro.Tick(hero, Time.unscaledDeltaTime);
        }
    }
}

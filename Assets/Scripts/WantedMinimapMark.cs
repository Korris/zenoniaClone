using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The Wanted Boss skull on the minimap (wanted-notifications.png section 8), on the boss's own map only: red and blinking while the boss
    /// stands (on the live boss when the zone shows it, else on its fixed tile), grey while it is away. Part of the Adventure Paper HUD:
    /// hidden with the HUD switch off or while the server's status is unknown. Minimap owns it and refreshes it with its dots (10 Hz).
    /// </summary>
    public sealed class WantedMinimapMark
    {
        const float Size = 20f, BlinkSeconds = 0.4f;

        readonly RectTransform rt;
        readonly Image image;
        readonly Sprite alive, respawning;

        /// <summary>`map` = the minimap's ground rect: the mark is placed by anchor, as a fraction of the map.</summary>
        public WantedMinimapMark(Transform map)
        {
            rt = UiKit.Rect(map, "WantedSkull", 0f, 0f, Size, Size, true);
            image = rt.gameObject.AddComponent<Image>();
            image.raycastTarget = false; image.preserveAspect = true;
            alive = PaperArt.Get(PaperPiece.WantedSkullAlive);
            respawning = PaperArt.Get(PaperPiece.WantedSkullRespawning);
            rt.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            WantedBossDef boss = UiScreens.UseNew(UiScreen.Hud) && PaperWanted.Known ? WantedBossTracker.Today : null;
            bool show = boss != null && MapRegistry.Current != null && MapRegistry.Current.Id == boss.MapId;
            if (rt.gameObject.activeSelf != show) rt.gameObject.SetActive(show);
            if (!show) return;
            bool up = WantedBossTracker.Status.Alive;
            Monster live = Monster.Wanted;
            Vector2 world = up && live != null && !live.IsDying
                ? live.Feet
                : new Vector2((boss.SpawnTx + 0.5f) * GameConfig.TileUnits, GameConfig.MapH - (boss.SpawnTy + 0.5f) * GameConfig.TileUnits);
            Vector2 at = new Vector2(Mathf.Clamp01(world.x / GameConfig.MapW), Mathf.Clamp01(world.y / GameConfig.MapH));
            rt.anchorMin = rt.anchorMax = at;
            Sprite s = up ? alive : respawning;
            if (image.sprite != s) image.sprite = s;
            // No art: a red / grey square still marks the spot.
            Color tint = s != null ? Color.white : up ? PaperTheme.BadgeRed : PaperTheme.Steel;
            tint.a = up && (int)(Time.unscaledTime / BlinkSeconds) % 2 == 1 ? 0.5f : 1f;
            image.color = tint;
        }
    }
}

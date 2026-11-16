using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// "Show on Map" of the Wanted board: the game has no world map, so it is a guide on the HUD. A gold arrow on a ring around the middle of
    /// the screen points from the hero to the boss's tile when the boss is on this map, else to the portal that leads towards its map by
    /// the fewest map changes (WantedRoute); under it the metres to that point and where it leads. The skull on the minimap is the other
    /// half (WantedMinimapMark). On is switched by the board's button and switched off when the bounty is paid; it is not saved.
    /// The route is looked up again once a second and on a map change (never per frame); the texts change only with the whole metre.
    /// </summary>
    public sealed class WantedGuide
    {
        /// <summary>The guide is switched on (this play session).</summary>
        public static bool On;

        const float RingX = 150f, RingY = 104f, ArrowSize = 30f, RouteSeconds = 1f;
        const float ArrivedUnits = 6f * GameConfig.TileUnits; // beside the boss: the arrow has done its work

        readonly RectTransform root, arrow;
        readonly Text metresText, destText;
        string routeFrom, routeBoss, shownDest;
        Vector2 target;
        bool hasTarget, sameMap;
        float routeLeft;
        int shownMetres = -1;

        public WantedGuide(RectTransform safe)
        {
            Vector2 mid = new Vector2(0.5f, 0.5f);
            root = UiWinKit.Anchored(safe, "WantedGuide", mid, mid, 0f, 0f, 120f, 70f);
            arrow = UiWinKit.Anchored(root, "Arrow", new Vector2(0.5f, 1f), mid, 0f, -ArrowSize * 0.5f, ArrowSize, ArrowSize);
            Image img = arrow.gameObject.AddComponent<Image>();
            img.raycastTarget = false; img.preserveAspect = true;
            img.sprite = PaperArt.Get(PaperPiece.WantedIconChevron); // points right at 0 degrees
            if (img.sprite == null) img.color = PaperTheme.Gold;
            metresText = PaperTheme.Title(root, "Metres", 0f, ArrowSize + 2f, 120f, 18f, "", PaperTheme.SectionSize, PaperTheme.TitleGold, PaperInk.World, TextAnchor.MiddleCenter);
            destText = PaperTheme.Body(root, "Dest", 0f, ArrowSize + 19f, 120f, 15f, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.World, TextAnchor.MiddleCenter, true);
            metresText.horizontalOverflow = destText.horizontalOverflow = HorizontalWrapMode.Overflow;
            root.gameObject.SetActive(false);
        }

        public void SetVisible(bool on) { if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on); }

        /// <summary>World position of a tile's centre (tiles count from the map's top-left, the world is y-up).</summary>
        static Vector2 TileCentre(int tx, int ty) { return new Vector2((tx + 0.5f) * GameConfig.TileUnits, GameConfig.MapH - (ty + 0.5f) * GameConfig.TileUnits); }

        /// <summary>World units from the hero to today's boss tile; only meaningful on the boss's own map (the area notice).</summary>
        public static float DistanceToBoss(HeroController hero)
        {
            WantedBossDef d = WantedBossTracker.Today;
            return (TileCentre(d.SpawnTx, d.SpawnTy) - hero.Position).magnitude;
        }

        public void Tick(HeroController hero, bool hudShown)
        {
            if (!On || !hudShown || hero.IsDead) { SetVisible(false); return; }
            WantedBossDef boss = WantedBossTracker.Today;
            string map = MapRegistry.Current.Id;
            routeLeft -= Time.unscaledDeltaTime;
            if (map != routeFrom || boss.BossType != routeBoss || routeLeft <= 0f) FindTarget(hero, boss, map);
            Vector2 to = target - hero.Position;
            float dist = to.magnitude;
            if (!hasTarget || (sameMap && dist < ArrivedUnits)) { SetVisible(false); return; }
            SetVisible(true);
            Vector2 dir = to / Mathf.Max(dist, 0.01f);
            root.anchoredPosition = new Vector2(dir.x * RingX, dir.y * RingY);
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            int metres = Mathf.RoundToInt(dist / GameConfig.TileUnits);
            if (metres == shownMetres) return;
            shownMetres = metres;
            metresText.text = metres + " m";
        }

        /// <summary>The point to walk to on this map: the boss's tile, or the nearest portal towards its map (none = no way from here: nothing is shown).</summary>
        void FindTarget(HeroController hero, WantedBossDef boss, string map)
        {
            routeFrom = map; routeBoss = boss.BossType; routeLeft = RouteSeconds;
            sameMap = map == boss.MapId;
            string dest;
            if (sameMap)
            {
                hasTarget = true;
                target = TileCentre(boss.SpawnTx, boss.SpawnTy);
                dest = boss.BossName;
            }
            else
            {
                int tx = Mathf.FloorToInt(hero.Position.x / GameConfig.TileUnits), ty = Mathf.FloorToInt((GameConfig.MapH - hero.Position.y) / GameConfig.TileUnits);
                MapPortal? portal = WantedRoute.NextPortal(map, boss.MapId, null, tx, ty); // no map has a boss gate today (MapGraphTests); null = every gate shut
                hasTarget = portal.HasValue;
                if (!hasTarget) return;
                target = TileCentre(portal.Value.Tx, portal.Value.Ty);
                IGameMap next = MapRegistry.Get(portal.Value.TargetMapId);
                dest = "to " + (next != null ? next.Name : portal.Value.TargetMapId);
            }
            if (dest == shownDest) return;
            shownDest = dest;
            destText.text = dest;
            shownMetres = -1;
        }
    }
}

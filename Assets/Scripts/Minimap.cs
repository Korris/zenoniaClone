using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Top-right minimap under the Bag and Settings buttons (web minimap-renderer.js, but the background is the map's own painted ground scaled down, so a new map
    /// needs no per-map work). Live dots: hero yellow (blinking), monsters red, boss big red, other players cyan; also villagers gold and portals white diamonds
    /// wherever the map draws them (MapScenery). Dots come from pools and move at 10 Hz, so nothing is allocated once the pools have grown.
    /// Only built with views (never on a server or a -nographics client). Display only: it takes no touches, so tapping it does nothing yet.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        const float TileUi = 5.5f, InnerW = TileMapBase.W * TileUi, InnerH = TileMapBase.H * TileUi; // 3:2 like every map
        const float FrameInset = 7f, OuterW = InnerW + FrameInset * 2f, OuterH = InnerH + FrameInset * 2f; // inset = the hud-panel gold rim at the RpgHud slice scale
        const float RightMargin = 8f, TopOffset = 8f;                      // same edge gap as the status plate; the Bag and Settings buttons sit below it
        const int BakePxPerTile = 12, BakeW = TileMapBase.W * BakePxPerTile, BakeH = TileMapBase.H * BakePxPerTile; // sharp up to a 1400 px tall screen
        const float TickSeconds = 0.1f, BlinkSeconds = 0.3f;               // web: hero blink 300 ms
        const float HeroSize = 9f, MonsterSize = 6f, BossSize = 12f, OtherSize = 7f, NpcSize = 7f, PortalSize = 6f;
        static readonly Color HeroOn = new Color(1f, 1f, 0.5f), HeroOff = new Color(1f, 0.82f, 0f);
        static readonly Color MonsterColor = new Color(0.88f, 0.25f, 0.25f), BossColor = new Color(1f, 0.19f, 0.19f);
        static readonly Color OtherColor = new Color(0.35f, 0.85f, 1f), NpcColor = new Color(1f, 0.72f, 0.12f), PortalColor = new Color(0.95f, 1f, 1f);
        static readonly Color DotEdge = new Color(0f, 0f, 0f, 0.8f);       // keeps the dots readable on bright sand and snow
        static readonly Color MissingGround = new Color(0.1f, 0.16f, 0.24f);

        RectTransform root, bossDot, heroDot;
        RawImage ground;
        Image heroImage;
        DotPool portalDots, npcDots, monsterDots, otherDots;
        Texture2D baked;
        IGameMap shownMap;
        bool shownPortalDots, shownVillagerDots;
        int shownPortals;
        float tickLeft;

        public static Minimap Create(Transform parent)
        {
            var go = new GameObject("Minimap");
            go.transform.SetParent(parent, false);
            Minimap m = go.AddComponent<Minimap>();
            m.Build(UiKit.Canvas(parent).transform);
            return m;
        }

        void OnDestroy() { if (baked != null) Destroy(baked); }

        void Build(Transform canvas)
        {
            root = TouchControls.Place(canvas, "MinimapPanel", new Vector2(1f, 1f), -(RightMargin + OuterW * 0.5f), -(TopOffset + OuterH * 0.5f), OuterW);
            root.sizeDelta = new Vector2(OuterW, OuterH);
            root.gameObject.AddComponent<Canvas>(); // own canvas: dots moving at 10 Hz do not re-batch the whole HUD; no raycaster, so it takes no touches
            Image frame = UiKit.Panel(root, Color.white);
            frame.sprite = UiArt.Panel(); frame.type = Image.Type.Sliced; frame.pixelsPerUnitMultiplier = 3.2f;

            RectTransform map = UiKit.Fill(root, "Ground", FrameInset);
            ground = map.gameObject.AddComponent<RawImage>();
            ground.raycastTarget = false;
            baked = new Texture2D(BakeW, BakeH, TextureFormat.RGBA32, false);
            baked.wrapMode = TextureWrapMode.Clamp;
            baked.filterMode = FilterMode.Bilinear;

            // Sibling order is the draw order: the hero is never hidden.
            portalDots = new DotPool(map, "Portals", null, PortalSize, PortalColor);
            npcDots = new DotPool(map, "Npcs", UiSprites.Circle(), NpcSize, NpcColor);
            monsterDots = new DotPool(map, "Monsters", UiSprites.Circle(), MonsterSize, MonsterColor);
            otherDots = new DotPool(map, "OtherHeroes", UiSprites.Circle(), OtherSize, OtherColor);
            bossDot = NewDot(map, "Boss", UiSprites.Circle(), BossSize, BossColor);
            heroDot = NewDot(map, "Hero", UiSprites.Circle(), HeroSize, HeroOn);
            heroImage = heroDot.GetComponent<Image>();
        }

        void Update()
        {
            HeroController hero = GameSession.Hero;
            bool show = hero != null && !InventoryPanel.IsOpen && !HeroChangeScreen.IsOpen && !UiWindows.ModalOpen && !Hud.CaptureClean;
            if (root.gameObject.activeSelf != show) { root.gameObject.SetActive(show); tickLeft = 0f; }
            if (!show) return;
            // Polled rather than called from MapTransfer: a portal, a loaded save and going online or offline all land here.
            // The portal count changes when a boss gate opens, or on a class switch to a save on the other side of it.
            if (MapRegistry.Current != shownMap || MapScenery.ShowsPortals != shownPortalDots || MapScenery.ShowsVillagers != shownVillagerDots || MapRegistry.Current.Portals.Length != shownPortals) Rebuild(MapRegistry.Current);
            tickLeft -= Time.unscaledDeltaTime;
            if (tickLeft > 0f) return;
            tickLeft = TickSeconds;
            Refresh(hero);
        }

        /// <summary>New map (or run mode): the background and the dots that never move.</summary>
        void Rebuild(IGameMap map)
        {
            shownMap = map;
            shownPortalDots = MapScenery.ShowsPortals;
            shownVillagerDots = MapScenery.ShowsVillagers;
            shownPortals = map.Portals.Length;
            tickLeft = 0f;
            Texture2D src = SpriteSheet.Load(map.GroundTexture);
            if (src != null) Bake(src);
            ground.texture = src != null ? baked : null; // no painting: a flat plate, the dots still work
            ground.color = src != null ? Color.white : MissingGround;

            portalDots.Begin();
            npcDots.Begin();
            if (shownPortalDots)
                foreach (MapPortal p in map.Portals)
                    if (!LowerOfPair(map, p)) portalDots.Put(new Vector2((p.Tx + 0.5f) * GameConfig.TileUnits, GameConfig.MapH - (p.Ty + 0.5f) * GameConfig.TileUnits));
            if (shownVillagerDots)
                foreach (MapNpc n in map.Npcs)
                    if (NpcDefs.Get(n.Id) != null) npcDots.Put(FrostHollowMap.TileFeet(n.Tx, n.Ty, 2f));
            portalDots.End();
            npcDots.End();
        }

        /// <summary>A wide road has a pair of trigger tiles but one light (MapScenery.BuildPortal), so it gets one marker too.</summary>
        static bool LowerOfPair(IGameMap map, MapPortal portal)
        {
            foreach (MapPortal other in map.Portals)
                if (other.TargetMapId == portal.TargetMapId && other.Tx == portal.Tx && other.Ty == portal.Ty - 1) return true;
            return false;
        }

        /// <summary>
        /// Shrinks the ground into `baked` on the GPU: the backdrops are not readable and have no mipmaps, so drawing one straight at this size would shimmer.
        /// Halving first averages the pixels the final step would otherwise skip. Copied to a Texture2D because a RenderTexture can lose its contents.
        /// </summary>
        void Bake(Texture2D src)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture step = null;
            int w = src.width, h = src.height;
            bool last;
            do
            {
                last = w / 2 < BakeW;
                w = last ? BakeW : w / 2;
                h = last ? BakeH : h / 2;
                RenderTexture next = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                next.filterMode = FilterMode.Bilinear;
                if (step == null) Graphics.Blit(src, next);
                else { Graphics.Blit(step, next); RenderTexture.ReleaseTemporary(step); }
                step = next;
            } while (!last);
            RenderTexture.active = step;
            baked.ReadPixels(new Rect(0f, 0f, BakeW, BakeH), 0, 0);
            baked.Apply(false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(step);
        }

        /// <summary>10 Hz: the moving dots. Online the monsters and other heroes are proxies in the same zone lists, so this code does not care.</summary>
        void Refresh(HeroController hero)
        {
            Zone zone = Zone.Main;
            monsterDots.Begin();
            otherDots.Begin();
            if (zone != null)
            {
                List<Monster> monsters = zone.Monsters;
                for (int i = 0; i < monsters.Count; i++)
                {
                    Monster m = monsters[i];
                    if (m != null && !m.IsDying && !m.IsBoss) monsterDots.Put(m.Feet);
                }
                IReadOnlyList<HeroController> heroes = zone.Heroes.All;
                for (int i = 0; i < heroes.Count; i++)
                {
                    HeroController h = heroes[i];
                    if (h != null && h != hero && !h.IsDead) otherDots.Put(h.Position);
                }
            }
            monsterDots.End();
            otherDots.End();

            Monster boss = Monster.Boss;
            bool bossUp = boss != null && !boss.IsDying;
            if (bossDot.gameObject.activeSelf != bossUp) bossDot.gameObject.SetActive(bossUp);
            if (bossUp) bossDot.anchoredPosition = MapPoint(boss.Feet);

            heroDot.anchoredPosition = MapPoint(hero.Position);
            heroImage.color = (int)(Time.unscaledTime / BlinkSeconds) % 2 == 0 ? HeroOn : HeroOff;
        }

        /// <summary>World units to the ground rect's bottom-left corner (both are y-up; the ground sprite starts at the world origin).</summary>
        static Vector2 MapPoint(Vector2 world)
        {
            return new Vector2(Mathf.Clamp01(world.x / GameConfig.MapW) * InnerW, Mathf.Clamp01(world.y / GameConfig.MapH) * InnerH);
        }

        /// <summary>One marker; no sprite = a white square turned into a diamond.</summary>
        static RectTransform NewDot(Transform parent, string name, Sprite sprite, float size, Color color)
        {
            RectTransform rt = TouchControls.Place(parent, name, Vector2.zero, 0f, 0f, size);
            TouchControls.Img(rt, sprite, color);
            if (sprite == null) rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Outline edge = rt.gameObject.AddComponent<Outline>();
            edge.effectColor = DotEdge;
            edge.effectDistance = new Vector2(1f, 1f);
            return rt;
        }

        /// <summary>Same-looking dots under one holder: Begin, Put each position, End hides what was left over.</summary>
        sealed class DotPool
        {
            readonly Transform holder;
            readonly string name;
            readonly Sprite sprite;
            readonly float size;
            readonly Color color;
            readonly List<RectTransform> dots = new List<RectTransform>();
            int used;

            public DotPool(Transform parent, string name, Sprite sprite, float size, Color color)
            {
                holder = UiKit.Fill(parent, name);
                this.name = name; this.sprite = sprite; this.size = size; this.color = color;
            }

            public void Begin() { used = 0; }

            public void Put(Vector2 world)
            {
                if (used == dots.Count) dots.Add(NewDot(holder, name, sprite, size, color));
                RectTransform d = dots[used++];
                if (!d.gameObject.activeSelf) d.gameObject.SetActive(true);
                d.anchoredPosition = MapPoint(world);
            }

            public void End()
            {
                for (int i = used; i < dots.Count; i++)
                    if (dots[i].gameObject.activeSelf) dots[i].gameObject.SetActive(false);
            }
        }
    }
}

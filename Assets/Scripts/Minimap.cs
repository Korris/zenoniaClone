using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Top-right minimap under the Bag and Settings buttons (web minimap-renderer.js, but the background is the map's own painted ground scaled down, so a new map
    /// needs no per-map work). Live dots: hero yellow (blinking), monsters red, boss big red, other players cyan; also villagers gold and portals white diamonds
    /// wherever the map draws them (MapScenery); a villager with a quest also gets "?" (quest to take) or "!" (quest to hand in) over his dot, from the hero's own QuestLog, so this sends nothing over the network.
    /// Dots come from pools and move at 10 Hz, so nothing is allocated once the pools have grown.
    /// Only built with views (never on a server or a -nographics client). Display only: it takes no touches, so tapping it does nothing yet.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        const float InnerW = 198f, InnerH = 132f; // fixed UI size (3:2, 36x24 tiles at 5.5 px); a bigger 3:2 map is just scaled into it
        const float FrameInset = 7f, OuterW = InnerW + FrameInset * 2f, OuterH = InnerH + FrameInset * 2f; // inset = the hud-panel gold rim at the RpgHud slice scale
        const float RightMargin = 8f, TopOffset = 8f;                      // same edge gap as the status plate; the Bag and Settings buttons sit below it
        const int BakeW = 432, BakeH = 288; // 12 px per tile of a 36x24 map: sharp up to a 1400 px tall screen; larger maps are shrunk into the same size
        const float TickSeconds = 0.1f, BlinkSeconds = 0.3f;               // web: hero blink 300 ms
        const float HeroSize = 9f, MonsterSize = 6f, BossSize = 12f, OtherSize = 7f, NpcSize = 7f, PortalSize = 6f, QuestSize = 15f;
        static readonly Color HeroOn = new Color(1f, 1f, 0.5f), HeroOff = new Color(1f, 0.82f, 0f);
        static readonly Color MonsterColor = new Color(0.88f, 0.25f, 0.25f), BossColor = new Color(1f, 0.19f, 0.19f);
        static readonly Color OtherColor = new Color(0.35f, 0.85f, 1f), NpcColor = new Color(1f, 0.72f, 0.12f), PortalColor = new Color(0.95f, 1f, 1f);
        static readonly Color DotEdge = new Color(0f, 0f, 0f, 0.8f);       // keeps the dots readable on bright sand and snow
        static readonly Color MissingGround = new Color(0.1f, 0.16f, 0.24f);

        RectTransform root, bossDot, heroDot;
        RawImage ground;
        Image heroImage;
        DotPool portalDots, npcDots, monsterDots, otherDots, questTakeDots, questHandInDots;
        struct QuestGiver { public string Id; public Vector2 Feet; }
        readonly List<QuestGiver> questGivers = new List<QuestGiver>(); // the villagers of this map who give quests; which marker they show changes with the quest
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
            questTakeDots = new DotPool(map, "QuestTake", QuestMarkerSprite(QuestionGlyph), QuestSize, Color.white);
            questHandInDots = new DotPool(map, "QuestHandIn", QuestMarkerSprite(ExclaimGlyph), QuestSize, Color.white);
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
            Texture2D src = MinimapSource(map);
            if (src != null) Bake(src);
            ground.texture = src != null ? baked : null; // no painting: a flat plate, the dots still work
            ground.color = src != null ? Color.white : MissingGround;

            portalDots.Begin();
            npcDots.Begin();
            questGivers.Clear();
            if (shownPortalDots)
                foreach (MapPortal p in map.Portals)
                    if (!LowerOfPair(map, p)) portalDots.Put(new Vector2((p.Tx + 0.5f) * GameConfig.TileUnits, GameConfig.MapH - (p.Ty + 0.5f) * GameConfig.TileUnits));
            if (shownVillagerDots)
                foreach (MapNpc n in map.Npcs)
                {
                    NpcDef def = NpcDefs.Get(n.Id);
                    if (def == null) continue;
                    Vector2 feet = FrostHollowMap.TileFeet(n.Tx, n.Ty, 2f);
                    npcDots.Put(feet);
                    if (def.QuestGiver) questGivers.Add(new QuestGiver { Id = n.Id, Feet = feet });
                }
            portalDots.End();
            npcDots.End();
        }

        /// <summary>
        /// The picture the minimap is baked from: a painted "-minimap" next to the ground wins (a chunked map has no single ground image, so it needs one);
        /// otherwise the single ground painting. A chunked map without a minimap picture gets a flat plate and a warning.
        /// </summary>
        static Texture2D MinimapSource(IGameMap map)
        {
            Texture2D art = SpriteSheet.TryLoad(map.GroundTexture + "-minimap");
            if (art != null) return art;
            if (map.GroundChunkCols * map.GroundChunkRows > 1)
            {
                Debug.LogWarning("[ZenoniaProto] Missing minimap Resources/" + map.GroundTexture + "-minimap.png for chunked map " + map.Id);
                return null;
            }
            return SpriteSheet.Load(map.GroundTexture);
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

            questTakeDots.Begin();
            questHandInDots.Begin();
            QuestLog quests = hero.Rpg != null ? hero.Rpg.Quests : null;
            if (quests != null)
                for (int i = 0; i < questGivers.Count; i++)
                {
                    QuestMarker marker = quests.MarkerFor(questGivers[i].Id);
                    if (marker == QuestMarker.Available) questTakeDots.Put(questGivers[i].Feet);
                    else if (marker == QuestMarker.Ready) questHandInDots.Put(questGivers[i].Feet);
                }
            questTakeDots.End();
            questHandInDots.End();

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

        static readonly string[] QuestionGlyph = { ".#####.", "##...##", "##...##", "....##.", "...##..", "...##..", ".......", "...##..", "...##.." };
        static readonly string[] ExclaimGlyph = { "..###..", "..###..", "..###..", "..###..", "..###..", "..###..", ".......", "..###..", "..###.." };

        /// <summary>The look of the "?" above a quest giver's head, at minimap size: gold rim, navy disc, gold 7 x 9 glyph. Drawn at 4x so it stays smooth when the HUD is scaled.</summary>
        static Sprite QuestMarkerSprite(string[] glyph)
        {
            const int Grid = 15, Scale = 4, Px = Grid * Scale;
            Color32 clear = new Color32(0, 0, 0, 0), rim = new Color(1f, 0.8f, 0.3f), disc = new Color(0.04f, 0.08f, 0.17f), ink = new Color(1f, 0.85f, 0.35f);
            var pixels = new Color32[Px * Px];
            float centre = Px * 0.5f, outer = centre, inner = centre - 1.2f * Scale;
            int glyphX = (Grid - 7) / 2, glyphTop = (Grid - 9) / 2;
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++)
                {
                    float dx = x + 0.5f - centre, dy = y + 0.5f - centre, d = Mathf.Sqrt(dx * dx + dy * dy);
                    int gx = x / Scale - glyphX, gy = (Px - 1 - y) / Scale - glyphTop; // glyph rows run top down, the texture bottom up
                    bool inked = gx >= 0 && gx < 7 && gy >= 0 && gy < 9 && glyph[gy][gx] == '#';
                    pixels[y * Px + x] = d > outer ? clear : d > inner ? rim : inked ? ink : disc;
                }
            var tex = new Texture2D(Px, Px, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Px, Px), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
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

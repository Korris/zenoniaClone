using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Big map markers, placed by anchor as a fraction of the map (so they follow any window size). Built once per map: monster camps (dashed ring over
    /// the zone rect + "Lv.6-7" pill), exits (portal swirl + navy "To Bramblewood Forest"), villagers (gold bag + name), the map boss's home (skull +
    /// red "Lv.12 Ambercap Warden"), today's Wanted boss when it lives here (horned face, WANTED tag, red name pill). Moving, 4 times a second: the map
    /// boss on its live spot (faded while it is dead), the Wanted boss (faded while away), party members (cyan + name, from the zone's heroes: online
    /// proxies, so only those in this map's zone), other players (small, faded); the hero's arrow follows every frame and turns with the facing.
    /// Single monsters are not drawn (the camps say where they are; 70 dots over a 700-unit map were clutter).
    /// </summary>
    public sealed partial class WorldMapWindow
    {
        const float TickSeconds = 0.25f, LabelH = 20f, LabelGap = 3f;
        const float HeroSize = 26f, PartySize = 14f, OtherSize = 9f, PortalSize = 26f, NpcSize = 20f, BossSize = 28f, WantedSize = 32f;
        static readonly Color CampTint = new Color(1f, 0.93f, 0.98f, 0.9f), Faded = new Color(1f, 1f, 1f, 0.45f);
        static readonly float[] FacingAngle = { -90f, 90f, 180f, 0f }; // HeroController facing 0 down, 1 up, 2 left, 3 right; the arrow art points right

        RectTransform staticLayer, bossMark, wantedMark, partyLayer, otherLayer, heroMark;
        Image bossImage, wantedImage;
        Text bossLabel, wantedLabel;
        CanvasGroup bossGroup, wantedGroup;
        Vector2 bossHome, wantedHome;
        WantedBossDef wantedShown;
        readonly List<RectTransform> partyDots = new List<RectTransform>(), otherDots = new List<RectTransform>();
        readonly List<Text> partyNames = new List<Text>();
        float tickLeft;

        void BuildMarkers(RectTransform map)
        {
            staticLayer = UiKit.Fill(map, "Static");
            bossMark = Marker(map, "Boss", "wm-boss", BossSize, Vector2.zero);
            bossGroup = bossMark.gameObject.AddComponent<CanvasGroup>();
            bossImage = bossMark.GetComponent<Image>();
            bossLabel = Label(bossMark, "", "wm-label-red", 55f, false);
            wantedMark = Marker(map, "Wanted", "wm-wanted-face", WantedSize, Vector2.zero);
            wantedGroup = wantedMark.gameObject.AddComponent<CanvasGroup>();
            wantedImage = wantedMark.GetComponent<Image>();
            RectTransform tag;
            RoHud.Pill(wantedMark, "Tag", new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), 0f, 0f, 15f, "wm-wanted-tag", null, 0f, "WANTED", 9, Color.white, out tag);
            tag.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(7, 7, 1, 1);
            tag.GetComponent<Image>().pixelsPerUnitMultiplier = 58f / (2f * 15f);
            wantedLabel = Label(wantedMark, "", "wm-label-red", 55f, false);
            otherLayer = UiKit.Fill(map, "Others");
            partyLayer = UiKit.Fill(map, "Party");
            heroMark = Marker(map, "Hero", "wm-arrow", HeroSize, Vector2.zero);
        }

        /// <summary>New map: the markers that never move, and the homes of the two bosses.</summary>
        void RebuildMarkers(IGameMap map)
        {
            for (int i = staticLayer.childCount - 1; i >= 0; i--) Destroy(staticLayer.GetChild(i).gameObject);
            foreach (WorldMapData.Camp c in WorldMapData.Camps(map))
            {
                RectTransform ring = UiKit.Fill(staticLayer, "Camp");
                ring.anchorMin = TileFrac(map, c.Tiles.x, c.Tiles.y + c.Tiles.height);
                ring.anchorMax = TileFrac(map, c.Tiles.x + c.Tiles.width, c.Tiles.y);
                ring.offsetMin = ring.offsetMax = Vector2.zero;
                Image img = RoHud.Art(ring, "wm-camp-ring");
                img.preserveAspect = false; img.color = CampTint;
                RectTransform pill;
                RoHud.Pill(ring, "Level", RoHud.Mid, RoHud.Mid, 0f, 0f, 18f, "wm-level-pill", null, 0f, c.MinLv == c.MaxLv ? "Lv." + c.MinLv : "Lv." + c.MinLv + "-" + c.MaxLv, 11, RoHud.Navy, out pill);
                pill.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(9, 9, 1, 1);
                pill.GetComponent<Image>().pixelsPerUnitMultiplier = 83f / (2f * 18f);
            }
            foreach (WorldMapData.Exit e in WorldMapData.Exits(map))
            {
                Vector2 at = TileCentre(map, e.Tx, e.Ty);
                Label(Marker(staticLayer, "Exit", "wm-portal", PortalSize, at), "To " + e.TargetName, "wm-label-navy", 56f, at.x > 0.62f);
            }
            foreach (MapNpc n in map.Npcs)
            {
                NpcDef def = NpcDefs.Get(n.Id);
                if (def == null) continue;
                RectTransform npc = Marker(staticLayer, "Npc", "wm-npc", NpcSize, TileCentre(map, n.Tx, n.Ty));
                if (!WorldMapData.IsShop(n.Id)) npc.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
                Text name = RoHud.Label(UiKit.Rect(npc, "Name", 0f, 0f, 120f, 14f), def.Name, 10, Color.white, TextAnchor.UpperCenter, true, true);
                RectTransform nr = name.rectTransform;
                nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0f); nr.pivot = new Vector2(0.5f, 1f); nr.anchoredPosition = Vector2.zero;
            }

            MonsterDef boss = map.Hostile ? MapSpawnTable.BossFor(map.Id) : null;
            bossMark.gameObject.SetActive(boss != null);
            if (boss != null)
            {
                Vector2Int t = MapSpawnTable.BossTileFor(map.Id);
                bossHome = TileCentre(map, t.x, t.y);
                bossLabel.text = "Lv." + boss.ItemLevel + " " + boss.Name;
                FlipLabel(bossLabel, bossHome.x > 0.62f);
            }
            wantedShown = null; // picked on the next tick (today's boss can change while the window is open)
            wantedMark.gameObject.SetActive(false);
            tickLeft = 0f;
        }

        void TickMarkers(bool force)
        {
            HeroController hero = GameSession.Hero;
            if (hero == null) return;
            Place(heroMark, WorldFrac(hero.Position));
            int f = Mathf.Clamp(hero.Facing, 0, 3);
            heroMark.localEulerAngles = new Vector3(0f, 0f, FacingAngle[f]);
            tickLeft -= Time.unscaledDeltaTime;
            if (!force && tickLeft > 0f) return;
            tickLeft = TickSeconds;
            TickBosses();
            TickHeroes(hero);
        }

        void TickBosses()
        {
            if (bossMark.gameObject.activeSelf)
            {
                Monster live = Monster.Boss;
                bool up = live != null && !live.IsDying;
                Place(bossMark, up ? WorldFrac(live.Feet) : bossHome);
                bossGroup.alpha = up ? 1f : Faded.a;
            }
            WantedBossDef today = WantedBossTracker.Today;
            IGameMap map = MapRegistry.Current;
            bool here = today != null && map != null && today.MapId == map.Id;
            if (wantedMark.gameObject.activeSelf != here) wantedMark.gameObject.SetActive(here);
            if (!here) return;
            if (today != wantedShown)
            {
                wantedShown = today;
                wantedHome = TileCentre(map, today.SpawnTx, today.SpawnTy);
                wantedLabel.text = "Lv." + today.BossLevel + " " + today.BossName;
                FlipLabel(wantedLabel, wantedHome.x > 0.62f);
            }
            Monster w = Monster.Wanted;
            bool alive = w != null && !w.IsDying;
            bool away = !alive && WantedBossTracker.Status.Known && !WantedBossTracker.Status.Alive;
            Place(wantedMark, alive ? WorldFrac(w.Feet) : wantedHome);
            wantedGroup.alpha = away ? Faded.a : 1f;
        }

        /// <summary>Party members (cyan + name) and other players (small, faded) among the zone's heroes.</summary>
        void TickHeroes(HeroController me)
        {
            int party = 0, others = 0;
            Zone zone = Zone.Main;
            if (zone != null)
            {
                PartyClientState roster = NetClient.Party;
                IReadOnlyList<HeroController> heroes = zone.Heroes.All;
                for (int i = 0; i < heroes.Count; i++)
                {
                    HeroController h = heroes[i];
                    if (h == null || h == me || h.IsDead) continue;
                    string mate = MateName(roster, h);
                    if (mate != null)
                    {
                        if (party == partyDots.Count)
                        {
                            RectTransform dot = Marker(partyLayer, "Mate", "wm-party", PartySize, Vector2.zero);
                            partyDots.Add(dot);
                            partyNames.Add(Label(dot, "", "wm-label-navy", 56f, false));
                        }
                        Vector2 at = WorldFrac(h.Position);
                        Show(partyDots[party], at);
                        if (partyNames[party].text != mate) partyNames[party].text = mate;
                        FlipLabel(partyNames[party], at.x > 0.62f);
                        party++;
                    }
                    else
                    {
                        if (others == otherDots.Count)
                        {
                            RectTransform dot = Marker(otherLayer, "Player", "wm-party", OtherSize, Vector2.zero);
                            dot.GetComponent<Image>().color = Faded;
                            otherDots.Add(dot);
                        }
                        Show(otherDots[others++], WorldFrac(h.Position));
                    }
                }
            }
            for (int i = party; i < partyDots.Count; i++) if (partyDots[i].gameObject.activeSelf) partyDots[i].gameObject.SetActive(false);
            for (int i = others; i < otherDots.Count; i++) if (otherDots[i].gameObject.activeSelf) otherDots[i].gameObject.SetActive(false);
        }

        /// <summary>The party member's name when `h` is one (online, in a party), else null.</summary>
        static string MateName(PartyClientState roster, HeroController h)
        {
            if (roster == null || !roster.InParty) return null;
            for (int m = 0; m < roster.Members.Count; m++)
            {
                PartyMemberInfo info = roster.Members[m];
                if (info.PlayerId != roster.MyId && PartyHudList.FindHero(info.PlayerId) == h) return info.Name;
            }
            return null;
        }

        static void Show(RectTransform rt, Vector2 at)
        {
            if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);
            Place(rt, at);
        }

        static void Place(RectTransform rt, Vector2 at)
        {
            if (rt.anchorMin != at) { rt.anchorMin = rt.anchorMax = at; rt.anchoredPosition = Vector2.zero; }
        }

        /// <summary>World units (y up, origin bottom-left) to a fraction of the map.</summary>
        static Vector2 WorldFrac(Vector2 world) { return new Vector2(Mathf.Clamp01(world.x / GameConfig.MapW), Mathf.Clamp01(world.y / GameConfig.MapH)); }

        /// <summary>A tile corner (tx, ty from the top-left) to a fraction of the map.</summary>
        static Vector2 TileFrac(IGameMap map, float tx, float ty) { return new Vector2(Mathf.Clamp01(tx / map.Width), Mathf.Clamp01(1f - ty / map.Height)); }

        /// <summary>A tile's centre as a fraction, kept off the very edge: an exit on the map's last column would draw half outside the frame.</summary>
        static Vector2 TileCentre(IGameMap map, int tx, int ty)
        {
            Vector2 f = TileFrac(map, tx + 0.5f, ty + 0.5f);
            return new Vector2(Mathf.Clamp(f.x, 0.02f, 0.98f), Mathf.Clamp(f.y, 0.04f, 0.96f));
        }

        /// <summary>A marker of one kit piece, `size` units square, centred on the fraction `at` of the map.</summary>
        static RectTransform Marker(Transform parent, string name, string piece, float size, Vector2 at)
        {
            RectTransform rt = UiKit.Rect(parent, name, 0f, 0f, size, size, true);
            RoHud.Art(rt, piece);
            Place(rt, at);
            return rt;
        }

        /// <summary>A pill label beside a marker (right of it, or left when `left`), vertically centred on it. `artH` = the skin's height in px.</summary>
        static Text Label(RectTransform marker, string text, string skin, float artH, bool left)
        {
            RectTransform rt;
            Text t = RoHud.Pill(marker, "Label", new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), LabelGap, 0f, LabelH, skin, null, 0f, text, 11, Color.white, out rt);
            rt.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(9, 9, 1, 1);
            rt.GetComponent<Image>().pixelsPerUnitMultiplier = artH / (2f * LabelH);
            FlipLabel(t, left);
            return t;
        }

        /// <summary>Puts a marker's label on its left (markers in the right part of the map, so the pill stays on the map) or right.</summary>
        static void FlipLabel(Text label, bool left)
        {
            var rt = (RectTransform)label.transform.parent;
            Vector2 anchor = new Vector2(left ? 0f : 1f, 0.5f);
            if (rt.anchorMin == anchor) return;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(left ? 1f : 0f, 0.5f);
            rt.anchoredPosition = new Vector2(left ? -LabelGap : LabelGap, 0f);
        }
    }
}

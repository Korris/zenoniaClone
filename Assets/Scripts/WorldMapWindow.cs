using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The RO-style big map (user 21/11: "làm bản đồ lớn kiểu RO đi"; mockup docs/reference/ro-soft-world-map/world-map-mockup-chatgpt-1.png, art
    /// ChatGPT sheet ro-world-map -> Resources/Sprites/ui/ro-soft/wm-*). M (PC) or a tap on the minimap opens it; M, Esc, the X or a tap outside
    /// closes it. One ivory window in the middle of the screen over a soft dim: the navy "World Map" tab, the map's name + level band, the whole
    /// current map (its minimap / ground painting, aspect kept: Rootburrow 60x24 is wide and short, Willowbrook 72x48 taller) with markers
    /// (WorldMapWindow.Markers.cs), and the legend. Not modal: the game keeps running and the hero keeps moving under it. Display only, nothing sent.
    /// </summary>
    public sealed partial class WorldMapWindow : MonoBehaviour
    {
        // Box model (canvas units, docs/ui-layout-rules.md section 1).
        const float EdgeGap = 12f, HeaderRise = 20f, MaxWindowW = 760f, MinWindowW = 600f;
        const float PadX = 18f, PadBottom = 14f, TitleTop = 24f, TitleH = 34f, LevelH = 30f, TitleGap = 10f, MapGap = 10f, LegendH = 40f;
        const float HeaderW = 210f, HeaderH = 40f, CloseSize = 42f, WindowCorner = 1.25f;
        const float ChromeH = TitleTop + TitleH + TitleGap + MapGap + LegendH + PadBottom; // everything but the map, top to bottom
        static readonly Color DimColor = new Color(0.12f, 0.16f, 0.24f, 0.32f);

        static WorldMapWindow instance;
        static int closedFrame = -1;

        RectTransform safe, window, mapArea, frame;
        RawImage ground;
        Text mapName, levelText;
        Vector2 laidOutFor;
        IGameMap shownMap;

        public static bool IsOpen { get { return instance != null && instance.gameObject.activeSelf; } }

        /// <summary>True while open and on the frame it closed: Esc closed the map, so it must not also open Settings.</summary>
        public static bool OwnsEsc { get { return IsOpen || closedFrame == Time.frameCount; } }

        public static void Toggle() { if (IsOpen) Close(); else Open(); }

        public static void Open()
        {
            if (GameSession.Hero == null || MapRegistry.Current == null) return;
            if (instance == null)
            {
                PaperLayout l = PaperLayout.CreateSafeArea(UiKit.Canvas(UiScreens.Root).transform, "WorldMapWindow");
                instance = l.gameObject.AddComponent<WorldMapWindow>();
                instance.Build(l);
            }
            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
            instance.laidOutFor = Vector2.zero;
            instance.shownMap = null;
            instance.Tick(true);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            instance.gameObject.SetActive(false);
            closedFrame = Time.frameCount;
        }

        /// <summary>Minimap.Update, every frame while the hero exists: the M key (not while typing or with a modal window / the bag open).</summary>
        public static void PollKey()
        {
            if (IsOpen && InputAdapter.WindowPressed(GameKey.Close)) { Close(); return; }
            bool free = !UiWindows.BlocksGameplay && !InventoryPanel.IsOpen && !HeroChangeScreen.IsOpen && !ChatWindow.RecentlyTyping;
            if (IsOpen ? InputAdapter.WindowPressed(GameKey.WorldMap) : free && InputAdapter.Pressed(GameKey.WorldMap)) Toggle();
        }

        void OnDestroy() { if (instance == this) instance = null; ReleaseGround(); }

        void Build(PaperLayout l)
        {
            safe = l.Safe;
            Image dim = UiKit.Fill(l.Root, "Dim").gameObject.AddComponent<Image>();
            dim.color = DimColor;
            UiWinKit.MakeButton(dim, Close); // a tap outside the window closes it
            dim.transform.SetAsFirstSibling();

            window = UiKit.Rect(safe, "Window", 0f, 0f, MinWindowW, 400f, true);
            Image plate = RoHud.Art(window, "wm-window", true); // raycast: a tap inside the window does not reach the dim
            plate.pixelsPerUnitMultiplier = WindowCorner;

            // The navy "World Map" tab over the top-left edge: folded-map icon on its left end, wing on its right end (9-slice border 110).
            RectTransform header = UiKit.Rect(window, "Header", 26f, -HeaderRise, HeaderW, HeaderH);
            RoHud.Art(header, "wm-header").pixelsPerUnitMultiplier = 100f / (2f * HeaderH);
            RoHud.Art(UiKit.Rect(header, "Icon", -14f, -10f, 52f, 48f), "wm-icon-map");
            RoHud.Label(UiKit.Rect(header, "Title", 44f, 0f, HeaderW - 90f, HeaderH), "World Map", 18, Color.white, TextAnchor.MiddleLeft, true, true);

            RectTransform close = UiKit.Rect(window, "Close", 0f, 0f, CloseSize, CloseSize);
            close.anchorMin = close.anchorMax = Vector2.one; close.pivot = new Vector2(0.5f, 0.5f);
            close.anchoredPosition = new Vector2(-12f, -12f);
            UiWinKit.MakeButton(RoHud.Art(close, "wm-close", true), Close);

            BuildTitleRow();

            mapArea = UiKit.Rect(window, "Map", PadX, TitleTop + TitleH + TitleGap, 400f, 200f);
            ground = UiKit.Fill(mapArea, "Ground").gameObject.AddComponent<RawImage>();
            ground.raycastTarget = false;
            BuildMarkers(mapArea);
            frame = UiKit.Fill(mapArea, "Frame", -4f); // the frame's band lies over the painting's edge
            RoHud.Art(frame, "wm-map-frame").pixelsPerUnitMultiplier = 1.6f;

            BuildLegend();
            NameTags.Occlude(window); // IMGUI name tags never draw over the window
        }

        /// <summary>Centred row under the top edge: [flourish] [navy pill: pin + map name] [level pill] [flourish], widths from the text.</summary>
        void BuildTitleRow()
        {
            RectTransform row = UiKit.Rect(window, "TitleRow", 0f, TitleTop, 10f, TitleH);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -TitleTop);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            row.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            Flourish(row, false);
            RectTransform pill;
            mapName = RoHud.Pill(row, "Name", RoHud.Mid, RoHud.Mid, 0f, 0f, TitleH, "wm-title-pill", "hud-icon-pin-white", 18f, "", 17, Color.white, out pill, 150f);
            pill.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(18, 20, 3, 3);
            pill.GetComponent<Image>().pixelsPerUnitMultiplier = 92f / (2f * TitleH);
            Fixed(pill, -1f, TitleH);
            RectTransform lv;
            levelText = RoHud.Pill(row, "Level", RoHud.Mid, RoHud.Mid, 0f, 0f, LevelH, "wm-level-pill", null, 0f, "", 15, RoHud.Navy, out lv, 76f);
            lv.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(16, 16, 3, 3);
            lv.GetComponent<Image>().pixelsPerUnitMultiplier = 83f / (2f * LevelH);
            Fixed(lv, -1f, LevelH);
            Flourish(row, true);
        }

        static void Flourish(RectTransform row, bool mirrored)
        {
            RectTransform f = UiKit.Rect(row, mirrored ? "FlourishRight" : "FlourishLeft", 0f, 0f, 70f, 23f);
            RoHud.Art(f, "wm-flourish");
            f.pivot = new Vector2(0.5f, 0.5f); // mirrored about its own middle, so it stays in its layout slot
            if (mirrored) f.localScale = new Vector3(-1f, 1f, 1f);
            Fixed(f, 70f, 23f);
        }

        /// <summary>Fixed layout size (w &lt; 0: keep the preferred width the content asks for).</summary>
        static void Fixed(RectTransform rt, float w, float h)
        {
            LayoutElement le = rt.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            if (w >= 0f) le.minWidth = le.preferredWidth = w;
            le.minHeight = le.preferredHeight = h;
        }

        void Update()
        {
            if (GameSession.Hero == null || UiWindows.ModalOpen || InventoryPanel.IsOpen || HeroChangeScreen.IsOpen) { Close(); return; }
            Tick(false);
        }

        void Tick(bool force)
        {
            IGameMap map = MapRegistry.Current;
            Vector2 room = safe.rect.size;
            if (map != shownMap) { shownMap = map; ShowMap(map); laidOutFor = Vector2.zero; }
            if (room != laidOutFor && room.x > 1f) { laidOutFor = room; Layout(map, room); force = true; }
            TickMarkers(force);
        }

        void ShowMap(IGameMap map)
        {
            mapName.text = map.Name;
            string lv = WorldMapData.LevelText(map);
            levelText.text = lv;
            levelText.transform.parent.gameObject.SetActive(lv != "");
            SetGround(map);
            RebuildMarkers(map);
        }

        /// <summary>
        /// Window size for this map and screen: the map as wide as fits (window at most MaxWindowW), its height from the map's aspect; when that is too tall,
        /// the height wins and the map gets narrower. The window never goes under MinWindowW (the legend's width): a narrow map is centred in it.
        /// </summary>
        void Layout(IGameMap map, Vector2 room)
        {
            float aspect = (float)map.Width / map.Height;
            float maxW = Mathf.Min(room.x - EdgeGap * 2f, MaxWindowW), maxH = room.y - EdgeGap * 2f - HeaderRise;
            float mw = maxW - PadX * 2f, mh = mw / aspect;
            if (mh > maxH - ChromeH) { mh = Mathf.Max(80f, maxH - ChromeH); mw = mh * aspect; }
            float ww = Mathf.Max(mw + PadX * 2f, Mathf.Min(MinWindowW, maxW)), wh = mh + ChromeH;
            window.sizeDelta = new Vector2(ww, wh);
            window.anchoredPosition = new Vector2(0f, -HeaderRise * 0.5f);
            mapArea.sizeDelta = new Vector2(mw, mh);
            mapArea.anchoredPosition = new Vector2((ww - mw) * 0.5f, -(TitleTop + TitleH + TitleGap));
            LayoutLegend(ww);
        }
    }
}

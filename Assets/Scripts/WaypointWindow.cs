using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The warp crystal's destination list (Rootburrow V5, RO-style waypoint, ChatGPT docs/reference/rootburrow-dark-25d/floors-purpose-chatgpt.md section 6): opened with
    /// "Warp" / E at an attuned crystal (DungeonInteract). Same ChatGPT window kit as the World Map (wm-window plate, navy wm-header tab, wm-close), one row per crystal
    /// of the dungeon + "Return to Bramblewood": an ivory row (btn-ivory) to warp to, a grey row (btn-grey) for the crystal the hero stands at ("Here") or one not
    /// attuned yet ("Locked", name hidden). A tap closes the list and starts the 3 s channel. Box model (docs/ui-layout-rules.md): window 380 wide, padding 18,
    /// rows 48 high (padding 8 / 14, icon 28, gap 10), 8 between rows; modal (UiWindows.RegisterModal), Esc / X / a tap outside closes.
    /// </summary>
    public sealed class WaypointWindow : MonoBehaviour
    {
        const float WindowW = 380f, PadX = 18f, TitleTop = 30f, SubH = 26f, RowH = 48f, RowGap = 8f, PadBottom = 18f, HeaderW = 220f, HeaderH = 40f, HeaderRise = 20f, CloseSize = 42f, IconSize = 28f;
        static readonly Color DimColor = new Color(0.12f, 0.16f, 0.24f, 0.32f), Muted = new Color(0.45f, 0.48f, 0.56f);

        static WaypointWindow instance;
        static bool registered;
        RectTransform window, rows;
        WaypointDef from;

        public static bool IsOpen { get { return instance != null && instance.gameObject.activeSelf; } }

        /// <summary>Opens the list for the crystal the hero stands at.</summary>
        public static void Open(WaypointDef at)
        {
            if (GameSession.Hero == null || at == null) return;
            if (instance == null)
            {
                PaperLayout l = PaperLayout.CreateSafeArea(UiKit.Canvas(UiScreens.Root).transform, "WaypointWindow");
                instance = l.gameObject.AddComponent<WaypointWindow>();
                instance.Build(l);
            }
            if (!registered) { registered = true; UiWindows.RegisterModal(() => IsOpen, Close); }
            instance.from = at;
            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
            instance.Fill();
        }

        public static void Close() { if (IsOpen) instance.gameObject.SetActive(false); }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(PaperLayout l)
        {
            Image dim = UiKit.Fill(l.Root, "Dim").gameObject.AddComponent<Image>();
            dim.color = DimColor;
            UiWinKit.MakeButton(dim, Close);
            dim.transform.SetAsFirstSibling();

            window = UiKit.Rect(l.Safe, "Window", 0f, 0f, WindowW, 300f, true);
            RoHud.Art(window, "wm-window", true).pixelsPerUnitMultiplier = 1.25f;
            RectTransform header = UiKit.Rect(window, "Header", 26f, -HeaderRise, HeaderW, HeaderH);
            RoHud.Art(header, "wm-header").pixelsPerUnitMultiplier = 100f / (2f * HeaderH);
            RoHud.Art(UiKit.Rect(header, "Icon", -10f, -6f, 44f, 44f), "wm-portal");
            RoHud.Label(UiKit.Rect(header, "Title", 44f, 0f, HeaderW - 70f, HeaderH), "Warp Crystal", 18, Color.white, TextAnchor.MiddleLeft, true, true);
            RectTransform close = UiKit.Rect(window, "Close", 0f, 0f, CloseSize, CloseSize);
            close.anchorMin = close.anchorMax = Vector2.one; close.pivot = new Vector2(0.5f, 0.5f);
            close.anchoredPosition = new Vector2(-12f, -12f);
            UiWinKit.MakeButton(RoHud.Art(close, "wm-close", true), Close);
            RoHud.Label(UiKit.Rect(window, "Sub", PadX, TitleTop, WindowW - PadX * 2f, SubH), "Choose where the crystal takes you", 15, RoHud.Navy, TextAnchor.MiddleCenter, true);
            rows = UiKit.Rect(window, "Rows", PadX, TitleTop + SubH + RowGap, WindowW - PadX * 2f, 10f);
            NameTags.Occlude(window);
        }

        void Fill()
        {
            for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);
            PlayerState me = GameSession.Hero.Rpg;
            int n = 0;
            foreach (WaypointDef w in DungeonPoints.Waypoints)
            {
                bool here = w == from, open = me != null && me.HasWaypoint(w.Id);
                Row(n++, open || here ? w.Name : "??? (not attuned)", here ? "Here" : open ? "" : "Locked", open && !here ? w.Id : null);
            }
            Row(n++, DungeonPoints.ExitName, "", DungeonPoints.ExitId);
            float h = TitleTop + SubH + RowGap + n * (RowH + RowGap) - RowGap + PadBottom;
            window.sizeDelta = new Vector2(WindowW, h);
            window.anchoredPosition = new Vector2(0f, -HeaderRise * 0.5f);
        }

        /// <summary>One destination row; `toId` null = not selectable (grey).</summary>
        void Row(int index, string name, string state, string toId)
        {
            float w = WindowW - PadX * 2f;
            RectTransform rt = UiKit.Rect(rows, "Row" + index, 0f, index * (RowH + RowGap), w, RowH);
            Image plate = RoHud.Art(rt, toId != null ? "btn-ivory" : "btn-grey", toId != null);
            if (plate.sprite != null) plate.pixelsPerUnitMultiplier = plate.sprite.rect.height / RowH; // whole art height on the row: even round ends
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(14, 14, 8, 8); h.spacing = 10f; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            RectTransform icon = UiKit.Rect(rt, "Icon", 0f, 0f, IconSize, IconSize);
            Image ic = RoHud.Art(icon, toId == DungeonPoints.ExitId ? "hud-icon-pin" : "wm-portal");
            if (toId == null) ic.color = new Color(1f, 1f, 1f, 0.55f);
            Size(icon, IconSize, IconSize, 0f);
            Text label = RoHud.Label(UiKit.Rect(rt, "Name", 0f, 0f, 10f, RowH - 16f), name, 16, toId != null ? RoHud.Navy : Muted, TextAnchor.MiddleLeft, true);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            Size((RectTransform)label.transform, -1f, RowH - 16f, 1f);
            if (state != "")
            {
                Text s = RoHud.Label(UiKit.Rect(rt, "State", 0f, 0f, 10f, RowH - 16f), state, 14, Muted, TextAnchor.MiddleRight, true);
                Size((RectTransform)s.transform, 64f, RowH - 16f, 0f);
            }
            if (toId == null) return;
            WaypointDef at = from;
            string target = toId;
            UiWinKit.MakeButton(plate, () => { Close(); DungeonInteract.StartWarp(at, target); });
        }

        static void Size(RectTransform rt, float w, float h, float flex)
        {
            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            if (w >= 0f) le.minWidth = le.preferredWidth = w;
            le.minHeight = le.preferredHeight = h;
            le.flexibleWidth = flex;
        }

        void Update()
        {
            if (GameSession.Hero == null || GameSession.Hero.IsDead) Close();
        }
    }
}

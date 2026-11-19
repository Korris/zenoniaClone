using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The RO Soft Fantasy HUD (docs/reference/ui-style-ro-soft-fantasy/02-hud.png), the default HUD while the RO look is on
    /// (UiStyle.Ro) and the Adventure Paper compact HUD is off (UiScreens.UseNew(UiScreen.Hud)), on mobile and on PC / web (user 19/11:
    /// "bản pc chuyển sang hud mới"). PC (Pc): no joystick, key caps on the buttons, 1-8 cast the loadout, chat bottom-left
    /// (RoHudChatBar). `--ui-style classic` = the old HUDs. All art is ChatGPT-painted (Assets/ArtLibrary/ui/ro-soft-hud/src, cut by Tools/ui-ro-soft-sheets/ro-hud-*.json into
    /// Resources/Sprites/ui/ro-soft/hud-*); nothing here draws a shape. Pieces:
    ///   RoHudStatus     top-left portrait, Lv, HP / MP / EXP (built by RpgHud)
    ///   RoHudParty      "Party (n/3)" under it (built by RpgHud)
    ///   RoHudMenu       Bag / Skills / Pet / Quest / More, top centre-right
    ///   RoHudMapPanel   map name + connection pills, the round minimap's ring and "+" (the minimap itself: Minimap.RoRound.cs)
    ///   RoHudWanted     the WANTED card under the minimap
    ///   RoHudControls   joystick, potions, Attack + 4 skills on an arc, Auto, Run (built by TouchControls.RoSoft.cs)
    ///   RoHudChatBar    the chat button + one-line chat bar (ChatWindow)
    /// Layout numbers are canvas units (640 high, so 1 mockup px of the 1672x941 board = 0.68 units), anchored to the safe area corners.
    /// </summary>
    public static class RoHud
    {
        /// <summary>True while this HUD replaces the classic touch HUD. Read every frame (capture flags may flip the switches).</summary>
        public static bool Active { get { return UiStyle.Ro && !UiScreens.UseNew(UiScreen.Hud); } }

        /// <summary>The PC / web layout of this HUD (HudLayout.IsWeb: WebGL, or `--hud web`): keyboard + mouse, no touch-only parts.</summary>
        public static bool Pc { get { return HudLayout.IsWeb; } }

        public static readonly Vector2 TopLeft = new Vector2(0f, 1f), TopRight = Vector2.one, BottomLeft = Vector2.zero, BottomRight = new Vector2(1f, 0f), Mid = new Vector2(0.5f, 0.5f);
        public static readonly Color Ink = RoSoftTheme.Ink, Navy = RoSoftTheme.Hex("#2E4A7A"), TextOnNavy = Color.white;

        // ---- layout (canvas units from the safe-area corner the piece is anchored to) ----
        public const float Margin = 8f;
        // top-left
        public const float PortraitSize = 92f, StatusPlateX = 58f, StatusPlateW = 256f, StatusPlateH = 96f;
        // top-right column
        public const float PillTop = 6f, PillH = 26f, MinimapSize = 150f, MinimapTop = PillTop + PillH + 2f, MinimapRight = 12f;
        public const float WantedTop = MinimapTop + MinimapSize + 6f, WantedW = 204f, WantedH = 100f;
        // menu row, right-aligned to the left of the minimap
        public const float MenuPlate = 56f, MenuPitch = 64f, MenuRight = MinimapRight + MinimapSize + 14f, MenuTop = 14f;
        // bottom-right cluster (centres)
        public const float AttackX = 226f, AttackY = 150f, AttackSize = 104f, SkillArc = 92f, SkillSize = 62f, ArcRing = 196f;
        public const float AutoX = 58f, AutoY = 200f, AutoSize = 68f, RunX = 74f, RunY = 90f, RunSize = 76f;
        public const float PotionY = 306f, PotionSize = 64f, PotionHpX = 124f, PotionMpX = 46f;
        // bottom-left
        public const float JoyX = 122f, JoyY = 178f, JoySize = 164f, KnobSize = 70f, KnobTravel = 48f;
        public const float ChatY = 54f, ChatBubbleSize = 48f, ChatBarW = 356f, ChatBarH = 38f; // the group is centred by RoHudChatBar

        /// <summary>A kit piece (null + one warning when the art is missing: nothing is drawn in its place).</summary>
        public static Sprite Piece(string name) { return RoSoftSprites.Named(name); }

        /// <summary>An image of a kit piece filling `rt`; sliced when the piece has a 9-slice border.</summary>
        public static Image Art(RectTransform rt, string piece, bool raycast = false)
        {
            Image i = rt.gameObject.AddComponent<Image>();
            i.sprite = Piece(piece);
            i.raycastTarget = raycast;
            if (i.sprite != null && i.sprite.border != Vector4.zero) { i.type = Image.Type.Sliced; i.pixelsPerUnitMultiplier = 1f; }
            else i.preserveAspect = true;
            i.enabled = i.sprite != null;
            return i;
        }

        /// <summary>A rect of size (w, h) centred at (x, y) from `corner` of `parent`.</summary>
        public static RectTransform Box(Transform parent, string name, Vector2 corner, float x, float y, float w, float h)
        {
            RectTransform rt = TouchControls.Place(parent, name, corner, x, y, w);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>A rect whose top-left corner sits at (x, y) below / right of the parent's top-left (y grows downwards).</summary>
        public static RectTransform TopLeftBox(Transform parent, string name, float x, float y, float w, float h)
        {
            return UiKit.Rect(parent, name, x, y, w, h);
        }

        /// <summary>Nunito text filling `rt`. `outline` = dark outline for white text over bars and the world.</summary>
        public static Text Label(RectTransform rt, string text, int size, Color color, TextAnchor align, bool bold = true, bool outline = false, bool lightOutline = false)
        {
            Text t = UiKit.Label(rt, text, size, color, align, bold);
            Font f = bold ? RoSoftTheme.Bold : RoSoftTheme.Body;
            if (f != null) t.font = f;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            Shadow sh = t.GetComponent<Shadow>(); // UiKit's black drop shadow: muddy on ivory plates
            if (sh != null) sh.enabled = false;
            if (outline)
            {
                Outline o = t.gameObject.AddComponent<Outline>();
                o.effectColor = lightOutline ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.12f, 0.18f, 0.3f, 0.85f);
                o.effectDistance = new Vector2(1f, -1f);
            }
            return t;
        }

        // Box model of every pill / labelled button (docs/ui-layout-rules.md section 1), canvas units.
        public const int PillPadX = 12, PillPadY = 3, PillGap = 4;

        /// <summary>
        /// A pill laid out like a CSS inline-flex box: 9-slice skin (radius = half the height, from the art), padding PillPadY / PillPadX,
        /// [icon] gap [text] centred vertically, width fitted to the content (ContentSizeFitter), never under `minWidth`.
        /// `pivot` = where it grows from (e.g. (1, 1) for a pill anchored to the top-right). Returns the text.
        /// </summary>
        public static Text Pill(Transform parent, string name, Vector2 corner, Vector2 pivot, float x, float y, float h, string skin, string icon, float iconSize, string text, int fontSize, Color color, out RectTransform rt, float minWidth = 0f, bool raycast = false)
        {
            rt = Box(parent, name, corner, x, y, 40f, h);
            rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(x, y);
            Art(rt, skin, raycast);
            var row = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(PillPadX, PillPadX, PillPadY, PillPadY);
            row.spacing = PillGap;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (minWidth > 0f) rt.gameObject.AddComponent<LayoutElement>().minWidth = minWidth;
            if (icon != null)
            {
                RectTransform ic = UiKit.Rect(rt, "Icon", 0f, 0f, iconSize, iconSize);
                Art(ic, icon);
                var le = ic.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.minWidth = iconSize; le.preferredHeight = le.minHeight = iconSize;
            }
            Text t = Label(UiKit.Rect(rt, "Text", 0f, 0f, 10f, h - PillPadY * 2), text, fontSize, color, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        /// <summary>
        /// PC key cap ("1", "Tab", "B"): a small navy pill with the key's name, centred at (x, y) from `corner` of `parent`, padding 2 / 6.
        /// Null on mobile or for an unbound key (an empty label): nothing to press there.
        /// </summary>
        public static GameObject KeyCap(Transform parent, Vector2 corner, float x, float y, string key, int fontSize = 11)
        {
            if (!Pc || string.IsNullOrEmpty(key)) return null;
            RectTransform rt;
            Pill(parent, "Key", corner, Mid, x, y, fontSize + 7f, "hud-lv-badge", null, 0f, key, fontSize, Color.white, out rt, fontSize + 7f);
            rt.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(6, 6, 2, 2);
            return rt.gameObject;
        }

        /// <summary>A round icon masked by a kit mask piece (portraits, skill icons). Returns the icon image inside the mask.</summary>
        public static Image Masked(RectTransform parent, string maskPiece, float inset)
        {
            RectTransform m = UiKit.Fill(parent, "Mask", inset);
            Image mask = Art(m, maskPiece);
            mask.preserveAspect = false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Image icon = UiKit.Fill(m, "Icon").gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            return icon;
        }

        /// <summary>A horizontal bar: track piece + a fill piece masked to the value (the fill keeps its rounded ends at any value).</summary>
        public sealed class Bar
        {
            public readonly RectTransform Root;
            readonly RectTransform fillArea;
            readonly Image fill;
            float shown = -1f;

            public Bar(Transform parent, string name, float x, float y, float w, float h, string track, string fillPiece)
            {
                Root = TopLeftBox(parent, name, x, y, w, h);
                Art(Root, track);
                fillArea = UiKit.Fill(Root, "FillArea", 1f);
                fillArea.gameObject.AddComponent<RectMask2D>();
                RectTransform f = UiKit.Fill(fillArea, "Fill");
                fill = Art(f, fillPiece);
                f.anchorMin = Vector2.zero; f.anchorMax = Vector2.one; f.pivot = new Vector2(0f, 0.5f);
                Set(1f);
            }

            /// <summary>0..1; the fill slides out of the clip rect from the right (no stretched caps).</summary>
            public void Set(float v)
            {
                v = Mathf.Clamp01(v);
                if (Mathf.Abs(v - shown) < 0.002f) return;
                shown = v;
                RectTransform f = fill.rectTransform;
                float w = fillArea.rect.width;
                f.offsetMin = new Vector2(-(1f - v) * w, 0f);
                f.offsetMax = new Vector2(-(1f - v) * w, 0f);
            }

            /// <summary>Re-applies the value after a layout change (the clip width is only known once the canvas laid out).</summary>
            public void Relayout() { float v = shown; shown = -1f; Set(v); }
        }
    }
}

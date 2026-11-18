using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Puts the RO Soft Fantasy look (RoSoftTheme / RoSoftSprites) on a window built by the classic gold-and-navy builders (UiKit, UiWinKit,
    /// BagWidgets) without touching what the window does: a skin pass over its Images and Texts, repeated every frame while the window shows,
    /// so rows built later and colours the window code sets on refresh are re-skinned before they are drawn.
    ///   Light (menus): win-panel -> ivory card (a panel inside a card -> white section), win-section -> white row, slots -> light item cells,
    ///                  btn-gold -> mint primary, btn-navy -> sky secondary (Discard / Cancel ... -> coral), tabs -> pills, close -> round X;
    ///                  text -> Nunito, light colours -> ink / darker gold, green, red (RoSoftTheme.OnLight), white on coloured buttons.
    ///   Dark (fight HUD): plates -> blue-black 90 % rounded plates; text stays white.
    /// Attach once after the window is built: RoSoftSkin.Light(root). Does nothing with --ui-style classic.
    /// </summary>
    public sealed class RoSoftSkin : MonoBehaviour
    {
        public enum Mode { Light, Dark }

        Dictionary<Sprite, RoSoftSprites.Shape> classic; // old art -> the shape that replaces it (per window: BagSprites repaints on a new scene)
        Sprite classicClose, portraitBackdrop;
        static readonly string[] DangerWords = { "Discard", "Cancel", "Decline", "Delete", "Kick", "Leave", "Abandon", "Remove", "Reject" };

        Mode mode;
        readonly List<Graphic> graphics = new List<Graphic>();
        readonly Dictionary<Text, Color> textSet = new Dictionary<Text, Color>(); // the colour this pass gave each text
        readonly HashSet<Text> fontDone = new HashSet<Text>();

        public static RoSoftSkin Light(Transform root) { return Attach(root, Mode.Light); }
        public static RoSoftSkin Dark(Transform root) { return Attach(root, Mode.Dark); }

        static RoSoftSkin Attach(Transform root, Mode m)
        {
            if (root == null || !UiStyle.Ro) return null;
            RoSoftSkin s = root.GetComponent<RoSoftSkin>();
            if (s == null) s = root.gameObject.AddComponent<RoSoftSkin>();
            s.mode = m;
            s.Apply();
            return s;
        }

        void BuildTable()
        {
            if (classic != null) return;
            classic = new Dictionary<Sprite, RoSoftSprites.Shape>();
            Add(UiArt.Win("panel"), RoSoftSprites.Shape.Card);
            Add(UiArt.Win("dialogue"), RoSoftSprites.Shape.Card);
            Add(UiArt.Win("toast"), RoSoftSprites.Shape.Glass);
            Add(UiArt.Win("section"), RoSoftSprites.Shape.Row);
            Add(UiArt.Win("slot"), RoSoftSprites.Shape.Cell);
            Add(UiArt.Win("btn-gold"), RoSoftSprites.Shape.Primary);
            Add(UiArt.Win("btn-navy"), RoSoftSprites.Shape.Secondary);
            Add(UiArt.Win("tab-active"), RoSoftSprites.Shape.TabOn);
            Add(UiArt.Win("tab-inactive"), RoSoftSprites.Shape.TabOff);
            Add(UiArt.Win("nameplate"), RoSoftSprites.Shape.Pill);
            Add(UiArt.Win("chat-input"), RoSoftSprites.Shape.Row);
            Add(UiArt.Panel(), RoSoftSprites.Shape.Card);
            Add(BagSprites.Panel(), RoSoftSprites.Shape.Section);
            Add(BagSprites.SlotBack(), RoSoftSprites.Shape.Cell);
            portraitBackdrop = BagSprites.PortraitBackdrop();
            Add(portraitBackdrop, RoSoftSprites.Shape.Section);
            // party / trade pieces (PartyArt, TradeArt)
            Add(PartyArt.Get("invite-frame"), RoSoftSprites.Shape.Card);
            Add(PartyArt.Get("panel-frame"), RoSoftSprites.Shape.Card);
            Add(PartyArt.Get("row-frame"), RoSoftSprites.Shape.Row);
            Add(PartyArt.Get("btn-accept"), RoSoftSprites.Shape.Primary);
            Add(PartyArt.Get("btn-invite"), RoSoftSprites.Shape.Primary);
            Add(PartyArt.Get("btn-decline"), RoSoftSprites.Shape.Danger);
            Add(PartyArt.Get("btn-cancel"), RoSoftSprites.Shape.Danger);
            Add(PartyArt.Get("btn-leave"), RoSoftSprites.Shape.Danger);
            Add(TradeArt.Get("offer-frame"), RoSoftSprites.Shape.Section);
            Add(TradeArt.Get("offer-frame-locked"), RoSoftSprites.Shape.SectionLocked);
            Add(TradeArt.Get("gold-field"), RoSoftSprites.Shape.Pill);
            Add(TradeArt.Get("title-plaque"), RoSoftSprites.Shape.Pill);
            Add(TradeArt.Get("btn-disabled"), RoSoftSprites.Shape.Disabled);
            Add(TradeArt.Get("btn-confirm"), RoSoftSprites.Shape.Primary);
            Add(TradeArt.Get("slot-locked"), RoSoftSprites.Shape.Cell);
            classicClose = UiArt.Win("close");
        }

        void Add(Sprite s, RoSoftSprites.Shape shape) { if (s != null) classic[s] = shape; }

        void LateUpdate() { Apply(); }

        public void Apply()
        {
            BuildTable();
            GetComponentsInChildren(false, graphics); // shown parts only: a closed window costs nothing, a part is skinned the frame it shows
            for (int i = 0; i < graphics.Count; i++)
            {
                Graphic g = graphics[i];
                Image img = g as Image;
                if (img != null) { SkinImage(img); continue; }
                Text t = g as Text;
                if (t != null) SkinText(t);
            }
        }

        // ---- images ----

        void SkinImage(Image img)
        {
            Sprite s = img.sprite;
            if (s == null) { SkinPlain(img); return; }
            if (s == classicClose) { img.sprite = RoSoftSprites.Close(); img.type = Image.Type.Simple; img.preserveAspect = true; return; }
            RoSoftSprites.Shape shape;
            if (!classic.TryGetValue(s, out shape)) return; // not old frame art (icons, rarity rings, portraits ...): keep
            if (mode == Mode.Dark) shape = shape == RoSoftSprites.Shape.Cell ? RoSoftSprites.Shape.NightCell : RoSoftSprites.Shape.Night;
            else
            {
                if (shape == RoSoftSprites.Shape.Card && InsideCard(img.transform)) shape = RoSoftSprites.Shape.Section;
                if (RoSoftSprites.IsButton(shape) && IsDanger(img.gameObject.name)) shape = RoSoftSprites.Shape.Danger;
                if (s == portraitBackdrop) img.color = RoSoftTheme.MintMist;
            }
            img.sprite = RoSoftSprites.Get(shape);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.preserveAspect = false;
        }

        /// <summary>Sprite-less fills: UiKit.Button navy faces -> sky buttons, dark plates -> sky mist, gold rules -> hairlines, dimmers lighter.</summary>
        void SkinPlain(Image img)
        {
            if (img.color.a <= 0.01f || img.GetComponent<Mask>() != null) return;
            if (mode == Mode.Dark) return;
            Rect r = img.rectTransform.rect;
            if (img.color.maxColorComponent < 0.12f && img.color.a < 0.8f) // dimmer / tap-catcher behind a window or popup
            {
                if (img.color.a > 0.36f) img.color = new Color(0.1f, 0.16f, 0.22f, 0.36f);
                return;
            }
            if (img.GetComponent<Button>() != null && img.color.maxColorComponent < 0.5f && img.color.a >= 0.8f && r.width < 420f && r.height < 120f) // UiKit.Button face
            {
                img.sprite = RoSoftSprites.Get(IsDanger(img.gameObject.name) ? RoSoftSprites.Shape.Danger : RoSoftSprites.Shape.Secondary);
                img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.color = Color.white;
                return;
            }
            Color c = RoSoftTheme.FillOnLight(img.color);
            if (c == img.color) return;
            img.color = c;
            foreach (Shadow line in img.GetComponents<Shadow>()) line.effectColor = RoSoftTheme.Line; // Outline rims of plates (Settings rows)
        }

        bool InsideCard(Transform t)
        {
            for (Transform p = t.parent; p != null && p != transform.parent; p = p.parent)
            {
                Image pi = p.GetComponent<Image>();
                if (pi != null && pi.sprite == RoSoftSprites.Get(RoSoftSprites.Shape.Card)) return true;
            }
            return false;
        }

        static bool IsDanger(string name)
        {
            foreach (string w in DangerWords) if (name.IndexOf(w, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        // ---- texts ----

        void SkinText(Text t)
        {
            if (!fontDone.Contains(t))
            {
                fontDone.Add(t);
                bool bold = t.fontStyle == FontStyle.Bold || t.fontStyle == FontStyle.BoldAndItalic || (t.font != null && (t.font.name.Contains("Bold") || t.font.name.Contains("Baloo")));
                if (t.font != null && t.font.name.Contains("Crimson")) t.fontSize = Mathf.Max(11, Mathf.RoundToInt(t.fontSize * 0.86f)); // Crimson runs small for its size
                t.font = bold ? RoSoftTheme.Bold : RoSoftTheme.Body;
                t.fontStyle = FontStyle.Normal;
            }
            if (mode == Mode.Dark) return;
            Color done;
            if (textSet.TryGetValue(t, out done) && t.color == done) return;
            bool onButton = OnButton(t.transform);
            Color c = onButton ? new Color(1f, 1f, 1f, t.color.a)
                : t.fontSize >= 20 && t.color.maxColorComponent > 0.5f ? new Color(RoSoftTheme.Ink.r, RoSoftTheme.Ink.g, RoSoftTheme.Ink.b, t.color.a) // window titles: ink, like the mockups
                : RoSoftTheme.OnLight(t.color);
            t.color = c;
            textSet[t] = c;
            bool white = c.r > 0.9f && c.g > 0.9f && c.b > 0.9f;
            foreach (Shadow sh in t.GetComponents<Shadow>()) // Outline derives from Shadow
            {
                sh.enabled = white;
                if (white) { sh.effectColor = new Color(0.1f, 0.25f, 0.3f, 0.35f); sh.effectDistance = new Vector2(0f, -1f); }
            }
        }

        bool OnButton(Transform t)
        {
            Transform p = t.parent;
            for (int i = 0; i < 2 && p != null; i++, p = p.parent)
            {
                Image pi = p.GetComponent<Image>();
                RoSoftSprites.Shape shape;
                if (pi != null && pi.sprite != null && RoSoftSprites.ShapeOf(pi.sprite, out shape)) return RoSoftSprites.IsButton(shape);
            }
            return false;
        }
    }
}

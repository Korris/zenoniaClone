using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Hero select layout (RO Soft Adventure, 26/11/2026), from ChatGPT's mockup docs/reference/hero-select-ro-adventure/mockup-chatgpt-1.png. All art is ChatGPT's, cut by
    /// Tools/cut-ui-paper-sheet.py (Tools/ui-ro-soft-sheets/hero-select-ro-*.json) into Sprites/ui/hero-select-ro. Numbers are units of a 1280 x 720 stage that is scaled to fit the
    /// screen and centred; the painted courtyard covers the whole screen behind it. Six classes stand in six even columns, each under its banner on a pedestal with a name plate;
    /// the heroes themselves are the real in-game layered sprites (HeroPreviewStage, idle facing down), so the picker always shows the V3 look. The panels are in .Panels.cs.
    /// </summary>
    public sealed partial class HeroSelectScreen
    {
        const float StageW = 1280f, StageH = 720f;
        const float ColumnStep = 193f;                 // mockup 252 px of 1672
        const float FeetY = 378f;                      // where every hero's feet stand
        const float HeroViewH = 232f;                  // on-screen height of the hero strip: HeroPreviewStage.ViewH world units
        const float PickedScale = 1.12f, PickedBanner = 1.08f;
        const float BannerTop = 92f, BannerH = 138f;
        const float PedestalW = 170f, PedestalAspect = 500f / 197f, PedestalFeet = 0.36f; // feet at the centre of the pedestal's top ellipse: row 71 of 197
        const float PlateW = 140f, PlateH = 34f, PlateBelowFeet = 44f;
        const float RingW = 176f, RingH = 68f;
        static readonly Color Ink = new Color32(0x34, 0x2F, 0x2B, 0xFF), Cream = new Color32(0xFF, 0xF8, 0xED, 0xFF);

        RectTransform bannerLayer, pedestalLayer;
        RawImage heroStrip;
        HeroPreviewStage heroes;

        static float ColumnX(int i) { return StageW * 0.5f + (i - 2.5f) * ColumnStep; }

        static Font serif, serifRegular;
        static Font Serif { get { return serif != null ? serif : serif = Resources.Load<Font>("Fonts/CrimsonText/CrimsonText-Bold"); } }
        static Font SerifRegular { get { return serifRegular != null ? serifRegular : serifRegular = Resources.Load<Font>("Fonts/CrimsonText/CrimsonText-Regular"); } }

        void Build()
        {
            root = UiKit.Fill(UiKit.Canvas(transform).transform, "HeroSelect");
            UiKit.Panel(root, Color.black, true); // also stops every touch reaching the game below

            RectTransform bg = UiKit.Fill(root, "Background");
            Image bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.sprite = Art("bg");
            bgImage.raycastTarget = false;
            AspectRatioFitter fit = bg.gameObject.AddComponent<AspectRatioFitter>(); // covers any screen shape
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = bgImage.sprite != null ? bgImage.sprite.rect.width / bgImage.sprite.rect.height : StageW / StageH;

            stage = new GameObject("Stage", typeof(RectTransform)).GetComponent<RectTransform>();
            stage.SetParent(root, false);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(StageW, StageH);

            ArtRect(stage, "Logo", "logo", 18f, 6f, 214f, 86f);
            Image title = ArtRect(stage, "Title", "title-banner", StageW * 0.5f, 8f, 480f, 94f, new Vector2(0.5f, 1f));
            Text titleText = Txt(title.rectTransform, "Text", 0f, 0f, 480f, 80f, "Select Class", 34, Ink, TextAnchor.MiddleCenter, Serif);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, 4f); // ChatGPT review: 8 units higher than first built, off the plate's lower edge

            bannerLayer = Layer("Banners");
            pedestalLayer = Layer("Pedestals");
            // The heroes: one strip of six columns filmed by their own camera, standing on the pedestals (the strip's bottom is FeetFromBottom world units below the feet).
            float stripW = ColumnStep * 6f, feetPad = HeroPreviewStage.FeetFromBottom / HeroPreviewStage.ViewH * HeroViewH;
            heroStrip = UiKit.Rect(stage, "Heroes", ColumnX(0) - ColumnStep * 0.5f, FeetY + feetPad - HeroViewH, stripW, HeroViewH).gameObject.AddComponent<RawImage>();
            heroes = HeroPreviewStage.Create(transform, heroStrip, PickerClasses.Length, ColumnStep / HeroViewH * HeroPreviewStage.ViewH, new Vector3(-30000f, -22000f, 0f)); // own spot: the character list films (-30000, -26000)

            BuildSlots();
            BuildInfoPanel();
            BuildSkillsPanel();
            BuildPreviewPanel();
            BuildButtons();
            FitStage();
        }

        RectTransform Layer(string name)
        {
            RectTransform rt = UiKit.Rect(stage, name, 0f, 0f, StageW, StageH);
            return rt;
        }

        /// <summary>Follows the window: the stage shrinks or grows to fit the screen, centred.</summary>
        void FitStage()
        {
            float s = Mathf.Min(root.rect.width / StageW, root.rect.height / StageH);
            if (s <= 0f) return;
            stage.localScale = new Vector3(s, s, 1f);
        }

        // ---- heroes ----

        /// <summary>The classes on the picker, left to right as in ChatGPT's mockup. The Fighter (old "boxing" slot) is no longer offered; its enum value stays for old saves.</summary>
        internal static readonly HeroClass[] PickerClasses = { HeroClass.Warrior, HeroClass.Wizard, HeroClass.Mechanic, HeroClass.Assassin, HeroClass.Cleric, HeroClass.Archer };
        // Hair colour each class shows on the picker (palettes.json hair index), nearest to its V3 design: starlight blonde, twilight lavender, moon silver, sakura rose, golden honey, story chestnut.
        static readonly int[] PickerHair = { 8, 6, 3, 7, 2, 0 };
        static readonly Rpg.CharacterAppearanceData[] pickerLooks = new Rpg.CharacterAppearanceData[6];

        static Rpg.CharacterAppearanceData PickerLook(int i)
        {
            return pickerLooks[i] ?? (pickerLooks[i] = new Rpg.CharacterAppearanceData { classId = RpgSession.ClassId(PickerClasses[i]), stored = true, hairColorIndex = PickerHair[i] });
        }

        void BuildSlots()
        {
            for (int i = 0; i < PickerClasses.Length; i++) AddSlot(i, PickerClasses[i]);
        }

        void AddSlot(int i, HeroClass id)
        {
            string art = CharacterFlowKit.ClassArtName(id);
            var s = new Slot { ClassIndex = (int)id, Art = art };
            float x = ColumnX(i);

            s.BannerImage = ArtRect(bannerLayer, "Banner_" + art, "banner-" + art, x, BannerTop, 96f, BannerH, new Vector2(0.5f, 1f));
            s.Banner = s.BannerImage.rectTransform;

            float ph = PedestalW / PedestalAspect;
            s.Pedestal = ArtRect(pedestalLayer, "Pedestal_" + art, "pedestal", x, FeetY, PedestalW, ph, new Vector2(0.5f, 1f - PedestalFeet)).rectTransform;
            s.Ring = ArtRect(pedestalLayer, "Ring_" + art, "picked-ring-white", x, FeetY, RingW, RingH, new Vector2(0.5f, 0.5f));
            s.Ring.preserveAspect = false;
            s.Ring.color = new Color(1f, 0.83f, 0.45f, 0f); // warm gold (rim #FFC65D, core #FFF5CC) on the grey ring; alpha set by Ease

            // Name plate: a box like CSS (padding 0 / 14, icon-text gap 6, vertically centred), the selected skin swapped in when picked.
            RectTransform plate = UiKit.Rect(pedestalLayer, "Plate_" + art, x - PlateW * 0.5f, FeetY + PlateBelowFeet - PlateH * 0.5f, PlateW, PlateH);
            s.PlateImage = Sliced(plate.gameObject.AddComponent<Image>(), "name-plate", new Vector4(48f, 12f, 48f, 12f), 79f / PlateH);
            var row = plate.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(14, 14, 0, 0);
            row.spacing = 6f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            Image icon = ArtRect(plate, "Icon", "class-" + art, 0f, 0f, 22f, 22f);
            var le = icon.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = 20f; le.preferredHeight = le.minHeight = 24f;
            s.Name = Txt(plate, "Name", 0f, 0f, 90f, PlateH, HeroClassDef.Get(id).Name, 17, Cream, TextAnchor.MiddleLeft, Serif);
            s.Name.alignByGeometry = true;
            var nle = s.Name.gameObject.AddComponent<LayoutElement>();
            nle.preferredHeight = PlateH;

            // Tall tap target around the hero and its pedestal (a small hero on a phone is hard to hit otherwise); the capture script presses "Pick<class>".
            RectTransform hit = UiKit.Rect(stage, "Pick" + (int)id, x - ColumnStep * 0.5f + 4f, FeetY - 250f, ColumnStep - 8f, 320f);
            Image hitImage = UiKit.Panel(hit, new Color(0f, 0f, 0f, 0f), true);
            Button b = hit.gameObject.AddComponent<Button>();
            b.targetGraphic = hitImage;
            b.transition = Selectable.Transition.None;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            int index = i;
            b.onClick.AddListener(() => Pick(index));
            slots.Add(s);
        }

        /// <summary>Art image at stage coordinates from the top-left (y grows down); pivot picks which point of the image sits there.</summary>
        static Image ArtRect(Transform parent, string name, string art, float x, float y, float w, float h, Vector2? pivot = null)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            rt.pivot = pivot ?? new Vector2(0f, 1f);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = art != null ? Art(art) : null;
            if (art != null && img.sprite == null) img.enabled = false; // missing art: nothing, not a white box
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A 9-slice skin (border in art px: left, bottom, right, top); density = art px per UI unit, so the painted corners keep their look at any box size.</summary>
        static Image Sliced(Image img, string art, Vector4 border, float density)
        {
            img.sprite = UiArt.Get("hero-select-ro/" + art, border);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = density;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Text in the screen's two fonts (Crimson Text serif for titles, Nunito for body), no shadow: dark ink on paper.</summary>
        static Text Txt(Transform parent, string name, float x, float y, float w, float h, string text, int size, Color color, TextAnchor align, Font font = null)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = font ?? RoSoftTheme.Body;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        static Button ArtButton(Transform parent, string name, string art, float x, float y, float size, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, size, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Art(art);
            img.preserveAspect = true;
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(onClick);
            return b;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Hero select layout. Numbers are the mock's pixels x 0.68, on a 1138 x 640 layout (16:9). The panels shrink to fit a narrower screen, pinned to the bottom; heroes and banners share six even columns that keep the
    /// background's scale and only spread with the screen width. Title and gear keep to the screen's top corners.
    /// </summary>
    public sealed partial class HeroSelectScreen
    {
        const float StageW = 1138f, StageH = 640f;
        const float HeroSize = 218f;        // drawn size of the square hero image
        const float HeroFeet = 0.03125f;    // height of the feet inside the hero image: row 744 of 768
        const float PickedScale = 1.2f, PickedPedestal = 1.14f, PickedBanner = 1.25f;
        const float PedestalAspect = 1024f / 357f, PedestalTop = 0.1f;      // where the feet stand: row 36 of the art
        const float PlaqueBelowFeet = 0.365f, NameWidth = 0.46f;            // the plaque's centre (row 166) as a fraction of the pedestal's height; name width of its width

        // Left to right as in the mock.                        Mechanic  Wizard  Warrior  Assassin  Boxing  Cleric
        static readonly float[] FeetY =                           { 378f, 395f, 388f, 395f, 398f, 398f };
        static readonly float[] HeroScale =                       { 1.06f, 1f, 1f, 0.8f, 0.83f, 0.88f }; // Northwind and Warrior use the same anatomical height and display scale.
        const float PedestalWidth = 170f;
        static readonly float[] HeroNudge =                       { 0f, 0f, 0f, 0f, 0f, 14f };  // the Cleric's face sits left of her feet: under the banner by the face
        const float ColumnStep = 170f, ColumnStepOfWidth = ColumnStep / StageW, MinColumnStep = 130f, MaxColumnStep = 205f; // six even columns: a banner, and its hero under it
        static readonly float[] BannerTop =                       { 78f, 24f, 92f, 146f, 160f, 129f };
        static readonly float[] BannerW =                         { 95f, 98f, 88f, 85f, 78f, 88f };  // the mock's Wizard is picked: 122 x 185 there
        static readonly float[] BannerH =                         { 146f, 148f, 136f, 116f, 105f, 126f };

        RectTransform slotsLayer, bannerLayer;

        /// <summary>Column centre at the 16:9 layout; FitStage spreads or tightens the columns evenly from there.</summary>
        static float ColumnBase(int i) { return StageW * 0.5f + (i - 2.5f) * ColumnStep; }

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

            // Behind every hero, in the heroes' frame: a banner shares its hero's column on any screen.
            bannerLayer = new GameObject("Banners", typeof(RectTransform)).GetComponent<RectTransform>();
            bannerLayer.SetParent(root, false);
            bannerLayer.anchorMin = bannerLayer.anchorMax = bannerLayer.pivot = new Vector2(0.5f, 1f);
            bannerLayer.sizeDelta = new Vector2(StageW, StageH);

            // Heroes keep the background's scale, so the arches stay right above their heads on any screen; a narrow screen only moves them closer together (FitStage).
            slotsLayer = new GameObject("Slots", typeof(RectTransform)).GetComponent<RectTransform>();
            slotsLayer.SetParent(root, false);
            slotsLayer.anchorMin = slotsLayer.anchorMax = slotsLayer.pivot = new Vector2(0.5f, 1f);
            slotsLayer.sizeDelta = new Vector2(StageW, StageH);

            stage = new GameObject("Stage", typeof(RectTransform)).GetComponent<RectTransform>();
            stage.SetParent(root, false);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(0.5f, 0f);
            stage.sizeDelta = new Vector2(StageW, StageH);

            BuildSlots();
            BuildInfoPanel();
            BuildSkillsPanel();
            BuildPreviewPanel();
            BuildStartPanel();

            Image titleImage = ArtRect(root, "Title", "title", 0f, 0f, 178f, 62f);
            titleImage.color = Gold; // painted pale so it can be tinted
            titleRt = titleImage.rectTransform;
            // The mock's corner is dark behind the title; the small line under it is unreadable on the bright stone otherwise.
            Image shade = ArtRect(titleRt, "Shade", null, -60f, -36f, 300f, 134f);
            shade.preserveAspect = false;
            shade.sprite = SoftDisc();
            shade.color = new Color(0f, 0f, 0f, 0.8f);
            Image titleArt = ArtRect(titleRt, "Art", "title", 0f, 0f, 178f, 62f);
            titleArt.color = Gold;
            titleImage.enabled = false;
            titleRt.anchorMin = titleRt.anchorMax = titleRt.pivot = new Vector2(0f, 1f);
            gearButton = IconButton(root, "Gear", UiArt.Get("hud-settings"), 0f, 0f, 46f, ConnectScreenGui.Open); // the mock's gear: online / host (dev) lives behind it
            gearRt = (RectTransform)gearButton.transform;
            gearRt.anchorMin = gearRt.anchorMax = gearRt.pivot = new Vector2(1f, 1f);
            FitStage();
        }

        /// <summary>Follows the window: the stage shrinks to the screen width, the corner pieces shrink with it.</summary>
        void FitStage()
        {
            float s = Mathf.Min(1f, root.rect.width / StageW);
            if (s <= 0f) return;
            stage.localScale = new Vector3(s, s, 1f);
            float step = Mathf.Clamp(root.rect.width * ColumnStepOfWidth, MinColumnStep, MaxColumnStep);
            for (int i = 0; i < slots.Count; i++)
            {
                float shift = (i - (slots.Count - 1) * 0.5f) * (step - ColumnStep);
                slots[i].Group.anchoredPosition = new Vector2(shift, 0f);
                slots[i].Banner.anchoredPosition = new Vector2(ColumnBase(i) + shift, -BannerTop[i]);
            }
            titleRt.localScale = gearRt.localScale = new Vector3(s, s, 1f);
            titleRt.anchoredPosition = new Vector2(20f * s, -14f * s);
            gearRt.anchoredPosition = new Vector2(-15f * s, -17f * s);
        }

        // ---- heroes ----

        void BuildSlots()
        {
            AddSlot(0, (int)HeroClass.Mechanic, "mechanic", new Color(1f, 0.66f, 0.24f));
            AddSlot(1, (int)HeroClass.Wizard, "wizard", new Color(0.42f, 0.56f, 1f));
            AddSlot(2, (int)HeroClass.Warrior, "warrior", new Color(1f, 0.32f, 0.24f));
            AddSlot(3, (int)HeroClass.Assassin, "assassin", new Color(0.72f, 0.45f, 1f));
            AddSlot(4, (int)HeroClass.Fighter, "boxing", new Color(1f, 0.48f, 0.18f)); // the Fighter keeps the mock's "boxing" art names
            AddSlot(5, (int)HeroClass.Cleric, "cleric", new Color(0.42f, 0.95f, 0.58f));
        }

        void AddSlot(int i, int classIndex, string art, Color color)
        {
            var s = new Slot { ClassIndex = classIndex, Art = art, Color = color, Scale = HeroScale[i] };
            float pw = PedestalWidth, ph = pw / PedestalAspect, plaque = ph * (PedestalTop + PlaqueBelowFeet);

            // Each hero is one group so the picked one, moved last, covers its neighbours completely.
            RectTransform group = UiKit.Rect(slotsLayer, "Slot_" + art, 0f, 0f, StageW, StageH);
            s.Group = group;

            s.BannerImage = ArtRect(bannerLayer, "Banner_" + art, "banner-" + art, ColumnBase(i), BannerTop[i], BannerW[i], BannerH[i], new Vector2(0.5f, 1f));
            s.BannerImage.preserveAspect = false; // the mock's banners are not all the same shape
            s.Banner = s.BannerImage.rectTransform;
            s.PedestalImage = ArtRect(group, "Pedestal", "pedestal", ColumnBase(i), FeetY[i], pw, ph, new Vector2(0.5f, 1f - PedestalTop));
            s.Pedestal = s.PedestalImage.rectTransform;
            s.NameImage = ArtRect(s.Pedestal, "Name", "name-" + art, pw * 0.5f, plaque, pw * NameWidth, ph * 0.2f, new Vector2(0.5f, 0.5f));
            // The painted line and diamonds, lit as in the mock: a white-hot line over its own shape blurred, in saturated additive light.
            float h, sat, val;
            Color.RGBToHSV(color, out h, out sat, out val);
            Color light = Color.HSVToRGB(h, 0.95f, 1f);
            float lineW = pw * 0.52f, lineH = lineW * 92f / 1024f, lineY = plaque + ph * 0.15f;
            float boxW = lineW * 1120f / 1024f, boxH = lineH * 188f / 92f; // name-underline-glow is the line's art blurred, 48 px bigger on every side
            Image haze = ArtRect(s.Pedestal, "Underline", "name-underline-glow", pw * 0.5f, lineY, boxW, boxH, new Vector2(0.5f, 0.5f));
            haze.preserveAspect = false;
            haze.color = light;
            haze.material = Additive();
            s.Underline = haze.gameObject;
            Image hazeTwice = ArtRect(haze.rectTransform, "Glow2", "name-underline-glow", boxW * 0.5f, boxH * 0.5f, boxW, boxH, new Vector2(0.5f, 0.5f)); // additive, so a second copy is brighter light, not a thicker shape
            hazeTwice.preserveAspect = false;
            hazeTwice.color = light;
            hazeTwice.material = Additive();
            Image line = ArtRect(haze.rectTransform, "Line", "name-underline", boxW * 0.5f, boxH * 0.5f, lineW, lineH, new Vector2(0.5f, 0.5f));
            line.color = Color.Lerp(light, Color.white, 0.95f);
            s.NamePicked = Color.Lerp(color, Color.white, 0.72f);
            s.NameImage.transform.SetAsLastSibling(); // the light stays behind the letters
            s.Glow = ArtRect(s.Pedestal, "Glow", null, pw * 0.5f, ph * PedestalTop, pw * 0.85f, 46f, new Vector2(0.5f, 0.5f));
            s.Glow.preserveAspect = false;
            s.Glow.sprite = SoftDisc();
            Color lit = color; lit.a = 0f;
            s.Glow.color = lit;
            s.HeroImage = ArtRect(group, "Hero", "hero-" + art, ColumnBase(i) + HeroNudge[i], FeetY[i], HeroSize, HeroSize, new Vector2(0.5f, HeroFeet));
            s.Hero = s.HeroImage.rectTransform;

            // Tall tap target around the hero: a small hero on a phone is hard to hit otherwise.
            RectTransform hit = UiKit.Rect(group, "Pick" + (classIndex == NotPlayable ? "Boxing" : classIndex.ToString()), ColumnBase(i) - 65f, 150f, 130f, 300f);
            Image hitImage = UiKit.Panel(hit, new Color(0f, 0f, 0f, 0f), true);
            Button b = hit.gameObject.AddComponent<Button>();
            b.targetGraphic = hitImage;
            b.transition = Selectable.Transition.None;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() => Pick(i));
            slots.Add(s);
        }

        /// <summary>Art image at stage coordinates from the top-left (y grows down); pivot picks which point of the image sits there.</summary>
        static Image ArtRect(Transform parent, string name, string art, float x, float y, float w, float h, Vector2? pivot = null)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            rt.pivot = pivot ?? new Vector2(0f, 1f);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = art != null ? Art(art) : null;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        static Material additive;

        static Material Additive()
        {
            if (additive == null) additive = new Material(Resources.Load<Shader>("Shaders/UiAdditive"));
            return additive;
        }

        static Sprite softDisc;

        /// <summary>White disc fading out to the rim, for the light under the picked hero's feet.</summary>
        static Sprite SoftDisc()
        {
            if (softDisc != null) return softDisc;
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float a = Mathf.Clamp01(1f - new Vector2(x + 0.5f - S * 0.5f, y + 0.5f - S * 0.5f).magnitude / (S * 0.5f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            softDisc = Sprite.Create(tex, new Rect(0f, 0f, S, S), new Vector2(0.5f, 0.5f), 100f);
            return softDisc;
        }

        // ---- panels ----

        RectTransform Plate(string name, float x, float y, float w, float h)
        {
            RectTransform rt = UiKit.Rect(stage, name, x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            Sprite s = UiArt.Get("hero-select/panel-frame", new Vector4(PanelSlice, PanelSlice, PanelSlice, PanelSlice));
            img.sprite = s;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = PanelDensity;
            return rt;
        }

        const float PanelSlice = 38f, PanelDensity = 3f; // art pixels of the corner ornaments, and how many fit one UI unit

        void BuildInfoPanel()
        {
            RectTransform p = Plate("Info", 10f, 459f, 355f, 157f);
            portrait = ArtRect(p, "Portrait", null, 10f, 8f, 133f, 141f);
            infoName = ArtRect(p, "InfoName", null, 158f, 9f, 150f, 20f);
            tagline = UiKit.Label(p, "Tagline", 158f, 33f, 190f, 34f, "", 11, new Color(0.88f, 0.92f, 1f), TextAnchor.UpperLeft);
            string[] names = { "ATK", "DEF", "HP", "MP", "CRIT" };
            string[] icons = { "stat-atk", "stat-def", "stat-hp", "stat-mp", "stat-crit" };
            for (int i = 0; i < StatCount; i++)
            {
                float y = 73f + i * 15.5f;
                ArtRect(p, "StatIcon" + i, icons[i], 158f, y, 12f, 12f);
                UiKit.Label(p, "Stat" + i, 173f, y - 1f, 34f, 14f, names[i], 10, new Color(0.88f, 0.92f, 1f));
                RectTransform track = UiKit.Rect(p, "Track" + i, 208f, y + 3f, 108f, 6f);
                UiKit.Panel(track, new Color(0.08f, 0.1f, 0.16f, 1f));
                statFills[i] = UiKit.Panel(UiKit.Fill(track, "Fill"), Color.white);
                statValues[i] = UiKit.Label(p, "Value" + i, 318f, y - 1f, 30f, 14f, "", 10, Color.white, TextAnchor.MiddleRight, true);
            }
        }

        void BuildSkillsPanel()
        {
            const float W = 288f;
            RectTransform p = Plate("Skills", 381f, 495f, W, 121f);
            skillHeader = ArtRect(p, "Header", "label-skill-preview", 44f, 12f, 200f, 12f);
            skillHeader.color = Gold;
            skillNameText = UiKit.Label(p, "SkillName", 8f, 8f, W - 16f, 20f, "", 12, Gold, TextAnchor.MiddleCenter, true);
            const float Cell = 49f, Step = 53f;
            float x0 = (W - Step * VisibleSkills + (Step - Cell)) * 0.5f;
            for (int i = 0; i < VisibleSkills; i++)
            {
                RectTransform cell = UiKit.Rect(p, "Skill" + i, x0 + i * Step, 34f, Cell, Cell);
                skillFrames[i] = ArtRect(cell, "Frame", "icon-frame", 0f, 0f, Cell, Cell);
                skillIcons[i] = ArtRect(cell, "Icon", null, 3f, 3f, Cell - 6f, Cell - 6f);
                skillFrames[i].transform.SetAsLastSibling();
                Image hit = UiKit.Panel(cell, new Color(0f, 0f, 0f, 0f), true);
                Button b = cell.gameObject.AddComponent<Button>();
                b.targetGraphic = hit;
                b.transition = Selectable.Transition.None;
                b.navigation = new Navigation { mode = Navigation.Mode.None };
                int index = i;
                b.onClick.AddListener(() => { if (skillOffset + index < skills.Count) ShowSkillName(skills[skillOffset + index]); });
            }
            Button left = IconButton(p, "SkillPrev", Art("arrow-left"), 2f, 48f, 18f, () => PageSkills(-1));
            Button right = IconButton(p, "SkillNext", Art("arrow-left"), W - 20f, 48f, 18f, () => PageSkills(1));
            right.transform.localScale = new Vector3(-1f, 1f, 1f);
            right.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            right.GetComponent<RectTransform>().anchoredPosition += new Vector2(9f, 0f);

            RectTransform slider = (RectTransform)ArtRect(p, "Slider", "slider", 16f, 94f, W - 32f, 10f).transform;
            slider.GetComponent<Image>().preserveAspect = false;
            sliderKnob = ArtRect(slider, "Knob", "slider-knob", 0f, 0f, 12f, 12f, new Vector2(0.5f, 0.5f));
            sliderKnob.rectTransform.anchoredPosition = Vector2.zero;
        }

        void BuildPreviewPanel()
        {
            RectTransform p = Plate("Preview", 677f, 454f, 255f, 164f);
            RectTransform view = UiKit.Rect(p, "View", 5f, 5f, 245f, 154f);
            RawImage raw = view.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            preview = gameObject.AddComponent<HeroSelectPreview>();
            preview.Init(raw);
            IconButton(p, "Replay", Art("play-button"), 213f, 123f, 34f, () => preview.Replay());
        }

        void BuildStartPanel()
        {
            stateText = UiKit.Label(stage, "State", 940f, 503f, 190f, 20f, "", 12, Color.white, TextAnchor.MiddleCenter, true);
            RectTransform rt = UiKit.Rect(stage, "StartButton", 940f, 525f, 190f, 54f);
            Image plate = rt.gameObject.AddComponent<Image>();
            plate.sprite = Art("btn-start");
            startButton = rt.gameObject.AddComponent<Button>();
            startButton.targetGraphic = plate;
            startButton.navigation = new Navigation { mode = Navigation.Mode.None };
            startButton.onClick.AddListener(OnStart);
            startLabel = ArtRect(rt, "Label", null, 95f, 27f, 134f, 22f, new Vector2(0.5f, 0.5f));
            nextLabel = UiKit.Label(UiKit.Fill(rt, "NextLabel"), "NEXT", 26, new Color(0.25f, 0.12f, 0.02f), TextAnchor.MiddleCenter, true);
            nextLabel.gameObject.SetActive(false);
            backButton = IconButton(stage, "BackButton", UiArt.Win("btn-navy"), 972f, 583f, 158f, OnBack);
            var backRt = (RectTransform)backButton.transform;
            backRt.sizeDelta = new Vector2(158f, 38f);
            backButton.GetComponent<Image>().preserveAspect = false;
            backButton.GetComponent<Image>().type = Image.Type.Sliced;
            backButton.GetComponent<Image>().pixelsPerUnitMultiplier = backButton.GetComponent<Image>().sprite.rect.height / 38f;
            UiKit.Label(backRt, "BackLabel", 0f, 0f, 158f, 38f, "BACK", 16, Color.white, TextAnchor.MiddleCenter, true);
        }

        static Button IconButton(Transform parent, string name, Sprite art, float x, float y, float size, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, size, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = art;
            img.preserveAspect = true;
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(onClick);
            return b;
        }
    }
}

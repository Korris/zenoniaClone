using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Hero select, bottom row of ChatGPT's mockup: the class card (portrait, name, tagline, five stat bars in their own colours), the skill row (five round gold-ringed
    /// icons, locked ones dimmed with a padlock, pages of five with arrows and dots; a tap shows the skill's name, level and MP), the live preview of the hero fighting
    /// (HeroSelectPreview) and the NEXT / BACK buttons. Stage units (1280 x 720). Sizes from ChatGPT's review V2 (docs/reference/hero-select-ro-adventure/design-notes-chatgpt.md).
    /// </summary>
    public sealed partial class HeroSelectScreen
    {
        const float PanelTop = 463f, PanelH = 226f;
        const float PaperDensity = 3f;                                      // art px of the paper panels per UI unit (corner ornaments ~21 units)
        static readonly Vector4 PaperBorder = new Vector4(64f, 64f, 64f, 64f);
        const float SkillCell = 50f, SkillGap = 6f;                          // round skill icons 48-50, 5-7 apart
        const int MaxDots = 6;
        static readonly Color[] StatColors = { new Color32(0xB8, 0x6B, 0x50, 0xFF), new Color32(0x65, 0x8F, 0xB7, 0xFF), new Color32(0x76, 0xA1, 0x6B, 0xFF),
                                               new Color32(0x8D, 0x79, 0xBA, 0xFF), new Color32(0xD2, 0xA2, 0x47, 0xFF) }; // ATK DEF HP MP CRIT

        Image infoIcon;
        Text infoName, skillInfo;
        readonly Image[] dots = new Image[MaxDots];
        readonly Image[] skillLocks = new Image[VisibleSkills];
        readonly Image[] skillPicked = new Image[VisibleSkills];
        readonly RectTransform[] skillCells = new RectTransform[VisibleSkills];
        const float SkillPanelW = 330f;

        void BuildInfoPanel()
        {
            RectTransform p = UiKit.Rect(stage, "Info", 14f, PanelTop, 424f, PanelH);
            Sliced(p.gameObject.AddComponent<Image>(), "info-panel", PaperBorder, PaperDensity);

            RectTransform pf = UiKit.Rect(p, "PortraitFrame", 12f, 14f, 168f, 198f);
            // The painted frame is a filled paper plate: it goes behind, the portrait sits in its window (inside the gold rim).
            Sliced(UiKit.Rect(pf, "Frame", 0f, 0f, 168f, 198f).gameObject.AddComponent<Image>(), "portrait-frame", new Vector4(44f, 44f, 44f, 44f), 3f);
            portrait = ArtRect(pf, "Portrait", null, 8f, 8f, 152f, 182f); // Tools/cut-hero-select-portraits.py cuts them at this aspect
            portrait.preserveAspect = false;

            // Name row: a box (icon 30, gap 8, vertically centred).
            RectTransform nameRow = UiKit.Rect(p, "NameRow", 190f, 14f, 222f, 38f);
            var row = nameRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            infoIcon = ArtRect(nameRow, "Icon", null, 0f, 0f, 30f, 34f);
            var le = infoIcon.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = 30f; le.preferredHeight = 34f;
            infoName = Txt(nameRow, "Name", 0f, 0f, 190f, 38f, "", 28, Ink, TextAnchor.MiddleLeft, Serif);
            infoName.alignByGeometry = true;
            infoName.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

            tagline = Txt(p, "Tagline", 192f, 54f, 222f, 42f, "", 14, Ink, TextAnchor.UpperLeft);
            tagline.lineSpacing = 0.92f;
            string[] names = { "ATK", "DEF", "HP", "MP", "CRIT" };
            string[] icons = { "stat-atk", "stat-def", "stat-hp", "stat-mp", "stat-crit" };
            for (int i = 0; i < StatCount; i++)
            {
                float y = 108f + i * 23.5f; // row centre
                ArtRect(p, "StatIcon" + i, icons[i], 192f, y - 9f, 18f, 18f);
                Text label = Txt(p, "Stat" + i, 214f, y - 10f, 46f, 20f, names[i], 14, Ink, TextAnchor.MiddleLeft, Serif);
                label.alignByGeometry = true;
                RectTransform track = UiKit.Rect(p, "Track" + i, 260f, y - 6f, 110f, 12f);
                Sliced(track.gameObject.AddComponent<Image>(), "bar-track", new Vector4(24f, 0f, 24f, 0f), 4f);
                RectTransform fill = UiKit.Fill(track, "Fill");
                statFills[i] = Sliced(fill.gameObject.AddComponent<Image>(), "bar-fill-white", new Vector4(24f, 0f, 24f, 0f), 4f);
                statFills[i].color = StatColors[i];
                statValues[i] = Txt(p, "Value" + i, 370f, y - 10f, 38f, 20f, "", 15, Ink, TextAnchor.MiddleRight);
                statValues[i].fontStyle = FontStyle.Bold;
                statValues[i].alignByGeometry = true;
            }
        }

        void BuildSkillsPanel()
        {
            const float W = SkillPanelW;
            RectTransform p = UiKit.Rect(stage, "Skills", 446f, PanelTop, W, PanelH);
            Sliced(p.gameObject.AddComponent<Image>(), "info-panel", PaperBorder, PaperDensity);
            skillNameText = Txt(p, "Header", 16f, 16f, W - 32f, 30f, "Skill Preview", 20, Ink, TextAnchor.MiddleCenter, Serif);
            skillNameText.alignByGeometry = true;

            // Five round icons: the icon under a circular mask (the ring's own hole), the gold ring on top; locked = dimmed + padlock; picked = the glowing ring.
            float rowW = SkillCell * VisibleSkills + SkillGap * (VisibleSkills - 1), x0 = (W - rowW) * 0.5f;
            for (int i = 0; i < VisibleSkills; i++)
            {
                RectTransform cell = skillCells[i] = UiKit.Rect(p, "Skill" + i, x0 + i * (SkillCell + SkillGap), 66f, SkillCell, SkillCell);
                Image hole = ArtRect(cell, "Mask", "skill-ring-hole", 0f, 0f, SkillCell, SkillCell);
                hole.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                skillIcons[i] = ArtRect(hole.rectTransform, "Icon", null, 0f, 0f, SkillCell, SkillCell);
                skillIcons[i].preserveAspect = false;
                ArtRect(cell, "Ring", "skill-ring", 0f, 0f, SkillCell, SkillCell);
                float glow = SkillCell * 141f / 128f;
                skillPicked[i] = ArtRect(cell, "Picked", "skill-ring-selected", SkillCell * 0.5f, SkillCell * 0.5f, glow, glow, new Vector2(0.5f, 0.5f));
                skillPicked[i].enabled = false;
                skillLocks[i] = ArtRect(cell, "Lock", "skill-lock", SkillCell - 14f, SkillCell - 17f, 14.5f, 18f); // ChatGPT V3: a smaller, softer padlock
                skillLocks[i].color = new Color(1f, 1f, 1f, 0.85f);
                Image hit = UiKit.Panel(cell, new Color(0f, 0f, 0f, 0f), true);
                Button b = cell.gameObject.AddComponent<Button>();
                b.targetGraphic = hit;
                b.transition = Selectable.Transition.None;
                b.navigation = new Navigation { mode = Navigation.Mode.None };
                int index = i;
                b.onClick.AddListener(() => { if (skillOffset + index < skills.Count) ShowSkillName(skills[skillOffset + index], index); });
            }
            skillInfo = Txt(p, "SkillInfo", 18f, 124f, W - 36f, 44f, "", 14, Ink, TextAnchor.MiddleCenter);

            // Page row: arrow, dots, arrow (the dots are laid out like CSS: centred, gap 8).
            const float rowY = 178f;
            ArtButton(p, "SkillPrev", "arrow-left", 24f, rowY, 26f, () => PageSkills(-1));
            ArtButton(p, "SkillNext", "arrow-right", W - 50f, rowY, 26f, () => PageSkills(1));
            RectTransform dotRow = UiKit.Rect(p, "Dots", 60f, rowY, W - 120f, 26f);
            var dl = dotRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            dl.spacing = 8f;
            dl.childAlignment = TextAnchor.MiddleCenter;
            dl.childControlWidth = dl.childControlHeight = false;
            dl.childForceExpandWidth = dl.childForceExpandHeight = false;
            for (int i = 0; i < MaxDots; i++) dots[i] = ArtRect(dotRow, "Dot" + i, "dot-off", 0f, 0f, 10f, 10f);
        }

        void BuildPreviewPanel()
        {
            const float W = 282f;
            RectTransform p = UiKit.Rect(stage, "Preview", 784f, PanelTop, W, PanelH);
            RectTransform view = UiKit.Rect(p, "View", 9f, 9f, W - 18f, PanelH - 18f);
            RawImage raw = view.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            // The preview texture is 436 x 274: crop its sides so the hero keeps its shape in the taller frame.
            float u = Mathf.Clamp01(((W - 18f) / (PanelH - 18f)) / (436f / 274f));
            raw.uvRect = new Rect((1f - u) * 0.5f, 0f, u, 1f);
            preview = gameObject.AddComponent<HeroSelectPreview>();
            preview.Init(raw);
            Sliced(UiKit.Fill(p, "Frame").gameObject.AddComponent<Image>(), "preview-frame", new Vector4(48f, 48f, 48f, 48f), 3f);
            ArtButton(p, "Replay", "play", W - 50f, PanelH - 50f, 36f, () => preview.Replay());
        }

        void BuildButtons()
        {
            // NEXT 184 x 58 over BACK 174 x 46, 18 apart, on one vertical axis, the pair centred on the preview panel's height.
            const float NextW = 184f, NextH = 58f, BackW = 174f, BackH = 46f, Gap = 18f, AxisX = 1173f;
            float top = PanelTop + (PanelH - (NextH + Gap + BackH)) * 0.5f;
            stateText = Txt(stage, "State", AxisX - 100f, top - 26f, 200f, 22f, "", 13, Ink, TextAnchor.MiddleCenter);
            startButton = TextButton("StartButton", "btn-next", new Vector4(64f, 24f, 64f, 24f), 117f, AxisX - NextW * 0.5f, top, NextW, NextH, "NEXT", 30, new Color32(0x4A, 0x2E, 0x12, 0xFF), "chevron-right", false, OnStart, out nextLabel);
            startLabel = startButton.GetComponent<Image>(); // the dev host's START uses the same button and text
            backButton = TextButton("BackButton", "btn-back", new Vector4(46f, 44f, 46f, 44f), 92f, AxisX - BackW * 0.5f, top + NextH + Gap, BackW, BackH, "BACK", 20, Cream, "back-arrow", true, OnBack, out _);
        }

        /// <summary>Painted button with a centred icon + text box (padding 0 / 20, gap 8), like CSS.</summary>
        Button TextButton(string name, string art, Vector4 border, float artH, float x, float y, float w, float h, string text, int size, Color color, string icon, bool iconFirst,
            UnityEngine.Events.UnityAction onClick, out Text label)
        {
            RectTransform rt = UiKit.Rect(stage, name, x, y, w, h);
            Image plate = Sliced(rt.gameObject.AddComponent<Image>(), art, border, artH / h);
            plate.raycastTarget = true;
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = plate;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(onClick);
            var row = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(20, 20, 0, 0);
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            float iconH = Mathf.Round(h * 0.42f);
            Image ic = ArtRect(rt, "Icon", icon, 0f, 0f, iconH, iconH);
            var le = ic.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.minWidth = iconH * (ic.sprite != null ? ic.sprite.rect.width / ic.sprite.rect.height : 1f);
            le.preferredHeight = iconH;
            label = Txt(rt, "Label", 0f, 0f, 100f, h, text, size, color, TextAnchor.MiddleCenter, Serif);
            label.alignByGeometry = true;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = h;
            if (iconFirst) ic.transform.SetAsFirstSibling(); else ic.transform.SetAsLastSibling();
            return b;
        }

        /// <summary>The icons of a page centred as one group (a short last page does not sit in the left of the panel).</summary>
        void CentreSkillRow(int count)
        {
            float rowW = SkillCell * count + SkillGap * Mathf.Max(0, count - 1), x0 = (SkillPanelW - rowW) * 0.5f;
            for (int i = 0; i < VisibleSkills; i++) skillCells[i].anchoredPosition = new Vector2(x0 + i * (SkillCell + SkillGap), skillCells[i].anchoredPosition.y);
        }

        void ShowDots(int pages, int page)
        {
            for (int i = 0; i < MaxDots; i++)
            {
                dots[i].gameObject.SetActive(i < pages && pages > 1);
                dots[i].sprite = Art(i == page ? "dot-on" : "dot-off");
            }
        }
    }
}

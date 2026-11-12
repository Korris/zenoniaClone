using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Layout of Customize and Name, from docs/reference/character-flow-screen-3-customize-and-name.png on the shared 1138 x 640 stage: the hairstyle grid (2 x 3) and colour swatches in the left plate,
    /// the hero on a pedestal under its class banner in the middle, the name plate (input + dice) on the right, BACK and CREATE along the bottom.
    /// </summary>
    public sealed partial class CharacterCustomizeScreen
    {
        const float HeroX = 569f, FeetY = 470f, HeroViewH = 440f, PedestalW = 270f, PedestalAspect = 448f / 208f, FeetOnPedestal = 0.22f, BannerW = 128f, BannerH = 188f, BannerBottom = 322f;
        const float TileW = 112f, TileH = 86f, TileGap = 14f, TileInset = 8f, SwatchSize = 40f, SwatchStep = 55f; // sized so two swatch rows fit INSIDE the plate (they spilled over BACK at 118 x 104 / 44)
        const int MaxSwatches = CharacterLookPrefs.MaxDye + 2; // "own colour" + every preset index

        sealed class Tile { public Image Frame; public Image[] Layers = new Image[4]; public RectTransform Cell; }
        sealed class Swatch { public Image Disc, Ring; }

        RectTransform root, stage, titleRt;
        readonly Tile[] tiles = new Tile[HairPreviewSprites.MaxStyles];
        readonly Swatch[] swatches = new Swatch[MaxSwatches];
        Image banner;
        InputField input;
        Text styleNote, colorNote, hint, status;
        Button createButton, backButton, diceButton;
        GameObject hairHeader, colorHeader;

        void Build()
        {
            root = CharacterFlowKit.Backdrop(transform, "CharacterCustomize");
            stage = CharacterFlowKit.Stage(root);
            CharacterFlowKit.Title(root, "CUSTOMIZE AND NAME", out titleRt);

            banner = CharacterFlowKit.Pic(stage, "Banner", null, HeroX - BannerW * 0.5f, BannerBottom - BannerH, BannerW, BannerH);
            banner.preserveAspect = false;
            float ph = PedestalW / PedestalAspect;
            CharacterFlowKit.Pic(stage, "Pedestal", CharacterFlowKit.Art("pedestal"), HeroX - PedestalW * 0.5f, FeetY - FeetOnPedestal * ph, PedestalW, ph);
            float viewW = HeroViewH * HeroPreviewStage.SlotW / HeroPreviewStage.ViewH;
            float feetOffset = HeroViewH * HeroPreviewStage.FeetFromBottom / HeroPreviewStage.ViewH;
            RectTransform heroRt = UiKit.Rect(stage, "Hero", HeroX - viewW * 0.5f, FeetY + feetOffset - HeroViewH, viewW, HeroViewH);
            preview = HeroPreviewStage.Create(transform, heroRt.gameObject.AddComponent<RawImage>(), 1);

            BuildLeftPlate();
            BuildNamePlate();
            backButton = CharacterFlowKit.GemButton(stage, "Back", "btn-navy", 262f, 550f, 250f, 78f, "BACK", 28, Color.white, OnBack);
            createButton = CharacterFlowKit.GemButton(stage, "Create", "btn-gold", 590f, 550f, 250f, 78f, "CREATE", 30, CharacterFlowKit.Ink, Submit);
            status = UiKit.Label(stage, "Status", 775f, 388f, 335f, 70f, "", 16, CharacterFlowKit.Soft, TextAnchor.UpperCenter, true);
        }

        void BuildLeftPlate()
        {
            RectTransform p = CharacterFlowKit.Plate(stage, "Left", 40f, 80f, 320f, 468f);
            hairHeader = UiKit.Label(p, "HairHeader", 0f, 16f, 320f, 24f, "HAIRSTYLE", 19, CharacterFlowKit.Gold, TextAnchor.MiddleCenter, true).gameObject;
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = new Tile();
                float x = 41f + (i % 2) * (TileW + TileGap), y = 46f + (i / 2) * (TileH + 8f);
                RectTransform rt = UiKit.Rect(p, "Tile" + i, x, y, TileW, TileH);
                t.Frame = rt.gameObject.AddComponent<Image>();
                t.Frame.preserveAspect = false;
                int style = i;
                CharacterFlowKit.Clickable(rt, t.Frame, () => OnStyle(style));
                RectTransform mask = UiKit.Rect(rt, "Mask", TileInset, TileInset, TileW - 2f * TileInset, TileH - 2f * TileInset);
                mask.gameObject.AddComponent<RectMask2D>();
                t.Cell = UiKit.Rect(mask, "Cell", 0f, 0f, 1f, 1f);
                for (int l = 0; l < t.Layers.Length; l++)
                {
                    RectTransform layer = UiKit.Fill(t.Cell, "Layer" + l);
                    t.Layers[l] = UiKit.Panel(layer, Color.white);
                }
                tiles[i] = t;
            }
            colorHeader = UiKit.Label(p, "ColorHeader", 0f, 330f, 320f, 24f, "HAIR COLOR", 19, CharacterFlowKit.Gold, TextAnchor.MiddleCenter, true).gameObject;
            for (int i = 0; i < swatches.Length; i++)
            {
                var s = new Swatch();
                float x = 30f + (i % 5) * SwatchStep, y = 362f + (i / 5) * (SwatchSize + 10f);
                s.Ring = CharacterFlowKit.Pic(p, "Ring" + i, CharacterFlowKit.Art("swatch-ring"), x - 9f, y - 9f, SwatchSize + 18f, SwatchSize + 18f);
                s.Disc = CharacterFlowKit.Pic(p, "Swatch" + i, CharacterFlowKit.Art("swatch"), x, y, SwatchSize, SwatchSize);
                s.Disc.raycastTarget = true;
                int dye = i - 1; // index 0 = own colour (-1)
                CharacterFlowKit.Clickable(s.Disc.rectTransform, s.Disc, () => OnDye(dye));
                swatches[i] = s;
            }
            styleNote = UiKit.Label(p, "StyleNote", 20f, 60f, 280f, 100f, "This hero has no hairstyles to pick yet.", 17, CharacterFlowKit.Soft, TextAnchor.UpperCenter, true);
            colorNote = UiKit.Label(p, "ColorNote", 20f, 366f, 280f, 80f, "Colour is not available for this hero yet.", 17, CharacterFlowKit.Soft, TextAnchor.UpperCenter, true);
        }

        void BuildNamePlate()
        {
            RectTransform p = CharacterFlowKit.Plate(stage, "Name", 775f, 205f, 335f, 170f);
            UiKit.Label(p, "NameHeader", 0f, 16f, 335f, 24f, "CHARACTER NAME", 19, CharacterFlowKit.Gold, TextAnchor.MiddleCenter, true);
            RectTransform field = UiKit.Rect(p, "Input", 22f, 58f, 236f, 48f);
            Image fieldBg = UiWinKit.Sliced(field, "chat-input", 48f / 96f, true);
            Text placeholder = UiKit.Label(UiKit.Fill(field, "Placeholder"), "Enter a name", 20, new Color(0.6f, 0.65f, 0.75f), TextAnchor.MiddleLeft);
            Text typed = UiKit.Label(UiKit.Fill(field, "Typed"), "", 24, Color.white, TextAnchor.MiddleLeft);
            placeholder.rectTransform.offsetMin = typed.rectTransform.offsetMin = new Vector2(14f, 2f);
            placeholder.rectTransform.offsetMax = typed.rectTransform.offsetMax = new Vector2(-10f, -2f);
            placeholder.horizontalOverflow = typed.horizontalOverflow = HorizontalWrapMode.Overflow;
            placeholder.fontStyle = FontStyle.Italic;
            input = field.gameObject.AddComponent<InputField>();
            input.targetGraphic = fieldBg; input.textComponent = typed; input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = NameMax;
            input.onValidateInput = (text, pos, c) => IsNameChar(c) ? c : '\0'; // the server only takes letters, digits and _
            input.onValueChanged.AddListener(v => hintDirty = true);
            Image dice = CharacterFlowKit.Pic(p, "Dice", CharacterFlowKit.Art("dice"), 268f, 58f, 48f, 48f);
            dice.raycastTarget = true;
            diceButton = CharacterFlowKit.Clickable(dice.rectTransform, dice, OnDice);
            hint = UiKit.Label(p, "Hint", 10f, 118f, 315f, 36f, NameHint, 15, CharacterFlowKit.Soft, TextAnchor.UpperCenter, true);
        }
    }
}

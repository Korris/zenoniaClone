using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Layout of the appearance step on the shared 1138 x 640 stage, in the RO Soft kit (Sprites/ui/ro-soft): the colour plate on the left
    /// (8 hair swatches, 6 outfit swatches with their palettes.json names, Random, a hairstyle row when the class has more than one), the live hero
    /// idle facing down on its pedestal in the middle, the name plate (online) or a note (offline) on the right,
    /// Back and Create / Confirm along the bottom. Swatch = kit slot frame + the round kit swatch tinted with the palette colour.
    /// </summary>
    public sealed partial class CharacterCustomizeScreen
    {
        const float HeroX = 585f, FeetY = 440f, HeroViewH = 440f, PedestalW = 270f, PedestalAspect = 448f / 208f, FeetOnPedestal = 0.22f, BannerW = 128f, BannerH = 188f, BannerBottom = 300f;
        const float HeroColumnW = 33f; // world units: the whole 320 px body cell, so the down idle sword tip and shield edge are never cut
        const float PlateX = 28f, PlateY = 84f, PlateW = 360f, PlateH = 528f;
        const float SlotSize = 58f, SlotStepX = 68f, SlotStepY = 72f, DiscInset = 12f; // 5 slots (330 px) fit the 360 px plate
        const float RightX = 790f, RightY = 190f, RightW = 320f, RightH = 176f;

        sealed class Swatch { public Image Frame, Disc; }

        RectTransform root, stage, titleRt;
        Swatch[] hairSwatches, outfitSwatches;
        Image banner;
        InputField input;
        Text titleText, hairName, outfitName, styleText, hint, status, createLabel, note;
        Button createButton, backButton, diceButton, randomButton;
        GameObject namePlate, notePlate, styleRow, styleHeader;

        /// <summary>Hairstyle row on the right (outfit colours shown) or inside the colour plate under the hair colours (fixed outfit).</summary>
        void PlaceStyleRow(bool inPlate)
        {
            ((RectTransform)styleRow.transform).anchoredPosition = inPlate ? new Vector2(PlateX + (PlateW - RightW) * 0.5f, -(PlateY + 288f)) : new Vector2(RightX, -420f);
            styleHeader.SetActive(inPlate && styleRow.activeSelf);
        }
        readonly System.Collections.Generic.List<GameObject> outfitParts = new System.Collections.Generic.List<GameObject>();

        void Build()
        {
            root = CharacterFlowKit.Backdrop(transform, "CharacterCustomize");
            stage = CharacterFlowKit.Stage(root);
            titleText = CharacterFlowKit.Title(root, "CUSTOMIZE AND NAME", out titleRt);

            banner = CharacterFlowKit.Pic(stage, "Banner", null, HeroX - BannerW * 0.5f, BannerBottom - BannerH, BannerW, BannerH);
            banner.preserveAspect = false;
            float ph = PedestalW / PedestalAspect;
            CharacterFlowKit.Pic(stage, "Pedestal", CharacterFlowKit.Art("pedestal"), HeroX - PedestalW * 0.5f, FeetY - FeetOnPedestal * ph, PedestalW, ph);
            float viewW = HeroViewH * HeroColumnW / HeroPreviewStage.ViewH;
            float feetOffset = HeroViewH * HeroPreviewStage.FeetFromBottom / HeroPreviewStage.ViewH;
            RectTransform heroRt = UiKit.Rect(stage, "Hero", HeroX - viewW * 0.5f, FeetY + feetOffset - HeroViewH, viewW, HeroViewH);
            preview = HeroPreviewStage.Create(transform, heroRt.gameObject.AddComponent<RawImage>(), 1, HeroColumnW);

            BuildColourPlate();
            BuildRightPlates();
            heroRt.SetAsLastSibling(); // the down idle sword tip reaches the colour plate's edge: the hero stands in front of it, never cut
            Text unused;
            backButton = CharacterFlowKit.RoButton(stage, "LookBack", RoSoftSprites.Shape.Secondary, HeroX - 182f, 556f, 200f, 64f, "BACK", 26, OnBack, out unused);
            createButton = CharacterFlowKit.RoButton(stage, "Create", RoSoftSprites.Shape.Primary, HeroX + 32f, 556f, 200f, 64f, "CREATE", 26, Submit, out createLabel);
            status = UiKit.Label(stage, "Status", RightX, RightY + RightH + 12f, RightW, 70f, "", 16, CharacterFlowKit.Soft, TextAnchor.UpperCenter, true);
        }

        void BuildColourPlate()
        {
            RectTransform p = CharacterFlowKit.RoPiece(stage, "ColourPlate", RoSoftSprites.Shape.Card, PlateX, PlateY, PlateW, PlateH).rectTransform;
            CharacterFlowKit.RoText(p, "HairHeader", 0f, 16f, PlateW, 28f, "HAIR COLOR", 21, RoSoftTheme.Ink);
            hairSwatches = Swatches(p, "Hair", Mathf.Min(HeroPalettes.HairCount, CharacterAppearanceData.HairColorCount), 50f, OnHair);
            hairName = CharacterFlowKit.RoText(p, "HairName", 0f, 194f, PlateW, 26f, "", 19, RoSoftTheme.MintDark);
            Image gem = CharacterFlowKit.Pic(p, "Divider", UiArt.Get("ro-soft/divider-gem"), 24f, 226f, PlateW - 48f, 16f);
            gem.preserveAspect = false;
            Text outfitHeader = CharacterFlowKit.RoText(p, "OutfitHeader", 0f, 248f, PlateW, 28f, "OUTFIT COLOR", 21, RoSoftTheme.Ink);
            outfitSwatches = Swatches(p, "Outfit", Mathf.Min(HeroPalettes.OutfitCount, CharacterAppearanceData.OutfitColorCount), 282f, OnOutfit);
            outfitName = CharacterFlowKit.RoText(p, "OutfitName", 0f, 426f, PlateW, 26f, "", 19, RoSoftTheme.MintDark);
            // the outfit block hides for a class whose body keeps its painted outfit (RO Fantasy bodies: HeroLayeredSet.FixedOutfit)
            outfitParts.Clear();
            outfitParts.Add(gem.gameObject);
            outfitParts.Add(outfitHeader.gameObject);
            outfitParts.Add(outfitName.gameObject);
            foreach (Swatch s in outfitSwatches) outfitParts.Add(s.Frame.gameObject);
            Text unused;
            randomButton = CharacterFlowKit.RoButton(p, "Random", RoSoftSprites.Shape.Gold, (PlateW - 190f) * 0.5f, 462f, 190f, 56f, "RANDOM", 22, OnRandom, out unused);

            // Hairstyle row: only when the class has more than one layered hairstyle (every class has one today; the index plumbing stays).
            // A fixed-outfit class (RO Fantasy) shows the row in the colour plate where the outfit colours were, under its own header.
            styleHeader = CharacterFlowKit.RoText(p, "StyleHeader", 0f, 248f, PlateW, 28f, "HAIRSTYLE", 21, RoSoftTheme.Ink).gameObject;
            styleRow = UiKit.Rect(stage, "StyleRow", RightX, 420f, RightW, 60f).gameObject;
            CharacterFlowKit.RoPiece(styleRow.transform, "Face", RoSoftSprites.Shape.Row, 60f, 4f, RightW - 120f, 52f);
            styleText = CharacterFlowKit.RoText(styleRow.transform, "Style", 60f, 4f, RightW - 120f, 52f, "", 20, RoSoftTheme.Ink);
            CharacterFlowKit.RoButton(styleRow.transform, "StylePrev", RoSoftSprites.Shape.Secondary, 0f, 4f, 52f, 52f, "<", 26, () => OnStyle(-1), out unused);
            CharacterFlowKit.RoButton(styleRow.transform, "StyleNext", RoSoftSprites.Shape.Secondary, RightW - 52f, 4f, 52f, 52f, ">", 26, () => OnStyle(1), out unused);
        }

        /// <summary>Rows of four swatches (five when more than 8) from y (a short last row is centred); tap = pick that palette index.</summary>
        Swatch[] Swatches(RectTransform plate, string name, int count, float y, System.Action<int> pick)
        {
            var list = new Swatch[count];
            int perRow = count > 8 ? 5 : 4; // 9 hair colours (Starlight Blonde, 24/11): 5 + 4 keeps two rows above the colour name
            float rowW = (perRow - 1) * SlotStepX + SlotSize;
            for (int i = 0; i < count; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, count - row * perRow);
                float x0 = (PlateW - rowW) * 0.5f + (perRow - inRow) * SlotStepX * 0.5f;
                var s = new Swatch();
                s.Frame = CharacterFlowKit.RoPiece(plate, name + i, RoSoftSprites.Shape.Cell, x0 + col * SlotStepX, y + row * SlotStepY, SlotSize, SlotSize);
                s.Disc = CharacterFlowKit.Pic(s.Frame.rectTransform, "Disc", CharacterFlowKit.Art("swatch"), DiscInset, DiscInset, SlotSize - 2f * DiscInset, SlotSize - 2f * DiscInset);
                int index = i;
                CharacterFlowKit.Clickable(s.Frame.rectTransform, s.Frame, () => pick(index));
                list[i] = s;
            }
            return list;
        }

        void BuildRightPlates()
        {
            namePlate = CharacterFlowKit.RoPiece(stage, "NamePlate", RoSoftSprites.Shape.Card, RightX, RightY, RightW, RightH).gameObject;
            Transform p = namePlate.transform;
            CharacterFlowKit.RoText(p, "NameHeader", 0f, 16f, RightW, 26f, "CHARACTER NAME", 20, RoSoftTheme.Ink);
            RectTransform field = UiKit.Rect(p, "Input", 22f, 56f, 220f, 48f);
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
            input.onValueChanged.AddListener(v => { hintDirty = true; offlineStatus = ""; });
            Image dice = CharacterFlowKit.Pic(p, "Dice", CharacterFlowKit.Art("dice"), 252f, 56f, 48f, 48f);
            dice.raycastTarget = true;
            diceButton = CharacterFlowKit.Clickable(dice.rectTransform, dice, OnDice);
            hint = CharacterFlowKit.RoText(p, "Hint", 10f, 116f, RightW - 20f, 44f, NameHint, 15, RoSoftTheme.InkSoft, TextAnchor.UpperCenter);

            notePlate = CharacterFlowKit.RoPiece(stage, "NotePlate", RoSoftSprites.Shape.Card, RightX, RightY, RightW, RightH).gameObject;
            note = CharacterFlowKit.RoText(notePlate.transform, "Note", 22f, 18f, RightW - 44f, RightH - 36f, "", 17, RoSoftTheme.Ink, TextAnchor.MiddleCenter);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Layout of the character slot screen on the shared 1138 x 640 stage, after the mockup (docs/reference/character-select-ro-fantasy/mockup/01-slot-screen-chatgpt-1.png),
    /// only from art, nothing drawn in code: the slot kit cut from the mockup's piece sheet (Sprites/ui/character-select: slot cards, pedestal, info panel, title banner,
    /// name label; Tools/ui-character-select-sheets/slot-ui-kit.json), the RO Soft kit buttons and the old character-flow icons. Title banner top centre, the account
    /// button top left, the big hero on its pedestal centre left, the info panel and buttons right, six slot cards along the bottom. A missing slot-kit piece falls back
    /// to the nearest RO Soft piece. All heroes are one HeroPreviewStage texture: slot r = card r's small hero, BigSlot = the big one.
    /// </summary>
    public sealed partial class CharacterListScreen
    {
        const int Rows = 6, BigSlot = Rows;
        const float CardX = 53f, CardY = 364f, CardW = 168f, CardH = 246f, CardStep = 172f;
        const float HeroX = 381f, PedestalY = 248f, PedestalW = 270f, BigFeetY = 281f, BigViewH = 280f;
        const float MiniPedW = 120f, MiniPedY = 132f, MiniFeetY = 147f, MiniViewH = 140f;
        const float InfoX = 524f, InfoY = 92f, InfoW = 348f, InfoH = 166f;
        const float CornerW = 190f, CornerH = 46f, CornerGap = 10f;

        sealed class Row
        {
            public GameObject Root, Hero, Empty;
            public Image Face;
            public RawImage Mini;
            public Text Name, Info, Level;
            public Button Button;
        }

        RectTransform root, stage, cornerRt, cornerRightRt;
        readonly Row[] rows = new Row[Rows];
        readonly List<GameObject> heroControls = new List<GameObject>();
        readonly List<Button> corner = new List<Button>(), cornerRight = new List<Button>();
        RawImage bigHero;
        Image pedestal;
        Button enterButton, createButton, deleteButton, pageUp, pageDown, logoutButton, offlineButton, directButton, onlineButton, backButton;
        Text enterLabel, infoName, infoClass, infoLevel, infoPlace, emptyNote, status;
        GameObject infoHero, infoEmpty, statusPlate;

        /// <summary>A piece of the slot kit (cut at 2x), or the nearest RO Soft piece while it is missing.</summary>
        static Sprite SlotArt(string name)
        {
            Sprite s = UiArt.Get("character-select/" + name);
            if (s != null) return s;
            switch (name)
            {
                case "slot-card-selected": return RoSoftSprites.Named("slot-selected");
                case "slot-card-empty": return RoSoftSprites.Named("slot-empty");
                case "pedestal": return CharacterFlowKit.Art("pedestal");
                default: return RoSoftSprites.Named("slot");
            }
        }

        static float Aspect(Sprite s, float fallback) { return s != null ? s.rect.width / s.rect.height : fallback; }

        void Fit()
        {
            float s = CharacterFlowKit.Fit(root, stage);
            cornerRt.localScale = cornerRightRt.localScale = new Vector3(s, s, 1f);
            cornerRt.anchoredPosition = new Vector2(16f * s, -14f * s);
            cornerRightRt.anchoredPosition = new Vector2(-16f * s, -14f * s);
        }

        void Build()
        {
            root = CharacterFlowKit.Backdrop(transform, "CharacterSelect");
            stage = CharacterFlowKit.Stage(root);

            Sprite banner = SlotArt("title-banner");
            float bw = 450f, bh = bw / Aspect(banner, 4.2f);
            Image title = CharacterFlowKit.Pic(stage, "TitleBanner", banner, CharacterFlowKit.StageW * 0.5f - bw * 0.5f, 4f, bw, bh);
            title.preserveAspect = false;
            CharacterFlowKit.RoText(title.rectTransform, "Text", bw * 0.16f, bh * 0.22f, bw * 0.68f, bh * 0.52f, "Chọn nhân vật", 34, Color.white).GetComponent<Shadow>().effectColor = new Color(0.05f, 0.12f, 0.3f, 0.6f);

            // the big hero: pedestal, then the texture every hero picture shares
            Sprite ped = SlotArt("pedestal");
            float ph = PedestalW / Aspect(ped, 2.47f);
            pedestal = CharacterFlowKit.Pic(stage, "ListPedestal", ped, HeroX - PedestalW * 0.5f, PedestalY, PedestalW, ph);
            float viewW = BigViewH * HeroPreviewStage.SlotW / HeroPreviewStage.ViewH, feet = BigViewH * HeroPreviewStage.FeetFromBottom / HeroPreviewStage.ViewH;
            bigHero = UiKit.Rect(stage, "ListHero", HeroX - viewW * 0.5f, BigFeetY + feet - BigViewH, viewW, BigViewH).gameObject.AddComponent<RawImage>();
            preview = HeroPreviewStage.Create(transform, bigHero, Rows + 1);
            bigHero.uvRect = new Rect((float)BigSlot / (Rows + 1), 0f, 1f / (Rows + 1), 1f);
            Text unused;
            heroControls.Add(CharacterFlowKit.RoButton(stage, "ListTurnLeft", RoSoftSprites.Shape.Row, HeroX - 136f, 168f, 46f, 46f, "<", 24, () => OnTurn(-1), out unused).gameObject);
            heroControls.Add(CharacterFlowKit.RoButton(stage, "ListTurnRight", RoSoftSprites.Shape.Row, HeroX + 88f, 168f, 46f, 46f, ">", 24, () => OnTurn(1), out unused).gameObject);

            BuildInfo();
            enterButton = CharacterFlowKit.RoButton(stage, "EnterGame", RoSoftSprites.Shape.Primary, 850f, 262f, 268f, 72f, "Vào game", 30, OnEnter, out enterLabel);
            createButton = CharacterFlowKit.RoButton(stage, "CreateNew", RoSoftSprites.Shape.Secondary, InfoX, 274f, 173f, 50f, "+ Tạo nhân vật", 18, OnCreate, out unused);
            deleteButton = CharacterFlowKit.RoButton(stage, "DeleteHero", RoSoftSprites.Shape.Danger, InfoX + 183f, 274f, 131f, 50f, "Xoá", 20, OnAskDelete, out unused);
            statusPlate = CharacterFlowKit.RoPiece(stage, "StatusPlate", RoSoftSprites.Shape.Glass, InfoX, 325f, 594f, 38f).gameObject; // readable over the dark stones
            status = CharacterFlowKit.RoText(statusPlate.transform, "Status", 12f, 0f, 570f, 38f, "", 15, RoSoftTheme.Ink);

            Texture shared = bigHero.texture;
            for (int r = 0; r < Rows; r++) rows[r] = BuildCard(r, shared);
            pageUp = CharacterFlowKit.RoButton(stage, "SlotPageLeft", RoSoftSprites.Shape.Row, 8f, 456f, 40f, 60f, "<", 22, () => OnPage(-1), out unused);
            pageDown = CharacterFlowKit.RoButton(stage, "SlotPageRight", RoSoftSprites.Shape.Row, 1090f, 456f, 40f, 60f, ">", 22, () => OnPage(1), out unused);
            BuildCorner();
            BuildConfirm();
        }

        void BuildInfo()
        {
            Image panel = CharacterFlowKit.Pic(stage, "InfoPanel", null, InfoX, InfoY, InfoW, InfoH);
            panel.sprite = UiArt.Get("character-select/info-panel", new Vector4(44f, 44f, 44f, 40f)) ?? RoSoftSprites.Get(RoSoftSprites.Shape.Card);
            panel.type = Image.Type.Sliced;
            panel.preserveAspect = false;
            panel.pixelsPerUnitMultiplier = 2f; // cut at 2x
            panel.raycastTarget = true;
            infoHero = UiKit.Rect(panel.rectTransform, "Hero", 0f, 0f, InfoW, InfoH).gameObject;
            infoName = InfoRow(infoHero.transform, "Name", null, "Tên:", 18f);
            infoClass = InfoRow(infoHero.transform, "Class", "icon-class", "Class:", 52f);
            infoLevel = InfoRow(infoHero.transform, "Level", "icon-level", "Cấp:", 86f);
            infoPlace = InfoRow(infoHero.transform, "Place", "icon-location", "Bản đồ cuối:", 120f);
            infoEmpty = UiKit.Rect(panel.rectTransform, "Empty", 0f, 0f, InfoW, InfoH).gameObject;
            emptyNote = CharacterFlowKit.RoText(infoEmpty.transform, "Note", 24f, 20f, InfoW - 48f, InfoH - 40f, "", 18, RoSoftTheme.Ink);
        }

        /// <summary>Icon (old character-flow art; none for the name yet), label and value of one info row.</summary>
        static Text InfoRow(Transform p, string name, string icon, string label, float y)
        {
            if (icon != null) CharacterFlowKit.Pic(p, name + "Icon", CharacterFlowKit.Art(icon), 26f, y + 3f, 24f, 24f);
            CharacterFlowKit.RoText(p, name + "Label", 60f, y, 120f, 30f, label, 17, RoSoftTheme.Ink, TextAnchor.MiddleLeft);
            Text value = CharacterFlowKit.RoText(p, name, 178f, y, InfoW - 196f, 30f, "", 19, RoSoftTheme.Ink, TextAnchor.MiddleLeft);
            value.resizeTextForBestFit = true; // a long map name shrinks instead of wrapping out of the row
            value.resizeTextMinSize = 12;
            value.resizeTextMaxSize = 19;
            value.horizontalOverflow = HorizontalWrapMode.Wrap;
            value.verticalOverflow = VerticalWrapMode.Truncate;
            return value;
        }

        Row BuildCard(int r, Texture shared)
        {
            var card = new Row();
            card.Face = CharacterFlowKit.Pic(stage, "HeroRow" + r, SlotArt("slot-card"), CardX + r * CardStep, CardY, CardW, CardH);
            card.Face.preserveAspect = false;
            card.Face.raycastTarget = true;
            card.Root = card.Face.gameObject;
            int index = r;
            card.Button = CharacterFlowKit.Clickable(card.Face.rectTransform, card.Face, () => OnRow(index));
            Transform f = card.Face.transform;

            card.Hero = UiKit.Rect(f, "Hero", 0f, 0f, CardW, CardH).gameObject;
            Sprite ped = SlotArt("pedestal");
            CharacterFlowKit.Pic(card.Hero.transform, "Pedestal", ped, (CardW - MiniPedW) * 0.5f, MiniPedY, MiniPedW, MiniPedW / Aspect(ped, 2.47f));
            float miniW = MiniViewH * HeroPreviewStage.SlotW / HeroPreviewStage.ViewH, feet = MiniViewH * HeroPreviewStage.FeetFromBottom / HeroPreviewStage.ViewH;
            card.Mini = UiKit.Rect(card.Hero.transform, "Mini", (CardW - miniW) * 0.5f, MiniFeetY + feet - MiniViewH, miniW, MiniViewH).gameObject.AddComponent<RawImage>();
            card.Mini.texture = shared;
            card.Mini.raycastTarget = false;
            card.Mini.uvRect = new Rect((float)r / (Rows + 1), 0f, 1f / (Rows + 1), 1f);
            Image label = CharacterFlowKit.Pic(card.Hero.transform, "NameLabel", SlotArt("name-label"), 10f, 172f, CardW - 20f, 32f);
            label.preserveAspect = false;
            card.Name = CharacterFlowKit.RoText(label.rectTransform, "Name", 10f, 0f, CardW - 40f, 32f, "", 16, Color.white);
            card.Name.GetComponent<Shadow>().effectColor = new Color(0.05f, 0.12f, 0.3f, 0.6f);
            card.Info = CharacterFlowKit.RoText(card.Hero.transform, "Class", 0f, 205f, CardW, 20f, "", 15, RoSoftTheme.Ink);
            card.Level = CharacterFlowKit.RoText(card.Hero.transform, "Level", 0f, 223f, CardW, 18f, "", 15, RoSoftTheme.Ink);

            card.Empty = UiKit.Rect(f, "Empty", 0f, 0f, CardW, CardH).gameObject;
            CharacterFlowKit.RoText(card.Empty.transform, "Text", 0f, 150f, CardW, 26f, "+ Tạo nhân vật", 16, RoSoftTheme.Ink);
            Image faded = CharacterFlowKit.Pic(card.Empty.transform, "Pedestal", ped, (CardW - 130f) * 0.5f, 188f, 130f, 130f / Aspect(ped, 2.47f));
            faded.color = new Color(1f, 1f, 1f, 0.45f);
            return card;
        }

        void BuildCorner()
        {
            cornerRt = UiKit.Rect(root, "ListCorner", 0f, 0f, 3f * (CornerW + CornerGap), CornerH);
            cornerRt.anchorMin = cornerRt.anchorMax = cornerRt.pivot = new Vector2(0f, 1f);
            cornerRightRt = UiKit.Rect(root, "ListCornerRight", 0f, 0f, 2f * (CornerW + CornerGap), CornerH);
            cornerRightRt.anchorMin = cornerRightRt.anchorMax = cornerRightRt.pivot = new Vector2(1f, 1f);
            // top left, as in the mockup: the account button (offline: go online; over the game: back); top right: the rest, clear of the title banner
            logoutButton = CornerButton(cornerRt, corner, "LogOut", RoSoftSprites.Shape.Secondary, "Đổi tài khoản", OnLogout);
            onlineButton = CornerButton(cornerRt, corner, "PlayOnline", RoSoftSprites.Shape.Secondary, "Chơi online", OnPlayOnline);
            backButton = CornerButton(cornerRt, corner, "BackToGame", RoSoftSprites.Shape.Secondary, "Quay lại", OnBackToGame);
            offlineButton = CornerButton(cornerRightRt, cornerRight, "PlayOffline", RoSoftSprites.Shape.Row, "Chơi offline", OnPlayOffline);
            directButton = CornerButton(cornerRightRt, cornerRight, "DirectConnect", RoSoftSprites.Shape.Row, "Kết nối (dev)", () => ConnectScreenGui.ShowDevPage());
        }

        static Button CornerButton(RectTransform parent, List<Button> list, string name, RoSoftSprites.Shape shape, string text, System.Action act)
        {
            Text unused;
            Button b = CharacterFlowKit.RoButton(parent, name, shape, 0f, 0f, CornerW, CornerH, text, 18, act, out unused);
            list.Add(b);
            return b;
        }

        static void ShowCorner(Button b, bool on) { if (b.gameObject.activeSelf != on) b.gameObject.SetActive(on); }

        /// <summary>The visible corner buttons from the top-left corner rightwards, in build order (the account button first).</summary>
        void LayoutCorner()
        {
            float x = 0f;
            foreach (Button b in corner)
            {
                if (!b.gameObject.activeSelf) continue;
                ((RectTransform)b.transform).anchoredPosition = new Vector2(x, 0f);
                x += CornerW + CornerGap;
            }
            x = cornerRightRt.rect.width; // top right: from the right edge leftwards
            foreach (Button b in cornerRight)
            {
                if (!b.gameObject.activeSelf) continue;
                x -= CornerW;
                ((RectTransform)b.transform).anchoredPosition = new Vector2(x, 0f);
                x -= CornerGap;
            }
        }
    }
}

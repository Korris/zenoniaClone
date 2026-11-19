using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Layout of the character select screen on the shared 1138 x 640 stage, only from art (RO Soft kit Sprites/ui/ro-soft + the old character-flow / hero-select
    /// pieces; nothing drawn in code): the roster card on the left (six hero cards = kit slot / gold selected slot, empty = slot-empty + btn-plus), the big hero
    /// in the middle (class banner, pedestal, turn arrows, motion tabs), the info card and the ENTER / CREATE / DELETE buttons on the right, the mode's buttons in
    /// the top-right corner. All heroes are one HeroPreviewStage texture: slot r = row r's small hero (upper body only), slot BigSlot = the big one.
    /// </summary>
    public sealed partial class CharacterListScreen
    {
        const int Rows = 6, BigSlot = Rows;
        const float RosterX = 24f, RosterY = 84f, RosterW = 340f, RosterH = 528f, RowX = 14f, RowY = 66f, RowW = 312f, RowH = 70f, RowStep = 76f;
        const float HeroX = 577f, FeetY = 432f, HeroViewH = 400f, PedestalW = 260f, PedestalAspect = 448f / 208f, FeetOnPedestal = 0.22f, BannerW = 120f, BannerH = 176f, BannerBottom = 300f;
        const float RightX = 794f, RightW = 320f;
        const float CornerW = 176f, CornerH = 52f, CornerGap = 10f;
        static readonly Rect MiniUv = new Rect(0.12f, 0.40f, 0.76f, 0.52f); // of one slot: head and chest of the small heroes

        sealed class Row
        {
            public GameObject Root, Hero, Empty;
            public Image Face;
            public RawImage Mini;
            public Text Name, Info, Badge;
            public Button Button;
        }

        RectTransform root, stage, titleRt, cornerRt;
        readonly Row[] rows = new Row[Rows];
        readonly Image[] tabs = new Image[3];
        readonly Text[] tabLabels = new Text[3];
        readonly List<GameObject> heroControls = new List<GameObject>();
        readonly List<Button> corner = new List<Button>();
        RawImage bigHero;
        Image banner, pedestal;
        Button enterButton, createButton, deleteButton, pageUp, pageDown, logoutButton, offlineButton, directButton, onlineButton, backButton;
        Text enterLabel, countText, ownerText, infoName, infoClass, infoLevel, infoPlace, infoState, emptyNote, status;
        GameObject infoHero, infoEmpty, statusPlate;

        void Fit()
        {
            float s = CharacterFlowKit.Fit(root, stage);
            var scale = new Vector3(s, s, 1f);
            titleRt.localScale = cornerRt.localScale = scale;
            titleRt.anchoredPosition = new Vector2(20f * s, -14f * s);
            cornerRt.anchoredPosition = new Vector2(-16f * s, -16f * s);
        }

        void Build()
        {
            root = CharacterFlowKit.Backdrop(transform, "CharacterSelect");
            stage = CharacterFlowKit.Stage(root);
            CharacterFlowKit.Title(root, "CHARACTER SELECT", out titleRt);

            // the big hero first (behind the plates), then the texture all hero pictures share
            banner = CharacterFlowKit.Pic(stage, "ListBanner", null, HeroX - BannerW * 0.5f, BannerBottom - BannerH, BannerW, BannerH);
            banner.preserveAspect = false;
            float ph = PedestalW / PedestalAspect;
            pedestal = CharacterFlowKit.Pic(stage, "ListPedestal", CharacterFlowKit.Art("pedestal"), HeroX - PedestalW * 0.5f, FeetY - FeetOnPedestal * ph, PedestalW, ph);
            float viewW = HeroViewH * HeroPreviewStage.SlotW / HeroPreviewStage.ViewH;
            float feetOffset = HeroViewH * HeroPreviewStage.FeetFromBottom / HeroPreviewStage.ViewH;
            bigHero = UiKit.Rect(stage, "ListHero", HeroX - viewW * 0.5f, FeetY + feetOffset - HeroViewH, viewW, HeroViewH).gameObject.AddComponent<RawImage>();
            preview = HeroPreviewStage.Create(transform, bigHero, Rows + 1);
            bigHero.uvRect = new Rect((float)BigSlot / (Rows + 1), 0f, 1f / (Rows + 1), 1f);

            BuildRoster();
            BuildHeroControls();
            BuildRight();
            BuildCorner();
            BuildConfirm();
        }

        void BuildRoster()
        {
            RectTransform p = CharacterFlowKit.RoPiece(stage, "Roster", RoSoftSprites.Shape.Card, RosterX, RosterY, RosterW, RosterH).rectTransform;
            countText = CharacterFlowKit.RoText(p, "Count", 22f, 12f, 220f, 28f, "", 21, RoSoftTheme.Ink, TextAnchor.MiddleLeft);
            ownerText = CharacterFlowKit.RoText(p, "Owner", 22f, 38f, 240f, 22f, "", 15, RoSoftTheme.InkSoft, TextAnchor.MiddleLeft);
            Text unused;
            pageUp = CharacterFlowKit.RoButton(p, "RosterPageUp", RoSoftSprites.Shape.Row, RosterW - 100f, 14f, 40f, 40f, "<", 22, () => OnPage(-1), out unused);
            pageDown = CharacterFlowKit.RoButton(p, "RosterPageDown", RoSoftSprites.Shape.Row, RosterW - 54f, 14f, 40f, 40f, ">", 22, () => OnPage(1), out unused);
            Texture shared = bigHero.texture;
            for (int r = 0; r < Rows; r++) rows[r] = BuildRow(p, r, shared);
        }

        Row BuildRow(RectTransform p, int r, Texture shared)
        {
            var row = new Row();
            row.Face = CharacterFlowKit.RoPiece(p, "HeroRow" + r, RoSoftSprites.Shape.Cell, RowX, RowY + r * RowStep, RowW, RowH);
            row.Root = row.Face.gameObject;
            int index = r;
            row.Button = CharacterFlowKit.Clickable(row.Face.rectTransform, row.Face, () => OnRow(index));

            row.Hero = UiKit.Rect(row.Face.rectTransform, "Hero", 0f, 0f, RowW, RowH).gameObject;
            float miniH = RowH - 10f, miniW = miniH * (MiniUv.width * HeroPreviewStage.SlotW) / (MiniUv.height * HeroPreviewStage.ViewH);
            row.Mini = UiKit.Rect(row.Hero.transform, "Mini", 10f, 5f, miniW, miniH).gameObject.AddComponent<RawImage>();
            row.Mini.texture = shared;
            row.Mini.raycastTarget = false;
            row.Mini.uvRect = new Rect((r + MiniUv.x) / (Rows + 1), MiniUv.y, MiniUv.width / (Rows + 1), MiniUv.height);
            row.Name = CharacterFlowKit.RoText(row.Hero.transform, "Name", 92f, 9f, 200f, 28f, "", 21, RoSoftTheme.Ink, TextAnchor.MiddleLeft);
            row.Info = CharacterFlowKit.RoText(row.Hero.transform, "Info", 92f, 37f, 150f, 22f, "", 16, RoSoftTheme.InkSoft, TextAnchor.MiddleLeft);
            row.Badge = CharacterFlowKit.RoText(row.Hero.transform, "Badge", RowW - 116f, 37f, 104f, 22f, "", 14, RoSoftTheme.MintDark, TextAnchor.MiddleRight);

            row.Empty = UiKit.Rect(row.Face.rectTransform, "Empty", 0f, 0f, RowW, RowH).gameObject;
            CharacterFlowKit.Pic(row.Empty.transform, "Plus", RoSoftSprites.Named("btn-plus"), 22f, 13f, 44f, 44f);
            CharacterFlowKit.RoText(row.Empty.transform, "Text", 82f, 0f, 220f, RowH, "Create character", 20, RoSoftTheme.MintDark, TextAnchor.MiddleLeft);
            return row;
        }

        void BuildHeroControls()
        {
            Text unused;
            heroControls.Add(CharacterFlowKit.RoButton(stage, "ListTurnLeft", RoSoftSprites.Shape.Row, HeroX - 180f, 280f, 56f, 56f, "<", 28, () => OnTurn(-1), out unused).gameObject);
            heroControls.Add(CharacterFlowKit.RoButton(stage, "ListTurnRight", RoSoftSprites.Shape.Row, HeroX + 124f, 280f, 56f, 56f, ">", 28, () => OnTurn(1), out unused).gameObject);
            string[] names = { "IDLE", "WALK", "ATTACK" };
            for (int i = 0; i < tabs.Length; i++)
            {
                var m = (HeroPreviewStage.Motion)i;
                Button b = CharacterFlowKit.RoButton(stage, "ListMotion" + i, RoSoftSprites.Shape.TabOff, HeroX - 171f + i * 114f, 478f, 110f, 44f, names[i], 17, () => OnMotion(m), out tabLabels[i]);
                tabs[i] = (Image)b.targetGraphic;
                heroControls.Add(b.gameObject);
            }
        }

        void BuildRight()
        {
            RectTransform card = CharacterFlowKit.RoPiece(stage, "InfoCard", RoSoftSprites.Shape.Card, RightX, RosterY, RightW, 252f).rectTransform;
            infoHero = UiKit.Rect(card, "Hero", 0f, 0f, RightW, 252f).gameObject;
            infoName = CharacterFlowKit.RoText(infoHero.transform, "InfoName", 20f, 16f, RightW - 40f, 40f, "", 30, RoSoftTheme.Ink);
            Image gem = CharacterFlowKit.Pic(infoHero.transform, "Divider", UiArt.Get("ro-soft/divider-gem"), 28f, 60f, RightW - 56f, 14f);
            gem.preserveAspect = false;
            infoClass = InfoRow(infoHero.transform, "Class", "icon-class", 84f);
            infoLevel = InfoRow(infoHero.transform, "Level", "icon-level", 120f);
            infoPlace = InfoRow(infoHero.transform, "Place", "icon-location", 156f);
            infoState = CharacterFlowKit.RoText(infoHero.transform, "State", 20f, 200f, RightW - 40f, 30f, "", 16, RoSoftTheme.MintDark);
            infoEmpty = UiKit.Rect(card, "Empty", 0f, 0f, RightW, 252f).gameObject;
            emptyNote = CharacterFlowKit.RoText(infoEmpty.transform, "Note", 24f, 30f, RightW - 48f, 190f, "", 19, RoSoftTheme.Ink);

            Text unused;
            enterButton = CharacterFlowKit.RoButton(stage, "EnterGame", RoSoftSprites.Shape.Primary, RightX, 352f, RightW, 74f, "ENTER GAME", 28, OnEnter, out enterLabel);
            createButton = CharacterFlowKit.RoButton(stage, "CreateNew", RoSoftSprites.Shape.Gold, RightX, 438f, 156f, 58f, "CREATE NEW", 18, OnCreate, out unused);
            deleteButton = CharacterFlowKit.RoButton(stage, "DeleteHero", RoSoftSprites.Shape.Danger, RightX + RightW - 156f, 438f, 156f, 58f, "DELETE", 18, OnAskDelete, out unused);
            statusPlate = CharacterFlowKit.RoPiece(stage, "StatusPlate", RoSoftSprites.Shape.Glass, RightX, 508f, RightW, 84f).gameObject;
            status = CharacterFlowKit.RoText(statusPlate.transform, "Status", 14f, 8f, RightW - 28f, 68f, "", 15, RoSoftTheme.Ink);
        }

        /// <summary>Icon (old character-flow art) + text row of the info card.</summary>
        static Text InfoRow(Transform p, string name, string icon, float y)
        {
            CharacterFlowKit.Pic(p, name + "Icon", CharacterFlowKit.Art(icon), 34f, y + 2f, 26f, 26f);
            return CharacterFlowKit.RoText(p, name, 74f, y, RightW - 96f, 30f, "", 19, RoSoftTheme.Ink, TextAnchor.MiddleLeft);
        }

        void BuildCorner()
        {
            cornerRt = UiKit.Rect(root, "ListCorner", 0f, 0f, 4f * (CornerW + CornerGap), CornerH);
            cornerRt.anchorMin = cornerRt.anchorMax = cornerRt.pivot = new Vector2(1f, 1f);
            logoutButton = CornerButton("LogOut", RoSoftSprites.Shape.Secondary, "LOG OUT", OnLogout);
            offlineButton = CornerButton("PlayOffline", RoSoftSprites.Shape.Row, "PLAY OFFLINE", OnPlayOffline);
            directButton = CornerButton("DirectConnect", RoSoftSprites.Shape.Row, "DIRECT (DEV)", () => ConnectScreenGui.ShowDevPage());
            onlineButton = CornerButton("PlayOnline", RoSoftSprites.Shape.Secondary, "PLAY ONLINE", OnPlayOnline);
            backButton = CornerButton("BackToGame", RoSoftSprites.Shape.Secondary, "BACK", OnBackToGame);
        }

        Button CornerButton(string name, RoSoftSprites.Shape shape, string text, System.Action act)
        {
            Text unused;
            Button b = CharacterFlowKit.RoButton(cornerRt, name, shape, 0f, 0f, CornerW, CornerH, text, 18, act, out unused);
            corner.Add(b);
            return b;
        }

        static void ShowCorner(Button b, bool on) { if (b.gameObject.activeSelf != on) b.gameObject.SetActive(on); }

        /// <summary>The visible corner buttons from the right edge leftwards, in build order (log out stays outermost).</summary>
        void LayoutCorner()
        {
            float x = cornerRt.rect.width;
            foreach (Button b in corner)
            {
                if (!b.gameObject.activeSelf) continue;
                var rt = (RectTransform)b.transform;
                x -= CornerW;
                rt.anchoredPosition = new Vector2(x, 0f);
                x -= CornerGap;
            }
        }
    }
}

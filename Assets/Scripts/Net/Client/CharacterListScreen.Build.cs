using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Layout of the character list, from the mock docs/reference/character-flow-screen-1-character-list.png on the shared 1138 x 640 stage: four pedestals in a row (banner behind, live hero on it,
    /// name plate on its front), the info panel with the picked hero and the gold / red buttons below. The heroes of all four columns are one RawImage (HeroPreviewStage) between the back layer
    /// (banners, pedestals, create tablets) and the front layer (name plates, tap areas), so plates and tablets are never hidden by a hero.
    /// </summary>
    public sealed partial class CharacterListScreen
    {
        const float ColumnStep = 255f, FeetY = 352f, PedestalW = 210f, PedestalAspect = 448f / 208f, FeetOnPedestal = 0.22f;
        const float GlowAspect = 242f / 507f, GlowFeet = 0.19f, PickedScale = 1.12f, BannerW = 98f, BannerH = 144f, BannerBottom = 202f;
        const float CreateW = 205f, CreateAspect = 541f / 447f, PedestalBottom = 428f; // the row sits 40 px above the info panel: at 392 / 468 the name plates and pedestals ran into it

        sealed class Column
        {
            public Image Banner, Pedestal, Glow, CreateArt;
            public Text CreateLabel, Name, Level;
            public GameObject Plate;
            public Button Hit;
            public RectTransform[] Scaled; // grow with the picked hero
        }

        RectTransform root, stage, titleRt, gearRt, menuRt;
        readonly Column[] columns = new Column[PageSize];
        Button leftArrow, rightArrow, enterButton, deleteButton;
        Image portrait;
        Text infoName, infoClass, infoLevel, infoPlace, emptyNote, status;

        static float ColumnX(int i) { return CharacterFlowKit.StageW * 0.5f + (i - (PageSize - 1) * 0.5f) * ColumnStep; }

        static void Place(RectTransform rt, float x, float y, Vector2 pivot)
        {
            rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(x, -y);
        }

        void Build()
        {
            root = CharacterFlowKit.Backdrop(transform, "CharacterList");
            stage = CharacterFlowKit.Stage(root);
            RectTransform back = UiKit.Rect(stage, "Back", 0f, 0f, CharacterFlowKit.StageW, CharacterFlowKit.StageH);
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0f, 1f);
            for (int i = 0; i < PageSize; i++) columns[i] = new Column();
            for (int i = 0; i < PageSize; i++) BuildBack(back, columns[i], i);

            float rowW = PageSize * ColumnStep, rowH = rowW * HeroPreviewStage.ViewH / (PageSize * HeroPreviewStage.SlotW);
            float feetOffset = rowH * HeroPreviewStage.FeetFromBottom / HeroPreviewStage.ViewH; // the feet stand this far above the texture's bottom
            RectTransform heroes = UiKit.Rect(stage, "Heroes", ColumnX(0) - ColumnStep * 0.5f, FeetY + feetOffset - rowH, rowW, rowH);
            preview = HeroPreviewStage.Create(transform, heroes.gameObject.AddComponent<RawImage>(), PageSize);

            RectTransform front = UiKit.Rect(stage, "Front", 0f, 0f, CharacterFlowKit.StageW, CharacterFlowKit.StageH);
            for (int i = 0; i < PageSize; i++) BuildFront(front, columns[i], i);
            leftArrow = Arrow("PageLeft", 16f, false, -1);
            rightArrow = Arrow("PageRight", 1122f, true, 1);
            BuildInfoPanel();
            status = UiKit.Label(stage, "Status", 65f, 606f, 1008f, 28f, "", 15, CharacterFlowKit.Soft, TextAnchor.MiddleCenter, true);
            BuildCorner();
        }

        void BuildBack(RectTransform layer, Column c, int i)
        {
            float cx = ColumnX(i), ph = PedestalW / PedestalAspect, gw = PedestalW * 507f / 448f;
            c.Banner = CharacterFlowKit.Pic(layer, "Banner" + i, null, 0f, 0f, BannerW, BannerH);
            c.Banner.preserveAspect = false;
            Place(c.Banner.rectTransform, cx, BannerBottom, new Vector2(0.5f, 0f));
            c.Pedestal = CharacterFlowKit.Pic(layer, "Pedestal" + i, CharacterFlowKit.Art("pedestal"), 0f, 0f, PedestalW, ph);
            Place(c.Pedestal.rectTransform, cx, FeetY, new Vector2(0.5f, 1f - FeetOnPedestal));
            c.Glow = CharacterFlowKit.Pic(layer, "Glow" + i, CharacterFlowKit.Art("pedestal-glow"), 0f, 0f, gw, gw * GlowAspect);
            Place(c.Glow.rectTransform, cx, FeetY, new Vector2(0.5f, 1f - GlowFeet));
            c.CreateArt = CharacterFlowKit.Pic(layer, "Create" + i, CharacterFlowKit.Art("create-slot"), 0f, 0f, CreateW, CreateW * CreateAspect);
            Place(c.CreateArt.rectTransform, cx, PedestalBottom, new Vector2(0.5f, 0f));
            c.CreateLabel = UiKit.Label(c.CreateArt.transform, "Text", 0f, 0f, CreateW, 56f, "Create\nCharacter", 17, new Color(1f, 0.95f, 0.82f), TextAnchor.MiddleCenter, true);
            c.CreateLabel.rectTransform.anchorMin = c.CreateLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.38f);
            c.CreateLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            c.CreateLabel.rectTransform.anchoredPosition = Vector2.zero;
            c.Scaled = new[] { c.Banner.rectTransform, c.Pedestal.rectTransform, c.Glow.rectTransform };
        }

        void BuildFront(RectTransform layer, Column c, int i)
        {
            float cx = ColumnX(i);
            RectTransform plate = UiKit.Rect(layer, "Plate" + i, cx - 78f, FeetY + 30f, 156f, 42f);
            UiKit.Panel(plate, new Color(0.04f, 0.06f, 0.11f, 0.86f));
            c.Plate = plate.gameObject;
            c.Name = UiKit.Label(plate, "Name", 0f, 2f, 156f, 22f, "", 16, CharacterFlowKit.Gold, TextAnchor.MiddleCenter, true);
            c.Level = UiKit.Label(plate, "Level", 0f, 22f, 156f, 18f, "", 14, Color.white, TextAnchor.MiddleCenter, true);
            int index = i;
            c.Hit = CharacterFlowKit.HitArea(layer, "Hit" + i, cx - 115f, 100f, 230f, 380f, () => OnColumn(index));
        }

        Button Arrow(string name, float x, bool mirrored, int delta)
        {
            const float W = 34f, H = 56f;
            Image img = CharacterFlowKit.Pic(stage, name, UiArt.Get("hero-select/arrow-left"), x, 290f, W, H);
            img.raycastTarget = true;
            if (mirrored) img.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // x is the right edge then: the mirror folds about the left edge
            return CharacterFlowKit.Clickable(img.rectTransform, img, () => { page += delta; dirty = true; });
        }

        void BuildInfoPanel()
        {
            RectTransform p = CharacterFlowKit.Plate(stage, "Info", 65f, 462f, 1008f, 138f);
            portrait = CharacterFlowKit.Pic(p, "Portrait", null, 18f, 10f, 118f, 118f);
            infoName = UiKit.Label(p, "InfoName", 160f, 8f, 420f, 38f, "", 28, new Color(0.62f, 0.76f, 1f), TextAnchor.MiddleLeft, true);
            infoClass = InfoRow(p, "Class", "icon-class", 54f);
            infoLevel = InfoRow(p, "Level", "icon-level", 80f);
            infoPlace = InfoRow(p, "Place", "icon-location", 106f);
            emptyNote = UiKit.Label(p, "Empty", 160f, 40f, 420f, 60f, "No heroes yet: create your first.", 20, CharacterFlowKit.Soft, TextAnchor.MiddleLeft, true);
            UiKit.Panel(UiKit.Rect(p, "Divider", 600f, 20f, 2f, 98f), new Color(1f, 0.82f, 0.4f, 0.55f));
            enterButton = CharacterFlowKit.GemButton(p, "Enter", "btn-gold", 625f, 33f, 225f, 70f, "ENTER GAME", 26, CharacterFlowKit.Ink, OnEnter);
            deleteButton = CharacterFlowKit.GemButton(p, "Delete", "btn-red", 868f, 48f, 126f, 40f, "DELETE", 17, Color.white, OnDelete);
        }

        /// <summary>Icon + text row of the info panel; the text sits in a holder so a row can hide as a whole.</summary>
        Text InfoRow(RectTransform p, string name, string icon, float y)
        {
            RectTransform row = UiKit.Rect(p, name + "Row", 160f, y, 420f, 24f);
            CharacterFlowKit.Pic(row, "Icon", CharacterFlowKit.Art(icon), 0f, 1f, 22f, 22f);
            return UiKit.Label(row, "Text", 32f, 0f, 380f, 24f, "", 18, new Color(0.92f, 0.95f, 1f), TextAnchor.MiddleLeft, true);
        }

        void BuildCorner()
        {
            CharacterFlowKit.Title(root, "CHARACTER LIST", out titleRt);
            RectTransform gear = MakeGear();
            CharacterFlowKit.Clickable(gear, gear.GetComponent<Image>(), () => menuRt.gameObject.SetActive(!menuRt.gameObject.activeSelf));
            menuRt = UiKit.Rect(root, "GearMenu", 0f, 0f, 200f, 0f);
            menuRt.anchorMin = menuRt.anchorMax = menuRt.pivot = new Vector2(1f, 1f);
            float y = 0f;
            AddMenuItem("Log out", ref y, () => AccountFlow.Logout());
            AddMenuItem("Play offline", ref y, () => ConnectScreenGui.LeaveToOffline(GameSession.Instance));
            if (ConnectScreenGui.DevPageAvailable) AddMenuItem("Direct connect (dev)", ref y, ConnectScreenGui.ShowDevPage);
            menuRt.sizeDelta = new Vector2(200f, y);
            menuRt.gameObject.SetActive(false);
        }

        RectTransform MakeGear()
        {
            Image img = CharacterFlowKit.Pic(root, "Gear", UiArt.Get("hud-settings"), 0f, 0f, 46f, 46f);
            img.raycastTarget = true;
            gearRt = img.rectTransform;
            gearRt.anchorMin = gearRt.anchorMax = gearRt.pivot = new Vector2(1f, 1f);
            return gearRt;
        }

        void AddMenuItem(string label, ref float y, System.Action act)
        {
            Button b = CharacterFlowKit.GemButton(menuRt, "Item" + label, "btn-navy", 0f, y, 200f, 48f, label, 16, Color.white, () => { menuRt.gameObject.SetActive(false); act(); });
            y += 52f;
        }
    }
}

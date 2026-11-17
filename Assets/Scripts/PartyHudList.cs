using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Compact party list of the new HUD (docs/reference/ui-redesign-261116/chatgpt/party/, user decisions 2026-11-17): under the player status
    /// block, top-left, on mobile and PC. Teammates only (your own HP is in the status block): round class portrait, crown on the leader,
    /// "Lv.N Name", framed HP bar (+ MP when the teammate is in sight), up to three buff slots; dead = greyed with an X, on another map or out of
    /// range = faded with the map name. Built from the status block's ChatGPT pieces (hudc/*), so it matches party-hud-chatgpt-1/2.png.
    /// The chevron collapses it to the title; tapping the title opens the full party board (the older PartyPanel: every member, Leave / Kick).
    /// Shown while UiScreens.UseNew(Hud), online and in a party.
    /// </summary>
    public sealed class PartyHudList : MonoBehaviour
    {
        // Sizes from the ChatGPT board (party-hud-chatgpt-3.png: row 4.5-5 % of the height, list 18-20 % of a 16:9 width) on the 640-unit canvas.
        const float Left = 8f, GapBelowStatus = 6f, W = 212f, TitleH = 18f, RowH = 34f, RowGap = 3f, Face = 30f, BarH = 8f, MpH = 6f, BuffSize = 13f;
        const int MaxRows = PartyBook.MaxMembers - 1, MaxBuffs = 3;
        const float RingHole = 0.06f; // the portrait ring's window, as on the status block
        static readonly Color Dim = new Color(0.55f, 0.55f, 0.6f, 1f);
        // Sheet HP1 (ChatGPT, Tools/ui-paper-sheets/hp1-party-pieces.json)
        const string PIcon = "hudp/party-icon", PDown = "hudp/chevron-down", PRight = "hudp/chevron-right", PCrown = "hudp/crown", PSkull = "hudp/dead-skull",
            PDeadX = "hudp/dead-x", PPin = "hudp/away-pin", PRow = "hudp/row-frame", PRowSelected = "hudp/row-frame-selected", PTitle = "hudp/title-frame";

        /// <summary>The full party board (PartyPanel) was opened from the list title.</summary>
        public static bool BoardOpen { get; private set; }

        sealed class Row
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Face, Crown, Skull, DeadX, Pin;
            public Text Name, Status;
            public HudCompactBar Hp, Mp;
            public HeroPortraitLayers Portrait;
            public GameObject[] Buffs = new GameObject[MaxBuffs];
            public Image[] BuffIcons = new Image[MaxBuffs];
        }

        RectTransform root;
        Text title;
        Image chevron;
        readonly Row[] rows = new Row[MaxRows];
        readonly List<BuffView> views = new List<BuffView>(8);
        bool collapsed;

        public static void Create(Transform canvas) { UiKit.Fill(canvas, "PartyHudList").gameObject.AddComponent<PartyHudList>().Build(); }

        void Build()
        {
            // Laid out like HTML: the list is a vertical stack that sizes itself to its rows (VerticalLayoutGroup + ContentSizeFitter), so
            // nothing is placed by hand and nothing can stick out of a frame (user 17/11: "lv and mana poke out of the party frame").
            root = UiWinKit.Anchored(transform, "List", new Vector2(0f, 1f), new Vector2(0f, 1f), Left, -(PlayerStatusBadge.Bottom + GapBelowStatus), W, TitleH);
            Stack(root.gameObject, RowGap, new RectOffset(0, 0, 0, 0), true);
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform head = UiKit.Rect(root, "Title", 0f, 0f, W, TitleH);
            Size(head, W, TitleH);
            Image headBg = HudCompact.Capsule(Backdrop(head), PTitle, Face0(PTitle), HudCompact.Rim, TitleH * 0.5f, true);
            var headRow = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            headRow.padding = new RectOffset(6, 2, 1, 1); headRow.spacing = 3f; headRow.childAlignment = TextAnchor.MiddleLeft;
            headRow.childControlWidth = headRow.childControlHeight = true; headRow.childForceExpandWidth = headRow.childForceExpandHeight = false;
            Size(Icon(head, "Icon", PIcon, 0f, 0f, TitleH - 4f).rectTransform, TitleH - 4f, TitleH - 4f);
            title = PaperTheme.Body(head, "Text", 0f, 0f, 10f, TitleH, "Party", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleLeft, true);
            Size(title.rectTransform, -1f, TitleH - 2f, 1f);
            UiWinKit.MakeButton(headBg, ToggleBoard); // the title opens / closes the full board
            chevron = UiKit.Rect(head, "Chevron", 0f, 0f, TitleH - 2f, TitleH - 2f).gameObject.AddComponent<Image>();
            Size(chevron.rectTransform, TitleH - 2f, TitleH - 2f);
            chevron.sprite = PaperArt.Get(PDown);
            chevron.preserveAspect = true;
            UiWinKit.MakeButton(chevron, () => collapsed = !collapsed);
            for (int i = 0; i < MaxRows; i++) rows[i] = BuildRow(i);
            root.gameObject.SetActive(false);
        }

        /// <summary>
        /// One teammate: a padded row [portrait | name / HP / MP stacked | buffs or pin] on the row frame. The row's height follows its
        /// content (a hidden bar or the "Out of range" line simply drops out of the stack), from the status block's pieces + sheet HP1.
        /// </summary>
        Row BuildRow(int i)
        {
            var r = new Row();
            r.Root = UiKit.Rect(root, "Mate" + i, 0f, 0f, W, RowH);
            Size(r.Root, W, -1f);
            HudCompact.Capsule(Backdrop(r.Root), PRow, Face0(PRow), HudCompact.Outline, 7f);
            r.Group = r.Root.gameObject.AddComponent<CanvasGroup>();
            r.Group.blocksRaycasts = false;
            var line = r.Root.gameObject.AddComponent<HorizontalLayoutGroup>();
            line.padding = new RectOffset(4, 6, 7, 7); line.spacing = 4f; line.childAlignment = TextAnchor.MiddleLeft; // top / bottom 7: 4 read too tight (user 17/11)
            line.childControlWidth = line.childControlHeight = true; line.childForceExpandWidth = line.childForceExpandHeight = false;
            r.Root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform faceBox = UiKit.Rect(r.Root, "FaceBox", 0f, 0f, Face, Face);
            Size(faceBox, Face, Face);
            Image ring;
            r.Face = HudCompact.Portrait(faceBox, "Portrait", 0f, 0f, Face, PaperPiece.HcPortraitRing, PaperPiece.HcPortraitPlate, RingHole, out ring);
            r.Portrait = new HeroPortraitLayers(r.Face);
            r.Skull = Icon(faceBox, "Skull", PSkull, Face * 0.2f, Face * 0.2f, Face * 0.6f);
            r.DeadX = Icon(faceBox, "DeadX", PDeadX, Face - 10f, Face - 11f, 11f);
            r.Crown = Icon(r.Root, "Crown", PCrown, -3f, -5f, 13f);
            r.Crown.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; // sits on the frame's corner, outside the flow

            RectTransform info = UiKit.Rect(r.Root, "Info", 0f, 0f, 10f, 10f);
            Size(info, -1f, -1f, 1f);
            Stack(info.gameObject, 2f, new RectOffset(0, 0, 0, 0), true);
            r.Name = PaperTheme.Body(info, "Name", 0f, 0f, 10f, 12f, "", PaperTheme.BarSize, Color.white, PaperInk.Outlined, TextAnchor.MiddleLeft, true);
            r.Name.supportRichText = true;
            r.Name.horizontalOverflow = HorizontalWrapMode.Overflow;
            Size(r.Name.rectTransform, -1f, 12f);
            RectTransform hp = UiKit.Rect(info, "Hp", 0f, 0f, 10f, BarH); Size(hp, -1f, BarH);
            r.Hp = new HudCompactBar(hp, BarH, PaperPiece.HcBarFrame, PaperPiece.HcBarFill, PaperTheme.BarHp, 1.1f, 1.1f);
            RectTransform mp = UiKit.Rect(info, "Mp", 0f, 0f, 10f, MpH); Size(mp, -1f, MpH);
            r.Mp = new HudCompactBar(mp, MpH, PaperPiece.HcBarFrame, PaperPiece.HcBarFill, PaperTheme.BarMp, 0.9f, 0.9f);
            r.Status = PaperTheme.Body(info, "Status", 0f, 0f, 10f, 12f, "", PaperTheme.BarSize - 1, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleLeft);
            Size(r.Status.rectTransform, -1f, 12f);

            RectTransform side = UiKit.Rect(r.Root, "Side", 0f, 0f, 10f, BuffSize);
            var sideRow = side.gameObject.AddComponent<HorizontalLayoutGroup>();
            sideRow.spacing = 1f; sideRow.childAlignment = TextAnchor.MiddleRight;
            sideRow.childControlWidth = sideRow.childControlHeight = true; sideRow.childForceExpandWidth = sideRow.childForceExpandHeight = false;
            for (int b = 0; b < MaxBuffs; b++)
            {
                RectTransform slot = UiKit.Rect(side, "Buff" + b, 0f, 0f, BuffSize, BuffSize);
                Size(slot, BuffSize, BuffSize);
                PaperArt.Draw(slot, PaperPiece.HcBuffSlot, HudCompact.Dark, Color.clear, 2f);
                r.BuffIcons[b] = UiKit.Fill(slot, "Icon", 1f).gameObject.AddComponent<Image>();
                r.BuffIcons[b].raycastTarget = false;
                r.Buffs[b] = slot.gameObject;
            }
            r.Pin = Icon(side, "Pin", PPin, 0f, 0f, BuffSize + 2f);
            Size(r.Pin.rectTransform, BuffSize + 2f, BuffSize + 2f);
            return r;
        }

        /// <summary>A vertical stack: children keep their preferred heights and take the full width (`expandWidth`).</summary>
        static void Stack(GameObject go, float spacing, RectOffset padding, bool expandWidth)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = padding; v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = expandWidth; v.childForceExpandHeight = false;
        }

        /// <summary>CSS-like box size of a layout child: width / height &lt; 0 = free (from content), `grow` = flex-grow of the width.</summary>
        static void Size(RectTransform rt, float w, float h, float grow = 0f)
        {
            LayoutElement le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            if (w >= 0f) { le.minWidth = w; le.preferredWidth = w; }
            if (h >= 0f) { le.minHeight = h; le.preferredHeight = h; }
            le.flexibleWidth = grow;
        }

        /// <summary>The frame of a laid-out box: a full-size child outside the flow (like a CSS background).</summary>
        static RectTransform Backdrop(RectTransform box)
        {
            RectTransform bg = UiKit.Fill(box, "Frame");
            bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            bg.SetAsFirstSibling();
            return bg;
        }

        /// <summary>Tint of a frame: the painted art as it is, else the flat dark fallback.</summary>
        static Color Face0(string piece) { return PaperArt.Has(piece) ? Color.white : HudCompact.Dark; }

        /// <summary>A plain picture of a sheet piece, `size` units high, aspect kept (no touch).</summary>
        static Image Icon(Transform parent, string name, string piece, float x, float y, float size)
        {
            Image img = UiKit.Rect(parent, name, x, y, size, size).gameObject.AddComponent<Image>();
            img.sprite = PaperArt.Get(piece);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>The list title and the HUD Party button: open / close the full party board (PartyPanel); without a party, say how to make one.</summary>
        public static void ToggleBoard()
        {
            if (!NetClient.Party.InParty) { BoardOpen = false; RpgHud.Toast("Tap a player to invite them to a party", PaperTheme.TextLight); return; }
            BoardOpen = !BoardOpen;
        }

        void Update()
        {
            PartyClientState party = NetClient.Party;
            bool show = UiScreens.UseNew(UiScreen.Hud) && PartyPanel.Live && party.InParty && !UiWindows.ModalOpen && !HeroChangeScreen.IsOpen;
            if (!show) BoardOpen = false;
            if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
            if (!show) return;
            Fill(party);
        }

        void Fill(PartyClientState party)
        {
            title.text = "Party " + party.Members.Count + "/" + PartyBook.MaxMembers;
            chevron.sprite = PaperArt.Get(collapsed ? PRight : PDown);
            string myMap = MapRegistry.Current != null ? MapRegistry.Current.Id : "";
            int shown = 0;
            for (int m = 0; m < party.Members.Count && shown < MaxRows; m++)
            {
                PartyMemberInfo info = party.Members[m];
                if (info.PlayerId == party.MyId) continue; // teammates only (user 17/11)
                FillRow(rows[shown++], info, m == 0, myMap);
            }
            for (int i = 0; i < MaxRows; i++) rows[i].Root.gameObject.SetActive(!collapsed && i < shown);
        }

        void FillRow(Row r, PartyMemberInfo info, bool leader, string myMap)
        {
            HeroController hero = FindHero(info.PlayerId);
            bool dead = info.Hp == 0, otherMap = info.MapId != myMap, away = otherMap || hero == null;
            HeroClassDef cls = info.HeroClass < HeroClassDef.All.Length ? HeroClassDef.All[info.HeroClass] : null;
            if (hero != null) r.Portrait.Show(hero); else r.Portrait.ShowPainted(cls); // out of sight: this client has no look for them
            r.Portrait.SetTint(dead ? Dim : Color.white);
            r.Skull.enabled = dead;
            r.DeadX.enabled = dead;
            r.Crown.enabled = leader;
            r.Name.text = "<color=#FDC04E>Lv." + info.Level + "</color> " + info.Name;
            bool bars = !away && !dead;
            r.Hp.Root.gameObject.SetActive(bars);
            r.Hp.Set(info.MaxHp > 0 ? (float)info.Hp / info.MaxHp : 0f);
            bool mp = bars && hero.MaxMp > 0f; // the roster has no MP: only a teammate in sight shows one
            r.Mp.Root.gameObject.SetActive(mp);
            if (mp) r.Mp.Set(hero.Mp / hero.MaxMp);
            r.Status.text = dead ? "Dead" : otherMap ? MapName(info.MapId) : away ? "Out of range" : "";
            r.Status.gameObject.SetActive(r.Status.text.Length > 0);
            r.Pin.enabled = away && !dead;
            r.Group.alpha = dead ? 0.8f : away ? 0.6f : 1f;
            views.Clear();
            if (bars) HeroBuffs.ActiveFor(hero, views); // away rows show the pin where the buffs were
            int b = 0;
            foreach (BuffView v in views)
            {
                if (b >= MaxBuffs) break;
                if (v.Source == BuffSource.None) continue;
                r.BuffIcons[b].sprite = SkillIcons.Get(BuffSource.Def(v.Source));
                r.Buffs[b++].SetActive(true);
            }
            for (; b < MaxBuffs; b++) r.Buffs[b].SetActive(false);
        }

        static HeroController FindHero(ushort id)
        {
            Zone zone = Zone.Main;
            if (zone == null) return null;
            foreach (HeroController h in zone.Heroes.All) if (h != null && h.PlayerId == id) return h; // the roster speaks player ids (PartyAllies does the same)
            return null;
        }

        static string MapName(string id)
        {
            IGameMap map = MapRegistry.Get(id);
            return map != null ? map.Name : "Another map";
        }
    }
}

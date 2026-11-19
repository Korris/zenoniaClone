using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// "Party (n/3)" panel of the RO Soft HUD (02-hud.png), under the status block: the navy title pill (tap = the full party board,
    /// PartyPanel), the round arrow that collapses / expands the rows, and one ivory card per teammate (your own HP is the status block,
    /// user rule 17/11): round portrait, role icon on its corner (crown = leader, else the class's role: sword fighters, green cross
    /// Cleric), "Lv.N Name", HP bar, and StatusIcons, an anchor right of the name for status icons (poison ...). Dead = greyed, other
    /// map / out of sight = faded with the map name instead of the bar. Data: NetClient.Party (server roster); shown online in a party.
    /// </summary>
    public sealed class RoHudParty
    {
        const float W = 196f, TitleH = 32f, RowH = 46f, RowGap = 5f, Face = 40f, BarH = 12f;
        const int MaxRows = PartyBook.MaxMembers - 1;
        static readonly Color Dim = new Color(0.6f, 0.6f, 0.64f, 1f);

        sealed class Row
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Face, Role;
            public HeroPortraitLayers Portrait;
            public RawImage Live; // the mate's real look (RoHudPortraits slot i + 1) while its hero is in sight
            public int Slot;
            public Text Name, Status;
            public RoHud.Bar Hp;
            public RectTransform StatusIcons;
            public PoisonStatusIcon Poison; // wanted boss poison on this teammate (2026-11-19)
        }

        readonly RectTransform root, arrow;
        readonly Text title;
        readonly Row[] rows = new Row[MaxRows];
        bool collapsed;
        /// <summary>The rows are folded (capture logs; any instance: there is one party panel).</summary>
        internal static bool Collapsed;
        int shownVersion = -1;

        public RoHudParty(RectTransform safe, float top)
        {
            root = UiKit.Rect(safe, "RoHudParty", RoHud.Margin, top, W, TitleH + MaxRows * (RowH + RowGap));
            RectTransform head;
            title = RoHud.Pill(root, "Title", RoHud.TopLeft, RoHud.TopLeft, 0f, 0f, TitleH, "hud-party-title", "hud-icon-party", 16f, "Party (0/3)", 13, RoHud.TextOnNavy, out head, W, true);
            title.alignment = TextAnchor.MiddleLeft;
            head.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            UiWinKit.MakeButton(head.GetComponent<Image>(), PartyHudList.ToggleBoard);
            arrow = RoHud.Box(root, "Collapse", RoHud.TopLeft, W - TitleH * 0.5f - 3f, -TitleH * 0.5f, TitleH - 6f, TitleH - 6f); // on the pill's right end, as 02-hud.png // centre pivot: it turns
            Image arrowArt = RoHud.Art(arrow, "hud-btn-arrow", true);
            UiWinKit.MakeButton(arrowArt, () => Collapsed = collapsed = !collapsed);
            for (int i = 0; i < MaxRows; i++) rows[i] = BuildRow(i);
            root.gameObject.SetActive(false);
        }

        Row BuildRow(int i)
        {
            var r = new Row();
            r.Root = RoHud.TopLeftBox(root, "Mate" + i, 0f, TitleH + RowGap + i * (RowH + RowGap), W, RowH);
            RoHud.Art(r.Root, "hud-party-row");
            r.Group = r.Root.gameObject.AddComponent<CanvasGroup>();
            r.Group.blocksRaycasts = false;
            RectTransform faceBox = RoHud.TopLeftBox(r.Root, "Face", 6f, (RowH - Face) * 0.5f, Face, Face);
            RectTransform m = RoHudPortraits.Frame(faceBox);
            r.Face = UiKit.Fill(m, "Portrait").gameObject.AddComponent<Image>();
            r.Face.raycastTarget = false;
            r.Portrait = new HeroPortraitLayers(r.Face);
            r.Slot = i + 1;
            r.Live = RoHudPortraits.Make(m, r.Slot);
            r.Role = RoHud.Art(RoHud.TopLeftBox(r.Root, "Role", 1f, 1f, 15f, 15f), "hud-icon-crown");
            r.Name = RoHud.Label(RoHud.TopLeftBox(r.Root, "Name", 52f, 6f, 100f, 16f), "", 12, RoHud.Ink, TextAnchor.MiddleLeft);
            r.StatusIcons = RoHud.TopLeftBox(r.Root, "StatusIcons", W - 36f, 3f, 22f, 22f); // poison and other status icons (right of the name)
            r.Poison = new PoisonStatusIcon(r.StatusIcons, 22f);
            r.Hp = new RoHud.Bar(r.Root, "Hp", 52f, 26f, W - 62f, BarH, "hud-bar-track", "hud-bar-teal");
            r.Status = RoHud.Label(RoHud.TopLeftBox(r.Root, "Status", 52f, 25f, W - 62f, 14f), "", 11, RoSoftTheme.InkSoft, TextAnchor.MiddleLeft, false);
            return r;
        }

        /// <summary>The status icon anchor of teammate row `i` (0 = first teammate shown), for other HUD pieces.</summary>
        public RectTransform StatusIconsOf(int i) { return i >= 0 && i < MaxRows ? rows[i].StatusIcons : null; }

        public void Tick(bool hudShown)
        {
            PartyClientState party = NetClient.Party;
            bool show = hudShown && PartyPanel.Live && party.InParty;
            if (!show) { if (root.gameObject.activeSelf) root.gameObject.SetActive(false); shownVersion = -1; return; }
            if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);
            if (shownVersion != party.Version) { shownVersion = party.Version; foreach (Row r in rows) r.Hp.Relayout(); }
            title.text = "Party (" + party.Members.Count + "/" + PartyBook.MaxMembers + ")";
            arrow.localEulerAngles = new Vector3(0f, 0f, collapsed ? 0f : -90f); // right = collapsed, down = open
            string myMap = MapRegistry.Current != null ? MapRegistry.Current.Id : "";
            int shown = 0;
            for (int m = 0; m < party.Members.Count && shown < MaxRows; m++)
            {
                PartyMemberInfo info = party.Members[m];
                if (info.PlayerId == party.MyId) continue;
                Fill(rows[shown++], info, m == 0, myMap);
            }
            for (int i = 0; i < MaxRows; i++)
            {
                bool on = !collapsed && i < shown;
                if (rows[i].Root.gameObject.activeSelf != on) rows[i].Root.gameObject.SetActive(on);
            }
        }

        static void Fill(Row r, PartyMemberInfo info, bool leader, string myMap)
        {
            HeroController hero = PartyHudList.FindHero(info.PlayerId);
            bool dead = info.Hp == 0, otherMap = info.MapId != myMap, away = otherMap || hero == null;
            HeroClassDef cls = info.HeroClass < HeroClassDef.All.Length ? HeroClassDef.All[info.HeroClass] : null;
            bool real = RoHudPortraits.Show(r.Live, r.Slot, hero); // in sight: its NetAppearance look (hair, colours, hat)
            if (r.Face.gameObject.activeSelf == real) r.Face.gameObject.SetActive(!real);
            if (!real) { if (hero != null) r.Portrait.Show(hero); else r.Portrait.ShowPainted(cls); }
            r.Portrait.SetTint(dead ? Dim : Color.white);
            r.Live.color = dead ? Dim : Color.white;
            Sprite role = RoHud.Piece(leader ? "hud-icon-crown" : cls != null && cls.Id == HeroClass.Cleric ? "hud-icon-plus" : "hud-icon-sword");
            if (r.Role.sprite != role) r.Role.sprite = role;
            r.Name.text = "Lv." + info.Level + " " + info.Name;
            bool bar = !away && !dead;
            r.Hp.Root.gameObject.SetActive(bar);
            if (bar) r.Hp.Set(info.MaxHp > 0 ? (float)info.Hp / info.MaxHp : 0f);
            r.Status.text = dead ? "Dead" : otherMap ? PartyHudList.MapName(info.MapId) : away ? "Out of range" : "";
            r.Group.alpha = dead ? 0.8f : away ? 0.65f : 1f;
            r.Poison.Show(bar ? hero : null);
        }
    }
}

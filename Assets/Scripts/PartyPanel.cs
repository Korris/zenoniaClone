using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// The party on the HUD: one row per member with class icon, name and HP, a crown on the leader, Leave, and for the leader a Kick button on the others. A member on another map
    /// shows a slate bar and the map name. The invite you have out waits on its own row with Cancel. Shown online only, and only when there is something to show: the server's messages
    /// (NetClient.Party) are the only source. It docks left of the minimap, because five rows under the Bag / Settings buttons would run into the skill arc, and starts below the boss bar.
    /// </summary>
    public sealed class PartyPanel : MonoBehaviour
    {
        const float W = 214f, TitleH = 40f, RowH = 40f, RowFrameH = 38f, PendingH = 42f, PadBottom = 12f, Inset = 8f, IconSize = 28f, KickSize = 30f, HpH = 15f, CrownSize = 16f;
        const float RowW = W - Inset * 2f, TextX = 38f, DockX = -218f, DockY = -44f;

        sealed class Row
        {
            public RectTransform Root;
            public Image Crown, ClassIcon;
            public Text Name;
            public UiKit.Bar Hp;
            public GameObject Kick;
        }

        readonly Row[] rows = new Row[PartyBook.MaxMembers];
        RectTransform panel, pendingRow;
        GameObject leave;
        Text title, pendingText;
        int shownVersion = -1, shownPendingSeconds = -1;
        string shownMap;
        double cancelledInvite;

        /// <summary>A client playing right now: the party UI has no business anywhere else (offline, on a host, on the connect screen).</summary>
        public static bool Live
        {
            get { return NetMode.IsClient && NetClient.Active != null && NetClient.Active.State == ClientState.Joined && GameSession.Hero != null && !Hud.CaptureClean; }
        }

        public static void Create(Transform canvas)
        {
            UiKit.Fill(canvas, "PartyPanel").gameObject.AddComponent<PartyPanel>().Build(); // full-screen layer: children anchor to the real screen
        }

        void Build()
        {
            panel = UiWinKit.Anchored(transform, "Panel", new Vector2(1f, 1f), new Vector2(1f, 1f), DockX, DockY, W, TitleH);
            PartyArt.Sliced(panel, "panel-frame", PartyArt.PanelFrameScale, true);
            title = UiKit.Label(panel, "Title", 16f, 6f, 110f, 30f, "Party", 17, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            leave = PartyArt.ArtButton(panel, "Leave", "btn-leave", W - 68f - 12f, 7f, 68f, 28f, "Leave", 13, UiWinKit.Cream, () => NetClient.SendParty(PartyOp.Leave)).gameObject;
            for (int i = 0; i < rows.Length; i++) rows[i] = BuildRow(i);

            pendingRow = UiKit.Rect(panel, "Pending", Inset, TitleH, RowW, PendingH);
            pendingText = UiKit.Label(pendingRow, "Text", 8f, 3f, RowW - 76f - 12f, PendingH - 6f, "", 12, UiWinKit.Cream, TextAnchor.MiddleLeft);
            PartyArt.ArtButton(pendingRow, "Cancel", "btn-cancel", RowW - 76f, 6f, 76f, 30f, "Cancel", 13, Color.white, CancelInvite);
            panel.gameObject.SetActive(false);
        }

        Row BuildRow(int index)
        {
            var r = new Row();
            r.Root = UiKit.Rect(panel, "Member" + index, Inset, TitleH + index * RowH, RowW, RowFrameH);
            PartyArt.Sliced(r.Root, "row-frame", PartyArt.RowFrameScale);
            r.ClassIcon = PartyArt.Plain(UiKit.Rect(r.Root, "Class", 5f, 5f, IconSize, IconSize), "class-warrior");
            r.Name = UiKit.Label(r.Root, "Name", TextX, 2f, RowW - TextX - 6f, 17f, "", 13, Color.white, TextAnchor.MiddleLeft, true);
            r.Name.verticalOverflow = VerticalWrapMode.Truncate; // one line; a long name shrinks to fit (17 units clear this font's line height, 13 did not and hid the text)
            r.Name.resizeTextForBestFit = true;
            r.Name.resizeTextMinSize = 8; r.Name.resizeTextMaxSize = 13;
            r.Hp = PartyArt.HpBar(r.Root, "Hp", TextX, 19f, RowW - TextX - 6f, HpH, 10);
            r.Kick = UiWinKit.MakeButton(PartyArt.Plain(UiKit.Rect(r.Root, "Kick", RowW - KickSize - 3f, 4f, KickSize, KickSize), "kick"), () => KickRow(index)).gameObject;
            r.Crown = PartyArt.Plain(UiKit.Rect(r.Root, "Crown", -4f, -5f, CrownSize, CrownSize), "crown");
            return r;
        }

        void KickRow(int index)
        {
            PartyClientState party = NetClient.Party;
            if (index < party.Members.Count) NetClient.SendParty(PartyOp.Kick, party.Members[index].PlayerId);
        }

        void CancelInvite()
        {
            PartyClientState.OpenInvite? open = NetClient.Party.Outgoing;
            if (!open.HasValue) return;
            cancelledInvite = open.Value.ExpiresAtMs; // gone from the panel now; a second press must not ask again
            NetClient.SendParty(PartyOp.CancelInvite);
        }

        void Update()
        {
            PartyClientState party = NetClient.Party;
            PartyClientState.OpenInvite? open = party.Outgoing;
            if (open.HasValue && open.Value.ExpiresAtMs == cancelledInvite) open = null;
            bool show = Live && (party.InParty || open.HasValue) && !InventoryPanel.IsOpen && !HeroChangeScreen.IsOpen && !UiWindows.ModalOpen;
            if (panel.gameObject.activeSelf != show) panel.gameObject.SetActive(show);
            if (!show) return;
            string map = MapRegistry.Current != null ? MapRegistry.Current.Id : "";
            bool layoutDue = party.Version != shownVersion || map != shownMap;
            int seconds = open.HasValue ? Mathf.Max(0, Mathf.CeilToInt((float)((open.Value.ExpiresAtMs - NetClient.ClientMs) / 1000.0))) : -1;
            if (layoutDue || seconds != shownPendingSeconds) { shownVersion = party.Version; shownMap = map; shownPendingSeconds = seconds; Fill(party, open, seconds, map); }
        }

        void Fill(PartyClientState party, PartyClientState.OpenInvite? open, int seconds, string myMap)
        {
            int n = party.Members.Count;
            title.text = n > 0 ? "Party " + n + "/" + PartyBook.MaxMembers : "Party";
            leave.SetActive(party.InParty);
            for (int i = 0; i < rows.Length; i++)
            {
                Row r = rows[i];
                r.Root.gameObject.SetActive(i < n);
                if (i >= n) continue;
                PartyMemberInfo m = party.Members[i];
                bool me = m.PlayerId == party.MyId, away = m.MapId != myMap, kick = party.IsLeader && !me;
                r.Crown.enabled = i == 0;
                r.ClassIcon.sprite = PartyArt.ClassIcon(m.HeroClass);
                r.Name.text = m.Name;
                r.Name.color = me ? new Color(1f, 0.9f, 0.5f) : Color.white;
                r.Hp.Root.sizeDelta = new Vector2(RowW - TextX - 6f - (kick ? KickSize + 3f : 0f), HpH);
                r.Hp.Set(m.MaxHp > 0 ? (float)m.Hp / m.MaxHp : 0f);
                PartyArt.SetHpFill(r.Hp, HpH, away);
                r.Hp.Caption.text = away ? MapName(m.MapId) : m.Hp + " / " + m.MaxHp;
                r.Kick.SetActive(kick);
            }
            pendingRow.gameObject.SetActive(open.HasValue);
            pendingRow.anchoredPosition = new Vector2(Inset, -(TitleH + n * RowH));
            if (open.HasValue) pendingText.text = "Inviting " + open.Value.Name + "\n" + seconds + " s";
            panel.sizeDelta = new Vector2(W, TitleH + n * RowH + (open.HasValue ? PendingH : 0f) + PadBottom);
        }

        static string MapName(string id)
        {
            IGameMap map = MapRegistry.Get(id);
            return map != null ? map.Name : "another map";
        }
    }
}

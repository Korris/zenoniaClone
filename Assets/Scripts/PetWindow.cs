using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The one Pet window (P, or the Pet cell of the inventory), docs/pet-system-design.md 9: the account's wallet on top (gold, Gacha Coins, Pet Crystals, Pet Food, balls),
    /// the 10 species in two rows (owned ones lit, the selected one framed gold) and four tabs below: Team (PetWindow.Team.cs), Collection + Growth (PetWindow.Growth.cs),
    /// Capture (PetWindow.Capture.cs); the Gacha button opens PetGachaWindow. Reads PlayerState.Account (offline the local file, online the server's mirror); every button
    /// goes through PetActions, so the same rules run on both sides. Redraws only when the fingerprint of what it shows changes.
    /// </summary>
    public sealed partial class PetWindow : MonoBehaviour
    {
        public enum Tab { Team, Collection, Growth, Capture }

        const float W = 640f, H = 584f, Cell = 84f, GridY = 92f, BodyY = 292f, BodyH = H - BodyY - 20f;
        static PetWindow instance;

        sealed class PetCell { public Image Frame, Icon; public Text Name, Stars; }

        readonly PetCell[] cells = new PetCell[PetConfig.All.Length];
        readonly Image[] tabButtons = new Image[4];
        RectTransform root, teamTab, collectionTab, growthTab, captureTab;
        Text walletText;
        Tab tab;
        int selected, shownKey = -1;
        bool open;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "PetWindow").gameObject.AddComponent<PetWindow>();
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void Open()
        {
            if (instance == null) return;
            PlayerState p = PetActions.Me;
            if (p != null && p.PetId != "") instance.selected = Mathf.Max(0, PetConfig.IndexOf(p.Pet));
            instance.open = true;
            instance.root.gameObject.SetActive(true);
            instance.SetTab(instance.tab);
        }

        public static void Close() { if (instance == null) return; instance.open = false; instance.root.gameObject.SetActive(false); }
        public static void Toggle() { if (IsOpen) Close(); else Open(); }
        /// <summary>Capture runs (--open-pets team|collection|growth|capture).</summary>
        public static void ShowTab(Tab t) { if (instance != null && instance.open) instance.SetTab(t); }
        public static void Select(int petIndex) { if (instance != null) { instance.selected = Mathf.Clamp(petIndex, 0, PetConfig.All.Length - 1); instance.shownKey = -1; } }

        void Build()
        {
            root = UiWinKit.Anchored(transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, W, H);
            UiWinKit.Sliced(root, "panel", W / 639f, true);
            UiKit.Label(root, "Title", 32f, 18f, 160f, 34f, "Pets  [P]", 22, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleLeft, true);
            walletText = UiKit.Label(root, "Wallet", 190f, 18f, W - 262f, 34f, "", 13, UiWinKit.Cream, TextAnchor.MiddleRight);
            UiWinKit.CloseButton(root, W - 62f, 20f, 32f, Close);
            string[] names = { "Team", "Collection", "Growth", "Capture" };
            for (int i = 0; i < 4; i++)
            {
                Tab t = (Tab)i;
                Button b = UiWinKit.ArtButton(root, "Tab" + names[i], "btn-navy", 34f + i * 112f, 56f, 104f, 30f, names[i], 14, Color.white, () => SetTab(t));
                tabButtons[i] = (Image)b.targetGraphic;
            }
            UiWinKit.ArtButton(root, "Gacha", "btn-gold", W - 34f - 120f, 56f, 120f, 30f, "Gacha  [G]", 14, new Color(0.25f, 0.12f, 0.02f), () => { Close(); PetGachaWindow.Open(); });
            for (int i = 0; i < cells.Length; i++) cells[i] = BuildCell(i);
            teamTab = Body("TeamTab"); collectionTab = Body("CollectionTab"); growthTab = Body("GrowthTab"); captureTab = Body("CaptureTab");
            BuildTeam(); BuildCollection(); BuildGrowth(); BuildCapture();
            root.gameObject.SetActive(false);
        }

        RectTransform Body(string name)
        {
            RectTransform rt = UiKit.Rect(root, name, 30f, BodyY, W - 60f, BodyH);
            UiWinKit.Sliced(rt, "section", 0.3f);
            return rt;
        }

        PetCell BuildCell(int i)
        {
            float x = 44f + (i % 5) * (Cell + 30f), y = GridY + (i / 5) * (Cell + 10f); // 10 species: 5 x 2
            RectTransform rt = UiKit.Rect(root, "Pet" + i, x, y, Cell, Cell);
            var c = new PetCell { Frame = UiWinKit.Sliced(rt, "slot", Cell / 160f, true) };
            UiWinKit.MakeButton(c.Frame, () => { selected = i; shownKey = -1; Refresh(); });
            c.Icon = UiKit.Icon(rt, "Icon", 13f, 8f, 58f);
            c.Name = UiKit.Label(rt, "Name", -14f, Cell - 22f, Cell + 28f, 18f, "", 10, Color.white, TextAnchor.MiddleCenter, true);
            c.Stars = UiKit.Label(rt, "Stars", Cell - 40f, 8f, 30f, 16f, "", 13, UiWinKit.Gold, TextAnchor.MiddleRight, true);
            return c;
        }

        void SetTab(Tab t)
        {
            tab = t;
            teamTab.gameObject.SetActive(t == Tab.Team);
            collectionTab.gameObject.SetActive(t == Tab.Collection);
            growthTab.gameObject.SetActive(t == Tab.Growth);
            captureTab.gameObject.SetActive(t == Tab.Capture);
            for (int i = 0; i < 4; i++) tabButtons[i].color = i == (int)t ? new Color(1f, 0.92f, 0.55f) : Color.white;
            shownKey = -1;
            Refresh();
        }

        void Update() { if (open) Refresh(); }

        /// <summary>A cheap fingerprint of what the window shows, so it only rebuilds texts when something changed.</summary>
        static int Key(PlayerState p, AccountState a, int sel, Tab tab)
        {
            int k = p.Gold * 31 + sel * 7 + (int)tab * 3 + p.Level * 11;
            foreach (string t in p.Team) k = k * 13 + (t ?? "").GetHashCode();
            if (a != null)
            {
                k = k * 17 + a.tickets + a.crystals * 3 + a.food * 5 + a.balls * 7 + a.rareBalls * 11 + a.sharedShards * 19 + a.ballsBoughtToday * 23 + (a.dayBossCaught ? 29 : 0) + (a.dayWantedCaught ? 37 : 0);
                foreach (OwnedPet o in a.pets) k = k * 13 + o.id.GetHashCode() + o.stars * 5 + o.shards + o.level * 101;
            }
            return k;
        }

        void Refresh()
        {
            PlayerState p = PetActions.Me;
            if (p == null) { Close(); return; }
            AccountState a = p.Account;
            int key = Key(p, a, selected, tab);
            if (key == shownKey) return;
            shownKey = key;
            walletText.text = p.Gold + " G   <color=#bfe6ff>" + (a != null ? a.tickets : 0) + " coins</color>   <color=#9fd0ff>" + (a != null ? a.crystals : 0) + " crystals</color>   <color=#ffd27a>"
                + (a != null ? a.food : 0) + " food</color>   " + (a != null ? a.balls : 0) + " / " + (a != null ? a.rareBalls : 0) + " balls";
            for (int i = 0; i < cells.Length; i++) FillCell(cells[i], PetConfig.All[i], a, p, i == selected);
            PetDef d = PetConfig.All[selected];
            if (tab == Tab.Team) FillTeam(d, a, p);
            else if (tab == Tab.Collection) FillCollection(d, a, p);
            else if (tab == Tab.Growth) FillGrowth(d, a, p);
            else FillCapture(d, a, p);
        }

        static void FillCell(PetCell c, PetDef d, AccountState a, PlayerState p, bool sel)
        {
            OwnedPet o = a != null ? a.Pet(d.Id) : null;
            c.Icon.sprite = PetArt.Icon(d);
            c.Icon.enabled = c.Icon.sprite != null;
            Color tint = PetArt.TintOf(d);
            c.Icon.color = o != null ? tint : new Color(tint.r * 0.3f, tint.g * 0.3f, tint.b * 0.3f, 0.55f); // locked: a dark silhouette
            c.Frame.color = sel ? new Color(1f, 0.95f, 0.6f) : o != null ? Color.Lerp(Color.white, PetArt.LabelColor(d), 0.35f) : new Color(0.7f, 0.7f, 0.75f, 0.8f);
            int slot = System.Array.IndexOf(p.Team, d.Id);
            c.Name.text = (d.Legacy ? "" : PetArt.Label(d) + " ") + d.Name + (slot >= 0 ? " <color=#9fff9f>(" + SlotShort[slot] + ")</color>" : "");
            c.Stars.text = o != null ? PetArt.Stars(o.stars) : "";
        }

        static readonly string[] SlotShare = { "100%", "skill 50%", "skill 50%", "passive 25%", "passive 25%" };
        static readonly string[] SlotShort = { "out", "sup", "sup", "stat", "stat" };
        static readonly string[] SlotNames = { "Combat", "Support 1", "Support 2", "Stat 1", "Stat 2" };
    }
}

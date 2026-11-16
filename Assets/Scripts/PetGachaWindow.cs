using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Pet gacha window (G), docs/pet-system-design.md 4: three tabs. Pull = Gacha Coins and Pet Crystals (150 crystals buy a coin; both earned in play only, no gold, no
    /// real money), UR shards (+ exchange for Frostfang King), pulls left to the SSR+ and UR guarantees, Pull x1 / x10 and the last results read back from the account's
    /// history (so it shows the same offline and online, where the server rolled; a shard result has no pet). Rates = the published table (PetGachaWindow.Tabs.cs).
    /// History = the last 200 pulls, 16 a page.
    /// </summary>
    public sealed partial class PetGachaWindow : MonoBehaviour
    {
        public enum Tab { Pull, Rates, History }

        const float W = 600f, H = 486f, BodyY = 96f;
        /// <summary>The villager who opens this window when talked to (Thatchwick, NpcDefs).</summary>
        public const string KeeperNpcId = "petKeeperLumi";
        static PetGachaWindow instance;

        RectTransform root, pullTab, ratesTab, historyTab;
        readonly Image[] tabButtons = new Image[3];
        Text ticketText, urText, pityText, resultTitle;
        readonly Image[] resultFrames = new Image[PetRules.MaxPullsAtOnce], resultIcons = new Image[PetRules.MaxPullsAtOnce];
        readonly Text[] resultTexts = new Text[PetRules.MaxPullsAtOnce];
        Tab tab;
        int lastPullCount, shownKey = -1;
        bool open;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "PetGachaWindow").gameObject.AddComponent<PetGachaWindow>();
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void Open()
        {
            if (instance == null) return;
            instance.open = true;
            instance.root.gameObject.SetActive(true);
            instance.SetTab(Tab.Pull);
        }

        public static void Close() { if (instance == null) return; instance.open = false; instance.root.gameObject.SetActive(false); }
        public static void Toggle() { if (IsOpen) Close(); else Open(); }
        public static void ShowTab(Tab t) { if (instance != null && instance.open) instance.SetTab(t); }

        /// <summary>Capture runs: the Pull button pressed by code.</summary>
        public static void TestPull(int count) { if (instance != null) instance.Pull(count); }

        void Build()
        {
            root = UiWinKit.Anchored(transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 8f, W, H);
            UiWinKit.Sliced(root, "panel", W / 639f, true);
            UiKit.Label(root, "Title", 32f, 18f, 300f, 34f, "Pet Gacha  [G]", 22, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleLeft, true);
            UiWinKit.CloseButton(root, W - 62f, 20f, 32f, Close);
            string[] names = { "Pull", "Rates", "History" };
            for (int i = 0; i < 3; i++)
            {
                Tab t = (Tab)i;
                Button b = UiWinKit.ArtButton(root, "Tab" + names[i], "btn-navy", 34f + i * 120f, 56f, 112f, 32f, names[i], 15, Color.white, () => SetTab(t));
                tabButtons[i] = (Image)b.targetGraphic;
            }
            pullTab = UiKit.Rect(root, "PullTab", 30f, BodyY, W - 60f, H - BodyY - 22f);
            ratesTab = UiKit.Rect(root, "RatesTab", 30f, BodyY, W - 60f, H - BodyY - 22f);
            historyTab = UiKit.Rect(root, "HistoryTab", 30f, BodyY, W - 60f, H - BodyY - 22f);
            BuildPull();
            BuildRates();
            BuildHistory();
            root.gameObject.SetActive(false);
        }

        void BuildPull()
        {
            UiWinKit.Sliced(pullTab, "section", 0.3f);
            ticketText = UiKit.Label(pullTab, "Coins", 14f, 8f, 300f, 26f, "", 15, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft, true);
            UiWinKit.ArtButton(pullTab, "BuyCoin", "btn-navy", 318f, 6f, 100f, 30f, "Buy 1 coin", 12, Color.white, () => PetActions.Send(InvOp.GachaBuyCoins, 1));
            UiWinKit.ArtButton(pullTab, "BuyCoins", "btn-navy", 422f, 6f, 110f, 30f, "Buy 10 coins", 12, Color.white, () => PetActions.Send(InvOp.GachaBuyCoins, 10));
            pityText = UiKit.Label(pullTab, "Pity", 14f, 36f, W - 90f, 22f, "", 13, UiWinKit.Cream);
            UiWinKit.ArtButton(pullTab, "Pull1", "btn-gold", 14f, 64f, 170f, 40f, "Pull x1  (1 coin)", 15, new Color(0.25f, 0.12f, 0.02f), () => Pull(1));
            UiWinKit.ArtButton(pullTab, "Pull10", "btn-gold", 196f, 64f, 190f, 40f, "Pull x10  (10 coins)", 15, new Color(0.25f, 0.12f, 0.02f), () => Pull(10));
            urText = UiKit.Label(pullTab, "UrShards", 398f, 62f, 140f, 20f, "", 12, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            UiWinKit.ArtButton(pullTab, "Exchange", "btn-navy", 398f, 82f, 134f, 26f, "Shards -> Frostfang King", 10, Color.white, () => PetActions.Send(InvOp.GachaExchange, 0));
            resultTitle = UiKit.Label(pullTab, "ResultTitle", 14f, 112f, W - 90f, 22f, "", 14, Color.white, TextAnchor.MiddleLeft, true);
            for (int i = 0; i < resultFrames.Length; i++)
            {
                float x = 14f + (i % 5) * 104f, y = 138f + (i / 5) * 112f;
                RectTransform cell = UiKit.Rect(pullTab, "Result" + i, x, y, 96f, 106f);
                resultFrames[i] = UiWinKit.Sliced(cell, "slot", 96f / 160f);
                resultIcons[i] = UiKit.Icon(cell, "Icon", 18f, 8f, 60f);
                resultTexts[i] = UiKit.Label(cell, "Text", 2f, 66f, 92f, 38f, "", 11, Color.white, TextAnchor.MiddleCenter, true);
            }
        }

        void SetTab(Tab t)
        {
            tab = t;
            pullTab.gameObject.SetActive(t == Tab.Pull);
            ratesTab.gameObject.SetActive(t == Tab.Rates);
            historyTab.gameObject.SetActive(t == Tab.History);
            for (int i = 0; i < 3; i++) tabButtons[i].color = i == (int)t ? new Color(1f, 0.92f, 0.55f) : Color.white;
            shownKey = -1;
            Refresh();
        }

        void Pull(int count)
        {
            lastPullCount = count;
            PetActions.Send(InvOp.GachaPull, (byte)count);
            shownKey = -1;
        }

        void Update() { if (open) Refresh(); }

        void Refresh()
        {
            PlayerState p = PetActions.Me;
            if (p == null) { Close(); return; }
            AccountState a = p.Account ?? new AccountState();
            int key = a.tickets * 7919 + a.crystals * 97 + a.totalPulls * 31 + a.urShards * 7 + (int)tab + historyPage * 101 + a.pets.Count;
            if (key == shownKey) return;
            shownKey = key;
            if (tab == Tab.Pull) FillPull(a);
            else if (tab == Tab.History) FillHistory(a);
        }

        void FillPull(AccountState a)
        {
            ticketText.text = "Gacha Coins " + a.tickets + "   <size=12><color=#9fd0ff>Pet Crystals " + a.crystals + "  (" + PetRules.CrystalsPerPull + " = 1 coin)</color></size>";
            urText.text = "UR shards " + a.urShards + " / " + PetRules.UrExchangeCost;
            pityText.text = "Guaranteed: SSR+ in " + PetGacha.PullsToSsr(a) + ",  UR in " + PetGacha.PullsToUr(a) + " pulls   (total " + a.totalPulls + ")   " + PetGachaText.CrystalSource;
            int n = Mathf.Min(lastPullCount, a.history.Count);
            resultTitle.text = n > 0 ? "Last " + (n == 1 ? "pull" : n + " pulls") : "Pull to meet a new companion.";
            for (int i = 0; i < resultFrames.Length; i++)
            {
                bool has = i < n;
                resultFrames[i].gameObject.SetActive(has);
                if (!has) continue;
                PullRecord r = a.history[a.history.Count - n + i];
                PetDef d = PetConfig.Get(r.id);
                resultFrames[i].color = d != null ? Color.Lerp(Color.white, PetArt.LabelColor(d), 0.7f) : new Color(0.8f, 0.85f, 0.9f);
                resultIcons[i].sprite = d != null ? PetArt.Icon(d) : PetArt.ShardIcon();
                resultIcons[i].enabled = resultIcons[i].sprite != null;
                resultIcons[i].color = d != null ? PetArt.TintOf(d) : Color.white;
                resultTexts[i].text = d == null ? "<color=#bfe6ff>+" + r.shards + " shared\nshards</color>" : PetArt.Label(d) + " " + d.Name + "\n" + (r.dup ? "<color=#ffd27a>+" + r.shards + " shards</color>" : "<color=#9fff9f>NEW</color>");
            }
        }
    }
}

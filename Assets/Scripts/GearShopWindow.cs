using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// A village blacksmith's window: same win-panel look as PotionShopWindow, one row per (tier, slot) the seller stocks (GearShops),
    /// always the tier's basic +0 common piece. The hero is frozen while it is open (UiWindows.ModalOpen). Offline it pays at once; online it
    /// only asks, the server checks seller, reach and gold and answers with the new state (InvOp.GearBuy), so rows redraw from whatever it says.
    /// A piece below the hero's level still buys (EquipLevel is only checked when worn): the row just says what level it needs.
    /// </summary>
    public sealed class GearShopWindow : MonoBehaviour
    {
        const float W = 560f, RowH = 52f, RowsY = 140f, BottomPad = 22f, TabW = 96f;
        // One tier at a time (a tab per tier): two tiers of 6 slots ran off a 640-unit-tall screen. Up to 3 tabs (docs/world-map-2-design.md).
        const int MaxTiersShown = 3;
        static readonly int MaxRows = Slots.Generated.Length;
        static readonly Color CanBuy = Color.white, CannotBuy = new Color(1f, 1f, 1f, 0.45f);
        static GearShopWindow instance;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        sealed class Row { public RectTransform Root; public int Tier, SlotIndex, Entry; public Image Icon, Button; public Text Name, Stats, ReqLv, Price; }

        readonly Row[] rows = new Row[MaxRows];
        RectTransform root;
        Text title, greeting, goldText;
        string seller;
        bool open;
        int minTier, maxTier;
        readonly Button[] tabs = new Button[MaxTiersShown];
        readonly Text[] tabLabels = new Text[MaxTiersShown];
        int shownGold = -1;

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "GearShopWindow").gameObject.AddComponent<GearShopWindow>();
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void Open(string npcId, string npcName, string line)
        {
            if (instance == null || !GearShops.IsSeller(npcId)) return;
            instance.Fill(npcId, npcName, line);
            instance.open = true;
            instance.root.gameObject.SetActive(true);
            instance.Refresh();
        }

        public static void Close()
        {
            if (instance == null) return;
            instance.open = false;
            instance.root.gameObject.SetActive(false);
        }

        void Build()
        {
            root = UiWinKit.Anchored(transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 8f, W, 200f);
            UiWinKit.Sliced(root, "panel", W / 639f, true);
            title = UiKit.Label(root, "Title", 32f, 18f, 300f, 34f, "", 22, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleLeft, true);
            UiWinKit.CloseButton(root, W - 62f, 20f, 32f, Close);
            UiKit.Icon(root, "GoldIcon", W - 212f, 27f, 18f).sprite = LootSprites.Coin();
            goldText = UiKit.Label(root, "Gold", W - 190f, 20f, 120f, 32f, "", 16, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            UiKit.Panel(UiKit.Rect(root, "Rule", 30f, 56f, W - 60f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));
            greeting = UiKit.Label(root, "Greeting", 32f, 60f, W - 64f, 40f, "", 13, UiWinKit.Cream, TextAnchor.MiddleLeft);
            for (int t = 0; t < MaxTiersShown; t++)
            {
                int k = t;
                tabs[t] = UiWinKit.ArtButton(root, "Tab" + t, "btn-navy", 30f + t * (TabW + 8f), 102f, TabW, 30f, "", 14, Color.white, () => ShowTier(minTier + k));
                tabLabels[t] = tabs[t].GetComponentInChildren<Text>();
            }
            for (int i = 0; i < MaxRows; i++) rows[i] = BuildRow(i);
            root.gameObject.SetActive(false);
        }

        Row BuildRow(int index)
        {
            var row = new Row();
            row.Root = UiKit.Rect(root, "Row" + index, 30f, RowsY + index * RowH, W - 60f, RowH - 4f);
            UiWinKit.Sliced(row.Root, "section", 0.3f);
            RectTransform slot = UiKit.Rect(row.Root, "Slot", 4f, 2f, 44f, 44f);
            UiWinKit.Sliced(slot, "slot", 44f / 160f);
            row.Icon = UiKit.Icon(slot, "Icon", 5f, 5f, 34f);
            row.Name = UiKit.Label(row.Root, "Name", 54f, 3f, 180f, 22f, "", 14, Color.white, TextAnchor.MiddleLeft, true);
            row.Stats = UiKit.Label(row.Root, "Stats", 54f, 24f, 180f, 20f, "", 12, UiWinKit.Dim);
            row.ReqLv = UiKit.Label(row.Root, "ReqLv", 240f, 3f, 76f, 42f, "", 12, UiWinKit.Cream, TextAnchor.MiddleCenter);
            row.Price = UiKit.Label(row.Root, "Price", 320f, 3f, 68f, 42f, "", 14, UiWinKit.Gold, TextAnchor.MiddleRight, true);
            Button buy = UiWinKit.ArtButton(row.Root, "Buy", "btn-gold", W - 60f - 78f, 8f, 70f, 32f, "Buy", 15, new Color(0.25f, 0.12f, 0.02f), () => Buy(row));
            row.Button = (Image)buy.targetGraphic;
            return row;
        }

        void Fill(string npcId, string npcName, string line)
        {
            seller = npcId;
            title.text = npcName + "'s Gear";
            greeting.text = line;
            GearShops.TierRange(npcId, out minTier, out maxTier);
            maxTier = Mathf.Min(maxTier, minTier + MaxTiersShown - 1);
            for (int t = 0; t < MaxTiersShown; t++)
            {
                bool on = minTier + t <= maxTier;
                tabs[t].gameObject.SetActive(on);
                if (on) tabLabels[t].text = "Tier " + (minTier + t);
            }
            PlayerState p = RpgSession.Player;
            int level = p != null ? p.Level : 1;
            int best = minTier;                                                            // open on the best tier the hero can already wield
            for (int tier = minTier; tier <= maxTier; tier++) if (level >= EquipLevel.ByTier[Mathf.Clamp(tier, 0, EquipLevel.ByTier.Length - 1)]) best = tier;
            ShowTier(best);
        }

        void ShowTier(int tier)
        {
            for (int t = 0; t < MaxTiersShown; t++) tabs[t].targetGraphic.color = minTier + t == tier ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            PlayerState p = RpgSession.Player;
            string classId = p != null ? p.ClassId : RpgClasses.Default;
            int n = 0;
                for (int slotIndex = 0; slotIndex < Slots.Generated.Length && n < MaxRows; slotIndex++)
                {
                    Row row = rows[n++];
                    row.Tier = tier; row.SlotIndex = slotIndex; row.Entry = GearShops.Encode(tier, slotIndex);
                    Item preview = GearCatalog.Preview(tier, slotIndex, classId);
                    row.Icon.sprite = LootSprites.Icon(preview);
                    row.Icon.enabled = row.Icon.sprite != null;
                    row.Name.text = preview.name;
                    row.Stats.text = string.Join(", ", ItemInfo.Lines(preview).Select(ItemInfo.Format));
                    string reqLabel = EquipLevel.Label(preview);
                    row.ReqLv.text = reqLabel == "" ? "No req." : reqLabel;
                    row.Price.text = GearCatalog.Price(tier, slotIndex) + " G";
                    row.Root.gameObject.SetActive(true);
                }
            for (int i = n; i < MaxRows; i++) rows[i].Root.gameObject.SetActive(false);
            for (int i = 0; i < n; i++) rows[i].Root.anchoredPosition = new Vector2(30f, -(RowsY + i * RowH));
            root.sizeDelta = new Vector2(W, RowsY + n * RowH + BottomPad);
            shownGold = -1;
        }

        void Buy(Row row)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            byte entry = (byte)row.Entry;
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.GearBuy, seller, entry); return; } // the answer's RpgState and text come back
            ActionResult r = p.BuyGear(row.Tier, row.SlotIndex);
            RpgHud.Toast(r.Text, r.Ok ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.7f, 0.5f));
            Refresh();
        }

        void Update() { if (open) Refresh(); }

        void Refresh()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) { Close(); return; }
            if (p.Gold != shownGold) { shownGold = p.Gold; goldText.text = p.Gold + " G"; }
            foreach (Row row in rows)
            {
                if (!row.Root.gameObject.activeSelf) continue;
                int price = GearCatalog.Price(row.Tier, row.SlotIndex);
                row.Button.color = p.Gold >= price ? CanBuy : CannotBuy; // still tappable: the refusal says why
            }
        }
    }
}

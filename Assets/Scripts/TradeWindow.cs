using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The trade window, open exactly while the server says a trade is: both offers, your bag, the gold keypad and the two-step Lock / Confirm. It only ever asks: everything drawn comes from
    /// the server's TradeStateMsg and the mirrored bag, and a press sends a uid or an amount. Tap a bag item to offer it, an offered item to take it back, the other side's item to inspect it.
    /// Layout in the 960 x 640 reference, narrowed as a whole on a screen narrower than that. Counts as modal (hero frozen, touch controls hidden). Online only: PartyPanel.Live.
    /// </summary>
    public sealed class TradeWindow : MonoBehaviour
    {
        const float FrameX = 34f, FrameY = 10f, FrameW = 892f, FrameH = 620f;
        const float PanelY = 72f, PanelH = 470f, MineX = 60f, TheirsX = 330f, OfferW = 258f, BagX = 600f, BagW = 300f, SlotSize = 96f, SlotGap = 8f, BagSlot = 82f, BagGap = 6f;
        const float ButtonY = 550f, ButtonH = 58f, FieldH = 44f, BannerSeconds = 3.5f;
        const int BagCols = 3, BagRows = 4, BagPage = BagCols * BagRows;
        static readonly Color DarkText = new Color(0.25f, 0.12f, 0.02f), LockedText = new Color(1f, 0.87f, 0.45f), ConfirmedText = new Color(0.56f, 1f, 0.56f), DimButtonText = new Color(0.7f, 0.72f, 0.75f);
        static TradeWindow instance;

        /// <summary>One item cell: frame, icon, +level in the corner, name along the bottom, the locked veil on top.</summary>
        sealed class Cell
        {
            public RectTransform Root;
            public Image Icon, Veil;
            public Text Plus, Name;
            public string Uid;
            public Item Item;
        }

        /// <summary>One side's panel.</summary>
        sealed class Offer
        {
            public RectTransform Root;
            public Image Frame, ClassIcon, Lock, Check;
            public Text Who, Status, Gold;
            public readonly Cell[] Cells = new Cell[TradeBook.MaxItems];
        }

        readonly Cell[] bagCells = new Cell[BagPage];
        readonly List<Item> bagItems = new List<Item>(PlayerState.InventorySize);
        RectTransform board, bannerRoot;
        Offer mine, theirs;
        Image lockImage, confirmImage;
        Text lockLabel, confirmLabel, bagGold, bannerText;
        CanvasGroup bannerGroup;
        Text inspectHint;
        TradeGoldPad pad;
        TradeInspect inspect;
        int page, shownVersion = -1, shownWarnings, sentAtVersion = -1;
        PlayerState shownPlayer;
        float bannerUntil;
        bool open;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "TradeWindow").gameObject.AddComponent<TradeWindow>(); // full-screen layer over everything
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>The window covers the toast strip, so a refusal shows here while it is open.</summary>
        public static void Flash(string text) { if (instance != null) instance.ShowBanner(text); }

        /// <summary>Esc: closes a popup first, then cancels the trade.</summary>
        public static void CloseKey()
        {
            if (!IsOpen) return;
            if (instance.pad.IsOpen) instance.pad.Close();
            else if (instance.inspect.IsOpen) instance.inspect.Close();
            else NetClient.SendTrade(TradeOp.Cancel);
        }

        // ---- build ----

        void Build()
        {
            UiKit.Panel(GetComponent<RectTransform>(), new Color(0f, 0f, 0f, 0.55f), true); // swallows taps so they never reach the world
            board = UiWinKit.Anchored(transform, "Board", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, UiKit.RefWidth, UiKit.RefHeight);
            PartyArt.Sliced(UiKit.Rect(board, "Frame", FrameX, FrameY, FrameW, FrameH), "invite-frame", PartyArt.InviteFrameScale, true);
            RectTransform plaque = UiKit.Rect(board, "Plaque", (UiKit.RefWidth - 280f) * 0.5f, 22f, 280f, 46f);
            TradeArt.ByHeight(plaque, "title-plaque", 46f);
            UiKit.Label(UiKit.Fill(plaque, "Title"), "Trade", 24, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleCenter, true);
            UiWinKit.CloseButton(board, 854f, 22f, 46f, () => NetClient.SendTrade(TradeOp.Cancel));

            mine = BuildOffer("MyOffer", MineX, OnMyCell);
            theirs = BuildOffer("TheirOffer", TheirsX, OnTheirCell);
            UiWinKit.MakeButton(mine.Root.Find("GoldField").GetComponent<Image>(), OpenPad);
            inspectHint = UiKit.Label(theirs.Root, "Hint", 0f, 445f, OfferW, 16f, "", 11, UiWinKit.Dim, TextAnchor.MiddleCenter);
            BuildBag();

            Button lockButton = PartyArt.ArtButton(board, "Lock", "btn-accept", 60f, ButtonY, 270f, ButtonH, "Lock Offer", 22, DarkText, PressLock);
            lockImage = (Image)lockButton.targetGraphic;
            lockLabel = lockButton.GetComponentInChildren<Text>();
            RectTransform confirmRt = UiKit.Rect(board, "Confirm", 350f, ButtonY, 290f, ButtonH);
            confirmImage = TradeArt.ByHeight(confirmRt, "btn-disabled", ButtonH);
            confirmLabel = UiKit.Label(UiKit.Fill(confirmRt, "Label"), "Confirm Trade", 22, DimButtonText, TextAnchor.MiddleCenter, true);
            UiWinKit.MakeButton(confirmImage, PressConfirm);
            PartyArt.ArtButton(board, "Cancel", "btn-cancel", 660f, ButtonY, 240f, ButtonH, "Cancel", 22, Color.white, () => NetClient.SendTrade(TradeOp.Cancel));

            pad = new TradeGoldPad(board);
            inspect = new TradeInspect(board);
            BuildBanner();
            SetShown(false);
        }

        Offer BuildOffer(string name, float x, System.Action<Cell> onTap)
        {
            var o = new Offer { Root = UiKit.Rect(board, name, x, PanelY, OfferW, PanelH) };
            o.Frame = TradeArt.Sliced(o.Root, "offer-frame", TradeArt.OfferFrameScale);
            o.ClassIcon = PartyArt.Plain(UiKit.Rect(o.Root, "Class", 14f, 12f, 40f, 40f), "class-warrior");
            o.Who = UiKit.Label(o.Root, "Who", 60f, 12f, 120f, 40f, "", 16, new Color(1f, 0.87f, 0.6f), TextAnchor.MiddleLeft, true);
            o.Who.verticalOverflow = VerticalWrapMode.Truncate;
            o.Who.resizeTextForBestFit = true; o.Who.resizeTextMinSize = 10; o.Who.resizeTextMaxSize = 16; // sixteen characters must fit before the lock
            o.Lock = TradeArt.Plain(UiKit.Rect(o.Root, "LockIcon", 182f, 17f, 30f, 30f), "lock");
            o.Check = TradeArt.Plain(UiKit.Rect(o.Root, "CheckIcon", 213f, 17f, 30f, 30f), "check");
            float left = (OfferW - SlotSize * 2f - SlotGap) * 0.5f;
            for (int i = 0; i < o.Cells.Length; i++)
                o.Cells[i] = BuildCell(o.Root, "Slot" + i, left + (i % 2) * (SlotSize + SlotGap), 60f + (i / 2) * (SlotSize + SlotGap), SlotSize, onTap);
            RectTransform field = UiKit.Rect(o.Root, "GoldField", 16f, 380f, 226f, FieldH);
            TradeArt.ByHeight(field, "gold-field", FieldH);
            TradeArt.Plain(UiKit.Rect(field, "Coin", -2f, 5f, 34f, 34f), "coin");
            o.Gold = UiKit.Label(field, "Number", 36f, 0f, 226f - 36f - 18f, FieldH, "0", 18, Color.white, TextAnchor.MiddleRight, true);
            o.Status = UiKit.Label(o.Root, "Status", 0f, 427f, OfferW, 18f, "", 14, UiWinKit.Dim, TextAnchor.MiddleCenter, true);
            return o;
        }

        Cell BuildCell(RectTransform parent, string name, float x, float y, float size, System.Action<Cell> onTap)
        {
            var c = new Cell { Root = UiKit.Rect(parent, name, x, y, size, size) };
            Image frame = UiWinKit.Sliced(c.Root, "slot", size / TradeArt.SlotNativeSize, true);
            UiWinKit.MakeButton(frame, () => onTap(c));
            float iconSize = size * 0.68f, labels = size >= SlotSize ? 22f : 0f;
            c.Icon = UiKit.Icon(c.Root, "Icon", (size - iconSize) * 0.5f, (size - iconSize - labels * 0.6f) * 0.5f, iconSize);
            c.Plus = UiKit.Label(c.Root, "Plus", 4f, 9f, size - 15f, 18f, "", 14, new Color(1f, 0.87f, 0.4f), TextAnchor.UpperRight, true); // inside the slot's rim, which is about 10 units wide
            if (labels > 0f)
            {
                c.Name = UiKit.Label(c.Root, "Name", 9f, size - 38f, size - 18f, 28f, "", 11, Color.white, TextAnchor.LowerCenter, true); // room for two lines above the rim
                c.Name.verticalOverflow = VerticalWrapMode.Truncate;
                c.Name.resizeTextForBestFit = true; c.Name.resizeTextMinSize = 9; c.Name.resizeTextMaxSize = 11;
                c.Name.lineSpacing = 0.85f;
            }
            c.Veil = TradeArt.Sliced(UiKit.Fill(c.Root, "Veil"), "slot-locked", size / TradeArt.SlotNativeSize);
            return c;
        }

        void BuildBag()
        {
            RectTransform bag = UiKit.Rect(board, "Bag", BagX, PanelY, BagW, PanelH);
            TradeArt.Sliced(bag, "offer-frame", TradeArt.OfferFrameScale);
            UiKit.Label(bag, "Title", 0f, 14f, BagW, 30f, "Your Bag", 18, new Color(1f, 0.87f, 0.6f), TextAnchor.MiddleCenter, true);
            float left = (BagW - BagSlot * BagCols - BagGap * (BagCols - 1)) * 0.5f;
            for (int i = 0; i < BagPage; i++)
                bagCells[i] = BuildCell(bag, "Slot" + i, left + (i % BagCols) * (BagSlot + BagGap), 50f + (i / BagCols) * (BagSlot + BagGap), BagSlot, OnBagCell);
            RectTransform back = UiKit.Rect(bag, "Prev", 16f, 404f, FieldH, FieldH);
            Image backImg = UiWinKit.Plain(back, "next-arrow");
            back.localScale = new Vector3(-1f, 1f, 1f); back.pivot = new Vector2(0.5f, 0.5f); back.anchoredPosition = new Vector2(16f + FieldH * 0.5f, -(404f + FieldH * 0.5f));
            UiWinKit.MakeButton(backImg, () => Turn(-1));
            RectTransform field = UiKit.Rect(bag, "GoldField", 66f, 404f, 168f, FieldH);
            TradeArt.ByHeight(field, "gold-field", FieldH);
            TradeArt.Plain(UiKit.Rect(field, "Coin", -2f, 5f, 34f, 34f), "coin");
            bagGold = UiKit.Label(field, "Number", 34f, 0f, 168f - 34f - 16f, FieldH, "0", 18, Color.white, TextAnchor.MiddleRight, true);
            UiWinKit.MakeButton(UiWinKit.Plain(UiKit.Rect(bag, "Next", 240f, 404f, FieldH, FieldH), "next-arrow"), () => Turn(1));
        }

        void BuildBanner()
        {
            bannerRoot = UiKit.Rect(board, "Banner", 270f, 275f, 420f, 84f);
            Sprite toast = UiArt.Win("toast");
            UiWinKit.Sliced(bannerRoot, "toast", 84f / (toast != null ? toast.rect.height : 84f));
            bannerGroup = bannerRoot.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false; bannerGroup.interactable = false;
            TradeArt.Plain(UiKit.Rect(bannerRoot, "Warning", 26f, 22f, 40f, 40f), "warning");
            bannerText = UiKit.Label(bannerRoot, "Text", 78f, 8f, 420f - 78f - 26f, 68f, "", 15, UiWinKit.Cream, TextAnchor.MiddleLeft, true);
            bannerRoot.gameObject.SetActive(false);
        }

        void SetShown(bool on)
        {
            open = on;
            board.gameObject.SetActive(on);
            GetComponent<Image>().enabled = on;
            if (!on) { pad.Close(); inspect.Close(); bannerRoot.gameObject.SetActive(false); }
        }

        // ---- presses ----

        void Send(TradeOp op, string uid = null, int gold = 0)
        {
            TradeStateMsg? s = NetClient.Trade.Session;
            if (s.HasValue) NetClient.SendTrade(new TradeRequestMsg { Op = op, Uid = uid, Gold = gold, Revision = s.Value.Revision });
        }

        void PressLock()
        {
            TradeStateMsg? s = NetClient.Trade.Session;
            if (!s.HasValue || sentAtVersion == NetClient.Trade.Version) return; // already asked, and nothing has changed since
            sentAtVersion = NetClient.Trade.Version;
            Send(s.Value.Mine.Locked ? TradeOp.Unlock : TradeOp.Lock);
        }

        void PressConfirm()
        {
            TradeStateMsg? s = NetClient.Trade.Session;
            if (!s.HasValue || !s.Value.Mine.Locked || !s.Value.Theirs.Locked || s.Value.Mine.Confirmed || sentAtVersion == NetClient.Trade.Version) return;
            sentAtVersion = NetClient.Trade.Version;
            Send(TradeOp.Confirm);
        }

        bool MyOfferLocked()
        {
            TradeStateMsg? s = NetClient.Trade.Session;
            if (!s.HasValue || !s.Value.Mine.Locked) return false;
            ShowBanner("Unlock your offer to change it");
            return true;
        }

        void OnMyCell(Cell c) { if (c.Uid != null && !MyOfferLocked()) Send(TradeOp.RemoveItem, c.Uid); }

        void OnBagCell(Cell c) { if (c.Uid != null && !MyOfferLocked()) Send(TradeOp.AddItem, c.Uid); }

        /// <summary>Capture runs: opens the look at the other side's first item, as a tap on it does.</summary>
        public static void TestInspectFirst() { if (instance != null) instance.OnTheirCell(instance.theirs.Cells[0]); }

        void OnTheirCell(Cell c) { if (c.Item != null) inspect.Open(c.Item, RpgSession.Player); }

        void OpenPad()
        {
            TradeStateMsg? s = NetClient.Trade.Session;
            PlayerState p = RpgSession.Player;
            if (!s.HasValue || p == null || MyOfferLocked()) return;
            pad.Open(s.Value.Mine.Gold, p.Gold, gold => Send(TradeOp.SetGold, null, gold));
        }

        void Turn(int by)
        {
            int pages = Mathf.Max(1, (bagItems.Count + BagPage - 1) / BagPage);
            page = (page + by + pages) % pages;
            FillBag();
        }

        // ---- frame ----

        void Update()
        {
            TradeClientState trade = NetClient.Trade;
            bool show = PartyPanel.Live && trade.Session.HasValue;
            if (show != open)
            {
                SetShown(show);
                if (show)
                {
                    InventoryPanel.Close(); PotionShopWindow.Close(); SkillTreeWindow.Close(); NpcDialogueBox.Close(); QuestOfferWindow.Close(); QuestLogWindow.Close();
                    transform.SetAsLastSibling();
                    page = 0; shownVersion = -1; shownWarnings = trade.WarningCount; sentAtVersion = -1;
                }
            }
            if (!show) return;
            float canvasW = ((RectTransform)transform).rect.width;
            float fit = canvasW > 0f ? Mathf.Min(1f, canvasW / UiKit.RefWidth) : 1f;
            board.localScale = new Vector3(fit, fit, 1f);
            PlayerState player = RpgSession.Player;
            if (trade.Version != shownVersion || player != shownPlayer)
            {
                shownVersion = trade.Version; shownPlayer = player;
                Refresh(trade.Session.Value, player);
            }
            if (trade.WarningCount != shownWarnings)
            {
                shownWarnings = trade.WarningCount;
                string line = TradeText.Banner(trade.LastChange, trade.Session.Value.Name);
                int colon = line.IndexOf(": ");
                ShowBanner(colon < 0 ? line : line.Substring(0, colon + 1) + "\n<color=#ffd75c>" + line.Substring(colon + 2) + "</color>");
            }
            if (bannerRoot.gameObject.activeSelf)
            {
                float left = bannerUntil - Time.unscaledTime;
                bannerGroup.alpha = Mathf.Clamp01(left / 0.4f);
                if (left <= 0f) bannerRoot.gameObject.SetActive(false);
            }
        }

        void ShowBanner(string text)
        {
            if (!open) return;
            bannerText.text = text;
            bannerUntil = Time.unscaledTime + BannerSeconds;
            bannerGroup.alpha = 1f;
            bannerRoot.gameObject.SetActive(true);
            bannerRoot.SetAsLastSibling();
        }

        // ---- drawing ----

        void Refresh(TradeStateMsg s, PlayerState player)
        {
            HeroController hero = GameSession.Hero;
            FillOffer(mine, s.Mine, "Your Offer", hero != null ? PartyArt.ClassIcon((int)hero.Class.Id) : null);
            FillOffer(theirs, s.Theirs, s.Name, PartyArt.ClassIcon(s.HeroClass));
            inspectHint.text = s.Theirs.Items.Length > 0 ? "Tap an item to inspect it" : "";

            bool both = s.Mine.Locked && s.Theirs.Locked;
            TradeArt.SetButton(lockImage, PartyArt.Get(s.Mine.Locked ? "btn-decline" : "btn-accept"), ButtonH);
            lockLabel.text = s.Mine.Locked ? "Unlock" : "Lock Offer";
            lockLabel.color = s.Mine.Locked ? UiWinKit.Cream : DarkText;
            bool canConfirm = both && !s.Mine.Confirmed;
            TradeArt.SetButton(confirmImage, TradeArt.Get(canConfirm ? "btn-confirm" : "btn-disabled"), ButtonH);
            confirmLabel.text = s.Mine.Confirmed ? "Waiting..." : "Confirm Trade";
            confirmLabel.color = canConfirm ? Color.white : DimButtonText;

            bagItems.Clear();
            if (player != null)
                foreach (Item item in player.Inventory)
                    if (!Offered(s.Mine, item.uid)) bagItems.Add(item);
            bagGold.text = Money(player != null ? player.Gold : 0);
            int pages = Mathf.Max(1, (bagItems.Count + BagPage - 1) / BagPage);
            if (page >= pages) page = pages - 1;
            FillBag();
        }

        static bool Offered(TradeOfferView offer, string uid)
        {
            foreach (TradeItemView v in offer.Items) if (v.Uid == uid) return true;
            return false;
        }

        static string Money(int n) { return n.ToString("N0", CultureInfo.InvariantCulture); }

        void FillOffer(Offer o, TradeOfferView view, string who, Sprite classIcon)
        {
            o.Frame.sprite = TradeArt.Get(view.Locked ? "offer-frame-locked" : "offer-frame");
            o.ClassIcon.sprite = classIcon;
            o.ClassIcon.enabled = classIcon != null;
            o.Who.text = who;
            o.Lock.enabled = view.Locked;
            o.Check.enabled = view.Confirmed;
            o.Gold.text = Money(view.Gold);
            o.Status.text = view.Confirmed ? "Confirmed" : view.Locked ? "Locked" : "Not locked";
            o.Status.color = view.Confirmed ? ConfirmedText : view.Locked ? LockedText : UiWinKit.Dim;
            for (int i = 0; i < o.Cells.Length; i++)
            {
                Cell c = o.Cells[i];
                Item item = i < view.Items.Length ? ItemOf(view.Items[i]) : null;
                FillCell(c, item);
                c.Veil.enabled = view.Locked;
            }
        }

        void FillBag()
        {
            for (int i = 0; i < bagCells.Length; i++)
            {
                int at = page * BagPage + i;
                FillCell(bagCells[i], at < bagItems.Count ? bagItems[at] : null);
                bagCells[i].Veil.enabled = false;
            }
        }

        static void FillCell(Cell c, Item item)
        {
            c.Item = item;
            c.Uid = item != null ? item.uid : null;
            c.Icon.sprite = item != null ? LootSprites.Icon(item) : null;
            c.Icon.enabled = c.Icon.sprite != null;
            ItemEnhanceSlotFx.Show(c.Icon, item);
            int plus = item != null ? EnhanceStats.Level(item) : 0;
            c.Plus.text = item != null && item.slot == ZenoniaProto.Rpg.Slots.Pet ? ItemInfo.DurabilityPercent(item) : plus > 0 ? "+" + plus : "";
            if (c.Name != null)
            {
                c.Name.text = item != null ? item.name : "";
                c.Name.color = item != null ? LootSprites.RarityColor(item) : Color.white;
            }
        }

        /// <summary>An item to draw from what the server said it is.</summary>
        public static Item ItemOf(TradeItemView v)
        {
            return new Item
            {
                uid = v.Uid, templateId = v.TemplateId, slot = v.Slot, name = v.Name, rarity = v.Rarity, classId = string.IsNullOrEmpty(v.ClassId) ? null : v.ClassId, itemLevel = v.ItemLevel, enhance = v.Enhance, durability = v.Durability,
                stats = new ItemStats { atk = v.Atk, def = v.Def, maxHp = v.MaxHp, maxMp = v.MaxMp, critChance = v.CritChance, speedMult = v.SpeedMult },
            };
        }
    }
}

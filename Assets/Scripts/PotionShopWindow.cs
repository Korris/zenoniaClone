using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// A village potion seller's window (win-panel): the seller's name and greeting, the buyer's gold, one row per potion on sale (icon, name, effect, owned, price, Buy).
    /// The hero is frozen while it is open (UiWindows.ModalOpen). Offline it pays at once; online it only asks, the server checks seller, reach and gold and answers with the
    /// new state, so the rows redraw from whatever the state says.
    /// </summary>
    public sealed class PotionShopWindow : MonoBehaviour
    {
        const float W = 500f, RowH = 52f, RowsY = 104f, BottomPad = 22f;
        const int MaxRows = PotionCatalog.Sizes * 2;
        static readonly Color CanBuy = Color.white, CannotBuy = new Color(1f, 1f, 1f, 0.45f);
        static PotionShopWindow instance;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        sealed class Row { public RectTransform Root; public bool Hp; public int Size; public Image Icon, Button; public Text Name, Effect, Owned, Price; }

        readonly Row[] rows = new Row[MaxRows];
        RectTransform root;
        Text title, greeting, goldText;
        string seller;
        bool open;
        int shownGold = -1;

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "PotionShopWindow").gameObject.AddComponent<PotionShopWindow>(); // full-screen layer: children anchor to the real screen
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void Open(string npcId, string npcName, string line)
        {
            if (instance == null || !PotionShops.IsSeller(npcId)) return;
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
            UiWinKit.Sliced(root, "panel", W / 639f, true); // swallows taps so they never reach the world
            title = UiKit.Label(root, "Title", 32f, 18f, 280f, 34f, "", 22, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleLeft, true);
            UiWinKit.CloseButton(root, W - 62f, 20f, 32f, Close);
            UiKit.Icon(root, "GoldIcon", W - 212f, 27f, 18f).sprite = LootSprites.Coin();
            goldText = UiKit.Label(root, "Gold", W - 190f, 20f, 120f, 32f, "", 16, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            UiKit.Panel(UiKit.Rect(root, "Rule", 30f, 56f, W - 60f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));
            greeting = UiKit.Label(root, "Greeting", 32f, 60f, W - 64f, 40f, "", 13, UiWinKit.Cream, TextAnchor.MiddleLeft);
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
            row.Name = UiKit.Label(row.Root, "Name", 54f, 3f, 170f, 22f, "", 14, Color.white, TextAnchor.MiddleLeft, true);
            row.Effect = UiKit.Label(row.Root, "Effect", 54f, 24f, 170f, 20f, "", 12, UiWinKit.Dim);
            row.Owned = UiKit.Label(row.Root, "Owned", 222f, 3f, 66f, 42f, "", 13, UiWinKit.Cream, TextAnchor.MiddleCenter);
            row.Price = UiKit.Label(row.Root, "Price", 286f, 3f, 62f, 42f, "", 14, UiWinKit.Gold, TextAnchor.MiddleRight, true);
            Button buy = UiWinKit.ArtButton(row.Root, "Buy", "btn-gold", W - 60f - 78f, 8f, 70f, 32f, "Buy", 15, new Color(0.25f, 0.12f, 0.02f), () => Buy(row));
            row.Button = (Image)buy.targetGraphic;
            return row;
        }

        void Fill(string npcId, string npcName, string line)
        {
            seller = npcId;
            title.text = npcName + "'s Potions";
            greeting.text = line;
            int n = 0;
            for (int hpSide = 0; hpSide < 2; hpSide++)
                for (int size = 0; size < PotionCatalog.Sizes; size++)
                {
                    Row row = rows[n++];
                    bool sold = PotionShops.Sells(npcId, size);
                    row.Root.gameObject.SetActive(false);
                    row.Hp = hpSide == 0; row.Size = size;
                    if (sold) row.Root.gameObject.SetActive(true);
                }
            int shown = 0;
            foreach (Row row in rows)
            {
                if (!row.Root.gameObject.activeSelf) continue;
                row.Root.anchoredPosition = new Vector2(30f, -(RowsY + shown * RowH));
                row.Icon.sprite = LootSprites.Potion(row.Hp, row.Size);
                row.Name.text = PotionCatalog.Name(row.Hp, row.Size);
                row.Effect.text = PotionCatalog.Effect(row.Hp, row.Size);
                row.Price.text = PotionCatalog.Price(row.Size) + " G";
                shown++;
            }
            root.sizeDelta = new Vector2(W, RowsY + shown * RowH + BottomPad);
            shownGold = -1;
        }

        void Buy(Row row)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.PotionBuy, seller, PotionCatalog.PackSlot(row.Hp, row.Size)); return; } // the answer's RpgState and text come back
            ActionResult r = p.BuyPotion(row.Hp, row.Size);
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
                row.Owned.text = "Owned " + p.PotionCount(row.Hp, row.Size);
                row.Button.color = p.Gold >= PotionCatalog.Price(row.Size) ? CanBuy : CannotBuy; // still tappable: the refusal says why
            }
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The bag window, MU Online style: worn gear and the inventory on one screen, in the gold-and-navy frame of the quest windows.
    /// Left: the hero between two rows of worn slots, Stats behind a button. Centre: 5 x 4 paged grid. Right: the selected item, compared with what is worn, with Equip / Unequip and Enhance.
    /// Tapping a worn slot filters the grid to what fits it; the chip clears the filter. A second tap within 0.35 s activates; arrows move the selection and J / Space activates.
    /// B / I toggles (RpgSession), Esc closes; the game is paused while it is open.
    /// Partial: InventoryPanel.Loadout.cs (left), InventoryPanel.Bag.cs (centre), InventoryPanel.Detail.cs (right). Mock: ArtLibrary/custom/inventory-ui-ref/bag-layout-mock-1.png.
    /// </summary>
    public partial class InventoryPanel : MonoBehaviour
    {
        const float W = 956f, H = 556f, PanelY = 70f, PanelH = 460f;
        const float LeftW = 336f, CentreW = 350f, RightW = 210f, Gap = 6f; // the hero panel is wide enough for the Warrior to stand in its middle with his sword beside him
        const float DoubleClickSeconds = 0.35f, MessageSeconds = 1.5f;
        const int WornClickBase = 1000; // keeps worn slots and grid cells apart in the double-tap check
        static InventoryPanel instance;

        RectTransform overlay;
        Text messageText;
        float messageUntil;
        float lastClickTime;
        int lastClickIndex = -1;
        bool confirmDiscard;

        public static bool IsOpen { get { return instance != null && instance.overlay.gameObject.activeSelf; } }

        /// <summary>Online: the server pushed a new state, redraw the open bag.</summary>
        public static void RefreshIfOpen() { if (IsOpen) instance.Refresh(); }

        public static void Toggle(Transform root) { if (IsOpen) Close(); else Open(root); }

        public static void Open(Transform root)
        {
            if (RpgSession.Player == null) return;
            if (instance == null)
            {
                var go = new GameObject("InventoryPanel");
                go.transform.SetParent(root, false);
                instance = go.AddComponent<InventoryPanel>();
                instance.Build(UiKit.Canvas(root).transform);
            }
            instance.overlay.gameObject.SetActive(true);
            instance.overlay.SetAsLastSibling();
            instance.messageUntil = 0f;
            instance.Refresh();
            GameClock.Paused = NetMode.UsesLocalTimeBending; // a shared zone cannot freeze for one player
        }

        public static void Close()
        {
            if (instance == null) return;
            instance.overlay.gameObject.SetActive(false);
            instance.confirmDiscard = false;
            instance.heroView.Stop();
            GameClock.Paused = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            GameClock.Paused = false;
        }

        // ---- build ----

        void Build(Transform canvas)
        {
            overlay = UiKit.Fill(canvas, "BagOverlay");
            UiKit.Panel(overlay, new Color(0f, 0f, 0f, 0.5f), true); // also stops taps reaching the game
            RectTransform win = UiWinKit.Anchored(overlay, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, W, H);
            UiWinKit.Sliced(win, "panel", 0.9f, true);

            Image bagIcon = UiKit.Icon(win, "BagIcon", 26f, 15f, 40f);
            bagIcon.sprite = BagSprites.BagIcon();
            UiKit.Label(win, "Title", 72f, 14f, 80f, 42f, "Bag", 24, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleLeft, true);
            BuildTab(win, 150f, "tab-active", "Bag", null);
            BuildTab(win, 286f, "tab-inactive", "Skill", () => { Close(); SkillTreeWindow.Open(transform.parent); });
            UiWinKit.CloseButton(win, W - 62f, 14f, 42f, Close);

            BuildLoadout(Section(win, "Left", 24f, LeftW));
            BuildBag(Section(win, "Centre", 24f + LeftW + Gap, CentreW));
            BuildDetail(Section(win, "Right", 24f + LeftW + Gap + CentreW + Gap, RightW));
            overlay.gameObject.SetActive(false);
        }

        static void BuildTab(RectTransform win, float x, string piece, string text, System.Action onClick)
        {
            RectTransform rt = UiKit.Rect(win, "Tab" + text, x, 16f, 128f, 40f);
            Image img = UiWinKit.Sliced(rt, piece, 0f);
            UiKit.Label(UiKit.Fill(rt, "Label"), text, 16, UiWinKit.Cream, TextAnchor.MiddleCenter, true);
            if (onClick != null) UiWinKit.MakeButton(img, onClick);
        }

        static RectTransform Section(RectTransform win, string name, float x, float w)
        {
            RectTransform rt = UiKit.Rect(win, name, x, PanelY, w, PanelH);
            UiWinKit.Sliced(rt, "panel", 0.18f); // thin line and small corners: only the outer frame is heavy
            return rt;
        }

        // ---- input ----

        void Update()
        {
            if (!IsOpen) return;
            if (messageText.text.Length > 0 && Time.unscaledTime > messageUntil) messageText.text = "";
            heroView.Tick(Time.unscaledDeltaTime);
            int dx = (InputAdapter.Pressed(GameKey.Right) ? 1 : 0) - (InputAdapter.Pressed(GameKey.Left) ? 1 : 0);
            int dy = (InputAdapter.Pressed(GameKey.Down) ? 1 : 0) - (InputAdapter.Pressed(GameKey.Up) ? 1 : 0);
            if (dx != 0 || dy != 0) { Navigate(dx, dy); Refresh(); }
            if (InputAdapter.Pressed(GameKey.Attack)) Activate();
        }

        /// <summary>Arrows: worn slots cycle, Right leaves them for the grid; in the grid, Left from the first column goes back to them.</summary>
        void Navigate(int dx, int dy)
        {
            confirmDiscard = false;
            PlayerState p = RpgSession.Player;
            if (wornSel >= 0)
            {
                if (dx > 0) { wornSel = -1; cellSel = page * PageSize; return; }
                wornSel = (wornSel + dy + dx + Slots.All.Length) % Slots.All.Length;
                return;
            }
            if (dx < 0 && cellSel % GridCols == 0) { wornSel = 0; return; }
            cellSel = Mathf.Clamp(cellSel + dx + dy * GridCols, 0, CellCount(p) - 1);
            page = cellSel / PageSize;
        }

        bool IsDoubleTap(int index)
        {
            bool same = index == lastClickIndex && Time.unscaledTime - lastClickTime < DoubleClickSeconds;
            lastClickIndex = index;
            lastClickTime = Time.unscaledTime;
            confirmDiscard = false;
            return same;
        }

        void OnCellClick(int cell)
        {
            bool twice = IsDoubleTap(cell);
            wornSel = -1;
            cellSel = cell;
            if (twice) Activate(); else Refresh();
        }

        /// <summary>A worn slot: select it and list what can go in it.</summary>
        void OnWornClick(int index)
        {
            bool twice = IsDoubleTap(WornClickBase + index);
            wornSel = index;
            SetFilter(Slots.All[index]);
            if (twice) Activate(); else Refresh();
        }

        // ---- actions ----

        Item SelectedItem(PlayerState p)
        {
            return wornSel >= 0 ? p.GetEquipped(Slots.All[wornSel]) : ItemAtCell(p, cellSel);
        }

        /// <summary>Drink a selected potion, equip a bag item, unequip a worn one.</summary>
        void Activate()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            Supply kind;
            if (wornSel < 0 && SupplyAtCell(cellSel, out kind))
            {
                if (IsPotion(kind))
                {
                    if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Use, "", PotionCatalog.PackSlot(PotionIsHp(kind), PotionSize(kind))); return; }
                    Flash(p.UsePotion(PotionIsHp(kind), PotionSize(kind)).Text);
                }
                Refresh();
                return;
            }
            Item item = SelectedItem(p);
            if (item == null) return;
            bool worn = wornSel >= 0;
            int slotIndex = Mathf.Max(0, Slots.Index(item.slot));
            if (NetMode.IsClient)
            {
                NetClient.SendInvAction(worn ? InvOp.Unequip : InvOp.Equip, item.uid, (byte)slotIndex);
                if (!worn && p.CanEquip(item)) wornSel = slotIndex; // the server's reply redraws
                return;
            }
            ActionResult r = worn ? p.Unequip(item.slot) : p.Equip(item);
            if (r.Ok) { RpgSession.SyncGear(); if (!worn) wornSel = slotIndex; }
            Flash(r.Ok ? worn ? "Unequipped" : "Equipped " + item.name : r.Text);
            Refresh();
        }

        void EnhanceSelected()
        {
            PlayerState p = RpgSession.Player;
            Item item = p != null ? SelectedItem(p) : null;
            EnhanceView view = Preview(p, item);
            if (!view.Can) { Flash(view.Reason); return; }
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Enhance, item.uid, 0); return; }
            int bandBefore = EnhanceStats.VisualBand(item);
            EnhanceResult r = EnhanceSystem.Attempt(p, item);
            if (!r.Ok) Flash("Cannot enhance");
            else if (r.Success)
            {
                Flash("Success! " + EnhanceStats.DisplayName(item) + (EnhanceStats.VisualBand(item) > bandBefore ? "  New look!" : ""));
                if (p.GetEquipped(item.slot) == item) RpgSession.SyncGear();
            }
            else Flash("Failed... stone lost (+" + r.Level + " kept)");
            Refresh();
        }

        void Flash(string text)
        {
            messageText.text = text;
            messageUntil = Time.unscaledTime + MessageSeconds;
        }

        // ---- refresh ----

        void Refresh()
        {
            PlayerState p = RpgSession.Player;
            HeroController hero = GameSession.Hero;
            if (p == null || hero == null) { Close(); return; }
            RefreshLoadout(p, hero);
            RefreshBag(p);
            RefreshDetail(p);
        }
    }
}

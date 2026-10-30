using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The bag window, a port of the web character menu (character-menu.js + inventory-panel.js): the painted bag frame on the right of the
    /// screen with Equipment / Inventory / Skill tabs. Equipment = the six worn slots around the hero portrait + a Status table, Unequip and
    /// Enhance. Inventory = a 3 x 4 paged grid (gold, potions and stones first, then the 20 bag slots) with the selected item's details,
    /// Equip / Use, Enhance, Sort and Discard. Click selects, double-click or J / Space activates, arrows move the selection.
    /// B / I toggles (RpgSession), Esc closes; the game is paused while it is open. Layout uses the web 960 x 640 canvas coordinates.
    /// Partial: InventoryPanel.Loadout.cs (Equipment tab), InventoryPanel.Bag.cs (Inventory tab).
    /// </summary>
    public partial class InventoryPanel : MonoBehaviour
    {
        const float BagX = 304f, BagY = 34f, BagW = 640f, BagH = 568f, DoubleClickSeconds = 0.35f, MessageSeconds = 1.5f;
        enum Mode { Equipment, Inventory, Skill }
        static readonly string[] TabNames = { "Equipment", "Inventory", "Skill" };
        static InventoryPanel instance;

        RectTransform overlay;
        Mode mode = Mode.Equipment;
        readonly BagWidgets.BagButton[] tabs = new BagWidgets.BagButton[3];
        readonly Image[] tabIcons = new Image[3];
        Image tabArrow;
        Text messageText;
        float messageUntil;
        float lastClickTime;
        int lastClickIndex = -1;
        bool confirmDiscard;
        string shownClass;

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
            UiKit.Panel(overlay, new Color(0f, 0f, 0f, 0.14f), true); // web drawOverlay(0.14); also stops clicks reaching the game
            // The web canvas (960 x 640) pinned to the right edge, so wide phone screens keep the hero visible on the left.
            RectTransform web = UiKit.Rect(overlay, "WebCanvas", 0f, 0f, UiKit.RefWidth, UiKit.RefHeight);
            web.anchorMin = web.anchorMax = web.pivot = new Vector2(1f, 0.5f);
            web.anchoredPosition = Vector2.zero;

            BagWidgets.Img(web, "FrameTitle", BagX, BagY, BagW, 60f, BagSprites.FrameTitle(), Color.white, true);
            BagWidgets.Img(web, "FrameBody", BagX, BagY + 60f, BagW, BagH - 60f, BagSprites.FrameBody(), Color.white, true);
            BagWidgets.Text(web, "Title", BagX + 98f, BagY + 18f, "Bag", 24, "#ffffff", TextAnchor.UpperLeft, 200f, true);
            Image close = BagWidgets.Img(web, "Close", BagX + BagW - 50f, BagY + 14f, 34f, 34f, null, Color.clear, true);
            close.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            BagWidgets.Text(close.transform, "X", 17f, 3f, "X", 22, "#fff5e9", TextAnchor.UpperCenter, 34f, true);

            for (int i = 0; i < TabNames.Length; i++)
            {
                float x = BagX + 16f + i * 202f, y = BagY + 62f;
                Mode m = (Mode)i;
                tabs[i] = new BagWidgets.BagButton(web, "Tab" + TabNames[i], x, y, 198f, 46f, "", BagSprites.Tone.Steel, () => SwitchMode(m));
                tabIcons[i] = UiKit.Icon(tabs[i].Root, "Icon", 22f, 6f, 34f);
                BagWidgets.Text(tabs[i].Root, "Name", 62f, 14f, TabNames[i], 16, "#f2f5ff", TextAnchor.UpperLeft, 130f, true);
            }
            tabArrow = BagWidgets.Img(web, "TabArrow", 0f, BagY + 62f + 45f, 20f, 10f, BagSprites.TabArrow(), Color.white);

            BuildLoadout(web);
            BuildBag(web);
            messageText = BagWidgets.Text(web, "Message", 616f, 503f, "", 12, "#f0d39a", TextAnchor.UpperLeft, 300f);
            overlay.gameObject.SetActive(false);
        }

        // ---- input ----

        void Update()
        {
            if (!IsOpen) return;
            if (messageText.text.Length > 0 && Time.unscaledTime > messageUntil) messageText.text = "";
            int dx = (InputAdapter.Pressed(GameKey.Right) ? 1 : 0) - (InputAdapter.Pressed(GameKey.Left) ? 1 : 0);
            int dy = (InputAdapter.Pressed(GameKey.Down) ? 1 : 0) - (InputAdapter.Pressed(GameKey.Up) ? 1 : 0);
            if (dx != 0 || dy != 0) { Navigate(dx, dy); Refresh(); }
            if (InputAdapter.Pressed(GameKey.Attack)) Activate();
        }

        void Navigate(int dx, int dy)
        {
            confirmDiscard = false;
            if (mode == Mode.Equipment) { equipSel = (equipSel + dy + dx + Slots.All.Length) % Slots.All.Length; return; }
            if (mode != Mode.Inventory) return;
            cellSel = Mathf.Clamp(cellSel + dx + dy * GridCols, 0, CellCount - 1);
            page = cellSel / PageSize;
        }

        /// <summary>Click on a cell: select it; a second click on the same cell within 0.35 s activates it (web double click).</summary>
        void OnCellClick(int index)
        {
            bool same = index == lastClickIndex && Time.unscaledTime - lastClickTime < DoubleClickSeconds;
            lastClickIndex = index;
            lastClickTime = Time.unscaledTime;
            if (mode == Mode.Equipment) equipSel = index; else cellSel = index;
            confirmDiscard = false;
            if (same) Activate(); else Refresh();
        }

        void SwitchMode(Mode m)
        {
            if (m == Mode.Skill) { Close(); SkillTreeWindow.Open(transform.parent); return; }
            mode = m;
            lastClickIndex = -1;
            confirmDiscard = false;
            Refresh();
        }

        // ---- actions ----

        static bool IsEquipped(PlayerState p, Item item) { return item != null && p.GetEquipped(item.slot) == item; }

        Item SelectedItem(PlayerState p)
        {
            if (mode == Mode.Equipment) return p.GetEquipped(Slots.All[equipSel]);
            int bag = cellSel - supplyOrder.Length;
            return mode == Mode.Inventory && bag >= 0 && bag < p.Inventory.Count ? p.Inventory[bag] : null;
        }

        /// <summary>Web activate(): drink a selected potion, equip a bag item, unequip a worn one.</summary>
        void Activate()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            if (mode == Mode.Inventory && cellSel < supplyOrder.Length)
            {
                Supply kind = supplyOrder[cellSel];
                if (kind == Supply.HpPotion || kind == Supply.MpPotion)
                {
                    if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.Use, "", (byte)(kind == Supply.HpPotion ? 0 : 1)); return; }
                    ActionResult used = p.UsePotion(kind == Supply.HpPotion);
                    Flash(used.Ok ? "Restored 30%" : used.Text);
                }
                Refresh();
                return;
            }
            Item item = SelectedItem(p);
            if (item == null) return;
            bool equipped = IsEquipped(p, item);
            if (NetMode.IsClient) { NetClient.SendInvAction(equipped ? InvOp.Unequip : InvOp.Equip, item.uid, (byte)Mathf.Max(0, Slots.Index(item.slot))); return; }
            ActionResult r = equipped ? p.Unequip(item.slot) : p.Equip(item);
            if (r.Ok) RpgSession.SyncGear();
            Flash(r.Ok ? equipped ? "Unequipped" : "Equipped " + item.name : r.Text);
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
                if (IsEquipped(p, item)) RpgSession.SyncGear();
            }
            else Flash("Failed... stone lost (+" + r.Level + " kept)");
            Refresh();
        }

        void Flash(string text)
        {
            messageText.text = text;
            messageUntil = Time.unscaledTime + MessageSeconds;
        }

        /// <summary>Web enhancePreview(): the next +level, its success chance and the stat it raises, or why it cannot be enhanced.</summary>
        struct EnhanceView { public bool Can, Max; public string Reason, StatLabel; public int From, To, StatFrom, StatTo, Owned, Chance; }

        static EnhanceView Preview(PlayerState p, Item item)
        {
            var v = new EnhanceView { Owned = p != null ? p.UpgradeStone : 0, StatLabel = "ATK" };
            if (p == null || !EnhanceStats.IsEnhanceable(item)) { v.Reason = "Weapons only"; return v; }
            string key = EnhanceStats.StatKey(item);
            int baseValue = item.stats.GetInt(key);
            v.StatLabel = key == "atk" ? "ATK" : key == "def" ? "DEF" : key == "maxHp" ? "Max HP" : key == "maxMp" ? "Max MP" : key;
            v.From = EnhanceStats.Level(item);
            v.StatFrom = baseValue + EnhanceStats.Bonus(baseValue, v.From, EnhanceStats.Scale(item));
            if (v.From >= EnhanceConfig.MaxEnhance) { v.Max = true; v.To = v.From; v.StatTo = v.StatFrom; v.Reason = "Max +" + EnhanceConfig.MaxEnhance; return v; }
            IUpgradeMethod method = StoneUpgradeMethod.Instance;
            v.To = v.From + 1;
            v.StatTo = baseValue + EnhanceStats.Bonus(baseValue, v.To, EnhanceStats.Scale(item));
            v.Chance = RpgMath.Round(method.SuccessChance(v.From) * 100.0);
            v.Can = method.CanPay(p, v.From);
            if (!v.Can) v.Reason = "Need Upgrade Stone";
            return v;
        }

        // ---- refresh ----

        void Refresh()
        {
            PlayerState p = RpgSession.Player;
            HeroController hero = GameSession.Hero;
            if (p == null || hero == null) { Close(); return; }
            if (shownClass != p.ClassId) { shownClass = p.ClassId; RefreshClassArt(p, hero.Class); }
            for (int i = 0; i < tabs.Length; i++) { tabs[i].SetSelected((int)mode == i); tabs[i].Set(true, null, i == (int)Mode.Skill); }
            RectTransform active = tabs[(int)mode].Root;
            ((RectTransform)tabArrow.transform).anchoredPosition = new Vector2(active.anchoredPosition.x + 89f, -(BagY + 62f + 45f));
            loadoutRoot.gameObject.SetActive(mode == Mode.Equipment);
            bagRoot.gameObject.SetActive(mode == Mode.Inventory);
            if (mode == Mode.Equipment) RefreshLoadout(p, hero);
            else RefreshBag(p);
        }

        /// <summary>Class-dependent art: tab icons and the portrait (rebuilt when the class changes).</summary>
        void RefreshClassArt(PlayerState p, HeroClassDef cls)
        {
            tabIcons[0].sprite = LootSprites.Icon(new Item { slot = Slots.Weapon, classId = p.ClassId, itemLevel = 1 });
            tabIcons[1].sprite = BagSprites.BagIcon();
            tabIcons[2].sprite = TouchControls.SkillSprite(cls, cls.SkillIcon >= 0 ? cls.SkillIcon : 0);
            foreach (Image icon in tabIcons) icon.enabled = icon.sprite != null;
            SetPortrait(cls);
        }
    }
}

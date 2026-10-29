using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Inventory / equipment / enhance window (B or I toggles, Esc closes; the game is paused while it is open).
    /// Left: the six equipment slots + hero stats. Middle: the 20-slot bag. Right: details of the selected item (stats compared with the
    /// equipped one, enhance preview) with Equip / Unequip, Enhance and Discard. Click selects, double-click equips / unequips.
    /// Built once with UGUI, then only refreshed after each action.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        const float W = 800f, H = 470f, Cell = 60f, Stride = 66f, DoubleClickSeconds = 0.35f;
        static readonly string[][] EquipGrid = { new[] { Slots.Helmet, Slots.Weapon }, new[] { Slots.Armor, Slots.Offhand }, new[] { Slots.Gloves, Slots.Boots } };
        static readonly Color Frame = new Color(0.3f, 0.38f, 0.48f, 1f), SelectedFrame = new Color(1f, 0.92f, 0.45f, 1f), Good = new Color(0.55f, 1f, 0.55f), Bad = new Color(1f, 0.5f, 0.45f);
        static InventoryPanel instance;

        sealed class SlotView
        {
            public Image Frame, Icon;
            public Text Corner, Caption;
            public Item Item;
            public string EquipSlot; // null for bag cells
        }

        readonly List<SlotView> equipViews = new List<SlotView>(6);
        readonly List<SlotView> bagViews = new List<SlotView>(PlayerState.InventorySize);
        RectTransform window;
        Text title, statsText, bagTitle, nameText, infoText, linesText, enhanceText, resultText;
        Button primaryButton, enhanceButton, discardButton;
        Text primaryLabel, enhanceLabel, discardLabel;
        Item selected;
        float lastClickTime;
        bool confirmDiscard;

        public static bool IsOpen { get { return instance != null && instance.window.gameObject.activeSelf; } }

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
            instance.window.gameObject.SetActive(true);
            instance.window.SetAsLastSibling();
            instance.resultText.text = "";
            instance.Refresh();
            GameClock.Paused = true;
        }

        public static void Close()
        {
            if (instance == null) return;
            instance.window.gameObject.SetActive(false);
            instance.selected = null;
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
            window = UiKit.Rect(canvas, "InventoryWindow", 0f, 0f, W, H, true);
            UiKit.Panel(window, new Color(0.04f, 0.07f, 0.12f, 0.94f), true); // raycast target: clicks never fall through to the game
            RectTransform header = UiKit.Rect(window, "Header", 0f, 0f, W, 34f);
            UiKit.Panel(header, new Color(0.1f, 0.16f, 0.25f, 1f));
            title = UiKit.Label(header, "Title", 14f, 0f, W - 80f, 34f, "", 17, Color.white, TextAnchor.MiddleLeft, true);
            Text closeLabel;
            UiKit.Button(header, "Close", W - 40f, 4f, 32f, 26f, "X", Close, out closeLabel);

            UiKit.Label(window, "EquipTitle", 16f, 40f, 150f, 22f, "Equipment", 14, new Color(0.75f, 0.85f, 1f), TextAnchor.MiddleLeft, true);
            for (int r = 0; r < EquipGrid.Length; r++)
                for (int c = 0; c < 2; c++)
                {
                    SlotView v = MakeCell(window, "Equip_" + EquipGrid[r][c], 16f + c * (Cell + 14f), 64f + r * (Cell + 18f));
                    v.EquipSlot = EquipGrid[r][c];
                    equipViews.Add(v);
                }
            statsText = UiKit.Label(window, "Stats", 16f, 300f, 160f, 160f, "", 13, Color.white, TextAnchor.UpperLeft);

            bagTitle = UiKit.Label(window, "BagTitle", 190f, 40f, 330f, 22f, "", 14, new Color(0.75f, 0.85f, 1f), TextAnchor.MiddleLeft, true);
            for (int i = 0; i < PlayerState.InventorySize; i++)
                bagViews.Add(MakeCell(window, "Bag_" + i, 190f + (i % 5) * Stride, 64f + (i / 5) * Stride));
            UiKit.Label(window, "Hint", 190f, 334f, 330f, 60f, "Click an item to select it, double-click to equip / unequip.\nEnhance weapons and offhands with Upgrade Stones (they drop from monsters).\n[1] / [2] potions also work here.   B / Esc closes.", 11, new Color(0.65f, 0.72f, 0.82f), TextAnchor.UpperLeft);

            RectTransform detail = UiKit.Rect(window, "Detail", 536f, 40f, 248f, 416f);
            UiKit.Panel(detail, new Color(0.08f, 0.12f, 0.19f, 0.95f));
            nameText = UiKit.Label(detail, "Name", 10f, 6f, 228f, 40f, "", 15, Color.white, TextAnchor.UpperLeft, true);
            infoText = UiKit.Label(detail, "Info", 10f, 48f, 228f, 36f, "", 12, new Color(0.75f, 0.8f, 0.88f), TextAnchor.UpperLeft);
            linesText = UiKit.Label(detail, "Lines", 10f, 86f, 228f, 110f, "", 13, Color.white, TextAnchor.UpperLeft);
            enhanceText = UiKit.Label(detail, "Enhance", 10f, 196f, 228f, 44f, "", 12, new Color(0.6f, 0.95f, 1f), TextAnchor.UpperLeft);
            primaryButton = UiKit.Button(detail, "Primary", 10f, 244f, 228f, 30f, "Equip", Primary, out primaryLabel);
            enhanceButton = UiKit.Button(detail, "EnhanceBtn", 10f, 280f, 228f, 30f, "Enhance", EnhanceSelected, out enhanceLabel);
            discardButton = UiKit.Button(detail, "Discard", 10f, 316f, 228f, 30f, "Discard", DiscardSelected, out discardLabel);
            resultText = UiKit.Label(detail, "Result", 10f, 352f, 228f, 58f, "", 13, Color.white, TextAnchor.UpperLeft, true);
            window.gameObject.SetActive(false);
        }

        SlotView MakeCell(Transform parent, string name, float x, float y)
        {
            var v = new SlotView();
            RectTransform rt = UiKit.Rect(parent, name, x, y, Cell, Cell);
            v.Frame = UiKit.Panel(rt, Frame, true);
            UiKit.Panel(UiKit.Fill(rt, "Inner", 2f), new Color(0.06f, 0.09f, 0.14f, 1f));
            v.Icon = UiKit.Icon(rt, "Icon", 4f, 4f, Cell - 8f);
            v.Caption = UiKit.Label(UiKit.Fill(rt, "Caption", 3f), "", 10, new Color(0.55f, 0.62f, 0.72f), TextAnchor.MiddleCenter);
            v.Corner = UiKit.Label(rt, "Corner", 2f, 1f, Cell - 6f, 16f, "", 12, new Color(1f, 0.9f, 0.5f), TextAnchor.UpperRight, true);
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = v.Frame;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() => OnCellClick(v));
            return v;
        }

        // ---- interaction ----

        void OnCellClick(SlotView v)
        {
            Deselect();
            if (v.Item == null) { selected = null; Refresh(); return; }
            bool same = v.Item == selected && Time.unscaledTime - lastClickTime < DoubleClickSeconds;
            lastClickTime = Time.unscaledTime;
            if (v.Item != selected) { selected = v.Item; confirmDiscard = false; resultText.text = ""; }
            if (same) Primary();
            else Refresh();
        }

        static void Deselect() { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); } // Space / Enter must never re-press a button

        static bool IsEquipped(PlayerState p, Item item) { return item != null && p.GetEquipped(item.slot) == item; }

        void Primary()
        {
            Deselect();
            PlayerState p = RpgSession.Player;
            if (p == null || selected == null) return;
            ActionResult r = IsEquipped(p, selected) ? p.Unequip(selected.slot) : p.Equip(selected);
            if (r.Ok) { Result(IsEquipped(p, selected) ? "Equipped " + EnhanceStats.DisplayName(selected) : "Moved to the bag", Good); RpgSession.SyncGear(); }
            else Result(r.Text, Bad);
            Refresh();
        }

        void EnhanceSelected()
        {
            Deselect();
            PlayerState p = RpgSession.Player;
            if (p == null || selected == null) return;
            int bandBefore = EnhanceStats.VisualBand(selected);
            EnhanceResult r = EnhanceSystem.Attempt(p, selected);
            if (!r.Ok)
            {
                Result(r.Reason == "no-cost" ? "Need an Upgrade Stone (monsters drop them)" : r.Reason == "max-level" ? "Already at the maximum +" + EnhanceConfig.MaxEnhance
                    : r.Reason == "not-weapon" ? "Only weapons and offhands can be enhanced" : "Can't enhance this item", Bad);
            }
            else if (r.Success)
            {
                bool newLook = EnhanceStats.VisualBand(selected) > bandBefore;
                Result("Success! " + EnhanceStats.DisplayName(selected) + (newLook ? "\nNew look unlocked!" : ""), Good);
                if (IsEquipped(p, selected)) RpgSession.SyncGear();
            }
            else Result("Failed... the stone was used up (still +" + r.Level + ")", Bad);
            Refresh();
        }

        void DiscardSelected()
        {
            Deselect();
            PlayerState p = RpgSession.Player;
            if (p == null || selected == null || IsEquipped(p, selected)) return;
            if (!confirmDiscard) { confirmDiscard = true; Refresh(); return; }
            string name = EnhanceStats.DisplayName(selected);
            p.RemoveFromInventory(selected.uid);
            selected = null;
            confirmDiscard = false;
            Result("Discarded " + name, new Color(0.85f, 0.85f, 0.85f));
            Refresh();
        }

        void Result(string text, Color c) { resultText.text = text; resultText.color = c; }

        // ---- refresh ----

        void Refresh()
        {
            PlayerState p = RpgSession.Player;
            HeroController hero = GameSession.Hero;
            if (p == null) { Close(); return; }
            if (selected != null && !IsEquipped(p, selected) && !p.Inventory.Contains(selected)) selected = null;
            title.text = "Inventory  -  " + (hero != null ? hero.Class.Name : RpgClasses.Get(p.ClassId).Name) + "  Lv " + p.Level;
            foreach (SlotView v in equipViews) Show(v, p.GetEquipped(v.EquipSlot), p, ItemInfo.SlotLabel(v.EquipSlot));
            for (int i = 0; i < bagViews.Count; i++) Show(bagViews[i], i < p.Inventory.Count ? p.Inventory[i] : null, p, "");
            bagTitle.text = "Bag  " + p.Inventory.Count + " / " + PlayerState.InventorySize;
            statsText.text = "ATK " + p.Atk + "     DEF " + p.Def + "\nHP " + Mathf.CeilToInt(p.Hp) + " / " + p.MaxHp + "\nMP " + Mathf.FloorToInt(p.Mp) + " / " + p.MaxMp
                + "\nSpeed " + Mathf.RoundToInt(p.Speed) + "     Crit " + Mathf.RoundToInt((0.08f + p.CritBonus) * 100f) + "%"
                + "\n\n<color=#ffd84d>" + p.Gold + " G</color>\nUpgrade Stones " + p.UpgradeStone + "\nPotions HP " + p.HpPotions + "  MP " + p.MpPotions;
            ShowDetail(p);
        }

        void Show(SlotView v, Item item, PlayerState p, string emptyCaption)
        {
            v.Item = item;
            v.Icon.sprite = item != null ? LootSprites.Icon(item) : null;
            v.Icon.enabled = v.Icon.sprite != null;
            bool usable = item == null || p.CanEquip(item);
            v.Icon.color = usable ? Color.white : new Color(1f, 0.6f, 0.6f, 0.55f); // other class's gear: dimmed red
            v.Caption.text = item == null ? emptyCaption : v.Icon.enabled ? "" : item.name;
            int level = EnhanceStats.Level(item);
            v.Corner.text = level > 0 ? "+" + level : "";
            Color rc = item != null ? LootSprites.RarityColor(item) : Frame;
            v.Frame.color = item != null && item == selected ? SelectedFrame : item != null ? Color.Lerp(Frame, rc, 0.7f) : Frame;
        }

        void ShowDetail(PlayerState p)
        {
            bool has = selected != null;
            primaryButton.gameObject.SetActive(has);
            enhanceButton.gameObject.SetActive(has && EnhanceStats.IsEnhanceable(selected));
            discardButton.gameObject.SetActive(has && !IsEquipped(p, selected));
            if (!has)
            {
                nameText.text = "Select an item";
                nameText.color = new Color(0.7f, 0.75f, 0.82f);
                infoText.text = "";
                linesText.text = "";
                enhanceText.text = "";
                return;
            }
            bool equipped = IsEquipped(p, selected);
            nameText.text = EnhanceStats.DisplayName(selected);
            nameText.color = LootSprites.RarityColor(selected);
            string owner = string.IsNullOrEmpty(selected.classId) ? "All classes" : RpgClasses.Get(selected.classId).Name + " only";
            bool usable = p.CanEquip(selected);
            infoText.text = ItemInfo.RarityLabel(selected.rarity) + " " + ItemInfo.SlotLabel(selected.slot) + "   iLv " + selected.itemLevel
                + "\n" + (usable ? owner : "<color=#ff8070>" + owner + "</color>") + (equipped ? "   <color=#9effa0>(equipped)</color>" : "");

            Item current = equipped ? null : p.GetEquipped(selected.slot);
            var sb = new StringBuilder();
            foreach (StatLine l in ItemInfo.Lines(selected, current))
            {
                sb.Append(ItemInfo.Format(l));
                string d = ItemInfo.FormatDelta(l);
                if (d.Length > 0) sb.Append("  <color=").Append(l.Delta > 0 ? "#8dff8d" : "#ff8070").Append(">").Append(d).Append("</color>");
                sb.Append('\n');
            }
            if (current != null) sb.Append("<color=#8a96a6>vs ").Append(EnhanceStats.DisplayName(current)).Append("</color>");
            linesText.text = sb.ToString();
            enhanceText.text = EnhanceStats.IsEnhanceable(selected) ? ItemInfo.EnhancePreview(selected) : "";

            primaryLabel.text = equipped ? "Unequip" : usable ? "Equip" : RpgClasses.Get(selected.classId).Name + " only";
            primaryButton.interactable = equipped || usable;
            int level = EnhanceStats.Level(selected);
            enhanceLabel.text = level >= EnhanceConfig.MaxEnhance ? "Max level" : "Enhance  (" + p.UpgradeStone + " stone" + (p.UpgradeStone == 1 ? "" : "s") + ")";
            enhanceButton.interactable = level < EnhanceConfig.MaxEnhance && p.UpgradeStone > 0;
            discardLabel.text = confirmDiscard ? "Click again to discard" : "Discard";
        }
    }
}

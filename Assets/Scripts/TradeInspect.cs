using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What an item in the other player's offer really is, before you confirm: this is where scams happen. There is no tooltip in the game, only the detail side of the bag window, so this is
    /// a small popup over the same shared words (ItemInfo, EnhanceStats): name with its +level, slot, class lock, level, rarity, and each stat against what you are wearing in that slot.
    /// </summary>
    public sealed class TradeInspect
    {
        const float W = 380f, H = 340f, IconBox = 76f, IconSize = 64f, StatsY = 140f, LineH = 28f;
        const int MaxLines = 6;

        readonly RectTransform root, box;
        readonly Image icon;
        readonly Text itemName, slotLine, levelLine, rule;
        readonly Text[] statTexts = new Text[MaxLines], deltaTexts = new Text[MaxLines];

        public TradeInspect(RectTransform board)
        {
            root = UiKit.Rect(board, "Inspect", 0f, 0f, UiKit.RefWidth, UiKit.RefHeight);
            UiWinKit.MakeButton(UiKit.Panel(root, new Color(0f, 0f, 0f, 0.5f), true), Close); // a tap outside closes it
            box = UiKit.Rect(root, "Box", (UiKit.RefWidth - W) * 0.5f, (UiKit.RefHeight - H) * 0.5f, W, H);
            PartyArt.Sliced(box, "invite-frame", PartyArt.InviteFrameScale, true);
            UiWinKit.CloseButton(box, W - 56f, 12f, 40f, Close);
            RectTransform slot = UiKit.Rect(box, "Slot", 24f, 24f, IconBox, IconBox);
            UiWinKit.Sliced(slot, "slot", IconBox / TradeArt.SlotNativeSize);
            icon = UiKit.Icon(slot, "Icon", (IconBox - IconSize) * 0.5f, (IconBox - IconSize) * 0.5f, IconSize);
            itemName = UiKit.Label(box, "Name", 110f, 22f, W - 110f - 58f, 44f, "", 17, Color.white, TextAnchor.UpperLeft, true);
            slotLine = UiKit.Label(box, "Slot2", 110f, 68f, W - 110f - 22f, 20f, "", 13, UiWinKit.Cream);
            levelLine = UiKit.Label(box, "Level", 110f, 88f, W - 110f - 22f, 20f, "", 13, UiWinKit.Dim);
            UiKit.Panel(UiKit.Rect(box, "Rule", 24f, 114f, W - 48f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));
            rule = UiKit.Label(box, "Against", 24f, 118f, W - 48f, 20f, "", 12, UiWinKit.Dim, TextAnchor.MiddleRight);
            for (int i = 0; i < MaxLines; i++)
            {
                statTexts[i] = UiKit.Label(box, "Stat" + i, 28f, StatsY + i * LineH, 200f, 26f, "", 15, Color.white);
                deltaTexts[i] = UiKit.Label(box, "Delta" + i, W - 28f - 120f, StatsY + i * LineH, 120f, 26f, "", 14, Color.white, TextAnchor.MiddleRight, true);
            }
            root.gameObject.SetActive(false);
        }

        public bool IsOpen { get { return root.gameObject.activeSelf; } }

        public void Close() { root.gameObject.SetActive(false); }

        public void Open(Item item, PlayerState me)
        {
            icon.sprite = LootSprites.Icon(item);
            icon.enabled = icon.sprite != null;
            ItemEnhanceSlotFx.Show(icon, item);
            itemName.text = EnhanceStats.DisplayName(item);
            itemName.color = LootSprites.RarityColor(item);
            bool usable = me == null || me.CanEquip(item);
            string locked = string.IsNullOrEmpty(item.classId) ? "" : "   " + (usable ? "" : "<color=#ff8070>") + RpgClasses.Get(item.classId).Name + " only" + (usable ? "" : "</color>");
            slotLine.text = ItemInfo.SlotLabel(item.slot) + locked;
            levelLine.text = item.slot == Slots.Pet ? ItemInfo.DurabilityLine(item) + (PetConfig.IsLow(item.durability) ? "   <color=#ff7060>Almost worn out!</color>" : "") : "Lv. " + item.itemLevel + "   " + ItemInfo.RarityLabel(item.rarity);

            Item worn = me != null ? me.GetEquipped(item.slot) : null;
            rule.text = worn != null ? "Against your " + EnhanceStats.DisplayName(worn) : "You wear nothing in this slot";
            var lines = ItemInfo.Lines(item, worn);
            for (int i = 0; i < MaxLines; i++)
            {
                bool has = i < lines.Count;
                statTexts[i].text = has ? ItemInfo.Format(lines[i]) : "";
                deltaTexts[i].text = "";
                if (!has) continue;
                double raw = worn != null ? lines[i].Delta : lines[i].Value;
                double shown = lines[i].Percent ? System.Math.Round(raw * 100.0) : System.Math.Round(raw);
                deltaTexts[i].text = (shown > 0 ? "+" : "") + shown + (lines[i].Percent ? "%" : "");
                deltaTexts[i].color = shown > 0 ? new Color(0.56f, 1f, 0.56f) : shown < 0 ? new Color(1f, 0.44f, 0.44f) : UiWinKit.Dim;
            }
            float h = StatsY + Mathf.Max(1, lines.Count) * LineH + 20f;
            box.sizeDelta = new Vector2(W, h);
            box.anchoredPosition = new Vector2((UiKit.RefWidth - W) * 0.5f, -(UiKit.RefHeight - h) * 0.5f);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
        }
    }
}

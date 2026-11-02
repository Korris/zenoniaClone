using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Left panel: the hero wearing the real gear between the 7 worn slots, the class line and the Stats popup. Coordinates are local to the panel.</summary>
    public partial class InventoryPanel
    {
        const float WornSize = 64f, WornIcon = 50f;
        static readonly string[] StatusLabels = { "HP", "MP", "ATK", "DEF", "Magic ATK", "Magic DEF", "CRI Rate", "Move Speed" };

        readonly BagWidgets.Slot[] equipSlots = new BagWidgets.Slot[Slots.All.Length];
        readonly Text[] statusValues = new Text[8];
        int wornSel; // index in Slots.All, -1 = a grid cell is selected
        HeroPortraitView heroView;
        Text classLevelText;
        RectTransform statsPopup;

        /// <summary>Top-left of worn slot i, Slots.All order. Two rows, armour above and weapons below the hero, with nothing beside him: the Warrior's sword reaches far past his body.</summary>
        static Vector2 WornPosition(int i)
        {
            const float Top = 10f, Bottom = 376f;
            switch (Slots.All[i])
            {
                case Slots.Helmet: return new Vector2(WornColumn(0), Top);
                case Slots.Armor: return new Vector2(WornColumn(1), Top);
                case Slots.Gloves: return new Vector2(WornColumn(2), Top);
                case Slots.Boots: return new Vector2(WornColumn(3), Top);
                case Slots.Weapon: return new Vector2(WornColumn(0), Bottom);
                case Slots.Offhand: return new Vector2(WornColumn(1), Bottom);
                default: return new Vector2(WornColumn(2), Bottom); // pet; the fourth cell of this row is the Stats button
            }
        }

        static float WornColumn(int column) { return 10f + column * 68f; }

        void BuildLoadout(RectTransform left)
        {
            RectTransform heroRect = UiKit.Rect(left, "Hero", 0f, 76f, 288f, 264f); // the whole panel width: a strong halo must not end on a straight edge
            RawImage heroImage = heroRect.gameObject.AddComponent<RawImage>();
            heroView = gameObject.AddComponent<HeroPortraitView>();
            heroView.Init(heroImage);
            classLevelText = BagWidgets.Text(left, "ClassLevel", 144f, 344f, "", 13, "#d9d3bd", TextAnchor.UpperCenter, 260f);

            for (int i = 0; i < equipSlots.Length; i++)
            {
                Vector2 at = WornPosition(i);
                int index = i;
                equipSlots[i] = new BagWidgets.Slot(left, "Equip_" + Slots.All[i], at.x, at.y, WornSize, () => OnWornClick(index));
            }

            new BagWidgets.BagButton(left, "Stats", WornColumn(3), 376f, WornSize, WornSize, "Stats", BagSprites.Tone.Blue, () => statsPopup.gameObject.SetActive(!statsPopup.gameObject.activeSelf));
            statsPopup = UiKit.Rect(left, "StatsPopup", 8f, 8f, 272f, 360f);
            UiWinKit.Sliced(statsPopup, "panel", 0.4f, true);
            BagWidgets.Text(statsPopup, "Title", 24f, 18f, "Stats", 18, "#ffedb3", TextAnchor.UpperLeft, 200f, true);
            for (int i = 0; i < StatusLabels.Length; i++)
            {
                float y = 54f + i * 36f;
                BagWidgets.Text(statsPopup, "Label" + i, 24f, y, StatusLabels[i], 15, "#c2e3f4", TextAnchor.UpperLeft, 140f);
                statusValues[i] = BagWidgets.Text(statsPopup, "Value" + i, 248f, y, "", 16, "#ffffff", TextAnchor.UpperRight, 100f, true);
            }
            statsPopup.gameObject.SetActive(false);
        }

        /// <summary>"95%" for a pet's cell, red near the end; null for anything else.</summary>
        static string PetDurabilityText(Item item)
        {
            if (item == null || item.slot != Slots.Pet) return null;
            string text = ItemInfo.DurabilityPercent(item);
            return PetConfig.IsLow(item.durability) ? "<color=#ff7060>" + text + "</color>" : text;
        }

        /// <summary>"Durability 80 / 1000" and the warning near the end.</summary>
        static string PetDurabilityLine(Item item)
        {
            string text = ItemInfo.DurabilityLine(item);
            return PetConfig.IsLow(item.durability) ? "<color=#ff7060>" + text + "  Almost worn out!</color>" : text;
        }

        /// <summary>Capture runs: this worn slot selected, the grid filtered to it.</summary>
        public static void TestSelectWorn(string slot)
        {
            if (instance == null) return;
            instance.OnWornClick(Slots.Index(slot));
        }

        void RefreshLoadout(PlayerState p, HeroController hero)
        {
            classLevelText.text = hero.Class.Name + "  Lv. " + p.Level;
            heroView.Show(hero.Class, p.GetEquipped(Slots.Weapon), p.GetEquipped(Slots.Offhand));
            for (int i = 0; i < equipSlots.Length; i++)
            {
                string slot = Slots.All[i];
                Item item = p.GetEquipped(slot);
                BagWidgets.Slot v = equipSlots[i];
                v.SetFrame(i == wornSel, item != null ? LootSprites.RarityColor(item) : BagWidgets.Hex("#8398a9"));
                int plus = EnhanceStats.Level(item);
                v.Count.text = PetDurabilityText(item) ?? (plus > 0 ? "+" + plus : "");
                if (item != null) v.SetIcon(LootSprites.Icon(item), WornIcon, Color.white);
                else // greyed outline of what belongs there
                {
                    bool classGear = slot == Slots.Weapon || slot == Slots.Offhand;
                    string template = slot == Slots.Pet ? PetConfig.All[0].TemplateId : null; // the first pet's outline; no icon yet = a bare frame
                    v.SetIcon(LootSprites.Icon(new Item { slot = slot, templateId = template, itemLevel = 1, classId = classGear ? p.ClassId : null }), WornIcon - 8f, new Color(0.62f, 0.66f, 0.7f, 0.22f));
                }
                v.SetEnhance(item);
            }

            float crit = (0.08f + p.CritBonus) * 100f;
            bool magic = p.ClassId == "wizard" || p.ClassId == "cleric";
            float baseSpeed = RpgClasses.Get(p.ClassId).Speed;
            int atk = Mathf.RoundToInt(p.DamageAtk);
            string[] values =
            {
                p.MaxHp.ToString(), p.MaxMp.ToString(), atk.ToString(), p.Def.ToString(), (magic ? atk : 0).ToString(), p.Def.ToString(),
                crit.ToString("0.0") + "%", Mathf.RoundToInt(baseSpeed > 0f ? p.Speed / baseSpeed * 100f : 100f) + "%",
            };
            for (int i = 0; i < values.Length; i++) statusValues[i].text = values[i];
        }
    }
}

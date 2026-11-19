using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;
using B = ZenoniaProto.InventoryRoBoxes;

namespace ZenoniaProto
{
    /// <summary>
    /// Left panel: the hero wearing the real gear (HeroPortraitView) on the castle stage, the 8 worn slots with captions around him, Lv, Power,
    /// Stats (popup with the eight values of BagText.StatusValues) and Enhance (the selected worn piece, else the worn weapon).
    /// The mockup's Ring cell holds the cosmetic Hat: the game has no ring slot. Coordinates are local to the panel.
    /// </summary>
    public partial class InventoryPanel
    {
        const float WornSize = 66f;

        readonly W.Cell[] equipSlots = new W.Cell[Slots.All.Length];
        readonly Text[] statusValues = new Text[8];
        int wornSel { get { return model.Worn; } } // index in Slots.All, -1 = a grid cell is selected
        HeroPortraitView heroView;
        Text levelText, powerText;
        RectTransform statsPopup;

        /// <summary>Centre of worn slot `slot` (mockup order: Weapon / Offhand, Armor / Necklace, Gloves / Pet, Boots / Hat).</summary>
        static Vector2 WornCentre(string slot)
        {
            switch (slot)
            {
                case Slots.Weapon: return new Vector2(78f, 64f);
                case Slots.Offhand: return new Vector2(254f, 64f);
                case Slots.Armor: return new Vector2(46f, 160f);
                case Slots.Helmet: return new Vector2(282f, 164f); // the "helmet" id is the necklace
                case Slots.Gloves: return new Vector2(44f, 256f);
                case Slots.Pet: return new Vector2(282f, 256f);
                case Slots.Boots: return new Vector2(64f, 362f);
                default: return new Vector2(266f, 362f); // Headgear (hat) where the mockup draws a ring
            }
        }

        void BuildLoadout(RectTransform left)
        {
            W.Img(left, "Stage", 8f, 8f, LeftW - 16f, 404f, "bag-stage"); // ice-blue Gothic window + thin magic circle (feet at ~y 320)
            RectTransform heroRect = UiKit.Rect(left, "Hero", -35f, 22f, 396f, 349f); // ~40 % larger than the old portrait, feet on the magic circle: the hero fills the stage as in the mockup
            RawImage heroImage = heroRect.gameObject.AddComponent<RawImage>();
            heroImage.raycastTarget = false;
            heroView = gameObject.AddComponent<HeroPortraitView>();
            heroView.Init(heroImage);

            for (int i = 0; i < equipSlots.Length; i++)
            {
                Vector2 c = WornCentre(Slots.All[i]);
                int index = i;
                W.Cell cell = new W.Cell(left, "Equip_" + Slots.All[i], c.x - WornSize * 0.5f, c.y - WornSize * 0.5f, WornSize, () => OnWornClick(index));
                cell.AddCaption(ItemInfo.SlotLabel(Slots.All[i]), 74f);
                equipSlots[i] = cell;
            }

            levelText = B.Pill(left, "LevelPlate", LeftW * 0.5f, 400f, 30f, "bag-lv-plate", "", 19, Color.white, 112f, true).Label;
            Image power = W.Img(left, "PowerPlate", 44f, 434f, LeftW - 88f, 36f, "bag-power-plate");
            W.Txt(power.transform, "Label", 46f, 0f, 70f, 35f, "Power", 18, W.Ink);
            powerText = W.Txt(power.transform, "Value", 124f, 0f, LeftW - 88f - 140f, 35f, "", 24, RoSoftTheme.Hex("#B75B20"), TextAnchor.MiddleRight);

            new B.IconButton(left, "Stats", 12f, 480f, 146f, 44f, "btn-sky", "bag-ico-stats", "Stats", 19, () => statsPopup.gameObject.SetActive(!statsPopup.gameObject.activeSelf));
            new B.IconButton(left, "EnhanceWorn", 166f, 480f, 148f, 44f, "btn-mint", "bag-ico-enhance", "Enhance", 19, EnhanceWorn);
        }

        /// <summary>The Stats popup over the left panel (closed by Stats again or its X).</summary>
        void BuildStatsPopup(RectTransform win)
        {
            statsPopup = W.Img(win, "StatsPopup", LeftX + 6f, PanelY + 6f, LeftW - 12f, 390f, "bag-window", true).rectTransform;
            W.Txt(statsPopup, "Title", 22f, 14f, 200f, 34f, "Stats", 22, W.Ink);
            W.Clickable(W.Img(statsPopup, "Close", LeftW - 12f - 50f, 10f, 38f, 38f, "bag-close", true), () => statsPopup.gameObject.SetActive(false));
            for (int i = 0; i < BagText.StatusLabels.Length; i++)
            {
                float y = 60f + i * 38f;
                W.Img(statsPopup, "Row" + i, 14f, y, LeftW - 40f, 34f, "bag-pill-white");
                W.Txt(statsPopup, "Label" + i, 30f, y, 150f, 33f, BagText.StatusLabels[i], 16, W.InkSoft, TextAnchor.MiddleLeft, false);
                statusValues[i] = W.Txt(statsPopup, "Value" + i, LeftW - 190f, y, 150f, 33f, "", 17, W.Ink, TextAnchor.MiddleRight);
            }
            statsPopup.gameObject.SetActive(false);
        }

        /// <summary>Capture runs: this worn slot selected, the grid filtered to it.</summary>
        public static void TestSelectWorn(string slot)
        {
            if (instance == null) return;
            instance.OnWornClick(Slots.Index(slot));
        }

        /// <summary>Left Enhance: the selected worn piece, else the worn weapon; it becomes the selection so the right panel shows the result.</summary>
        void EnhanceWorn()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            int index = wornSel >= 0 && Slots.All[wornSel] != Slots.Pet ? wornSel : Slots.Index(Slots.Weapon);
            Item item = p.GetEquipped(Slots.All[index]);
            if (item == null) { Flash("Nothing worn there to enhance."); return; }
            model.Worn = index;
            Show(BagActions.Enhance(p, item));
        }

        /// <summary>
        /// One number for the hero's strength, from the live stats: ATK x3 + DEF x2 + HP / 2 + MP / 4 + crit % x 20 + level x 10.
        /// Display only (not used by any rule, so the server does not need it).
        /// </summary>
        static int Power(PlayerState p)
        {
            float crit = (0.08f + p.CritBonus) * 100f;
            return Mathf.RoundToInt(p.DamageAtk * 3f + p.Def * 2f + p.MaxHp * 0.5f + p.MaxMp * 0.25f + crit * 20f + p.Level * 10f);
        }

        void RefreshLoadout(PlayerState p, HeroController hero)
        {
            levelText.text = "Lv. " + p.Level;
            powerText.text = Power(p).ToString("N0", CultureInfo.InvariantCulture);
            heroView.Show(hero.Class, p.GetEquipped(Slots.Weapon), p.GetEquipped(Slots.Offhand), p);
            for (int i = 0; i < equipSlots.Length; i++)
            {
                string slot = Slots.All[i];
                W.Cell v = equipSlots[i];
                bool selected = i == wornSel;
                if (slot == Slots.Pet) // the Pet Companion (account pet), not an item: a tap opens the Pet window
                {
                    PetDef pet = p.Pet;
                    v.SetState(selected, false, pet != null ? PetArt.RarityColor(pet.Rarity) : Color.white);
                    v.Count.text = pet != null ? PetArt.Stars(p.PetStars) : "";
                    if (pet != null) v.SetIcon(PetArt.Icon(pet), PetArt.TintOf(pet));
                    else v.SetIcon(PetArt.Icon(PetConfig.All[0]), new Color(0.55f, 0.6f, 0.7f, 0.5f), 0.7f);
                    ItemEnhanceSlotFx.Show(v.Icon, null);
                    continue;
                }
                Item item = p.GetEquipped(slot);
                v.SetState(selected, false, item != null ? LootSprites.RarityColor(item) : Color.white);
                int plus = EnhanceStats.Level(item);
                v.Count.text = plus > 0 ? "+" + plus : "";
                if (item != null) v.SetIcon(LootSprites.Icon(item), Color.white);
                else // greyed outline of what belongs there
                {
                    bool classGear = slot == Slots.Weapon || slot == Slots.Offhand;
                    v.SetIcon(LootSprites.Icon(new Item { slot = slot, itemLevel = 1, classId = classGear ? p.ClassId : null }), new Color(0.55f, 0.6f, 0.7f, 0.5f), 0.7f);
                }
                ItemEnhanceSlotFx.Show(v.Icon, item);
            }

            string[] values = BagText.StatusValues(p);
            for (int i = 0; i < values.Length; i++) statusValues[i].text = values[i];
        }
    }
}

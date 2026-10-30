using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Equipment tab (web drawLoadout): the hero portrait on a lit dais with the six worn slots around it (weapon / helmet / armor / gloves on
    /// the left, boots / offhand + two locked slots on the right), the "+ Status" table, Unequip and Enhance with its preview line.
    /// </summary>
    public partial class InventoryPanel
    {
        static readonly string[] StatusLabels = { "HP", "MP", "ATK", "DEF", "Magic ATK", "Magic DEF", "CRI Rate", "Move Speed" };

        RectTransform loadoutRoot;
        int equipSel;
        readonly BagWidgets.Slot[] equipSlots = new BagWidgets.Slot[6];
        readonly Text[] statusValues = new Text[8];
        Image portrait;
        Text classLevelText, loadoutEnhanceText;
        BagWidgets.BagButton unequipButton, loadoutEnhanceButton;

        /// <summary>Web equipSlotPosition(): centre of worn slot i (Slots.All order).</summary>
        static Vector2 EquipSlotCentre(int i)
        {
            bool left = i < 4;
            int row = left ? i : i == 5 ? 1 : 0;
            return new Vector2(left ? 360f : 658f, 202f + row * 84f);
        }

        void BuildLoadout(RectTransform web)
        {
            loadoutRoot = UiKit.Rect(web, "Loadout", 0f, 0f, UiKit.RefWidth, UiKit.RefHeight);
            BagWidgets.Panel(loadoutRoot, "Left", 320f, 154f, 376f, 414f);
            BagWidgets.Panel(loadoutRoot, "Right", 702f, 154f, 226f, 414f);
            BagWidgets.Img(loadoutRoot, "Backdrop", 400f, 186f, 218f, 306f, BagSprites.PortraitBackdrop(), Color.white);
            portrait = UiKit.Icon(loadoutRoot, "Portrait", 399f, 240f, 216f);
            classLevelText = BagWidgets.Text(loadoutRoot, "ClassLevel", 508f, 490f, "", 12, "#b0cadb", TextAnchor.UpperCenter, 200f);

            for (int i = 0; i < equipSlots.Length; i++)
            {
                Vector2 c = EquipSlotCentre(i);
                int index = i;
                equipSlots[i] = new BagWidgets.Slot(loadoutRoot, "Equip_" + Slots.All[i], c.x - 30f, c.y - 30f, 60f, () => OnCellClick(index));
            }
            for (int i = 2; i < 4; i++)
            {
                var locked = new BagWidgets.Slot(loadoutRoot, "Locked" + i, 628f, 172f + i * 84f, 60f, null);
                locked.SetFrame(false, BagWidgets.Hex("#4e667d"), true);
                locked.SetIcon(null, 0f, Color.white);
            }

            BagWidgets.Text(loadoutRoot, "StatusTitle", 716f, 165f, "+ Status", 17, "#ffffff", TextAnchor.UpperLeft, 200f, true);
            for (int i = 0; i < StatusLabels.Length; i++)
            {
                float y = 204f + i * 33f;
                BagWidgets.Img(loadoutRoot, "Row" + i, 714f, y - 6f, 200f, 31f, null, BagWidgets.Hex(i % 2 == 1 ? "#0b192c33" : "#45648319"));
                BagWidgets.Img(loadoutRoot, "Rule" + i, 714f, y - 8f, 200f, 1f, null, BagWidgets.Hex("#91acc322"));
                BagWidgets.Text(loadoutRoot, "Label" + i, 716f, y, StatusLabels[i], 13, "#bbdaed", TextAnchor.UpperLeft, 120f);
                statusValues[i] = BagWidgets.Text(loadoutRoot, "Value" + i, 914f, y, "", 14, "#ffffff", TextAnchor.UpperRight, 100f, true);
            }

            loadoutEnhanceText = BagWidgets.Text(loadoutRoot, "EnhanceLine", 332f, 538f, "", 12, "#f0d39a", TextAnchor.UpperLeft, 205f);
            loadoutEnhanceButton = new BagWidgets.BagButton(loadoutRoot, "Enhance", 540f, 528f, 150f, 32f, "Enhance", BagSprites.Tone.Gold, EnhanceSelected);
            unequipButton = new BagWidgets.BagButton(loadoutRoot, "Unequip", 748f, 528f, 160f, 32f, "Unequip", BagSprites.Tone.Blue, Activate);
        }

        /// <summary>Armed idle frame, facing down (web registry.heroes[classId].down.idle[0]).</summary>
        void SetPortrait(HeroClassDef cls)
        {
            Sprite[] row = SpriteSheet.Row(SpriteSheet.Grid("Sprites/" + cls.SheetFolder + "/idle", GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, new Vector2(0.5f, 0f)), 0);
            portrait.sprite = row != null && row.Length > 0 ? row[0] : null;
            portrait.enabled = portrait.sprite != null;
        }

        void RefreshLoadout(PlayerState p, HeroController hero)
        {
            classLevelText.text = hero.Class.Name + "  Lv. " + p.Level;
            for (int i = 0; i < equipSlots.Length; i++)
            {
                string slot = Slots.All[i];
                Item item = p.GetEquipped(slot);
                BagWidgets.Slot v = equipSlots[i];
                v.SetFrame(i == equipSel, item != null ? LootSprites.RarityColor(item) : BagWidgets.Hex("#8398a9"));
                int plus = EnhanceStats.Level(item);
                v.Count.text = plus > 0 ? "+" + plus : "";
                if (item != null) v.SetIcon(LootSprites.Icon(item), 46f, Color.white);
                else // greyed outline of what belongs there (web: grayscale at 22% alpha)
                {
                    bool classGear = slot == Slots.Weapon || slot == Slots.Offhand;
                    v.SetIcon(LootSprites.Icon(new Item { slot = slot, itemLevel = 1, classId = classGear ? p.ClassId : null }), 39f, new Color(0.62f, 0.66f, 0.7f, 0.22f));
                }
            }

            float crit = (0.08f + p.CritBonus) * 100f;
            bool magic = p.ClassId == "wizard" || p.ClassId == "cleric";
            float baseSpeed = RpgClasses.Get(p.ClassId).Speed;
            string[] values =
            {
                p.MaxHp.ToString(), p.MaxMp.ToString(), p.Atk.ToString(), p.Def.ToString(), (magic ? p.Atk : 0).ToString(), p.Def.ToString(),
                crit.ToString("0.0") + "%", Mathf.RoundToInt(baseSpeed > 0f ? p.Speed / baseSpeed * 100f : 100f) + "%",
            };
            for (int i = 0; i < values.Length; i++) statusValues[i].text = values[i];

            Item held = SelectedItem(p);
            unequipButton.Set(held != null);
            bool enhanceable = EnhanceStats.IsEnhanceable(held);
            EnhanceView e = Preview(p, held);
            loadoutEnhanceButton.Set(enhanceable, null, !e.Can);
            loadoutEnhanceText.text = !enhanceable ? "" : e.Max ? e.Reason : "+" + e.From + " -> +" + e.To + "  " + e.Chance + "%  Stone x" + e.Owned;
        }
    }
}

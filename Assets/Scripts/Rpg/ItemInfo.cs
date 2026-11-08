using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>One display line of an item's stats; Delta = change versus the item currently equipped in that slot.</summary>
    public struct StatLine { public string Label; public double Value, Delta; public bool Percent; }

    /// <summary>Presentation helpers for items (icon names, slot labels, stat lines + comparison). Pure, shared by the HUD, inventory and ground loot.</summary>
    public static class ItemInfo
    {
        static readonly string[] StatKeys = { "atk", "def", "maxHp", "maxMp", "critChance", "speedMult" };
        static readonly string[] StatLabels = { "ATK", "DEF", "HP", "MP", "Crit", "Speed" };

        /// <summary>Art tier of an item (shared by the icon and the held overlay): itemLevel / 10, 0..9.</summary>
        public static int ArtTier(Item item) { return item != null ? ItemDefs.EquipmentTier(item.itemLevel) : 0; }

        /// <summary>Resources/Sprites/icons name: "helmet-3", or "weapon-wizard-0" / "offhand-cleric-2" for class gear.</summary>
        public static string IconName(Item item)
        {
            if (item == null) return null;
            if (item.slot == Slots.Pet) { PetDef pet = PetConfig.Of(item); return pet != null ? "pet-" + pet.Id : null; }
            int tier = ArtTier(item);
            if (item.slot == Slots.Weapon || item.slot == Slots.Offhand)
                return item.slot + "-" + RpgClasses.Get(item.classId).Id + "-" + tier;
            return item.slot + "-" + tier;
        }

        /// <summary>Armor / gloves / boots: worn by every class, but drawn as a piece of the viewer's class tier set. The "helmet" slot (shown as a Necklace) keeps one shared icon set.</summary>
        public static bool HasClassSetIcon(string slot) { return slot == Slots.Armor || slot == Slots.Gloves || slot == Slots.Boots; }

        /// <summary>
        /// Class-specific icon name of an armour piece, "armor-wizard-4" (the piece of that class's tier set), or null when the slot has no class icons / no valid class.
        /// Armour carries no classId, so the class is the item's own tag when present, else `viewerClassId` (the player looking at it).
        /// Callers fall back to <see cref="IconName"/> when this PNG is missing (e.g. the fighter has no set icons).
        /// </summary>
        public static string ClassIconName(Item item, string viewerClassId)
        {
            if (item == null || !HasClassSetIcon(item.slot)) return null;
            string cls = RpgClasses.IsValid(item.classId) ? item.classId : viewerClassId;
            return RpgClasses.IsValid(cls) ? item.slot + "-" + cls + "-" + ArtTier(item) : null;
        }

        public static string SlotLabel(string slot)
        {
            switch (slot)
            {
                case Slots.Weapon: return "Weapon";
                case Slots.Helmet: return "Necklace"; // designer 08/11: the "helmet" slot id (saves, server) is a stat-only necklace
                case Slots.Armor: return "Armor";
                case Slots.Gloves: return "Gloves";
                case Slots.Boots: return "Boots";
                case Slots.Offhand: return "Offhand";
                case Slots.Pet: return "Pet";
                default: return slot ?? "";
            }
        }

        /// <summary>False for the necklace, gloves and boots: stats only, nothing on the character changes.</summary>
        public static bool ChangesAppearance(string slot) { return slot != Slots.Helmet && slot != Slots.Gloves && slot != Slots.Boots; }

        public const string NoAppearanceLine = "Does not change appearance";

        public static string RarityLabel(string rarity)
        {
            return string.IsNullOrEmpty(rarity) ? "Common" : char.ToUpperInvariant(rarity[0]) + rarity.Substring(1);
        }

        /// <summary>"#rrggbb" of the rarity (white when unknown).</summary>
        public static string RarityColor(string rarity) { RarityDef r = ItemDefs.Rarity(rarity); return r != null ? r.Color : "#ffffff"; }

        static double Get(ItemStats s, int i)
        {
            switch (i)
            {
                case 4: return s.critChance;
                case 5: return s.speedMult;
                default: return s.GetInt(StatKeys[i]);
            }
        }

        /// <summary>Effective (enhanced) stats of the item as lines; with `compare` every stat either item has is listed with its delta.</summary>
        public static List<StatLine> Lines(Item item, Item compare = null)
        {
            var lines = new List<StatLine>();
            if (item == null) return lines;
            ItemStats s = EnhanceStats.Effective(item);
            ItemStats c = compare != null && compare != item ? EnhanceStats.Effective(compare) : null;
            for (int i = 0; i < StatKeys.Length; i++)
            {
                double v = Get(s, i), o = c != null ? Get(c, i) : 0.0;
                if (v == 0.0 && o == 0.0) continue;
                lines.Add(new StatLine { Label = StatLabels[i], Value = v, Delta = c != null ? v - o : 0.0, Percent = i >= 4 });
            }
            PetDef pet = PetConfig.Of(item), other = c != null ? PetConfig.Of(compare) : null;
            if (pet != null)
            {
                AddPetLine(lines, "Max HP", pet.MaxHpPct, other != null ? other.MaxHpPct : 0.0, c != null);
                AddPetLine(lines, "Damage", pet.DamagePct, other != null ? other.DamagePct : 0.0, c != null);
            }
            return lines;
        }

        static void AddPetLine(List<StatLine> lines, string label, double value, double against, bool comparing)
        {
            if (value == 0.0 && against == 0.0) return;
            lines.Add(new StatLine { Label = label, Value = value, Delta = comparing ? value - against : 0.0, Percent = true });
        }

        /// <summary>"Durability 950 / 1000" of a pet, "" for anything else.</summary>
        public static string DurabilityLine(Item item)
        {
            return PetConfig.Of(item) != null ? "Durability " + item.durability + " / " + PetConfig.MaxDurability : "";
        }

        /// <summary>"95%" on a pet's slot, never 0% while it lives; "" for anything else.</summary>
        public static string DurabilityPercent(Item item)
        {
            return PetConfig.Of(item) != null ? (int)Math.Ceiling(item.durability * 100.0 / PetConfig.MaxDurability) + "%" : "";
        }

        /// <summary>"ATK 14", "Crit +3%", "Speed +5%".</summary>
        public static string Format(StatLine l)
        {
            return l.Percent ? l.Label + " +" + RpgMath.Round(l.Value * 100.0) + "%" : l.Label + " " + RpgMath.Round(l.Value);
        }

        /// <summary>"(+3)" / "(-2)" / "" for a comparison delta.</summary>
        public static string FormatDelta(StatLine l)
        {
            double d = l.Percent ? Math.Round(l.Delta * 100.0) : Math.Round(l.Delta);
            if (d == 0.0) return "";
            return "(" + (d > 0 ? "+" : "") + d + (l.Percent ? "%" : "") + ")";
        }

        /// <summary>Enhance preview of a weapon / offhand: current level, next level, success chance and the stat(s) it raises (before -> after).</summary>
        public static string EnhancePreview(Item item, IUpgradeMethod method = null)
        {
            if (!EnhanceStats.IsEnhanceable(item)) return "";
            method = method ?? StoneUpgradeMethod.Instance;
            int level = EnhanceStats.Level(item);
            if (level >= EnhanceConfig.MaxEnhance) return "+" + level + " (max)";
            string head = "+" + level + " -> +" + (level + 1) + "  " + RpgMath.Round(method.SuccessChance(level) * 100.0) + "%\n";
            if (item.slot == Slots.Offhand) return head + SecondWeaponText.Preview(item) + "\n(" + method.CostLabel + ")";
            int baseValue = item.stats.GetInt("atk");
            int now = baseValue + EnhanceStats.Bonus(baseValue, level), next = baseValue + EnhanceStats.Bonus(baseValue, level + 1);
            return head + "ATK " + now + " -> " + next + "  (" + method.CostLabel + ")";
        }
    }
}

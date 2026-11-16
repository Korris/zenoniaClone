using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Names, numbers and pictures both looks of the bag show the same way (InventoryPanel, ArmoryWindow): all read from the real item / player data.</summary>
    public static class BagText
    {
        public const string GoldBlurb = "Gold used for trading.", StoneBlurb = "Used to enhance weapons. Success drops at higher +.";
        public static readonly string[] StatusLabels = { "HP", "MP", "ATK", "DEF", "Magic ATK", "Magic DEF", "CRI Rate", "Move Speed" };

        public static Sprite SupplySprite(BagSupply kind)
        {
            return kind == BagSupply.Coin ? LootSprites.Coin() : kind == BagSupply.Stone ? LootSprites.Stone() : LootSprites.Potion(BagModel.PotionIsHp(kind), BagModel.PotionSize(kind));
        }

        public static string SupplyName(BagSupply kind)
        {
            return BagModel.IsPotion(kind) ? PotionCatalog.Name(BagModel.PotionIsHp(kind), BagModel.PotionSize(kind)) : kind == BagSupply.Coin ? "Gold" : "Upgrade Stone";
        }

        /// <summary>What a supply does; a potion also says what Use does.</summary>
        public static string SupplyBlurb(BagSupply kind)
        {
            if (!BagModel.IsPotion(kind)) return kind == BagSupply.Coin ? GoldBlurb : StoneBlurb;
            return PotionCatalog.Effect(BagModel.PotionIsHp(kind), BagModel.PotionSize(kind)) + " Use: drink it and set it on the quick button.";
        }

        /// <summary>This potion stack is the one the quick button drinks.</summary>
        public static bool IsQuickPotion(PlayerState p, BagSupply kind)
        {
            return BagModel.IsPotion(kind) && p.PotionPick(BagModel.PotionIsHp(kind)) == BagModel.PotionSize(kind);
        }

        /// <summary>Item name as the bag shows it: the class weapon name of its tier when there is one, without the rarity prefix (the rarity has its own label), with "+N".</summary>
        public static string ItemName(Item item)
        {
            string name = WeaponNames.BaseName(item.classId, item.slot, ItemInfo.ArtTier(item));
            if (name == null)
            {
                name = item.name ?? "";
                RarityDef rarity = ItemDefs.Rarity(item.rarity);
                if (rarity != null && !string.IsNullOrEmpty(rarity.Prefix) && name.StartsWith(rarity.Prefix + " ", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(rarity.Prefix.Length + 1);
            }
            int plus = EnhanceStats.Level(item);
            return plus > 0 ? name + " +" + plus : name;
        }

        /// <summary>Rarity as the game names it: the item prefix ("Fine", "Superior", "Legendary"), "Common" for the plain one.</summary>
        public static string RarityName(Item item)
        {
            RarityDef rarity = ItemDefs.Rarity(item.rarity);
            return rarity != null && rarity.Prefix != null ? rarity.Prefix : ItemInfo.RarityLabel(item.rarity);
        }

        /// <summary>"14" or "25%".</summary>
        public static string TableValue(double value, bool percent)
        {
            return Math.Round(value * (percent ? 100.0 : 1.0)).ToString() + (percent ? "%" : "");
        }

        /// <summary>Signed change of a stat line versus what is worn ("+3", "-9%", "0"); `shown` gets the rounded number for colouring.</summary>
        public static string Change(StatLine line, bool compared, out double shown)
        {
            double d = compared ? line.Delta : line.Value; // nothing worn: the whole stat is a gain
            shown = line.Percent ? Math.Round(d * 100.0) : Math.Round(d);
            return (shown > 0 ? "+" : "") + shown + (line.Percent ? "%" : "");
        }

        /// <summary>The eight values of the Stats popup, in StatusLabels order.</summary>
        public static string[] StatusValues(PlayerState p)
        {
            float crit = (0.08f + p.CritBonus) * 100f;
            bool magic = p.ClassId == "wizard" || p.ClassId == "cleric";
            float baseSpeed = RpgClasses.Get(p.ClassId).Speed;
            int atk = Mathf.RoundToInt(p.DamageAtk);
            return new[]
            {
                p.MaxHp.ToString(), p.MaxMp.ToString(), atk.ToString(), p.Def.ToString(), (magic ? atk : 0).ToString(), p.Def.ToString(),
                crit.ToString("0.0") + "%", Mathf.RoundToInt(baseSpeed > 0f ? p.Speed / baseSpeed * 100f : 100f) + "%",
            };
        }
    }
}

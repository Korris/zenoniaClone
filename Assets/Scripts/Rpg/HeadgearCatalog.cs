using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>Where a hat comes from (docs/character-appearance-layered-spec.md "Đã chốt" 3). Nobody picks one at character creation.</summary>
    public static class HeadgearSource
    {
        public const string Starter = "starter", Shop = "shop", Monster = "monster", Wanted = "wanted";
    }

    /// <summary>One cosmetic hat. Id = CharacterAppearanceData.equippedHeadgearId / NetAppearance.Headgear (stable, never reused).</summary>
    public sealed class HeadgearDef
    {
        public int Id, Price;
        public string Key, Name, Source, DropFrom, ClassId, Rarity;
        public double Rate;
        /// <summary>A hood that covers the whole head (nun veil, aviator cap): the hair layer is not drawn while it is worn.</summary>
        public bool HidesHair;
        /// <summary>ItemDefs template id of the hat item: "hat-&lt;key&gt;".</summary>
        public string TemplateId { get { return "hat-" + Key; } }
    }

    /// <summary>
    /// The 19 cosmetic hats (headgear slot, no stats) and how each is obtained: newbie quest gift by class, village blacksmith shop, signature drop of a
    /// field monster species (1-5 % a kill), rare drop of a Wanted Boss (each contributor rolls). These rules are the authority for the zone server, the
    /// offline game and the tests; Data/hero-layered/headgear.json mirrors them (a test keeps both equal) and adds the art layout the client reads.
    /// Engine-free.
    /// </summary>
    public static class HeadgearCatalog
    {
        /// <summary>Any Wanted Boss (DropFrom of the ultimate wizard hat).</summary>
        public const string AnyWantedBoss = "*";
        /// <summary>GearBuy Slot byte of a hat: ShopEntryBase + hat id (gear entries are 0..GearShops.EntryCount-1).</summary>
        public const int ShopEntryBase = 128;

        public static readonly HeadgearDef[] All =
        {
            H(1, "iron-helmet", "Small Iron Helmet", HeadgearSource.Starter, null, 0, 0, "warrior", "common"),
            H(2, "wizard-hat", "Pointed Wizard Hat", HeadgearSource.Starter, null, 0, 0, "wizard", "common"),
            H(3, "copper-goggles", "Copper Goggles", HeadgearSource.Starter, null, 0, 0, "mechanic", "common"),
            H(4, "ninja-headband", "Red Ninja Headband", HeadgearSource.Starter, null, 0, 0, "assassin", "common"),
            H(5, "nun-veil", "White Nun Veil", HeadgearSource.Starter, null, 0, 0, "cleric", "common"),
            H(6, "straw-hat", "Straw Hat", HeadgearSource.Shop, null, 0, 150, null, "common"),
            H(7, "cloth-headband", "Cloth Headband", HeadgearSource.Shop, null, 0, 120, null, "common"),
            H(8, "pompom-beanie", "Pompom Beanie", HeadgearSource.Shop, null, 0, 200, null, "common"),
            H(9, "aviator-cap", "Leather Aviator Cap", HeadgearSource.Shop, null, 0, 280, null, "common"),
            H(10, "bunny-ears", "Bunny Ears", HeadgearSource.Monster, "hornRabbit", 0.04, 0, null, "uncommon"),
            H(11, "slime-cap", "Slime Cap", HeadgearSource.Monster, "frostSlime", 0.03, 0, null, "uncommon"),
            H(12, "fox-ears", "Fox Ears", HeadgearSource.Monster, "snowFox", 0.025, 0, null, "uncommon"),
            H(13, "devil-horns", "Small Devil Horns", HeadgearSource.Monster, "iceImp", 0.02, 0, null, "uncommon"),
            H(14, "frog-hat", "Frog Hat", HeadgearSource.Monster, "mireToad", 0.015, 0, null, "uncommon"),
            H(15, "wildflower-wreath", "Wildflower Wreath", HeadgearSource.Monster, "thornSprite", 0.03, 0, null, "uncommon"),
            H(16, "silver-wolf-crown", "Silver Wolf Crown", HeadgearSource.Wanted, "rimeclawTyrant", 0.06, 0, null, "rare"),
            H(17, "ice-crystal-tiara", "Ice Crystal Tiara", HeadgearSource.Wanted, "glacialColossus", 0.06, 0, null, "rare"),
            H(18, "serpent-crown", "Serpent Crown", HeadgearSource.Wanted, "mireHydraQueen", 0.06, 0, null, "rare"),
            H(19, "ultimate-wizard-hat", "Ultimate Wizard Hat", HeadgearSource.Wanted, AnyWantedBoss, 0.01, 0, null, "epic"),
        };

        /// <summary>Highest id (server-api HEADGEAR_COUNT; ids are 1..Count, 0 = bare head).</summary>
        public static int Count { get { return All.Length; } }

        /// <summary>Class without a starter hat of its own (the Fighter) gets this one from the newbie quest.</summary>
        public const int FallbackStarterId = 7;

        static HeadgearDef H(int id, string key, string name, string source, string from, double rate, int price, string cls, string rarity)
        {
            return new HeadgearDef { Id = id, Key = key, Name = name, Source = source, DropFrom = from, Rate = rate, Price = price, ClassId = cls, Rarity = rarity, HidesHair = key == "nun-veil" || key == "aviator-cap" };
        }

        public static HeadgearDef Get(int id) { return id >= 1 && id <= All.Length ? All[id - 1] : null; }

        public static HeadgearDef ByTemplate(string templateId)
        {
            if (templateId == null) return null;
            foreach (HeadgearDef d in All) if (d.TemplateId == templateId) return d;
            return null;
        }

        /// <summary>Hat id of a worn / bag item, 0 for anything that is not a hat.</summary>
        public static int IdOf(Item item)
        {
            if (item == null || item.slot != Slots.Headgear) return 0;
            HeadgearDef d = ByTemplate(item.templateId);
            return d != null ? d.Id : 0;
        }

        /// <summary>The class starter hat the newbie quest gives.</summary>
        public static HeadgearDef StarterFor(string classId)
        {
            foreach (HeadgearDef d in All) if (d.Source == HeadgearSource.Starter && d.ClassId == classId) return d;
            return Get(FallbackStarterId);
        }

        public static List<HeadgearDef> BySource(string source)
        {
            var list = new List<HeadgearDef>();
            foreach (HeadgearDef d in All) if (d.Source == source) list.Add(d);
            return list;
        }

        /// <summary>A new hat item. `uid` null = "hat-&lt;key&gt;-&lt;random&gt;" (the save and server-api need unique uids).</summary>
        public static Item CreateItem(HeadgearDef d, Rand rand, string uid = null)
        {
            if (d == null) return null;
            return new Item
            {
                uid = uid ?? d.TemplateId + "-" + (long)Math.Floor((rand ?? RpgRandom.Default)() * 1e9), templateId = d.TemplateId, slot = Slots.Headgear,
                name = d.Name, rarity = d.Rarity, itemLevel = 1, stats = new ItemStats(),
            };
        }

        /// <summary>One kill of a field monster: its signature hat at that species' rate, else null. Draws rand() only for species that have one.</summary>
        public static Item RollMonsterDrop(string monsterType, Rand rand)
        {
            foreach (HeadgearDef d in All)
                if (d.Source == HeadgearSource.Monster && d.DropFrom == monsterType) return rand() < d.Rate ? CreateItem(d, rand) : null;
            return null;
        }

        /// <summary>A Wanted Boss kill, rolled once per contributor: the boss's own crown, then the ultimate wizard hat any Wanted Boss can give. Empty for other monsters.</summary>
        public static List<Item> RollWantedDrops(string bossType, Rand rand)
        {
            var drops = new List<Item>();
            foreach (HeadgearDef d in All)
            {
                if (d.Source != HeadgearSource.Wanted || (d.DropFrom != bossType && d.DropFrom != AnyWantedBoss)) continue;
                if (!IsWantedBoss(bossType)) break;
                if (rand() < d.Rate) drops.Add(CreateItem(d, rand));
            }
            return drops;
        }

        /// <summary>The three daily Wanted Bosses (the bosses with a crown of their own).</summary>
        public static bool IsWantedBoss(string type)
        {
            foreach (HeadgearDef d in All) if (d.Source == HeadgearSource.Wanted && d.DropFrom == type) return true;
            return false;
        }

        // ---- village shop (GearShops sellers, InvOp.GearBuy) ----

        public static int ShopEntry(int hatId) { return ShopEntryBase + hatId; }

        /// <summary>A GearBuy Slot byte that names a shop hat; false for gear entries and hats the shop does not sell.</summary>
        public static bool TryShopHat(int entry, out HeadgearDef hat)
        {
            hat = entry > ShopEntryBase ? Get(entry - ShopEntryBase) : null;
            if (hat != null && hat.Source != HeadgearSource.Shop) hat = null;
            return hat != null;
        }
    }
}

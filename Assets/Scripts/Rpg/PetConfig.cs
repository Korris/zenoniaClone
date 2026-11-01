namespace ZenoniaProto.Rpg
{
    /// <summary>One pet. Its bonus comes from this table by template id, never from the item's own stats, so a save cannot change it.</summary>
    public sealed class PetDef
    {
        public string Id, TemplateId, Name, Effect;
        public int Price;
        public double MaxHpPct, DamagePct;
        public int DrainPerHitTaken, DrainPerAttack;
    }

    /// <summary>
    /// The numbers of the Pet slot, all in one place: every class, one worn at a time, no tiers, no enhancing. PlayerState.RecomputeStats applies them, on the server and in the client's mirror alike.
    /// MaxHpPct multiplies (base + gear) x (1 + second weapon HP %). DamagePct multiplies (base + gear) ATK in PlayerState.DamageAtk, then temporary ATK buffs, the skill or combo multiplier, the crit x1.6 and
    /// the +-10 % roll multiply it, and monster DEF x 0.5 is subtracted last. Heals and shields read PlayerState.Atk, without the pet. Ids of the art: Sprites/pets/&lt;Id&gt;/art.png, Sprites/icons/pet-&lt;Id&gt;.png.
    /// Keep TemplateId in step with PET_TEMPLATE_IDS in server-api/src/rpg/rpg-constants.js.
    /// </summary>
    public static class PetConfig
    {
        public static readonly PetDef[] All =
        {
            new PetDef { Id = "spirit", TemplateId = "pet-spirit", Name = "Spirit", Effect = "+15% Max HP", Price = 3000, MaxHpPct = 0.15, DrainPerHitTaken = 1 },
            new PetDef { Id = "imp", TemplateId = "pet-imp", Name = "Imp", Effect = "+10% damage", Price = 3000, DamagePct = 0.10, DrainPerAttack = 1 },
        };

        /// <summary>Durability of a new pet. It only goes down, in the zone; at 0 the pet is gone for good. Keep in step with PET_MAX_DURABILITY in server-api/src/rpg/rpg-constants.js.</summary>
        public const int MaxDurability = 1000;
        /// <summary>At or under this share of the maximum the windows warn, once by a message when it is crossed.</summary>
        public const double LowShare = 0.10;
        /// <summary>One attack event: every hit the hero lands within this long of the first one costs the imp 1 durability together, however many monsters a swing or an area skill touches.</summary>
        public const int AttackWindowMs = 250;
        public const string WornOutText = "Your pet has worn out", LowText = "Your pet is almost worn out";

        public static bool IsLow(int durability) { return durability <= MaxDurability * LowShare; }

        /// <summary>Pets are not sorted by rarity; this only picks the colour of their frame and name.</summary>
        public const string Rarity = "rare";

        public static PetDef Get(string id) { foreach (PetDef d in All) if (d.Id == id) return d; return null; }
        public static PetDef ByTemplate(string templateId) { foreach (PetDef d in All) if (d.TemplateId == templateId) return d; return null; }
        public static PetDef ByIndex(int i) { return i >= 0 && i < All.Length ? All[i] : null; }

        /// <summary>The pet this item is; null for anything else, or nothing worn.</summary>
        public static PetDef Of(Item item) { return item != null && item.slot == Slots.Pet ? ByTemplate(item.templateId) : null; }

        /// <summary>NetAppearance.PetId: 0 = none, otherwise index + 1.</summary>
        public static byte WireId(Item worn)
        {
            PetDef d = Of(worn);
            return d != null ? (byte)(System.Array.IndexOf(All, d) + 1) : (byte)0;
        }

        /// <summary>Art id of a NetAppearance.PetId, null for none or an id this build does not know.</summary>
        public static string IdOfWire(byte wire) { PetDef d = ByIndex(wire - 1); return d != null ? d.Id : null; }

        /// <summary>Art id of the worn item, null for none.</summary>
        public static string IdOf(Item worn) { PetDef d = Of(worn); return d != null ? d.Id : null; }

        /// <summary>Who sells pets: the general merchants, not the healer.</summary>
        public static bool SoldBy(string npcId) { return npcId == "merchantDale" || npcId == "apothecaryNara"; }

        /// <summary>Null when this hero may buy from this seller: it stocks pets, works on the hero's map and stands in reach. Gold and bag room are PlayerState.BuyPet's check.</summary>
        public static string Refusal(string npcId, string mapId, int petIndex, float distance)
        {
            if (!SoldBy(npcId) || PotionShops.MapOf(npcId) != mapId) return "Nobody sells that here";
            if (ByIndex(petIndex) == null) return "Not sold here";
            if (distance > PotionShops.ReachUnits) return "Too far from the shop";
            return null;
        }
    }
}

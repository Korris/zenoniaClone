using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Potion sizes: how much of the maximum they restore, their names and shop prices. Drops and old saves are Small.</summary>
    public static class PotionCatalog
    {
        public const int Small = 0, Medium = 1, Large = 2, Sizes = 3, MaxStack = 999;
        static readonly double[] Fractions = { 0.3, 0.5, 0.8 };
        static readonly int[] Prices = { 15, 60, 180 }; // a few Frost Hollow kills, a small Lv 20 fight, a few kills in the Sunspire region
        static readonly string[] SizeNames = { "Small", "Medium", "Large" };

        public static bool ValidSize(int size) { return size >= 0 && size < Sizes; }
        public static double Fraction(int size) { return Fractions[size]; }
        public static int Percent(int size) { return (int)Math.Round(Fractions[size] * 100.0); }
        public static int Price(int size) { return Prices[size]; }
        public static string Name(bool hp, int size) { return SizeNames[size] + (hp ? " HP Potion" : " MP Potion"); }
        public static string Effect(bool hp, int size) { return "Restores " + Percent(size) + "% " + (hp ? "HP" : "MP") + "."; }

        /// <summary>InvOp.Use / PotionBuy slot byte: bit 0 = MP, the bits above = size + 1 (0 = whichever the quick button would drink).</summary>
        public static byte PackSlot(bool hp, int size) { return (byte)((hp ? 0 : 1) | ((size + 1) << 1)); }
        public static bool SlotIsHp(byte slot) { return (slot & 1) == 0; }
        public static int SlotSize(byte slot) { return (slot >> 1) - 1; }

        /// <summary>PotionUsed event step: bit 0 = HP, the bits above = size.</summary>
        public static byte PackEvent(bool hp, int size) { return (byte)((hp ? 1 : 0) | (size << 1)); }
        public static bool EventIsHp(byte step) { return (step & 1) == 1; }
        public static int EventSize(byte step) { int s = step >> 1; return ValidSize(s) ? s : Small; }
    }

    /// <summary>Who sells potions and where. The server checks a purchase against this and the distance to the seller; the client window only shows the same list.</summary>
    public static class PotionShops
    {
        public const string Oakhaven = "oakhavenVillage", Sunspire = "sunspireOasis";
        /// <summary>World units a buyer may stand from the seller: the talk range plus some slack for movement the server saw last.</summary>
        public const float ReachUnits = 44f;

        /// <summary>The map a seller works on, null when this villager sells nothing.</summary>
        public static string MapOf(string npcId)
        {
            switch (npcId)
            {
                case "healerNina":
                case "merchantDale": return Oakhaven;
                case "apothecaryNara": return Sunspire;
                default: return null;
            }
        }

        public static bool IsSeller(string npcId) { return MapOf(npcId) != null; }

        /// <summary>Oakhaven stocks Small and Medium, Sunspire all three.</summary>
        public static int LargestSize(string npcId) { return MapOf(npcId) == Sunspire ? PotionCatalog.Large : PotionCatalog.Medium; }

        public static bool Sells(string npcId, int size) { return IsSeller(npcId) && PotionCatalog.ValidSize(size) && size <= LargestSize(npcId); }

        /// <summary>Null when this hero may buy: the seller stocks the size, works on the hero's map and stands in reach.</summary>
        public static string Refusal(string npcId, string mapId, int size, float distance)
        {
            if (!IsSeller(npcId) || MapOf(npcId) != mapId) return "Nobody sells that here";
            if (!Sells(npcId, size)) return "Not sold here";
            if (distance > ReachUnits) return "Too far from the shop";
            return null;
        }
    }
}

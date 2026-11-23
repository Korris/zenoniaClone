using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Card rarity, N lowest. A boss card is SSR and carries the crown flag (IsBoss), the crown is not a fifth tier.</summary>
    public enum CardRarity { N = 0, R = 1, SR = 2, SSR = 3 }

    /// <summary>What a card is good at. Decides the stat of its Power gem (ATK, DEF, HP, Utility = crit or max MP).</summary>
    public enum CardCategory { Atk = 0, Def = 1, Hp = 2, Utility = 3 }

    /// <summary>Soul Gem kind made from a card: Power (attack side) or Guard (defence side). Stored as 'p' / 'g' in item sockets.</summary>
    public enum GemKind { Power = 0, Guard = 1 }

    /// <summary>The stat a gem option adds.</summary>
    public enum GemStat { Atk, Def, MaxHp, MaxMp, Crit }

    /// <summary>One gem option: a flat stat bonus. Crit is a whole percent (3 = +3 % crit).</summary>
    public struct GemOption
    {
        public GemStat Stat;
        public int Value;
        public GemOption(GemStat stat, int value) { Stat = stat; Value = value; }
    }

    /// <summary>
    /// One monster card (pure data, no Unity). Id equals the MonsterDef type id (card of that monster); Region is a map id.
    /// Edge numbers are 1-9 (top, right, bottom, left) for the duel; the rarity fixes the total (CardTable.EdgeSumMin / Max).
    /// </summary>
    public sealed class CardDef
    {
        public readonly string Id, MonsterId, Name, Region, Element, Description;
        public readonly CardRarity Rarity;
        public readonly bool IsBoss;
        public readonly CardCategory Category;
        public readonly bool UtilityMp; // Utility cards: true = max MP, false = crit
        public readonly int Top, Right, Bottom, Left;

        public CardDef(string id, string name, string region, CardRarity rarity, bool isBoss, string element, CardCategory category, bool utilityMp, int top, int right, int bottom, int left, string description)
        {
            Id = id; MonsterId = id; Name = name; Region = region; Rarity = rarity; IsBoss = isBoss; Element = element; Category = category; UtilityMp = utilityMp;
            Top = top; Right = right; Bottom = bottom; Left = left; Description = description;
        }

        public int EdgeSum { get { return Top + Right + Bottom + Left; } }
        public int MaxEdge { get { return Math.Max(Math.Max(Top, Right), Math.Max(Bottom, Left)); } }

        /// <summary>Gem option of this card, by gem kind. Power = the category's stat; Guard = Def (Hp for a Def card). Values by rarity (GemTable).</summary>
        public GemOption Option(GemKind kind)
        {
            GemStat stat;
            if (kind == GemKind.Power)
                stat = Category == CardCategory.Atk ? GemStat.Atk : Category == CardCategory.Def ? GemStat.Def : Category == CardCategory.Hp ? GemStat.MaxHp : UtilityMp ? GemStat.MaxMp : GemStat.Crit;
            else stat = Category == CardCategory.Def ? GemStat.MaxHp : GemStat.Def;
            return new GemOption(stat, GemTable.Value(stat, Rarity));
        }
    }

    /// <summary>Gem option values by rarity (N, R, SR, SSR). One place to tune, no migration: saves keep ids, never numbers.</summary>
    public static class GemTable
    {
        static readonly int[] AtkV = { 6, 12, 20, 32 }, DefV = { 4, 8, 14, 22 }, HpV = { 40, 80, 140, 220 }, CritV = { 1, 2, 3, 5 }, MpV = { 15, 30, 50, 80 };

        public static int Value(GemStat stat, CardRarity rarity)
        {
            int i = (int)rarity;
            switch (stat)
            {
                case GemStat.Atk: return AtkV[i];
                case GemStat.Def: return DefV[i];
                case GemStat.MaxHp: return HpV[i];
                case GemStat.MaxMp: return MpV[i];
                default: return CritV[i];
            }
        }
    }
}

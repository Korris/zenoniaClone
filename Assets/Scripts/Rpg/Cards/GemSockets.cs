using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Soul Gems inside items. A socketed gem is the string "cardId:p" (Power) or "cardId:g" (Guard) in Item.sockets. How many sockets an item has depends only on its slot
    /// (weapon 2, armor 2, necklace = the "helmet" slot 1, everything else 0), never on tier. The option of a gem is a flat bonus: it is NOT multiplied by enhance or tier.
    /// </summary>
    public static class GemSockets
    {
        public static int SlotCount(string slot)
        {
            switch (slot)
            {
                case Slots.Weapon: return 2;
                case Slots.Armor: return 2;
                case Slots.Helmet: return 1;
                default: return 0;
            }
        }

        public static string Encode(string cardId, GemKind kind) { return cardId + ":" + (kind == GemKind.Power ? "p" : "g"); }

        /// <summary>A gem as one byte for the network (InvOp.GemSocket Slot): card index * 2 + 0 Power / 1 Guard (0..79).</summary>
        public static byte Code(string cardId, GemKind kind) { return (byte)(CardTable.Index(cardId) * 2 + (kind == GemKind.Guard ? 1 : 0)); }

        public static bool TryDecode(byte code, out string cardId, out GemKind kind)
        {
            CardDef card = CardTable.ByIndex(code / 2);
            cardId = card != null ? card.Id : null;
            kind = (code & 1) == 1 ? GemKind.Guard : GemKind.Power;
            return card != null;
        }

        /// <summary>Splits "meadowSlime:p". False for anything that is not a known card with p / g.</summary>
        public static bool TryParse(string gem, out string cardId, out GemKind kind)
        {
            cardId = null; kind = GemKind.Power;
            int colon = gem == null ? -1 : gem.IndexOf(':');
            if (colon <= 0 || colon != gem.Length - 2) return false;
            char k = gem[colon + 1];
            if (k != 'p' && k != 'g') return false;
            cardId = gem.Substring(0, colon);
            kind = k == 'p' ? GemKind.Power : GemKind.Guard;
            return CardTable.IsCardId(cardId);
        }

        /// <summary>The option of a socketed gem, or false when the string is not a gem.</summary>
        public static bool TryOption(string gem, out GemOption option)
        {
            string id; GemKind kind;
            option = default(GemOption);
            if (!TryParse(gem, out id, out kind)) return false;
            option = CardTable.Get(id).Option(kind);
            return true;
        }

        /// <summary>Everything the gems of one item add, as item stats (crit is a fraction like every item crit). Zero for an item with no gems. Flat: no enhance multiplier.</summary>
        public static ItemStats Sum(Item item)
        {
            var s = new ItemStats();
            if (item == null || item.sockets == null) return s;
            int n = System.Math.Min(item.sockets.Count, SlotCount(item.slot)); // sockets past the slot's count never count (a forged save)
            for (int i = 0; i < n; i++)
            {
                GemOption o;
                if (!TryOption(item.sockets[i], out o)) continue;
                switch (o.Stat)
                {
                    case GemStat.Atk: s.atk += o.Value; break;
                    case GemStat.Def: s.def += o.Value; break;
                    case GemStat.MaxHp: s.maxHp += o.Value; break;
                    case GemStat.MaxMp: s.maxMp += o.Value; break;
                    default: s.critChance += o.Value / 100f; break;
                }
            }
            return s;
        }

        public static string StatLabel(GemStat stat)
        {
            switch (stat)
            {
                case GemStat.Atk: return "ATK";
                case GemStat.Def: return "DEF";
                case GemStat.MaxHp: return "Max HP";
                case GemStat.MaxMp: return "Max MP";
                default: return "Crit";
            }
        }

        /// <summary>"ATK +6", "Crit +3%": one line for a gem option.</summary>
        public static string OptionText(GemOption o) { return StatLabel(o.Stat) + " +" + o.Value + (o.Stat == GemStat.Crit ? "%" : ""); }

        /// <summary>"Meadow Slime Power: ATK +6" for every gem of the item (the "Gem Option" lines of the item detail).</summary>
        public static List<string> Describe(Item item)
        {
            var lines = new List<string>();
            if (item == null || item.sockets == null) return lines;
            foreach (string gem in item.sockets)
            {
                string id; GemKind kind; GemOption o;
                if (!TryParse(gem, out id, out kind) || !TryOption(gem, out o)) continue;
                lines.Add(CardTable.Get(id).Name + " " + kind + ": " + OptionText(o));
            }
            return lines;
        }
    }
}

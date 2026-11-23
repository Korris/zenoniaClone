using System.Collections.Generic;
using System.Text;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>One stack of Soul Gems the character owns, free and locked copies of one card and kind merged.</summary>
    public sealed class GemStock
    {
        public CardDef Card;
        public GemKind Kind;
        public int Free, Locked;
        public int Total { get { return Free + Locked; } }
        public GemOption Option { get { return Card.Option(Kind); } }
    }

    /// <summary>What the Soul Gem screens list, read from a PlayerState (the server mirror online). Pure: no Unity types besides the lists.</summary>
    public static class GemUiModel
    {
        /// <summary>Gem stacks (free + locked merged), highest rarity first, filtered by the stat of their option.</summary>
        public static List<GemStock> Stocks(PlayerState p, GemFilter filter)
        {
            var list = new List<GemStock>();
            if (p == null) return list;
            foreach (GemEntry g in p.Cards.Gems)
            {
                CardDef c = CardTable.Get(g.id);
                if (c == null || g.n <= 0) continue;
                GemStock s = list.Find(x => x.Card == c && x.Kind == (GemKind)g.kind);
                if (s == null) { s = new GemStock { Card = c, Kind = (GemKind)g.kind }; list.Add(s); }
                if (g.locked) s.Locked += g.n; else s.Free += g.n;
            }
            list.RemoveAll(s => !GemUiArt.Matches(filter, s.Option.Stat));
            list.Sort((a, b) => a.Card.Rarity != b.Card.Rarity ? b.Card.Rarity.CompareTo(a.Card.Rarity) : CardTable.Index(a.Card.Id).CompareTo(CardTable.Index(b.Card.Id)) != 0 ? CardTable.Index(a.Card.Id).CompareTo(CardTable.Index(b.Card.Id)) : a.Kind.CompareTo(b.Kind));
            return list;
        }

        /// <summary>Cards the character owns, highest rarity first, filtered by category.</summary>
        public static List<CardDef> OwnedCards(PlayerState p, GemFilter filter)
        {
            var list = new List<CardDef>();
            if (p == null) return list;
            foreach (KeyValuePair<string, int> kv in p.Cards.Cards)
            {
                CardDef c = CardTable.Get(kv.Key);
                if (c != null && kv.Value > 0 && GemUiArt.Matches(filter, c.Category)) list.Add(c);
            }
            list.Sort((a, b) => a.Rarity != b.Rarity ? b.Rarity.CompareTo(a.Rarity) : CardTable.Index(a.Id).CompareTo(CardTable.Index(b.Id)));
            return list;
        }

        /// <summary>Items that have sockets (weapon, necklace, armor): the worn ones or the bag ones.</summary>
        public static List<Item> Gear(PlayerState p, bool worn)
        {
            var list = new List<Item>();
            if (p == null) return list;
            if (worn)
            {
                foreach (string slot in Slots.All) { Item e = p.GetEquipped(slot); if (e != null && GemSockets.SlotCount(e.slot) > 0) list.Add(e); }
                return list;
            }
            foreach (Item i in p.Inventory) if (GemSockets.SlotCount(i.slot) > 0) list.Add(i);
            return list;
        }

        /// <summary>Changes whenever anything the gem screens show changes (gold, cards, gems, sockets): the windows redraw when it differs from the last one.</summary>
        public static string StateKey(PlayerState p)
        {
            if (p == null) return "";
            var sb = new StringBuilder(128);
            sb.Append(p.Gold);
            foreach (KeyValuePair<string, int> kv in p.Cards.Cards) sb.Append('|').Append(kv.Key).Append(kv.Value).Append('/').Append(p.Cards.Locked(kv.Key));
            foreach (GemEntry g in p.Cards.Gems) sb.Append('#').Append(g.id).Append(g.kind).Append(g.locked ? 'l' : 'f').Append(g.n);
            foreach (Item i in p.Inventory) if (i.sockets != null && i.sockets.Count > 0) sb.Append('@').Append(i.uid).Append(string.Join(",", i.sockets));
            foreach (string slot in Slots.All) { Item e = p.GetEquipped(slot); if (e != null) { sb.Append('^').Append(e.uid); if (e.sockets != null && e.sockets.Count > 0) sb.Append(string.Join(",", e.sockets)); } }
            sb.Append(p.Inventory.Count);
            return sb.ToString();
        }

        /// <summary>Map name of a card's region ("Willowbrook Meadows"), else the id.</summary>
        public static string RegionName(string id) { IGameMap map = MapRegistry.Get(id); return map != null ? map.Name : id; }

        /// <summary>The socket dots of an item: gem strings in order, then empty ones up to the slot's count.</summary>
        public static int Filled(Item i) { return i == null || i.sockets == null ? 0 : System.Math.Min(i.sockets.Count, GemSockets.SlotCount(i.slot)); }
    }
}

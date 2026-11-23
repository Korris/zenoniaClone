using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    public enum DeckError { None = 0, TooMany = 1, Unknown = 2, NotEnough = 3, TooManyBoss = 4, TooManySrPlus = 5, NotFull = 6 }

    /// <summary>
    /// Deck limits of the card duel (design notes + plan question 7): 8 cards, at most 1 boss, at most 2 SR / SSR cards that are NOT bosses (a boss does not use one of the 2).
    /// A deck can only hold copies the character owns; Lock Card does not matter here (a locked card is still allowed in the deck, it just cannot be sold or converted). Pure; the server runs it on every SaveDeck and every match start.
    /// </summary>
    public static class DeckRules
    {
        public const int Size = DuelMatch.DeckSize, MaxBoss = 1, MaxSrPlus = 2;

        /// <summary>First rule a deck breaks, None when it is fine. `owned` null = no ownership check (NPC decks). `requireFull`: exactly 8 cards (a match); false lets a deck under construction be saved.</summary>
        public static DeckError Validate(IList<string> deck, CardCollection owned, bool requireFull)
        {
            if (deck == null) return DeckError.NotFull;
            if (deck.Count > Size) return DeckError.TooMany;
            if (requireFull && deck.Count < Size) return DeckError.NotFull;
            var used = new Dictionary<string, int>();
            foreach (string id in deck)
            {
                if (!CardTable.IsCardId(id)) return DeckError.Unknown;
                int n; used.TryGetValue(id, out n); used[id] = ++n;
                if (owned != null && n > owned.Count(id)) return DeckError.NotEnough;
            }
            if (Bosses(deck) > MaxBoss) return DeckError.TooManyBoss;
            if (SrPlus(deck) > MaxSrPlus) return DeckError.TooManySrPlus;
            return DeckError.None;
        }

        /// <summary>Replaces the character's saved deck (the wire form of "Save Deck" / Add to Deck / Remove). Nothing changes unless the whole deck is allowed.</summary>
        public static DeckError Save(PlayerState p, IList<string> deck)
        {
            DeckError e = Validate(deck, p.Cards, false);
            if (e != DeckError.None) return e;
            var next = new List<string>(deck); // `deck` may be p.Cards.Deck itself
            p.Cards.Deck.Clear();
            p.Cards.Deck.AddRange(next);
            return DeckError.None;
        }

        public static int Bosses(IList<string> deck) { int n = 0; foreach (string id in deck) { CardDef c = CardTable.Get(id); if (c != null && c.IsBoss) n++; } return n; }

        /// <summary>SR / SSR cards that are not bosses (the "SR+ 2/2" line).</summary>
        public static int SrPlus(IList<string> deck) { int n = 0; foreach (string id in deck) { CardDef c = CardTable.Get(id); if (c != null && !c.IsBoss && c.Rarity >= CardRarity.SR) n++; } return n; }

        /// <summary>Sum of every edge of every card: the "Deck Power" line.</summary>
        public static int Power(IList<string> deck) { int n = 0; foreach (string id in deck) { CardDef c = CardTable.Get(id); if (c != null) n += c.EdgeSum; } return n; }

        public static string Text(DeckError e)
        {
            switch (e)
            {
                case DeckError.None: return "";
                case DeckError.TooMany: return "A deck holds 8 cards at most";
                case DeckError.Unknown: return "Unknown card in the deck";
                case DeckError.NotEnough: return "You do not own more copies of that card";
                case DeckError.TooManyBoss: return "Only 1 Boss card per deck";
                case DeckError.TooManySrPlus: return "Only 2 SR / SSR cards per deck (Boss not counted)";
                default: return "The deck needs 8 cards";
            }
        }

        /// <summary>Can `id` be added to `deck` now? Empty string = yes, else the reason (used by the Add to Deck button before it asks the server).</summary>
        public static string WhyNotAdd(IList<string> deck, string id, CardCollection owned)
        {
            if (deck.Count >= Size) return "The deck is full (8 / 8)";
            var next = new List<string>(deck) { id };
            return Text(Validate(next, owned, false));
        }
    }
}

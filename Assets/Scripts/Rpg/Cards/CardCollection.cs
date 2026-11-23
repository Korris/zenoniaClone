using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// What a character owns of the card system: card copies, Soul Gems, the Album (first-ownership bits that never go away), Album claims, the duel deck
    /// and the counters later phases fill (daily, NPC first wins, rank). Pure logic; ToSave / Load map it to SaveData with sanitising.
    /// </summary>
    public sealed class CardCollection
    {
        public const int MaxGemRows = 160, MaxDeck = 8, MaxNpcFirstWins = 0xFFFF;

        readonly Dictionary<string, int> cards = new Dictionary<string, int>();
        readonly Dictionary<string, int> locks = new Dictionary<string, int>(); // copies per card id the player locked (always <= the count owned)
        readonly List<GemEntry> gems = new List<GemEntry>();
        readonly bool[] album = new bool[CardTable.Count];
        public int AlbumClaimed;                    // bit i = milestone i claimed (AlbumRules)
        public readonly List<string> Deck = new List<string>(); // duel deck, card ids (phase 05)
        public CardDailyData Daily = new CardDailyData();
        public int NpcFirstWins;                    // bit per duel NPC already beaten once (phase 05)
        public CardRankData Rank = new CardRankData();

        public int Count(string id) { int n; return id != null && cards.TryGetValue(id, out n) ? n : 0; }
        /// <summary>Copies of this card the player locked (Lock Card).</summary>
        public int Locked(string id) { int n; return id != null && locks.TryGetValue(id, out n) ? n : 0; }
        /// <summary>Copies sitting in the duel deck.</summary>
        public int InDeck(string id) { int n = 0; for (int i = 0; i < Deck.Count; i++) if (Deck[i] == id) n++; return n; }
        /// <summary>Copies that may be sold, converted or wagered: owned minus locked minus in the deck (locks and the deck never share a copy).</summary>
        public int Spare(string id) { return Math.Max(0, Count(id) - Locked(id) - InDeck(id)); }
        public bool IsFull(string id) { return Count(id) >= CardTable.MaxStack; }

        /// <summary>Locks `n` copies of an owned card (0 unlocks). Cannot lock copies that are in the deck or that are not owned. False (nothing changes) then.</summary>
        public bool SetLocked(string id, int n)
        {
            if (n < 0 || n > Count(id) - InDeck(id)) return false;
            if (n == 0) locks.Remove(id); else locks[id] = n;
            return true;
        }

        public int OwnedKinds { get { return cards.Count; } }
        public bool InAlbum(string id) { int i = CardTable.Index(id); return i >= 0 && album[i]; }
        public int AlbumCount { get { int n = 0; for (int i = 0; i < album.Length; i++) if (album[i]) n++; return n; } }
        public IEnumerable<KeyValuePair<string, int>> Cards { get { return cards; } }
        public IList<GemEntry> Gems { get { return gems; } }

        /// <summary>Adds `n` copies (capped at CardTable.MaxStack) and marks the Album. Returns true when it was the first time this card entered the Album. Unknown id: nothing happens.</summary>
        public bool Add(string id, int n = 1)
        {
            int idx = CardTable.Index(id);
            if (idx < 0 || n <= 0) return false;
            bool first = !album[idx];
            album[idx] = true;
            cards[id] = Math.Min(CardTable.MaxStack, Count(id) + n);
            return first;
        }

        /// <summary>Takes `n` copies out (sell, convert, wager). The Album keeps the card. False (and nothing changes) when fewer than n are spare (owned, not locked, not in the deck).</summary>
        public bool Remove(string id, int n = 1)
        {
            int have = Count(id);
            if (n <= 0 || Spare(id) < n) return false;
            if (have == n) cards.Remove(id); else cards[id] = have - n;
            return true;
        }

        public int GemCount(string id, GemKind kind, bool locked) { GemEntry g = FindGem(id, (int)kind, locked); return g == null ? 0 : g.n; }

        /// <summary>True when AddGem would store all `n` gems: known card, the 80-row limit not passed, the stack would not pass 999. Callers (convert, remove) ask this BEFORE they take anything away.</summary>
        public bool CanAddGem(string id, GemKind kind, bool locked, int n = 1)
        {
            if (!CardTable.IsCardId(id) || n <= 0) return false;
            GemEntry g = FindGem(id, (int)kind, locked);
            if (g == null) return gems.Count < MaxGemRows && n <= CardTable.MaxStack;
            return g.n + n <= CardTable.MaxStack;
        }

        /// <summary>Adds Soul Gems of a card. False (nothing added) when CanAddGem says no.</summary>
        public bool AddGem(string id, GemKind kind, bool locked, int n = 1)
        {
            if (!CanAddGem(id, kind, locked, n)) return false;
            GemEntry g = FindGem(id, (int)kind, locked);
            if (g == null) { g = new GemEntry { id = id, kind = (int)kind, locked = locked, n = 0 }; gems.Add(g); }
            g.n += n;
            return true;
        }

        public bool RemoveGem(string id, GemKind kind, bool locked, int n = 1)
        {
            GemEntry g = FindGem(id, (int)kind, locked);
            if (g == null || n <= 0 || g.n < n) return false;
            g.n -= n;
            if (g.n == 0) gems.Remove(g);
            return true;
        }

        GemEntry FindGem(string id, int kind, bool locked)
        {
            for (int i = 0; i < gems.Count; i++) if (gems[i].id == id && gems[i].kind == kind && gems[i].locked == locked) return gems[i];
            return null;
        }

        /// <summary>Writes every card field into the save.</summary>
        public void ToSave(SaveData d)
        {
            d.cards = new List<CardEntry>();
            foreach (KeyValuePair<string, int> kv in cards) d.cards.Add(new CardEntry { id = kv.Key, n = kv.Value, locked = Locked(kv.Key) });
            d.gems = new List<GemEntry>();
            foreach (GemEntry g in gems) d.gems.Add(new GemEntry { id = g.id, kind = g.kind, locked = g.locked, n = g.n });
            d.album = new List<int>();
            for (int i = 0; i < album.Length; i++) if (album[i]) d.album.Add(i);
            d.albumClaimed = AlbumClaimed;
            d.cardDeck = new List<string>(Deck);
            d.cardDaily = new CardDailyData { day = Daily.day, npcWins = Daily.npcWins, pvpRewards = Daily.pvpRewards, wagers = Daily.wagers };
            d.cardNpcFirstWins = NpcFirstWins;
            d.cardRank = new CardRankData { season = Rank.season, mmr = Rank.mmr, lp = Rank.lp, wins = Rank.wins, losses = Rank.losses, draws = Rank.draws, best = Rank.best };
        }

        /// <summary>Reads the card fields of a (parsed) save, dropping anything unknown or out of range. Missing fields (old saves) = nothing owned. Returns this.</summary>
        public CardCollection Load(SaveData d)
        {
            cards.Clear(); locks.Clear(); gems.Clear(); Deck.Clear(); Array.Clear(album, 0, album.Length);
            foreach (CardEntry e in d.cards ?? new List<CardEntry>())
                if (e != null && CardTable.IsCardId(e.id) && e.n > 0) { cards[e.id] = Math.Min(CardTable.MaxStack, Count(e.id) + e.n); locks[e.id] = Locked(e.id) + Math.Max(0, e.locked); }
            foreach (string id in new List<string>(locks.Keys)) { int k = Math.Min(locks[id], Count(id)); if (k > 0) locks[id] = k; else locks.Remove(id); } // never more locked than owned
            foreach (GemEntry g in d.gems ?? new List<GemEntry>())
                if (g != null && g.n > 0 && (g.kind == 0 || g.kind == 1)) // a stack over 999 (forged / merged rows) is cut at 999
                {
                    int room = CardTable.MaxStack - GemCount(g.id, (GemKind)g.kind, g.locked);
                    if (room > 0) AddGem(g.id, (GemKind)g.kind, g.locked, Math.Min(g.n, room));
                }
            foreach (int i in d.album ?? new List<int>()) if (i >= 0 && i < album.Length) album[i] = true;
            foreach (string id in cards.Keys) album[CardTable.Index(id)] = true; // owning a card always means it is in the Album
            AlbumClaimed = d.albumClaimed & ((1 << AlbumRules.Milestones.Length) - 1);
            for (int m = 0; m < AlbumRules.Milestones.Length; m++) if (AlbumCount < AlbumRules.Milestones[m]) AlbumClaimed &= ~(1 << m); // a claim without the cards behind it is a forged one
            var used = new Dictionary<string, int>();
            foreach (string id in d.cardDeck ?? new List<string>())
            {
                int u; used.TryGetValue(id ?? "", out u);
                if (Deck.Count < MaxDeck && u < Count(id) - Locked(id)) { Deck.Add(id); used[id] = u + 1; } // only cards still owned, as many as owned and not locked
            }
            CardDailyData dy = d.cardDaily ?? new CardDailyData();
            Daily = new CardDailyData { day = Math.Max(0, dy.day), npcWins = Math.Max(0, dy.npcWins), pvpRewards = Math.Max(0, dy.pvpRewards), wagers = Math.Max(0, dy.wagers) };
            NpcFirstWins = RpgMath.Clamp(d.cardNpcFirstWins, 0, MaxNpcFirstWins);
            CardRankData r = d.cardRank ?? new CardRankData();
            Rank = new CardRankData { season = Math.Max(0, r.season), mmr = Math.Max(0, r.mmr), lp = Math.Max(0, r.lp), wins = Math.Max(0, r.wins), losses = Math.Max(0, r.losses), draws = Math.Max(0, r.draws), best = Math.Max(0, r.best) };
            return this;
        }
    }
}

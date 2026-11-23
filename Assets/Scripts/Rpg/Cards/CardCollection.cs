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
        readonly HashSet<string> locked = new HashSet<string>(); // card ids the player locked (Lock Card): the flag belongs to the KIND, so copies that drop later are locked too
        readonly List<GemEntry> gems = new List<GemEntry>();
        readonly bool[] album = new bool[CardTable.Count];
        public int AlbumClaimed;                    // bit i = milestone i claimed (AlbumRules)
        public readonly List<string> Deck = new List<string>(); // duel deck, card ids (phase 05)
        public CardDailyData Daily = new CardDailyData();
        public int NpcFirstWins;                    // bit per duel NPC already beaten once (phase 05)
        public CardRankData Rank = new CardRankData();

        public int Count(string id) { int n; return id != null && cards.TryGetValue(id, out n) ? n : 0; }
        /// <summary>Lock Card is on for this kind: none of its copies can be sold or converted (they can still go in the duel deck).</summary>
        public bool IsLocked(string id) { return id != null && locked.Contains(id); }
        /// <summary>Copies sitting in the duel deck.</summary>
        public int InDeck(string id) { int n = 0; for (int i = 0; i < Deck.Count; i++) if (Deck[i] == id) n++; return n; }
        /// <summary>Copies that may be sold, converted or wagered: none when the kind is locked, else owned minus the copies in the duel deck.</summary>
        public int Spare(string id) { return IsLocked(id) ? 0 : Math.Max(0, Count(id) - InDeck(id)); }
        public bool IsFull(string id) { return Count(id) >= CardTable.MaxStack; }

        /// <summary>Turns Lock Card on or off for an owned card. False (nothing changes) when the card is not owned.</summary>
        public bool SetLocked(string id, bool on)
        {
            if (Count(id) < 1) return false;
            if (on) locked.Add(id); else locked.Remove(id);
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

        // ---- card stakes of a PvP duel (the zone copies what server-api does to the stored character: services/card-wager-service.js) ----

        /// <summary>Takes the whole duel deck out of the collection (one copy per deck slot; the deck list itself stays so the cards can come back). Returns the kinds whose Lock flag went with their last copy.</summary>
        public List<string> EscrowDeck()
        {
            var lockedBack = new List<string>();
            foreach (string id in new List<string>(Deck))
            {
                int have = Count(id);
                if (have < 1) continue;
                if (have == 1)
                {
                    cards.Remove(id);
                    if (locked.Remove(id) && !lockedBack.Contains(id)) lockedBack.Add(id);
                }
                else cards[id] = have - 1;
            }
            return lockedBack;
        }

        /// <summary>Gives cards back / adds the card taken from the loser (one copy per id in the list). A kind that comes back after its last copy left gets its Lock flag again when it is in `lockedKinds`. Then the deck is cut to the copies owned.</summary>
        public void ReturnCards(IList<string> ids, IList<string> lockedKinds)
        {
            foreach (string id in ids)
            {
                bool existed = Count(id) > 0;
                Add(id);
                if (!existed && lockedKinds != null && lockedKinds.Contains(id)) locked.Add(id);
            }
            TrimDeck();
        }

        /// <summary>Drops deck slots that have no copy behind them (the card the winner took leaves the loser's deck; the deck page then shows 7 / 8).</summary>
        public void TrimDeck()
        {
            var left = new Dictionary<string, int>(cards);
            for (int i = 0; i < Deck.Count; i++)
            {
                int n; left.TryGetValue(Deck[i], out n);
                if (n < 1) { Deck.RemoveAt(i); i--; } else left[Deck[i]] = n - 1;
            }
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
            foreach (KeyValuePair<string, int> kv in cards) d.cards.Add(new CardEntry { id = kv.Key, n = kv.Value, locked = IsLocked(kv.Key) ? 1 : 0 });
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
            cards.Clear(); locked.Clear(); gems.Clear(); Deck.Clear(); Array.Clear(album, 0, album.Length);
            foreach (CardEntry e in d.cards ?? new List<CardEntry>())
                if (e != null && CardTable.IsCardId(e.id) && e.n > 0) { cards[e.id] = Math.Min(CardTable.MaxStack, Count(e.id) + e.n); if (e.locked > 0) locked.Add(e.id); }
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
                if (Deck.Count < MaxDeck && u < Count(id)) { Deck.Add(id); used[id] = u + 1; } // only cards still owned, as many copies as owned (a locked card may be in the deck)
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

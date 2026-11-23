using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The Ranked waiting line (phase 07), pure: no clock of its own, no networking. Players are matched by their hidden MMR: |difference| must be within 150, and the window grows by 100 for every 30 s the
    /// LONGER-waiting of the two has waited. Never matched: the same party (the game has no friend list yet; a party is the closest thing), a player who is not Ready right now (in a fight, say: they keep their place),
    /// or the opponent they just met (10 minutes, `RematchCooldownMs`). The longest waiter is served first and gets the closest MMR among those it may be matched with.
    /// </summary>
    public sealed class RankedQueue
    {
        public const int BaseWindow = 150, WidenStep = 100;
        public const long WidenEveryMs = 30000, DefaultRematchCooldownMs = 600000;
        /// <summary>How long two players that just met are kept from meeting again (test servers shorten it).</summary>
        public long RematchCooldownMs = DefaultRematchCooldownMs;

        public sealed class Waiter
        {
            public ushort Id;
            public string Key;
            /// <summary>The account behind the character (the key itself when the server has no accounts): two characters of one account never meet, and the rematch cooldown is per account pair.</summary>
            public string Account;
            public int Mmr, Party;
            public long Since;
            public bool Ready = true;
        }

        public struct Pair { public Waiter A, B; }

        readonly List<Waiter> line = new List<Waiter>();
        readonly Dictionary<string, long> recent = new Dictionary<string, long>();

        public int Count { get { return line.Count; } }

        public bool Contains(ushort id) { return Find(id) != null; }

        public Waiter Find(ushort id) { foreach (Waiter w in line) if (w.Id == id) return w; return null; }

        /// <summary>Adds a player at `nowMs`. False when already in the line.</summary>
        public bool Add(ushort id, string key, int mmr, int party, long nowMs, string account = null)
        {
            if (Find(id) != null) return false;
            line.Add(new Waiter { Id = id, Key = key ?? "", Account = string.IsNullOrEmpty(account) ? key ?? "" : account, Mmr = mmr, Party = party, Since = nowMs });
            return true;
        }

        public bool Remove(ushort id)
        {
            Waiter w = Find(id);
            return w != null && line.Remove(w);
        }

        public long WaitedMs(ushort id, long nowMs) { Waiter w = Find(id); return w == null ? 0L : Math.Max(0L, nowMs - w.Since); }

        public List<Waiter> All() { return new List<Waiter>(line); }

        /// <summary>The MMR distance a player who has waited `waitedMs` accepts: 150, +100 every 30 s.</summary>
        public static int Window(long waitedMs) { return BaseWindow + WidenStep * (int)(Math.Max(0L, waitedMs) / WidenEveryMs); }

        static string PairKey(string a, string b) { return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a; }

        bool Allowed(Waiter a, Waiter b, long nowMs)
        {
            if (a.Key == b.Key || a.Account == b.Account) return false; // never the same character, nor two characters of one account
            if (a.Party != 0 && a.Party == b.Party) return false;
            long met;
            if (recent.TryGetValue(PairKey(a.Account, b.Account), out met) && nowMs - met < RematchCooldownMs) return false;
            int window = Math.Max(Window(nowMs - a.Since), Window(nowMs - b.Since));
            return Math.Abs(a.Mmr - b.Mmr) <= window;
        }

        /// <summary>Makes every match that is possible right now: the longest waiter first, each with its closest-MMR partner. The matched players leave the line and the pair is noted for the rematch cooldown.</summary>
        public List<Pair> Match(long nowMs)
        {
            var made = new List<Pair>();
            var order = new List<Waiter>(line);
            order.Sort((x, y) => x.Since.CompareTo(y.Since));
            var used = new HashSet<Waiter>();
            foreach (Waiter a in order)
            {
                if (used.Contains(a) || !a.Ready) continue;
                Waiter best = null;
                foreach (Waiter b in order)
                {
                    if (b == a || used.Contains(b) || !b.Ready || !Allowed(a, b, nowMs)) continue;
                    if (best == null || Math.Abs(b.Mmr - a.Mmr) < Math.Abs(best.Mmr - a.Mmr)) best = b;
                }
                if (best == null) continue;
                used.Add(a); used.Add(best);
                made.Add(new Pair { A = a, B = best });
                recent[PairKey(a.Account, best.Account)] = nowMs;
            }
            foreach (Pair p in made) { line.Remove(p.A); line.Remove(p.B); }
            if (recent.Count > 256) Forget(nowMs);
            return made;
        }

        /// <summary>The pair was not played after all (a player was no longer eligible): they may be matched again at once.</summary>
        public void ForgetPair(Waiter a, Waiter b) { recent.Remove(PairKey(a.Account, b.Account)); }

        void Forget(long nowMs)
        {
            var old = new List<string>();
            foreach (KeyValuePair<string, long> kv in recent) if (nowMs - kv.Value >= RematchCooldownMs) old.Add(kv.Key);
            foreach (string k in old) recent.Remove(k);
        }
    }
}

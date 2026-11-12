using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Pays out the damage shares of one kill once parties are counted (math in PartyExp). What a party's members earned by damage goes in one pot, split among those on the zone within
    /// 400 units of the kill. A member who hit the monster always shares, however far away it stands by then: it must not lose EXP it earned. Dead members and members on other zones get nothing.
    /// </summary>
    public static class PartyKillShare
    {
        static readonly List<HeroController> Sharing = new List<HeroController>(PartyBook.MaxMembers);
        static readonly HashSet<int> Paid = new HashSet<int>();

        /// <summary>Pays `exp[i]` of each winner: partyless winners in full, parties through their pot. `winners` and `exp` run in parallel.</summary>
        public static void Pay(Zone zone, Monster m, List<HeroController> winners, int[] exp)
        {
            PartyService service = PartyService.Active;
            PartyBook book = service != null ? service.Book : null;
            Paid.Clear();
            for (int i = 0; i < winners.Count; i++)
            {
                HeroController w = winners[i];
                int party = book != null ? book.PartyIdOf(w.PlayerId) : 0;
                if (party == 0)
                {
                    if (exp[i] > 0 || winners.Count == 1) w.Owner.GainExp(exp[i], m.Feet);
                    continue;
                }
                if (Paid.Add(party)) PayParty(zone, m, book, party, w, winners, exp);
            }
        }

        static void PayParty(Zone zone, Monster m, PartyBook book, int party, HeroController anyMember, List<HeroController> winners, int[] exp)
        {
            int pool = 0, top = -1;
            for (int i = 0; i < winners.Count; i++)
            {
                if (book.PartyIdOf(winners[i].PlayerId) != party) continue;
                pool += exp[i];
                if (top < 0 || exp[i] > exp[top]) top = i;
            }
            Sharing.Clear();
            AddSharing(zone, m, book, party, winners[top], winners, Sharing); // first: it takes what does not divide evenly
            int[] shares = PartyExp.Split(pool, Sharing.Count);
            for (int i = 0; i < shares.Length; i++) if (shares[i] > 0) Sharing[i].Owner.GainExp(shares[i], m.Feet);
            if (NetStats.Current != null) NetStats.Current.Add("partyExpShares", 1);
            Debug.Log("[ZenoniaProto] party: " + m.DisplayName + " paid " + pool + " EXP to party " + party + " as " + string.Join(" + ", shares) + " (" + Sharing.Count + " sharing)");
        }

        /// <summary>
        /// Adds who shares one party's pot (and, for the Wanted Boss, who took part): `first`, then every other member alive on the zone with an RPG owner who hit the monster
        /// or stands within 400 units of it. One definition for both, so the reward set can never drift from the EXP set.
        /// </summary>
        static void AddSharing(Zone zone, Monster m, PartyBook book, int party, HeroController first, List<HeroController> winners, List<HeroController> into)
        {
            into.Add(first);
            IReadOnlyList<HeroController> here = zone.Heroes.All;
            for (int i = 0; i < here.Count; i++)
            {
                HeroController h = here[i];
                if (h == null || h == first || h.IsDead || h.Owner == null || book.PartyIdOf(h.PlayerId) != party) continue;
                Vector2 d = h.Position - m.Feet;
                if (winners.Contains(h) || PartyExp.InRange(d.x, d.y)) into.Add(h);
            }
        }

        static readonly HashSet<int> SeenParties = new HashSet<int>();

        /// <summary>Everyone who took part in this kill: the winners plus the in-range members of their parties, each hero once. `into` is cleared first and must not be the Sharing list.</summary>
        public static void CollectParticipants(Zone zone, Monster m, List<HeroController> winners, List<HeroController> into)
        {
            into.Clear();
            PartyService service = PartyService.Active;
            PartyBook book = service != null ? service.Book : null;
            SeenParties.Clear();
            for (int i = 0; i < winners.Count; i++)
            {
                HeroController w = winners[i];
                int party = book != null ? book.PartyIdOf(w.PlayerId) : 0;
                if (party == 0) { if (!into.Contains(w)) into.Add(w); continue; }
                if (!SeenParties.Add(party)) continue;
                Sharing.Clear();
                AddSharing(zone, m, book, party, w, winners, Sharing);
                for (int k = 0; k < Sharing.Count; k++) if (!into.Contains(Sharing[k])) into.Add(Sharing[k]);
            }
        }
    }
}

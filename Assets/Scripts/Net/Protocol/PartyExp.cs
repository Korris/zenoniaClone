namespace ZenoniaProto.Net
{
    /// <summary>
    /// EXP sharing math (plan phase 06): members on the zone within 400 units split evenly, and the pot grows 10 % per member sharing. A lone member gets no bonus: nobody is
    /// sharing, and a free 10 % would pay for parking a friend in town.
    /// </summary>
    public static class PartyExp
    {
        public const float ShareRangeUnits = 400f;
        public const int BonusPctPerMember = 10;

        public static bool InRange(float dx, float dy) { return dx * dx + dy * dy <= ShareRangeUnits * ShareRangeUnits; }

        public static int BonusPct(int members) { return members < 2 ? 0 : members * BonusPctPerMember; }

        /// <summary>What `members` heroes share from `exp`: the bonus is rounded down.</summary>
        public static int Pot(int exp, int members)
        {
            if (exp <= 0 || members <= 0) return 0;
            return (int)(exp * (100L + BonusPct(members)) / 100L);
        }

        /// <summary>Each member's share of the pot; what does not divide evenly goes to the first entry, so the caller lists the top damage dealer first.</summary>
        public static int[] Split(int exp, int members)
        {
            if (members <= 0) return new int[0];
            var shares = new int[members];
            int pot = Pot(exp, members), each = pot / members;
            for (int i = 0; i < members; i++) shares[i] = each;
            shares[0] += pot - each * members;
            return shares;
        }
    }
}

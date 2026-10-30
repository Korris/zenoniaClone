using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>Per-monster damage table (hero NetId -> damage dealt): the basis of the EXP split and of who owns the loot.</summary>
    public partial class Monster
    {
        readonly Dictionary<ushort, int> damageBy = new Dictionary<ushort, int>(4);

        public IReadOnlyDictionary<ushort, int> DamageBy { get { return damageBy; } }

        void Credit(HeroController src, int dealt)
        {
            if (src == null || src.NetId == 0 || dealt <= 0) return;
            int sum;
            damageBy.TryGetValue(src.NetId, out sum);
            damageBy[src.NetId] = sum + dealt;
        }
    }
}

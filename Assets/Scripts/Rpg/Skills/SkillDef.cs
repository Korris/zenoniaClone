using System;

namespace ZenoniaProto.Rpg
{
    public struct SkillReq
    {
        public int Index, Rank;
        public SkillReq(int index, int rank) { Index = index; Rank = rank; }
    }

    /// <summary>One skill of the tree (design: plans/261030-1508-skill-tree-design). Values are rank 1; MP grows per rank, cooldown is fixed.</summary>
    public sealed class SkillDef
    {
        public const int MaxRank = 10;
        public const int PerClass = 8;
        public const float DefaultDamageGrowth = 0.12f;

        public string Id, Class, Name;
        public int Index;                       // 0-7 = icon / preview order R1C1..R2C4
        public int UnlockLevel;
        public SkillReq[] Prereqs = new SkillReq[0];
        public int Mp, MpPerRank;
        public int CooldownMs, CastLockMs = 350;
        public SkillTargeting Targeting;
        public float TargetRange;               // tiles: NearestEnemy / LowestAlly search radius or blink range
        public SkillEffect[] Effects = new SkillEffect[0];
        public LegacySkill Legacy;
        public float DamageGrowth = DefaultDamageGrowth; // +12% of the base value per rank above 1

        public SkillTier Tier { get { return TierOf(UnlockLevel); } }

        /// <summary>Basic below Lv 20, Advanced 20-39, Ultimate from 40.</summary>
        public static SkillTier TierOf(int level) { return level < 20 ? SkillTier.Basic : level < 40 ? SkillTier.Advanced : SkillTier.Ultimate; }

        public int MpAt(int rank) { return Mp + MpPerRank * (Math.Max(1, rank) - 1); }

        /// <summary>Damage / heal / shield multiplier of a rank (rank 1 = 1.0).</summary>
        public float DamageMul(int rank) { return 1f + DamageGrowth * (Math.Max(1, rank) - 1); }

        /// <summary>Damage / heal % ATK of an effect at a rank.</summary>
        public float PctAt(SkillEffect e, int rank) { return e.Pct * DamageMul(rank); }

        /// <summary>Buff value at a rank: shields scale like damage; ATK / speed / next-hit buffs are fixed.</summary>
        public float BuffAt(SkillEffect e, int rank) { return e.Buff.Kind == BuffKind.Shield ? e.Buff.Value * DamageMul(rank) : e.Buff.Value; }
    }
}

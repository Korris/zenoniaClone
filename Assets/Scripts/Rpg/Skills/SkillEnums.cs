namespace ZenoniaProto.Rpg
{
    public enum SkillTier { Basic, Advanced, Ultimate }

    /// <summary>How the cast resolves its aim / target on the authority (the client only sends "slot pressed").</summary>
    public enum SkillTargeting { Facing, NearestEnemy, Self, LowestAlly, AroundSelf }

    public enum AreaShape { Single, Circle, Cone, Arc, Line }

    /// <summary>The composable building blocks a skill timeline is made of (executor: phase 03).</summary>
    public enum EffectKind { Strike, Projectile, Motion, Buff, Heal, Summon, Rain, Zone }

    public enum StatusKind { None, Slow, Freeze, Stun, Burn, Poison, Mark, Knockback, Launch, Chill }

    public enum BuffKind { None, AtkPct, AtkSpeedPct, Shield, NextHitsPct, DefPct } // sent as a number: append only

    public enum MotionKind { None, Dash, Blink, Leap }

    /// <summary>Skills kept as hand-written adapters (existing ultimates); their SkillEffect list is empty.</summary>
    public enum LegacySkill { None, EarthBreak, IceStorm, RocketBarrage, BladeCyclone }
}

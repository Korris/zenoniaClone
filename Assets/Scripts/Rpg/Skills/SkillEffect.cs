namespace ZenoniaProto.Rpg
{
    /// <summary>Status applied to a monster by a hit. Value meaning depends on Kind (slow %, burn / poison % ATK per second, mark % damage taken, knockback tiles).</summary>
    public struct StatusSpec
    {
        public StatusKind Kind; public float Value; public int Ms, Charges;
        public StatusSpec(StatusKind kind, float value, int ms, int charges = 0) { Kind = kind; Value = value; Ms = ms; Charges = charges; }
    }

    /// <summary>Buff on a hero: AtkPct / AtkSpeedPct / NextHitsPct in percent, Shield in % ATK of the caster.</summary>
    public struct BuffSpec
    {
        public BuffKind Kind; public float Value; public int Ms;
        public BuffSpec(BuffKind kind, float value, int ms) { Kind = kind; Value = value; Ms = ms; }
    }

    /// <summary>
    /// One step of a skill timeline (flat record; unused fields stay default). Distances are in TILES (x GameConfig.TileUnits at runtime), angles in degrees,
    /// Pct in percent of the caster's ATK. Plain engine-free data: shared by server, client and the dotnet tests.
    /// </summary>
    public sealed class SkillEffect
    {
        public EffectKind Kind;
        public int AtMs;                          // start offset inside the cast timeline
        public int Hits = 1, IntervalMs;          // repeated strikes
        public AreaShape Shape;
        public float Range, Radius, Length, Width, AngleDeg; // Range = distance of the area centre from the caster (0 = on the caster)
        public float Pct;                         // damage or heal, % ATK (scaled by SkillDef.DamageMul)
        public StatusSpec Status;                 // applied to every monster hit
        public BuffSpec Buff;
        public bool Attached;                     // Zone: follows the caster instead of staying on the ground
        public bool Ally;                         // Heal / Buff / Zone: affects heroes in the area instead of monsters
        public int Count = 1;                     // projectiles / rain drops; for a Strike with Pct 0 and a Status: max monsters affected
        public float SpreadDeg, Speed, ImpactRadius; // Speed = tiles/s; ImpactRadius > 0: projectile / rain drop explodes in a circle of that radius
        public bool Pierce, Homing;
        public int DurationMs;                    // Summon / Zone lifetime, Motion travel time
        public MotionKind Motion; public float MoveTiles;
        public int BonusVsMask; public float BonusPct, BehindBonusPct; // BonusVsMask: bit (1 << StatusKind) of statuses that grant BonusPct
        public string Vfx, Sfx;                   // art / audio keys (phase 06+)

        public static int StatusBit(StatusKind k) { return 1 << (int)k; }
    }
}

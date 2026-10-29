namespace ZenoniaProto
{
    /// <summary>Stat block copied from the web monster-definitions (speed px/s, ranges px, times ms).</summary>
    public sealed class MonsterDef
    {
        public readonly string Sheet;       // Resources/Sprites/monsters/<Sheet>.png (+ "-attack")
        public readonly float Hp, Atk, Def, Speed;
        public readonly float AggroRange, LeashRange, AttackRange, WindupMs, CooldownMs;
        public readonly float HitW, HitH;
        public readonly float BodyHeightUnits; // painted body height, for the HP bar (web bodyHeight px / 8)

        public MonsterDef(string sheet, float hp, float atk, float def, float speed, float aggro, float leash,
            float attackRange, float windupMs, float cooldownMs, float hitW, float hitH, float bodyHeightPx)
        {
            Sheet = sheet; Hp = hp; Atk = atk; Def = def; Speed = speed; AggroRange = aggro; LeashRange = leash;
            AttackRange = attackRange; WindupMs = windupMs; CooldownMs = cooldownMs; HitW = hitW; HitH = hitH;
            BodyHeightUnits = bodyHeightPx / GameConfig.FramePpu;
        }

        public static readonly MonsterDef IceGoblin = new MonsterDef("iceGoblin", 58f, 11f, 3f, 44f, 70f, 150f, 12f, 350f, 1200f, 12f, 10f, 152f);
        public static readonly MonsterDef FrostSlime = new MonsterDef("frostSlime", 36f, 7f, 2f, 30f, 70f, 150f, 12f, 350f, 1200f, 12f, 8f, 104f);
    }
}

namespace ZenoniaProto
{
    /// <summary>Stat block copied from the web monster-definitions (speed px/s, ranges px, times ms).</summary>
    public sealed class MonsterDef
    {
        public readonly string Sheet;       // Resources/Sprites/monsters/<Sheet>.png (+ "-attack")
        public readonly string Name;
        public readonly float Hp, Atk, Def, Speed;
        public readonly float AggroRange, LeashRange, AttackRange, WindupMs, CooldownMs;
        public readonly float HitW, HitH;
        public readonly float BodyHeightUnits; // painted body height, for the HP bar (web bodyHeight px / 8)
        public readonly int ItemLevel;
        public float RenderScale = 1f;         // boss draws larger (web renderScale)
        public bool Stationary;                // web stationaryUntilAggro: waits at its post until the hero comes close
        public bool IsBoss;

        public MonsterDef(string name, string sheet, float hp, float atk, float def, float speed, int itemLevel, float aggro = 70f, float leash = 150f,
            float attackRange = 12f, float windupMs = 350f, float cooldownMs = 1200f, float hitW = 12f, float hitH = 10f, float bodyHeightPx = 152f)
        {
            Name = name; Sheet = sheet; Hp = hp; Atk = atk; Def = def; Speed = speed; ItemLevel = itemLevel;
            AggroRange = aggro; LeashRange = leash; AttackRange = attackRange; WindupMs = windupMs; CooldownMs = cooldownMs;
            HitW = hitW; HitH = hitH;
            BodyHeightUnits = bodyHeightPx / GameConfig.FramePpu;
        }

        // web monster-definitions.js (Frost Hollow block) + asset-atlas-hd-monsters.js bodyHeight
        public static readonly MonsterDef FrostSlime = new MonsterDef("Frost Slime", "frostSlime", 36f, 7f, 2f, 30f, 1, hitH: 8f, bodyHeightPx: 104f);
        public static readonly MonsterDef IceGoblin = new MonsterDef("Ice Goblin", "iceGoblin", 58f, 11f, 3f, 44f, 2, bodyHeightPx: 152f);
        public static readonly MonsterDef SpectralWolf = new MonsterDef("Spectral Wolf", "spectralWolf", 48f, 10f, 1f, 62f, 2, aggro: 95f, cooldownMs: 900f, hitW: 14f, hitH: 8f, bodyHeightPx: 156f);
        public static readonly MonsterDef FrostfangKing = new MonsterDef("Frostfang King", "frostfangKing", 580f, 33f, 3f, 36f, 3, aggro: 85f, leash: 120f,
            attackRange: 20f, windupMs: 650f, cooldownMs: 1500f, hitW: 22f, hitH: 14f, bodyHeightPx: 170f) { RenderScale = 1.65f, Stationary = true, IsBoss = true };

        public static MonsterDef ByType(string type)
        {
            switch (type)
            {
                case "frostSlime": return FrostSlime;
                case "iceGoblin": return IceGoblin;
                case "spectralWolf": return SpectralWolf;
                case "frostfangKing": return FrostfangKing;
                default: return null;
            }
        }
    }
}

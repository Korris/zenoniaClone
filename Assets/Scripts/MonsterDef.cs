using UnityEngine;

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
        public string FallbackSheet;           // existing sheet drawn (tinted) until Resources/Sprites/monsters/<Sheet>.png lands; see Monster.LoadSheets
        public Color FallbackTint = Color.white;

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

        // Amber Dunes (Lv 20-27, no boss). Frost Slime = Lv 1 and Ice Goblin = Lv 2 (their ItemLevel) give the line HP 14+22L, Atk 3+4L, Def 1+L;
        // each block takes its mid level and a role factor; the fast ones also take Spectral Wolf's low Def, speed and short cooldown.
        public static readonly MonsterDef DuneScarab = new MonsterDef("Dune Scarab", "duneScarab", 380f, 70f, 7f, 62f, 21, aggro: 95f, cooldownMs: 900f, hitH: 8f, bodyHeightPx: 102f)
            { FallbackSheet = "frostSlime", FallbackTint = new Color(1f, 0.78f, 0.45f) };
        public static readonly MonsterDef SandRaider = new MonsterDef("Sand Raider", "sandRaider", 760f, 100f, 40f, 44f, 24, bodyHeightPx: 146f)
            { FallbackSheet = "iceGoblin", FallbackTint = new Color(1f, 0.82f, 0.6f) };
        public static readonly MonsterDef AmberWisp = new MonsterDef("Amber Wisp", "amberWisp", 500f, 125f, 9f, 72f, 26, aggro: 110f, leash: 170f, windupMs: 300f, cooldownMs: 850f, bodyHeightPx: 114f)
            { FallbackSheet = "spectralWolf", FallbackTint = new Color(1f, 0.72f, 0.3f) };

        // Emberfen Marsh (Lv 27-35, no boss) and Sunken Reliquary (Lv 33-35): the same line and role factors. The fast ones take Amber Wisp's timings; Ruin Wisp is melee because the
        // monster AI has no ranged attacks. Bosses (Frostfang Den Lv 20, Reliquary Lv 35) take the line at their level with the king's HP factor (about x7) and Def factor (x0.75), the king's
        // timings, and Atk x1.5: his Atk factor (x2.2) would two-shot a hero of that level, because the line grows faster than hero HP.
        public static readonly MonsterDef GlacierWarden = new MonsterDef("Glacier Warden", "glacierWarden", 3200f, 125f, 16f, 36f, 20, aggro: 85f, leash: 120f,
            attackRange: 20f, windupMs: 650f, cooldownMs: 1500f, hitW: 22f, hitH: 14f, bodyHeightPx: 164f) { RenderScale = 1.65f, Stationary = true, IsBoss = true, FallbackSheet = "frostfangKing", FallbackTint = new Color(0.55f, 0.8f, 1f) };
        public static readonly MonsterDef BogSlime = new MonsterDef("Bog Slime", "bogSlime", 505f, 85f, 12f, 30f, 28, hitH: 8f, bodyHeightPx: 97f)
            { FallbackSheet = "frostSlime", FallbackTint = new Color(0.62f, 0.9f, 0.5f) };
        public static readonly MonsterDef MireToad = new MonsterDef("Mire Toad", "mireToad", 840f, 125f, 40f, 44f, 31, bodyHeightPx: 110f)
            { FallbackSheet = "iceGoblin", FallbackTint = new Color(0.6f, 0.85f, 0.45f) };
        public static readonly MonsterDef MarshStalker = new MonsterDef("Marsh Stalker", "marshStalker", 650f, 165f, 10f, 72f, 34, aggro: 110f, leash: 170f, windupMs: 300f, cooldownMs: 850f, hitW: 14f, hitH: 8f, bodyHeightPx: 144f)
            { FallbackSheet = "spectralWolf", FallbackTint = new Color(0.55f, 0.75f, 0.4f) };
        public static readonly MonsterDef RuinWisp = new MonsterDef("Ruin Wisp", "ruinWisp", 610f, 155f, 12f, 62f, 34, aggro: 95f, cooldownMs: 900f, bodyHeightPx: 119f)
            { FallbackSheet = "spectralWolf", FallbackTint = new Color(0.55f, 0.9f, 0.85f) };
        public static readonly MonsterDef MirefangHydra = new MonsterDef("Mirefang Hydra", "mirefangHydra", 5500f, 215f, 27f, 36f, 35, aggro: 95f, leash: 140f,
            attackRange: 24f, windupMs: 650f, cooldownMs: 1500f, hitW: 26f, hitH: 16f, bodyHeightPx: 166f) { RenderScale = 1.85f, Stationary = true, IsBoss = true, FallbackSheet = "frostfangKing", FallbackTint = new Color(0.5f, 0.85f, 0.55f) };

        /// <summary>Wire index of a definition (Spawn message Type byte): append only, existing indices must not move.</summary>
        public static readonly MonsterDef[] All = { FrostSlime, IceGoblin, SpectralWolf, FrostfangKing, DuneScarab, SandRaider, AmberWisp, GlacierWarden, BogSlime, MireToad, MarshStalker, RuinWisp, MirefangHydra };
        public static int IndexOf(MonsterDef d) { return System.Array.IndexOf(All, d); }
        public static MonsterDef ByIndex(int i) { return i >= 0 && i < All.Length ? All[i] : null; }

        public static MonsterDef ByType(string type)
        {
            switch (type)
            {
                case "frostSlime": return FrostSlime;
                case "iceGoblin": return IceGoblin;
                case "spectralWolf": return SpectralWolf;
                case "frostfangKing": return FrostfangKing;
                case "duneScarab": return DuneScarab;
                case "sandRaider": return SandRaider;
                case "amberWisp": return AmberWisp;
                case "glacierWarden": return GlacierWarden;
                case "bogSlime": return BogSlime;
                case "mireToad": return MireToad;
                case "marshStalker": return MarshStalker;
                case "ruinWisp": return RuinWisp;
                case "mirefangHydra": return MirefangHydra;
                default: return null;
            }
        }
    }
}

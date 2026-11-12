using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Stat block copied from the web monster-definitions (speed px/s, ranges px, times ms).</summary>
    public sealed class MonsterDef
    {
        public readonly string Sheet;       // Resources/Sprites/monsters/<Sheet>.png (+ "-attack")
        public readonly string Name;
        public readonly float Atk, Def, Speed;
        readonly float hpBase;
        /// <summary>Max HP. An event boss (IsWanted) takes the WantedHpScale test override (--wanted-hp-scale, 1 in production); no other monster does.</summary>
        public float Hp { get { return IsWanted ? hpBase * WantedHpScale : hpBase; } }
        public static float WantedHpScale = 1f;
        public readonly float AggroRange, LeashRange, AttackRange, WindupMs, CooldownMs;
        public readonly float HitW, HitH;
        public readonly float BodyHeightUnits; // painted body height, for the HP bar (web bodyHeight px / 8)
        public readonly int ItemLevel;
        public float RenderScale = 1f;         // boss draws larger (web renderScale)
        public bool Stationary;                // web stationaryUntilAggro: waits at its post until the hero comes close
        public bool IsBoss;
        public bool IsWanted;                  // daily Wanted Boss (event raid boss): behaves like a boss (HP bar, stationary, no shoving) but is not the map boss: no boss flags, own respawn clock (WantedBossSpawn)
        public int WalkCellPx = 192, AttackCellPx = 256; // sheet cell sizes; event bosses draw 384 / 512 cells at doubled pixels per unit (Monster.LoadSheets)
        public string FallbackSheet;           // existing sheet drawn (tinted) until Resources/Sprites/monsters/<Sheet>.png lands; see Monster.LoadSheets
        public Color FallbackTint = Color.white;

        public MonsterDef(string name, string sheet, float hp, float atk, float def, float speed, int itemLevel, float aggro = 70f, float leash = 150f,
            float attackRange = 12f, float windupMs = 350f, float cooldownMs = 1200f, float hitW = 12f, float hitH = 10f, float bodyHeightPx = 152f)
        {
            // Farm balance: double health for every monster, including bosses, on both authority and client.
            Name = name; Sheet = sheet; hpBase = hp * 2f; Atk = atk; Def = def; Speed = speed; ItemLevel = itemLevel;
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

        // Willowbrook meadow monsters (sheets from Tools/build-meadow-monsters.py), same line HP 14+22L, Atk 3+4L, Def 1+L, EXP 14L, gold L..2L+1.
        // Horn Rabbit (Lv 2): low HP and Def, fast, short cooldown, like Spectral Wolf. Thorn Boar (Lv 3): heavy (HP x1.3, Atk x1.15), slower than Ice Goblin, long windup, wide hitbox, low aggro.
        public static readonly MonsterDef HornRabbit = new MonsterDef("Horn Rabbit", "hornRabbit", 46f, 10f, 1f, 64f, 2, aggro: 90f, cooldownMs: 900f, hitW: 12f, hitH: 8f, bodyHeightPx: 128f);
        public static readonly MonsterDef ThornBoar = new MonsterDef("Thorn Boar", "thornBoar", 104f, 17f, 4f, 40f, 3, aggro: 55f, attackRange: 16f, windupMs: 450f, cooldownMs: 1400f, hitW: 18f, hitH: 10f, bodyHeightPx: 180f);
        // Lv 1-6 line: Meadow Slime = Frost Slime x0.9 (weak, slow). Meadow Mantis (Lv 5): fast and sharp (HP x0.8, Atk x1.3, Def 2), like Spectral Wolf timings.
        // Bramble Treant (Lv 6): tanky and slow (HP x1.8, Atk x1.3, Def 10), long windup and cooldown, wide hitbox; drawn 1.2x because the 192 walk cell caps the sheet at about boar height
        // (RenderScale also scales the HP bar and name position).
        public static readonly MonsterDef MeadowSlime = new MonsterDef("Meadow Slime", "meadowSlime", 32f, 6f, 1f, 28f, 1, hitH: 8f, bodyHeightPx: 112f);
        public static readonly MonsterDef MeadowMantis = new MonsterDef("Meadow Mantis", "meadowMantis", 100f, 30f, 2f, 68f, 5, aggro: 100f, cooldownMs: 900f, windupMs: 300f, hitW: 12f, hitH: 8f, bodyHeightPx: 178f);
        public static readonly MonsterDef BrambleTreant = new MonsterDef("Bramble Treant", "brambleTreant", 263f, 35f, 10f, 28f, 6, aggro: 55f, attackRange: 18f, windupMs: 650f, cooldownMs: 1700f, hitW: 22f, hitH: 14f, bodyHeightPx: 172f)
            { RenderScale = 1.2f };

        // Frost Hollow ice monsters (Lv 7-9, the large field's far side), same line HP 14+22L, Atk 3+4L, Def 1+L, EXP 14L. Real sheets come later (Tools/build-ice-monsters.py); until then the
        // fallback sheet is tinted. Ice Wraith: fast and brittle (HP x0.8, Atk x1.2, Def 4) with Spectral Wolf timings. Frostbite Yeti: heavy (HP x1.4, Atk x1.2, Def 9), slow, long windup, wide hitbox.
        // Crystal Golem: tank (HP x1.8, Atk x1.1, Def 16), slowest, longest windup and cooldown. RenderScale also scales the HP bar and name position.
        public static readonly MonsterDef IceWraith = new MonsterDef("Ice Wraith", "iceWraith", 134f, 37f, 4f, 66f, 7, aggro: 100f, cooldownMs: 900f, windupMs: 300f, hitW: 12f, hitH: 8f, bodyHeightPx: 140f)
            { FallbackSheet = "spectralWolf", FallbackTint = new Color(0.7f, 0.9f, 1f) };
        public static readonly MonsterDef FrostbiteYeti = new MonsterDef("Frostbite Yeti", "frostbiteYeti", 266f, 42f, 9f, 34f, 8, aggro: 60f, attackRange: 18f, windupMs: 600f, cooldownMs: 1500f, hitW: 22f, hitH: 14f, bodyHeightPx: 176f)
            { RenderScale = 1.2f, FallbackSheet = "iceGoblin", FallbackTint = new Color(0.95f, 0.97f, 1f) };
        public static readonly MonsterDef CrystalGolem = new MonsterDef("Crystal Golem", "crystalGolem", 382f, 43f, 16f, 26f, 9, aggro: 50f, attackRange: 18f, windupMs: 700f, cooldownMs: 1800f, hitW: 24f, hitH: 14f, bodyHeightPx: 168f)
            { RenderScale = 1.25f, FallbackSheet = "frostSlime", FallbackTint = new Color(0.45f, 0.7f, 1f) };

        // Frost Hollow middle tiers (Lv 3-5, the fields between the old valley and the deep ice), same line. Snow Fox (Lv 3): fast and brittle (HP x0.8, Atk x1.2, Def 2) with Spectral Wolf timings; its sheet is
        // installed. Ice Imp (Lv 4): middle weight (Atk x1.15, Def 5), quicker than the goblin, short reach. Frost Bear (Lv 5): tanky and slow (HP x1.5, Atk x1.2, Def 8), long windup and cooldown, wide hitbox,
        // drawn 1.15x. Ice Imp and Frost Bear draw a tinted fallback sheet until their own sheets land (python Tools/build-meadow-monsters.py iceImp|frostBear).
        public static readonly MonsterDef SnowFox = new MonsterDef("Snow Fox", "snowFox", 64f, 18f, 2f, 66f, 3, aggro: 100f, cooldownMs: 900f, windupMs: 300f, hitW: 12f, hitH: 8f, bodyHeightPx: 123f);
        public static readonly MonsterDef IceImp = new MonsterDef("Ice Imp", "iceImp", 102f, 22f, 5f, 50f, 4, aggro: 80f, attackRange: 14f, windupMs: 400f, cooldownMs: 1100f, hitW: 14f, hitH: 9f, bodyHeightPx: 138f);
        public static readonly MonsterDef FrostBear = new MonsterDef("Frost Bear", "frostBear", 186f, 28f, 8f, 32f, 5, aggro: 55f, attackRange: 18f, windupMs: 600f, cooldownMs: 1500f, hitW: 22f, hitH: 14f, bodyHeightPx: 178f)
            { RenderScale = 1.15f };

        // Daily Wanted Bosses (event raid bosses, WantedBoss.Rotation): big stationary monsters hit by many players at once, so HP is FIXED (never scales with the player count).
        // One table to tune: HP = WantedHpFactor x the map boss of the level band (ctor input, so the farm x2 applies to both: King 580 -> 17400 declared), Atk = WantedAtkFactor x that boss,
        // drawn WantedRenderScale (about 3 hero bodies: 164 px of a 192 cell = 20.5 units, 3 x = 61.5 = a 170 px boss body x 2.9) with a hit box and reach to match. Art lands later as
        // Sprites/monsters/<type>.png (4x4 cells of 384) + <type>-attack.png (4x4 cells of 512, feet 448 px down); until then the map boss's sheet is drawn tinted.
        public const float WantedHpFactor = 30f, WantedAtkFactor = 1.5f, WantedRenderScale = 4.5f, WantedHitW = 78f, WantedHitH = 45f, WantedAttackRange = 66f; // 4.5 hero bodies since 2026-11-12 (user: 3 was still too small); hit box and reach grew with it
        static MonsterDef Wanted(string name, string sheet, MonsterDef basis, int itemLevel, Color tint)
        {
            return new MonsterDef(name, sheet, basis.hpBase / 2f * WantedHpFactor, basis.Atk * WantedAtkFactor, basis.Def, basis.Speed, itemLevel, aggro: 110f, leash: 160f,
                attackRange: WantedAttackRange, windupMs: 800f, cooldownMs: 1800f, hitW: WantedHitW, hitH: WantedHitH, bodyHeightPx: 164f)
                { RenderScale = WantedRenderScale, Stationary = true, IsBoss = true, IsWanted = true, WalkCellPx = 384, AttackCellPx = 512, FallbackSheet = "frostfangKing", FallbackTint = tint };
        }
        public static readonly MonsterDef RimeclawTyrant = Wanted("Rimeclaw Tyrant", "rimeclawTyrant", FrostfangKing, 10, new Color(0.75f, 0.9f, 1f));
        public static readonly MonsterDef GlacialColossus = Wanted("Glacial Colossus", "glacialColossus", GlacierWarden, 25, new Color(0.4f, 0.65f, 1f));
        public static readonly MonsterDef MireHydraQueen = Wanted("Mire Hydra Queen", "mireHydraQueen", MirefangHydra, 40, new Color(0.45f, 0.95f, 0.5f));

        /// <summary>Wire index of a definition (Spawn message Type byte): append only, existing indices must not move.</summary>
        public static readonly MonsterDef[] All = { FrostSlime, IceGoblin, SpectralWolf, FrostfangKing, DuneScarab, SandRaider, AmberWisp, GlacierWarden, BogSlime, MireToad, MarshStalker, RuinWisp, MirefangHydra, HornRabbit, ThornBoar, MeadowSlime, MeadowMantis, BrambleTreant, IceWraith, FrostbiteYeti, CrystalGolem, SnowFox, IceImp, FrostBear, RimeclawTyrant, GlacialColossus, MireHydraQueen };
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
                case "hornRabbit": return HornRabbit;
                case "thornBoar": return ThornBoar;
                case "meadowSlime": return MeadowSlime;
                case "meadowMantis": return MeadowMantis;
                case "brambleTreant": return BrambleTreant;
                case "iceWraith": return IceWraith;
                case "frostbiteYeti": return FrostbiteYeti;
                case "crystalGolem": return CrystalGolem;
                case "snowFox": return SnowFox;
                case "iceImp": return IceImp;
                case "frostBear": return FrostBear;
                case "rimeclawTyrant": return RimeclawTyrant;
                case "glacialColossus": return GlacialColossus;
                case "mireHydraQueen": return MireHydraQueen;
                default: return null;
            }
        }
    }
}

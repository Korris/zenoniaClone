using System.Collections.Generic;

namespace ZenoniaProto
{
    public enum HeroClass { Warrior, Wizard, Mechanic }

    /// <summary>One held-gear layer (web held-gear-specs `parts`): manifest key, overlay PNG folder and web drawScale / minSx.</summary>
    public sealed class GearPartSpec
    {
        public readonly string Id, Folder;
        public readonly float DrawScale, MinSx;
        public readonly bool Glow; // additive copy of the enhanced art (weapon only)

        public GearPartSpec(string id, string folder, float drawScale = 1f, float minSx = 0f, bool glow = false)
        {
            Id = id; Folder = folder; DrawScale = drawScale; MinSx = minSx; Glow = glow;
        }
    }

    /// <summary>
    /// Playable class table: web class-definitions.js base stats, HD sheet folders + side-row rules (asset-atlas-hd-*.js),
    /// held-gear parts, combo chain (hero-stats-config.js COMBO_CONFIG_BY_CLASS) and the FX art folder.
    /// </summary>
    public sealed class HeroClassDef
    {
        public HeroClass Id;
        public string Name, Tagline, ComboLabel;
        public string SheetFolder, BareFolder, GearFile, FxFolder;
        public string[] BareAnims;
        public HashSet<string> MirrorLeft;     // anims whose left row is the right row mirrored
        public float MaxHp, Atk, Def, Speed;
        public ComboHit[] Hits;
        public float ChainMs;
        public GearPartSpec[] Parts;
        public bool HasRobot;
        public bool Ranged { get { return Hits[0].Bolt != null; } }

        static readonly string[] AllBare = { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt" };

        public static readonly HeroClassDef Warrior = new HeroClassDef
        {
            Id = HeroClass.Warrior, Name = "Warrior", Tagline = "Sword master. Tough, fast combos up close.", ComboLabel = "4-hit sword combo",
            SheetFolder = "hero", BareFolder = "bare", GearFile = "held-gear", FxFolder = null,
            BareAnims = AllBare, MirrorLeft = new HashSet<string>(),
            MaxHp = 100f, Atk = 12f, Def = 4f, Speed = 80f, Hits = ComboConfigs.WarriorHits, ChainMs = 400f,
            Parts = new[] { new GearPartSpec("shield", "shield"), new GearPartSpec("sword", "sword", 1.4f, 0.85f, true) },
        };

        public static readonly HeroClassDef Wizard = new HeroClassDef
        {
            Id = HeroClass.Wizard, Name = "Wizard", Tagline = "Spellcaster. Strikes from range with magic bolts.", ComboLabel = "3 magic bolts + arcane burst",
            SheetFolder = "wizard", BareFolder = "wizard-bare", GearFile = "held-wizard", FxFolder = "wizard",
            BareAnims = new[] { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt", "death" },
            MirrorLeft = new HashSet<string> { "attack1", "attack2" },
            MaxHp = 80f, Atk = 13f, Def = 3f, Speed = 80f, Hits = ComboConfigs.WizardHits, ChainMs = 450f,
            Parts = new[] { new GearPartSpec("staff", "wizard-staff", 1f, 0f, true) },
        };

        public static readonly HeroClassDef Mechanic = new HeroClassDef
        {
            Id = HeroClass.Mechanic, Name = "Mechanic", Tagline = "Gunslinging engineer. Rapid shots, cannon finisher, robot buddy.", ComboLabel = "3 rapid shots + cannon blast",
            SheetFolder = "mechanic", BareFolder = "mechanic-bare", GearFile = "held-mechanic", FxFolder = "mechanic",
            BareAnims = new[] { "idle", "walk", "attack1", "attack3", "dash", "hurt" }, // attack2 / death: baked armed art
            MirrorLeft = new HashSet<string> { "attack3", "dash", "hurt", "death" },
            MaxHp = 90f, Atk = 12f, Def = 4f, Speed = 82f, Hits = ComboConfigs.MechanicHits, ChainMs = 420f,
            Parts = new[] { new GearPartSpec("gun", "mechanic-gun", 1f, 0f, true) }, HasRobot = true,
        };

        public static readonly HeroClassDef[] All = { Warrior, Wizard, Mechanic };

        public static HeroClassDef Get(HeroClass id) { return All[(int)id]; }
    }
}

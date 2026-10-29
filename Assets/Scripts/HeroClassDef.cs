using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    public enum HeroClass { Warrior, Wizard, Mechanic, Assassin, Cleric }

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
        public string CompanionFolder, CompanionFloatFolder; // floating companion (mechanic robot / cleric spirit); null = none
        public float CompanionScale = 0.26f;                  // web companion-specs scale: frame px per art px
        public float CritBonus;                                // added to the base crit chance (assassin)
        public ClassPalette Palette = ClassPalette.Neutral;   // colour identity of the basic-attack FX
        /// <summary>Palette in use right now (the mechanic can switch orange / blue with C).</summary>
        public ClassPalette ActivePalette { get { return Id == HeroClass.Mechanic && ClassPalette.MechanicBlue ? ClassPalette.Blue : Palette; } }
        public float MaxMp = 60f;
        public string SkillName;                       // class signature skill on L (null = none)
        public float SkillMp, SkillCooldownMs, SkillLockMs;
        public bool Ranged { get { return Hits[0].Bolt != null; } }

        static readonly string[] AllBare = { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt" };

        public static readonly HeroClassDef Warrior = new HeroClassDef
        {
            Id = HeroClass.Warrior, Name = "Warrior", Tagline = "Sword master. Tough, fast combos up close.", ComboLabel = "4-hit sword combo",
            SheetFolder = "hero", BareFolder = "bare", GearFile = "held-gear", FxFolder = null, Palette = ClassPalette.Red,
            BareAnims = AllBare, MirrorLeft = new HashSet<string>(),
            MaxHp = 100f, Atk = 12f, Def = 4f, Speed = 80f, Hits = ComboConfigs.WarriorHits, ChainMs = 400f,
            Parts = new[] { new GearPartSpec("shield", "shield"), new GearPartSpec("sword", "sword", 1.4f, 0.85f, true) },
            SkillName = "Earth Break", SkillMp = 29f, SkillCooldownMs = 8000f, SkillLockMs = 1450f,
        };

        public static readonly HeroClassDef Wizard = new HeroClassDef
        {
            Id = HeroClass.Wizard, Name = "Wizard", Tagline = "Spellcaster. Strikes from range with magic bolts.", ComboLabel = "3 magic bolts + arcane burst",
            SheetFolder = "wizard", BareFolder = "wizard-bare", GearFile = "held-wizard", FxFolder = "wizard", Palette = ClassPalette.Arcane,
            BareAnims = new[] { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt", "death" },
            MirrorLeft = new HashSet<string> { "attack1", "attack2" },
            MaxHp = 80f, Atk = 13f, Def = 3f, Speed = 80f, Hits = ComboConfigs.WizardHits, ChainMs = 450f,
            Parts = new[] { new GearPartSpec("staff", "wizard-staff", 1f, 0f, true) },
            SkillName = "Ice Storm", SkillMp = 20f, SkillCooldownMs = 7700f, SkillLockMs = 650f,
        };

        public static readonly HeroClassDef Mechanic = new HeroClassDef
        {
            Id = HeroClass.Mechanic, Name = "Mechanic", Tagline = "Gunslinging engineer. Rapid shots, cannon finisher, robot buddy.", ComboLabel = "3 rapid shots + cannon blast",
            SheetFolder = "mechanic", BareFolder = "mechanic-bare", GearFile = "held-mechanic", FxFolder = "mechanic", Palette = ClassPalette.Orange,
            BareAnims = new[] { "idle", "walk", "attack1", "attack3", "dash", "hurt" }, // attack2 / death: baked armed art
            MirrorLeft = new HashSet<string> { "attack3", "dash", "hurt", "death" },
            MaxHp = 90f, Atk = 12f, Def = 4f, Speed = 82f, Hits = ComboConfigs.MechanicHits, ChainMs = 420f,
            Parts = new[] { new GearPartSpec("gun", "mechanic-gun", 1f, 0f, true) },
            CompanionFolder = "mechanic-robot", CompanionFloatFolder = "mechanic-robot-float", CompanionScale = 0.26f,
        };

        // Assassin: two daggers (offhand part drawn behind, main hand in front), warrior slash FX recoloured purple, double-stab finisher.
        public static readonly HeroClassDef Assassin = new HeroClassDef
        {
            Id = HeroClass.Assassin, Name = "Assassin", Tagline = "Dual-dagger striker. Speed, crits and shadow strikes.", ComboLabel = "3 slashes + double stab",
            SheetFolder = "assassin", BareFolder = "assassin-bare", GearFile = "held-assassin", FxFolder = null, Palette = ClassPalette.Violet,
            BareAnims = new[] { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt" }, // death: baked art
            MirrorLeft = new HashSet<string>(),
            MaxHp = 88f, MaxMp = 50f, Atk = 13f, Def = 3f, Speed = 92f, CritBonus = 0.06f, Hits = ComboConfigs.AssassinHits, ChainMs = 380f,
            Parts = new[] { new GearPartSpec("off", "assassin-dagger", 1f, 0f, true), new GearPartSpec("main", "assassin-dagger", 1f, 0f, true) },
            SkillName = "Shadow Step", SkillMp = 9f, SkillCooldownMs = 3900f, SkillLockMs = 260f,
        };

        // Cleric: holy staff + spirit companion, green-gold recoloured wizard orbs.
        public static readonly HeroClassDef Cleric = new HeroClassDef
        {
            Id = HeroClass.Cleric, Name = "Cleric", Tagline = "Holy support caster. Heals, with a spirit companion and holy light.", ComboLabel = "3 holy orbs + burst",
            SheetFolder = "cleric", BareFolder = "cleric-bare", GearFile = "held-cleric", FxFolder = "wizard", Palette = ClassPalette.Green,
            BareAnims = new[] { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt" }, // death: baked art
            MirrorLeft = new HashSet<string>(),
            MaxHp = 85f, MaxMp = 75f, Atk = 11f, Def = 4f, Speed = 80f, Hits = ComboConfigs.ClericHits, ChainMs = 450f,
            Parts = new[] { new GearPartSpec("staff", "cleric-staff", 1f, 0f, true) },
            CompanionFolder = "cleric-spirit", CompanionFloatFolder = "cleric-spirit-float", CompanionScale = 0.3f,
            SkillName = "Heal", SkillMp = 12f, SkillCooldownMs = 8500f, SkillLockMs = 350f,
        };

        public static readonly HeroClassDef[] All = { Warrior, Wizard, Mechanic, Assassin, Cleric };

        public static HeroClassDef Get(HeroClass id) { return All[(int)id]; }
    }
}

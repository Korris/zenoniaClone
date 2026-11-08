using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    public enum HeroClass { Warrior, Wizard, Mechanic, Assassin, Cleric, Fighter } // append only: saves and net packets carry the int

    /// <summary>One held-gear layer (web held-gear-specs `parts`): manifest key, overlay PNG folder and web drawScale / minSx.</summary>
    public sealed class GearPartSpec
    {
        public readonly string Id, Folder;
        public readonly float DrawScale, MinSx;
        public readonly bool Glow; // enhance effect includes stars + motes (every part gets the shine and halo)
        /// <summary>Optional part (the wizard's spellbook): a frame without its anchor, or with a hidden / fallback one, just does not draw it;
        /// every other part's missing anchor sends the whole frame to the baked armed art.</summary>
        public readonly bool Optional;
        /// <summary>Drawn from the offhand slot (warrior shield, assassin off-hand dagger, wizard spellbook); every other part is the weapon.</summary>
        public bool IsOffhand { get { return Id == "shield" || Id == "off" || Id == "cannon" || Id == "book"; } }

        public GearPartSpec(string id, string folder, float drawScale = 1f, float minSx = 0f, bool glow = false, bool optional = false)
        {
            Id = id; Folder = folder; DrawScale = drawScale; MinSx = minSx; Glow = glow; Optional = optional;
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
        public string HandFolder; // only the weapon fist, drawn above the weapon; null = none
        public string[] HandAnims; // anims with a sheet in HandFolder; a blank frame there draws nothing
        public string BigIdleDown; // idle facing down with art wider than the frame (Tools/build-idle-down-set.py); null = the armed row
        public string[] BareAnims;
        public string[] ExtraAnims;            // skill poses only this class has (null = none); loaded armed + bare like the rest
        public int RisingStep;                 // combo step whose slash FX rises (arc on the weapon side, low to high); 0 = none (HeroFx.RisingArc)
        public string[] ComboAnims;            // per combo step anim (null = attack1 / attack2 alternate + attack3 finisher); "meleeN" = Tools/build-melee-sheets.py output
        public HashSet<string> MirrorLeft;     // anims whose left row is the right row mirrored
        public float MaxHp, Atk, Def, Speed;
        public ComboHit[] Hits;
        public float ChainMs;
        public GearPartSpec[] Parts;
        public string CompanionFolder, CompanionFloatFolder; // floating companion (mechanic robot / cleric spirit); null = none
        public bool CompanionGrounded;                        // stands on the ground beside the hero (mechanic turret): no hover, no bob, static art, faces the hero's way
        public float CompanionScale = 0.26f;                  // web companion-specs scale: frame px per art px
        public float CritBonus;                                // added to the base crit chance (assassin)
        public ClassPalette Palette = ClassPalette.Neutral;   // colour identity of the basic-attack FX
        /// <summary>Palette in use right now (the mechanic can switch orange / blue with C).</summary>
        public ClassPalette ActivePalette { get { return Id == HeroClass.Mechanic && ClassPalette.MechanicBlue ? ClassPalette.Blue : Palette; } }
        public float MaxMp = 60f;
        public float WalkCycleMs = 560f;               // one full step cycle, whatever the sheet's frame count
        public string SkillName;                       // class signature skill on L (null = none)
        public float SkillMp, SkillCooldownMs, SkillLockMs;
        /// <summary>Touch button art: index in Resources/Sprites/classes/&lt;class&gt;-N.png (web SKILL_ORDER slot; see Tools/build-skill-icons.py).</summary>
        public int SkillIcon = -1, DashIcon = -1;
        public string IconClass { get { return Name.ToLowerInvariant(); } }
        public bool Ranged { get { return Hits[0].Bolt != null; } }

        static readonly string[] AllBare = { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt" };

        public static readonly HeroClassDef Warrior = new HeroClassDef
        {
            Id = HeroClass.Warrior, Name = "Warrior", Tagline = "Sword master. Tough, fast combos up close.", ComboLabel = "4-hit sword combo",
            BigIdleDown = "classes/warrior/body/armed/idle-down-big", SheetFolder = "classes/warrior/body/armed", BareFolder = "classes/warrior/body/bare", HandFolder = "classes/warrior/body/hand", HandAnims = new[] { "idle", "walk", "hurt", "attack3", "melee1", "melee2", "melee3", "melee4", "melee1lr", "melee2rl", "melee3up" }, GearFile = "classes/warrior/held-gear", FxFolder = null, Palette = ClassPalette.Red,
            BareAnims = AllBare, MirrorLeft = new HashSet<string>(),
            ComboAnims = new[] { "melee1lr", "melee2rl", "melee3up", "melee4" }, RisingStep = 3, // slash left -> right, slash right -> left, rising slash, jump cleave (Tools/build-warrior-combo-directions.py; old melee1-3 kept, Earth Break borrows melee1)
            ExtraAnims = new[] { InfernoCrashUltimate.PoseAnim, BerserkerAwakeningFx.PoseAnim, "melee1", "melee3" }, // melee1: Earth Break swing, melee3: Blade Cyclone spin (no longer combo hits)
            MaxHp = 100f, Atk = 12f, Def = 4f, Speed = 80f, Hits = ComboConfigs.WarriorHits, ChainMs = 400f, WalkCycleMs = 640f, // 4 pose walk sheet, slowed so each pose reads; the feet slide a little at Speed 80
            Parts = new[] { new GearPartSpec("shield", "classes/warrior/weapons/shield", 0.8f), new GearPartSpec("sword", "classes/warrior/weapons/sword", 2f, 0.85f, true) },
            SkillName = "Earth Break", SkillMp = 29f, SkillCooldownMs = 8000f, SkillLockMs = 1450f, SkillIcon = 4, DashIcon = 7, // bladeStorm, dashStrike
        };

        public static readonly HeroClassDef Wizard = new HeroClassDef
        {
            Id = HeroClass.Wizard, Name = "Wizard", Tagline = "Spellcaster. Strikes from range with magic bolts.", ComboLabel = "3 magic bolts + arcane burst",
            SheetFolder = "classes/wizard/body/armed", BareFolder = "classes/wizard/body/bare", HandFolder = "classes/wizard/body/hand", HandAnims = new[] { "idle", "walk", "hurt", "attack1", "attack2", "attack3", "attack4" }, GearFile = "classes/wizard/held-wizard", FxFolder = "classes/wizard/fx/combat", Palette = ClassPalette.Arcane,
            BareAnims = new[] { "idle", "walk", "attack1", "attack2", "attack3", "attack4", "dash", "hurt", "death" },
            // Board 2026-11-08 (docs/reference/wizard-basic-attack-board.png, Tools/build-wizard-attack-board-repaint.py): Arcane Thrust (staff),
            // Frost Page (book), Twin Sigil (staff raised + book; also the skill cast pose), Astral Nova (both hands, the bursting orb).
            // Every facing has its own row: the staff stays in the right hand, the book in the left when facing left.
            ComboAnims = new[] { "attack1", "attack2", "attack3", "attack4" },
            MirrorLeft = new HashSet<string>(),
            MaxHp = 80f, Atk = 13f, Def = 3f, Speed = 80f, Hits = ComboConfigs.WizardHits, ChainMs = 450f,
            Parts = new[] { new GearPartSpec("staff", "classes/wizard/weapons/staff", 1f, 0f, true), new GearPartSpec("book", "classes/wizard/weapons/book", 1f, 0f, false, true) }, // spellbook: offhand, anchored on idle facing down only (Tools/build-wizard-tier-staff-book.py)
            SkillName = "Ice Storm", SkillMp = 20f, SkillCooldownMs = 7700f, SkillLockMs = 650f, SkillIcon = 9, DashIcon = 12, // blizzard, blink
        };

        public static readonly HeroClassDef Mechanic = new HeroClassDef
        {
            Id = HeroClass.Mechanic, Name = "Mechanic", Tagline = "Gunslinging engineer. Rapid shots, cannon finisher, a turret at his side.", ComboLabel = "3 rapid shots + cannon blast",
            SheetFolder = "classes/mechanic/body/armed", BareFolder = "classes/mechanic/body/bare", HandFolder = "classes/mechanic/body/hand", HandAnims = new[] { "idle", "walk", "attack1", "attack2", "attack3", "attack4", "rocket", "dash", "hurt" }, GearFile = "classes/mechanic/held-mechanic-dual", FxFolder = "classes/mechanic/fx/combat", Palette = ClassPalette.Yellow,
            BareAnims = new[] { "idle", "walk", "attack1", "attack2", "attack3", "attack4", "dash", "hurt" }, // death: baked armed art
            ComboAnims = new[] { "attack1", "attack2", "attack3", "attack4" }, // left pistol, right pistol, both, cannon off the back (Tools/build-mechanic-dual.py; way back in its docstring)
            ExtraAnims = new[] { MechanicRocketPose.PoseAnim },
            MirrorLeft = new HashSet<string> { "attack3", "attack4", "rocket", "dash", "hurt", "death" },
            MaxHp = 90f, Atk = 12f, Def = 4f, Speed = 82f, Hits = ComboConfigs.MechanicHits, ChainMs = 420f,
            Parts = new[] { new GearPartSpec("cannon", "classes/mechanic/weapons/cannon", 1f, 0f, true), new GearPartSpec("gunL", "classes/mechanic/weapons/pistol", 1f, 0f, true), new GearPartSpec("gun", "classes/mechanic/weapons/pistol", 1f, 0f, true) }, // both pistols are the one weapon item; the cannon is the offhand, its art per facing in the anchors
            CompanionFolder = null, CompanionFloatFolder = null, CompanionScale = 0.26f, // the floating robot is off for the two-pistol trial
            DashIcon = 12, // jetBoots
        };

        // Assassin: two daggers (offhand part drawn behind, main hand in front), warrior slash FX recoloured purple, double-stab finisher.
        public static readonly HeroClassDef Assassin = new HeroClassDef
        {
            Id = HeroClass.Assassin, Name = "Assassin", Tagline = "Dual-dagger striker. Speed, crits and shadow strikes.", ComboLabel = "3 slashes + double stab",
            SheetFolder = "classes/assassin/body/armed", BareFolder = "classes/assassin/body/bare", HandFolder = "classes/assassin/body/hand", HandAnims = new[] { "idle", "walk", "hurt", "melee1", "melee2", "melee3", "melee4" }, GearFile = "classes/assassin/held-assassin", FxFolder = null, Palette = ClassPalette.Violet,
            BareAnims = new[] { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt" }, // death: baked art
            MirrorLeft = new HashSet<string>(),
            ExtraAnims = new[] { NightfallExecutionUltimate.PoseAnim },
            ComboAnims = new[] { "melee1", "melee2", "melee3", "melee4" }, // forehand slash, rising backhand, jumping X chop, dash double stab (Tools/build-assassin-new-style.py)
            MaxHp = 88f, MaxMp = 50f, Atk = 13f, Def = 3f, Speed = 92f, CritBonus = 0.06f, Hits = ComboConfigs.AssassinHits, ChainMs = 380f,
            Parts = new[] { new GearPartSpec("off", "classes/assassin/weapons/dagger", 1.3f, 0f, true), new GearPartSpec("main", "classes/assassin/weapons/dagger", 1.3f, 0f, true) }, // 1.3 = daggers ~40% of body height (keep Tools/build-melee-sheets.py draw_scale in sync)
            SkillName = "Shadow Step", SkillMp = 9f, SkillCooldownMs = 3900f, SkillLockMs = 260f, SkillIcon = 0, DashIcon = 10, // shadowStep, evasion
        };

        // Cleric: holy staff + soft holy shield (offhand), green-gold recoloured wizard orbs.
        public static readonly HeroClassDef Cleric = new HeroClassDef
        {
            Id = HeroClass.Cleric, Name = "Cleric", Tagline = "Holy support caster. Heals, with a holy shield and holy light.", ComboLabel = "3 holy orbs + burst",
            SheetFolder = "classes/cleric/body/armed", BareFolder = "classes/cleric/body/bare", HandFolder = "classes/cleric/body/hand", HandAnims = new[] { "idle", "walk", "hurt", "attack1", "attack2", "attack3", "attack4" }, GearFile = "classes/cleric/held-cleric", FxFolder = "classes/wizard/fx/combat", Palette = ClassPalette.Green,
            ExtraAnims = new[] { ClericBuffFx.PoseAnim },
            BareAnims = new[] { "idle", "walk", "attack1", "attack2", "attack3", "attack4", "dash", "hurt" }, // death: baked art
            ComboAnims = new[] { "attack1", "attack2", "attack3", "attack4" }, // Light Thrust, Aegis Ray, Halo Lance, Judgment Bloom (board docs/reference/cleric-basic-attack-board.png, Tools/build-cleric-attack-body.py)
            MirrorLeft = new HashSet<string>(),
            MaxHp = 85f, MaxMp = 75f, Atk = 11f, Def = 4f, Speed = 80f, Hits = ComboConfigs.ClericHits, ChainMs = 450f,
            Parts = new[] { new GearPartSpec("shield", "classes/cleric/weapons/shield", 0.8f), new GearPartSpec("staff", "classes/cleric/weapons/staff", 1f, 0f, true) },
            SkillName = "Heal", SkillMp = 12f, SkillCooldownMs = 8500f, SkillLockMs = 350f, SkillIcon = 5, DashIcon = 12, // heal, hasteHymn
        };

        // Fighter (the hero-select "boxing" slot, 2026-11-04): a pair of gloves is the one weapon item (`off` glove drawn behind, `main` in front, like the
        // Assassin's daggers), no hand overlay (the glove covers the fist), four punches jab / cross / hook / uppercut on melee1..melee4, orange FX.
        // Body sheets + glove art land from Tools/build-fighter-new-style.py / build-fighter-gloves.py; until then the renderer logs missing sheets and draws nothing.
        public static readonly HeroClassDef Fighter = new HeroClassDef
        {
            Id = HeroClass.Fighter, Name = "Fighter", Tagline = "Bare-knuckle brawler. Fast punches, a crushing uppercut.", ComboLabel = "jab, cross, hook + uppercut",
            SheetFolder = "classes/fighter/body/armed", BareFolder = "classes/fighter/body/bare", HandFolder = null, HandAnims = null, GearFile = "classes/fighter/held-fighter", FxFolder = null, Palette = ClassPalette.Orange,
            BareAnims = new[] { "idle", "walk", "dash", "hurt" }, // melee1..4 load bare too (HeroRenderer: every "melee*" anim); death: baked art
            MirrorLeft = new HashSet<string>(),
            ComboAnims = new[] { "melee1", "melee2", "melee3", "melee4" }, // jab, cross, hook, uppercut finisher (Tools/build-fighter-new-style.py)
            MaxHp = 95f, MaxMp = 50f, Atk = 13f, Def = 4f, Speed = 88f, CritBonus = 0.04f, Hits = ComboConfigs.FighterHits, ChainMs = 360f,
            Parts = new[] { new GearPartSpec("off", "classes/fighter/weapons/glove", 1f, 0f, true), new GearPartSpec("main", "classes/fighter/weapons/glove", 1f, 0f, true) }, // anchor s ~0.18: glove ~1.6x a bare fist
            SkillName = "Dash Punch", SkillMp = 8f, SkillCooldownMs = 3000f, SkillLockMs = 320f, SkillIcon = 0, DashIcon = 10, // no fighter icon sheet yet: the touch button falls back to text
        };

        public static readonly HeroClassDef[] All = { Warrior, Wizard, Mechanic, Assassin, Cleric, Fighter };

        public static HeroClassDef Get(HeroClass id) { return All[(int)id]; }
    }
}

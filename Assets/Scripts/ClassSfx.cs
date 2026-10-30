namespace ZenoniaProto
{
    /// <summary>
    /// Per-class combat sound table (ElevenLabs clips under Resources/Audio/sfx/&lt;class&gt;/). Melee classes get one whoosh per combo step +
    /// hit / crit impacts; casters get a cast sound per bolt and an impact / burst when it lands. Every class shouts on the combo finisher.
    /// HeroFx and ComboProjectile call these; a null entry means "no sound" for that class.
    /// </summary>
    public static class ClassSfx
    {
        sealed class Set
        {
            public string Folder;          // null slash folder = no per-step whoosh
            public bool StepSlashes;       // slash1..slash4.mp3
            public string[] Hit, Crit, Shot, Finisher, Shout, Impact, Burst;
        }

        static readonly Set[] Sets = Build();

        static Set[] Build()
        {
            var s = new Set[HeroClassDef.All.Length];
            s[(int)HeroClass.Warrior] = new Set
            {
                Folder = "warrior", StepSlashes = true, Shout = new[] { "warrior/shout4-a" },
                Hit = new[] { "warrior/hit-a", "warrior/hit-b" }, Crit = new[] { "warrior/crit-a", "warrior/crit-b" },
            };
            s[(int)HeroClass.Assassin] = new Set
            {
                Folder = "assassin", StepSlashes = true, Shout = new[] { "assassin/shout4-a" },
                Hit = new[] { "assassin/hit-a", "assassin/hit-b" }, Crit = new[] { "assassin/crit-a", "assassin/crit-b" },
            };
            s[(int)HeroClass.Mechanic] = new Set
            {
                Shot = new[] { "mechanic/shot-a", "mechanic/shot-b", "mechanic/shot-c" }, Finisher = new[] { "mechanic/cannon-a", "mechanic/cannon-b" },
                Shout = new[] { "mechanic/shout4-a", "mechanic/shout4-b" },
                Impact = new[] { "mechanic/hit-a" }, Burst = new[] { "mechanic/explosion-a", "mechanic/explosion-b" },
            };
            s[(int)HeroClass.Wizard] = new Set
            {
                Shot = new[] { "wizard/cast-a", "wizard/cast-b" }, Shout = new[] { "wizard/shout4-a", "wizard/shout4-b" },
                Impact = new[] { "wizard/hit-a", "wizard/hit-b" }, Burst = new[] { "wizard/burst-a", "wizard/burst-b" },
            };
            s[(int)HeroClass.Cleric] = new Set // female voice; casts low-passed (the raw chimes were shrill), no per-orb impact sound (too noisy)
            {
                Shot = new[] { "cleric/cast-a", "cleric/cast-b" }, Shout = new[] { "cleric/shout4-a", "cleric/shout4-b" },
                Burst = new[] { "cleric/burst-a", "cleric/burst-b" },
            };
            return s;
        }

        static Set For(HeroClass id) { int i = (int)id; return i >= 0 && i < Sets.Length ? Sets[i] : null; }

        /// <summary>Melee swing: the step's whoosh, plus the finisher shout.</summary>
        public static void Swing(HeroClass id, int step, bool finisher)
        {
            Set s = For(id);
            if (s == null) return;
            if (s.StepSlashes) GameSfx.Play(s.Folder + "/slash" + UnityEngine.Mathf.Clamp(step, 1, 4));
            if (finisher && s.Shout != null) GameSfx.PlayAny(s.Shout, 0.9f);
        }

        /// <summary>A melee swing connected.</summary>
        public static void MeleeHit(HeroClass id, bool crit)
        {
            Set s = For(id);
            if (s != null) GameSfx.PlayAny(crit && s.Crit != null ? s.Crit : s.Hit, 0.85f);
        }

        /// <summary>A bolt left the weapon: shot / cast sound, or the finisher sound + shout.</summary>
        public static void BoltLaunched(HeroClass id, bool finisher)
        {
            Set s = For(id);
            if (s == null) return;
            if (!finisher) { GameSfx.PlayAny(s.Shot, 0.8f); return; }
            GameSfx.PlayAny(s.Finisher ?? s.Shot);
            GameSfx.PlayAny(s.Shout, 0.9f);
        }

        /// <summary>A bolt landed: small impact, or the burst for an exploding finisher.</summary>
        public static void BoltImpact(HeroClass id, bool blast)
        {
            Set s = For(id);
            if (s == null) return;
            if (blast) GameSfx.PlayAny(s.Burst); else GameSfx.PlayAny(s.Impact, 0.7f);
        }
    }
}

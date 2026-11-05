namespace ZenoniaProto
{
    /// <summary>Combo chains per class, copied from the web hero-stats-config.js (COMBO_CONFIG, WIZARD_COMBO_CONFIG, MECHANIC_COMBO_CONFIG).</summary>
    public static class ComboConfigs
    {
        public static readonly ComboHit[] WarriorHits =
        {
            new ComboHit(50f, 90f, 110f, 1.0f, 30f, 28f, 40f, 6f, 1.45f),
            new ComboHit(50f, 90f, 110f, 1.15f, 32f, 30f, 50f, 8f, 1.55f),
            new ComboHit(50f, 90f, 110f, 1.25f, 34f, 32f, 55f, 8f, 1.65f),
            new ComboHit(90f, 120f, 220f, 1.8f, 42f, 40f, 110f, 16f, 1.15f),
        };

        // Ranged bolt ranges were halved on 2026-11-05 (user: the ranged classes hit from too far); shared with the server, rebuild it.
        // Wizard: caster stands still (stepPx 0); the 4th step is a slower, bigger bolt that bursts (AoE r26).
        static readonly BoltDef MagicBolt = new BoltDef(230f, 60f, "magicBolt", 0.8f, 10f, 10f, 0f, null, null, BoltStyle.Arcane);
        static readonly BoltDef ArcaneOrb = new BoltDef(180f, 55f, "magicBolt", 1.3f, 14f, 14f, 26f, "arcaneBurst", null, BoltStyle.Arcane);
        public static readonly ComboHit[] WizardHits =
        {
            new ComboHit(70f, 60f, 130f, 0.9f, 0f, 0f, 30f, 0f, 1f, MagicBolt),
            new ComboHit(70f, 60f, 130f, 1.0f, 0f, 0f, 35f, 0f, 1f, MagicBolt),
            new ComboHit(70f, 60f, 130f, 1.1f, 0f, 0f, 40f, 0f, 1f, MagicBolt),
            new ComboHit(130f, 80f, 260f, 1.6f, 0f, 0f, 90f, 0f, 1f, ArcaneOrb),
        };

        // Mechanic: three fast bullets, then a plasma cannon blast (AoE r24) with a muzzle flash on every shot.
        static readonly BoltDef Bullet = new BoltDef(340f, 70f, "bullet", 1f, 8f, 8f, 0f, null, "muzzleFlash", BoltStyle.Plasma);
        static readonly BoltDef Plasma = new BoltDef(240f, 65f, "plasmaShot", 1.2f, 12f, 12f, 24f, "explosion", "muzzleFlash", BoltStyle.Plasma);
        public static readonly ComboHit[] MechanicHits =
        {
            new ComboHit(50f, 50f, 110f, 0.85f, 0f, 0f, 25f, 0f, 1f, Bullet),
            new ComboHit(50f, 50f, 110f, 0.95f, 0f, 0f, 30f, 0f, 1f, Bullet),
            new ComboHit(50f, 50f, 110f, 1.05f, 0f, 0f, 35f, 0f, 1f, Bullet),
            new ComboHit(140f, 80f, 260f, 1.6f, 0f, 0f, 100f, 0f, 1f, Plasma),
        };

        // Assassin: three quick slashes, then a double-stab finisher (stabs 2, purple FX).
        // 2026-11-03: each hit 1.3x longer (0.16 s was ~27 ms per frame of the new 6-frame sprites: the poses did not read), still faster than
        // the Warrior's 0.25 s; multipliers 1.3x too, so the combo's damage per second is unchanged.
        public static readonly ComboHit[] AssassinHits =
        {
            new ComboHit(45f, 75f, 100f, 0.98f, 22f, 24f, 25f, 5f, 1.1f, null, 1),
            new ComboHit(45f, 75f, 100f, 1.1f, 24f, 26f, 30f, 6f, 1.2f, null, 1),
            new ComboHit(50f, 75f, 105f, 1.24f, 26f, 28f, 35f, 6f, 1.25f, null, 1),
            new ComboHit(80f, 110f, 180f, 2.08f, 30f, 30f, 70f, 10f, 1.0f, null, 2),
        };

        // Fighter: jab, cross, hook, then an uppercut finisher. Shorter reach than the daggers (fists), each punch 200-210 ms, the uppercut 2.2x with a
        // heavy knockback; the hero steps into every punch (StepPx) so the combo closes the distance. Damage per second close to the Assassin's.
        public static readonly ComboHit[] FighterHits =
        {
            new ComboHit(40f, 70f, 90f, 0.95f, 20f, 22f, 20f, 7f, 0.75f),
            new ComboHit(40f, 70f, 90f, 1.05f, 22f, 24f, 25f, 8f, 0.8f),
            new ComboHit(45f, 75f, 90f, 1.2f, 24f, 28f, 35f, 8f, 0.9f),
            new ComboHit(70f, 100f, 170f, 2.2f, 26f, 28f, 95f, 12f, 1.1f),
        };

        // Cleric: light arrows (HolyArrow) with a gold pillar impact; three quick shots then a bursting one (AoE r26).
        static readonly BoltDef ClericOrb = new BoltDef(220f, 60f, "magicBolt", 0.8f, 10f, 10f, 0f, null, null, BoltStyle.HolyArrow);
        static readonly BoltDef ClericBurstOrb = new BoltDef(180f, 55f, "magicBolt", 1.3f, 14f, 14f, 26f, "arcaneBurst", null, BoltStyle.HolyArrow);
        public static readonly ComboHit[] ClericHits =
        {
            new ComboHit(70f, 60f, 130f, 0.85f, 0f, 0f, 30f, 0f, 1f, ClericOrb),
            new ComboHit(70f, 60f, 130f, 1.0f, 0f, 0f, 35f, 0f, 1f, ClericOrb),
            new ComboHit(70f, 60f, 130f, 1.1f, 0f, 0f, 40f, 0f, 1f, ClericOrb),
            new ComboHit(130f, 80f, 250f, 1.6f, 0f, 0f, 80f, 0f, 1f, ClericBurstOrb),
        };
    }
}

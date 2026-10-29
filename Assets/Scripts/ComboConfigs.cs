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

        // Wizard: caster stands still (stepPx 0); the 4th step is a slower, bigger bolt that bursts (AoE r26).
        static readonly BoltDef MagicBolt = new BoltDef(230f, 120f, "magicBolt", 0.8f, 10f, 10f);
        static readonly BoltDef ArcaneOrb = new BoltDef(180f, 110f, "magicBolt", 1.3f, 14f, 14f, 26f, "arcaneBurst");
        public static readonly ComboHit[] WizardHits =
        {
            new ComboHit(70f, 60f, 130f, 0.9f, 0f, 0f, 30f, 0f, 1f, MagicBolt),
            new ComboHit(70f, 60f, 130f, 1.0f, 0f, 0f, 35f, 0f, 1f, MagicBolt),
            new ComboHit(70f, 60f, 130f, 1.1f, 0f, 0f, 40f, 0f, 1f, MagicBolt),
            new ComboHit(130f, 80f, 260f, 1.6f, 0f, 0f, 90f, 0f, 1f, ArcaneOrb),
        };

        // Mechanic: three fast bullets, then a plasma cannon blast (AoE r24) with a muzzle flash on every shot.
        static readonly BoltDef Bullet = new BoltDef(340f, 140f, "bullet", 1f, 8f, 8f, 0f, null, "muzzleFlash");
        static readonly BoltDef Plasma = new BoltDef(240f, 130f, "plasmaShot", 1.2f, 12f, 12f, 24f, "explosion", "muzzleFlash");
        public static readonly ComboHit[] MechanicHits =
        {
            new ComboHit(50f, 50f, 110f, 0.85f, 0f, 0f, 25f, 0f, 1f, Bullet),
            new ComboHit(50f, 50f, 110f, 0.95f, 0f, 0f, 30f, 0f, 1f, Bullet),
            new ComboHit(50f, 50f, 110f, 1.05f, 0f, 0f, 35f, 0f, 1f, Bullet),
            new ComboHit(140f, 80f, 260f, 1.6f, 0f, 0f, 100f, 0f, 1f, Plasma),
        };
    }
}

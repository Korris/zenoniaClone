namespace ZenoniaProto
{
    /// <summary>Combo chains per class, copied from the web hero-stats-config.js (COMBO_CONFIG, WIZARD_COMBO_CONFIG, MECHANIC_COMBO_CONFIG).</summary>
    public static class ComboConfigs
    {
        // Knockback (7th number) 2026-11-15, chosen with ChatGPT (docs/reference/combo-knockback-chatgpt.md; user: every hit shoved the monster so it
        // never hit back): hits 1-3 never push (0), only the finisher does. Finisher push = knockback x 1.35 x 0.17 s, about Warrior 25, Fighter 22,
        // Assassin 14, Wizard 18, Mechanic 20, Cleric 16 units; a crit no longer doubles it. Shared with the server: rebuild it.
        // Warrior 2026-11-20 (user: no more combo, one hard lock-target slash on a single monster): ONE heavy step, chained by holding / pressing
        // again. It locks a monster (Rpg/LockTargetRules, HeroController.LockTarget.cs), turns the hero to it and damages only that monster; no
        // cleave box, no push (the target stays in reach and keeps fighting back). 400 ms per slash, 1.75x: about the old 4-hit chain's damage
        // per second on one target (5.2x over ~1.18 s). The hit lands at the end of the 120 ms windup = attack frame f1 (sword coming down).
        // Width is unused by the lock strike. Shared with the server: rebuild it.
        public static readonly ComboHit[] WarriorHits =
        {
            new ComboHit(120f, 90f, 190f, 1.75f, 34f, 32f, 0f, 4f, 1.5f),
        };

        // Ranged bolt ranges were halved on 2026-11-05 (user: the ranged classes hit from too far); shared with the server, rebuild it.
        // Wizard: caster stands still (stepPx 0); the 4th step is a slower, bigger bolt that bursts (AoE r26).
        static readonly BoltDef MagicBolt = new BoltDef(230f, 60f, "magicBolt", 0.8f, 10f, 10f, 0f, null, null, BoltStyle.Arcane);
        static readonly BoltDef ArcaneOrb = new BoltDef(180f, 55f, "magicBolt", 1.3f, 14f, 14f, 26f, "arcaneBurst", null, BoltStyle.Arcane);
        // Board 2026-11-08: hit 2 (Frost Page) is the book's frost shard. Same numbers as MagicBolt (gameplay unchanged), only its picture differs
        // (ComboProjectile.ArcaneRow plays the v3 set's shard row for this sprite name).
        public const string FrostShardSprite = "frostShard";
        static readonly BoltDef FrostShard = new BoltDef(230f, 60f, FrostShardSprite, 0.8f, 10f, 10f, 0f, null, null, BoltStyle.Arcane);
        // Combo v2 (board 2026-11-10: Crystal Thrust, Frost Sweep, Glacier Slam, Blizzard Nova): hit 3's picture is a wave of ice spikes along the
        // ground (the v4 set's spikes row, drawn upright: ComboProjectile.ArcaneRow), same numbers as the magic bolt.
        public const string GlacierSpikesSprite = "glacierSpikes";
        static readonly BoltDef GlacierSpikes = new BoltDef(230f, 60f, GlacierSpikesSprite, 0.8f, 10f, 10f, 0f, null, null, BoltStyle.Arcane);
        // 2026-11-10 (user: the hits were too fast to read with the 6 / 8-frame v2 bodies): hits 1-3 350 ms (was 260), hit 4 640 ms (was 470);
        // multipliers x1.35 so the combo's damage per second is unchanged. Shared with the server: rebuild it.
        public static readonly ComboHit[] WizardHits =
        {
            new ComboHit(100f, 80f, 170f, 1.2f, 0f, 0f, 0f, 0f, 1f, MagicBolt),
            new ComboHit(100f, 80f, 170f, 1.35f, 0f, 0f, 0f, 0f, 1f, FrostShard),
            new ComboHit(100f, 80f, 170f, 1.5f, 0f, 0f, 0f, 0f, 1f, GlacierSpikes),
            new ComboHit(180f, 110f, 350f, 2.2f, 0f, 0f, 78f, 0f, 1f, ArcaneOrb),
        };

        // Mechanic: two fast bullets, one big bullet (hit 3, both pistols at once), then a plasma cannon blast (AoE r24) with a muzzle flash on every shot.
        // 2026-11-08: the bullets leave the middle of the body (RangedAimRules.BoltOrigin), 12 units behind the old muzzle point, so their range carries
        // those 12 (70 -> 82) and the shot still reaches as far ahead of the hero. The cannon shot (hit 4) too since the same day (65 -> 77; its ~3x shell starts small at the
        // body and grows, MechanicCannonBlastFx.BoltFlightScale, so it does not cover him). Hit 3 used to be a bullet plus a no-damage tracer from the
        // other pistol; it is now one bullet, 1.8x on screen with a 14 unit hit box, and keeps the hit's 1.05 (the pair's total), so the DPS is unchanged.
        // Approved board (same day, later): every shot leaves the gun muzzle (RangedAimRules.MechanicBoltOrigin) and flies Range minus the muzzle's lead
        // over the body point, so these ranges still count from the body and end where they did.
        static readonly BoltDef Bullet = new BoltDef(340f, 82f, "bullet", 1f, 8f, 8f, 0f, null, "muzzleFlash", BoltStyle.Plasma);
        static readonly BoltDef BigBullet = new BoltDef(340f, 82f, "bullet", 1.8f, 14f, 14f, 0f, null, "muzzleFlash", BoltStyle.Plasma);
        static readonly BoltDef Plasma = new BoltDef(240f, 77f, "plasmaShot", 1.2f, 12f, 12f, 24f, "explosion", "muzzleFlash", BoltStyle.Plasma);
        public static readonly ComboHit[] MechanicHits =
        {
            new ComboHit(50f, 50f, 110f, 0.85f, 0f, 0f, 0f, 0f, 1f, Bullet),
            new ComboHit(50f, 50f, 110f, 0.95f, 0f, 0f, 0f, 0f, 1f, Bullet),
            new ComboHit(50f, 50f, 110f, 1.05f, 0f, 0f, 0f, 0f, 1f, BigBullet),
            new ComboHit(140f, 80f, 260f, 1.6f, 0f, 0f, 87f, 0f, 1f, Plasma),
        };

        // Assassin: three quick slashes, then a double-stab finisher (stabs 2, purple FX).
        // 2026-11-03: each hit 1.3x longer (0.16 s was ~27 ms per frame of the new 6-frame sprites: the poses did not read), still faster than
        // the Warrior's 0.25 s; multipliers 1.3x too, so the combo's damage per second is unchanged.
        public static readonly ComboHit[] AssassinHits =
        {
            new ComboHit(45f, 75f, 100f, 0.98f, 22f, 24f, 0f, 5f, 1.1f, null, 1),
            new ComboHit(45f, 75f, 100f, 1.1f, 24f, 26f, 0f, 6f, 1.2f, null, 1),
            new ComboHit(50f, 75f, 105f, 1.24f, 26f, 28f, 0f, 6f, 1.25f, null, 1),
            new ComboHit(80f, 110f, 180f, 2.08f, 30f, 30f, 61f, 10f, 1.0f, null, 2),
        };

        // Fighter: jab, cross, hook, then an uppercut finisher. Shorter reach than the daggers (fists), each punch 200-210 ms, the uppercut 2.2x with a
        // heavy knockback; the hero steps into every punch (StepPx) so the combo closes the distance. Damage per second close to the Assassin's.
        public static readonly ComboHit[] FighterHits =
        {
            new ComboHit(40f, 70f, 90f, 0.95f, 20f, 22f, 0f, 7f, 0.75f),
            new ComboHit(40f, 70f, 90f, 1.05f, 22f, 24f, 0f, 8f, 0.8f),
            new ComboHit(45f, 75f, 90f, 1.2f, 24f, 28f, 0f, 8f, 0.9f),
            new ComboHit(70f, 100f, 170f, 2.2f, 26f, 28f, 95f, 12f, 1.1f),
        };

        // Cleric: light arrows (HolyArrow) with a gold pillar impact; three quick shots then a bursting one (AoE r26).
        // Board 2026-11-08 (docs/reference/cleric-basic-attack-board.png): Light Thrust, Aegis Ray (light arrow), Halo Lance (hit 3: the lance row of
        // VfxSprites "cleric-v2", picked by its sprite name; same speed, range and hit box), Judgment Bloom (holy orb bursting, r26). Timing unchanged.
        // Every shot leaves the staff crystal (Aegis Ray: the shield), RangedAimRules.ClericBoltOrigin, and flies Range minus how far that is ahead of
        // the old start (12 in front of the body), so these ranges still end where they did.
        public const string HaloLanceSprite = "haloLance";
        static readonly BoltDef ClericOrb = new BoltDef(220f, 60f, "magicBolt", 0.8f, 10f, 10f, 0f, null, null, BoltStyle.HolyArrow);
        static readonly BoltDef ClericLance = new BoltDef(220f, 60f, HaloLanceSprite, 0.8f, 10f, 10f, 0f, null, null, BoltStyle.HolyArrow);
        static readonly BoltDef ClericBurstOrb = new BoltDef(180f, 55f, "magicBolt", 1.3f, 14f, 14f, 26f, "arcaneBurst", null, BoltStyle.HolyArrow);
        public static readonly ComboHit[] ClericHits =
        {
            new ComboHit(70f, 60f, 130f, 0.85f, 0f, 0f, 0f, 0f, 1f, ClericOrb),
            new ComboHit(70f, 60f, 130f, 1.0f, 0f, 0f, 0f, 0f, 1f, ClericOrb),
            new ComboHit(70f, 60f, 130f, 1.1f, 0f, 0f, 0f, 0f, 1f, ClericLance),
            new ComboHit(130f, 80f, 250f, 1.6f, 0f, 0f, 70f, 0f, 1f, ClericBurstOrb),
        };
    }
}

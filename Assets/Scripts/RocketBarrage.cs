using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic ultimate ROCKET BARRAGE (web carpetBomb / UltimateEffect rocketBarrage, rank 3: 10 rockets, blast radius 18, 400% ATK each, range 70). Timeline (ms, same as the web):
    /// cyan lock-on reticle 0-1400, rockets launch in three groups at 400 / 440 / 480 (600 ms climb), each one descends over the 520 ms before its impact at 1550 + 85 * i,
    /// every impact damages its own blast circle (35 knockback), blast art 500 ms, smoke from +280 for 570 ms. The targets are a fixed snapshot of the forward fan: they do not follow the caster.
    /// Unity extras: additive cyan / orange bloom layers, shock rings, debris + dust + ember GPU particles, a soft shake per rocket, then hit-stop, punch-zoom and a soft flash on the last one.
    /// </summary>
    public class RocketBarrageUltimate : MonoBehaviour
    {
        public const float Radius = Rpg.LegacySkillNumbers.RocketBarrageRadius, Mult = Rpg.LegacySkillNumbers.RocketBarrageMult, Range = Rpg.LegacySkillNumbers.RocketBarrageRange;
        public const float LockMs = 1400f, LaunchMs = 400f, LaunchGapMs = 40f, ClimbMs = 600f, ImpactMs = 1550f, ImpactGapMs = 85f, DescendMs = 520f, BlastMs = 500f, SmokeDelayMs = 280f, SmokeMs = 570f, TailMs = 850f;
        const int Rockets = Rpg.LegacySkillNumbers.RocketBarrageRockets;
        const float TotalMs = ImpactMs + (Rockets - 1) * ImpactGapMs + TailMs;
        const string Folder = "fx/rocketbarrage";
        const float SourcePx = 229f;   // nominal atlas column width: a web stamp `size` wide spans this many source px
        const float BlastSize = Radius * 2.6f;
        const float ClimbHeight = 52f; // the climb ends this far above the target, where the descent art takes over (web)
        const float MuzzleHeight = 9f, CenterLift = 6f, SmokeLift = 4f; // web offsets, y flipped
        static readonly Vector2[] DirVec = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
        static readonly float[] DirAngle = { -90f, 90f, 180f, 0f };
        static readonly Vector2 DescendVec = new Vector2(-25f, 85f);   // rocket start offset from its impact point (web, y flipped)
        static readonly Color Cyan = new Color(0.35f, 0.82f, 1f), Orange = new Color(1f, 0.6f, 0.15f);

        sealed class Rocket
        {
            public Vector2 Target; public float LaunchMs, ImpactMs;
            public SpriteRenderer Climb, ClimbGlow, Descend, DescendGlow, Blast, BlastGlow, Smoke;
        }

        HeroController hero;
        Vector2 caster, center;
        int dir;
        float age, atk;
        bool launched;
        int impacts;
        readonly Rocket[] rockets = new Rocket[Rockets];
        Sprite[] lockFrames, climb, descend, blast, smoke;
        SpriteRenderer lockSr, lockGlow;

        public static void Cast(HeroController owner) { Cast(owner, 1f); }

        /// <summary>Skill-tree cast: `damageScale` is the rank multiplier (SkillDef.DamageMul) applied to the caster ATK of this cast.</summary>
        public static void Cast(HeroController owner, float damageScale)
        {
            var go = new GameObject("RocketBarrage");
            go.transform.SetParent(owner.transform.parent, false);
            go.AddComponent<RocketBarrageUltimate>().Begin(owner, damageScale);
        }

        // Frames sliced from ArtLibrary/custom/rocket-barrage-fx.png (web band / column cuts), canvas px per row: lock 229x320, launch 240x150, descend 229x230, blast 237x235, smoke 229x210.
        // Pivot y = web stamp anchor x canvas height; world size = web stamp size.
        static Sprite[] LockArt() { return UltimateFx.Strip(Folder, "lock", 114.5f, 320f * 0.62f, 65f / SourcePx); }
        static Sprite[] ClimbArt() { return UltimateFx.Strip(Folder, "launch", 120f, 150f * 0.5f, 30f / SourcePx); }
        static Sprite[] DescendArt() { return UltimateFx.Strip(Folder, "descend", 114.5f, 230f * 0.85f, 40f / SourcePx); }
        static Sprite[] BlastArt() { return UltimateFx.Strip(Folder, "blast", 118.5f, 235f * 0.8f, BlastSize / SourcePx); }
        static Sprite[] SmokeArt() { return UltimateFx.Strip(Folder, "smoke", 114.5f, 210f * 0.55f, BlastSize / SourcePx); }

        /// <summary>Loads and slices the 30 painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload() { LockArt(); ClimbArt(); DescendArt(); BlastArt(); SmokeArt(); }

        void Begin(HeroController owner, float damageScale)
        {
            hero = owner;
            caster = owner.Position;
            dir = owner.Facing;
            center = caster + DirVec[dir] * Range * 0.78f + new Vector2(0f, CenterLift);
            atk = owner.AtkStat * damageScale;
            for (int i = 0; i < Rockets; i++) rockets[i] = MakeRocket(i);
            if (!NetMode.RendersFx) return; // headless server: damage timeline only, no art / layers
            lockFrames = LockArt(); climb = ClimbArt(); descend = DescendArt(); blast = BlastArt(); smoke = SmokeArt();
            int g = UltimateFx.GroundFxOrder;
            lockSr = UltimateFx.Layer(transform, "Lock", out lockGlow);
            lockSr.sortingOrder = g + 2; lockGlow.sortingOrder = g + 3;
            lockSr.transform.position = center;
            for (int i = 0; i < Rockets; i++) MakeLayers(rockets[i], i);
            Apply();
        }

        Rocket MakeRocket(int i)
        {
            // web angle + (i / (n - 1) - 0.5) * 60 deg with y down: the minus keeps the same rocket on the same side after the flip
            float a = (DirAngle[dir] - (i / (Rockets - 1f) - 0.5f) * Rpg.LegacySkillNumbers.RocketBarrageFanDeg) * Mathf.Deg2Rad;
            float dist = Range * (Rpg.LegacySkillNumbers.RocketBarrageNear + (i % 3) * Rpg.LegacySkillNumbers.RocketBarrageStep);
            return new Rocket
            {
                Target = caster + new Vector2(Mathf.Cos(a) * dist, Mathf.Sin(a) * dist + CenterLift),
                LaunchMs = LaunchMs + (i % 3) * LaunchGapMs, ImpactMs = ImpactMs + i * ImpactGapMs,
            };
        }

        void MakeLayers(Rocket r, int i)
        {
            int o = FeelConfig.FxOrder - 20 + i;
            r.Climb = UltimateFx.Layer(transform, "Climb" + i, out r.ClimbGlow); r.Climb.sortingOrder = o; r.ClimbGlow.sortingOrder = o + 10;
            Vector2 path = ClimbPath(r);
            r.Climb.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(path.y, path.x) * Mathf.Rad2Deg);
            r.Descend = UltimateFx.Layer(transform, "Descend" + i, out r.DescendGlow); r.Descend.sortingOrder = o + 20; r.DescendGlow.sortingOrder = o + 30;
            r.Blast = UltimateFx.Layer(transform, "Blast" + i, out r.BlastGlow); r.Blast.sortingOrder = o + 40; r.BlastGlow.sortingOrder = o + 50;
            r.Blast.transform.position = r.Target;
            r.Smoke = ProceduralSprites.Child(transform, "Smoke" + i, null); r.Smoke.sortingOrder = o + 60;
            r.Smoke.transform.position = r.Target + new Vector2(0f, SmokeLift);
        }

        /// <summary>Caster to the point above the target where the climb ends (the art's heading; the body starts at the muzzle).</summary>
        Vector2 ClimbPath(Rocket r) { return r.Target - caster + new Vector2(0f, ClimbHeight); }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the timeline (web)
            age += GameClock.Dt * 1000f;
            if (!launched && age >= LaunchMs) { launched = true; Launch(); }
            while (impacts < Rockets && age >= rockets[impacts].ImpactMs) Explode(impacts++);
            Apply();
        }

        void Launch()
        {
            GameSfx.Play("skills/mechanic/homing-rockets-cast", 0.9f);
            UltimateFx.Embers(caster + new Vector2(0f, MuzzleHeight), 14, 5f, 34f, new Color(1f, 0.66f, 0.25f, 1f));
        }

        void Explode(int i)
        {
            Rocket r = rockets[i];
            bool last = i == Rockets - 1;
            SkillDamage.Radial(r.Target, Radius, atk, Mult, Rpg.LegacySkillNumbers.RocketBarrageKnockback, last, ClassPalette.Orange, 0f, hero);
            if (!NetMode.RendersFx) return;
            if (i % 2 == 0 || last) GameSfx.Play("skills/mechanic/homing-rockets-impact", last ? 0.9f : 0.7f); // every other rocket: 85 ms apart they would stack into noise
            CameraFollow.AddTrauma(last ? 0.55f : 0.16f);
            ShockRing.Spawn(r.Target, Radius * 1.1f, 0.3f, new Color(1f, 0.62f, 0.2f, 0.85f), UltimateFx.GroundFxOrder + 6);
            UltimateFx.Embers(r.Target, 12, Radius * 0.5f, 60f, new Color(1f, 0.6f, 0.15f, 1f));
            UltimateFx.Debris(r.Target, 5, Radius * 0.5f, 80f);
            UltimateFx.Dust(r.Target, 3, Radius * 0.6f);
            if (!last) return;
            GameClock.HitStop(60f);
            CameraFollow.Punch(0.6f);
            ScreenFlash.Trigger(new Color(1f, 0.55f, 0.15f, 0.1f), 0.2f);
        }

        static void Show(SpriteRenderer sr, SpriteRenderer glow, Sprite[] frames, float t, float alpha, Color glowColor, float glowAlpha)
        {
            bool on = frames != null && t >= 0f && t < 1f && alpha > 0f;
            sr.enabled = on;
            if (glow != null) glow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            Sprite sp = frames[Mathf.Min(5, Mathf.FloorToInt(t * 6f))];
            sr.sprite = sp;
            sr.color = new Color(1f, 1f, 1f, alpha);
            if (glow == null) return;
            glow.sprite = sp;
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowAlpha * alpha);
        }

        void Apply()
        {
            if (!NetMode.RendersFx) return;
            Show(lockSr, lockGlow, lockFrames, age / LockMs, Mathf.Min(1f, (LockMs - age) / 300f), Cyan, 0.3f);
            for (int i = 0; i < Rockets; i++)
            {
                Rocket r = rockets[i];
                // climb: straight line from the muzzle to above the target, on the thrust frames only (web launch * 0.7)
                float c = (age - r.LaunchMs) / ClimbMs;
                bool climbing = c >= 0f && c < 1f;
                if (climbing) r.Climb.transform.position = caster + new Vector2(0f, MuzzleHeight) + ClimbPath(r) * c;
                Show(r.Climb, r.ClimbGlow, climb, climbing ? c * 0.7f : -1f, 1f, Cyan, 0.35f);
                // descent: slides down the diagonal into the impact point (web descending * 0.8)
                float d = (age - r.ImpactMs + DescendMs) / DescendMs;
                bool falling = d >= 0f && d < 1f;
                if (falling) r.Descend.transform.position = r.Target + DescendVec * (1f - d);
                Show(r.Descend, r.DescendGlow, descend, falling ? d * 0.8f : -1f, 1f, Cyan, 0.35f);
                Show(r.Blast, r.BlastGlow, blast, (age - r.ImpactMs) / BlastMs, 1f, Orange, 0.35f);
                float s = (age - r.ImpactMs - SmokeDelayMs) / SmokeMs;
                Show(r.Smoke, null, smoke, s, Mathf.Max(0f, 0.85f * (1f - s)), Orange, 0f);
            }
        }
    }
}

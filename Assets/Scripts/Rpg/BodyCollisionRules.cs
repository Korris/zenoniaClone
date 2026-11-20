namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Soft unit collision, Ragnarok 3 style (ChatGPT, Tay chat 6afffe38, 2026-11-20): heroes pass through each other; a hero and a monster keep
    /// their FEET apart. Bosses have a hard foot core the hero cannot walk into; normal monsters a smaller, soft one (sprites may overlap a bit).
    /// Nobody shoves: a monster's own move stops at a hero (it never pushes the crowd), and a hero already overlapping (spawned / knocked in)
    /// drifts out over SeparateSeconds, through the tile walls, or simply stays overlapped when it cannot. Knockback only comes from skills.
    /// Distances are in "ground space": world y counts double, because the 3/4 view squashes the floor to half height (the target ring is a
    /// 1 : 0.5 ellipse for the same reason), so a boss is touched from the south at half the distance it is touched from the side.
    /// Engine-free: the offline sim, the zone server (authority) and the predicting client run the same numbers; unit-tested.
    /// </summary>
    public static class BodyCollisionRules
    {
        /// <summary>The hero is ~20 units tall = ~1 m (ChatGPT's numbers are in metres for a 1 m hero).</summary>
        public const float UnitsPerMeter = 20f;
        /// <summary>Hero foot radius 0.22 m.</summary>
        public const float HeroRadius = 0.22f * UnitsPerMeter;
        /// <summary>World y is doubled when measuring on the floor.</summary>
        public const float GroundYScale = 2f;
        /// <summary>Normal monsters: the contact distance is this fraction of foot + hero radius (slight overlap allowed).</summary>
        public const float NormalSoftness = 0.85f;
        /// <summary>An overlap is resolved over this long (0.15-0.25 s), never in one jump.</summary>
        public const float SeparateSeconds = 0.2f;
        /// <summary>Cap of that drift, units / s (a hero walks ~60): no teleport even for a deep overlap.</summary>
        public const float MaxSeparateSpeed = 50f;
        /// <summary>Wanted boss foot core 0.75-1.0 m; a map boss 0.4-0.75 m; a normal monster up to 0.7 m (all from its body width).</summary>
        public const float WantedFootMin = 0.75f * UnitsPerMeter, WantedFootMax = 1.0f * UnitsPerMeter;
        public const float BossFootMin = 0.4f * UnitsPerMeter, BossFootMax = 0.75f * UnitsPerMeter;
        public const float NormalFootMin = 4f, NormalFootMax = 0.7f * UnitsPerMeter;
        /// <summary>Foot core per melee-body half width (LockTargetRules.BodyHalfWidth). A Wanted boss's melee body follows its painted height
        /// (heads, necks), its feet are about half of it; a normal monster or map boss's melee body is its hit box, the feet nearly as wide
        /// (measured in game 2026-11-20: Frostfang King's feet ~13 units, Frost Slime ~5).</summary>
        public const float WantedFootPerBodyHalfWidth = 0.5f, FootPerBodyHalfWidth = 0.8f;
        /// <summary>A monster pressed against a hero strikes from up to this much past the contact distance (its own AttackRange may be shorter than
        /// a blocked approach allows, e.g. a boss touched from the side).</summary>
        public const float TouchStrikeSlack = 2f;
        /// <summary>A boss also strikes a hero whose chest is this close to its melee body edge: the Warrior's contact reach (LockTargetRules: 12 + 4),
        /// so a boss never walks on into a hero that already stands in sword contact with it (its AttackRange is feet to feet, shorter than its
        /// painted body: Frostfang King used to step into the hero, 2026-11-20).</summary>
        public const float BossContactReach = 16f;

        static float Clamp(float v, float lo, float hi) { return v < lo ? lo : v > hi ? hi : v; }
        static float Sqrt(float v) { return (float)System.Math.Sqrt(v); }

        /// <summary>Foot core radius of a monster from MonsterDef numbers only (hit box width, painted height), so server and client agree.</summary>
        public static float FootRadius(float hitW, float drawnHeight, bool boss, bool wanted)
        {
            float hw = LockTargetRules.BodyHalfWidth(hitW, drawnHeight);
            if (wanted) return Clamp(hw * WantedFootPerBodyHalfWidth, WantedFootMin, WantedFootMax);
            float r = hw * FootPerBodyHalfWidth;
            if (boss) return Clamp(r, BossFootMin, BossFootMax);
            return Clamp(r, NormalFootMin, NormalFootMax);
        }

        /// <summary>Ground distance under which a hero and this monster overlap.</summary>
        public static float ContactDistance(float footRadius, bool boss)
        {
            return (footRadius + HeroRadius) * (boss ? 1f : NormalSoftness);
        }

        /// <summary>Ground-space distance of a world offset.</summary>
        public static float GroundDist(float dx, float dy)
        {
            float gy = dy * GroundYScale;
            return Sqrt(dx * dx + gy * gy);
        }

        /// <summary>A mover at offset (dx, dy) from an obstacle (mover - obstacle) wants to move by (mx, my) world units. If that ends inside
        /// `contact`, the part of the move toward the obstacle is removed (it slides around it); moving away or along is untouched.</summary>
        public static void ClipMove(float dx, float dy, float contact, ref float mx, ref float my)
        {
            float gx = dx, gy = dy * GroundYScale;
            float ngx = gx + mx, ngy = gy + my * GroundYScale;
            if (ngx * ngx + ngy * ngy >= contact * contact) return; // the move stays outside
            float len = Sqrt(gx * gx + gy * gy);
            if (len < 0.01f) return; // on the same point: no direction to block; Separate pulls them apart
            float nx = gx / len, ny = gy / len;
            float mgx = mx, mgy = my * GroundYScale;
            float inward = mgx * nx + mgy * ny;
            if (inward >= 0f) return; // moving away (or along): an overlapping hero must be able to walk out
            mgx -= nx * inward; mgy -= ny * inward;
            mx = mgx; my = mgy / GroundYScale;
        }

        /// <summary>Drift of an overlapping hero away from an obstacle this step (world units), zero when they do not overlap. Speed = overlap /
        /// SeparateSeconds (capped), so it eases out instead of jumping. Exactly on top: it steps south (toward the camera, in front of the body).</summary>
        public static bool Separate(float dx, float dy, float contact, float dt, out float sx, out float sy)
        {
            sx = sy = 0f;
            float gx = dx, gy = dy * GroundYScale;
            float len = Sqrt(gx * gx + gy * gy);
            if (len >= contact) return false;
            float nx, ny;
            if (len < 0.01f) { nx = 0f; ny = -1f; } else { nx = gx / len; ny = gy / len; }
            float speed = (contact - len) / SeparateSeconds;
            if (speed > MaxSeparateSpeed) speed = MaxSeparateSpeed;
            float step = speed * dt;
            if (step > contact - len) step = contact - len;
            sx = nx * step; sy = ny * step / GroundYScale;
            return true;
        }

        /// <summary>True when a monster at contact with a hero may strike it although the hero is past its AttackRange.</summary>
        public static bool TouchReach(float dx, float dy, float contact, float slack)
        {
            return GroundDist(dx, dy) <= contact + slack;
        }
    }
}

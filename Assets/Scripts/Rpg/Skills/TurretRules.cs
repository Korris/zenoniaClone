using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The numbers of the Mechanic's Deploy Turret, all in this one place. SkillTable.Mechanic.cs reads the game rules from here; shared with the server, so a change to them needs a server rebuild.
    /// MP, cooldown, unlock level and prerequisites stay in the table like every other skill. Damage per shot at a rank: ShotPct x SkillDef.DamageMul(rank), +12% per rank.
    /// </summary>
    public static class TurretConfig
    {
        public const int DurationMs = 12000, FireIntervalMs = 800, FirstShotMs = 400;
        public const float RangeTiles = 6f, ShotPct = 65f;

        // Picture only.
        public const float DrawHeightUnits = 15f, ShotSpeedUnitsPerSec = 340f; // hero is about 20 units tall; the speed of the mechanic's own bullet. The hit lands when the shot arrives.
        public const float ArtWidthPx = 241f, ArtHeightPx = 260f, ArtGripXPx = 125f, ArtGripYPx = 256f; // Tools/build-deploy-turret.py: middle of the base, on the ground row (y from the top)
        public const float MuzzleX = -0.36f, MuzzleY = 0.813f; // from the grip, in art heights, barrel pointing left as painted
        public const int DeployMs = 240, FadeMs = 220, BlinkLastMs = 2000;
    }

    /// <summary>When a turret dies and may fire. It keeps its shot ready while nothing is in range, so a monster walking in is shot at once; missed shots never pile up into a burst.</summary>
    public sealed class TurretClock
    {
        readonly int durationMs, intervalMs;
        float ageMs, nextShotMs;
        bool waited;

        public TurretClock(int durationMs, int intervalMs, int firstShotMs)
        {
            this.durationMs = durationMs; this.intervalMs = Math.Max(1, intervalMs); nextShotMs = firstShotMs;
        }

        public void Tick(float dtMs) { ageMs += dtMs; }
        public bool Expired { get { return ageMs >= durationMs; } }
        public int RemainingMs { get { return Math.Max(0, (int)Math.Ceiling(durationMs - ageMs)); } }
        public bool ShotDue { get { return !Expired && ageMs >= nextShotMs; } }

        /// <summary>The due shot left: the next one comes an interval after it was due (frame length does not slow the rhythm), or after now when it had to wait for a target.</summary>
        public void Fired() { nextShotMs = waited ? ageMs + intervalMs : nextShotMs + intervalMs; waited = false; }

        /// <summary>The shot was due but nothing is in range.</summary>
        public void NoTarget() { waited = true; }
    }

    public struct TurretCandidate
    {
        public float X, Y;
        public bool Dying;
        public TurretCandidate(float x, float y, bool dying) { X = x; Y = y; Dying = dying; }
    }

    public static class TurretTargeting
    {
        /// <summary>Index of the closest live candidate whose point is within `reach` of (x, y), -1 when none.</summary>
        public static int Nearest(float x, float y, float reach, IList<TurretCandidate> all)
        {
            int best = -1;
            float bestSq = reach * reach;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Dying) continue;
                float dx = all[i].X - x, dy = all[i].Y - y, sq = dx * dx + dy * dy;
                if (sq <= bestSq) { bestSq = sq; best = i; }
            }
            return best;
        }
    }

    /// <summary>The live turrets of a zone, at most one per owner hero: placing a new one hands back the one it replaces.</summary>
    public sealed class TurretRoster<T> where T : class
    {
        struct Slot { public ushort Owner; public T Turret; }
        readonly List<Slot> slots = new List<Slot>(4);

        public int Count { get { return slots.Count; } }
        public T this[int index] { get { return slots[index].Turret; } }

        public T Place(ushort owner, T turret)
        {
            T replaced = null;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].Owner != owner) continue;
                replaced = slots[i].Turret;
                slots.RemoveAt(i);
            }
            slots.Add(new Slot { Owner = owner, Turret = turret });
            return replaced;
        }

        public void Remove(T turret)
        {
            for (int i = slots.Count - 1; i >= 0; i--) if (ReferenceEquals(slots[i].Turret, turret)) slots.RemoveAt(i);
        }
    }
}

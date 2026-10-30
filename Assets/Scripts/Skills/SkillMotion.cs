using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Forced hero motion of Dash / Leap effects: walks the hero `units` along a direction over `ms`, step by step through the map collision
    /// (never through walls), then removes itself. Runs on the authority only; clients see the new position in the snapshots.
    /// </summary>
    public sealed class SkillMotion : MonoBehaviour
    {
        ISkillBody body;
        Vector2 dir;
        float units, ms, age;

        public static void Begin(ISkillBody body, Vector2 dir, float units, float ms)
        {
            if (body == null || body.IsDead || units <= 0f || dir.sqrMagnitude < 0.0001f) return;
            var m = new GameObject("SkillMotion").AddComponent<SkillMotion>();
            m.transform.SetParent(body.Hero.transform.parent, false);
            m.body = body; m.dir = dir.normalized; m.units = units; m.ms = Mathf.Max(1f, ms);
        }

        void Update()
        {
            if (body == null || body.IsDead || age >= ms) { Destroy(gameObject); return; }
            float dtMs = Mathf.Min(GameClock.Dt * 1000f, ms - age);
            age += dtMs;
            float step = units * dtMs / ms;
            bool bx, by;
            Vector2 p = MapRegistry.Current.Move(body.Feet, dir.x * step, dir.y * step, HeroSkillBody.BoxW, HeroSkillBody.BoxH, out bx, out by);
            body.Hero.Teleport(p, body.Facing);
            if (bx && by) Destroy(gameObject); // wedged in a corner
        }
    }
}

using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic's floating robot (web companion-follow.js + companion-renderer.js): hovers on the shoulder the hero's back is turned to,
    /// eases toward that spot, bobs, and is y-sorted by its own ground point. Tier 1 unenhanced plays the 4-facing float loop;
    /// every other tier shows the static tier art (base / enhanced) with a slightly bigger code bob.
    /// </summary>
    public class RobotCompanion : MonoBehaviour
    {
        // Web COMPANION_OFFSETS (y flipped: world y points up). Index == facing: down, up, left, right.
        static readonly Vector2[] Offsets = { new Vector2(11f, 3f), new Vector2(-11f, -3f), new Vector2(10f, 0f), new Vector2(-10f, 0f) };
        const float Hover = 11f, FollowMs = 160f, SnapDistance = 90f, MaxStepMs = 100f, BobPeriodMs = 1500f, FloatFrameMs = 140f, ArtScale = 0.26f;

        HeroController hero;
        SpriteRenderer sr;
        Vector2 pos;
        float ageMs;

        public static RobotCompanion Create(Transform parent, HeroController owner)
        {
            var go = new GameObject("Robot");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RobotCompanion>();
            r.hero = owner;
            r.sr = go.AddComponent<SpriteRenderer>();
            r.pos = owner.Position + Offsets[owner.Facing];
            return r;
        }

        void LateUpdate()
        {
            if (hero == null) { Destroy(gameObject); return; }
            sr.enabled = !hero.IsDead;
            if (hero.IsDead) return;
            Vector2 target = hero.Position + Offsets[hero.Facing];
            float dtMs = Mathf.Min(MaxStepMs, GameClock.Dt * 1000f);
            ageMs += dtMs;
            if (Vector2.Distance(target, pos) > SnapDistance) pos = target;
            else pos += (target - pos) * (1f - Mathf.Exp(-dtMs / FollowMs));
            Draw();
        }

        void Draw()
        {
            HeroRenderer view = hero.View;
            HeldGearData gear = view.Gear;
            if (gear == null || gear.Robot == null || gear.Robot.Length == 0) { sr.sprite = null; return; }
            int tier = Mathf.Clamp(view.Tier, 0, gear.Robot.Length - 1);
            bool animated = tier == 0 && !view.Enhanced;
            string dir = HeroRenderer.DirNames[hero.Facing];
            GripInfo[] loop = animated ? gear.RobotFloat(dir) : null;
            Sprite sprite;
            if (loop != null && loop.Length > 0)
            {
                int i = Mathf.FloorToInt(ageMs / FloatFrameMs) % loop.Length;
                sprite = GearOverlays.Pivoted("Sprites/mechanic-robot-float/" + dir + "-" + i, loop[i]);
            }
            else
            {
                animated = false;
                sprite = GearOverlays.Get("mechanic-robot", gear.Robot[tier], view.Enhanced);
            }
            sr.sprite = sprite;
            float bob = Mathf.Sin(ageMs / BobPeriodMs * Mathf.PI * 2f) * (animated ? 0.6f : 1.3f);
            transform.position = new Vector3(pos.x, pos.y + Hover + bob, 0f);
            float k = ArtScale * GearOverlays.FrameUnit * GearOverlays.Ppu;
            transform.localScale = new Vector3(k, k, 1f);
            sr.color = new Color(1f, 1f, 1f, 0.96f);
            sr.sortingOrder = GameConfig.OrderBase(pos.y) + 3;
        }
    }
}

using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Look of a ranged monster's shot that has no painted picture (MonsterRangedProfile.Sprite == null: Taurus spore + thorn seed): a small bright comet made of
    /// particles, never a sprite (CLAUDE.md VFX rule). One particle system per shot (a handful at a time), world simulation space so the trail stays where it was left:
    /// a stretched head particle every frame + a few fading motes behind it. Colour = the profile's ColorRgb with a pale core. Sits on the projectile object and follows it
    /// (MonsterProjectile.Place moves the object); client and offline only (the headless server never builds it).
    /// </summary>
    public sealed class MonsterShotParticles : MonoBehaviour
    {
        const float Lift = 6f, HeadLife = 0.07f, MoteLife = 0.34f, MotesPerSecond = 46f;
        ParticleSystem ps;
        Color glow, core;
        Vector2 dir;
        float size, speed, moteDebt;

        /// <summary>Builds the particle comet on `go` (the projectile object). Null when the light FX toolkit is not usable (the caller falls back to the plain quill).</summary>
        public static MonsterShotParticles Create(GameObject go, MonsterRangedProfile profile, float angleDeg, int order)
        {
            if (!SkillLightMaterials.Ready) return null;
            var c = new Color32((byte)(profile.ColorRgb >> 16), (byte)(profile.ColorRgb >> 8), (byte)profile.ColorRgb, 255);
            var m = go.AddComponent<MonsterShotParticles>();
            m.glow = c;
            m.core = Color.Lerp(c, Color.white, 0.65f);
            m.size = Mathf.Max(3f, profile.HitSize * 1.15f);
            m.speed = profile.ProjectileSpeed;
            float rad = angleDeg * Mathf.Deg2Rad;
            m.dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            m.ps = SkillLightMaterials.Sparks(go.transform, order, 64, 0f, 0.05f);
            m.ps.gameObject.name = "ShotComet";
            return m;
        }

        public void SetOrder(int order) { if (ps != null) ps.GetComponent<ParticleSystemRenderer>().sortingOrder = order; }

        void LateUpdate()
        {
            if (ps == null) return;
            float dt = GameClock.Dt;
            if (dt <= 0f) return; // hit-stop: no new particles
            // 2.5D / 3D maps: the world y axis is depth on the ground, so chest height is lifted along the camera-facing up (View25D.Tilt), not added to y.
            Vector3 up = View25D.Active ? View25D.Tilt * new Vector3(0f, Lift, 0f) : new Vector3(0f, Lift, 0f);
            Vector3 p = transform.position + up;
            Vector3 d3 = new Vector3(dir.x, dir.y, 0f);
            WantedFxParticles.Emit(ps, p, d3 * speed, size, core, HeadLife);
            moteDebt += MotesPerSecond * dt;
            for (; moteDebt >= 1f; moteDebt -= 1f)
            {
                Vector3 side = new Vector3(-dir.y, dir.x, 0f) * Random.Range(-1.4f, 1.4f);
                Vector3 at = p - d3 * Random.Range(0f, speed * dt * 1.5f);
                WantedFxParticles.Emit(ps, at, side - d3 * 2f, size * Random.Range(0.45f, 0.8f), glow, MoteLife * Random.Range(0.7f, 1f));
            }
        }
    }
}

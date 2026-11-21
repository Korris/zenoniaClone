using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The Mechanic's explosion (rockets, cannon, Orbital Cannon), redone big after the user watched the first shader pass (21/11: "skill tên lửa
    /// sao nhìn nó bé tí vậy, không có độ ngầu gì hết"; "tuy skill không dùng sprite nữa nhưng vẫn phải có độ hoành tráng"): attack skills sit at the
    /// top of their size band, the light still gathers at the blast centre. Layers, all at the real blast radius r:
    ///  0-120 ms hot white-yellow core flash (~1.3 r) + 4-point star; 0-320 ms orange fireball (3 solid puffs growing 0.4 r -> 1.15 r, dissolving);
    ///  0-340 ms two shockwave rings on the ground (to r and 1.15 r) + scorch pool; 100-1000 ms dark smoke puffs rising and dissolving;
    ///  sparks + dark debris streaks thrown out under gravity. Crowd LOD from the host (strangers: main shapes, few particles).
    /// </summary>
    public static partial class MechanicLightBursts
    {
        public static ClericLightFx Explosion(Vector2 at, float r, float dim, bool big)
        {
            ClericLightLook.Pal fire = MechanicLightLook.Fire;
            float k = big ? 1.2f : 1f, end = big ? 1050f : 860f, core = Mathf.Max(16f, r * 1.3f) * k;
            Vector2 mid = new Vector2(0f, Mathf.Min(10f, r * 0.35f));
            var f = Host("Blast", at, end, dim);
            f.Ground(MechanicLightLook.Pool(fire), 0f, 520f, r * 1.1f, r * 2.3f * k, 1f).Kind(LayerKind.Glow)
             .Ground(MechanicLightLook.Line(MechanicLightLook.Band, fire), 0f, 300f, r * 0.5f, MechanicLightLook.BandQuad(r * k), 1f, ClericLightFx.Squash, 0f, 9)
             .Ground(MechanicLightLook.Line(MechanicLightLook.Band, MechanicLightLook.Honey), 60f, 380f, r * 0.6f, MechanicLightLook.BandQuad(r * 1.15f * k), 0.7f, ClericLightFx.Squash, 0f, 8)
             .Upright(MechanicLightLook.Flash(fire), ClericLightFx.Band.Over, false, 0f, 140f, core * 0.6f, core * 1.15f, core * 0.6f, core * 1.15f, 1f, mid, 6)
             .Upright(MechanicLightLook.Glyph(MechanicLightLook.Spark, MechanicLightLook.Honey), ClericLightFx.Band.Over, false, 0f, 200f, core * 0.7f, core * 1.5f, core * 0.7f, core * 1.5f, 0.9f, mid, 7).Kind(LayerKind.Detail);
            for (int i = 0; i < 3; i++)                                                         // orange fireball
            {
                float a = (i * 120f + Random.Range(-30f, 30f)) * Mathf.Deg2Rad, d = r * 0.18f, s = r * k * Random.Range(0.95f, 1.2f);
                Vector2 off = mid + new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d * 0.8f);
                f.Upright(MechanicLightLook.FireballMat, ClericLightFx.Band.Over, false, i * 25f, 300f + i * 35f, s * 0.4f, s * 1.15f, s * 0.4f, s * 1.1f, 1f, off, 3 + i)
                 .Rise(r * 0.25f).Dissolve();
            }
            int puffs = big ? 6 : 4;                                                            // dark smoke
            for (int i = 0; i < puffs; i++)
            {
                float a = (i * 360f / puffs + Random.Range(-25f, 25f)) * Mathf.Deg2Rad, d = r * Random.Range(0.3f, 0.6f), s = r * k * Random.Range(0.75f, 1.05f);
                Vector2 off = mid + new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d * ClericLightFx.Squash);
                float t0 = 90f + i * 30f;
                f.Upright(MechanicLightLook.SootMat, ClericLightFx.Band.Over, false, t0, t0 + (big ? 820f : 680f), s * 0.5f, s * 1.3f, s * 0.5f, s * 1.3f, 0.85f, off, i % 2 == 0 ? -2 : 1)
                 .Rise(r * 0.6f).Dissolve();
            }
            f.At(0f, h =>
            {
                Sparks(h, at + mid, big ? 26 : 18, 110f + r * 3f, 90f, 360f, MechanicLightLook.Ember, 0.4f, 2.2f);
                Sparks(h, at + mid, big ? 12 : 8, 80f + r * 2.5f, 90f, 200f, MechanicLightLook.Debris, 0.5f, 2.6f);
            });
            return f;
        }
    }
}

using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic gun FX (ranged-attack-fx-reference.png) in the active palette (orange by default, blue with C): a charge glow that builds in the
    /// barrel, a big muzzle blast flare, and an impact with a white-hot starburst, hot sparks and a brown dust / rock debris cloud.
    /// </summary>
    public static class MechanicFx
    {
        static readonly Color Dust = new Color(0.5f, 0.36f, 0.24f);

        /// <summary>Barrel charge glow + muzzle blast along the shot direction (angle deg, 0 = right).</summary>
        public static void Muzzle(Vector2 barrel, float angle, bool big, ClassPalette p)
        {
            float rad = angle * Mathf.Deg2Rad;
            var fwd = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            RuneFx.Spawn(barrel, big ? 16f : 10f, 1f, 0.12f, 0f, p.Mid, p.Core, FeelConfig.FxOrder - 8, false);                    // charge glow in the barrel
            DaggerFx.Ray(barrel, angle, big ? 34f : 22f, big ? 6f : 4.2f, 0.12f, p.Mid, p.Core);                                    // big muzzle blast flare
            DaggerFx.Ray(barrel, angle + 24f, big ? 20f : 13f, 2.2f, 0.1f, p.Edge, p.Core);
            DaggerFx.Ray(barrel, angle - 24f, big ? 20f : 13f, 2.2f, 0.1f, p.Edge, p.Core);
            UltimateFx.Embers(barrel + fwd * 4f, big ? 10 : 5, 2f, 6f, new Color(p.Mid.r, p.Mid.g, p.Mid.b, 1f), 0f);
        }

        /// <summary>Starburst flash + hot sparks + brown dust and rock chunks.</summary>
        public static void Impact(Vector2 pos, bool big, ClassPalette p)
        {
            RuneFx.Spawn(pos, big ? 34f : 20f, 1f, 0.22f, 0f, p.Mid, p.Core, FeelConfig.FxOrder + 4, false);
            for (int i = 0; i < (big ? 10 : 6); i++) DaggerFx.Ray(pos, i * (big ? 36f : 60f) + Random.Range(0f, 20f), Random.Range(big ? 18f : 11f, big ? 30f : 18f), 2f, 0.2f, p.Mid, p.Core);
            ShockRing.Spawn(pos, big ? 22f : 12f, 0.25f, p.Edge, FeelConfig.FxOrder + 3, 1f, 0.3f);
            UltimateFx.Embers(pos, big ? 26 : 12, 3f, 38f, new Color(1f, 0.55f, 0.15f, 1f));                 // orange-hot sparks in either palette
            UltimateFx.Debris(pos, big ? 14 : 6, big ? 12f : 6f, big ? 70f : 45f);                          // brown rock chunks
            UltimateFx.Dust(pos + new Vector2(0f, -2f), big ? 9 : 4, big ? 10f : 5f, Dust);                  // dust cloud
        }
    }
}

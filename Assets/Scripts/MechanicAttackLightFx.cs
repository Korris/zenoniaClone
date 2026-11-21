using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Light picture of the Mechanic's basic attack (ComboConfigs.MechanicHits), RO3 Soft Fantasy V1 (2026-11-21), replacing the painted
    /// mechanic-v3 frames. Bolt speeds, ranges, hit boxes and the cannon's blast radius stay ComboConfigs'; muzzle points stay RangedAimRules'.
    ///  - Rig: hits 1-2 a honey bullet (head 0.35 H turned along its flight, trail ~0.1 H, ~0.06 s); hit 3 (both pistols) a bigger honey bolt with
    ///    a cyan trail; hit 4 the cannon shell (0.7 H) growing out of the muzzle, behind the hero for its first 18 units when fired up (Sổ lỗi 08/11);
    ///  - Muzzle: flare + small flash + 2 sparks at the gun (others' shots: no flash, rule 12);
    ///  - Impact: honey pop (big bolt: + cyan sparks); the cannon: MechanicLightBursts.Explosion on the blast radius.
    /// </summary>
    public static class MechanicAttackLightFx
    {
        public enum Shot { Bullet, Big, Shell }

        static bool Own(HeroController shooter) { HeroController me = LocalPlayer.Hero; return me == null || shooter == null || shooter == me; }

        /// <summary>The flying light of one combo bolt (lives on the pooled ComboProjectile, reused bolt after bolt).</summary>
        public sealed class Rig
        {
            const float ShellGrowUnits = 18f, ShellStart = 0.4f, BehindUnits = 18f;
            readonly Transform root;
            readonly MeshRenderer head;
            readonly TrailRenderer trail;
            Vector2 start, last;
            Vector2 size;
            Shot kind;
            int behindOrder;

            public Rig(Transform parent)
            {
                root = new GameObject("MechanicBoltLight").transform;
                root.SetParent(parent, false);
                head = LightSkillAssets.Quad(root, "Head", MechanicLightLook.Glyph(MechanicLightLook.Core, MechanicLightLook.Honey), false, FeelConfig.FxOrder - 12);
                trail = ClericAttackLightFx.MakeTrail(root, 2f, 0.06f, 1f, MechanicLightLook.Honey);
                root.gameObject.SetActive(false);
            }

            /// <summary>`behind` = sorting order just behind the shooter for a shell fired up (0 = never).</summary>
            public void Begin(Vector2 pos, float angleDeg, Shot shot, bool own, int behind)
            {
                kind = shot; start = last = pos; behindOrder = behind;
                root.position = new Vector3(pos.x, pos.y, 0f);
                size = shot == Shot.Shell ? new Vector2(18f, 11f) : shot == Shot.Big ? new Vector2(13f, 6f) : new Vector2(9f, 3.8f);
                head.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
                ClericLightLook.Pal tp = shot == Shot.Big ? MechanicLightLook.Tech : MechanicLightLook.Honey;
                trail.sharedMaterial = ClericLightLook.Trail(tp);
                trail.widthMultiplier = (shot == Shot.Shell ? 5.5f : shot == Shot.Big ? 3.6f : 2.4f) * (own ? 1f : 0.6f);
                trail.time = shot == Shot.Shell ? 0.11f : shot == Shot.Big ? 0.08f : 0.06f;
                ClericAttackLightFx.SetTrailColour(trail, own ? 1f : 0.4f, tp);
                LightSkillAssets.Set(head, own ? 1f : 0.6f);
                root.gameObject.SetActive(true);
                trail.Clear();
                trail.emitting = own; // raid rule: another player's bullet is only its glowing head
                Tick(pos);
            }

            public void Tick(Vector2 pos)
            {
                root.position = new Vector3(pos.x, pos.y, 0f);
                Vector2 d = pos - last;
                if (d.sqrMagnitude > 0.01f) head.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                last = pos;
                float travelled = Vector2.Distance(start, pos);
                float g = kind == Shot.Shell ? Mathf.Lerp(ShellStart, 1f, Mathf.Clamp01(travelled / ShellGrowUnits)) : Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(travelled / 8f));
                head.transform.localScale = new Vector3(size.x * g, size.y * g, 1f);
                bool behind = behindOrder != 0 && travelled < BehindUnits;
                head.sortingOrder = behind ? behindOrder : FeelConfig.FxOrder - 12;
                trail.sortingOrder = behind ? behindOrder - 1 : FeelConfig.FxOrder - 14;
                head.enabled = FeelConfig.FxOn;
            }

            public void End() { trail.emitting = false; root.gameObject.SetActive(false); }
        }

        /// <summary>Pistol flash at the shooting gun's muzzle (hits 1-3).</summary>
        public static void Muzzle(Vector2 tip, float angleDeg, bool big, HeroController shooter)
        {
            if (!Own(shooter)) return;
            MechanicLightBursts.Muzzle(tip, angleDeg, big ? 18f : 13f, 1f, MechanicLightLook.Honey, big ? 5 : 3);
        }

        /// <summary>The bolt hit at `pos` (drawn bolt point); blast = the cannon's burst of `radius` units; big = hit 3.</summary>
        public static void Impact(Vector2 pos, bool blast, float radius, bool big, HeroController shooter)
        {
            float dim = Own(shooter) ? 1f : 0.35f;
            if (blast) { MechanicLightBursts.Explosion(pos + new Vector2(0f, -6f), radius, dim, true); return; }
            MechanicLightBursts.Pop(pos, big ? 19f : 14f, dim, MechanicLightLook.Honey, big ? MechanicLightLook.Crackle : MechanicLightLook.Ember);
        }

        /// <summary>
        /// Cannon discharge (hit 4) at the muzzle along `angleDeg`: a long flare, core flash, three thin rings thrown forward, sparks, a little smoke
        /// out of the back of the barrel. `order` != 0: drawn just behind the shooter (fired up). MechanicCannonBlastFx keeps recoil, sound, shake.
        /// </summary>
        public static void Cannon(Vector2 at, float angleDeg, float dim, int order)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            ClericLightFx f = MechanicLightBursts.Muzzle(at, angleDeg, 34f, dim, MechanicLightLook.Honey, 10, order);
            Material ring = MechanicLightLook.Line(MechanicLightLook.Band, MechanicLightLook.Honey);
            int o = order != 0 ? order : FeelConfig.FxOrder - 6;
            for (int i = 0; i < 3; i++)
            {
                float t0 = i * 35f;
                MechanicLightQuad.Add(f, ring, at + dir * (8f + i * 9f), angleDeg, t0, t0 + 190f, new Vector2(4f, 9f), new Vector2(9f, 24f - i * 4f), 0.75f - i * 0.12f, o + i);
            }
            Vector2 back = at - dir * 5f - f.Anchor;
            for (int i = 0; i < 2; i++)
                f.Upright(MechanicLightLook.SmokeMat, ClericLightFx.Band.Over, false, 20f + i * 40f, 420f + i * 40f, 3f, 7f, 3f, 7f, 0.55f, back + new Vector2(Random.Range(-2f, 2f), i * 2f), -4).Rise(5f).Dissolve();
        }
    }
}

using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Painted-sprite (VfxSet) projectile loop and impact, see VfxSprites.</summary>
    public partial class ComboProjectile
    {
        Sprite[] projFrames; // the loop this bolt plays: the set's proj row, or its bolt row (the Mechanic's big blue hit-3 bolt)

        /// <summary>The Mechanic's hit-3 bolt (one big shot from both pistols) uses the v2 set's blue bolt row at this scale (the row is drawn ~18 units long).</summary>
        const float BigBoltScale = 1.2f;

        /// <summary>Wizard v3 set (board 2026-11-08): hit 4's orb loops the bolt row, hit 2's frost shard (BoltDef sprite "frostShard") the shard row; null = the proj row.</summary>
        Sprite[] ArcaneRow(bool big)
        {
            if (def.Style != BoltStyle.Arcane) return null;
            int row = big ? VfxSet.Bolt : def.Sprite == ComboConfigs.FrostShardSprite ? VfxSet.Shard : def.Sprite == ComboConfigs.GlacierSpikesSprite ? VfxSet.Spikes : -1;
            return row >= 0 && vfx.Rows.Length > row ? vfx.Rows[row] : null;
        }

        /// <summary>The Wizard v4 glacier spikes (hit 3) are painted standing on the ground: they slide toward the target without turning.</summary>
        bool Upright { get { return def.Style == BoltStyle.Arcane && def.Sprite == ComboConfigs.GlacierSpikesSprite && vfx.Rows.Length > VfxSet.Spikes && vfx.Rows[VfxSet.Spikes] != null; } }

        /// <summary>Cleric v2 set (board 2026-11-08): hit 4's holy orb loops the bolt row, hit 3's halo lance (BoltDef sprite "haloLance") the lance row; null = the proj row (light arrow).</summary>
        Sprite[] ClericRow(bool big)
        {
            if (def.Style != BoltStyle.HolyArrow) return null;
            int row = big ? VfxSet.Bolt : def.Sprite == ComboConfigs.HaloLanceSprite ? VfxSet.Lance : -1;
            return row >= 0 && vfx.Rows.Length > row ? vfx.Rows[row] : null;
        }

        bool IsBigBolt { get { return def.Style == BoltStyle.Plasma && !finisher && def.Scale > 1f && vfx.Rows.Length > VfxSet.Bolt && vfx.Rows[VfxSet.Bolt] != null; } }

        /// <summary>Painted projectile: 4-frame loop pointing right (pivot on the bright head), rotated to the travel direction.</summary>
        void BeginPainted(float angleDeg, bool big)
        {
            animT = 0f;
            projFrames = IsBigBolt ? vfx.Rows[VfxSet.Bolt] : ArcaneRow(big) ?? ClericRow(big) ?? vfx.Rows[VfxSet.Proj];
            sr.enabled = glow.enabled = true;
            sr.sprite = glow.sprite = projFrames[0];
            sr.color = Color.white;
            sr.sortingOrder = FeelConfig.FxOrder - 10;
            glow.sortingOrder = FeelConfig.FxOrder - 9;
            // The Mechanic's cannon shell fired up leaves the middle of his body away from the camera: behind him until it clears his head.
            behindOrder = def.Style == BoltStyle.Plasma && big && dir.y > 0.7f ? GameConfig.OrderBase(pos.y - Rpg.RangedAimRules.BoltLift) - 2 : 0;
            behindUnits = MechanicBehindUnits;
            behindFeetY = behindTopY = 0f; // band off (Mechanic distance rule)
            transform.rotation = Quaternion.Euler(0f, 0f, Upright ? 0f : angleDeg);
            transform.localScale = FlightScale(0f, big);
            transform.position = new Vector3(pos.x, pos.y + VisualLift, 0f);
            gameObject.SetActive(true);
            Animate();
        }

        /// <summary>
        /// On-screen scale of the bolt after `travelled` units: Mechanic bullets grow their tail out of the muzzle, the shell grows out of the body.
        /// Cleric bolts (light arrow, halo lance, holy orb) leave the staff crystal / shield (RangedAimRules.ClericBoltOrigin) and grow their tail out
        /// of it the same way, so the ~25 unit lance does not lie back across her head; the orb also opens from ClericOrbStart of its size.
        /// </summary>
        Vector3 FlightScale(float travelled, bool big, float tailRoom = float.MaxValue)
        {
            if (def.Style == BoltStyle.HolyArrow)
            {
                float s0 = big ? VfxAnim.FinisherScale * Mathf.Lerp(ClericOrbStart, 1f, Mathf.Clamp01(travelled / ClericOrbGrowUnits)) : 1f;
                return BulletFlightScale(s0, Mathf.Min(travelled, tailRoom), projFrames[0]);
            }
            if (def.Style != BoltStyle.Plasma)
            {
                float s0 = big ? VfxAnim.FinisherScale : 1f;
                return new Vector3(s0, s0, 1f);
            }
            if (big) return MechanicCannonBlastFx.BoltFlightScale(travelled);
            float s = IsBigBolt ? BigBoltScale : def.Scale;
            return BulletFlightScale(s, travelled, projFrames[0]);
        }

        void Animate()
        {
            animT += Time.deltaTime;
            Sprite f = projFrames[(int)(animT * 16f) & 3];
            sr.sprite = glow.sprite = f;
            glow.enabled = FeelConfig.FxOn;
            glow.color = new Color(1f, 1f, 1f, VfxAnim.GlowAlpha);
            if (def.Style != BoltStyle.Plasma && def.Style != BoltStyle.HolyArrow) return;
            float travelled = reach - life * def.Speed;
            if (behindOrder != 0 && behindTopY > behindFeetY) { AnimateBand(travelled); return; }
            transform.localScale = FlightScale(travelled, finisher);
            if (behindOrder == 0) return;
            bool behind = travelled < behindUnits; // the Mechanic's hit 4 fired up
            sr.sortingOrder = behind ? behindOrder : FeelConfig.FxOrder - 10;
            glow.sortingOrder = behind ? behindOrder + 1 : FeelConfig.FxOrder - 9;
        }

        /// <summary>
        /// Cleric bolt over her figure (KeepBehindShooter): behind her while its drawn head is inside the band (feet .. hair top); once the head is out,
        /// in front, with the tail only as long as the part of the flight outside the band (the head's distance back to the band edge along the flight),
        /// so neither the lance coming down from the raised staff nor a shot auto-aimed up across her head ever lies over her face.
        /// </summary>
        void AnimateBand(float travelled)
        {
            float drawnY = pos.y + VisualLift;
            bool behind = drawnY > behindFeetY && drawnY < behindTopY;
            float room = float.MaxValue;
            if (!behind && dir.y > 0.01f && drawnY >= behindTopY) room = (drawnY - behindTopY) / dir.y;
            else if (!behind && dir.y < -0.01f && drawnY <= behindFeetY) room = (behindFeetY - drawnY) / -dir.y;
            transform.localScale = FlightScale(travelled, finisher, room);
            sr.sortingOrder = behind ? behindOrder : FeelConfig.FxOrder - 10;
            glow.sortingOrder = behind ? behindOrder + 1 : FeelConfig.FxOrder - 9;
        }

        // Sorting order that keeps the bolt behind the shooter (0 = off): the Mechanic's hit 4 fired up for its first behindUnits (head top ~20 units
        // over the feet, the shell starts 4 up); Cleric bolts fired up, or down from the raised staff, while over her figure (KeepBehindShooter).
        int behindOrder;
        float behindUnits, behindFeetY, behindTopY;
        const float MechanicBehindUnits = 18f, ClericOrbStart = 0.55f, ClericOrbGrowUnits = 12f;

        /// <summary>
        /// Picture only: draw this bolt behind a shooter standing at `feetY` while its drawn head is between her feet and `heightAbove` over them
        /// (Rpg.RangedAimRules.ClericBehindTop); `on` false = never (Rpg.RangedAimRules.ClericStartsBehind).
        /// </summary>
        public void KeepBehindShooter(float feetY, float heightAbove, bool on)
        {
            if (!on || vfx == null) return; // painted bolts only (headless server and the procedural fallback draw nothing to sort)
            behindOrder = GameConfig.OrderBase(feetY) - 2;
            behindFeetY = feetY;
            behindTopY = feetY + heightAbove;
            sr.sortingOrder = behindOrder;
            glow.sortingOrder = behindOrder + 1;
        }

        const float BulletTailMin = 0.15f;

        /// <summary>
        /// The Mechanic's bullets start at the gun muzzle: the tail grows out of the start point as the head travels instead of lying back across the
        /// hero (full length once the head is a tail length out). The tail length is the art's own: the head (pivot) to the left end. Picture only,
        /// the hit box is BoltDef W / H.
        /// </summary>
        static Vector3 BulletFlightScale(float scale, float travelled, Sprite art)
        {
            float tailUnits = art.pivot.x / art.pixelsPerUnit;
            return new Vector3(scale * Mathf.Clamp(travelled / (tailUnits * scale), BulletTailMin, 1f), scale, 1f);
        }

        /// <summary>
        /// Painted impact row at the hit point (cleric pillar anchored on the ground, others centred); the finisher is 1.35x with the monster-hit ring /
        /// white flash and embers on top. The Mechanic's cannon blast plays the v2 set's blast row sized to the blast radius; its big hit-3 bolt a 1.35x impact.
        /// </summary>
        void PaintedHitFx(Vector2 at, bool blast)
        {
            Vector2 ground = def.Style == BoltStyle.HolyArrow ? pos + new Vector2(0f, -2f) : at;
            // the Mechanic's cannon, the Wizard's orb (v3 orb burst) and the Cleric's orb (v2 judgment burst) play their blast row sized to the blast radius, with a small impact on top
            if (blast && (def.Style == BoltStyle.Plasma || def.Style == BoltStyle.Arcane || def.Style == BoltStyle.HolyArrow) && VfxAnim.Blast(vfx, ground, def.ExplodeRadius)) VfxAnim.Impact(vfx, ground, false);
            else VfxAnim.Impact(vfx, ground, blast || IsBigBolt);
            if (!blast || !FeelConfig.FxOn) return;
            UltimateFx.Embers(at, 10, 6f, 30f, new Color(pal.Mid.r, pal.Mid.g, pal.Mid.b, 1f));
        }
    }
}

using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Painted-sprite (VfxSet) projectile loop and impact, see VfxSprites.</summary>
    public partial class ComboProjectile
    {
        /// <summary>Painted projectile: 4-frame loop pointing right (pivot on the bright head), rotated to the travel direction.</summary>
        void BeginPainted(float angleDeg, bool big)
        {
            animT = 0f;
            sr.enabled = glow.enabled = true;
            sr.sprite = glow.sprite = vfx.Rows[VfxSet.Proj][0];
            sr.color = Color.white;
            sr.sortingOrder = FeelConfig.FxOrder - 10;
            glow.sortingOrder = FeelConfig.FxOrder - 9;
            float s = (big ? VfxAnim.FinisherScale : 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            transform.localScale = new Vector3(s, s, 1f);
            transform.position = new Vector3(pos.x, pos.y + VisualLift, 0f);
            gameObject.SetActive(true);
            Animate();
        }

        void Animate()
        {
            animT += Time.deltaTime;
            Sprite f = vfx.Rows[VfxSet.Proj][(int)(animT * 16f) & 3];
            sr.sprite = glow.sprite = f;
            glow.enabled = FeelConfig.FxOn;
            glow.color = new Color(1f, 1f, 1f, VfxAnim.GlowAlpha);
        }

        /// <summary>Painted impact row at the hit point (cleric pillar anchored on the ground, others centred); the finisher is 1.35x with the monster-hit ring / white flash and embers on top.</summary>
        void PaintedHitFx(Vector2 at, bool blast)
        {
            Vector2 ground = def.Style == BoltStyle.HolyArrow ? pos + new Vector2(0f, -2f) : at;
            VfxAnim.Impact(vfx, ground, blast);
            if (!blast || !FeelConfig.FxOn) return;
            UltimateFx.Embers(at, 10, 6f, 30f, new Color(pal.Mid.r, pal.Mid.g, pal.Mid.b, 1f));
        }
    }
}

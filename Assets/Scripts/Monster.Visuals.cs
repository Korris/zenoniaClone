using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Monster sprite / bar presentation.</summary>
    public partial class Monster
    {
        /// <summary>Attack frame index (0..3) like the web renderer, or -1 when not striking.</summary>
        int AttackIndex()
        {
            if (state == State.Windup) return timer > def.WindupMs * 0.5f ? 0 : 1;
            if (state == State.Cooldown)
            {
                float elapsed = def.CooldownMs - timer;
                if (elapsed >= 0f && elapsed < RecoveryMs) return 2 + Mathf.FloorToInt(elapsed / (RecoveryMs * 0.5f));
            }
            return -1;
        }

        /// <summary>
        /// Hit reaction: with effects on the sprite flashes pure white via the SpriteFlash shader (held through the hit-stop freeze)
        /// and jitters a little; off = the old red tint.
        /// </summary>
        void ApplyHitReaction(float alpha)
        {
            bool fx = FeelConfig.FxOn && flash > 0f;
            body.color = !fx && flash > 0f ? new Color(1f, 0.55f, 0.55f, alpha) : new Color(1f, 1f, 1f, alpha);
            FeelMaterials.SetFlash(body, fx ? Mathf.Clamp01(flash / (FeelConfig.FlashMs * 0.6f)) : 0f);
            float jitter = fx ? (Time.frameCount % 2 == 0 ? FeelConfig.HitJitterUnits : -FeelConfig.HitJitterUnits) : 0f;
            body.transform.localPosition = new Vector3(jitter, 0f, 0f);
        }

        void Render()
        {
            int order = GameConfig.OrderBase(y);
            float alpha = 1f;
            if (state == State.Dying)
            {
                alpha = 1f - Mathf.Clamp01(deathTimer / FadeMs);
                if (alpha <= 0f) { body.enabled = false; shadow.enabled = false; barBack.enabled = false; barFill.enabled = false; return; }
            }
            Sprite[] attackRow = SpriteSheet.Row(attack, facing);
            Sprite[] walkRow = SpriteSheet.Row(walk, facing);
            int ai = AttackIndex();
            Sprite s = null;
            if (flash <= 0f && ai >= 0 && attackRow != null && ai < attackRow.Length) s = attackRow[ai];
            else if (walkRow != null && walkRow.Length > 0)
            {
                bool still = state == State.Idle || state == State.Cooldown;
                s = walkRow[still ? 0 : Mathf.FloorToInt(animTimer / WalkFrameMs) % walkRow.Length];
            }
            body.sprite = s;
            ApplyHitReaction(alpha);
            body.sortingOrder = order + 3;
            shadow.sortingOrder = order;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * alpha);

            bool showBar = state != State.Dying && hp < def.Hp;
            barBack.enabled = showBar; barFill.enabled = showBar;
            if (showBar)
            {
                float top = def.BodyHeightUnits + 2f;
                barBack.transform.localPosition = new Vector3(-7f, top, 0f);
                barBack.transform.localScale = new Vector3(14f, 2f, 1f);
                barFill.transform.localPosition = new Vector3(-7f, top, 0f);
                barFill.transform.localScale = new Vector3(14f * hp / def.Hp, 1.4f, 1f);
                barBack.sortingOrder = order + 6;
                barFill.sortingOrder = order + 7;
            }
        }
    }
}

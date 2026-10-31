using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Status effects of a monster (slow, chill, freeze, stun, burn, poison, mark): the rules live in the engine-free MonsterStatus; this half applies
    /// them to the simulation (authority only), turns DoT ticks into credited hits, and draws the tint / mark reticle / stun stars (RendersFx only).
    /// </summary>
    public partial class Monster
    {
        const float ChillPct = 50f;               // Ice Storm: half speed
        readonly MonsterStatus status = new MonsterStatus();
        Action<object, float> dotSink;
        SpriteRenderer markSr;
        SpriteRenderer[] stars;

        public MonsterStatus Status { get { return status; } }
        /// <summary>Facing code 0 down, 1 up, 2 left, 3 right (the "behind" bonus test).</summary>
        public int FacingCode { get { return facing; } }
        /// <summary>Height of the painted head above the feet (where an overhead mark sits).</summary>
        public float OverheadUnits { get { return def.BodyHeightUnits * def.RenderScale + AirLift(); } }

        /// <summary>
        /// Applies a status (authority only; bosses convert freeze / stun into a slow). Knockback and launch are not statuses: use TakeHit / Launch.
        /// `src` owns damage over time and the mark. Returns true when the status took hold.
        /// </summary>
        public bool ApplyStatus(StatusSpec spec, HeroController src)
        {
            if (IsDying || proxy || !NetMode.IsAuthority) return false;
            return status.Apply(spec, src, def.IsBoss, src != null ? src.AtkStat : 0f);
        }

        /// <summary>Ice Storm: slows movement and tints the sprite blue for the given time.</summary>
        public void Chill(float ms) { ApplyStatus(new StatusSpec(StatusKind.Chill, ChillPct, Mathf.RoundToInt(ms)), null); }

        float SpeedScale { get { return status.SpeedFactor; } }

        /// <summary>One damage-over-time tick: a quiet hit credited to the caster (EXP and loot ownership follow the damage table).</summary>
        void OnDot(object src, float dmg)
        {
            TakeHit(Mathf.Max(1, Mathf.RoundToInt(dmg)), Vector2.zero, false, false, true, src as HeroController);
        }

        /// <summary>Sprite tint: freeze blue > burn orange > poison green > slow / chill light blue.</summary>
        Color StatusTint
        {
            get
            {
                switch (IsDying ? StatusKind.None : status.Dominant)
                {
                    case StatusKind.Freeze: return new Color(0.5f, 0.78f, 1f);
                    case StatusKind.Burn: return new Color(1f, 0.6f, 0.32f);
                    case StatusKind.Poison: return new Color(0.62f, 1f, 0.58f);
                    case StatusKind.Chill:
                    case StatusKind.Slow: return new Color(0.68f, 0.86f, 1f);
                    default: return Color.white;
                }
            }
        }

        /// <summary>Mark reticle on the body and spinning stars above the head while stunned. Children are created on first use.</summary>
        void UpdateStatusOverlays(int order)
        {
            bool mark = !IsDying && status.Has(StatusKind.Mark), stun = !IsDying && status.Has(StatusKind.Stun);
            if (mark && markSr == null)
            {
                markSr = ProceduralSprites.Child(transform, "MarkReticle", StatusOverlaySprites.Reticle());
                markSr.color = new Color(1f, 1f, 1f, 0.85f);
            }
            if (markSr != null)
            {
                markSr.enabled = mark && !DeathMarkFx.Covers(this); // the painted Death Mark replaces the reticle while it stands here
                markSr.transform.localPosition = new Vector3(0f, def.BodyHeightUnits * def.RenderScale * 0.45f + AirLift(), 0f);
                markSr.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(animTimer * 0.012f));
                markSr.sortingOrder = order + 8;
            }
            if (stun && stars == null)
            {
                stars = new SpriteRenderer[3];
                for (int i = 0; i < stars.Length; i++) stars[i] = ProceduralSprites.Child(transform, "StunStar" + i, StatusOverlaySprites.Star());
            }
            if (stars == null) return;
            float top = def.BodyHeightUnits * def.RenderScale + 3f + AirLift();
            for (int i = 0; i < stars.Length; i++)
            {
                float a = animTimer * 0.006f + i * 2.094f;
                stars[i].enabled = stun;
                stars[i].transform.localPosition = new Vector3(Mathf.Cos(a) * 6f, top + Mathf.Sin(a) * 1.6f, 0f);
                stars[i].sortingOrder = order + 8;
            }
        }
    }
}

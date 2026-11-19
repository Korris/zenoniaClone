using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Wanted Boss freeze cue on the hero's own body sprite (docs/wanted-boss-skills-design.md). A light blue tint, same family as
    /// Monster.Status.cs's own slow / chill tint, so the body still reads as iced where the frozen-block.png overlay's hollow middle lets it
    /// show through -- and is the whole cue by itself when that sheet is not painted yet (sprite-rules.md "Hạn chế fallback" / "Khi hình chưa có").
    /// HeroController.WantedFreezeFx calls this every Render() frame, after Show() has already set body.color for the frame.
    /// 2026-11-19: the layered RO Fantasy hero draws through HeroLayeredView (body sprite empty), so both tints also go there (MultiplyTint).
    /// </summary>
    public partial class HeroRenderer
    {
        static readonly Color FreezeTint = new Color(0.68f, 0.86f, 1f);
        static readonly Color PoisonTint = new Color(0.55f, 0.85f, 0.25f); // olive, blended in by `amount` (HeroController.WantedPoisonFx.cs)

        public void SetWantedFreezeTint(bool on) { if (on) MultiplyFigure(FreezeTint); }

        /// <summary>Wanted boss poison: blends the figure's colour toward olive by `amount` (0.08 others .. 0.2 self, 0.7 for the drain flash).
        /// Called after Show every frame, like the freeze tint, so it never accumulates.</summary>
        public void SetWantedPoisonTint(float amount)
        {
            if (amount > 0f) MultiplyFigure(Color.Lerp(Color.white, PoisonTint, Mathf.Clamp01(amount)));
        }

        void MultiplyFigure(Color m)
        {
            if (layered != null) layered.MultiplyTint(m);
            if (body == null || body.sprite == null) return;
            Color c = body.color;
            body.color = new Color(c.r * m.r, c.g * m.g, c.b * m.b, c.a);
        }
    }
}

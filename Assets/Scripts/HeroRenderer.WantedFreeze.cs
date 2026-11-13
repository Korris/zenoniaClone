using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Wanted Boss freeze cue on the hero's own body sprite (docs/wanted-boss-skills-design.md). A light blue tint, same family as
    /// Monster.Status.cs's own slow / chill tint, so the body still reads as iced where the frozen-block.png overlay's hollow middle lets it
    /// show through -- and is the whole cue by itself when that sheet is not painted yet (sprite-rules.md "Hạn chế fallback" / "Khi hình chưa có").
    /// HeroController.WantedFreezeFx calls this every Render() frame, after Show() has already set body.color for the frame.
    /// </summary>
    public partial class HeroRenderer
    {
        static readonly Color FreezeTint = new Color(0.68f, 0.86f, 1f);

        public void SetWantedFreezeTint(bool on)
        {
            if (!on || body == null) return;
            Color c = body.color;
            body.color = new Color(c.r * FreezeTint.r, c.g * FreezeTint.g, c.b * FreezeTint.b, c.a);
        }
    }
}

namespace ZenoniaProto
{
    /// <summary>
    /// Same-spot stack for your own hits on one monster (ChatGPT, RO-style spec 2026-11-20, chat "So sánh số sát thương RPG",
    /// https://chatgpt.com/c/6b0015c8-84c8-83ec-9236-67e79b0751b7): every hit pops at the same point above the monster, with no sideways dodge;
    /// older numbers keep rising and fading, so hits 400 ms apart read as a vertical stack on their own. At most 3 plain numbers live per
    /// monster: a 4th fades the oldest out in 80 ms. Crits use the same point and draw on top (second pass in DamageNumber).
    /// Replaces the two-column zigzag "Damage Stream" of the old multi-hit combo (user 20/11: "2 số dam kế nhau, đáng nhẽ phải nhảy cùng 1 chỗ").
    /// </summary>
    public partial class DamageNumber
    {
        const int MaxPlainPerTarget = 3;
        const float CullFadeMs = 80f;

        static void StreamPlace(ref Entry e, object key)
        {
            e.OffX = 0f;
            e.OffY = 0f;
            e.Key = key;
            if (e.Kind == DamageNumberKind.Crit) return; // crits are never culled and never cull
            int live = 0, oldest = -1;
            float oldestAge = -1f;
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active || Pool[i].Kind == DamageNumberKind.Crit || !ReferenceEquals(Pool[i].Key, key)) continue;
                live++;
                if (Pool[i].AgeMs > oldestAge) { oldestAge = Pool[i].AgeMs; oldest = i; }
            }
            if (live < MaxPlainPerTarget || oldest < 0) return;
            float life = DamageNumberMotion.Of(Pool[oldest].Kind).LifeMs;
            if (Pool[oldest].AgeMs < life - CullFadeMs) Pool[oldest].AgeMs = life - CullFadeMs; // fade out fast instead of popping away
        }
    }
}

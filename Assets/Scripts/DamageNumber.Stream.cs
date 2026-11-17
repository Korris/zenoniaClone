using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// "Damage Stream" (ChatGPT V3 Multi-Hit Barrage, docs/reference/warrior-skill-redesign-v2-chatgpt.md): the plain hits on one monster climb two
    /// zigzag columns of 24 fixed slots above its head instead of searching for free space, so a fast multi-hit skill reads as one pouring stream.
    /// Numbers may overlap (that is the effect); the newest draws on top. A monster's slot counter restarts after 1.2 s without a hit.
    /// Crits and finishers keep the normal layout (centre, they push the others aside).
    /// </summary>
    public partial class DamageNumber
    {
        const int StreamSlots = 24; // a tall column like Ragnarok Midgard Senki's
        const float StreamResetSeconds = 1.2f, StreamStepPx = 11f, StreamNearPx = 24f, StreamFarPx = 32f; // scaled with the 30 px numbers

        struct StreamState { public int Index; public float LastAt; }

        static readonly Dictionary<object, StreamState> Streams = new Dictionary<object, StreamState>();

        static void StreamPlace(ref Entry e, object key)
        {
            float now = Time.unscaledTime;
            StreamState st;
            if (!Streams.TryGetValue(key, out st) || now - st.LastAt > StreamResetSeconds) st.Index = 0;
            int slot = st.Index % StreamSlots, row = slot / 2;
            e.OffX = (slot % 2 == 0 ? -1f : 1f) * (row % 2 == 0 ? StreamNearPx : StreamFarPx);
            e.OffY = slot * StreamStepPx;
            st.Index++;
            st.LastAt = now;
            Streams[key] = st;
            if (Streams.Count > 64) PruneStreams(now); // dead monsters leave their keys behind
        }

        static readonly List<object> Stale = new List<object>();

        static void PruneStreams(float now)
        {
            Stale.Clear();
            foreach (KeyValuePair<object, StreamState> kv in Streams) if (now - kv.Value.LastAt > StreamResetSeconds) Stale.Add(kv.Key);
            foreach (object k in Stale) Streams.Remove(k);
        }
    }
}

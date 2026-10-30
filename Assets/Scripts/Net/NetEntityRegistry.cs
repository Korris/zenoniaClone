using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Zone-wide NetId space shared by heroes, monsters and loot (one id = one entity, so a Despawn message needs no kind). The authority hands ids out
    /// (Alloc); a client registers the ids the server sent. A released id is quarantined for 10 s of zone time so a late packet can never address a new entity.
    /// </summary>
    public sealed class NetEntityRegistry
    {
        public const float QuarantineMs = 10000f;
        readonly Dictionary<ushort, object> map = new Dictionary<ushort, object>(512);
        readonly HashSet<ushort> quarantined = new HashSet<ushort>();
        readonly Queue<KeyValuePair<ushort, float>> expiry = new Queue<KeyValuePair<ushort, float>>();
        ushort next = 1;

        public int Count { get { return map.Count; } }

        /// <summary>Next free id (never 0, skips used + quarantined ids) bound to `entity`; 0 when all 65535 ids are taken.</summary>
        public ushort Alloc(object entity)
        {
            for (int guard = 0; guard < 65536; guard++)
            {
                if (next == 0) next = 1;
                ushort id = next++;
                if (map.ContainsKey(id) || quarantined.Contains(id)) continue;
                map[id] = entity;
                return id;
            }
            return 0;
        }

        /// <summary>Client side: binds a server-chosen id.</summary>
        public void Register(ushort id, object entity) { if (id != 0) map[id] = entity; }

        public object Get(ushort id) { object o; return map.TryGetValue(id, out o) ? o : null; }
        public T Get<T>(ushort id) where T : class { return Get(id) as T; }

        /// <summary>Frees the id (quarantined until nowMs + 10 s).</summary>
        public void Release(ushort id, float nowMs)
        {
            if (id == 0 || !map.Remove(id)) return;
            quarantined.Add(id);
            expiry.Enqueue(new KeyValuePair<ushort, float>(id, nowMs + QuarantineMs));
        }

        public void Tick(float nowMs)
        {
            while (expiry.Count > 0 && expiry.Peek().Value <= nowMs) quarantined.Remove(expiry.Dequeue().Key);
        }

        public void Clear() { map.Clear(); quarantined.Clear(); expiry.Clear(); }
    }
}

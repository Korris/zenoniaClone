using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Every hero standing in a zone (the local player, party members, remote players on the server, proxies on a client) with a per-zone NetId
    /// from the zone's shared registry. Monsters, the spawner and loot ask it instead of a single-hero static: nearest living hero, any hero near
    /// a point, lookup by id.
    /// </summary>
    public sealed class ZoneHeroes
    {
        readonly List<HeroController> all = new List<HeroController>();
        readonly NetEntityRegistry ids;
        readonly Zone zone;

        public ZoneHeroes(NetEntityRegistry registry, Zone owner) { ids = registry; zone = owner; }

        public IReadOnlyList<HeroController> All { get { return all; } }
        public int Count { get { return all.Count; } }

        /// <summary>Adds a hero and assigns its NetId (authority), or binds the server's id (client proxy: pass `serverId`).</summary>
        public void Add(HeroController hero, ushort serverId = 0)
        {
            if (hero == null || all.Contains(hero)) return;
            if (serverId != 0) { hero.NetId = serverId; ids.Register(serverId, hero); }
            else hero.NetId = ids.Alloc(hero);
            all.Add(hero);
        }

        public void Remove(HeroController hero)
        {
            if (!all.Remove(hero)) return;
            ids.Release(hero.NetId, zone.TimeMs);
        }

        public HeroController ById(ushort id)
        {
            return id == 0 ? null : ids.Get(id) as HeroController;
        }

        /// <summary>Closest living hero within maxDist of `pos` (null when none).</summary>
        public HeroController NearestAlive(Vector2 pos, float maxDist)
        {
            HeroController best = null;
            float bestSq = maxDist * maxDist;
            for (int i = 0; i < all.Count; i++)
            {
                HeroController h = all[i];
                if (h == null || h.IsDead) continue;
                float sq = (h.Position - pos).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = h; }
            }
            return best;
        }

        /// <summary>True when any hero (alive or not) stands within `radius` of `pos`; the spawner uses it to keep respawns away from players.</summary>
        public bool AnyWithin(Vector2 pos, float radius)
        {
            float sq = radius * radius;
            for (int i = 0; i < all.Count; i++) if (all[i] != null && (all[i].Position - pos).sqrMagnitude < sq) return true;
            return false;
        }
    }
}

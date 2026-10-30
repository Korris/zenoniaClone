using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Every hero standing in a zone (the local player, party members, remote players on the server) with a per-zone NetId. Monsters, the spawner
    /// and loot ask it instead of a single-hero static: nearest living hero, any hero near a point, lookup by id.
    /// </summary>
    public sealed class ZoneHeroes
    {
        readonly List<HeroController> all = new List<HeroController>();
        ushort nextId = 1;

        public IReadOnlyList<HeroController> All { get { return all; } }
        public int Count { get { return all.Count; } }

        /// <summary>Adds a hero and assigns its NetId (never 0; wraps past 65535 skipping ids in use).</summary>
        public void Add(HeroController hero)
        {
            if (hero == null || all.Contains(hero)) return;
            for (int guard = 0; guard < 65536; guard++)
            {
                if (nextId == 0) nextId = 1;
                if (ById(nextId) == null) break;
                nextId++;
            }
            hero.NetId = nextId++;
            all.Add(hero);
        }

        public void Remove(HeroController hero) { all.Remove(hero); }

        public HeroController ById(ushort id)
        {
            for (int i = 0; i < all.Count; i++) if (all[i].NetId == id) return all[i];
            return null;
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

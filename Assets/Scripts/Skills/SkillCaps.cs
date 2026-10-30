using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Concurrency caps per caster (server CPU / screen clutter): 3 live casts, 2 minions, 4 zones. Over the cap the OLDEST one ends.</summary>
    public static class SkillCaps
    {
        public const int MaxRuns = 3, MaxMinions = 2, MaxZones = 4;
        static readonly Dictionary<HeroController, Dictionary<int, List<MonoBehaviour>>> Live = new Dictionary<HeroController, Dictionary<int, List<MonoBehaviour>>>();

        /// <summary>Registers a live object of `kind` (cap = limit) for a caster, destroying the oldest ones past the limit.</summary>
        public static void Add(HeroController owner, int limit, MonoBehaviour obj)
        {
            if (owner == null) return;
            Dictionary<int, List<MonoBehaviour>> kinds;
            if (!Live.TryGetValue(owner, out kinds)) Live[owner] = kinds = new Dictionary<int, List<MonoBehaviour>>();
            List<MonoBehaviour> list;
            if (!kinds.TryGetValue(limit, out list)) kinds[limit] = list = new List<MonoBehaviour>();
            list.RemoveAll(o => o == null);
            list.Add(obj);
            while (list.Count > limit) { if (list[0] != null) Object.Destroy(list[0].gameObject); list.RemoveAt(0); }
        }

        /// <summary>Drops bookkeeping of heroes that no longer exist (called when a cast starts).</summary>
        public static void Prune()
        {
            List<HeroController> gone = null;
            foreach (HeroController h in Live.Keys) if (h == null) (gone ?? (gone = new List<HeroController>())).Add(h);
            if (gone != null) foreach (HeroController h in gone) Live.Remove(h);
        }
    }
}

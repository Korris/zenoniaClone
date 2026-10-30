using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    public enum LearnResult { Ok, MaxRank, LevelTooLow, PrereqMissing, NoSp }

    /// <summary>
    /// Skill ranks of one character (0-10 per tree slot). SP is derived, never stored: free = level - sum(ranks), so a tampered save can only
    /// ever lose ranks (Sanitize), never gain points.
    /// </summary>
    public sealed class SkillRanks
    {
        public string ClassId { get; private set; }
        readonly byte[] ranks = new byte[SkillDef.PerClass];

        public SkillRanks(string classId) { ClassId = classId; }

        public int this[int index] { get { return index >= 0 && index < ranks.Length ? ranks[index] : 0; } }

        public int Sum { get { int s = 0; foreach (byte r in ranks) s += r; return s; } }

        public int Free(int level) { return level - Sum; }

        /// <summary>Why a skill can (not) be learned / ranked up right now. Checked in the order max rank, level, prerequisites, SP.</summary>
        public LearnResult CanLearn(SkillDef def, int level)
        {
            if (def == null || def.Class != ClassId) return LearnResult.PrereqMissing;
            if (this[def.Index] >= SkillDef.MaxRank) return LearnResult.MaxRank;
            if (level < def.UnlockLevel) return LearnResult.LevelTooLow;
            if (!PrereqsMet(def)) return LearnResult.PrereqMissing;
            return Free(level) < 1 ? LearnResult.NoSp : LearnResult.Ok;
        }

        /// <summary>Spends 1 SP on the skill (learn or rank up). Returns the CanLearn verdict; changes nothing unless Ok.</summary>
        public LearnResult Learn(SkillDef def, int level)
        {
            LearnResult r = CanLearn(def, level);
            if (r == LearnResult.Ok) ranks[def.Index]++;
            return r;
        }

        public void Reset() { Array.Clear(ranks, 0, ranks.Length); }

        bool PrereqsMet(SkillDef def)
        {
            foreach (SkillReq p in def.Prereqs) if (this[p.Index] < p.Rank) return false;
            return true;
        }

        /// <summary>
        /// Makes any rank set legal for a level: clamp 0..10, drop ranks whose unlock level / prerequisites fail, then while sum &gt; level remove one
        /// rank at a time from the newest skill (highest unlock level, then highest index). Deterministic, so server and client agree.
        /// </summary>
        public void Sanitize(int level)
        {
            SkillDef[] defs = SkillTable.ForClass(ClassId);
            if (defs.Length == 0) { Reset(); return; }
            for (int i = 0; i < ranks.Length; i++) ranks[i] = (byte)Math.Min((int)ranks[i], SkillDef.MaxRank);
            DropIllegal(defs, level);
            while (Sum > level)
            {
                SkillDef newest = null;
                foreach (SkillDef d in defs)
                    if (ranks[d.Index] > 0 && (newest == null || d.UnlockLevel > newest.UnlockLevel || (d.UnlockLevel == newest.UnlockLevel && d.Index > newest.Index))) newest = d;
                if (newest == null) break;
                ranks[newest.Index]--;
                DropIllegal(defs, level);
            }
        }

        /// <summary>Single pass in unlock order (prerequisites always unlock earlier, so one pass settles chains).</summary>
        void DropIllegal(SkillDef[] defs, int level)
        {
            var order = new List<SkillDef>(defs);
            order.Sort((a, b) => a.UnlockLevel != b.UnlockLevel ? a.UnlockLevel.CompareTo(b.UnlockLevel) : a.Index.CompareTo(b.Index));
            foreach (SkillDef d in order)
                if (ranks[d.Index] > 0 && (level < d.UnlockLevel || !PrereqsMet(d))) ranks[d.Index] = 0;
        }

        // ---- save format: 8 ints ----

        public List<int> ToList() { var l = new List<int>(ranks.Length); foreach (byte r in ranks) l.Add(r); return l; }

        /// <summary>Loads raw save ints (null / short / negative / huge tolerated) and sanitizes them for the level.</summary>
        public void Load(IList<int> raw, int level)
        {
            Reset();
            if (raw != null) for (int i = 0; i < ranks.Length && i < raw.Count; i++) ranks[i] = (byte)Math.Max(0, Math.Min(raw[i], SkillDef.MaxRank));
            Sanitize(level);
        }
    }
}

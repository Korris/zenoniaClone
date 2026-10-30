using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The skill magazine: 8 slots (page 1 slot 1 is always Dash, so 3 pages x 3 = Dash + these 8) each holding a tree index or -1.
    /// A skill sits in at most one slot and only if it is learned.
    /// </summary>
    public sealed class SkillLoadout
    {
        public const int Slots = SkillDef.PerClass;
        readonly sbyte[] slots = new sbyte[Slots];

        public SkillLoadout() { Clear(); }

        public int this[int slot] { get { return slot >= 0 && slot < Slots ? slots[slot] : -1; } }

        public int SlotOf(int skillIndex)
        {
            for (int i = 0; i < Slots; i++) if (slots[i] == skillIndex) return i;
            return -1;
        }

        /// <summary>Puts a learned skill into a slot (moving it out of its old slot, replacing the occupant). False if unlearned / out of range.</summary>
        public bool Assign(int slot, int skillIndex, SkillRanks ranks)
        {
            if (slot < 0 || slot >= Slots || skillIndex < 0 || skillIndex >= SkillDef.PerClass || ranks[skillIndex] <= 0) return false;
            int old = SlotOf(skillIndex);
            if (old >= 0) slots[old] = -1;
            slots[slot] = (sbyte)skillIndex;
            return true;
        }

        public void Unassign(int slot) { if (slot >= 0 && slot < Slots) slots[slot] = -1; }

        /// <summary>Newly learned skill goes into the first empty slot (no-op when already placed or the magazine is full). Returns the slot or -1.</summary>
        public int AutoFill(int skillIndex)
        {
            if (skillIndex < 0 || skillIndex >= SkillDef.PerClass) return -1;
            int at = SlotOf(skillIndex);
            if (at >= 0) return at;
            for (int i = 0; i < Slots; i++) if (slots[i] < 0) { slots[i] = (sbyte)skillIndex; return i; }
            return -1;
        }

        public void Clear() { for (int i = 0; i < Slots; i++) slots[i] = -1; }

        /// <summary>Drops slots holding unlearned skills and duplicates (first occurrence wins).</summary>
        public void Sanitize(SkillRanks ranks)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < Slots; i++)
                if (slots[i] >= 0 && (ranks[slots[i]] <= 0 || !seen.Add(slots[i]))) slots[i] = -1;
        }

        public List<int> ToList() { var l = new List<int>(Slots); foreach (sbyte s in slots) l.Add(s); return l; }

        /// <summary>Loads raw save ints (null / short / out-of-range tolerated), then sanitizes against the ranks.</summary>
        public void Load(IList<int> raw, SkillRanks ranks)
        {
            Clear();
            if (raw != null) for (int i = 0; i < Slots && i < raw.Count; i++) slots[i] = raw[i] >= 0 && raw[i] < SkillDef.PerClass ? (sbyte)raw[i] : (sbyte)-1;
            Sanitize(ranks);
        }
    }
}

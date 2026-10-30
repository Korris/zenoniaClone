using System;
using System.Collections.Generic;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>One slot of a magazine page: the fixed Dash, a learned tree skill (SkillIndex) or empty / locked.</summary>
    public struct SkillSlot
    {
        public GameKey Key;
        public int SkillIndex;   // tree index 0-7 when this slot holds a skill, else -1
        public bool Locked;
        public static readonly SkillSlot Lock = new SkillSlot { Locked = true, SkillIndex = -1 };
        public static SkillSlot Of(GameKey key) { return new SkillSlot { Key = key, SkillIndex = -1 }; }
        public static SkillSlot Skill(int index) { return new SkillSlot { Key = GameKey.Skill1 + index, SkillIndex = index }; }
        public bool IsSkill { get { return !Locked && SkillIndex >= 0; } }
    }

    /// <summary>
    /// Data for the touch "skill magazine": 3 pages x 3 slots = the fixed Dash (page 1 slot 1) + the 8 player-assigned loadout slots
    /// (PlayerState.Loadout, edited in the skill tree window). Swiping the cluster cycles pages; the selected page is remembered per class for
    /// the session. A tapped skill presses GameKey.Skill1+index, the same key the keyboard slots and the network input frame use.
    /// </summary>
    public static class SkillMagazine
    {
        public const int SlotCount = 3, MaxPages = 3;

        /// <summary>A skill slot was tapped (tree index). Default: press that skill key for the hero (HeroController.TryCastSkill, offline and online alike).</summary>
        public static Action<int> CastRequested = DefaultCast;

        static readonly Dictionary<HeroClass, int> Selected = new Dictionary<HeroClass, int>();

        public static int PageCount(HeroClassDef cls) { return MaxPages; }

        public static int CurrentPage(HeroClassDef cls)
        {
            int p;
            Selected.TryGetValue(cls.Id, out p);
            return p % PageCount(cls);
        }

        /// <summary>Moves the selected page by dir (wraps) and returns the new page index.</summary>
        public static int Cycle(HeroClassDef cls, int dir)
        {
            int n = PageCount(cls);
            int p = ((CurrentPage(cls) + dir) % n + n) % n;
            Selected[cls.Id] = p;
            return p;
        }

        /// <summary>Slot of a page: flat index 0 = Dash, 1-8 = loadout slot (empty = locked look).</summary>
        public static SkillSlot Slot(HeroClassDef cls, int page, int slot)
        {
            int flat = (page % MaxPages) * SlotCount + slot;
            if (flat == 0) return SkillSlot.Of(GameKey.Dash);
            PlayerState p = RpgSession.Player;
            if (p == null || p.ClassId != RpgSession.ClassId(cls.Id)) return SkillSlot.Lock;
            int index = p.Loadout[flat - 1];
            return index >= 0 && p.Skills[index] > 0 ? SkillSlot.Skill(index) : SkillSlot.Lock;
        }

        /// <summary>Changes whenever ranks / loadout / class change, so the view rebinds its slots.</summary>
        public static int Version()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return 0;
            int h = p.ClassId.GetHashCode();
            for (int i = 0; i < SkillDef.PerClass; i++) h = h * 31 + p.Loadout[i] * 11 + p.Skills[i];
            return h;
        }

        /// <summary>Tree skill in a slot, or null.</summary>
        public static SkillDef Def(SkillSlot s)
        {
            PlayerState p = RpgSession.Player;
            return s.IsSkill && p != null ? SkillTable.Get(p.ClassId, s.SkillIndex) : null;
        }

        static void DefaultCast(int index) { TouchControls.Press(GameKey.Skill1 + index); }
    }
}

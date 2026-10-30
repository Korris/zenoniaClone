using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>One slot of a magazine page: a bound GameKey (Dash, Skill ...) or locked / empty.</summary>
    public struct SkillSlot
    {
        public GameKey Key;
        public bool Locked;
        public static readonly SkillSlot Lock = new SkillSlot { Locked = true };
        public static SkillSlot Of(GameKey key) { return new SkillSlot { Key = key }; }
    }

    /// <summary>
    /// Data for the touch "skill magazine": per class a list of pages, each page = SlotCount entries. Swiping the cluster cycles pages.
    /// New skills: add a GameKey (or skill id) and put a SkillSlot.Of(...) in the table below; nothing else in the UI changes.
    /// Keyboard keys (K / L) always work regardless of the visible page. The selected page is remembered per class for the session.
    /// </summary>
    public static class SkillMagazine
    {
        public const int SlotCount = 3, MaxPages = 2;

        // Default loadout for every class. A Skill entry becomes a locked slot for classes without a signature skill (Mechanic).
        static readonly SkillSlot[][] Default =
        {
            new[] { SkillSlot.Of(GameKey.Dash), SkillSlot.Of(GameKey.Skill), SkillSlot.Lock },
            new[] { SkillSlot.Lock, SkillSlot.Lock, SkillSlot.Lock },
        };

        // Per-class overrides (empty for now): Overrides[HeroClass.Wizard] = new[] { page1, page2 };
        static readonly Dictionary<HeroClass, SkillSlot[][]> Overrides = new Dictionary<HeroClass, SkillSlot[][]>();
        static readonly Dictionary<HeroClass, int> Selected = new Dictionary<HeroClass, int>();

        public static int PageCount(HeroClassDef cls) { return Table(cls).Length; }

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

        public static SkillSlot Slot(HeroClassDef cls, int page, int slot)
        {
            SkillSlot[][] t = Table(cls);
            SkillSlot s = t[page % t.Length][slot];
            if (!s.Locked && s.Key == GameKey.Skill && cls.SkillName == null) return SkillSlot.Lock;
            return s;
        }

        static SkillSlot[][] Table(HeroClassDef cls)
        {
            SkillSlot[][] t;
            return Overrides.TryGetValue(cls.Id, out t) ? t : Default;
        }
    }
}

using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Keyboard side of the skill magazine: U / I(L) / O press the three visible slots of the current page, which hold Dash or a learned skill.
    /// The press is reported as the key that slot stands for (Dash, or Skill1..Skill8 = the tree index), so the hero, the input sampler and the
    /// network frame only ever see skill indexes, however the press was made (keyboard, magazine tap, bot).
    /// </summary>
    public static partial class InputAdapter
    {
        static bool SlotKeyPressed(GameKey target)
        {
            if (target != GameKey.Dash && (target < GameKey.Skill1 || target > GameKey.Skill8)) return false;
            HeroController hero = LocalPlayer.Hero;
            if (hero == null) return false;
            for (int s = 0; s < SkillMagazine.SlotCount; s++)
            {
                if (!KeyPressed(GameKey.SlotKey1 + s)) continue;
                SkillSlot slot = SkillMagazine.Slot(hero.Class, SkillMagazine.CurrentPage(hero.Class), s);
                if (slot.IsSkill ? target == GameKey.Skill1 + slot.SkillIndex : (!slot.Locked && target == slot.Key)) return true;
            }
            return false;
        }
    }
}

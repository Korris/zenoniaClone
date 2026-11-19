#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// PC keys of the RO Soft HUD (RoHud.Pc: the web / PC layout with the RO look): the digit keys 1-8 cast the 8 loadout slots of the skill
    /// window directly (the ring shows 1-4 on its first page, 5-8 on the second; Tab turns the page), instead of the old hotbar's 1 / 2 / 3
    /// magazine slots. The press is reported as Skill1..Skill8 (the tree index), like a ring tap, so the hero and the net input never change.
    /// </summary>
    public static partial class InputAdapter
    {
        /// <summary>True while the digits belong to the RO loadout (the magazine slot keys then stay quiet).</summary>
        static bool RoLoadoutKeys { get { return webKeys && RoHud.Active; } }

        /// <summary>Capture runs: the loadout slot whose digit counts as pressed this frame (-1 = none), as a real key would.</summary>
        internal static int TestDigitSlot = -1;

        /// <summary>The digit of loadout slot `slot` (0-7) went down this frame.</summary>
        static bool DigitPressed(int slot)
        {
            if (slot == TestDigitSlot) return true;
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha1 + slot) || Input.GetKeyDown(KeyCode.Keypad1 + slot);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb[Key.Digit1 + slot].wasPressedThisFrame || kb[Key.Numpad1 + slot].wasPressedThisFrame);
#else
            return false;
#endif
        }

        /// <summary>Skill1..Skill8 asked by a digit: the skill in that loadout slot (learned, as the ring would cast it).</summary>
        static bool RoLoadoutKeyPressed(GameKey target)
        {
            if (target < GameKey.Skill1 || target > GameKey.Skill8) return false;
            HeroController hero = LocalPlayer.Hero;
            Rpg.PlayerState p = hero != null ? hero.Rpg : null;
            if (p == null) return false;
            for (int s = 0; s < Rpg.SkillLoadout.Slots && s < 8; s++)
            {
                int skill = p.Loadout[s];
                if (skill >= 0 && p.Skills[skill] > 0 && target == GameKey.Skill1 + skill && DigitPressed(s)) return true;
            }
            return false;
        }
    }
}

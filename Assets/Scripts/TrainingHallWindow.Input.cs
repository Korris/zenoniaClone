using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// TrainingHallWindow: taps and keys, the old window's handlers one for one (OnNode, OnLearn, OnAssign, OnSlot) so learning,
    /// ranking up and assigning ask ISkillTreeActions exactly as before. New: the arrow keys walk the statues (2 x 4); while a slot is
    /// being picked Left / Right move the arrow over the belt and J / Space places the skill there. Outside assign mode J / Space do
    /// nothing: a key must not spend a skill point by accident. The keys are read with InputAdapter.WindowPressed: this window is a
    /// modal, so the normal key gate keeps them from the hero.
    /// </summary>
    public sealed partial class TrainingHallWindow
    {
        void OnStatue(int index)
        {
            selected = index;
            assignMode = false;
            Refresh();
        }

        void OnLearn()
        {
            Say(SkillTreeActions.Current.Learn(selected));
            Refresh();
        }

        /// <summary>The scroll's Assign button: starts picking a slot for the selected skill, or cancels it.</summary>
        void OnAssign()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            assignMode = !assignMode && p.Skills[selected] > 0;
            if (assignMode) targetSlot = FirstFreeSlot(p);
            Refresh();
        }

        /// <summary>A belt slot (flat 0-8, 0 = Dash): places the selected skill while picking, otherwise selects the skill that sits there.</summary>
        void OnSlot(int flat)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            if (assignMode)
            {
                if (flat == 0) { Say(ActionResult.Fail("Dash is fixed in the first slot")); return; }
                assignMode = false;
                Say(SkillTreeActions.Current.Assign(flat - 1, selected));
            }
            else if (flat > 0 && p.Loadout[flat - 1] >= 0) selected = p.Loadout[flat - 1];
            Refresh();
        }

        void ReadKeys()
        {
            if (dragSkill >= 0 || respecPopup.activeSelf) return;
            int dx = (InputAdapter.WindowPressed(GameKey.Right) ? 1 : 0) - (InputAdapter.WindowPressed(GameKey.Left) ? 1 : 0);
            int dy = (InputAdapter.WindowPressed(GameKey.Down) ? 1 : 0) - (InputAdapter.WindowPressed(GameKey.Up) ? 1 : 0);
            if (assignMode)
            {
                if (dx != 0) targetSlot = (targetSlot - 1 + dx + SkillLoadout.Slots) % SkillLoadout.Slots + 1; // 1-8, round the belt
                if (InputAdapter.WindowPressed(GameKey.Attack)) OnSlot(targetSlot);
                return;
            }
            if (dx == 0 && dy == 0) return;
            int col = Mathf.Clamp(selected % Columns + dx, 0, Columns - 1), row = Mathf.Clamp(selected / Columns + dy, 0, SkillDef.PerClass / Columns - 1);
            if (row * Columns + col != selected) OnStatue(row * Columns + col);
        }
    }
}

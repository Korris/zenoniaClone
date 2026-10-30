namespace ZenoniaProto.Net
{
    /// <summary>
    /// Bits of one input frame (one client sim step, 1/30 s). Up..Right are the held direction keys; Attack..MpPotion are PRESSES that happened
    /// during this step (edge, not held), so a frame is self-contained and a replay of the same frames gives the same result.
    /// </summary>
    public static class InputBits
    {
        public const ushort Up = 1, Down = 2, Left = 4, Right = 8, Attack = 16, Dash = 32, Skill = 64, HpPotion = 128, MpPotion = 256;
        public const ushort MoveMask = Up | Down | Left | Right;
        /// <summary>Which of the 8 tree skills the Skill press means: 3 bits above the potion bits (so a frame stays 2 B).</summary>
        public const int SkillIndexShift = 9;
        public const ushort SkillIndexMask = 7 << SkillIndexShift;
        public const ushort AllMask = 511 | SkillIndexMask;

        /// <summary>`bits` with a press of tree skill `index` (0-7) added; out-of-range indexes are ignored.</summary>
        public static ushort WithSkill(ushort bits, int index)
        {
            if (index < 0 || index > 7) return bits;
            return (ushort)((bits & ~SkillIndexMask) | Skill | (index << SkillIndexShift));
        }

        /// <summary>Tree skill pressed in this frame, or -1.</summary>
        public static int SkillIndex(ushort bits) { return (bits & Skill) != 0 ? (bits & SkillIndexMask) >> SkillIndexShift : -1; }

        /// <summary>True when the frame presses tree skill `index`.</summary>
        public static bool PressesSkill(ushort bits, int index) { return SkillIndex(bits) == index; }

        /// <summary>
        /// Frame bits plus the press bits of frames the jitter buffer cut. The held directions are the frame's; presses accumulate, and when both carry a
        /// skill press the frame's own wins (indexes must never be OR-ed together).
        /// </summary>
        public static ushort Merge(ushort frame, ushort carried)
        {
            ushort skill = (frame & Skill) != 0 ? (ushort)(frame & (Skill | SkillIndexMask)) : (ushort)(carried & (Skill | SkillIndexMask));
            return (ushort)(((frame | carried) & ~(Skill | SkillIndexMask)) | skill);
        }
    }
}

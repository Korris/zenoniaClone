using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Cooldown timers of the 8 skills of one hero (engine-free). Capture / Restore let a predicting client rewind and replay them.</summary>
    public sealed class SkillCooldowns
    {
        public const int Slots = SkillDef.PerClass;
        readonly float[] left = new float[Slots];

        public bool Ready(int index) { return index >= 0 && index < Slots && left[index] <= 0f; }
        public float LeftMs(int index) { return index >= 0 && index < Slots ? left[index] : 0f; }

        public void Start(int index, float ms) { if (index >= 0 && index < Slots) left[index] = Math.Max(0f, ms); }

        public void Tick(float dtMs) { for (int i = 0; i < Slots; i++) if (left[i] > 0f) left[i] = Math.Max(0f, left[i] - dtMs); }

        public void Reset() { Array.Clear(left, 0, Slots); }

        public void Capture(float[] into) { Array.Copy(left, into, Slots); }
        public void Restore(float[] from) { Array.Copy(from, left, Slots); }
    }
}

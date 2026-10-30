using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>One firing of a skill effect: which effect, which repeat (strike hit / rain drop) and when (ms since the cast).</summary>
    public struct SkillStep
    {
        public int EffectIndex, HitIndex, AtMs;
        public SkillStep(int effect, int hit, int atMs) { EffectIndex = effect; HitIndex = hit; AtMs = atMs; }
    }

    /// <summary>
    /// Engine-free timeline of a skill: expands repeating effects (Strike Hits, Rain Count) into individual steps sorted by time, and tells the runner
    /// which steps are due. Identical on server, client replay and in the dotnet tests. Zones and projectiles repeat internally, they need one step.
    /// </summary>
    public static class SkillSchedule
    {
        /// <summary>Steps of all effects, ordered by time (ties keep effect order).</summary>
        public static SkillStep[] Build(SkillEffect[] effects)
        {
            var list = new List<SkillStep>();
            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffect e = effects[i];
                int repeats = Repeats(e);
                for (int h = 0; h < repeats; h++) list.Add(new SkillStep(i, h, e.AtMs + h * Math.Max(0, e.IntervalMs)));
            }
            for (int i = 1; i < list.Count; i++) // stable insertion sort: the lists are tiny
            {
                SkillStep s = list[i];
                int j = i - 1;
                while (j >= 0 && list[j].AtMs > s.AtMs) { list[j + 1] = list[j]; j--; }
                list[j + 1] = s;
            }
            return list.ToArray();
        }

        /// <summary>How many times an effect fires on its own: Strike = Hits, Rain = Count, everything else once.</summary>
        public static int Repeats(SkillEffect e)
        {
            switch (e.Kind)
            {
                case EffectKind.Strike: return Math.Max(1, e.Hits);
                case EffectKind.Rain: return Math.Max(1, e.Count);
                default: return 1;
            }
        }

        /// <summary>Index of the first step NOT yet due at `elapsedMs` (steps before `cursor` have already fired).</summary>
        public static int Advance(SkillStep[] steps, int cursor, int elapsedMs)
        {
            while (cursor < steps.Length && steps[cursor].AtMs <= elapsedMs) cursor++;
            return cursor;
        }

        /// <summary>Time of the last step (the runner stays alive at least this long).</summary>
        public static int LastStepMs(SkillStep[] steps) { return steps.Length == 0 ? 0 : steps[steps.Length - 1].AtMs; }
    }
}

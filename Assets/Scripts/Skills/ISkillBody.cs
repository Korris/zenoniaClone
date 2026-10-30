using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What the skill executor needs from a caster. HeroSkillBody adapts a HeroController through its public API; phase 04 may implement this
    /// on the hero itself (cast lock, invulnerability and prediction-aware motion). Everything here runs on the authority except Palette / IsLocal.
    /// </summary>
    public interface ISkillBody
    {
        HeroController Hero { get; }
        Vector2 Feet { get; }
        /// <summary>Facing code 0 down, 1 up, 2 left, 3 right.</summary>
        int Facing { get; }
        ushort NetId { get; }
        string ClassId { get; }
        ClassPalette Palette { get; }
        bool IsDead { get; }
        /// <summary>True for the hero this client controls (feel effects: shake, hit-stop).</summary>
        bool IsLocal { get; }
        /// <summary>Attack stat with the temporary buffs applied.</summary>
        float Atk { get; }
        BuffState Buffs { get; }
        void Face(Vector2 dir);
        /// <summary>Jumps to `dest` (caller checked the rect is free). False when refused.</summary>
        bool TryBlink(Vector2 dest);
        /// <summary>Moves the hero `units` along `dir` over `ms`, stopping at walls (Dash / Leap).</summary>
        void BeginForcedMove(Vector2 dir, float units, float ms);
        void AddInvuln(float ms);
        /// <summary>Restores HP (clamped to max); returns what was actually healed.</summary>
        float Heal(float amount);
    }
}

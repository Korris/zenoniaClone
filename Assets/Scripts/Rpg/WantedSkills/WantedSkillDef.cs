namespace ZenoniaProto.Rpg
{
    /// <summary>Ground shape of a Wanted Boss skill (docs/wanted-boss-skills-design.md): a quarter / cone, a circle, or a line from the boss.</summary>
    public enum WantedAreaShape { Circle, Cone, Line }

    /// <summary>
    /// One telegraphed skill of a daily Wanted Boss. Distances are in TILES (x WantedSkillArea.TileUnits at runtime, like SkillEffect); angles in
    /// degrees. OnPlayers = Count separate circles centred on distinct heroes' positions, locked when the telegraph starts (Broken Chain Slam,
    /// Crystal Fall, Poison Bloom, Queen's Toxic Reign); otherwise the shape is anchored at the boss, facing the locked direction (Cone / Line) or
    /// centred on it (Circle), optionally with SafeArcCount safe lanes of SafeArcDeg width cut out (the enrage skills: Tyrant's Blizzard 1 lane,
    /// Core Overload 2 opposite lanes). DamageMult 1.0 = one normal attack of that boss (the design doc's convention). CastRow indexes the boss's
    /// own "&lt;id&gt;-cast.png" (-1 = no dedicated cast picture: play the ordinary attack sheet, or nothing for a cone skill that already uses it).
    /// </summary>
    public sealed class WantedSkillDef
    {
        public int Id;
        public string Name;
        /// <summary>Two short English sentences for the Wanted board's "How to dodge" row: what the skill is, then what to do. Display only.</summary>
        public string Hint;
        public WantedAreaShape Shape;
        public bool OnPlayers;
        public float RadiusTiles, AngleDeg, LengthTiles, WidthTiles;
        /// <summary>OnPlayers: number of locked circles. Otherwise unused (SafeArcCount governs the safe-lane circle).</summary>
        public int Count = 1;
        public float SafeArcDeg;
        public int SafeArcCount;
        public int TelegraphMs, CooldownMs;
        public float DamageMult;
        public StatusKind StatusKind;
        public float StatusValue;
        public int StatusMs;
        public float KnockbackTiles;
        public int CastRow = -1;
        /// <summary>Own pose sheet of this skill ("&lt;sheet&gt;" + suffix, 4x4 cells like the attack sheet: rows down / up / left / right, 4 poses). null = use CastRow / the attack pose.
        /// Poses follow ChatGPT's animation spec: F1-F2 while the warning runs, F3 at the impact / launch, F4 for the rest (WantedSkillPose).</summary>
        public string PoseSheetSuffix;
        /// <summary>&gt; 0 = a CHARGE: the shape (a Line) is only the warning; when the telegraph ends the boss dashes along it for this many ms and hits each hero it reaches once (Monster.WantedCharge.cs, WantedChargeRules).</summary>
        public int ChargeMs;
        public bool Enrage;
        /// <summary>A phase change, not an attack (Elder Root Guardian's Awakening): it takes its warning time and plays its pose, but no shape is drawn and nobody is hit.</summary>
        public bool Transition;
        /// <summary>Venom Drain (WantedVenomDrain): only POISONED heroes inside the shape are hit, their poison is removed, and the boss heals
        /// DrainHealShare of the HP really taken, at most DrainHealCap x its max HP per cast. Not picked while no poisoned hero is inside.</summary>
        public bool DrainPoisoned;
        public float DrainHealShare, DrainHealCap;
    }
}

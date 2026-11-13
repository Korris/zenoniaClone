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
        public bool Enrage;
    }
}

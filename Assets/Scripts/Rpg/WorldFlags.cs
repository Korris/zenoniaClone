namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Taurus V4 (24/11/2026): the live game only holds the new Taurus world. The pre-Taurus world (Frost Hollow, Oakhaven, Rootburrow, old quests, Wanted Boss...)
    /// stays in the code but is switched off unless <see cref="LegacyEnabled"/> is set (`--legacy-maps` on the client / server, and the dotnet test projects, which still cover the old rules).
    /// Dependency-free so the Net test project can compile this one file next to the map sources.
    /// </summary>
    public static class WorldFlags
    {
        /// <summary>The map a new character starts on (offline, host, server). Phase 02 of the Taurus plan replaces the stub map behind this id with the real village.</summary>
        public const string StartMapId = "tauWillowbrookVillage";

        /// <summary>True = the pre-Taurus maps, quests and Wanted Boss are live again. Set once at launch (Bootstrap / ZoneServer), never during play.</summary>
        public static bool LegacyEnabled;
    }
}

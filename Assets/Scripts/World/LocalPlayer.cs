namespace ZenoniaProto
{
    /// <summary>
    /// The hero this process plays and looks through (offline hero, host / client own hero). A dedicated server has none. HeroController.Instance,
    /// GameSession.Hero, the HUD, touch controls and the camera all forward here instead of owning a static of their own.
    /// </summary>
    public static class LocalPlayer
    {
        public static HeroController Hero { get; private set; }
        public static PlayerRpg Rpg { get { return Hero != null ? Hero.Owner : null; } }

        public static void Set(HeroController hero) { Hero = hero; }
        /// <summary>Clears the slot only when `hero` still owns it (a replaced hero is destroyed after its successor is set).</summary>
        public static void Clear(HeroController hero) { if (Hero == hero) Hero = null; }
        public static void ResetState() { Hero = null; }
    }
}

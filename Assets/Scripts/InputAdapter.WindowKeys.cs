namespace ZenoniaProto
{
    /// <summary>InputAdapter: keys a modal window reads for itself.</summary>
    public static partial class InputAdapter
    {
        /// <summary>
        /// A key the open window itself reads (arrows / J inside the rebuilt bag, ArmoryWindow). Pressed() answers false for those while a
        /// modal is open, so the hero does not move behind the window; the window asks here instead. Typing in the chat still wins.
        /// One answer per key press (no auto-repeat, no mouse / touch attack): keyboard and the capture bot only.
        /// </summary>
        public static bool WindowPressed(GameKey k)
        {
            if (UiWindows.ChatTyping || WebKeyBlocked(k)) return false;
            return (BotPressed != null && BotPressed(k)) || KeyPressed(k);
        }
    }
}

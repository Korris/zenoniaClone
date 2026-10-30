namespace ZenoniaProto
{
    /// <summary>
    /// Shared state of the in-game windows (dialogue, quest offer, chat): decides when the hero must not react to keys / touch.
    /// InputAdapter reads BlocksGameplay, so WASD / attack typed into the chat or pressed inside a window never reaches the hero.
    /// </summary>
    public static class UiWindows
    {
        /// <summary>The NPC dialogue or the quest offer is open.</summary>
        public static bool DialogueOpen { get { return NpcDialogueBox.IsOpen || QuestOfferWindow.IsOpen; } }

        /// <summary>A modal window is open (dialogue, quest offer or skill tree): hero frozen, touch controls hidden, Esc closes it.</summary>
        public static bool ModalOpen { get { return DialogueOpen || SkillTreeWindow.IsOpen; } }

        /// <summary>The chat input field owns the keyboard.</summary>
        public static bool ChatTyping;

        /// <summary>An NPC is within talk range: E means "talk" and must not also change the weapon tier.</summary>
        public static bool InteractPrompt;

        public static bool BlocksGameplay { get { return ModalOpen || ChatTyping; } }

        public static void ResetState() { ChatTyping = false; InteractPrompt = false; }
    }
}

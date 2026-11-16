using System;
using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Shared state of the in-game windows (dialogue, quest offer, chat): decides when the hero must not react to keys / touch.
    /// InputAdapter reads BlocksGameplay, so WASD / attack typed into the chat or pressed inside a window never reaches the hero.
    /// </summary>
    public static class UiWindows
    {
        // Windows added after this list was written (the Adventure Paper screens, through UiScreens) register themselves here
        // instead of being added to ModalOpen by hand. Registrations are for the whole run: they survive map changes.
        static readonly List<Func<bool>> registeredOpen = new List<Func<bool>>();
        static readonly List<Action> registeredClose = new List<Action>();

        /// <summary>The NPC dialogue or the quest offer is open.</summary>
        public static bool DialogueOpen { get { return NpcDialogueBox.IsOpen || QuestOfferWindow.IsOpen; } }

        /// <summary>A modal window is open (dialogue, quest offer, quest list, skill tree, potion shop, trade or settings): hero frozen, touch controls hidden, Esc closes it.</summary>
        public static bool ModalOpen { get { return DialogueOpen || QuestLogWindow.IsOpen || SkillTreeWindow.IsOpen || PotionShopWindow.IsOpen || GearShopWindow.IsOpen || TradeWindow.IsOpen || SettingsWindow.IsOpen || PetWindow.IsOpen || PetGachaWindow.IsOpen || RegisteredOpen; } }

        /// <summary>A window registered with RegisterModal is open.</summary>
        public static bool RegisteredOpen
        {
            get
            {
                for (int i = 0; i < registeredOpen.Count; i++) if (registeredOpen[i]()) return true;
                return false;
            }
        }

        /// <summary>Makes a window modal: while `isOpen` answers true it counts in ModalOpen, and `close` runs when Esc / a lost hero closes the windows.</summary>
        public static void RegisterModal(Func<bool> isOpen, Action close)
        {
            registeredOpen.Add(isOpen);
            registeredClose.Add(close);
        }

        /// <summary>Closes every registered window (WindowsHost: Esc, or the hero is gone).</summary>
        public static void CloseRegistered()
        {
            for (int i = 0; i < registeredClose.Count; i++) if (registeredOpen[i]()) registeredClose[i]();
        }

        /// <summary>The chat input field owns the keyboard.</summary>
        public static bool ChatTyping;

        /// <summary>An NPC is within talk range: E means "talk" and must not also change the weapon tier.</summary>
        public static bool InteractPrompt;

        public static bool BlocksGameplay { get { return ModalOpen || ChatTyping; } }

        public static void ResetState() { ChatTyping = false; InteractPrompt = false; }
    }
}

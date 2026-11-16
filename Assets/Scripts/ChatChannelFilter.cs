using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Which chat lines the notice board lists. View = All (every channel whose filter chip is on, in the order they arrived) or one
    /// channel (only its lines). System lines (quests, the Wanted boss) have no chip: they show in All and in World, like the old World tab.
    /// Pure logic, no UI.
    /// </summary>
    public sealed class ChatChannelFilter
    {
        /// <summary>View value of the "All" ribbon; any other view is (int)ChatChannel of World / Map / Party / Whisper.</summary>
        public const int All = -1;
        public const int Channels = 4; // World, Map, Party, Whisper: the channels a player can read and send on

        public int View = All;
        readonly bool[] on = { true, true, true, true };

        public bool IsOn(ChatChannel c) { return on[(int)c]; }
        public void Toggle(ChatChannel c) { on[(int)c] = !on[(int)c]; }
        public void Enable(ChatChannel c) { on[(int)c] = true; }
        public void EnableAll() { for (int i = 0; i < Channels; i++) on[i] = true; }

        public bool AllOn
        {
            get
            {
                for (int i = 0; i < Channels; i++) if (!on[i]) return false;
                return true;
            }
        }

        public bool Shows(ChatChannel c)
        {
            if (c == ChatChannel.System) return View == All || View == (int)ChatChannel.World;
            return View == All ? on[(int)c] : (int)c == View;
        }

        /// <summary>Fills `into` with the lines of `history` this filter shows, oldest first (history is already in arrival order).</summary>
        public void Collect(IList<ChatMessage> history, List<ChatMessage> into)
        {
            into.Clear();
            for (int i = 0; i < history.Count; i++)
                if (Shows(history[i].Channel)) into.Add(history[i]);
        }
    }
}
